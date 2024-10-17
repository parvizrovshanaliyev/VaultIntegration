using Microsoft.Extensions.Configuration;
using Vault.Models;

namespace Vault;

public class VaultConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets or sets the <see cref="VaultConfig"/> to use for retrieving values
    /// </summary>
    public VaultConfig? Options { get; set; }
    public IHashiCorpVaultClient? Client { get; set; }
    
    public VaultConfigurationSource(IHashiCorpVaultClient client, VaultConfig options)
    {
        Client = client;
        Options = options;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new VaultConfigurationProvider(Client,Options);
}