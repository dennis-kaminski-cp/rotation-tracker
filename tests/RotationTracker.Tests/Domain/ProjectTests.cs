using RotationTracker.Domain;

namespace RotationTracker.Tests.Domain;

public sealed class ProjectTests
{
    [Fact]
    public void Create_TrimsName()
    {
        var project = Project.Create("  Payments  ");

        Assert.Equal("Payments", project.Name);
        Assert.NotEqual(Guid.Empty, project.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankName(string name)
    {
        Assert.Throws<ArgumentException>(() => Project.Create(name));
    }

    [Fact]
    public void Create_RejectsNameLongerThanMax()
    {
        Assert.Throws<ArgumentException>(() => Project.Create(new string('x', Project.NameMaxLength + 1)));
    }
}
