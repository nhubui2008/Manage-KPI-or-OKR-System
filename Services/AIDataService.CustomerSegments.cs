using System.Security.Claims;
using System.Text;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public partial class AIDataService
    {
        private static readonly string[] CommercialKeywords =
        {
            "doanh thu", "revenue", "sales", "ban hang", "bán hàng", "khach", "khách",
            "customer", "client", "hop dong", "hợp đồng", "don hang", "đơn hàng",
            "loi nhuan", "lợi nhuận", "thi truong", "thị trường", "khu vuc", "khu vực",
            "region", "san pham", "sản phẩm", "lead", "pipeline", "crm"
        };

        public async Task<string> BuildCustomerSegmentContextAsync(ClaimsPrincipal user, SuggestCustomerSegmentsRequest request)
        {
            var scope = await BuildScopeAsync(user);

            var employeeId = request.EmployeeId;
            if (!employeeId.HasValue && scope.IsEmployeeLike && scope.CurrentEmployeeId.HasValue)
            {
                employeeId = scope.CurrentEmployeeId.Value;
            }

            EnsureEmployeeAccess(scope, employeeId);
            EnsureDepartmentAccess(scope, request.DepartmentId);

            var selectedPeriod = await GetSelectedPeriodAsync(request.PeriodId);
            var previousPeriod = await GetPreviousPeriodAsync(selectedPeriod);
            var targetEmployeeIds = await ResolveTargetEmployeeIdsAsync(scope, employeeId, request.DepartmentId);

            var builder = NewContextHeader(scope, selectedPeriod);
            builder.AppendLine("MUC TIEU AI: goi y tep khach hang va danh gia tiem nang khach hang co the giup nhan vien/phong ban dat KPI doanh thu.");
            builder.AppendLine("Luu y: he thong hien CHUA co bang CRM/Customer rieng. Neu thieu du lieu ve ten khach hang, khu vuc, san pham/nganh hang, vong doi khach hang thi phai noi ro trong DataGaps va chi suy luan tu KPI/OKR/check-in/bao cao tai chinh noi bo.");

            await AppendEmployeeCommercialProfileAsync(builder, scope, targetEmployeeIds, employeeId, request.DepartmentId);
            await AppendPeriodPerformanceAsync(builder, targetEmployeeIds, selectedPeriod, "KY DANG XEM");
            if (previousPeriod != null)
            {
                await AppendPeriodPerformanceAsync(builder, targetEmployeeIds, previousPeriod, "KY TRUOC DE SO SANH");
            }
            else
            {
                builder.AppendLine("KY TRUOC DE SO SANH: chua tim thay ky danh gia truoc do.");
            }

            await AppendCommercialKpisAsync(builder, targetEmployeeIds, selectedPeriod, previousPeriod);
            await AppendCommercialOkrsAsync(builder, targetEmployeeIds, request.DepartmentId, selectedPeriod, previousPeriod);
            await AppendFinancialTargetsAsync(builder, selectedPeriod, previousPeriod);

            builder.AppendLine("DINH DANG CAN GOI Y:");
            builder.AppendLine("- Moi tep khach hang can neu: ten tep, vi sao hop voi nhan vien, san pham/dich vu nen ban, khu vuc neu co, moi/cu/lau nam, diem tiem nang 0-100, can cu doanh thu, hanh dong tiep theo.");
            builder.AppendLine("- Uu tien dua tren ky truoc: KPI nao dat/chua dat, doanh thu/target nao con thieu, check-in nao co ghi chu lien quan khach hang.");
            builder.AppendLine("- Khong bia ten khach hang cu the neu context khong co ten; co the goi y cap 'tep/phan khuc' thay vi danh sach cong ty.");

            return builder.ToString();
        }

        private async Task AppendEmployeeCommercialProfileAsync(
            StringBuilder builder,
            AIDataScope scope,
            List<int> targetEmployeeIds,
            int? requestedEmployeeId,
            int? requestedDepartmentId)
        {
            builder.AppendLine("HO SO NHAN VIEN / PHAM VI BAN HANG:");

            if (!targetEmployeeIds.Any())
            {
                builder.AppendLine("- Khong co nhan vien trong pham vi du lieu duoc phep.");
                return;
            }

            var employees = await _context.Employees
                .Where(e => targetEmployeeIds.Contains(e.Id))
                .OrderBy(e => e.FullName)
                .Take(20)
                .ToListAsync();

            var employeeIds = employees.Select(e => e.Id).ToList();
            var assignments = await _context.EmployeeAssignments
                .Where(a => a.EmployeeId.HasValue && employeeIds.Contains(a.EmployeeId.Value) && a.IsActive == true)
                .OrderByDescending(a => a.EffectiveDate)
                .ToListAsync();

            var departmentIds = assignments.Where(a => a.DepartmentId.HasValue).Select(a => a.DepartmentId!.Value).Distinct().ToList();
            var positionIds = assignments.Where(a => a.PositionId.HasValue).Select(a => a.PositionId!.Value).Distinct().ToList();
            var departments = departmentIds.Any()
                ? await _context.Departments.Where(d => departmentIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.DepartmentName ?? "N/A")
                : new Dictionary<int, string>();
            var positions = positionIds.Any()
                ? await _context.Positions.Where(p => positionIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.PositionName ?? "N/A")
                : new Dictionary<int, string>();

            foreach (var employee in employees)
            {
                var latestAssignment = assignments.FirstOrDefault(a => a.EmployeeId == employee.Id);
                var departmentName = latestAssignment?.DepartmentId != null && departments.TryGetValue(latestAssignment.DepartmentId.Value, out var dept)
                    ? dept
                    : "N/A";
                var positionName = latestAssignment?.PositionId != null && positions.TryGetValue(latestAssignment.PositionId.Value, out var pos)
                    ? pos
                    : "N/A";
                var scopeNote = employee.Id == scope.CurrentEmployeeId ? "tai khoan dang dang nhap" : "nhan vien trong pham vi";
                builder.AppendLine($"- #{employee.Id} {employee.FullName} ({employee.EmployeeCode}); {scopeNote}; phong ban {departmentName}; chuc vu {positionName}; email {employee.Email ?? "N/A"}.");
            }

            if (requestedDepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(requestedDepartmentId.Value);
                builder.AppendLine($"Phong ban duoc yeu cau: #{department?.Id} {department?.DepartmentName ?? "N/A"}.");
            }

            if (!requestedEmployeeId.HasValue && !requestedDepartmentId.HasValue)
            {
                builder.AppendLine("Nguoi dung khong chon rieng nhan vien/phong ban, AI can goi y theo pham vi dang nhin thay cua tai khoan hien tai.");
            }
        }

        private async Task AppendPeriodPerformanceAsync(StringBuilder builder, List<int> employeeIds, EvaluationPeriod? period, string label)
        {
            builder.AppendLine(label + ":");
            if (period == null)
            {
                builder.AppendLine("- Khong xac dinh ky danh gia.");
                return;
            }

            var results = await _context.EvaluationResults
                .Where(r => r.EmployeeId.HasValue && employeeIds.Contains(r.EmployeeId.Value) && r.PeriodId == period.Id)
                .ToListAsync();
            var employeeNames = await _context.Employees
                .Where(e => employeeIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.FullName ?? $"Nhan vien #{e.Id}");

            builder.AppendLine($"- Ky: #{period.Id} {period.PeriodName}; {period.StartDate:dd/MM/yyyy}-{period.EndDate:dd/MM/yyyy}.");
            if (!results.Any())
            {
                builder.AppendLine("- Chua co EvaluationResult trong ky nay.");
            }

            foreach (var result in results.OrderByDescending(r => r.TotalScore).Take(10))
            {
                var name = result.EmployeeId.HasValue && employeeNames.TryGetValue(result.EmployeeId.Value, out var employeeName)
                    ? employeeName
                    : "N/A";
                builder.AppendLine($"- {name}: tong diem {FormatDecimal(result.TotalScore)}, phan loai {result.Classification ?? "N/A"}, nhan xet {result.ReviewComment ?? "N/A"}.");
            }
        }

        private async Task AppendCommercialKpisAsync(
            StringBuilder builder,
            List<int> employeeIds,
            EvaluationPeriod? selectedPeriod,
            EvaluationPeriod? previousPeriod)
        {
            builder.AppendLine("KPI/BAO CAO DOANH THU - KHACH HANG:");

            var periodIds = new[] { selectedPeriod?.Id, previousPeriod?.Id }
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var directKpiIds = employeeIds.Any()
                ? await _context.KPI_Employee_Assignments
                    .Where(a => employeeIds.Contains(a.EmployeeId) && (a.Status == null || a.Status == "Active"))
                    .Select(a => a.KPIId)
                    .ToListAsync()
                : new List<int>();

            var employeeDepartmentIds = employeeIds.Any()
                ? await _context.EmployeeAssignments
                    .Where(a => a.EmployeeId.HasValue &&
                                employeeIds.Contains(a.EmployeeId.Value) &&
                                a.DepartmentId.HasValue &&
                                a.IsActive == true)
                    .Select(a => a.DepartmentId!.Value)
                    .Distinct()
                    .ToListAsync()
                : new List<int>();

            var departmentKpiIds = employeeDepartmentIds.Any()
                ? await _context.KPI_Department_Assignments
                    .Where(a => employeeDepartmentIds.Contains(a.DepartmentId))
                    .Select(a => a.KPIId)
                    .ToListAsync()
                : new List<int>();

            var kpiIds = directKpiIds.Concat(departmentKpiIds).Distinct().ToList();
            var kpis = await _context.KPIs
                .Where(k => k.IsActive == true && kpiIds.Contains(k.Id))
                .Where(k => !periodIds.Any() || (k.PeriodId.HasValue && periodIds.Contains(k.PeriodId.Value)))
                .OrderByDescending(k => k.PeriodId)
                .ThenBy(k => k.KPIName)
                .ToListAsync();

            var kpiDetails = kpis.Any()
                ? await _context.KPIDetails
                    .Where(d => d.KPIId.HasValue && kpis.Select(k => k.Id).Contains(d.KPIId.Value))
                    .ToDictionaryAsync(d => d.KPIId!.Value)
                : new Dictionary<int, KPIDetail>();

            var commercialKpis = kpis
                .Where(k => IsCommercialText(k.KPIName) ||
                            (kpiDetails.TryGetValue(k.Id, out var detail) &&
                             (IsCommercialText(detail.MeasurementUnit) || IsMoneyUnit(detail.MeasurementUnit))))
                .Take(20)
                .ToList();

            if (!commercialKpis.Any())
            {
                builder.AppendLine("- Khong tim thay KPI co dau hieu doanh thu/khach hang. AI can neu day la khoang trong du lieu.");
            }

            var commercialKpiIds = commercialKpis.Select(k => k.Id).ToList();
            var checkIns = commercialKpiIds.Any()
                ? await _context.KPICheckIns
                    .Where(c => c.EmployeeId.HasValue &&
                                employeeIds.Contains(c.EmployeeId.Value) &&
                                c.KPIId.HasValue &&
                                commercialKpiIds.Contains(c.KPIId.Value) &&
                                (c.ReviewStatus == "Approved" || c.ReviewStatus == null))
                    .OrderByDescending(c => c.CheckInDate)
                    .ToListAsync()
                : new List<KPICheckIn>();

            var checkInIds = checkIns.Select(c => c.Id).ToList();
            var checkInDetails = checkInIds.Any()
                ? await _context.CheckInDetails.Where(d => d.CheckInId.HasValue && checkInIds.Contains(d.CheckInId.Value)).ToListAsync()
                : new List<CheckInDetail>();

            foreach (var kpi in commercialKpis)
            {
                kpiDetails.TryGetValue(kpi.Id, out var detail);
                var latestCheckIn = checkIns.FirstOrDefault(c => c.KPIId == kpi.Id);
                var latestDetail = latestCheckIn == null ? null : checkInDetails.FirstOrDefault(d => d.CheckInId == latestCheckIn.Id);
                builder.AppendLine($"- KPI #{kpi.Id} ky #{kpi.PeriodId}: {kpi.KPIName}; target {FormatDecimal(detail?.TargetValue)} {detail?.MeasurementUnit}; latest achieved {FormatDecimal(latestDetail?.AchievedValue)}; progress {FormatDecimal(latestDetail?.ProgressPercentage)}%; note {latestDetail?.Note ?? "N/A"}.");
            }

            var comparisons = await _context.KPI_Result_Comparisons
                .Where(c => c.EmployeeId.HasValue &&
                            employeeIds.Contains(c.EmployeeId.Value) &&
                            (!periodIds.Any() || (c.PeriodId.HasValue && periodIds.Contains(c.PeriodId.Value))))
                .OrderByDescending(c => c.ProcessedDate)
                .Take(20)
                .ToListAsync();

            if (comparisons.Any())
            {
                var comparisonKpiIds = comparisons.Where(c => c.KPIId.HasValue).Select(c => c.KPIId!.Value).Distinct().ToList();
                var comparisonKpis = await _context.KPIs
                    .Where(k => comparisonKpiIds.Contains(k.Id))
                    .ToDictionaryAsync(k => k.Id, k => k.KPIName ?? $"KPI #{k.Id}");
                builder.AppendLine("KPI_Result_Comparison / du lieu chot doanh thu:");
                foreach (var item in comparisons)
                {
                    var kpiName = item.KPIId.HasValue && comparisonKpis.TryGetValue(item.KPIId.Value, out var name) ? name : "N/A";
                    builder.AppendLine($"- Employee #{item.EmployeeId}; KPI {kpiName}; period #{item.PeriodId}; target {FormatDecimal(item.SystemTargetValue)}; achieved {FormatDecimal(item.EmployeeAchievedValue)}; completion {FormatDecimal(item.CompletionPercent)}%; final {item.FinalResult ?? "N/A"}.");
                }
            }
        }

        private async Task AppendCommercialOkrsAsync(
            StringBuilder builder,
            List<int> employeeIds,
            int? departmentId,
            EvaluationPeriod? selectedPeriod,
            EvaluationPeriod? previousPeriod)
        {
            builder.AppendLine("OKR/KR LIEN QUAN KHACH HANG - DOANH THU:");

            var okrIds = new List<int>();
            if (employeeIds.Any())
            {
                okrIds.AddRange(await _context.OKR_Employee_Allocations
                    .Where(a => employeeIds.Contains(a.EmployeeId))
                    .Select(a => a.OKRId)
                    .ToListAsync());
            }

            if (departmentId.HasValue)
            {
                okrIds.AddRange(await _context.OKR_Department_Allocations
                    .Where(a => a.DepartmentId == departmentId.Value)
                    .Select(a => a.OKRId)
                    .ToListAsync());
            }

            okrIds = okrIds.Distinct().ToList();
            var okrs = await _context.OKRs
                .Where(o => o.IsActive == true && okrIds.Contains(o.Id))
                .OrderByDescending(o => o.CreatedAt)
                .Take(30)
                .ToListAsync();

            var allowedCycles = new[] { selectedPeriod, previousPeriod }
                .Where(p => p != null)
                .SelectMany(p => BuildPeriodCycleAliases(p!))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (allowedCycles.Any())
            {
                okrs = okrs.Where(o => string.IsNullOrWhiteSpace(o.Cycle) || allowedCycles.Contains(o.Cycle, StringComparer.OrdinalIgnoreCase)).ToList();
            }

            var okrIdsForKr = okrs.Select(o => o.Id).ToList();
            var keyResults = okrIdsForKr.Any()
                ? await _context.OKRKeyResults.Where(kr => kr.OKRId.HasValue && okrIdsForKr.Contains(kr.OKRId.Value)).ToListAsync()
                : new List<OKRKeyResult>();

            var rows = keyResults
                .Where(kr => IsCommercialText(kr.KeyResultName) || IsCommercialText(kr.Unit) || IsMoneyUnit(kr.Unit))
                .Take(20)
                .ToList();

            if (!rows.Any())
            {
                builder.AppendLine("- Khong tim thay KR co dau hieu doanh thu/khach hang.");
            }

            foreach (var kr in rows)
            {
                var okrName = okrs.FirstOrDefault(o => o.Id == kr.OKRId)?.ObjectiveName ?? "N/A";
                var progress = ProgressHelper.CalculateProgress(kr.CurrentValue ?? 0, kr.TargetValue ?? 0, kr.IsInverse);
                builder.AppendLine($"- OKR {okrName}; KR #{kr.Id}: {kr.KeyResultName}; target {FormatDecimal(kr.TargetValue)} {kr.Unit}; current {FormatDecimal(kr.CurrentValue)}; progress {FormatDecimal(progress)}%; status {kr.ResultStatus ?? "N/A"}.");
            }
        }

        private async Task AppendFinancialTargetsAsync(StringBuilder builder, EvaluationPeriod? selectedPeriod, EvaluationPeriod? previousPeriod)
        {
            builder.AppendLine("MUC TIEU TAI CHINH / DOANH THU CAP CHIEN LUOC:");
            var years = new[] { selectedPeriod?.StartDate?.Year, previousPeriod?.StartDate?.Year, DateTime.Now.Year }
                .Where(y => y.HasValue)
                .Select(y => y!.Value)
                .Distinct()
                .ToList();

            var statements = await _context.MissionVisions
                .Where(m => m.IsActive == true &&
                            m.FinancialTarget.HasValue &&
                            (!years.Any() || !m.TargetYear.HasValue || years.Contains(m.TargetYear.Value)))
                .OrderByDescending(m => m.TargetYear)
                .ThenBy(m => m.MissionVisionType)
                .Take(10)
                .ToListAsync();

            if (!statements.Any())
            {
                builder.AppendLine("- Chua co Mission/Vision/YearlyGoal co FinancialTarget.");
            }

            foreach (var item in statements)
            {
                builder.AppendLine($"- {item.TypeDisplayName} nam {item.TargetYear?.ToString() ?? "N/A"}: target tai chinh {FormatDecimal(item.FinancialTarget)}; noi dung {item.Content ?? "N/A"}.");
            }
        }

        private async Task<EvaluationPeriod?> GetPreviousPeriodAsync(EvaluationPeriod? selectedPeriod)
        {
            if (selectedPeriod == null)
            {
                return await _context.EvaluationPeriods
                    .Where(p => p.IsActive == true)
                    .OrderByDescending(p => p.StartDate)
                    .Skip(1)
                    .FirstOrDefaultAsync();
            }

            var query = _context.EvaluationPeriods.Where(p => p.IsActive == true && p.Id != selectedPeriod.Id);
            if (selectedPeriod.StartDate.HasValue)
            {
                query = query.Where(p => p.EndDate.HasValue && p.EndDate.Value < selectedPeriod.StartDate.Value);
            }

            return await query
                .OrderByDescending(p => p.StartDate)
                .ThenByDescending(p => p.Id)
                .FirstOrDefaultAsync();
        }

        private static bool IsCommercialText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.ToLowerInvariant();
            return CommercialKeywords.Any(keyword => normalized.Contains(keyword));
        }

        private static bool IsMoneyUnit(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.ToLowerInvariant();
            return normalized.Contains("vnd") ||
                   normalized.Contains("vnđ") ||
                   normalized.Contains("đ") ||
                   normalized.Contains("dong") ||
                   normalized.Contains("đồng") ||
                   normalized.Contains("usd") ||
                   normalized.Contains("$");
        }

        private static IEnumerable<string> BuildPeriodCycleAliases(EvaluationPeriod period)
        {
            if (!string.IsNullOrWhiteSpace(period.PeriodName))
            {
                yield return period.PeriodName;
            }

            if (period.StartDate.HasValue)
            {
                var quarter = ((period.StartDate.Value.Month - 1) / 3) + 1;
                yield return $"Q{quarter}-{period.StartDate.Value.Year}";
            }
        }
    }
}
