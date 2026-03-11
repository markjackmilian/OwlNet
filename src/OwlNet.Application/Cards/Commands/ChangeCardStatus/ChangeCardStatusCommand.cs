using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Enums;

namespace OwlNet.Application.Cards.Commands.ChangeCardStatus;

/// <summary>
/// Command to transition a card to a different board status.
/// The transition is recorded as an immutable entry in the card's status history.
/// </summary>
public sealed record ChangeCardStatusCommand
    : IRequest<ChangeCardStatusCommand, ValueTask<Result>>
{
    /// <summary>
    /// The identifier of the card whose status is being changed.
    /// </summary>
    public required Guid CardId { get; init; }

    /// <summary>
    /// The identifier of the target <see cref="OwlNet.Domain.Entities.BoardStatus"/>.
    /// Must belong to the same project as the card.
    /// </summary>
    public required Guid NewStatusId { get; init; }

    /// <summary>
    /// The identifier of the actor performing the change — a user ID or an agent/trigger identifier.
    /// </summary>
    public required string ChangedBy { get; init; }

    /// <summary>
    /// Indicates whether the change was performed manually by a user or automatically by a trigger.
    /// </summary>
    public required ChangeSource ChangeSource { get; init; }
}
