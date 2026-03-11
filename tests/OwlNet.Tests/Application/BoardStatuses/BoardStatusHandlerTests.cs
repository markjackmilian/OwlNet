using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.BoardStatuses.Commands.AddBoardStatus;
using OwlNet.Application.BoardStatuses.Commands.DeleteBoardStatus;
using OwlNet.Application.BoardStatuses.Commands.RenameBoardStatus;
using OwlNet.Application.BoardStatuses.Commands.ReorderBoardStatuses;
using OwlNet.Application.BoardStatuses.Queries.GetGlobalDefaultStatuses;
using OwlNet.Application.BoardStatuses.Queries.GetProjectStatuses;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Application.BoardStatuses;

/// <summary>
/// Comprehensive unit tests for all BoardStatus CQRS command and query handlers.
/// Covers <see cref="AddBoardStatusCommandHandler"/>, <see cref="RenameBoardStatusCommandHandler"/>,
/// <see cref="DeleteBoardStatusCommandHandler"/>, <see cref="ReorderBoardStatusesCommandHandler"/>,
/// <see cref="GetGlobalDefaultStatusesQueryHandler"/>, and <see cref="GetProjectStatusesQueryHandler"/>.
/// Each handler is tested for its happy path, validation failures, edge cases, and error scenarios.
/// </summary>
public sealed class BoardStatusHandlerTests
{
    private readonly IBoardStatusRepository _repository;

    public BoardStatusHandlerTests()
    {
        _repository = Substitute.For<IBoardStatusRepository>();
    }

    // ──────────────────────────────────────────────
    // AddBoardStatusCommand — Global Default (Happy Path)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task AddBoardStatus_ValidGlobalDefault_ReturnsSuccessWithId()
    {
        // Arrange
        var command = new AddBoardStatusCommand { Name = "New Status", ProjectId = null };

        _repository.ExistsWithNameInScopeAsync(
                Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.GetGlobalDefaultsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatusDto>()));
        _repository.AddAsync(Arg.Any<BoardStatus>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new AddBoardStatusCommandHandler(
            _repository,
            NullLogger<AddBoardStatusCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldNotBe(Guid.Empty)
        );
    }

    [Fact]
    public async Task AddBoardStatus_ValidGlobalDefault_PersistsToRepository()
    {
        // Arrange
        var command = new AddBoardStatusCommand { Name = "Backlog", ProjectId = null };

        _repository.ExistsWithNameInScopeAsync(
                Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.GetGlobalDefaultsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatusDto>()));
        _repository.AddAsync(Arg.Any<BoardStatus>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new AddBoardStatusCommandHandler(
            _repository,
            NullLogger<AddBoardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<BoardStatus>(s => s.Name == "Backlog" && s.ProjectId == null),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // AddBoardStatusCommand — Project-Level (Happy Path)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task AddBoardStatus_ValidProjectLevel_ReturnsSuccessWithId()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var command = new AddBoardStatusCommand { Name = "In Progress", ProjectId = projectId };

        _repository.ExistsWithNameInScopeAsync(
                Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatusDto>()));
        _repository.AddAsync(Arg.Any<BoardStatus>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new AddBoardStatusCommandHandler(
            _repository,
            NullLogger<AddBoardStatusCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldNotBe(Guid.Empty)
        );
    }

    [Fact]
    public async Task AddBoardStatus_ValidProjectLevel_PersistsWithCorrectProjectId()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var command = new AddBoardStatusCommand { Name = "In Progress", ProjectId = projectId };

        _repository.ExistsWithNameInScopeAsync(
                Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatusDto>()));
        _repository.AddAsync(Arg.Any<BoardStatus>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new AddBoardStatusCommandHandler(
            _repository,
            NullLogger<AddBoardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<BoardStatus>(s => s.Name == "In Progress" && s.ProjectId == projectId),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // AddBoardStatusCommand — Duplicate Name
    // ──────────────────────────────────────────────

    [Fact]
    public async Task AddBoardStatus_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var command = new AddBoardStatusCommand { Name = "Existing Status", ProjectId = null };

        _repository.ExistsWithNameInScopeAsync(
                Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new AddBoardStatusCommandHandler(
            _repository,
            NullLogger<AddBoardStatusCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("A status with this name already exists.")
        );
    }

    [Fact]
    public async Task AddBoardStatus_DuplicateName_DoesNotPersist()
    {
        // Arrange
        var command = new AddBoardStatusCommand { Name = "Existing Status", ProjectId = null };

        _repository.ExistsWithNameInScopeAsync(
                Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new AddBoardStatusCommandHandler(
            _repository,
            NullLogger<AddBoardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().AddAsync(Arg.Any<BoardStatus>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // AddBoardStatusCommand — Sort Order Calculation
    // ──────────────────────────────────────────────

    [Fact]
    public async Task AddBoardStatus_WithExistingStatuses_SetsCorrectSortOrder()
    {
        // Arrange — existing statuses have max SortOrder of 2, so new should be 3
        var command = new AddBoardStatusCommand { Name = "New Column", ProjectId = null };

        _repository.ExistsWithNameInScopeAsync(
                Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.GetGlobalDefaultsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatusDto>
            {
                new(Guid.NewGuid(), "Backlog", 0, true, null),
                new(Guid.NewGuid(), "In Progress", 1, true, null),
                new(Guid.NewGuid(), "Done", 2, true, null)
            }));
        _repository.AddAsync(Arg.Any<BoardStatus>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new AddBoardStatusCommandHandler(
            _repository,
            NullLogger<AddBoardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<BoardStatus>(s => s.SortOrder == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddBoardStatus_EmptyScope_SetsSortOrderToZero()
    {
        // Arrange — no existing statuses in scope
        var command = new AddBoardStatusCommand { Name = "First Status", ProjectId = null };

        _repository.ExistsWithNameInScopeAsync(
                Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.GetGlobalDefaultsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatusDto>()));
        _repository.AddAsync(Arg.Any<BoardStatus>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new AddBoardStatusCommandHandler(
            _repository,
            NullLogger<AddBoardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<BoardStatus>(s => s.SortOrder == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddBoardStatus_ProjectWithExistingStatuses_QueriesProjectScope()
    {
        // Arrange — project-level scope should query GetByProjectIdAsync, not GetGlobalDefaultsAsync
        var projectId = Guid.NewGuid();
        var command = new AddBoardStatusCommand { Name = "Review", ProjectId = projectId };

        _repository.ExistsWithNameInScopeAsync(
                Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatusDto>
            {
                new(Guid.NewGuid(), "Backlog", 0, false, projectId),
                new(Guid.NewGuid(), "In Progress", 1, false, projectId)
            }));
        _repository.AddAsync(Arg.Any<BoardStatus>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new AddBoardStatusCommandHandler(
            _repository,
            NullLogger<AddBoardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — sort order should be max(1) + 1 = 2
        await _repository.Received(1).AddAsync(
            Arg.Is<BoardStatus>(s => s.SortOrder == 2),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // RenameBoardStatusCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RenameBoardStatus_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var status = BoardStatus.CreateGlobalDefault("Old Name", 0);
        var command = new RenameBoardStatusCommand { Id = status.Id, NewName = "New Name" };

        _repository.GetEntityByIdAsync(status.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(status));
        _repository.ExistsWithNameInScopeAsync(
                Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new RenameBoardStatusCommandHandler(
            _repository,
            NullLogger<RenameBoardStatusCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task RenameBoardStatus_ValidRequest_UpdatesEntityNameAndSaves()
    {
        // Arrange
        var status = BoardStatus.CreateGlobalDefault("Old Name", 0);
        var command = new RenameBoardStatusCommand { Id = status.Id, NewName = "Renamed Status" };

        _repository.GetEntityByIdAsync(status.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(status));
        _repository.ExistsWithNameInScopeAsync(
                Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new RenameBoardStatusCommandHandler(
            _repository,
            NullLogger<RenameBoardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        status.Name.ShouldBe("Renamed Status");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenameBoardStatus_ValidRequest_ChecksUniquenessWithExcludedId()
    {
        // Arrange
        var status = BoardStatus.CreateGlobalDefault("Old Name", 0);
        var command = new RenameBoardStatusCommand { Id = status.Id, NewName = "New Name" };

        _repository.GetEntityByIdAsync(status.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(status));
        _repository.ExistsWithNameInScopeAsync(
                Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new RenameBoardStatusCommandHandler(
            _repository,
            NullLogger<RenameBoardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — verifies the handler passes the status's own ID as excludeId
        await _repository.Received(1).ExistsWithNameInScopeAsync(
            "New Name",
            status.ProjectId,
            status.Id,
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // RenameBoardStatusCommand — Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RenameBoardStatus_NotFound_ReturnsFailure()
    {
        // Arrange
        var command = new RenameBoardStatusCommand { Id = Guid.NewGuid(), NewName = "New Name" };

        _repository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(null));

        var sut = new RenameBoardStatusCommandHandler(
            _repository,
            NullLogger<RenameBoardStatusCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Board status not found.")
        );
    }

    [Fact]
    public async Task RenameBoardStatus_NotFound_DoesNotSave()
    {
        // Arrange
        var command = new RenameBoardStatusCommand { Id = Guid.NewGuid(), NewName = "New Name" };

        _repository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(null));

        var sut = new RenameBoardStatusCommandHandler(
            _repository,
            NullLogger<RenameBoardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // RenameBoardStatusCommand — Duplicate Name
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RenameBoardStatus_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var status = BoardStatus.CreateGlobalDefault("Original", 0);
        var command = new RenameBoardStatusCommand { Id = status.Id, NewName = "Taken Name" };

        _repository.GetEntityByIdAsync(status.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(status));
        _repository.ExistsWithNameInScopeAsync(
                "Taken Name", status.ProjectId, status.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new RenameBoardStatusCommandHandler(
            _repository,
            NullLogger<RenameBoardStatusCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("A status with this name already exists.")
        );
    }

    [Fact]
    public async Task RenameBoardStatus_DuplicateName_DoesNotUpdateEntityOrSave()
    {
        // Arrange
        var status = BoardStatus.CreateGlobalDefault("Original", 0);
        var command = new RenameBoardStatusCommand { Id = status.Id, NewName = "Taken Name" };

        _repository.GetEntityByIdAsync(status.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(status));
        _repository.ExistsWithNameInScopeAsync(
                "Taken Name", status.ProjectId, status.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var sut = new RenameBoardStatusCommandHandler(
            _repository,
            NullLogger<RenameBoardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        status.Name.ShouldBe("Original");
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // DeleteBoardStatusCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteBoardStatus_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var status = BoardStatus.CreateGlobalDefault("To Delete", 0);
        var command = new DeleteBoardStatusCommand { Id = status.Id };

        _repository.GetEntityByIdAsync(status.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(status));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new DeleteBoardStatusCommandHandler(
            _repository,
            NullLogger<DeleteBoardStatusCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteBoardStatus_ValidRequest_RemovesEntityAndSaves()
    {
        // Arrange
        var status = BoardStatus.CreateGlobalDefault("To Delete", 0);
        var command = new DeleteBoardStatusCommand { Id = status.Id };

        _repository.GetEntityByIdAsync(status.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(status));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new DeleteBoardStatusCommandHandler(
            _repository,
            NullLogger<DeleteBoardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _repository.Received(1).Remove(status);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // DeleteBoardStatusCommand — Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteBoardStatus_NotFound_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteBoardStatusCommand { Id = Guid.NewGuid() };

        _repository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(null));

        var sut = new DeleteBoardStatusCommandHandler(
            _repository,
            NullLogger<DeleteBoardStatusCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Board status not found.")
        );
    }

    [Fact]
    public async Task DeleteBoardStatus_NotFound_DoesNotRemoveOrSave()
    {
        // Arrange
        var command = new DeleteBoardStatusCommand { Id = Guid.NewGuid() };

        _repository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(null));

        var sut = new DeleteBoardStatusCommandHandler(
            _repository,
            NullLogger<DeleteBoardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _repository.DidNotReceive().Remove(Arg.Any<BoardStatus>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // ReorderBoardStatusesCommand — Global Reorder (Happy Path)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ReorderBoardStatuses_ValidGlobalReorder_ReturnsSuccess()
    {
        // Arrange — three global statuses, reorder them in reverse
        var status1 = BoardStatus.CreateGlobalDefault("Backlog", 0);
        var status2 = BoardStatus.CreateGlobalDefault("In Progress", 1);
        var status3 = BoardStatus.CreateGlobalDefault("Done", 2);

        var command = new ReorderBoardStatusesCommand
        {
            ProjectId = null,
            OrderedStatusIds = [status3.Id, status2.Id, status1.Id]
        };

        _repository.GetGlobalDefaultEntitiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatus> { status1, status2, status3 }));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new ReorderBoardStatusesCommandHandler(
            _repository,
            NullLogger<ReorderBoardStatusesCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ReorderBoardStatuses_ValidGlobalReorder_UpdatesSortOrdersCorrectly()
    {
        // Arrange — three global statuses, reorder them in reverse
        var status1 = BoardStatus.CreateGlobalDefault("Backlog", 0);
        var status2 = BoardStatus.CreateGlobalDefault("In Progress", 1);
        var status3 = BoardStatus.CreateGlobalDefault("Done", 2);

        var command = new ReorderBoardStatusesCommand
        {
            ProjectId = null,
            OrderedStatusIds = [status3.Id, status2.Id, status1.Id]
        };

        _repository.GetGlobalDefaultEntitiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatus> { status1, status2, status3 }));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new ReorderBoardStatusesCommandHandler(
            _repository,
            NullLogger<ReorderBoardStatusesCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — status3 is first (index 0), status2 stays (index 1), status1 is last (index 2)
        status3.SortOrder.ShouldBe(0);
        status2.SortOrder.ShouldBe(1);
        status1.SortOrder.ShouldBe(2);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // ReorderBoardStatusesCommand — Project Reorder (Happy Path)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ReorderBoardStatuses_ValidProjectReorder_ReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var status1 = BoardStatus.CreateForProject("Todo", 0, projectId);
        var status2 = BoardStatus.CreateForProject("Doing", 1, projectId);

        var command = new ReorderBoardStatusesCommand
        {
            ProjectId = projectId,
            OrderedStatusIds = [status2.Id, status1.Id]
        };

        _repository.GetEntitiesByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatus> { status1, status2 }));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new ReorderBoardStatusesCommandHandler(
            _repository,
            NullLogger<ReorderBoardStatusesCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ReorderBoardStatuses_ValidProjectReorder_UpdatesSortOrdersCorrectly()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var status1 = BoardStatus.CreateForProject("Todo", 0, projectId);
        var status2 = BoardStatus.CreateForProject("Doing", 1, projectId);

        var command = new ReorderBoardStatusesCommand
        {
            ProjectId = projectId,
            OrderedStatusIds = [status2.Id, status1.Id]
        };

        _repository.GetEntitiesByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatus> { status1, status2 }));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new ReorderBoardStatusesCommandHandler(
            _repository,
            NullLogger<ReorderBoardStatusesCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — status2 is now first (index 0), status1 is second (index 1)
        status2.SortOrder.ShouldBe(0);
        status1.SortOrder.ShouldBe(1);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderBoardStatuses_ProjectScope_QueriesProjectEntities()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var status1 = BoardStatus.CreateForProject("Todo", 0, projectId);

        var command = new ReorderBoardStatusesCommand
        {
            ProjectId = projectId,
            OrderedStatusIds = [status1.Id]
        };

        _repository.GetEntitiesByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatus> { status1 }));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new ReorderBoardStatusesCommandHandler(
            _repository,
            NullLogger<ReorderBoardStatusesCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — should query project entities, not global
        await _repository.Received(1).GetEntitiesByProjectIdAsync(projectId, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().GetGlobalDefaultEntitiesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // ReorderBoardStatusesCommand — Count Mismatch
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ReorderBoardStatuses_CountMismatch_ReturnsFailure()
    {
        // Arrange — scope has 3 statuses but command only provides 2 IDs
        var status1 = BoardStatus.CreateGlobalDefault("Backlog", 0);
        var status2 = BoardStatus.CreateGlobalDefault("In Progress", 1);
        var status3 = BoardStatus.CreateGlobalDefault("Done", 2);

        var command = new ReorderBoardStatusesCommand
        {
            ProjectId = null,
            OrderedStatusIds = [status1.Id, status2.Id]
        };

        _repository.GetGlobalDefaultEntitiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatus> { status1, status2, status3 }));

        var sut = new ReorderBoardStatusesCommandHandler(
            _repository,
            NullLogger<ReorderBoardStatusesCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("The number of status IDs does not match the number of statuses in this scope.")
        );
    }

    [Fact]
    public async Task ReorderBoardStatuses_CountMismatch_DoesNotSave()
    {
        // Arrange
        var status1 = BoardStatus.CreateGlobalDefault("Backlog", 0);
        var status2 = BoardStatus.CreateGlobalDefault("In Progress", 1);
        var status3 = BoardStatus.CreateGlobalDefault("Done", 2);

        var command = new ReorderBoardStatusesCommand
        {
            ProjectId = null,
            OrderedStatusIds = [status1.Id, status2.Id]
        };

        _repository.GetGlobalDefaultEntitiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatus> { status1, status2, status3 }));

        var sut = new ReorderBoardStatusesCommandHandler(
            _repository,
            NullLogger<ReorderBoardStatusesCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // ReorderBoardStatusesCommand — Invalid ID
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ReorderBoardStatuses_InvalidId_ReturnsFailure()
    {
        // Arrange — command includes an ID that doesn't belong to the scope
        var status1 = BoardStatus.CreateGlobalDefault("Backlog", 0);
        var status2 = BoardStatus.CreateGlobalDefault("In Progress", 1);
        var rogueId = Guid.NewGuid();

        var command = new ReorderBoardStatusesCommand
        {
            ProjectId = null,
            OrderedStatusIds = [status1.Id, rogueId]
        };

        _repository.GetGlobalDefaultEntitiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatus> { status1, status2 }));

        var sut = new ReorderBoardStatusesCommandHandler(
            _repository,
            NullLogger<ReorderBoardStatusesCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("One or more status IDs do not belong to this scope.")
        );
    }

    [Fact]
    public async Task ReorderBoardStatuses_InvalidId_DoesNotSave()
    {
        // Arrange
        var status1 = BoardStatus.CreateGlobalDefault("Backlog", 0);
        var status2 = BoardStatus.CreateGlobalDefault("In Progress", 1);
        var rogueId = Guid.NewGuid();

        var command = new ReorderBoardStatusesCommand
        {
            ProjectId = null,
            OrderedStatusIds = [status1.Id, rogueId]
        };

        _repository.GetGlobalDefaultEntitiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatus> { status1, status2 }));

        var sut = new ReorderBoardStatusesCommandHandler(
            _repository,
            NullLogger<ReorderBoardStatusesCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // GetGlobalDefaultStatusesQuery — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetGlobalDefaultStatuses_ReturnsListFromRepository()
    {
        // Arrange
        var expectedStatuses = new List<BoardStatusDto>
        {
            new(Guid.NewGuid(), "Backlog", 0, true, null),
            new(Guid.NewGuid(), "In Progress", 1, true, null),
            new(Guid.NewGuid(), "Done", 2, true, null)
        };

        _repository.GetGlobalDefaultsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedStatuses));

        var sut = new GetGlobalDefaultStatusesQueryHandler(
            _repository,
            NullLogger<GetGlobalDefaultStatusesQueryHandler>.Instance);

        var query = new GetGlobalDefaultStatusesQuery();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(3),
            () => result[0].Name.ShouldBe("Backlog"),
            () => result[1].Name.ShouldBe("In Progress"),
            () => result[2].Name.ShouldBe("Done")
        );
    }

    [Fact]
    public async Task GetGlobalDefaultStatuses_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        _repository.GetGlobalDefaultsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatusDto>()));

        var sut = new GetGlobalDefaultStatusesQueryHandler(
            _repository,
            NullLogger<GetGlobalDefaultStatusesQueryHandler>.Instance);

        var query = new GetGlobalDefaultStatusesQuery();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────────
    // GetProjectStatusesQuery — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetProjectStatuses_ReturnsListFromRepository()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var expectedStatuses = new List<BoardStatusDto>
        {
            new(Guid.NewGuid(), "Todo", 0, false, projectId),
            new(Guid.NewGuid(), "In Review", 1, false, projectId)
        };

        _repository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedStatuses));

        var sut = new GetProjectStatusesQueryHandler(
            _repository,
            NullLogger<GetProjectStatusesQueryHandler>.Instance);

        var query = new GetProjectStatusesQuery { ProjectId = projectId };

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(2),
            () => result[0].Name.ShouldBe("Todo"),
            () => result[0].ProjectId.ShouldBe(projectId),
            () => result[1].Name.ShouldBe("In Review"),
            () => result[1].ProjectId.ShouldBe(projectId)
        );
    }

    [Fact]
    public async Task GetProjectStatuses_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _repository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatusDto>()));

        var sut = new GetProjectStatusesQueryHandler(
            _repository,
            NullLogger<GetProjectStatusesQueryHandler>.Instance);

        var query = new GetProjectStatusesQuery { ProjectId = projectId };

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetProjectStatuses_CallsRepositoryWithCorrectProjectId()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _repository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatusDto>()));

        var sut = new GetProjectStatusesQueryHandler(
            _repository,
            NullLogger<GetProjectStatusesQueryHandler>.Instance);

        var query = new GetProjectStatusesQuery { ProjectId = projectId };

        // Act
        await sut.Handle(query, CancellationToken.None);

        // Assert
        await _repository.Received(1).GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>());
    }
}
