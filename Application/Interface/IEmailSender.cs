using Application.Dtos;
using AS.NotificationService.Models;

namespace Application.Interface
{
    public interface IEmailSender
    {
        Task<bool> SendAsync(EmailRequestDto request);

      
    }
}
