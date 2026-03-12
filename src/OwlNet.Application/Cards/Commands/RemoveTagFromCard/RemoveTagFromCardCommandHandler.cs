using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Commands.RemoveTagFromCard;

/// <summary>
/// Handles the <see cref="RemoveTagFromCardCommand"/> by loading the card entity,
/// delegating the removal to the domain entity, and persisting the result.
/// </summary>
/// <remarks>
/// The domain's <c>RemoveTag</c> method is a no-op when the tag is not currently assigned
/// to the card — no error is raised. Only the card's existence is validated.
/// </remarks>
public sealed class RemoveTagFromCardCommandHandler : IRequestHandler<RemoveTagFromCardCommand, ValueTask<Result>>
{
    private readonly ICardRepository _cardRepository;
    private readonly ILogger<RemoveTagFromCardCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveTagFromCardCommandHandler"/> class.
    /// </summary>
    /// <param name="cardRepository">The card repository.</param>
    /// <param name="logger">The logger instance.</param>
    public RemoveTagFromCardCommandHandler(
        ICardRepository cardRepository,
        ILogger<RemoveTagFromCardCommandHandler> logger)
    {
        _cardRepository = cardRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(RemoveTagFromCardCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Removing tag {TagId} from card {CardId}",
            request.TagId, request.CardId);

        var card = await _cardRepository.GetEntityByIdWithTagsAsync(request.CardId, cancellationToken);

        if (card is null)
        {
            _logger.LogWarning("Card {CardId} not found when removing tag {TagId}", request.CardId, request.TagId);
            return Result.Failure("Card not found.");
        }

        card.RemoveTag(request.TagId);

        await _cardRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tag {TagId} removed from card {CardId} (or was not present — no-op)",
            request.TagId, request.CardId);

        return Result.Success();
    }
}
