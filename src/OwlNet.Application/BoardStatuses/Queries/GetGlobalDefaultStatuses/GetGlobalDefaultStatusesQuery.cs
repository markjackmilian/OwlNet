using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.BoardStatuses.Queries.GetGlobalDefaultStatuses;

/// <summary>
/// Query to retrieve all global default board statuses, ordered by sort order.
/// </summary>
public sealed record GetGlobalDefaultStatusesQuery
    : IRequest<GetGlobalDefaultStatusesQuery, ValueTask<List<BoardStatusDto>>>;
