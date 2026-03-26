using System.Net;
using System.Net.Mail;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.Extensions.Configuration;

namespace Manage_KPI_or_OKR_System.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly EncryptionHelper _encryptionHelper;

        // ĐÃ SỬA: Thêm EncryptionHelper encryptionHelper vào tham số ở đây
        public EmailService(IConfiguration config, EncryptionHelper encryptionHelper)
        {
            _config = config;
            _encryptionHelper = encryptionHelper;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var smtpServer = _config["SmtpSettings:Server"];
            var smtpPort = int.Parse(_config["SmtpSettings:Port"] ?? "587");
            var senderEmail = _config["SmtpSettings:SenderEmail"];
            var senderName = _config["SmtpSettings:SenderName"];

            // Lấy mật khẩu đã mã hóa từ cấu hình và giải mã nó
            var encryptedPassword = _config["SmtpSettings:Password"];
            var decryptedPassword = _encryptionHelper.Decrypt(encryptedPassword!);

            using (var client = new SmtpClient(smtpServer, smtpPort))
            {
                client.UseDefaultCredentials = false;
                // ĐÃ SỬA: Phải dùng decryptedPassword ở đây mới đúng mật khẩu thực của Gmail
                client.Credentials = new NetworkCredential(senderEmail, decryptedPassword);
                client.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail!, senderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}