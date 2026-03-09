using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using OwlNet.Application.Common.Models;
using OwlNet.Infrastructure.Services;
using Shouldly;

namespace OwlNet.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for <see cref="OpenCodeMessageService"/>. The HTTP layer is faked using custom
/// <see cref="HttpMessageHandler"/> implementations — no real network calls are made.
/// </summary>
public sealed class OpenCodeMessageServiceTests
{
    private const string BaseUrl = "http://localhost:4096";

    /// <summary>
    /// Creates an <see cref="OpenCodeMessageService"/> backed by the given <see cref="HttpClient"/>.
    /// </summary>
    private static OpenCodeMessageService CreateSut(HttpClient httpClient)
    {
        return new OpenCodeMessageService(httpClient, NullLogger<OpenCodeMessageService>.Instance);
    }

    /// <summary>
    /// Creates a valid <see cref="SendPromptRequest"/> with sensible defaults.
    /// </summary>
    private static SendPromptRequest CreateValidPromptRequest(
        string text = "Hello",
        string? providerID = null,
        string? modelID = null,
        string? agent = null)
    {
        return new SendPromptRequest(text, providerID, modelID, agent);
    }

    /// <summary>
    /// Creates a valid <see cref="ExecuteCommandRequest"/> with sensible defaults.
    /// </summary>
    private static ExecuteCommandRequest CreateValidCommandRequest(
        string command = "compact",
        IReadOnlyList<string>? args = null)
    {
        return new ExecuteCommandRequest(command, args);
    }

    // ──────────────────────────────────────────────
    // HTTP helpers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Creates an <see cref="HttpClient"/> backed by a handler that returns the specified
    /// status code and JSON content.
    /// </summary>
    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string? jsonContent = null)
    {
        var handler = new FakeHttpMessageHandler(statusCode, jsonContent);
        return new HttpClient(handler);
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> backed by a handler that throws the specified exception.
    /// </summary>
    private static HttpClient CreateThrowingHttpClient(Exception exception)
    {
        var handler = new ThrowingHttpMessageHandler(exception);
        return new HttpClient(handler);
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> backed by a handler that captures the outgoing request
    /// (including body content) for inspection and returns HTTP 200 with the specified JSON content.
    /// </summary>
    private static HttpClient CreateCapturingHttpClient(out CapturingHttpMessageHandler handler, string? jsonContent = null)
    {
        handler = new CapturingHttpMessageHandler(jsonContent);
        return new HttpClient(handler);
    }

    // ──────────────────────────────────────────────
    // 1. SendPromptAsync — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SendPromptAsync_ValidResponse_ReturnsSuccessWithMessageDto()
    {
        // Arrange
        var json = """{"id": "msg_abc123", "role": "assistant", "createdAt": 1709827200000, "model": "claude-sonnet-4-20250514", "parts": [{"type": "text", "text": "Hello, world!"}]}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest("Hello");

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.Message.Id.ShouldBe("msg_abc123"),
            () => result.Value.Message.Role.ShouldBe("assistant"),
            () => result.Value.Message.CreatedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1709827200000)),
            () => result.Value.Message.Model.ShouldBe("claude-sonnet-4-20250514"),
            () => result.Value.Parts.Count.ShouldBe(1),
            () => result.Value.Parts[0].Type.ShouldBe("text"),
            () => result.Value.Parts[0].Content.ShouldBe("Hello, world!")
        );
    }

    [Fact]
    public async Task SendPromptAsync_WithModelOverride_SendsCorrectRequestBody()
    {
        // Arrange
        var json = """{"id": "msg_abc123", "role": "assistant", "createdAt": 1709827200000, "model": "claude-sonnet-4-20250514", "parts": [{"type": "text", "text": "Hi"}]}""";
        var httpClient = CreateCapturingHttpClient(out var handler, json);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest("Hello", providerID: "anthropic", modelID: "claude-sonnet-4-20250514");

        // Act
        await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        handler.CapturedRequestBody.ShouldNotBeNull();
        handler.CapturedRequestBody.ShouldSatisfyAllConditions(
            () => handler.CapturedRequestBody!.ShouldContain("\"providerID\":\"anthropic\""),
            () => handler.CapturedRequestBody!.ShouldContain("\"modelID\":\"claude-sonnet-4-20250514\"")
        );
    }

    [Fact]
    public async Task SendPromptAsync_WithoutModelOverride_OmitsModelInBody()
    {
        // Arrange
        var json = """{"id": "msg_abc123", "role": "assistant", "createdAt": 1709827200000, "model": null, "parts": [{"type": "text", "text": "Hi"}]}""";
        var httpClient = CreateCapturingHttpClient(out var handler, json);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest("Hello");

        // Act
        await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        handler.CapturedRequestBody.ShouldNotBeNull();
        handler.CapturedRequestBody.ShouldContain("\"model\":null");
    }

    [Fact]
    public async Task SendPromptAsync_WithAgent_SendsAgentInBody()
    {
        // Arrange
        var json = """{"id": "msg_abc123", "role": "assistant", "createdAt": 1709827200000, "model": null, "parts": [{"type": "text", "text": "Hi"}]}""";
        var httpClient = CreateCapturingHttpClient(out var handler, json);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest("Hello", agent: "build");

        // Act
        await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        handler.CapturedRequestBody.ShouldNotBeNull();
        handler.CapturedRequestBody.ShouldContain("\"agent\":\"build\"");
    }

    [Fact]
    public async Task SendPromptAsync_ResponseWithToolCallParts_MapsAllPartTypes()
    {
        // Arrange
        var json = """{"id": "msg_abc123", "role": "assistant", "createdAt": 1709827200000, "model": "claude-sonnet-4-20250514", "parts": [{"type": "text", "text": "Let me check..."}, {"type": "tool_call", "toolCallId": "tc_123", "toolName": "read_file", "text": null}, {"type": "tool_result", "toolCallId": "tc_123", "toolName": null, "text": "file contents"}]}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest("Check the file");

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Parts.Count.ShouldBe(3);
        result.Value.Parts.ShouldSatisfyAllConditions(
            () => result.Value.Parts[0].Type.ShouldBe("text"),
            () => result.Value.Parts[0].Content.ShouldBe("Let me check..."),
            () => result.Value.Parts[0].ToolCallId.ShouldBeNull(),
            () => result.Value.Parts[0].ToolName.ShouldBeNull(),
            () => result.Value.Parts[1].Type.ShouldBe("tool_call"),
            () => result.Value.Parts[1].Content.ShouldBeNull(),
            () => result.Value.Parts[1].ToolCallId.ShouldBe("tc_123"),
            () => result.Value.Parts[1].ToolName.ShouldBe("read_file"),
            () => result.Value.Parts[2].Type.ShouldBe("tool_result"),
            () => result.Value.Parts[2].Content.ShouldBe("file contents"),
            () => result.Value.Parts[2].ToolCallId.ShouldBe("tc_123"),
            () => result.Value.Parts[2].ToolName.ShouldBeNull()
        );
    }

    [Fact]
    public async Task SendPromptAsync_ConstructsCorrectUri()
    {
        // Arrange
        var json = """{"id": "msg_abc123", "role": "assistant", "createdAt": 1709827200000, "model": null, "parts": [{"type": "text", "text": "Hi"}]}""";
        var httpClient = CreateCapturingHttpClient(out var handler, json);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest("Hello");

        // Act
        await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        handler.CapturedRequest.ShouldNotBeNull();
        handler.CapturedRequest!.ShouldSatisfyAllConditions(
            () => handler.CapturedRequest!.RequestUri!.ToString().ShouldBe("http://localhost:4096/session/sess_1/message"),
            () => handler.CapturedRequest!.Method.ShouldBe(HttpMethod.Post)
        );
    }

    // ──────────────────────────────────────────────
    // 2. SendPromptAsync — Validation
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SendPromptAsync_NullSessionId_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, null!, request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Session ID is required")
        );
    }

    [Fact]
    public async Task SendPromptAsync_EmptySessionId_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "", request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Session ID is required")
        );
    }

    [Fact]
    public async Task SendPromptAsync_NullRequest_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", null!, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Prompt request is required")
        );
    }

    [Fact]
    public async Task SendPromptAsync_EmptyPromptText_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest(text: "");

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Prompt text is required")
        );
    }

    // ──────────────────────────────────────────────
    // 3. SubmitPromptAsync — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SubmitPromptAsync_Success_ReturnsSuccessResult()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest("Hello");

        // Act
        var result = await sut.SubmitPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task SubmitPromptAsync_ConstructsCorrectUri()
    {
        // Arrange
        var httpClient = CreateCapturingHttpClient(out var handler);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest("Hello");

        // Act
        await sut.SubmitPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        handler.CapturedRequest.ShouldNotBeNull();
        handler.CapturedRequest!.ShouldSatisfyAllConditions(
            () => handler.CapturedRequest!.RequestUri!.ToString().ShouldBe("http://localhost:4096/session/sess_1/prompt_async"),
            () => handler.CapturedRequest!.Method.ShouldBe(HttpMethod.Post)
        );
    }

    // ──────────────────────────────────────────────
    // 4. SubmitPromptAsync — Validation
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SubmitPromptAsync_NullSessionId_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SubmitPromptAsync(BaseUrl, null!, request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Session ID is required")
        );
    }

    [Fact]
    public async Task SubmitPromptAsync_EmptyPromptText_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest(text: "");

        // Act
        var result = await sut.SubmitPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Prompt text is required")
        );
    }

    // ──────────────────────────────────────────────
    // 5. ListMessagesAsync — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ListMessagesAsync_ValidResponse_ReturnsSuccessWithMessageList()
    {
        // Arrange
        var json = """[{"id": "msg_1", "role": "user", "createdAt": 1709827200000, "model": null, "parts": [{"type": "text", "text": "Hello"}]}, {"id": "msg_2", "role": "assistant", "createdAt": 1709827300000, "model": "claude-sonnet-4-20250514", "parts": [{"type": "text", "text": "Hi there!"}]}]""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.ListMessagesAsync(BaseUrl, "sess_1", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.Count.ShouldBe(2),
            () => result.Value[0].Message.Id.ShouldBe("msg_1"),
            () => result.Value[0].Message.Role.ShouldBe("user"),
            () => result.Value[0].Message.CreatedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1709827200000)),
            () => result.Value[0].Message.Model.ShouldBeNull(),
            () => result.Value[0].Parts.Count.ShouldBe(1),
            () => result.Value[0].Parts[0].Type.ShouldBe("text"),
            () => result.Value[0].Parts[0].Content.ShouldBe("Hello"),
            () => result.Value[1].Message.Id.ShouldBe("msg_2"),
            () => result.Value[1].Message.Role.ShouldBe("assistant"),
            () => result.Value[1].Message.CreatedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1709827300000)),
            () => result.Value[1].Message.Model.ShouldBe("claude-sonnet-4-20250514"),
            () => result.Value[1].Parts.Count.ShouldBe(1),
            () => result.Value[1].Parts[0].Type.ShouldBe("text"),
            () => result.Value[1].Parts[0].Content.ShouldBe("Hi there!")
        );
    }

    [Fact]
    public async Task ListMessagesAsync_EmptyArray_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "[]");
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.ListMessagesAsync(BaseUrl, "sess_1", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListMessagesAsync_MessageWithMissingId_SkipsInvalidMessage()
    {
        // Arrange — array contains one null-ID message and one valid
        var json = """[{"id": null, "role": "user", "createdAt": 1709827200000, "model": null, "parts": [{"type": "text", "text": "Bad"}]}, {"id": "msg_valid", "role": "assistant", "createdAt": 1709827300000, "model": "claude-sonnet-4-20250514", "parts": [{"type": "text", "text": "Good"}]}]""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.ListMessagesAsync(BaseUrl, "sess_1", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.Count.ShouldBe(1),
            () => result.Value[0].Message.Id.ShouldBe("msg_valid"),
            () => result.Value[0].Message.Role.ShouldBe("assistant")
        );
    }

    // ──────────────────────────────────────────────
    // 6. ListMessagesAsync — Validation
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ListMessagesAsync_NullSessionId_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.ListMessagesAsync(BaseUrl, null!, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Session ID is required")
        );
    }

    // ──────────────────────────────────────────────
    // 7. GetMessageAsync — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetMessageAsync_ValidResponse_ReturnsSuccessWithMessageDto()
    {
        // Arrange
        var json = """{"id": "msg_abc123", "role": "assistant", "createdAt": 1709827200000, "model": "claude-sonnet-4-20250514", "parts": [{"type": "text", "text": "Hello, world!"}]}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.GetMessageAsync(BaseUrl, "sess_1", "msg_abc123", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.Message.Id.ShouldBe("msg_abc123"),
            () => result.Value.Message.Role.ShouldBe("assistant"),
            () => result.Value.Message.CreatedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1709827200000)),
            () => result.Value.Message.Model.ShouldBe("claude-sonnet-4-20250514"),
            () => result.Value.Parts.Count.ShouldBe(1),
            () => result.Value.Parts[0].Type.ShouldBe("text"),
            () => result.Value.Parts[0].Content.ShouldBe("Hello, world!")
        );
    }

    [Fact]
    public async Task GetMessageAsync_ConstructsCorrectUri()
    {
        // Arrange
        var json = """{"id": "msg_abc123", "role": "assistant", "createdAt": 1709827200000, "model": null, "parts": [{"type": "text", "text": "Hi"}]}""";
        var httpClient = CreateCapturingHttpClient(out var handler, json);
        var sut = CreateSut(httpClient);

        // Act
        await sut.GetMessageAsync(BaseUrl, "sess_1", "msg_abc123", CancellationToken.None);

        // Assert
        handler.CapturedRequest.ShouldNotBeNull();
        handler.CapturedRequest!.ShouldSatisfyAllConditions(
            () => handler.CapturedRequest!.RequestUri!.ToString().ShouldBe("http://localhost:4096/session/sess_1/message/msg_abc123"),
            () => handler.CapturedRequest!.Method.ShouldBe(HttpMethod.Get)
        );
    }

    // ──────────────────────────────────────────────
    // 8. GetMessageAsync — Validation
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetMessageAsync_NullSessionId_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.GetMessageAsync(BaseUrl, null!, "msg_1", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Session ID is required")
        );
    }

    [Fact]
    public async Task GetMessageAsync_EmptyMessageId_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.GetMessageAsync(BaseUrl, "sess_1", "", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Message ID is required")
        );
    }

    // ──────────────────────────────────────────────
    // 9. ExecuteCommandAsync — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ExecuteCommandAsync_Success_ReturnsSuccessResult()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);
        var request = CreateValidCommandRequest();

        // Act
        var result = await sut.ExecuteCommandAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithArgs_SendsArgsInBody()
    {
        // Arrange
        var httpClient = CreateCapturingHttpClient(out var handler);
        var sut = CreateSut(httpClient);
        var request = CreateValidCommandRequest("compact", args: new List<string> { "arg1", "arg2" });

        // Act
        await sut.ExecuteCommandAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        handler.CapturedRequestBody.ShouldNotBeNull();
        handler.CapturedRequestBody.ShouldSatisfyAllConditions(
            () => handler.CapturedRequestBody!.ShouldContain("\"command\":\"compact\""),
            () => handler.CapturedRequestBody!.ShouldContain("\"args\":[\"arg1\",\"arg2\"]")
        );
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithoutArgs_OmitsArgsInBody()
    {
        // Arrange
        var httpClient = CreateCapturingHttpClient(out var handler);
        var sut = CreateSut(httpClient);
        var request = CreateValidCommandRequest("compact");

        // Act
        await sut.ExecuteCommandAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        handler.CapturedRequestBody.ShouldNotBeNull();
        handler.CapturedRequestBody.ShouldSatisfyAllConditions(
            () => handler.CapturedRequestBody!.ShouldContain("\"command\":\"compact\""),
            () => handler.CapturedRequestBody!.ShouldContain("\"args\":null")
        );
    }

    [Fact]
    public async Task ExecuteCommandAsync_ConstructsCorrectUri()
    {
        // Arrange
        var httpClient = CreateCapturingHttpClient(out var handler);
        var sut = CreateSut(httpClient);
        var request = CreateValidCommandRequest();

        // Act
        await sut.ExecuteCommandAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        handler.CapturedRequest.ShouldNotBeNull();
        handler.CapturedRequest!.ShouldSatisfyAllConditions(
            () => handler.CapturedRequest!.RequestUri!.ToString().ShouldBe("http://localhost:4096/session/sess_1/command"),
            () => handler.CapturedRequest!.Method.ShouldBe(HttpMethod.Post)
        );
    }

    // ──────────────────────────────────────────────
    // 10. ExecuteCommandAsync — Validation
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ExecuteCommandAsync_NullSessionId_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);
        var request = CreateValidCommandRequest();

        // Act
        var result = await sut.ExecuteCommandAsync(BaseUrl, null!, request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Session ID is required")
        );
    }

    [Fact]
    public async Task ExecuteCommandAsync_NullRequest_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.ExecuteCommandAsync(BaseUrl, "sess_1", null!, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Command request is required")
        );
    }

    [Fact]
    public async Task ExecuteCommandAsync_EmptyCommandName_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);
        var request = CreateValidCommandRequest(command: "");

        // Act
        var result = await sut.ExecuteCommandAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Command name is required")
        );
    }

    // ──────────────────────────────────────────────
    // 11. Error scenarios (via SendPromptAsync)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SendPromptAsync_ServerReturns500_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.InternalServerError);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Server error (500)")
        );
    }

    [Fact]
    public async Task SendPromptAsync_ServerReturns404_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.NotFound);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Server error (404)")
        );
    }

    [Fact]
    public async Task SendPromptAsync_ConnectionRefused_ReturnsFailure()
    {
        // Arrange
        var socketException = new SocketException((int)SocketError.ConnectionRefused);
        var httpException = new HttpRequestException("Connection refused", socketException);
        var httpClient = CreateThrowingHttpClient(httpException);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Connection refused")
        );
    }

    [Fact]
    public async Task SendPromptAsync_NetworkError_ReturnsFailure()
    {
        // Arrange
        var httpException = new HttpRequestException("Name resolution failed");
        var httpClient = CreateThrowingHttpClient(httpException);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Network error")
        );
    }

    [Fact]
    public async Task SendPromptAsync_Timeout_ReturnsFailure()
    {
        // Arrange — TaskCanceledException NOT caused by the caller's token simulates an HTTP timeout
        var timeoutException = new TaskCanceledException(
            "The request was canceled due to the configured HttpClient.Timeout",
            new TimeoutException());
        var httpClient = CreateThrowingHttpClient(timeoutException);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Request timed out")
        );
    }

    [Fact]
    public async Task SendPromptAsync_InvalidJson_ReturnsFailure()
    {
        // Arrange — HTTP 200 with non-JSON body
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "this is not json");
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Unexpected response format")
        );
    }

    [Fact]
    public async Task SendPromptAsync_NullResponseBody_ReturnsFailure()
    {
        // Arrange — Server returns 200 with JSON literal "null"
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "null");
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Unexpected response format")
        );
    }

    [Fact]
    public async Task SendPromptAsync_MissingMessageId_ReturnsFailure()
    {
        // Arrange — Server returns 200 with a message that has null ID
        var json = """{"id": null, "role": "assistant", "createdAt": 1709827200000, "model": null, "parts": [{"type": "text", "text": "Hi"}]}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("missing a required ID")
        );
    }

    [Fact]
    public async Task SendPromptAsync_MalformedUrl_ReturnsFailure()
    {
        // Arrange — a URL that cannot be parsed by the Uri constructor
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SendPromptAsync("not-a-url://[invalid", "sess_1", request, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Invalid server URL format.")
        );
    }

    // ──────────────────────────────────────────────
    // 12. Edge cases
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SendPromptAsync_ResponseWithNullParts_ReturnsEmptyPartsList()
    {
        // Arrange — Message with null parts array
        var json = """{"id": "msg_abc123", "role": "assistant", "createdAt": 1709827200000, "model": null, "parts": null}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Parts.ShouldBeEmpty();
    }

    [Fact]
    public async Task SendPromptAsync_ResponseWithNullRole_DefaultsToUnknown()
    {
        // Arrange — Message with null role
        var json = """{"id": "msg_abc123", "role": null, "createdAt": 1709827200000, "model": null, "parts": [{"type": "text", "text": "Hi"}]}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);
        var request = CreateValidPromptRequest();

        // Act
        var result = await sut.SendPromptAsync(BaseUrl, "sess_1", request, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Message.Role.ShouldBe("unknown");
    }

    [Fact]
    public async Task ListMessagesAsync_NullResponseBody_ReturnsFailure()
    {
        // Arrange — Server returns 200 with JSON literal "null"
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "null");
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.ListMessagesAsync(BaseUrl, "sess_1", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Unexpected response format")
        );
    }

    // ──────────────────────────────────────────────
    // Fake HTTP message handlers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Returns a fixed <see cref="HttpStatusCode"/> and optional JSON content for every request.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _jsonContent;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string? jsonContent = null)
        {
            _statusCode = statusCode;
            _jsonContent = jsonContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);

            if (_jsonContent is not null)
            {
                response.Content = new StringContent(_jsonContent, System.Text.Encoding.UTF8, "application/json");
            }

            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// Throws the specified <see cref="Exception"/> for every request, simulating network or timeout failures.
    /// </summary>
    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }

    /// <summary>
    /// Captures the outgoing <see cref="HttpRequestMessage"/> (including body content) for assertion
    /// and returns HTTP 200 with optional JSON content.
    /// </summary>
    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly string? _jsonContent;

        public HttpRequestMessage? CapturedRequest { get; private set; }

        /// <summary>
        /// Gets the captured request body content as a string, or <c>null</c> if no body was sent.
        /// </summary>
        public string? CapturedRequestBody { get; private set; }

        public CapturingHttpMessageHandler(string? jsonContent = null)
        {
            _jsonContent = jsonContent;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CapturedRequest = new HttpRequestMessage(request.Method, request.RequestUri);

            if (request.Content is not null)
            {
                CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);

            if (_jsonContent is not null)
            {
                response.Content = new StringContent(_jsonContent, System.Text.Encoding.UTF8, "application/json");
            }

            return response;
        }
    }
}
