using Microsoft.EntityFrameworkCore;
using RotationTracker.Domain;
using RotationTracker.Infrastructure;

namespace RotationTracker.Features.Services;

public sealed record CreateServiceResult(Guid? ServiceId, string? Error);

public sealed class CreateServiceHandler(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<CreateServiceResult> Handle(Guid projectId, string name, string? description, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var project = await db.Projects.FindAsync([projectId], ct);
        if (project is null)
            return new(null, "Project not found.");

        Service service;
        try
        {
            service = Service.Create(projectId, name, description);
        }
        catch (ArgumentException ex)
        {
            return new(null, ex.Message);
        }

        if (await db.Services.AnyAsync(s => s.ProjectId == projectId && s.Name == service.Name, ct))
            return new(null, $"A service named '{service.Name}' already exists in project '{project.Name}'.");

        db.Services.Add(service);
        await db.SaveChangesAsync(ct);
        return new(service.Id, null);
    }
}
