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
    /// <remarks>
    /// If the type is not specified or is invalid, the method defaults to <see cref="VaultConfigTypes.Other"/>.
    /// This allows the application to fall back to using environment variables or appsettings.json.
    /// </remarks>
    public static VaultConfigTypes GetVaultConfigType(this IConfiguration configuration)
    {
        // Retrieve the value of "VaultConfig:Type" from the configuration.
        var typeString = configuration.GetSection($"{nameof(VaultConfig)}:{nameof(VaultConfig.Type)}").Value;

        // Attempt to parse the retrieved value into a VaultConfigTypes enum.
        if (Enum.TryParse(typeString, true, out VaultConfigTypes configType))
        {
            Console.WriteLine($"VaultConfig type detected: {configType}");
            return configType;
        }

        // Log a message if parsing fails or the type is not specified, then default to 'Other'.
        Console.WriteLine("VaultConfig type not specified or invalid. Defaulting to 'Other'.");
        return VaultConfigTypes.Other;
    }

    /// <summary>
    /// Adds Vault-based secret retrieval to the configuration builder if the configuration type is set to "Vault".
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> used to build the application's configuration.</param>
    /// <param name="config">The existing configuration instance used to retrieve Vault settings.</param>
    /// <returns>
    /// The updated <see cref="IConfigurationBuilder"/> with Vault settings if the Vault type is selected;
    /// otherwise, returns the builder without changes to use default settings like appsettings or environment variables.
    /// </returns>
    /// <remarks>
    /// This method allows seamless integration with HashiCorp Vault for secret management, enhancing security by
    /// avoiding hardcoded secrets in configuration files.
    /// </remarks>
    public static IConfigurationBuilder AddVault(this IConfigurationBuilder builder, IConfiguration config)
    {
        // Retrieve the Vault configuration settings from the provided configuration.
        var options = GetVaultConfig(config);

        // Check if the configuration type is set to "Vault".
        if (config.GetVaultConfigType() != VaultConfigTypes.Vault)
        {
            Console.WriteLine(
                "Vault type not selected. Using default configuration settings from appsettings.json or environment variables.");
            return builder; // Return the unmodified builder if Vault is not being used.
        }

        // Create a client for interacting with the HashiCorp Vault.
        var client = new HashiCorpVaultClient(options);

        // Add the Vault configuration source to the builder, enabling retrieval of secrets from Vault.
        builder.Add(new VaultConfigurationSource(client, options));

        Console.WriteLine("Vault configuration has been successfully added to the configuration builder.");
        return builder;
    }

    /// <summary>
    /// Retrieves the PostgreSQL connection string based on the current configuration type.
    /// Uses HashiCorp Vault if the type is "Vault"; otherwise, retrieves the value from appsettings.json or environment variables.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> instance used to retrieve the connection string.</param>
    /// <returns>The PostgreSQL connection string retrieved from Vault if applicable, or from default sources.</returns>
    /// <remarks>
    /// This method simplifies accessing database connection strings by centralizing the logic
    /// for determining whether to use a secret management system or traditional configuration sources.
    /// </remarks>
    public static string GetPostgreSqlConnectionString(this IConfiguration configuration)
    {
        // Check if the configuration type is set to "Vault".
        if (configuration.GetVaultConfigType() == VaultConfigTypes.Vault)
        {
            Console.WriteLine("Retrieving PostgreSQL connection string from Vault.");
            return configuration.GetVaultVariable(VaultSecretKeys.ConnectionStringsPostgreSql);
        }

        // If not using Vault, retrieve the connection string from appsettings.json or environment variables.
        Console.WriteLine("Retrieving PostgreSQL connection string from appsettings.json or environment variables.");
        return EnvironmentUtility.GetDatabaseConnectionString(configuration);
    }

    /// <summary>
    /// Retrieves the Redis connection string based on the current configuration type.
    /// Uses HashiCorp Vault if the type is "Vault"; otherwise, retrieves the value from appsettings.json or environment variables.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> instance used to retrieve the connection string.</param>
    /// <returns>The Redis connection string retrieved from Vault if applicable, or from default sources.</returns>
    /// <remarks>
    /// This method ensures that sensitive Redis connection information can be managed securely using Vault,
    /// while still providing a fallback to appsettings or environment variables if Vault is not configured.
    /// </remarks>
    public static string GetRedisConnectionString(this IConfiguration configuration)
    {
        // Check if the configuration type is set to "Vault".
        if (configuration.GetVaultConfigType() == VaultConfigTypes.Vault)
        {
            Console.WriteLine("Retrieving Redis connection string from Vault.");
            return configuration.GetVaultVariable(VaultSecretKeys.ConnectionStringsRedis);
        }

        // If not using Vault, retrieve the Redis connection string from appsettings.json or environment variables.
        Console.WriteLine("Retrieving Redis connection string from appsettings.json or environment variables.");
        return EnvironmentUtility.GetRedisConnectionString(configuration);
    }


    /// <summary>
    /// Retrieves a configuration value by its name, attempting to fetch it from the Vault if necessary.
    /// </summary>
    /// <param name="config">The configuration instance.</param>
    /// <param name="name">The name of the configuration value to retrieve.</param>
    /// <returns>The retrieved configuration value.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the configuration value is not found or is empty.</exception>
    public static string GetVaultVariable(this IConfiguration config, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The variable name provided for Vault retrieval cannot be null or empty.",
                nameof(name));
        }

        Console.WriteLine($"[Vault Retrieval] Starting the retrieval process for the configuration value '{name}'.");

        // Attempt to retrieve the value from the configuration
        var value = config[name];

        // Check if the value is null or empty
        if (string.IsNullOrWhiteSpace(value))
        {
            Console.WriteLine(
                $"[Vault Retrieval] Failed to retrieve the configuration value for '{name}'. The value was not found in the Vault or is empty.");
            throw new KeyNotFoundException(
                $"[Vault Retrieval] The configuration value for '{name}' was not found or is empty.");
        }

        Console.WriteLine($"[Vault Retrieval] Successfully retrieved the configuration value for '{name}'.");

        return value;
    }


    /// <summary>
    /// Retrieves and binds the Vault configuration settings from the configuration source.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The configured <see cref="VaultConfig"/>.</returns>
    private static VaultConfig GetVaultConfig(this IConfiguration configuration)
    {
        Console.WriteLine("Get Vault Config...");

        var vaultConfig = new VaultConfig
        {
            // Attempt to retrieve sensitive values from environment variables
            Url = EnvironmentUtility.GetEnvironmentVariable(EnvironmentUtility.VAULT_URL),
            RoleId = EnvironmentUtility.GetEnvironmentVariable(EnvironmentUtility.VAULT_ROLE_ID),
            SecretId = EnvironmentUtility.GetEnvironmentVariable(EnvironmentUtility.VAULT_SECRET_ID)
        };

        // Determine if all sensitive environment variables are set
        var hasEnvValues = !string.IsNullOrWhiteSpace(vaultConfig.Url)
                           && !string.IsNullOrWhiteSpace(vaultConfig.RoleId)
                           && !string.IsNullOrWhiteSpace(vaultConfig.SecretId);

        // If environment variables are set, use them and bind additional config values
        if (hasEnvValues)
        {
            Console.WriteLine("Using Vault Config from Environment Variables...");
            BindNonSensitiveValues(configuration, vaultConfig);
        }
        else
        {
            Console.WriteLine("Using Vault Config from appsettings.json...");
            configuration.GetSection(nameof(VaultConfig)).Bind(vaultConfig);
        }

        Console.WriteLine($"Vault Config: Address: {vaultConfig.Url}");

        return vaultConfig;
    }

    /// <summary>
    /// Binds non-sensitive values from the configuration to the provided VaultConfig object.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="vaultConfig">The Vault configuration object to populate.</param>
    private static void BindNonSensitiveValues(IConfiguration configuration, VaultConfig vaultConfig)
    {
        var section = configuration.GetSection(nameof(VaultConfig));

        vaultConfig.Path = section.GetValue<string>(nameof(VaultConfig.Path));
        vaultConfig.MountPoint = section.GetValue<string>(nameof(VaultConfig.MountPoint));
        vaultConfig.Type = section.GetValue<string>(nameof(VaultConfig.Type));
    }
}