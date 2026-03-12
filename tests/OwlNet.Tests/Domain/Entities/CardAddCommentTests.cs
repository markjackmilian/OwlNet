using OwlNet.Domain.Entities;
using OwlNet.Domain.Enums;
using Shouldly;

namespace OwlNet.Tests.Domain.Entities;

/// <summary>
/// Unit tests for the <see cref="Card.AddComment"/> method.
/// Covers appending human and agent comments to the <see cref="Card.Comments"/> collection,
/// verifying the returned <see cref="CardComment"/> properties, confirming that
/// <see cref="Card.UpdatedAt"/> is not modified by comment additions, and validating that
/// invalid inputs are rejected by delegating to <see cref="CardComment.Create"/>.
/// </summary>
public sealed class CardAddCommentTests
{
    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static Card CreateValidCard() =>
        Card.Create(
            title: "Test Card",
            description: null,
            priority: CardPriority.Medium,
            statusId: Guid.NewGuid(),
            projectId: Guid.NewGuid(),
            number: 1,
            createdBy: "user-123");

    // ──────────────────────────────────────────────
    // AddComment — Human Comment
    // ──────────────────────────────────────────────

    [Fact]
    public void AddComment_HumanComment_AppendsToCommentsCollection()
    {
        // Arrange
        var card = CreateValidCard();

        // Act
        card.AddComment("First comment", CommentAuthorType.Human, authorId: "user-1");

        // Assert
        card.Comments.Count.ShouldBe(1);
    }

    [Fact]
    public void AddComment_HumanComment_ReturnsNewComment()
    {
        // Arrange
        var card = CreateValidCard();
        const string content = "A human comment.";
        const string authorId = "user-42";

        // Act
        var comment = card.AddComment(content, CommentAuthorType.Human, authorId: authorId);

        // Assert
        comment.ShouldSatisfyAllConditions(
            () => comment.ShouldNotBeNull(),
            () => comment.Id.ShouldNotBe(Guid.Empty),
            () => comment.CardId.ShouldBe(card.Id),
            () => comment.Content.ShouldBe(content),
            () => comment.AuthorType.ShouldBe(CommentAuthorType.Human),
            () => comment.AuthorId.ShouldBe(authorId),
            () => comment.AgentName.ShouldBeNull()
        );
    }

    [Fact]
    public void AddComment_HumanComment_DoesNotUpdateUpdatedAt()
    {
        // Arrange
        var card = CreateValidCard();
        var updatedAtBefore = card.UpdatedAt;

        // Act
        card.AddComment("A comment", CommentAuthorType.Human, authorId: "user-1");

        // Assert — AddComment must not touch UpdatedAt (comments are a separate concern)
        card.UpdatedAt.ShouldBe(updatedAtBefore);
    }

    // ──────────────────────────────────────────────
    // AddComment — Agent Comment
    // ──────────────────────────────────────────────

    [Fact]
    public void AddComment_AgentComment_AppendsToCommentsCollection()
    {
        // Arrange
        var card = CreateValidCard();

        // Act
        card.AddComment("Agent result.", CommentAuthorType.Agent, agentName: "owl-coder");

        // Assert
        card.Comments.Count.ShouldBe(1);
    }

    // ──────────────────────────────────────────────
    // AddComment — Multiple Comments
    // ──────────────────────────────────────────────

    [Fact]
    public void AddComment_MultipleComments_AppendsAllInOrder()
    {
        // Arrange
        var card = CreateValidCard();

        // Act
        var first  = card.AddComment("First",  CommentAuthorType.Human, authorId: "user-1");
        var second = card.AddComment("Second", CommentAuthorType.Human, authorId: "user-2");
        var third  = card.AddComment("Third",  CommentAuthorType.Agent, agentName: "owl-coder");

        // Assert
        card.Comments.Count.ShouldBe(3);

        card.Comments.ShouldSatisfyAllConditions(
            () => card.Comments[0].Id.ShouldBe(first.Id),
            () => card.Comments[1].Id.ShouldBe(second.Id),
            () => card.Comments[2].Id.ShouldBe(third.Id)
        );
    }

    // ──────────────────────────────────────────────
    // AddComment — Validation (delegated to CardComment.Create)
    // ──────────────────────────────────────────────

    [Fact]
    public void AddComment_HumanCommentWithNullAuthorId_ThrowsArgumentException()
    {
        // Arrange
        var card = CreateValidCard();

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => card.AddComment("Valid content", CommentAuthorType.Human, authorId: null));
    }

    [Fact]
    public void AddComment_AgentCommentWithNullAgentName_ThrowsArgumentException()
    {
        // Arrange
        var card = CreateValidCard();

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => card.AddComment("Valid content", CommentAuthorType.Agent, agentName: null));
    }

    [Fact]
    public void AddComment_EmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var card = CreateValidCard();

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => card.AddComment(string.Empty, CommentAuthorType.Human, authorId: "user-1"));
    }
}
