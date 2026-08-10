using Manage_KPI_or_OKR_System.Models;

namespace Manage_KPI_or_OKR_System.Services;

public interface IPasswordResetService
{
    Task<string> CreateTokenAsync(SystemUser user, CancellationToken cancellationToken = default);
    Task<bool> IsTokenUsableAsync(string? token, CancellationToken cancellationToken = default);
    Task<bool> TryResetPasswordAsync(string? token, string newPassword, CancellationToken cancellationToken = default);
    Task InvalidateUnusedTokensAsync(int systemUserId, CancellationToken cancellationToken = default);
}
