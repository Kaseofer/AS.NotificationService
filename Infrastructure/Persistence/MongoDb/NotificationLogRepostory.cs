using AS.NotificationService.Domain.Entities;
using AS.NotificationService.Domain.Repositories;
using AS.NotificationService.Infrastructure.Persistence.MongoDB.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Infrastructure.Persistence.MongoDB;

public class NotificationLogRepository : INotificationLogRepository
{
    private readonly IMongoCollection<NotificationLog> _notificationLogs;

    public NotificationLogRepository(IOptions<MongoDbSettings> settings)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);
        _notificationLogs = mongoDatabase.GetCollection<NotificationLog>(
            settings.Value.NotificationLogsCollectionName);

        // Crear índices para mejorar rendimiento
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        try
        {
            // Índice por recipient
            var recipientIndexModel = new CreateIndexModel<NotificationLog>(
                Builders<NotificationLog>.IndexKeys.Ascending(x => x.Recipient));

            // Índice por tipo de notificación
            var typeIndexModel = new CreateIndexModel<NotificationLog>(
                Builders<NotificationLog>.IndexKeys.Ascending(x => x.NotificationType));

            // Índice por fecha (descendente para queries más recientes)
            var dateIndexModel = new CreateIndexModel<NotificationLog>(
                Builders<NotificationLog>.IndexKeys.Descending(x => x.CreatedAt));

            // Índice compuesto por éxito y fecha
            var successDateIndexModel = new CreateIndexModel<NotificationLog>(
                Builders<NotificationLog>.IndexKeys
                    .Ascending(x => x.IsSuccess)
                    .Descending(x => x.CreatedAt));

            _notificationLogs.Indexes.CreateMany(new[]
            {
                recipientIndexModel,
                typeIndexModel,
                dateIndexModel,
                successDateIndexModel
            });
        }
        catch (Exception ex)
        {
            // Log pero no falla - los índices son opcionales
            Console.WriteLine($"Error creando índices: {ex.Message}");
        }
    }

    // ============================================
    // CREATE
    // ============================================

    public async Task<NotificationLog> CreateAsync(NotificationLog notificationLog)
    {
        notificationLog.CreatedAt = DateTime.UtcNow;
        notificationLog.UpdatedAt = DateTime.UtcNow;
        await _notificationLogs.InsertOneAsync(notificationLog);
        return notificationLog;
    }

    public async Task<IEnumerable<NotificationLog>> CreateManyAsync(
        IEnumerable<NotificationLog> notificationLogs)
    {
        var logs = notificationLogs.ToList();
        var now = DateTime.UtcNow;

        foreach (var log in logs)
        {
            log.CreatedAt = now;
            log.UpdatedAt = now;
        }

        await _notificationLogs.InsertManyAsync(logs);
        return logs;
    }

    // ============================================
    // READ
    // ============================================

    public async Task<NotificationLog?> GetByIdAsync(string id)
    {
        return await _notificationLogs
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<NotificationLog>> GetAllAsync(int skip = 0, int limit = 100)
    {
        return await _notificationLogs
            .Find(_ => true)
            .SortByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotificationLog>> GetByRecipientAsync(string recipient)
    {
        return await _notificationLogs
            .Find(x => x.Recipient == recipient)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotificationLog>> GetByTypeAsync(NotificationType type)
    {
        return await _notificationLogs
            .Find(x => x.NotificationType == type)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotificationLog>> GetFailedNotificationsAsync(
        int skip = 0, int limit = 100)
    {
        return await _notificationLogs
            .Find(x => x.IsSuccess == false)
            .SortByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotificationLog>> GetByDateRangeAsync(
        DateTime startDate, DateTime endDate)
    {
        return await _notificationLogs
            .Find(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<long> GetTotalCountAsync()
    {
        return await _notificationLogs.CountDocumentsAsync(_ => true);
    }

    public async Task<long> GetSuccessCountAsync()
    {
        return await _notificationLogs.CountDocumentsAsync(x => x.IsSuccess == true);
    }

    public async Task<long> GetFailedCountAsync()
    {
        return await _notificationLogs.CountDocumentsAsync(x => x.IsSuccess == false);
    }

    // ============================================
    // UPDATE
    // ============================================

    public async Task<bool> UpdateAsync(string id, NotificationLog notificationLog)
    {
        notificationLog.UpdatedAt = DateTime.UtcNow;
        var result = await _notificationLogs.ReplaceOneAsync(
            x => x.Id == id,
            notificationLog);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> MarkAsSuccessAsync(string id)
    {
        var update = Builders<NotificationLog>.Update
            .Set(x => x.IsSuccess, true)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Unset(x => x.ErrorMessage);

        var result = await _notificationLogs.UpdateOneAsync(
            x => x.Id == id,
            update);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> MarkAsFailedAsync(string id, string errorMessage)
    {
        var update = Builders<NotificationLog>.Update
            .Set(x => x.IsSuccess, false)
            .Set(x => x.ErrorMessage, errorMessage)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _notificationLogs.UpdateOneAsync(
            x => x.Id == id,
            update);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> IncrementAttemptCountAsync(string id)
    {
        var update = Builders<NotificationLog>.Update
            .Inc(x => x.AttemptCount, 1)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _notificationLogs.UpdateOneAsync(
            x => x.Id == id,
            update);

        return result.ModifiedCount > 0;
    }

    // ============================================
    // DELETE
    // ============================================

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _notificationLogs.DeleteOneAsync(x => x.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<long> DeleteOldLogsAsync(DateTime olderThan)
    {
        var result = await _notificationLogs.DeleteManyAsync(
            x => x.CreatedAt < olderThan);

        return result.DeletedCount;
    }
}