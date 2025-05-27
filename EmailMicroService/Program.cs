using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using EmailMicroService.Services; // ← namespace för listenern
using EmailService.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Key Vault – ladda in känsliga värden
string keyVaultUrl = builder.Configuration["KeyVaultUrl"];
builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());

// 2. För debug/loggning
string apiKey = builder.Configuration["SendGrid--ApiKey"];
string fromEmail = builder.Configuration["SendGrid--From"];
string fromName = builder.Configuration["SendGrid--FromName"];

Console.WriteLine($"🔐 SendGrid--ApiKey is {(string.IsNullOrEmpty(apiKey) ? "MISSING" : "LOADED")}");
Console.WriteLine($"🔐 SendGrid--From: {fromEmail}");
Console.WriteLine($"🔐 SendGrid--FromName: {fromName}");

// 3. Dependency Injection
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddHostedService<EmailQueueListener>(); // 👈 Service Bus listener

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

// 6. Bygg app och konfigurera middleware
var app = builder.Build();

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
