using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AS.NotificationService.Domain.Entities
{
    public class NotificationLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("notificationType")]
        [BsonRepresentation(BsonType.String)]
        public NotificationType NotificationType { get; set; }

        [BsonElement("source")]
        public string Source { get; set; } = string.Empty;

        [BsonElement("recipient")]
        public string Recipient { get; set; } = string.Empty;

        [BsonElement("subject")]
        public string? Subject { get; set; }

        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;

        [BsonElement("isSuccess")]
        public bool IsSuccess { get; set; }

        [BsonElement("errorMessage")]
        public string? ErrorMessage { get; set; }

        [BsonElement("attemptCount")]
        public int AttemptCount { get; set; } = 1;

        [BsonElement("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

}