// ============================================
// Program.cs (SIMPLIFICADO)
// ============================================
using AS.NotificationService.API.Extensions;
using AS.NotificationService.Application.Extensions;
using AS.NotificationService.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configuración
builder.Configuration.AddEnvironmentVariables();




// Registro de servicios por capas

builder.Services.AddInfrastructureServices();
builder.Services.AddApplicationServices();
builder.Services.AddPresentationServices(builder.Configuration);


var app = builder.Build();

// Configuración del pipeline
app.ConfigureSwagger();
app.ConfigureHealthChecks();

app.MapControllers();

// Puerto dinámico para Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
Console.WriteLine("║         🚀 NOTIFICATION SERVICE STARTED              ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
Console.WriteLine($"📍 Port: {port}");
Console.WriteLine($"📄 Swagger: http://localhost:{port}");
Console.WriteLine($"💚 Health Check: http://localhost:{port}/health");
Console.WriteLine($"📬 RabbitMQ Consumer: Active");
Console.WriteLine("════════════════════════════════════════════════════════");

app.Run();