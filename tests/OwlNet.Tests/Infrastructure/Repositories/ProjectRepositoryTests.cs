using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OwlNet.Domain.Entities;
using OwlNet.Infrastructure.Persistence;
using OwlNet.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace OwlNet.Tests.Infrastructure.Repositories;

/// <summary>
/// Unit tests for <see cref="ProjectRepository"/> that exercise the real EF Core implementation
/// using an in-memory database. These tests verify that uniqueness checks for name and path
/// correctly exclude archived projects, ensuring that archived data never causes false conflicts.
/// </summary>
public sealed class ProjectRepositoryTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a fresh <see cref="ApplicationDbContext"/> backed by an isolated in-memory
    /// database. Each call uses a unique database name so tests never share state.
    /// </summary>
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Seeds a <see cref="Project"/> into the given context and saves it.
    /// Returns the persisted project so callers can inspect or mutate it further.
    /// </summary>
    private static async Task<Project> SeedProjectAsync(
        ApplicationDbContext dbContext,
        string name,
        string path,
        bool archived = false)
    {
        var project = Project.Create(name, path, description: null);

        if (archived)
        {
            project.Archive();
        }

        await dbContext.Projects.AddAsync(project);
        await dbContext.SaveChangesAsync();

        return project;
    }

    // -------------------------------------------------------------------------
    // ExistsWithNameAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExistsWithNameAsync_ProjectWithNameIsArchived_ReturnsFalse()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        await SeedProjectAsync(dbContext, name: "Test Project", path: @"C:\Projects\Test", archived: true);

        var sut = new ProjectRepository(dbContext, NullLogger<ProjectRepository>.Instance);

        // Act
        var result = await sut.ExistsWithNameAsync("Test Project", cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsWithNameAsync_ProjectWithNameIsActive_ReturnsTrue()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        await SeedProjectAsync(dbContext, name: "Test Project", path: @"C:\Projects\Test", archived: false);

        var sut = new ProjectRepository(dbContext, NullLogger<ProjectRepository>.Instance);

        // Act
        var result = await sut.ExistsWithNameAsync("Test Project", cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsWithNameAsync_TwoArchivedProjectsWithSameName_ReturnsFalse()
    {
        // Arrange — two archived projects sharing the same name; no active project exists
        await using var dbContext = CreateDbContext();
        await SeedProjectAsync(dbContext, name: "Shared Name", path: @"C:\Projects\First", archived: true);
        await SeedProjectAsync(dbContext, name: "Shared Name", path: @"C:\Projects\Second", archived: true);

        var sut = new ProjectRepository(dbContext, NullLogger<ProjectRepository>.Instance);

        // Act
        var result = await sut.ExistsWithNameAsync("Shared Name", cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // ExistsWithPathAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExistsWithPathAsync_ProjectWithPathIsArchived_ReturnsFalse()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        await SeedProjectAsync(dbContext, name: "Test Project", path: @"C:\Projects\Test", archived: true);

        var sut = new ProjectRepository(dbContext, NullLogger<ProjectRepository>.Instance);

        // Act
        var result = await sut.ExistsWithPathAsync(@"C:\Projects\Test", cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsWithPathAsync_ProjectWithPathIsActive_ReturnsTrue()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        await SeedProjectAsync(dbContext, name: "Test Project", path: @"C:\Projects\Test", archived: false);

        var sut = new ProjectRepository(dbContext, NullLogger<ProjectRepository>.Instance);

        // Act
        var result = await sut.ExistsWithPathAsync(@"C:\Projects\Test", cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsWithPathAsync_TwoArchivedProjectsWithSamePath_ReturnsFalse()
    {
        // Arrange — two archived projects sharing the same path; no active project exists
        const string sharedPath = @"C:\Projects\Shared";

        await using var dbContext = CreateDbContext();
        await SeedProjectAsync(dbContext, name: "First Project", path: sharedPath, archived: true);
        await SeedProjectAsync(dbContext, name: "Second Project", path: sharedPath, archived: true);

        var sut = new ProjectRepository(dbContext, NullLogger<ProjectRepository>.Instance);

        // Act
        var result = await sut.ExistsWithPathAsync(sharedPath, cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }
}
