namespace VaultIntegration.WebApp.Infrastructure.MinIO;

public sealed class RemoteFileConfig 
{
    public string Type { get; set; }
    public int Port { get; set; }
    public string Directory { get; set; }
    public string BucketName { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}