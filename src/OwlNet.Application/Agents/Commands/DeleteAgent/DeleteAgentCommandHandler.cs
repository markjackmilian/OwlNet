using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Agents.Commands.DeleteAgent;

/// <summary>
/// Handles the <see cref="DeleteAgentCommand"/> by validating the project state
/// and deleting the agent definition file from disk.
/// </summary>
public sealed class DeleteAgentCommandHandler
    : IRequestHandler<DeleteAgentCommand, ValueTask<Result>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IAgentFileService _agentFileService;
    private readonly ILogger<DeleteAgentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAgentCommandHandler"/> class.
    /// </summary>
    /// <param name="projectRepository">The project repository for looking up project details.</param>
    /// <param name="agentFileService">The agent file service for filesystem operations.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteAgentCommandHandler(
        IProjectRepository projectRepository,
        IAgentFileService agentFileService,
        ILogger<DeleteAgentCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _agentFileService = agentFileService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        DeleteAgentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Deleting agent {AgentName} for project {ProjectId}",
            request.AgentName, request.ProjectId);

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
            return Result.Failure("Project not found.");
        }

        if (project.IsArchived)
        {
            _logger.LogWarning(
                "Cannot delete agent in archived project {ProjectId}", request.ProjectId);
            return Result.Failure("Cannot delete agents in an archived project.");
        }

        try
        {
            await _agentFileService.DeleteAgentAsync(
                project.Path, request.AgentName, cancellationToken);
        }
        catch (IOException ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete agent file {AgentName} for project {ProjectId}",
                request.AgentName, request.ProjectId);
            return Result.Failure("Failed to delete agent. Check filesystem permissions.");
        }

        _logger.LogInformation(
            "Agent {AgentName} deleted successfully for project {ProjectId}",
            request.AgentName, request.ProjectId);

        return Result.Success();
    }
}
