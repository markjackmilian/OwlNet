using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Queries.GetProjectById;

/// <summary>
/// Query to retrieve a single project by its ID, regardless of archived status.
/// </summary>
public sealed record GetProjectByIdQuery
    : IRequest<GetProjectByIdQuery, ValueTask<Result<ProjectDto>>>
{
    /// <summary>
    /// The ID of the project to retrieve.
    /// </summary>
    public required Guid Id { get; init; }
}
