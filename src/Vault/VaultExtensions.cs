using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vault.Models;

namespace Vault;

/// <summary>
/// Provides extension methods for configuring Vault and retrieving secrets from the configuration.
/// </summary>
public static class VaultExtensions
{
    public static IServiceCollection ConfigureWithVault<TConfig>(
            this IServiceCollection services,
            IConfiguration configuration) where TConfig : class, new()
        {
            var config = new TConfig();
            configuration.GetSection(typeof(TConfig).Name).Bind(config);
    
            if (config is IKeyMappings keyMappingsProvider)
            {
                var mappings = keyMappingsProvider.GetKeyMappings();
    
                foreach (var mapping in mappings)
                {
                    var vaultValue = configuration.GetVaultVariable(mapping.Key);
                    
                    if (!string.IsNullOrWhiteSpace(vaultValue))
                    {
                        var property = typeof(TConfig).GetProperty(mapping.Value);
                        if (property != null && property.CanWrite)
                        {
                            property.SetValue(config, Convert.ChangeType(vaultValue, property.PropertyType));
                        }
                    }
                }
            }
    
            services.Configure<TConfig>(_ =>
            {
                foreach (var property in typeof(TConfig).GetProperties())
                {
                    property.SetValue(_, property.GetValue(config));
                }
            });
    
            return services;
        }
    
    /// <summary>
    /// Determines the type of secret management configuration to use (e.g., Vault or Other).
    /// Retrieves the type from the "VaultConfig:Type" section in the provided configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance from which to retrieve the secret management type.</param>
    /// <returns>The parsed <see cref="VaultConfigTypes"/> indicating the source for secrets (Vault or Other).</returns>
    public static VaultConfigTypes GetVaultConfigType(this IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var typeString = configuration.GetSection($"{nameof(VaultConfig)}:{nameof(VaultConfig.Type)}").Value;
        typeString ??= EnvironmentUtility.GetEnvironmentVariable(EnvironmentUtility.VAULT_TYPE);

        if (Enum.TryParse(typeString, true, out VaultConfigTypes configType))
        {
            Console.WriteLine($"VaultConfig type detected: {configType}");
            return configType;
        }

        Console.WriteLine("VaultConfig type not specified or invalid. Defaulting to 'Other'.");
        
        return VaultConfigTypes.Other;
    }

    /// <summary>
    /// Adds Vault-based secret retrieval to the configuration builder if the configuration type is set to "Vault".
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> used to build the application's configuration.</param>
    /// <param name="options"></param>
    /// <returns>The updated <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddVaultConfigurationSource(this IConfigurationBuilder builder, VaultConfig options)
    {
        options.Validate();
        
        if (options.Type != VaultConfigTypes.Vault.ToString())
        {
            Console.WriteLine("Vault type not selected. Using default configuration settings.");
            return builder;
        }
        
        var client = new HashiCorpVaultClient(options);

        builder.Add(new VaultConfigurationSource(client, options));

        Console.WriteLine("Vault configuration has been successfully added.");
        
        return builder;
    }
    

    /// <summary>
    /// Retrieves the PostgreSQL connection string based on the current configuration type.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> instance used to retrieve the connection string.</param>
    /// <returns>The PostgreSQL connection string.</returns>
    public static string GetPostgreSqlConnectionStringFromVault(this IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        if (configuration.GetVaultConfigType() == VaultConfigTypes.Vault)
        {
            Console.WriteLine("Retrieving PostgreSQL connection string from Vault.");
            return configuration.GetVaultVariable(VaultSecretKeys.ConnectionStringsPostgreSql);
        }

        Console.WriteLine("Retrieving PostgreSQL connection string from appsettings.json or environment variables.");
        return EnvironmentUtility.GetDatabaseConnectionString(configuration);
    }

    /// <summary>
    /// Retrieves the Redis connection string based on the current configuration type.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> instance used to retrieve the connection string.</param>
    /// <returns>The Redis connection string.</returns>
    public static string GetRedisConnectionStringFromVault(this IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        if (configuration.GetVaultConfigType() == VaultConfigTypes.Vault)
        {
            Console.WriteLine("Retrieving Redis connection string from Vault.");
            return configuration.GetVaultVariable(VaultSecretKeys.ConnectionStringsRedis);
        }

        Console.WriteLine("Retrieving Redis connection string from appsettings.json or environment variables.");
        return EnvironmentUtility.GetRedisConnectionString(configuration);
    }

    /// <summary>
    /// Retrieves a configuration value by its name, attempting to fetch it from Vault if necessary.
    /// </summary>
    /// <param name="config">The configuration instance.</param>
    /// <param name="name">The name of the configuration value to retrieve.</param>
    /// <returns>The retrieved configuration value.</returns>
    public static string GetVaultVariable(this IConfiguration config, string name)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The variable name cannot be null or empty.", nameof(name));
        }

        Console.WriteLine($"[Vault Retrieval] Retrieving configuration value for '{name}'.");

        var value = config[name];

        if (string.IsNullOrWhiteSpace(value))
        {
            Console.WriteLine($"[Vault Retrieval] Failed to retrieve value for '{name}'.");
            throw new KeyNotFoundException($"The configuration value for '{name}' was not found or is empty.");
        }

        Console.WriteLine($"[Vault Retrieval] Successfully retrieved value for '{name}'.");
        return value;
    }
}