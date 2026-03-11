using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Queries.GetCardStatusHistory;

/// <summary>
/// Query to retrieve the full status transition history for a specific card,
/// ordered by change timestamp descending (most recent first).
/// </summary>
public sealed record GetCardStatusHistoryQuery
    : IRequest<GetCardStatusHistoryQuery, ValueTask<List<CardStatusHistoryDto>>>
{
    /// <summary>
    /// The identifier of the card whose status history to retrieve.
    /// </summary>
    public required Guid CardId { get; init; }
}
