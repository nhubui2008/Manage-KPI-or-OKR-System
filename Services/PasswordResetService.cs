using System.Security.Cryptography;
using System.Text;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services;

public sealed class PasswordResetService : IPasswordResetService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);
    private readonly MiniERPDbContext _context;

    public PasswordResetService(MiniERPDbContext context)
    {
        _context = context;
    }

    public async Task<string> CreateTokenAsync(SystemUser user, CancellationToken cancellationToken = default)
    {
        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var now = DateTime.UtcNow;
        var tokens = _context.Set<PasswordResetToken>();

        // A new request invalidates any previous unused link for this account.
        var previousTokens = await tokens
            .Where(token => token.SystemUserId == user.Id && token.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
        tokens.RemoveRange(previousTokens);

        tokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            SystemUserId = user.Id,
            TokenHash = HashToken(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(TokenLifetime)
        });

        await _context.SaveChangesAsync(cancellationToken);
        return rawToken;
    }

    public async Task<bool> IsTokenUsableAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 128)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        return await _context.Set<PasswordResetToken>().AnyAsync(
            resetToken => resetToken.TokenHash == HashToken(token)
                && resetToken.UsedAtUtc == null
                && resetToken.ExpiresAtUtc > now,
            cancellationToken);
    }

    public async Task<bool> TryResetPasswordAsync(string? token, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 128)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var resetToken = await _context.Set<PasswordResetToken>()
            .Include(item => item.SystemUser)
            .SingleOrDefaultAsync(
                item => item.TokenHash == HashToken(token)
                    && item.UsedAtUtc == null
                    && item.ExpiresAtUtc > now,
                cancellationToken);

        if (resetToken?.SystemUser == null || resetToken.SystemUser.IsActive != true)
        {
            return false;
        }

        resetToken.UsedAtUtc = now;
        resetToken.SystemUser.PasswordHash = PasswordHelper.HashPassword(newPassword);
        resetToken.SystemUser.LastPasswordChange = now;
        var otherUnusedTokens = await _context.Set<PasswordResetToken>()
            .Where(item =>
                item.SystemUserId == resetToken.SystemUserId &&
                item.Id != resetToken.Id &&
                item.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
        _context.Set<PasswordResetToken>().RemoveRange(otherUnusedTokens);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // The UsedAtUtc concurrency token makes a link single-use under concurrent requests.
            return false;
        }
    }

    public async Task InvalidateUnusedTokensAsync(
        int systemUserId,
        CancellationToken cancellationToken = default)
    {
        if (systemUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(systemUserId));
        }

        var tokens = _context.Set<PasswordResetToken>();
        var unusedTokens = await tokens
            .Where(item => item.SystemUserId == systemUserId && item.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
        if (unusedTokens.Count == 0)
        {
            return;
        }

        tokens.RemoveRange(unusedTokens);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
