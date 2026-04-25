using DevStudioDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevStudioDomain;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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
    }
}