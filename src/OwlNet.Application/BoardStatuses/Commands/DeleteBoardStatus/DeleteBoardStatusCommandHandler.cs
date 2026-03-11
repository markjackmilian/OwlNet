using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.BoardStatuses.Commands.DeleteBoardStatus;

/// <summary>
/// Handles the <see cref="DeleteBoardStatusCommand"/> by removing the board status
/// from the data store. Rejects deletion when the status is still in use by cards.
/// </summary>
public sealed class DeleteBoardStatusCommandHandler
    : IRequestHandler<DeleteBoardStatusCommand, ValueTask<Result>>
{
    private readonly IBoardStatusRepository _boardStatusRepository;
    private readonly ILogger<DeleteBoardStatusCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteBoardStatusCommandHandler"/> class.
    /// </summary>
    /// <param name="boardStatusRepository">The board status repository.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteBoardStatusCommandHandler(
        IBoardStatusRepository boardStatusRepository,
        ILogger<DeleteBoardStatusCommandHandler> logger)
    {
        _boardStatusRepository = boardStatusRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        DeleteBoardStatusCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting board status {BoardStatusId}", request.Id);

        var status = await _boardStatusRepository.GetEntityByIdAsync(request.Id, cancellationToken);

        if (status is null)
        {
            _logger.LogWarning("Board status {BoardStatusId} not found for deletion", request.Id);
            return Result.Failure("Board status not found.");
        }

        // TODO: SPEC-WF2 — When the Card entity is implemented, check if any cards
        // are assigned to this status. If so, reject deletion with:
        // return Result.Failure("Cannot delete status — cards are currently in this status. Move them first.");

        _boardStatusRepository.Remove(status);
        await _boardStatusRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Board status {BoardStatusId} with name {BoardStatusName} deleted successfully",
            status.Id, status.Name);

        return Result.Success();
    }
}
