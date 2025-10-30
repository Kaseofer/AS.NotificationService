using AS.NotificationService.Application.Service.Interface;
using AS.NotificationService.Domain.Entities;
using AS.NotificationService.Domain.Models;
using AS.NotificationService.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace AS.NotificationService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IEmailSender _emailSender;
    private readonly IWhatsAppSender _whatsAppSender;
    private readonly INotificationLogRepository _notificationRepository;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IEmailSender emailSender,
        IWhatsAppSender whatsAppSender,
        INotificationLogRepository notificationRepository,
        ILogger<NotificationsController> logger)
    {
        _emailSender = emailSender;
        _whatsAppSender = whatsAppSender;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    // ============================================
    // EMAIL SIMPLE
    // ============================================
    [HttpPost("email/simple")]
    public async Task<IActionResult> SendSimpleEmail([FromBody] SendSimpleEmailRequest request)
    {
        _logger.LogInformation("📧 [WebAPI] Enviando email simple a {To}", request.To);

        // Crear log inicial
        var notificationLog = new NotificationLog
        {
            NotificationType = NotificationType.Email,
            Recipient = request.To,
            Subject = request.Subject,
            Message = request.Body,
            IsSuccess = false,
            Source = "WebAPI",
            AttemptCount = 1,
            Metadata = new Dictionary<string, string>
            {
                ["Source"] = "WebAPI",
                ["Endpoint"] = "/api/notifications/email/simple",
                ["ReceivedAt"] = DateTime.UtcNow.ToString("O")
            }
        };

        try
        {
            // Guardar log inicial
            await _notificationRepository.CreateAsync(notificationLog);

            // Enviar email
            var success = await _emailSender.SendEmailAsync(
                request.To,
                request.Subject,
                request.Body,
                request.IsHtml ?? true);

            // Actualizar log con resultado
            notificationLog.IsSuccess = success;
            if (success)
            {
                notificationLog.Metadata["SentAt"] = DateTime.UtcNow.ToString("O");
                notificationLog.Metadata["Result"] = "Success";
            }
            else
            {
                notificationLog.ErrorMessage = "Email sender returned false";
                notificationLog.Metadata["FailedAt"] = DateTime.UtcNow.ToString("O");
            }

            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);

            return success
                ? Ok(new { message = "Email enviado exitosamente", logId = notificationLog.Id })
                : StatusCode(500, new { message = "Error al enviar email", logId = notificationLog.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error enviando email");

            notificationLog.IsSuccess = false;
            notificationLog.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
            notificationLog.Metadata["ExceptionType"] = ex.GetType().Name;
            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);

            return StatusCode(500, new { message = ex.Message, logId = notificationLog.Id });
        }
    }

    // ============================================
    // EMAIL MÚLTIPLE
    // ============================================
    [HttpPost("email/multiple")]
    public async Task<IActionResult> SendMultipleEmail([FromBody] SendMultipleEmailRequest request)
    {
        _logger.LogInformation("📧 [WebAPI] Enviando a {Count} destinatarios", request.ToList.Count);

        var notificationLog = new NotificationLog
        {
            NotificationType = NotificationType.Email,
            Recipient = string.Join(", ", request.ToList),
            Subject = request.Subject,
            Message = request.Body,
            IsSuccess = false,
            AttemptCount = 1,
            Source = "WebAPI",
            Metadata = new Dictionary<string, string>
            {
                ["Source"] = "WebAPI",
                ["Endpoint"] = "/api/notifications/email/multiple",
                ["RecipientsCount"] = request.ToList.Count.ToString(),
                ["ReceivedAt"] = DateTime.UtcNow.ToString("O")
            }
        };

        try
        {
            await _notificationRepository.CreateAsync(notificationLog);

            var success = await _emailSender.SendEmailAsync(
                request.ToList,
                request.Subject,
                request.Body,
                request.IsHtml ?? true);

            notificationLog.IsSuccess = success;
            if (success)
            {
                notificationLog.Metadata["SentAt"] = DateTime.UtcNow.ToString("O");
            }
            else
            {
                notificationLog.ErrorMessage = "Email sender returned false";
            }

            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);

            return success
                ? Ok(new { message = $"Email enviado a {request.ToList.Count} destinatarios", logId = notificationLog.Id })
                : StatusCode(500, new { message = "Error", logId = notificationLog.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error");
            notificationLog.IsSuccess = false;
            notificationLog.ErrorMessage = ex.Message;
            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);
            return StatusCode(500, new { message = ex.Message, logId = notificationLog.Id });
        }
    }

    // ============================================
    // EMAIL CON CC/BCC
    // ============================================
    [HttpPost("email/cc-bcc")]
    public async Task<IActionResult> SendEmailWithCcBcc([FromBody] SendEmailCcBccRequest request)
    {
        _logger.LogInformation("📧 [WebAPI] Enviando con CC/BCC a {To}", request.To);

        var recipients = request.To;
        if (request.CcList?.Any() == true) recipients += $", CC: {string.Join(", ", request.CcList)}";
        if (request.BccList?.Any() == true) recipients += $", BCC: {string.Join(", ", request.BccList)}";

        var notificationLog = new NotificationLog
        {
            NotificationType = NotificationType.Email,
            Recipient = recipients,
            Subject = request.Subject,
            Message = request.Body,
            IsSuccess = false,
            Source = "WebAPI",
            Metadata = new Dictionary<string, string>
            {
                ["Source"] = "WebAPI",
                ["Endpoint"] = "/api/notifications/email/cc-bcc",
                ["HasCC"] = (request.CcList?.Any() ?? false).ToString(),
                ["HasBCC"] = (request.BccList?.Any() ?? false).ToString(),
                ["ReceivedAt"] = DateTime.UtcNow.ToString("O")
            }
        };

        try
        {
            await _notificationRepository.CreateAsync(notificationLog);

            var success = await _emailSender.SendEmailAsync(
                request.To,
                request.Subject,
                request.Body,
                request.CcList,
                request.BccList,
                request.IsHtml ?? true);

            notificationLog.IsSuccess = success;
            if (!success) notificationLog.ErrorMessage = "Email sender returned false";
            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);

            return success
                ? Ok(new { message = "Email enviado", logId = notificationLog.Id })
                : StatusCode(500, new { message = "Error", logId = notificationLog.Id });
        }
        catch (Exception ex)
        {
            notificationLog.IsSuccess = false;
            notificationLog.ErrorMessage = ex.Message;
            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);
            return StatusCode(500, new { message = ex.Message, logId = notificationLog.Id });
        }
    }

    // ============================================
    // EMAIL CON ADJUNTO
    // ============================================
    [HttpPost("email/attachment")]
    public async Task<IActionResult> SendEmailWithAttachment([FromForm] SendEmailAttachmentRequest request)
    {
        _logger.LogInformation("📧 [WebAPI] Enviando con adjunto a {To}", request.To);

        if (request.Attachment == null || request.Attachment.Length == 0)
            return BadRequest(new { message = "No se proporcionó archivo" });

        var notificationLog = new NotificationLog
        {
            NotificationType = NotificationType.Email,
            Recipient = request.To,
            Subject = request.Subject,
            Message = request.Body,
            IsSuccess = false,
            Source = "WebAPI",
            Metadata = new Dictionary<string, string>
            {
                ["Source"] = "WebAPI",
                ["Endpoint"] = "/api/notifications/email/attachment",
                ["HasAttachment"] = "true",
                ["AttachmentName"] = request.Attachment.FileName,
                ["AttachmentSize"] = request.Attachment.Length.ToString(),
                ["ReceivedAt"] = DateTime.UtcNow.ToString("O")
            }
        };

        var tempPath = Path.Combine(Path.GetTempPath(), request.Attachment.FileName);

        try
        {
            await _notificationRepository.CreateAsync(notificationLog);

            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await request.Attachment.CopyToAsync(stream);
            }

            var success = await _emailSender.SendEmailWithAttachmentAsync(
                request.To,
                request.Subject,
                request.Body,
                tempPath,
                request.Attachment.FileName,
                request.IsHtml ?? true);

            notificationLog.IsSuccess = success;
            if (!success) notificationLog.ErrorMessage = "Email sender returned false";
            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);

            return success
                ? Ok(new { message = "Email con adjunto enviado", logId = notificationLog.Id })
                : StatusCode(500, new { message = "Error", logId = notificationLog.Id });
        }
        catch (Exception ex)
        {
            notificationLog.IsSuccess = false;
            notificationLog.ErrorMessage = ex.Message;
            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);
            return StatusCode(500, new { message = ex.Message, logId = notificationLog.Id });
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }

    // ============================================
    // EMAIL AVANZADO
    // ============================================
    [HttpPost("email/advanced")]
    public async Task<IActionResult> SendAdvancedEmail([FromBody] EmailRequest request)
    {
        _logger.LogInformation("📧 [WebAPI] Enviando email avanzado a {To}", request.To);

        var notificationLog = new NotificationLog
        {
            NotificationType = NotificationType.Email,
            Recipient = request.To ?? "unknown",
            Subject = request.Subject,
            Message = request.HtmlBody ?? request.TextBody ?? "",
            IsSuccess = false,
            Source = "WebAPI",
            Metadata = new Dictionary<string, string>
            {
                ["Source"] = "WebAPI",
                ["Endpoint"] = "/api/notifications/email/advanced",
                ["MessageId"] = request.MessageId ?? Guid.NewGuid().ToString(),
                ["ReceivedAt"] = DateTime.UtcNow.ToString("O")
            }
        };

        try
        {
            await _notificationRepository.CreateAsync(notificationLog);

            var success = await _emailSender.SendAsync(request);

            notificationLog.IsSuccess = success;
            if (!success) notificationLog.ErrorMessage = "Email sender returned false";
            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);

            return success
                ? Ok(new { message = "Email enviado", logId = notificationLog.Id, messageId = request.MessageId })
                : StatusCode(500, new { message = "Error", logId = notificationLog.Id });
        }
        catch (Exception ex)
        {
            notificationLog.IsSuccess = false;
            notificationLog.ErrorMessage = ex.Message;
            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);
            return StatusCode(500, new { message = ex.Message, logId = notificationLog.Id });
        }
    }

    // ============================================
    // WHATSAPP
    // ============================================
    [HttpPost("whatsapp")]
    public async Task<IActionResult> SendWhatsApp([FromBody] SendWhatsAppRequest request)
    {
        _logger.LogInformation("💬 [WebAPI] Enviando WhatsApp a {To}", request.To);

        var notificationLog = new NotificationLog
        {
            NotificationType = NotificationType.WhatsApp,
            Recipient = request.To,
            Message = request.Message,
            IsSuccess = false,
            Source = "WebAPI",
            Metadata = new Dictionary<string, string>
            {
                ["Source"] = "WebAPI",
                ["Endpoint"] = "/api/notifications/whatsapp",
                ["ReceivedAt"] = DateTime.UtcNow.ToString("O")
            }
        };

        try
        {
            await _notificationRepository.CreateAsync(notificationLog);

            var success = await _whatsAppSender.SendMessageAsync(
                request.To,
                request.Message,
                request.Metadata);

            notificationLog.IsSuccess = success;
            if (!success) notificationLog.ErrorMessage = "WhatsApp sender returned false";
            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);

            return success
                ? Ok(new { message = "WhatsApp enviado", logId = notificationLog.Id })
                : StatusCode(500, new { message = "Error", logId = notificationLog.Id });
        }
        catch (Exception ex)
        {
            notificationLog.IsSuccess = false;
            notificationLog.ErrorMessage = ex.Message;
            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);
            return StatusCode(500, new { message = ex.Message, logId = notificationLog.Id });
        }
    }

    // ============================================
    // CONSULTA DE LOGS
    // ============================================

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int skip = 0, [FromQuery] int limit = 100)
    {
        var logs = await _notificationRepository.GetAllAsync(skip, limit);
        var total = await _notificationRepository.GetTotalCountAsync();
        return Ok(new { total, skip, limit, data = logs });
    }

    [HttpGet("logs/source/{source}")]
    public async Task<IActionResult> GetLogsBySource(string source)
    {
        var logs = await _notificationRepository.GetAllAsync(0, 1000);
        var filtered = logs.Where(l =>
            l.Metadata != null &&
            l.Metadata.ContainsKey("Source") &&
            l.Metadata["Source"].Equals(source, StringComparison.OrdinalIgnoreCase));

        return Ok(filtered);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var total = await _notificationRepository.GetTotalCountAsync();
        var success = await _notificationRepository.GetSuccessCountAsync();
        var failed = await _notificationRepository.GetFailedCountAsync();

        return Ok(new
        {
            total,
            success,
            failed,
            successRate = total > 0 ? (double)success / total * 100 : 0
        });
    }
}

// ============================================
// DTOs
// ============================================

public record SendSimpleEmailRequest(string To, string Subject, string Body, bool? IsHtml = true);
public record SendMultipleEmailRequest(List<string> ToList, string Subject, string Body, bool? IsHtml = true);
public record SendEmailCcBccRequest(string To, string Subject, string Body, List<string>? CcList = null, List<string>? BccList = null, bool? IsHtml = true);
public record SendWhatsAppRequest(string To, string Message, Dictionary<string, string>? Metadata = null);

public class SendEmailAttachmentRequest
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public IFormFile? Attachment { get; set; }
    public bool? IsHtml { get; set; } = true;
}