using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.ProjectTags.Commands.DeleteProjectTag;

/// <summary>
/// Handles the <see cref="DeleteProjectTagCommand"/> by loading the tag entity and
/// performing a hard delete. Associated <c>CardTag</c> join records are cascade-deleted
/// by the database.
/// </summary>
public sealed class DeleteProjectTagCommandHandler
    : IRequestHandler<DeleteProjectTagCommand, ValueTask<Result>>
{
    private readonly IProjectTagRepository _projectTagRepository;
    private readonly ILogger<DeleteProjectTagCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteProjectTagCommandHandler"/> class.
    /// </summary>
    /// <param name="projectTagRepository">The project tag repository.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteProjectTagCommandHandler(
        IProjectTagRepository projectTagRepository,
        ILogger<DeleteProjectTagCommandHandler> logger)
    {
        _projectTagRepository = projectTagRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        DeleteProjectTagCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deleting project tag {TagId}", request.TagId);

        var tag = await _projectTagRepository.GetByIdAsync(request.TagId, cancellationToken);

        if (tag is null)
        {
            _logger.LogWarning("Project tag {TagId} not found for deletion", request.TagId);
            return Result.Failure("Tag not found.");
        }

        _projectTagRepository.Remove(tag);
        await _projectTagRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Project tag {TagId} deleted successfully from project {ProjectId}",
            tag.Id, tag.ProjectId);

        return Result.Success();
    }
}
