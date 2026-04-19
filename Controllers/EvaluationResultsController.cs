using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class EvaluationResultsController : Controller
    {
        private const string SubmissionDraft = "Draft";
        private const string SubmissionPendingDirector = "PendingDirectorReview";
        private const string SubmissionApproved = "Approved";
        private const string SubmissionRejected = "Rejected";

        private readonly MiniERPDbContext _context;
        public EvaluationResultsController(MiniERPDbContext context) { _context = context; }

        [HasPermission("EVALRESULTS_VIEW")]
        public async Task<IActionResult> Index()
        {
            var resultsQuery = _context.EvaluationResults.OrderByDescending(r => r.Id).AsQueryable();

            // Filter Results if Sales or Employee
            if (User.IsInRole("Sales") || User.IsInRole("sales") || 
                User.IsInRole("Employee") || User.IsInRole("employee"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
                    if (employee != null)
                    {
                        resultsQuery = resultsQuery.Where(r => r.EmployeeId == employee.Id);
                    }
                    else
                    {
                        resultsQuery = resultsQuery.Where(r => false);
                    }
                }
            }
            else if (AccessScopeHelper.IsManagerScoped(User))
            {
                var manager = await GetCurrentEmployeeAsync();
                var managedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, manager);
                var managedEmployeeIds = await AccessScopeHelper.GetEmployeeIdsInDepartmentsAsync(_context, managedDepartmentIds);
                resultsQuery = managedEmployeeIds.Any()
                    ? resultsQuery.Where(r => r.EmployeeId.HasValue && managedEmployeeIds.Contains(r.EmployeeId.Value))
                    : resultsQuery.Where(r => false);
            }

            var results = await resultsQuery.ToListAsync();

            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName ?? "N/A");
            var periods = await _context.EvaluationPeriods.ToDictionaryAsync(p => p.Id, p => p.PeriodName ?? "N/A");
            var ranks = await _context.GradingRanks.ToDictionaryAsync(r => r.Id, r => r.RankCode ?? "N/A");
            var submitterIds = results
                .Where(r => r.SubmittedById.HasValue)
                .Select(r => r.SubmittedById!.Value)
                .Concat(results.Where(r => r.DirectorReviewedById.HasValue).Select(r => r.DirectorReviewedById!.Value))
                .Distinct()
                .ToList();
            var workflowEmployees = submitterIds.Any()
                ? await _context.Employees.Where(e => submitterIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.FullName ?? "N/A")
                : new Dictionary<int, string>();

            ViewBag.Employees = employees;
            ViewBag.Periods = periods;
            ViewBag.Ranks = ranks;
            ViewBag.WorkflowEmployees = workflowEmployees;
            ViewBag.AllEmployees = await GetEvaluationAssignableEmployeesAsync();
            ViewBag.AllPeriods = await _context.EvaluationPeriods.Where(p => p.IsActive == true).ToListAsync();
            var allRanks = await _context.GradingRanks.ToListAsync();
            ViewBag.AllRanks = allRanks;
            ViewBag.Classifications = allRanks.Where(r => !string.IsNullOrEmpty(r.Description))
                                              .Select(r => r.Description)
                                              .Distinct()
                                              .ToList();
            ViewBag.CanSubmitEvaluation = User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("Manager");
            ViewBag.CanReviewEvaluation = User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("Director");

            return View(results);
        }

        [HasPermission("EVALRESULTS_REVIEW", "EVALRESULTS_EDIT")]
        public async Task<IActionResult> ReviewBoard()
        {
            if (!(User.IsInRole("Admin") || User.IsInRole("Administrator") ||
                  User.IsInRole("Director") || User.IsInRole("Manager") ||
                  User.IsInRole("HR") || User.IsInRole("Human Resources")))
            {
                return Forbid();
            }

            var currentEmployee = await GetCurrentEmployeeAsync();
            var query = _context.EvaluationResults.AsQueryable();

            if (User.IsInRole("Director") || User.IsInRole("Admin") || User.IsInRole("Administrator"))
            {
                query = query.Where(r => r.SubmissionStatus == SubmissionPendingDirector);
            }
            else if (User.IsInRole("Manager") && currentEmployee != null)
            {
                query = query.Where(r => r.SubmittedById == currentEmployee.Id);
            }
            else
            {
                query = query.Where(r => false);
            }

            var results = await query.OrderByDescending(r => r.SubmittedAt ?? DateTime.MinValue).ToListAsync();
            var employeeIds = results
                .Select(r => r.EmployeeId ?? 0)
                .Concat(results.Select(r => r.SubmittedById ?? 0))
                .Concat(results.Select(r => r.DirectorReviewedById ?? 0))
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            var employees = employeeIds.Any()
                ? await _context.Employees.Where(e => employeeIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.FullName ?? "N/A")
                : new Dictionary<int, string>();

            var periodIds = results.Where(r => r.PeriodId.HasValue).Select(r => r.PeriodId!.Value).Distinct().ToList();
            var periods = periodIds.Any()
                ? await _context.EvaluationPeriods.Where(p => periodIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.PeriodName ?? "N/A")
                : new Dictionary<int, string>();

            var rankIds = results.Where(r => r.RankId.HasValue).Select(r => r.RankId!.Value).Distinct().ToList();
            var ranks = rankIds.Any()
                ? await _context.GradingRanks.Where(r => rankIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.RankCode ?? "N/A")
                : new Dictionary<int, string>();

            ViewBag.Employees = employees;
            ViewBag.Periods = periods;
            ViewBag.Ranks = ranks;
            ViewBag.CanDirectorReview = User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("Director");

            return View(results);
        }

        [HttpGet]
        [HasPermission("EVALRESULTS_CREATE")]
        public async Task<IActionResult> Create()
        {
            if (!(User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("Manager") || User.IsInRole("HR"))) 
                return Forbid();

            ViewBag.AllEmployees = await GetEvaluationAssignableEmployeesAsync();
            ViewBag.AllPeriods = await _context.EvaluationPeriods.Where(p => p.IsActive == true).ToListAsync();
            var allRanks = await _context.GradingRanks.ToListAsync();
            ViewBag.AllRanks = allRanks;
            ViewBag.Classifications = allRanks.Where(r => !string.IsNullOrEmpty(r.Description))
                                              .Select(r => r.Description)
                                              .Distinct()
                                              .ToList();
            return View();
        }

        [HttpPost]
        [HasPermission("EVALRESULTS_CREATE")]
        public async Task<IActionResult> Create(EvaluationResult model)
        {
            if (!(User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("Manager") || User.IsInRole("HR"))) 
                return Forbid();

            if (ModelState.IsValid)
            {
                if (!await CanCurrentUserAccessEvaluationEmployeeAsync(model.EmployeeId))
                {
                    return Forbid();
                }

                var isDuplicate = await _context.EvaluationResults
                    .AnyAsync(r => r.EmployeeId == model.EmployeeId && r.PeriodId == model.PeriodId);
                if (isDuplicate)
                {
                    TempData["ErrorMessage"] = "Kết quả đánh giá cho nhân viên này trong kỳ này đã tồn tại.";
                    return RedirectToAction(nameof(Index));
                }

                if (!await ApplyRankFromScoreAsync(model))
                {
                    return RedirectToAction(nameof(Index));
                }

                model.SubmissionStatus ??= SubmissionDraft;
                _context.EvaluationResults.Add(model);
                await _context.SaveChangesAsync();

                // Ghi nhật ký hệ thống (Audit Log)
                var employee = await _context.Employees.FindAsync(model.EmployeeId);
                var period = await _context.EvaluationPeriods.FindAsync(model.PeriodId);
                string scoreStr = model.TotalScore?.ToString("0.#") ?? "0";
                string auditInfo = $"Tạo mới kết quả đánh giá: {employee?.FullName} - {period?.PeriodName} - Điểm: {scoreStr} ({model.Classification})";
                await LogAuditAsync("CREATE", null, auditInfo);

                TempData["SuccessMessage"] = $"Đã lưu kết quả đánh giá thành công! Tổng điểm: {(model.TotalScore % 1 == 0 ? model.TotalScore?.ToString("0") : model.TotalScore?.ToString("0.#"))}đ";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("EVALRESULTS_EDIT")]
        public async Task<IActionResult> Edit(EvaluationResult model)
        {
            if (!(User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("Manager") || User.IsInRole("HR"))) 
                return Forbid();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu sửa kết quả đánh giá không hợp lệ. Vui lòng kiểm tra lại điểm, nhân viên và kỳ đánh giá.";
                return RedirectToAction(nameof(Index));
            }

            var existing = await _context.EvaluationResults.FindAsync(model.Id);
            if (existing == null) return NotFound();
            if (!await CanCurrentUserAccessEvaluationEmployeeAsync(model.EmployeeId) ||
                !await CanCurrentUserAccessEvaluationEmployeeAsync(existing.EmployeeId))
            {
                return Forbid();
            }

            var isDuplicate = await _context.EvaluationResults
                .AnyAsync(r => r.Id != model.Id &&
                               r.EmployeeId == model.EmployeeId &&
                               r.PeriodId == model.PeriodId);
            if (isDuplicate)
            {
                TempData["ErrorMessage"] = "Kết quả đánh giá cho nhân viên này trong kỳ này đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            // Lưu dữ liệu cũ để ghi Log
            var oldEmployee = await _context.Employees.FindAsync(existing.EmployeeId);
            var oldPeriod = await _context.EvaluationPeriods.FindAsync(existing.PeriodId);
            string oldInfo = $"Cũ: {oldEmployee?.FullName} - {oldPeriod?.PeriodName} - Điểm: {existing.TotalScore?.ToString("0.#")} ({existing.Classification})";

            existing.EmployeeId = model.EmployeeId;
            existing.PeriodId = model.PeriodId;
            existing.TotalScore = model.TotalScore;
            if (!await ApplyRankFromScoreAsync(existing))
            {
                return RedirectToAction(nameof(Index));
            }
            existing.ReviewComment = model.ReviewComment;

            _context.Update(existing);
            await _context.SaveChangesAsync();

            // Ghi nhật ký hệ thống (Audit Log)
            var newEmployee = await _context.Employees.FindAsync(model.EmployeeId);
            var newPeriod = await _context.EvaluationPeriods.FindAsync(model.PeriodId);
            string newInfo = $"Mới: {newEmployee?.FullName} - {newPeriod?.PeriodName} - Điểm: {existing.TotalScore?.ToString("0.#")} ({existing.Classification})";
            await LogAuditAsync("UPDATE", oldInfo, newInfo);

            TempData["SuccessMessage"] = $"Đã cập nhật kết quả đánh giá thành công! Tổng điểm: {(model.TotalScore % 1 == 0 ? model.TotalScore?.ToString("0") : model.TotalScore?.ToString("0.#"))}đ";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("EVALRESULTS_EDIT")]
        public async Task<IActionResult> SubmitForDirectorReview(int id, string? managerComment, string? returnUrl)
        {
            var result = await _context.EvaluationResults.FindAsync(id);
            if (result == null) return NotFound();

            var submitter = await GetCurrentEmployeeAsync();
            if (!await CanSubmitEvaluationAsync(result, submitter))
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(managerComment))
            {
                result.ReviewComment = managerComment.Trim();
            }

            result.SubmissionStatus = SubmissionPendingDirector;
            result.SubmittedById = submitter?.Id;
            result.SubmittedAt = DateTime.Now;
            result.DirectorReviewedById = null;
            result.DirectorReviewedAt = null;
            result.DirectorReviewComment = null;

            await _context.SaveChangesAsync();
            await LogAuditAsync("SUBMIT_REVIEW", null, $"Trưởng phòng gửi đánh giá #{id} lên giám đốc duyệt");

            TempData["SuccessMessage"] = "Đã gửi đánh giá lên giám đốc để review.";
            return RedirectBack(returnUrl);
        }

        [HttpPost]
        [HasPermission("EVALRESULTS_REVIEW", "EVALRESULTS_EDIT")]
        public async Task<IActionResult> DirectorReview(int id, string decision, string? directorReviewComment, string? returnUrl)
        {
            var result = await _context.EvaluationResults.FindAsync(id);
            if (result == null) return NotFound();

            var reviewer = await GetCurrentEmployeeAsync();
            if (!CanReviewEvaluation(reviewer))
            {
                return Forbid();
            }

            if (result.SubmissionStatus != SubmissionPendingDirector)
            {
                TempData["ErrorMessage"] = "Đánh giá này không còn ở trạng thái chờ giám đốc review.";
                return RedirectBack(returnUrl);
            }

            var isApproved = string.Equals(decision, SubmissionApproved, StringComparison.OrdinalIgnoreCase);
            var isRejected = string.Equals(decision, SubmissionRejected, StringComparison.OrdinalIgnoreCase);
            if (!isApproved && !isRejected)
            {
                TempData["ErrorMessage"] = "Quyết định review không hợp lệ.";
                return RedirectBack(returnUrl);
            }

            result.SubmissionStatus = isApproved ? SubmissionApproved : SubmissionRejected;
            result.DirectorReviewedById = reviewer?.Id;
            result.DirectorReviewedAt = DateTime.Now;
            result.DirectorReviewComment = directorReviewComment?.Trim();

            await _context.SaveChangesAsync();
            await LogAuditAsync(isApproved ? "DIRECTOR_APPROVE" : "DIRECTOR_REJECT", null, $"Giám đốc review đánh giá #{id}: {result.SubmissionStatus}");

            TempData["SuccessMessage"] = isApproved
                ? "Đã duyệt đánh giá và kết quả."
                : "Đã từ chối đánh giá, trưởng phòng cần rà soát lại.";
            return RedirectBack(returnUrl);
        }

        [HttpPost]
        [HasPermission("EVALRESULTS_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!(User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("Manager") || User.IsInRole("HR"))) 
                return Forbid();

            var result = await _context.EvaluationResults.FindAsync(id);
            if (result != null)
            {
                if (!await CanCurrentUserAccessEvaluationEmployeeAsync(result.EmployeeId))
                {
                    return Forbid();
                }

                var employee = await _context.Employees.FindAsync(result.EmployeeId);
                var period = await _context.EvaluationPeriods.FindAsync(result.PeriodId);
                string auditInfo = $"Xóa kết quả đánh giá: {employee?.FullName} - {period?.PeriodName} - Điểm: {result.TotalScore?.ToString("0.#")} ({result.Classification})";

                _context.EvaluationResults.Remove(result);
                await _context.SaveChangesAsync();

                // Ghi nhật ký hệ thống (Audit Log)
                await LogAuditAsync("DELETE", auditInfo, "Đã xóa bản ghi");

                TempData["SuccessMessage"] = "Đã xóa kết quả đánh giá!";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<Employee?> GetCurrentEmployeeAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return null;
            }

            return await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId && e.IsActive == true);
        }

        private async Task<bool> CanSubmitEvaluationAsync(EvaluationResult result, Employee? submitter)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Administrator"))
            {
                return true;
            }

            if (submitter == null || !User.IsInRole("Manager") || !result.EmployeeId.HasValue)
            {
                return false;
            }

            var managedDeptIds = await _context.Departments
                .Where(d => d.ManagerId == submitter.Id && d.IsActive == true)
                .Select(d => d.Id)
                .ToListAsync();

            if (!managedDeptIds.Any())
            {
                return false;
            }

            return await _context.EmployeeAssignments.AnyAsync(a =>
                a.EmployeeId == result.EmployeeId &&
                a.DepartmentId.HasValue &&
                managedDeptIds.Contains(a.DepartmentId.Value) &&
                a.IsActive == true);
        }

        private async Task<List<Employee>> GetEvaluationAssignableEmployeesAsync()
        {
            var query = _context.Employees.Where(e => e.IsActive == true);
            if (AccessScopeHelper.IsManagerScoped(User))
            {
                var manager = await GetCurrentEmployeeAsync();
                var managedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, manager);
                var managedEmployeeIds = await AccessScopeHelper.GetEmployeeIdsInDepartmentsAsync(_context, managedDepartmentIds);
                query = managedEmployeeIds.Any()
                    ? query.Where(e => managedEmployeeIds.Contains(e.Id))
                    : query.Where(e => false);
            }

            return await query.OrderBy(e => e.FullName).ToListAsync();
        }

        private async Task<bool> ApplyRankFromScoreAsync(EvaluationResult result)
        {
            if (!result.TotalScore.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập tổng điểm trước khi lưu kết quả đánh giá.";
                return false;
            }

            if (result.TotalScore.Value < 0 || result.TotalScore.Value > 100)
            {
                TempData["ErrorMessage"] = "Tổng điểm phải nằm trong khoảng 0 đến 100.";
                return false;
            }

            var rank = await _context.GradingRanks
                .Where(r => r.MinScore.HasValue && r.MinScore <= result.TotalScore.Value)
                .OrderByDescending(r => r.MinScore)
                .FirstOrDefaultAsync();

            if (rank == null)
            {
                TempData["ErrorMessage"] = "Chưa cấu hình bảng xếp hạng phù hợp với tổng điểm này.";
                return false;
            }

            result.RankId = rank.Id;
            result.Classification = rank.Description;
            return true;
        }

        private async Task<bool> CanCurrentUserAccessEvaluationEmployeeAsync(int? employeeId)
        {
            if (!employeeId.HasValue)
            {
                return false;
            }

            if (!AccessScopeHelper.IsManagerScoped(User))
            {
                return true;
            }

            return await AccessScopeHelper.CanManageEmployeeAsync(_context, User, employeeId.Value);
        }

        private bool CanReviewEvaluation(Employee? reviewer)
        {
            return User.IsInRole("Admin") || User.IsInRole("Administrator") ||
                   (reviewer != null && User.IsInRole("Director"));
        }

        private IActionResult RedirectBack(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LogAuditAsync(string actionType, string? oldData, string newData)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var log = new AuditLog
                {
                    SystemUserId = userId,
                    ActionType = actionType,
                    ImpactedTable = "EvaluationResults",
                    OldData = oldData,
                    NewData = newData,
                    LogTime = DateTime.Now
                };
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
        }
    }
}
