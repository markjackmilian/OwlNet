namespace OwlNet.Application.Common.Models;

/// <summary>
/// Read-only projection of a <see cref="OwlNet.Domain.Entities.BoardStatus"/> entity.
/// </summary>
/// <param name="Id">The unique identifier.</param>
/// <param name="Name">The status display name.</param>
/// <param name="SortOrder">The display order on the board.</param>
/// <param name="IsDefault">Whether this status was created from a system default.</param>
/// <param name="ProjectId">The owning project ID, or <see langword="null"/> for global defaults.</param>
public sealed record BoardStatusDto(
    Guid Id,
    string Name,
    int SortOrder,
    bool IsDefault,
    Guid? ProjectId);
