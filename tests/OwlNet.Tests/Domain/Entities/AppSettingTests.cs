using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Domain.Entities;

/// <summary>
/// Unit tests for <see cref="AppSetting"/> domain entity.
/// Covers the <see cref="AppSetting.Create"/> factory method (validation, null coercion)
/// and the <see cref="AppSetting.UpdateValue"/> mutation method (value assignment, timestamp refresh).
/// </summary>
public sealed class AppSettingTests
{
    // ──────────────────────────────────────────────
    // Create — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_ValidKeyAndValue_ReturnsInstanceWithCorrectProperties()
    {
        // Arrange
        var key = "App:Theme";
        var value = "Dark";
        var before = DateTimeOffset.UtcNow;

        // Act
        var setting = AppSetting.Create(key, value);

        // Assert
        var after = DateTimeOffset.UtcNow;

        setting.ShouldSatisfyAllConditions(
            () => setting.Key.ShouldBe(key),
            () => setting.Value.ShouldBe(value),
            () => setting.CreatedAt.ShouldBe(setting.UpdatedAt),
            () => setting.CreatedAt.ShouldBeGreaterThanOrEqualTo(before),
            () => setting.CreatedAt.ShouldBeLessThanOrEqualTo(after)
        );
    }

    // ──────────────────────────────────────────────
    // Create — Key Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_NullKey_ThrowsArgumentException()
    {
        // Arrange
        string key = null!;

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => AppSetting.Create(key, "some-value"));

        exception.ParamName.ShouldBe("key");
    }

    [Fact]
    public void Create_EmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var key = string.Empty;

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => AppSetting.Create(key, "some-value"));

        exception.ParamName.ShouldBe("key");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Create_WhitespaceKey_ThrowsArgumentException(string key)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => AppSetting.Create(key, "some-value"));

        exception.ParamName.ShouldBe("key");
    }

    // ──────────────────────────────────────────────
    // Create — Null Value Coercion
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_NullValue_CoercesToEmptyString()
    {
        // Arrange
        string value = null!;

        // Act
        var setting = AppSetting.Create("App:ValidKey", value);

        // Assert
        setting.Value.ShouldBe(string.Empty);
    }

    // ──────────────────────────────────────────────
    // UpdateValue — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void UpdateValue_SetsValueAndUpdatesTimestamp()
    {
        // Arrange
        var setting = AppSetting.Create("App:Theme", "Light");
        var createdAt = setting.CreatedAt;
        var newValue = "Dark";

        // Act
        setting.UpdateValue(newValue);

        // Assert
        setting.ShouldSatisfyAllConditions(
            () => setting.Value.ShouldBe(newValue),
            () => setting.CreatedAt.ShouldBe(createdAt),
            () => setting.UpdatedAt.ShouldBeGreaterThanOrEqualTo(createdAt)
        );
    }

    // ──────────────────────────────────────────────
    // UpdateValue — Null Value Coercion
    // ──────────────────────────────────────────────

    [Fact]
    public void UpdateValue_NullValue_CoercesToEmptyString()
    {
        // Arrange
        var setting = AppSetting.Create("App:Theme", "Dark");

        // Act
        setting.UpdateValue(null!);

        // Assert
        setting.Value.ShouldBe(string.Empty);
    }
}
