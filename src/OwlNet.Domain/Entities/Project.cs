namespace OwlNet.Domain.Entities;

/// <summary>
/// Represents a project that groups related work items and resources.
/// A project can be archived to hide it from active views without deleting data.
/// </summary>
public sealed class Project
{
    /// <summary>
    /// Gets the unique identifier for this project.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the project name. Must be between 3 and 100 characters.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the absolute filesystem path of the project folder.
    /// This value is set only via <see cref="Create"/> and is immutable after creation.
    /// Must not be empty or whitespace and must not exceed 500 characters.
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional plain-text description. May be an empty string but never <see langword="null"/>.
    /// Maximum 500 characters.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this project is archived.
    /// Archived projects are hidden from active views but retain all data.
    /// </summary>
    public bool IsArchived { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this project has been marked as a favorite.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool IsFavorited { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this project was first created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this project was last modified.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Required by EF Core for materialization. Do not call directly.
    /// </summary>
    private Project() { }

    /// <summary>
    /// Creates a new <see cref="Project"/> with the specified name, filesystem path, and optional description.
    /// </summary>
    /// <param name="name">
    /// The project name. Must not be <see langword="null"/> or whitespace and must be between 3 and 100 characters.
    /// </param>
    /// <param name="path">
    /// The absolute filesystem path of the project folder. Must not be <see langword="null"/> or whitespace
    /// and must not exceed 500 characters. The value is trimmed before assignment.
    /// </param>
    /// <param name="description">
    /// An optional plain-text description. A <see langword="null"/> value is coerced to <see cref="string.Empty"/>.
    /// Must not exceed 500 characters.
    /// </param>
    /// <returns>A new <see cref="Project"/> instance with <see cref="IsArchived"/> set to <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace, or not between 3 and 100 characters.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="path"/> is <see langword="null"/>, empty, or whitespace, or exceeds 500 characters.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="description"/> exceeds 500 characters.
    /// </exception>
    public static Project Create(string name, string path, string? description)
    {
        ValidateName(name);
        ValidatePath(path);
        ValidateDescription(description);

        var now = DateTimeOffset.UtcNow;

        return new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Path = path.Trim(),
            Description = description ?? string.Empty,
            IsArchived = false,
            IsFavorited = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Updates the project name and description, and refreshes the <see cref="UpdatedAt"/> timestamp.
    /// </summary>
    /// <param name="name">
    /// The new project name. Must not be <see langword="null"/> or whitespace and must be between 3 and 100 characters.
    /// </param>
    /// <param name="description">
    /// The new description. A <see langword="null"/> value is coerced to <see cref="string.Empty"/>.
    /// Must not exceed 500 characters.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace, or not between 3 and 100 characters.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="description"/> exceeds 500 characters.
    /// </exception>
    public void Update(string name, string? description)
    {
        ValidateName(name);
        ValidateDescription(description);

        Name = name;
        Description = description ?? string.Empty;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Archives the project, hiding it from active views while retaining all data.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the project is already archived.
    /// </exception>
    public void Archive()
    {
        if (IsArchived)
        {
            throw new InvalidOperationException("Project is already archived.");
        }

        IsArchived = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Toggles the <see cref="IsFavorited"/> state and refreshes the <see cref="UpdatedAt"/> timestamp.
    /// </summary>
    public void ToggleFavorite()
    {
        IsFavorited = !IsFavorited;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Restores an archived project, making it visible in active views again.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the project is not archived.
    /// </exception>
    public void Restore()
    {
        if (!IsArchived)
        {
            throw new InvalidOperationException("Project is not archived.");
        }

        IsArchived = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name must not be null or whitespace.", nameof(name));
        }

        if (name.Length < 3 || name.Length > 100)
        {
            throw new ArgumentException("Project name must be between 3 and 100 characters.", nameof(name));
        }
    }

    private static void ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Project path must not be null or whitespace.", nameof(path));
        }

        if (path.Trim().Length > 500)
        {
            throw new ArgumentException("Project path must not exceed 500 characters.", nameof(path));
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Length > 500)
        {
            throw new ArgumentException("Project description must not exceed 500 characters.", nameof(description));
        }
    }
}
