using AS.NotificationService.Application.Service.Interface;
using AS.NotificationService.Domain.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("diagnostics")]
public class DiagnosticsController : ControllerBase
{
    private readonly IEmailSender _emailSender;

    public DiagnosticsController(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    [HttpGet("ping-smtp")]
    public async Task<IActionResult> PingSmtp()
    {
        var testEmail = new EmailRequest
        {
            To = "kaseofer@gmail.com", // Cambiá esto por tu correo real
            Subject = "🔧 Test SMTP desde Railway",
            TextBody = "Este es un correo de prueba.",
            HtmlBody = "<p><strong>Este es un correo de prueba</strong></p>",
            MessageId = Guid.NewGuid().ToString()
        };

        try
        {
            var result = await _emailSender.SendAsync(testEmail);

            if (result)
                return Ok("✅ SMTP operativo. Correo enviado correctamente.");
            else
                return StatusCode(500, "❌ Fallo en el envío. Verificá configuración SMTP.");
        }
        catch (Exception ex)
        {
            var errorMessage = $"❌ Error interno: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $" | InnerException: {ex.InnerException.Message}";
            }

            Console.WriteLine(errorMessage);
            return StatusCode(500, errorMessage);
        }
    }
}
