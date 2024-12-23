using Microsoft.EntityFrameworkCore;

namespace VaultIntegration.WebApp.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
}