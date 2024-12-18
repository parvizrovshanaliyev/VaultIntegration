# HashiCorp Vault Integration for .NET Applications

## Overview

This guide demonstrates how to integrate HashiCorp Vault into a .NET application for managing sensitive configuration settings such as database connection strings, API keys, and other secrets. By using Vault, you can securely store and retrieve secrets, minimizing the risk of hardcoding sensitive information in your application's configuration files.

## Prerequisites

- A running instance of [HashiCorp Vault](https://www.vaultproject.io/).
- A .NET 7 application (or higher).
- The VaultSharp library for interacting with Vault. You can install it via NuGet:

```bash
dotnet add package VaultSharp
dotnet add package Polly
```

### **`appsettings.json` Configuration**

When using **Vault**, the `appsettings.json` file primarily contains the Vault configuration settings. Sensitive data,
such as connection strings and credentials, are retrieved dynamically from Vault and are not stored in the `appsettings.json`. 
This approach enhances security by centralizing secret management.

#### **Vault Configuration (`Type: Vault`)**

When Vault is used, the `appsettings.json` file includes only the settings required to connect to Vault, such as its URL, RoleId, and SecretId. Secrets like database connection strings or Redis configurations are dynamically retrieved from Vault using the specified `Path` and `MountPoint`.

Example `appsettings.json` when using Vault:

```json
{
  "VaultConfig": {
    "Type": "Vault",
    "Url": "https://vault.example.com",
    "RoleId": "your-role-id",
    "SecretId": "your-secret-id",
    "Path": "ns/dev/erusumSensitiveData",
    "MountPoint": "secret"
  }
}
```

**Key Points:**
- Sensitive information (e.g., connection strings) is not hardcoded.
- Vault acts as the source of truth for all secrets.
- The `Path` defines where the secrets are stored in Vault.

#### **Traditional Configuration (`Type: Other`)**

When Vault is **not** used, the `appsettings.json` file stores the sensitive information directly, such as database
connection strings and other credentials. This approach is less secure because secrets are stored in plain text.

Example `appsettings.json` without Vault:

```json
{
  "VaultConfig": {
    "Type": "Other"
  },
  "ConnectionStrings": {
    "PostgreSql": "Server=myserver;Database=mydb;User Id=myuser;Password=mypassword;"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

**Key Points:**
- Secrets are directly embedded in the file, which may be sufficient for local development but is insecure for production.
- The `Type` is set to `"Other"` to indicate that secrets are not retrieved from Vault.
- If transitioning to Vault, these sensitive keys can be removed from `appsettings.json` and stored in Vault.

---

## Integrating Vault with Your Application

### `Program.cs` Setup

In your `Program.cs`, integrate Vault with the configuration builder and inject the database context:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add shared settings and Vault integration
builder.InjectSharedAppSetings();

// Configure services, including setting up the database context using the connection string from Vault or other sources.
builder.Services.AddPersistence(builder.Configuration);

var app = builder.Build();
```

### `InjectSharedAppSetings` Method

The `InjectSharedAppSetings` method adds shared settings and integrates Vault into the configuration builder:

```csharp
public static void InjectSharedAppSetings(WebApplicationBuilder builder, AppFolders appFolder = AppFolders.APIs)
{
    IWebHostEnvironment env = builder.Environment;
    string sharedSettingsFolderPath = Path.Combine(env.ContentRootPath, "..", "SharedFiles");

    if (appFolder == AppFolders.WebStatus)
    {
        sharedSettingsFolderPath = Path.Combine(env.ContentRootPath, "..", "..", "APIs", "SharedFiles");
    }

    var environmentSettings = Path.Combine(sharedSettingsFolderPath, $"SharedAppSettings.{EnvironmentUtility.GetEnvironmentVariable()}.json");

    builder.Configuration
           .SetBasePath(env.ContentRootPath)
           .AddJsonFile("appsettings.json", optional: true)
           .AddJsonFile("SharedAppSettings.json", optional: true)
           .AddJsonFile(environmentSettings, optional: true, true)
           .AddJsonFile($"appsettings.{EnvironmentUtility.GetEnvironmentVariable()}.json", optional: true)
           .AddJsonFile($"SharedAppSettings.{EnvironmentUtility.GetEnvironmentVariable()}.json", optional: true, true);

    builder.Configuration.AddEnvironmentVariables();

    // Add Vault to the configuration builder
    builder.Configuration.AddVault(builder.Configuration);
}
```

This method:
- Loads shared settings and environment-specific JSON files.
- Adds environment variables to the configuration.
- Integrates Vault, allowing secrets to be fetched securely.

### Using the PostgreSQL Connection String

To use the PostgreSQL connection string retrieved from Vault or other sources, follow this example:

```csharp
public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
{
    // Get the PostgreSQL connection string from Vault or other sources.
    var postgresqlConnectionString = configuration.GetPostgreSqlConnectionString();

    services.AddDbContext<eRusumContext>((provider, options) =>
    {
        options.UseNpgsql(postgresqlConnectionString, options =>
        {
            options.CommandTimeout(10); // Set a timeout for database commands (in seconds).
        });

        options.EnableSensitiveDataLogging(true); // Enable detailed logging (for development purposes only).
    });

    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    services.AddScoped(typeof(IRepository<>), typeof(EFRepository<>));
    services.AddScoped(typeof(IEFUnitOfWork), typeof(EFUnitOfWork));

    return services;
}
```

### Example: Retrieving the PostgreSQL Connection String

The `GetPostgreSqlConnectionString` method determines whether to retrieve the connection string from Vault or fall back to appsettings or environment variables:

```csharp
public static string GetPostgreSqlConnectionString(this IConfiguration configuration)
{
    if (configuration.GetVaultConfigType() == VaultConfigTypes.Vault)
    {
        Console.WriteLine("Retrieving PostgreSQL connection string from Vault.");
        return configuration.GetVaultVariable(VaultSecretKeys.ConnectionStringsPostgreSql);
    }

    Console.WriteLine("Retrieving PostgreSQL connection string from appsettings.json or environment variables.");
    return EnvironmentUtility.GetDatabaseConnectionString(configuration);
}
```

### Using the Redis Connection String

To use the Redis connection string retrieved from Vault or other sources, follow this example:

```csharp
public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetRedisConnectionStringFromVault();
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = connectionString;
        options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
        {
            EndPoints = { connectionString },
            AbortOnConnectFail = false,
            AsyncTimeout = 3000,
            ConnectTimeout = 4000,
            SyncTimeout = 3000,
        };
    });
    services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
    services.AddSingleton<IDistributedCacheService, DistributedCacheService>();

    return services;
}
```

---

### **Using Configurations for IOptions Pattern**

#### Add Configurations in a Centralized Way
```csharp
public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration configuration)
{
    services.Configure<RemoteFileConfig>(configuration.GetSection(nameof(RemoteFileConfig)));
    services.Configure<EmailConfig>(configuration.GetSection(nameof(EmailConfig)));

    return services;
}
```

### **Example 1: Keys Matching `RemoteFileConfig` Properties**

If the Vault or environment keys exactly match the class properties, the default `.Bind()` method works seamlessly.

#### **Environment or Vault Example**
```bash
RemoteFileConfig:Host="http://localhost:9000"
RemoteFileConfig:UserName="minioadmin"
RemoteFileConfig:Password="minioadmin"
RemoteFileConfig:BucketName="exampleBucket"
```

#### **Service Configuration**
```csharp
// Add Minio to IServiceCollection
public static IServiceCollection AddMinio(this IServiceCollection services, IConfiguration configuration)
{
    services.Configure<RemoteFileConfig>(configuration.GetSection(nameof(RemoteFileConfig)));
    
    var config = new RemoteFileConfig();
    configuration.GetSection(nameof(RemoteFileConfig)).Bind(config);

    try
    {
        if (config is not null && !string.IsNullOrEmpty(config.Type))
        {
            if (config.Type.Equals(nameof(RemoteFileConfigTypes.Minio), StringComparison.OrdinalIgnoreCase))
            {
                services.AddMinio(config.UserName, config.Password);
            }
            Console.WriteLine($"Connected to: RemoteFileConfigHost - {config.Host}");
        }
        else
        {
            Console.WriteLine($"Error: SharedAppSetting.{EnvironmentUtility.GetEnvironmentVariable()}.json could NOT read.");
        }
    }
    catch (Exception ex)
    {
        if (string.IsNullOrEmpty(config.Type))
        {
            Console.WriteLine($"Error: SharedAppSetting.{EnvironmentUtility.GetEnvironmentVariable()}.json could not read or RemoteFileConfigType variable is empty.");
        }
        else if (!string.IsNullOrEmpty(config.Host))
            Console.WriteLine($"Could not connect to {config.Host}");

        Console.WriteLine($"ExceptionMessage: {ex.Message}");
    }
    return services;
}
// RemoteFileConfig class
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

#### **Using `RemoteFileConfig` in a Service**
public sealed class MinioService : IRemoteFileAdapterService
{
    private readonly IMinioClient _minioClient;
    private readonly RemoteFileConfig _config;
    private readonly string _bucketName;
    public MinioService(IOptions<RemoteFileConfig> config)
    {
        _config = config.Value;

        if (config.Value.Type.Equals(nameof(RemoteFileConfigTypes.Minio), StringComparison.OrdinalIgnoreCase))
        {
            _bucketName = config.Value.BucketName;
            _minioClient = new MinioClient()
                                        .WithEndpoint(_config.Host)
                                        .WithCredentials(_config.UserName, _config.Password)
                                        .WithSSL(false)
                                        .Build();
        }
    }
}
```

### **Example 2: Keys Differing from `RemoteFileConfig` Properties**

If the keys in Vault or environment variables **do not match** the property names, you need custom key mapping.

#### **Environment or Vault Example**
```bash
RemoteFileConfigHost="http://localhost:9000"
RemoteFileConfigUserName="minioadmin"
RemoteFileConfigPassword="minioadmin"
RemoteFileConfigBucketName="exampleBucket"
```

#### **Service Configuration with Custom Mapping**
```csharp
public static IServiceCollection AddMinio(this IServiceCollection services, IConfiguration configuration)
{
    // Use custom Vault-based configuration binding
    services.ConfigureWithVault<RemoteFileConfig>(configuration);
    
    var config = new RemoteFileConfig();
    configuration.GetSection(nameof(RemoteFileConfig)).Bind(config);
    config.Host = configuration.GetVaultVariable(EnvironmentUtility.RemoteFileConfigHost);
    config.UserName = configuration.GetVaultVariable(EnvironmentUtility.RemoteFileConfigUserName);
    config.Password = configuration.GetVaultVariable(EnvironmentUtility.RemoteFileConfigPassword);
    config.BucketName = configuration.GetVaultVariable(EnvironmentUtility.RemoteFileConfigBucketName);

    try
    {
        if (config is not null && !string.IsNullOrEmpty(config.Type))
        {
            if (config.Type.Equals(nameof(RemoteFileConfigTypes.Minio), StringComparison.OrdinalIgnoreCase))
            {
                services.AddMinio(config.UserName, config.Password);
            }
            Console.WriteLine($"Connected to: RemoteFileConfigHost - {config.Host}");
        }
        else
        {
            Console.WriteLine($"Error: SharedAppSetting.{EnvironmentUtility.GetEnvironmentVariable()}.json could NOT read.");
        }
    }
    catch (Exception ex)
    {
        if (string.IsNullOrEmpty(config.Type))
        {
            Console.WriteLine($"Error: SharedAppSetting.{EnvironmentUtility.GetEnvironmentVariable()}.json could not read or RemoteFileConfigType variable is empty.");
        }
        else if (!string.IsNullOrEmpty(config.Host))
            Console.WriteLine($"Could not connect to {config.Host}");

        Console.WriteLine($"ExceptionMessage: {ex.Message}");
    }
    return services;
}

// RemoteFileConfig class needs to implement IKeyMappings
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
```

#### **`ConfigureWithVault` Helper Method**

```csharp
public static IServiceCollection ConfigureWithVault<TConfig>(
            this IServiceCollection services,
            IConfiguration configuration) where TConfig : class, new()
{
    var config = new TConfig();
    configuration.GetSection(typeof(TConfig).Name).Bind(config);

    if (config is IKeyMappings keyMappingsProvider)
    {
        var mappings = keyMappingsProvider.GetKeyMappings();

        foreach (var mapping in mappings)
        {
            var vaultValue = configuration.GetVaultVariable(mapping.Key);
            
            if (!string.IsNullOrWhiteSpace(vaultValue))
            {
                var property = typeof(TConfig).GetProperty(mapping.Value);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(config, Convert.ChangeType(vaultValue, property.PropertyType));
                }
            }
        }
    }

    services.Configure<TConfig>(_ =>
    {
        foreach (var property in typeof(TConfig).GetProperties())
        {
            property.SetValue(_, property.GetValue(config));
        }
    });

    return services;
}
```

### Example: Storing Secrets in Vault

To store a connection string in Vault using the Key/Value secrets engine, use the following command:

```bash
vault kv put secret/ns/dev/erusumSensitiveData ConnectionStringsPostgreSql="Server=myserver;Database=mydb;User Id=myuser;Password=mypassword;"
```

### Example: Retrieving Secrets Using `curl`

Verify the stored secrets using `curl`:

```bash
curl --header "X-Vault-Token: YOUR_VAULT_TOKEN" --request GET https://vault.example.com/v1/secret/data/ns/dev/erusumSensitiveData
```

### Setting Environment Variables

For security, set environment variables for `VaultConfig` parameters:

```bash
export VAULT_URL="https://vault.example.com"
export VAULT_ROLE_ID="your-role-id"
export VAULT_SECRET_ID="your-secret-id"
```

This allows your application to read these values at runtime without storing them in `appsettings.json`.

## Benefits of Using Vault

- **Enhanced Security**: Secrets are stored and managed in a central, secure location, minimizing the risk of exposing sensitive data in configuration files.
- **Flexible Configuration**: Switch between using Vault and traditional methods by changing a single configuration value.
- **Environment-Specific Management**: Use different sources for secrets in development, staging, and production environments.

## Conclusion

Integrating HashiCorp Vault into your .NET application allows for secure and flexible management of sensitive data like connection 
strings. By leveraging the methods provided, you can easily adapt to different environments while ensuring that your
application's secrets remain protected.

