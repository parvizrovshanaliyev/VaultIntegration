namespace VaultIntegration.WebApp.Configs;

public class EmailConfig
{
    public string From { get; set; } =string.Empty;
    public string Password { get; set; } =string.Empty;
    public string Host { get; set; } =string.Empty;
    public int Port { get; set; } 
    public bool EnableSsl { get; set; }
}