using Azure.Messaging.ServiceBus;
using EmailMicroService.DTOs;
using EmailService.DTOs;
using System.Text.Json;
using EmailService.Services;

namespace EmailMicroService.Services;

public class EmailQueueListener : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailQueueListener> _logger;

    public EmailQueueListener(IConfiguration config, IEmailSender emailSender, ILogger<EmailQueueListener> logger)
    {
        _emailSender = emailSender;
        _logger = logger;

        var client = new ServiceBusClient(config["ServiceBus:ConnectionString"]);
        _processor = client.CreateProcessor("email-queue", new ServiceBusProcessorOptions());
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 EmailQueueListener started and listening to 'email-queue'");

        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += ErrorHandlerAsync;

        return _processor.StartProcessingAsync(stoppingToken);
    }


    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {

        try
        {
            var body = args.Message.Body.ToString();
            var message = JsonSerializer.Deserialize<EmailMessageDto>(body);
            _logger.LogInformation($"📨 Received message for {message?.To}");

            if (message != null)
            {
                await _emailSender.SendConfirmationEmailAsync(message.To, message.ConfirmationUrl);
                await args.CompleteMessageAsync(args.Message);
                _logger.LogInformation("✅ Email processed and message completed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Failed to process message: {ex.Message}");
        }
    }

    private Task ErrorHandlerAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError($"⚠️ Service Bus Error: {args.Exception.Message}");
        return Task.CompletedTask;
    }
}
