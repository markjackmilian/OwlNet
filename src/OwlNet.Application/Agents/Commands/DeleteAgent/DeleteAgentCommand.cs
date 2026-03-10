using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Agents.Commands.DeleteAgent;

/// <summary>
/// Command to delete an existing agent definition file from a project's
/// <c>.opencode/agents/</c> directory.
/// </summary>
public sealed record DeleteAgentCommand : IRequest<DeleteAgentCommand, ValueTask<Result>>
{
    /// <summary>
    /// The ID of the project that owns the agent file to delete.
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// The agent name (filename without the <c>.md</c> extension) to delete.
    /// </summary>
    public required string AgentName { get; init; }
}
