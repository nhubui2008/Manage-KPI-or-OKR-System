using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class EvaluationPeriodsController : Controller
    {
        private readonly MiniERPDbContext _context;
        public EvaluationPeriodsController(MiniERPDbContext context) { _context = context; }

        [HasPermission("EVALPERIODS_VIEW")]
        public async Task<IActionResult> Index(
            string? searchString,
            int? pageNumber,
            int? year = null,
            string? periodType = null,
            int? statusId = null,
            string? quickFilter = null,
            string? sortBy = null)
        {
            searchString = string.IsNullOrWhiteSpace(searchString) ? null : searchString.Trim();
            periodType = NormalizePeriodType(periodType);
            quickFilter = NormalizeQuickFilter(quickFilter);
            sortBy = NormalizeSort(sortBy);

            var today = DateTime.Today;
            var endingSoonDate = today.AddDays(7);
            IQueryable<EvaluationPeriod> scopedQuery = _context.EvaluationPeriods
                .AsNoTracking()
                .Where(p => p.IsActive == true);

            var closedStatusNames = new[] { "Đóng", "Closed", "Completed" };
            var closedStatusIdsQuery = _context.Statuses
                .AsNoTracking()
                .Where(s => s.StatusType == WorkflowStatusHelper.StatusTypeEvaluationPeriod &&
                            s.StatusName != null &&
                            closedStatusNames.Contains(s.StatusName))
                .Select(s => s.Id);
            var query = ApplySearchAndStructuredFilters(
                scopedQuery,
                searchString,
                year,
                periodType,
                statusId);
            query = ApplyOperationalFilter(query, quickFilter, today, endingSoonDate, closedStatusIdsQuery);
            query = sortBy switch
            {
                "start" => query.OrderBy(p => p.StartDate ?? DateTime.MaxValue).ThenBy(p => p.Id),
                "ending" => query.OrderBy(p => p.EndDate ?? DateTime.MaxValue).ThenBy(p => p.Id),
                "name" => query.OrderBy(p => p.PeriodName).ThenBy(p => p.Id),
                _ => query.OrderByDescending(p => p.StartDate ?? DateTime.MinValue).ThenByDescending(p => p.Id)
            };

            const int pageSize = 10;
            var requestedPageIndex = pageNumber is > 0 ? pageNumber.Value : 1;

            // Status options, filter facets and filtered summary share one SQL round-trip.
            var aggregateRows = await _context.Statuses
                .AsNoTracking()
                .Where(s => s.StatusType == WorkflowStatusHelper.StatusTypeEvaluationPeriod)
                .Select(s => new EvaluationPeriodIndexAggregateRow
                {
                    Kind = 0,
                    Year = (int?)null,
                    PeriodType = (string?)null,
                    StatusId = (int?)s.Id,
                    StatusName = s.StatusName,
                    TotalCount = 0,
                    InProgressCount = 0,
                    UpcomingCount = 0,
                    EndingSoonCount = 0,
                    CompletedCount = 0,
                    ItemId = 0,
                    PeriodName = (string?)null,
                    StartDate = (DateTime?)null,
                    EndDate = (DateTime?)null,
                    IsSystemProcessed = false,
                    KpiCount = 0,
                    EvaluationResultCount = 0
                })
                .Concat(scopedQuery.Select(p => new EvaluationPeriodIndexAggregateRow
                {
                    Kind = 1,
                    Year = p.StartDate.HasValue ? (int?)p.StartDate.Value.Year : null,
                    PeriodType = p.PeriodType,
                    StatusId = (int?)null,
                    StatusName = (string?)null,
                    TotalCount = 0,
                    InProgressCount = 0,
                    UpcomingCount = 0,
                    EndingSoonCount = 0,
                    CompletedCount = 0,
                    ItemId = 0,
                    PeriodName = (string?)null,
                    StartDate = (DateTime?)null,
                    EndDate = (DateTime?)null,
                    IsSystemProcessed = false,
                    KpiCount = 0,
                    EvaluationResultCount = 0
                }).Distinct())
                .Concat(query
                    .GroupBy(_ => 1)
                    .Select(group => new EvaluationPeriodIndexAggregateRow
                    {
                        Kind = 2,
                        Year = (int?)null,
                        PeriodType = (string?)null,
                        StatusId = (int?)null,
                        StatusName = (string?)null,
                        TotalCount = group.Count(),
                        InProgressCount = group.Sum(p =>
                            (!p.StatusId.HasValue || !closedStatusIdsQuery.Contains(p.StatusId.Value)) &&
                            p.StartDate.HasValue && p.StartDate.Value.Date <= today &&
                            p.EndDate.HasValue && p.EndDate.Value.Date >= today ? 1 : 0),
                        UpcomingCount = group.Sum(p =>
                            (!p.StatusId.HasValue || !closedStatusIdsQuery.Contains(p.StatusId.Value)) &&
                            p.StartDate.HasValue && p.StartDate.Value.Date > today ? 1 : 0),
                        EndingSoonCount = group.Sum(p =>
                            (!p.StatusId.HasValue || !closedStatusIdsQuery.Contains(p.StatusId.Value)) &&
                            p.StartDate.HasValue && p.StartDate.Value.Date <= today &&
                            p.EndDate.HasValue && p.EndDate.Value.Date >= today &&
                            p.EndDate.Value.Date <= endingSoonDate ? 1 : 0),
                        CompletedCount = group.Sum(p =>
                            (p.StatusId.HasValue && closedStatusIdsQuery.Contains(p.StatusId.Value)) ||
                            (p.EndDate.HasValue && p.EndDate.Value.Date < today) ? 1 : 0),
                        ItemId = 0,
                        PeriodName = (string?)null,
                        StartDate = (DateTime?)null,
                        EndDate = (DateTime?)null,
                        IsSystemProcessed = false,
                        KpiCount = 0,
                        EvaluationResultCount = 0
                    }))
                .Concat(query
                    .Skip((requestedPageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new EvaluationPeriodIndexAggregateRow
                    {
                        Kind = 3,
                        Year = (int?)null,
                        PeriodType = p.PeriodType,
                        StatusId = p.StatusId,
                        StatusName = _context.Statuses
                            .Where(s => p.StatusId.HasValue && s.Id == p.StatusId.Value)
                            .Select(s => s.StatusName)
                            .FirstOrDefault(),
                        TotalCount = 0,
                        InProgressCount = 0,
                        UpcomingCount = 0,
                        EndingSoonCount = 0,
                        CompletedCount = 0,
                        ItemId = p.Id,
                        PeriodName = p.PeriodName,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        IsSystemProcessed = p.IsSystemProcessed == true,
                        KpiCount = _context.KPIs.Count(k => k.IsActive == true && k.PeriodId == p.Id),
                        EvaluationResultCount = _context.EvaluationResults.Count(r => r.PeriodId == p.Id)
                    }))
                .ToListAsync();
            var statuses = aggregateRows
                .Where(row => row.Kind == 0 && row.StatusId.HasValue)
                .GroupBy(row => row.StatusId!.Value)
                .Select(group => new EvaluationPeriodStatusOptionViewModel
                {
                    Id = group.Key,
                    Name = group.Select(row => row.StatusName).FirstOrDefault() ?? $"Trạng thái #{group.Key}"
                })
                .OrderBy(item => item.Name)
                .ToList();
            var availableYears = aggregateRows
                .Where(row => row.Kind == 1 && row.Year.HasValue)
                .Select(row => row.Year!.Value)
                .Distinct()
                .OrderByDescending(value => value)
                .ToList();
            var availablePeriodTypes = aggregateRows
                .Where(row => row.Kind == 1 && row.PeriodType != null)
                .Select(row => NormalizePeriodType(row.PeriodType))
                .Where(value => value is "MONTH" or "QUARTER" or "YEAR")
                .Select(value => value!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetPeriodTypeSortOrder)
                .ToList();
            var summaryRow = aggregateRows.FirstOrDefault(row => row.Kind == 2);
            var summary = summaryRow == null
                ? new EvaluationPeriodIndexSummaryViewModel()
                : new EvaluationPeriodIndexSummaryViewModel
                {
                    TotalCount = summaryRow.TotalCount,
                    InProgressCount = summaryRow.InProgressCount,
                    UpcomingCount = summaryRow.UpcomingCount,
                    EndingSoonCount = summaryRow.EndingSoonCount,
                    CompletedCount = summaryRow.CompletedCount
                };

            var totalCount = summary.TotalCount;
            var pageIndex = requestedPageIndex;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (pageIndex > totalPages)
            {
                pageIndex = totalPages;
            }

            var pageRows = aggregateRows
                .Where(row => row.Kind == 3)
                .Select(MapAggregatePageRow)
                .ToList();
            if (pageIndex != requestedPageIndex)
            {
                pageRows = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new EvaluationPeriodIndexQueryRow
                    {
                        Id = p.Id,
                        PeriodName = p.PeriodName,
                        PeriodType = p.PeriodType,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        StatusId = p.StatusId,
                        StatusName = _context.Statuses
                            .Where(s => p.StatusId.HasValue && s.Id == p.StatusId.Value)
                            .Select(s => s.StatusName)
                            .FirstOrDefault(),
                        IsSystemProcessed = p.IsSystemProcessed == true,
                        KpiCount = _context.KPIs.Count(k => k.IsActive == true && k.PeriodId == p.Id),
                        EvaluationResultCount = _context.EvaluationResults.Count(r => r.PeriodId == p.Id)
                    })
                    .ToListAsync();
            }

            var items = pageRows.Select(row => new EvaluationPeriodIndexItemViewModel
            {
                Id = row.Id,
                PeriodName = row.PeriodName ?? $"Kỳ đánh giá #{row.Id}",
                PeriodType = NormalizePeriodType(row.PeriodType) ?? row.PeriodType ?? string.Empty,
                StartDate = row.StartDate,
                EndDate = row.EndDate,
                StatusId = row.StatusId,
                StatusName = row.StatusName ?? "Không xác định",
                IsSystemProcessed = row.IsSystemProcessed,
                KpiCount = row.KpiCount,
                EvaluationResultCount = row.EvaluationResultCount,
                OperationalStatus = ResolveOperationalStatus(
                    row.StartDate,
                    row.EndDate,
                    row.StatusName,
                    today,
                    endingSoonDate)
            }).ToList();

            var permissions = await PermissionLookupHelper.HasPermissionsAsync(
                _context,
                User,
                new[] { "EVALPERIODS_CREATE", "EVALPERIODS_EDIT", "EVALPERIODS_DELETE" });
            var hasActiveFilters = searchString != null || year.HasValue || periodType != null ||
                                   statusId.HasValue || quickFilter != null;
            var model = new EvaluationPeriodIndexViewModel
            {
                Items = new PaginatedList<EvaluationPeriodIndexItemViewModel>(items, totalCount, pageIndex, pageSize),
                SearchString = searchString,
                Year = year,
                PeriodType = periodType,
                StatusId = statusId,
                QuickFilter = quickFilter,
                SortBy = sortBy,
                CanCreatePeriod = permissions.TryGetValue("EVALPERIODS_CREATE", out var canCreate) && canCreate,
                CanEditPeriod = permissions.TryGetValue("EVALPERIODS_EDIT", out var canEdit) && canEdit,
                CanDeletePeriod = permissions.TryGetValue("EVALPERIODS_DELETE", out var canDelete) && canDelete,
                HasActiveFilters = hasActiveFilters,
                IsFilteredEmpty = hasActiveFilters && totalCount == 0,
                Summary = summary,
                AvailableYears = availableYears,
                AvailablePeriodTypes = availablePeriodTypes,
                AvailableStatuses = statuses
            };

            return View(model);
        }

        private static IQueryable<EvaluationPeriod> ApplySearchAndStructuredFilters(
            IQueryable<EvaluationPeriod> query,
            string? searchString,
            int? year,
            string? periodType,
            int? statusId)
        {
            if (searchString != null)
            {
                query = query.Where(p => p.PeriodName != null && p.PeriodName.Contains(searchString));
            }

            if (year.HasValue)
            {
                query = query.Where(p =>
                    (p.StartDate.HasValue && p.StartDate.Value.Year == year.Value) ||
                    (p.EndDate.HasValue && p.EndDate.Value.Year == year.Value));
            }

            if (periodType != null)
            {
                var aliases = GetPeriodTypeAliases(periodType);
                query = query.Where(p => p.PeriodType != null && aliases.Contains(p.PeriodType));
            }

            if (statusId.HasValue)
            {
                query = query.Where(p => p.StatusId == statusId.Value);
            }

            return query;
        }

        private static IQueryable<EvaluationPeriod> ApplyOperationalFilter(
            IQueryable<EvaluationPeriod> query,
            string? quickFilter,
            DateTime today,
            DateTime endingSoonDate,
            IQueryable<int> closedStatusIds)
        {
            return quickFilter switch
            {
                "running" => query.Where(p =>
                    (!p.StatusId.HasValue || !closedStatusIds.Contains(p.StatusId.Value)) &&
                    p.StartDate.HasValue && p.StartDate.Value.Date <= today &&
                    p.EndDate.HasValue && p.EndDate.Value.Date >= today),
                "upcoming" => query.Where(p =>
                    (!p.StatusId.HasValue || !closedStatusIds.Contains(p.StatusId.Value)) &&
                    p.StartDate.HasValue && p.StartDate.Value.Date > today),
                "ending" => query.Where(p =>
                    (!p.StatusId.HasValue || !closedStatusIds.Contains(p.StatusId.Value)) &&
                    p.StartDate.HasValue && p.StartDate.Value.Date <= today &&
                    p.EndDate.HasValue && p.EndDate.Value.Date >= today &&
                    p.EndDate.Value.Date <= endingSoonDate),
                "overdue" => query.Where(p =>
                    (!p.StatusId.HasValue || !closedStatusIds.Contains(p.StatusId.Value)) &&
                    p.EndDate.HasValue && p.EndDate.Value.Date < today),
                "closed" => query.Where(p =>
                    p.StatusId.HasValue && closedStatusIds.Contains(p.StatusId.Value)),
                _ => query
            };
        }

        private static string ResolveOperationalStatus(
            DateTime? startDate,
            DateTime? endDate,
            string? statusName,
            DateTime today,
            DateTime endingSoonDate)
        {
            if (IsClosedStatusName(statusName)) return "closed";
            if (startDate?.Date > today) return "upcoming";
            if (endDate?.Date < today) return "overdue";
            if (startDate?.Date <= today && endDate?.Date <= endingSoonDate) return "ending";
            if (startDate?.Date <= today && endDate?.Date >= today) return "running";
            return "unknown";
        }

        private static bool IsClosedStatusName(string? statusName)
        {
            return string.Equals(statusName, "Đóng", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(statusName, "Closed", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(statusName, "Completed", StringComparison.OrdinalIgnoreCase);
        }

        private static string? NormalizeQuickFilter(string? quickFilter)
        {
            var value = quickFilter?.Trim().ToLowerInvariant();
            return value is "running" or "upcoming" or "ending" or "overdue" or "closed" ? value : null;
        }

        private static string NormalizeSort(string? sortBy)
        {
            var value = sortBy?.Trim().ToLowerInvariant();
            return value is "start" or "ending" or "name" ? value : "recent";
        }

        private static string[] GetPeriodTypeAliases(string periodType)
        {
            return periodType switch
            {
                "MONTH" => new[] { "MONTH", "THANG", "THÁNG", "HANG THANG", "HÀNG THÁNG", "Tháng" },
                "QUARTER" => new[] { "QUARTER", "QUY", "QUÝ", "HANG QUY", "HÀNG QUÝ", "Quý" },
                "YEAR" => new[] { "YEAR", "NAM", "NĂM", "HANG NAM", "HÀNG NĂM", "Năm" },
                _ => new[] { periodType }
            };
        }

        private static int GetPeriodTypeSortOrder(string periodType)
        {
            return periodType switch
            {
                "MONTH" => 1,
                "QUARTER" => 2,
                "YEAR" => 3,
                _ => 4
            };
        }

        private sealed class EvaluationPeriodIndexQueryRow
        {
            public int Id { get; init; }
            public string? PeriodName { get; init; }
            public string? PeriodType { get; init; }
            public DateTime? StartDate { get; init; }
            public DateTime? EndDate { get; init; }
            public int? StatusId { get; init; }
            public string? StatusName { get; init; }
            public bool IsSystemProcessed { get; init; }
            public int KpiCount { get; init; }
            public int EvaluationResultCount { get; init; }
        }

        private static EvaluationPeriodIndexQueryRow MapAggregatePageRow(
            EvaluationPeriodIndexAggregateRow row)
        {
            return new EvaluationPeriodIndexQueryRow
            {
                Id = row.ItemId,
                PeriodName = row.PeriodName,
                PeriodType = row.PeriodType,
                StartDate = row.StartDate,
                EndDate = row.EndDate,
                StatusId = row.StatusId,
                StatusName = row.StatusName,
                IsSystemProcessed = row.IsSystemProcessed,
                KpiCount = row.KpiCount,
                EvaluationResultCount = row.EvaluationResultCount
            };
        }

        private sealed class EvaluationPeriodIndexAggregateRow
        {
            public int Kind { get; init; }
            public int? Year { get; init; }
            public string? PeriodType { get; init; }
            public int? StatusId { get; init; }
            public string? StatusName { get; init; }
            public int TotalCount { get; init; }
            public int InProgressCount { get; init; }
            public int UpcomingCount { get; init; }
            public int EndingSoonCount { get; init; }
            public int CompletedCount { get; init; }
            public int ItemId { get; init; }
            public string? PeriodName { get; init; }
            public DateTime? StartDate { get; init; }
            public DateTime? EndDate { get; init; }
            public bool IsSystemProcessed { get; init; }
            public int KpiCount { get; init; }
            public int EvaluationResultCount { get; init; }
        }

        [HttpGet]
        [HasPermission("EVALPERIODS_CREATE")]
        public IActionResult Create()
        {
            return View(new EvaluationPeriodInputViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("EVALPERIODS_CREATE")]
        public async Task<IActionResult> Create(EvaluationPeriodInputViewModel model)
        {
            NormalizeInput(model);
            if (!await ValidateInputAsync(model))
            {
                return View(model);
            }

            var openStatusId = await _context.GetStatusIdAsync(
                WorkflowStatusHelper.StatusTypeEvaluationPeriod,
                EvaluationPeriodRules.StatusOpen);
            if (!openStatusId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Chưa cấu hình trạng thái Mở cho kỳ đánh giá.");
                return View(model);
            }

            var period = new EvaluationPeriod
            {
                PeriodName = model.PeriodName,
                PeriodType = model.PeriodType,
                StartDate = model.StartDate?.Date,
                EndDate = model.EndDate?.Date,
                StatusId = openStatusId,
                IsActive = true,
                IsSystemProcessed = false
            };
            _context.EvaluationPeriods.Add(period);
            AddAuditLog("CREATE", null, period);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã tạo kỳ đánh giá ở trạng thái Mở.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [HasPermission("EVALPERIODS_EDIT")]
        public async Task<IActionResult> Edit(int id)
        {
            var period = await _context.EvaluationPeriods
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);
            if (period == null) return NotFound();

            return View(new EvaluationPeriodInputViewModel
            {
                Id = period.Id,
                PeriodName = period.PeriodName,
                PeriodType = NormalizePeriodType(period.PeriodType),
                StartDate = period.StartDate,
                EndDate = period.EndDate
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("EVALPERIODS_EDIT")]
        public async Task<IActionResult> Edit(int id, EvaluationPeriodInputViewModel model)
        {
            if (id != model.Id) return NotFound();

            var existing = await _context.EvaluationPeriods
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);
            if (existing == null) return NotFound();

            NormalizeInput(model);
            if (!await ValidateInputAsync(model, id))
            {
                return View(model);
            }

            var changesProtectedFields =
                !string.Equals(NormalizePeriodType(existing.PeriodType), model.PeriodType, StringComparison.Ordinal) ||
                existing.StartDate?.Date != model.StartDate?.Date ||
                existing.EndDate?.Date != model.EndDate?.Date;
            var dependencies = await GetDependencySummaryAsync(id);
            if (changesProtectedFields && dependencies.HasAny)
            {
                ModelState.AddModelError(string.Empty,
                    $"Kỳ này đã có {dependencies.KpiCount} KPI, {dependencies.CheckInCount} check-in và " +
                    $"{dependencies.EvaluationResultCount} kết quả đánh giá. Chỉ được sửa tên kỳ để bảo toàn lịch sử.");
                return View(model);
            }

            var oldData = new
            {
                existing.PeriodName,
                existing.PeriodType,
                existing.StartDate,
                existing.EndDate
            };
            existing.PeriodName = model.PeriodName;
            existing.PeriodType = model.PeriodType;
            existing.StartDate = model.StartDate?.Date;
            existing.EndDate = model.EndDate?.Date;
            AddAuditLog("UPDATE", oldData, existing);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã cập nhật kỳ đánh giá thành công.";
            return RedirectToAction(nameof(Index));
        }

        private static string? NormalizePeriodType(string? periodType)
        {
            return EvaluationPeriodRules.NormalizePeriodType(periodType);
        }

        private static void NormalizeInput(EvaluationPeriodInputViewModel model)
        {
            model.PeriodName = model.PeriodName?.Trim();
            model.PeriodType = NormalizePeriodType(model.PeriodType);
        }

        private async Task<bool> ValidateInputAsync(
            EvaluationPeriodInputViewModel model,
            int? excludeId = null)
        {
            foreach (var error in EvaluationPeriodRules.ValidateInput(model))
            {
                ModelState.AddModelError(error.Key, error.Value);
            }

            if (!string.IsNullOrWhiteSpace(model.PeriodName) &&
                await _context.EvaluationPeriods.AnyAsync(p =>
                    p.PeriodName == model.PeriodName && p.IsActive == true && p.Id != excludeId))
            {
                ModelState.AddModelError(nameof(model.PeriodName),
                    "Tên kỳ đánh giá đã tồn tại. Vui lòng chọn tên khác.");
            }

            if (model.StartDate.HasValue && model.EndDate.HasValue &&
                model.PeriodType is EvaluationPeriodRules.TypeMonth or
                    EvaluationPeriodRules.TypeQuarter or EvaluationPeriodRules.TypeYear)
            {
                var aliases = GetPeriodTypeAliases(model.PeriodType);
                var isOverlapping = await _context.EvaluationPeriods.AnyAsync(p =>
                    p.IsActive == true &&
                    p.Id != excludeId &&
                    p.PeriodType != null && aliases.Contains(p.PeriodType) &&
                    p.StartDate.HasValue && p.EndDate.HasValue &&
                    model.StartDate.Value.Date <= p.EndDate.Value.Date &&
                    model.EndDate.Value.Date >= p.StartDate.Value.Date);
                if (isOverlapping)
                {
                    ModelState.AddModelError(nameof(model.StartDate),
                        "Khoảng thời gian trùng với một kỳ đánh giá đang hoạt động cùng loại.");
                }
            }

            return ModelState.IsValid;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("EVALPERIODS_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            var period = await _context.EvaluationPeriods
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);
            if (period == null) return NotFound();

            var dependencies = await GetDependencySummaryAsync(id);
            if (dependencies.HasAny)
            {
                TempData["ErrorMessage"] =
                    $"Không thể vô hiệu hóa kỳ này vì đang có {dependencies.KpiCount} KPI, " +
                    $"{dependencies.CheckInCount} check-in và {dependencies.EvaluationResultCount} kết quả đánh giá liên kết.";
                return RedirectToAction(nameof(Index));
            }

            period.IsActive = false;
            AddAuditLog("DELETE", period, new { period.Id, IsActive = false });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã vô hiệu hóa kỳ đánh giá.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("EVALPERIODS_EDIT")]
        public Task<IActionResult> StartProcessing(int id)
        {
            return ChangeLifecycleStatusAsync(
                id,
                EvaluationPeriodRules.StatusInProgress,
                "Đã chuyển kỳ đánh giá sang trạng thái Đang xử lý.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("EVALPERIODS_EDIT")]
        public async Task<IActionResult> Close(int id)
        {
            var period = await _context.EvaluationPeriods
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);
            if (period == null) return NotFound();

            var currentStatus = await GetStatusNameAsync(period.StatusId);
            if (!EvaluationPeriodRules.CanTransition(currentStatus, EvaluationPeriodRules.StatusClosed))
            {
                TempData["ErrorMessage"] = "Chỉ kỳ đang ở trạng thái Đang xử lý mới có thể đóng.";
                return RedirectToAction(nameof(Index));
            }

            var blockers = await GetCloseBlockersAsync(id);
            if (blockers.HasAny)
            {
                TempData["ErrorMessage"] =
                    $"Chưa thể đóng kỳ: {blockers.IncompleteKpiCount} KPI chưa hoàn tất, " +
                    $"{blockers.PendingCheckInCount} check-in chờ duyệt và " +
                    $"{blockers.IncompleteEvaluationCount} kết quả chưa được duyệt.";
                return RedirectToAction(nameof(Index));
            }

            var closedStatusId = await GetStatusIdAsync(EvaluationPeriodRules.StatusClosed);
            if (!closedStatusId.HasValue)
            {
                TempData["ErrorMessage"] = "Chưa cấu hình trạng thái Đóng cho kỳ đánh giá.";
                return RedirectToAction(nameof(Index));
            }

            var oldStatusId = period.StatusId;
            period.StatusId = closedStatusId;
            period.IsSystemProcessed = true;
            AddAuditLog("CLOSE", new { period.Id, StatusId = oldStatusId }, period);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã đóng kỳ đánh giá.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("EVALPERIODS_EDIT")]
        public async Task<IActionResult> Reopen(int id)
        {
            var period = await _context.EvaluationPeriods
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);
            if (period == null) return NotFound();

            var currentStatus = await GetStatusNameAsync(period.StatusId);
            var targetStatus = period.StartDate?.Date > DateTime.Today
                ? EvaluationPeriodRules.StatusOpen
                : EvaluationPeriodRules.StatusInProgress;
            if (!EvaluationPeriodRules.CanTransition(currentStatus, targetStatus))
            {
                TempData["ErrorMessage"] = "Chỉ kỳ đã đóng mới có thể mở lại.";
                return RedirectToAction(nameof(Index));
            }

            var targetStatusId = await GetStatusIdAsync(targetStatus);
            if (!targetStatusId.HasValue)
            {
                TempData["ErrorMessage"] = $"Chưa cấu hình trạng thái {targetStatus}.";
                return RedirectToAction(nameof(Index));
            }

            var oldStatusId = period.StatusId;
            period.StatusId = targetStatusId;
            period.IsSystemProcessed = false;
            AddAuditLog("REOPEN", new { period.Id, StatusId = oldStatusId }, period);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã mở lại kỳ đánh giá ở trạng thái {targetStatus}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> ChangeLifecycleStatusAsync(
            int id,
            string targetStatus,
            string successMessage)
        {
            var period = await _context.EvaluationPeriods
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);
            if (period == null) return NotFound();

            var currentStatus = await GetStatusNameAsync(period.StatusId);
            if (!EvaluationPeriodRules.CanTransition(currentStatus, targetStatus))
            {
                TempData["ErrorMessage"] = $"Không thể chuyển từ {currentStatus ?? "Không xác định"} sang {targetStatus}.";
                return RedirectToAction(nameof(Index));
            }

            if (targetStatus == EvaluationPeriodRules.StatusInProgress &&
                period.StartDate?.Date > DateTime.Today)
            {
                TempData["ErrorMessage"] = "Kỳ đánh giá chưa đến ngày bắt đầu nên chưa thể chuyển sang Đang xử lý.";
                return RedirectToAction(nameof(Index));
            }

            var targetStatusId = await GetStatusIdAsync(targetStatus);
            if (!targetStatusId.HasValue)
            {
                TempData["ErrorMessage"] = $"Chưa cấu hình trạng thái {targetStatus}.";
                return RedirectToAction(nameof(Index));
            }

            var oldStatusId = period.StatusId;
            period.StatusId = targetStatusId;
            AddAuditLog("STATUS_CHANGE", new { period.Id, StatusId = oldStatusId }, period);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = successMessage;
            return RedirectToAction(nameof(Index));
        }

        private Task<int?> GetStatusIdAsync(string statusName)
        {
            return _context.GetStatusIdAsync(
                WorkflowStatusHelper.StatusTypeEvaluationPeriod,
                statusName);
        }

        private async Task<string?> GetStatusNameAsync(int? statusId)
        {
            if (!statusId.HasValue) return null;
            return await _context.Statuses
                .Where(s => s.Id == statusId.Value &&
                            s.StatusType == WorkflowStatusHelper.StatusTypeEvaluationPeriod)
                .Select(s => s.StatusName)
                .FirstOrDefaultAsync();
        }

        private async Task<EvaluationPeriodDependencySummary> GetDependencySummaryAsync(int periodId)
        {
            var kpiIds = _context.KPIs.Where(k => k.PeriodId == periodId).Select(k => k.Id);
            return new EvaluationPeriodDependencySummary(
                await kpiIds.CountAsync(),
                await _context.KPICheckIns.CountAsync(c => c.KPIId.HasValue && kpiIds.Contains(c.KPIId.Value)),
                await _context.EvaluationResults.CountAsync(r => r.PeriodId == periodId));
        }

        private async Task<EvaluationPeriodCloseBlockers> GetCloseBlockersAsync(int periodId)
        {
            var finalKpiStatusIds = await _context.GetStatusIdsAsync(
                WorkflowStatusHelper.StatusTypeKpi,
                WorkflowStatusHelper.KpiFinalStatusNames);
            var periodKpiIds = _context.KPIs
                .Where(k => k.PeriodId == periodId)
                .Select(k => k.Id);
            var incompleteKpiCount = await _context.KPIs.CountAsync(k =>
                k.PeriodId == periodId && k.IsActive == true &&
                (!k.StatusId.HasValue || !finalKpiStatusIds.Contains(k.StatusId.Value)));
            var pendingCheckInCount = await _context.KPICheckIns.CountAsync(c =>
                c.KPIId.HasValue && periodKpiIds.Contains(c.KPIId.Value) && c.ReviewStatus == "Pending");
            var incompleteEvaluationCount = await _context.EvaluationResults.CountAsync(r =>
                r.PeriodId == periodId && r.SubmissionStatus != "Approved");
            return new EvaluationPeriodCloseBlockers(
                incompleteKpiCount,
                pendingCheckInCount,
                incompleteEvaluationCount);
        }

        private void AddAuditLog(string actionType, object? oldData, object? newData)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _context.AuditLogs.Add(new AuditLog
            {
                SystemUserId = int.TryParse(userIdValue, out var userId) ? userId : null,
                ActionType = actionType,
                ImpactedTable = "EvaluationPeriods",
                OldData = oldData == null ? null : JsonSerializer.Serialize(oldData),
                NewData = newData == null ? null : JsonSerializer.Serialize(newData),
                LogTime = DateTime.Now
            });
        }

        private sealed record EvaluationPeriodDependencySummary(
            int KpiCount,
            int CheckInCount,
            int EvaluationResultCount)
        {
            public bool HasAny => KpiCount > 0 || CheckInCount > 0 || EvaluationResultCount > 0;
        }

        private sealed record EvaluationPeriodCloseBlockers(
            int IncompleteKpiCount,
            int PendingCheckInCount,
            int IncompleteEvaluationCount)
        {
            public bool HasAny => IncompleteKpiCount > 0 || PendingCheckInCount > 0 || IncompleteEvaluationCount > 0;
        }
    }
}
