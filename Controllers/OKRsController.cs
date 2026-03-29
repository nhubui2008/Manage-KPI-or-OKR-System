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
            var okrs = await _context.OKRs
                .Where(o => o.IsActive == true)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var okrIds = okrs.Select(o => o.Id).ToList();
            
            var keyResults = await _context.OKRKeyResults
                .Where(k => okrIds.Contains(k.OKRId ?? 0))
                .ToListAsync();

            var krDict = new Dictionary<int, List<OKRKeyResult>>();
            foreach (var kr in keyResults)
            {
                if (kr.OKRId.HasValue)
                {
                    if (!krDict.ContainsKey(kr.OKRId.Value))
                        krDict[kr.OKRId.Value] = new List<OKRKeyResult>();
                    krDict[kr.OKRId.Value].Add(kr);
                }
            }

            ViewBag.KeyResults = krDict;

            return View(okrs);
        }

        [HttpPost]
        public async Task<IActionResult> Create(OKR model)
        {
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
                _context.OKRKeyResults.Add(kr);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm Kết quả Then chốt thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
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
