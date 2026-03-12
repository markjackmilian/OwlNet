using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Queries.GetCardComments;

/// <summary>
/// Handles the <see cref="GetCardCommentsQuery"/> by returning all comments for a specific card
/// as read-only projections, ordered by creation timestamp ascending (oldest first).
/// </summary>
public sealed class GetCardCommentsQueryHandler
    : IRequestHandler<GetCardCommentsQuery, ValueTask<IReadOnlyList<CardCommentDto>>>
{
    private readonly ICardCommentRepository _cardCommentRepository;
    private readonly ILogger<GetCardCommentsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCardCommentsQueryHandler"/> class.
    /// </summary>
    /// <param name="cardCommentRepository">The card comment repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetCardCommentsQueryHandler(
        ICardCommentRepository cardCommentRepository,
        ILogger<GetCardCommentsQueryHandler> logger)
    {
        _cardCommentRepository = cardCommentRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<CardCommentDto>> Handle(
        GetCardCommentsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving comments for card {CardId}",
            request.CardId);

        var comments = await _cardCommentRepository.GetByCardIdAsync(
            request.CardId,
            cancellationToken);

        _logger.LogInformation(
            "Retrieved {CommentCount} comment(s) for card {CardId}",
            comments.Count, request.CardId);

        return comments;
    }
}
