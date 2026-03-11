using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Commands.UpdateCard;

/// <summary>
/// Handles the <see cref="UpdateCardCommand"/> by loading the card entity,
/// applying the title/description/priority update, and persisting the changes.
/// </summary>
public sealed class UpdateCardCommandHandler
    : IRequestHandler<UpdateCardCommand, ValueTask<Result>>
{
    private readonly ICardRepository _cardRepository;
    private readonly ILogger<UpdateCardCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCardCommandHandler"/> class.
    /// </summary>
    /// <param name="cardRepository">The card repository.</param>
    /// <param name="logger">The logger instance.</param>
    public UpdateCardCommandHandler(
        ICardRepository cardRepository,
        ILogger<UpdateCardCommandHandler> logger)
    {
        _cardRepository = cardRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        UpdateCardCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating card {CardId} with title {CardTitle}",
            request.Id, request.Title);

        var card = await _cardRepository.GetEntityByIdAsync(request.Id, cancellationToken);

        if (card is null)
        {
            _logger.LogWarning("Card {CardId} not found for update", request.Id);
            return Result.Failure("Card not found.");
        }

        card.Update(request.Title, request.Description, request.Priority);

        await _cardRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Card {CardId} updated successfully with priority {Priority}",
            card.Id, card.Priority);

        return Result.Success();
    }
}
