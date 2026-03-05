namespace OwlNet.Domain.Entities;

/// <summary>
/// Represents a global application setting stored as a key-value pair.
/// Settings are application-wide (not per-user).
/// </summary>
public sealed class AppSetting
{
    /// <summary>
    /// Gets the unique identifier for this setting.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Gets the setting key. Must be unique and non-empty.
    /// Example: <c>"OpenCode:HealthCheckUrl"</c>.
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the setting value. May be an empty string but never <see langword="null"/>.
    /// Example: <c>"https://localhost:5001/health"</c>.
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp indicating when this setting was first created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this setting was last modified.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Required by EF Core for materialization. Do not call directly.
    /// </summary>
    private AppSetting() { }

    /// <summary>
    /// Creates a new <see cref="AppSetting"/> with the specified key and value.
    /// </summary>
    /// <param name="key">
    /// The setting key. Must not be <see langword="null"/> or whitespace.
    /// </param>
    /// <param name="value">
    /// The setting value. A <see langword="null"/> value is coerced to <see cref="string.Empty"/>.
    /// </param>
    /// <returns>A new <see cref="AppSetting"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    public static AppSetting Create(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Setting key must not be null or whitespace.", nameof(key));
        }

        var now = DateTimeOffset.UtcNow;

        return new AppSetting
        {
            Key = key,
            Value = value ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Updates the setting value and refreshes the <see cref="UpdatedAt"/> timestamp.
    /// </summary>
    /// <param name="value">
    /// The new value. A <see langword="null"/> value is coerced to <see cref="string.Empty"/>.
    /// </param>
    public void UpdateValue(string value)
    {
        Value = value ?? string.Empty;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
