using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.WorkflowTriggers.Commands.DeleteWorkflowTrigger;

/// <summary>
/// Command to permanently delete a workflow trigger and all its associated agents.
/// Associated <see cref="OwlNet.Domain.Entities.WorkflowTriggerAgent"/> records are
/// cascade-deleted by the database.
/// </summary>
public sealed record DeleteWorkflowTriggerCommand
    : IRequest<DeleteWorkflowTriggerCommand, ValueTask<Result>>
{
    /// <summary>
    /// The identifier of the workflow trigger to delete.
    /// </summary>
    public required Guid TriggerId { get; init; }
}
