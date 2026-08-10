namespace Manage_KPI_or_OKR_System.Services;

public sealed class PasswordResetRateLimiter : IPasswordResetRateLimiter
{
    private const int MaximumTrackedKeys = 2_048;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private readonly object _gate = new();
    private readonly Dictionary<string, List<DateTime>> _attempts = new(StringComparer.Ordinal);

    public bool TryAcquire(string? remoteAddress, string? normalizedEmail)
    {
        var now = DateTime.UtcNow;
        var address = string.IsNullOrWhiteSpace(remoteAddress) ? "unknown" : remoteAddress;
        var email = string.IsNullOrWhiteSpace(normalizedEmail) ? "empty" : normalizedEmail;
        var addressKey = $"ip:{address}";
        var emailKey = $"email:{email}";

        lock (_gate)
        {
            RemoveExpiredEntries(now);
            var addressAttempts = GetAttempts(addressKey);
            var emailAttempts = GetAttempts(emailKey);

            if (addressAttempts.Count >= 10 || emailAttempts.Count >= 3)
            {
                return false;
            }

            if (_attempts.Count >= MaximumTrackedKeys &&
                !_attempts.ContainsKey(addressKey) &&
                !_attempts.ContainsKey(emailKey))
            {
                return false;
            }

            addressAttempts.Add(now);
            emailAttempts.Add(now);
            return true;
        }
    }

    private List<DateTime> GetAttempts(string key)
    {
        if (!_attempts.TryGetValue(key, out var attempts))
        {
            attempts = new List<DateTime>();
            _attempts[key] = attempts;
        }

        return attempts;
    }

    private void RemoveExpiredEntries(DateTime now)
    {
        var expiresBefore = now - Window;
        foreach (var key in _attempts.Keys.ToArray())
        {
            var attempts = _attempts[key];
            attempts.RemoveAll(attempt => attempt <= expiresBefore);
            if (attempts.Count == 0)
            {
                _attempts.Remove(key);
            }
        }
    }
}
