using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class RolesController : Controller
    {
        private readonly MiniERPDbContext _context;

        public RolesController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HasPermission("ROLES_VIEW")]
        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles.ToListAsync();
            return View(roles);
        }

        [HttpGet]
        [HasPermission("ROLES_CREATE")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [HasPermission("ROLES_CREATE")]
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
        [HasPermission("ROLES_DELETE")]
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
        [HasPermission("ROLES_VIEW")]
        public async Task<IActionResult> ManagePermissions(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();

            var allPermissions = await _context.Permissions.ToListAsync();
            var rolePermissionIds = await _context.Role_Permissions
                .Where(rp => rp.RoleId == id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            ViewBag.Role = role;
            ViewBag.RolePermissionIds = rolePermissionIds;

            return View(allPermissions);
        }

        [HttpPost]
        [HasPermission("ROLES_EDIT")]
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
        [HasPermission("ROLES_CREATE")]
        public async Task<IActionResult> SyncPermissions()
        {
            var codes = new[] {
                // ===== PERMISSION CŨ (giữ nguyên để backward-compatible) =====
                "MANAGER_CREATE_OKR", "MANAGER_ASSIGN_KPI", "EMPLOYEE_UPDATE_KPI_PROGRESS",
                "HR_EVALUATE_KPI", "HR_MANAGE_EMPLOYEES", "SALES_CREATE_ORDERS",
                "SALES_MANAGE_CUSTOMERS", "SALES_CREATE_INVOICES", "WAREHOUSE_MANAGE_PRODUCTS",
                "WAREHOUSE_IMPORT_INVENTORY", "WAREHOUSE_VIEW_INVENTORY", "HR_APPROVE_KPI",
                "DELIVERY_UPDATE_STATUS", "DELIVERY_CREATE_NOTES", "ADMIN_VIEW_AUDIT_LOGS",
                "ADMIN_MANAGE_ROLES", "ADMIN_MANAGE_USERS",

                // ===== PERMISSION MỚI – PHÂN QUYỀN CHI TIẾT TỪNG CHỨC NĂNG =====
                // Nhân viên
                "EMPLOYEES_VIEW", "EMPLOYEES_CREATE", "EMPLOYEES_EDIT", "EMPLOYEES_DELETE",
                // Phòng ban
                "DEPARTMENTS_VIEW", "DEPARTMENTS_CREATE", "DEPARTMENTS_EDIT", "DEPARTMENTS_DELETE",
                // Chức vụ
                "POSITIONS_VIEW", "POSITIONS_CREATE", "POSITIONS_EDIT", "POSITIONS_DELETE",
                // KPI
                "KPIS_VIEW", "KPIS_CREATE", "KPIS_EDIT", "KPIS_DELETE",
                // KPI Check-in
                "KPICHECKINS_VIEW", "KPICHECKINS_CREATE",
                // OKR
                "OKRS_VIEW", "OKRS_CREATE", "OKRS_EDIT", "OKRS_DELETE",
                // Kỳ đánh giá
                "EVALPERIODS_VIEW", "EVALPERIODS_CREATE", "EVALPERIODS_EDIT", "EVALPERIODS_DELETE",
                // Kết quả đánh giá
                "EVALRESULTS_VIEW", "EVALRESULTS_CREATE", "EVALRESULTS_EDIT", "EVALRESULTS_DELETE",
                // Báo cáo đánh giá
                "EVALREPORTS_VIEW", "EVALREPORTS_EDIT",
                // Quy tắc thưởng
                "BONUSRULES_VIEW", "BONUSRULES_CREATE", "BONUSRULES_EDIT", "BONUSRULES_DELETE",
                // Sứ mệnh & Tầm nhìn
                "MISSIONS_VIEW", "MISSIONS_CREATE", "MISSIONS_DELETE",
                // Người dùng hệ thống
                "SYSUSERS_VIEW", "SYSUSERS_CREATE", "SYSUSERS_EDIT", "SYSUSERS_DELETE",
                // Vai trò (Roles)
                "ROLES_VIEW", "ROLES_CREATE", "ROLES_EDIT", "ROLES_DELETE",
                // Nhật ký hệ thống
                "AUDITLOGS_VIEW"
            };


            int addedCount = 0;
            foreach (var code in codes)
            {
                if (!await _context.Permissions.AnyAsync(p => p.PermissionCode == code.Trim()))
                {
                    _context.Permissions.Add(new Permission { 
                        PermissionCode = code.Trim(), 
                        PermissionName = code.Replace("_", " ").ToLower() 
                    });
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã đồng bộ {addedCount} mã quyền vào hệ thống!";
            }
            else
            {
                TempData["SuccessMessage"] = "Tất cả mã quyền đã tồn tại trong hệ thống.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
