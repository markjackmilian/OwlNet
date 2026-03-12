using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.ProjectTags.Queries.GetProjectTags;

/// <summary>
/// Query to retrieve all tags belonging to a specific project,
/// ordered by tag name ascending.
/// </summary>
public sealed record GetProjectTagsQuery
    : IRequest<GetProjectTagsQuery, ValueTask<IReadOnlyList<ProjectTagDto>>>
{
    /// <summary>
    /// The identifier of the project whose tags to retrieve.
    /// </summary>
    public required Guid ProjectId { get; init; }
}
