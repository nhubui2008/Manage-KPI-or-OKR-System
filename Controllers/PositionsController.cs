using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize] // Đã xóa [HasPermission], mở khóa hoàn toàn cho mọi người đã đăng nhập
    public class PositionsController : Controller
    {
        private readonly MiniERPDbContext _context;

        public PositionsController(MiniERPDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DANH SÁCH & LỌC (INDEX)
        // ==========================================
        public async Task<IActionResult> Index(string searchString)
        {
            // Lưu lại từ khóa để giữ trên thanh tìm kiếm
            ViewData["CurrentFilter"] = searchString;

            // Truy vấn các chức vụ chưa bị xóa
            var query = _context.Positions.Where(p => p.IsActive == true).AsQueryable();

            // LỌC (SEARCH)
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim().ToLower();
                query = query.Where(p => 
                    (p.PositionName != null && p.PositionName.ToLower().Contains(searchString)) ||
                    (p.PositionCode != null && p.PositionCode.ToLower().Contains(searchString))
                );
            }

            // Sắp xếp theo cấp bậc (RankLevel) tăng dần, rồi theo tên chức vụ
            var positions = await query
                .OrderBy(p => p.RankLevel)
                .ThenBy(p => p.PositionName)
                .ToListAsync();

            return View(positions);
        }

        // ==========================================
        // 2. CHI TIẾT (DETAILS)
        // ==========================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var position = await _context.Positions.FirstOrDefaultAsync(m => m.Id == id);
            if (position == null) return NotFound();

            return View(position);
        }

        // ==========================================
        // 3. THÊM MỚI (CREATE)
        // ==========================================
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Position position)
        {
            if (ModelState.IsValid)
            {
                position.IsActive = true;
                _context.Positions.Add(position);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm chức vụ thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(position);
        }

        // ==========================================
        // 4. CHỈNH SỬA (EDIT)
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var position = await _context.Positions.FindAsync(id);
            if (position == null) return NotFound();

            return View(position);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Position position)
        {
            if (id != position.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPos = await _context.Positions.FindAsync(id);
                    if (existingPos == null) return NotFound();

                    // Cập nhật thông tin
                    existingPos.PositionCode = position.PositionCode;
                    existingPos.PositionName = position.PositionName;
                    existingPos.RankLevel = position.RankLevel;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật chức vụ thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PositionExists(position.Id)) return NotFound();
                    else throw;
                }
            }
            return View(position);
        }

        // ==========================================
        // 5. XÓA MỀM (DELETE)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position != null)
            {
                // Chỉ đổi trạng thái IsActive thành false thay vì xóa hẳn (Soft Delete)
                position.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa chức vụ thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy chức vụ cần xóa!";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // Hàm kiểm tra tồn tại
        private bool PositionExists(int id)
        {
            return _context.Positions.Any(e => e.Id == id);
        }
    }
}