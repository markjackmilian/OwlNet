using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.BoardStatuses.Queries.GetProjectStatuses;

/// <summary>
/// Query to retrieve all board statuses for a specific project, ordered by sort order.
/// </summary>
public sealed record GetProjectStatusesQuery
    : IRequest<GetProjectStatusesQuery, ValueTask<List<BoardStatusDto>>>
{
    /// <summary>
    /// The project identifier to retrieve statuses for.
    /// </summary>
    public required Guid ProjectId { get; init; }
}
