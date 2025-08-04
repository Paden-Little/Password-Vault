using System.Net;
using System.Net.Mail;

public class EmailService
{
    private readonly string smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");

    private readonly int smtpPort = 587;
    private readonly string smtpUser = Environment.GetEnvironmentVariable("SMTP_USERNAME");
    private readonly string smtpPass = Environment.GetEnvironmentVariable("SMTP_PASSWORD");

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

        using var message = new MailMessage(smtpUser, toEmail, subject, body);
        message.IsBodyHtml = true;

        await client.SendMailAsync(message);

        Console.WriteLine($"SMTP HOST: {smtpHost}");
        Console.WriteLine($"SMTP USER: {smtpUser}");
        Console.WriteLine($"SMTP PASS: {smtpPass}");

    }
}
