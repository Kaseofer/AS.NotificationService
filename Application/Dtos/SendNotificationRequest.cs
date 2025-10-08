using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AS.NotificationService.Application.Dtos
{
    public class SendNotificationRequest
    {
        public string Type { get; set; } // "email" o "whatsapp"
        public string To { get; set; }
        public string Subject { get; set; }
        public string HtmlBody { get; set; }
        public string TextBody { get; set; }
        public string From { get; set; }
        public string ReplyTo { get; set; }
    }
}
