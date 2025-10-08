namespace AS.NotificationService.Domain.Models
{
    public class EmailRequest
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string HtmlBody { get; set; }
        public string TextBody { get; set; }
        public string From { get; set; }
        public string ReplyTo { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string MessageId { get; set; }
    }
}