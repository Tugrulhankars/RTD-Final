using Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistance.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Persistance;
public static class PersistanceServiceRegistration
{
    public static IServiceCollection AddPersistanceServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddDbContext<DatabaseContext.Context>(opt =>
        {
            var connectionString = configuration?.GetConnectionString("DefaultConnection")
                ?? configuration?["ConnectionStrings:DefaultConnection"]
                ?? "Server=localhost;Database=RtdStartegyRule-Service;User Id=sa;Password=20002002.;Encrypt=False;TrustServerCertificate=True;";
            opt.UseSqlServer(connectionString);
        });
        services.AddScoped<IStrategyRepository, StrategyRepository>();
        services.AddScoped<IStrategyEventRepository, StrategyEventRepository>();
        return services;
    }
}
