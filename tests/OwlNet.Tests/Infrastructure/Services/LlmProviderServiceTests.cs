using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Infrastructure.Services;
using Shouldly;

namespace OwlNet.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for <see cref="LlmProviderService"/>. All external dependencies
/// (IAppSettingService, IEncryptionService, HttpClient) are substituted or faked.
/// </summary>
public sealed class LlmProviderServiceTests
{
    private readonly IAppSettingService _appSettingService;
    private readonly IEncryptionService _encryptionService;

    public LlmProviderServiceTests()
    {
        _appSettingService = Substitute.For<IAppSettingService>();
        _encryptionService = Substitute.For<IEncryptionService>();
    }

    /// <summary>
    /// Creates a <see cref="LlmProviderService"/> with the shared substitutes and the given HTTP client.
    /// </summary>
    private LlmProviderService CreateSut(HttpClient? httpClient = null)
    {
        httpClient ??= CreateHttpClient(HttpStatusCode.OK);

        return new LlmProviderService(
            _appSettingService,
            _encryptionService,
            httpClient,
            NullLogger<LlmProviderService>.Instance);
    }

    // ──────────────────────────────────────────────
    // HTTP helpers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Creates an <see cref="HttpClient"/> backed by a handler that returns the specified status code.
    /// </summary>
    private static HttpClient CreateHttpClient(HttpStatusCode statusCode)
    {
        var handler = new FakeHttpMessageHandler(statusCode);
        return new HttpClient(handler) { BaseAddress = new Uri("https://openrouter.ai/api/v1/") };
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> backed by a handler that throws <see cref="HttpRequestException"/>.
    /// </summary>
    private static HttpClient CreateThrowingHttpClient()
    {
        var handler = new ThrowingHttpMessageHandler();
        return new HttpClient(handler) { BaseAddress = new Uri("https://openrouter.ai/api/v1/") };
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> backed by a handler that captures the outgoing request for inspection.
    /// </summary>
    private static HttpClient CreateCapturingHttpClient(out CapturingHttpMessageHandler handler)
    {
        handler = new CapturingHttpMessageHandler();
        return new HttpClient(handler) { BaseAddress = new Uri("https://openrouter.ai/api/v1/") };
    }

    // ──────────────────────────────────────────────
    // GetConfigurationAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetConfigurationAsync_BothSettingsExist_ReturnsConfiguredDto()
    {
        // Arrange
        var encryptedApiKey = "encrypted-api-key";
        var decryptedApiKey = "sk-test-key-12345";
        var modelId = "anthropic/claude-sonnet-4";

        _appSettingService.GetByKeyAsync("LlmProvider:ApiKey", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success(encryptedApiKey)));

        _appSettingService.GetByKeyAsync("LlmProvider:ModelId", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success(modelId)));

        _encryptionService.TryDecrypt(encryptedApiKey, out Arg.Any<string?>())
            .Returns(x => { x[1] = decryptedApiKey; return true; });

        var sut = CreateSut();

        // Act
        var result = await sut.GetConfigurationAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.IsConfigured.ShouldBeTrue(),
            () => result.Value.DecryptionFailed.ShouldBeFalse(),
            () => result.Value.ApiKey.ShouldBe(decryptedApiKey),
            () => result.Value.ModelId.ShouldBe(modelId)
        );
    }

    [Fact]
    public async Task GetConfigurationAsync_NoSettingsExist_ReturnsNotConfiguredDto()
    {
        // Arrange
        _appSettingService.GetByKeyAsync("LlmProvider:ApiKey", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure("Setting not found: LlmProvider:ApiKey")));

        _appSettingService.GetByKeyAsync("LlmProvider:ModelId", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure("Setting not found: LlmProvider:ModelId")));

        var sut = CreateSut();

        // Act
        var result = await sut.GetConfigurationAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.IsConfigured.ShouldBeFalse(),
            () => result.Value.ApiKey.ShouldBeNull(),
            () => result.Value.ModelId.ShouldBeNull(),
            () => result.Value.DecryptionFailed.ShouldBeFalse()
        );
    }

    [Fact]
    public async Task GetConfigurationAsync_DecryptionFails_ReturnsDecryptionFailedDto()
    {
        // Arrange
        var encryptedApiKey = "corrupted-encrypted-data";
        var modelId = "anthropic/claude-sonnet-4";

        _appSettingService.GetByKeyAsync("LlmProvider:ApiKey", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success(encryptedApiKey)));

        _appSettingService.GetByKeyAsync("LlmProvider:ModelId", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success(modelId)));

        _encryptionService.TryDecrypt(encryptedApiKey, out Arg.Any<string?>())
            .Returns(x => { x[1] = null; return false; });

        var sut = CreateSut();

        // Act
        var result = await sut.GetConfigurationAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.DecryptionFailed.ShouldBeTrue(),
            () => result.Value.IsConfigured.ShouldBeFalse(),
            () => result.Value.ApiKey.ShouldBeNull(),
            () => result.Value.ModelId.ShouldBe(modelId)
        );
    }

    [Fact]
    public async Task GetConfigurationAsync_OnlyApiKeyExists_ReturnsNotConfigured()
    {
        // Arrange
        var encryptedApiKey = "encrypted-api-key";
        var decryptedApiKey = "sk-test-key-12345";

        _appSettingService.GetByKeyAsync("LlmProvider:ApiKey", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success(encryptedApiKey)));

        _appSettingService.GetByKeyAsync("LlmProvider:ModelId", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure("Setting not found: LlmProvider:ModelId")));

        _encryptionService.TryDecrypt(encryptedApiKey, out Arg.Any<string?>())
            .Returns(x => { x[1] = decryptedApiKey; return true; });

        var sut = CreateSut();

        // Act
        var result = await sut.GetConfigurationAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldSatisfyAllConditions(
            () => result.Value.IsConfigured.ShouldBeFalse(),
            () => result.Value.ApiKey.ShouldBe(decryptedApiKey),
            () => result.Value.ModelId.ShouldBeNull(),
            () => result.Value.DecryptionFailed.ShouldBeFalse()
        );
    }

    // ──────────────────────────────────────────────
    // SaveConfigurationAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SaveConfigurationAsync_ValidInput_EncryptsAndSavesBothAtomically()
    {
        // Arrange
        var apiKey = "sk-test-key-12345";
        var modelId = "anthropic/claude-sonnet-4";
        var encryptedApiKey = "encrypted-api-key";

        _encryptionService.Encrypt(apiKey).Returns(encryptedApiKey);

        _appSettingService.SaveBatchAsync(Arg.Any<IReadOnlyList<KeyValuePair<string, string>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var sut = CreateSut();

        // Act
        var result = await sut.SaveConfigurationAsync(apiKey, modelId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        _encryptionService.Received(1).Encrypt(apiKey);
        await _appSettingService.Received(1).SaveBatchAsync(
            Arg.Is<IReadOnlyList<KeyValuePair<string, string>>>(settings =>
                settings.Count == 2
                && settings.Any(s => s.Key == "LlmProvider:ApiKey" && s.Value == encryptedApiKey)
                && settings.Any(s => s.Key == "LlmProvider:ModelId" && s.Value == modelId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveConfigurationAsync_BatchSaveFails_ReturnsFailure()
    {
        // Arrange
        var apiKey = "sk-test-key-12345";
        var modelId = "anthropic/claude-sonnet-4";
        var encryptedApiKey = "encrypted-api-key";

        _encryptionService.Encrypt(apiKey).Returns(encryptedApiKey);

        _appSettingService.SaveBatchAsync(Arg.Any<IReadOnlyList<KeyValuePair<string, string>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure("A database error occurred while saving settings.")));

        var sut = CreateSut();

        // Act
        var result = await sut.SaveConfigurationAsync(apiKey, modelId, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("database error")
        );
    }

    // ──────────────────────────────────────────────
    // VerifyConnectionAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task VerifyConnectionAsync_HttpOk_ReturnsSuccess()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.OK);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.VerifyConnectionAsync("sk-test-key", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.ErrorMessage.ShouldBeNull()
        );
    }

    [Fact]
    public async Task VerifyConnectionAsync_HttpUnauthorized_ReturnsInvalidApiKey()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.Unauthorized);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.VerifyConnectionAsync("sk-invalid-key", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.ErrorMessage.ShouldBe("Invalid API key.")
        );
    }

    [Fact]
    public async Task VerifyConnectionAsync_HttpForbidden_ReturnsAccessDenied()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.Forbidden);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.VerifyConnectionAsync("sk-forbidden-key", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.ErrorMessage.ShouldBe("Access denied. Check API key permissions.")
        );
    }

    [Fact]
    public async Task VerifyConnectionAsync_UnexpectedStatusCode_ReturnsHttpStatusError()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.InternalServerError);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.VerifyConnectionAsync("sk-test-key", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.ErrorMessage.ShouldBe("Unexpected error: HTTP 500.")
        );
    }

    [Fact]
    public async Task VerifyConnectionAsync_NetworkError_ReturnsConnectionError()
    {
        // Arrange
        var httpClient = CreateThrowingHttpClient();
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.VerifyConnectionAsync("sk-test-key", CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.ErrorMessage.ShouldBe("Unable to reach OpenRouter. Check your internet connection.")
        );
    }

    [Fact]
    public async Task VerifyConnectionAsync_SetsAuthorizationHeader()
    {
        // Arrange
        var apiKey = "sk-my-secret-api-key";
        var httpClient = CreateCapturingHttpClient(out var handler);
        var sut = CreateSut(httpClient);

        // Act
        await sut.VerifyConnectionAsync(apiKey, CancellationToken.None);

        // Assert
        handler.CapturedRequest.ShouldNotBeNull();
        handler.CapturedRequest!.Headers.Authorization.ShouldSatisfyAllConditions(
            () => handler.CapturedRequest.Headers.Authorization.ShouldNotBeNull(),
            () => handler.CapturedRequest.Headers.Authorization!.Scheme.ShouldBe("Bearer"),
            () => handler.CapturedRequest.Headers.Authorization!.Parameter.ShouldBe(apiKey)
        );
    }

    [Fact]
    public async Task VerifyConnectionAsync_SendsGetRequestToModelsEndpoint()
    {
        // Arrange
        var httpClient = CreateCapturingHttpClient(out var handler);
        var sut = CreateSut(httpClient);

        // Act
        await sut.VerifyConnectionAsync("sk-test-key", CancellationToken.None);

        // Assert
        handler.CapturedRequest.ShouldNotBeNull();
        handler.CapturedRequest!.ShouldSatisfyAllConditions(
            () => handler.CapturedRequest.Method.ShouldBe(HttpMethod.Get),
            () => handler.CapturedRequest.RequestUri!.ToString().ShouldContain("models")
        );
    }

    // ──────────────────────────────────────────────
    // Fake HTTP message handlers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Returns a fixed <see cref="HttpStatusCode"/> for every request.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public FakeHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }

    /// <summary>
    /// Throws <see cref="HttpRequestException"/> for every request, simulating a network failure.
    /// </summary>
    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Simulated network failure");
        }
    }

    /// <summary>
    /// Captures the outgoing <see cref="HttpRequestMessage"/> for assertion and returns HTTP 200.
    /// </summary>
    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Clone the essential parts since the original request may be disposed
            CapturedRequest = new HttpRequestMessage(request.Method, request.RequestUri);
            if (request.Headers.Authorization is not null)
            {
                CapturedRequest.Headers.Authorization = new AuthenticationHeaderValue(
                    request.Headers.Authorization.Scheme,
                    request.Headers.Authorization.Parameter);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
