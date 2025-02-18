namespace UseCases.Services.EmailService;

public interface IEmailService
{
    Task SendEmailAsync(string receiver, string subject, string htmlBody);
}
