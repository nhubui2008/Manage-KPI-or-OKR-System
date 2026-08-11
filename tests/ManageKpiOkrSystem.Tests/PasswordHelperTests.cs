using System.Security.Cryptography;
using System.Text;
using Manage_KPI_or_OKR_System.Helpers;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public class PasswordHelperTests
{
    [Fact]
    public void HashPassword_UsesSaltedPbkdf2AndVerifies()
    {
        const string password = "MatKhau@123";

        var firstHash = PasswordHelper.HashPassword(password);
        var secondHash = PasswordHelper.HashPassword(password);

        Assert.StartsWith("pbkdf2-sha256$", firstHash);
        Assert.NotEqual(firstHash, secondHash);
        Assert.True(PasswordHelper.VerifyPassword(password, firstHash));
        Assert.False(PasswordHelper.VerifyPassword("sai-mat-khau", firstHash));
        Assert.False(PasswordHelper.NeedsRehash(firstHash));
    }

    [Fact]
    public void VerifyPassword_AcceptsLegacySha256ForGradualMigration()
    {
        const string password = "legacy-password";
        var legacyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();

        Assert.True(PasswordHelper.VerifyPassword(password, legacyHash));
        Assert.False(PasswordHelper.VerifyPassword("wrong", legacyHash));
        Assert.True(PasswordHelper.NeedsRehash(legacyHash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-password-hash")]
    [InlineData("pbkdf2-sha256$invalid$salt$hash")]
    public void VerifyPassword_RejectsMalformedHashes(string storedHash)
    {
        Assert.False(PasswordHelper.VerifyPassword("password", storedHash));
    }

    [Theory]
    [InlineData("pbkdf2-sha256$210000$zE60s5hAbuSkd1BaQndwnw==$3u17Jox7oiCGmjuc5u9eV1t6NSbWMNZMUhUOEqNpzSs=")]
    [InlineData("pbkdf2-sha256$210000$jzL0cxJlETqIFIVKYcz7pw==$S27cd1HjOe67Ogfr1D0b/8r1Ln1CgScb8T1/wlJBueY=")]
    [InlineData("pbkdf2-sha256$210000$7lwCYCpB264/LXg4jsP4tA==$ghSlwkj5D17ucboF0i2uZd5rUQ0cv++EsoZhhDasC2Y=")]
    [InlineData("pbkdf2-sha256$210000$09VM2tGwt+hjOhz4HZ5y9Q==$KKzKHWd9IIbePGzF7dnw1K23qyHj8rAKC1arCvRl4Hg=")]
    [InlineData("pbkdf2-sha256$210000$EhBrPi0OrdEsfRWx4St72g==$NCXse870hDBagvpzDKJE9qKzYlYTZCNGJ1461i8qyno=")]
    [InlineData("pbkdf2-sha256$210000$HKILUzt3ytUBw84UaHZuhg==$IVIGD/CPlCPt+1UNt2W7w13b16bdwMdWOxTuTueB/Yc=")]
    [InlineData("pbkdf2-sha256$210000$GX+jT5vvY0wtNPgcEOSK6Q==$KCwuxCG0qIo623/MZh8Q9fHPLijy1DJotc7hdRMJ8OU=")]
    public void VerifyPassword_AcceptsProjectTeamSeedHashes(string storedHash)
    {
        Assert.True(PasswordHelper.VerifyPassword("NextGen@2026", storedHash));
        Assert.False(PasswordHelper.VerifyPassword("wrong-password", storedHash));
    }
}
