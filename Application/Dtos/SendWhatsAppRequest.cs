using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AS.NotificationService.Application.Dtos
{
    public class SendWhatsAppRequest
    {
        public string To { get; set; }
        public string Message { get; set; }
        public string MediaUrl { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}
