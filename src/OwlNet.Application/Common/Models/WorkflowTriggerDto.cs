namespace OwlNet.Application.Common.Models;

/// <summary>
/// Read-only projection of a <see cref="OwlNet.Domain.Entities.WorkflowTrigger"/> entity,
/// including the ordered list of agents that execute when the trigger fires.
/// </summary>
/// <param name="Id">The unique identifier of the workflow trigger.</param>
/// <param name="ProjectId">The identifier of the owning project.</param>
/// <param name="Name">
/// The human-readable label for the trigger (e.g., "Code Review on Develop → Review").
/// </param>
/// <param name="FromStatusId">
/// The identifier of the source <see cref="OwlNet.Domain.Entities.BoardStatus"/>
/// that starts the transition.
/// </param>
/// <param name="ToStatusId">
/// The identifier of the destination <see cref="OwlNet.Domain.Entities.BoardStatus"/>
/// that completes the transition.
/// </param>
/// <param name="Prompt">
/// The shared prompt sent to all agents when the trigger fires (1–10 000 characters).
/// </param>
/// <param name="IsEnabled">
/// Whether the trigger is active. Disabled triggers are not evaluated on status changes.
/// </param>
/// <param name="CreatedAt">The UTC timestamp when the trigger was created.</param>
/// <param name="UpdatedAt">The UTC timestamp of the last update to the trigger.</param>
/// <param name="TriggerAgents">
/// The ordered list of agents to execute sequentially when the trigger fires,
/// sorted by <see cref="WorkflowTriggerAgentDto.SortOrder"/> ascending.
/// </param>
public sealed record WorkflowTriggerDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    Guid FromStatusId,
    Guid ToStatusId,
    string Prompt,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<WorkflowTriggerAgentDto> TriggerAgents);
