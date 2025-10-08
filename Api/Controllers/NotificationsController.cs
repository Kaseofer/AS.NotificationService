// Controllers/NotificationsController.cs
namespace AS.NotificationService.Controllers
{
    using AS.NotificationService.Application.Dtos;
    using AS.NotificationService.Application.Service.Interface;
    using AS.NotificationService.Domain.Models;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly IEmailSender _emailSender;
        private readonly IWhatsAppSender _whatsAppSender;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            IEmailSender emailSender,
            IWhatsAppSender whatsAppSender,
            ILogger<NotificationsController> logger)
        {
            _emailSender = emailSender;
            _whatsAppSender = whatsAppSender;
            _logger = logger;
        }

        /// <summary>
        /// Enviar un email
        /// </summary>
        [HttpPost("email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.To))
                    return BadRequest(new { error = "El campo 'To' es requerido" });

                if (string.IsNullOrEmpty(request.Subject))
                    return BadRequest(new { error = "El campo 'Subject' es requerido" });

                if (string.IsNullOrEmpty(request.HtmlBody) && string.IsNullOrEmpty(request.TextBody))
                    return BadRequest(new { error = "Debe proporcionar 'HtmlBody' o 'TextBody'" });

                var emailRequest = new EmailRequest
                {
                    To = request.To,
                    Subject = request.Subject,
                    HtmlBody = request.HtmlBody,
                    TextBody = request.TextBody,
                    From = request.From,
                    ReplyTo = request.ReplyTo,
                    Headers = request.Headers,
                    MessageId = Guid.NewGuid().ToString()
                };

                var result = await _emailSender.SendAsync(emailRequest);

                if (result)
                {
                    _logger.LogInformation($"Email sent successfully to {request.To}");
                    return Ok(new
                    {
                        success = true,
                        message = "Email enviado correctamente",
                        messageId = emailRequest.MessageId,
                        recipient = request.To
                    });
                }
                else
                {
                    _logger.LogError($"Failed to send email to {request.To}");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Error al enviar el email"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while sending email");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno al procesar la solicitud",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Enviar un WhatsApp
        /// </summary>
        [HttpPost("whatsapp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendWhatsApp([FromBody] SendWhatsAppRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.To))
                    return BadRequest(new { error = "El campo 'To' es requerido" });

                if (string.IsNullOrEmpty(request.Message))
                    return BadRequest(new { error = "El campo 'Message' es requerido" });

                var whatsAppRequest = new WhatsAppRequest
                {
                    To = request.To,
                    Message = request.Message,
                    MediaUrl = request.MediaUrl,
                    MessageId = Guid.NewGuid().ToString(),
                    Metadata = request.Metadata
                };

                var result = await _whatsAppSender.SendAsync(whatsAppRequest);

                if (result)
                {
                    _logger.LogInformation($"WhatsApp sent successfully to {request.To}");
                    return Ok(new
                    {
                        success = true,
                        message = "WhatsApp enviado correctamente",
                        messageId = whatsAppRequest.MessageId,
                        recipient = request.To
                    });
                }
                else
                {
                    _logger.LogError($"Failed to send WhatsApp to {request.To}");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Error al enviar el WhatsApp"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while sending WhatsApp");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno al procesar la solicitud",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Enviar notificación (email o whatsapp según el tipo)
        /// </summary>
        [HttpPost("send")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Type))
                    return BadRequest(new { error = "El campo 'Type' es requerido (email o whatsapp)" });

                switch (request.Type.ToLower())
                {
                    case "email":
                        return await SendEmail(new SendEmailRequest
                        {
                            To = request.To,
                            Subject = request.Subject,
                            HtmlBody = request.HtmlBody,
                            TextBody = request.TextBody,
                            From = request.From,
                            ReplyTo = request.ReplyTo
                        });

                    case "whatsapp":
                        return await SendWhatsApp(new SendWhatsAppRequest
                        {
                            To = request.To,
                            Message = request.TextBody ?? request.HtmlBody
                        });

                    default:
                        return BadRequest(new { error = $"Tipo de notificación no soportado: {request.Type}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while sending notification");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Health check del servicio
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                service = "NotificationService",
                status = "healthy",
                timestamp = DateTime.UtcNow
            });
        }
    }

    // DTOs para las peticiones
   
   

    
}