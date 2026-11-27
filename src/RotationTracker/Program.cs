using Microsoft.EntityFrameworkCore;
using RotationTracker.Components;
using RotationTracker.Features.Projects;
using RotationTracker.Features.Services;
using RotationTracker.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddHealthChecks();

builder.Services.AddScoped<CreateProjectHandler>();
builder.Services.AddScoped<CreateServiceHandler>();

var app = builder.Build();

// ponytail: migrates on startup; with several replicas move this to a Kubernetes init Job.
await using (var db = await app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync())
{
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapHealthChecks("/healthz");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
