using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Queries.GetAllProjects;

/// <summary>
/// Query to retrieve all non-archived projects ordered by name ascending.
/// </summary>
public sealed record GetAllProjectsQuery
    : IRequest<GetAllProjectsQuery, ValueTask<Result<List<ProjectDto>>>>
{
}
