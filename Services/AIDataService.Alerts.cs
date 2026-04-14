using System.Security.Claims;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public partial class AIDataService
    {
        public async Task<IReadOnlyList<AIRiskCandidate>> GetRiskCandidatesAsync(ClaimsPrincipal user, int? periodId)
        {
            var scope = await BuildScopeAsync(user);
            var period = await GetSelectedPeriodAsync(periodId);
            var scopedKpiIds = await GetScopedKpiIdsAsync(scope);
            var candidates = new List<AIRiskCandidate>();

            var kpis = await _context.KPIs
                .Where(k => k.IsActive == true && scopedKpiIds.Contains(k.Id))
                .Where(k => period == null || k.PeriodId == period.Id)
                .OrderByDescending(k => k.CreatedAt)
                .Take(80)
                .ToListAsync();

            var kpiIds = kpis.Select(k => k.Id).ToList();
            var detailMap = await _context.KPIDetails
                .Where(d => d.KPIId.HasValue && kpiIds.Contains(d.KPIId.Value))
                .ToDictionaryAsync(d => d.KPIId!.Value);

            var checkIns = ScopeCheckIns(_context.KPICheckIns.AsQueryable(), scope);
            checkIns = ApplyPeriodToCheckIns(checkIns, period);
            var checkInRows = await checkIns.Where(c => c.KPIId.HasValue && kpiIds.Contains(c.KPIId.Value)).ToListAsync();
            var checkInIds = checkInRows.Select(c => c.Id).ToList();
            var detailRows = await _context.CheckInDetails
                .Where(d => d.CheckInId.HasValue && checkInIds.Contains(d.CheckInId.Value))
                .ToListAsync();

            var expectedProgress = GetExpectedProgress(period);
            var finalQuarter = IsFinalQuarter(period);
            var today = DateTime.Today;

            foreach (var kpi in kpis)
            {
                var rows = checkInRows.Where(c => c.KPIId == kpi.Id).ToList();
                detailMap.TryGetValue(kpi.Id, out var kpiDetail);

                if (!rows.Any())
                {
                    candidates.Add(new AIRiskCandidate
                    {
                        SourceType = "KPI",
                        SourceRefId = kpi.Id,
                        PeriodId = period?.Id ?? kpi.PeriodId,
                        Severity = "medium",
                        Title = "KPI chua co check-in",
                        Content = $"KPI '{kpi.KPIName}' chua co check-in trong ky hien tai.",
                        Evidence = $"Target {FormatDecimal(kpiDetail?.TargetValue)} {kpiDetail?.MeasurementUnit}; expected progress {Math.Round(expectedProgress, 1)}%."
                    });
                    continue;
                }

                var rowIds = rows.Select(r => r.Id).ToHashSet();
                var details = detailRows.Where(d => d.CheckInId.HasValue && rowIds.Contains(d.CheckInId.Value)).ToList();
                var avgProgress = details
                    .Where(d => d.ProgressPercentage.HasValue)
                    .Select(d => d.ProgressPercentage!.Value)
                    .DefaultIfEmpty(0)
                    .Average();
                var lastCheckIn = rows.Max(r => r.CheckInDate) ?? today;

                if ((decimal)avgProgress + 15 < expectedProgress)
                {
                    candidates.Add(new AIRiskCandidate
                    {
                        SourceType = "KPI",
                        SourceRefId = kpi.Id,
                        PeriodId = period?.Id ?? kpi.PeriodId,
                        Severity = "high",
                        Title = "KPI cham hon tien do ky",
                        Content = $"KPI '{kpi.KPIName}' dang dat {Math.Round(avgProgress, 1)}%, thap hon tien do ky du kien {Math.Round(expectedProgress, 1)}%.",
                        Evidence = $"Last check-in {lastCheckIn:dd/MM/yyyy}; target {FormatDecimal(kpiDetail?.TargetValue)} {kpiDetail?.MeasurementUnit}."
                    });
                }

                if (finalQuarter && avgProgress < 70)
                {
                    candidates.Add(new AIRiskCandidate
                    {
                        SourceType = "KPI",
                        SourceRefId = kpi.Id,
                        PeriodId = period?.Id ?? kpi.PeriodId,
                        Severity = "high",
                        Title = "KPI rui ro cuoi ky",
                        Content = $"KPI '{kpi.KPIName}' duoi 70% khi ky danh gia da vao 25% thoi gian cuoi.",
                        Evidence = $"Progress TB {Math.Round(avgProgress, 1)}%; last check-in {lastCheckIn:dd/MM/yyyy}."
                    });
                }

                if ((today - lastCheckIn.Date).TotalDays > 7)
                {
                    candidates.Add(new AIRiskCandidate
                    {
                        SourceType = "KPI",
                        SourceRefId = kpi.Id,
                        PeriodId = period?.Id ?? kpi.PeriodId,
                        Severity = "medium",
                        Title = "KPI qua 7 ngay chua check-in",
                        Content = $"KPI '{kpi.KPIName}' chua co check-in moi tu {lastCheckIn:dd/MM/yyyy}.",
                        Evidence = $"So ngay chua cap nhat: {(today - lastCheckIn.Date).Days}."
                    });
                }
            }

            var scopedOkrIds = await GetScopedOkrIdsAsync(scope);
            var keyResults = await _context.OKRKeyResults
                .Where(kr => kr.OKRId.HasValue && scopedOkrIds.Contains(kr.OKRId.Value))
                .ToListAsync();

            foreach (var kr in keyResults)
            {
                var progress = ProgressHelper.CalculateProgress(kr.CurrentValue ?? 0, kr.TargetValue ?? 0, kr.IsInverse);
                if (progress < 50)
                {
                    candidates.Add(new AIRiskCandidate
                    {
                        SourceType = "OKRKeyResult",
                        SourceRefId = kr.Id,
                        PeriodId = period?.Id,
                        Severity = progress < 30 ? "high" : "medium",
                        Title = "Key Result duoi nguong 50%",
                        Content = $"Key Result '{kr.KeyResultName}' moi dat {Math.Round(progress, 1)}%.",
                        Evidence = $"Current {FormatDecimal(kr.CurrentValue)} / target {FormatDecimal(kr.TargetValue)} {kr.Unit}."
                    });
                }
            }

            return candidates
                .GroupBy(c => new { c.SourceType, c.SourceRefId, c.Title })
                .Select(g => g.First())
                .OrderBy(c => c.Severity == "high" ? 0 : c.Severity == "medium" ? 1 : 2)
                .ThenBy(c => c.Title)
                .Take(12)
                .ToList();
        }

        public async Task<IReadOnlyList<SmartAlertDto>> GetVisibleSmartAlertsAsync(ClaimsPrincipal user)
        {
            var employee = await GetCurrentEmployeeAsync(user);
            if (employee == null)
            {
                return Array.Empty<SmartAlertDto>();
            }

            var now = DateTime.Now;
            return await _context.SystemAlerts
                .Where(a => a.ReceiverId == employee.Id &&
                            a.AlertType == "AI Insight" &&
                            (a.ExpiresAt == null || a.ExpiresAt > now))
                .OrderByDescending(a => a.CreateDate)
                .Take(10)
                .Select(a => new SmartAlertDto
                {
                    Id = a.Id,
                    Severity = a.Severity,
                    Title = a.SourceType ?? a.AlertType,
                    Content = a.Content,
                    SourceType = a.SourceType,
                    SourceRefId = a.SourceRefId,
                    PeriodId = a.PeriodId,
                    CreatedAt = a.CreateDate
                })
                .ToListAsync();
        }
    }
}
