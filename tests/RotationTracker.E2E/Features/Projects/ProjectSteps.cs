using Microsoft.Playwright;
using Reqnroll;
using static Microsoft.Playwright.Assertions;

namespace RotationTracker.E2E.Features.Projects;

[Binding]
public sealed class ProjectSteps(IPage page)
{
    [Given("a project named {string} exists")]
    public async Task ProjectExists(string name)
    {
        await CreateProject(name);
        await page.WaitForURLAsync("**/projects");
    }

    [When("I create a project named {string}")]
    public async Task CreateProject(string name)
    {
        await page.GotoAsync("projects/new");
        await page.GetByLabel("Name").FillAsync(name);
        await page.GetByRole(AriaRole.Button, new() { Name = "Create" }).ClickAsync();
    }

    [Then("the project list contains {string}")]
    public Task ProjectListContains(string name)
    {
        return Expect(page.GetByRole(AriaRole.Listitem).Filter(new() { HasText = name })).ToBeVisibleAsync();
    }

    [Then("I see the error {string}")]
    public Task SeeError(string message)
    {
        return Expect(page.GetByText(message)).ToBeVisibleAsync();
    }
}
