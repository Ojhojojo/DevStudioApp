using DevStudioDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevStudioDomain;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WorkItem>()
            .HasOne(k => k.Project)
            .WithMany()
            .HasForeignKey(k => k.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.Name);

        modelBuilder.Entity<AppSettings>()
            .HasData(new AppSettings
            {
                Id = 1,
                Theme = "light",
                LoggingLevel = "Information",
                DefaultWorktreeBasePath = string.Empty,
                UpdatedAt = DateTime.UtcNow
            });
    }
}