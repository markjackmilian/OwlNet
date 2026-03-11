namespace OwlNet.Application.Common.Models;

/// <summary>
/// Read-only projection of a <see cref="OwlNet.Domain.Entities.WorkflowTriggerAgent"/> entity,
/// representing a single agent entry in the ordered execution chain of a workflow trigger.
/// </summary>
/// <param name="Id">The unique identifier of the trigger-agent association.</param>
/// <param name="WorkflowTriggerId">The identifier of the owning <see cref="WorkflowTriggerDto"/>.</param>
/// <param name="AgentName">
/// The file name (without extension) of the agent in the project's
/// <c>.opencode/agents/</c> directory.
/// </param>
/// <param name="SortOrder">
/// The zero-based execution sequence index. Agents are executed in ascending order.
/// </param>
public sealed record WorkflowTriggerAgentDto(
    Guid Id,
    Guid WorkflowTriggerId,
    string AgentName,
    int SortOrder);
