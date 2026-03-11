using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Application.Cards.Commands.CreateCard;

/// <summary>
/// Handles the <see cref="CreateCardCommand"/> by resolving the project's first board status,
/// computing the next card number, creating the card entity, and persisting it.
/// </summary>
public sealed class CreateCardCommandHandler
    : IRequestHandler<CreateCardCommand, ValueTask<Result<Guid>>>
{
    private readonly ICardRepository _cardRepository;
    private readonly IBoardStatusRepository _boardStatusRepository;
    private readonly ILogger<CreateCardCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCardCommandHandler"/> class.
    /// </summary>
    /// <param name="cardRepository">The card repository.</param>
    /// <param name="boardStatusRepository">The board status repository.</param>
    /// <param name="logger">The logger instance.</param>
    public CreateCardCommandHandler(
        ICardRepository cardRepository,
        IBoardStatusRepository boardStatusRepository,
        ILogger<CreateCardCommandHandler> logger)
    {
        _cardRepository = cardRepository;
        _boardStatusRepository = boardStatusRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Guid>> Handle(
        CreateCardCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating card for project {ProjectId} with title {CardTitle} by {CreatedBy}",
            request.ProjectId, request.Title, request.CreatedBy);

        var statuses = await _boardStatusRepository.GetByProjectIdAsync(
            request.ProjectId, cancellationToken);

        if (statuses.Count == 0)
        {
            _logger.LogWarning(
                "Cannot create card — project {ProjectId} has no statuses configured",
                request.ProjectId);

            return Result<Guid>.Failure("Cannot create card — project has no statuses configured.");
        }

        var defaultStatus = statuses.MinBy(s => s.SortOrder)!;

        _logger.LogDebug(
            "Resolved default status {StatusId} (SortOrder={SortOrder}) for project {ProjectId}",
            defaultStatus.Id, defaultStatus.SortOrder, request.ProjectId);

        var number = await _cardRepository.GetNextNumberAsync(request.ProjectId, cancellationToken);

        var card = Card.Create(
            title: request.Title,
            description: request.Description,
            priority: request.Priority,
            statusId: defaultStatus.Id,
            projectId: request.ProjectId,
            number: number,
            createdBy: request.CreatedBy);

        await _cardRepository.AddAsync(card, cancellationToken);

        try
        {
            await _cardRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.Message.Contains("UNIQUE") || ex.Message.Contains("duplicate"))
        {
            _logger.LogWarning(ex, "Card number conflict for project {ProjectId} — concurrent creation detected", request.ProjectId);
            return Result<Guid>.Failure("Card creation failed due to a conflict. Please retry.");
        }

        _logger.LogInformation(
            "Card {CardId} (#{CardNumber}) created in project {ProjectId} with status {StatusId}",
            card.Id, card.Number, request.ProjectId, defaultStatus.Id);

        return Result<Guid>.Success(card.Id);
    }
}
