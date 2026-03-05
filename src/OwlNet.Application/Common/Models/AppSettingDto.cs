namespace OwlNet.Application.Common.Models;

/// <summary>
/// A read-only projection of an <see cref="OwlNet.Domain.Entities.AppSetting"/>
/// containing the key, value, and last-updated timestamp.
/// </summary>
/// <param name="Key">The unique setting key.</param>
/// <param name="Value">The setting value.</param>
/// <param name="UpdatedAt">The UTC timestamp of the last modification.</param>
public sealed record AppSettingDto(string Key, string Value, DateTimeOffset UpdatedAt);
