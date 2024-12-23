using VaultIntegration.WebApp.Configs;
using VaultIntegration.WebApp.Infrastructure.Caching;

namespace VaultIntegration.WebApp.Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMinioService(configuration);
            services.AddCachingService(configuration);

            return services;
        }
    }
}