namespace EmailService.Configuration;

public class EmailSettings
{
    public string SendGridApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "no-reply@yourdomain.com";
    public string FromName { get; set; } = "Ventixe";
}
