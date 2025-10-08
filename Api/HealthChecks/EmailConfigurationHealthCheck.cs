using AS.NotificationService.Infrastructure.Email.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AS.NotificationService.HealthChecks
{
    public class EmailConfigurationHealthCheck : IHealthCheck
    {
        private readonly EmailSettings _emailSettings;
        private readonly HttpClient _httpClient;

        public EmailConfigurationHealthCheck(IOptions<EmailSettings> emailSettings, HttpClient httpClient)
        {
            _emailSettings = emailSettings.Value;
            _httpClient = httpClient;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>();

                // Verificar configuración básica
                var configIssues = new List<string>();

                if (string.IsNullOrEmpty(_emailSettings.SmtpServer))
                    configIssues.Add("SMTP Server not configured");

                if (string.IsNullOrEmpty(_emailSettings.SenderEmail))
                    configIssues.Add("Sender Email not configured");

                if (string.IsNullOrEmpty(_emailSettings.SenderPassword))
                    configIssues.Add("Sender Password/API Key not configured");

                data["smtp_server"] = _emailSettings.SmtpServer ?? "Not configured";
                data["smtp_port"] = _emailSettings.SmtpPort;
                data["sender_email"] = MaskEmail(_emailSettings.SenderEmail);
                data["enable_ssl"] = _emailSettings.EnableSsl;
                data["is_body_html"] = _emailSettings.IsBodyHtml;

                if (configIssues.Any())
                {
                    return HealthCheckResult.Unhealthy(
                        "Email configuration is incomplete",
                        data: data);
                }

                // Verificar conectividad con Maileroo API (sin enviar email)
                try
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("X-API-Key", _emailSettings.SenderPassword);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    var response = await _httpClient.GetAsync("https://smtp.maileroo.com", cts.Token);

                    data["maileroo_connectivity"] = response.IsSuccessStatusCode ? "OK" : $"Error: {response.StatusCode}";
                    data["api_key_masked"] = MaskApiKey(_emailSettings.SenderPassword);

                    if (!response.IsSuccessStatusCode)
                    {
                        return HealthCheckResult.Degraded(
                            "Email service configuration is valid but API connectivity failed",
                            data: data);
                    }
                }
                catch (Exception ex)
                {
                    data["maileroo_connectivity"] = $"Connection failed: {ex.Message}";
                    return HealthCheckResult.Degraded(
                        "Email service configuration is valid but connectivity check failed",
                        data: data);
                }

                return HealthCheckResult.Healthy(
                    "Email service is properly configured and operational",
                    data: data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    $"Email health check failed: {ex.Message}",
                    exception: ex);
            }
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "Not configured";

            var parts = email.Split('@');
            if (parts.Length != 2) return "Invalid format";

            var username = parts[0];
            var domain = parts[1];

            var maskedUsername = username.Length <= 3
                ? new string('*', username.Length)
                : username.Substring(0, 2) + new string('*', username.Length - 2);

            return $"{maskedUsername}@{domain}";
        }

        private static string MaskApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return "Not configured";
            return apiKey.Length <= 8
                ? new string('*', apiKey.Length)
                : apiKey.Substring(0, 4) + new string('*', apiKey.Length - 8) + apiKey.Substring(apiKey.Length - 4);
        }
    }
}