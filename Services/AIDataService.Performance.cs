using System.Security.Claims;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public partial class AIDataService
    {
        public async Task<string> BuildPerformanceContextAsync(ClaimsPrincipal user, AnalyzePerformanceRequest request)
        {
            return (await BuildPerformanceAnalysisContextAsync(user, request)).Text;
        }

        public async Task<AuthorizedPerformanceContext> BuildPerformanceAnalysisContextAsync(
            ClaimsPrincipal user,
            AnalyzePerformanceRequest request)
        {
            var scope = await BuildScopeAsync(user);
            EnsureEmployeeAccess(scope, request.EmployeeId);
            EnsureDepartmentAccess(scope, request.DepartmentId);
            var selectedPeriod = await GetSelectedPeriodAsync(request.PeriodId);

            var targetEmployeeIds = await ResolveTargetEmployeeIdsAsync(scope, request.EmployeeId, request.DepartmentId);
            var checkIns = _context.KPICheckIns.AsNoTracking().AsQueryable();
            checkIns = checkIns.Where(c => c.EmployeeId.HasValue && targetEmployeeIds.Contains(c.EmployeeId.Value));
            // Pending, rejected, and unclassified submissions are never used
            // as official performance evidence.
            checkIns = OfficialCheckIns(checkIns);
            checkIns = ApplyPeriodToCheckIns(checkIns, selectedPeriod);

            var checkInRows = await checkIns
                .OrderByDescending(c => c.CheckInDate)
                .ThenByDescending(c => c.Id)
                .ToListAsync();
            var checkInIds = checkInRows.Select(c => c.Id).ToList();
            var detailRows = await _context.CheckInDetails
                .AsNoTracking()
                .Where(d => d.CheckInId.HasValue && checkInIds.Contains(d.CheckInId.Value))
                .ToListAsync();

            var kpiIds = checkInRows.Where(c => c.KPIId.HasValue).Select(c => c.KPIId!.Value).Distinct().ToList();
            var kpiNames = await _context.KPIs
                .AsNoTracking()
                .Where(k => kpiIds.Contains(k.Id))
                .ToDictionaryAsync(k => k.Id, k => k.KPIName ?? "N/A");

            var builder = NewContextHeader(scope, selectedPeriod);
            builder.AppendLine($"Pham vi phan tich: {targetEmployeeIds.Count} nhan vien, {checkInRows.Count} check-in.");

            var measuredDetails = detailRows
                .Where(detail => detail.ProgressPercentage.HasValue)
                .ToList();
            if (!measuredDetails.Any())
            {
                builder.AppendLine("- Chua co tien do do luong tu check-in da duyet de phan tich.");
                return new AuthorizedPerformanceContext(builder.ToString(), false);
            }

            var avgProgress = measuredDetails
                .Select(d => d.ProgressPercentage!.Value)
                .Average();
            builder.AppendLine($"Tien do trung binh: {Math.Round(avgProgress, 1)}%.");

            builder.AppendLine("Tong hop theo nhan vien:");
            var byEmployee = checkInRows
                .GroupBy(c => c.EmployeeId)
                .Select(g =>
                {
                    var ids = g.Select(c => c.Id).ToHashSet();
                    var details = measuredDetails
                        .Where(d => d.CheckInId.HasValue && ids.Contains(d.CheckInId.Value))
                        .ToList();
                    var progress = details
                        .Select(d => d.ProgressPercentage!.Value)
                        .DefaultIfEmpty(0)
                        .Average();
                    return new
                    {
                        EmployeeId = g.Key,
                        Count = g.Count(),
                        HasMeasuredProgress = details.Count != 0,
                        AvgProgress = progress,
                        LastCheckIn = g.Max(c => c.CheckInDate)
                    };
                })
                .Where(item => item.HasMeasuredProgress)
                .OrderByDescending(x => x.AvgProgress)
                .ThenBy(x => x.EmployeeId)
                .Take(12)
                .ToList();

            foreach (var item in byEmployee)
            {
                var employeeReference = item.EmployeeId.HasValue ? $"employee #{item.EmployeeId.Value}" : "employee unknown";
                builder.AppendLine($"- {employeeReference}: {item.Count} check-in, progress TB {Math.Round(item.AvgProgress, 1)}%, check-in gan nhat {item.LastCheckIn:dd/MM/yyyy}.");
            }

            builder.AppendLine("Check-in gan day:");
            foreach (var checkIn in checkInRows.Take(10))
            {
                var detail = detailRows.FirstOrDefault(d => d.CheckInId == checkIn.Id);
                var kpiName = checkIn.KPIId.HasValue && kpiNames.ContainsKey(checkIn.KPIId.Value) ? kpiNames[checkIn.KPIId.Value] : "N/A";
                builder.AppendLine($"- {checkIn.CheckInDate:dd/MM/yyyy}: employee #{checkIn.EmployeeId}, KPI {kpiName}, progress {FormatDecimal(detail?.ProgressPercentage)}%.");
            }

            return new AuthorizedPerformanceContext(builder.ToString(), true);
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

            var result = await _context.EvaluationResults
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == evaluationResultId);
            if (result == null)
            {
                return new AIReviewContext { IsAllowed = false };
            }

            if (!scope.CanSeeAll && !scope.IsHR && result.EmployeeId.HasValue && !scope.EmployeeIds.Contains(result.EmployeeId.Value))
            {
                return new AIReviewContext { IsAllowed = false };
            }

            var period = result.PeriodId.HasValue
                ? await _context.EvaluationPeriods.AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == result.PeriodId.Value)
                : null;
            var rank = result.RankId.HasValue
                ? await _context.GradingRanks.AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == result.RankId.Value)
                : null;
            var perfContext = await BuildPerformanceContextAsync(user, new AnalyzePerformanceRequest { EmployeeId = result.EmployeeId, PeriodId = result.PeriodId });

            var builder = NewContextHeader(scope, period);
            builder.AppendLine($"Ket qua danh gia: employee #{result.EmployeeId}; ky {period?.PeriodName}; tong diem {FormatDecimal(result.TotalScore)}; rank {rank?.RankCode ?? "N/A"}; phan loai {result.Classification ?? "N/A"}; trang thai {result.SubmissionStatus ?? "Draft"}.");
            builder.AppendLine($"Nhan xet hien tai: {result.ReviewComment ?? "Chua co"}.");
            builder.AppendLine("Du lieu hieu suat thuc te:");
            builder.AppendLine(perfContext);

            return new AIReviewContext { IsAllowed = true, ContextText = builder.ToString() };
        }
    }
}
