using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.ProjectTags.Commands.UpdateProjectTag;

/// <summary>
/// Command to update the name and/or color of an existing project tag.
/// Only non-null fields are applied; pass <see cref="ClearColor"/> to explicitly remove the color.
/// </summary>
public sealed record UpdateProjectTagCommand
    : IRequest<UpdateProjectTagCommand, ValueTask<Result>>
{
    /// <summary>
    /// The identifier of the tag to update.
    /// </summary>
    public required Guid TagId { get; init; }

    /// <summary>
    /// The new display name for the tag. When <see langword="null"/> or whitespace, the name
    /// is left unchanged. When provided, must be 1–50 characters and unique within the project
    /// (case-insensitive).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The new hex color code for the tag (e.g., <c>#FF5733</c>). When <see langword="null"/>,
    /// the color is left unchanged unless <see cref="ClearColor"/> is <see langword="true"/>.
    /// When provided, must match the pattern <c>^#[0-9A-Fa-f]{6}$</c>.
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// When <see langword="true"/>, removes the color from the tag regardless of the value of
    /// <see cref="Color"/>. Takes precedence over <see cref="Color"/>.
    /// </summary>
    public bool ClearColor { get; init; }
}
