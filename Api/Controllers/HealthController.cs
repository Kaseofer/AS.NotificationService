using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Reflection;

namespace AS.NotificationService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly IConfiguration _configuration;

        public HealthController(HealthCheckService healthCheckService, IConfiguration configuration)
        {
            _healthCheckService = healthCheckService;
            _configuration = configuration;
        }

        // Health check básico para Railway
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var report = await _healthCheckService.CheckHealthAsync();

            var response = new
            {
                status = report.Status.ToString(),
                timestamp = DateTime.UtcNow,
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                service = "AgendaSalud.EmailService",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                uptime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"dd\.hh\:mm\:ss"),
                checks = report.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        status = kvp.Value.Status.ToString(),
                        duration = kvp.Value.Duration.TotalMilliseconds,
                        description = kvp.Value.Description,
                        data = kvp.Value.Data
                    }
                )
            };

            return report.Status == HealthStatus.Healthy
                ? Ok(response)
                : StatusCode(503, response);
        }

        // Health check detallado
        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailed()
        {
            var report = await _healthCheckService.CheckHealthAsync();

            var response = new
            {
                status = report.Status.ToString(),
                timestamp = DateTime.UtcNow,
                totalDuration = report.TotalDuration.TotalMilliseconds,
                service = new
                {
                    name = "AgendaSalud.EmailService",
                    version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    uptime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"dd\.hh\:mm\:ss"),
                    processId = Environment.ProcessId,
                    machineName = Environment.MachineName,
                    workingSet = GC.GetTotalMemory(false),
                    threadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count
                },
                checks = report.Entries.Select(kvp => new
                {
                    name = kvp.Key,
                    status = kvp.Value.Status.ToString(),
                    duration = kvp.Value.Duration.TotalMilliseconds,
                    description = kvp.Value.Description,
                    exception = kvp.Value.Exception?.Message,
                    data = kvp.Value.Data,
                    tags = kvp.Value.Tags
                }).ToList()
            };

            return report.Status == HealthStatus.Healthy
                ? Ok(response)
                : StatusCode(503, response);
        }

        // Endpoint específico para Railway health check
        [HttpGet("live")]
        public IActionResult Live()
        {
            return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
        }

        // Endpoint para readiness check
        [HttpGet("ready")]
        public async Task<IActionResult> Ready()
        {
            try
            {
                var report = await _healthCheckService.CheckHealthAsync();

                if (report.Status == HealthStatus.Healthy)
                {
                    return Ok(new
                    {
                        status = "ready",
                        timestamp = DateTime.UtcNow,
                        message = "Service is ready to accept requests"
                    });
                }
                else
                {
                    return StatusCode(503, new
                    {
                        status = "not_ready",
                        timestamp = DateTime.UtcNow,
                        message = "Service is not ready to accept requests",
                        issues = report.Entries.Where(e => e.Value.Status != HealthStatus.Healthy)
                                             .Select(e => e.Key).ToArray()
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    status = "error",
                    timestamp = DateTime.UtcNow,
                    message = ex.Message
                });
            }
        }
    }
}