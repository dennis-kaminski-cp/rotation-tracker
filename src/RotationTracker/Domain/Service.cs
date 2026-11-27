namespace RotationTracker.Domain;

public sealed class Service
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    private Service(Guid id, Guid projectId, string name, string? description)
    {
        Id = id;
        ProjectId = projectId;
        Name = name;
        Description = description;
    }

    public static Service Create(Guid projectId, string name, string? description)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Service name is required.", nameof(name));
        if (trimmed.Length > NameMaxLength)
            throw new ArgumentException($"Service name must be at most {NameMaxLength} characters.", nameof(name));

        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (trimmedDescription?.Length > DescriptionMaxLength)
            throw new ArgumentException($"Service description must be at most {DescriptionMaxLength} characters.", nameof(description));

        return new Service(Guid.CreateVersion7(), projectId, trimmed, trimmedDescription);
    }
}
