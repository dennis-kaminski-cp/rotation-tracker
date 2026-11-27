using Microsoft.EntityFrameworkCore;

namespace RotationTracker.Features.Projects;

public sealed partial class ListProjectsPage
{
    private List<string>? projects;

    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        projects = await db.Projects.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync();
    }
}
