using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.SystemAgents.Queries.GetSystemAgentByName;

/// <summary>
/// Query to retrieve a single system agent by its unique name.
/// </summary>
public sealed record GetSystemAgentByNameQuery
    : IRequest<GetSystemAgentByNameQuery, ValueTask<Result<SystemAgentDto>>>
{
    /// <summary>
    /// The name of the system agent to retrieve.
    /// </summary>
    public required string Name { get; init; }
}
