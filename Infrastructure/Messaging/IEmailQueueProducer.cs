using AS.NotificationService.Models;

namespace AS.NotificationService.Queue
{
        public interface IEmailQueueProducer
        {
             Task EnqueueAsync(EmailRequestDto request);
        }
    
}
