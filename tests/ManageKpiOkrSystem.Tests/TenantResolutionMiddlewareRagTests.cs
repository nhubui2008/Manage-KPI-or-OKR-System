using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class TenantResolutionMiddlewareRagTests
{
    [Fact]
    public async Task InvokeAsync_ReplacesRoleAndDepartmentClaimsFromSelectedTenant()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var seedTenant = new TenantContext();
        seedTenant.SetRequest(1, 99);
        await using (var seed = Context(databaseName, seedTenant))
        {
            seed.AddRange(
                new Tenant { Id = 1, Code = "tenant-one", Name = "Tenant one" },
                new Role { Id = 10, RoleName = "Admin", IsActive = true },
                new SystemUser { Id = 99, Username = "owner", Email = "owner@example.test", IsActive = true },
                new Department { Id = 7, DepartmentCode = "OPS", DepartmentName = "Operations", IsActive = true },
                new Employee
                {
                    Id = 50,
                    EmployeeCode = "E-50",
                    FullName = "Owner",
                    Email = "owner@example.com",
                    Phone = "0900000000",
                    SystemUserId = 99,
                    IsActive = true
                });
            await seed.SaveChangesAsync();
            seed.AddRange(
                new TenantMembership
                {
                    Id = 1,
                    TenantId = 1,
                    SystemUserId = 99,
                    RoleId = 10,
                    IsActive = true
                },
                new EmployeeAssignment
                {
                    Id = 60,
                    EmployeeId = 50,
                    DepartmentId = 7,
                    IsActive = true
                });
            await seed.SaveChangesAsync();
        }

        var requestTenant = new TenantContext();
        await using var context = Context(databaseName, requestTenant);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("SystemUserId", "99"),
            new Claim("TenantId", "1"),
            new Claim(ClaimTypes.Role, "stale-role"),
            new Claim(KnowledgeDocumentAccessPolicy.DepartmentClaimType, "999")
        }, "Test"));
        var httpContext = new DefaultHttpContext { User = principal };
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<TenantResolutionMiddleware>.Instance,
            new TestEnvironment());

        await middleware.InvokeAsync(httpContext, requestTenant, context);

        Assert.True(nextCalled);
        Assert.Equal(1, requestTenant.TenantId);
        Assert.Equal(99, requestTenant.SystemUserId);
        Assert.Equal(new[] { "Admin" }, principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value));
        Assert.Equal(
            new[] { "7" },
            principal.FindAll(KnowledgeDocumentAccessPolicy.DepartmentClaimType).Select(claim => claim.Value));
        var filter = new EvidenceSecurityFilterBuilder().Build(principal);
        Assert.Contains("department:7", filter, StringComparison.Ordinal);
        Assert.DoesNotContain("department:999", filter, StringComparison.Ordinal);
    }

    private static MiniERPDbContext Context(string databaseName, ITenantContext tenantContext) =>
        new(new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options, tenantContext);

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Production";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
