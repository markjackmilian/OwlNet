using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OwlNet.Domain.Entities;
using OwlNet.Infrastructure.Persistence;
using OwlNet.Infrastructure.Services;
using Shouldly;

namespace OwlNet.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for <see cref="AppSettingService"/> using EF Core InMemory provider.
/// Each test uses a unique in-memory database to ensure complete isolation.
/// </summary>
public sealed class AppSettingServiceTests
{
    /// <summary>
    /// Creates a fresh <see cref="ApplicationDbContext"/> backed by a unique in-memory database.
    /// </summary>
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Seeds an <see cref="AppSetting"/> into the given context and saves it.
    /// </summary>
    private static async Task SeedSettingAsync(
        ApplicationDbContext context,
        string key,
        string value)
    {
        var setting = AppSetting.Create(key, value);
        await context.AppSettings.AddAsync(setting);
        await context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    // GetByKeyAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetByKeyAsync_ExistingKey_ReturnsSuccessWithValue()
    {
        // Arrange
        using var context = CreateDbContext();
        var expectedKey = "App:Theme";
        var expectedValue = "Dark";
        await SeedSettingAsync(context, expectedKey, expectedValue);

        var sut = new AppSettingService(context, NullLogger<AppSettingService>.Instance);

        // Act
        var result = await sut.GetByKeyAsync(expectedKey, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldBe(expectedValue)
        );
    }

    [Fact]
    public async Task GetByKeyAsync_NonExistentKey_ReturnsFailure()
    {
        // Arrange
        using var context = CreateDbContext();
        var nonExistentKey = "App:NonExistent";

        var sut = new AppSettingService(context, NullLogger<AppSettingService>.Instance);

        // Act
        var result = await sut.GetByKeyAsync(nonExistentKey, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain(nonExistentKey)
        );
    }

    // ──────────────────────────────────────────────
    // GetAllAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WithSettings_ReturnsAllOrderedByKey()
    {
        // Arrange
        using var context = CreateDbContext();

        // Seed in deliberately non-alphabetical order
        await SeedSettingAsync(context, "Zebra:Setting", "z-value");
        await SeedSettingAsync(context, "Alpha:Setting", "a-value");
        await SeedSettingAsync(context, "Middle:Setting", "m-value");

        var sut = new AppSettingService(context, NullLogger<AppSettingService>.Instance);

        // Act
        var result = await sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var settings = result.Value;
        settings.ShouldSatisfyAllConditions(
            () => settings.Count.ShouldBe(3),
            () => settings[0].Key.ShouldBe("Alpha:Setting"),
            () => settings[0].Value.ShouldBe("a-value"),
            () => settings[1].Key.ShouldBe("Middle:Setting"),
            () => settings[1].Value.ShouldBe("m-value"),
            () => settings[2].Key.ShouldBe("Zebra:Setting"),
            () => settings[2].Value.ShouldBe("z-value")
        );
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateDbContext();
        var sut = new AppSettingService(context, NullLogger<AppSettingService>.Instance);

        // Act
        var result = await sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldBeEmpty()
        );
    }

    // ──────────────────────────────────────────────
    // SaveAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_NewKey_CreatesSettingSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var sut = new AppSettingService(context, NullLogger<AppSettingService>.Instance);

        var key = "App:NewSetting";
        var value = "new-value";

        // Act
        var result = await sut.SaveAsync(key, value, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var persisted = await context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        persisted.ShouldSatisfyAllConditions(
            () => persisted.ShouldNotBeNull(),
            () => persisted!.Key.ShouldBe(key),
            () => persisted!.Value.ShouldBe(value)
        );
    }

    [Fact]
    public async Task SaveAsync_ExistingKey_UpdatesValueSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var key = "App:Existing";
        var originalValue = "original";
        var updatedValue = "updated";

        await SeedSettingAsync(context, key, originalValue);

        var sut = new AppSettingService(context, NullLogger<AppSettingService>.Instance);

        // Act
        var result = await sut.SaveAsync(key, updatedValue, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var allWithKey = await context.AppSettings
            .Where(s => s.Key == key)
            .ToListAsync();

        allWithKey.ShouldSatisfyAllConditions(
            () => allWithKey.Count.ShouldBe(1),
            () => allWithKey[0].Value.ShouldBe(updatedValue)
        );
    }

    [Fact]
    public async Task SaveAsync_EmptyValue_SavesSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var sut = new AppSettingService(context, NullLogger<AppSettingService>.Instance);

        var key = "App:EmptyValue";
        var emptyValue = string.Empty;

        // Act
        var result = await sut.SaveAsync(key, emptyValue, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var persisted = await context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        persisted.ShouldSatisfyAllConditions(
            () => persisted.ShouldNotBeNull(),
            () => persisted!.Value.ShouldBe(string.Empty)
        );
    }
}
