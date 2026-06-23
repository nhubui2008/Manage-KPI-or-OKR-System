using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Manage_KPI_or_OKR_System.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpServer = _config["SmtpSettings:Server"];
                var smtpPort = _config.GetValue<int>("SmtpSettings:Port", 587);
                var senderName = _config["SmtpSettings:SenderName"];
                var senderEmail = _config["SmtpSettings:SenderEmail"];
                var senderPassword = _config["SmtpSettings:Password"];

                if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
                {
                    _logger.LogWarning("SMTP credentials not configured. Simulating email send.");
                    _logger.LogInformation($"[SIMULATED EMAIL] To: {toEmail}\nSubject: {subject}\nBody: {body}");
                    // Just simulation if no credentials are provided.
                    return;
                }

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email.");
                throw new Exception($"Lỗi SMTP: {ex.Message} - Vui lòng kiểm tra lại Email và App Password!");
            }
        }
    }
}
