using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.Tenancy;

public interface ITenantProvisioningService
{
    Task<TenantMembership> EnsureCustomerTenantAsync(
        SystemUser user,
        int? createdBySystemUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Platform-only customer provisioning. It creates a tenant-admin membership
/// without changing the user's global role, so tenant Admin never becomes a
/// platform administrator.
/// </summary>
public sealed class TenantProvisioningService : ITenantProvisioningService
{
    private readonly MiniERPDbContext _context;

    public TenantProvisioningService(MiniERPDbContext context) => _context = context;

    public async Task<TenantMembership> EnsureCustomerTenantAsync(
        SystemUser user,
        int? createdBySystemUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (user.Id <= 0)
        {
            throw new InvalidOperationException("The customer account must be saved before tenant provisioning.");
        }

        var adminRole = await AuthRoleHelper.EnsureAdminRoleAsync(_context);
        var tenantCode = $"tenant-{user.Id}";
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(candidate => candidate.Code == tenantCode, cancellationToken);
        if (tenant == null)
        {
            var displayName = string.IsNullOrWhiteSpace(user.Username)
                ? $"Workspace {user.Id}"
                : user.Username.Trim();
            tenant = new Tenant
            {
                Code = tenantCode,
                Name = displayName.Length <= 100 ? displayName : displayName[..100],
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.Tenants.Add(tenant);
        }
        else
        {
            tenant.IsActive = true;
        }

        var existing = await _context.TenantMemberships
            .FirstOrDefaultAsync(
                membership => membership.SystemUserId == user.Id &&
                              membership.TenantId == tenant.Id,
                cancellationToken);
        if (existing != null)
        {
            existing.RoleId = adminRole.Id;
            existing.IsActive = true;
            return existing;
        }

        var membership = new TenantMembership
        {
            Tenant = tenant,
            SystemUserId = user.Id,
            RoleId = adminRole.Id,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBySystemUserId = createdBySystemUserId
        };
        _context.TenantMemberships.Add(membership);
        return membership;
    }
}
