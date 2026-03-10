using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Agents.Models;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Application.SystemAgents.Commands.GenerateSystemAgentPrompt;
using Shouldly;

namespace OwlNet.Tests.Application.SystemAgents.Commands.GenerateSystemAgentPrompt;

/// <summary>
/// Unit tests for <see cref="GenerateSystemAgentPromptCommandHandler"/>.
/// Covers LLM response parsing (questions JSON, markdown JSON, code-block extraction,
/// raw fallback), LLM failure propagation, force-generate message construction,
/// conversation history mapping, and temperature parameter verification.
/// </summary>
public sealed class GenerateSystemAgentPromptCommandHandlerTests
{
    private const string DefaultAgentType = "subagent";
    private const string DefaultAgentName = "git-agent";
    private const string DefaultAgentDescription = "Performs Git operations: commits, branches, diffs, and conflict resolution.";

    private readonly ILlmChatService _llmChatService;
    private readonly GenerateSystemAgentPromptCommandHandler _sut;

    public GenerateSystemAgentPromptCommandHandlerTests()
    {
        _llmChatService = Substitute.For<ILlmChatService>();
        _sut = new GenerateSystemAgentPromptCommandHandler(
            _llmChatService,
            NullLogger<GenerateSystemAgentPromptCommandHandler>.Instance);
    }

    // ──────────────────────────────────────────────
    // Happy Path — Questions JSON
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_LlmReturnsQuestionsJson_ReturnsQuestionsResponse()
    {
        // Arrange
        var questionsJson = """
            {"type":"questions","message":"Let me ask you a few things.","questions":["Q1?","Q2?"]}
            """;
        var command = CreateCommand();

        ConfigureLlmSuccess(questionsJson);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.ResponseType.ShouldBe(AgentGenerationResponseType.Questions),
            () => result.Value.Questions.ShouldNotBeNull(),
            () => result.Value.Questions!.Count.ShouldBe(2),
            () => result.Value.Questions![0].ShouldBe("Q1?"),
            () => result.Value.Questions![1].ShouldBe("Q2?"),
            () => result.Value.AssistantMessage.ShouldBe("Let me ask you a few things."),
            () => result.Value.GeneratedMarkdown.ShouldBeNull()
        );
    }

    // ──────────────────────────────────────────────
    // Happy Path — Markdown JSON
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_LlmReturnsMarkdownJson_ReturnsGeneratedMarkdownResponse()
    {
        // Arrange — \n in raw string literal is literal backslash+n, which JSON deserializes as newline
        var markdownJson =
            """{"type":"markdown","message":"Here's your agent.","content":"---\ndescription: A git agent\n---\nYou are a helpful git assistant."}""";
        var expectedContent = "---\ndescription: A git agent\n---\nYou are a helpful git assistant.";
        var command = CreateCommand();

        ConfigureLlmSuccess(markdownJson);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.ResponseType.ShouldBe(AgentGenerationResponseType.GeneratedMarkdown),
            () => result.Value.GeneratedMarkdown.ShouldBe(expectedContent),
            () => result.Value.AssistantMessage.ShouldBe("Here's your agent."),
            () => result.Value.Questions.ShouldBeNull()
        );
    }

    // ──────────────────────────────────────────────
    // LLM Failure
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_LlmChatFails_ReturnsFailure()
    {
        // Arrange
        var errorMessage = "LLM provider is not configured. Please configure an API key in Settings.";
        var command = CreateCommand();

        _llmChatService.SendChatCompletionAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<ChatMessage>>(),
                Arg.Any<double>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure(errorMessage)));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe(errorMessage)
        );
    }

    // ──────────────────────────────────────────────
    // Fallback — Invalid JSON → Raw Markdown
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_LlmReturnsInvalidJson_FallsBackToRawMarkdown()
    {
        // Arrange
        var plainText = "Here is a plain text response that is not JSON at all.";
        var command = CreateCommand();

        ConfigureLlmSuccess(plainText);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.ResponseType.ShouldBe(AgentGenerationResponseType.GeneratedMarkdown),
            () => result.Value.GeneratedMarkdown.ShouldBe(plainText),
            () => result.Value.AssistantMessage.ShouldBe(plainText),
            () => result.Value.Questions.ShouldBeNull()
        );
    }

    // ──────────────────────────────────────────────
    // JSON in Code Block Extraction
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_LlmReturnsJsonInCodeBlock_ParsesSuccessfully()
    {
        // Arrange
        var codeBlockResponse = """
            Here is the response:
            ```json
            {"type":"questions","message":"I have some questions.","questions":["Q1?"]}
            ```
            """;
        var command = CreateCommand();

        ConfigureLlmSuccess(codeBlockResponse);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.ResponseType.ShouldBe(AgentGenerationResponseType.Questions),
            () => result.Value.Questions.ShouldNotBeNull(),
            () => result.Value.Questions!.Count.ShouldBe(1),
            () => result.Value.Questions![0].ShouldBe("Q1?"),
            () => result.Value.AssistantMessage.ShouldBe("I have some questions.")
        );
    }

    // ──────────────────────────────────────────────
    // Force Generate — No History
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ForceGenerateNoHistory_AddsForceInstructionToInitialMessage()
    {
        // Arrange
        var command = CreateCommand(forceGenerate: true, conversationHistory: []);

        IReadOnlyList<ChatMessage>? capturedMessages = null;
        _llmChatService.SendChatCompletionAsync(
                Arg.Any<string>(),
                Arg.Do<IReadOnlyList<ChatMessage>>(msgs => capturedMessages = msgs),
                Arg.Any<double>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("""{"type":"markdown","message":"Done.","content":"# Agent"}""")));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedMessages.ShouldNotBeNull();
        capturedMessages.Count.ShouldBe(1);
        capturedMessages[0].ShouldSatisfyAllConditions(
            () => capturedMessages[0].Role.ShouldBe("user"),
            () => capturedMessages[0].Content.ShouldContain("force_generate = true"),
            () => capturedMessages[0].Content.ShouldContain("without asking further questions")
        );
    }

    // ──────────────────────────────────────────────
    // Force Generate — With History
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ForceGenerateWithHistory_AddsForceMessageAtEnd()
    {
        // Arrange
        var history = new List<ConversationMessage>
        {
            new("assistant", "What Git workflows should it support?"),
            new("user", "GitFlow and trunk-based development")
        };
        var command = CreateCommand(forceGenerate: true, conversationHistory: history);

        IReadOnlyList<ChatMessage>? capturedMessages = null;
        _llmChatService.SendChatCompletionAsync(
                Arg.Any<string>(),
                Arg.Do<IReadOnlyList<ChatMessage>>(msgs => capturedMessages = msgs),
                Arg.Any<double>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("""{"type":"markdown","message":"Done.","content":"# Agent"}""")));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedMessages.ShouldNotBeNull();
        // 1 initial + 2 history + 1 force-generate = 4 messages
        capturedMessages.Count.ShouldBe(4);

        var lastMessage = capturedMessages[^1];
        lastMessage.ShouldSatisfyAllConditions(
            () => lastMessage.Role.ShouldBe("user"),
            () => lastMessage.Content.ShouldContain("force_generate = true"),
            () => lastMessage.Content.ShouldContain("generate the agent definition now")
        );
    }

    // ──────────────────────────────────────────────
    // Conversation History Mapping
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithConversationHistory_IncludesHistoryInMessages()
    {
        // Arrange
        var history = new List<ConversationMessage>
        {
            new("assistant", "What is the agent's primary task?"),
            new("user", "Git commit and branch management"),
            new("assistant", "What languages should it support?"),
            new("user", "Bash and PowerShell")
        };
        var command = CreateCommand(conversationHistory: history);

        IReadOnlyList<ChatMessage>? capturedMessages = null;
        _llmChatService.SendChatCompletionAsync(
                Arg.Any<string>(),
                Arg.Do<IReadOnlyList<ChatMessage>>(msgs => capturedMessages = msgs),
                Arg.Any<double>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("""{"type":"questions","message":"More questions.","questions":["Q?"]}""")));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedMessages.ShouldNotBeNull();
        // 1 initial + 4 history = 5 messages
        capturedMessages.Count.ShouldBe(5);

        // History entries should be at indices 1–4, preserving order and roles
        capturedMessages[1].ShouldSatisfyAllConditions(
            () => capturedMessages[1].Role.ShouldBe("assistant"),
            () => capturedMessages[1].Content.ShouldBe("What is the agent's primary task?")
        );
        capturedMessages[2].ShouldSatisfyAllConditions(
            () => capturedMessages[2].Role.ShouldBe("user"),
            () => capturedMessages[2].Content.ShouldBe("Git commit and branch management")
        );
        capturedMessages[3].ShouldSatisfyAllConditions(
            () => capturedMessages[3].Role.ShouldBe("assistant"),
            () => capturedMessages[3].Content.ShouldBe("What languages should it support?")
        );
        capturedMessages[4].ShouldSatisfyAllConditions(
            () => capturedMessages[4].Role.ShouldBe("user"),
            () => capturedMessages[4].Content.ShouldBe("Bash and PowerShell")
        );
    }

    // ──────────────────────────────────────────────
    // Temperature Verification
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_PassesCorrectTemperature()
    {
        // Arrange
        var command = CreateCommand();

        double capturedTemperature = -1;
        _llmChatService.SendChatCompletionAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<ChatMessage>>(),
                Arg.Do<double>(t => capturedTemperature = t),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("""{"type":"questions","message":"Hi","questions":["Q?"]}""")));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedTemperature.ShouldBe(0.4);
    }

    // ──────────────────────────────────────────────
    // Edge Case — Empty Questions Array
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyQuestionsArray_ReturnsEmptyList()
    {
        // Arrange
        var emptyQuestionsJson = """{"type":"questions","message":"No questions needed.","questions":[]}""";
        var command = CreateCommand();

        ConfigureLlmSuccess(emptyQuestionsJson);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.ResponseType.ShouldBe(AgentGenerationResponseType.Questions),
            () => result.Value.Questions.ShouldNotBeNull(),
            () => result.Value.Questions!.ShouldBeEmpty(),
            () => result.Value.AssistantMessage.ShouldBe("No questions needed.")
        );
    }

    // ──────────────────────────────────────────────
    // Edge Case — Initial Message Contains Agent Details
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_InitialMessageContainsAgentDetails()
    {
        // Arrange
        var command = CreateCommand(
            agentType: "all",
            agentName: "deploy-agent",
            agentDescription: "Handles deployment pipelines");

        IReadOnlyList<ChatMessage>? capturedMessages = null;
        _llmChatService.SendChatCompletionAsync(
                Arg.Any<string>(),
                Arg.Do<IReadOnlyList<ChatMessage>>(msgs => capturedMessages = msgs),
                Arg.Any<double>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("""{"type":"questions","message":"Hi","questions":["Q?"]}""")));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedMessages.ShouldNotBeNull();
        var initialMessage = capturedMessages[0].Content;
        initialMessage.ShouldSatisfyAllConditions(
            () => initialMessage.ShouldContain("all"),
            () => initialMessage.ShouldContain("deploy-agent"),
            () => initialMessage.ShouldContain("Handles deployment pipelines")
        );
    }

    // ──────────────────────────────────────────────
    // Edge Case — Markdown JSON with Null Content
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_MarkdownJsonWithNullContent_ReturnsEmptyStringAsMarkdown()
    {
        // Arrange
        var markdownJsonNullContent = """{"type":"markdown","message":"Generated.","content":null}""";
        var command = CreateCommand();

        ConfigureLlmSuccess(markdownJsonNullContent);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.ResponseType.ShouldBe(AgentGenerationResponseType.GeneratedMarkdown),
            () => result.Value.GeneratedMarkdown.ShouldBe(string.Empty),
            () => result.Value.AssistantMessage.ShouldBe("Generated.")
        );
    }

    // ──────────────────────────────────────────────
    // Edge Case — Questions JSON with Null Questions
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_QuestionsJsonWithNullQuestions_ReturnsEmptyList()
    {
        // Arrange
        var questionsJsonNullList = """{"type":"questions","message":"Hmm.","questions":null}""";
        var command = CreateCommand();

        ConfigureLlmSuccess(questionsJsonNullList);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.ResponseType.ShouldBe(AgentGenerationResponseType.Questions),
            () => result.Value.Questions.ShouldNotBeNull(),
            () => result.Value.Questions!.ShouldBeEmpty()
        );
    }

    // ──────────────────────────────────────────────
    // Edge Case — ForceGenerate false with no history
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoForceGenerateNoHistory_SendsSingleInitialMessage()
    {
        // Arrange
        var command = CreateCommand(forceGenerate: false, conversationHistory: []);

        IReadOnlyList<ChatMessage>? capturedMessages = null;
        _llmChatService.SendChatCompletionAsync(
                Arg.Any<string>(),
                Arg.Do<IReadOnlyList<ChatMessage>>(msgs => capturedMessages = msgs),
                Arg.Any<double>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("""{"type":"questions","message":"Hi","questions":["Q?"]}""")));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedMessages.ShouldNotBeNull();
        capturedMessages.Count.ShouldBe(1);
        capturedMessages[0].Role.ShouldBe("user");
        capturedMessages[0].Content.ShouldNotContain("force_generate");
    }

    // ──────────────────────────────────────────────
    // Edge Case — ForceGenerate false with history
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoForceGenerateWithHistory_DoesNotAppendForceMessage()
    {
        // Arrange
        var history = new List<ConversationMessage>
        {
            new("assistant", "What Git workflows?"),
            new("user", "GitFlow")
        };
        var command = CreateCommand(forceGenerate: false, conversationHistory: history);

        IReadOnlyList<ChatMessage>? capturedMessages = null;
        _llmChatService.SendChatCompletionAsync(
                Arg.Any<string>(),
                Arg.Do<IReadOnlyList<ChatMessage>>(msgs => capturedMessages = msgs),
                Arg.Any<double>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("""{"type":"questions","message":"Hi","questions":["Q?"]}""")));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedMessages.ShouldNotBeNull();
        // 1 initial + 2 history = 3 messages (no force-generate appended)
        capturedMessages.Count.ShouldBe(3);
        capturedMessages[^1].ShouldSatisfyAllConditions(
            () => capturedMessages[^1].Role.ShouldBe("user"),
            () => capturedMessages[^1].Content.ShouldBe("GitFlow")
        );
    }

    // ──────────────────────────────────────────────
    // Edge Case — System Prompt Passed to LLM
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_PassesNonEmptySystemPrompt()
    {
        // Arrange
        var command = CreateCommand();

        string? capturedSystemPrompt = null;
        _llmChatService.SendChatCompletionAsync(
                Arg.Do<string>(sp => capturedSystemPrompt = sp),
                Arg.Any<IReadOnlyList<ChatMessage>>(),
                Arg.Any<double>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success("""{"type":"questions","message":"Hi","questions":["Q?"]}""")));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedSystemPrompt.ShouldNotBeNullOrWhiteSpace();
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private void ConfigureLlmSuccess(string responseText)
    {
        _llmChatService.SendChatCompletionAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<ChatMessage>>(),
                Arg.Any<double>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success(responseText)));
    }

    private static GenerateSystemAgentPromptCommand CreateCommand(
        string agentType = DefaultAgentType,
        string agentName = DefaultAgentName,
        string agentDescription = DefaultAgentDescription,
        bool forceGenerate = false,
        IReadOnlyList<ConversationMessage>? conversationHistory = null) =>
        new()
        {
            AgentType = agentType,
            AgentName = agentName,
            AgentDescription = agentDescription,
            ForceGenerate = forceGenerate,
            ConversationHistory = conversationHistory ?? []
        };
}
