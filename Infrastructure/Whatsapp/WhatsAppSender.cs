using AS.NotificationService.Application.Service.Interface;
using AS.NotificationService.Domain.Models;
using AS.NotificationService.Infrastructure.WhatsApp.Settings;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using AS.NotificationService.Domain.Repositories;
using AS.NotificationService.Domain.Entities;

namespace AS.NotificationService.Infrastructure.WhatsApp
{
    public class WhatsAppSender : IWhatsAppSender
    {
        private readonly WhatsAppSettings _settings;
        private readonly INotificationLogRepository _notificationRepository;
        private readonly ILogger<WhatsAppSender> _logger;

        public WhatsAppSender(
            IOptions<WhatsAppSettings> options,
            INotificationLogRepository repo,
            ILogger<WhatsAppSender> logger)
        {
            _settings = options.Value;
            _notificationRepository = repo;
            _logger = logger;

            // Inicializar Twilio si no está en modo mock
            if (!_settings.UseMockMode &&
                !string.IsNullOrEmpty(_settings.AccountSid) &&
                !string.IsNullOrEmpty(_settings.AuthToken))
            {
                TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
            }
        }
        public async Task<bool> SendMessageAsync(
            string phoneNumber,
            string message,
            Dictionary<string, string>? metadata = null)
        {
            // 1. Crear log inicial
            var notificationLog = new NotificationLog
            {
                NotificationType = NotificationType.WhatsApp,
                Recipient = phoneNumber,
                Message = message,
                AttemptCount = 1,
                IsSuccess = false,
                Metadata = metadata
            };

            var log = await _notificationRepository.CreateAsync(notificationLog);

            try
            {
                // 2. Modo MOCK
                if (_settings.UseMockMode)
                {
                    _logger.LogInformation(
                        "🟢 [MOCK WhatsApp] Mensaje enviado a {PhoneNumber}: {Message}",
                        phoneNumber,
                        message.Substring(0, Math.Min(50, message.Length))
                    );

                    await Task.Delay(500);
                    await _notificationRepository.MarkAsSuccessAsync(log.Id);
                    return true;
                }

                // 3. Modo TWILIO
                _logger.LogInformation("Enviando WhatsApp vía Twilio a {PhoneNumber}", phoneNumber);

                var messageOptions = new CreateMessageOptions(
                    new PhoneNumber($"whatsapp:{phoneNumber}"))
                {
                    From = new PhoneNumber($"whatsapp:{_settings.TwilioPhoneNumber}"),
                    Body = message
                };

                var messageResource = await MessageResource.CreateAsync(messageOptions);

                if (messageResource.Status == MessageResource.StatusEnum.Sent ||
                    messageResource.Status == MessageResource.StatusEnum.Queued)
                {
                    _logger.LogInformation(
                        "WhatsApp enviado exitosamente. SID: {Sid}",
                        messageResource.Sid
                    );
                    await _notificationRepository.MarkAsSuccessAsync(log.Id);
                    return true;
                }
                else
                {
                    _logger.LogError(
                        "Error al enviar WhatsApp. Status: {Status}",
                        messageResource.Status
                    );
                    await _notificationRepository.MarkAsFailedAsync(
                        log.Id,
                        $"Twilio Status: {messageResource.Status}"
                    );
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp to {PhoneNumber}", phoneNumber);
                await _notificationRepository.MarkAsFailedAsync(log.Id, ex.Message);
                return false;
            }
        }

        public Task<bool> SendMessageAsync(WhatsAppRequest request)
        {
            throw new NotImplementedException();
        }
    }
}