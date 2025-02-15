using Domain.Shared;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using UseCases.Services.EmailService;

namespace Infrastructure.EmailService;

public class EmailService(MailSettings mailSettings) : IEmailService
{
    public async Task SendEmailAsync(string receiver, string subject, string htmlBody)
    {
        // Create a new email message
        var email = new MimeMessage
        {
            Sender = new MailboxAddress(mailSettings.DisplayName, mailSettings.Mail),
            From = { new MailboxAddress(mailSettings.DisplayName, mailSettings.Mail) },
            To = { MailboxAddress.Parse(receiver) },
            Subject = subject,
            Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody()
        };

        using var smtp = new SmtpClient();

        try
        {
            await smtp.ConnectAsync(
                mailSettings.Host,
                mailSettings.Port,
                SecureSocketOptions.StartTls
            );
            await smtp.AuthenticateAsync(mailSettings.Mail, mailSettings.Password);
            await smtp.SendAsync(email);
        }
        finally
        {
            await smtp.DisconnectAsync(true);
        }
    }
}
