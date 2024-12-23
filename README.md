# **Integrating HashiCorp Vault with .NET Applications**

## **Overview**

This guide explains how to securely integrate HashiCorp Vault into a .NET application.
Vault enables centralized management of sensitive configuration settings such as database connection strings,
API keys, and other secrets. By leveraging Vault, you eliminate the need to hardcode sensitive information in your application,
significantly improving security.

---

## **Prerequisites**

To successfully implement Vault integration, ensure you have the following:

1. **HashiCorp Vault**: A running instance of [HashiCorp Vault](https://www.vaultproject.io/).
2. **.NET 7 or Higher**: This guide assumes you are using a .NET 7 application.
3. **VaultSharp Library**: A .NET client library for interacting with Vault. Install it via NuGet:

   ```bash
   dotnet add package VaultSharp
   dotnet add package Polly
   ```

---

## **Configuration Options**

### **Option 1: Using `appsettings.json`**

In environments where using environment variables isn't feasible, you can include Vault configuration in `appsettings.json`. Ensure sensitive data, such as database connection strings, are **not** stored in the file.

Example `appsettings.json` for Vault:

```json
{
  "VaultConfig": {
    "Type": "Vault",
    "Url": "https://vault.example.com",
    "RoleId": "your-role-id",
    "SecretId": "your-secret-id",
    "Path": "ns/dev/SensitiveData",
    "MountPoint": "secret"
  }
}
```

### **Option 2: Using Environment Variables**

To enhance security, especially in production, provide Vault configuration through environment variables:

```bash
export VAULT_URL="https://vault.example.com"
export VAULT_ROLE_ID="your-role-id"
export VAULT_SECRET_ID="your-secret-id"
export VAULT_PATH="ns/dev/SensitiveData"
export VAULT_MOUNT_POINT="secret"
export VAULT_TYPE="Vault"
```

This approach minimizes the risk of accidental exposure in source control.

---

## **Integration Workflow**

### **Step 1: Setting Up in `Program.cs`**

Integrate Vault into the application configuration pipeline and register services:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Inject shared settings and integrate Vault
builder.InjectSharedAppSettings();

// Add services
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Configure Vault settings
builder.Services.Configure<VaultConfig>(builder.Configuration.GetSection(nameof(VaultConfig)));

var app = builder.Build();
```

### **Step 2: Implementing `InjectSharedAppSettings`**

The `InjectSharedAppSettings` method configures shared settings and integrates Vault into the configuration pipeline:

```csharp
public static void InjectSharedAppSettings(WebApplicationBuilder builder, AppFolders appFolder = AppFolders.APIs)
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
           .AddJsonFile(environmentSettings, optional: true, reloadOnChange: true)
           .AddJsonFile($"appsettings.{EnvironmentUtility.GetEnvironmentVariable()}.json", optional: true)
           .AddJsonFile($"SharedAppSettings.{EnvironmentUtility.GetEnvironmentVariable()}.json", optional: true, reloadOnChange: true)
           .AddEnvironmentVariables();

    // Add Vault as a configuration source
    builder.Configuration.AddVaultConfigurationSource(new VaultConfig
    {
        Type = builder.Configuration["Vault:Type"] ?? Environment.GetEnvironmentVariable("VAULT_TYPE"),
        Url = builder.Configuration["Vault:Url"] ?? Environment.GetEnvironmentVariable("VAULT_URL"),
        RoleId = builder.Configuration["Vault:RoleId"] ?? Environment.GetEnvironmentVariable("VAULT_ROLE_ID"),
        SecretId = builder.Configuration["Vault:SecretId"] ?? Environment.GetEnvironmentVariable("VAULT_SECRET_ID"),
        Path = builder.Configuration["Vault:Path"] ?? Environment.GetEnvironmentVariable("VAULT_PATH"),
        MountPoint = builder.Configuration["Vault:MountPoint"] ?? Environment.GetEnvironmentVariable("VAULT_MOUNT_POINT"),
    });
}
```

---

## **Debugging Guide**

To trace the flow of Vault integration and diagnose issues, debug the following components:

### 1. **`AddVaultConfigurationSource`**

This method initializes the Vault client and adds the configuration source. Debug the provided options:

```csharp
Console.WriteLine($"Vault URL: {options.Url}");
Console.WriteLine($"Vault RoleId: {options.RoleId}");
Console.WriteLine($"Vault Path: {options.Path}");
```

### 2. **`VaultConfigurationSource`**

`VaultConfigurationSource` provides configuration data retrieved from Vault. Ensure it's initialized correctly:

```csharp
Console.WriteLine("VaultConfigurationSource initialized.");
```

### 3. **`VaultConfigurationProvider`**

This provider fetches secrets from Vault. Debug secret retrieval:

```csharp
Console.WriteLine($"Fetching secret: Path={path}, Key={key}");
```

### 4. **`HashiCorpVaultClient`**

Ensure the client authenticates with Vault and retrieves secrets properly:

```csharp
Console.WriteLine("Authenticating with Vault...");
Console.WriteLine($"Vault Token: {authResponse.Auth.ClientToken}");
```

---

## **Using Secrets from Vault**

### **Example: PostgreSQL Connection String**

Retrieve the PostgreSQL connection string from Vault or fallback sources:

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

### **Example: Configuring the Database Context**

```csharp
public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetPostgreSqlConnectionString();

    services.AddDbContext<eRusumContext>((provider, options) =>
    {
        options.UseNpgsql(connectionString, o => o.CommandTimeout(10));
        options.EnableSensitiveDataLogging(true);
    });

    services.AddScoped(typeof(IRepository<>), typeof(EFRepository<>));
    services.AddScoped(typeof(IEFUnitOfWork), typeof(EFUnitOfWork));

    return services;
}
```

---

### ** Using Secrets with the `IOptions` Pattern**

When integrating with classes using the `IOptions` pattern, you can seamlessly map secrets to class properties if the secret keys are named according to the property structure.

#### **Example Configuration Class**

```csharp
public class RemoteFileConfig
{
    public string Host { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
}
```

#### **Mapping Vault Secrets to Class Properties**

If the Vault secrets are stored with keys matching the class property names (e.g., `RemoteFileConfig:Host`, `RemoteFileConfig:UserName`), the `.Bind()` method in `IOptions` will automatically populate the class:

```bash
vault kv put secret/ns/dev/SensitiveData RemoteFileConfig:Host="http://localhost:9000" RemoteFileConfig:UserName="minioadmin" RemoteFileConfig:Password="minioadmin" RemoteFileConfig:BucketName="exampleBucket"
```

#### **Registering the Configuration in `Program.cs`**

```csharp
builder.Services.Configure<RemoteFileConfig>(builder.Configuration.GetSection(nameof(RemoteFileConfig)));
```

#### **Using the Configuration**

```csharp
public class MinioService
{
    private readonly RemoteFileConfig _config;

    public MinioService(IOptions<RemoteFileConfig> config)
    {
        _config = config.Value;
        Console.WriteLine($"Host: {_config.Host}, UserName: {_config.UserName}");
    }
}
```

This approach ensures proper binding and eliminates manual mapping of secrets.

---

## **Storing and Accessing Secrets**

### **Storing Secrets in Vault**

Store sensitive data in Vault using the CLI:

```bash
vault kv put secret/ns/dev/SensitiveData ConnectionStringsPostgreSql="Server=myserver;Database=mydb;User Id=myuser;Password=mypassword;"
```

### **Retrieving Secrets**

Test retrieval using `curl`:

```bash
curl --header "X-Vault-Token: YOUR_VAULT_TOKEN" --request GET https://vault.example.com/v1/secret/data/ns/dev/SensitiveData
```

---

## **Benefits of Vault Integration**

- **Enhanced Security**: Centralizes sensitive information and minimizes exposure risk.
- **Environment Flexibility**: Easily switch between environments (e.g., development, staging, production).
- **Dynamic Secret Management**: Rotate secrets without modifying application code.

---

## **Conclusion**

Integrating HashiCorp Vault into your .NET application ensures secure, centralized, and scalable secret management. With proper debugging and configuration practices, you can confidently adapt to various environments while maintaining strict security protocols.

--- 