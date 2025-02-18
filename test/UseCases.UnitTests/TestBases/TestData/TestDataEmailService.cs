using UseCases.Services.EmailService;

namespace UseCases.UnitTests.TestBases.TestData;

public class TestDataEmailService : IEmailService
{
    public record EmailData(string Receiver, string Subject, string HtmlBody);

    public List<EmailData> SentEmails { get; } = [];

    public Task SendEmailAsync(string receiver, string subject, string htmlBody)
    {
        SentEmails.Add(new EmailData(receiver, subject, htmlBody));
        return Task.CompletedTask;
    }
}
