using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Storage.Blobs;
using EmailMicroService.Services;
using EmailService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.AzureAppServices;

var builder = WebApplication.CreateBuilder(args);

// --- LOGGNING KONFIGURATION ---
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
// Läs in Key Vault URL från appsettings eller miljövariabler
string keyVaultUrl = builder.Configuration["KeyVaultUrl"];
logger.LogInformation("KeyVaultUrl från konfiguration: {KeyVaultUrl}", keyVaultUrl);

// Lägg till Key Vault som konfigurationskälla
builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());

logger.LogInformation("Key Vault laddad");

// --- KONFIGURATION AV SERVICE ---
// Läs in känsliga data från konfiguration (Key Vault)
string apiKey = builder.Configuration["SendGrid--ApiKey"];
string fromEmail = builder.Configuration["SendGrid--From"];
string fromName = builder.Configuration["SendGrid--FromName"];
string blobConnectionString = builder.Configuration["BlobConnectionString"];

logger.LogInformation("🔐 SendGrid--ApiKey is {Status}", string.IsNullOrEmpty(apiKey) ? "MISSING" : "LOADED");
logger.LogInformation("🔐 SendGrid--From: {FromEmail}", fromEmail);
logger.LogInformation("🔐 SendGrid--FromName: {FromName}", fromName);
logger.LogInformation("🔐 BlobConnectionString is {Status}", string.IsNullOrEmpty(blobConnectionString) ? "MISSING" : "LOADED");

// --- DEPENDENCY INJECTION ---
// Registrera BlobServiceClient som singleton (thread-safe)
builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));

// Registrera IEmailSender som scoped (kan vara singleton om inga scoped resurser behövs)
builder.Services.AddScoped<IEmailSender, EmailSender>();

// Registrera EmailQueueListener som hosted service (singleton)
builder.Services.AddHostedService<EmailQueueListener>();

logger.LogInformation("Tjänster registrerade");

// --- CORS ---
// Tillåt alla origin, headers och metoder (kan justeras efter behov)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// --- CONTROLLERS & SWAGGER ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

logger.LogInformation("Bygger app");

// --- BUILD APP ---
var app = builder.Build();

logger.LogInformation("App byggd - innan Middleware pipeline");

// --- MIDDLEWARE ---
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

// --- ROUTING ---
app.MapControllers();

logger.LogInformation("MapControllers anropat");
logger.LogInformation("App started");

// --- STARTA APPEN ---
app.Run();
