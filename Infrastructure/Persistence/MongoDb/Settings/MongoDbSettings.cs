namespace AS.NotificationService.Infrastructure.Persistence.MongoDB.Settings;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string NotificationLogsCollectionName { get; set; } = string.Empty;
}