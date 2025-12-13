using Application.Services;
using Microsoft.EntityFrameworkCore;
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
    public static IServiceCollection AddPersistanceServices(this IServiceCollection services)
    {
        // Burada DbContext ve repository'ler eklenir
        // services.AddDbContext<AppDbContext>(options => ...);
        // services.AddScoped<IYourRepository, YourRepository>();
        // DbContext'i scoped olarak kaydet
        services.AddDbContext<DatabaseContext.Context>(opt =>
        {
            opt.UseSqlServer("server=MetropolTilkisi;database=RtdStartegyRule-Service;integrated security=SSPI;persist security info=False;Trusted_Connection=True;Encrypt=false");
        });
        
        // Repository'leri scoped olarak kaydet
        services.AddScoped<IStrategyRepository, StrategyRepository>();
        services.AddScoped<IStrategyEventRepository, StrategyEventRepository>();
        return services;
    }
}
