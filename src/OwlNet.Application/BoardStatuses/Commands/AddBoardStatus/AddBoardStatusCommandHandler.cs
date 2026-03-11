using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Application.BoardStatuses.Commands.AddBoardStatus;

/// <summary>
/// Handles the <see cref="AddBoardStatusCommand"/> by creating a new board status
/// and persisting it to the data store. Supports both global default and project-level statuses.
/// </summary>
public sealed class AddBoardStatusCommandHandler
    : IRequestHandler<AddBoardStatusCommand, ValueTask<Result<Guid>>>
{
    private readonly IBoardStatusRepository _boardStatusRepository;
    private readonly ILogger<AddBoardStatusCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddBoardStatusCommandHandler"/> class.
    /// </summary>
    /// <param name="boardStatusRepository">The board status repository.</param>
    /// <param name="logger">The logger instance.</param>
    public AddBoardStatusCommandHandler(
        IBoardStatusRepository boardStatusRepository,
        ILogger<AddBoardStatusCommandHandler> logger)
    {
        _boardStatusRepository = boardStatusRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Guid>> Handle(
        AddBoardStatusCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Adding board status {StatusName} to {Scope}",
            request.Name,
            request.ProjectId.HasValue ? $"project {request.ProjectId.Value}" : "global defaults");

        if (await _boardStatusRepository.ExistsWithNameInScopeAsync(
                request.Name, request.ProjectId, cancellationToken: cancellationToken))
        {
            _logger.LogWarning(
                "Duplicate board status name {StatusName} in scope {ProjectId}",
                request.Name, request.ProjectId);

            return Result<Guid>.Failure("A status with this name already exists.");
        }

        var sortOrder = await DetermineNextSortOrderAsync(request.ProjectId, cancellationToken);

        var status = BoardStatus.Create(request.Name, sortOrder, request.ProjectId);

        await _boardStatusRepository.AddAsync(status, cancellationToken);
        await _boardStatusRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Board status {StatusId} created with name {StatusName} and sort order {SortOrder} in {Scope}",
            status.Id, status.Name, sortOrder,
            request.ProjectId.HasValue ? $"project {request.ProjectId.Value}" : "global defaults");

        return Result<Guid>.Success(status.Id);
    }

    /// <summary>
    /// Determines the next sort order value by finding the current maximum in the scope and adding 1.
    /// Returns 0 when no statuses exist in the scope yet.
    /// </summary>
    private async Task<int> DetermineNextSortOrderAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        var existingStatuses = projectId.HasValue
            ? await _boardStatusRepository.GetByProjectIdAsync(projectId.Value, cancellationToken)
            : await _boardStatusRepository.GetGlobalDefaultsAsync(cancellationToken);

        return existingStatuses.Count > 0
            ? existingStatuses.Max(s => s.SortOrder) + 1
            : 0;
    }
}
