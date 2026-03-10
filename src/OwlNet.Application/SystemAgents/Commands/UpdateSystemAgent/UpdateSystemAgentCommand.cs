using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.SystemAgents.Commands.UpdateSystemAgent;

/// <summary>
/// Command to update an existing system agent's mutable properties.
/// <see cref="Name"/> is intentionally excluded — it is immutable after creation.
/// </summary>
public sealed record UpdateSystemAgentCommand : IRequest<UpdateSystemAgentCommand, ValueTask<Result>>
{
    /// <summary>
    /// The ID of the system agent to update.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The new human-readable label shown in the UI. Must be 2–100 characters.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The new short summary of the agent's purpose. Must be 10–500 characters.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The new OpenCode agent mode. Must be one of <c>"primary"</c>, <c>"subagent"</c>, or <c>"all"</c>.
    /// </summary>
    public required string Mode { get; init; }

    /// <summary>
    /// The new full Markdown content of the agent file, including YAML frontmatter and body.
    /// Must not be empty or whitespace.
    /// </summary>
    public required string Content { get; init; }
}
