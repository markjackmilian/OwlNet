using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Application.Projects.Commands.ArchiveProject;
using OwlNet.Application.Projects.Commands.CreateProject;
using OwlNet.Application.Projects.Commands.RestoreProject;
using OwlNet.Application.Projects.Commands.ToggleProjectFavorite;
using OwlNet.Application.Projects.Commands.UpdateProject;
using OwlNet.Application.Projects.Queries.GetAllProjects;
using OwlNet.Application.Projects.Queries.GetProjectById;
using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Application.Projects;

/// <summary>
/// Comprehensive unit tests for all Project CQRS command and query handlers.
/// Covers <see cref="CreateProjectCommandHandler"/>, <see cref="UpdateProjectCommandHandler"/>,
/// <see cref="ArchiveProjectCommandHandler"/>, <see cref="RestoreProjectCommandHandler"/>,
/// <see cref="GetAllProjectsQueryHandler"/>, and <see cref="GetProjectByIdQueryHandler"/>.
/// Each handler is tested for its happy path, validation failures, edge cases, and error scenarios.
/// </summary>
public sealed class ProjectCommandHandlerTests
{
    private const string DefaultPath = @"C:\Projects\TestProject";

    private readonly IProjectRepository _repository;
    private readonly IBoardStatusRepository _boardStatusRepository;
    private readonly IFileSystem _fileSystem;

    public ProjectCommandHandlerTests()
    {
        _repository = Substitute.For<IProjectRepository>();
        _boardStatusRepository = Substitute.For<IBoardStatusRepository>();
        _fileSystem = Substitute.For<IFileSystem>();

        // Default: no global statuses to copy (overridden in specific tests)
        _boardStatusRepository.GetGlobalDefaultsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatusDto>()));
        _boardStatusRepository.AddRangeAsync(Arg.Any<IEnumerable<BoardStatus>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    // ──────────────────────────────────────────────
    // CreateProjectCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateProject_ValidCommand_ReturnsSuccessWithProjectId()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "New Project", Path = DefaultPath, Description = "A test project" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldNotBe(Guid.Empty)
        );
    }

    [Fact]
    public async Task CreateProject_ValidCommand_PersistsProjectToRepository()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "Persisted Project", Path = DefaultPath, Description = "Should be saved" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<Project>(p => p.Name == "Persisted Project" && p.Description == "Should be saved"),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProject_NullDescription_PersistsProjectWithEmptyDescription()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "No Description", Path = DefaultPath, Description = null };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldNotBe(Guid.Empty)
        );

        await _repository.Received(1).AddAsync(
            Arg.Is<Project>(p => p.Description == string.Empty),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // CreateProjectCommand — Duplicate Name
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateProject_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "Existing Project", Path = DefaultPath, Description = "Duplicate" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("A project with this name already exists.")
        );
    }

    [Fact]
    public async Task CreateProject_DuplicateName_DoesNotPersist()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "Existing Project", Path = DefaultPath, Description = "Duplicate" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // CreateProjectCommand — Duplicate Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateProject_DuplicatePath_ReturnsFailure()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "New Project", Path = DefaultPath, Description = "Duplicate path" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("A project with this path already exists.")
        );
    }

    [Fact]
    public async Task CreateProject_DuplicatePath_DoesNotPersist()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "New Project", Path = DefaultPath, Description = "Duplicate path" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProject_DuplicatePath_DoesNotCheckDirectoryExistence()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "New Project", Path = DefaultPath, Description = "Duplicate path" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _fileSystem.DidNotReceive().DirectoryExists(Arg.Any<string>());
    }

    // ──────────────────────────────────────────────
    // CreateProjectCommand — Directory Does Not Exist
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateProject_DirectoryDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "New Project", Path = DefaultPath, Description = "Missing directory" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("The specified directory does not exist.")
        );
    }

    [Fact]
    public async Task CreateProject_DirectoryDoesNotExist_DoesNotPersist()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "New Project", Path = DefaultPath, Description = "Missing directory" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // CreateProjectCommand — Path is persisted correctly
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateProject_ValidCommand_PersistsProjectWithTrimmedPath()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "Trimmed Path Project", Path = @"  C:\Projects\Test  ", Description = "Path with spaces" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<Project>(p => p.Path == @"C:\Projects\Test"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProject_ValidCommand_ChecksDirectoryWithTrimmedPath()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "Trimmed Check Project", Path = @"  C:\Projects\Test  ", Description = "Path with spaces" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _fileSystem.Received(1).DirectoryExists(@"C:\Projects\Test");
    }

    // ──────────────────────────────────────────────
    // CreateProjectCommand — Board Status Copying
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateProject_WithGlobalDefaults_CopiesStatusesToProject()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "New Project", Path = DefaultPath, Description = "Test" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var globalDefaults = new List<BoardStatusDto>
        {
            new(Guid.NewGuid(), "Backlog", 0, true, null),
            new(Guid.NewGuid(), "Develop", 1, true, null),
            new(Guid.NewGuid(), "Done", 2, true, null)
        };
        _boardStatusRepository.GetGlobalDefaultsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(globalDefaults));
        _boardStatusRepository.AddRangeAsync(Arg.Any<IEnumerable<BoardStatus>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        await _boardStatusRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<BoardStatus>>(statuses =>
                statuses.Count() == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProject_WithGlobalDefaults_CopiedStatusesHaveCorrectProperties()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "New Project", Path = DefaultPath, Description = "Test" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var globalDefaults = new List<BoardStatusDto>
        {
            new(Guid.NewGuid(), "Backlog", 0, true, null),
            new(Guid.NewGuid(), "Develop", 1, true, null),
            new(Guid.NewGuid(), "Done", 2, true, null)
        };
        _boardStatusRepository.GetGlobalDefaultsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(globalDefaults));

        List<BoardStatus>? capturedStatuses = null;
        _boardStatusRepository.AddRangeAsync(Arg.Any<IEnumerable<BoardStatus>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => capturedStatuses = callInfo.Arg<IEnumerable<BoardStatus>>().ToList());

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        capturedStatuses.ShouldNotBeNull();
        capturedStatuses.Count.ShouldBe(3);

        capturedStatuses.ShouldSatisfyAllConditions(
            () => capturedStatuses[0].Name.ShouldBe("Backlog"),
            () => capturedStatuses[0].SortOrder.ShouldBe(0),
            () => capturedStatuses[0].IsDefault.ShouldBeTrue(),
            () => capturedStatuses[0].ProjectId.ShouldBe(result.Value),
            () => capturedStatuses[1].Name.ShouldBe("Develop"),
            () => capturedStatuses[1].SortOrder.ShouldBe(1),
            () => capturedStatuses[1].IsDefault.ShouldBeTrue(),
            () => capturedStatuses[1].ProjectId.ShouldBe(result.Value),
            () => capturedStatuses[2].Name.ShouldBe("Done"),
            () => capturedStatuses[2].SortOrder.ShouldBe(2),
            () => capturedStatuses[2].IsDefault.ShouldBeTrue(),
            () => capturedStatuses[2].ProjectId.ShouldBe(result.Value)
        );
    }

    [Fact]
    public async Task CreateProject_NoGlobalDefaults_CallsAddRangeWithEmptyCollection()
    {
        // Arrange — _boardStatusRepository already returns empty list by default in constructor
        var command = new CreateProjectCommand { Name = "New Project", Path = DefaultPath, Description = "Test" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _boardStatusRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<BoardStatus>>(statuses => !statuses.Any()),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // UpdateProjectCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateProject_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var project = Project.Create("Original Name", DefaultPath, "Original description");
        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Updated Name",
            Description = "Updated description"
        };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));
        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new UpdateProjectCommandHandler(
            _repository,
            NullLogger<UpdateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateProject_ValidCommand_UpdatesEntityAndSaves()
    {
        // Arrange
        var project = Project.Create("Original Name", DefaultPath, "Original description");
        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Updated Name",
            Description = "Updated description"
        };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));
        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new UpdateProjectCommandHandler(
            _repository,
            NullLogger<UpdateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        project.ShouldSatisfyAllConditions(
            () => project.Name.ShouldBe("Updated Name"),
            () => project.Description.ShouldBe("Updated description")
        );
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProject_ValidCommand_ChecksNameUniquenessWithExcludedId()
    {
        // Arrange
        var project = Project.Create("Original Name", DefaultPath, "Original description");
        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Updated Name",
            Description = "Updated description"
        };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));
        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new UpdateProjectCommandHandler(
            _repository,
            NullLogger<UpdateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — verifies the handler passes the project's own ID as excludeId
        await _repository.Received(1).ExistsWithNameAsync(
            "Updated Name",
            project.Id,
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // UpdateProjectCommand — Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateProject_NonExistentProject_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateProjectCommand
        {
            Id = Guid.NewGuid(),
            Name = "Updated Name",
            Description = "Updated description"
        };

        _repository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(null));

        var sut = new UpdateProjectCommandHandler(
            _repository,
            NullLogger<UpdateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Project not found.")
        );
    }

    [Fact]
    public async Task UpdateProject_NonExistentProject_DoesNotSave()
    {
        // Arrange
        var command = new UpdateProjectCommand
        {
            Id = Guid.NewGuid(),
            Name = "Updated Name",
            Description = "Updated description"
        };

        _repository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(null));

        var sut = new UpdateProjectCommandHandler(
            _repository,
            NullLogger<UpdateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // UpdateProjectCommand — Archived Project
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateProject_ArchivedProject_ReturnsFailure()
    {
        // Arrange
        var project = Project.Create("Archived Project", DefaultPath, "Cannot update");
        project.Archive();

        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "New Name",
            Description = "New description"
        };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));

        var sut = new UpdateProjectCommandHandler(
            _repository,
            NullLogger<UpdateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Cannot update an archived project.")
        );
    }

    [Fact]
    public async Task UpdateProject_ArchivedProject_DoesNotCheckNameOrSave()
    {
        // Arrange
        var project = Project.Create("Archived Project", DefaultPath, "Cannot update");
        project.Archive();

        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "New Name",
            Description = "New description"
        };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));

        var sut = new UpdateProjectCommandHandler(
            _repository,
            NullLogger<UpdateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive()
            .ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // UpdateProjectCommand — Duplicate Name
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateProject_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var project = Project.Create("Original Name", DefaultPath, "Original description");
        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Taken Name",
            Description = "Updated description"
        };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));
        _repository.ExistsWithNameAsync("Taken Name", project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new UpdateProjectCommandHandler(
            _repository,
            NullLogger<UpdateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("A project with this name already exists.")
        );
    }

    [Fact]
    public async Task UpdateProject_DuplicateName_DoesNotUpdateEntityOrSave()
    {
        // Arrange
        var project = Project.Create("Original Name", DefaultPath, "Original description");
        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Taken Name",
            Description = "Updated description"
        };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));
        _repository.ExistsWithNameAsync("Taken Name", project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new UpdateProjectCommandHandler(
            _repository,
            NullLogger<UpdateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        project.Name.ShouldBe("Original Name");
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // ArchiveProjectCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ArchiveProject_ActiveProject_ReturnsSuccess()
    {
        // Arrange
        var project = Project.Create("Active Project", DefaultPath, "Will be archived");
        var command = new ArchiveProjectCommand { Id = project.Id };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new ArchiveProjectCommandHandler(
            _repository,
            NullLogger<ArchiveProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ArchiveProject_ActiveProject_SetsIsArchivedAndSaves()
    {
        // Arrange
        var project = Project.Create("Active Project", DefaultPath, "Will be archived");
        var command = new ArchiveProjectCommand { Id = project.Id };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new ArchiveProjectCommandHandler(
            _repository,
            NullLogger<ArchiveProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        project.IsArchived.ShouldBeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // ArchiveProjectCommand — Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ArchiveProject_NonExistentProject_ReturnsFailure()
    {
        // Arrange
        var command = new ArchiveProjectCommand { Id = Guid.NewGuid() };

        _repository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(null));

        var sut = new ArchiveProjectCommandHandler(
            _repository,
            NullLogger<ArchiveProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Project not found.")
        );
    }

    [Fact]
    public async Task ArchiveProject_NonExistentProject_DoesNotSave()
    {
        // Arrange
        var command = new ArchiveProjectCommand { Id = Guid.NewGuid() };

        _repository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(null));

        var sut = new ArchiveProjectCommandHandler(
            _repository,
            NullLogger<ArchiveProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // ArchiveProjectCommand — Already Archived
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ArchiveProject_AlreadyArchived_ReturnsFailure()
    {
        // Arrange
        var project = Project.Create("Archived Project", DefaultPath, "Already archived");
        project.Archive();

        var command = new ArchiveProjectCommand { Id = project.Id };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));

        var sut = new ArchiveProjectCommandHandler(
            _repository,
            NullLogger<ArchiveProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Project is already archived.")
        );
    }

    [Fact]
    public async Task ArchiveProject_AlreadyArchived_DoesNotSave()
    {
        // Arrange
        var project = Project.Create("Archived Project", DefaultPath, "Already archived");
        project.Archive();

        var command = new ArchiveProjectCommand { Id = project.Id };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));

        var sut = new ArchiveProjectCommandHandler(
            _repository,
            NullLogger<ArchiveProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // RestoreProjectCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RestoreProject_ArchivedProject_ReturnsSuccess()
    {
        // Arrange
        var project = Project.Create("Archived Project", DefaultPath, "Will be restored");
        project.Archive();

        var command = new RestoreProjectCommand { Id = project.Id };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new RestoreProjectCommandHandler(
            _repository,
            NullLogger<RestoreProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task RestoreProject_ArchivedProject_ClearsIsArchivedAndSaves()
    {
        // Arrange
        var project = Project.Create("Archived Project", DefaultPath, "Will be restored");
        project.Archive();

        var command = new RestoreProjectCommand { Id = project.Id };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new RestoreProjectCommandHandler(
            _repository,
            NullLogger<RestoreProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        project.IsArchived.ShouldBeFalse();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // RestoreProjectCommand — Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RestoreProject_NonExistentProject_ReturnsFailure()
    {
        // Arrange
        var command = new RestoreProjectCommand { Id = Guid.NewGuid() };

        _repository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(null));

        var sut = new RestoreProjectCommandHandler(
            _repository,
            NullLogger<RestoreProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Project not found.")
        );
    }

    [Fact]
    public async Task RestoreProject_NonExistentProject_DoesNotSave()
    {
        // Arrange
        var command = new RestoreProjectCommand { Id = Guid.NewGuid() };

        _repository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(null));

        var sut = new RestoreProjectCommandHandler(
            _repository,
            NullLogger<RestoreProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // RestoreProjectCommand — Not Archived
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RestoreProject_ActiveProject_ReturnsFailure()
    {
        // Arrange
        var project = Project.Create("Active Project", DefaultPath, "Not archived");
        var command = new RestoreProjectCommand { Id = project.Id };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));

        var sut = new RestoreProjectCommandHandler(
            _repository,
            NullLogger<RestoreProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Project is not archived.")
        );
    }

    [Fact]
    public async Task RestoreProject_ActiveProject_DoesNotSave()
    {
        // Arrange
        var project = Project.Create("Active Project", DefaultPath, "Not archived");
        var command = new RestoreProjectCommand { Id = project.Id };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));

        var sut = new RestoreProjectCommandHandler(
            _repository,
            NullLogger<RestoreProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // GetAllProjectsQuery — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllProjects_WithActiveProjects_ReturnsSuccessWithList()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var projects = new List<ProjectDto>
        {
            new(Guid.NewGuid(), "Alpha Project", @"C:\Alpha", "First project", false, false, now, now),
            new(Guid.NewGuid(), "Beta Project", @"C:\Beta", "Second project", false, false, now, now),
            new(Guid.NewGuid(), "Gamma Project", @"C:\Gamma", "Third project", false, false, now, now)
        };

        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(projects));

        var sut = new GetAllProjectsQueryHandler(
            _repository,
            NullLogger<GetAllProjectsQueryHandler>.Instance);

        var query = new GetAllProjectsQuery();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.Count.ShouldBe(3),
            () => result.Value[0].Name.ShouldBe("Alpha Project"),
            () => result.Value[1].Name.ShouldBe("Beta Project"),
            () => result.Value[2].Name.ShouldBe("Gamma Project")
        );
    }

    [Fact]
    public async Task GetAllProjects_NoActiveProjects_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ProjectDto>()));

        var sut = new GetAllProjectsQueryHandler(
            _repository,
            NullLogger<GetAllProjectsQueryHandler>.Instance);

        var query = new GetAllProjectsQuery();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldBeEmpty()
        );
    }

    // ──────────────────────────────────────────────
    // GetProjectByIdQuery — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetProjectById_ExistingProject_ReturnsSuccessWithDto()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var projectDto = new ProjectDto(projectId, "Test Project", DefaultPath, "A description", false, false, now, now);

        _repository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));

        var sut = new GetProjectByIdQueryHandler(
            _repository,
            NullLogger<GetProjectByIdQueryHandler>.Instance);

        var query = new GetProjectByIdQuery { Id = projectId };

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.Id.ShouldBe(projectId),
            () => result.Value.Name.ShouldBe("Test Project"),
            () => result.Value.Path.ShouldBe(DefaultPath),
            () => result.Value.Description.ShouldBe("A description"),
            () => result.Value.IsArchived.ShouldBeFalse(),
            () => result.Value.CreatedAt.ShouldBe(now),
            () => result.Value.UpdatedAt.ShouldBe(now)
        );
    }

    [Fact]
    public async Task GetProjectById_ArchivedProject_ReturnsSuccessWithDto()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var projectDto = new ProjectDto(projectId, "Archived Project", DefaultPath, "Archived", true, false, now, now);

        _repository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(projectDto));

        var sut = new GetProjectByIdQueryHandler(
            _repository,
            NullLogger<GetProjectByIdQueryHandler>.Instance);

        var query = new GetProjectByIdQuery { Id = projectId };

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.Id.ShouldBe(projectId),
            () => result.Value.IsArchived.ShouldBeTrue()
        );
    }

    // ──────────────────────────────────────────────
    // GetProjectByIdQuery — Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetProjectById_NonExistentProject_ReturnsFailure()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        _repository.GetByIdAsync(unknownId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectDto?>(null));

        var sut = new GetProjectByIdQueryHandler(
            _repository,
            NullLogger<GetProjectByIdQueryHandler>.Instance);

        var query = new GetProjectByIdQuery { Id = unknownId };

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Project not found.")
        );
    }

    // ──────────────────────────────────────────────
    // ToggleProjectFavoriteCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ToggleProjectFavorite_ValidId_ReturnsSuccess()
    {
        // Arrange
        var project = Project.Create("Favorite Project", DefaultPath, "Toggle test");
        var command = new ToggleProjectFavoriteCommand { Id = project.Id };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new ToggleProjectFavoriteCommandHandler(
            _repository,
            NullLogger<ToggleProjectFavoriteCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ToggleProjectFavorite_ValidId_CallsSaveChanges()
    {
        // Arrange
        var project = Project.Create("Favorite Project", DefaultPath, "Save test");
        var command = new ToggleProjectFavoriteCommand { Id = project.Id };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new ToggleProjectFavoriteCommandHandler(
            _repository,
            NullLogger<ToggleProjectFavoriteCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleProjectFavorite_ValidId_TogglesIsFavorited()
    {
        // Arrange
        var project = Project.Create("Favorite Project", DefaultPath, "Toggle state test");
        project.IsFavorited.ShouldBeFalse();

        var command = new ToggleProjectFavoriteCommand { Id = project.Id };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new ToggleProjectFavoriteCommandHandler(
            _repository,
            NullLogger<ToggleProjectFavoriteCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        project.IsFavorited.ShouldBeTrue();
    }

    // ──────────────────────────────────────────────
    // ToggleProjectFavoriteCommand — Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ToggleProjectFavorite_ProjectNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new ToggleProjectFavoriteCommand { Id = Guid.NewGuid() };

        _repository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(null));

        var sut = new ToggleProjectFavoriteCommandHandler(
            _repository,
            NullLogger<ToggleProjectFavoriteCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Project not found.")
        );
    }

    [Fact]
    public async Task ToggleProjectFavorite_ProjectNotFound_DoesNotSave()
    {
        // Arrange
        var command = new ToggleProjectFavoriteCommand { Id = Guid.NewGuid() };

        _repository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(null));

        var sut = new ToggleProjectFavoriteCommandHandler(
            _repository,
            NullLogger<ToggleProjectFavoriteCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Relaxed Uniqueness — Active-Only Scenarios
    // ──────────────────────────────────────────────

    /// <summary>
    /// Scenario 1: il repository restituisce false per il nome perché il progetto
    /// che lo usa è archiviato (ExistsWithNameAsync filtra solo attivi).
    /// Il handler vede false → la creazione deve avere successo.
    /// </summary>
    [Fact]
    public async Task CreateProject_NameUsedByArchivedProject_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "Archived Project Name",
            Path = DefaultPath,
            Description = "Same name as an archived project"
        };

        // Il repository filtra solo i progetti attivi: il nome esiste solo su un
        // progetto archiviato, quindi restituisce false.
        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldNotBe(Guid.Empty)
        );
    }

    /// <summary>
    /// Scenario 1 (side-effect): quando il nome è libero tra i progetti attivi,
    /// il progetto viene effettivamente persistito nel repository.
    /// </summary>
    [Fact]
    public async Task CreateProject_NameUsedByArchivedProject_PersistsNewProject()
    {
        // Arrange
        const string reusedName = "Archived Project Name";
        var command = new CreateProjectCommand
        {
            Name = reusedName,
            Path = DefaultPath,
            Description = "Reusing a name freed by archiving"
        };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — il progetto con il nome riutilizzato viene salvato
        await _repository.Received(1).AddAsync(
            Arg.Is<Project>(p => p.Name == reusedName),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Scenario 2: il repository restituisce false per il path perché il progetto
    /// che lo usa è archiviato (ExistsWithPathAsync filtra solo attivi).
    /// Il handler vede false → la creazione deve avere successo.
    /// </summary>
    [Fact]
    public async Task CreateProject_PathUsedByArchivedProject_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "Brand New Project",
            Path = DefaultPath,
            Description = "Same path as an archived project"
        };

        // Il repository filtra solo i progetti attivi: il path esiste solo su un
        // progetto archiviato, quindi restituisce false.
        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldNotBe(Guid.Empty)
        );
    }

    /// <summary>
    /// Scenario 2 (side-effect): quando il path è libero tra i progetti attivi,
    /// il progetto viene effettivamente persistito nel repository.
    /// </summary>
    [Fact]
    public async Task CreateProject_PathUsedByArchivedProject_PersistsNewProject()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "Brand New Project",
            Path = DefaultPath,
            Description = "Reusing a path freed by archiving"
        };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — il progetto con il path riutilizzato viene salvato
        await _repository.Received(1).AddAsync(
            Arg.Is<Project>(p => p.Path == DefaultPath),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Scenario 3: nome uguale a un progetto attivo → il repository restituisce true
    /// → il handler deve rifiutare con il messaggio esatto della nuova semantica.
    /// </summary>
    [Fact]
    public async Task CreateProject_NameUsedByActiveProject_ReturnsFailureWithExactMessage()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "Active Project Name",
            Path = DefaultPath,
            Description = "Conflicts with an active project"
        };

        // Il repository filtra solo i progetti attivi: il nome è già usato da un attivo.
        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — messaggio esatto come da spec
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("A project with this name already exists.")
        );
    }

    /// <summary>
    /// Scenario 4: path uguale a un progetto attivo → il repository restituisce true
    /// → il handler deve rifiutare con il messaggio esatto della nuova semantica.
    /// </summary>
    [Fact]
    public async Task CreateProject_PathUsedByActiveProject_ReturnsFailureWithExactMessage()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "Unique Name",
            Path = DefaultPath,
            Description = "Conflicts with an active project path"
        };

        // Nome libero, ma il path è già usato da un progetto attivo.
        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — messaggio esatto come da spec
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("A project with this path already exists.")
        );
    }

    /// <summary>
    /// Scenario 5: aggiornare il nome di un progetto a uno già usato da un altro
    /// progetto attivo → il handler deve rifiutare con il messaggio esatto.
    /// </summary>
    [Fact]
    public async Task UpdateProject_NameUsedByActiveProject_ReturnsFailureWithExactMessage()
    {
        // Arrange
        var project = Project.Create("Original Name", DefaultPath, "Will try to rename");
        var command = new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Active Taken Name",
            Description = "Updated description"
        };

        _repository.GetEntityByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Project?>(project));
        // Il repository (filtrando solo attivi, escludendo il progetto stesso) trova un conflitto.
        _repository.ExistsWithNameAsync("Active Taken Name", project.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new UpdateProjectCommandHandler(
            _repository,
            NullLogger<UpdateProjectCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — messaggio esatto come da spec
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("A project with this name already exists.")
        );
    }

    /// <summary>
    /// Scenario 6: verifica che CreateProject chiami ExistsWithNameAsync senza excludeId
    /// (null), poiché in fase di creazione non esiste ancora un ID da escludere.
    /// </summary>
    [Fact]
    public async Task CreateProject_ValidCommand_CallsExistsWithNameAsyncWithNullExcludeId()
    {
        // Arrange
        const string projectName = "New Unique Project";
        var command = new CreateProjectCommand
        {
            Name = projectName,
            Path = DefaultPath,
            Description = "Verifying repository call parameters"
        };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — il handler non deve passare alcun excludeId in fase di creazione
        await _repository.Received(1).ExistsWithNameAsync(
            projectName,
            Arg.Is<Guid?>(id => id == null),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Scenario 7: verifica che CreateProject chiami ExistsWithPathAsync senza excludeId
    /// (null), poiché in fase di creazione non esiste ancora un ID da escludere.
    /// </summary>
    [Fact]
    public async Task CreateProject_ValidCommand_CallsExistsWithPathAsyncWithNullExcludeId()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "New Unique Project",
            Path = DefaultPath,
            Description = "Verifying repository call parameters"
        };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.ExistsWithPathAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
            _boardStatusRepository,
            _fileSystem,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — il handler non deve passare alcun excludeId in fase di creazione
        await _repository.Received(1).ExistsWithPathAsync(
            DefaultPath,
            Arg.Is<Guid?>(id => id == null),
            Arg.Any<CancellationToken>());
    }
}
