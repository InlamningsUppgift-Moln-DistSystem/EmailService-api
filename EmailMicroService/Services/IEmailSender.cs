namespace EmailService.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendConfirmationEmailAsync(string to, string confirmationUrl);
}
