using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Application.Projects.Commands.CreateProject;

/// <summary>
/// Handles the <see cref="CreateProjectCommand"/> by creating a new project entity
/// and persisting it to the data store.
/// </summary>
public sealed class CreateProjectCommandHandler
    : IRequestHandler<CreateProjectCommand, ValueTask<Result<Guid>>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<CreateProjectCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProjectCommandHandler"/> class.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="logger">The logger instance.</param>
    public CreateProjectCommandHandler(
        IProjectRepository projectRepository,
        ILogger<CreateProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Guid>> Handle(
        CreateProjectCommand request,
        CancellationToken cancellationToken)
    {
        if (await _projectRepository.ExistsWithNameAsync(request.Name, cancellationToken: cancellationToken))
        {
            return Result<Guid>.Failure("A project with this name already exists.");
        }

        var project = Project.Create(request.Name, request.Description);

        await _projectRepository.AddAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Project {ProjectId} created with name {ProjectName}", project.Id, project.Name);

        return Result<Guid>.Success(project.Id);
    }
}
