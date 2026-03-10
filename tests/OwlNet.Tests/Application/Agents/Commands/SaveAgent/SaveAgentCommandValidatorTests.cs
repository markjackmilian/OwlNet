using FluentValidation.Results;
using OwlNet.Application.Agents.Commands.SaveAgent;
using Shouldly;

namespace OwlNet.Tests.Application.Agents.Commands.SaveAgent;

/// <summary>
/// Unit tests for <see cref="SaveAgentCommandValidator"/>.
/// Covers ProjectId, AgentName (format, length, pattern), and Content validation rules.
/// </summary>
public sealed class SaveAgentCommandValidatorTests
{
    private readonly SaveAgentCommandValidator _validator = new();

    // ──────────────────────────────────────────────
    // Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("my-agent")]
    [InlineData("agent1")]
    [InlineData("A1")]
    [InlineData("code-reviewer")]
    [InlineData("test-agent-123")]
    [InlineData("ab")]
    [InlineData("X1-Y2-Z3")]
    public void Validate_ValidAgentNames_ShouldPass(string agentName)
    {
        // Arrange
        var command = CreateValidCommand(agentName: agentName);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_AgentNameAtMaxLength_ShouldPass()
    {
        // Arrange — exactly 50 characters, starts and ends with alphanumeric
        var agentName = "a" + new string('b', 48) + "c";
        var command = CreateValidCommand(agentName: agentName);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_AgentNameAtMinLength_ShouldPass()
    {
        // Arrange — exactly 2 characters
        var command = CreateValidCommand(agentName: "ab");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    // ──────────────────────────────────────────────
    // ProjectId Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyProjectId_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand(projectId: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.PropertyName == "ProjectId")
        );
    }

    // ──────────────────────────────────────────────
    // AgentName Validation — Empty / Length
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyAgentName_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand(agentName: "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.PropertyName == "AgentName")
        );
    }

    [Fact]
    public void Validate_WhitespaceAgentName_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand(agentName: " ");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.PropertyName == "AgentName")
        );
    }

    [Fact]
    public void Validate_AgentNameTooShort_ShouldFail()
    {
        // Arrange — 1 character is below MinimumLength(2)
        var command = CreateValidCommand(agentName: "a");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e =>
                e.PropertyName == "AgentName" &&
                e.ErrorMessage.Contains("at least 2 characters"))
        );
    }

    [Fact]
    public void Validate_AgentNameTooLong_ShouldFail()
    {
        // Arrange — 51 characters exceeds MaximumLength(50)
        var agentName = new string('a', 51);
        var command = CreateValidCommand(agentName: agentName);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e =>
                e.PropertyName == "AgentName" &&
                e.ErrorMessage.Contains("must not exceed 50 characters"))
        );
    }

    // ──────────────────────────────────────────────
    // AgentName Validation — Pattern
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_AgentNameWithSpaces_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand(agentName: "my agent");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e =>
                e.PropertyName == "AgentName" &&
                e.ErrorMessage.Contains("letters, numbers, and hyphens"))
        );
    }

    [Fact]
    public void Validate_AgentNameWithUnderscores_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand(agentName: "my_agent");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e =>
                e.PropertyName == "AgentName" &&
                e.ErrorMessage.Contains("letters, numbers, and hyphens"))
        );
    }

    [Fact]
    public void Validate_AgentNameStartingWithHyphen_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand(agentName: "-agent");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e =>
                e.PropertyName == "AgentName" &&
                e.ErrorMessage.Contains("must start and end with a letter or number"))
        );
    }

    [Fact]
    public void Validate_AgentNameEndingWithHyphen_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand(agentName: "agent-");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e =>
                e.PropertyName == "AgentName" &&
                e.ErrorMessage.Contains("must start and end with a letter or number"))
        );
    }

    [Theory]
    [InlineData("agent.name")]
    [InlineData("agent@name")]
    [InlineData("agent/name")]
    [InlineData("agent\\name")]
    [InlineData("agent!")]
    [InlineData("#agent")]
    public void Validate_AgentNameWithSpecialCharacters_ShouldFail(string agentName)
    {
        // Arrange
        var command = CreateValidCommand(agentName: agentName);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.PropertyName == "AgentName")
        );
    }

    // ──────────────────────────────────────────────
    // Content Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyContent_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand(content: "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e =>
                e.PropertyName == "Content" &&
                e.ErrorMessage == "Agent content cannot be empty.")
        );
    }

    [Fact]
    public void Validate_WhitespaceContent_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand(content: "   ");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.PropertyName == "Content")
        );
    }

    // ──────────────────────────────────────────────
    // Multiple Invalid Fields
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_MultipleInvalidFields_ShouldFailAll()
    {
        // Arrange — all three fields are invalid
        var command = new SaveAgentCommand
        {
            ProjectId = Guid.Empty,
            AgentName = "",
            Content = ""
        };

        // Act
        var result = _validator.Validate(command);

        // Assert — should have errors for all three properties
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.Count.ShouldBeGreaterThanOrEqualTo(3),
            () => result.Errors.ShouldContain(e => e.PropertyName == "ProjectId"),
            () => result.Errors.ShouldContain(e => e.PropertyName == "AgentName"),
            () => result.Errors.ShouldContain(e => e.PropertyName == "Content")
        );
    }

    // ──────────────────────────────────────────────
    // Error Messages
    // ──────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyProjectId_ReturnsCorrectErrorMessage()
    {
        // Arrange
        var command = CreateValidCommand(projectId: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.ShouldContain(e =>
            e.PropertyName == "ProjectId" &&
            e.ErrorMessage == "Project ID is required.");
    }

    [Fact]
    public void Validate_EmptyAgentName_ReturnsCorrectErrorMessage()
    {
        // Arrange
        var command = CreateValidCommand(agentName: "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.ShouldContain(e =>
            e.PropertyName == "AgentName" &&
            e.ErrorMessage == "Agent name is required.");
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static SaveAgentCommand CreateValidCommand(
        Guid? projectId = null,
        string agentName = "my-agent",
        string content = "# Agent Definition\n\nYou are a helpful assistant.") =>
        new()
        {
            ProjectId = projectId ?? Guid.NewGuid(),
            AgentName = agentName,
            Content = content
        };
}
