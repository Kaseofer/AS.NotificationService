namespace AS.NotificationService.Infrastructure.Logger
{
    public interface IAppLoggerFactory
    {
        IAppLogger<T> CreateLogger<T>();
    }
}
