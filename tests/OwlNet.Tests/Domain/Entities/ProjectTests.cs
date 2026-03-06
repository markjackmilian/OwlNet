using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Domain.Entities;

/// <summary>
/// Unit tests for the <see cref="Project"/> domain entity.
/// Covers the <see cref="Project.Create"/> factory method (validation, null coercion, boundary values),
/// the <see cref="Project.Update"/> mutation method (property assignment, timestamp refresh, validation),
/// the <see cref="Project.Archive"/> method (state transition, idempotency guard), and
/// the <see cref="Project.Restore"/> method (state transition, idempotency guard).
/// </summary>
public sealed class ProjectTests
{
    // ──────────────────────────────────────────────
    // Create — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_ValidNameAndDescription_ReturnsInstanceWithCorrectProperties()
    {
        // Arrange
        var name = "OwlNet Project";
        var description = "A sample project for testing.";
        var before = DateTimeOffset.UtcNow;

        // Act
        var project = Project.Create(name, description);

        // Assert
        var after = DateTimeOffset.UtcNow;

        project.ShouldSatisfyAllConditions(
            () => project.Id.ShouldNotBe(Guid.Empty),
            () => project.Name.ShouldBe(name),
            () => project.Description.ShouldBe(description),
            () => project.IsArchived.ShouldBeFalse(),
            () => project.CreatedAt.ShouldBe(project.UpdatedAt),
            () => project.CreatedAt.ShouldBeGreaterThanOrEqualTo(before),
            () => project.CreatedAt.ShouldBeLessThanOrEqualTo(after)
        );
    }

    [Fact]
    public void Create_NullDescription_CoercesToEmptyString()
    {
        // Arrange
        string? description = null;

        // Act
        var project = Project.Create("Valid Name", description);

        // Assert
        project.Description.ShouldBe(string.Empty);
    }

    [Fact]
    public void Create_ValidNameOnly_SetsDescriptionToEmpty()
    {
        // Arrange & Act
        var project = Project.Create("Another Project", null);

        // Assert
        project.ShouldSatisfyAllConditions(
            () => project.Name.ShouldBe("Another Project"),
            () => project.Description.ShouldBe(string.Empty)
        );
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
        var exception = Should.Throw<ArgumentException>(
            () => Project.Create(name, "Some description"));

        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    public void Create_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var name = string.Empty;

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => Project.Create(name, "Some description"));

        exception.ParamName.ShouldBe("name");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Create_WhitespaceName_ThrowsArgumentException(string name)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => Project.Create(name, "Some description"));

        exception.ParamName.ShouldBe("name");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    public void Create_NameTooShort_ThrowsArgumentException(string name)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => Project.Create(name, "Some description"));

        exception.ShouldSatisfyAllConditions(
            () => exception.ParamName.ShouldBe("name"),
            () => exception.Message.ShouldContain("between 3 and 100")
        );
    }

    [Fact]
    public void Create_NameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var name = new string('x', 101);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => Project.Create(name, "Some description"));

        exception.ShouldSatisfyAllConditions(
            () => exception.ParamName.ShouldBe("name"),
            () => exception.Message.ShouldContain("between 3 and 100")
        );
    }

    [Fact]
    public void Create_NameExactlyThreeChars_Succeeds()
    {
        // Arrange
        var name = "Abc";

        // Act
        var project = Project.Create(name, null);

        // Assert
        project.Name.ShouldBe(name);
    }

    [Fact]
    public void Create_NameExactlyHundredChars_Succeeds()
    {
        // Arrange
        var name = new string('x', 100);

        // Act
        var project = Project.Create(name, null);

        // Assert
        project.Name.ShouldBe(name);
    }

    // ──────────────────────────────────────────────
    // Create — Description Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_DescriptionTooLong_ThrowsArgumentException()
    {
        // Arrange
        var description = new string('d', 501);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => Project.Create("Valid Name", description));

        exception.ShouldSatisfyAllConditions(
            () => exception.ParamName.ShouldBe("description"),
            () => exception.Message.ShouldContain("500")
        );
    }

    [Fact]
    public void Create_DescriptionExactly500Chars_Succeeds()
    {
        // Arrange
        var description = new string('d', 500);

        // Act
        var project = Project.Create("Valid Name", description);

        // Assert
        project.Description.ShouldBe(description);
    }

    // ──────────────────────────────────────────────
    // Update — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Update_ValidNameAndDescription_UpdatesPropertiesAndTimestamp()
    {
        // Arrange
        var project = Project.Create("Original Name", "Original description");
        var createdAt = project.CreatedAt;
        var newName = "Updated Name";
        var newDescription = "Updated description";

        // Act
        project.Update(newName, newDescription);

        // Assert
        project.ShouldSatisfyAllConditions(
            () => project.Name.ShouldBe(newName),
            () => project.Description.ShouldBe(newDescription),
            () => project.CreatedAt.ShouldBe(createdAt),
            () => project.UpdatedAt.ShouldBeGreaterThanOrEqualTo(createdAt)
        );
    }

    [Fact]
    public void Update_NullDescription_CoercesToEmptyString()
    {
        // Arrange
        var project = Project.Create("Original Name", "Has a description");

        // Act
        project.Update("Updated Name", null);

        // Assert
        project.Description.ShouldBe(string.Empty);
    }

    // ──────────────────────────────────────────────
    // Update — Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Update_NullName_ThrowsArgumentException()
    {
        // Arrange
        var project = Project.Create("Valid Name", null);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => project.Update(null!, "Some description"));

        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    public void Update_NameTooShort_ThrowsArgumentException()
    {
        // Arrange
        var project = Project.Create("Valid Name", null);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => project.Update("ab", "Some description"));

        exception.ShouldSatisfyAllConditions(
            () => exception.ParamName.ShouldBe("name"),
            () => exception.Message.ShouldContain("between 3 and 100")
        );
    }

    [Fact]
    public void Update_NameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var project = Project.Create("Valid Name", null);
        var longName = new string('x', 101);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => project.Update(longName, "Some description"));

        exception.ShouldSatisfyAllConditions(
            () => exception.ParamName.ShouldBe("name"),
            () => exception.Message.ShouldContain("between 3 and 100")
        );
    }

    [Fact]
    public void Update_DescriptionTooLong_ThrowsArgumentException()
    {
        // Arrange
        var project = Project.Create("Valid Name", null);
        var longDescription = new string('d', 501);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => project.Update("Valid Name", longDescription));

        exception.ShouldSatisfyAllConditions(
            () => exception.ParamName.ShouldBe("description"),
            () => exception.Message.ShouldContain("500")
        );
    }

    // ──────────────────────────────────────────────
    // Archive
    // ──────────────────────────────────────────────

    [Fact]
    public void Archive_ActiveProject_SetsIsArchivedAndUpdatesTimestamp()
    {
        // Arrange
        var project = Project.Create("Active Project", "Will be archived");
        var createdAt = project.CreatedAt;

        // Act
        project.Archive();

        // Assert
        project.ShouldSatisfyAllConditions(
            () => project.IsArchived.ShouldBeTrue(),
            () => project.CreatedAt.ShouldBe(createdAt),
            () => project.UpdatedAt.ShouldBeGreaterThanOrEqualTo(createdAt)
        );
    }

    [Fact]
    public void Archive_AlreadyArchived_ThrowsInvalidOperationException()
    {
        // Arrange
        var project = Project.Create("Active Project", null);
        project.Archive();

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(
            () => project.Archive());

        exception.Message.ShouldContain("already archived");
    }

    // ──────────────────────────────────────────────
    // Restore
    // ──────────────────────────────────────────────

    [Fact]
    public void Restore_ArchivedProject_ClearsIsArchivedAndUpdatesTimestamp()
    {
        // Arrange
        var project = Project.Create("Archived Project", "Will be restored");
        project.Archive();
        var archivedAt = project.UpdatedAt;

        // Act
        project.Restore();

        // Assert
        project.ShouldSatisfyAllConditions(
            () => project.IsArchived.ShouldBeFalse(),
            () => project.UpdatedAt.ShouldBeGreaterThanOrEqualTo(archivedAt)
        );
    }

    [Fact]
    public void Restore_ActiveProject_ThrowsInvalidOperationException()
    {
        // Arrange
        var project = Project.Create("Active Project", null);

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(
            () => project.Restore());

        exception.Message.ShouldContain("not archived");
    }
}
