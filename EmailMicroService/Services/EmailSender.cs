using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EmailService.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _logPath;

        public EmailSender(IConfiguration config)
        {
            _apiKey = config["SendGrid--ApiKey"];
            _fromEmail = config["SendGrid--From"];
            _fromName = config["SendGrid--FromName"] ?? "EmailService";

            var baseDir = AppContext.BaseDirectory;
            _logPath = Path.Combine(baseDir, "logs", "email-debug.txt");

            Log($"Constructor loaded. ApiKey: {Mask(_apiKey)}, From: {_fromEmail}, FromName: {_fromName}");

            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_fromEmail))
            {
                Log("❌ Missing SendGrid configuration values from Key Vault.");
                throw new Exception("❌ Missing SendGrid configuration values from Key Vault.");
            }
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            Log($"📧 Sending email to {to} with subject: '{subject}'");

            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var toAddress = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, body, body);

            var response = await client.SendEmailAsync(msg);
            var responseBody = await response.Body.ReadAsStringAsync();

            Log($"📬 SendGrid response: {response.StatusCode} - {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                Log($"❌ Failed to send email: {response.StatusCode}");
                throw new Exception($"❌ Failed to send email: {response.StatusCode} - {responseBody}");
            }

            Log($"✅ Email successfully sent to {to}");
        }

        public async Task SendConfirmationEmailAsync(string to, string confirmationUrl)
        {
            Log($"↪️ Preparing confirmation email to {to}");
            var body = $"Please confirm your email by clicking this link: {confirmationUrl}";
            await SendEmailAsync(to, "Confirm your email", body);
        }

        private void Log(string message)
        {
            try
            {
                var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                var dir = Path.GetDirectoryName(_logPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(_logPath, entry);
            }
            catch
            {
                // Silent fail
            }
        }

        private string Mask(string value)
        {
            if (string.IsNullOrEmpty(value)) return "null";
            return value.Length <= 4 ? "****" : new string('*', value.Length - 4) + value[^4..];
        }
    }
}
