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

        modelBuilder.Entity<WorkItem>()
            .HasIndex(k => new { k.ProjectId, k.Status, k.Order });

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.Name);

        modelBuilder.Entity<AppSettings>()
            .HasData(new AppSettings
            {
                Id = 1,
                Theme = "light",
                LoggingLevel = "Information",
                DefaultWorktreeBasePath = string.Empty,
                UpdatedAt = new DateTime(2026, 4, 25, 8, 35, 53, 484, DateTimeKind.Utc).AddTicks(8046)
            });
    }
}