using RotationTracker.Domain;

namespace RotationTracker.Tests.Domain;

public sealed class ServiceTests
{
    private static readonly Guid ProjectId = Guid.CreateVersion7();

    [Fact]
    public void Create_TrimsNameAndDescription()
    {
        var service = Service.Create(ProjectId, "  Checkout API  ", "  Handles card payments  ");

        Assert.Equal("Checkout API", service.Name);
        Assert.Equal("Handles card payments", service.Description);
        Assert.Equal(ProjectId, service.ProjectId);
        Assert.NotEqual(Guid.Empty, service.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankName(string name)
    {
        Assert.Throws<ArgumentException>(() => Service.Create(ProjectId, name, null));
    }

    [Fact]
    public void Create_RejectsNameLongerThanMax()
    {
        Assert.Throws<ArgumentException>(() => Service.Create(ProjectId, new string('x', Service.NameMaxLength + 1), null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_StoresBlankDescriptionAsNull(string? description)
    {
        Assert.Null(Service.Create(ProjectId, "Checkout API", description).Description);
    }

    [Fact]
    public void Create_RejectsDescriptionLongerThanMax()
    {
        Assert.Throws<ArgumentException>(() => Service.Create(ProjectId, "Checkout API", new string('x', Service.DescriptionMaxLength + 1)));
    }
}
