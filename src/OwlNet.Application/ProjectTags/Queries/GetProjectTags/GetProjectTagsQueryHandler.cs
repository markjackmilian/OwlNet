using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.ProjectTags.Queries.GetProjectTags;

/// <summary>
/// Handles the <see cref="GetProjectTagsQuery"/> by returning all tags for a given project,
/// ordered by tag name ascending.
/// </summary>
public sealed class GetProjectTagsQueryHandler
    : IRequestHandler<GetProjectTagsQuery, ValueTask<IReadOnlyList<ProjectTagDto>>>
{
    private readonly IProjectTagRepository _projectTagRepository;
    private readonly ILogger<GetProjectTagsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetProjectTagsQueryHandler"/> class.
    /// </summary>
    /// <param name="projectTagRepository">The project tag repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetProjectTagsQueryHandler(
        IProjectTagRepository projectTagRepository,
        ILogger<GetProjectTagsQueryHandler> logger)
    {
        _projectTagRepository = projectTagRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ProjectTagDto>> Handle(
        GetProjectTagsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving tags for project {ProjectId}", request.ProjectId);

        var tags = await _projectTagRepository.GetByProjectIdAsync(
            request.ProjectId, cancellationToken);

        _logger.LogDebug(
            "Retrieved {TagCount} tag(s) for project {ProjectId}",
            tags.Count, request.ProjectId);

        return tags;
    }
}
