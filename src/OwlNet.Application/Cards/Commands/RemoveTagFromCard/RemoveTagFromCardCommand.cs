using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Commands.RemoveTagFromCard;

/// <summary>
/// Command to remove a <see cref="OwlNet.Domain.Entities.ProjectTag"/> from a card.
/// The operation is a no-op when the tag is not currently assigned to the card —
/// no error is raised in that case.
/// </summary>
public sealed record RemoveTagFromCardCommand : IRequest<RemoveTagFromCardCommand, ValueTask<Result>>
{
    /// <summary>
    /// The identifier of the card from which the tag will be removed.
    /// </summary>
    public required Guid CardId { get; init; }

    /// <summary>
    /// The identifier of the <see cref="OwlNet.Domain.Entities.ProjectTag"/> to remove.
    /// </summary>
    public required Guid TagId { get; init; }
}
