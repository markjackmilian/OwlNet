using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Commands.DeleteCard;

/// <summary>
/// Command to permanently delete a card and all its associated status history.
/// This operation is irreversible.
/// </summary>
public sealed record DeleteCardCommand
    : IRequest<DeleteCardCommand, ValueTask<Result>>
{
    /// <summary>
    /// The identifier of the card to delete.
    /// </summary>
    public required Guid Id { get; init; }
}
