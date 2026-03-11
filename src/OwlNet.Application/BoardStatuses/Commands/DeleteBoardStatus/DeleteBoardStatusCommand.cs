using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.BoardStatuses.Commands.DeleteBoardStatus;

/// <summary>
/// Command to delete a board status. Deletion is rejected if any cards
/// currently use this status (card entity check to be added in SPEC-WF2).
/// </summary>
public sealed record DeleteBoardStatusCommand
    : IRequest<DeleteBoardStatusCommand, ValueTask<Result>>
{
    /// <summary>
    /// The board status identifier to delete.
    /// </summary>
    public required Guid Id { get; init; }
}
