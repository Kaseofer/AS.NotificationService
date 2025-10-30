namespace AS.NotificationService.Infrastructure.WhatsApp.Settings
{
    public class WhatsAppSettings
    {
        public bool UseMockMode { get; set; } = true;  // Por defecto en Mock
        public string? AccountSid { get; set; }  // Para Twilio
        public string? AuthToken { get; set; }   // Para Twilio
        public object TwilioPhoneNumber { get; internal set; }


    }
}