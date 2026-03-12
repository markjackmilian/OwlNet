using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Domain.Entities;

/// <summary>
/// Unit tests for the <see cref="ProjectTag"/> domain entity.
/// Covers the <see cref="ProjectTag.Create"/> factory method (validation, color, timestamps),
/// the <see cref="ProjectTag.Rename"/> mutation method, and the
/// <see cref="ProjectTag.UpdateColor"/> mutation method.
/// </summary>
public sealed class ProjectTagTests
{
    // ──────────────────────────────────────────────
    // Create — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_ValidNameAndColor_ReturnsTagWithCorrectProperties()
    {
        // Arrange
        var name = "  Bug  ";
        var color = "#FF5733";
        var projectId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        // Act
        var tag = ProjectTag.Create(name, color, projectId);

        // Assert
        var after = DateTimeOffset.UtcNow;

        tag.ShouldSatisfyAllConditions(
            () => tag.Id.ShouldNotBe(Guid.Empty),
            () => tag.ProjectId.ShouldBe(projectId),
            () => tag.Name.ShouldBe("Bug"),          // trimmed
            () => tag.Color.ShouldBe(color),
            () => tag.CreatedAt.ShouldBeGreaterThanOrEqualTo(before),
            () => tag.CreatedAt.ShouldBeLessThanOrEqualTo(after),
            () => tag.UpdatedAt.ShouldBe(tag.CreatedAt)
        );
    }

    [Fact]
    public void Create_ValidNameNullColor_ReturnsTagWithNullColor()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var tag = ProjectTag.Create("Feature", null, projectId);

        // Assert
        tag.Color.ShouldBeNull();
    }

    [Theory]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    [InlineData("#aabbcc")]
    [InlineData("#1A2B3C")]
    public void Create_ValidHexColor_SetsColor(string color)
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var tag = ProjectTag.Create("Tag", color, projectId);

        // Assert
        tag.Color.ShouldBe(color);
    }

    // ──────────────────────────────────────────────
    // Create — Name Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_NullName_ThrowsArgumentException()
    {
        // Arrange
        string name = null!;

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => ProjectTag.Create(name, null, Guid.NewGuid()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_BlankName_ThrowsArgumentException(string name)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => ProjectTag.Create(name, null, Guid.NewGuid()));
    }

    [Fact]
    public void Create_NameExceeds50Chars_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('x', 51);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => ProjectTag.Create(longName, null, Guid.NewGuid()));

        exception.Message.ShouldContain("50");
    }

    [Fact]
    public void Create_NameExactly50Chars_Succeeds()
    {
        // Arrange
        var name = new string('x', 50);

        // Act
        var tag = ProjectTag.Create(name, null, Guid.NewGuid());

        // Assert
        tag.Name.ShouldBe(name);
    }

    // ──────────────────────────────────────────────
    // Create — Color Validation
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData("red")]
    [InlineData("#GGG")]
    [InlineData("#12345")]   // 5 hex digits
    [InlineData("#1234567")] // 7 hex digits
    [InlineData("FF5733")]   // missing #
    [InlineData("")]
    public void Create_InvalidHexColor_ThrowsArgumentException(string color)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => ProjectTag.Create("Tag", color, Guid.NewGuid()));

        exception.Message.ShouldContain("hex color");
    }

    // ──────────────────────────────────────────────
    // Rename — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Rename_ValidName_UpdatesNameAndUpdatedAt()
    {
        // Arrange
        var tag = ProjectTag.Create("OldName", null, Guid.NewGuid());
        var updatedAtBefore = tag.UpdatedAt;

        // Act
        tag.Rename("  NewName  ");

        // Assert
        tag.ShouldSatisfyAllConditions(
            () => tag.Name.ShouldBe("NewName"),   // trimmed
            () => tag.UpdatedAt.ShouldBeGreaterThanOrEqualTo(updatedAtBefore)
        );
    }

    [Fact]
    public void Rename_ValidName_DoesNotChangeCreatedAt()
    {
        // Arrange
        var tag = ProjectTag.Create("OldName", null, Guid.NewGuid());
        var createdAt = tag.CreatedAt;

        // Act
        tag.Rename("NewName");

        // Assert
        tag.CreatedAt.ShouldBe(createdAt);
    }

    // ──────────────────────────────────────────────
    // Rename — Validation
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Rename_BlankName_ThrowsArgumentException(string newName)
    {
        // Arrange
        var tag = ProjectTag.Create("ValidName", null, Guid.NewGuid());

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => tag.Rename(newName));
    }

    [Fact]
    public void Rename_NullName_ThrowsArgumentException()
    {
        // Arrange
        var tag = ProjectTag.Create("ValidName", null, Guid.NewGuid());

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => tag.Rename(null!));
    }

    [Fact]
    public void Rename_NameExceeds50Chars_ThrowsArgumentException()
    {
        // Arrange
        var tag = ProjectTag.Create("ValidName", null, Guid.NewGuid());
        var longName = new string('x', 51);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => tag.Rename(longName));

        exception.Message.ShouldContain("50");
    }

    // ──────────────────────────────────────────────
    // UpdateColor — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void UpdateColor_ValidHexColor_UpdatesColor()
    {
        // Arrange
        var tag = ProjectTag.Create("Tag", "#FF0000", Guid.NewGuid());
        var updatedAtBefore = tag.UpdatedAt;

        // Act
        tag.UpdateColor("#00FF00");

        // Assert
        tag.ShouldSatisfyAllConditions(
            () => tag.Color.ShouldBe("#00FF00"),
            () => tag.UpdatedAt.ShouldBeGreaterThanOrEqualTo(updatedAtBefore)
        );
    }

    [Fact]
    public void UpdateColor_NullColor_ClearsColor()
    {
        // Arrange
        var tag = ProjectTag.Create("Tag", "#FF0000", Guid.NewGuid());

        // Act
        tag.UpdateColor(null);

        // Assert
        tag.Color.ShouldBeNull();
    }

    [Fact]
    public void UpdateColor_NullColor_UpdatesUpdatedAt()
    {
        // Arrange
        var tag = ProjectTag.Create("Tag", "#FF0000", Guid.NewGuid());
        var updatedAtBefore = tag.UpdatedAt;

        // Act
        tag.UpdateColor(null);

        // Assert
        tag.UpdatedAt.ShouldBeGreaterThanOrEqualTo(updatedAtBefore);
    }

    // ──────────────────────────────────────────────
    // UpdateColor — Validation
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData("red")]
    [InlineData("#GGG")]
    [InlineData("#12345")]
    [InlineData("FF5733")]
    public void UpdateColor_InvalidHexColor_ThrowsArgumentException(string color)
    {
        // Arrange
        var tag = ProjectTag.Create("Tag", null, Guid.NewGuid());

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => tag.UpdateColor(color));

        exception.Message.ShouldContain("hex color");
    }
}
