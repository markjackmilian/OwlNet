using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Agents.Commands.UpdateAgent;

/// <summary>
/// Command to update (overwrite) the content of an existing agent definition file
/// in a project's <c>.opencode/agents/</c> directory.
/// </summary>
public sealed record UpdateAgentCommand : IRequest<UpdateAgentCommand, ValueTask<Result>>
{
    /// <summary>
    /// The ID of the project that owns the agent file.
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// The agent name (filename without the <c>.md</c> extension) to update.
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    /// The full Markdown content to write, including any YAML frontmatter.
    /// </summary>
    public required string Content { get; init; }
}
