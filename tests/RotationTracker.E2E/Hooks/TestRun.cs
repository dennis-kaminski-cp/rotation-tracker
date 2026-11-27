using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Reqnroll;
using Reqnroll.BoDi;
using RotationTracker.Infrastructure;
using Testcontainers.PostgreSql;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace RotationTracker.E2E.Hooks;

// One Postgres container and one app instance per test run, on random ports,
// so several worktrees can run the suite at the same time.
[Binding]
public sealed class TestRun
{
    private static PostgreSqlContainer database = null!;
    private static AppFactory app = null!;
    private static IPlaywright playwright = null!;
    private static IBrowser browser = null!;

    [BeforeTestRun]
    public static async Task StartAsync()
    {
        if (Microsoft.Playwright.Program.Main(["install", "chromium"]) != 0)
            throw new InvalidOperationException("Playwright browser install failed.");

        database = new PostgreSqlBuilder("postgres:18").Build();
        await database.StartAsync();

        app = new AppFactory(database.GetConnectionString());
        app.UseKestrel(0);
        app.StartServer();

        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync();
    }

    [AfterTestRun]
    public static async Task StopAsync()
    {
        await browser.DisposeAsync();
        playwright.Dispose();
        await app.DisposeAsync();
        await database.DisposeAsync();
    }

    [BeforeScenario]
    public static async Task StartScenarioAsync(IObjectContainer container)
    {
        await ResetDatabaseAsync();
        var context = await browser.NewContextAsync(new() { BaseURL = app.ClientOptions.BaseAddress.ToString() });
        container.RegisterInstanceAs(await context.NewPageAsync());
        container.RegisterInstanceAs(context);
    }

    [AfterScenario]
    public static Task StopScenarioAsync(IBrowserContext context) => context.CloseAsync();

    private static async Task ResetDatabaseAsync()
    {
        var dbFactory = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var tables = db.Model.GetEntityTypes().Select(t => $"\"{t.GetTableName()}\"");
        var sql = $"TRUNCATE {string.Join(", ", tables)} CASCADE";
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    private sealed class AppFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = connectionString,
                }));
        }
    }
}
