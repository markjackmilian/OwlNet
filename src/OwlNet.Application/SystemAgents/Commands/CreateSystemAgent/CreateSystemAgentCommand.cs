using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.SystemAgents.Commands.CreateSystemAgent;

/// <summary>
/// Command to create a new system agent. Returns the created agent's ID on success.
/// </summary>
public sealed record CreateSystemAgentCommand : IRequest<CreateSystemAgentCommand, ValueTask<Result<Guid>>>
{
    /// <summary>
    /// The stable agent identifier used as the default filename (without <c>.md</c> extension).
    /// Must be 2–50 characters and contain only alphanumeric characters and hyphens.
    /// Immutable after creation.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The human-readable label shown in the UI. Must be 2–100 characters.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The short summary of the agent's purpose. Must be 10–500 characters.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The OpenCode agent mode. Must be one of <c>"primary"</c>, <c>"subagent"</c>, or <c>"all"</c>.
    /// </summary>
    public required string Mode { get; init; }

    /// <summary>
    /// The full Markdown content of the agent file, including YAML frontmatter and body.
    /// Must not be empty or whitespace.
    /// </summary>
    public required string Content { get; init; }
}
