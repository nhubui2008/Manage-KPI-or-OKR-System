using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class AuthRoleHelperTests
{
    [Fact]
    public async Task EnsureDefaultSelfServiceRoleAsync_CreatesEmployeeRoleWithDashboardAccess()
    {
        await using var context = CreateContext();

        var role = await AuthRoleHelper.EnsureDefaultSelfServiceRoleAsync(context);

        Assert.Equal(AuthRoleHelper.DefaultSelfServiceRoleName, role.RoleName);
        Assert.True(await HasDashboardPermissionAsync(context, role.Id));
    }

    [Fact]
    public async Task EnsureUserHasLoginRoleAsync_AssignsDefaultRoleWhenRoleIsMissing()
    {
        await using var context = CreateContext();
        var user = new SystemUser
        {
            Username = "new.gmail",
            Email = "new.gmail@example.com",
            IsActive = true
        };
        context.SystemUsers.Add(user);
        await context.SaveChangesAsync();

        var role = await AuthRoleHelper.EnsureUserHasLoginRoleAsync(context, user);

        Assert.Equal(AuthRoleHelper.DefaultSelfServiceRoleName, role.RoleName);
        Assert.Equal(role.Id, user.RoleId);
        Assert.True(await HasDashboardPermissionAsync(context, role.Id));
    }

    [Fact]
    public async Task EnsureUserHasLoginRoleAsync_RepairsLegacyUserRoleWithoutDashboardPermission()
    {
        await using var context = CreateContext();
        var legacyRole = new Role { RoleName = "User", IsActive = true };
        context.Roles.Add(legacyRole);
        await context.SaveChangesAsync();

        var user = new SystemUser
        {
            Username = "legacy.gmail",
            Email = "legacy.gmail@example.com",
            RoleId = legacyRole.Id,
            IsActive = true
        };
        context.SystemUsers.Add(user);
        await context.SaveChangesAsync();

        var role = await AuthRoleHelper.EnsureUserHasLoginRoleAsync(context, user);

        Assert.Equal(AuthRoleHelper.DefaultSelfServiceRoleName, role.RoleName);
        Assert.Equal(role.Id, user.RoleId);
        Assert.True(await HasDashboardPermissionAsync(context, role.Id));
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MiniERPDbContext(options);
    }

    private static Task<bool> HasDashboardPermissionAsync(MiniERPDbContext context, int roleId)
    {
        return context.Role_Permissions
            .Join(context.Permissions,
                rp => rp.PermissionId,
                p => p.Id,
                (rp, p) => new { rp, p })
            .AnyAsync(x =>
                x.rp.RoleId == roleId &&
                x.p.PermissionCode == AuthRoleHelper.DashboardPermissionCode);
    }
}
