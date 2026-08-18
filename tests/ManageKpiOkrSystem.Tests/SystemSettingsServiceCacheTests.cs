using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class SystemSettingsServiceCacheTests
{
    [Fact]
    public async Task BrandingCache_IsTenantScopedAndInvalidatedAfterWrite()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await SeedBrandingAsync(options, tenantId: 1, "Tenant One");
        await SeedBrandingAsync(options, tenantId: 2, "Tenant Two");
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var tenantOne = new TenantContext();
        tenantOne.SetBackgroundTenant(1);
        await using var firstContext = new MiniERPDbContext(options, tenantOne);
        var firstService = new SystemSettingsService(firstContext, cache, tenantOne);

        Assert.Equal("Tenant One", (await firstService.GetBrandingAsync()).ProductName);

        var tenantTwo = new TenantContext();
        tenantTwo.SetBackgroundTenant(2);
        await using var secondContext = new MiniERPDbContext(options, tenantTwo);
        var secondService = new SystemSettingsService(secondContext, cache, tenantTwo);

        Assert.Equal("Tenant Two", (await secondService.GetBrandingAsync()).ProductName);

        await firstService.SetValuesAsync(
            new Dictionary<string, string?>
            {
                [SystemSettingCodes.ProductName] = "Tenant One Updated"
            },
            updatedById: null);

        Assert.Equal("Tenant One Updated", (await firstService.GetBrandingAsync()).ProductName);
        Assert.Equal("Tenant Two", (await secondService.GetBrandingAsync()).ProductName);
    }

    private static async Task SeedBrandingAsync(
        DbContextOptions<MiniERPDbContext> options,
        int tenantId,
        string productName)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetBackgroundTenant(tenantId);
        await using var context = new MiniERPDbContext(options, tenantContext);
        context.SystemParameters.Add(new SystemParameter
        {
            ParameterCode = SystemSettingCodes.ProductName,
            Value = productName
        });
        await context.SaveChangesAsync();
    }
}
