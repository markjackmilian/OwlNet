using OwlNet.Application.Cards.Commands.AddAgentComment;
using OwlNet.Application.Cards.Commands.AddHumanComment;
using Shouldly;

namespace OwlNet.Tests.Application.Cards;

/// <summary>
/// Unit tests for <see cref="AddHumanCommentCommandValidator"/> and
/// <see cref="AddAgentCommentCommandValidator"/>.
/// Covers all validation rules: CardId (required), Content (required, max 10,000 chars),
/// AuthorId (required for human comments), and AgentName (required for agent comments).
/// No mocking is needed — validators are pure synchronous logic with no external dependencies.
/// </summary>
public sealed class CardCommentValidatorTests
{
    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static AddHumanCommentCommand CreateValidHumanCommand(
        Guid? cardId = null,
        string content = "This is a valid human comment.",
        string authorId = "user-123") =>
        new()
        {
            CardId = cardId ?? Guid.NewGuid(),
            Content = content,
            AuthorId = authorId
        };

    private static AddAgentCommentCommand CreateValidAgentCommand(
        Guid? cardId = null,
        string content = "This is a valid agent comment.",
        string agentName = "owl-coder") =>
        new()
        {
            CardId = cardId ?? Guid.NewGuid(),
            Content = content,
            AgentName = agentName
        };

    // ══════════════════════════════════════════════
    // AddHumanCommentCommandValidator
    // ══════════════════════════════════════════════

    private readonly AddHumanCommentCommandValidator _humanValidator = new();

    // ──────────────────────────────────────────────
    // Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_ValidHumanComment_PassesValidation()
    {
        // Arrange
        var command = CreateValidHumanCommand();

        // Act
        var result = _humanValidator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeTrue(),
            () => result.Errors.ShouldBeEmpty()
        );
    }

    // ──────────────────────────────────────────────
    // CardId Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyCardId_FailsValidation()
    {
        // Arrange
        var command = CreateValidHumanCommand(cardId: Guid.Empty);

        // Act
        var result = _humanValidator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.PropertyName == "CardId")
        );
    }

    // ──────────────────────────────────────────────
    // Content Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyContent_FailsValidation()
    {
        // Arrange
        var command = CreateValidHumanCommand(content: "");

        // Act
        var result = _humanValidator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e =>
                e.PropertyName == "Content" &&
                e.ErrorMessage.Contains("Comment cannot be empty."))
        );
    }

    [Fact]
    public void Validate_WhitespaceContent_FailsValidation()
    {
        // Arrange
        var command = CreateValidHumanCommand(content: "   ");

        // Act
        var result = _humanValidator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.PropertyName == "Content")
        );
    }

    [Fact]
    public void Validate_ContentExceeds10000Chars_FailsValidation()
    {
        // Arrange — 10,001 characters exceeds the MaximumLength(10_000) rule
        var command = CreateValidHumanCommand(content: new string('x', 10_001));

        // Act
        var result = _humanValidator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e =>
                e.PropertyName == "Content" &&
                e.ErrorMessage.Contains("Comment cannot exceed 10,000 characters."))
        );
    }

    [Fact]
    public void Validate_ContentExactly10000Chars_PassesValidation()
    {
        // Arrange — exactly 10,000 characters is the boundary value that must pass
        var command = CreateValidHumanCommand(content: new string('x', 10_000));

        // Act
        var result = _humanValidator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    // ──────────────────────────────────────────────
    // AuthorId Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyAuthorId_FailsValidation()
    {
        // Arrange
        var command = CreateValidHumanCommand(authorId: "");

        // Act
        var result = _humanValidator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e =>
                e.PropertyName == "AuthorId" &&
                e.ErrorMessage.Contains("Author ID is required for human comments."))
        );
    }

    [Fact]
    public void Validate_WhitespaceAuthorId_FailsValidation()
    {
        // Arrange
        var command = CreateValidHumanCommand(authorId: "   ");

        // Act
        var result = _humanValidator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.PropertyName == "AuthorId")
        );
    }

    // ══════════════════════════════════════════════
    // AddAgentCommentCommandValidator
    // ══════════════════════════════════════════════

    private readonly AddAgentCommentCommandValidator _agentValidator = new();

    // ──────────────────────────────────────────────
    // Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_ValidAgentComment_PassesValidation()
    {
        // Arrange
        var command = CreateValidAgentCommand();

        // Act
        var result = _agentValidator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeTrue(),
            () => result.Errors.ShouldBeEmpty()
        );
    }

    // ──────────────────────────────────────────────
    // CardId Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyCardId_AgentComment_FailsValidation()
    {
        // Arrange
        var command = CreateValidAgentCommand(cardId: Guid.Empty);

        // Act
        var result = _agentValidator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.PropertyName == "CardId")
        );
    }

    // ──────────────────────────────────────────────
    // Content Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyContent_AgentComment_FailsValidation()
    {
        // Arrange
        var command = CreateValidAgentCommand(content: "");

        // Act
        var result = _agentValidator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e =>
                e.PropertyName == "Content" &&
                e.ErrorMessage.Contains("Comment cannot be empty."))
        );
    }

    [Fact]
    public void Validate_ContentExceeds10000Chars_AgentComment_FailsValidation()
    {
        // Arrange — 10,001 characters exceeds the MaximumLength(10_000) rule
        var command = CreateValidAgentCommand(content: new string('x', 10_001));

        // Act
        var result = _agentValidator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e =>
                e.PropertyName == "Content" &&
                e.ErrorMessage.Contains("Comment cannot exceed 10,000 characters."))
        );
    }

    // ──────────────────────────────────────────────
    // AgentName Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyAgentName_FailsValidation()
    {
        // Arrange
        var command = CreateValidAgentCommand(agentName: "");

        // Act
        var result = _agentValidator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e =>
                e.PropertyName == "AgentName" &&
                e.ErrorMessage.Contains("Agent name is required for agent comments."))
        );
    }

    [Fact]
    public void Validate_WhitespaceAgentName_FailsValidation()
    {
        // Arrange
        var command = CreateValidAgentCommand(agentName: "   ");

        // Act
        var result = _agentValidator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.PropertyName == "AgentName")
        );
    }
}
