using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Commands.ArchiveProject;

/// <summary>
/// Handles the <see cref="ArchiveProjectCommand"/> by setting <c>IsArchived = true</c>
/// on the target project.
/// </summary>
public sealed class ArchiveProjectCommandHandler
    : IRequestHandler<ArchiveProjectCommand, ValueTask<Result>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<ArchiveProjectCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveProjectCommandHandler"/> class.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="logger">The logger instance.</param>
    public ArchiveProjectCommandHandler(
        IProjectRepository projectRepository,
        ILogger<ArchiveProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        ArchiveProjectCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetEntityByIdAsync(request.Id, cancellationToken);

        if (project is null)
        {
            return Result.Failure("Project not found.");
        }

        if (project.IsArchived)
        {
            return Result.Failure("Project is already archived.");
        }

        project.Archive();
        await _projectRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Project {ProjectId} archived", project.Id);

        return Result.Success();
    }
}
