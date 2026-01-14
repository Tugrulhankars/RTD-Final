using Domain.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            string exchange = "notification.exchange.direct";
            string routingKey = queueName switch
            {
                "strategy-notifications" => "notification.strategy.notification.key",
                _ => queueName
            };
            await channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: "direct",
                durable: true,
                autoDelete: false,
                arguments: null
            );
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            if (message is StrategyNotificationEvent strategyEvent)
            {
                if (strategyEvent.StrategyId == 0)
                {
                    _logger.LogError("❌ StrategyNotificationEvent StrategyId is 0! Cannot send to RabbitMQ.");
                    throw new InvalidOperationException("StrategyNotificationEvent StrategyId cannot be 0");
                }
                if (strategyEvent.UserId == 0)
                {
                    _logger.LogError("❌ StrategyNotificationEvent UserId is 0! Cannot send to RabbitMQ. StrategyId={StrategyId}", 
                        strategyEvent.StrategyId);
                    throw new InvalidOperationException("StrategyNotificationEvent UserId cannot be 0");
                }
                if (string.IsNullOrEmpty(strategyEvent.Action))
                {
                    _logger.LogError("❌ StrategyNotificationEvent Action is null or empty! Cannot send to RabbitMQ. StrategyId={StrategyId}, UserId={UserId}", 
                        strategyEvent.StrategyId, strategyEvent.UserId);
                    throw new InvalidOperationException("StrategyNotificationEvent Action cannot be null or empty");
                }
                // Ensure UserEmail is always set before sending
                if (string.IsNullOrEmpty(strategyEvent.UserEmail))
                {
                    _logger.LogWarning("⚠️ StrategyNotificationEvent UserEmail is null or empty before sending. Setting fallback email. StrategyId={StrategyId}, UserId={UserId}", 
                        strategyEvent.StrategyId, strategyEvent.UserId);
                    strategyEvent.UserEmail = $"user{strategyEvent.UserId}@example.com";
                }
                _logger.LogInformation("📤 Sending StrategyNotificationEvent to RabbitMQ: StrategyId={StrategyId}, UserId={UserId}, UserEmail={UserEmail}, Action={Action}, StrategyName={StrategyName}, StockSymbol={StockSymbol}, Status={Status}, CurrentPrice={CurrentPrice}, Timestamp={Timestamp}", 
                    strategyEvent.StrategyId,
                    strategyEvent.UserId,
                    strategyEvent.UserEmail,
                    strategyEvent.Action ?? "NULL",
                    strategyEvent.StrategyName ?? "NULL",
                    strategyEvent.StockSymbol ?? "NULL",
                    strategyEvent.Status ?? "NULL",
                    strategyEvent.CurrentPrice,
                    strategyEvent.Timestamp);
            }
            var messageJson = System.Text.Json.JsonSerializer.Serialize(message, jsonOptions);
            var bodyBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message, jsonOptions);
            var body = new ReadOnlyMemory<byte>(bodyBytes);
            _logger.LogInformation("📤 RabbitMQ Message JSON Content (Length={Length} bytes): {MessageJson}", bodyBytes.Length, messageJson);
            var properties = new BasicProperties
            {
                Persistent = true
            };
            if (queueName == "strategy-notifications")
            {
                properties.Headers = new Dictionary<string, object?>
                {
                    { "__TypeId__", "org.rtd.rtdnotificationservice.event.StrategyNotificationEvent" }
                };
                _logger.LogDebug("RabbitMQ Header eklendi: __TypeId__={TypeId}", 
                    "org.rtd.rtdnotificationservice.event.StrategyNotificationEvent");
            }
            _logger.LogInformation("RabbitMQ'ya mesaj gönderiliyor: Exchange={Exchange}, RoutingKey={RoutingKey}, MessageType={MessageType}, MessageSize={MessageSize} bytes", 
                exchange, routingKey, typeof(T).Name, bodyBytes.Length);
            _logger.LogDebug("RabbitMQ mesaj içeriği: {MessageContent}", messageJson);
            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body
            );
            _logger.LogInformation("✓ RabbitMQ mesajı başarıyla gönderildi: Exchange={Exchange}, RoutingKey={RoutingKey}, MessageType={MessageType}, MessageSize={MessageSize} bytes", 
                exchange, routingKey, typeof(T).Name, body.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ mesajı gönderilirken hata oluştu: Queue={QueueName}, MessageType={MessageType}", 
                queueName, typeof(T).Name);
            throw;
        }
    }
}
