using System.Text.RegularExpressions;

namespace OwlNet.Domain.Entities;

/// <summary>
/// Represents a tag in a project's vocabulary.
/// Tags are project-scoped and can be assigned to any <see cref="Card"/> within the same project
/// to support categorization and filtering.
/// </summary>
/// <remarks>
/// Tag names are unique within a project (case-insensitive). The uniqueness constraint is
/// enforced at the Application layer (via the repository) rather than here in the domain,
/// because it requires a database query.
/// </remarks>
public sealed class ProjectTag
{
    private static readonly Regex HexColorRegex =
        new(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Gets the unique identifier for this tag.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the foreign key of the <see cref="Project"/> this tag belongs to.
    /// Immutable after creation.
    /// </summary>
    public Guid ProjectId { get; private set; }

    /// <summary>
    /// Gets the display name of this tag.
    /// Must be between 1 and 50 characters and must not be blank.
    /// Unique within the owning project (case-insensitive).
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional hex color code used for visual display of this tag (e.g., <c>#FF5733</c>).
    /// When <see langword="null"/> the UI falls back to a neutral default color.
    /// Maximum 7 characters.
    /// </summary>
    public string? Color { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this tag was first created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this tag was last modified.
    /// Updated on every mutation (<see cref="Rename"/> or <see cref="UpdateColor"/>).
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Required by EF Core for materialization. Do not call directly.
    /// </summary>
    private ProjectTag() { }

    /// <summary>
    /// Creates a new <see cref="ProjectTag"/> and assigns it to the specified project.
    /// </summary>
    /// <param name="name">
    /// The tag name. Must not be <see langword="null"/> or whitespace and must be between
    /// 1 and 50 characters.
    /// </param>
    /// <param name="color">
    /// An optional hex color code (e.g., <c>#FF5733</c>). When provided it must match the
    /// pattern <c>^#[0-9A-Fa-f]{6}$</c>. Pass <see langword="null"/> to create a tag with
    /// no color.
    /// </param>
    /// <param name="projectId">The identifier of the owning <see cref="Project"/>.</param>
    /// <returns>A new <see cref="ProjectTag"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, whitespace, empty,
    /// or exceeds 50 characters; or when <paramref name="color"/> is provided but is not a
    /// valid hex color code.
    /// </exception>
    public static ProjectTag Create(string name, string? color, Guid projectId)
    {
        ValidateName(name);
        ValidateColor(color);

        var now = DateTimeOffset.UtcNow;

        return new ProjectTag
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name.Trim(),
            Color = color,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Renames this tag and refreshes the <see cref="UpdatedAt"/> timestamp.
    /// </summary>
    /// <param name="newName">
    /// The new tag name. Must not be <see langword="null"/> or whitespace and must be between
    /// 1 and 50 characters.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="newName"/> is <see langword="null"/>, whitespace, empty,
    /// or exceeds 50 characters.
    /// </exception>
    public void Rename(string newName)
    {
        ValidateName(newName);

        Name = newName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the color of this tag and refreshes the <see cref="UpdatedAt"/> timestamp.
    /// </summary>
    /// <param name="newColor">
    /// The new hex color code (e.g., <c>#FF5733</c>), or <see langword="null"/> to remove
    /// the color from this tag.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="newColor"/> is provided but is not a valid hex color code.
    /// </exception>
    public void UpdateColor(string? newColor)
    {
        ValidateColor(newColor);

        Color = newColor;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tag name must not be null or whitespace.", nameof(name));
        }

        if (name.Length > 50)
        {
            throw new ArgumentException("Tag name must not exceed 50 characters.", nameof(name));
        }
    }

    private static void ValidateColor(string? color)
    {
        if (color is not null && !HexColorRegex.IsMatch(color))
        {
            throw new ArgumentException(
                "Color must be a valid hex color code (e.g., #FF5733).",
                nameof(color));
        }
    }
}
