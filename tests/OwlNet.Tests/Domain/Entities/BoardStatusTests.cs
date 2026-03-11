using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Domain.Entities;

/// <summary>
/// Unit tests for the <see cref="BoardStatus"/> domain entity.
/// Covers the <see cref="BoardStatus.Create"/> factory method (validation, boundary values, default parameters),
/// the <see cref="BoardStatus.CreateGlobalDefault"/> convenience factory (global default creation),
/// the <see cref="BoardStatus.CreateForProject"/> convenience factory (project-level creation),
/// the <see cref="BoardStatus.Rename"/> mutation method (name assignment, validation, timestamp refresh), and
/// the <see cref="BoardStatus.UpdateSortOrder"/> mutation method (sort order assignment, timestamp refresh).
/// </summary>
public sealed class BoardStatusTests
{
    // ──────────────────────────────────────────────
    // Create — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_ValidParameters_ReturnsInstanceWithCorrectProperties()
    {
        // Arrange
        var name = "Backlog";
        var sortOrder = 0;
        var projectId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        // Act
        var status = BoardStatus.Create(name, sortOrder, projectId);

        // Assert
        var after = DateTimeOffset.UtcNow;

        status.ShouldSatisfyAllConditions(
            () => status.Id.ShouldNotBe(Guid.Empty),
            () => status.Name.ShouldBe(name),
            () => status.SortOrder.ShouldBe(sortOrder),
            () => status.IsDefault.ShouldBeFalse(),
            () => status.ProjectId.ShouldBe(projectId),
            () => status.CreatedAt.ShouldBe(status.UpdatedAt),
            () => status.CreatedAt.ShouldBeGreaterThanOrEqualTo(before),
            () => status.CreatedAt.ShouldBeLessThanOrEqualTo(after)
        );
    }

    [Fact]
    public void Create_WithNullProjectId_SetsProjectIdToNull()
    {
        // Arrange & Act
        var status = BoardStatus.Create("Backlog", 0, projectId: null);

        // Assert
        status.ProjectId.ShouldBeNull();
    }

    [Fact]
    public void Create_WithProjectId_SetsProjectIdCorrectly()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var status = BoardStatus.Create("In Progress", 1, projectId);

        // Assert
        status.ProjectId.ShouldBe(projectId);
    }

    [Fact]
    public void Create_IsDefaultTrue_SetsIsDefaultToTrue()
    {
        // Arrange & Act
        var status = BoardStatus.Create("Done", 2, projectId: null, isDefault: true);

        // Assert
        status.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void Create_IsDefaultFalse_SetsIsDefaultToFalse()
    {
        // Arrange & Act — isDefault defaults to false when omitted
        var status = BoardStatus.Create("Review", 3, projectId: null);

        // Assert
        status.IsDefault.ShouldBeFalse();
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
            () => BoardStatus.Create(name, 0, projectId: null));

        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    public void Create_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var name = string.Empty;

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => BoardStatus.Create(name, 0, projectId: null));

        exception.ParamName.ShouldBe("name");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Create_WhitespaceName_ThrowsArgumentException(string name)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => BoardStatus.Create(name, 0, projectId: null));

        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    public void Create_NameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var name = new string('x', 101);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => BoardStatus.Create(name, 0, projectId: null));

        exception.ShouldSatisfyAllConditions(
            () => exception.ParamName.ShouldBe("name"),
            () => exception.Message.ShouldContain("100")
        );
    }

    [Fact]
    public void Create_NameExactly100Chars_Succeeds()
    {
        // Arrange
        var name = new string('x', 100);

        // Act
        var status = BoardStatus.Create(name, 0, projectId: null);

        // Assert
        status.Name.ShouldBe(name);
    }

    [Fact]
    public void Create_NameSingleChar_Succeeds()
    {
        // Arrange
        var name = "A";

        // Act
        var status = BoardStatus.Create(name, 0, projectId: null);

        // Assert
        status.Name.ShouldBe(name);
    }

    // ──────────────────────────────────────────────
    // CreateGlobalDefault
    // ──────────────────────────────────────────────

    [Fact]
    public void CreateGlobalDefault_ValidName_SetsProjectIdNullAndIsDefaultTrue()
    {
        // Arrange
        var name = "To Do";
        var sortOrder = 0;

        // Act
        var status = BoardStatus.CreateGlobalDefault(name, sortOrder);

        // Assert
        status.ShouldSatisfyAllConditions(
            () => status.Name.ShouldBe(name),
            () => status.SortOrder.ShouldBe(sortOrder),
            () => status.ProjectId.ShouldBeNull(),
            () => status.IsDefault.ShouldBeTrue(),
            () => status.Id.ShouldNotBe(Guid.Empty)
        );
    }

    // ──────────────────────────────────────────────
    // CreateForProject
    // ──────────────────────────────────────────────

    [Fact]
    public void CreateForProject_ValidParameters_SetsProjectIdAndIsDefault()
    {
        // Arrange
        var name = "In Review";
        var sortOrder = 2;
        var projectId = Guid.NewGuid();

        // Act
        var status = BoardStatus.CreateForProject(name, sortOrder, projectId, isDefault: true);

        // Assert
        status.ShouldSatisfyAllConditions(
            () => status.Name.ShouldBe(name),
            () => status.SortOrder.ShouldBe(sortOrder),
            () => status.ProjectId.ShouldBe(projectId),
            () => status.IsDefault.ShouldBeTrue(),
            () => status.Id.ShouldNotBe(Guid.Empty)
        );
    }

    [Fact]
    public void CreateForProject_IsDefaultOmitted_DefaultsToFalse()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var status = BoardStatus.CreateForProject("Custom Status", 5, projectId);

        // Assert
        status.IsDefault.ShouldBeFalse();
    }

    // ──────────────────────────────────────────────
    // Rename — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Rename_ValidNewName_UpdatesNameAndTimestamp()
    {
        // Arrange
        var status = BoardStatus.Create("Original", 0, projectId: null);
        var createdAt = status.CreatedAt;
        var newName = "Renamed Status";

        // Act
        status.Rename(newName);

        // Assert
        status.ShouldSatisfyAllConditions(
            () => status.Name.ShouldBe(newName),
            () => status.CreatedAt.ShouldBe(createdAt),
            () => status.UpdatedAt.ShouldBeGreaterThanOrEqualTo(createdAt)
        );
    }

    // ──────────────────────────────────────────────
    // Rename — Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Rename_NullName_ThrowsArgumentException()
    {
        // Arrange
        var status = BoardStatus.Create("Valid", 0, projectId: null);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => status.Rename(null!));

        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    public void Rename_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var status = BoardStatus.Create("Valid", 0, projectId: null);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => status.Rename(string.Empty));

        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    public void Rename_NameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var status = BoardStatus.Create("Valid", 0, projectId: null);
        var longName = new string('x', 101);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => status.Rename(longName));

        exception.ShouldSatisfyAllConditions(
            () => exception.ParamName.ShouldBe("name"),
            () => exception.Message.ShouldContain("100")
        );
    }

    // ──────────────────────────────────────────────
    // UpdateSortOrder
    // ──────────────────────────────────────────────

    [Fact]
    public void UpdateSortOrder_NewValue_UpdatesSortOrderAndTimestamp()
    {
        // Arrange
        var status = BoardStatus.Create("Backlog", 0, projectId: null);
        var createdAt = status.CreatedAt;
        var newSortOrder = 5;

        // Act
        status.UpdateSortOrder(newSortOrder);

        // Assert
        status.ShouldSatisfyAllConditions(
            () => status.SortOrder.ShouldBe(newSortOrder),
            () => status.CreatedAt.ShouldBe(createdAt),
            () => status.UpdatedAt.ShouldBeGreaterThanOrEqualTo(createdAt)
        );
    }
}
