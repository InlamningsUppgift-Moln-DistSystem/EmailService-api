using Azure.Messaging.ServiceBus.Administration;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _config;

    public HealthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet("servicebus")]
    public IActionResult CheckServiceBusConnection()
    {
        var connectionString = _config["ServiceBus:ConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
            return BadRequest("ServiceBus connection string is missing.");

        try
        {
            var adminClient = new ServiceBusAdministrationClient(connectionString);
            bool queueExists = adminClient.QueueExistsAsync("email-queue").GetAwaiter().GetResult();

            if (queueExists)
                return Ok("Service Bus queue 'email-queue' reachable.");
            else
                return NotFound("Queue 'email-queue' does not exist.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to connect to Service Bus: {ex.Message}");
        }
    }
}
