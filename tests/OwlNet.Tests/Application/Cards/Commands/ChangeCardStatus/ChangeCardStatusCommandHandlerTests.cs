using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Cards.Commands.ChangeCardStatus;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Domain.Entities;
using OwlNet.Domain.Enums;
using Shouldly;

namespace OwlNet.Tests.Application.Cards.Commands.ChangeCardStatus;

/// <summary>
/// Unit tests for <see cref="ChangeCardStatusCommandHandler"/>.
/// Verifies the handler's behavior when persisting a card status transition:
/// happy path, card-not-found, status-not-found, cross-project rejection, and same-status no-op.
/// </summary>
public sealed class ChangeCardStatusCommandHandlerTests
{
    private readonly ICardRepository _cardRepository;
    private readonly IBoardStatusRepository _boardStatusRepository;
    private readonly ChangeCardStatusCommandHandler _sut;

    public ChangeCardStatusCommandHandlerTests()
    {
        _cardRepository = Substitute.For<ICardRepository>();
        _boardStatusRepository = Substitute.For<IBoardStatusRepository>();
        _sut = new ChangeCardStatusCommandHandler(
            _cardRepository,
            _boardStatusRepository,
            NullLogger<ChangeCardStatusCommandHandler>.Instance);

        // Safe default: SaveChanges succeeds silently.
        _cardRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
    }

    // ──────────────────────────────────────────────
    // Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
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

        var command = new ChangeCardStatusCommand
        {
            CardId = card.Id,
            NewStatusId = newStatus.Id,
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesCardStatusAndPersists()
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

        var command = new ChangeCardStatusCommand
        {
            CardId = card.Id,
            NewStatusId = newStatus.Id,
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — domain entity was mutated and the change was persisted
        card.ShouldSatisfyAllConditions(
            () => card.StatusId.ShouldBe(newStatus.Id),
            () => card.StatusHistory.Count.ShouldBe(2) // initial + the new transition
        );
        await _cardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_RecordsStatusHistoryEntry()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var originalStatusId = Guid.NewGuid();

        var card = Card.Create("My Card", null, CardPriority.Medium, originalStatusId, projectId, 1, "user-123");
        var newStatus = BoardStatus.CreateForProject("Done", 2, projectId);

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _boardStatusRepository.GetEntityByIdAsync(newStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(newStatus));

        var command = new ChangeCardStatusCommand
        {
            CardId = card.Id,
            NewStatusId = newStatus.Id,
            ChangedBy = "trigger-agent",
            ChangeSource = ChangeSource.Trigger
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — a new history entry was appended with the correct metadata
        var latestHistory = card.StatusHistory.Last();
        latestHistory.ShouldSatisfyAllConditions(
            () => latestHistory.NewStatusId.ShouldBe(newStatus.Id),
            () => latestHistory.PreviousStatusId.ShouldBe(originalStatusId),
            () => latestHistory.ChangedBy.ShouldBe("trigger-agent"),
            () => latestHistory.ChangeSource.ShouldBe(ChangeSource.Trigger)
        );
    }

    // ──────────────────────────────────────────────
    // Card Not Found
    // ──────────────────────────────────────────────

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

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("not found", Case.Insensitive)
        );
    }

    [Fact]
    public async Task Handle_CardNotFound_DoesNotQueryStatusRepositoryOrSave()
    {
        // Arrange — short-circuit: once the card is missing, no further work should happen.
        _cardRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        var command = new ChangeCardStatusCommand
        {
            CardId = Guid.NewGuid(),
            NewStatusId = Guid.NewGuid(),
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _boardStatusRepository.DidNotReceive()
            .GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _cardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Status Not Found
    // ──────────────────────────────────────────────

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

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("not found", Case.Insensitive)
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

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _cardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Cross-Project Status (Domain Guard)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_CrossProjectStatus_ReturnsFailure()
    {
        // Arrange — card belongs to projectA; the target status belongs to projectB.
        // Card.ChangeStatus rejects this because newStatusProjectId != card.ProjectId.
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

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — domain guard fired; handler must surface the failure
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

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — domain rejected the transition; no persistence must occur
        await _cardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CrossProjectStatus_CardStatusIdUnchanged()
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

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — the card's StatusId must remain unchanged after a rejected transition
        card.StatusId.ShouldBe(originalStatusId);
    }

    // ──────────────────────────────────────────────
    // Same Status (No-Op)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_SameStatus_ReturnsSuccess()
    {
        // Arrange — the card is already in the target status.
        // Card.ChangeStatus treats this as a no-op and returns DomainResult.Success(),
        // so the handler must still return Result.Success() and call SaveChanges.
        var projectId = Guid.NewGuid();
        var currentStatusId = Guid.NewGuid();

        var card = Card.Create("Stable Card", null, CardPriority.Low, currentStatusId, projectId, 1, "user-123");

        // The target status has the same ID as the card's current status.
        // BoardStatus.CreateForProject generates a new Guid for Id, so we need a status
        // whose Id matches currentStatusId. We use the card's own StatusId as the command target.
        var sameStatus = BoardStatus.CreateForProject("Backlog", 0, projectId);

        // Re-create the card using the same status Id that the BoardStatus entity will have.
        // Since BoardStatus.Create generates its own Guid, we build the card around that Id.
        var sameStatusId = sameStatus.Id;
        var cardWithSameStatus = Card.Create("Stable Card", null, CardPriority.Low, sameStatusId, projectId, 1, "user-123");

        _cardRepository.GetEntityByIdAsync(cardWithSameStatus.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(cardWithSameStatus));
        _boardStatusRepository.GetEntityByIdAsync(sameStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(sameStatus));

        var command = new ChangeCardStatusCommand
        {
            CardId = cardWithSameStatus.Id,
            NewStatusId = sameStatus.Id, // same as current
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — no-op is still a success
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_SameStatus_StillCallsSaveChanges()
    {
        // Arrange — even when the status does not change, the handler must call SaveChanges
        // because the domain no-op returns DomainResult.Success() and the handler proceeds
        // to the persistence step unconditionally.
        var projectId = Guid.NewGuid();
        var sameStatus = BoardStatus.CreateForProject("Backlog", 0, projectId);
        var card = Card.Create("Stable Card", null, CardPriority.Low, sameStatus.Id, projectId, 1, "user-123");

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _boardStatusRepository.GetEntityByIdAsync(sameStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(sameStatus));

        var command = new ChangeCardStatusCommand
        {
            CardId = card.Id,
            NewStatusId = sameStatus.Id, // same as current
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — SaveChanges is called even for a no-op transition
        await _cardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameStatus_DoesNotAppendNewHistoryEntry()
    {
        // Arrange — the domain no-op must not add a history record.
        var projectId = Guid.NewGuid();
        var sameStatus = BoardStatus.CreateForProject("Backlog", 0, projectId);
        var card = Card.Create("Stable Card", null, CardPriority.Low, sameStatus.Id, projectId, 1, "user-123");

        // After Card.Create there is exactly 1 history entry (the initial assignment).
        var historyCountBeforeCommand = card.StatusHistory.Count;

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _boardStatusRepository.GetEntityByIdAsync(sameStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(sameStatus));

        var command = new ChangeCardStatusCommand
        {
            CardId = card.Id,
            NewStatusId = sameStatus.Id,
            ChangedBy = "user-123",
            ChangeSource = ChangeSource.Manual
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — no new history entry was appended
        card.StatusHistory.Count.ShouldBe(historyCountBeforeCommand);
    }
}
