using OwlNet.Domain.Entities;
using OwlNet.Domain.Enums;
using Shouldly;

namespace OwlNet.Tests.Domain.Entities;

/// <summary>
/// Unit tests for the <see cref="CardAttachment"/> domain entity.
/// Covers the <see cref="CardAttachment.Create"/> factory method: happy path (all 7 properties),
/// <c>FileName</c> validation (null, whitespace, length boundary), <c>Content</c> validation
/// (null, whitespace, very large), and <c>AgentName</c> validation (null, whitespace, length boundary).
/// </summary>
public sealed class CardAttachmentTests
{
    // ──────────────────────────────────────────────
    // Create — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_ValidParameters_ReturnsAttachmentWithCorrectProperties()
    {
        // Arrange
        var cardId = Guid.NewGuid();
        const string fileName = "code-review-summary.md";
        const string content = "# Code Review\n\nAll looks good.";
        const string agentName = "owl-coder";
        var workflowTriggerId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        // Act
        var attachment = CardAttachment.Create(cardId, fileName, content, agentName, workflowTriggerId);

        // Assert
        var after = DateTimeOffset.UtcNow;

        attachment.ShouldSatisfyAllConditions(
            () => attachment.Id.ShouldNotBe(Guid.Empty),
            () => attachment.CardId.ShouldBe(cardId),
            () => attachment.FileName.ShouldBe(fileName),
            () => attachment.Content.ShouldBe(content),
            () => attachment.AgentName.ShouldBe(agentName),
            () => attachment.WorkflowTriggerId.ShouldBe(workflowTriggerId),
            () => attachment.CreatedAt.ShouldBeGreaterThanOrEqualTo(before),
            () => attachment.CreatedAt.ShouldBeLessThanOrEqualTo(after)
        );
    }

    // ──────────────────────────────────────────────
    // Create — FileName Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_NullFileName_ThrowsArgumentException()
    {
        // Arrange
        string fileName = null!;

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => CardAttachment.Create(Guid.NewGuid(), fileName, "# content", "owl-coder", Guid.NewGuid()));
    }

    [Fact]
    public void Create_WhitespaceFileName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => CardAttachment.Create(Guid.NewGuid(), "   ", "# content", "owl-coder", Guid.NewGuid()));
    }

    [Fact]
    public void Create_FileNameExceeding200Chars_ThrowsArgumentException()
    {
        // Arrange — one character over the limit
        var fileName = new string('a', 201);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => CardAttachment.Create(Guid.NewGuid(), fileName, "# content", "owl-coder", Guid.NewGuid()));

        exception.Message.ShouldContain("200");
    }

    [Fact]
    public void Create_FileNameExactly200Chars_DoesNotThrow()
    {
        // Arrange — exactly at the boundary
        var fileName = new string('a', 200);

        // Act
        var attachment = CardAttachment.Create(Guid.NewGuid(), fileName, "# content", "owl-coder", Guid.NewGuid());

        // Assert
        attachment.FileName.Length.ShouldBe(200);
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
            () => CardAttachment.Create(Guid.NewGuid(), "report.md", content, "owl-coder", Guid.NewGuid()));
    }

    [Fact]
    public void Create_WhitespaceContent_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => CardAttachment.Create(Guid.NewGuid(), "report.md", "   ", "owl-coder", Guid.NewGuid()));
    }

    [Fact]
    public void Create_VeryLargeContent_DoesNotThrow()
    {
        // Arrange — 500 KB of content; no domain-level size limit
        var content = new string('x', 500_000);

        // Act
        var attachment = CardAttachment.Create(Guid.NewGuid(), "large-report.md", content, "owl-coder", Guid.NewGuid());

        // Assert
        attachment.Content.Length.ShouldBe(500_000);
    }

    // ──────────────────────────────────────────────
    // Create — AgentName Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_NullAgentName_ThrowsArgumentException()
    {
        // Arrange
        string agentName = null!;

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => CardAttachment.Create(Guid.NewGuid(), "report.md", "# content", agentName, Guid.NewGuid()));
    }

    [Fact]
    public void Create_WhitespaceAgentName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => CardAttachment.Create(Guid.NewGuid(), "report.md", "# content", "   ", Guid.NewGuid()));
    }

    [Fact]
    public void Create_AgentNameExceeding200Chars_ThrowsArgumentException()
    {
        // Arrange — one character over the limit
        var agentName = new string('a', 201);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => CardAttachment.Create(Guid.NewGuid(), "report.md", "# content", agentName, Guid.NewGuid()));

        exception.Message.ShouldContain("200");
    }

    [Fact]
    public void Create_AgentNameExactly200Chars_DoesNotThrow()
    {
        // Arrange — exactly at the boundary
        var agentName = new string('a', 200);

        // Act
        var attachment = CardAttachment.Create(Guid.NewGuid(), "report.md", "# content", agentName, Guid.NewGuid());

        // Assert
        attachment.AgentName.Length.ShouldBe(200);
    }
}

/// <summary>
/// Unit tests for the <see cref="Card.AddAttachment"/> method.
/// Covers appending attachments to the <see cref="Card.Attachments"/> collection, verifying the
/// returned <see cref="CardAttachment"/> properties, confirming that <see cref="Card.UpdatedAt"/>
/// is not modified by attachment additions, and validating that invalid inputs are rejected by
/// delegating to <see cref="CardAttachment.Create"/>.
/// </summary>
public sealed class CardAddAttachmentTests
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
    // AddAttachment — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void AddAttachment_ValidParameters_ReturnsAttachmentWithCorrectCardId()
    {
        // Arrange
        var card = CreateValidCard();
        var workflowTriggerId = Guid.NewGuid();

        // Act
        var attachment = card.AddAttachment(
            fileName: "code-review.md",
            content: "# Code Review\n\nLooks good.",
            agentName: "owl-coder",
            workflowTriggerId: workflowTriggerId);

        // Assert
        attachment.ShouldSatisfyAllConditions(
            () => attachment.ShouldNotBeNull(),
            () => attachment.Id.ShouldNotBe(Guid.Empty),
            () => attachment.CardId.ShouldBe(card.Id),
            () => attachment.FileName.ShouldBe("code-review.md"),
            () => attachment.AgentName.ShouldBe("owl-coder"),
            () => attachment.WorkflowTriggerId.ShouldBe(workflowTriggerId)
        );
    }

    [Fact]
    public void AddAttachment_ValidParameters_AppendsToAttachmentsCollection()
    {
        // Arrange
        var card = CreateValidCard();

        // Act
        card.AddAttachment(
            fileName: "report.md",
            content: "# Report",
            agentName: "owl-tester",
            workflowTriggerId: Guid.NewGuid());

        // Assert
        card.Attachments.Count.ShouldBe(1);
    }

    [Fact]
    public void AddAttachment_ValidParameters_DoesNotUpdateCardUpdatedAt()
    {
        // Arrange
        var card = CreateValidCard();
        var updatedAtBefore = card.UpdatedAt;

        // Act
        card.AddAttachment(
            fileName: "report.md",
            content: "# Report",
            agentName: "owl-tester",
            workflowTriggerId: Guid.NewGuid());

        // Assert — AddAttachment must not touch UpdatedAt (attachments are a separate concern)
        card.UpdatedAt.ShouldBe(updatedAtBefore);
    }

    // ──────────────────────────────────────────────
    // AddAttachment — Validation (delegated to CardAttachment.Create)
    // ──────────────────────────────────────────────

    [Fact]
    public void AddAttachment_BlankFileName_ThrowsArgumentException()
    {
        // Arrange
        var card = CreateValidCard();

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => card.AddAttachment(
                fileName: "   ",
                content: "# content",
                agentName: "owl-coder",
                workflowTriggerId: Guid.NewGuid()));
    }

    [Fact]
    public void AddAttachment_BlankContent_ThrowsArgumentException()
    {
        // Arrange
        var card = CreateValidCard();

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => card.AddAttachment(
                fileName: "report.md",
                content: "   ",
                agentName: "owl-coder",
                workflowTriggerId: Guid.NewGuid()));
    }

    [Fact]
    public void AddAttachment_BlankAgentName_ThrowsArgumentException()
    {
        // Arrange
        var card = CreateValidCard();

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => card.AddAttachment(
                fileName: "report.md",
                content: "# content",
                agentName: "   ",
                workflowTriggerId: Guid.NewGuid()));
    }
}
