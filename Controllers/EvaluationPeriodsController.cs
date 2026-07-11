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
        public async Task<IActionResult> Create()
        {
            var statuses = await _context.Statuses
                .Where(s => s.StatusType == WorkflowStatusHelper.StatusTypeEvaluationPeriod)
                .ToDictionaryAsync(s => s.Id, s => s.StatusName);
            ViewBag.Statuses = statuses;
            return View();
        }

        [HttpPost]
        [HasPermission("EVALPERIODS_CREATE")]
        public async Task<IActionResult> Create(EvaluationPeriod model)
        {
            model.PeriodType = NormalizePeriodType(model.PeriodType);

            if (ModelState.IsValid)
            {
                var error = await ValidatePeriodAsync(model);
                if (error != null)
                {
                    TempData["ErrorMessage"] = error;
                    return RedirectToAction(nameof(Index));
                }

                model.IsActive = true;
                model.IsSystemProcessed = false;
                _context.EvaluationPeriods.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo kỳ đánh giá mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            var statuses = await _context.Statuses
                .Where(s => s.StatusType == WorkflowStatusHelper.StatusTypeEvaluationPeriod)
                .ToDictionaryAsync(s => s.Id, s => s.StatusName);
            ViewBag.Statuses = statuses;
            return View(model);
        }

        [HttpPost]
        [HasPermission("EVALPERIODS_EDIT")]
        public async Task<IActionResult> Edit(EvaluationPeriod model)
        {
            model.PeriodType = NormalizePeriodType(model.PeriodType);

            if (ModelState.IsValid)
            {
                var existing = await _context.EvaluationPeriods.FindAsync(model.Id);
                if (existing == null) return NotFound();

                var error = await ValidatePeriodAsync(model, model.Id);
                if (error != null)
                {
                    TempData["ErrorMessage"] = error;
                    return RedirectToAction(nameof(Index));
                }

                existing.PeriodName = model.PeriodName;
                existing.PeriodType = model.PeriodType;
                existing.StartDate = model.StartDate;
                existing.EndDate = model.EndDate;
                existing.StatusId = model.StatusId;

                _context.Update(existing);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã cập nhật kỳ đánh giá thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private static string? NormalizePeriodType(string? periodType)
        {
            var value = periodType?.Trim().ToUpperInvariant();
            return value switch
            {
                "MONTH" or "THANG" or "THÁNG" or "HANG THANG" or "HÀNG THÁNG" => "MONTH",
                "QUARTER" or "QUY" or "QUÝ" or "HANG QUY" or "HÀNG QUÝ" => "QUARTER",
                "YEAR" or "NAM" or "NĂM" or "HANG NAM" or "HÀNG NĂM" => "YEAR",
                _ => string.IsNullOrWhiteSpace(value) ? null : value
            };
        }

        private async Task<string?> ValidatePeriodAsync(EvaluationPeriod model, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(model.PeriodName) ||
                string.IsNullOrWhiteSpace(model.PeriodType) ||
                !model.StartDate.HasValue ||
                !model.EndDate.HasValue)
            {
                return "Vui lòng nhập đầy đủ tên kỳ, loại kỳ, ngày bắt đầu và ngày kết thúc.";
            }

            // 1. Kiểm tra trùng tên (giữa các bản ghi đang hoạt động)
            if (await _context.EvaluationPeriods.AnyAsync(p => p.PeriodName == model.PeriodName && p.IsActive == true && p.Id != excludeId))
            {
                return "Tên kỳ đánh giá đã tồn tại. Vui lòng chọn tên khác.";
            }

            // 2. Kiểm tra khoảng thời gian hợp lệ
            if (model.EndDate.Value < model.StartDate.Value)
            {
                return "Ngày kết thúc không thể trước ngày bắt đầu.";
            }

            // 3. Kiểm tra độ dài kỳ đánh giá
            var durationDays = (model.EndDate.Value - model.StartDate.Value).Days + 1;
            if (model.PeriodType == "MONTH" && durationDays > 32)
            {
                return "Kỳ đánh giá Hàng tháng không nên dài quá 31 ngày.";
            }
            else if (model.PeriodType == "QUARTER" && durationDays < 80)
            {
                return "Kỳ đánh giá Hàng quý phải có độ dài khoảng 3 tháng (ít nhất 80 ngày).";
            }

            // 4. Kiểm tra trùng lặp khoảng thời gian (Overlap check cho cùng loại kỳ)
            bool isOverlapping = await _context.EvaluationPeriods.AnyAsync(p => 
                p.IsActive == true && 
                p.Id != excludeId &&
                p.PeriodType == model.PeriodType &&
                p.StartDate.HasValue &&
                p.EndDate.HasValue &&
                model.StartDate.Value <= p.EndDate.Value &&
                model.EndDate.Value >= p.StartDate.Value);

            if (isOverlapping)
            {
                return "Khoảng thời gian này đã bị trùng lặp với một kỳ đánh giá khác cùng loại.";
            }

            return null;
        }

        [HttpPost]
        [HasPermission("EVALPERIODS_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            var period = await _context.EvaluationPeriods.FindAsync(id);
            if (period != null)
            {
                period.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa kỳ đánh giá!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
