using MailKit.Net.Smtp;
using MimeKit;
using Manage_KPI_or_OKR_System.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    public EmailService(IConfiguration config) => _config = config;

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var emailSettings = _config.GetSection("EmailSettings");
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));
        mimeMessage.To.Add(MailboxAddress.Parse(email));
        mimeMessage.Subject = subject;
        mimeMessage.Body = new TextPart("html") { Text = message };

        using var client = new SmtpClient();
        await client.ConnectAsync(emailSettings["MailServer"], int.Parse(emailSettings["MailPort"]!), MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(emailSettings["SenderEmail"], emailSettings["Password"]);
        await client.SendAsync(mimeMessage);
        await client.DisconnectAsync(true);
    }
}