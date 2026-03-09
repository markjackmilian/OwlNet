namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents a request to send a prompt to the OpenCode Server.
/// Supports optional model override and agent selection.
/// </summary>
/// <param name="Text">The prompt text content to send.</param>
/// <param name="ProviderID">
/// An optional provider ID for model override (e.g., <c>"anthropic"</c>).
/// When specified, <paramref name="ModelID"/> should also be specified for a valid model override.
/// </param>
/// <param name="ModelID">
/// An optional model ID for model override (e.g., <c>"claude-sonnet-4-20250514"</c>).
/// When specified, <paramref name="ProviderID"/> should also be specified for a valid model override.
/// </param>
/// <param name="Agent">An optional agent selection (e.g., <c>"build"</c>, <c>"plan"</c>).</param>
public sealed record SendPromptRequest(
    string Text,
    string? ProviderID,
    string? ModelID,
    string? Agent);
