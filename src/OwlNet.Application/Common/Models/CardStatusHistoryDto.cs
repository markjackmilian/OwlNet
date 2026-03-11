using OwlNet.Domain.Enums;

namespace OwlNet.Application.Common.Models;

/// <summary>
/// Read-only projection of a <see cref="OwlNet.Domain.Entities.CardStatusHistory"/> record,
/// including denormalised status names resolved from the related
/// <see cref="OwlNet.Domain.Entities.BoardStatus"/> entries.
/// </summary>
/// <param name="Id">The unique identifier of the history record.</param>
/// <param name="CardId">The identifier of the card whose status changed.</param>
/// <param name="PreviousStatusId">
/// The identifier of the status before the transition,
/// or <see langword="null"/> when the card was first created.
/// </param>
/// <param name="PreviousStatusName">
/// The display name of the previous status (denormalised join),
/// or <see langword="null"/> when the card was first created.
/// </param>
/// <param name="NewStatusId">The identifier of the status after the transition.</param>
/// <param name="NewStatusName">The display name of the new status (denormalised join).</param>
/// <param name="ChangedAt">The UTC timestamp when the status transition occurred.</param>
/// <param name="ChangedBy">The identifier or display name of the actor who triggered the change.</param>
/// <param name="ChangeSource">Indicates whether the transition was performed manually or by an automated trigger.</param>
public sealed record CardStatusHistoryDto(
    Guid Id,
    Guid CardId,
    Guid? PreviousStatusId,
    string? PreviousStatusName,
    Guid NewStatusId,
    string NewStatusName,
    DateTimeOffset ChangedAt,
    string ChangedBy,
    ChangeSource ChangeSource);
