using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using KoiFengShuiSystem.Shared.Infrastructure.Background;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;

namespace KoiFengShuiSystem.Shared.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<KoiFengShuiContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(KoiFengShuiContext).Assembly.GetName().Name)));

        // Traffic logging: one singleton buffer shared by the middleware-facing
        // ITrafficSink and the hosted flush loop.
        services.Configure<TrafficSinkOptions>(configuration.GetSection(TrafficSinkOptions.SectionName));
        services.AddSingleton<ITrafficSink, BackgroundTrafficSink>();
        services.AddHostedService(sp => (BackgroundTrafficSink)sp.GetRequiredService<ITrafficSink>());

        return services;
    }
}
