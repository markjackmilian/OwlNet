using OwlNet.Application.Projects.Commands.InstallSystemAgent;
using Shouldly;

namespace OwlNet.Tests.Application.Projects.Commands;

/// <summary>
/// Unit tests for <see cref="InstallSystemAgentCommandValidator"/>.
/// Covers all validation rules: SystemAgentId, ProjectId, and FileName constraints
/// (required, minimum length, maximum length, allowed characters).
/// </summary>
public sealed class InstallSystemAgentCommandValidatorTests
{
    private readonly InstallSystemAgentCommandValidator _sut = new();

    // ──────────────────────────────────────────────
    // Helper
    // ──────────────────────────────────────────────

    private static InstallSystemAgentCommand CreateValidCommand(
        Guid? systemAgentId = null,
        Guid? projectId = null,
        string fileName = "git-agent",
        bool allowOverwrite = false) =>
        new()
        {
            SystemAgentId = systemAgentId ?? Guid.NewGuid(),
            ProjectId = projectId ?? Guid.NewGuid(),
            FileName = fileName,
            AllowOverwrite = allowOverwrite
        };

    // ──────────────────────────────────────────────
    // FileName — Required
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_EmptyFileName_FailsWithRequiredMessage()
    {
        // Arrange
        var command = CreateValidCommand(fileName: string.Empty);

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.ErrorMessage == "File name is required.")
        );
    }

    // ──────────────────────────────────────────────
    // FileName — Minimum Length
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_FileNameOfOneCharacter_FailsWithMinimumLengthMessage()
    {
        // Arrange
        var command = CreateValidCommand(fileName: "a");

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.ErrorMessage == "File name must be at least 2 characters.")
        );
    }

    // ──────────────────────────────────────────────
    // FileName — Maximum Length
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_FileNameOf51Characters_FailsWithMaximumLengthMessage()
    {
        // Arrange
        var fileName = new string('a', 51);
        var command = CreateValidCommand(fileName: fileName);

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.ErrorMessage == "File name must not exceed 50 characters.")
        );
    }

    // ──────────────────────────────────────────────
    // FileName — Invalid Characters
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData("git_agent")]   // underscore
    [InlineData("git agent")]   // space
    [InlineData("git.agent")]   // dot
    [InlineData("git/agent")]   // slash
    [InlineData("git@agent")]   // at sign
    public async Task ValidateAsync_FileNameWithInvalidCharacters_FailsWithRegexMessage(string invalidFileName)
    {
        // Arrange
        var command = CreateValidCommand(fileName: invalidFileName);

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.ErrorMessage == "File name can only contain letters, numbers, and hyphens.")
        );
    }

    // ──────────────────────────────────────────────
    // FileName — Valid
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData("git-agent")]           // hyphens
    [InlineData("ab")]                  // exactly 2 characters (boundary)
    [InlineData("MyAgent123")]          // mixed case + digits
    [InlineData("UPPERCASE")]           // all uppercase
    [InlineData("lowercase")]           // all lowercase
    public async Task ValidateAsync_ValidFileName_PassesValidation(string validFileName)
    {
        // Arrange
        var command = CreateValidCommand(fileName: validFileName);

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.Errors.ShouldNotContain(e =>
            e.PropertyName == nameof(InstallSystemAgentCommand.FileName));
    }

    // ──────────────────────────────────────────────
    // SystemAgentId — Required
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_EmptySystemAgentId_FailsWithRequiredMessage()
    {
        // Arrange
        var command = CreateValidCommand(systemAgentId: Guid.Empty);

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.ErrorMessage == "System agent ID is required.")
        );
    }

    // ──────────────────────────────────────────────
    // ProjectId — Required
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_EmptyProjectId_FailsWithRequiredMessage()
    {
        // Arrange
        var command = CreateValidCommand(projectId: Guid.Empty);

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeFalse(),
            () => result.Errors.ShouldContain(e => e.ErrorMessage == "Project ID is required.")
        );
    }

    // ──────────────────────────────────────────────
    // All Fields Valid
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_AllFieldsValid_PassesValidation()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}
