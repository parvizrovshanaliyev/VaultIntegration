namespace Vault.Models;

/// <summary>
/// Represents the configuration settings required to interact with a secret management system,
/// such as HashiCorp Vault or other sources like environment variables or appsettings.
/// </summary>
public class VaultConfig
{
    /// <summary>
    /// Specifies the type of secret management to use.
    /// Use "Vault" to fetch secrets from HashiCorp Vault,
    /// or "Other" to use traditional sources like environment variables or appsettings.json.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// The base URL of the HashiCorp Vault instance.
    /// This is required if <see cref="Type"/> is set to "Vault".
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The Role ID used for AppRole authentication with HashiCorp Vault.
    /// This is required if <see cref="Type"/> is set to "Vault".
    /// </summary>
    public string? RoleId { get; set; }

    /// <summary>
    /// The Secret ID used for AppRole authentication with HashiCorp Vault.
    /// This is required if <see cref="Type"/> is set to "Vault".
    /// </summary>
    public string? SecretId { get; set; }

    /// <summary>
    /// The path within the Vault where secrets are stored.
    /// This is required if <see cref="Type"/> is set to "Vault".
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// The mount point used for the Key/Value secrets engine in Vault.
    /// This is required if <see cref="Type"/> is set to "Vault".
    /// </summary>
    public string? MountPoint { get; set; }

    /// <summary>
    /// A list of secret keys to be retrieved from the secret management system.
    /// </summary>
    public List<string> Secrets { get; set; } = new List<string>();
}

/// <summary>
/// Enum that defines the available types of secret management configurations.
/// </summary>
public enum VaultConfigTypes
{
    /// <summary>
    /// Use HashiCorp Vault for retrieving secrets.
    /// </summary>
    Vault,

    /// <summary>
    /// Use other methods like environment variables or appsettings.json for retrieving secrets.
    /// </summary>
    Other
}
