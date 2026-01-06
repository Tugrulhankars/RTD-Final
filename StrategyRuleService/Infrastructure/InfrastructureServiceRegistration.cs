using Infrastructure.Services.Grpc;
using Infrastructure.Services.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
namespace Infrastructure;
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGrpcServices(configuration);
        services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();
        return services;
    }
}
