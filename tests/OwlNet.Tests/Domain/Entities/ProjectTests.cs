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
    private const string DefaultPath = @"C:\Projects\TestProject";

    // ──────────────────────────────────────────────
    // Create — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_ValidNameAndDescription_ReturnsInstanceWithCorrectProperties()
    {
        // Arrange
        var name = "OwlNet Project";
        var path = @"C:\Projects\OwlNet";
        var description = "A sample project for testing.";
        var before = DateTimeOffset.UtcNow;

        // Act
        var project = Project.Create(name, path, description);

        // Assert
        var after = DateTimeOffset.UtcNow;

        project.ShouldSatisfyAllConditions(
            () => project.Id.ShouldNotBe(Guid.Empty),
            () => project.Name.ShouldBe(name),
            () => project.Path.ShouldBe(path),
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
        var project = Project.Create("Valid Name", DefaultPath, description);

        // Assert
        project.Description.ShouldBe(string.Empty);
    }

    [Fact]
    public void Create_ValidNameOnly_SetsDescriptionToEmpty()
    {
        // Arrange & Act
        var project = Project.Create("Another Project", DefaultPath, null);

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
            () => Project.Create(name, DefaultPath, "Some description"));

        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    public void Create_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var name = string.Empty;

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => Project.Create(name, DefaultPath, "Some description"));

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
            () => Project.Create(name, DefaultPath, "Some description"));

        exception.ParamName.ShouldBe("name");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    public void Create_NameTooShort_ThrowsArgumentException(string name)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => Project.Create(name, DefaultPath, "Some description"));

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
            () => Project.Create(name, DefaultPath, "Some description"));

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
        var project = Project.Create(name, DefaultPath, null);

        // Assert
        project.Name.ShouldBe(name);
    }

    [Fact]
    public void Create_NameExactlyHundredChars_Succeeds()
    {
        // Arrange
        var name = new string('x', 100);

        // Act
        var project = Project.Create(name, DefaultPath, null);

        // Assert
        project.Name.ShouldBe(name);
    }

    // ──────────────────────────────────────────────
    // Create — Path Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_NullPath_ThrowsArgumentException()
    {
        // Arrange
        string path = null!;

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => Project.Create("Valid Name", path, "Some description"));

        exception.ParamName.ShouldBe("path");
    }

    [Fact]
    public void Create_EmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var path = string.Empty;

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => Project.Create("Valid Name", path, "Some description"));

        exception.ParamName.ShouldBe("path");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Create_WhitespacePath_ThrowsArgumentException(string path)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => Project.Create("Valid Name", path, "Some description"));

        exception.ShouldSatisfyAllConditions(
            () => exception.ParamName.ShouldBe("path"),
            () => exception.Message.ShouldContain("must not be null or whitespace")
        );
    }

    [Fact]
    public void Create_PathTooLong_ThrowsArgumentException()
    {
        // Arrange
        var path = new string('p', 501);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => Project.Create("Valid Name", path, "Some description"));

        exception.ShouldSatisfyAllConditions(
            () => exception.ParamName.ShouldBe("path"),
            () => exception.Message.ShouldContain("500")
        );
    }

    [Fact]
    public void Create_PathExactly500Chars_Succeeds()
    {
        // Arrange
        var path = new string('p', 500);

        // Act
        var project = Project.Create("Valid Name", path, null);

        // Assert
        project.Path.ShouldBe(path);
    }

    [Fact]
    public void Create_PathWithLeadingAndTrailingSpaces_IsTrimmed()
    {
        // Arrange
        var path = @"  C:\Projects\Test  ";

        // Act
        var project = Project.Create("Valid Name", path, null);

        // Assert
        project.Path.ShouldBe(@"C:\Projects\Test");
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
            () => Project.Create("Valid Name", DefaultPath, description));

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
        var project = Project.Create("Valid Name", DefaultPath, description);

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
        var project = Project.Create("Original Name", DefaultPath, "Original description");
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
        var project = Project.Create("Original Name", DefaultPath, "Has a description");

        // Act
        project.Update("Updated Name", null);

        // Assert
        project.Description.ShouldBe(string.Empty);
    }

    // ──────────────────────────────────────────────
    // Update — Does NOT Change Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Update_DoesNotChangePath()
    {
        // Arrange
        var originalPath = @"C:\Projects\Original";
        var project = Project.Create("Original Name", originalPath, "Original description");

        // Act
        project.Update("Updated Name", "Updated description");

        // Assert
        project.Path.ShouldBe(originalPath);
    }

    // ──────────────────────────────────────────────
    // Update — Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Update_NullName_ThrowsArgumentException()
    {
        // Arrange
        var project = Project.Create("Valid Name", DefaultPath, null);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => project.Update(null!, "Some description"));

        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    public void Update_NameTooShort_ThrowsArgumentException()
    {
        // Arrange
        var project = Project.Create("Valid Name", DefaultPath, null);

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
        var project = Project.Create("Valid Name", DefaultPath, null);
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
        var project = Project.Create("Valid Name", DefaultPath, null);
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
        var project = Project.Create("Active Project", DefaultPath, "Will be archived");
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
        var project = Project.Create("Active Project", DefaultPath, null);
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
        var project = Project.Create("Archived Project", DefaultPath, "Will be restored");
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
        var project = Project.Create("Active Project", DefaultPath, null);

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(
            () => project.Restore());

        exception.Message.ShouldContain("not archived");
    }

    // ──────────────────────────────────────────────
    // ToggleFavorite
    // ──────────────────────────────────────────────

    [Fact]
    public void ToggleFavorite_WhenNotFavorited_SetsIsFavoritedToTrue()
    {
        // Arrange
        var project = Project.Create("Favorite Test", DefaultPath, "Toggle favorite");
        var beforeToggle = project.UpdatedAt;
        project.IsFavorited.ShouldBeFalse();

        // Act
        project.ToggleFavorite();

        // Assert
        project.ShouldSatisfyAllConditions(
            () => project.IsFavorited.ShouldBeTrue(),
            () => project.UpdatedAt.ShouldBeGreaterThanOrEqualTo(beforeToggle)
        );
    }

    [Fact]
    public void ToggleFavorite_WhenFavorited_SetsIsFavoritedToFalse()
    {
        // Arrange
        var project = Project.Create("Favorite Test", DefaultPath, "Toggle favorite twice");
        project.ToggleFavorite(); // now true
        var afterFirstToggle = project.UpdatedAt;

        // Act
        project.ToggleFavorite(); // now false

        // Assert
        project.ShouldSatisfyAllConditions(
            () => project.IsFavorited.ShouldBeFalse(),
            () => project.UpdatedAt.ShouldBeGreaterThanOrEqualTo(afterFirstToggle)
        );
    }

    [Fact]
    public void ToggleFavorite_WhenArchived_StillToggles()
    {
        // Arrange
        var project = Project.Create("Archived Favorite", DefaultPath, "Can still toggle");
        project.Archive();

        // Act
        project.ToggleFavorite();

        // Assert
        project.ShouldSatisfyAllConditions(
            () => project.IsFavorited.ShouldBeTrue(),
            () => project.IsArchived.ShouldBeTrue()
        );
    }

    [Fact]
    public void ToggleFavorite_UpdatesUpdatedAtTimestamp()
    {
        // Arrange
        var project = Project.Create("Timestamp Test", DefaultPath, "Verify timestamp updates");
        var createdAt = project.CreatedAt;

        // Act
        project.ToggleFavorite();

        // Assert
        project.ShouldSatisfyAllConditions(
            () => project.UpdatedAt.ShouldBeGreaterThanOrEqualTo(createdAt),
            () => project.CreatedAt.ShouldBe(createdAt)
        );
    }
}
