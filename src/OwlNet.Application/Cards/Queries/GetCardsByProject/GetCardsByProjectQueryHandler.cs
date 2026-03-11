using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Queries.GetCardsByProject;

/// <summary>
/// Handles the <see cref="GetCardsByProjectQuery"/> by returning all cards for a given project,
/// applying optional status and priority filters, ordered by card number ascending.
/// </summary>
public sealed class GetCardsByProjectQueryHandler
    : IRequestHandler<GetCardsByProjectQuery, ValueTask<List<CardDto>>>
{
    private readonly ICardRepository _cardRepository;
    private readonly ILogger<GetCardsByProjectQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCardsByProjectQueryHandler"/> class.
    /// </summary>
    /// <param name="cardRepository">The card repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetCardsByProjectQueryHandler(
        ICardRepository cardRepository,
        ILogger<GetCardsByProjectQueryHandler> logger)
    {
        _cardRepository = cardRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<List<CardDto>> Handle(
        GetCardsByProjectQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Retrieving cards for project {ProjectId} — StatusId filter: {StatusId}, Priority filter: {Priority}",
            request.ProjectId,
            request.StatusId,
            request.Priority);

        var cards = await _cardRepository.GetByProjectIdAsync(
            request.ProjectId,
            request.StatusId,
            request.Priority,
            cancellationToken);

        _logger.LogDebug(
            "Retrieved {CardCount} card(s) for project {ProjectId}",
            cards.Count,
            request.ProjectId);

        return cards;
    }
}
