using Microsoft.Playwright;
using Reqnroll;
using static Microsoft.Playwright.Assertions;

namespace RotationTracker.E2E.Features.Services;

[Binding]
public sealed class ServiceSteps(IPage page)
{
    [Given("project {string} has a service named {string}")]
    public async Task ServiceExists(string project, string name)
    {
        await Create(name, null, project);
        await page.WaitForURLAsync("**/projects");
    }

    [When("I create a service named {string} in project {string}")]
    public Task CreateService(string name, string project) => Create(name, null, project);

    [When("I create a service named {string} with description {string} in project {string}")]
    public Task CreateServiceWithDescription(string name, string description, string project) =>
        Create(name, description, project);

    [When("I create a service named {string} with a {int}-character description in project {string}")]
    public Task CreateServiceWithLongDescription(string name, int length, string project) =>
        Create(name, new string('x', length), project);

    private async Task Create(string name, string? description, string project)
    {
        await page.GotoAsync("projects");
        await Project(project).GetByRole(AriaRole.Link, new() { Name = "Add service" }).ClickAsync();
        await page.GetByLabel("Name").FillAsync(name);
        if (description is not null)
            await page.GetByLabel("Description").FillAsync(description);
        await page.GetByRole(AriaRole.Button, new() { Name = "Create" }).ClickAsync();
    }

    [Then("project {string} lists the service {string}")]
    public Task ProjectListsService(string project, string name) =>
        Expect(Service(project, name)).ToBeVisibleAsync();

    [Then("project {string} lists the service {string} with description {string}")]
    public Task ProjectListsServiceWithDescription(string project, string name, string description) =>
        Expect(Service(project, name)).ToContainTextAsync(description);

    [Then("project {string} lists no services")]
    public Task ProjectListsNoServices(string project) =>
        Expect(Project(project).GetByText("No services yet.")).ToBeVisibleAsync();

    [Then("I see an error that the description is too long")]
    public Task SeeDescriptionTooLong() =>
        Expect(page.GetByText("The field Description must be a string with a maximum length of 500.")).ToBeVisibleAsync();

    private ILocator Project(string name) =>
        page.GetByRole(AriaRole.Region, new() { Name = name, Exact = true });

    private ILocator Service(string project, string name) =>
        Project(project).GetByRole(AriaRole.Listitem).Filter(new() { HasText = name });
}
