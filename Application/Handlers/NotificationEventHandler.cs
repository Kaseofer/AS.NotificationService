using Application.Interface;

namespace Infrastructure.Messaging.Consumer.Handlers
{
    public class NotificationEventHandler
    {
        private readonly IEmailSender _emailSender;

        public async Task Handle(NotificationEvent notification)
        {
            switch (notification.Type)
            {
                case "email":
                    await _emailSender.SendAsync(notification);
                    break;
                case "whatsapp":
                    // futuro
                    break;
            }
        }
    }
}
