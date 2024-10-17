namespace Vault;

public static class VaultSecretKeys
{
    public const string ConnectionStringsPostgreSql = nameof(ConnectionStringsPostgreSql);
    public const string ConnectionStringsRedis = nameof(ConnectionStringsRedis);
    
    public const string JwtTokenConfigPasswordSalt = nameof(JwtTokenConfigPasswordSalt);
    public const string JwtTokenConfigIssuerSigningKey = nameof(JwtTokenConfigIssuerSigningKey);
    
    public const string ForgotPasswordConfigResetPasswordLink = nameof(ForgotPasswordConfigResetPasswordLink);
    
    public const string ElasticSearchConfigUri = nameof(ElasticSearchConfigUri);
    public const string ElasticSearchConfigUserName = nameof(ElasticSearchConfigUserName);
    public const string ElasticSearchConfigPassword = nameof(ElasticSearchConfigPassword);
    
    public const string RabbitMQConfigHost = nameof(RabbitMQConfigHost);
    public const string RabbitMQConfigUserName = nameof(RabbitMQConfigUserName);
    public const string RabbitMQConfigPassword = nameof(RabbitMQConfigPassword);
    public const string RabbitMQConfigPort = nameof(RabbitMQConfigPort);
    
    public const string RemoteFileConfigHost = nameof(RemoteFileConfigHost);
    public const string RemoteFileConfigUserName = nameof(RemoteFileConfigUserName);
    public const string RemoteFileConfigPassword = nameof(RemoteFileConfigPassword);
    public const string RemoteFileConfigBucketName = nameof(RemoteFileConfigBucketName);
}