using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.ProjectTags.Commands.UpdateProjectTag;

/// <summary>
/// Handles the <see cref="UpdateProjectTagCommand"/> by loading the tag entity, applying
/// the requested name and/or color changes, and persisting the result.
/// </summary>
public sealed class UpdateProjectTagCommandHandler
    : IRequestHandler<UpdateProjectTagCommand, ValueTask<Result>>
{
    private readonly IProjectTagRepository _projectTagRepository;
    private readonly ILogger<UpdateProjectTagCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProjectTagCommandHandler"/> class.
    /// </summary>
    /// <param name="projectTagRepository">The project tag repository.</param>
    /// <param name="logger">The logger instance.</param>
    public UpdateProjectTagCommandHandler(
        IProjectTagRepository projectTagRepository,
        ILogger<UpdateProjectTagCommandHandler> logger)
    {
        _projectTagRepository = projectTagRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        UpdateProjectTagCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating project tag {TagId}", request.TagId);

        var tag = await _projectTagRepository.GetByIdAsync(request.TagId, cancellationToken);

        if (tag is null)
        {
            _logger.LogWarning("Project tag {TagId} not found for update", request.TagId);
            return Result.Failure("Tag not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var nameExists = await _projectTagRepository.ExistsByNameAsync(
                tag.ProjectId, request.Name, request.TagId, cancellationToken);

            if (nameExists)
            {
                _logger.LogWarning(
                    "Tag name {TagName} already exists in project {ProjectId} (excluding tag {TagId})",
                    request.Name, tag.ProjectId, request.TagId);

                return Result.Failure("A tag with this name already exists in this project.");
            }

            tag.Rename(request.Name);
        }

        if (request.ClearColor)
        {
            tag.UpdateColor(null);
        }
        else if (request.Color is not null)
        {
            tag.UpdateColor(request.Color);
        }

        await _projectTagRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Project tag {TagId} updated successfully in project {ProjectId}",
            tag.Id, tag.ProjectId);

        return Result.Success();
    }
}
