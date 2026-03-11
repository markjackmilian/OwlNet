using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Enums;

namespace OwlNet.Application.Cards.Queries.GetCardsByProject;

/// <summary>
/// Query to retrieve all cards belonging to a specific project,
/// with optional filtering by status and/or priority.
/// Results are ordered by card number ascending.
/// </summary>
public sealed record GetCardsByProjectQuery
    : IRequest<GetCardsByProjectQuery, ValueTask<List<CardDto>>>
{
    /// <summary>
    /// The identifier of the project whose cards to retrieve.
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// When provided, only cards whose current status matches this identifier are returned.
    /// Pass <see langword="null"/> to include cards in any status.
    /// </summary>
    public Guid? StatusId { get; init; }

    /// <summary>
    /// When provided, only cards with the specified priority level are returned.
    /// Pass <see langword="null"/> to include cards of any priority.
    /// </summary>
    public CardPriority? Priority { get; init; }
}
