using Microsoft.Extensions.Configuration;
using Vault.Models;

namespace Vault;

public class VaultConfigurationProvider : ConfigurationProvider
{
    private readonly IHashiCorpVaultClient _client;
    private readonly string? _basePrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultConfigurationProvider"/> class.
    /// </summary>
    /// <param name="client">The Vault client used for fetching secrets.</param>
    /// <param name="config">The configuration settings for Vault.</param>
    /// <exception cref="ArgumentNullException">Thrown when client or config is null.</exception>
    public VaultConfigurationProvider(IHashiCorpVaultClient client, VaultConfig config)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        var vaultConfig = config ?? throw new ArgumentNullException(nameof(config));
        _basePrefix = NormalizeBasePath(vaultConfig.Path);
    }

    /// <summary>
    /// Loads secrets from Vault synchronously and updates the configuration data.
    /// </summary>
    public override void Load() => LoadAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Loads secrets from Vault asynchronously and updates the configuration data.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task LoadAsync()
    {
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var path = _basePrefix;

        try
        {
            // Fetch secrets from Vault using the configured client.
            var secretResponse = await _client.GetSecretsAsync().ConfigureAwait(false);
            
            if (secretResponse?.Data?.Data == null || !secretResponse.Data.Data.Any())
            {
                Console.WriteLine($"No secrets found in Vault at path '{path}'.");
                return;
            }

            // Process the key-value pairs in the retrieved secrets
            foreach (var (key, value) in secretResponse.Data.Data)
            {
                if (value == null)
                {
                    Console.WriteLine($"Key '{key}' found but value is null in Vault at path '{path}'.");
                    continue;
                }

                var normalizedKey = NormalizeKey(key);
                data[normalizedKey] = value.ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving secrets from Vault at path '{path}': {ex.Message}");
        }

        Data = data;
    }
    
    /// <summary>
    /// Normalizes a key by replacing '/' with ':' for consistent configuration formatting.
    /// </summary>
    /// <param name="key">The original key.</param>
    /// <returns>The normalized key.</returns>
    private static string NormalizeKey(string key)
    {
        return key.Replace('/', ':');
    }

    /// <summary>
    /// Normalizes the base path, ensuring it ends with a slash.
    /// </summary>
    /// <param name="keyPrefix">The base path to normalize.</param>
    /// <returns>The normalized base path.</returns>
    private static string? NormalizeBasePath(string? keyPrefix)
    {
        if (string.IsNullOrWhiteSpace(keyPrefix))
        {
            return null;
        }

        return keyPrefix.EndsWith("/") ? keyPrefix : $"{keyPrefix}/";
    }
}