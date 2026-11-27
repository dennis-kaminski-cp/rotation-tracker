namespace RotationTracker.Domain;

public sealed class Project
{
    public const int NameMaxLength = 100;

    public Guid Id { get; private set; }
    public string Name { get; private set; }

    private Project(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public static Project Create(string name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Project name is required.", nameof(name));
        if (trimmed.Length > NameMaxLength)
            throw new ArgumentException($"Project name must be at most {NameMaxLength} characters.", nameof(name));

        return new Project(Guid.CreateVersion7(), trimmed);
    }
}
