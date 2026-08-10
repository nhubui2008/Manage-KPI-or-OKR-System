using Manage_KPI_or_OKR_System.Services;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class PasswordResetRateLimiterTests
{
    [Fact]
    public void TryAcquire_LimitsAnEmailToThreeRequestsPerWindow()
    {
        var limiter = new PasswordResetRateLimiter();

        Assert.True(limiter.TryAcquire("127.0.0.1", "user@example.com"));
        Assert.True(limiter.TryAcquire("127.0.0.1", "user@example.com"));
        Assert.True(limiter.TryAcquire("127.0.0.1", "user@example.com"));
        Assert.False(limiter.TryAcquire("127.0.0.1", "user@example.com"));
    }

    [Fact]
    public void TryAcquire_LimitsAnAddressAcrossDifferentEmails()
    {
        var limiter = new PasswordResetRateLimiter();

        for (var index = 0; index < 10; index++)
        {
            Assert.True(limiter.TryAcquire("127.0.0.1", $"user{index}@example.com"));
        }

        Assert.False(limiter.TryAcquire("127.0.0.1", "another@example.com"));
    }
}
