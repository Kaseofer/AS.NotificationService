namespace AS.NotificationService.Infrastructure.Email.Settings
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = null!;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = null!;
        public string SenderPassword { get; set; } = null!;
        public bool EnableSsl { get; set; }
        public bool IsBodyHtml { get; internal set; }
    }
}
