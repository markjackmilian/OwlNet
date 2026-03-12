using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Enums;

namespace OwlNet.Application.Cards.Commands.AddHumanComment;

/// <summary>
/// Handles the <see cref="AddHumanCommentCommand"/> by loading the target card, delegating
/// comment creation to the domain entity, and persisting the new <c>CardComment</c> record.
/// </summary>
public sealed class AddHumanCommentCommandHandler
    : IRequestHandler<AddHumanCommentCommand, ValueTask<Result<Guid>>>
{
    private readonly ICardRepository _cardRepository;
    private readonly ICardCommentRepository _cardCommentRepository;
    private readonly ILogger<AddHumanCommentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddHumanCommentCommandHandler"/> class.
    /// </summary>
    /// <param name="cardRepository">The card repository.</param>
    /// <param name="cardCommentRepository">The card comment repository.</param>
    /// <param name="logger">The logger instance.</param>
    public AddHumanCommentCommandHandler(
        ICardRepository cardRepository,
        ICardCommentRepository cardCommentRepository,
        ILogger<AddHumanCommentCommandHandler> logger)
    {
        _cardRepository = cardRepository;
        _cardCommentRepository = cardCommentRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Guid>> Handle(
        AddHumanCommentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Adding human comment to card {CardId} by author {AuthorId}",
            request.CardId, request.AuthorId);

        var card = await _cardRepository.GetEntityByIdAsync(request.CardId, cancellationToken);

        if (card is null)
        {
            _logger.LogWarning(
                "Card {CardId} not found when adding human comment by {AuthorId}",
                request.CardId, request.AuthorId);

            return Result<Guid>.Failure("Card not found.");
        }

        var comment = card.AddComment(
            request.Content,
            CommentAuthorType.Human,
            authorId: request.AuthorId);

        await _cardCommentRepository.AddAsync(comment, cancellationToken);
        await _cardCommentRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Human comment {CommentId} added to card {CardId} by author {AuthorId}",
            comment.Id, request.CardId, request.AuthorId);

        return Result<Guid>.Success(comment.Id);
    }
}
