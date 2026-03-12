using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Queries.GetCardComments;

/// <summary>
/// Query to retrieve all comments for a specific card,
/// ordered by creation timestamp ascending (oldest first, chronological thread).
/// </summary>
public sealed record GetCardCommentsQuery
    : IRequest<GetCardCommentsQuery, ValueTask<IReadOnlyList<CardCommentDto>>>
{
    /// <summary>
    /// The identifier of the card whose comments to retrieve.
    /// </summary>
    public required Guid CardId { get; init; }
}
