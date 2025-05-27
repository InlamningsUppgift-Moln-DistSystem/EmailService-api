using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Storage.Blobs;
using EmailMicroService.Services;
using EmailService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.AzureAppServices;

var builder = WebApplication.CreateBuilder(args);

// --- LOGGNING ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddAzureWebAppDiagnostics();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
System.Diagnostics.Trace.AutoFlush = true;
System.Diagnostics.Trace.WriteLine("🟠 System.Diagnostics.Trace is active");

// --- KEY VAULT ---
string keyVaultUrl = builder.Configuration["KeyVaultUrl"];
builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());

// --- KONFIGURATION AV SECRETS ---
string apiKey = builder.Configuration["SendGrid--ApiKey"];
string fromEmail = builder.Configuration["SendGrid--From"];
string fromName = builder.Configuration["SendGrid--FromName"];
string blobConnectionString = builder.Configuration["BlobConnectionString"];

// --- DEPENDENCY INJECTION ---
builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddHostedService<EmailQueueListener>(); // Lyssnar på Service Bus
builder.Services.AddHostedService<ServiceBusListener>(); // 🔄 Ny permanent bakgrundsprocessor
builder.Services.AddHostedService<SelfPingService>();

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

// --- CONTROLLERS + SWAGGER ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- LOGGER ---
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 EmailService startup complete");
logger.LogInformation("🔐 KeyVault URL: {Url}", keyVaultUrl);
logger.LogInformation("🔐 SendGrid ApiKey status: {Status}", string.IsNullOrEmpty(apiKey) ? "MISSING" : "LOADED");
logger.LogInformation("🔐 FromEmail: {From}", fromEmail ?? "(null)");
logger.LogInformation("🔐 BlobConnectionString status: {Status}", string.IsNullOrEmpty(blobConnectionString) ? "MISSING" : "LOADED");

logger.LogInformation("✅ Middleware pipeline klar – appen är redo att ta emot trafik");
Console.WriteLine("🧪 TEST: Console.WriteLine syns också");
System.Diagnostics.Debug.WriteLine("🔧 Detta går till Debug-fönstret i VS");

// --- MIDDLEWARE ---
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EmailService API V1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
