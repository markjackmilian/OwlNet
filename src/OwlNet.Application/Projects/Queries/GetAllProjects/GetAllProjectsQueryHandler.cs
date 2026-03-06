using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Queries.GetAllProjects;

/// <summary>
/// Handles the <see cref="GetAllProjectsQuery"/> by returning all non-archived projects
/// ordered by name ascending.
/// </summary>
public sealed class GetAllProjectsQueryHandler
    : IRequestHandler<GetAllProjectsQuery, ValueTask<Result<List<ProjectDto>>>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<GetAllProjectsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllProjectsQueryHandler"/> class.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetAllProjectsQueryHandler(
        IProjectRepository projectRepository,
        ILogger<GetAllProjectsQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<List<ProjectDto>>> Handle(
        GetAllProjectsQuery request,
        CancellationToken cancellationToken)
    {
        var projects = await _projectRepository.GetAllActiveAsync(cancellationToken);

        _logger.LogDebug("Retrieved {ProjectCount} active projects", projects.Count);

        return Result<List<ProjectDto>>.Success(projects);
    }
}
