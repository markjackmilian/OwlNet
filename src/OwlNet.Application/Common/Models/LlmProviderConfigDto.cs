namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents the LLM provider configuration.
/// </summary>
/// <param name="ApiKey">The decrypted API key (may be null if decryption failed).</param>
/// <param name="ModelId">The model identifier (e.g., "anthropic/claude-sonnet-4").</param>
/// <param name="IsConfigured">Whether both API key and Model ID are present and valid.</param>
/// <param name="DecryptionFailed">Whether the saved API key could not be decrypted.</param>
public sealed record LlmProviderConfigDto(
    string? ApiKey,
    string? ModelId,
    bool IsConfigured,
    bool DecryptionFailed);
