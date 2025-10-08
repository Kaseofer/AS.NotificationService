namespace AS.NotificationService.Infrastructure.Logger
{
    public interface IAppLogger<T>
    {
        void LogInformation(string message);
        void LogWarning(string message);
        public void LogError(string message, Exception? ex = null, object? entity = null);

        void LogEntity(object entity);
    }
}
