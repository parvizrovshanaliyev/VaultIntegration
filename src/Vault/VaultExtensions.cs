using Microsoft.Extensions.Configuration;
using Vault.Models;

namespace Vault;

/// <summary>
/// Provides extension methods for configuring Vault and retrieving secrets from the configuration.
/// </summary>
public static class VaultExtensions
{
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
    /// <param name="config">The existing configuration instance used to retrieve Vault settings.</param>
    /// <returns>The updated <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddVault(this IConfigurationBuilder builder, IConfiguration config)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (config == null) throw new ArgumentNullException(nameof(config));

        var options = GetVaultConfig(config);

        if (config.GetVaultConfigType() != VaultConfigTypes.Vault)
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
    public static string GetPostgreSqlConnectionString(this IConfiguration configuration)
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
    public static string GetRedisConnectionString(this IConfiguration configuration)
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

    /// <summary>
    /// Retrieves and binds the Vault configuration settings from the configuration source.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The configured <see cref="VaultConfig"/>.</returns>
    private static VaultConfig GetVaultConfig(this IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var vaultConfig = new VaultConfig
        {
            Url = EnvironmentUtility.GetEnvironmentVariable(EnvironmentUtility.VAULT_URL),
            RoleId = EnvironmentUtility.GetEnvironmentVariable(EnvironmentUtility.VAULT_ROLE_ID),
            SecretId = EnvironmentUtility.GetEnvironmentVariable(EnvironmentUtility.VAULT_SECRET_ID)
        };

        if (!string.IsNullOrWhiteSpace(vaultConfig.Url) &&
            !string.IsNullOrWhiteSpace(vaultConfig.RoleId) &&
            !string.IsNullOrWhiteSpace(vaultConfig.SecretId))
        {
            Console.WriteLine("Using Vault Config from environment variables.");
            BindNonSensitiveValues(configuration, vaultConfig);
        }
        else
        {
            Console.WriteLine("Using Vault Config from appsettings.json.");
            configuration.GetSection(nameof(VaultConfig)).Bind(vaultConfig);
        }

        Console.WriteLine($"Vault Config: URL: {vaultConfig.Url}");
        return vaultConfig;
    }

    /// <summary>
    /// Binds non-sensitive values from the configuration to the provided VaultConfig object.
    /// </summary>
    private static void BindNonSensitiveValues(IConfiguration configuration, VaultConfig vaultConfig)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (vaultConfig == null) throw new ArgumentNullException(nameof(vaultConfig));

        var section = configuration.GetSection(nameof(VaultConfig));
        vaultConfig.Path = section.GetValue<string>(nameof(VaultConfig.Path));
        vaultConfig.MountPoint = section.GetValue<string>(nameof(VaultConfig.MountPoint));
        vaultConfig.Type = section.GetValue<string>(nameof(VaultConfig.Type));
    }
}