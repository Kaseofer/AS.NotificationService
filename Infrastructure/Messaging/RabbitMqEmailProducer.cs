using AS.NotificationService.Config;
using AS.NotificationService.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace AS.NotificationService.Queue
{
    public class RabbitMqEmailProducer : IEmailQueueProducer
    {
        private readonly IChannel _channel;
        private readonly string _queueName;
        protected readonly RabbitMqSettings _rabbitMqSettings;

        public RabbitMqEmailProducer(IConnection connection, IOptions<RabbitMqSettings> options)
        {
            _rabbitMqSettings = options.Value;

            _queueName = _rabbitMqSettings.QueueName;

            // Crear canal de forma asíncrona
            _channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Declarar la cola
            _channel.QueueDeclareAsync(
                queue: _rabbitMqSettings.QueueName,
                durable: _rabbitMqSettings.Durable,
                exclusive: _rabbitMqSettings.Exclusive,
                autoDelete: _rabbitMqSettings.AutoDelete,
                arguments: null
            ).GetAwaiter().GetResult();
        }

        public async Task EnqueueAsync(EmailRequestDto request)
        {
            var factory = new ConnectionFactory()
            {
                Uri = new Uri("amqp://OgRyxz4MMKMDpmOM:07-y_HRLYfH.V.UyBdYIQ0gejb-kkXm~@caboose.proxy.rlwy.net:25209")
            };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            // Aseguramos la cola
            await channel.QueueDeclareAsync(
                queue: _rabbitMqSettings.QueueName,
                durable: _rabbitMqSettings.Durable,
                exclusive: _rabbitMqSettings.Exclusive,
                autoDelete: _rabbitMqSettings.AutoDelete,
                arguments: null
            );

            var emailStr = System.Text.Json.JsonSerializer.Serialize(request);

            // Publicamos un mensaje
            var body = Encoding.UTF8.GetBytes(emailStr);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = Guid.NewGuid().ToString()
            };

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: _rabbitMqSettings.QueueName,
                mandatory: false,
                basicProperties: props,
                body: body
            );

            Console.WriteLine($"📤 Enqueued email to {request.To} with MessageId: {props.MessageId}");
        }
    }
}
