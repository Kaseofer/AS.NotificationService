
// ============================================
// Infrastructure/Messaging/Consumers/NotificationQueueConsumer.cs
// ============================================
namespace AS.NotificationService.Infrastructure.Messaging.Consumers
{
    using AS.NotificationService.Domain.Events;
    using AS.NotificationService.Infrastructure.Messaging.Handlers;
    using AS.NotificationService.Infrastructure.Messaging.Settings;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    using System.Text;
    using System.Text.Json;

    public class NotificationQueueConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationQueueConsumer> _logger;

        public NotificationQueueConsumer(
            IServiceProvider serviceProvider,
            IOptions<RabbitMQSettings> settings,
            ILogger<NotificationQueueConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            var config = settings.Value;
            _queueName = config.QueueName;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = config.HostName,
                    Port = config.Port,
                    UserName = config.UserName,
                    Password = config.Password,
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Configurar prefetch - procesar 1 mensaje a la vez
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _logger.LogInformation($"🔌 Connected to RabbitMQ - Queue: {_queueName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to connect to RabbitMQ");
                throw;
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;

                _logger.LogInformation($"📨 Message received - RoutingKey: {routingKey}");

                try
                {
                    // Deserializar el evento
                    var notification = JsonSerializer.Deserialize<NotificationEvent>(message, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (notification == null)
                    {
                        _logger.LogError("Failed to deserialize notification");
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
                    }

                    // Procesar el mensaje
                    await ProcessNotification(notification);

                    // ACK - Confirmar procesamiento exitoso
                    _channel.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation($"✅ Message processed successfully - NotificationId: {notification.NotificationId}");
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "❌ JSON deserialization error");
                    // No requeue si hay error de deserialización
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing message");
                    // NACK - Rechazar y NO reencolar (ir a DLX si está configurado)
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(
                queue: _queueName,
                autoAck: false, // Manual ACK
                consumer: consumer
            );

            _logger.LogInformation($"✅ Started consuming from queue: {_queueName}");

            return Task.CompletedTask;
        }

        private async Task ProcessNotification(NotificationEvent notification)
        {
            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<NotificationEventHandler>();
            await handler.HandleAsync(notification);
        }

        public override void Dispose()
        {
            _logger.LogInformation("Disposing NotificationQueueConsumer");
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}