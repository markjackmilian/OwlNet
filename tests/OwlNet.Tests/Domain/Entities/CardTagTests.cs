using OwlNet.Domain.Entities;
using OwlNet.Domain.Enums;
using Shouldly;

namespace OwlNet.Tests.Domain.Entities;

/// <summary>
/// Unit tests for the <see cref="Card.AddTag"/> and <see cref="Card.RemoveTag"/> methods.
/// Covers the same-project guard, idempotency, no-op removal, and <c>UpdatedAt</c> refresh.
/// </summary>
public sealed class CardTagTests
{
    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static Card CreateCard(Guid? projectId = null)
    {
        return Card.Create(
            title: "Test Card",
            description: null,
            priority: CardPriority.Medium,
            statusId: Guid.NewGuid(),
            projectId: projectId ?? Guid.NewGuid(),
            number: 1,
            createdBy: "user-123");
    }

    // ──────────────────────────────────────────────
    // AddTag — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void AddTag_SameProject_ReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var tagId = Guid.NewGuid();

        // Act
        var result = card.AddTag(tagId, projectId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AddTag_SameProject_AddsTagToCollection()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var tagId = Guid.NewGuid();

        // Act
        card.AddTag(tagId, projectId);

        // Assert
        card.Tags.ShouldSatisfyAllConditions(
            () => card.Tags.Count.ShouldBe(1),
            () => card.Tags[0].TagId.ShouldBe(tagId),
            () => card.Tags[0].CardId.ShouldBe(card.Id)
        );
    }

    [Fact]
    public void AddTag_SameProject_UpdatesUpdatedAt()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var updatedAtBefore = card.UpdatedAt;

        // Act
        card.AddTag(Guid.NewGuid(), projectId);

        // Assert
        card.UpdatedAt.ShouldBeGreaterThanOrEqualTo(updatedAtBefore);
    }

    // ──────────────────────────────────────────────
    // AddTag — Cross-Project Guard
    // ──────────────────────────────────────────────

    [Fact]
    public void AddTag_DifferentProject_ReturnsFailure()
    {
        // Arrange
        var cardProjectId = Guid.NewGuid();
        var differentProjectId = Guid.NewGuid();
        var card = CreateCard(cardProjectId);

        // Act
        var result = card.AddTag(Guid.NewGuid(), differentProjectId);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("does not belong to this project")
        );
    }

    [Fact]
    public void AddTag_DifferentProject_DoesNotAddTagToCollection()
    {
        // Arrange
        var cardProjectId = Guid.NewGuid();
        var differentProjectId = Guid.NewGuid();
        var card = CreateCard(cardProjectId);

        // Act
        card.AddTag(Guid.NewGuid(), differentProjectId);

        // Assert
        card.Tags.ShouldBeEmpty();
    }

    [Fact]
    public void AddTag_DifferentProject_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var cardProjectId = Guid.NewGuid();
        var card = CreateCard(cardProjectId);
        var updatedAtBefore = card.UpdatedAt;

        // Act
        card.AddTag(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        card.UpdatedAt.ShouldBe(updatedAtBefore);
    }

    // ──────────────────────────────────────────────
    // AddTag — Idempotency
    // ──────────────────────────────────────────────

    [Fact]
    public void AddTag_SameTagTwice_IsIdempotent()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var tagId = Guid.NewGuid();

        // Act
        card.AddTag(tagId, projectId);
        var result = card.AddTag(tagId, projectId);

        // Assert — second call is a no-op: success returned, no duplicate entry
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => card.Tags.Count.ShouldBe(1)
        );
    }

    [Fact]
    public void AddTag_MultipleDistinctTags_AddsAll()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var tagId1 = Guid.NewGuid();
        var tagId2 = Guid.NewGuid();

        // Act
        card.AddTag(tagId1, projectId);
        card.AddTag(tagId2, projectId);

        // Assert
        card.Tags.Count.ShouldBe(2);
    }

    // ──────────────────────────────────────────────
    // RemoveTag — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void RemoveTag_ExistingTag_RemovesFromTags()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var tagId = Guid.NewGuid();
        card.AddTag(tagId, projectId);

        // Act
        card.RemoveTag(tagId);

        // Assert
        card.Tags.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveTag_ExistingTag_UpdatesUpdatedAt()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var tagId = Guid.NewGuid();
        card.AddTag(tagId, projectId);
        var updatedAtBefore = card.UpdatedAt;

        // Act
        card.RemoveTag(tagId);

        // Assert
        card.UpdatedAt.ShouldBeGreaterThanOrEqualTo(updatedAtBefore);
    }

    [Fact]
    public void RemoveTag_ExistingTag_OnlyRemovesTargetTag()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var tagId1 = Guid.NewGuid();
        var tagId2 = Guid.NewGuid();
        card.AddTag(tagId1, projectId);
        card.AddTag(tagId2, projectId);

        // Act
        card.RemoveTag(tagId1);

        // Assert
        card.ShouldSatisfyAllConditions(
            () => card.Tags.Count.ShouldBe(1),
            () => card.Tags[0].TagId.ShouldBe(tagId2)
        );
    }

    // ──────────────────────────────────────────────
    // RemoveTag — No-Op
    // ──────────────────────────────────────────────

    [Fact]
    public void RemoveTag_NonExistingTag_IsNoOp()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var existingTagId = Guid.NewGuid();
        card.AddTag(existingTagId, projectId);

        // Act — remove a tag that was never added
        card.RemoveTag(Guid.NewGuid());

        // Assert — existing tag is untouched
        card.Tags.Count.ShouldBe(1);
    }

    [Fact]
    public void RemoveTag_NonExistingTag_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var updatedAtBefore = card.UpdatedAt;

        // Act — remove a tag that was never added
        card.RemoveTag(Guid.NewGuid());

        // Assert
        card.UpdatedAt.ShouldBe(updatedAtBefore);
    }
}
