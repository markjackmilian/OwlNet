using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Application.Projects.Queries.GetProjectAgents;
using Shouldly;

namespace OwlNet.Tests.Application.Projects;

/// <summary>
/// Unit tests for <see cref="GetProjectAgentsQueryHandler"/>.
/// Covers happy path, project not found, archived project, empty agent list,
/// correct service invocation, and service not called on failure.
/// </summary>
public sealed class GetProjectAgentsQueryHandlerTests
{
    private const string DefaultProjectPath = @"C:\Projects\TestProject";

    private readonly IProjectRepository _projectRepository;
    private readonly IAgentFileService _agentFileService;
    private readonly GetProjectAgentsQueryHandler _sut;

    public GetProjectAgentsQueryHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _agentFileService = Substitute.For<IAgentFileService>();
        _sut = new GetProjectAgentsQueryHandler(
            _projectRepository,
            _agentFileService,
            NullLogger<GetProjectAgentsQueryHandler>.Instance);
    }

    // ──────────────────────────────────────────────
    // Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidProject_ReturnsSuccessWithAgents()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var project = new ProjectDto(projectId, "My Project", DefaultProjectPath, "A project", false, false, now, now);
        var query = new GetProjectAgentsQuery { ProjectId = projectId };

        var agents = new List<AgentFileDto>
        {
            new("owl-coder", @"C:\Projects\TestProject\.opencode\agents\owl-coder.md", "code", "Backend developer", "---\nmode: code\n---\nContent"),
            new("owl-tester", @"C:\Projects\TestProject\.opencode\agents\owl-tester.md", "test", "Test engineer", "---\nmode: test\n---\nContent")
        };

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(project));
        _agentFileService.GetAgentsAsync(DefaultProjectPath, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AgentFileDto>>(agents));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.Count.ShouldBe(2),
            () => result.Value[0].FileName.ShouldBe("owl-coder"),
            () => result.Value[0].Mode.ShouldBe("code"),
            () => result.Value[0].Description.ShouldBe("Backend developer"),
            () => result.Value[1].FileName.ShouldBe("owl-tester"),
            () => result.Value[1].Mode.ShouldBe("test"),
            () => result.Value[1].Description.ShouldBe("Test engineer")
        );
    }

    // ──────────────────────────────────────────────
    // Project Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsFailure()
    {
        // Arrange
        var query = new GetProjectAgentsQuery { ProjectId = Guid.NewGuid() };

        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(null));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Project not found.")
        );
    }

    // ──────────────────────────────────────────────
    // Archived Project
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ArchivedProject_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var archivedProject = new ProjectDto(projectId, "Archived Project", DefaultProjectPath, "Archived", true, false, now, now);
        var query = new GetProjectAgentsQuery { ProjectId = projectId };

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(archivedProject));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Project not found.")
        );
    }

    // ──────────────────────────────────────────────
    // Empty Agents List
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyAgentsList_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var project = new ProjectDto(projectId, "Empty Project", DefaultProjectPath, "No agents", false, false, now, now);
        var query = new GetProjectAgentsQuery { ProjectId = projectId };

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(project));
        _agentFileService.GetAgentsAsync(DefaultProjectPath, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AgentFileDto>>(new List<AgentFileDto>()));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldBeEmpty()
        );
    }

    // ──────────────────────────────────────────────
    // Service Invocation Verification
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidProject_CallsAgentFileServiceWithCorrectPath()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectPath = @"C:\Custom\Path\MyProject";
        var now = DateTimeOffset.UtcNow;
        var project = new ProjectDto(projectId, "Custom Path Project", projectPath, "Custom path", false, false, now, now);
        var query = new GetProjectAgentsQuery { ProjectId = projectId };

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(project));
        _agentFileService.GetAgentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AgentFileDto>>(new List<AgentFileDto>()));

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _agentFileService.Received(1).GetAgentsAsync(projectPath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProjectNotFound_DoesNotCallAgentFileService()
    {
        // Arrange
        var query = new GetProjectAgentsQuery { ProjectId = Guid.NewGuid() };

        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(null));

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _agentFileService.DidNotReceive().GetAgentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ArchivedProject_DoesNotCallAgentFileService()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var archivedProject = new ProjectDto(projectId, "Archived", DefaultProjectPath, "Archived", true, false, now, now);
        var query = new GetProjectAgentsQuery { ProjectId = projectId };

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(archivedProject));

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _agentFileService.DidNotReceive().GetAgentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Agent File Service Exception
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_AgentFileServiceThrows_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var project = new ProjectDto(projectId, "My Project", DefaultProjectPath, "A project", false, false, now, now);
        var query = new GetProjectAgentsQuery { ProjectId = projectId };

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(project));
        _agentFileService.GetAgentsAsync(DefaultProjectPath, Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("Disk read error"));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Failed to load agents.")
        );
    }
}
