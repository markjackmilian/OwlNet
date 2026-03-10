using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.SystemAgents.Queries.GetAllSystemAgents;

/// <summary>
/// Query to retrieve all system agents ordered by name ascending.
/// </summary>
public sealed record GetAllSystemAgentsQuery
    : IRequest<GetAllSystemAgentsQuery, ValueTask<Result<IReadOnlyList<SystemAgentDto>>>>
{
}
