using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Commands.DeleteCard;

/// <summary>
/// Handles the <see cref="DeleteCardCommand"/> by loading the card entity and
/// performing a hard delete. Associated status history is cascade-deleted by the database.
/// </summary>
public sealed class DeleteCardCommandHandler
    : IRequestHandler<DeleteCardCommand, ValueTask<Result>>
{
    private readonly ICardRepository _cardRepository;
    private readonly ILogger<DeleteCardCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCardCommandHandler"/> class.
    /// </summary>
    /// <param name="cardRepository">The card repository.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteCardCommandHandler(
        ICardRepository cardRepository,
        ILogger<DeleteCardCommandHandler> logger)
    {
        _cardRepository = cardRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        DeleteCardCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting card {CardId}", request.Id);

        var card = await _cardRepository.GetEntityByIdAsync(request.Id, cancellationToken);

        if (card is null)
        {
            _logger.LogWarning("Card {CardId} not found for deletion", request.Id);
            return Result.Failure("Card not found.");
        }

        _cardRepository.Remove(card);
        await _cardRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Card {CardId} (#{CardNumber}) deleted successfully from project {ProjectId}",
            card.Id, card.Number, card.ProjectId);

        return Result.Success();
    }
}
