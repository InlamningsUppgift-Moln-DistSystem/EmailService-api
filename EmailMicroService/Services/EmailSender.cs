using EmailService.Configuration;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Mail;

namespace EmailService.Services;

public class EmailSender : IEmailSender
{
    private readonly EmailSettings _settings;

    public EmailSender(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        Console.WriteLine($"📧 Trying to send email to {to} with subject '{subject}'");

        var client = new SendGridClient(_settings.SendGridApiKey);
        var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
        var toAddress = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, body, body);

        var response = await client.SendEmailAsync(msg);

        var responseBody = await response.Body.ReadAsStringAsync();
        Console.WriteLine($"📬 SendGrid response: {response.StatusCode} - {responseBody}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"SendGrid error: {response.StatusCode} - {responseBody}");
        }

        Console.WriteLine($"✅ Email successfully sent to {to}");
    }



    public async Task SendConfirmationEmailAsync(string to, string confirmationUrl)
    {
        var body = $"Please confirm your email by clicking this link: {confirmationUrl}";
        await SendEmailAsync(to, "Confirm your email", body);
    }
}
