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
using System.Globalization;


namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class KPICheckInsController : Controller
    {
        private const string ReviewStatusPending = "Pending";
        private const string ReviewStatusApproved = "Approved";
        private const string ReviewStatusRejected = "Rejected";

        private readonly MiniERPDbContext _context;

        public KPICheckInsController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HasPermission("KPICHECKINS_VIEW", "CHECKINS_VIEW")]
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

                    var employeeDepartmentIds = await _context.EmployeeAssignments
                        .Where(a => a.EmployeeId == employee.Id && a.IsActive == true && a.DepartmentId.HasValue)
                        .Select(a => a.DepartmentId!.Value)
                        .ToListAsync();

                    var departmentAllocatedKpiIds = employeeDepartmentIds.Any()
                        ? await _context.KPI_Department_Assignments
                            .Where(a => employeeDepartmentIds.Contains(a.DepartmentId))
                            .Select(a => a.KPIId)
                            .ToListAsync()
                        : new List<int>();

                    allocatedKpiIds = allocatedKpiIds
                        .Concat(departmentAllocatedKpiIds)
                        .Distinct()
                        .ToList();

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

            var checkInComments = await _context.GoalComments
                .Where(c => c.CheckInId.HasValue && checkInIds.Contains(c.CheckInId.Value))
                .OrderBy(c => c.CommentTime)
                .ToListAsync();

            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id);
            var kpis = await _context.KPIs.ToDictionaryAsync(k => k.Id);
            var statuses = await _context.CheckInStatuses.ToDictionaryAsync(s => s.Id, s => s.StatusName);
            var allEmployees = await employeeQuery.ToListAsync();
            var allKpis = await kpiQuery.ToListAsync();
            var allKpiIds = allKpis.Select(k => k.Id).ToList();
            var kpiData = await _context.KPIDetails
                .Where(d => d.KPIId.HasValue && allKpiIds.Contains(d.KPIId.Value))
                .ToDictionaryAsync(d => d.KPIId ?? 0);

            ViewBag.Details = checkInDetails;
            ViewBag.Employees = employees;
            ViewBag.KPIs = kpis;
            ViewBag.CheckInStatuses = statuses;
            ViewBag.CheckInComments = checkInComments
                .GroupBy(c => c.CheckInId ?? 0)
                .ToDictionary(g => g.Key, g => g.ToList());
            ViewBag.AllEmployees = allEmployees;
            ViewBag.AllKPIs = allKpis;
            ViewBag.KPIData = kpiData;
            ViewBag.AllStatuses = await _context.CheckInStatuses.ToListAsync();
            ViewBag.FailReasons = await _context.FailReasons.ToListAsync();
            ViewBag.CanReviewCheckIns = User.IsInRole("Admin") || User.IsInRole("Administrator") ||
                User.IsInRole("Manager") || User.IsInRole("Director") ||
                User.IsInRole("HR") || User.IsInRole("Human Resources");
            ViewBag.ReturnUrl = Request.Path + Request.QueryString;

            return View(checkIns);
        }

        [HttpGet]
        [HasPermission("KPICHECKINS_CREATE", "CHECKINS_CREATE")]
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

            bool isManager = User.IsInRole("Manager");
            bool isDirector = User.IsInRole("Director");
            bool isRestrictedRole = isManager || isDirector ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales");

            if (isRestrictedRole)
            {
                if (employee != null)
                {
                    List<int> targetEmployeeIds = new List<int> { employee.Id };
                    var targetDepartmentIds = await _context.EmployeeAssignments
                        .Where(ea => ea.EmployeeId == employee.Id && ea.IsActive == true && ea.DepartmentId.HasValue)
                        .Select(ea => ea.DepartmentId!.Value)
                        .ToListAsync();

                    if (isManager)
                    {
                        // Trưởng phòng: Thấy KPI của nhân viên trong phòng ban
                        var managedDeptIds = await _context.Departments
                            .Where(d => d.ManagerId == employee.Id && d.IsActive == true)
                            .Select(d => d.Id)
                            .ToListAsync();

                        var deptEmployeeIds = await _context.EmployeeAssignments
                            .Where(ea => managedDeptIds.Contains(ea.DepartmentId ?? 0) && ea.IsActive == true)
                            .Select(ea => ea.EmployeeId ?? 0)
                            .ToListAsync();

                        targetEmployeeIds.AddRange(deptEmployeeIds);
                        targetDepartmentIds.AddRange(managedDeptIds);
                    }
                    else if (isDirector)
                    {
                        // Giám đốc: Thấy KPI của toàn bộ trưởng phòng
                        var allManagerIds = await _context.Departments
                            .Where(d => d.IsActive == true && d.ManagerId != null)
                            .Select(d => d.ManagerId!.Value)
                            .ToListAsync();

                        targetEmployeeIds.AddRange(allManagerIds);

                        var allDepartmentIds = await _context.Departments
                            .Where(d => d.IsActive == true)
                            .Select(d => d.Id)
                            .ToListAsync();

                        targetDepartmentIds.AddRange(allDepartmentIds);
                    }

                    targetEmployeeIds = targetEmployeeIds.Distinct().ToList();
                    targetDepartmentIds = targetDepartmentIds.Distinct().ToList();

                    var employeeAllowedKpiIds = await _context.KPI_Employee_Assignments
                        .Where(a => targetEmployeeIds.Contains(a.EmployeeId) && (a.Status == null || a.Status == "Active"))
                        .Select(a => a.KPIId)
                        .ToListAsync();

                    var departmentAllowedKpiIds = targetDepartmentIds.Any()
                        ? await _context.KPI_Department_Assignments
                            .Where(a => targetDepartmentIds.Contains(a.DepartmentId))
                            .Select(a => a.KPIId)
                            .ToListAsync()
                        : new List<int>();

                    var allowedKpiIds = employeeAllowedKpiIds
                        .Concat(departmentAllowedKpiIds)
                        .Distinct()
                        .ToList();

                    kpiQuery = kpiQuery.Where(k => allowedKpiIds.Contains(k.Id) || k.AssignerId == employee.Id);

                    // Lọc danh sách nhân viên để chọn (nếu là Manager/Director có quyền chọn cho người khác)
                    if (isManager || isDirector)
                    {
                        employeeQuery = employeeQuery.Where(e => targetEmployeeIds.Contains(e.Id));
                    }
                    else
                    {
                        employeeQuery = employeeQuery.Where(e => e.Id == employee.Id);
                    }
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

            var visibleEmployeeIds = ((List<Employee>)ViewBag.AllEmployees).Select(e => e.Id).ToList();
            var assignmentWeights = await _context.KPI_Employee_Assignments
                .Where(a => visibleEmployeeIds.Contains(a.EmployeeId) &&
                            kpiIds.Contains(a.KPIId) &&
                            (a.Status == null || a.Status == "Active"))
                .ToListAsync();

            ViewBag.AssignmentWeights = assignmentWeights
                .GroupBy(a => a.EmployeeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(a => a.KPIId, a => (a.Weight ?? 1m) * 100));

            ViewBag.EmployeeKPIIds = await BuildEmployeeKpiMapAsync((List<Employee>)ViewBag.AllEmployees, kpis);
        }

        private async Task<Dictionary<int, List<int>>> BuildEmployeeKpiMapAsync(List<Employee> employees, List<KPI> kpis)
        {
            var employeeIds = employees.Select(e => e.Id).Distinct().ToList();
            var kpiIds = kpis.Select(k => k.Id).Distinct().ToList();
            var kpiOrder = kpiIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
            var map = employeeIds.ToDictionary(id => id, _ => new HashSet<int>());

            if (!employeeIds.Any() || !kpiIds.Any())
            {
                return map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
            }

            var directAssignments = await _context.KPI_Employee_Assignments
                .Where(a => employeeIds.Contains(a.EmployeeId) &&
                            kpiIds.Contains(a.KPIId) &&
                            (a.Status == null || a.Status == "Active"))
                .Select(a => new { a.EmployeeId, a.KPIId })
                .ToListAsync();

            foreach (var assignment in directAssignments)
            {
                map[assignment.EmployeeId].Add(assignment.KPIId);
            }

            var employeeDepartmentPairs = await _context.EmployeeAssignments
                .Where(a => a.EmployeeId.HasValue &&
                            employeeIds.Contains(a.EmployeeId.Value) &&
                            a.DepartmentId.HasValue &&
                            a.IsActive == true)
                .Select(a => new { EmployeeId = a.EmployeeId!.Value, DepartmentId = a.DepartmentId!.Value })
                .ToListAsync();

            var departmentIds = employeeDepartmentPairs.Select(a => a.DepartmentId).Distinct().ToList();
            if (departmentIds.Any())
            {
                var departmentAssignments = await _context.KPI_Department_Assignments
                    .Where(a => departmentIds.Contains(a.DepartmentId) && kpiIds.Contains(a.KPIId))
                    .Select(a => new { a.DepartmentId, a.KPIId })
                    .ToListAsync();

                var kpisByDepartment = departmentAssignments
                    .GroupBy(a => a.DepartmentId)
                    .ToDictionary(g => g.Key, g => g.Select(a => a.KPIId).Distinct().ToList());

                foreach (var employeeDepartment in employeeDepartmentPairs)
                {
                    if (!kpisByDepartment.TryGetValue(employeeDepartment.DepartmentId, out var departmentKpiIds))
                    {
                        continue;
                    }

                    foreach (var kpiId in departmentKpiIds)
                    {
                        map[employeeDepartment.EmployeeId].Add(kpiId);
                    }
                }
            }

            foreach (var kpi in kpis.Where(k => k.AssignerId.HasValue && employeeIds.Contains(k.AssignerId.Value)))
            {
                map[kpi.AssignerId!.Value].Add(kpi.Id);
            }

            return map.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
                    .OrderBy(id => kpiOrder.GetValueOrDefault(id, int.MaxValue))
                    .ToList());
        }

        [HttpPost]
        [HasPermission("KPICHECKINS_CREATE", "CHECKINS_CREATE", "EMPLOYEE_UPDATE_KPI_PROGRESS")]
        public async Task<IActionResult> Create(KPICheckIn model, string AchievedValue, string Note)
        {
            decimal achievedValue = 0;
            if (string.IsNullOrWhiteSpace(AchievedValue))
            {
                ModelState.AddModelError("AchievedValue", "Vui lòng nhập kết quả đạt được.");
            }
            else
            {
                // Thay thế dấu phẩy bằng dấu chấm để luôn có định dạng chuẩn dot-separator nếu người dùng nhập tay dấu phẩy
                // Tuy nhiên type="number" luôn gửi dấu chấm.
                string normalizedValue = AchievedValue.Replace(",", ".");
                if (!decimal.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out achievedValue))
                {
                    ModelState.AddModelError("AchievedValue", "Giá trị đạt được không hợp lệ.");
                }
            }

            bool isRestrictedRole = User.IsInRole("Employee") || User.IsInRole("employee") ||
                                    User.IsInRole("Sales") || User.IsInRole("sales");
            Employee? currentEmployee = await GetCurrentEmployeeAsync();

            if (!string.IsNullOrWhiteSpace(AchievedValue) && achievedValue < 0)
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

                var selectedEmployeeDepartmentIds = await _context.EmployeeAssignments
                    .Where(a => a.EmployeeId == model.EmployeeId.Value &&
                                a.IsActive == true &&
                                a.DepartmentId.HasValue)
                    .Select(a => a.DepartmentId!.Value)
                    .ToListAsync();

                var isAssignedToDepartment = selectedEmployeeDepartmentIds.Any() && await _context.KPI_Department_Assignments
                    .AnyAsync(a => a.KPIId == kpi.Id && selectedEmployeeDepartmentIds.Contains(a.DepartmentId));

                if (!isAssignedToKpi && !isAssignedToDepartment && kpi.AssignerId != model.EmployeeId.Value)
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
                model.SubmittedById = currentEmployee?.Id;
                var isReviewerSubmitting = User.IsInRole("Admin") || User.IsInRole("Administrator") ||
                                           User.IsInRole("Manager") || User.IsInRole("Director") ||
                                           User.IsInRole("HR") || User.IsInRole("Human Resources");
                model.ReviewStatus = isReviewerSubmitting ? ReviewStatusApproved : ReviewStatusPending;
                if (isReviewerSubmitting)
                {
                    model.ReviewedById = currentEmployee?.Id;
                    model.ReviewedAt = DateTime.Now;
                    model.ReviewComment = "Tự động xác nhận vì người cập nhật có quyền quản lý.";
                }
                _context.KPICheckIns.Add(model);
                await _context.SaveChangesAsync();

                // 2. Tính % tiến độ theo cấu hình KPI (Dựa trên số lượng công việc được giao/trọng số)
                decimal progress = 0;
                if (kpiDetail != null)
                {
                    // Lấy trọng số của nhân viên cho KPI này
                    var assignment = await _context.KPI_Employee_Assignments
                        .FirstOrDefaultAsync(a => a.KPIId == model.KPIId && a.EmployeeId == model.EmployeeId && (a.Status == null || a.Status == "Active"));

                    decimal weight = assignment?.Weight ?? 1.0m;
                    if (weight <= 0) weight = 1.0m; // Default to 100% if weight not set or 0

                    decimal individualTarget = (kpiDetail.TargetValue ?? 0) * weight;
                    progress = ProgressHelper.CalculateProgress(achievedValue, individualTarget, kpiDetail.IsInverse);
                }

                // Tự động gán trạng thái dựa trên tiến độ thực tế (1: On Track, 2: At Risk, 3: Late)
                if (progress >= 95) model.StatusId = 1;
                else if (progress >= 50) model.StatusId = 2;
                else model.StatusId = 3;

                // 3. Lưu thông tin chi tiết (Achieved Value, Note, Progress %)
                var detail = new CheckInDetail
                {
                    CheckInId = model.Id,
                    AchievedValue = achievedValue,
                    Note = Note,
                    ProgressPercentage = Math.Round(progress, 2)
                };
                _context.CheckInDetails.Add(detail);

                if (model.ReviewStatus != ReviewStatusApproved)
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Đã gửi check-in KPI và đang chờ quản lý xác nhận trước khi cập nhật điểm chính thức.";
                    return RedirectToAction(nameof(Index));
                }

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
                        ? ProgressHelper.CalculateProgress(achievedValue, passThreshold, kpiDetail.IsInverse)
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

                var employeeDepartmentIdsForScore = await _context.EmployeeAssignments
                    .Where(a => a.EmployeeId == model.EmployeeId &&
                                a.IsActive == true &&
                                a.DepartmentId.HasValue)
                    .Select(a => a.DepartmentId!.Value)
                    .ToListAsync();

                var departmentAssignedKpiIds = employeeDepartmentIdsForScore.Any()
                    ? await _context.KPI_Department_Assignments
                        .Where(a => employeeDepartmentIdsForScore.Contains(a.DepartmentId))
                        .Select(a => a.KPIId)
                        .ToListAsync()
                    : new List<int>();

                var assignedKpiIds = assignedKpiWeights
                    .Select(a => a.KPIId)
                    .Concat(departmentAssignedKpiIds)
                    .Distinct()
                    .ToList();

                var weightByKpiId = assignedKpiWeights
                    .GroupBy(a => a.KPIId)
                    .ToDictionary(a => a.Key, a => a.First().Weight <= 0 ? 1m : a.First().Weight);

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
                            .Where(c => c.KPIId == pk.Id &&
                                        c.EmployeeId == model.EmployeeId &&
                                        (c.ReviewStatus == ReviewStatusApproved || c.ReviewStatus == null))
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

        [HttpPost]
        [HasPermission("KPICHECKINS_REVIEW", "CHECKINS_EDIT")]
        public async Task<IActionResult> Review(int id, string decision, string? reviewComment, string? reviewScore, string? returnUrl)
        {
            var checkIn = await _context.KPICheckIns.FirstOrDefaultAsync(c => c.Id == id);
            if (checkIn == null) return NotFound();

            var reviewer = await GetCurrentEmployeeAsync();
            if (!await CanReviewCheckInAsync(checkIn, reviewer))
            {
                return Forbid();
            }

            var currentReviewStatus = string.IsNullOrWhiteSpace(checkIn.ReviewStatus)
                ? ReviewStatusApproved
                : checkIn.ReviewStatus;
            if (!string.Equals(currentReviewStatus, ReviewStatusPending, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Check-in này đã được xử lý, không thể duyệt lại.";
                return RedirectBack(returnUrl);
            }

            var isApproved = string.Equals(decision, ReviewStatusApproved, StringComparison.OrdinalIgnoreCase);
            var isRejected = string.Equals(decision, ReviewStatusRejected, StringComparison.OrdinalIgnoreCase);
            if (!isApproved && !isRejected)
            {
                TempData["ErrorMessage"] = "Trạng thái xác nhận không hợp lệ.";
                return RedirectBack(returnUrl);
            }

            if (!TryParseOptionalScore(reviewScore, out var score, out var scoreError))
            {
                TempData["ErrorMessage"] = scoreError;
                return RedirectBack(returnUrl);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                checkIn.ReviewStatus = isApproved ? ReviewStatusApproved : ReviewStatusRejected;
                checkIn.ReviewedById = reviewer?.Id;
                checkIn.ReviewedAt = DateTime.Now;
                checkIn.ReviewComment = reviewComment?.Trim();
                checkIn.ReviewScore = score;

                if (!string.IsNullOrWhiteSpace(checkIn.ReviewComment) || score.HasValue)
                {
                    _context.GoalComments.Add(new GoalComment
                    {
                        KPIId = checkIn.KPIId,
                        CheckInId = checkIn.Id,
                        CommenterId = reviewer?.Id,
                        CommentType = isApproved ? "ReviewApproved" : "ReviewRejected",
                        Rating = score,
                        Content = checkIn.ReviewComment,
                        CommentTime = DateTime.Now
                    });
                }

                if (isApproved)
                {
                    var detail = await _context.CheckInDetails.FirstOrDefaultAsync(d => d.CheckInId == checkIn.Id);
                    var kpi = checkIn.KPIId.HasValue
                        ? await _context.KPIs.FirstOrDefaultAsync(k => k.Id == checkIn.KPIId.Value && k.IsActive == true)
                        : null;
                    var kpiDetail = checkIn.KPIId.HasValue
                        ? await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == checkIn.KPIId.Value)
                        : null;

                    if (detail != null && kpi != null)
                    {
                        await ApplyApprovedCheckInImpactAsync(checkIn, detail, kpi, kpiDetail);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = isApproved
                    ? "Đã xác nhận check-in KPI và cập nhật điểm chính thức."
                    : "Đã từ chối check-in KPI. Kết quả này sẽ không được tính vào đánh giá chính thức.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi khi xử lý xác nhận check-in: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectBack(returnUrl);
        }

        [HttpPost]
        [HasPermission("KPICHECKINS_VIEW", "CHECKINS_VIEW", "KPIS_VIEW")]
        public async Task<IActionResult> AddComment(int? kpiId, int? checkInId, string content, string? rating, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập nội dung bình luận.";
                return RedirectBack(returnUrl);
            }

            if (!TryParseOptionalScore(rating, out var parsedRating, out var ratingError))
            {
                TempData["ErrorMessage"] = ratingError;
                return RedirectBack(returnUrl);
            }

            var commenter = await GetCurrentEmployeeAsync();
            KPICheckIn? checkIn = null;
            if (checkInId.HasValue)
            {
                checkIn = await _context.KPICheckIns.FirstOrDefaultAsync(c => c.Id == checkInId.Value);
                if (checkIn == null) return NotFound();

                if (!await CanAccessCheckInAsync(checkIn, commenter))
                {
                    return Forbid();
                }

                kpiId = checkIn.KPIId;
            }

            _context.GoalComments.Add(new GoalComment
            {
                KPIId = kpiId,
                CheckInId = checkInId,
                CommenterId = commenter?.Id,
                CommentType = "Comment",
                Rating = parsedRating,
                Content = content.Trim(),
                CommentTime = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã thêm bình luận/đánh giá cho KPI.";
            return RedirectBack(returnUrl);
        }

        private async Task ApplyApprovedCheckInImpactAsync(KPICheckIn checkIn, CheckInDetail detail, KPI kpi, KPIDetail? kpiDetail)
        {
            var progress = detail.ProgressPercentage ?? 0m;
            var achievedValue = detail.AchievedValue ?? 0m;

            if (kpiDetail != null)
            {
                decimal passThreshold = kpiDetail.PassThreshold ?? kpiDetail.TargetValue ?? 100;
                decimal passProgress = passThreshold > 0
                    ? ProgressHelper.CalculateProgress(achievedValue, passThreshold, kpiDetail.IsInverse)
                    : progress;

                if (progress >= 100)
                {
                    kpi.StatusId = 4;
                }
                else if (passProgress >= 100 || progress >= 70)
                {
                    kpi.StatusId = 5;
                }
                else if (progress >= 40)
                {
                    kpi.StatusId = 3;
                }
                else
                {
                    kpi.StatusId = 6;
                }
            }
            else
            {
                kpi.StatusId = 3;
            }

            if (!checkIn.EmployeeId.HasValue)
            {
                return;
            }

            var assignedKpiWeights = await _context.KPI_Employee_Assignments
                .Where(a => a.EmployeeId == checkIn.EmployeeId && (a.Status == null || a.Status == "Active"))
                .Select(a => new { a.KPIId, Weight = a.Weight ?? 1m })
                .ToListAsync();

            var employeeDepartmentIdsForScore = await _context.EmployeeAssignments
                .Where(a => a.EmployeeId == checkIn.EmployeeId &&
                            a.IsActive == true &&
                            a.DepartmentId.HasValue)
                .Select(a => a.DepartmentId!.Value)
                .ToListAsync();

            var departmentAssignedKpiIds = employeeDepartmentIdsForScore.Any()
                ? await _context.KPI_Department_Assignments
                    .Where(a => employeeDepartmentIdsForScore.Contains(a.DepartmentId))
                    .Select(a => a.KPIId)
                    .ToListAsync()
                : new List<int>();

            var assignedKpiIds = assignedKpiWeights
                .Select(a => a.KPIId)
                .Concat(departmentAssignedKpiIds)
                .Distinct()
                .ToList();

            var weightByKpiId = assignedKpiWeights
                .GroupBy(a => a.KPIId)
                .ToDictionary(a => a.Key, a => a.First().Weight <= 0 ? 1m : a.First().Weight);

            var periodKpis = await _context.KPIs
                .Where(pk => pk.PeriodId == kpi.PeriodId && pk.IsActive == true && assignedKpiIds.Contains(pk.Id))
                .ToListAsync();

            decimal totalScore = 0;
            if (periodKpis.Any())
            {
                decimal weightedProgress = 0;
                decimal totalWeight = 0;
                foreach (var periodKpi in periodKpis)
                {
                    var weight = weightByKpiId.GetValueOrDefault(periodKpi.Id, 1m);
                    totalWeight += weight;

                    var latestCheckIn = await _context.KPICheckIns
                        .Where(c => c.KPIId == periodKpi.Id &&
                                    c.EmployeeId == checkIn.EmployeeId &&
                                    (c.ReviewStatus == ReviewStatusApproved || c.ReviewStatus == null))
                        .OrderByDescending(c => c.CheckInDate)
                        .FirstOrDefaultAsync();

                    if (latestCheckIn != null)
                    {
                        var latestDetail = await _context.CheckInDetails.FirstOrDefaultAsync(d => d.CheckInId == latestCheckIn.Id);
                        if (latestDetail != null)
                        {
                            weightedProgress += (latestDetail.ProgressPercentage ?? 0) * weight;
                        }
                    }
                }

                if (totalWeight > 0)
                {
                    totalScore = Math.Round(weightedProgress / totalWeight, 2);
                }
            }

            var rank = await _context.GradingRanks
                .Where(r => r.MinScore <= totalScore)
                .OrderByDescending(r => r.MinScore)
                .FirstOrDefaultAsync();

            if (rank == null)
            {
                return;
            }

            var evalResult = await _context.EvaluationResults
                .FirstOrDefaultAsync(er => er.EmployeeId == checkIn.EmployeeId && er.PeriodId == kpi.PeriodId);

            if (evalResult == null)
            {
                evalResult = new EvaluationResult
                {
                    EmployeeId = checkIn.EmployeeId,
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

            var bonusRule = await _context.BonusRules.FirstOrDefaultAsync(br => br.RankId == rank.Id);
            if (bonusRule == null)
            {
                return;
            }

            var expectedBonus = await _context.RealtimeExpectedBonuses
                .FirstOrDefaultAsync(rb => rb.EmployeeId == checkIn.EmployeeId && rb.PeriodId == kpi.PeriodId);

            decimal fixedAmount = bonusRule.FixedAmount ?? 0;
            decimal percentageAmount = fixedAmount != 0 && bonusRule.BonusPercentage.HasValue
                ? fixedAmount * bonusRule.BonusPercentage.Value / 100m
                : 0m;
            decimal bonusAmount = fixedAmount + percentageAmount;

            if (expectedBonus == null)
            {
                expectedBonus = new RealtimeExpectedBonus
                {
                    EmployeeId = checkIn.EmployeeId,
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

        private async Task<Employee?> GetCurrentEmployeeAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return null;
            }

            return await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId && e.IsActive == true);
        }

        private async Task<bool> CanAccessCheckInAsync(KPICheckIn checkIn, Employee? currentEmployee)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Administrator") ||
                User.IsInRole("HR") || User.IsInRole("Human Resources") ||
                User.IsInRole("Director"))
            {
                return true;
            }

            if (currentEmployee == null)
            {
                return false;
            }

            if (checkIn.EmployeeId == currentEmployee.Id || checkIn.SubmittedById == currentEmployee.Id)
            {
                return true;
            }

            return await CanReviewCheckInAsync(checkIn, currentEmployee);
        }

        private async Task<bool> CanReviewCheckInAsync(KPICheckIn checkIn, Employee? reviewer)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Administrator") ||
                User.IsInRole("HR") || User.IsInRole("Human Resources") ||
                User.IsInRole("Director"))
            {
                return true;
            }

            if (reviewer == null || !User.IsInRole("Manager") || checkIn.EmployeeId == reviewer.Id)
            {
                return false;
            }

            var managedDeptIds = await _context.Departments
                .Where(d => d.ManagerId == reviewer.Id && d.IsActive == true)
                .Select(d => d.Id)
                .ToListAsync();

            if (managedDeptIds.Any())
            {
                var employeeDeptIds = await _context.EmployeeAssignments
                    .Where(a => a.EmployeeId == checkIn.EmployeeId &&
                                a.IsActive == true &&
                                a.DepartmentId.HasValue)
                    .Select(a => a.DepartmentId!.Value)
                    .ToListAsync();

                if (employeeDeptIds.Any(id => managedDeptIds.Contains(id)))
                {
                    return true;
                }
            }

            return checkIn.KPIId.HasValue && await _context.KPIs
                .AnyAsync(k => k.Id == checkIn.KPIId.Value && k.AssignerId == reviewer.Id);
        }

        private static bool TryParseOptionalScore(string? rawScore, out decimal? score, out string? error)
        {
            score = null;
            error = null;

            if (string.IsNullOrWhiteSpace(rawScore))
            {
                return true;
            }

            var normalized = rawScore.Replace(",", ".");
            if (!decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedScore))
            {
                error = "Điểm đánh giá không hợp lệ.";
                return false;
            }

            if (parsedScore < 0 || parsedScore > 100)
            {
                error = "Điểm đánh giá phải nằm trong khoảng 0 đến 100.";
                return false;
            }

            score = Math.Round(parsedScore, 2);
            return true;
        }

        private IActionResult RedirectBack(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
