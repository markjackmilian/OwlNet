using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Commands.AddTagToCard;

/// <summary>
/// Handles the <see cref="AddTagToCardCommand"/> by loading both the card and the tag,
/// delegating the assignment to the domain entity, and persisting the result.
/// </summary>
/// <remarks>
/// The domain's <c>AddTag</c> method is idempotent — assigning a tag that is already present
/// is a no-op and does not produce an error. Cross-project assignments are rejected by the domain
/// and surfaced as a failure <see cref="Result"/>.
/// </remarks>
public sealed class AddTagToCardCommandHandler : IRequestHandler<AddTagToCardCommand, ValueTask<Result>>
{
    private readonly ICardRepository _cardRepository;
    private readonly IProjectTagRepository _projectTagRepository;
    private readonly ILogger<AddTagToCardCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddTagToCardCommandHandler"/> class.
    /// </summary>
    /// <param name="cardRepository">The card repository.</param>
    /// <param name="projectTagRepository">The project tag repository.</param>
    /// <param name="logger">The logger instance.</param>
    public AddTagToCardCommandHandler(
        ICardRepository cardRepository,
        IProjectTagRepository projectTagRepository,
        ILogger<AddTagToCardCommandHandler> logger)
    {
        _cardRepository = cardRepository;
        _projectTagRepository = projectTagRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(AddTagToCardCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Adding tag {TagId} to card {CardId}",
            request.TagId, request.CardId);

        var card = await _cardRepository.GetEntityByIdWithTagsAsync(request.CardId, cancellationToken);

        if (card is null)
        {
            _logger.LogWarning("Card {CardId} not found when adding tag {TagId}", request.CardId, request.TagId);
            return Result.Failure("Card not found.");
        }

        var tag = await _projectTagRepository.GetByIdAsync(request.TagId, cancellationToken);

        if (tag is null)
        {
            _logger.LogWarning("Tag {TagId} not found when adding to card {CardId}", request.TagId, request.CardId);
            return Result.Failure("Tag not found.");
        }

        var domainResult = card.AddTag(request.TagId, tag.ProjectId);

        if (domainResult.IsFailure)
        {
            _logger.LogWarning(
                "Domain rejected adding tag {TagId} to card {CardId}: {Error}",
                request.TagId, request.CardId, domainResult.Error);

            return Result.Failure(domainResult.Error!);
        }

        await _cardRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tag {TagId} successfully added to card {CardId}",
            request.TagId, request.CardId);

        return Result.Success();
    }
}
