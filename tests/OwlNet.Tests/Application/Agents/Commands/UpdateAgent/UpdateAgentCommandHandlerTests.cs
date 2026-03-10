using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OwlNet.Application.Agents.Commands.UpdateAgent;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using Shouldly;

namespace OwlNet.Tests.Application.Agents.Commands.UpdateAgent;

/// <summary>
/// Unit tests for <see cref="UpdateAgentCommandHandler"/>.
/// Covers happy path, project not found, archived project, empty/whitespace content,
/// and filesystem write failure scenarios.
/// </summary>
public sealed class UpdateAgentCommandHandlerTests
{
    private const string DefaultProjectPath = @"C:\Projects\TestProject";
    private const string ValidAgentName = "owl-coder";
    private const string ValidContent = "# Agent Definition\n\nYou are a helpful assistant.";

    private readonly IProjectRepository _projectRepository;
    private readonly IAgentFileService _agentFileService;
    private readonly UpdateAgentCommandHandler _sut;

    public UpdateAgentCommandHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _agentFileService = Substitute.For<IAgentFileService>();
        _sut = new UpdateAgentCommandHandler(
            _projectRepository,
            _agentFileService,
            NullLogger<UpdateAgentCommandHandler>.Instance);
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
        var command = CreateCommand(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.WriteAgentAsync(DefaultProjectPath, ValidAgentName, ValidContent, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
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

    [Fact]
    public async Task Handle_ValidRequest_UsesProjectPathFromDto()
    {
        // Arrange — verify the handler uses the path from the ProjectDto, not a hardcoded value
        var projectId = Guid.NewGuid();
        var customPath = @"D:\CustomProjects\MyApp";
        var projectDto = CreateProjectDto(projectId, path: customPath);
        var agentName = "my-agent";
        var content = "# Updated content";
        var command = CreateCommand(projectId, agentName: agentName, content: content);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.WriteAgentAsync(customPath, agentName, content, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _agentFileService.Received(1).WriteAgentAsync(
            customPath, agentName, content, Arg.Any<CancellationToken>());
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
            () => result.Error.ShouldBe("Cannot update agents in an archived project.")
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
            .WriteAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Empty / Whitespace Content
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyContent_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId, content: "");

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Agent content cannot be empty.")
        );
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData(" \t\n ")]
    public async Task Handle_WhitespaceOnlyContent_ReturnsFailure(string whitespaceContent)
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId, content: whitespaceContent);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Agent content cannot be empty.")
        );
    }

    [Fact]
    public async Task Handle_EmptyContent_DoesNotCallAgentFileService()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId, content: "");

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));

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
        _agentFileService.WriteAgentAsync(DefaultProjectPath, ValidAgentName, ValidContent, Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("Access denied"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Failed to save agent. Check filesystem permissions.")
        );
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static UpdateAgentCommand CreateCommand(
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
