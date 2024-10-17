using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Vault.Models;
using VaultSharp.V1.Commons;

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
        var vaultConfig = config ?? throw new ArgumentNullException(nameof(config));
        _client = client ?? throw new ArgumentNullException(nameof(client));
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

            // Process each key-value pair in the retrieved secrets.
            foreach (var kvp in secretResponse.Data.Data)
            {
                var key = kvp.Key.Replace('/', ':');
                data[key] = kvp.Value?.ToString() ?? throw new KeyNotFoundException(
                    $"Key '{kvp.Key}' found but value is null in Vault at path '{path}'");
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error retrieving secrets from Vault at path '{path}': {ex}");
        }

        // Store the retrieved data in the configuration provider.
        Data = data!;
    }

    /// <summary>
    /// Adds a secret to the data dictionary if it exists in the Vault response.
    /// </summary>
    /// <param name="secretResponse">The response from Vault containing the secret data.</param>
    /// <param name="secretItem">The specific secret item key to add to the dictionary.</param>
    /// <param name="data">The dictionary to store the secret values.</param>
    /// <param name="path">The path in Vault where the secrets are stored.</param>
    private static void AddSecretToDictionary(Secret<SecretData> secretResponse, string secretItem, Dictionary<string, string> data, string? path)
    {
        try
        {
            // Attempt to retrieve the specific secret item from the response.
            if (secretResponse.Data.Data.TryGetValue(secretItem, out var value))
            {
                var key = secretItem.Replace('/', ':');
                data[key] = value?.ToString() ?? throw new KeyNotFoundException(
                    $"Key '{secretItem}' found but value is null in Vault at path '{path}'");
            }
            else
            {
                Trace.WriteLine($"Key '{secretItem}' not found in Vault at path '{path}'");
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error processing key '{secretItem}' from Vault: {ex}");
        }
    }

    /// <summary>
    /// Normalizes the base path, ensuring it ends with a slash.
    /// </summary>
    /// <param name="keyPrefix">The base path to normalize.</param>
    /// <returns>The normalized base path, ensuring a trailing slash.</returns>
    private static string? NormalizeBasePath(string? keyPrefix)
    {
        if (string.IsNullOrWhiteSpace(keyPrefix))
        {
            return string.Empty;
        }

        return keyPrefix.EndsWith("/") ? keyPrefix : $"{keyPrefix}/";
    }
}