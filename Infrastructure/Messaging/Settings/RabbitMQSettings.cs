namespace AS.NotificationService.Infrastructure.Messaging.Settings
{
    public class RabbitMQSettings
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string QueueName { get; set; }
    }
}
