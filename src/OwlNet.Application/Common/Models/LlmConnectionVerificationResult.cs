namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents the result of verifying a connection to an LLM provider.
/// </summary>
/// <param name="IsSuccess">Whether the connection verification succeeded.</param>
/// <param name="ErrorMessage">The error message if verification failed; null on success.</param>
public sealed record LlmConnectionVerificationResult(bool IsSuccess, string? ErrorMessage)
{
    /// <summary>
    /// Creates a successful verification result.
    /// </summary>
    /// <returns>A <see cref="LlmConnectionVerificationResult"/> indicating a successful connection.</returns>
    public static LlmConnectionVerificationResult Success() => new(true, null);

    /// <summary>
    /// Creates a failed verification result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">A description of why the connection verification failed.</param>
    /// <returns>A <see cref="LlmConnectionVerificationResult"/> indicating a failed connection.</returns>
    public static LlmConnectionVerificationResult Failure(string errorMessage) => new(false, errorMessage);
}
