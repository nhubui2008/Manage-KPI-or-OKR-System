using Microsoft.AspNetCore.DataProtection;

namespace Manage_KPI_or_OKR_System.Helpers
{
    public class EncryptionHelper
    {
        private readonly IDataProtector _protector;

        public EncryptionHelper(IDataProtectionProvider provider)
        {
            // "SmtpPasswordPurpose" là một key định danh duy nhất cho việc mã hóa này
            _protector = provider.CreateProtector("SmtpPasswordPurpose");
        }

        public string Encrypt(string plainText) => _protector.Protect(plainText);

        public string Decrypt(string cipherText) => _protector.Unprotect(cipherText);
    }
}