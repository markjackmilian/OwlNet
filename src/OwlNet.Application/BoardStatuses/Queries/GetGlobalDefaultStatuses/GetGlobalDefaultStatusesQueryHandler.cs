using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.BoardStatuses.Queries.GetGlobalDefaultStatuses;

/// <summary>
/// Handles the <see cref="GetGlobalDefaultStatusesQuery"/> by returning all global default
/// board statuses ordered by sort order ascending.
/// </summary>
public sealed class GetGlobalDefaultStatusesQueryHandler
    : IRequestHandler<GetGlobalDefaultStatusesQuery, ValueTask<List<BoardStatusDto>>>
{
    private readonly IBoardStatusRepository _boardStatusRepository;
    private readonly ILogger<GetGlobalDefaultStatusesQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGlobalDefaultStatusesQueryHandler"/> class.
    /// </summary>
    /// <param name="boardStatusRepository">The board status repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetGlobalDefaultStatusesQueryHandler(
        IBoardStatusRepository boardStatusRepository,
        ILogger<GetGlobalDefaultStatusesQueryHandler> logger)
    {
        _boardStatusRepository = boardStatusRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<List<BoardStatusDto>> Handle(
        GetGlobalDefaultStatusesQuery request,
        CancellationToken cancellationToken)
    {
        var statuses = await _boardStatusRepository.GetGlobalDefaultsAsync(cancellationToken);

        _logger.LogDebug("Retrieved {StatusCount} global default board statuses", statuses.Count);

        return statuses;
    }
}
