namespace Infrastructure.Services.RabbitMQ;
public interface IRabbitMQPublisher
{
    Task PublishAsync<T>(T message, string queueName) where T : class;
}
