using Vault;

namespace VaultIntegration.WebApp.Configs;

public sealed class RemoteFileConfig : IKeyMappings
{
    public string Type { get; set; }
    public int Port { get; set; }
    public string Directory { get; set; }
    public string BucketName { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public Dictionary<string, string> GetKeyMappings() => new()
    {
        { EnvironmentUtility.RemoteFileConfigBucketName, nameof(BucketName) },
        { EnvironmentUtility.RemoteFileConfigHost, nameof(Host) },
        { EnvironmentUtility.RemoteFileConfigUserName, nameof(UserName) },
        { EnvironmentUtility.RemoteFileConfigPassword, nameof(Password) }
    };
}