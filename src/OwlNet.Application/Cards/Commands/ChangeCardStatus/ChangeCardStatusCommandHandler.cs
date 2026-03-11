using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Commands.ChangeCardStatus;

/// <summary>
/// Handles the <see cref="ChangeCardStatusCommand"/> by loading both the card and the target
/// board status, delegating the transition to the domain entity, and persisting the result.
/// The transition is rejected by the domain when the target status belongs to a different project.
/// </summary>
public sealed class ChangeCardStatusCommandHandler
    : IRequestHandler<ChangeCardStatusCommand, ValueTask<Result>>
{
    private readonly ICardRepository _cardRepository;
    private readonly IBoardStatusRepository _boardStatusRepository;
    private readonly ILogger<ChangeCardStatusCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeCardStatusCommandHandler"/> class.
    /// </summary>
    /// <param name="cardRepository">The card repository.</param>
    /// <param name="boardStatusRepository">The board status repository.</param>
    /// <param name="logger">The logger instance.</param>
    public ChangeCardStatusCommandHandler(
        ICardRepository cardRepository,
        IBoardStatusRepository boardStatusRepository,
        ILogger<ChangeCardStatusCommandHandler> logger)
    {
        _cardRepository = cardRepository;
        _boardStatusRepository = boardStatusRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        ChangeCardStatusCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Changing status of card {CardId} to {NewStatusId} by {ChangedBy} via {ChangeSource}",
            request.CardId, request.NewStatusId, request.ChangedBy, request.ChangeSource);

        var card = await _cardRepository.GetEntityByIdAsync(request.CardId, cancellationToken);

        if (card is null)
        {
            _logger.LogWarning("Card {CardId} not found for status change", request.CardId);
            return Result.Failure("Card not found.");
        }

        var newStatus = await _boardStatusRepository.GetEntityByIdAsync(
            request.NewStatusId, cancellationToken);

        if (newStatus is null)
        {
            _logger.LogWarning(
                "Target status {NewStatusId} not found for card {CardId}",
                request.NewStatusId, request.CardId);

            return Result.Failure("Status not found.");
        }

        // BoardStatus.ProjectId is nullable (null = global default template).
        // Passing Guid.Empty when null causes the domain guard to reject the transition,
        // because Guid.Empty will never match the card's ProjectId.
        var domainResult = card.ChangeStatus(
            newStatusId: request.NewStatusId,
            newStatusProjectId: newStatus.ProjectId ?? Guid.Empty,
            changedBy: request.ChangedBy,
            changeSource: request.ChangeSource);

        if (domainResult.IsFailure)
        {
            _logger.LogWarning(
                "Domain rejected status change for card {CardId} to status {NewStatusId}: {Error}",
                request.CardId, request.NewStatusId, domainResult.Error);

            return Result.Failure(domainResult.Error);
        }

        await _cardRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Card {CardId} status changed to {NewStatusId} successfully",
            card.Id, request.NewStatusId);

        return Result.Success();
    }
}
