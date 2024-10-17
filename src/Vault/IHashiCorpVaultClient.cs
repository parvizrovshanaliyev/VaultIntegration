using VaultSharp.V1.Commons;

namespace Vault;

public interface IHashiCorpVaultClient
{
    Task<Secret<SecretData>> GetSecretsAsync();
}