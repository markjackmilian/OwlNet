using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Provides operations for sending chat completion requests to the configured LLM provider.
/// </summary>
public interface ILlmChatService
{
    /// <summary>
    /// Sends a chat completion request to the configured LLM provider.
    /// </summary>
    /// <param name="systemPrompt">The system prompt that sets the assistant's behavior and context.</param>
    /// <param name="messages">An ordered list of chat messages forming the conversation history.</param>
    /// <param name="temperature">
    /// The sampling temperature controlling response randomness (0.0–2.0). Lower values produce
    /// more deterministic output; higher values produce more creative output. Defaults to 0.4.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the assistant's response text on success,
    /// or a failure result with an error message if the request could not be completed.
    /// </returns>
    Task<Result<string>> SendChatCompletionAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> messages,
        double temperature = 0.4,
        CancellationToken cancellationToken = default);
}
