using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Queries.GetProjectAgents;

/// <summary>
/// Handles the <see cref="GetProjectAgentsQuery"/> by looking up the project and
/// returning the agent definition files discovered in its filesystem directory.
/// </summary>
public sealed class GetProjectAgentsQueryHandler
    : IRequestHandler<GetProjectAgentsQuery, ValueTask<Result<IReadOnlyList<AgentFileDto>>>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IAgentFileService _agentFileService;
    private readonly ILogger<GetProjectAgentsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetProjectAgentsQueryHandler"/> class.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="agentFileService">The agent file service for filesystem discovery.</param>
    /// <param name="logger">The logger instance.</param>
    public GetProjectAgentsQueryHandler(
        IProjectRepository projectRepository,
        IAgentFileService agentFileService,
        ILogger<GetProjectAgentsQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _agentFileService = agentFileService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<AgentFileDto>>> Handle(
        GetProjectAgentsQuery request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null || project.IsArchived)
        {
            _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
            return Result<IReadOnlyList<AgentFileDto>>.Failure("Project not found.");
        }

        try
        {
            var agents = await _agentFileService.GetAgentsAsync(project.Path, cancellationToken);

            _logger.LogInformation(
                "Retrieved {AgentCount} agents for project {ProjectId}",
                agents.Count,
                request.ProjectId);

            return Result<IReadOnlyList<AgentFileDto>>.Success(agents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agents for project {ProjectId}", request.ProjectId);
            return Result<IReadOnlyList<AgentFileDto>>.Failure("Failed to load agents.");
        }
    }
}
