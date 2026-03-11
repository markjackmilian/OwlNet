using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.WorkflowTriggers.Commands.DeleteWorkflowTrigger;

/// <summary>
/// Handles the <see cref="DeleteWorkflowTriggerCommand"/> by loading the trigger and
/// permanently removing it from the data store. Associated
/// <see cref="OwlNet.Domain.Entities.WorkflowTriggerAgent"/> records are cascade-deleted
/// by the database.
/// </summary>
public sealed class DeleteWorkflowTriggerCommandHandler
    : IRequestHandler<DeleteWorkflowTriggerCommand, ValueTask<Result>>
{
    private readonly IWorkflowTriggerRepository _workflowTriggerRepository;
    private readonly ILogger<DeleteWorkflowTriggerCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteWorkflowTriggerCommandHandler"/> class.
    /// </summary>
    /// <param name="workflowTriggerRepository">The workflow trigger repository.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteWorkflowTriggerCommandHandler(
        IWorkflowTriggerRepository workflowTriggerRepository,
        ILogger<DeleteWorkflowTriggerCommandHandler> logger)
    {
        _workflowTriggerRepository = workflowTriggerRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        DeleteWorkflowTriggerCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting workflow trigger {TriggerId}", request.TriggerId);

        var trigger = await _workflowTriggerRepository.GetEntityByIdAsync(
            request.TriggerId, cancellationToken);

        if (trigger is null)
        {
            _logger.LogWarning(
                "Workflow trigger {TriggerId} not found for deletion", request.TriggerId);

            return Result.Failure("Workflow trigger not found.");
        }

        _workflowTriggerRepository.Remove(trigger);
        await _workflowTriggerRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Workflow trigger {TriggerId} with name {TriggerName} deleted successfully",
            trigger.Id, trigger.Name);

        return Result.Success();
    }
}
