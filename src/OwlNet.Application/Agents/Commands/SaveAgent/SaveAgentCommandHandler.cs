using System.Text.RegularExpressions;
using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Agents.Commands.SaveAgent;

/// <summary>
/// Handles the <see cref="SaveAgentCommand"/> by validating the project state,
/// checking for duplicate agent names, and writing the agent definition file to disk.
/// </summary>
public sealed partial class SaveAgentCommandHandler
    : IRequestHandler<SaveAgentCommand, ValueTask<Result<string>>>
{
    /// <summary>
    /// Regex pattern for valid agent names: alphanumeric and hyphens, must start and end
    /// with a letter or number, between 2 and 50 characters.
    /// </summary>
    [GeneratedRegex(@"^[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?$")]
    private static partial Regex AgentNameRegex();

    private readonly IProjectRepository _projectRepository;
    private readonly IAgentFileService _agentFileService;
    private readonly ILogger<SaveAgentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveAgentCommandHandler"/> class.
    /// </summary>
    /// <param name="projectRepository">The project repository for looking up project details.</param>
    /// <param name="agentFileService">The agent file service for filesystem operations.</param>
    /// <param name="logger">The logger instance.</param>
    public SaveAgentCommandHandler(
        IProjectRepository projectRepository,
        IAgentFileService agentFileService,
        ILogger<SaveAgentCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _agentFileService = agentFileService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<string>> Handle(
        SaveAgentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Saving agent {AgentName} for project {ProjectId}",
            request.AgentName, request.ProjectId);

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
            return Result<string>.Failure("Project not found.");
        }

        if (project.IsArchived)
        {
            _logger.LogWarning(
                "Cannot create agent in archived project {ProjectId}", request.ProjectId);
            return Result<string>.Failure("Cannot create agents in an archived project.");
        }

        if (request.AgentName.Length < 2 || request.AgentName.Length > 50)
        {
            _logger.LogWarning(
                "Agent name {AgentName} has invalid length ({Length}) for project {ProjectId}",
                request.AgentName, request.AgentName.Length, request.ProjectId);
            return Result<string>.Failure(
                "Agent name must be between 2 and 50 characters.");
        }

        if (!AgentNameRegex().IsMatch(request.AgentName))
        {
            _logger.LogWarning(
                "Invalid agent name {AgentName} for project {ProjectId}",
                request.AgentName, request.ProjectId);
            return Result<string>.Failure(
                "Agent name can only contain letters, numbers, and hyphens, and must start and end with a letter or number.");
        }

        var existingAgent = await _agentFileService.GetAgentAsync(
            project.Path, request.AgentName, cancellationToken);

        if (existingAgent is not null)
        {
            _logger.LogWarning(
                "Agent {AgentName} already exists in project {ProjectId}",
                request.AgentName, request.ProjectId);
            return Result<string>.Failure("An agent with this name already exists.");
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
            return Result<string>.Failure("Failed to save agent file. Please check filesystem permissions and try again.");
        }

        var filePath = $"{project.Path}/.opencode/agents/{request.AgentName}.md";

        _logger.LogInformation(
            "Agent {AgentName} saved successfully at {FilePath} for project {ProjectId}",
            request.AgentName, filePath, request.ProjectId);

        return Result<string>.Success(filePath);
    }
}
