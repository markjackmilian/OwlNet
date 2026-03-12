using OwlNet.Domain.Enums;

namespace OwlNet.Application.Common.Models;

/// <summary>
/// Read-only projection of a <see cref="OwlNet.Domain.Entities.Card"/> entity,
/// including a denormalised <see cref="StatusName"/> resolved from the related
/// <see cref="OwlNet.Domain.Entities.BoardStatus"/>.
/// </summary>
/// <param name="Id">The unique identifier of the card.</param>
/// <param name="Number">The sequential, human-readable number scoped to the owning project.</param>
/// <param name="Title">The short title of the card.</param>
/// <param name="Description">The full description of the card.</param>
/// <param name="Priority">The priority level of the card.</param>
/// <param name="StatusId">The identifier of the current <see cref="OwlNet.Domain.Entities.BoardStatus"/>.</param>
/// <param name="StatusName">The display name of the current status (denormalised join).</param>
/// <param name="ProjectId">The identifier of the owning project.</param>
/// <param name="CreatedAt">The UTC timestamp when the card was created.</param>
/// <param name="UpdatedAt">The UTC timestamp of the last update to the card.</param>
/// <param name="Tags">The list of tags associated with the card.</param>
public sealed record CardDto(
    Guid Id,
    int Number,
    string Title,
    string Description,
    CardPriority Priority,
    Guid StatusId,
    string StatusName,
    Guid ProjectId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ProjectTagDto> Tags);
