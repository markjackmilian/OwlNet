using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OwlNet.Application.Agents.Queries.GetAgentFile;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using Shouldly;

namespace OwlNet.Tests.Application.Agents.Queries.GetAgentFile;

/// <summary>
/// Unit tests for <see cref="GetAgentFileQueryHandler"/>.
/// Covers happy path, project not found, archived project, agent not found,
/// and service exception scenarios.
/// </summary>
public sealed class GetAgentFileQueryHandlerTests
{
    private const string DefaultProjectPath = @"C:\Projects\TestProject";
    private const string ValidAgentName = "owl-coder";

    private readonly IProjectRepository _projectRepository;
    private readonly IAgentFileService _agentFileService;
    private readonly GetAgentFileQueryHandler _sut;

    public GetAgentFileQueryHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _agentFileService = Substitute.For<IAgentFileService>();
        _sut = new GetAgentFileQueryHandler(
            _projectRepository,
            _agentFileService,
            NullLogger<GetAgentFileQueryHandler>.Instance);
    }

    // ──────────────────────────────────────────────
    // Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var query = CreateQuery(projectId);
        var expectedAgent = CreateAgentFileDto();

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.GetAgentAsync(DefaultProjectPath, ValidAgentName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(expectedAgent));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldBe(expectedAgent)
        );
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsDtoWithCorrectProperties()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var query = CreateQuery(projectId);
        var expectedAgent = CreateAgentFileDto(
            fileName: "owl-reviewer",
            mode: "review",
            description: "Code review agent",
            rawContent: "---\nmode: review\ndescription: Code review agent\n---\n\n# Reviewer");

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.GetAgentAsync(DefaultProjectPath, ValidAgentName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(expectedAgent));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.FileName.ShouldBe("owl-reviewer"),
            () => result.Value.FilePath.ShouldBe($"{DefaultProjectPath}/.opencode/agents/owl-reviewer.md"),
            () => result.Value.Mode.ShouldBe("review"),
            () => result.Value.Description.ShouldBe("Code review agent"),
            () => result.Value.RawContent.ShouldContain("# Reviewer")
        );
    }

    [Fact]
    public async Task Handle_ValidRequest_UsesProjectPathFromDto()
    {
        // Arrange — verify the handler passes the project path from the DTO to the service
        var projectId = Guid.NewGuid();
        var customPath = @"D:\CustomProjects\MyApp";
        var projectDto = CreateProjectDto(projectId, path: customPath);
        var agentName = "my-agent";
        var query = CreateQuery(projectId, agentName: agentName);
        var expectedAgent = CreateAgentFileDto(fileName: agentName);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.GetAgentAsync(customPath, agentName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(expectedAgent));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _agentFileService.Received(1).GetAgentAsync(
            customPath, agentName, Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Project Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var query = CreateQuery(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(null));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Project not found.")
        );
    }

    [Fact]
    public async Task Handle_ProjectNotFound_DoesNotCallAgentFileService()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var query = CreateQuery(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(null));

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _agentFileService.DidNotReceive()
            .GetAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Archived Project
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ArchivedProject_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var archivedProject = CreateProjectDto(projectId, isArchived: true);
        var query = CreateQuery(projectId);

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

    [Fact]
    public async Task Handle_ArchivedProject_DoesNotCallAgentFileService()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var archivedProject = CreateProjectDto(projectId, isArchived: true);
        var query = CreateQuery(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(archivedProject));

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _agentFileService.DidNotReceive()
            .GetAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Agent Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_AgentNotFound_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var query = CreateQuery(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.GetAgentAsync(DefaultProjectPath, ValidAgentName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(null));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Agent not found.")
        );
    }

    // ──────────────────────────────────────────────
    // Service Exception
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_AgentFileServiceThrows_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var query = CreateQuery(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.GetAgentAsync(DefaultProjectPath, ValidAgentName, Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("Access denied"));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Failed to load agent.")
        );
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static GetAgentFileQuery CreateQuery(
        Guid projectId,
        string agentName = ValidAgentName) =>
        new()
        {
            ProjectId = projectId,
            AgentName = agentName
        };

    private static ProjectDto CreateProjectDto(
        Guid projectId,
        string path = DefaultProjectPath,
        bool isArchived = false)
    {
        var now = DateTimeOffset.UtcNow;
        return new ProjectDto(
            Id: projectId,
            Name: "Test Project",
            Path: path,
            Description: "A test project",
            IsArchived: isArchived,
            IsFavorited: false,
            CreatedAt: now,
            UpdatedAt: now);
    }

    private static AgentFileDto CreateAgentFileDto(
        string fileName = ValidAgentName,
        string? filePath = null,
        string mode = "code",
        string description = "A helpful coding assistant",
        string rawContent = "---\nmode: code\ndescription: A helpful coding assistant\n---\n\n# Agent Definition\n\nYou are a helpful assistant.") =>
        new(
            FileName: fileName,
            FilePath: filePath ?? $"{DefaultProjectPath}/.opencode/agents/{fileName}.md",
            Mode: mode,
            Description: description,
            RawContent: rawContent);
}
