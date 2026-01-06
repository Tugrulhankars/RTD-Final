namespace PaymentService.Services;
public interface IRabbitMQPublisher
{
    Task PublishAsync<T>(T message, string queueName) where T : class;
}
