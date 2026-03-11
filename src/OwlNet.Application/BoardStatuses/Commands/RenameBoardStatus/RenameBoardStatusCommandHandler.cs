using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.BoardStatuses.Commands.RenameBoardStatus;

/// <summary>
/// Handles the <see cref="RenameBoardStatusCommand"/> by renaming an existing board status
/// after verifying uniqueness within its scope (project-level or global defaults).
/// </summary>
public sealed class RenameBoardStatusCommandHandler
    : IRequestHandler<RenameBoardStatusCommand, ValueTask<Result>>
{
    private readonly IBoardStatusRepository _boardStatusRepository;
    private readonly ILogger<RenameBoardStatusCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenameBoardStatusCommandHandler"/> class.
    /// </summary>
    /// <param name="boardStatusRepository">The board status repository.</param>
    /// <param name="logger">The logger instance.</param>
    public RenameBoardStatusCommandHandler(
        IBoardStatusRepository boardStatusRepository,
        ILogger<RenameBoardStatusCommandHandler> logger)
    {
        _boardStatusRepository = boardStatusRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        RenameBoardStatusCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Renaming board status {BoardStatusId} to {NewName}",
            request.Id, request.NewName);

        var status = await _boardStatusRepository.GetEntityByIdAsync(request.Id, cancellationToken);

        if (status is null)
        {
            _logger.LogWarning("Board status {BoardStatusId} not found", request.Id);
            return Result.Failure("Board status not found.");
        }

        if (await _boardStatusRepository.ExistsWithNameInScopeAsync(
                request.NewName, status.ProjectId, excludeId: status.Id, cancellationToken: cancellationToken))
        {
            _logger.LogWarning(
                "Duplicate board status name {NewName} in scope {ProjectId}",
                request.NewName, status.ProjectId);

            return Result.Failure("A status with this name already exists.");
        }

        status.Rename(request.NewName);

        await _boardStatusRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Board status {BoardStatusId} renamed to {NewName} in {Scope}",
            status.Id, status.Name,
            status.ProjectId.HasValue ? $"project {status.ProjectId.Value}" : "global defaults");

        return Result.Success();
    }
}
