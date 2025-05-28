using EmailMicroService.DTOs;
using EmailService.DTOs;
using EmailService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmailService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;

    public EmailController(IEmailSender emailSender, IConfiguration config)
    {
        _emailSender = emailSender;
        _config = config;
    }

    [HttpPost("send-confirm-email-update")]
    public async Task<IActionResult> SendConfirmationEmailUpdate([FromBody] EmailConfirmationRequestDto request)
    {
        try
        {
            Console.WriteLine($"📩 Incoming email confirmation request for: {request.To}");

            await _emailSender.SendConfirmationEmailAsync(request.To, request.ConfirmationUrl);

            Console.WriteLine("✅ Email sent without exception");
            return Ok("Email sent.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception while sending email: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("send-generic")]
    public async Task<IActionResult> SendGeneric([FromBody] EmailRequestDto request)
    {
        try
        {
            await _emailSender.SendEmailAsync(request.To, request.Subject, request.Body);
            return Ok("Email sent.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("debug-sendgrid")]
    public IActionResult DebugSendGrid()
    {
        string apiKey = _config["SendGrid--ApiKey"];
        string fromEmail = _config["SendGrid--From"];
        string fromName = _config["SendGrid--FromName"];

        return Ok(new
        {
            ApiKeyStatus = string.IsNullOrEmpty(apiKey) ? "❌ Missing" : $"✅ Loaded ({apiKey.Length} chars)",
            FromEmail = string.IsNullOrEmpty(fromEmail) ? "❌ Missing" : fromEmail,
            FromName = string.IsNullOrEmpty(fromName) ? "⚠️ Empty (defaulting to 'EmailService')" : fromName
        });
    }

    //For the contact form
    [HttpPost("send-contact-message")]
    public async Task<IActionResult> SendContactMessage([FromBody] ContactMessageDto message)
    {
        if (string.IsNullOrWhiteSpace(message.Email) ||
            string.IsNullOrWhiteSpace(message.Name) ||
            string.IsNullOrWhiteSpace(message.Message))
        {
            return BadRequest("Name, email, and message are all required.");
        }

        try
        {
            var formattedBody = $"""
        <p><strong>From:</strong> {message.Email}</p>
        <p><strong>Name:</strong> {message.Name}</p>
        <p><strong>Message:</strong></p>
        <p>{message.Message}</p>
        """;

            await _emailSender.SendEmailAsync(
                to: "kevin.swardh@utb.ecutbildning.se",
                subject: "New Contact Form Message",
                body: formattedBody
            );

            return Ok("Message sent.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to send contact message: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }



}
