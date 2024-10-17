using Vault.Models;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.Commons;

namespace Vault;

public class HashiCorpVaultClient : IHashiCorpVaultClient
{
    private readonly VaultConfig _config;
    private readonly IVaultClient _vaultClient;

    public HashiCorpVaultClient(VaultConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        
        var vaultClientSettings = new VaultClientSettings(
            _config.Url,
            new AppRoleAuthMethodInfo(_config.RoleId, _config.SecretId)
        );
        
        _vaultClient = new VaultClient(vaultClientSettings);
    }

    /// <summary>
    /// Retrieves secrets from the specified path in Vault.
    /// </summary>
    /// <returns>A Secret object containing the retrieved data.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no data is found at the specified path.</exception>
    public async Task<Secret<SecretData>> GetSecretsAsync()
    {
        var path = _config.Path ?? throw new ArgumentNullException(nameof(_config.Path), "Vault path cannot be null.");
        var mountPoint = _config.MountPoint ??  throw new ArgumentNullException(nameof(_config.MountPoint), "Vault mount point cannot be null.");

        var response = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
            path: path,
            mountPoint: mountPoint);

        // Check for missing data in the response
        if (response?.Data == null)
        {
            throw new KeyNotFoundException($"No data found in Vault at path '{path}'.");
        }

        return response;
    }
}
