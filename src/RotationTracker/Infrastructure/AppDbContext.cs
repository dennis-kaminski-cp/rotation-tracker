using Microsoft.EntityFrameworkCore;
using RotationTracker.Domain;

namespace RotationTracker.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Service> Services => Set<Service>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(project =>
        {
            project.Property(p => p.Name).HasMaxLength(Project.NameMaxLength);
            project.HasIndex(p => p.Name).IsUnique();
        });

        modelBuilder.Entity<Service>(service =>
        {
            service.Property(s => s.Name).HasMaxLength(Service.NameMaxLength);
            service.Property(s => s.Description).HasMaxLength(Service.DescriptionMaxLength);
            service.HasOne<Project>().WithMany().HasForeignKey(s => s.ProjectId);
            service.HasIndex(s => new { s.ProjectId, s.Name }).IsUnique();
        });
    }
}
