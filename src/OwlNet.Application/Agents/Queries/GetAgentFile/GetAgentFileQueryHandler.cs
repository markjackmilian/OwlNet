using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Agents.Queries.GetAgentFile;

/// <summary>
/// Handles the <see cref="GetAgentFileQuery"/> by looking up the project and
/// returning the requested agent definition file from its filesystem directory.
/// </summary>
public sealed class GetAgentFileQueryHandler
    : IRequestHandler<GetAgentFileQuery, ValueTask<Result<AgentFileDto>>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IAgentFileService _agentFileService;
    private readonly ILogger<GetAgentFileQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAgentFileQueryHandler"/> class.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="agentFileService">The agent file service for filesystem operations.</param>
    /// <param name="logger">The logger instance.</param>
    public GetAgentFileQueryHandler(
        IProjectRepository projectRepository,
        IAgentFileService agentFileService,
        ILogger<GetAgentFileQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _agentFileService = agentFileService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<AgentFileDto>> Handle(
        GetAgentFileQuery request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null || project.IsArchived)
        {
            _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
            return Result<AgentFileDto>.Failure("Project not found.");
        }

        try
        {
            var agent = await _agentFileService.GetAgentAsync(
                project.Path,
                request.AgentName,
                cancellationToken);

            if (agent is null)
            {
                _logger.LogWarning(
                    "Agent {AgentName} not found in project {ProjectId}",
                    request.AgentName,
                    request.ProjectId);
                return Result<AgentFileDto>.Failure("Agent not found.");
            }

            _logger.LogInformation(
                "Retrieved agent {AgentName} for project {ProjectId}",
                request.AgentName,
                request.ProjectId);

            return Result<AgentFileDto>.Success(agent);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load agent {AgentName} for project {ProjectId}",
                request.AgentName,
                request.ProjectId);
            return Result<AgentFileDto>.Failure("Failed to load agent.");
        }
    }
}
