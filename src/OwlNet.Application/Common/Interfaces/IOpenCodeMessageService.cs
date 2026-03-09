using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Service for sending prompts, retrieving messages, and executing commands within
/// OpenCode sessions. All methods accept the server base URL as a parameter to support
/// runtime URL changes.
/// </summary>
public interface IOpenCodeMessageService
{
    /// <summary>
    /// Sends a synchronous prompt to a session by calling <c>POST /session/:id/message</c>.
    /// The call blocks until the AI agent has produced a complete response, which may take
    /// several minutes depending on the model and prompt complexity. An extended HTTP timeout
    /// (default 300 seconds) is enforced for this operation.
    /// </summary>
    /// <param name="baseUrl">
    /// The base URL of the OpenCode Server (e.g., <c>"http://127.0.0.1:4096"</c>).
    /// </param>
    /// <param name="sessionId">The unique identifier of the session to send the prompt to.</param>
    /// <param name="request">
    /// The prompt request containing the text content and optional model override or agent selection.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the AI response as a <see cref="MessageWithPartsDto"/>
    /// on success, or a failure result with error details on failure.
    /// </returns>
    Task<Result<MessageWithPartsDto>> SendPromptAsync(
        string baseUrl,
        string sessionId,
        SendPromptRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a prompt asynchronously by calling <c>POST /session/:id/prompt_async</c>.
    /// The server returns immediately (HTTP 204) without waiting for the AI response.
    /// The response can be retrieved later via <see cref="ListMessagesAsync"/> or server-sent events.
    /// </summary>
    /// <param name="baseUrl">
    /// The base URL of the OpenCode Server (e.g., <c>"http://127.0.0.1:4096"</c>).
    /// </param>
    /// <param name="sessionId">The unique identifier of the session to submit the prompt to.</param>
    /// <param name="request">
    /// The prompt request containing the text content and optional model override or agent selection.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure with error details.
    /// </returns>
    Task<Result> SubmitPromptAsync(
        string baseUrl,
        string sessionId,
        SendPromptRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all messages in a session by calling <c>GET /session/:id/message</c>.
    /// Messages are returned in chronological order and include both user prompts and AI responses.
    /// </summary>
    /// <param name="baseUrl">
    /// The base URL of the OpenCode Server (e.g., <c>"http://127.0.0.1:4096"</c>).
    /// </param>
    /// <param name="sessionId">The unique identifier of the session whose messages to list.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a read-only list of <see cref="MessageWithPartsDto"/>
    /// on success, or a failure result with error details on failure.
    /// </returns>
    Task<Result<IReadOnlyList<MessageWithPartsDto>>> ListMessagesAsync(
        string baseUrl,
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific message by ID by calling <c>GET /session/:id/message/:messageID</c>.
    /// Returns the message metadata and all associated parts (text, tool calls, tool results, etc.).
    /// </summary>
    /// <param name="baseUrl">
    /// The base URL of the OpenCode Server (e.g., <c>"http://127.0.0.1:4096"</c>).
    /// </param>
    /// <param name="sessionId">The unique identifier of the session containing the message.</param>
    /// <param name="messageId">The unique identifier of the message to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the <see cref="MessageWithPartsDto"/> on success,
    /// or a failure result with error details on failure.
    /// </returns>
    Task<Result<MessageWithPartsDto>> GetMessageAsync(
        string baseUrl,
        string sessionId,
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a slash command in a session by calling <c>POST /session/:id/command</c>.
    /// Slash commands perform special operations such as compacting context, switching agents,
    /// or triggering built-in workflows.
    /// </summary>
    /// <param name="baseUrl">
    /// The base URL of the OpenCode Server (e.g., <c>"http://127.0.0.1:4096"</c>).
    /// </param>
    /// <param name="sessionId">The unique identifier of the session to execute the command in.</param>
    /// <param name="request">
    /// The command request containing the command name and optional arguments.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure with error details.
    /// </returns>
    Task<Result> ExecuteCommandAsync(
        string baseUrl,
        string sessionId,
        ExecuteCommandRequest request,
        CancellationToken cancellationToken = default);
}
