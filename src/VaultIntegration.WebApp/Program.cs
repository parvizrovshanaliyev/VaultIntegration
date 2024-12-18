using Vault;

var builder = WebApplication.CreateBuilder(args);

InjectSharedAppSettings(builder);

// Add services to the container.
builder.Services.AddControllers(); // Register the controllers for API endpoints

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
    builder.Configuration.AddVault(builder.Configuration);
}
