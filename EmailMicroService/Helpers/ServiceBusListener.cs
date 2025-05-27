using Azure.Messaging.ServiceBus;

public class ServiceBusListener : BackgroundService
{
    private readonly ServiceBusProcessor _processor;

    public ServiceBusListener(IConfiguration config)
    {
        var client = new ServiceBusClient(config["ServiceBus:ConnectionString"]);
        _processor = client.CreateProcessor("email-queue");

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ErrorHandler;
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var json = args.Message.Body.ToString();
        Console.WriteLine($"📥 Message received: {json}");
        await args.CompleteMessageAsync(args.Message);
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine($"🔥 Error: {args.Exception.Message}");
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _processor.StartProcessingAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
