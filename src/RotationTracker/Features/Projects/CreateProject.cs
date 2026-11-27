using Microsoft.EntityFrameworkCore;
using RotationTracker.Domain;
using RotationTracker.Infrastructure;

namespace RotationTracker.Features.Projects;

public sealed record CreateProjectResult(Guid? ProjectId, string? Error);

public sealed class CreateProjectHandler(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<CreateProjectResult> Handle(string name, CancellationToken ct = default)
    {
        Project project;
        try
        {
            project = Project.Create(name);
        }
        catch (ArgumentException ex)
        {
            return new(null, ex.Message);
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await db.Projects.AnyAsync(p => p.Name == project.Name, ct))
        {
            return new(null, $"A project named '{project.Name}' already exists.");
        }

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return new(project.Id, null);
    }
}
