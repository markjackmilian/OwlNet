using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Queries.GetProjectById;

/// <summary>
/// Handles the <see cref="GetProjectByIdQuery"/> by returning a single project
/// regardless of its archived status.
/// </summary>
public sealed class GetProjectByIdQueryHandler
    : IRequestHandler<GetProjectByIdQuery, ValueTask<Result<ProjectDto>>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<GetProjectByIdQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetProjectByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetProjectByIdQueryHandler(
        IProjectRepository projectRepository,
        ILogger<GetProjectByIdQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<ProjectDto>> Handle(
        GetProjectByIdQuery request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);

        if (project is null)
        {
            _logger.LogWarning("Project {ProjectId} not found", request.Id);
            return Result<ProjectDto>.Failure("Project not found.");
        }

        return Result<ProjectDto>.Success(project);
    }
}
