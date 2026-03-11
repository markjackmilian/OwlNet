using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Application.WorkflowTriggers.Commands.UpdateWorkflowTrigger;

/// <summary>
/// Handles the <see cref="UpdateWorkflowTriggerCommand"/> by loading the existing trigger,
/// validating the referenced board statuses, removing the stale agent records from the EF Core
/// change tracker, applying the update, rebuilding the agent list, and persisting the changes.
/// Agent existence on the filesystem is intentionally not validated here — per SPEC-WF3,
/// missing agents must not block saving or execution.
/// </summary>
public sealed class UpdateWorkflowTriggerCommandHandler
    : IRequestHandler<UpdateWorkflowTriggerCommand, ValueTask<Result>>
{
    private readonly IWorkflowTriggerRepository _workflowTriggerRepository;
    private readonly IBoardStatusRepository _boardStatusRepository;
    private readonly ILogger<UpdateWorkflowTriggerCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateWorkflowTriggerCommandHandler"/> class.
    /// </summary>
    /// <param name="workflowTriggerRepository">The workflow trigger repository.</param>
    /// <param name="boardStatusRepository">The board status repository.</param>
    /// <param name="logger">The logger instance.</param>
    public UpdateWorkflowTriggerCommandHandler(
        IWorkflowTriggerRepository workflowTriggerRepository,
        IBoardStatusRepository boardStatusRepository,
        ILogger<UpdateWorkflowTriggerCommandHandler> logger)
    {
        _workflowTriggerRepository = workflowTriggerRepository;
        _boardStatusRepository = boardStatusRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        UpdateWorkflowTriggerCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating workflow trigger {TriggerId} (transition {FromStatusId} → {ToStatusId}) " +
            "with {AgentCount} agent(s), IsEnabled={IsEnabled}",
            request.TriggerId, request.FromStatusId, request.ToStatusId,
            request.Agents.Count, request.IsEnabled);

        var trigger = await _workflowTriggerRepository.GetEntityByIdAsync(
            request.TriggerId, cancellationToken);

        if (trigger is null)
        {
            _logger.LogWarning("Workflow trigger {TriggerId} not found", request.TriggerId);
            return Result.Failure("Workflow trigger not found.");
        }

        var fromStatus = await _boardStatusRepository.GetEntityByIdAsync(
            request.FromStatusId, cancellationToken);

        if (fromStatus is null)
        {
            _logger.LogWarning("Source status {FromStatusId} not found", request.FromStatusId);
            return Result.Failure("Source status not found.");
        }

        if (fromStatus.ProjectId != trigger.ProjectId)
        {
            _logger.LogWarning(
                "Source status {FromStatusId} belongs to project {OwnerProjectId}, not {ProjectId}",
                request.FromStatusId, fromStatus.ProjectId, trigger.ProjectId);

            return Result.Failure("Source status does not belong to this project.");
        }

        var toStatus = await _boardStatusRepository.GetEntityByIdAsync(
            request.ToStatusId, cancellationToken);

        if (toStatus is null)
        {
            _logger.LogWarning("Destination status {ToStatusId} not found", request.ToStatusId);
            return Result.Failure("Destination status not found.");
        }

        if (toStatus.ProjectId != trigger.ProjectId)
        {
            _logger.LogWarning(
                "Destination status {ToStatusId} belongs to project {OwnerProjectId}, not {ProjectId}",
                request.ToStatusId, toStatus.ProjectId, trigger.ProjectId);

            return Result.Failure("Destination status does not belong to this project.");
        }

        trigger.Update(
            request.Name,
            request.FromStatusId,
            request.ToStatusId,
            request.Prompt,
            request.IsEnabled);

        var newAgents = request.Agents
            .Select(item => WorkflowTriggerAgent.Create(trigger.Id, item.AgentName, item.SortOrder))
            .ToList();

        // Snapshot the current agents before SetAgents clears the backing list, then remove them
        // from the EF Core change tracker. Without this, EF Core keeps the old
        // WorkflowTriggerAgent rows as Unchanged and never issues DELETE statements.
        var oldAgents = trigger.TriggerAgents.ToList();
        _workflowTriggerRepository.RemoveAgents(oldAgents);
        trigger.SetAgents(newAgents);

        await _workflowTriggerRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Workflow trigger {TriggerId} updated successfully with name {TriggerName}",
            trigger.Id, trigger.Name);

        return Result.Success();
    }
}
