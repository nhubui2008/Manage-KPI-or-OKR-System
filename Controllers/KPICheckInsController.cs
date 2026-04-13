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
            try
            {
                // Đảm bảo cột IsInverse tồn tại trong database (fix lỗi schema mismatch cho KPI)
                await _context.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KPIDetails') AND name = 'IsInverse') ALTER TABLE KPIDetails ADD IsInverse bit NOT NULL DEFAULT 0;");
            }
            catch { }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? systemUserId = int.TryParse(userIdStr, out int uid) ? uid : null;
            var employee = systemUserId.HasValue ? await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == systemUserId) : null;

            var checkInQuery = _context.KPICheckIns.AsQueryable();
            var kpiQuery = _context.KPIs.Where(k => k.IsActive == true);
            var employeeQuery = _context.Employees.Where(e => e.IsActive == true);

            // Phân quyền: Employee, Warehouse, Sales chỉ thấy dữ liệu của chính mình
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                if (employee != null)
                {
                    checkInQuery = checkInQuery.Where(c => c.EmployeeId == employee.Id);
                    
                    var allocatedKpiIds = await _context.KPI_Employee_Assignments
                        .Where(a => a.EmployeeId == employee.Id)
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
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? systemUserId = int.TryParse(userIdStr, out int uid) ? uid : null;
            var employee = systemUserId.HasValue ? await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == systemUserId) : null;

            var kpiQuery = _context.KPIs.Where(k => k.IsActive == true);
            var employeeQuery = _context.Employees.Where(e => e.IsActive == true);

            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                if (employee != null)
                {
                    var allocatedKpiIds = await _context.KPI_Employee_Assignments
                        .Where(a => a.EmployeeId == employee.Id)
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
            var kpiIds = ((List<Manage_KPI_or_OKR_System.Models.KPI>)ViewBag.AllKPIs).Select(k => k.Id).ToList();
            var kpiDetails = await _context.KPIDetails
                .Where(d => kpiIds.Contains(d.KPIId ?? 0))
                .ToDictionaryAsync(d => d.KPIId ?? 0);
            ViewBag.KPIData = kpiDetails;

            var model = new KPICheckIn();
            if (kpiId.HasValue)
            {
                model.KPIId = kpiId.Value;
                // Nếu là Employee, tự động gán EmployeeId
                if (employee != null && (User.IsInRole("Employee") || User.IsInRole("employee") ||
                                       User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                                       User.IsInRole("Sales") || User.IsInRole("sales")))
                {
                    model.EmployeeId = employee.Id;
                }
            }

            return View(model);
        }

        [HttpPost]
        [HasPermission("KPICHECKINS_CREATE", "EMPLOYEE_UPDATE_KPI_PROGRESS")]
        public async Task<IActionResult> Create(KPICheckIn model, decimal AchievedValue, string Note)
        {
            if (AchievedValue < 0)
            {
                TempData["ErrorMessage"] = "Kết quả đạt được không thể là số âm.";
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Security: Employee chỉ được check-in cho chính mình
                if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                    User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                    User.IsInRole("Sales") || User.IsInRole("sales"))
                {
                    var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(userIdStr, out int userId))
                    {
                        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
                        if (employee == null || model.EmployeeId != employee.Id)
                        {
                            return Forbid();
                        }
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                                    .SelectMany(v => v.Errors)
                                    .Select(e => e.ErrorMessage));
                    TempData["ErrorMessage"] = "Dữ liệu không hợp lệ: " + errors;
                    return RedirectToAction(nameof(Index));
                }

                // 1. Lưu thông tin Check-in chính
                model.CheckInDate = DateTime.Now;
                _context.KPICheckIns.Add(model);
                await _context.SaveChangesAsync();

                // 2. Lấy thông tin KPI và Target để tính % tiến độ
                var kpi = await _context.KPIs.FindAsync(model.KPIId);
                if (kpi == null)
                {
                    throw new Exception("Không tìm thấy thông tin KPI tương ứng.");
                }

                var kpiDetail = await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == model.KPIId);
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
                        kpi.StatusId = 4; // "Hoàn thành"
                    }
                    else if (passProgress >= 100 || progress >= 70)
                    {
                        // Vượt ngưỡng Pass hoặc >= 70% Target
                        kpi.StatusId = 5; // "Gần đạt" 
                    }
                    else if (progress >= 40)
                    {
                        // Đang triển khai nhưng chưa chắc đạt
                        kpi.StatusId = 3; // "Đang thực hiện"
                    }
                    else
                    {
                        // Dưới 40% target → nguy cơ không đạt
                        kpi.StatusId = 6; // "Không đạt"
                    }
                }
                else
                {
                    // Không có KPIDetail → mặc định "Đang thực hiện"
                    kpi.StatusId = 3;
                }

                // 4. TÍNH TỔNG ĐIỂM (TotalScore) DỰA TRÊN TRUNG BÌNH CÁC KPI TRONG CÙNG KỲ
                var assignedKpiIds = await _context.KPI_Employee_Assignments
                    .Where(a => a.EmployeeId == model.EmployeeId)
                    .Select(a => a.KPIId)
                    .ToListAsync();

                var periodKpis = await _context.KPIs
                    .Where(k => k.PeriodId == kpi.PeriodId && k.IsActive == true && assignedKpiIds.Contains(k.Id))
                    .ToListAsync();

                decimal totalScore = 0;
                if (periodKpis.Any())
                {
                    decimal sumProgress = 0;
                    foreach (var pk in periodKpis)
                    {
                        var latestCheckIn = await _context.KPICheckIns
                            .Where(c => c.KPIId == pk.Id && c.EmployeeId == model.EmployeeId)
                            .OrderByDescending(c => c.CheckInDate)
                            .FirstOrDefaultAsync();

                        if (latestCheckIn != null)
                        {
                            var pkDetail = await _context.CheckInDetails.FirstOrDefaultAsync(d => d.CheckInId == latestCheckIn.Id);
                            if (pkDetail != null)
                            {
                                sumProgress += pkDetail.ProgressPercentage ?? 0;
                            }
                        }
                    }
                    totalScore = Math.Round(sumProgress / periodKpis.Count, 2);
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
                        .FirstOrDefaultAsync(er => er.EmployeeId == model.EmployeeId && er.PeriodId == kpi.PeriodId);

                    if (evalResult == null)
                    {
                        evalResult = new EvaluationResult
                        {
                            EmployeeId = model.EmployeeId,
                            PeriodId = kpi.PeriodId,
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
                            .FirstOrDefaultAsync(rb => rb.EmployeeId == model.EmployeeId && rb.PeriodId == kpi.PeriodId);

                        decimal bonusAmount = bonusRule.FixedAmount ?? 0;
                        
                        if (expectedBonus == null)
                        {
                            expectedBonus = new RealtimeExpectedBonus
                            {
                                EmployeeId = model.EmployeeId,
                                PeriodId = kpi.PeriodId,
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
