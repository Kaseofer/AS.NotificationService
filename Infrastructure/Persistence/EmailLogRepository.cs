using AS.NotificationService.Persistence.Interface;
using Npgsql;
using System.Net.Mail;
using System.Text.Json;

namespace Postino.EmailService.Persistence
{
    public class EmailLogRepository: IEmailLogRepository
    {
        protected readonly string _connectionString;
       public EmailLogRepository(IConfiguration configuration) 
        {

            _connectionString = configuration.GetConnectionString("AuditDb")!;
        }

        public async Task LogAsync(string entityId, string eventType, string to, object payload)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                   INSERT INTO audit_log (entity_id, event_type, payload, destination)
                   VALUES (@e, @t, @p::jsonb, @d)";
                cmd.Parameters.AddWithValue("e", entityId);
                cmd.Parameters.AddWithValue("t", eventType);
                cmd.Parameters.AddWithValue("p", JsonSerializer.Serialize(payload));
                cmd.Parameters.AddWithValue("d", to);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
           
        }
    }
}