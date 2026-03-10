using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Application.Projects.Commands.InstallSystemAgent;
using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Application.Projects.Commands;

/// <summary>
/// Unit tests for <see cref="InstallSystemAgentCommandHandler"/>.
/// Covers the six acceptance criteria from SPEC-SA3-system-agent-install.md:
/// happy path (no conflict), conflict without overwrite, conflict with overwrite,
/// system agent not found, project not found, and archived project.
/// </summary>
public sealed class InstallSystemAgentCommandHandlerTests
{
    private const string DefaultProjectPath = "/projects/test";
    private const string DefaultFileName = "git-agent";

    private readonly IProjectRepository _projectRepository;
    private readonly ISystemAgentRepository _systemAgentRepository;
    private readonly IAgentFileService _agentFileService;
    private readonly InstallSystemAgentCommandHandler _sut;

    public InstallSystemAgentCommandHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _systemAgentRepository = Substitute.For<ISystemAgentRepository>();
        _agentFileService = Substitute.For<IAgentFileService>();
        _sut = new InstallSystemAgentCommandHandler(
            _projectRepository,
            _systemAgentRepository,
            _agentFileService,
            NullLogger<InstallSystemAgentCommandHandler>.Instance);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static ProjectDto CreateProjectDto(
        Guid? id = null,
        string path = DefaultProjectPath,
        bool isArchived = false)
    {
        var now = DateTimeOffset.UtcNow;
        return new ProjectDto(
            id ?? Guid.NewGuid(),
            "Test Project",
            path,
            string.Empty,
            isArchived,
            false,
            now,
            now);
    }

    private static SystemAgent CreateSystemAgent(
        Guid? id = null,
        string name = "git-agent",
        string content = "# Git Agent\nThis is the git agent content.") =>
        SystemAgent.Create(name, "Git Agent", "A git agent for version control operations", "primary", content);

    private static AgentFileDto CreateAgentFileDto(string fileName = DefaultFileName) =>
        new(fileName, $"/projects/test/.opencode/agents/{fileName}.md", "primary", "A git agent", "# Git Agent");

    // ──────────────────────────────────────────────
    // Happy Path — No Conflict
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommandNoExistingFile_ReturnsSuccess()
    {
        // Arrange
        var project = CreateProjectDto();
        var systemAgent = CreateSystemAgent();
        var command = new InstallSystemAgentCommand
        {
            ProjectId = project.Id,
            SystemAgentId = systemAgent.Id,
            FileName = DefaultFileName,
            AllowOverwrite = false
        };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(project));
        _systemAgentRepository.GetByIdAsync(systemAgent.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(systemAgent));
        _agentFileService.GetAgentAsync(project.Path, DefaultFileName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(null));
        _agentFileService.WriteAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.IsFailure.ShouldBeFalse()
        );
    }

    [Fact]
    public async Task Handle_ValidCommandNoExistingFile_CallsWriteAgentWithCorrectParameters()
    {
        // Arrange
        var agentContent = "# Git Agent\nThis is the git agent content.";
        var project = CreateProjectDto(path: "/projects/my-project");
        var systemAgent = CreateSystemAgent(content: agentContent);
        var command = new InstallSystemAgentCommand
        {
            ProjectId = project.Id,
            SystemAgentId = systemAgent.Id,
            FileName = "custom-git",
            AllowOverwrite = false
        };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(project));
        _systemAgentRepository.GetByIdAsync(systemAgent.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(systemAgent));
        _agentFileService.GetAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(null));
        _agentFileService.WriteAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _agentFileService.Received(1).WriteAgentAsync(
            Arg.Is<string>(p => p == "/projects/my-project"),
            Arg.Is<string>(f => f == "custom-git"),
            Arg.Is<string>(c => c == agentContent),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Conflict Without Overwrite
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_FileAlreadyExistsAndAllowOverwriteFalse_ReturnsConflictFailure()
    {
        // Arrange
        var project = CreateProjectDto();
        var systemAgent = CreateSystemAgent();
        var existingFile = CreateAgentFileDto();
        var command = new InstallSystemAgentCommand
        {
            ProjectId = project.Id,
            SystemAgentId = systemAgent.Id,
            FileName = DefaultFileName,
            AllowOverwrite = false
        };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(project));
        _systemAgentRepository.GetByIdAsync(systemAgent.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(systemAgent));
        _agentFileService.GetAgentAsync(project.Path, DefaultFileName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(existingFile));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("conflict")
        );
    }

    [Fact]
    public async Task Handle_FileAlreadyExistsAndAllowOverwriteFalse_DoesNotCallWriteAgent()
    {
        // Arrange
        var project = CreateProjectDto();
        var systemAgent = CreateSystemAgent();
        var existingFile = CreateAgentFileDto();
        var command = new InstallSystemAgentCommand
        {
            ProjectId = project.Id,
            SystemAgentId = systemAgent.Id,
            FileName = DefaultFileName,
            AllowOverwrite = false
        };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(project));
        _systemAgentRepository.GetByIdAsync(systemAgent.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(systemAgent));
        _agentFileService.GetAgentAsync(project.Path, DefaultFileName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(existingFile));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _agentFileService.DidNotReceive().WriteAgentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Conflict With Overwrite
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_FileAlreadyExistsAndAllowOverwriteTrue_ReturnsSuccess()
    {
        // Arrange
        var project = CreateProjectDto();
        var systemAgent = CreateSystemAgent();
        var command = new InstallSystemAgentCommand
        {
            ProjectId = project.Id,
            SystemAgentId = systemAgent.Id,
            FileName = DefaultFileName,
            AllowOverwrite = true
        };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(project));
        _systemAgentRepository.GetByIdAsync(systemAgent.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(systemAgent));
        _agentFileService.WriteAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_FileAlreadyExistsAndAllowOverwriteTrue_CallsWriteAgentWithoutCheckingConflict()
    {
        // Arrange
        var project = CreateProjectDto();
        var systemAgent = CreateSystemAgent();
        var command = new InstallSystemAgentCommand
        {
            ProjectId = project.Id,
            SystemAgentId = systemAgent.Id,
            FileName = DefaultFileName,
            AllowOverwrite = true
        };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(project));
        _systemAgentRepository.GetByIdAsync(systemAgent.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(systemAgent));
        _agentFileService.WriteAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — conflict check is skipped when AllowOverwrite is true
        await _agentFileService.DidNotReceive().GetAgentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _agentFileService.Received(1).WriteAgentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // System Agent Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_SystemAgentNotFound_ReturnsFailure()
    {
        // Arrange
        var project = CreateProjectDto();
        var unknownSystemAgentId = Guid.NewGuid();
        var command = new InstallSystemAgentCommand
        {
            ProjectId = project.Id,
            SystemAgentId = unknownSystemAgentId,
            FileName = DefaultFileName,
            AllowOverwrite = false
        };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(project));
        _systemAgentRepository.GetByIdAsync(unknownSystemAgentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(null));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("System agent not found.")
        );
    }

    [Fact]
    public async Task Handle_SystemAgentNotFound_DoesNotCallAgentFileService()
    {
        // Arrange
        var project = CreateProjectDto();
        var unknownSystemAgentId = Guid.NewGuid();
        var command = new InstallSystemAgentCommand
        {
            ProjectId = project.Id,
            SystemAgentId = unknownSystemAgentId,
            FileName = DefaultFileName,
            AllowOverwrite = false
        };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(project));
        _systemAgentRepository.GetByIdAsync(unknownSystemAgentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(null));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _agentFileService.DidNotReceive().GetAgentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _agentFileService.DidNotReceive().WriteAgentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Project Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsFailure()
    {
        // Arrange
        var unknownProjectId = Guid.NewGuid();
        var command = new InstallSystemAgentCommand
        {
            ProjectId = unknownProjectId,
            SystemAgentId = Guid.NewGuid(),
            FileName = DefaultFileName,
            AllowOverwrite = false
        };

        _projectRepository.GetByIdAsync(unknownProjectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(null));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Project not found.")
        );
    }

    [Fact]
    public async Task Handle_ProjectNotFound_DoesNotQuerySystemAgentOrFileService()
    {
        // Arrange
        var unknownProjectId = Guid.NewGuid();
        var command = new InstallSystemAgentCommand
        {
            ProjectId = unknownProjectId,
            SystemAgentId = Guid.NewGuid(),
            FileName = DefaultFileName,
            AllowOverwrite = false
        };

        _projectRepository.GetByIdAsync(unknownProjectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(null));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — short-circuit: no downstream calls should be made
        await _systemAgentRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _agentFileService.DidNotReceive().GetAgentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _agentFileService.DidNotReceive().WriteAgentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Project Archived
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProjectIsArchived_ReturnsFailure()
    {
        // Arrange
        var archivedProject = CreateProjectDto(isArchived: true);
        var command = new InstallSystemAgentCommand
        {
            ProjectId = archivedProject.Id,
            SystemAgentId = Guid.NewGuid(),
            FileName = DefaultFileName,
            AllowOverwrite = false
        };

        _projectRepository.GetByIdAsync(archivedProject.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(archivedProject));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Project is archived.")
        );
    }

    [Fact]
    public async Task Handle_ProjectIsArchived_DoesNotQuerySystemAgentOrFileService()
    {
        // Arrange
        var archivedProject = CreateProjectDto(isArchived: true);
        var command = new InstallSystemAgentCommand
        {
            ProjectId = archivedProject.Id,
            SystemAgentId = Guid.NewGuid(),
            FileName = DefaultFileName,
            AllowOverwrite = false
        };

        _projectRepository.GetByIdAsync(archivedProject.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(archivedProject));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — short-circuit: no downstream calls should be made
        await _systemAgentRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _agentFileService.DidNotReceive().GetAgentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _agentFileService.DidNotReceive().WriteAgentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
