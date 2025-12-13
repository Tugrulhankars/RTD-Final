using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace PortfolioService.Services;

public class KafkaConsumerService<T> 
    where T : class
{
    private readonly IConfiguration _configuration;

    public KafkaConsumerService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<T> Consume(string topicName)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "PortfolioService",
            AutoOffsetReset = AutoOffsetReset.Latest
        };

        var consumer = new ConsumerBuilder<int, T>(config)
            .SetKeyDeserializer(new KeySerializer<int>())
            .SetValueDeserializer(new ValueSerializer<T>())
            .Build();

        consumer.Subscribe(topicName);

        while (true)
        {
            var consumerResult = consumer.Consume();
            if (consumerResult == null)
            {
                Console.WriteLine($"mesaj: {consumerResult.Message.Value}");
                T @event = consumerResult.Message.Value;
                return @event;
            }
        }
    }
}
