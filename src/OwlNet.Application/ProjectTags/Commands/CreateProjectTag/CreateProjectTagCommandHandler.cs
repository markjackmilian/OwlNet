using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Application.ProjectTags.Commands.CreateProjectTag;

/// <summary>
/// Handles the <see cref="CreateProjectTagCommand"/> by verifying name uniqueness within the
/// project, creating the tag entity, and persisting it.
/// </summary>
public sealed class CreateProjectTagCommandHandler
    : IRequestHandler<CreateProjectTagCommand, ValueTask<Result<Guid>>>
{
    private readonly IProjectTagRepository _projectTagRepository;
    private readonly ILogger<CreateProjectTagCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProjectTagCommandHandler"/> class.
    /// </summary>
    /// <param name="projectTagRepository">The project tag repository.</param>
    /// <param name="logger">The logger instance.</param>
    public CreateProjectTagCommandHandler(
        IProjectTagRepository projectTagRepository,
        ILogger<CreateProjectTagCommandHandler> logger)
    {
        _projectTagRepository = projectTagRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Guid>> Handle(
        CreateProjectTagCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Creating project tag with name {TagName} in project {ProjectId}",
            request.Name, request.ProjectId);

        var nameExists = await _projectTagRepository.ExistsByNameAsync(
            request.ProjectId, request.Name, cancellationToken: cancellationToken);

        if (nameExists)
        {
            _logger.LogWarning(
                "Tag name {TagName} already exists in project {ProjectId}",
                request.Name, request.ProjectId);

            return Result<Guid>.Failure("A tag with this name already exists in this project.");
        }

        var tag = ProjectTag.Create(request.Name, request.Color, request.ProjectId);

        await _projectTagRepository.AddAsync(tag, cancellationToken);
        await _projectTagRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Project tag {TagId} with name {TagName} created in project {ProjectId}",
            tag.Id, tag.Name, tag.ProjectId);

        return Result<Guid>.Success(tag.Id);
    }
}
