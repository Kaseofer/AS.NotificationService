using AS.NotificationService.Domain.Entities;

namespace AS.NotificationService.Domain.Repositories
{

    public interface INotificationLogRepository
    {
        // Create
        Task<NotificationLog> CreateAsync(NotificationLog notificationLog);
        Task<IEnumerable<NotificationLog>> CreateManyAsync(IEnumerable<NotificationLog> notificationLogs);

        // Read
        Task<NotificationLog?> GetByIdAsync(string id);
        Task<IEnumerable<NotificationLog>> GetAllAsync(int skip = 0, int limit = 100);
        Task<IEnumerable<NotificationLog>> GetByRecipientAsync(string recipient);
        Task<IEnumerable<NotificationLog>> GetByTypeAsync(NotificationType type);
        Task<IEnumerable<NotificationLog>> GetFailedNotificationsAsync(int skip = 0, int limit = 100);
        Task<IEnumerable<NotificationLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<long> GetTotalCountAsync();
        Task<long> GetSuccessCountAsync();
        Task<long> GetFailedCountAsync();

        // Update
        Task<bool> UpdateAsync(string id, NotificationLog notificationLog);
        Task<bool> MarkAsSuccessAsync(string id);
        Task<bool> MarkAsFailedAsync(string id, string errorMessage);
        Task<bool> IncrementAttemptCountAsync(string id);

        // Delete
        Task<bool> DeleteAsync(string id);
        Task<long> DeleteOldLogsAsync(DateTime olderThan);
    }
}