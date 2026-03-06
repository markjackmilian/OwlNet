using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Commands.ToggleProjectFavorite;

/// <summary>
/// Handles the <see cref="ToggleProjectFavoriteCommand"/> by toggling the favorite status of a project.
/// </summary>
public sealed class ToggleProjectFavoriteCommandHandler
    : IRequestHandler<ToggleProjectFavoriteCommand, ValueTask<Result>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<ToggleProjectFavoriteCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToggleProjectFavoriteCommandHandler"/> class.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="logger">The logger instance.</param>
    public ToggleProjectFavoriteCommandHandler(
        IProjectRepository projectRepository,
        ILogger<ToggleProjectFavoriteCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        ToggleProjectFavoriteCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Toggling favorite for project {ProjectId}", request.Id);

        var project = await _projectRepository.GetEntityByIdAsync(request.Id, cancellationToken);

        if (project is null)
        {
            _logger.LogWarning("Cannot toggle favorite: project {ProjectId} not found", request.Id);
            return Result.Failure("Project not found.");
        }

        project.ToggleFavorite();
        await _projectRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Project {ProjectId} favorite toggled to {IsFavorited}", project.Id, project.IsFavorited);

        return Result.Success();
    }
}
