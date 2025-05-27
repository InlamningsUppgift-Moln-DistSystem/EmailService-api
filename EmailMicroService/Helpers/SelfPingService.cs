using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class SelfPingService : BackgroundService
{
    private readonly HttpClient _httpClient = new();
    private const string PingUrl = "https://emailservice-api-e4c5b9cnfxehg6h8.swedencentral-01.azurewebsites.net/api/health";


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _httpClient.GetAsync(PingUrl, stoppingToken);
                Console.WriteLine($"🔄 Self-ping: {(int)response.StatusCode} at {DateTime.UtcNow:T}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Self-ping failed: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }
}
