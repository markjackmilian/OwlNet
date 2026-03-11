using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Queries.GetCardStatusHistory;

/// <summary>
/// Handles the <see cref="GetCardStatusHistoryQuery"/> by returning the full status transition
/// history for a specific card, ordered by change timestamp descending (most recent first).
/// </summary>
public sealed class GetCardStatusHistoryQueryHandler
    : IRequestHandler<GetCardStatusHistoryQuery, ValueTask<List<CardStatusHistoryDto>>>
{
    private readonly ICardRepository _cardRepository;
    private readonly ILogger<GetCardStatusHistoryQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCardStatusHistoryQueryHandler"/> class.
    /// </summary>
    /// <param name="cardRepository">The card repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetCardStatusHistoryQueryHandler(
        ICardRepository cardRepository,
        ILogger<GetCardStatusHistoryQueryHandler> logger)
    {
        _cardRepository = cardRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<List<CardStatusHistoryDto>> Handle(
        GetCardStatusHistoryQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Retrieving status history for card {CardId}",
            request.CardId);

        var history = await _cardRepository.GetStatusHistoryAsync(
            request.CardId,
            cancellationToken);

        _logger.LogDebug(
            "Retrieved {HistoryCount} status history record(s) for card {CardId}",
            history.Count,
            request.CardId);

        return history;
    }
}
