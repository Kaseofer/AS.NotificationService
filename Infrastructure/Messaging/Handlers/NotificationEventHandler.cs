// ============================================
// Infrastructure/Messaging/Handlers/NotificationEventHandler.cs
// ============================================
namespace AS.NotificationService.Infrastructure.Messaging.Handlers
{
    using AS.NotificationService.Application.Service.Interface;
    using AS.NotificationService.Domain.Events;
    using AS.NotificationService.Domain.Models;
    using AS.NotificationService.Dtos;
    using Microsoft.Extensions.Logging;

    public class NotificationEventHandler
    {
        private readonly IEmailSender _emailSender;
        private readonly IWhatsAppSender _whatsAppSender;
        private readonly ILogger<NotificationEventHandler> _logger;

        public NotificationEventHandler(IEmailSender emailSender,
                                        IWhatsAppSender whatsAppSender,
                                        ILogger<NotificationEventHandler> logger)
        {
            _whatsAppSender = whatsAppSender;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task HandleAsync(NotificationEvent notification)
        {
            _logger.LogInformation($"Processing notification: {notification.NotificationId} - Type: {notification.Type}");

            try
            {
                switch (notification.Type.ToLower())
                {
                    case "email":
                        await HandleEmailNotification(notification);
                        break;

                    case "whatsapp":
                        await HandleWhatsAppNotification(notification);
                        break;

                    default:
                        _logger.LogWarning($"Unknown notification type: {notification.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling notification {notification.NotificationId}");
                throw;
            }
        }

        private async Task HandleEmailNotification(NotificationEvent notification)
        {
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
                _logger.LogInformation($"✅ Email sent successfully: {notification.NotificationId}");
            }
            else
            {
                _logger.LogError($"❌ Failed to send email: {notification.NotificationId}");
                throw new Exception($"Email sending failed for notification {notification.NotificationId}");
            }
        }

        private async Task HandleWhatsAppNotification(NotificationEvent notification)
        {
            var whatsAppRequest = new WhatsAppRequest
            {
                To = notification.To,
                Message = notification.TextBody ?? notification.HtmlBody,
                MessageId = notification.NotificationId.ToString(),
                Metadata = notification.Metadata
            };

            var result = await _whatsAppSender.SendAsync(whatsAppRequest);

            if (result)
            {
                _logger.LogInformation($"✅ WhatsApp sent: {notification.NotificationId}");
            }
            else
            {
                _logger.LogError($"❌ Failed to send WhatsApp: {notification.NotificationId}");
                throw new Exception($"WhatsApp sending failed for {notification.NotificationId}");
            }
        }
    }
}
