namespace EmailService.DTOs;

public class EmailConfirmationRequestDto
{
    public string To { get; set; } = string.Empty;
    public string ConfirmationUrl { get; set; } = string.Empty;
}
