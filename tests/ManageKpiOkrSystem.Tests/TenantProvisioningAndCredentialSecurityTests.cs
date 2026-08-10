using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class TenantProvisioningAndCredentialSecurityTests
{
    [Fact]
    public async Task Provisioning_is_deterministic_idempotent_and_does_not_grant_global_or_other_tenant_admin()
    {
        await using var context = CreateContext();
        var adminRole = new Role
        {
            RoleName = AuthRoleHelper.AdminRoleName,
            IsActive = true
        };
        var employeeRole = new Role
        {
            RoleName = AuthRoleHelper.DefaultSelfServiceRoleName,
            IsActive = true
        };
        var otherTenant = new Tenant
        {
            Code = "existing-customer",
            Name = "Existing customer"
        };
        context.AddRange(adminRole, employeeRole, otherTenant);
        await context.SaveChangesAsync();

        var user = new SystemUser
        {
            Username = " customer-owner ",
            Email = "owner@example.test",
            PasswordHash = "existing-hash",
            RoleId = employeeRole.Id,
            IsActive = true
        };
        context.SystemUsers.Add(user);
        await context.SaveChangesAsync();

        var otherMembership = new TenantMembership
        {
            TenantId = otherTenant.Id,
            SystemUserId = user.Id,
            RoleId = employeeRole.Id,
            IsActive = true
        };
        context.TenantMemberships.Add(otherMembership);
        await context.SaveChangesAsync();

        var service = new TenantProvisioningService(context);
        var first = await service.EnsureCustomerTenantAsync(
            user,
            createdBySystemUserId: 42);
        await context.SaveChangesAsync();
        var firstMembershipId = first.Id;
        var firstTenantId = first.TenantId;

        var second = await service.EnsureCustomerTenantAsync(
            user,
            createdBySystemUserId: 99);
        await context.SaveChangesAsync();

        Assert.Equal(firstMembershipId, second.Id);
        Assert.Equal(firstTenantId, second.TenantId);
        Assert.Equal(employeeRole.Id, user.RoleId);

        var customerTenant = Assert.Single(
            await context.Tenants
                .Where(tenant => tenant.Code == $"tenant-{user.Id}")
                .ToListAsync());
        Assert.Equal("customer-owner", customerTenant.Name);
        Assert.True(customerTenant.IsActive);

        var customerMembership = Assert.Single(
            await context.TenantMemberships
                .Where(membership =>
                    membership.SystemUserId == user.Id &&
                    membership.TenantId == customerTenant.Id)
                .ToListAsync());
        Assert.Equal(adminRole.Id, customerMembership.RoleId);
        Assert.True(customerMembership.IsActive);
        Assert.Equal(42, customerMembership.CreatedBySystemUserId);

        var unchangedOtherMembership = await context.TenantMemberships
            .SingleAsync(membership =>
                membership.SystemUserId == user.Id &&
                membership.TenantId == otherTenant.Id);
        Assert.Equal(employeeRole.Id, unchangedOtherMembership.RoleId);
        Assert.True(unchangedOtherMembership.IsActive);
        Assert.Equal(2, await context.TenantMemberships.CountAsync(
            membership => membership.SystemUserId == user.Id));
    }

    [Fact]
    public async Task InvalidateUnusedTokens_makes_an_existing_reset_link_unusable()
    {
        await using var context = CreateContext();
        var user = ActiveUser("invalidate-token");
        context.SystemUsers.Add(user);
        await context.SaveChangesAsync();
        var service = new PasswordResetService(context);
        var token = await service.CreateTokenAsync(user);
        Assert.True(await service.IsTokenUsableAsync(token));

        await service.InvalidateUnusedTokensAsync(user.Id);

        Assert.False(await service.IsTokenUsableAsync(token));
        Assert.Empty(await context.PasswordResetTokens
            .Where(item => item.SystemUserId == user.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task Successful_reset_consumes_selected_token_and_invalidates_all_other_links()
    {
        await using var context = CreateContext();
        var user = ActiveUser("reset-all-links");
        context.SystemUsers.Add(user);
        await context.SaveChangesAsync();
        var service = new PasswordResetService(context);
        var selectedToken = await service.CreateTokenAsync(user);
        const string otherToken = "another-valid-reset-link";
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            SystemUserId = user.Id,
            TokenHash = HashToken(otherToken),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
        });
        await context.SaveChangesAsync();
        Assert.True(await service.IsTokenUsableAsync(selectedToken));
        Assert.True(await service.IsTokenUsableAsync(otherToken));

        var reset = await service.TryResetPasswordAsync(
            selectedToken,
            "NewSecurePassword123!");

        Assert.True(reset);
        Assert.False(await service.IsTokenUsableAsync(selectedToken));
        Assert.False(await service.IsTokenUsableAsync(otherToken));
        Assert.True(PasswordHelper.VerifyPassword(
            "NewSecurePassword123!",
            user.PasswordHash!));
        Assert.NotNull(user.LastPasswordChange);
        Assert.Single(await context.PasswordResetTokens
            .Where(item => item.SystemUserId == user.Id)
            .ToListAsync());
    }

    [Fact]
    public void Credential_login_posts_use_the_named_login_attempt_rate_limit()
    {
        var authLogin = typeof(AuthController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method =>
                method.Name == nameof(AuthController.Login) &&
                method.ReturnType == typeof(Task<IActionResult>));
        var purchaseLogin = typeof(HomeController)
            .GetMethod(
                nameof(HomeController.LoginAndPurchase),
                BindingFlags.Instance | BindingFlags.Public);

        AssertNamedRateLimit(authLogin);
        Assert.NotNull(purchaseLogin);
        AssertNamedRateLimit(purchaseLogin);
    }

    private static void AssertNamedRateLimit(MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal("LoginAttempts", attribute.PolicyName);
    }

    private static SystemUser ActiveUser(string username) =>
        new()
        {
            Username = username,
            Email = $"{username}@example.test",
            PasswordHash = PasswordHelper.HashPassword("ExistingPassword123!"),
            IsActive = true
        };

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new MiniERPDbContext(options);
    }
}
