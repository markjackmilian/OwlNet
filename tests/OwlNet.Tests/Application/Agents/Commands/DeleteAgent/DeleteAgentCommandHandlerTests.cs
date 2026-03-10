using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OwlNet.Application.Agents.Commands.DeleteAgent;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using Shouldly;

namespace OwlNet.Tests.Application.Agents.Commands.DeleteAgent;

/// <summary>
/// Unit tests for <see cref="DeleteAgentCommandHandler"/>.
/// Covers happy path, project not found, archived project,
/// and filesystem IOException scenarios.
/// </summary>
public sealed class DeleteAgentCommandHandlerTests
{
    private const string DefaultProjectPath = @"C:\Projects\TestProject";
    private const string ValidAgentName = "owl-coder";

    private readonly IProjectRepository _projectRepository;
    private readonly IAgentFileService _agentFileService;
    private readonly DeleteAgentCommandHandler _sut;

    public DeleteAgentCommandHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _agentFileService = Substitute.For<IAgentFileService>();
        _sut = new DeleteAgentCommandHandler(
            _projectRepository,
            _agentFileService,
            NullLogger<DeleteAgentCommandHandler>.Instance);
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
        _agentFileService.DeleteAgentAsync(DefaultProjectPath, ValidAgentName, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsDeleteAgentWithCorrectParameters()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.DeleteAgentAsync(DefaultProjectPath, ValidAgentName, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _agentFileService.Received(1).DeleteAgentAsync(
            DefaultProjectPath,
            ValidAgentName,
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
        var command = CreateCommand(projectId, agentName: agentName);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.DeleteAgentAsync(customPath, agentName, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _agentFileService.Received(1).DeleteAgentAsync(
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
            .DeleteAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
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
            () => result.Error.ShouldBe("Cannot delete agents in an archived project.")
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
            .DeleteAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // DeleteAgent IOException
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_DeleteAgentThrowsIOException_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDto = CreateProjectDto(projectId);
        var command = CreateCommand(projectId);

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));
        _agentFileService.DeleteAgentAsync(DefaultProjectPath, ValidAgentName, Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("Access denied"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Failed to delete agent. Check filesystem permissions.")
        );
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static DeleteAgentCommand CreateCommand(
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
}
