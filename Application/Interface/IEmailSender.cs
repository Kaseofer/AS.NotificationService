using AS.NotificationService.Domain.Models;

namespace AS.NotificationService.Application.Service.Interface;

public interface IEmailSender
{
    /// <summary>
    /// Envía un email usando EmailRequest (método principal)
    /// </summary>
    Task<bool> SendAsync(EmailRequest request);

    /// <summary>
    /// Envía un email simple a un destinatario
    /// </summary>
    Task<bool> SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true);

    /// <summary>
    /// Envía un email a múltiples destinatarios
    /// </summary>
    Task<bool> SendEmailAsync(
        IEnumerable<string> toList,
        string subject,
        string body,
        bool isHtml = true);

    /// <summary>
    /// Envía un email con CC y BCC
    /// </summary>
    Task<bool> SendEmailAsync(
        string to,
        string subject,
        string body,
        IEnumerable<string>? ccList = null,
        IEnumerable<string>? bccList = null,
        bool isHtml = true);

    /// <summary>
    /// Envía un email con archivo adjunto (base64)
    /// </summary>
    Task<bool> SendEmailWithAttachmentAsync(
        string to,
        string subject,
        string body,
        string attachmentPath,
        string? attachmentName = null,
        bool isHtml = true);
}