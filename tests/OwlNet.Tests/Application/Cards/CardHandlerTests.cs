using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Cards.Commands.ChangeCardStatus;
using OwlNet.Application.Cards.Commands.CreateCard;
using OwlNet.Application.Cards.Commands.DeleteCard;
using OwlNet.Application.Cards.Commands.UpdateCard;
using OwlNet.Application.Cards.Queries.GetCardsByProject;
using OwlNet.Application.Cards.Queries.GetCardStatusHistory;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;
using OwlNet.Domain.Enums;
using Shouldly;

namespace OwlNet.Tests.Application.Cards;

/// <summary>
/// Comprehensive unit tests for all Card CQRS command and query handlers.
/// Covers <see cref="CreateCardCommandHandler"/>, <see cref="UpdateCardCommandHandler"/>,
/// <see cref="DeleteCardCommandHandler"/>, <see cref="ChangeCardStatusCommandHandler"/>,
/// <see cref="GetCardsByProjectQueryHandler"/>, and <see cref="GetCardStatusHistoryQueryHandler"/>.
/// Each handler is tested for its happy path, validation failures, edge cases, and error scenarios.
/// </summary>
public sealed class CardHandlerTests
{
    private readonly ICardRepository _cardRepository;
    private readonly IBoardStatusRepository _boardStatusRepository;

    public CardHandlerTests()
    {
        _cardRepository = Substitute.For<ICardRepository>();
        _boardStatusRepository = Substitute.For<IBoardStatusRepository>();
    }

    // ──────────────────────────────────────────────
    // CreateCardCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProjectWithStatuses_CreatesCardAndReturnsId()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statusId = Guid.NewGuid();

        var statuses = new List<BoardStatusDto>
        {
            new(statusId, "Backlog", 0, true, projectId)
        };

        _boardStatusRepository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _cardRepository.GetNextNumberAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<int>(1));
        _cardRepository.AddAsync(Arg.Any<Card>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        _cardRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new CreateCardCommand
        {
            Title = "My First Card",
            Description = "A description",
            Priority = CardPriority.Medium,
            ProjectId = projectId,
            CreatedBy = "user-123"
        };

        var sut = new CreateCardCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<CreateCardCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldNotBe(Guid.Empty)
        );
    }

    [Fact]
    public async Task Handle_ProjectWithStatuses_PersistsCardToRepository()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statusId = Guid.NewGuid();

        var statuses = new List<BoardStatusDto>
        {
            new(statusId, "Backlog", 0, true, projectId)
        };

        _boardStatusRepository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _cardRepository.GetNextNumberAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<int>(1));
        _cardRepository.AddAsync(Arg.Any<Card>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        _cardRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new CreateCardCommand
        {
            Title = "Persisted Card",
            Priority = CardPriority.High,
            ProjectId = projectId,
            CreatedBy = "user-123"
        };

        var sut = new CreateCardCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<CreateCardCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _cardRepository.Received(1).AddAsync(
            Arg.Is<Card>(c => c.Title == "Persisted Card" && c.ProjectId == projectId),
            Arg.Any<CancellationToken>());
        await _cardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProjectWithNoStatuses_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _boardStatusRepository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatusDto>()));

        var command = new CreateCardCommand
        {
            Title = "Orphan Card",
            Priority = CardPriority.Low,
            ProjectId = projectId,
            CreatedBy = "user-123"
        };

        var sut = new CreateCardCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<CreateCardCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("no statuses configured")
        );
    }

    [Fact]
    public async Task Handle_ProjectWithNoStatuses_DoesNotPersistCard()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _boardStatusRepository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BoardStatusDto>()));

        var command = new CreateCardCommand
        {
            Title = "Orphan Card",
            Priority = CardPriority.Low,
            ProjectId = projectId,
            CreatedBy = "user-123"
        };

        var sut = new CreateCardCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<CreateCardCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _cardRepository.DidNotReceive().AddAsync(Arg.Any<Card>(), Arg.Any<CancellationToken>());
        await _cardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProjectWithMultipleStatuses_UsesLowestSortOrder()
    {
        // Arrange — three statuses with different SortOrders; the handler must pick SortOrder=0
        var projectId = Guid.NewGuid();
        var firstStatusId = Guid.NewGuid();  // SortOrder 0 — should be chosen
        var secondStatusId = Guid.NewGuid(); // SortOrder 1
        var thirdStatusId = Guid.NewGuid();  // SortOrder 2

        var statuses = new List<BoardStatusDto>
        {
            new(secondStatusId, "In Progress", 1, false, projectId),
            new(thirdStatusId,  "Done",        2, false, projectId),
            new(firstStatusId,  "Backlog",     0, true,  projectId)
        };

        _boardStatusRepository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _cardRepository.GetNextNumberAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<int>(1));
        _cardRepository.AddAsync(Arg.Any<Card>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        _cardRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new CreateCardCommand
        {
            Title = "Status Order Card",
            Priority = CardPriority.Medium,
            ProjectId = projectId,
            CreatedBy = "user-123"
        };

        var sut = new CreateCardCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<CreateCardCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — card must be assigned to the status with SortOrder=0 (firstStatusId)
        await _cardRepository.Received(1).AddAsync(
            Arg.Is<Card>(c => c.StatusId == firstStatusId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateCardNumber_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statusId = Guid.NewGuid();

        var statuses = new List<BoardStatusDto>
        {
            new(statusId, "Backlog", 0, true, projectId)
        };

        _boardStatusRepository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _cardRepository.GetNextNumberAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<int>(1));
        _cardRepository.AddAsync(Arg.Any<Card>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        _cardRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask(Task.FromException(
                new Exception("UNIQUE constraint failed: Cards.ProjectId, Cards.Number"))));

        var command = new CreateCardCommand
        {
            Title = "Duplicate Card",
            Priority = CardPriority.Medium,
            ProjectId = projectId,
            CreatedBy = "user-123"
        };

        var sut = new CreateCardCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<CreateCardCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("conflict", Case.Insensitive)
        );
    }

    // ──────────────────────────────────────────────
    // UpdateCardCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingCard_UpdatesAndReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var card = Card.Create("Original Title", null, CardPriority.Low, statusId, projectId, 1, "user-123");

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new UpdateCardCommand
        {
            Id = card.Id,
            Title = "Updated Title",
            Description = "New description",
            Priority = CardPriority.High
        };

        var sut = new UpdateCardCommandHandler(
            _cardRepository,
            NullLogger<UpdateCardCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ExistingCard_MutatesEntityAndSaves()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var card = Card.Create("Original Title", null, CardPriority.Low, statusId, projectId, 1, "user-123");

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new UpdateCardCommand
        {
            Id = card.Id,
            Title = "Updated Title",
            Description = "New description",
            Priority = CardPriority.High
        };

        var sut = new UpdateCardCommandHandler(
            _cardRepository,
            NullLogger<UpdateCardCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — entity was mutated in-place and SaveChanges was called
        card.ShouldSatisfyAllConditions(
            () => card.Title.ShouldBe("Updated Title"),
            () => card.Description.ShouldBe("New description"),
            () => card.Priority.ShouldBe(CardPriority.High)
        );
        await _cardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentCard_ReturnsFailure()
    {
        // Arrange
        _cardRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        var command = new UpdateCardCommand
        {
            Id = Guid.NewGuid(),
            Title = "Ghost Card",
            Priority = CardPriority.Medium
        };

        var sut = new UpdateCardCommandHandler(
            _cardRepository,
            NullLogger<UpdateCardCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("not found")
        );
    }

    [Fact]
    public async Task Handle_NonExistentCard_DoesNotSave()
    {
        // Arrange
        _cardRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        var command = new UpdateCardCommand
        {
            Id = Guid.NewGuid(),
            Title = "Ghost Card",
            Priority = CardPriority.Medium
        };

        var sut = new UpdateCardCommandHandler(
            _cardRepository,
            NullLogger<UpdateCardCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _cardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // DeleteCardCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingCard_RemovesAndReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var card = Card.Create("Card To Delete", null, CardPriority.Medium, statusId, projectId, 1, "user-123");

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new DeleteCardCommand { Id = card.Id };

        var sut = new DeleteCardCommandHandler(
            _cardRepository,
            NullLogger<DeleteCardCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ExistingCard_CallsRemoveAndSaves()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var card = Card.Create("Card To Delete", null, CardPriority.Medium, statusId, projectId, 1, "user-123");

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new DeleteCardCommand { Id = card.Id };

        var sut = new DeleteCardCommandHandler(
            _cardRepository,
            NullLogger<DeleteCardCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _cardRepository.Received(1).Remove(card);
        await _cardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentCard_ReturnsFailureOnDelete()
    {
        // Arrange
        _cardRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        var command = new DeleteCardCommand { Id = Guid.NewGuid() };

        var sut = new DeleteCardCommandHandler(
            _cardRepository,
            NullLogger<DeleteCardCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("not found")
        );
    }

    [Fact]
    public async Task Handle_NonExistentCard_DoesNotRemoveOrSave()
    {
        // Arrange
        _cardRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        var command = new DeleteCardCommand { Id = Guid.NewGuid() };

        var sut = new DeleteCardCommandHandler(
            _cardRepository,
            NullLogger<DeleteCardCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _cardRepository.DidNotReceive().Remove(Arg.Any<Card>());
        await _cardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // ChangeCardStatusCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidStatusChange_ReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var originalStatusId = Guid.NewGuid();
        var newStatusId = Guid.NewGuid();

        var card = Card.Create("My Card", null, CardPriority.Medium, originalStatusId, projectId, 1, "user-123");
        var newStatus = BoardStatus.CreateForProject("In Progress", 1, projectId);
        // Reflect the newStatusId we'll pass in the command by using the real entity's Id
        // We use the real BoardStatus entity so the domain guard sees matching ProjectIds.

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _boardStatusRepository.GetEntityByIdAsync(newStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(newStatus));
        _cardRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new ChangeCardStatusCommand
        {
            CardId = card.Id,
            NewStatusId = newStatus.Id,
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        var sut = new ChangeCardStatusCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<ChangeCardStatusCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidStatusChange_UpdatesCardStatusAndSaves()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var originalStatusId = Guid.NewGuid();

        var card = Card.Create("My Card", null, CardPriority.Medium, originalStatusId, projectId, 1, "user-123");
        var newStatus = BoardStatus.CreateForProject("In Progress", 1, projectId);

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _boardStatusRepository.GetEntityByIdAsync(newStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(newStatus));
        _cardRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new ChangeCardStatusCommand
        {
            CardId = card.Id,
            NewStatusId = newStatus.Id,
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        var sut = new ChangeCardStatusCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<ChangeCardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — card's StatusId was updated and SaveChanges was called
        card.StatusId.ShouldBe(newStatus.Id);
        await _cardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CardNotFound_ReturnsFailure()
    {
        // Arrange
        _cardRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        var command = new ChangeCardStatusCommand
        {
            CardId = Guid.NewGuid(),
            NewStatusId = Guid.NewGuid(),
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        var sut = new ChangeCardStatusCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<ChangeCardStatusCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("not found")
        );
    }

    [Fact]
    public async Task Handle_CardNotFound_DoesNotQueryStatusRepository()
    {
        // Arrange
        _cardRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        var command = new ChangeCardStatusCommand
        {
            CardId = Guid.NewGuid(),
            NewStatusId = Guid.NewGuid(),
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        var sut = new ChangeCardStatusCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<ChangeCardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — short-circuit: status repo must not be queried
        await _boardStatusRepository.DidNotReceive()
            .GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _cardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StatusNotFound_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var card = Card.Create("My Card", null, CardPriority.Medium, statusId, projectId, 1, "user-123");

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _boardStatusRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(null));

        var command = new ChangeCardStatusCommand
        {
            CardId = card.Id,
            NewStatusId = Guid.NewGuid(),
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        var sut = new ChangeCardStatusCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<ChangeCardStatusCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("not found")
        );
    }

    [Fact]
    public async Task Handle_StatusNotFound_DoesNotSave()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var card = Card.Create("My Card", null, CardPriority.Medium, statusId, projectId, 1, "user-123");

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _boardStatusRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(null));

        var command = new ChangeCardStatusCommand
        {
            CardId = card.Id,
            NewStatusId = Guid.NewGuid(),
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        var sut = new ChangeCardStatusCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<ChangeCardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _cardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CrossProjectStatus_ReturnsFailure()
    {
        // Arrange — card belongs to projectA, but the target status belongs to projectB.
        // The domain guard in Card.ChangeStatus rejects this transition.
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var originalStatusId = Guid.NewGuid();

        var card = Card.Create("Cross-Project Card", null, CardPriority.Medium, originalStatusId, projectA, 1, "user-123");
        var foreignStatus = BoardStatus.CreateForProject("Done", 2, projectB); // belongs to projectB

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _boardStatusRepository.GetEntityByIdAsync(foreignStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(foreignStatus));

        var command = new ChangeCardStatusCommand
        {
            CardId = card.Id,
            NewStatusId = foreignStatus.Id,
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        var sut = new ChangeCardStatusCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<ChangeCardStatusCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — domain rejected the transition
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldNotBeNullOrWhiteSpace()
        );
    }

    [Fact]
    public async Task Handle_CrossProjectStatus_DoesNotSave()
    {
        // Arrange
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var originalStatusId = Guid.NewGuid();

        var card = Card.Create("Cross-Project Card", null, CardPriority.Medium, originalStatusId, projectA, 1, "user-123");
        var foreignStatus = BoardStatus.CreateForProject("Done", 2, projectB);

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _boardStatusRepository.GetEntityByIdAsync(foreignStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(foreignStatus));

        var command = new ChangeCardStatusCommand
        {
            CardId = card.Id,
            NewStatusId = foreignStatus.Id,
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        var sut = new ChangeCardStatusCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<ChangeCardStatusCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — domain rejected, so no persistence should occur
        await _cardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // GetCardsByProjectQuery — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidProjectId_ReturnsCards()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var expectedCards = new List<CardDto>
        {
            new(Guid.NewGuid(), 1, "Card One",   "Desc 1", CardPriority.Low,    statusId, "Backlog", projectId, now, now, []),
            new(Guid.NewGuid(), 2, "Card Two",   "Desc 2", CardPriority.Medium, statusId, "Backlog", projectId, now, now, []),
            new(Guid.NewGuid(), 3, "Card Three", "Desc 3", CardPriority.High,   statusId, "Backlog", projectId, now, now, [])
        };

        _cardRepository.GetByProjectIdAsync(
                projectId,
                Arg.Any<Guid?>(),
                Arg.Any<CardPriority?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<List<CardDto>>(expectedCards));

        var query = new GetCardsByProjectQuery { ProjectId = projectId };

        var sut = new GetCardsByProjectQueryHandler(
            _cardRepository,
            NullLogger<GetCardsByProjectQueryHandler>.Instance);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(3),
            () => result[0].Title.ShouldBe("Card One"),
            () => result[1].Title.ShouldBe("Card Two"),
            () => result[2].Title.ShouldBe("Card Three")
        );
    }

    [Fact]
    public async Task Handle_ValidProjectId_CallsRepositoryWithCorrectArguments()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statusFilter = Guid.NewGuid();
        var priorityFilter = CardPriority.High;

        _cardRepository.GetByProjectIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<CardPriority?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<List<CardDto>>(new List<CardDto>()));

        var query = new GetCardsByProjectQuery
        {
            ProjectId = projectId,
            StatusId = statusFilter,
            Priority = priorityFilter
        };

        var sut = new GetCardsByProjectQueryHandler(
            _cardRepository,
            NullLogger<GetCardsByProjectQueryHandler>.Instance);

        // Act
        await sut.Handle(query, CancellationToken.None);

        // Assert — repository must be called with the exact filters from the query
        await _cardRepository.Received(1).GetByProjectIdAsync(
            projectId,
            statusFilter,
            priorityFilter,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProjectWithNoCards_ReturnsEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _cardRepository.GetByProjectIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<CardPriority?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<List<CardDto>>(new List<CardDto>()));

        var query = new GetCardsByProjectQuery { ProjectId = projectId };

        var sut = new GetCardsByProjectQueryHandler(
            _cardRepository,
            NullLogger<GetCardsByProjectQueryHandler>.Instance);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────────
    // GetCardStatusHistoryQuery — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCardId_ReturnsHistory()
    {
        // Arrange
        var cardId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var expectedHistory = new List<CardStatusHistoryDto>
        {
            new(Guid.NewGuid(), cardId, statusId, "Backlog",     Guid.NewGuid(), "In Progress", now.AddMinutes(-5), "user-123", ChangeSource.Manual),
            new(Guid.NewGuid(), cardId, null,      null,          statusId,       "Backlog",     now.AddMinutes(-10), "user-123", ChangeSource.Manual)
        };

        _cardRepository.GetStatusHistoryAsync(cardId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<List<CardStatusHistoryDto>>(expectedHistory));

        var query = new GetCardStatusHistoryQuery { CardId = cardId };

        var sut = new GetCardStatusHistoryQueryHandler(
            _cardRepository,
            NullLogger<GetCardStatusHistoryQueryHandler>.Instance);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(2),
            () => result[0].NewStatusName.ShouldBe("In Progress"),
            () => result[1].PreviousStatusId.ShouldBeNull()
        );
    }

    [Fact]
    public async Task Handle_ValidCardId_CallsRepositoryWithCorrectCardId()
    {
        // Arrange
        var cardId = Guid.NewGuid();

        _cardRepository.GetStatusHistoryAsync(cardId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<List<CardStatusHistoryDto>>(new List<CardStatusHistoryDto>()));

        var query = new GetCardStatusHistoryQuery { CardId = cardId };

        var sut = new GetCardStatusHistoryQueryHandler(
            _cardRepository,
            NullLogger<GetCardStatusHistoryQueryHandler>.Instance);

        // Act
        await sut.Handle(query, CancellationToken.None);

        // Assert
        await _cardRepository.Received(1).GetStatusHistoryAsync(cardId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CardWithNoHistory_ReturnsEmptyList()
    {
        // Arrange
        var cardId = Guid.NewGuid();

        _cardRepository.GetStatusHistoryAsync(cardId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<List<CardStatusHistoryDto>>(new List<CardStatusHistoryDto>()));

        var query = new GetCardStatusHistoryQuery { CardId = cardId };

        var sut = new GetCardStatusHistoryQueryHandler(
            _cardRepository,
            NullLogger<GetCardStatusHistoryQueryHandler>.Instance);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }
}
