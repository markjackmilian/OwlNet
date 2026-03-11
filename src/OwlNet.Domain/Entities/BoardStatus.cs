namespace OwlNet.Domain.Entities;

/// <summary>
/// Represents a board column status used on the Kanban board.
/// Serves dual purpose: when <see cref="ProjectId"/> is <see langword="null"/> the status is a
/// global default template applied to new projects; when <see cref="ProjectId"/> has a value the
/// status belongs to that specific project and can be customized independently.
/// </summary>
public sealed class BoardStatus
{
    /// <summary>
    /// Gets the unique identifier for this board status.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the status name displayed as the board column header.
    /// Must be between 1 and 100 characters and must not be blank.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the display order that determines the left-to-right column position on the board.
    /// Lower values appear further to the left.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this status was created from a system default.
    /// This is an informational flag and does not affect behavior.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Gets the optional foreign key to the owning <see cref="Project"/>.
    /// When <see langword="null"/> the status is a global default template;
    /// when set the status belongs to the specified project.
    /// </summary>
    public Guid? ProjectId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this board status was first created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this board status was last modified.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Required by EF Core for materialization. Do not call directly.
    /// </summary>
    private BoardStatus() { }

    /// <summary>
    /// Creates a new <see cref="BoardStatus"/> with the specified properties.
    /// </summary>
    /// <param name="name">
    /// The status name. Must not be <see langword="null"/> or whitespace and must be between 1 and 100 characters.
    /// </param>
    /// <param name="sortOrder">The display order (left-to-right) on the board.</param>
    /// <param name="projectId">
    /// The owning project identifier, or <see langword="null"/> for a global default status.
    /// </param>
    /// <param name="isDefault">
    /// Whether this status was created from a system default. Defaults to <see langword="false"/>.
    /// </param>
    /// <returns>A new <see cref="BoardStatus"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace,
    /// or exceeds 100 characters.
    /// </exception>
    public static BoardStatus Create(string name, int sortOrder, Guid? projectId, bool isDefault = false)
    {
        ValidateName(name);

        var now = DateTimeOffset.UtcNow;

        return new BoardStatus
        {
            Id = Guid.NewGuid(),
            Name = name,
            SortOrder = sortOrder,
            IsDefault = isDefault,
            ProjectId = projectId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Creates a new global default <see cref="BoardStatus"/> that is not tied to any project.
    /// Convenience method for seeding the system's default statuses.
    /// </summary>
    /// <param name="name">
    /// The status name. Must not be <see langword="null"/> or whitespace and must be between 1 and 100 characters.
    /// </param>
    /// <param name="sortOrder">The display order (left-to-right) on the board.</param>
    /// <returns>A new global default <see cref="BoardStatus"/> instance with <see cref="IsDefault"/> set to <see langword="true"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace,
    /// or exceeds 100 characters.
    /// </exception>
    public static BoardStatus CreateGlobalDefault(string name, int sortOrder)
    {
        return Create(name, sortOrder, projectId: null, isDefault: true);
    }

    /// <summary>
    /// Creates a new project-level <see cref="BoardStatus"/> belonging to the specified project.
    /// Convenience method for creating statuses within a project scope.
    /// </summary>
    /// <param name="name">
    /// The status name. Must not be <see langword="null"/> or whitespace and must be between 1 and 100 characters.
    /// </param>
    /// <param name="sortOrder">The display order (left-to-right) on the board.</param>
    /// <param name="projectId">The owning project identifier.</param>
    /// <param name="isDefault">
    /// Whether this status was copied from a system default. Defaults to <see langword="false"/>.
    /// </param>
    /// <returns>A new project-level <see cref="BoardStatus"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace,
    /// or exceeds 100 characters.
    /// </exception>
    public static BoardStatus CreateForProject(string name, int sortOrder, Guid projectId, bool isDefault = false)
    {
        return Create(name, sortOrder, projectId, isDefault);
    }

    /// <summary>
    /// Renames this board status and refreshes the <see cref="UpdatedAt"/> timestamp.
    /// </summary>
    /// <param name="newName">
    /// The new status name. Must not be <see langword="null"/> or whitespace and must be between 1 and 100 characters.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="newName"/> is <see langword="null"/>, empty, whitespace,
    /// or exceeds 100 characters.
    /// </exception>
    public void Rename(string newName)
    {
        ValidateName(newName);

        Name = newName;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the display order of this board status and refreshes the <see cref="UpdatedAt"/> timestamp.
    /// </summary>
    /// <param name="newSortOrder">The new display order value.</param>
    public void UpdateSortOrder(int newSortOrder)
    {
        SortOrder = newSortOrder;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Board status name must not be null or whitespace.", nameof(name));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Board status name must not exceed 100 characters.", nameof(name));
        }
    }
}
