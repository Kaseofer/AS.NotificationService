using AS.NotificationService.Models;
using AS.NotificationService.Queue;
using Microsoft.AspNetCore.Mvc;

namespace AS.NotificationService.Controllers
{
    [ApiController]
    [Route("api/email")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailQueueProducer _queueProducer;

        public EmailController(IEmailQueueProducer queueProducer)
        {
            _queueProducer = queueProducer;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequestDto dto)
        {
            await _queueProducer.EnqueueAsync(dto);
            return Accepted(new { message = "Email encolado correctamente", dto.MessageId });
        }
    }
}
