using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Agents.Commands.UpdateAgent;

/// <summary>
/// Handles the <see cref="UpdateAgentCommand"/> by validating the project state
/// and content, then overwriting the agent definition file on disk.
/// </summary>
public sealed class UpdateAgentCommandHandler
    : IRequestHandler<UpdateAgentCommand, ValueTask<Result>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IAgentFileService _agentFileService;
    private readonly ILogger<UpdateAgentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAgentCommandHandler"/> class.
    /// </summary>
    /// <param name="projectRepository">The project repository for looking up project details.</param>
    /// <param name="agentFileService">The agent file service for filesystem operations.</param>
    /// <param name="logger">The logger instance.</param>
    public UpdateAgentCommandHandler(
        IProjectRepository projectRepository,
        IAgentFileService agentFileService,
        ILogger<UpdateAgentCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _agentFileService = agentFileService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        UpdateAgentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating agent {AgentName} for project {ProjectId}",
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
                "Cannot update agent in archived project {ProjectId}", request.ProjectId);
            return Result.Failure("Cannot update agents in an archived project.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            _logger.LogWarning(
                "Agent content is empty for {AgentName} in project {ProjectId}",
                request.AgentName, request.ProjectId);
            return Result.Failure("Agent content cannot be empty.");
        }

        try
        {
            await _agentFileService.WriteAgentAsync(
                project.Path, request.AgentName, request.Content, cancellationToken);
        }
        catch (IOException ex)
        {
            _logger.LogError(
                ex,
                "Failed to write agent file {AgentName} for project {ProjectId}",
                request.AgentName, request.ProjectId);
            return Result.Failure("Failed to save agent. Check filesystem permissions.");
        }

        _logger.LogInformation(
            "Agent {AgentName} updated successfully for project {ProjectId}",
            request.AgentName, request.ProjectId);

        return Result.Success();
    }
}
