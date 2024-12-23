using Vault;
using Vault.Models;
using VaultIntegration.WebApp.Configs;
using VaultIntegration.WebApp.Infrastructure;
using VaultIntegration.WebApp.Persistence;

var builder = WebApplication.CreateBuilder(args);

InjectSharedAppSettings(builder);

// Add services to the container.
builder.Services.AddControllers(); // Register the controllers for API endpoints

builder.Services.Configure<VaultConfig>(builder.Configuration.GetSection(nameof(VaultConfig)));
builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection(nameof(EmailConfig)));

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllers(); // Map the controllers

app.Run();


void InjectSharedAppSettings(WebApplicationBuilder builder)
{
    IWebHostEnvironment env = builder.Environment;

    string sharedSettingsFolderPath = Path.Combine(env.ContentRootPath, "..", "SharedFiles");

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
    builder.Configuration.AddVaultConfigurationSource(options: new VaultConfig()
    {
        Type = builder.Configuration["Vault:Type"] ?? EnvironmentUtility.GetEnvironmentVariable(EnvironmentUtility.VAULT_TYPE),
        Url = builder.Configuration["Vault:Url"] ?? EnvironmentUtility.GetEnvironmentVariable(EnvironmentUtility.VAULT_URL),
        RoleId = builder.Configuration["Vault:RoleId"] ?? EnvironmentUtility.GetEnvironmentVariable(EnvironmentUtility.VAULT_ROLE_ID),
        SecretId = builder.Configuration["Vault:SecretId"] ?? EnvironmentUtility.GetEnvironmentVariable(EnvironmentUtility.VAULT_SECRET_ID),
        Path = builder.Configuration["Vault:Path"] ?? EnvironmentUtility.GetEnvironmentVariable(EnvironmentUtility.VAULT_PATH),
        MountPoint = builder.Configuration["Vault:MountPoint"] ?? EnvironmentUtility.GetEnvironmentVariable(EnvironmentUtility.VAULT_MOUNT_POINT),
    });
}
