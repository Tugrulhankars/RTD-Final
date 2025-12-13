
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace AccountService.Services;

public class RabbitMqPublisher : IRabbitMQPublisher
{
    private readonly IConfiguration _configuration;

    public RabbitMqPublisher(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task Producer<T>(T message, string queueName)
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
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
            );

        var messageJson = JsonConvert.SerializeObject(message);
        var body=Encoding.UTF8.GetBytes(messageJson);

        await channel.BasicPublishAsync(
            exchange:"",
            routingKey:queueName,
            body: body
            );

    }
}
