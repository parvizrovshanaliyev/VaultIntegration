
# HashiCorp Vault Integration for .NET Applications

## Overview

This guide demonstrates how to integrate HashiCorp Vault into a .NET application for managing sensitive configuration settings such as database connection strings, API keys, and other secrets. By using Vault, you can securely store and retrieve secrets, minimizing the risk of hardcoding sensitive information in your application's configuration files.

## Prerequisites

- A running instance of [HashiCorp Vault](https://www.vaultproject.io/).
- A .NET 7 application (or higher).
- The VaultSharp library for interacting with Vault. You can install it via NuGet:

```bash
dotnet add package VaultSharp
```

## Configuration

### `appsettings.json`

The `appsettings.json` file should contain the configuration settings for Vault, including its URL, RoleId, and SecretId. You can also specify additional configuration settings, such as connection strings and Redis connection settings:

```json
{
  "VaultConfig": {
    "Type": "Vault", 
    "Url": "https://vault.example.com",
    "RoleId": "your-role-id",
    "SecretId": "your-secret-id",
    "Path": "ns/dev/erusumSensitiveData",
    "MountPoint": "secret"
  },
  "ConnectionStrings": {
    "PostgreSql": "Server=myserver;Database=mydb;User Id=myuser;Password=mypassword;"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

- **`Type`**: Defines the secret source. Use `"Vault"` to retrieve secrets from HashiCorp Vault, or `"Other"` to use traditional methods like `appsettings.json` or environment variables.
- **`Url`**: The base URL of the HashiCorp Vault instance.
- **`RoleId`** and **`SecretId`**: Credentials for Vault's AppRole authentication.
- **`Path`**: The path in Vault where secrets are stored.
- **`MountPoint`**: The mount point for the Key/Value secrets engine in Vault.

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
/// <summary>
/// Retrieves the PostgreSQL connection string based on the current configuration type.
/// Uses HashiCorp Vault if the type is "Vault"; otherwise, retrieves the value from appsettings.json or environment variables.
/// </summary>
/// <param name="configuration">The <see cref="IConfiguration"/> instance used to retrieve the connection string.</param>
/// <returns>The PostgreSQL connection string retrieved from Vault if applicable, or from default sources.</returns>
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

Integrating HashiCorp Vault into your .NET application allows for secure and flexible management of sensitive data like connection strings. By leveraging the methods provided, you can easily adapt to different environments while ensuring that your application's secrets remain protected.
