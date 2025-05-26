using EmailService.Services;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var builder = WebApplication.CreateBuilder(args);

// 1. Ladda Key Vault tidigt
string keyVaultUrl = builder.Configuration["KeyVaultUrl"];
builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());

// Efter Key Vault
string apiKey = builder.Configuration["SendGrid--ApiKey"];
string fromEmail = builder.Configuration["SendGrid--From"];
string fromName = builder.Configuration["SendGrid--FromName"];

Console.WriteLine($"🔐 SendGrid--ApiKey is {(string.IsNullOrEmpty(apiKey) ? "MISSING" : "LOADED")}");
Console.WriteLine($"🔐 SendGrid--From: {fromEmail}");
Console.WriteLine($"🔐 SendGrid--FromName: {fromName}");


// 2. Lägg till EmailSender – använder IConfiguration direkt
builder.Services.AddScoped<IEmailSender, EmailSender>();

// 3. Lägg till CORS (öppet för externa API-anrop)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 4. Swagger och Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5. Middleware pipeline
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
