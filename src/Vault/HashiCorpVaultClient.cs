using Polly;
using Polly.Retry;
using Vault.Models;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.Commons;

namespace Vault;

public class HashiCorpVaultClient : IHashiCorpVaultClient
{
    private readonly VaultConfig _config;
    private readonly IVaultClient _vaultClient;
    private readonly AsyncRetryPolicy _retryPolicy;

    public HashiCorpVaultClient(VaultConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // Validate essential configurations
        if (string.IsNullOrWhiteSpace(_config.Url))
        {
            throw new ArgumentException("Vault URL must be provided.", nameof(_config.Url));
        }

        if (string.IsNullOrWhiteSpace(_config.RoleId) || string.IsNullOrWhiteSpace(_config.SecretId))
        {
            throw new ArgumentException("RoleId and SecretId must be provided for Vault authentication.");
        }

        var vaultClientSettings = new VaultClientSettings(
            _config.Url,
            new AppRoleAuthMethodInfo(_config.RoleId, _config.SecretId)
        );

        _vaultClient = new VaultClient(vaultClientSettings);

        // Configure a retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine(
                        $"Retry {retryCount} encountered an error: {exception.Message}. Retrying in {timeSpan}...");
                });
    }
    
    /// <summary>
    /// Retrieves secrets from the specified path in Vault with fault-tolerant retry logic.
    /// </summary>
    /// <returns>A <see cref="Secret{SecretData}"/> object containing the retrieved data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when path or mount point is not configured.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when no data is found at the specified path.</exception>
    public async Task<Secret<SecretData>> GetSecretsAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.Path))
        {
            throw new ArgumentNullException(nameof(_config.Path), "Vault path cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(_config.MountPoint))
        {
            throw new ArgumentNullException(nameof(_config.MountPoint), "Vault mount point cannot be null or empty.");
        }
        
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                var response = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                    path: _config.Path,
                    mountPoint: _config.MountPoint
                );

                if (response?.Data == null || !response.Data.Data.Any())
                {
                    throw new KeyNotFoundException($"No data found in Vault at path '{_config.Path}'.");
                }

                return response;
            }
            catch (VaultApiException ex)
            {
                // Specific handling for Vault API-related issues
                throw new InvalidOperationException(
                    $"Error occurred while fetching secrets from Vault: {ex.Message}",
                    ex
                );
            }
        });
    }
}
