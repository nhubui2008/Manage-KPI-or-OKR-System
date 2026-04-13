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
    public class KPICheckInsController : Controller
    {
        private readonly MiniERPDbContext _context;

        public KPICheckInsController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HasPermission("KPICHECKINS_VIEW")]
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? systemUserId = int.TryParse(userIdStr, out int uid) ? uid : null;
            var employee = systemUserId.HasValue ? await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == systemUserId) : null;

            var checkInQuery = _context.KPICheckIns.AsQueryable();
            var kpiQuery = _context.KPIs.Where(k => k.IsActive == true && (k.StatusId == null || (k.StatusId != 0 && k.StatusId != 2)));
            var employeeQuery = _context.Employees.Where(e => e.IsActive == true);

            // Phân quyền: Employee, Sales chỉ thấy dữ liệu của chính mình
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                if (employee != null)
                {
                    checkInQuery = checkInQuery.Where(c => c.EmployeeId == employee.Id);
                    
                    var allocatedKpiIds = await _context.KPI_Employee_Assignments
                        .Where(a => a.EmployeeId == employee.Id && (a.Status == null || a.Status == "Active"))
                        .Select(a => a.KPIId)
                        .ToListAsync();
                    
                    kpiQuery = kpiQuery.Where(k => allocatedKpiIds.Contains(k.Id) || k.AssignerId == employee.Id);
                    employeeQuery = employeeQuery.Where(e => e.Id == employee.Id);
                }
                else
                {
                    // Nếu không tìm thấy thông tin Employee tương ứng, không cho thấy gì
                    checkInQuery = checkInQuery.Where(c => false);
                    kpiQuery = kpiQuery.Where(k => false);
                    employeeQuery = employeeQuery.Where(e => false);
                }
            }

            var checkIns = await checkInQuery
                .OrderByDescending(c => c.CheckInDate)
                .Take(50)
                .ToListAsync();
            
            var checkInIds = checkIns.Select(c => c.Id).ToList();

            var checkInDetails = await _context.CheckInDetails
                .Where(d => checkInIds.Contains(d.CheckInId ?? 0))
                .ToDictionaryAsync(d => d.CheckInId ?? 0);

            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id);
            var kpis = await _context.KPIs.ToDictionaryAsync(k => k.Id);
            var statuses = await _context.CheckInStatuses.ToDictionaryAsync(s => s.Id, s => s.StatusName);

            ViewBag.Details = checkInDetails;
            ViewBag.Employees = employees;
            ViewBag.KPIs = kpis;
            ViewBag.CheckInStatuses = statuses;
            ViewBag.AllEmployees = await employeeQuery.ToListAsync();
            ViewBag.AllKPIs = await kpiQuery.ToListAsync();
            ViewBag.AllStatuses = await _context.CheckInStatuses.ToListAsync();
            ViewBag.FailReasons = await _context.FailReasons.ToListAsync();

            return View(checkIns);
        }

        [HttpGet]
        [HasPermission("KPICHECKINS_CREATE")]
        public async Task<IActionResult> Create(int? kpiId)
        {
            await PopulateCreateViewBag();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? systemUserId = int.TryParse(userIdStr, out int uid) ? uid : null;
            var employee = systemUserId.HasValue ? await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == systemUserId) : null;

            var model = new KPICheckIn();
            if (kpiId.HasValue)
            {
                model.KPIId = kpiId.Value;
                
                // Lọc danh sách KPI để chỉ hiện thị KPI đã chọn (theo yêu cầu người dùng)
                var allKpis = ViewBag.AllKPIs as List<Manage_KPI_or_OKR_System.Models.KPI>;
                if (allKpis != null && allKpis.Any(k => k.Id == kpiId.Value))
                {
                    ViewBag.AllKPIs = allKpis.Where(k => k.Id == kpiId.Value).ToList();
                    
                    // Lọc cả KPIData để đảm bảo JS vẫn hoạt động đúng
                    var allKpiData = ViewBag.KPIData as Dictionary<int, Manage_KPI_or_OKR_System.Models.KPIDetail>;
                    if (allKpiData != null && allKpiData.ContainsKey(kpiId.Value))
                    {
                        var filteredData = new Dictionary<int, Manage_KPI_or_OKR_System.Models.KPIDetail>();
                        filteredData[kpiId.Value] = allKpiData[kpiId.Value];
                        ViewBag.KPIData = filteredData;
                    }
                }

                // Nếu là Employee, tự động gán EmployeeId
                if (employee != null && (User.IsInRole("Employee") || User.IsInRole("employee") ||
                                       User.IsInRole("Sales") || User.IsInRole("sales")))
                {
                    model.EmployeeId = employee.Id;
                }
            }

            return View(model);
        }

        private async Task PopulateCreateViewBag()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? systemUserId = int.TryParse(userIdStr, out int uid) ? uid : null;
            var employee = systemUserId.HasValue ? await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == systemUserId) : null;

            var kpiQuery = _context.KPIs.Where(k => k.IsActive == true && (k.StatusId == null || (k.StatusId != 0 && k.StatusId != 2)));
            var employeeQuery = _context.Employees.Where(e => e.IsActive == true);

            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                if (employee != null)
                {
                    var allocatedKpiIds = await _context.KPI_Employee_Assignments
                        .Where(a => a.EmployeeId == employee.Id && (a.Status == null || a.Status == "Active"))
                        .Select(a => a.KPIId)
                        .ToListAsync();

                    kpiQuery = kpiQuery.Where(k => allocatedKpiIds.Contains(k.Id) || k.AssignerId == employee.Id);
                    employeeQuery = employeeQuery.Where(e => e.Id == employee.Id);
                }
                else
                {
                    kpiQuery = kpiQuery.Where(k => false);
                    employeeQuery = employeeQuery.Where(e => false);
                }
            }

            ViewBag.AllEmployees = await employeeQuery.ToListAsync();
            ViewBag.AllKPIs = await kpiQuery.ToListAsync();
            ViewBag.AllStatuses = await _context.CheckInStatuses.ToListAsync();
            ViewBag.FailReasons = await _context.FailReasons.ToListAsync();

            // Fetch KPI details for real-time progress calculation in JS
            var kpis = (List<Manage_KPI_or_OKR_System.Models.KPI>)ViewBag.AllKPIs;
            var kpiIds = kpis.Select(k => k.Id).ToList();
            var kpiDetails = await _context.KPIDetails
                .Where(d => kpiIds.Contains(d.KPIId ?? 0))
                .ToDictionaryAsync(d => d.KPIId ?? 0);
            ViewBag.KPIData = kpiDetails;
        }

        [HttpPost]
        [HasPermission("KPICHECKINS_CREATE", "EMPLOYEE_UPDATE_KPI_PROGRESS")]
        public async Task<IActionResult> Create(KPICheckIn model, decimal AchievedValue, string Note)
        {
            bool isRestrictedRole = User.IsInRole("Employee") || User.IsInRole("employee") ||
                                    User.IsInRole("Sales") || User.IsInRole("sales");
            Employee? currentEmployee = null;

            if (AchievedValue < 0)
            {
                ModelState.AddModelError("AchievedValue", "Kết quả đạt được không thể là số âm.");
            }

            if (!model.EmployeeId.HasValue)
            {
                ModelState.AddModelError(nameof(model.EmployeeId), "Vui lòng chọn nhân viên check-in.");
            }

            if (!model.KPIId.HasValue)
            {
                ModelState.AddModelError(nameof(model.KPIId), "Vui lòng chọn KPI cần check-in.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateCreateViewBag();
                ViewBag.AchievedValue = AchievedValue;
                ViewBag.Note = Note;
                return View(model);
            }

            var selectedEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == model.EmployeeId!.Value && e.IsActive == true);
            if (selectedEmployee == null)
            {
                ModelState.AddModelError(nameof(model.EmployeeId), "Nhân viên được chọn không tồn tại hoặc đã ngừng hoạt động.");
            }

            var kpi = await _context.KPIs
                .FirstOrDefaultAsync(k => k.Id == model.KPIId!.Value && k.IsActive == true);
            if (kpi == null)
            {
                ModelState.AddModelError(nameof(model.KPIId), "KPI được chọn không tồn tại hoặc đã bị vô hiệu hóa.");
            }

            var kpiDetail = await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == model.KPIId);
            if (kpiDetail == null)
            {
                ModelState.AddModelError(nameof(model.KPIId), "KPI chưa có cấu hình chỉ tiêu nên chưa thể check-in.");
            }

            if (isRestrictedRole)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    currentEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId && e.IsActive == true);
                }

                if (currentEmployee == null || model.EmployeeId != currentEmployee.Id)
                {
                    return Forbid();
                }
            }

            if (kpi != null && model.EmployeeId.HasValue)
            {
                var isAssignedToKpi = await _context.KPI_Employee_Assignments
                    .AnyAsync(a => a.KPIId == kpi.Id &&
                                   a.EmployeeId == model.EmployeeId.Value &&
                                   (a.Status == null || a.Status == "Active"));

                if (!isAssignedToKpi && kpi.AssignerId != model.EmployeeId.Value)
                {
                    ModelState.AddModelError(nameof(model.KPIId), "Nhân viên này chưa được phân bổ KPI được chọn.");
                }

                if (kpi.StatusId == 0)
                {
                    ModelState.AddModelError(nameof(model.KPIId), "KPI đang chờ duyệt nên chưa thể check-in.");
                }
                else if (kpi.StatusId == 2)
                {
                    ModelState.AddModelError(nameof(model.KPIId), "KPI đã bị từ chối nên không thể check-in.");
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateCreateViewBag();
                ViewBag.AchievedValue = AchievedValue;
                ViewBag.Note = Note;
                return View(model);
            }

            var validatedKpi = kpi!;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lưu thông tin Check-in chính
                model.CheckInDate = DateTime.Now;
                _context.KPICheckIns.Add(model);
                await _context.SaveChangesAsync();

                // 2. Tính % tiến độ theo cấu hình KPI
                decimal progress = 0;
                if (kpiDetail != null)
                {
                    progress = ProgressHelper.CalculateProgress(AchievedValue, kpiDetail.TargetValue ?? 0, kpiDetail.IsInverse);
                }

                // Tự động gán trạng thái dựa trên tiến độ thực tế (1: On Track, 2: At Risk, 3: Late)
                if (progress >= 95) model.StatusId = 1;
                else if (progress >= 50) model.StatusId = 2;
                else model.StatusId = 3;

                // 3. Lưu thông tin chi tiết (Achieved Value, Note, Progress %)
                var detail = new CheckInDetail
                {
                    CheckInId = model.Id,
                    AchievedValue = AchievedValue,
                    Note = Note,
                    ProgressPercentage = Math.Round(progress, 2)
                };
                _context.CheckInDetails.Add(detail);

                // ============================================
                // 3.5 TỰ ĐỘNG CẬP NHẬT TRẠNG THÁI KPI
                //     dựa trên tiến độ so với mục tiêu
                // ============================================
                if (kpiDetail != null)
                {
                    decimal passThreshold = kpiDetail.PassThreshold ?? kpiDetail.TargetValue ?? 100;
                    decimal failThreshold = kpiDetail.FailThreshold ?? 0;

                    // Tính progress dựa trên PassThreshold (% đạt so với ngưỡng qua)
                    decimal passProgress = passThreshold > 0 
                        ? ProgressHelper.CalculateProgress(AchievedValue, passThreshold, kpiDetail.IsInverse)
                        : progress;

                    if (progress >= 100) 
                    {
                        // Đạt hoặc vượt mục tiêu
                        validatedKpi.StatusId = 4; // "Hoàn thành"
                    }
                    else if (passProgress >= 100 || progress >= 70)
                    {
                        // Vượt ngưỡng Pass hoặc >= 70% Target
                        validatedKpi.StatusId = 5; // "Gần đạt"
                    }
                    else if (progress >= 40)
                    {
                        // Đang triển khai nhưng chưa chắc đạt
                        validatedKpi.StatusId = 3; // "Đang thực hiện"
                    }
                    else
                    {
                        // Dưới 40% target → nguy cơ không đạt
                        validatedKpi.StatusId = 6; // "Không đạt"
                    }
                }
                else
                {
                    // Không có KPIDetail → mặc định "Đang thực hiện"
                    validatedKpi.StatusId = 3;
                }

                // 4. TÍNH TỔNG ĐIỂM (TotalScore) THEO TRỌNG SỐ CÁC KPI TRONG CÙNG KỲ
                var assignedKpiWeights = await _context.KPI_Employee_Assignments
                    .Where(a => a.EmployeeId == model.EmployeeId && (a.Status == null || a.Status == "Active"))
                    .Select(a => new { a.KPIId, Weight = a.Weight ?? 1m })
                    .ToListAsync();

                var assignedKpiIds = assignedKpiWeights.Select(a => a.KPIId).ToList();
                var weightByKpiId = assignedKpiWeights.ToDictionary(a => a.KPIId, a => a.Weight <= 0 ? 1m : a.Weight);

                var periodKpis = await _context.KPIs
                    .Where(k => k.PeriodId == validatedKpi.PeriodId && k.IsActive == true && assignedKpiIds.Contains(k.Id))
                    .ToListAsync();

                decimal totalScore = 0;
                if (periodKpis.Any())
                {
                    decimal weightedProgress = 0;
                    decimal totalWeight = 0;
                    foreach (var pk in periodKpis)
                    {
                        var weight = weightByKpiId.GetValueOrDefault(pk.Id, 1m);
                        totalWeight += weight;

                        var latestCheckIn = await _context.KPICheckIns
                            .Where(c => c.KPIId == pk.Id && c.EmployeeId == model.EmployeeId)
                            .OrderByDescending(c => c.CheckInDate)
                            .FirstOrDefaultAsync();

                        if (latestCheckIn != null)
                        {
                            var pkDetail = await _context.CheckInDetails.FirstOrDefaultAsync(d => d.CheckInId == latestCheckIn.Id);
                            if (pkDetail != null)
                            {
                                weightedProgress += (pkDetail.ProgressPercentage ?? 0) * weight;
                            }
                        }
                    }

                    if (totalWeight > 0)
                    {
                        totalScore = Math.Round(weightedProgress / totalWeight, 2);
                    }
                }

                // 5. MAP TIẾN ĐỘ VÀO BẢNG XẾP LOẠI (GradingRank)
                var rank = await _context.GradingRanks
                    .Where(r => r.MinScore <= totalScore)
                    .OrderByDescending(r => r.MinScore)
                    .FirstOrDefaultAsync();

                if (rank != null)
                {
                    // 6. CẬP NHẬT/TẠO KẾT QUẢ ĐÁNH GIÁ (EvaluationResult)
                    var evalResult = await _context.EvaluationResults
                        .FirstOrDefaultAsync(er => er.EmployeeId == model.EmployeeId && er.PeriodId == validatedKpi.PeriodId);

                    if (evalResult == null)
                    {
                        evalResult = new EvaluationResult
                        {
                            EmployeeId = model.EmployeeId,
                            PeriodId = validatedKpi.PeriodId,
                            TotalScore = totalScore, 
                            RankId = rank.Id,
                            Classification = rank.Description
                        };
                        _context.EvaluationResults.Add(evalResult);
                    }
                    else
                    {
                        evalResult.TotalScore = totalScore;
                        evalResult.RankId = rank.Id;
                        evalResult.Classification = rank.Description;
                    }

                    // 7. QUY ĐỔI THƯỞNG (BonusRule & RealtimeExpectedBonus)
                    var bonusRule = await _context.BonusRules.FirstOrDefaultAsync(br => br.RankId == rank.Id);
                    if (bonusRule != null)
                    {
                        var expectedBonus = await _context.RealtimeExpectedBonuses
                            .FirstOrDefaultAsync(rb => rb.EmployeeId == model.EmployeeId && rb.PeriodId == validatedKpi.PeriodId);

                        decimal fixedAmount = bonusRule.FixedAmount ?? 0;
                        decimal percentageAmount = fixedAmount != 0 && bonusRule.BonusPercentage.HasValue
                            ? fixedAmount * bonusRule.BonusPercentage.Value / 100m
                            : 0m;
                        decimal bonusAmount = fixedAmount + percentageAmount;
                        
                        if (expectedBonus == null)
                        {
                            expectedBonus = new RealtimeExpectedBonus
                            {
                                EmployeeId = model.EmployeeId,
                                PeriodId = validatedKpi.PeriodId,
                                ExpectedBonus = bonusAmount,
                                LastUpdated = DateTime.Now
                            };
                            _context.RealtimeExpectedBonuses.Add(expectedBonus);
                        }
                        else
                        {
                            expectedBonus.ExpectedBonus = bonusAmount;
                            expectedBonus.LastUpdated = DateTime.Now;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Đã thực hiện check-in KPI, cập nhật xếp hạng và quy đổi thưởng tự động thành công!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi khi lưu Check-in: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
