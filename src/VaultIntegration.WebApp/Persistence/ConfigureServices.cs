using Microsoft.EntityFrameworkCore;
using Vault;

namespace VaultIntegration.WebApp.Persistence;

public static class ConfigureServices
{
    public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetPostgreSqlConnectionStringFromVault();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
    }
    
    
}