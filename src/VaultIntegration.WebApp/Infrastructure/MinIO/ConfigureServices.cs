namespace VaultIntegration.WebApp.Configs;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Minio;

public static class ConfigureServices
{
    public static void AddMinioService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RemoteFileConfig>(configuration.GetSection(nameof(RemoteFileConfig)));

        // Bind RemoteFileConfig from appsettings.json or environment variables or other configuration sources
        var remoteFileConfig = new RemoteFileConfig();
        
        configuration.GetSection(nameof(RemoteFileConfig)).Bind(remoteFileConfig);

        // Validate required MinIO fields
        if (string.IsNullOrWhiteSpace(remoteFileConfig.Host) ||
            string.IsNullOrWhiteSpace(remoteFileConfig.UserName) ||
            string.IsNullOrWhiteSpace(remoteFileConfig.Password))
        {
            throw new ArgumentException("MinIO configuration is invalid. Please check your settings.");
        }

        // Register MinIO client
        services.AddSingleton<IMinioClient>(sp =>
        {
            return new MinioClient()
                .WithEndpoint(remoteFileConfig.Host, remoteFileConfig.Port)
                .WithCredentials(remoteFileConfig.UserName, remoteFileConfig.Password)
                .Build();
        });
    }
}
