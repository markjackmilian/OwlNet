using System.Text.Json;
using OwlNet.Application.Common.Models;
using Shouldly;

namespace OwlNet.Tests.Application.Common.Models;

/// <summary>
/// Unit tests for <see cref="OpenCodeEventDto"/> record.
/// Covers construction, property assignment, record equality/inequality,
/// and with-expression immutability semantics.
/// </summary>
public sealed class OpenCodeEventDtoTests
{
    // ──────────────────────────────────────────────
    // Construction — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_AllPropertiesSet_CreatesRecordWithCorrectValues()
    {
        // Arrange
        var type = "session.updated";
        var timestamp = DateTimeOffset.UtcNow;
        var data = JsonDocument.Parse("""{"sessionId":"abc-123"}""").RootElement;

        // Act
        var dto = new OpenCodeEventDto(type, timestamp, data);

        // Assert
        dto.ShouldSatisfyAllConditions(
            () => dto.Type.ShouldBe(type),
            () => dto.Timestamp.ShouldBe(timestamp),
            () => dto.Data.ShouldNotBeNull(),
            () => dto.Data!.Value.GetProperty("sessionId").GetString().ShouldBe("abc-123")
        );
    }

    [Fact]
    public void Constructor_NullData_CreatesRecordWithNullData()
    {
        // Arrange
        var type = "message.created";
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var dto = new OpenCodeEventDto(type, timestamp, null);

        // Assert
        dto.ShouldSatisfyAllConditions(
            () => dto.Type.ShouldBe(type),
            () => dto.Timestamp.ShouldBe(timestamp),
            () => dto.Data.ShouldBeNull()
        );
    }

    // ──────────────────────────────────────────────
    // Record Equality
    // ──────────────────────────────────────────────

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var type = "server.connected";
        var timestamp = new DateTimeOffset(2026, 3, 9, 12, 0, 0, TimeSpan.Zero);

        var dto1 = new OpenCodeEventDto(type, timestamp, null);
        var dto2 = new OpenCodeEventDto(type, timestamp, null);

        // Act & Assert
        dto1.ShouldSatisfyAllConditions(
            () => dto1.ShouldBe(dto2),
            () => (dto1 == dto2).ShouldBeTrue(),
            () => dto1.GetHashCode().ShouldBe(dto2.GetHashCode())
        );
    }

    // ──────────────────────────────────────────────
    // Record Inequality
    // ──────────────────────────────────────────────

    [Fact]
    public void Inequality_DifferentType_AreNotEqual()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 3, 9, 12, 0, 0, TimeSpan.Zero);
        var dto1 = new OpenCodeEventDto("message.created", timestamp, null);
        var dto2 = new OpenCodeEventDto("message.updated", timestamp, null);

        // Act & Assert
        dto1.ShouldNotBe(dto2);
        (dto1 != dto2).ShouldBeTrue();
    }

    [Fact]
    public void Inequality_DifferentTimestamp_AreNotEqual()
    {
        // Arrange
        var type = "session.updated";
        var dto1 = new OpenCodeEventDto(type, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), null);
        var dto2 = new OpenCodeEventDto(type, new DateTimeOffset(2026, 6, 15, 12, 30, 0, TimeSpan.Zero), null);

        // Act & Assert
        dto1.ShouldNotBe(dto2);
        (dto1 != dto2).ShouldBeTrue();
    }

    [Fact]
    public void Inequality_NullVsNonNullData_AreNotEqual()
    {
        // Arrange
        var type = "message.created";
        var timestamp = new DateTimeOffset(2026, 3, 9, 12, 0, 0, TimeSpan.Zero);
        var data = JsonDocument.Parse("""{"key":"value"}""").RootElement;

        var dto1 = new OpenCodeEventDto(type, timestamp, null);
        var dto2 = new OpenCodeEventDto(type, timestamp, data);

        // Act & Assert
        dto1.ShouldNotBe(dto2);
        (dto1 != dto2).ShouldBeTrue();
    }

    // ──────────────────────────────────────────────
    // With-expression (immutable copy)
    // ──────────────────────────────────────────────

    [Fact]
    public void WithExpression_ModifiedType_CreatesNewRecordWithUpdatedProperty()
    {
        // Arrange
        var original = new OpenCodeEventDto(
            "message.created",
            new DateTimeOffset(2026, 3, 9, 12, 0, 0, TimeSpan.Zero),
            null);

        // Act
        var modified = original with { Type = "message.completed" };

        // Assert
        modified.ShouldSatisfyAllConditions(
            () => modified.Type.ShouldBe("message.completed"),
            () => modified.Timestamp.ShouldBe(original.Timestamp),
            () => modified.Data.ShouldBe(original.Data),
            () => modified.ShouldNotBe(original)
        );
    }

    [Fact]
    public void WithExpression_ModifiedTimestamp_CreatesNewRecordWithUpdatedProperty()
    {
        // Arrange
        var original = new OpenCodeEventDto(
            "session.updated",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            null);
        var newTimestamp = new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);

        // Act
        var modified = original with { Timestamp = newTimestamp };

        // Assert
        modified.ShouldSatisfyAllConditions(
            () => modified.Timestamp.ShouldBe(newTimestamp),
            () => modified.Type.ShouldBe(original.Type),
            () => modified.Data.ShouldBe(original.Data),
            () => modified.ShouldNotBe(original)
        );
    }

    [Fact]
    public void WithExpression_ModifiedData_CreatesNewRecordWithUpdatedProperty()
    {
        // Arrange
        var original = new OpenCodeEventDto(
            "message.updated",
            new DateTimeOffset(2026, 3, 9, 12, 0, 0, TimeSpan.Zero),
            null);
        var newData = JsonDocument.Parse("""{"content":"hello"}""").RootElement;

        // Act
        var modified = original with { Data = newData };

        // Assert
        modified.ShouldSatisfyAllConditions(
            () => modified.Data.ShouldNotBeNull(),
            () => modified.Data!.Value.GetProperty("content").GetString().ShouldBe("hello"),
            () => modified.Type.ShouldBe(original.Type),
            () => modified.Timestamp.ShouldBe(original.Timestamp),
            () => modified.ShouldNotBe(original)
        );
    }
}
