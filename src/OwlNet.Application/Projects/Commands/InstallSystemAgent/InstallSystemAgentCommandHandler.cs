using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Commands.InstallSystemAgent;

/// <summary>
/// Handles the <see cref="InstallSystemAgentCommand"/> by writing a system agent's content
/// as a Markdown file inside the target project's <c>.opencode/agents/</c> directory.
/// </summary>
public sealed class InstallSystemAgentCommandHandler
    : IRequestHandler<InstallSystemAgentCommand, ValueTask<Result>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ISystemAgentRepository _systemAgentRepository;
    private readonly IAgentFileService _agentFileService;
    private readonly ILogger<InstallSystemAgentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallSystemAgentCommandHandler"/> class.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="systemAgentRepository">The system agent repository.</param>
    /// <param name="agentFileService">The agent file service for filesystem operations.</param>
    /// <param name="logger">The logger instance.</param>
    public InstallSystemAgentCommandHandler(
        IProjectRepository projectRepository,
        ISystemAgentRepository systemAgentRepository,
        IAgentFileService agentFileService,
        ILogger<InstallSystemAgentCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _systemAgentRepository = systemAgentRepository;
        _agentFileService = agentFileService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        InstallSystemAgentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Installing system agent {SystemAgentId} as {FileName} into project {ProjectId}",
            request.SystemAgentId,
            request.FileName,
            request.ProjectId);

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure("Project not found.");
        }

        if (project.IsArchived)
        {
            return Result.Failure("Project is archived.");
        }

        var systemAgent = await _systemAgentRepository.GetByIdAsync(request.SystemAgentId, cancellationToken);

        if (systemAgent is null)
        {
            return Result.Failure("System agent not found.");
        }

        if (!request.AllowOverwrite)
        {
            var existing = await _agentFileService.GetAgentAsync(project.Path, request.FileName, cancellationToken);

            if (existing is not null)
            {
                return Result.Failure("conflict");
            }
        }

        await _agentFileService.WriteAgentAsync(project.Path, request.FileName, systemAgent.Content, cancellationToken);

        _logger.LogInformation(
            "System agent {SystemAgentId} installed as {FileName} in project {ProjectId}",
            request.SystemAgentId,
            request.FileName,
            request.ProjectId);

        return Result.Success();
    }
}
