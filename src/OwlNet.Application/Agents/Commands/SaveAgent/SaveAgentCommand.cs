using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Agents.Commands.SaveAgent;

/// <summary>
/// Command to create a new agent definition file in a project's
/// <c>.opencode/agents/</c> directory. Returns the saved file path on success.
/// </summary>
public sealed record SaveAgentCommand : IRequest<SaveAgentCommand, ValueTask<Result<string>>>
{
    /// <summary>
    /// The ID of the project where the agent file will be created.
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// The agent name used as the filename (without the <c>.md</c> extension).
    /// Must be 2-50 characters, alphanumeric and hyphens only, starting and ending with a letter or number.
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    /// The full Markdown content of the agent definition file, including any YAML frontmatter.
    /// </summary>
    public required string Content { get; init; }
}
