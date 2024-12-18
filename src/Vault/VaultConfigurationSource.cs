using Microsoft.Extensions.Configuration;
using Vault.Models;

namespace Vault;

public class VaultConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// The configuration settings for Vault.
    /// </summary>
    public VaultConfig Options { get; }

    /// <summary>
    /// The Vault client used for retrieving configuration values.
    /// </summary>
    public IHashiCorpVaultClient Client { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultConfigurationSource"/> class.
    /// </summary>
    /// <param name="client">The Vault client used for fetching secrets.</param>
    /// <param name="options">The Vault configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown when client or options are null.</exception>
    public VaultConfigurationSource(IHashiCorpVaultClient client, VaultConfig options)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client), "Vault client cannot be null.");
        Options = options ?? throw new ArgumentNullException(nameof(options), "Vault configuration options cannot be null.");
    }

    /// <summary>
    /// Builds the Vault configuration provider.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <returns>A new instance of <see cref="VaultConfigurationProvider"/>.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder), "Configuration builder cannot be null.");
        }

        return new VaultConfigurationProvider(Client, Options);
    }
}