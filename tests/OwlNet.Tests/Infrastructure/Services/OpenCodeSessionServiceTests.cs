using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using OwlNet.Application.Common.Models;
using OwlNet.Infrastructure.Services;
using Shouldly;

namespace OwlNet.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for <see cref="OpenCodeSessionService"/>. The HTTP layer is faked using custom
/// <see cref="HttpMessageHandler"/> implementations — no real network calls are made.
/// </summary>
public sealed class OpenCodeSessionServiceTests
{
    private const string BaseUrl = "http://localhost:4096";

    /// <summary>
    /// Creates an <see cref="OpenCodeSessionService"/> backed by the given <see cref="HttpClient"/>.
    /// </summary>
    private static OpenCodeSessionService CreateSut(HttpClient httpClient)
    {
        return new OpenCodeSessionService(httpClient, NullLogger<OpenCodeSessionService>.Instance);
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
    // 1. CreateSession — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateSessionAsync_ValidResponse_ReturnsSuccessWithSessionDto()
    {
        // Arrange
        var json = """{"id": "sess_abc123", "title": "My Session", "createdAt": 1709827200000, "updatedAt": 1709827200000}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.CreateSessionAsync(BaseUrl, "My Session", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.Id.ShouldBe("sess_abc123"),
            () => result.Value.Title.ShouldBe("My Session"),
            () => result.Value.CreatedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1709827200000)),
            () => result.Value.UpdatedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1709827200000))
        );
    }

    [Fact]
    public async Task CreateSessionAsync_WithTitle_SendsCorrectRequestBody()
    {
        // Arrange
        var json = """{"id": "sess_abc123", "title": "Test Title", "createdAt": 1709827200000, "updatedAt": 1709827200000}""";
        var httpClient = CreateCapturingHttpClient(out var handler, json);
        var sut = CreateSut(httpClient);

        // Act
        await sut.CreateSessionAsync(BaseUrl, "Test Title", CancellationToken.None);

        // Assert
        handler.CapturedRequestBody.ShouldNotBeNull();
        handler.CapturedRequestBody.ShouldContain("\"title\":\"Test Title\"");
    }

    [Fact]
    public async Task CreateSessionAsync_WithoutTitle_SendsNullTitle()
    {
        // Arrange
        var json = """{"id": "sess_abc123", "title": null, "createdAt": 1709827200000, "updatedAt": 1709827200000}""";
        var httpClient = CreateCapturingHttpClient(out var handler, json);
        var sut = CreateSut(httpClient);

        // Act
        await sut.CreateSessionAsync(BaseUrl, null, CancellationToken.None);

        // Assert
        handler.CapturedRequestBody.ShouldNotBeNull();
        handler.CapturedRequestBody.ShouldContain("\"title\":null");
    }

    // ──────────────────────────────────────────────
    // 2. ListSessions — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ListSessionsAsync_ValidResponse_ReturnsSuccessWithSessionList()
    {
        // Arrange
        var json = """[{"id": "sess_1", "title": "First", "createdAt": 1709827200000, "updatedAt": 1709827200000}, {"id": "sess_2", "title": "Second", "createdAt": 1709827300000, "updatedAt": 1709827300000}]""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.ListSessionsAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.Count.ShouldBe(2),
            () => result.Value[0].Id.ShouldBe("sess_1"),
            () => result.Value[0].Title.ShouldBe("First"),
            () => result.Value[0].CreatedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1709827200000)),
            () => result.Value[1].Id.ShouldBe("sess_2"),
            () => result.Value[1].Title.ShouldBe("Second"),
            () => result.Value[1].CreatedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1709827300000))
        );
    }

    [Fact]
    public async Task ListSessionsAsync_EmptyArray_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "[]");
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.ListSessionsAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListSessionsAsync_SessionWithMissingId_SkipsInvalidSession()
    {
        // Arrange — array contains one valid session and one with null ID
        var json = """[{"id": null, "title": "Bad", "createdAt": 1709827200000, "updatedAt": 1709827200000}, {"id": "sess_valid", "title": "Good", "createdAt": 1709827300000, "updatedAt": 1709827300000}]""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.ListSessionsAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.Count.ShouldBe(1),
            () => result.Value[0].Id.ShouldBe("sess_valid"),
            () => result.Value[0].Title.ShouldBe("Good")
        );
    }

    // ──────────────────────────────────────────────
    // 3. GetSession — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetSessionAsync_ValidResponse_ReturnsSuccessWithSessionDto()
    {
        // Arrange
        var json = """{"id": "sess_abc123", "title": "My Session", "createdAt": 1709827200000, "updatedAt": 1709827200000}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.GetSessionAsync(BaseUrl, "sess_abc123", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.Id.ShouldBe("sess_abc123"),
            () => result.Value.Title.ShouldBe("My Session"),
            () => result.Value.CreatedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1709827200000)),
            () => result.Value.UpdatedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1709827200000))
        );
    }

    [Fact]
    public async Task GetSessionAsync_ConstructsCorrectUri()
    {
        // Arrange
        var json = """{"id": "sess_abc123", "title": "Test", "createdAt": 1709827200000, "updatedAt": 1709827200000}""";
        var httpClient = CreateCapturingHttpClient(out var handler, json);
        var sut = CreateSut(httpClient);

        // Act
        await sut.GetSessionAsync(BaseUrl, "sess_abc123", CancellationToken.None);

        // Assert
        handler.CapturedRequest.ShouldNotBeNull();
        handler.CapturedRequest!.RequestUri!.ToString().ShouldBe("http://localhost:4096/session/sess_abc123");
        handler.CapturedRequest.Method.ShouldBe(HttpMethod.Get);
    }

    // ──────────────────────────────────────────────
    // 4. DeleteSession — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteSessionAsync_Success_ReturnsSuccessResult()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.DeleteSessionAsync(BaseUrl, "sess_abc123", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteSessionAsync_ConstructsCorrectUri()
    {
        // Arrange
        var httpClient = CreateCapturingHttpClient(out var handler);
        var sut = CreateSut(httpClient);

        // Act
        await sut.DeleteSessionAsync(BaseUrl, "sess_abc123", CancellationToken.None);

        // Assert
        handler.CapturedRequest.ShouldNotBeNull();
        handler.CapturedRequest!.ShouldSatisfyAllConditions(
            () => handler.CapturedRequest!.RequestUri!.ToString().ShouldBe("http://localhost:4096/session/sess_abc123"),
            () => handler.CapturedRequest!.Method.ShouldBe(HttpMethod.Delete)
        );
    }

    // ──────────────────────────────────────────────
    // 5. AbortSession — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task AbortSessionAsync_Success_ReturnsSuccessResult()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.AbortSessionAsync(BaseUrl, "sess_abc123", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AbortSessionAsync_ConstructsCorrectUri()
    {
        // Arrange
        var httpClient = CreateCapturingHttpClient(out var handler);
        var sut = CreateSut(httpClient);

        // Act
        await sut.AbortSessionAsync(BaseUrl, "sess_abc123", CancellationToken.None);

        // Assert
        handler.CapturedRequest.ShouldNotBeNull();
        handler.CapturedRequest!.ShouldSatisfyAllConditions(
            () => handler.CapturedRequest!.RequestUri!.ToString().ShouldBe("http://localhost:4096/session/sess_abc123/abort"),
            () => handler.CapturedRequest!.Method.ShouldBe(HttpMethod.Post)
        );
    }

    // ──────────────────────────────────────────────
    // 6. UpdateSession — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateSessionAsync_ValidResponse_ReturnsSuccessWithUpdatedDto()
    {
        // Arrange
        var json = """{"id": "sess_abc123", "title": "Updated Title", "createdAt": 1709827200000, "updatedAt": 1709827400000}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.UpdateSessionAsync(BaseUrl, "sess_abc123", "Updated Title", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.Id.ShouldBe("sess_abc123"),
            () => result.Value.Title.ShouldBe("Updated Title"),
            () => result.Value.CreatedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1709827200000)),
            () => result.Value.UpdatedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1709827400000))
        );
    }

    [Fact]
    public async Task UpdateSessionAsync_ConstructsCorrectUriAndMethod()
    {
        // Arrange
        var json = """{"id": "sess_abc123", "title": "New Title", "createdAt": 1709827200000, "updatedAt": 1709827200000}""";
        var httpClient = CreateCapturingHttpClient(out var handler, json);
        var sut = CreateSut(httpClient);

        // Act
        await sut.UpdateSessionAsync(BaseUrl, "sess_abc123", "New Title", CancellationToken.None);

        // Assert
        handler.CapturedRequest.ShouldNotBeNull();
        handler.CapturedRequest!.ShouldSatisfyAllConditions(
            () => handler.CapturedRequest!.RequestUri!.ToString().ShouldBe("http://localhost:4096/session/sess_abc123"),
            () => handler.CapturedRequest!.Method.ShouldBe(HttpMethod.Patch)
        );
    }

    // ──────────────────────────────────────────────
    // 7. GetSessionStatuses — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetSessionStatusesAsync_ValidResponse_ReturnsSuccessWithDictionary()
    {
        // Arrange
        var json = """{"sess_1": {"status": "idle"}, "sess_2": {"status": "running"}}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.GetSessionStatusesAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.Count.ShouldBe(2),
            () => result.Value["sess_1"].Status.ShouldBe("idle"),
            () => result.Value["sess_2"].Status.ShouldBe("running")
        );
    }

    [Fact]
    public async Task GetSessionStatusesAsync_EmptyResponse_ReturnsEmptyDictionary()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "{}");
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.GetSessionStatusesAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSessionStatusesAsync_NullStatus_DefaultsToUnknown()
    {
        // Arrange — a status entry with null status field
        var json = """{"sess_1": {"status": null}}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.GetSessionStatusesAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value["sess_1"].Status.ShouldBe("unknown");
    }

    // ──────────────────────────────────────────────
    // 8. Session ID validation
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetSessionAsync_NullSessionId_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.GetSessionAsync(BaseUrl, null!, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Session ID is required")
        );
    }

    [Fact]
    public async Task GetSessionAsync_EmptySessionId_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.GetSessionAsync(BaseUrl, "", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Session ID is required")
        );
    }

    [Fact]
    public async Task DeleteSessionAsync_NullSessionId_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.DeleteSessionAsync(BaseUrl, null!, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Session ID is required")
        );
    }

    [Fact]
    public async Task AbortSessionAsync_NullSessionId_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.AbortSessionAsync(BaseUrl, null!, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Session ID is required")
        );
    }

    [Fact]
    public async Task UpdateSessionAsync_NullSessionId_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.UpdateSessionAsync(BaseUrl, null!, "Some Title", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Session ID is required")
        );
    }

    // ──────────────────────────────────────────────
    // 9. HTTP error status codes
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateSessionAsync_ServerReturns500_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.InternalServerError);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.CreateSessionAsync(BaseUrl, "Test", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Server error (500)")
        );
    }

    [Fact]
    public async Task CreateSessionAsync_ServerReturns404_ReturnsFailure()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.NotFound);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.CreateSessionAsync(BaseUrl, "Test", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Server error (404)")
        );
    }

    // ──────────────────────────────────────────────
    // 10. Network / connection errors
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateSessionAsync_ConnectionRefused_ReturnsFailure()
    {
        // Arrange
        var socketException = new SocketException((int)SocketError.ConnectionRefused);
        var httpException = new HttpRequestException("Connection refused", socketException);
        var httpClient = CreateThrowingHttpClient(httpException);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.CreateSessionAsync(BaseUrl, "Test", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Connection refused")
        );
    }

    [Fact]
    public async Task CreateSessionAsync_NetworkError_ReturnsFailure()
    {
        // Arrange
        var httpException = new HttpRequestException("Name resolution failed");
        var httpClient = CreateThrowingHttpClient(httpException);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.CreateSessionAsync(BaseUrl, "Test", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Network error")
        );
    }

    [Fact]
    public async Task CreateSessionAsync_Timeout_ReturnsFailure()
    {
        // Arrange — TaskCanceledException NOT caused by the caller's token simulates an HTTP timeout
        var timeoutException = new TaskCanceledException(
            "The request was canceled due to the configured HttpClient.Timeout",
            new TimeoutException());
        var httpClient = CreateThrowingHttpClient(timeoutException);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.CreateSessionAsync(BaseUrl, "Test", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Request timed out")
        );
    }

    // ──────────────────────────────────────────────
    // 11. Invalid responses
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateSessionAsync_InvalidJson_ReturnsFailure()
    {
        // Arrange — HTTP 200 with non-JSON body
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "this is not json");
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.CreateSessionAsync(BaseUrl, "Test", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Unexpected response format")
        );
    }

    [Fact]
    public async Task CreateSessionAsync_NullResponseBody_ReturnsFailure()
    {
        // Arrange — Server returns 200 with JSON literal "null"
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "null");
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.CreateSessionAsync(BaseUrl, "Test", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Unexpected response format")
        );
    }

    [Fact]
    public async Task CreateSessionAsync_MissingSessionId_ReturnsFailure()
    {
        // Arrange — Server returns 200 with a session that has null ID
        var json = """{"id": null, "title": "Test", "createdAt": 1709827200000, "updatedAt": 1709827200000}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.CreateSessionAsync(BaseUrl, "Test", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("missing a required ID")
        );
    }

    // ──────────────────────────────────────────────
    // 12. Malformed URL
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateSessionAsync_MalformedUrl_ReturnsFailure()
    {
        // Arrange — a URL that cannot be parsed by the Uri constructor
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.CreateSessionAsync("not-a-url://[invalid", "Test", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Invalid server URL format.")
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
