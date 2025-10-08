using System.Runtime.InteropServices;

namespace AS.NotificationService.Persistence.Interface
{
    public interface IEmailLogRepository
    {
        Task LogAsync(string entityId, string eventType,string to, object payload);
    }
}
