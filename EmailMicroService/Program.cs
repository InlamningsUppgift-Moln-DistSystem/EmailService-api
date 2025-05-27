using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using EmailMicroService.Services; // ← namespace för listenern
using EmailService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.AzureAppServices;

var builder = WebApplication.CreateBuilder(args);

// Konfigurera logging med Azure App Service diagnostics och Console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddAzureWebAppDiagnostics();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    logging.AddAzureWebAppDiagnostics();
    logging.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger("Program");


logger.LogInformation("Program.cs start - innan Key Vault laddning");

// 1. Key Vault – ladda in känsliga värden
string keyVaultUrl = builder.Configuration["KeyVaultUrl"];
logger.LogInformation("KeyVaultUrl från konfiguration: {KeyVaultUrl}", keyVaultUrl);

builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());

logger.LogInformation("Key Vault laddad");

// 2. För debug/loggning
string apiKey = builder.Configuration["SendGrid--ApiKey"];
string fromEmail = builder.Configuration["SendGrid--From"];
string fromName = builder.Configuration["SendGrid--FromName"];

logger.LogInformation("🔐 SendGrid--ApiKey is {Status}", string.IsNullOrEmpty(apiKey) ? "MISSING" : "LOADED");
logger.LogInformation("🔐 SendGrid--From: {FromEmail}", fromEmail);
logger.LogInformation("🔐 SendGrid--FromName: {FromName}", fromName);

// 3. Dependency Injection
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddHostedService<EmailQueueListener>(); // 👈 Service Bus listener

logger.LogInformation("Tjänster registrerade");

// 4. CORS – tillåt alla origins (just nu)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 5. Swagger och Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

logger.LogInformation("Bygger app");

var app = builder.Build();

logger.LogInformation("App byggd - innan Middleware pipeline");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EmailService API V1");
    c.RoutePrefix = "swagger";
});

logger.LogInformation("Swagger UI satt");

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

logger.LogInformation("Middleware konfigurerat");

app.MapControllers();

logger.LogInformation("MapControllers anropat");

logger.LogInformation("App started");

app.Run();
