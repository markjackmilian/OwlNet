using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.ProjectTags.Commands.CreateProjectTag;

/// <summary>
/// Command to create a new tag in a project's tag vocabulary.
/// Tag names must be unique within the project (case-insensitive).
/// </summary>
public sealed record CreateProjectTagCommand
    : IRequest<CreateProjectTagCommand, ValueTask<Result<Guid>>>
{
    /// <summary>
    /// The display name of the new tag. Required, 1–50 characters.
    /// Must be unique within the project (case-insensitive).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// An optional hex color code for visual display of the tag (e.g., <c>#FF5733</c>).
    /// When provided, must match the pattern <c>^#[0-9A-Fa-f]{6}$</c>.
    /// Pass <see langword="null"/> to create a tag with no color.
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// The identifier of the project this tag belongs to.
    /// </summary>
    public required Guid ProjectId { get; init; }
}
