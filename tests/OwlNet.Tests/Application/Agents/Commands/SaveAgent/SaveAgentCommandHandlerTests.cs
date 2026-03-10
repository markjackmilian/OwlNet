using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OwlNet.Application.Agents.Commands.SaveAgent;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using Shouldly;

namespace OwlNet.Tests.Application.Agents.Commands.SaveAgent;

/// <summary>
/// Unit tests for <see cref="SaveAgentCommandHandler"/>.
/// Covers happy path, project not found, archived project, invalid agent name,
/// duplicate agent name, and filesystem write failure scenarios.
/// </summary>
public sealed class SaveAgentCommandHandlerTests
{
    private const string DefaultProjectPath = @"C:\Projects\TestProject";
    private const string ValidAgentName = "owl-coder";
    private const string ValidContent = "# Agent Definition\n\nYou are a helpful assistant.";

    private readonly IProjectRepository _projectRepository;
    private readonly IAgentFileService _agentFileService;
    private readonly SaveAgentCommandHandler _sut;

    public SaveAgentCommandHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _agentFileService = Substitute.For<IAgentFileService>();
        _sut = new SaveAgentCommandHandler(
            _projectRepository,
            _agentFileService,
            NullLogger<SaveAgentCommandHandler>.Instance);
    }

    // ──────────────────────────────────────────────
    // Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessWithFilePath()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.GetAgentAsync(DefaultProjectPath, ValidAgentName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(null));
        _agentFileService.WriteAgentAsync(DefaultProjectPath, ValidAgentName, ValidContent, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        var expectedPath = $"{DefaultProjectPath}/.opencode/agents/{ValidAgentName}.md";
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldBe(expectedPath)
        );
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsWriteAgentWithCorrectParameters()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.GetAgentAsync(DefaultProjectPath, ValidAgentName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(null));
        _agentFileService.WriteAgentAsync(DefaultProjectPath, ValidAgentName, ValidContent, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _agentFileService.Received(1).WriteAgentAsync(
            DefaultProjectPath,
            ValidAgentName,
            ValidContent,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("my-agent")]
    [InlineData("agent1")]
    [InlineData("a1")]
    [InlineData("AB")]
    [InlineData("test-agent-name")]
    [InlineData("X1-Y2-Z3")]
    public async Task Handle_VariousValidAgentNames_ReturnsSuccess(string agentName)
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId, agentName: agentName);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.GetAgentAsync(DefaultProjectPath, agentName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(null));
        _agentFileService.WriteAgentAsync(DefaultProjectPath, agentName, ValidContent, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ──────────────────────────────────────────────
    // Project Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var command = CreateCommand(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
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
    public async Task Handle_ProjectNotFound_DoesNotCallAgentFileService()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var command = CreateCommand(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(null));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _agentFileService.DidNotReceive()
            .GetAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _agentFileService.DidNotReceive()
            .WriteAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
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
        var command = CreateCommand(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(archivedProject));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Cannot create agents in an archived project.")
        );
    }

    [Fact]
    public async Task Handle_ArchivedProject_DoesNotCallAgentFileService()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var archivedProject = CreateProjectDto(projectId, isArchived: true);
        var command = CreateCommand(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(archivedProject));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _agentFileService.DidNotReceive()
            .GetAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _agentFileService.DidNotReceive()
            .WriteAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Invalid Agent Name
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public async Task Handle_AgentNameTooShort_ReturnsLengthFailure(string invalidName)
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId, agentName: invalidName);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Agent name must be between 2 and 50 characters.")
        );
    }

    [Fact]
    public async Task Handle_AgentNameTooLong_ReturnsLengthFailure()
    {
        // Arrange — 51 characters exceeds the 50-character maximum
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var longName = new string('a', 51);
        var command = CreateCommand(projectId, agentName: longName);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Agent name must be between 2 and 50 characters.")
        );
    }

    [Theory]
    [InlineData("agent name")]
    [InlineData("agent_name")]
    [InlineData("agent.name")]
    [InlineData("-agent")]
    [InlineData("agent-")]
    [InlineData("agent@name")]
    [InlineData("agent/name")]
    [InlineData("agent\\name")]
    [InlineData("agent name with spaces")]
    [InlineData("agent!")]
    [InlineData("#agent")]
    public async Task Handle_InvalidAgentName_ReturnsFailure(string invalidName)
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId, agentName: invalidName);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe(
                "Agent name can only contain letters, numbers, and hyphens, and must start and end with a letter or number.")
        );
    }

    [Fact]
    public async Task Handle_InvalidAgentName_DoesNotCheckForDuplicateOrWrite()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId, agentName: "invalid name!");

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _agentFileService.DidNotReceive()
            .GetAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _agentFileService.DidNotReceive()
            .WriteAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Duplicate Agent Name
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_DuplicateAgentName_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId);

        var existingAgent = new AgentFileDto(
            FileName: ValidAgentName,
            FilePath: $"{DefaultProjectPath}/.opencode/agents/{ValidAgentName}.md",
            Mode: "code",
            Description: "Existing agent",
            RawContent: "# Existing");

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.GetAgentAsync(DefaultProjectPath, ValidAgentName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(existingAgent));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("An agent with this name already exists.")
        );
    }

    [Fact]
    public async Task Handle_DuplicateAgentName_DoesNotCallWriteAgent()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId);

        var existingAgent = new AgentFileDto(
            FileName: ValidAgentName,
            FilePath: $"{DefaultProjectPath}/.opencode/agents/{ValidAgentName}.md",
            Mode: "code",
            Description: "Existing agent",
            RawContent: "# Existing");

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.GetAgentAsync(DefaultProjectPath, ValidAgentName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(existingAgent));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _agentFileService.DidNotReceive()
            .WriteAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // WriteAgent IOException
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_WriteAgentThrowsIOException_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.GetAgentAsync(DefaultProjectPath, ValidAgentName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(null));
        _agentFileService.WriteAgentAsync(DefaultProjectPath, ValidAgentName, ValidContent, Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("Access denied"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe(
                "Failed to save agent file. Please check filesystem permissions and try again.")
        );
    }

    // ──────────────────────────────────────────────
    // Edge Cases — Validation Order
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ArchivedProjectWithInvalidName_ReturnsArchivedError()
    {
        // Arrange — archived project check should happen before name validation
        var projectId = Guid.NewGuid();
        var archivedProject = CreateProjectDto(projectId, isArchived: true);
        var command = CreateCommand(projectId, agentName: "invalid name!");

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(archivedProject));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — archived check takes precedence over name validation
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Cannot create agents in an archived project.")
        );
    }

    [Fact]
    public async Task Handle_ValidRequest_UsesProjectPathFromDto()
    {
        // Arrange — verify the handler uses the path from the ProjectDto, not a hardcoded value
        var projectId = Guid.NewGuid();
        var customPath = @"D:\CustomProjects\MyApp";
        var projectDto = CreateProjectDto(projectId, path: customPath);
        var agentName = "my-agent";
        var command = CreateCommand(projectId, agentName: agentName);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.GetAgentAsync(customPath, agentName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentFileDto?>(null));
        _agentFileService.WriteAgentAsync(customPath, agentName, ValidContent, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        var expectedPath = $"{customPath}/.opencode/agents/{agentName}.md";
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldBe(expectedPath)
        );

        await _agentFileService.Received(1).WriteAgentAsync(
            customPath, agentName, ValidContent, Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static SaveAgentCommand CreateCommand(
        Guid projectId,
        string agentName = ValidAgentName,
        string content = ValidContent) =>
        new()
        {
            ProjectId = projectId,
            AgentName = agentName,
            Content = content
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
}
