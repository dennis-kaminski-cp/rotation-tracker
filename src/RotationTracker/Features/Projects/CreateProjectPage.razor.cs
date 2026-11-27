using System.ComponentModel.DataAnnotations;
using RotationTracker.Domain;

namespace RotationTracker.Features.Projects;

public sealed partial class CreateProjectPage
{
    private readonly FormModel form = new();
    private string? error;

    private async Task Submit()
    {
        var result = await Handler.Handle(form.Name);
        if (result.Error is not null)
        {
            error = result.Error;
            return;
        }

        Navigation.NavigateTo("projects");
    }

    private sealed class FormModel
    {
        [Required, StringLength(Project.NameMaxLength)]
        public string Name { get; set; } = "";
    }
}
