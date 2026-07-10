using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class MissionVisionsController : Controller
    {
        private static readonly string[] ManagementPermissions =
        {
            "MISSIONS_CREATE",
            "MISSIONS_EDIT",
            "MISSIONS_DELETE"
        };

        private readonly MiniERPDbContext _context;

        public MissionVisionsController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HasPermission("MISSIONS_VIEW")]
        public async Task<IActionResult> Index(int? year, bool allYears = false)
        {
            var activeMissions = _context.MissionVisions
                .AsNoTracking()
                .Where(m => m.IsActive == true);
            var availableYears = await activeMissions
                .Where(m => m.MissionVisionType == MissionVision.TypeYearlyGoal && m.TargetYear.HasValue)
                .Select(m => m.TargetYear!.Value)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            int? defaultYear = availableYears.Contains(DateTime.Now.Year)
                ? DateTime.Now.Year
                : availableYears.Select(value => (int?)value).FirstOrDefault();
            if (year.HasValue && !availableYears.Contains(year.Value))
            {
                return defaultYear.HasValue
                    ? RedirectToAction(nameof(Index), new { year = defaultYear.Value })
                    : RedirectToAction(nameof(Index));
            }

            int? selectedYear = allYears ? null : year ?? defaultYear;
            var longTermStatements = await activeMissions
                .Where(m => m.MissionVisionType == MissionVision.TypeVision ||
                            m.MissionVisionType == MissionVision.TypeMission)
                .OrderBy(m => m.MissionVisionType == MissionVision.TypeVision ? 0 : 1)
                .ThenBy(m => m.CreatedAt)
                .ToListAsync();
            var yearlyGoalsQuery = activeMissions
                .Where(m => m.MissionVisionType == MissionVision.TypeYearlyGoal && m.TargetYear.HasValue);
            if (selectedYear.HasValue)
            {
                yearlyGoalsQuery = yearlyGoalsQuery.Where(m => m.TargetYear == selectedYear.Value);
            }

            var yearlyGoals = await yearlyGoalsQuery
                .OrderByDescending(m => m.TargetYear)
                .ThenByDescending(m => m.CreatedAt)
                .ToListAsync();
            var permissions = await PermissionLookupHelper.HasPermissionsAsync(
                _context,
                User,
                ManagementPermissions);

            return View(new MissionVisionIndexViewModel
            {
                LongTermStatements = longTermStatements,
                YearlyGoals = yearlyGoals,
                AvailableYears = availableYears,
                SelectedYear = selectedYear,
                ShowAllYears = allYears || !selectedYear.HasValue,
                CanCreateMission = permissions["MISSIONS_CREATE"],
                CanEditMission = permissions["MISSIONS_EDIT"],
                CanDeleteMission = permissions["MISSIONS_DELETE"]
            });
        }

        [HttpGet]
        [HasPermission("MISSIONS_CREATE")]
        public IActionResult Create(string? type = null)
        {
            var selectedType = type is MissionVision.TypeVision or MissionVision.TypeMission or MissionVision.TypeYearlyGoal
                ? type
                : MissionVision.TypeYearlyGoal;
            return View(new MissionVision
            {
                MissionVisionType = selectedType,
                TargetYear = selectedType == MissionVision.TypeYearlyGoal ? DateTime.Now.Year : null
            });
        }

        [HttpPost]
        [HasPermission("MISSIONS_CREATE")]
        public async Task<IActionResult> Create(MissionVision model)
        {
            PrepareMissionVisionForSave(model);
            await ValidateMissionVisionAsync(model);

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.CreatedById = GetCurrentUserId();
                model.IsActive = true;
                _context.MissionVisions.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã lưu chiến lược thành công!";
                return RedirectToMissionIndex(model);
            }

            return View(model);
        }

        [HttpGet]
        [HasPermission("MISSIONS_EDIT")]
        public async Task<IActionResult> Edit(int id)
        {
            var missionVision = await _context.MissionVisions
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive == true);

            if (missionVision == null) return NotFound();

            return View(missionVision);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("MISSIONS_EDIT")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MissionVisionType,TargetYear,Content,FinancialTarget")] MissionVision model)
        {
            if (id != model.Id) return NotFound();

            var existingMissionVision = await _context.MissionVisions
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive == true);
            if (existingMissionVision == null) return NotFound();

            PrepareMissionVisionForSave(model);
            await ValidateMissionVisionAsync(model, existingMissionVision);

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra, vui lòng kiểm tra lại dữ liệu.";
                return View(model);
            }

            existingMissionVision.MissionVisionType = model.MissionVisionType;
            existingMissionVision.TargetYear = model.TargetYear;
            existingMissionVision.Content = model.Content;
            existingMissionVision.FinancialTarget = model.FinancialTarget;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật mục tiêu chiến lược thành công!";

            return RedirectToMissionIndex(existingMissionVision);
        }

        [HttpPost]
        [HasPermission("MISSIONS_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            var missionVision = await _context.MissionVisions
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive == true);
            if (missionVision == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy mục tiêu chiến lược đang hoạt động.";
                return RedirectToAction(nameof(Index));
            }

            var linkedEmployeeCount = await _context.Employees
                .CountAsync(e => e.IsActive == true && e.StrategicGoalId == id);
            if (linkedEmployeeCount > 0)
            {
                TempData["ErrorMessage"] = $"Không thể vô hiệu hóa mục tiêu vì đang được {linkedEmployeeCount} nhân viên sử dụng.";
                return RedirectToMissionIndex(missionVision);
            }

            missionVision.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã vô hiệu hóa mục tiêu chiến lược.";
            return RedirectToMissionIndex(missionVision);
        }

        private void PrepareMissionVisionForSave(MissionVision model)
        {
            var requestedType = model.MissionVisionType?.Trim();
            model.MissionVisionType = requestedType switch
            {
                MissionVision.TypeVision => MissionVision.TypeVision,
                MissionVision.TypeMission => MissionVision.TypeMission,
                MissionVision.TypeYearlyGoal => MissionVision.TypeYearlyGoal,
                _ => MissionVision.TypeYearlyGoal
            };
            if (requestedType != model.MissionVisionType)
            {
                ModelState.AddModelError(nameof(model.MissionVisionType), "Loại thiết lập không hợp lệ.");
            }

            model.Content = model.Content?.Trim();

            if (model.MissionVisionType != MissionVision.TypeYearlyGoal)
            {
                model.TargetYear = null;
                ModelState.Remove(nameof(model.TargetYear));
            }
        }

        private async Task ValidateMissionVisionAsync(
            MissionVision model,
            MissionVision? existingMissionVision = null)
        {
            if (string.IsNullOrWhiteSpace(model.Content))
            {
                ModelState.AddModelError(nameof(model.Content), "Vui lòng nhập nội dung chiến lược.");
            }
            else if (model.Content.Length > 1000)
            {
                ModelState.AddModelError(nameof(model.Content), "Nội dung chiến lược không được vượt quá 1000 ký tự.");
            }

            if (model.MissionVisionType == MissionVision.TypeYearlyGoal && !model.TargetYear.HasValue)
            {
                ModelState.AddModelError(nameof(model.TargetYear), "Vui lòng nhập năm áp dụng cho mục tiêu theo năm.");
            }
            else if (model.TargetYear is < 2000 or > 2100)
            {
                ModelState.AddModelError(nameof(model.TargetYear), "Năm áp dụng phải nằm trong khoảng 2000 đến 2100.");
            }

            if (model.FinancialTarget < 0)
            {
                ModelState.AddModelError(nameof(model.FinancialTarget), "Mục tiêu tài chính không được là số âm.");
            }

            if (model.MissionVisionType is MissionVision.TypeVision or MissionVision.TypeMission)
            {
                var duplicateExists = await _context.MissionVisions
                    .AsNoTracking()
                    .AnyAsync(m => m.IsActive == true &&
                                   m.MissionVisionType == model.MissionVisionType &&
                                   (existingMissionVision == null || m.Id != existingMissionVision.Id));
                if (duplicateExists)
                {
                    ModelState.AddModelError(
                        nameof(model.MissionVisionType),
                        $"Đã có {model.TypeDisplayName} đang hoạt động. Hãy chỉnh sửa nội dung hiện tại thay vì tạo thêm.");
                }
            }

            if (existingMissionVision?.MissionVisionType == MissionVision.TypeYearlyGoal &&
                model.MissionVisionType != MissionVision.TypeYearlyGoal)
            {
                var hasLinkedEmployees = await _context.Employees
                    .AsNoTracking()
                    .AnyAsync(e => e.IsActive == true && e.StrategicGoalId == existingMissionVision.Id);
                if (hasLinkedEmployees)
                {
                    ModelState.AddModelError(
                        nameof(model.MissionVisionType),
                        "Không thể đổi loại vì mục tiêu đang được nhân viên sử dụng.");
                }
            }
        }

        private int? GetCurrentUserId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                ? userId
                : null;
        }

        private IActionResult RedirectToMissionIndex(MissionVision missionVision)
        {
            return missionVision.MissionVisionType == MissionVision.TypeYearlyGoal && missionVision.TargetYear.HasValue
                ? RedirectToAction(nameof(Index), new { year = missionVision.TargetYear })
                : RedirectToAction(nameof(Index));
        }
    }
}
