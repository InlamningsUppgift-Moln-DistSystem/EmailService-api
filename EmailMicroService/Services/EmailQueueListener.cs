﻿using Azure.Messaging.ServiceBus;
using EmailMicroService.DTOs;
using EmailService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EmailMicroService.Services
{
    public class EmailQueueListener : BackgroundService
    {
        private readonly ServiceBusProcessor _processor;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailQueueListener> _logger;

        public EmailQueueListener(IConfiguration config, IServiceProvider serviceProvider, ILogger<EmailQueueListener> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            var connectionString = config["ServiceBus:ConnectionString"];
            Console.WriteLine($"🔐 ServiceBus connection string loaded? {(!string.IsNullOrEmpty(connectionString))}");

            try
            {
                var client = new ServiceBusClient(connectionString);
                _processor = client.CreateProcessor("email-queue", new ServiceBusProcessorOptions());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to create ServiceBusProcessor: {ex.Message}");
                throw;
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Console.WriteLine("🚀 EmailQueueListener (console) started");
                _logger.LogInformation("🚀 EmailQueueListener started and listening to 'email-queue'");

                _processor.ProcessMessageAsync += HandleMessageAsync;
                _processor.ProcessErrorAsync += ErrorHandlerAsync;

                return _processor.StartProcessingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ExecuteAsync failed: {ex.Message}");
                throw;
            }
        }

        private async Task HandleMessageAsync(ProcessMessageEventArgs args)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            try
            {
                var body = args.Message.Body.ToString();
                var message = JsonSerializer.Deserialize<EmailMessageDto>(body);

                Console.WriteLine($"📨 Received message for {message?.To}");
                _logger.LogInformation("📨 Received message for {To}", message?.To);

                if (message != null)
                {
                    await emailSender.SendEmailAsync(message.To, message.Subject, message.Body);
                    await args.CompleteMessageAsync(args.Message);

                    Console.WriteLine($"✅ Email processed and completed for {message.To}");
                    _logger.LogInformation("✅ Email processed and completed for {To}", message.To);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in HandleMessageAsync: {ex.Message}");
                _logger.LogError(ex, "Error handling message");
            }
        }

        private Task ErrorHandlerAsync(ProcessErrorEventArgs args)
        {
            Console.WriteLine($"⚠️ Service Bus error: {args.Exception.Message}");
            _logger.LogError(args.Exception, "Service Bus error");
            return Task.CompletedTask;
        }
    }
}
