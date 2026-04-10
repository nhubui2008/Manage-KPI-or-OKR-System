using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission(PermissionCodes.AdminManageRoles)]
    public class RolesController : Controller
    {
        private readonly MiniERPDbContext _context;

        public RolesController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles.ToListAsync();
            return View(roles);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string roleName, string description)
        {
            if (!string.IsNullOrEmpty(roleName))
            {
                var role = new Role 
                { 
                    RoleName = roleName, 
                    Description = description, 
                    IsActive = true, 
                    CreatedAt = DateTime.Now 
                };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo nhóm quyền mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role != null)
            {
                // Kiểm tra xem có người dùng nào đang gán role này không
                var hasUsers = await _context.SystemUsers.AnyAsync(u => u.RoleId == id);
                if (hasUsers)
                {
                    TempData["ErrorMessage"] = "Không thể xóa quyền này vì đang có nhân viên thuộc quyền này. Vui lòng chuyển nhân viên sang quyền khác trước khi thực hiện xóa.";
                    return RedirectToAction(nameof(Index));
                }

                try 
                {
                    _context.Roles.Remove(role);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Xóa nhóm quyền thành công!";
                }
                catch (DbUpdateException)
                {
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa nhóm quyền. Có thể có dữ liệu liên quan khác đang tham chiếu đến quyền này.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ManagePermissions(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();

            var corePermissionCodes = GetCorePermissions().Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var allPermissions = await _context.Permissions
                .Where(p => p.PermissionCode != null && corePermissionCodes.Contains(p.PermissionCode))
                .OrderBy(p => p.PermissionCode)
                .ToListAsync();
            var rolePermissionIds = await _context.Role_Permissions
                .Where(rp => rp.RoleId == id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            ViewBag.Role = role;
            ViewBag.RolePermissionIds = rolePermissionIds;

            return View(allPermissions);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePermissions(int roleId, List<int> permissionIds)
        {
            // Xóa các quyền cũ
            var oldPermissions = _context.Role_Permissions.Where(rp => rp.RoleId == roleId);
            _context.Role_Permissions.RemoveRange(oldPermissions);

            // Thêm các quyền mới
            if (permissionIds != null && permissionIds.Any())
            {
                foreach (var pId in permissionIds)
                {
                    _context.Role_Permissions.Add(new Role_Permission { RoleId = roleId, PermissionId = pId });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật cấu hình quyền chi tiết thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> SyncPermissions()
        {
            var permissions = GetCorePermissions();

            int addedCount = 0;
            int updatedCount = 0;
            foreach (var permission in permissions)
            {
                var existingPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.PermissionCode == permission.Key);

                if (existingPermission == null)
                {
                    _context.Permissions.Add(new Permission { 
                        PermissionCode = permission.Key,
                        PermissionName = permission.Value
                    });
                    addedCount++;
                }
                else if (!string.Equals(existingPermission.PermissionName, permission.Value, StringComparison.Ordinal))
                {
                    existingPermission.PermissionName = permission.Value;
                    updatedCount++;
                }
            }

            if (addedCount > 0 || updatedCount > 0)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã đồng bộ quyền thành công. Thêm mới: {addedCount}, cập nhật tên hiển thị: {updatedCount}.";
            }
            else
            {
                TempData["SuccessMessage"] = "Tất cả mã quyền đã tồn tại trong hệ thống.";
            }

            return RedirectToAction(nameof(Index));
        }

        private static Dictionary<string, string> GetCorePermissions()
        {
            return new Dictionary<string, string>
            {
                [PermissionCodes.AdminManageUsers] = "Quản lý tài khoản hệ thống",
                [PermissionCodes.AdminManageRoles] = "Quản lý nhóm quyền",
                [PermissionCodes.AdminViewAuditLogs] = "Xem nhật ký hệ thống",
                [PermissionCodes.HrManageEmployees] = "Quản lý nhân viên",
                [PermissionCodes.HrManageOrganization] = "Quản lý phòng ban và chức vụ",
                [PermissionCodes.HrApproveKpi] = "Quản lý kỳ đánh giá",
                [PermissionCodes.HrEvaluateKpi] = "Tạo và cập nhật kết quả đánh giá",
                [PermissionCodes.HrViewEvaluationReports] = "Xem báo cáo đánh giá",
                [PermissionCodes.HrManageBonusRules] = "Quản lý quy tắc thưởng",
                [PermissionCodes.ManagerManageMissionVision] = "Quản lý sứ mệnh và tầm nhìn",
                [PermissionCodes.ManagerCreateOkr] = "Tạo và quản lý OKR",
                [PermissionCodes.ManagerAssignKpi] = "Tạo, phân bổ và duyệt KPI",
                [PermissionCodes.EmployeeUpdateKpiProgress] = "Xem KPI/OKR được giao và thực hiện check-in"
            };
        }
    }
}
