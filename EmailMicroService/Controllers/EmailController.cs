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
        var apiKey = _config["SendGrid--ApiKey"];
        var from = _config["SendGrid--From"];
        var fromName = _config["SendGrid--FromName"];

        return Ok(new
        {
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? "❌ MISSING" : "✅ LOADED",
            From = string.IsNullOrWhiteSpace(from) ? "❌ MISSING" : from,
            FromName = string.IsNullOrWhiteSpace(fromName) ? "❌ MISSING" : fromName
        });
    }
}
