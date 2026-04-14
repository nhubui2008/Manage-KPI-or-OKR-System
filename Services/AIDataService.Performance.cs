using System.Security.Claims;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public partial class AIDataService
    {
        public async Task<string> BuildPerformanceContextAsync(ClaimsPrincipal user, AnalyzePerformanceRequest request)
        {
            var scope = await BuildScopeAsync(user);
            EnsureEmployeeAccess(scope, request.EmployeeId);
            EnsureDepartmentAccess(scope, request.DepartmentId);
            var selectedPeriod = await GetSelectedPeriodAsync(request.PeriodId);

            var targetEmployeeIds = await ResolveTargetEmployeeIdsAsync(scope, request.EmployeeId, request.DepartmentId);
            var checkIns = _context.KPICheckIns.AsQueryable();
            checkIns = checkIns.Where(c => c.EmployeeId.HasValue && targetEmployeeIds.Contains(c.EmployeeId.Value));
            checkIns = ApplyPeriodToCheckIns(checkIns, selectedPeriod);

            var checkInRows = await checkIns.OrderByDescending(c => c.CheckInDate).ToListAsync();
            var checkInIds = checkInRows.Select(c => c.Id).ToList();
            var detailRows = await _context.CheckInDetails
                .Where(d => d.CheckInId.HasValue && checkInIds.Contains(d.CheckInId.Value))
                .ToListAsync();

            var employeeNames = await _context.Employees
                .Where(e => targetEmployeeIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.FullName ?? "N/A");
            var kpiIds = checkInRows.Where(c => c.KPIId.HasValue).Select(c => c.KPIId!.Value).Distinct().ToList();
            var kpiNames = await _context.KPIs
                .Where(k => kpiIds.Contains(k.Id))
                .ToDictionaryAsync(k => k.Id, k => k.KPIName ?? "N/A");

            var builder = NewContextHeader(scope, selectedPeriod);
            builder.AppendLine($"Pham vi phan tich: {targetEmployeeIds.Count} nhan vien, {checkInRows.Count} check-in.");

            if (!detailRows.Any())
            {
                builder.AppendLine("- Chua co du lieu chi tiet check-in de phan tich.");
                return builder.ToString();
            }

            var avgProgress = detailRows
                .Where(d => d.ProgressPercentage.HasValue)
                .Select(d => d.ProgressPercentage!.Value)
                .DefaultIfEmpty(0)
                .Average();
            builder.AppendLine($"Tien do trung binh: {Math.Round(avgProgress, 1)}%.");

            builder.AppendLine("Tong hop theo nhan vien:");
            var byEmployee = checkInRows
                .GroupBy(c => c.EmployeeId)
                .Select(g =>
                {
                    var ids = g.Select(c => c.Id).ToHashSet();
                    var details = detailRows.Where(d => d.CheckInId.HasValue && ids.Contains(d.CheckInId.Value)).ToList();
                    var progress = details
                        .Where(d => d.ProgressPercentage.HasValue)
                        .Select(d => d.ProgressPercentage!.Value)
                        .DefaultIfEmpty(0)
                        .Average();
                    return new
                    {
                        EmployeeId = g.Key,
                        Count = g.Count(),
                        AvgProgress = progress,
                        LastCheckIn = g.Max(c => c.CheckInDate)
                    };
                })
                .OrderByDescending(x => x.AvgProgress)
                .Take(12)
                .ToList();

            foreach (var item in byEmployee)
            {
                var name = item.EmployeeId.HasValue && employeeNames.ContainsKey(item.EmployeeId.Value) ? employeeNames[item.EmployeeId.Value] : "N/A";
                builder.AppendLine($"- {name}: {item.Count} check-in, progress TB {Math.Round(item.AvgProgress, 1)}%, check-in gan nhat {item.LastCheckIn:dd/MM/yyyy}.");
            }

            builder.AppendLine("Check-in gan day:");
            foreach (var checkIn in checkInRows.Take(10))
            {
                var detail = detailRows.FirstOrDefault(d => d.CheckInId == checkIn.Id);
                var employeeName = checkIn.EmployeeId.HasValue && employeeNames.ContainsKey(checkIn.EmployeeId.Value) ? employeeNames[checkIn.EmployeeId.Value] : "N/A";
                var kpiName = checkIn.KPIId.HasValue && kpiNames.ContainsKey(checkIn.KPIId.Value) ? kpiNames[checkIn.KPIId.Value] : "N/A";
                builder.AppendLine($"- {checkIn.CheckInDate:dd/MM/yyyy}: {employeeName}, KPI {kpiName}, progress {FormatDecimal(detail?.ProgressPercentage)}%, note {detail?.Note ?? "N/A"}.");
            }

            return builder.ToString();
        }

        public async Task<AIReviewContext> BuildReviewContextAsync(ClaimsPrincipal user, int evaluationResultId)
        {
            var scope = await BuildScopeAsync(user);
            var canWriteReview = scope.IsAdmin || scope.IsManager || scope.IsHR ||
                                 await HasPermissionAsync(user, "EVALRESULTS_CREATE", "EVALRESULTS_EDIT");

            if (!canWriteReview)
            {
                return new AIReviewContext { IsAllowed = false };
            }

            var result = await _context.EvaluationResults.FindAsync(evaluationResultId);
            if (result == null)
            {
                return new AIReviewContext { IsAllowed = false };
            }

            if (!scope.CanSeeAll && !scope.IsHR && result.EmployeeId.HasValue && !scope.EmployeeIds.Contains(result.EmployeeId.Value))
            {
                return new AIReviewContext { IsAllowed = false };
            }

            var employee = result.EmployeeId.HasValue ? await _context.Employees.FindAsync(result.EmployeeId.Value) : null;
            var period = result.PeriodId.HasValue ? await _context.EvaluationPeriods.FindAsync(result.PeriodId.Value) : null;
            var rank = result.RankId.HasValue ? await _context.GradingRanks.FindAsync(result.RankId.Value) : null;
            var perfContext = await BuildPerformanceContextAsync(user, new AnalyzePerformanceRequest { EmployeeId = result.EmployeeId, PeriodId = result.PeriodId });

            var builder = NewContextHeader(scope, period);
            builder.AppendLine($"Ket qua danh gia: employee #{employee?.Id} {employee?.FullName}; ky {period?.PeriodName}; tong diem {FormatDecimal(result.TotalScore)}; rank {rank?.RankCode ?? "N/A"}; phan loai {result.Classification ?? "N/A"}.");
            builder.AppendLine($"Nhan xet hien tai: {result.ReviewComment ?? "Chua co"}.");
            builder.AppendLine("Du lieu hieu suat thuc te:");
            builder.AppendLine(perfContext);

            return new AIReviewContext { IsAllowed = true, ContextText = builder.ToString() };
        }
    }
}
