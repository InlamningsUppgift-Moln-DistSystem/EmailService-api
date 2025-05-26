using EmailService.Configuration;
using EmailService.Services;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var builder = WebApplication.CreateBuilder(args);

// 1. Ladda Key Vault tidigt
string keyVaultUrl = builder.Configuration["KeyVaultUrl"];
builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());

// 2. Ladda in SendGrid-inställningar från Key Vault manuellt
builder.Services.Configure<EmailSettings>(options =>
{
    options.SendGridApiKey = builder.Configuration["SendGrid--ApiKey"];
    options.FromEmail = builder.Configuration["SendGrid--From"];
    options.FromName = builder.Configuration["SendGrid--FromName"];
});

// 3. Lägg till tjänster
builder.Services.AddScoped<IEmailSender, EmailSender>();

// 4. Lägg till CORS (tillåter alla – just nu OK för API-kommunikation)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 5. Lägg till Swagger och Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 6. Middleware pipeline
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
