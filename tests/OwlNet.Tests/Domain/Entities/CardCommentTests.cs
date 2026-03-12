using OwlNet.Domain.Entities;
using OwlNet.Domain.Enums;
using Shouldly;

namespace OwlNet.Tests.Domain.Entities;

/// <summary>
/// Unit tests for the <see cref="CardComment"/> domain entity.
/// Covers the <see cref="CardComment.Create"/> factory method: happy paths for human and agent
/// comments, content validation (null, whitespace, boundary lengths), and author-field validation
/// (missing AuthorId for human comments, missing AgentName for agent comments).
/// </summary>
public sealed class CardCommentTests
{
    // ──────────────────────────────────────────────
    // Create — Happy Path (Human)
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_HumanComment_ValidParameters_ReturnsCommentWithCorrectProperties()
    {
        // Arrange
        var cardId = Guid.NewGuid();
        const string content = "This is a human comment.";
        const string authorId = "user-abc";
        var before = DateTimeOffset.UtcNow;

        // Act
        var comment = CardComment.Create(
            cardId,
            content,
            CommentAuthorType.Human,
            authorId: authorId);

        // Assert
        var after = DateTimeOffset.UtcNow;

        comment.ShouldSatisfyAllConditions(
            () => comment.Id.ShouldNotBe(Guid.Empty),
            () => comment.CardId.ShouldBe(cardId),
            () => comment.Content.ShouldBe(content),
            () => comment.AuthorType.ShouldBe(CommentAuthorType.Human),
            () => comment.AuthorId.ShouldBe(authorId),
            () => comment.AgentName.ShouldBeNull(),
            () => comment.WorkflowTriggerId.ShouldBeNull(),
            () => comment.CreatedAt.ShouldBeGreaterThanOrEqualTo(before),
            () => comment.CreatedAt.ShouldBeLessThanOrEqualTo(after)
        );
    }

    // ──────────────────────────────────────────────
    // Create — Happy Path (Agent)
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_AgentComment_ValidParameters_ReturnsCommentWithCorrectProperties()
    {
        // Arrange
        var cardId = Guid.NewGuid();
        const string content = "Agent analysis complete.";
        const string agentName = "owl-coder";

        // Act
        var comment = CardComment.Create(
            cardId,
            content,
            CommentAuthorType.Agent,
            agentName: agentName);

        // Assert
        comment.ShouldSatisfyAllConditions(
            () => comment.Id.ShouldNotBe(Guid.Empty),
            () => comment.CardId.ShouldBe(cardId),
            () => comment.Content.ShouldBe(content),
            () => comment.AuthorType.ShouldBe(CommentAuthorType.Agent),
            () => comment.AgentName.ShouldBe(agentName),
            () => comment.AuthorId.ShouldBeNull(),
            () => comment.WorkflowTriggerId.ShouldBeNull()
        );
    }

    [Fact]
    public void Create_AgentCommentWithWorkflowTriggerId_SetsWorkflowTriggerId()
    {
        // Arrange
        var cardId = Guid.NewGuid();
        var workflowTriggerId = Guid.NewGuid();

        // Act
        var comment = CardComment.Create(
            cardId,
            "Triggered by workflow.",
            CommentAuthorType.Agent,
            agentName: "owl-tester",
            workflowTriggerId: workflowTriggerId);

        // Assert
        comment.WorkflowTriggerId.ShouldBe(workflowTriggerId);
    }

    // ──────────────────────────────────────────────
    // Create — Content Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_NullContent_ThrowsArgumentException()
    {
        // Arrange
        string content = null!;

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => CardComment.Create(
                Guid.NewGuid(),
                content,
                CommentAuthorType.Human,
                authorId: "user-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WhitespaceContent_ThrowsArgumentException(string content)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => CardComment.Create(
                Guid.NewGuid(),
                content,
                CommentAuthorType.Human,
                authorId: "user-1"));
    }

    [Fact]
    public void Create_ContentExceeds10000Chars_ThrowsArgumentException()
    {
        // Arrange — one character over the limit
        var content = new string('x', 10_001);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => CardComment.Create(
                Guid.NewGuid(),
                content,
                CommentAuthorType.Human,
                authorId: "user-1"));

        exception.Message.ShouldContain("10,000");
    }

    [Fact]
    public void Create_ContentExactly10000Chars_Succeeds()
    {
        // Arrange — exactly at the boundary
        var content = new string('x', 10_000);

        // Act
        var comment = CardComment.Create(
            Guid.NewGuid(),
            content,
            CommentAuthorType.Human,
            authorId: "user-1");

        // Assert
        comment.Content.Length.ShouldBe(10_000);
    }

    // ──────────────────────────────────────────────
    // Create — Human Author Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_HumanCommentWithNullAuthorId_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => CardComment.Create(
                Guid.NewGuid(),
                "Valid content",
                CommentAuthorType.Human,
                authorId: null));
    }

    [Fact]
    public void Create_HumanCommentWithWhitespaceAuthorId_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => CardComment.Create(
                Guid.NewGuid(),
                "Valid content",
                CommentAuthorType.Human,
                authorId: "   "));
    }

    // ──────────────────────────────────────────────
    // Create — Agent Author Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_AgentCommentWithNullAgentName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => CardComment.Create(
                Guid.NewGuid(),
                "Valid content",
                CommentAuthorType.Agent,
                agentName: null));
    }

    [Fact]
    public void Create_AgentCommentWithWhitespaceAgentName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => CardComment.Create(
                Guid.NewGuid(),
                "Valid content",
                CommentAuthorType.Agent,
                agentName: "   "));
    }
}
