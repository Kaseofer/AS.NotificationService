using System.Text.Encodings.Web;
using System.Text.Json;


namespace AS.NotificationService.Infrastructure.Logger
{
    public class FileLogger<T> : IAppLogger<T>
    {
        private readonly string _logFilePath;

        public FileLogger()
        {
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);
            _logFilePath = Path.Combine(logDirectory, $"{typeof(T).Name}_{DateTime.Today:yyyyMMdd}.log");
        }

        public void LogInformation(string message) => WriteLog("INFO", message);
        public void LogWarning(string message) => WriteLog("WARN", message);
        public void LogError(string message, Exception? ex = null, object? entity = null)
        {
            var error = ex != null ? $"{message} | Exception: {ex.Message}" : message;
            WriteLog("ERROR", error);

            var InnerEx = ex.InnerException;

            if (InnerEx != null)
            {
                var innerError = $"Inner Exception:{InnerEx.Message}";
                WriteLog("ERROR", innerError);

            }

            if (entity != null)
                LogEntity(entity);
        }

        private void WriteLog(string level, string message)
        {
            var logEntry = $"{DateTime.Now:HH:mm:ss} [{level}] {message}";
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
        }

        public void LogEntity(object entity)
        {
            var entityJson = SerializeEntity(entity);
            WriteLog("INFO", $"{entity.GetType().Name}: {entityJson}");
        }
        private string SerializeEntity(object entity)
        {
            return entity is string str ? str :
                   JsonSerializer.Serialize(entity, new JsonSerializerOptions
                   {
                       WriteIndented = true,
                       Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                   });
        }
    }
}