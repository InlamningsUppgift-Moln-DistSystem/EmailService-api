using EmailService.DTOs;
using EmailService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmailService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailSender _emailSender;

    public EmailController(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    [HttpPost("send-confirm-email-update")]
    public async Task<IActionResult> SendConfirmationEmailUpdate([FromBody] EmailConfirmationRequestDto request)
    {
        await _emailSender.SendConfirmationEmailAsync(request.To, request.ConfirmationUrl);
        return Ok("Confirmation email sent.");
    }

    [HttpPost("send-generic")]
    public async Task<IActionResult> SendGeneric([FromBody] EmailRequestDto request)
    {
        await _emailSender.SendEmailAsync(request.To, request.Subject, request.Body);
        return Ok("Email sent.");
    }
}
