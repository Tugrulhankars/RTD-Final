using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
namespace PaymentService.Services;
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
            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };
            var messageJson = JsonConvert.SerializeObject(message, settings);
            var body = Encoding.UTF8.GetBytes(messageJson);
            var typeMapping = typeof(T).Name switch
            {
                "PaymentSuccessEvent" => "org.rtd.rtdpaymentservice.events.PaymentSuccessEvent",
                "PaymentFailedEvent" => "org.rtd.rtdpaymentservice.events.PaymentFailedEvent",
                _ => typeof(T).FullName ?? typeof(T).Name
            };
            _logger.LogWarning("RabbitMQ.Client 7.x properties desteği olmadığı için headers gönderilemiyor. TypeMapping={TypeMapping}. Java tarafı için RabbitMQ.Client 6.x kullanılması önerilir.", typeMapping);
            _logger.LogInformation("RabbitMQ'ya payment event gönderiliyor: Queue={QueueName}, MessageType={MessageType}, TypeMapping={TypeMapping}, MessageSize={MessageSize} bytes", 
                queueName, typeof(T).Name, typeMapping, body.Length);
            _logger.LogInformation("RabbitMQ payment event JSON içeriği: {MessageContent}", messageJson);
            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                body: body
            );
            _logger.LogInformation("✓ RabbitMQ payment event'i başarıyla gönderildi: Queue={QueueName}, MessageType={MessageType}, MessageSize={MessageSize} bytes", 
                queueName, typeof(T).Name, body.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ payment event'i gönderilirken hata oluştu: Queue={QueueName}, MessageType={MessageType}", 
                queueName, typeof(T).Name);
            throw;
        }
    }
}
