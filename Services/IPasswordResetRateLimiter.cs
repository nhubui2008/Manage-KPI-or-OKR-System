namespace Manage_KPI_or_OKR_System.Services;

public interface IPasswordResetRateLimiter
{
    bool TryAcquire(string? remoteAddress, string? normalizedEmail);
}
