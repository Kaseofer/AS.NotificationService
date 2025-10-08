using AS.NotificationService.Config;
using AS.NotificationService.Models;
using AS.NotificationService.Service.Interface;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace AS.NotificationService.Workers
{
    public class EmailWorker : BackgroundService
    {
        private  IConnection _connection;
        private readonly IServiceProvider _serviceProvider;
        private IChannel _channel;

        private readonly ILogger<EmailWorker> _logger;
        private readonly RabbitMqSettings _settings;

        public EmailWorker(ILogger<EmailWorker> logger, IServiceProvider serviceProvider, IOptions<RabbitMqSettings> options)
        {

            _serviceProvider = serviceProvider;
            _logger = logger;
            _settings = options.Value;

            


        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory { Uri = new Uri(_settings.Uri) };
            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(null, stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: _settings.QueueName,
                durable: _settings.Durable,
                exclusive: _settings.Exclusive,
                autoDelete: _settings.AutoDelete,
                arguments: null
            );

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) => await HandleMessageAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(
                queue: _settings.QueueName,
                autoAck: false,
                consumer: consumer
            );

            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(1000, stoppingToken);
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            if (string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine("⚠️ Mensaje vacío. Se descarta.");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }

            try
            {
                var request = JsonSerializer.Deserialize<EmailRequestDto>(message);
                request!.MessageId = ea.BasicProperties?.MessageId ?? Guid.NewGuid().ToString();

                if (string.IsNullOrWhiteSpace(request.To))
                {
                    Console.WriteLine($"⚠️ Campo 'To' vacío en {request.MessageId}. Se descarta.");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                Console.WriteLine($"📥 Procesando email a {request.To} con ID: {request.MessageId}");
                await emailSender.SendAsync(request);

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                var isRecoverable = !(ex is JsonException);
                Console.WriteLine($"❌ Error en {ea.BasicProperties?.MessageId}: {ex.Message} | Requeue: {isRecoverable}");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: isRecoverable);
            }
        }

    }
}
