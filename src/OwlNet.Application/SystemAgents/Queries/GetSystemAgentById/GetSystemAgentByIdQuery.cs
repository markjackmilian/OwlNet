using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.SystemAgents.Queries.GetSystemAgentById;

/// <summary>
/// Query to retrieve a single system agent by its unique identifier.
/// </summary>
public sealed record GetSystemAgentByIdQuery
    : IRequest<GetSystemAgentByIdQuery, ValueTask<Result<SystemAgentDto>>>
{
    /// <summary>
    /// The ID of the system agent to retrieve.
    /// </summary>
    public required Guid Id { get; init; }
}
