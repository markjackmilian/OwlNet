using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="ILlmChatService"/> that sends chat completion
/// requests to the OpenRouter API. Retrieves API key and model configuration from
/// <see cref="ILlmProviderService"/> and returns result objects for all outcomes.
/// </summary>
public sealed class LlmChatService : ILlmChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILlmProviderService _llmProviderService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<LlmChatService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmChatService"/> class.
    /// </summary>
    /// <param name="llmProviderService">The LLM provider service for retrieving API key and model configuration.</param>
    /// <param name="httpClient">The HTTP client configured for OpenRouter API communication.</param>
    /// <param name="logger">The logger instance for structured diagnostic output.</param>
    public LlmChatService(
        ILlmProviderService llmProviderService,
        HttpClient httpClient,
        ILogger<LlmChatService> logger)
    {
        _llmProviderService = llmProviderService;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<string>> SendChatCompletionAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> messages,
        double temperature = 0.4,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending chat completion request with {MessageCount} messages and temperature {Temperature}",
            messages.Count,
            temperature);

        var configResult = await GetValidatedConfigurationAsync(cancellationToken);
        if (configResult.IsFailure)
        {
            return Result<string>.Failure(configResult.Error);
        }

        var (apiKey, modelId) = configResult.Value;

        try
        {
            var requestBody = BuildRequestBody(systemPrompt, messages, modelId, temperature);
            using var response = await SendRequestAsync(apiKey, requestBody, cancellationToken);

            return await ProcessResponseAsync(response, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Chat completion request was cancelled by the caller");
            return Result<string>.Failure("The request was cancelled.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Chat completion request timed out");
            return Result<string>.Failure(
                "The request timed out. The LLM provider may be slow or unreachable. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Chat completion request failed due to a network error");
            return Result<string>.Failure(
                "Unable to reach the LLM provider. Check your internet connection and try again.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse chat completion response from the LLM provider");
            return Result<string>.Failure(
                "Received an invalid response from the LLM provider. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during chat completion");
            return Result<string>.Failure(
                "An unexpected error occurred while communicating with the LLM provider.");
        }
    }

    /// <summary>
    /// Retrieves and validates the LLM provider configuration, ensuring both API key and model are present.
    /// </summary>
    private async Task<Result<(string ApiKey, string ModelId)>> GetValidatedConfigurationAsync(
        CancellationToken cancellationToken)
    {
        var configResult = await _llmProviderService.GetConfigurationAsync(cancellationToken);

        if (configResult.IsFailure)
        {
            _logger.LogWarning("Failed to retrieve LLM provider configuration: {Error}", configResult.Error);
            return Result<(string, string)>.Failure(configResult.Error);
        }

        var config = configResult.Value;

        if (!config.IsConfigured)
        {
            _logger.LogWarning(
                "LLM provider is not configured (DecryptionFailed: {DecryptionFailed})",
                config.DecryptionFailed);
            return Result<(string, string)>.Failure(
                "LLM provider is not configured. Please configure it in Settings.");
        }

        return Result<(string, string)>.Success((config.ApiKey!, config.ModelId!));
    }

    /// <summary>
    /// Builds the JSON request body for the OpenRouter chat completion endpoint.
    /// </summary>
    private static string BuildRequestBody(
        string systemPrompt,
        IReadOnlyList<ChatMessage> messages,
        string modelId,
        double temperature)
    {
        var allMessages = new List<object>(messages.Count + 1)
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var message in messages)
        {
            allMessages.Add(new { role = message.Role, content = message.Content });
        }

        var requestPayload = new
        {
            model = modelId,
            temperature,
            messages = allMessages
        };

        return JsonSerializer.Serialize(requestPayload, JsonOptions);
    }

    /// <summary>
    /// Sends the chat completion HTTP request to the OpenRouter API.
    /// </summary>
    private async Task<HttpResponseMessage> SendRequestAsync(
        string apiKey,
        string requestBody,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending POST request to chat/completions endpoint");

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Processes the HTTP response, extracting the assistant's message content or returning an error.
    /// </summary>
    private async Task<Result<string>> ProcessResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return HandleHttpError(response.StatusCode, responseBody);
        }

        var chatResponse = JsonSerializer.Deserialize<OpenRouterChatResponse>(responseBody, JsonOptions);

        if (chatResponse?.Choices is not { Count: > 0 })
        {
            _logger.LogWarning("Chat completion response contained no choices");
            return Result<string>.Failure(
                "The LLM provider returned an empty response. Please try again.");
        }

        var content = chatResponse.Choices[0].Message?.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("Chat completion response contained an empty message");
            return Result<string>.Failure(
                "The LLM provider returned an empty message. Please try again.");
        }

        _logger.LogInformation("Chat completion request completed successfully");
        return Result<string>.Success(content);
    }

    /// <summary>
    /// Maps HTTP error status codes to user-friendly error messages.
    /// </summary>
    private Result<string> HandleHttpError(HttpStatusCode statusCode, string responseBody)
    {
        var errorMessage = statusCode switch
        {
            HttpStatusCode.Unauthorized =>
                "Invalid API key. Please check your LLM provider configuration in Settings.",
            HttpStatusCode.Forbidden =>
                "Access denied. Your API key does not have permission for this operation.",
            HttpStatusCode.TooManyRequests =>
                "Rate limit exceeded. Please wait a moment and try again.",
            HttpStatusCode.BadRequest =>
                "The request was rejected by the LLM provider. The model may not support this request.",
            HttpStatusCode.NotFound =>
                "The configured model was not found. Please verify the model ID in Settings.",
            _ =>
                $"The LLM provider returned an error (HTTP {(int)statusCode}). Please try again later."
        };

        _logger.LogWarning(
            "Chat completion request failed with HTTP {StatusCode}: {ResponseBody}",
            (int)statusCode,
            responseBody);

        return Result<string>.Failure(errorMessage);
    }

    /// <summary>
    /// Represents the top-level response from the OpenRouter chat completion endpoint.
    /// </summary>
    private sealed class OpenRouterChatResponse
    {
        /// <summary>
        /// Gets or sets the list of completion choices returned by the model.
        /// </summary>
        public List<OpenRouterChoice>? Choices { get; set; }
    }

    /// <summary>
    /// Represents a single completion choice in the OpenRouter response.
    /// </summary>
    private sealed class OpenRouterChoice
    {
        /// <summary>
        /// Gets or sets the message generated by the model for this choice.
        /// </summary>
        public OpenRouterChoiceMessage? Message { get; set; }
    }

    /// <summary>
    /// Represents the message content within a completion choice.
    /// </summary>
    private sealed class OpenRouterChoiceMessage
    {
        /// <summary>
        /// Gets or sets the text content of the assistant's response.
        /// </summary>
        public string? Content { get; set; }
    }
}
