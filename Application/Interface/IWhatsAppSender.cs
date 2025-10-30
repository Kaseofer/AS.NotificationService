// Service/Interface/IWhatsAppSender.cs
using AS.NotificationService.Domain.Models;

namespace AS.NotificationService.Application.Service.Interface
{
    public interface IWhatsAppSender
    {
        Task<bool> SendMessageAsync
            (string phoneNumber,
            string message,
            Dictionary<string, string>? metadata = null);
    }
}