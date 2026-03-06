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
    private readonly IProjectRepository _repository;

    public ProjectCommandHandlerTests()
    {
        _repository = Substitute.For<IProjectRepository>();
    }

    // ──────────────────────────────────────────────
    // CreateProjectCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateProject_ValidCommand_ReturnsSuccessWithProjectId()
    {
        // Arrange
        var command = new CreateProjectCommand { Name = "New Project", Description = "A test project" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
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
        var command = new CreateProjectCommand { Name = "Persisted Project", Description = "Should be saved" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
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
        var command = new CreateProjectCommand { Name = "No Description", Description = null };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateProjectCommandHandler(
            _repository,
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
        var command = new CreateProjectCommand { Name = "Existing Project", Description = "Duplicate" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new CreateProjectCommandHandler(
            _repository,
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
        var command = new CreateProjectCommand { Name = "Existing Project", Description = "Duplicate" };

        _repository.ExistsWithNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new CreateProjectCommandHandler(
            _repository,
            NullLogger<CreateProjectCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // UpdateProjectCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateProject_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var project = Project.Create("Original Name", "Original description");
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
        var project = Project.Create("Original Name", "Original description");
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
        var project = Project.Create("Original Name", "Original description");
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
        var project = Project.Create("Archived Project", "Cannot update");
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
        var project = Project.Create("Archived Project", "Cannot update");
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
        var project = Project.Create("Original Name", "Original description");
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
        var project = Project.Create("Original Name", "Original description");
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
        var project = Project.Create("Active Project", "Will be archived");
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
        var project = Project.Create("Active Project", "Will be archived");
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
        var project = Project.Create("Archived Project", "Already archived");
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
        var project = Project.Create("Archived Project", "Already archived");
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
        var project = Project.Create("Archived Project", "Will be restored");
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
        var project = Project.Create("Archived Project", "Will be restored");
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
        var project = Project.Create("Active Project", "Not archived");
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
        var project = Project.Create("Active Project", "Not archived");
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
            new(Guid.NewGuid(), "Alpha Project", "First project", false, false, now, now),
            new(Guid.NewGuid(), "Beta Project", "Second project", false, false, now, now),
            new(Guid.NewGuid(), "Gamma Project", "Third project", false, false, now, now)
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
        var projectDto = new ProjectDto(projectId, "Test Project", "A description", false, false, now, now);

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
        var projectDto = new ProjectDto(projectId, "Archived Project", "Archived", true, false, now, now);

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
        var project = Project.Create("Favorite Project", "Toggle test");
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
        var project = Project.Create("Favorite Project", "Save test");
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
        var project = Project.Create("Favorite Project", "Toggle state test");
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
}
