using Microsoft.EntityFrameworkCore;
using RotationTracker.Domain;

namespace RotationTracker.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(project =>
        {
            project.Property(p => p.Name).HasMaxLength(Project.NameMaxLength);
            project.HasIndex(p => p.Name).IsUnique();
        });
    }
}
