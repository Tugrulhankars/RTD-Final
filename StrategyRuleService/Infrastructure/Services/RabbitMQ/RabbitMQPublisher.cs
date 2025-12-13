using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace Infrastructure.Services.RabbitMQ;

public class RabbitMQPublisher : IRabbitMQPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMQPublisher> _logger;

    public RabbitMQPublisher(IConfiguration configuration, ILogger<RabbitMQPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, string queueName) where T : class
    {
        try
        {
            var rabbitMqUri = _configuration["RabbitMQ:Uri"] ?? "amqps://okzwdbrz:AmGKgw5DTXuIAjOraNCNzFiqI5_lhV-s@kebnekaise.lmq.cloudamqp.com/okzwdbrz";
            var factory = new ConnectionFactory
            {
                Uri = new Uri(rabbitMqUri)
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true, // Mesajların kalıcı olması için
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var messageJson = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(messageJson);

            _logger.LogInformation("RabbitMQ'ya mesaj gönderiliyor: Queue={QueueName}, MessageType={MessageType}, MessageSize={MessageSize} bytes", 
                queueName, typeof(T).Name, body.Length);
            
            _logger.LogDebug("RabbitMQ mesaj içeriği: {MessageContent}", messageJson);

            // RabbitMQ.Client 7.x API'si - sadece 3 parametre alıyor
            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                body: body
            );

            _logger.LogInformation("✓ RabbitMQ mesajı başarıyla gönderildi: Queue={QueueName}, MessageType={MessageType}, MessageSize={MessageSize} bytes", 
                queueName, typeof(T).Name, body.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ mesajı gönderilirken hata oluştu: Queue={QueueName}, MessageType={MessageType}", 
                queueName, typeof(T).Name);
            throw; // Exception handling middleware'e iletmek için
        }
    }
}

