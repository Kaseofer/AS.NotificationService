using AS.NotificationService.Application.Service.Interface;
using AS.NotificationService.Domain.Entities;
using AS.NotificationService.Domain.Models;
using AS.NotificationService.Domain.Repositories;
using AS.NotificationService.Infrastructure.Email.Settings;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AS.NotificationService.Infrastructure.Email;

public class EmailSender : IEmailSender
{
    protected readonly EmailSettings _settings;
    protected readonly INotificationLogRepository _notificationRepository; // ← MongoDB
    private readonly HttpClient _httpClient;

    public EmailSender(
        IOptions<EmailSettings> options,
        INotificationLogRepository notificationRepository, // ← Cambio aquí
        HttpClient httpClient)
    {
        _settings = options.Value;
        _notificationRepository = notificationRepository;
        _httpClient = httpClient;
    }

    // ============================================
    // MÉTODO PRINCIPAL - SendAsync
    // ============================================
    public async Task<bool> SendAsync(EmailRequest request)
    {
        var notificationLog = new NotificationLog
        {
            NotificationType = NotificationType.Email,
            Recipient = request.To ?? "unknown",
            Subject = request.Subject ?? "Sin asunto",
            Message = request.HtmlBody ?? request.TextBody ?? "",
            Metadata = new Dictionary<string, string>
            {
                ["MessageId"] = request.MessageId ?? Guid.NewGuid().ToString(),
                ["From"] = request.From ?? _settings.SenderEmail
            }
        };

        try
        {
            // DEBUG
            Console.WriteLine($"📝 Subject: '{request.Subject}'");
            Console.WriteLine($"📝 To: '{request.To}'");
            Console.WriteLine($"📝 MessageId: '{request.MessageId}'");

            // VALIDACIONES
            if (string.IsNullOrEmpty(request.Subject))
            {
                Console.WriteLine("⚠️ Subject vacío, usando predeterminado");
                request.Subject = "Notificación AgendaSalud";
            }

            if (string.IsNullOrEmpty(request.To))
            {
                Console.WriteLine("❌ Destinatario vacío");
                notificationLog.IsSuccess = false;
                notificationLog.ErrorMessage = "Destinatario vacío";
                await _notificationRepository.CreateAsync(notificationLog);
                return false;
            }

            if (string.IsNullOrEmpty(request.HtmlBody) && string.IsNullOrEmpty(request.TextBody))
            {
                Console.WriteLine("❌ Body vacío");
                notificationLog.IsSuccess = false;
                notificationLog.ErrorMessage = "Contenido del email vacío";
                await _notificationRepository.CreateAsync(notificationLog);
                return false;
            }

            // Payload para Maileroo
            var payload = new
            {
                from = new
                {
                    address = _settings.SenderEmail,
                    display_name = _settings.SenderName ?? "AgendaSalud Notificaciones"
                },
                to = new[] { new { address = request.To } },
                subject = request.Subject,
                html = request.HtmlBody,
                plain = request.TextBody ?? request.HtmlBody,
                tracking = true
            };

            var success = await SendToMailerooAsync(payload, notificationLog);
            return success;
        }
        catch (Exception ex)
        {
            return await HandleException(ex, notificationLog);
        }
    }

    // ============================================
    // MÉTODO SIMPLE - Un destinatario
    // ============================================
    public async Task<bool> SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true)
    {
        Console.WriteLine($"📧 SendEmailAsync - To: {to}, Subject: {subject}");

        var request = new EmailRequest
        {
            MessageId = Guid.NewGuid().ToString(),
            To = to,
            From = _settings.SenderEmail,
            Subject = subject,
            HtmlBody = isHtml ? body : null,
            TextBody = isHtml ? null : body
        };

        return await SendAsync(request);
    }

    // ============================================
    // MÉTODO MÚLTIPLES DESTINATARIOS
    // ============================================
    public async Task<bool> SendEmailAsync(
        IEnumerable<string> toList,
        string subject,
        string body,
        bool isHtml = true)
    {
        Console.WriteLine($"📧 SendEmailAsync - Múltiples: {toList.Count()}");

        var notificationLog = new NotificationLog
        {
            NotificationType = NotificationType.Email,
            Recipient = string.Join(", ", toList),
            Subject = subject ?? "Notificación AgendaSalud",
            Message = body ?? "",
            Metadata = new Dictionary<string, string>
            {
                ["MessageId"] = Guid.NewGuid().ToString(),
                ["RecipientsCount"] = toList.Count().ToString()
            }
        };

        try
        {
            if (!toList.Any())
            {
                Console.WriteLine("❌ Lista vacía");
                notificationLog.IsSuccess = false;
                notificationLog.ErrorMessage = "Lista de destinatarios vacía";
                await _notificationRepository.CreateAsync(notificationLog);
                return false;
            }

            var recipients = toList.Select(email => new { address = email }).ToArray();

            var payload = new
            {
                from = new
                {
                    address = _settings.SenderEmail,
                    display_name = _settings.SenderName ?? "AgendaSalud Notificaciones"
                },
                to = recipients,
                subject = subject ?? "Notificación AgendaSalud",
                html = isHtml ? body : null,
                plain = isHtml ? null : body,
                tracking = true
            };

            return await SendToMailerooAsync(payload, notificationLog);
        }
        catch (Exception ex)
        {
            return await HandleException(ex, notificationLog);
        }
    }

    // ============================================
    // MÉTODO CON CC Y BCC
    // ============================================
    public async Task<bool> SendEmailAsync(
        string to,
        string subject,
        string body,
        IEnumerable<string>? ccList = null,
        IEnumerable<string>? bccList = null,
        bool isHtml = true)
    {
        Console.WriteLine($"📧 SendEmailAsync con CC/BCC - To: {to}");

        var allRecipients = to;
        if (ccList != null && ccList.Any())
            allRecipients += $", CC: {string.Join(", ", ccList)}";
        if (bccList != null && bccList.Any())
            allRecipients += $", BCC: {string.Join(", ", bccList)}";

        var notificationLog = new NotificationLog
        {
            NotificationType = NotificationType.Email,
            Recipient = allRecipients,
            Subject = subject ?? "Notificación AgendaSalud",
            Message = body ?? "",
            Metadata = new Dictionary<string, string>
            {
                ["MessageId"] = Guid.NewGuid().ToString(),
                ["HasCC"] = (ccList?.Any() ?? false).ToString(),
                ["HasBCC"] = (bccList?.Any() ?? false).ToString()
            }
        };

        try
        {
            var payload = new Dictionary<string, object>
            {
                ["from"] = new
                {
                    address = _settings.SenderEmail,
                    display_name = _settings.SenderName ?? "AgendaSalud Notificaciones"
                },
                ["to"] = new[] { new { address = to } },
                ["subject"] = subject ?? "Notificación AgendaSalud",
                ["tracking"] = true
            };

            if (isHtml)
                payload["html"] = body;
            else
                payload["plain"] = body;

            if (ccList != null && ccList.Any())
                payload["cc"] = ccList.Select(e => new { address = e }).ToArray();

            if (bccList != null && bccList.Any())
                payload["bcc"] = bccList.Select(e => new { address = e }).ToArray();

            return await SendToMailerooAsync(payload, notificationLog);
        }
        catch (Exception ex)
        {
            return await HandleException(ex, notificationLog);
        }
    }

    // ============================================
    // MÉTODO CON ADJUNTO
    // ============================================
    public async Task<bool> SendEmailWithAttachmentAsync(
        string to,
        string subject,
        string body,
        string attachmentPath,
        string? attachmentName = null,
        bool isHtml = true)
    {
        Console.WriteLine($"📧 Email con adjunto - To: {to}, File: {attachmentPath}");

        var notificationLog = new NotificationLog
        {
            NotificationType = NotificationType.Email,
            Recipient = to,
            Subject = subject ?? "Notificación AgendaSalud",
            Message = body ?? "",
            Metadata = new Dictionary<string, string>
            {
                ["MessageId"] = Guid.NewGuid().ToString(),
                ["HasAttachment"] = "true",
                ["AttachmentPath"] = attachmentPath
            }
        };

        try
        {
            if (!File.Exists(attachmentPath))
            {
                Console.WriteLine($"❌ Archivo no encontrado: {attachmentPath}");
                notificationLog.IsSuccess = false;
                notificationLog.ErrorMessage = $"Archivo no encontrado: {attachmentPath}";
                await _notificationRepository.CreateAsync(notificationLog);
                return false;
            }

            var fileBytes = await File.ReadAllBytesAsync(attachmentPath);
            var base64Content = Convert.ToBase64String(fileBytes);
            var fileName = attachmentName ?? Path.GetFileName(attachmentPath);
            var mimeType = GetMimeType(attachmentPath);

            Console.WriteLine($"📎 Adjunto: {fileName} ({fileBytes.Length} bytes)");

            var payload = new
            {
                from = new
                {
                    address = _settings.SenderEmail,
                    display_name = _settings.SenderName ?? "AgendaSalud Notificaciones"
                },
                to = new[] { new { address = to } },
                subject = subject ?? "Notificación AgendaSalud",
                html = isHtml ? body : null,
                plain = isHtml ? null : body,
                tracking = true,
                attachments = new[]
                {
                    new
                    {
                        filename = fileName,
                        content = base64Content,
                        content_type = mimeType,
                        encoding = "base64"
                    }
                }
            };

            return await SendToMailerooAsync(payload, notificationLog);
        }
        catch (Exception ex)
        {
            return await HandleException(ex, notificationLog);
        }
    }

    // ============================================
    // MÉTODOS PRIVADOS
    // ============================================

    private async Task<bool> SendToMailerooAsync(
        object payload,
        NotificationLog notificationLog)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.SenderPassword);

            Console.WriteLine($"🔄 Enviando email via Maileroo API...");
            Console.WriteLine($"📧 De: {_settings.SenderEmail}");
            Console.WriteLine($"📬 Para: {notificationLog.Recipient}");
            Console.WriteLine($"📋 Subject: '{notificationLog.Subject}'");

            var response = await _httpClient.PostAsync(
                "https://smtp.maileroo.com/api/v2/emails",
                content);

            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"📋 Status: {response.StatusCode}");
            Console.WriteLine($"📄 Response: {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Email enviado correctamente");

                notificationLog.IsSuccess = true;
                notificationLog.Metadata ??= new Dictionary<string, string>();
                notificationLog.Metadata["StatusCode"] = response.StatusCode.ToString();
                notificationLog.Metadata["Response"] = responseBody;

                await _notificationRepository.CreateAsync(notificationLog);
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Error: {response.StatusCode}");

                notificationLog.IsSuccess = false;
                notificationLog.ErrorMessage = $"{response.StatusCode} - {responseBody}";
                notificationLog.Metadata ??= new Dictionary<string, string>();
                notificationLog.Metadata["StatusCode"] = response.StatusCode.ToString();
                notificationLog.Metadata["Response"] = responseBody;

                await _notificationRepository.CreateAsync(notificationLog);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en SendToMailerooAsync: {ex.Message}");
            throw;
        }
    }

    private async Task<bool> HandleException(Exception ex, NotificationLog notificationLog)
    {
        Console.WriteLine($"❌ Error: {ex.GetType().Name} - {ex.Message}");

        notificationLog.IsSuccess = false;
        notificationLog.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
        notificationLog.Metadata ??= new Dictionary<string, string>();
        notificationLog.Metadata["ExceptionType"] = ex.GetType().Name;
        notificationLog.Metadata["StackTrace"] = ex.StackTrace ?? "N/A";

        await _notificationRepository.CreateAsync(notificationLog);
        return false;
    }

    private string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}