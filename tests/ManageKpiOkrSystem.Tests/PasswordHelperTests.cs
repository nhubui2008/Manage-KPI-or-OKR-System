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
}
