using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class SystemUsersTenantIsolationTests
{
    [Fact]
    public async Task Index_lists_only_current_tenant_and_uses_membership_role_and_status()
    {
        var tenantContext = SeedContext();
        await using var context = CreateContext(tenantContext);
        var roles = await SeedCatalogAsync(context);
        var first = User("first", roles.Employee.Id, isActive: false);
        var second = User("second", roles.Manager.Id);
        var shared = User("shared", roles.Employee.Id);
        context.SystemUsers.AddRange(first, second, shared);
        await context.SaveChangesAsync();
        context.TenantMemberships.AddRange(
            Membership(1, first, roles.Manager.Id, isActive: true),
            Membership(2, second, roles.Employee.Id, isActive: true),
            Membership(1, shared, roles.Employee.Id, isActive: false),
            Membership(2, shared, roles.Manager.Id, isActive: true));
        await context.SaveChangesAsync();
        SelectTenant(tenantContext);

        var controller = Controller(context, tenantContext);
        var view = Assert.IsType<ViewResult>(await controller.Index("", null, ""));
        var users = Assert.IsAssignableFrom<IEnumerable<SystemUser>>(view.Model).ToList();

        Assert.Equal(2, users.Count);
        Assert.DoesNotContain(users, item => item.Id == second.Id);
        var tenantFirst = Assert.Single(users, item => item.Id == first.Id);
        Assert.Equal(roles.Manager.Id, tenantFirst.RoleId);
        Assert.True(tenantFirst.IsActive);
        var tenantShared = Assert.Single(users, item => item.Id == shared.Id);
        Assert.Equal(roles.Employee.Id, tenantShared.RoleId);
        Assert.False(tenantShared.IsActive);
    }

    [Fact]
    public async Task AssignRole_and_ToggleLock_change_only_current_membership()
    {
        var tenantContext = SeedContext();
        await using var context = CreateContext(tenantContext);
        var roles = await SeedCatalogAsync(context);
        var target = User("target", roles.Employee.Id);
        context.SystemUsers.Add(target);
        await context.SaveChangesAsync();
        context.TenantMemberships.AddRange(
            Membership(1, target, roles.Employee.Id, isActive: true),
            Membership(2, target, roles.Manager.Id, isActive: true));
        await context.SaveChangesAsync();
        SelectTenant(tenantContext);

        var controller = Controller(context, tenantContext);
        Assert.IsType<RedirectToActionResult>(
            await controller.AssignRole(target.Id, roles.Manager.Id));
        Assert.IsType<RedirectToActionResult>(
            await controller.ToggleLock(target.Id));

        var tenantOne = await context.TenantMemberships
            .SingleAsync(item => item.TenantId == 1 && item.SystemUserId == target.Id);
        var tenantTwo = await context.TenantMemberships
            .SingleAsync(item => item.TenantId == 2 && item.SystemUserId == target.Id);
        Assert.Equal(roles.Manager.Id, tenantOne.RoleId);
        Assert.False(tenantOne.IsActive);
        Assert.Equal(roles.Manager.Id, tenantTwo.RoleId);
        Assert.True(tenantTwo.IsActive);
        Assert.Equal(roles.Employee.Id, target.RoleId);
        Assert.True(target.IsActive);
    }

    [Fact]
    public async Task Create_stores_role_and_activation_in_current_tenant_membership()
    {
        var tenantContext = SeedContext();
        await using var context = CreateContext(tenantContext);
        var roles = await SeedCatalogAsync(context);
        SelectTenant(tenantContext);
        var controller = Controller(context, tenantContext);

        var result = await controller.Create(new SystemUser
        {
            Username = "new-user",
            Email = "new-user@example.test",
            PasswordHash = "safePass123",
            RoleId = roles.Manager.Id
        });

        Assert.IsType<RedirectToActionResult>(result);
        var user = await context.SystemUsers.SingleAsync(item => item.Username == "new-user");
        var membership = await context.TenantMemberships
            .SingleAsync(item => item.SystemUserId == user.Id);
        Assert.Null(user.RoleId);
        Assert.True(user.IsActive);
        Assert.Equal(1, membership.TenantId);
        Assert.Equal(roles.Manager.Id, membership.RoleId);
        Assert.True(membership.IsActive);
        Assert.NotEqual("safePass123", user.PasswordHash);
    }

    [Fact]
    public async Task Multi_tenant_user_blocks_global_identity_change_but_allows_tenant_role_change()
    {
        var tenantContext = SeedContext();
        await using var context = CreateContext(tenantContext);
        var roles = await SeedCatalogAsync(context);
        var target = User("shared-user", roles.Employee.Id);
        context.SystemUsers.Add(target);
        await context.SaveChangesAsync();
        context.TenantMemberships.AddRange(
            Membership(1, target, roles.Employee.Id, isActive: true),
            Membership(2, target, roles.Employee.Id, isActive: true));
        await context.SaveChangesAsync();
        SelectTenant(tenantContext);

        var forbiddenController = Controller(context, tenantContext);
        var forbidden = await forbiddenController.Edit(
            target.Id,
            new SystemUser
            {
                Id = target.Id,
                Username = "renamed",
                Email = target.Email,
                RoleId = roles.Manager.Id,
                IsActive = true
            },
            newPassword: null);
        Assert.IsType<ForbidResult>(forbidden);
        Assert.Equal("shared-user", target.Username);

        var allowedController = Controller(context, tenantContext);
        var allowed = await allowedController.Edit(
            target.Id,
            new SystemUser
            {
                Id = target.Id,
                Username = target.Username,
                Email = target.Email,
                RoleId = roles.Manager.Id,
                IsActive = true
            },
            newPassword: null);
        Assert.IsType<RedirectToActionResult>(allowed);
        var tenantOne = await context.TenantMemberships
            .SingleAsync(item => item.TenantId == 1 && item.SystemUserId == target.Id);
        var tenantTwo = await context.TenantMemberships
            .SingleAsync(item => item.TenantId == 2 && item.SystemUserId == target.Id);
        Assert.Equal(roles.Manager.Id, tenantOne.RoleId);
        Assert.Equal(roles.Employee.Id, tenantTwo.RoleId);

        var reset = await Controller(context, tenantContext)
            .ResetPassword(target.Id, "anotherSafe123");
        Assert.IsType<ForbidResult>(reset);
    }

    [Fact]
    public async Task Delete_deactivates_only_current_membership_and_never_deletes_global_user()
    {
        var tenantContext = SeedContext();
        await using var context = CreateContext(tenantContext);
        var roles = await SeedCatalogAsync(context);
        var target = User("delete-target", roles.Employee.Id);
        context.SystemUsers.Add(target);
        await context.SaveChangesAsync();
        context.TenantMemberships.AddRange(
            Membership(1, target, roles.Employee.Id, isActive: true),
            Membership(2, target, roles.Manager.Id, isActive: true));
        await context.SaveChangesAsync();
        SelectTenant(tenantContext);

        var result = await Controller(context, tenantContext).DeleteConfirmed(target.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(await context.SystemUsers.FindAsync(target.Id));
        Assert.False((await context.TenantMemberships.SingleAsync(item =>
            item.TenantId == 1 && item.SystemUserId == target.Id)).IsActive);
        Assert.True((await context.TenantMemberships.SingleAsync(item =>
            item.TenantId == 2 && item.SystemUserId == target.Id)).IsActive);
    }

    [Fact]
    public async Task Tenant_Admin_cannot_assign_reserved_platform_role_and_missing_tenant_fails_closed()
    {
        var tenantContext = SeedContext();
        await using var context = CreateContext(tenantContext);
        var roles = await SeedCatalogAsync(context);
        var reserved = new Role { RoleName = "SaaS Admin", IsActive = true };
        var target = User("role-target", roles.Employee.Id);
        context.Roles.Add(reserved);
        context.SystemUsers.Add(target);
        await context.SaveChangesAsync();
        context.TenantMemberships.Add(Membership(1, target, roles.Employee.Id, true));
        await context.SaveChangesAsync();
        SelectTenant(tenantContext);

        var forbidden = await Controller(context, tenantContext)
            .AssignRole(target.Id, reserved.Id);
        Assert.IsType<ForbidResult>(forbidden);
        Assert.Equal(
            roles.Employee.Id,
            (await context.TenantMemberships.SingleAsync()).RoleId);

        var unresolved = new TenantContext();
        unresolved.SetRequest(null, systemUserId: 99);
        Assert.IsType<ForbidResult>(
            await Controller(context, unresolved).Index("", null, ""));
    }

    private static TenantContext SeedContext()
    {
        var context = new TenantContext();
        context.SetDevelopmentCompatibility(systemUserId: 99);
        return context;
    }

    private static void SelectTenant(TenantContext context) =>
        context.SetRequest(tenantId: 1, systemUserId: 99);

    private static MiniERPDbContext CreateContext(ITenantContext tenantContext) =>
        new(new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options, tenantContext);

    private static async Task<(Role Employee, Role Manager)> SeedCatalogAsync(
        MiniERPDbContext context)
    {
        context.Tenants.AddRange(
            new Tenant { Id = 1, Code = "one", Name = "Tenant one" },
            new Tenant { Id = 2, Code = "two", Name = "Tenant two" });
        var employee = new Role { RoleName = "Employee", IsActive = true };
        var manager = new Role { RoleName = "Manager", IsActive = true };
        context.Roles.AddRange(employee, manager);
        await context.SaveChangesAsync();
        return (employee, manager);
    }

    private static SystemUser User(string username, int? roleId, bool isActive = true) =>
        new()
        {
            Username = username,
            Email = $"{username}@example.test",
            PasswordHash = "existing-hash",
            RoleId = roleId,
            IsActive = isActive,
            CreatedAt = DateTime.Now
        };

    private static TenantMembership Membership(
        int tenantId,
        SystemUser user,
        int? roleId,
        bool isActive) =>
        new()
        {
            TenantId = tenantId,
            SystemUser = user,
            RoleId = roleId,
            IsActive = isActive
        };

    private static SystemUsersController Controller(
        MiniERPDbContext context,
        ITenantContext tenantContext)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "99"),
                new Claim("SystemUserId", "99"),
                // This is a tenant role, not the explicit PlatformAdmin claim.
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"))
        };
        return new SystemUsersController(context, tenantContext)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object> _values = new();

        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>(_values);

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
            _values = new Dictionary<string, object>(values);
        }
    }
}
