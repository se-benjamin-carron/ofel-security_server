using System.Net;
using System.Net.Mail;

namespace Ofel.Security.Server.Services;

public class EmailAlertService
{
    private readonly SecurityConfig _config;

    public EmailAlertService(SecurityConfig config) => _config = config;

    public void Send(string subject, string body)
    {
        if (string.IsNullOrEmpty(_config.AlertEmail) || string.IsNullOrEmpty(_config.SmtpPassword))
        {
            Console.WriteLine("[EmailAlert] Skipped — OFEL_ALERT_EMAIL or OFEL_SMTP_PASSWORD not configured.");
            return;
        }

        try
        {
#pragma warning disable SYSLIB0021 // SmtpClient is functional on .NET 8
            using var client = new SmtpClient(_config.SmtpHost, _config.SmtpPort)
            {
                EnableSsl   = true,
                Credentials = new NetworkCredential(_config.SmtpUser, _config.SmtpPassword),
            };
#pragma warning restore SYSLIB0021
            var msg = new MailMessage(_config.SmtpUser, _config.AlertEmail, subject, body);
            client.Send(msg);
            Console.WriteLine($"[EmailAlert] Sent: {subject}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailAlert] Failed to send — {ex.Message}");
        }
    }
}
