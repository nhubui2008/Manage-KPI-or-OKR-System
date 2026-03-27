using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace Manage_KPI_or_OKR_System.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        // ĐÃ SỬA: Xóa EncryptionHelper khỏi constructor
        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var smtpServer = _config["SmtpSettings:Server"];
            var smtpPort = int.Parse(_config["SmtpSettings:Port"] ?? "587");
            var senderEmail = _config["SmtpSettings:SenderEmail"];
            var senderName = _config["SmtpSettings:SenderName"];

            // ĐÃ SỬA: Lấy trực tiếp mật khẩu từ file .env (không cần giải mã)
            var password = _config["SmtpSettings:Password"];

            using (var client = new SmtpClient(smtpServer, smtpPort))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(senderEmail, password);
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