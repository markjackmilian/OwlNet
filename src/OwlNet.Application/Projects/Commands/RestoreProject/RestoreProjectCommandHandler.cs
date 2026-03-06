using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Commands.RestoreProject;

/// <summary>
/// Handles the <see cref="RestoreProjectCommand"/> by setting <c>IsArchived = false</c>
/// on the target project.
/// </summary>
public sealed class RestoreProjectCommandHandler
    : IRequestHandler<RestoreProjectCommand, ValueTask<Result>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<RestoreProjectCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestoreProjectCommandHandler"/> class.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="logger">The logger instance.</param>
    public RestoreProjectCommandHandler(
        IProjectRepository projectRepository,
        ILogger<RestoreProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        RestoreProjectCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetEntityByIdAsync(request.Id, cancellationToken);

        if (project is null)
        {
            return Result.Failure("Project not found.");
        }

        if (!project.IsArchived)
        {
            return Result.Failure("Project is not archived.");
        }

        project.Restore();
        await _projectRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Project {ProjectId} restored", project.Id);

        return Result.Success();
    }
}
