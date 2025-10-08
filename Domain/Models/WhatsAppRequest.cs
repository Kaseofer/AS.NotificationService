// Models/WhatsAppRequest.cs
namespace AS.NotificationService.Domain.Models
{
    public class WhatsAppRequest
    {
        public string To { get; set; }  // Número de teléfono (ej: +5491112345678)
        public string Message { get; set; }  // Mensaje de texto
        public string MediaUrl { get; set; }  // URL de imagen/video (opcional)
        public string MessageId { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}