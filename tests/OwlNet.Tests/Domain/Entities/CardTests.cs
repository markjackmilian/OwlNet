using OwlNet.Domain.Common;
using OwlNet.Domain.Entities;
using OwlNet.Domain.Enums;
using Shouldly;

namespace OwlNet.Tests.Domain.Entities;

/// <summary>
/// Unit tests for the <see cref="Card"/> domain entity.
/// Covers the <see cref="Card.Create"/> factory method (validation, boundary values, initial history),
/// the <see cref="Card.Update"/> mutation method (property updates, validation, timestamp refresh), and
/// the <see cref="Card.ChangeStatus"/> method (no-op, valid transition, cross-project guard, history recording).
/// </summary>
public sealed class CardTests
{
    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static Card CreateValidCard(
        string title = "My Card",
        string? description = "Some description",
        CardPriority priority = CardPriority.Medium,
        Guid? statusId = null,
        Guid? projectId = null,
        int number = 1,
        string createdBy = "user-123")
    {
        return Card.Create(
            title,
            description,
            priority,
            statusId ?? Guid.NewGuid(),
            projectId ?? Guid.NewGuid(),
            number,
            createdBy);
    }

    // ──────────────────────────────────────────────
    // Create — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_ValidParameters_ReturnsCardWithCorrectProperties()
    {
        // Arrange
        var title = "  Fix login bug  ";
        var description = "Detailed description";
        var priority = CardPriority.High;
        var statusId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var number = 42;
        var createdBy = "user-abc";
        var before = DateTimeOffset.UtcNow;

        // Act
        var card = Card.Create(title, description, priority, statusId, projectId, number, createdBy);

        // Assert
        var after = DateTimeOffset.UtcNow;

        card.ShouldSatisfyAllConditions(
            () => card.Id.ShouldNotBe(Guid.Empty),
            () => card.Number.ShouldBe(number),
            () => card.Title.ShouldBe("Fix login bug"),
            () => card.Description.ShouldBe(description),
            () => card.Priority.ShouldBe(priority),
            () => card.StatusId.ShouldBe(statusId),
            () => card.ProjectId.ShouldBe(projectId),
            () => card.CreatedAt.ShouldBeGreaterThanOrEqualTo(before),
            () => card.CreatedAt.ShouldBeLessThanOrEqualTo(after),
            () => card.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before),
            () => card.UpdatedAt.ShouldBeLessThanOrEqualTo(after),
            () => card.CreatedAt.ShouldBe(card.UpdatedAt)
        );
    }

    [Fact]
    public void Create_ValidParameters_AddsInitialHistoryRecord()
    {
        // Arrange
        var statusId = Guid.NewGuid();
        var createdBy = "user-xyz";

        // Act
        var card = Card.Create("My Card", null, CardPriority.Low, statusId, Guid.NewGuid(), 1, createdBy);

        // Assert
        card.StatusHistory.Count.ShouldBe(1);

        var history = card.StatusHistory[0];
        history.ShouldSatisfyAllConditions(
            () => history.Id.ShouldNotBe(Guid.Empty),
            () => history.CardId.ShouldBe(card.Id),
            () => history.PreviousStatusId.ShouldBeNull(),
            () => history.NewStatusId.ShouldBe(statusId),
            () => history.ChangedBy.ShouldBe(createdBy),
            () => history.ChangeSource.ShouldBe(ChangeSource.Manual)
        );
    }

    // ──────────────────────────────────────────────
    // Create — Title Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_NullTitle_ThrowsArgumentException()
    {
        // Arrange
        string title = null!;

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => Card.Create(title, null, CardPriority.Medium, Guid.NewGuid(), Guid.NewGuid(), 1, "user-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_WhitespaceTitle_ThrowsArgumentException(string title)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => Card.Create(title, null, CardPriority.Medium, Guid.NewGuid(), Guid.NewGuid(), 1, "user-1"));
    }

    [Fact]
    public void Create_TitleExceeds200Chars_ThrowsArgumentException()
    {
        // Arrange
        var title = new string('x', 201);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => Card.Create(title, null, CardPriority.Medium, Guid.NewGuid(), Guid.NewGuid(), 1, "user-1"));

        exception.Message.ShouldContain("200");
    }

    [Fact]
    public void Create_TitleExactly200Chars_Succeeds()
    {
        // Arrange
        var title = new string('x', 200);

        // Act
        var card = Card.Create(title, null, CardPriority.Medium, Guid.NewGuid(), Guid.NewGuid(), 1, "user-1");

        // Assert
        card.Title.ShouldBe(title);
    }

    [Fact]
    public void Create_TitleWithLeadingTrailingSpaces_TrimmedOnCreate()
    {
        // Arrange
        var title = "   Trimmed Title   ";

        // Act
        var card = Card.Create(title, null, CardPriority.Medium, Guid.NewGuid(), Guid.NewGuid(), 1, "user-1");

        // Assert
        card.Title.ShouldBe("Trimmed Title");
    }

    // ──────────────────────────────────────────────
    // Create — Description Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_NullDescription_SetsEmptyString()
    {
        // Act
        var card = Card.Create("Valid Title", null, CardPriority.Low, Guid.NewGuid(), Guid.NewGuid(), 1, "user-1");

        // Assert
        card.Description.ShouldBe(string.Empty);
    }

    [Fact]
    public void Create_DescriptionExceeds5000Chars_ThrowsArgumentException()
    {
        // Arrange
        var description = new string('d', 5001);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => Card.Create("Valid Title", description, CardPriority.Medium, Guid.NewGuid(), Guid.NewGuid(), 1, "user-1"));

        exception.Message.ShouldContain("5000");
    }

    [Fact]
    public void Create_DescriptionExactly5000Chars_Succeeds()
    {
        // Arrange
        var description = new string('d', 5000);

        // Act
        var card = Card.Create("Valid Title", description, CardPriority.Medium, Guid.NewGuid(), Guid.NewGuid(), 1, "user-1");

        // Assert
        card.Description.ShouldBe(description);
    }

    // ──────────────────────────────────────────────
    // Create — CreatedBy Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_NullCreatedBy_ThrowsArgumentException()
    {
        // Arrange
        string createdBy = null!;

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => Card.Create("Valid Title", null, CardPriority.Medium, Guid.NewGuid(), Guid.NewGuid(), 1, createdBy));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_WhitespaceCreatedBy_ThrowsArgumentException(string createdBy)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => Card.Create("Valid Title", null, CardPriority.Medium, Guid.NewGuid(), Guid.NewGuid(), 1, createdBy));
    }

    // ──────────────────────────────────────────────
    // Update — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Update_ValidParameters_UpdatesProperties()
    {
        // Arrange
        var card = CreateValidCard(title: "Original Title", description: "Original desc", priority: CardPriority.Low);
        var newTitle = "Updated Title";
        var newDescription = "Updated description";
        var newPriority = CardPriority.Critical;

        // Act
        card.Update(newTitle, newDescription, newPriority);

        // Assert
        card.ShouldSatisfyAllConditions(
            () => card.Title.ShouldBe(newTitle),
            () => card.Description.ShouldBe(newDescription),
            () => card.Priority.ShouldBe(newPriority)
        );
    }

    [Fact]
    public void Update_ValidParameters_UpdatesUpdatedAt()
    {
        // Arrange
        var card = CreateValidCard();
        var createdAt = card.CreatedAt;

        // Act
        card.Update("New Title", "New desc", CardPriority.High);

        // Assert
        card.ShouldSatisfyAllConditions(
            () => card.CreatedAt.ShouldBe(createdAt),
            () => card.UpdatedAt.ShouldBeGreaterThanOrEqualTo(createdAt)
        );
    }

    [Fact]
    public void Update_NullDescription_SetsEmptyString()
    {
        // Arrange
        var card = CreateValidCard(description: "Some description");

        // Act
        card.Update("Valid Title", null, CardPriority.Medium);

        // Assert
        card.Description.ShouldBe(string.Empty);
    }

    // ──────────────────────────────────────────────
    // Update — Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Update_NullTitle_ThrowsArgumentException()
    {
        // Arrange
        var card = CreateValidCard();

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => card.Update(null!, "desc", CardPriority.Medium));
    }

    [Fact]
    public void Update_TitleExceeds200Chars_ThrowsArgumentException()
    {
        // Arrange
        var card = CreateValidCard();
        var longTitle = new string('x', 201);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => card.Update(longTitle, "desc", CardPriority.Medium));

        exception.Message.ShouldContain("200");
    }

    [Fact]
    public void Update_DescriptionExceeds5000Chars_ThrowsArgumentException()
    {
        // Arrange
        var card = CreateValidCard();
        var longDescription = new string('d', 5001);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => card.Update("Valid Title", longDescription, CardPriority.Medium));

        exception.Message.ShouldContain("5000");
    }

    // ──────────────────────────────────────────────
    // ChangeStatus — No-Op (same status)
    // ──────────────────────────────────────────────

    [Fact]
    public void ChangeStatus_SameStatus_ReturnsSuccessWithNoHistoryRecord()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var card = CreateValidCard(statusId: statusId, projectId: projectId);
        var updatedAtBefore = card.UpdatedAt;

        // Act
        var result = card.ChangeStatus(statusId, projectId, "user-1", ChangeSource.Manual);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => card.StatusHistory.Count.ShouldBe(1),
            () => card.UpdatedAt.ShouldBe(updatedAtBefore)
        );
    }

    // ──────────────────────────────────────────────
    // ChangeStatus — Valid Transition
    // ──────────────────────────────────────────────

    [Fact]
    public void ChangeStatus_DifferentStatusSameProject_ReturnsSuccessAndAddsHistory()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var oldStatusId = Guid.NewGuid();
        var newStatusId = Guid.NewGuid();
        var changedBy = "user-456";
        var card = CreateValidCard(statusId: oldStatusId, projectId: projectId);

        // Act
        var result = card.ChangeStatus(newStatusId, projectId, changedBy, ChangeSource.Manual);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        card.StatusId.ShouldBe(newStatusId);
        card.StatusHistory.Count.ShouldBe(2);

        var latestHistory = card.StatusHistory[1];
        latestHistory.ShouldSatisfyAllConditions(
            () => latestHistory.PreviousStatusId.ShouldBe(oldStatusId),
            () => latestHistory.NewStatusId.ShouldBe(newStatusId),
            () => latestHistory.ChangedBy.ShouldBe(changedBy),
            () => latestHistory.ChangeSource.ShouldBe(ChangeSource.Manual)
        );
    }

    [Fact]
    public void ChangeStatus_ValidChange_UpdatesUpdatedAt()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateValidCard(projectId: projectId);
        var updatedAtBefore = card.UpdatedAt;

        // Act
        card.ChangeStatus(Guid.NewGuid(), projectId, "user-1", ChangeSource.Manual);

        // Assert
        card.UpdatedAt.ShouldBeGreaterThanOrEqualTo(updatedAtBefore);
    }

    [Fact]
    public void ChangeStatus_WithTriggerSource_RecordsCorrectChangeSource()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateValidCard(projectId: projectId);

        // Act
        card.ChangeStatus(Guid.NewGuid(), projectId, "trigger-agent-1", ChangeSource.Trigger);

        // Assert
        var latestHistory = card.StatusHistory[1];
        latestHistory.ShouldSatisfyAllConditions(
            () => latestHistory.ChangeSource.ShouldBe(ChangeSource.Trigger),
            () => latestHistory.ChangedBy.ShouldBe("trigger-agent-1")
        );
    }

    // ──────────────────────────────────────────────
    // ChangeStatus — Cross-Project Guard
    // ──────────────────────────────────────────────

    [Fact]
    public void ChangeStatus_StatusFromDifferentProject_ReturnsFailure()
    {
        // Arrange
        var cardProjectId = Guid.NewGuid();
        var differentProjectId = Guid.NewGuid();
        var card = CreateValidCard(projectId: cardProjectId);

        // Act
        var result = card.ChangeStatus(Guid.NewGuid(), differentProjectId, "user-1", ChangeSource.Manual);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("does not belong to this project")
        );
    }

    [Fact]
    public void ChangeStatus_StatusFromDifferentProject_DoesNotMutateCard()
    {
        // Arrange
        var cardProjectId = Guid.NewGuid();
        var originalStatusId = Guid.NewGuid();
        var card = CreateValidCard(statusId: originalStatusId, projectId: cardProjectId);
        var historyCountBefore = card.StatusHistory.Count;

        // Act
        card.ChangeStatus(Guid.NewGuid(), Guid.NewGuid(), "user-1", ChangeSource.Manual);

        // Assert
        card.ShouldSatisfyAllConditions(
            () => card.StatusId.ShouldBe(originalStatusId),
            () => card.StatusHistory.Count.ShouldBe(historyCountBefore)
        );
    }
}
