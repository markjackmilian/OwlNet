using System.Reflection;
using OwlNet.Application.Common.Constants;
using Shouldly;

namespace OwlNet.Tests.Application.Common.Constants;

/// <summary>
/// Unit tests for <see cref="OpenCodeEventTypes"/> constants.
/// Verifies that each event type has the expected string value, all values are unique,
/// and all values follow the <c>category.action</c> naming convention.
/// </summary>
public sealed class OpenCodeEventTypesTests
{
    // ──────────────────────────────────────────────
    // Individual constant values
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData(nameof(OpenCodeEventTypes.ServerConnected), "server.connected")]
    [InlineData(nameof(OpenCodeEventTypes.SessionUpdated), "session.updated")]
    [InlineData(nameof(OpenCodeEventTypes.SessionDeleted), "session.deleted")]
    [InlineData(nameof(OpenCodeEventTypes.MessageCreated), "message.created")]
    [InlineData(nameof(OpenCodeEventTypes.MessageUpdated), "message.updated")]
    [InlineData(nameof(OpenCodeEventTypes.MessageCompleted), "message.completed")]
    [InlineData(nameof(OpenCodeEventTypes.ConnectionLost), "connection.lost")]
    [InlineData(nameof(OpenCodeEventTypes.ConnectionRestored), "connection.restored")]
    public void Constant_HasExpectedValue(string fieldName, string expectedValue)
    {
        // Arrange
        var field = typeof(OpenCodeEventTypes).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);

        // Act
        var actualValue = field?.GetRawConstantValue() as string;

        // Assert
        actualValue.ShouldBe(expectedValue);
    }

    // ──────────────────────────────────────────────
    // Uniqueness
    // ──────────────────────────────────────────────

    [Fact]
    public void AllConstants_AreUnique()
    {
        // Arrange
        var values = GetAllConstantValues();

        // Act
        var distinctCount = values.Distinct().Count();

        // Assert
        distinctCount.ShouldBe(values.Count, "Duplicate event type values detected");
    }

    // ──────────────────────────────────────────────
    // Naming convention: category.action
    // ──────────────────────────────────────────────

    [Fact]
    public void AllConstants_FollowCategoryDotActionPattern()
    {
        // Arrange
        var values = GetAllConstantValues();

        // Act & Assert
        values.ShouldAllBe(
            value => value.Count(c => c == '.') == 1,
            "Every event type should contain exactly one dot (category.action pattern)");
    }

    [Fact]
    public void AllConstants_HaveNonEmptyCategoryAndAction()
    {
        // Arrange
        var values = GetAllConstantValues();

        // Act & Assert — each segment around the dot must be non-empty
        values.ShouldAllBe(
            value => value.Split('.').Length == 2
                && value.Split('.')[0].Length > 0
                && value.Split('.')[1].Length > 0,
            "Every event type should have non-empty category and action segments");
    }

    // ──────────────────────────────────────────────
    // Count guard — catches accidental additions/removals
    // ──────────────────────────────────────────────

    [Fact]
    public void AllConstants_CountIsEight()
    {
        // Arrange & Act
        var values = GetAllConstantValues();

        // Assert
        values.Count.ShouldBe(8);
    }

    // ──────────────────────────────────────────────
    // Helper
    // ──────────────────────────────────────────────

    private static List<string> GetAllConstantValues()
    {
        return typeof(OpenCodeEventTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();
    }
}
