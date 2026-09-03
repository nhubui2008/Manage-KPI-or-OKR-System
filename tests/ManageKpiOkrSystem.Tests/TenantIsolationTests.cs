using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task Tenant_filter_hides_another_tenants_rows_and_stamps_new_rows()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var setup = CreateContext(databaseName, new TenantContext());
        setup.Tenants.AddRange(
            new Tenant { Id = 1, Name = "Tenant one", Code = "one" },
            new Tenant { Id = 2, Name = "Tenant two", Code = "two" });
        await setup.SaveChangesAsync();

        var tenantOne = new TenantContext();
        tenantOne.SetRequest(1, systemUserId: 1);
        await using (var firstTenant = CreateContext(databaseName, tenantOne))
        {
            var department = new Department { DepartmentCode = "ENG", DepartmentName = "Engineering" };
            firstTenant.Departments.Add(department);
            await firstTenant.SaveChangesAsync();
            Assert.Equal(1, firstTenant.Entry(department).Property<int>("TenantId").CurrentValue);
        }

        var tenantTwo = new TenantContext();
        tenantTwo.SetRequest(2, systemUserId: 2);
        await using var secondTenant = CreateContext(databaseName, tenantTwo);
        Assert.Empty(await secondTenant.Departments.ToListAsync());
    }

    [Fact]
    public async Task Unresolved_production_request_fails_closed_for_queries_and_writes()
    {
        var context = new TenantContext();
        context.SetRequest(tenantId: null, systemUserId: 1);
        await using var db = CreateContext(Guid.NewGuid().ToString(), context);

        Assert.Empty(await db.Departments.ToListAsync());
        db.Departments.Add(new Department { DepartmentCode = "NOPE", DepartmentName = "Blocked" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Background_tenant_uses_the_same_filter_and_write_stamping()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var tenantOne = new TenantContext();
        tenantOne.SetBackgroundTenant(1, 100);

        await using (var context = CreateContext(databaseName, tenantOne))
        {
            context.Tenants.AddRange(
                new Tenant { Id = 1, Code = "tenant-one", Name = "Tenant one" },
                new Tenant { Id = 2, Code = "tenant-two", Name = "Tenant two" });
            context.Departments.Add(new Department
            {
                DepartmentCode = "BG-ONE",
                DepartmentName = "Background one"
            });
            await context.SaveChangesAsync();
        }

        var tenantTwo = new TenantContext();
        tenantTwo.SetBackgroundTenant(2, 200);
        await using (var context = CreateContext(databaseName, tenantTwo))
        {
            context.Departments.Add(new Department
            {
                DepartmentCode = "BG-TWO",
                DepartmentName = "Background two"
            });
            await context.SaveChangesAsync();

            var visible = await context.Departments
                .Select(department => department.DepartmentCode)
                .ToListAsync();
            Assert.Equal(new[] { "BG-TWO" }, visible);
        }
    }

    [Fact]
    public async Task Tenant_write_rejects_a_reference_to_another_tenants_department()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var setup = CreateContext(databaseName, new TenantContext()))
        {
            setup.Tenants.AddRange(
                new Tenant { Id = 1, Name = "Tenant one", Code = "one" },
                new Tenant { Id = 2, Name = "Tenant two", Code = "two" });
            await setup.SaveChangesAsync();
        }

        var tenantTwo = new TenantContext();
        tenantTwo.SetRequest(2, systemUserId: 2);
        int foreignDepartmentId;
        await using (var secondTenant = CreateContext(databaseName, tenantTwo))
        {
            var department = new Department { DepartmentCode = "FIN", DepartmentName = "Finance" };
            secondTenant.Departments.Add(department);
            await secondTenant.SaveChangesAsync();
            foreignDepartmentId = department.Id;
        }

        var tenantOne = new TenantContext();
        tenantOne.SetRequest(1, systemUserId: 1);
        await using var firstTenant = CreateContext(databaseName, tenantOne);
        firstTenant.EmployeeAssignments.Add(new EmployeeAssignment { DepartmentId = foreignDepartmentId });

        await Assert.ThrowsAsync<InvalidOperationException>(() => firstTenant.SaveChangesAsync());
    }

    [Fact]
    public async Task Entity_updated_via_db_update_without_changing_tenant_succeeds_in_production_request()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var setup = CreateContext(databaseName, new TenantContext()))
        {
            setup.Tenants.Add(new Tenant { Id = 1, Name = "Tenant one", Code = "one" });
            await setup.SaveChangesAsync();
        }

        var tenantOne = new TenantContext();
        tenantOne.SetRequest(1, systemUserId: 1);
        await using var db = CreateContext(databaseName, tenantOne);

        var department = new Department { DepartmentCode = "DEV", DepartmentName = "Development" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        department.DepartmentName = "Software Development";
        db.Update(department);

        var exception = await Record.ExceptionAsync(() => db.SaveChangesAsync());
        Assert.Null(exception);

        var updated = await db.Departments.SingleAsync(d => d.DepartmentCode == "DEV");
        Assert.Equal("Software Development", updated.DepartmentName);
    }

    [Fact]
    public async Task Entity_updated_with_different_tenant_throws_cross_tenant_exception()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var setup = CreateContext(databaseName, new TenantContext()))
        {
            setup.Tenants.AddRange(
                new Tenant { Id = 1, Name = "Tenant one", Code = "one" },
                new Tenant { Id = 2, Name = "Tenant two", Code = "two" });
            await setup.SaveChangesAsync();
        }

        var tenantOne = new TenantContext();
        tenantOne.SetRequest(1, systemUserId: 1);
        await using var db = CreateContext(databaseName, tenantOne);

        var department = new Department { DepartmentCode = "HR", DepartmentName = "Human Resources" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        // Mutating TenantId away from current tenant throws
        db.Entry(department).Property<int>("TenantId").CurrentValue = 2;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("cannot be modified or deleted from another tenant", exception.Message);
    }

    [Fact]
    public async Task Entity_updated_from_foreign_tenant_into_current_tenant_throws_immutable_exception()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var setup = CreateContext(databaseName, new TenantContext()))
        {
            setup.Tenants.AddRange(
                new Tenant { Id = 1, Name = "Tenant one", Code = "one" },
                new Tenant { Id = 2, Name = "Tenant two", Code = "two" });
            await setup.SaveChangesAsync();
        }

        var tenantOne = new TenantContext();
        tenantOne.SetRequest(1, systemUserId: 1);
        await using var db = CreateContext(databaseName, tenantOne);

        var department = new Department { DepartmentCode = "OPS", DepartmentName = "Operations" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        // Simulate an entity originally belonging to tenant 2 being repointed to tenant 1
        var entry = db.Entry(department);
        entry.Property<int>("TenantId").OriginalValue = 2;
        entry.Property<int>("TenantId").CurrentValue = 1;
        entry.Property<int>("TenantId").IsModified = true;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("TenantId is immutable after an entity is created", exception.Message);
    }

    private static MiniERPDbContext CreateContext(string databaseName, ITenantContext tenantContext) =>
        new(new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options, tenantContext);
}
