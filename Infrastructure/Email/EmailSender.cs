using AS.NotificationService.Application.Service.Interface;
using AS.NotificationService.Domain.Models;
using AS.NotificationService.Infrastructure.Email.Settings;
using AS.NotificationService.Persistence.Interface;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AS.NotificationService.Infrastructure.Email
{
    public class EmailSender : IEmailSender
    {
        protected readonly EmailSettings _settings;
        protected readonly IEmailLogRepository _emailRepository;
        private readonly HttpClient _httpClient;

        public EmailSender(IOptions<EmailSettings> options, IEmailLogRepository repo, HttpClient httpClient)
        {
            _settings = options.Value;
            _emailRepository = repo;
            _httpClient = httpClient;
        }

        // VERSIÓN CON MAILEROO API (sin SMTP) + DEBUG Y VALIDACIONES
        public async Task<bool> SendAsync(EmailRequest request)
        {
            try
            {
                // DEBUG: Verificar qué datos están llegando
                Console.WriteLine($"📝 Subject recibido: '{request.Subject}'");
                Console.WriteLine($"📝 To recibido: '{request.To}'");
                Console.WriteLine($"📝 From recibido: '{request.From}'");
                Console.WriteLine($"📝 HtmlBody: '{request.HtmlBody}'");
                Console.WriteLine($"📝 TextBody: '{request.TextBody}'");
                Console.WriteLine($"📝 MessageId: '{request.MessageId}'");
                Console.WriteLine($"📝 ReplyTo: '{request.ReplyTo}'");
                Console.WriteLine($"📝 Headers count: {request.Headers?.Count ?? 0}");

                // VALIDACIONES
                if (string.IsNullOrEmpty(request.Subject))
                {
                    Console.WriteLine("❌ Subject está vacío, usando subject por defecto");
                    request.Subject = "Notificación AgendaSalud";
                }

                if (string.IsNullOrEmpty(request.To))
                {
                    Console.WriteLine("❌ Destinatario está vacío");
                    return false;
                }

                if (string.IsNullOrEmpty(request.HtmlBody) && string.IsNullOrEmpty(request.TextBody))
                {
                    Console.WriteLine("❌ Tanto HtmlBody como TextBody están vacíos");
                    return false;
                }

                // Payload CORRECTO según documentación oficial de Maileroo
                var payload = new
                {
                    from = new
                    {
                        address = _settings.SenderEmail,
                        display_name = "AgendaSalud Notificaciones"
                    },
                    to = new[]
                    {
                        new
                        {
                            address = request.To
                        }
                    },
                    subject = request.Subject ?? "Sin asunto",
                    html = request.HtmlBody,
                    plain = request.TextBody ?? request.HtmlBody ?? "Contenido no disponible",
                    tracking = true
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true, // Para mejor lectura en logs
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // No escapar caracteres especiales
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Limpiar headers previos y agregar autenticación
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.SenderPassword);

                // LOGGING DETALLADO
                Console.WriteLine($"🔄 Enviando email via API...");
                Console.WriteLine($"📧 Endpoint: https://smtp.maileroo.com/api/v2/emails");
                Console.WriteLine($"🔐 De: {_settings.SenderEmail}");
                Console.WriteLine($"📬 Para: {request.To}");
                Console.WriteLine($"📋 Subject final: '{payload.subject}'");
                Console.WriteLine($"🔑 API Key (primeros 8 chars): {_settings.SenderPassword.Substring(0, 8)}...");
                Console.WriteLine($"📄 Payload completo:");
                Console.WriteLine(json);

                // Enviar via API - ENDPOINT CORRECTO
                var response = await _httpClient.PostAsync("https://smtp.maileroo.com/api/v2/emails", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"📋 HTTP Status: {response.StatusCode}");
                Console.WriteLine($"📄 Response Body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Correo enviado correctamente via API.");

                    // Log exitoso con información de la respuesta
                    var successInfo = new
                    {
                        Method = "API",
                        StatusCode = response.StatusCode.ToString(),
                        Response = responseBody,
                        Timestamp = DateTime.UtcNow,
                        Subject = request.Subject,
                        Recipient = request.To
                    };

                    await _emailRepository.LogAsync(request.MessageId, "Envio Exitoso (API)", request.To, successInfo);
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Error API: {response.StatusCode} - {response.ReasonPhrase}");

                    var errorInfo = new
                    {
                        Method = "API",
                        StatusCode = response.StatusCode.ToString(),
                        Response = responseBody,
                        ReasonPhrase = response.ReasonPhrase,
                        Timestamp = DateTime.UtcNow,
                        Subject = request.Subject,
                        Recipient = request.To
                    };

                    await _emailRepository.LogAsync(request.MessageId, $"Error API: {response.StatusCode} - {response.ReasonPhrase}", request.To, errorInfo);
                    return false;
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"❌ Error HTTP: {httpEx.Message}");
                Console.WriteLine($"🔍 Inner Exception: {httpEx.InnerException?.Message}");

                var errorInfo = new
                {
                    Type = "HttpRequestException",
                    Message = httpEx.Message,
                    InnerMessage = httpEx.InnerException?.Message ?? "N/A",
                    Timestamp = DateTime.UtcNow
                };

                await _emailRepository.LogAsync(request.MessageId, "Error HTTP", request.To, errorInfo);
                return false;
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                Console.WriteLine($"❌ Timeout al enviar email: {tcEx.Message}");

                var errorInfo = new
                {
                    Type = "TimeoutException",
                    Message = "La solicitud tardó demasiado tiempo",
                    Timestamp = DateTime.UtcNow
                };

                await _emailRepository.LogAsync(request.MessageId, "Timeout", request.To, errorInfo);
                return false;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"❌ Error de serialización JSON: {jsonEx.Message}");

                var errorInfo = new
                {
                    Type = "JsonException",
                    Message = jsonEx.Message,
                    Timestamp = DateTime.UtcNow
                };

                await _emailRepository.LogAsync(request.MessageId, "Error JSON", request.To, errorInfo);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error general: {ex.Message}");
                Console.WriteLine($"🔍 Tipo: {ex.GetType().Name}");
                Console.WriteLine($"🔍 Stack Trace: {ex.StackTrace}");

                var errorInfo = new
                {
                    Type = ex.GetType().Name,
                    Message = ex.Message,
                    Source = ex.Source ?? "N/A",
                    Timestamp = DateTime.UtcNow
                };

                await _emailRepository.LogAsync(request.MessageId, "Error General", request.To, errorInfo);
                return false;
            }
        }
    }
}