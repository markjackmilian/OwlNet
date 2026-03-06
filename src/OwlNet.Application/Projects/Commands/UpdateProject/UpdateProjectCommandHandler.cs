using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Commands.UpdateProject;

/// <summary>
/// Handles the <see cref="UpdateProjectCommand"/> by updating an existing project's
/// name and description.
/// </summary>
public sealed class UpdateProjectCommandHandler
    : IRequestHandler<UpdateProjectCommand, ValueTask<Result>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<UpdateProjectCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProjectCommandHandler"/> class.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="logger">The logger instance.</param>
    public UpdateProjectCommandHandler(
        IProjectRepository projectRepository,
        ILogger<UpdateProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        UpdateProjectCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetEntityByIdAsync(request.Id, cancellationToken);

        if (project is null)
        {
            return Result.Failure("Project not found.");
        }

        if (project.IsArchived)
        {
            return Result.Failure("Cannot update an archived project.");
        }

        if (await _projectRepository.ExistsWithNameAsync(request.Name, request.Id, cancellationToken))
        {
            return Result.Failure("A project with this name already exists.");
        }

        project.Update(request.Name, request.Description);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Project {ProjectId} updated", project.Id);

        return Result.Success();
    }
}
