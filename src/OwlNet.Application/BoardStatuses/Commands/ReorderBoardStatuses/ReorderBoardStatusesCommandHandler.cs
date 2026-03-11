using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.BoardStatuses.Commands.ReorderBoardStatuses;

/// <summary>
/// Handles the <see cref="ReorderBoardStatusesCommand"/> by updating the sort order
/// of all board statuses within a scope to match the provided ordered list.
/// </summary>
public sealed class ReorderBoardStatusesCommandHandler
    : IRequestHandler<ReorderBoardStatusesCommand, ValueTask<Result>>
{
    private readonly IBoardStatusRepository _boardStatusRepository;
    private readonly ILogger<ReorderBoardStatusesCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReorderBoardStatusesCommandHandler"/> class.
    /// </summary>
    /// <param name="boardStatusRepository">The board status repository.</param>
    /// <param name="logger">The logger instance.</param>
    public ReorderBoardStatusesCommandHandler(
        IBoardStatusRepository boardStatusRepository,
        ILogger<ReorderBoardStatusesCommandHandler> logger)
    {
        _boardStatusRepository = boardStatusRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        ReorderBoardStatusesCommand request,
        CancellationToken cancellationToken)
    {
        var scope = request.ProjectId.HasValue
            ? $"project {request.ProjectId.Value}"
            : "global defaults";

        _logger.LogInformation(
            "Reordering {StatusCount} board statuses in {Scope}",
            request.OrderedStatusIds.Count, scope);

        var entities = request.ProjectId.HasValue
            ? await _boardStatusRepository.GetEntitiesByProjectIdAsync(request.ProjectId.Value, cancellationToken)
            : await _boardStatusRepository.GetGlobalDefaultEntitiesAsync(cancellationToken);

        if (request.OrderedStatusIds.Count != entities.Count)
        {
            _logger.LogWarning(
                "Reorder rejected — expected {ExpectedCount} statuses but received {ActualCount} IDs for {Scope}",
                entities.Count, request.OrderedStatusIds.Count, scope);

            return Result.Failure("The number of status IDs does not match the number of statuses in this scope.");
        }

        var entityMap = entities.ToDictionary(e => e.Id);

        foreach (var id in request.OrderedStatusIds)
        {
            if (!entityMap.ContainsKey(id))
            {
                _logger.LogWarning(
                    "Reorder rejected — status {BoardStatusId} does not belong to {Scope}",
                    id, scope);

                return Result.Failure("One or more status IDs do not belong to this scope.");
            }
        }

        for (var i = 0; i < request.OrderedStatusIds.Count; i++)
        {
            var entity = entityMap[request.OrderedStatusIds[i]];
            entity.UpdateSortOrder(i);
        }

        await _boardStatusRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully reordered {StatusCount} board statuses in {Scope}",
            request.OrderedStatusIds.Count, scope);

        return Result.Success();
    }
}
