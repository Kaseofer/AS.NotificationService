using AS.NotificationService.Domain.Models;

namespace AS.NotificationService.Application.Service.Interface
{
    

    public interface IEmailSender
    {
        Task<bool> SendAsync(EmailRequest request);
    }
}