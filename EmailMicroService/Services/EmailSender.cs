using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

namespace EmailService.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailSender(IConfiguration config)
        {
            _apiKey = config["SendGrid--ApiKey"];
            _fromEmail = config["SendGrid--From"];
            _fromName = config["SendGrid--FromName"] ?? "EmailService";

            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_fromEmail))
            {
                throw new Exception("❌ Missing SendGrid configuration values from Key Vault.");
            }
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            Console.WriteLine($"📧 Sending email to {to} with subject: '{subject}'");

            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var toAddress = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, body, body);

            var response = await client.SendEmailAsync(msg);
            var responseBody = await response.Body.ReadAsStringAsync();

            Console.WriteLine($"📬 SendGrid response: {response.StatusCode} - {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"❌ Failed to send email: {response.StatusCode} - {responseBody}");
            }

            Console.WriteLine($"✅ Email successfully sent to {to}");
        }

        public async Task SendConfirmationEmailAsync(string to, string confirmationUrl)
        {
            var body = $"Please confirm your email by clicking this link: {confirmationUrl}";
            await SendEmailAsync(to, "Confirm your email", body);
        }
    }
}
