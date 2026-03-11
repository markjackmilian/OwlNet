using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Application.WorkflowTriggers.Commands.CreateWorkflowTrigger;

/// <summary>
/// Handles the <see cref="CreateWorkflowTriggerCommand"/> by validating the referenced board
/// statuses, creating the <see cref="WorkflowTrigger"/> aggregate with its ordered
/// <see cref="WorkflowTriggerAgent"/> list, and persisting it to the data store.
/// Agent existence on the filesystem is intentionally not validated here — per SPEC-WF3,
/// missing agents must not block saving or execution.
/// </summary>
public sealed class CreateWorkflowTriggerCommandHandler
    : IRequestHandler<CreateWorkflowTriggerCommand, ValueTask<Result<Guid>>>
{
    private readonly IWorkflowTriggerRepository _workflowTriggerRepository;
    private readonly IBoardStatusRepository _boardStatusRepository;
    private readonly ILogger<CreateWorkflowTriggerCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateWorkflowTriggerCommandHandler"/> class.
    /// </summary>
    /// <param name="workflowTriggerRepository">The workflow trigger repository.</param>
    /// <param name="boardStatusRepository">The board status repository.</param>
    /// <param name="logger">The logger instance.</param>
    public CreateWorkflowTriggerCommandHandler(
        IWorkflowTriggerRepository workflowTriggerRepository,
        IBoardStatusRepository boardStatusRepository,
        ILogger<CreateWorkflowTriggerCommandHandler> logger)
    {
        _workflowTriggerRepository = workflowTriggerRepository;
        _boardStatusRepository = boardStatusRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Guid>> Handle(
        CreateWorkflowTriggerCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating workflow trigger {TriggerName} for project {ProjectId} " +
            "(transition {FromStatusId} → {ToStatusId}) with {AgentCount} agent(s)",
            request.Name, request.ProjectId, request.FromStatusId, request.ToStatusId,
            request.Agents.Count);

        var fromStatus = await _boardStatusRepository.GetEntityByIdAsync(
            request.FromStatusId, cancellationToken);

        if (fromStatus is null)
        {
            _logger.LogWarning("Source status {FromStatusId} not found", request.FromStatusId);
            return Result<Guid>.Failure("Source status not found.");
        }

        if (fromStatus.ProjectId != request.ProjectId)
        {
            _logger.LogWarning(
                "Source status {FromStatusId} belongs to project {OwnerProjectId}, not {ProjectId}",
                request.FromStatusId, fromStatus.ProjectId, request.ProjectId);

            return Result<Guid>.Failure("Source status does not belong to this project.");
        }

        var toStatus = await _boardStatusRepository.GetEntityByIdAsync(
            request.ToStatusId, cancellationToken);

        if (toStatus is null)
        {
            _logger.LogWarning("Destination status {ToStatusId} not found", request.ToStatusId);
            return Result<Guid>.Failure("Destination status not found.");
        }

        if (toStatus.ProjectId != request.ProjectId)
        {
            _logger.LogWarning(
                "Destination status {ToStatusId} belongs to project {OwnerProjectId}, not {ProjectId}",
                request.ToStatusId, toStatus.ProjectId, request.ProjectId);

            return Result<Guid>.Failure("Destination status does not belong to this project.");
        }

        var trigger = WorkflowTrigger.Create(
            request.ProjectId,
            request.Name,
            request.FromStatusId,
            request.ToStatusId,
            request.Prompt);

        var agents = request.Agents
            .Select(item => WorkflowTriggerAgent.Create(trigger.Id, item.AgentName, item.SortOrder))
            .ToList();

        trigger.SetAgents(agents);

        await _workflowTriggerRepository.AddAsync(trigger, cancellationToken);
        await _workflowTriggerRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Workflow trigger {TriggerId} created with name {TriggerName} for project {ProjectId}",
            trigger.Id, trigger.Name, trigger.ProjectId);

        return Result<Guid>.Success(trigger.Id);
    }
}
