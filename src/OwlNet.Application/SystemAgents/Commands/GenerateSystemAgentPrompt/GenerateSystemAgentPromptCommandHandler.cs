using System.Text.Json;
using System.Text.RegularExpressions;
using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Agents.Models;
using OwlNet.Application.Agents.Resources;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.SystemAgents.Commands.GenerateSystemAgentPrompt;

/// <summary>
/// Handles the <see cref="GenerateSystemAgentPromptCommand"/> by building a conversation context,
/// calling the LLM via <see cref="ILlmChatService"/>, and parsing the response into either
/// follow-up questions or a generated system agent markdown definition.
/// </summary>
public sealed partial class GenerateSystemAgentPromptCommandHandler
    : IRequestHandler<GenerateSystemAgentPromptCommand, ValueTask<Result<AgentGenerationResponseDto>>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [GeneratedRegex(@"```json\s*\n([\s\S]*?)\n\s*```")]
    private static partial Regex JsonCodeBlockRegex();

    private readonly ILlmChatService _llmChatService;
    private readonly ILogger<GenerateSystemAgentPromptCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateSystemAgentPromptCommandHandler"/> class.
    /// </summary>
    /// <param name="llmChatService">The LLM chat service for sending completion requests.</param>
    /// <param name="logger">The logger instance.</param>
    public GenerateSystemAgentPromptCommandHandler(
        ILlmChatService llmChatService,
        ILogger<GenerateSystemAgentPromptCommandHandler> logger)
    {
        _llmChatService = llmChatService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<AgentGenerationResponseDto>> Handle(
        GenerateSystemAgentPromptCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Generating system agent prompt for {AgentName} (type: {AgentType}, forceGenerate: {ForceGenerate}, historyCount: {HistoryCount})",
            request.AgentName, request.AgentType, request.ForceGenerate, request.ConversationHistory.Count);

        var systemPrompt = AgentArchitectPrompt.GetSystemPrompt();
        var messages = BuildMessageList(request);

        var chatResult = await _llmChatService.SendChatCompletionAsync(
            systemPrompt, messages, temperature: 0.4, cancellationToken);

        if (chatResult.IsFailure)
        {
            _logger.LogError("LLM chat completion failed for system agent {AgentName}: {Error}",
                request.AgentName, chatResult.Error);
            return Result<AgentGenerationResponseDto>.Failure(chatResult.Error);
        }

        var responseText = chatResult.Value;

        _logger.LogDebug("LLM response received for system agent {AgentName} ({ResponseLength} chars)",
            request.AgentName, responseText.Length);

        var dto = ParseLlmResponse(responseText);

        _logger.LogInformation(
            "System agent prompt generation completed for {AgentName} with response type {ResponseType}",
            request.AgentName, dto.ResponseType);

        return Result<AgentGenerationResponseDto>.Success(dto);
    }

    /// <summary>
    /// Builds the ordered list of chat messages to send to the LLM, including the initial
    /// context message, conversation history, and optional force-generate instruction.
    /// </summary>
    private static List<ChatMessage> BuildMessageList(GenerateSystemAgentPromptCommand request)
    {
        var messages = new List<ChatMessage>();

        var initialMessage = BuildInitialUserMessage(request);
        messages.Add(new ChatMessage("user", initialMessage));

        foreach (var entry in request.ConversationHistory)
        {
            messages.Add(new ChatMessage(entry.Role, entry.Content));
        }

        if (request.ForceGenerate && request.ConversationHistory.Count > 0)
        {
            messages.Add(new ChatMessage("user",
                "Please generate the agent definition now with the information gathered so far. force_generate = true"));
        }

        return messages;
    }

    /// <summary>
    /// Builds the initial user message that provides the LLM with the system agent's type, name,
    /// and description context.
    /// </summary>
    private static string BuildInitialUserMessage(GenerateSystemAgentPromptCommand request)
    {
        var message = $"""
            I want to create an agent with the following details:
            - Type: {request.AgentType}
            - Name: {request.AgentName}
            - Description: {request.AgentDescription}
            """;

        if (request.ForceGenerate && request.ConversationHistory.Count == 0)
        {
            message += "\nPlease generate the agent definition now without asking further questions. force_generate = true";
        }

        return message;
    }

    /// <summary>
    /// Parses the raw LLM response text into an <see cref="AgentGenerationResponseDto"/>.
    /// Attempts JSON deserialization first, then falls back to extracting JSON from a markdown
    /// code block, and finally treats the entire response as generated markdown.
    /// </summary>
    private AgentGenerationResponseDto ParseLlmResponse(string responseText)
    {
        var parsed = TryParseJson(responseText);

        if (parsed is null)
        {
            parsed = TryExtractJsonFromCodeBlock(responseText);
        }

        if (parsed is not null)
        {
            return MapParsedResponse(parsed);
        }

        _logger.LogWarning("Could not parse LLM response as JSON; treating as raw markdown fallback");

        return new AgentGenerationResponseDto(
            AgentGenerationResponseType.GeneratedMarkdown,
            Questions: null,
            GeneratedMarkdown: responseText,
            AssistantMessage: responseText);
    }

    /// <summary>
    /// Attempts to deserialize the response text directly as a <see cref="LlmAgentResponse"/>.
    /// </summary>
    private static LlmAgentResponse? TryParseJson(string text)
    {
        try
        {
            var trimmed = text.Trim();
            if (!trimmed.StartsWith('{'))
            {
                return null;
            }

            return JsonSerializer.Deserialize<LlmAgentResponse>(trimmed, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to extract a JSON object from a markdown <c>```json ... ```</c> code block
    /// and deserialize it as a <see cref="LlmAgentResponse"/>.
    /// </summary>
    private static LlmAgentResponse? TryExtractJsonFromCodeBlock(string text)
    {
        var match = JsonCodeBlockRegex().Match(text);
        if (!match.Success)
        {
            return null;
        }

        return TryParseJson(match.Groups[1].Value);
    }

    /// <summary>
    /// Maps a successfully parsed <see cref="LlmAgentResponse"/> to the appropriate
    /// <see cref="AgentGenerationResponseDto"/> based on the response type.
    /// </summary>
    private static AgentGenerationResponseDto MapParsedResponse(LlmAgentResponse parsed)
    {
        if (string.Equals(parsed.Type, "questions", StringComparison.OrdinalIgnoreCase))
        {
            return new AgentGenerationResponseDto(
                AgentGenerationResponseType.Questions,
                Questions: parsed.Questions?.AsReadOnly() ?? (IReadOnlyList<string>)[],
                GeneratedMarkdown: null,
                AssistantMessage: parsed.Message);
        }

        return new AgentGenerationResponseDto(
            AgentGenerationResponseType.GeneratedMarkdown,
            Questions: null,
            GeneratedMarkdown: parsed.Content ?? string.Empty,
            AssistantMessage: parsed.Message);
    }

    /// <summary>
    /// Internal DTO for deserializing the structured JSON response from the LLM.
    /// </summary>
    private sealed class LlmAgentResponse
    {
        /// <summary>
        /// The response type — <c>"questions"</c> or <c>"markdown"</c>.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The assistant's conversational message for display in the UI.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The list of follow-up questions when <see cref="Type"/> is <c>"questions"</c>.
        /// </summary>
        public List<string>? Questions { get; set; }

        /// <summary>
        /// The generated system agent markdown content when <see cref="Type"/> is <c>"markdown"</c>.
        /// </summary>
        public string? Content { get; set; }
    }
}
