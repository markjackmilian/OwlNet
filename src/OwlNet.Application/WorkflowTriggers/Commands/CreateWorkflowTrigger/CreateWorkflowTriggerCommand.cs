using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.WorkflowTriggers.Commands.CreateWorkflowTrigger;

/// <summary>
/// Represents a single agent entry in the <see cref="CreateWorkflowTriggerCommand"/> agent list.
/// </summary>
/// <param name="AgentName">
/// The agent file name (without the <c>.md</c> extension). Must exist in the project's
/// <c>.opencode/agents/</c> directory.
/// </param>
/// <param name="SortOrder">
/// The zero-based execution order of this agent within the trigger's agent chain.
/// Must be greater than or equal to 0.
/// </param>
public sealed record CreateWorkflowTriggerAgentItem(
    string AgentName,
    int SortOrder);

/// <summary>
/// Command to create a new workflow trigger for a project.
/// The trigger fires when a card transitions from <see cref="FromStatusId"/> to
/// <see cref="ToStatusId"/>, invoking each agent in <see cref="Agents"/> sequentially.
/// </summary>
public sealed record CreateWorkflowTriggerCommand
    : IRequest<CreateWorkflowTriggerCommand, ValueTask<Result<Guid>>>
{
    /// <summary>
    /// The identifier of the project that owns this trigger.
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// The human-readable label for this trigger. Required, 1–150 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The identifier of the source <see cref="OwlNet.Domain.Entities.BoardStatus"/> for the
    /// transition. Must differ from <see cref="ToStatusId"/> and must belong to
    /// <see cref="ProjectId"/>.
    /// </summary>
    public required Guid FromStatusId { get; init; }

    /// <summary>
    /// The identifier of the destination <see cref="OwlNet.Domain.Entities.BoardStatus"/> for
    /// the transition. Must differ from <see cref="FromStatusId"/> and must belong to
    /// <see cref="ProjectId"/>.
    /// </summary>
    public required Guid ToStatusId { get; init; }

    /// <summary>
    /// The shared prompt sent to all agents when this trigger fires.
    /// Required, 1–10 000 characters.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// The ordered list of agents to invoke when this trigger fires.
    /// Must contain at least one entry. Each agent must exist on the filesystem.
    /// </summary>
    public required IReadOnlyList<CreateWorkflowTriggerAgentItem> Agents { get; init; }
}
