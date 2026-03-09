using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using OwlNet.Application.Common.Models;
using OwlNet.Infrastructure.Services;
using Shouldly;

namespace OwlNet.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for <see cref="OpenCodeClient"/>. The HTTP layer is faked using custom
/// <see cref="HttpMessageHandler"/> implementations — no real network calls are made.
/// </summary>
public sealed class OpenCodeClientTests
{
    private const string BaseUrl = "http://localhost:4096";

    /// <summary>
    /// Creates an <see cref="OpenCodeClient"/> backed by the given <see cref="HttpClient"/>.
    /// </summary>
    private static OpenCodeClient CreateSut(HttpClient httpClient)
    {
        return new OpenCodeClient(httpClient, NullLogger<OpenCodeClient>.Instance);
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
    /// for inspection and returns HTTP 200 with the specified JSON content.
    /// </summary>
    private static HttpClient CreateCapturingHttpClient(out CapturingHttpMessageHandler handler, string? jsonContent = null)
    {
        handler = new CapturingHttpMessageHandler(jsonContent);
        return new HttpClient(handler);
    }

    // ──────────────────────────────────────────────
    // Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task HealthCheckAsync_ServerHealthy_ReturnsSuccessWithHealthResult()
    {
        // Arrange
        var json = """{"healthy": true, "version": "1.2.20"}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.HealthCheckAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.IsHealthy.ShouldBeTrue(),
            () => result.Value.Version.ShouldBe("1.2.20")
        );
    }

    [Fact]
    public async Task HealthCheckAsync_ServerUnhealthy_ReturnsSuccessWithUnhealthyResult()
    {
        // Arrange
        var json = """{"healthy": false, "version": "1.2.20"}""";
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.HealthCheckAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.IsHealthy.ShouldBeFalse(),
            () => result.Value.Version.ShouldBe("1.2.20")
        );
    }

    // ──────────────────────────────────────────────
    // HTTP error status codes
    // ──────────────────────────────────────────────

    [Fact]
    public async Task HealthCheckAsync_ServerReturns500_ReturnsFailureWithStatusCode()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.InternalServerError);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.HealthCheckAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Server error (500)")
        );
    }

    [Fact]
    public async Task HealthCheckAsync_ServerReturns404_ReturnsFailureWithStatusCode()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.NotFound);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.HealthCheckAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Server error (404)")
        );
    }

    // ──────────────────────────────────────────────
    // Network / connection errors
    // ──────────────────────────────────────────────

    [Fact]
    public async Task HealthCheckAsync_ConnectionRefused_ReturnsConnectionRefusedFailure()
    {
        // Arrange
        var socketException = new SocketException((int)SocketError.ConnectionRefused);
        var httpException = new HttpRequestException("Connection refused", socketException);
        var httpClient = CreateThrowingHttpClient(httpException);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.HealthCheckAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Connection refused")
        );
    }

    [Fact]
    public async Task HealthCheckAsync_NetworkError_ReturnsNetworkErrorFailure()
    {
        // Arrange
        var httpException = new HttpRequestException("Name resolution failed");
        var httpClient = CreateThrowingHttpClient(httpException);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.HealthCheckAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("Network error")
        );
    }

    // ──────────────────────────────────────────────
    // Timeout
    // ──────────────────────────────────────────────

    [Fact]
    public async Task HealthCheckAsync_Timeout_ReturnsTimeoutFailure()
    {
        // Arrange — TaskCanceledException NOT caused by the caller's token simulates an HTTP timeout
        var timeoutException = new TaskCanceledException(
            "The request was canceled due to the configured HttpClient.Timeout",
            new TimeoutException());
        var httpClient = CreateThrowingHttpClient(timeoutException);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.HealthCheckAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Request timed out")
        );
    }

    // ──────────────────────────────────────────────
    // Invalid / malformed responses
    // ──────────────────────────────────────────────

    [Fact]
    public async Task HealthCheckAsync_InvalidJson_ReturnsUnexpectedFormatFailure()
    {
        // Arrange — HTTP 200 with non-JSON body
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "this is not json");
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.HealthCheckAsync(BaseUrl, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Unexpected response format")
        );
    }

    // ──────────────────────────────────────────────
    // Malformed URL
    // ──────────────────────────────────────────────

    [Fact]
    public async Task HealthCheckAsync_MalformedUrl_ReturnsInvalidUrlFailure()
    {
        // Arrange — a URL that cannot be parsed by the Uri constructor
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.HealthCheckAsync("not-a-url://[invalid", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("Invalid server URL format.")
        );
    }

    // ──────────────────────────────────────────────
    // URI construction
    // ──────────────────────────────────────────────

    [Fact]
    public async Task HealthCheckAsync_ConstructsCorrectUri()
    {
        // Arrange
        var json = """{"healthy": true, "version": "1.0.0"}""";
        var httpClient = CreateCapturingHttpClient(out var handler, json);
        var sut = CreateSut(httpClient);

        // Act
        await sut.HealthCheckAsync("http://localhost:4096", CancellationToken.None);

        // Assert
        handler.CapturedRequest.ShouldNotBeNull();
        handler.CapturedRequest!.RequestUri!.ToString().ShouldBe("http://localhost:4096/global/health");
    }

    [Fact]
    public async Task HealthCheckAsync_TrimsTrailingSlash()
    {
        // Arrange — baseUrl has a trailing slash; the resulting URI must NOT have a double slash
        var json = """{"healthy": true, "version": "1.0.0"}""";
        var httpClient = CreateCapturingHttpClient(out var handler, json);
        var sut = CreateSut(httpClient);

        // Act
        await sut.HealthCheckAsync("http://localhost:4096/", CancellationToken.None);

        // Assert
        handler.CapturedRequest.ShouldNotBeNull();
        handler.CapturedRequest!.RequestUri!.ToString().ShouldBe("http://localhost:4096/global/health");
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
    /// Captures the outgoing <see cref="HttpRequestMessage"/> for assertion and returns HTTP 200
    /// with optional JSON content.
    /// </summary>
    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly string? _jsonContent;

        public HttpRequestMessage? CapturedRequest { get; private set; }

        public CapturingHttpMessageHandler(string? jsonContent = null)
        {
            _jsonContent = jsonContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CapturedRequest = new HttpRequestMessage(request.Method, request.RequestUri);

            var response = new HttpResponseMessage(HttpStatusCode.OK);

            if (_jsonContent is not null)
            {
                response.Content = new StringContent(_jsonContent, System.Text.Encoding.UTF8, "application/json");
            }

            return Task.FromResult(response);
        }
    }
}
