using System.Security.Cryptography;
using System.Text;

namespace Manage_KPI_or_OKR_System.Helpers
{
    public static class PasswordHelper
    {
        private const string Pbkdf2Prefix = "pbkdf2-sha256";
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int IterationCount = 210_000;

        public static string HashPassword(string password)
        {
            ArgumentNullException.ThrowIfNull(password);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                IterationCount,
                HashAlgorithmName.SHA256,
                HashSize);

            return string.Join(
                '$',
                Pbkdf2Prefix,
                IterationCount,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
        }

        public static bool VerifyPassword(string inputPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            return storedHash.StartsWith(Pbkdf2Prefix + "$", StringComparison.Ordinal)
                ? VerifyPbkdf2(inputPassword, storedHash)
                : VerifyLegacySha256(inputPassword, storedHash);
        }

        public static bool NeedsRehash(string? storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return true;
            }

            var parts = storedHash.Split('$');
            return parts.Length != 4 ||
                   !string.Equals(parts[0], Pbkdf2Prefix, StringComparison.Ordinal) ||
                   !int.TryParse(parts[1], out var iterations) ||
                   iterations < IterationCount;
        }

        private static bool VerifyPbkdf2(string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 4 ||
                    !string.Equals(parts[0], Pbkdf2Prefix, StringComparison.Ordinal) ||
                    !int.TryParse(parts[1], out var iterations) ||
                    iterations <= 0 ||
                    iterations > 1_000_000)
                {
                    return false;
                }

                var salt = Convert.FromBase64String(parts[2]);
                var expectedHash = Convert.FromBase64String(parts[3]);
                if (salt.Length < SaltSize || expectedHash.Length != HashSize)
                {
                    return false;
                }

                var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool VerifyLegacySha256(string password, string storedHash)
        {
            if (storedHash.Length != 64)
            {
                return false;
            }

            try
            {
                var expectedHash = Convert.FromHexString(storedHash);
                var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
