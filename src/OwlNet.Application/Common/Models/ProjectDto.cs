namespace OwlNet.Application.Common.Models;

/// <summary>
/// Data transfer object representing a project.
/// </summary>
/// <param name="Id">The unique project identifier.</param>
/// <param name="Name">The project name.</param>
/// <param name="Description">The optional project description.</param>
/// <param name="IsArchived">Whether the project is archived.</param>
/// <param name="IsFavorited">Whether the project is marked as a favorite.</param>
/// <param name="CreatedAt">When the project was created.</param>
/// <param name="UpdatedAt">When the project was last modified.</param>
public sealed record ProjectDto(
    Guid Id,
    string Name,
    string Description,
    bool IsArchived,
    bool IsFavorited,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
