using WebOtchet.Models;
using Microsoft.EntityFrameworkCore;

namespace WebOtchet.Data;

public class WebotchetDbContext : DbContext
{
    public WebotchetDbContext(DbContextOptions<WebotchetDbContext> options) : base(options)
    {
        
    }
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<User> Users => Set<User>();

     protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(WebotchetDbContext).Assembly
        );
    }


}
