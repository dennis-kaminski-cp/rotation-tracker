using Microsoft.EntityFrameworkCore;
using Npgsql;
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

        var duplicate = new CreateProjectResult(null, $"A project named '{project.Name}' already exists.");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await db.Projects.AnyAsync(p => p.Name == project.Name, ct))
        {
            return duplicate;
        }

        db.Projects.Add(project);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return duplicate;
        }

        return new(project.Id, null);
    }

    // The check above misses a concurrent create of the same name; the unique index catches it.
    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Projects_Name",
        };
    }
}
