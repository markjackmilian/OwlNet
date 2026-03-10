using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Agents.Queries.GetAgentFile;

/// <summary>
/// Query to retrieve a single agent definition file by name for a given project.
/// Used by the Agent Editor page to load an agent's content.
/// </summary>
public sealed record GetAgentFileQuery
    : IRequest<GetAgentFileQuery, ValueTask<Result<AgentFileDto>>>
{
    /// <summary>
    /// The ID of the project that owns the agent file.
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// The agent name (filename without the <c>.md</c> extension) to retrieve.
    /// </summary>
    public required string AgentName { get; init; }
}
