using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using EmailMicroService.Services;
using EmailService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.AzureAppServices;

var builder = WebApplication.CreateBuilder(args);

// --- LOGGING KONFIGURATION ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddAzureWebAppDiagnostics();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var logger = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    logging.AddAzureWebAppDiagnostics();
    logging.SetMinimumLevel(LogLevel.Information);
}).CreateLogger("Program");

logger.LogInformation("Program.cs start - innan Key Vault laddning");

// --- KEY VAULT KONFIGURATION ---
string keyVaultUrl = builder.Configuration["KeyVaultUrl"];
logger.LogInformation("KeyVaultUrl från konfiguration: {KeyVaultUrl}", keyVaultUrl);

builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());

logger.LogInformation("Key Vault laddad");

// --- KONFIGURATION AV SERVICE ---
string apiKey = builder.Configuration["SendGrid--ApiKey"];
string fromEmail = builder.Configuration["SendGrid--From"];
string fromName = builder.Configuration["SendGrid--FromName"];

logger.LogInformation("🔐 SendGrid--ApiKey is {Status}", string.IsNullOrEmpty(apiKey) ? "MISSING" : "LOADED");
logger.LogInformation("🔐 SendGrid--From: {FromEmail}", fromEmail);
logger.LogInformation("🔐 SendGrid--FromName: {FromName}", fromName);

// --- Dependency Injection ---
// IEmailSender som scoped (om den använder scoped resurser, annars singleton)
builder.Services.AddScoped<IEmailSender, EmailSender>();

// EmailQueueListener som hosted service singleton, men injicerar IServiceProvider istället för IEmailSender direkt
builder.Services.AddHostedService<EmailQueueListener>();

logger.LogInformation("Tjänster registrerade");

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// --- Controllers & Swagger ---
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
