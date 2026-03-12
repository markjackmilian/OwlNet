namespace OwlNet.Application.Common.Models;

/// <summary>
/// Data transfer object representing a project tag.
/// </summary>
public sealed record ProjectTagDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Color,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
