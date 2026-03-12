using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Commands.AddTagToCard;

/// <summary>
/// Command to assign an existing <see cref="OwlNet.Domain.Entities.ProjectTag"/> to a card.
/// The operation is idempotent — assigning a tag that is already present on the card is a no-op.
/// The tag must belong to the same project as the card; cross-project assignments are rejected.
/// </summary>
public sealed record AddTagToCardCommand : IRequest<AddTagToCardCommand, ValueTask<Result>>
{
    /// <summary>
    /// The identifier of the card to which the tag will be assigned.
    /// </summary>
    public required Guid CardId { get; init; }

    /// <summary>
    /// The identifier of the <see cref="OwlNet.Domain.Entities.ProjectTag"/> to assign.
    /// Must belong to the same project as the card.
    /// </summary>
    public required Guid TagId { get; init; }
}
