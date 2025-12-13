namespace AccountService.Services;

public interface IRabbitMQPublisher
{
    Task Producer<T>(T message,string queueName);
}
