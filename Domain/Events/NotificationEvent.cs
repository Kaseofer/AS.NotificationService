namespace AS.NotificationService.Domain.Events
{
    public class NotificationEvent
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
