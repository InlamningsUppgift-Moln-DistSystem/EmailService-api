using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class SelfPingService : BackgroundService
{
    private readonly HttpClient _httpClient = new();
    private int _counter = 0;

    private const string HealthUrl = "https://emailservice-api-e4c5b9cnfxehg6h8.swedencentral-01.azurewebsites.net/api/health";
    private const string SwaggerUrl = "https://emailservice-api-e4c5b9cnfxehg6h8.swedencentral-01.azurewebsites.net/swagger";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            string target = (_counter % 2 == 0) ? HealthUrl : SwaggerUrl;

            try
            {
                var response = await _httpClient.GetAsync(target, stoppingToken);
                Console.WriteLine($"🔄 Self-ping: {(int)response.StatusCode} at {DateTime.UtcNow:T} → {target}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Self-ping failed: {ex.Message}");
            }

            _counter++;
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // ⏱ 1 minut
        }
    }
}
