using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Provides operations for managing LLM provider configuration,
/// including retrieval, persistence, and connection verification.
/// </summary>
public interface ILlmProviderService
{
    /// <summary>
    /// Retrieves the current LLM provider configuration.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the <see cref="LlmProviderConfigDto"/> on success,
    /// or a failure result if the configuration could not be loaded.
    /// </returns>
    Task<Result<LlmProviderConfigDto>> GetConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the LLM provider configuration. The API key is encrypted before storage.
    /// </summary>
    /// <param name="apiKey">The plain-text API key to encrypt and persist.</param>
    /// <param name="modelId">The model identifier to persist (e.g., "anthropic/claude-sonnet-4").</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure of the save operation.
    /// </returns>
    Task<Result> SaveConfigurationAsync(string apiKey, string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies connectivity to the LLM provider using the specified API key.
    /// </summary>
    /// <param name="apiKey">The plain-text API key to use for the verification request.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="LlmConnectionVerificationResult"/> indicating whether the connection succeeded
    /// or failed, with an error message on failure.
    /// </returns>
    Task<LlmConnectionVerificationResult> VerifyConnectionAsync(string apiKey, CancellationToken cancellationToken = default);
}
