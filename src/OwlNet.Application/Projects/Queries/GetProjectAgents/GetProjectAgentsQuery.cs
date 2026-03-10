using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Queries.GetProjectAgents;

/// <summary>
/// Query to retrieve all agent definition files for a given project.
/// </summary>
public sealed record GetProjectAgentsQuery
    : IRequest<GetProjectAgentsQuery, ValueTask<Result<IReadOnlyList<AgentFileDto>>>>
{
    /// <summary>
    /// The ID of the project whose agents should be retrieved.
    /// </summary>
    public required Guid ProjectId { get; init; }
}
