using AS.NotificationService.Application.Service.Interface;
using AS.NotificationService.Domain.Entities;
using AS.NotificationService.Domain.Events;
using AS.NotificationService.Domain.Models;
using AS.NotificationService.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AS.NotificationService.Infrastructure.Messaging.Handlers;

public class NotificationEventHandler
{
    private readonly IEmailSender _emailSender;
    private readonly IWhatsAppSender _whatsAppSender;
    private readonly INotificationLogRepository _notificationRepository;
    private readonly ILogger<NotificationEventHandler> _logger;

    public NotificationEventHandler(
        IEmailSender emailSender,
        IWhatsAppSender whatsAppSender,
        INotificationLogRepository notificationRepository, // ← Inyectar repositorio
        ILogger<NotificationEventHandler> logger)
    {
        _emailSender = emailSender;
        _whatsAppSender = whatsAppSender;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task HandleAsync(NotificationEvent notification)
    {
        _logger.LogInformation(
            "📥 Procesando notificación desde RabbitMQ: {NotificationId} - Tipo: {Type}",
            notification.NotificationId,
            notification.Type);

        // Crear log inicial de la notificación recibida
        var notificationLog = await CreateInitialLog(notification);

        try
        {
            switch (notification.Type.ToLower())
            {
                case "email":
                    await HandleEmailNotification(notification, notificationLog);
                    break;

                case "whatsapp":
                    await HandleWhatsAppNotification(notification, notificationLog);
                    break;

                default:
                    _logger.LogWarning("⚠️ Tipo de notificación desconocido: {Type}", notification.Type);

                    notificationLog.IsSuccess = false;
                    notificationLog.ErrorMessage = $"Tipo de notificación desconocido: {notification.Type}";
                    await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error procesando notificación {NotificationId}", notification.NotificationId);

            // Registrar el error en MongoDB
            notificationLog.IsSuccess = false;
            notificationLog.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
            notificationLog.Metadata ??= new Dictionary<string, string>();
            notificationLog.Metadata["ExceptionType"] = ex.GetType().Name;
            notificationLog.Metadata["StackTrace"] = ex.StackTrace ?? "N/A";

            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);

            throw;
        }
    }

    // ============================================
    // CREAR LOG INICIAL
    // ============================================
    private async Task<NotificationLog> CreateInitialLog(NotificationEvent notification)
    {
        var notificationType = notification.Type.ToLower() switch
        {
            "email" => NotificationType.Email,
            "whatsapp" => NotificationType.WhatsApp,
            "sms" => NotificationType.SMS,
            "push" => NotificationType.Push,
            _ => NotificationType.Email
        };

        var notificationLog = new NotificationLog
        {
            NotificationType = notificationType,
            Recipient = notification.To ?? "unknown",
            Subject = notification.Subject,
            Source = "RabbitMQ",
            Message = notification.HtmlBody ?? notification.TextBody ?? "",
            IsSuccess = false, // Por defecto false hasta que se confirme el envío
            AttemptCount = 1,
            Metadata = new Dictionary<string, string>
            {
                ["Source"] = "RabbitMQ",
                ["NotificationId"] = notification.NotificationId.ToString(),
                ["From"] = notification.From ?? "N/A",
                ["QueueReceivedAt"] = DateTime.UtcNow.ToString("O")
            }
        };

        // Agregar metadata adicional si existe
        if (notification.Metadata != null)
        {
            foreach (var kvp in notification.Metadata)
            {
                notificationLog.Metadata[$"Original_{kvp.Key}"] = kvp.Value;
            }
        }

        // Guardar en MongoDB
        await _notificationRepository.CreateAsync(notificationLog);

        _logger.LogInformation(
            "📝 Log creado en MongoDB: {LogId} para notificación {NotificationId}",
            notificationLog.Id,
            notification.NotificationId);

        return notificationLog;
    }

    // ============================================
    // HANDLE EMAIL
    // ============================================
    private async Task HandleEmailNotification(
        NotificationEvent notification,
        NotificationLog notificationLog)
    {
        _logger.LogInformation("📧 Procesando email: {NotificationId}", notification.NotificationId);

        var emailRequest = new EmailRequest
        {
            To = notification.To,
            Subject = notification.Subject,
            HtmlBody = notification.HtmlBody,
            TextBody = notification.TextBody,
            From = notification.From,
            ReplyTo = notification.ReplyTo,
            Headers = notification.Headers,
            MessageId = notification.NotificationId.ToString()
        };

        var result = await _emailSender.SendAsync(emailRequest);

        if (result)
        {
            _logger.LogInformation("✅ Email enviado exitosamente: {NotificationId}", notification.NotificationId);

            // Actualizar log como exitoso
            notificationLog.IsSuccess = true;
            notificationLog.ErrorMessage = null;
            notificationLog.Metadata ??= new Dictionary<string, string>();
            notificationLog.Metadata["SentAt"] = DateTime.UtcNow.ToString("O");
            notificationLog.Metadata["Result"] = "Success";

            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);
        }
        else
        {
            _logger.LogError("❌ Error al enviar email: {NotificationId}", notification.NotificationId);

            // Actualizar log como fallido
            notificationLog.IsSuccess = false;
            notificationLog.ErrorMessage = "Email sender returned false";
            notificationLog.Metadata ??= new Dictionary<string, string>();
            notificationLog.Metadata["FailedAt"] = DateTime.UtcNow.ToString("O");

            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);

            throw new Exception($"Email sending failed for notification {notification.NotificationId}");
        }
    }

    // ============================================
    // HANDLE WHATSAPP
    // ============================================
    private async Task HandleWhatsAppNotification(
        NotificationEvent notification,
        NotificationLog notificationLog)
    {
        _logger.LogInformation("💬 Procesando WhatsApp: {NotificationId}", notification.NotificationId);

        var message = notification.TextBody ?? notification.HtmlBody ?? "";

        var result = await _whatsAppSender.SendMessageAsync(
            notification.To!,
            message,
            notification.Metadata);

        if (result)
        {
            _logger.LogInformation("✅ WhatsApp enviado exitosamente: {NotificationId}", notification.NotificationId);

            // Actualizar log como exitoso
            notificationLog.IsSuccess = true;
            notificationLog.ErrorMessage = null;
            notificationLog.Metadata ??= new Dictionary<string, string>();
            notificationLog.Metadata["SentAt"] = DateTime.UtcNow.ToString("O");
            notificationLog.Metadata["Result"] = "Success";

            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);
        }
        else
        {
            _logger.LogError("❌ Error al enviar WhatsApp: {NotificationId}", notification.NotificationId);

            // Actualizar log como fallido
            notificationLog.IsSuccess = false;
            notificationLog.ErrorMessage = "WhatsApp sender returned false";
            notificationLog.Metadata ??= new Dictionary<string, string>();
            notificationLog.Metadata["FailedAt"] = DateTime.UtcNow.ToString("O");

            await _notificationRepository.UpdateAsync(notificationLog.Id!, notificationLog);

            throw new Exception($"WhatsApp sending failed for notification {notification.NotificationId}");
        }
    }
}