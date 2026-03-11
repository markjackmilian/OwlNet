using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.WorkflowTriggers.Commands.UpdateWorkflowTrigger;

/// <summary>
/// Represents a single agent entry in the <see cref="UpdateWorkflowTriggerCommand"/> agent list.
/// </summary>
/// <param name="AgentName">
/// The agent file name (without the <c>.md</c> extension). Must exist in the project's
/// <c>.opencode/agents/</c> directory.
/// </param>
/// <param name="SortOrder">
/// The zero-based execution order of this agent within the trigger's agent chain.
/// Must be greater than or equal to 0.
/// </param>
public sealed record UpdateWorkflowTriggerAgentItem(
    string AgentName,
    int SortOrder);

/// <summary>
/// Command to update an existing workflow trigger.
/// Replaces all mutable fields and rebuilds the agent list atomically.
/// </summary>
public sealed record UpdateWorkflowTriggerCommand
    : IRequest<UpdateWorkflowTriggerCommand, ValueTask<Result>>
{
    /// <summary>
    /// The identifier of the workflow trigger to update.
    /// </summary>
    public required Guid TriggerId { get; init; }

    /// <summary>
    /// The new human-readable label for this trigger. Required, 1–150 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The new source <see cref="OwlNet.Domain.Entities.BoardStatus"/> identifier.
    /// Must differ from <see cref="ToStatusId"/> and must belong to the trigger's project.
    /// </summary>
    public required Guid FromStatusId { get; init; }

    /// <summary>
    /// The new destination <see cref="OwlNet.Domain.Entities.BoardStatus"/> identifier.
    /// Must differ from <see cref="FromStatusId"/> and must belong to the trigger's project.
    /// </summary>
    public required Guid ToStatusId { get; init; }

    /// <summary>
    /// The new shared prompt sent to all agents when this trigger fires.
    /// Required, 1–10 000 characters.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Whether the trigger should be active after the update.
    /// </summary>
    public required bool IsEnabled { get; init; }

    /// <summary>
    /// The new ordered list of agents to invoke when this trigger fires.
    /// Must contain at least one entry. Each agent must exist on the filesystem.
    /// Replaces the previous agent list entirely.
    /// </summary>
    public required IReadOnlyList<UpdateWorkflowTriggerAgentItem> Agents { get; init; }
}
