using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.BoardStatuses.Commands.DeleteBoardStatus;

/// <summary>
/// Handles the <see cref="DeleteBoardStatusCommand"/> by removing the board status
/// from the data store. Rejects deletion when the status is still in use by cards
/// or referenced by one or more workflow triggers.
/// </summary>
public sealed class DeleteBoardStatusCommandHandler
    : IRequestHandler<DeleteBoardStatusCommand, ValueTask<Result>>
{
    private readonly IBoardStatusRepository _boardStatusRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IWorkflowTriggerRepository _workflowTriggerRepository;
    private readonly ILogger<DeleteBoardStatusCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteBoardStatusCommandHandler"/> class.
    /// </summary>
    /// <param name="boardStatusRepository">The board status repository.</param>
    /// <param name="cardRepository">The card repository, used to guard against deleting a status that is still in use by cards.</param>
    /// <param name="workflowTriggerRepository">The workflow trigger repository, used to guard against deleting a status referenced by triggers.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteBoardStatusCommandHandler(
        IBoardStatusRepository boardStatusRepository,
        ICardRepository cardRepository,
        IWorkflowTriggerRepository workflowTriggerRepository,
        ILogger<DeleteBoardStatusCommandHandler> logger)
    {
        _boardStatusRepository = boardStatusRepository;
        _cardRepository = cardRepository;
        _workflowTriggerRepository = workflowTriggerRepository;
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

        if (await _cardRepository.ExistsWithStatusAsync(request.Id, cancellationToken))
        {
            _logger.LogWarning(
                "Cannot delete board status {BoardStatusId} — cards are currently in this status",
                request.Id);
            return Result.Failure("Cannot delete status — cards are currently in this status. Move them first.");
        }

        if (await _workflowTriggerRepository.ExistsWithStatusIdAsync(request.Id, cancellationToken))
        {
            _logger.LogWarning(
                "Cannot delete board status {BoardStatusId} — it is referenced by one or more workflow triggers",
                request.Id);
            return Result.Failure("Cannot delete status — it is referenced by one or more workflow triggers. Update or delete the triggers first.");
        }

        _boardStatusRepository.Remove(status);
        await _boardStatusRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Board status {BoardStatusId} with name {BoardStatusName} deleted successfully",
            status.Id, status.Name);

        return Result.Success();
    }
}
