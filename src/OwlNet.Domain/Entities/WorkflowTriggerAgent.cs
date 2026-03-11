namespace OwlNet.Domain.Entities;

/// <summary>
/// Represents the ordered association between a <see cref="WorkflowTrigger"/> and an agent
/// that should be invoked when the trigger fires.
/// Agents within a trigger are executed sequentially in ascending <see cref="SortOrder"/>.
/// </summary>
public sealed class WorkflowTriggerAgent
{
    /// <summary>
    /// Gets the unique identifier for this trigger-agent association.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the foreign key of the owning <see cref="WorkflowTrigger"/>.
    /// </summary>
    public Guid WorkflowTriggerId { get; private set; }

    /// <summary>
    /// Gets the name of the agent to invoke.
    /// Corresponds to the file name (without extension) of the agent definition
    /// in the project's <c>.opencode/agents/</c> directory.
    /// Must be between 1 and 200 characters and must not be blank.
    /// </summary>
    public string AgentName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the zero-based execution order of this agent within its trigger.
    /// Agents are invoked in ascending order; if one fails the chain is interrupted.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Required by EF Core for materialization. Do not call directly.
    /// </summary>
    private WorkflowTriggerAgent() { }

    /// <summary>
    /// Creates a new <see cref="WorkflowTriggerAgent"/> with the specified properties.
    /// </summary>
    /// <param name="workflowTriggerId">
    /// The identifier of the owning <see cref="WorkflowTrigger"/>.
    /// </param>
    /// <param name="agentName">
    /// The agent file name (without extension). Must not be <see langword="null"/> or whitespace
    /// and must be between 1 and 200 characters.
    /// </param>
    /// <param name="sortOrder">
    /// The zero-based execution order within the trigger's agent chain.
    /// </param>
    /// <returns>A new <see cref="WorkflowTriggerAgent"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="agentName"/> is <see langword="null"/>, empty, whitespace,
    /// or exceeds 200 characters.
    /// </exception>
    public static WorkflowTriggerAgent Create(Guid workflowTriggerId, string agentName, int sortOrder)
    {
        ValidateAgentName(agentName);

        return new WorkflowTriggerAgent
        {
            Id = Guid.NewGuid(),
            WorkflowTriggerId = workflowTriggerId,
            AgentName = agentName,
            SortOrder = sortOrder
        };
    }

    private static void ValidateAgentName(string agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            throw new ArgumentException("Agent name must not be null or whitespace.", nameof(agentName));
        }

        if (agentName.Length > 200)
        {
            throw new ArgumentException("Agent name must not exceed 200 characters.", nameof(agentName));
        }
    }
}
