using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.BoardStatuses.Queries.GetProjectStatuses;

/// <summary>
/// Handles the <see cref="GetProjectStatusesQuery"/> by returning all board statuses
/// for a specific project ordered by sort order ascending.
/// </summary>
public sealed class GetProjectStatusesQueryHandler
    : IRequestHandler<GetProjectStatusesQuery, ValueTask<List<BoardStatusDto>>>
{
    private readonly IBoardStatusRepository _boardStatusRepository;
    private readonly ILogger<GetProjectStatusesQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetProjectStatusesQueryHandler"/> class.
    /// </summary>
    /// <param name="boardStatusRepository">The board status repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetProjectStatusesQueryHandler(
        IBoardStatusRepository boardStatusRepository,
        ILogger<GetProjectStatusesQueryHandler> logger)
    {
        _boardStatusRepository = boardStatusRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<List<BoardStatusDto>> Handle(
        GetProjectStatusesQuery request,
        CancellationToken cancellationToken)
    {
        var statuses = await _boardStatusRepository.GetByProjectIdAsync(
            request.ProjectId, cancellationToken);

        _logger.LogDebug(
            "Retrieved {StatusCount} board statuses for project {ProjectId}",
            statuses.Count,
            request.ProjectId);

        return statuses;
    }
}
