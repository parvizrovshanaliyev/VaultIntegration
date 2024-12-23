using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Vault;

namespace VaultIntegration.WebApp.Infrastructure.Caching;

public static class ConfigureServices
{
    public static IServiceCollection AddCachingService(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind Redis configuration from appsettings.json or environment variables
        var redisConnectionStr = configuration.GetRedisConnectionStringFromVault();

        // Register Redis connection multiplexer as a singleton
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionStr));


        return services;
    }
}