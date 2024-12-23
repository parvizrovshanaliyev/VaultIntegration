using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Vault;

public static class EnvironmentUtility
{

    public readonly static string Production = Environments.Production;
    public readonly static string Development = Environments.Development;
    public readonly static string Staging = Environments.Staging;
    public readonly static string Uat = nameof(Uat);

    public readonly static string ApplicationName = nameof(ApplicationName);
    public readonly static string ConnectionStringsPostgreSql = nameof(ConnectionStringsPostgreSql);
    public readonly static string ConnectionStringsRedis = nameof(ConnectionStringsRedis);

    public readonly static string JwtTokenConfigPasswordSalt = nameof(JwtTokenConfigPasswordSalt);
    public readonly static string JwtTokenConfigIssuerSigningKey = nameof(JwtTokenConfigIssuerSigningKey);

    public readonly static string ForgotPasswordConfigResetPasswordLink = nameof(ForgotPasswordConfigResetPasswordLink);

    public readonly static string ElasticSearchConfigUri = nameof(ElasticSearchConfigUri);
    public readonly static string ElasticSearchConfigUserName = nameof(ElasticSearchConfigUserName);
    public readonly static string ElasticSearchConfigPassword = nameof(ElasticSearchConfigPassword);

    public readonly static string RabbitMQConfigHost = nameof(RabbitMQConfigHost);
    public readonly static string RabbitMQConfigUserName = nameof(RabbitMQConfigUserName);
    public readonly static string RabbitMQConfigPassword = nameof(RabbitMQConfigPassword);
    public readonly static string RabbitMQConfigPort = nameof(RabbitMQConfigPort);

    public readonly static string RemoteFileConfigHost = nameof(RemoteFileConfigHost);
    public readonly static string RemoteFileConfigUserName = nameof(RemoteFileConfigUserName);
    public readonly static string RemoteFileConfigPassword = nameof(RemoteFileConfigPassword);
    public readonly static string RemoteFileConfigBucketName = nameof(RemoteFileConfigBucketName);

    public const string VAULT_TYPE = nameof(VAULT_TYPE);
    public const string VAULT_URL = nameof(VAULT_URL);
    public const string VAULT_ROLE_ID = nameof(VAULT_ROLE_ID);
    public const string VAULT_SECRET_ID = nameof(VAULT_SECRET_ID);
    public const string VAULT_PATH = nameof(VAULT_PATH);
    public const string VAULT_MOUNT_POINT = nameof(VAULT_MOUNT_POINT);
    



    public const string ASPNETCORE_ENVIRONMENT = nameof(ASPNETCORE_ENVIRONMENT);

    private static bool IsEnvironment(string environment) => string.Equals(GetEnvironmentVariable(), environment, StringComparison.InvariantCultureIgnoreCase);

    public static bool IsProduction() => IsEnvironment(Production);
    public static bool IsStaging() => IsEnvironment(Staging);
    public static bool IsDevelopment() => IsEnvironment(Development);
    public static bool IsUat() => IsEnvironment(Uat);

    public static void SetEnvironmentDevelopment() => Environment.SetEnvironmentVariable(ASPNETCORE_ENVIRONMENT, Development);
    public static void SetEnvironmentStaging() => Environment.SetEnvironmentVariable(ASPNETCORE_ENVIRONMENT, Staging);
    public static void SetEnvironmentUat() => Environment.SetEnvironmentVariable(ASPNETCORE_ENVIRONMENT, Uat);
    public static void SetEnvironmentProduction() => Environment.SetEnvironmentVariable(ASPNETCORE_ENVIRONMENT, Production);

    public static string? GetEnvironmentVariable(string name = ASPNETCORE_ENVIRONMENT) => Environment.GetEnvironmentVariable(name);
    public static string? GetDatabaseConnectionString(this IConfiguration configuration) => IsDevelopment() ? configuration.GetConnectionString("PostgreSql") ?? GetEnvironmentVariable(ConnectionStringsPostgreSql) : GetEnvironmentVariable(ConnectionStringsPostgreSql);
    public static string? GetRedisConnectionString(this IConfiguration configuration) => IsDevelopment() ? configuration.GetConnectionString("Redis") ?? GetEnvironmentVariable(ConnectionStringsRedis) : GetEnvironmentVariable(ConnectionStringsRedis);
    public static string? GetApplicationName() => GetEnvironmentVariable(ApplicationName);


}
