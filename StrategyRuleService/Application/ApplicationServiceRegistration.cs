using Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StrategyRuleService.Worker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
namespace Application;
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<FlowchartLogger>();
        services.AddSingleton<INRulesService, NRulesService>();
        services.AddHostedService<StrategyProcessingHostedService>();
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        ActivitySource activitySource=new ActivitySource("StrategyRuleService");
        services.AddOpenTelemetry().WithTracing(opt =>
        {
            opt.AddSource(activitySource.Name)
            .ConfigureResource(resource =>
            {
                resource.AddService("StrategyRuleService");
            });
            opt.AddAspNetCoreInstrumentation(asp =>
            {
                asp.Filter = (context) =>
                {
                    if (!string.IsNullOrEmpty(context.Request.Path.Value))
                    {
                        return context.Request.Path.Value.Contains("api",StringComparison.InvariantCulture);
                    }
                    return false;
                };
            });
            opt.AddEntityFrameworkCoreInstrumentation(efOptions =>
            {
                efOptions.SetDbStatementForText = true;
                efOptions.SetDbStatementForStoredProcedure = true;
            });
            opt.AddConsoleExporter();
            opt.AddOtlpExporter();
        });
        return services;
    }
}
