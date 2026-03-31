using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Security.Claims;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("MANAGER_CREATE_OKR")]
    public class OKRsController : Controller
    {
        private readonly MiniERPDbContext _context;

        public OKRsController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Đảm bảo cột CurrentValue tồn tại trong database (fix lỗi schema mismatch)
                await _context.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OKRKeyResults') AND name = 'CurrentValue') ALTER TABLE OKRKeyResults ADD CurrentValue decimal(18,2) NULL;");
            }
            catch { }

            var query = _context.OKRs.Where(o => o.IsActive == true).Include(o => o.KeyResults).AsQueryable();

            // Filter OKRs if Warehouse or Employee
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
                    if (employee != null)
                    {
                        var allocatedOkrIds = await _context.OKR_Employee_Allocations
                            .Where(a => a.EmployeeId == employee.Id)
                            .Select(a => a.OKRId)
                            .ToListAsync();
                        query = query.Where(o => allocatedOkrIds.Contains(o.Id) || o.CreatedById == employee.Id);
                    }
                    else
                    {
                        query = query.Where(o => false);
                    }
                }
            }

            var okrs = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            return View(okrs);
        }

        [HttpPost]
        public async Task<IActionResult> Create(OKR model)
        {
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee")) 
                return Forbid();

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.IsActive = true;
                _context.OKRs.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo OKR mới thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddKeyResult(OKRKeyResult kr)
        {
            if (ModelState.IsValid)
            {
                kr.CurrentValue = 0; // Khởi tạo tiến độ ban đầu là 0
                _context.OKRKeyResults.Add(kr);
                await _context.SaveChangesAsync();
                
                // Lấy thông tin OKR để tính toán tiến độ mới
                var okr = await _context.OKRs.Include(o => o.KeyResults).FirstOrDefaultAsync(o => o.Id == kr.OKRId);
                TempData["SuccessMessage"] = $"Đã thêm KR thành công! Tiến độ mục tiêu: {okr?.TotalProgress}%";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> EditKeyResult(OKRKeyResult model)
        {
            if (ModelState.IsValid)
            {
                var kr = await _context.OKRKeyResults.FindAsync(model.Id);
                if (kr != null)
                {
                    kr.KeyResultName = model.KeyResultName;
                    kr.TargetValue = model.TargetValue;
                    kr.CurrentValue = model.CurrentValue;
                    kr.Unit = model.Unit;
                    
                    await _context.SaveChangesAsync();
                    
                    var okr = await _context.OKRs.Include(o => o.KeyResults).FirstOrDefaultAsync(o => o.Id == kr.OKRId);
                    TempData["SuccessMessage"] = $"Đã cập nhật KR thành công! Tiến độ mục tiêu hiện tại: {okr?.TotalProgress}%";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteKeyResult(int id)
        {
            var kr = await _context.OKRKeyResults.FindAsync(id);
            if (kr != null)
            {
                int? okrId = kr.OKRId;
                _context.OKRKeyResults.Remove(kr);
                await _context.SaveChangesAsync();
                
                var okr = await _context.OKRs.Include(o => o.KeyResults).FirstOrDefaultAsync(o => o.Id == okrId);
                TempData["SuccessMessage"] = $"Đã xóa KR thành công! Tiến độ mục tiêu còn lại: {okr?.TotalProgress}%";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee")) 
                return Forbid();

            var okr = await _context.OKRs.FindAsync(id);
            if (okr != null)
            {
                okr.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa OKR!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
