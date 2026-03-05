using System.Net;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="ILlmProviderService"/> that orchestrates
/// LLM provider configuration persistence, API key encryption/decryption, and connection
/// verification against the OpenRouter API. All methods return result objects and never
/// throw exceptions to the caller.
/// </summary>
public sealed class LlmProviderService : ILlmProviderService
{
    private const string ApiKeySettingKey = "LlmProvider:ApiKey";
    private const string ModelIdSettingKey = "LlmProvider:ModelId";

    private readonly IAppSettingService _appSettingService;
    private readonly IEncryptionService _encryptionService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<LlmProviderService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmProviderService"/> class.
    /// </summary>
    /// <param name="appSettingService">The application settings service for reading and writing configuration values.</param>
    /// <param name="encryptionService">The encryption service for protecting and unprotecting API keys.</param>
    /// <param name="httpClient">The HTTP client configured for OpenRouter API communication.</param>
    /// <param name="logger">The logger instance for structured diagnostic output.</param>
    public LlmProviderService(
        IAppSettingService appSettingService,
        IEncryptionService encryptionService,
        HttpClient httpClient,
        ILogger<LlmProviderService> logger)
    {
        _appSettingService = appSettingService;
        _encryptionService = encryptionService;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<LlmProviderConfigDto>> GetConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving LLM provider configuration");

            var apiKeyResult = await _appSettingService.GetByKeyAsync(ApiKeySettingKey, cancellationToken);
            var modelIdResult = await _appSettingService.GetByKeyAsync(ModelIdSettingKey, cancellationToken);

            string? decryptedApiKey = null;
            var decryptionFailed = false;

            if (apiKeyResult.IsSuccess)
            {
                if (!_encryptionService.TryDecrypt(apiKeyResult.Value, out decryptedApiKey))
                {
                    _logger.LogWarning(
                        "Failed to decrypt stored API key — the data protection key may have changed");
                    decryptionFailed = true;
                    decryptedApiKey = null;
                }
            }

            var modelId = modelIdResult.IsSuccess ? modelIdResult.Value : null;

            var isConfigured = !decryptionFailed
                && !string.IsNullOrEmpty(decryptedApiKey)
                && !string.IsNullOrEmpty(modelId);

            var dto = new LlmProviderConfigDto(
                decryptedApiKey,
                modelId,
                isConfigured,
                decryptionFailed);

            _logger.LogInformation(
                "LLM provider configuration retrieved (IsConfigured: {IsConfigured}, DecryptionFailed: {DecryptionFailed})",
                isConfigured,
                decryptionFailed);

            return Result<LlmProviderConfigDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving LLM provider configuration");
            return Result<LlmProviderConfigDto>.Failure(
                "An unexpected error occurred while retrieving LLM provider configuration.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveConfigurationAsync(
        string apiKey,
        string modelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Saving LLM provider configuration");

            var encryptedApiKey = _encryptionService.Encrypt(apiKey);

            var settings = new List<KeyValuePair<string, string>>
            {
                new(ApiKeySettingKey, encryptedApiKey),
                new(ModelIdSettingKey, modelId)
            };

            var saveResult = await _appSettingService.SaveBatchAsync(settings, cancellationToken);

            if (saveResult.IsFailure)
            {
                _logger.LogError("Failed to save LLM provider configuration: {Error}", saveResult.Error);
                return saveResult;
            }

            _logger.LogInformation("LLM provider configuration saved successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while saving LLM provider configuration");
            return Result.Failure(
                "An unexpected error occurred while saving LLM provider configuration.");
        }
    }

    /// <inheritdoc />
    public async Task<LlmConnectionVerificationResult> VerifyConnectionAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Verifying LLM provider connection");

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "models");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", apiKey);

            using var response = await _httpClient.SendAsync(request, linkedCts.Token);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("LLM provider connection verified successfully");
                return LlmConnectionVerificationResult.Success();
            }

            var errorMessage = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Invalid API key.",
                HttpStatusCode.Forbidden => "Access denied. Check API key permissions.",
                _ => $"Unexpected error: HTTP {(int)response.StatusCode}."
            };

            _logger.LogWarning(
                "LLM provider connection verification failed with HTTP {StatusCode}: {ErrorMessage}",
                (int)response.StatusCode,
                errorMessage);

            return LlmConnectionVerificationResult.Failure(errorMessage);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("LLM provider connection verification was cancelled by the caller");
            return LlmConnectionVerificationResult.Failure("Verification was cancelled.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("LLM provider connection verification timed out");
            return LlmConnectionVerificationResult.Failure("Timeout. Try again later.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "LLM provider connection verification failed due to a network error");
            return LlmConnectionVerificationResult.Failure(
                "Unable to reach OpenRouter. Check your internet connection.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unexpected error occurred during LLM provider connection verification");
            return LlmConnectionVerificationResult.Failure(
                "An unexpected error occurred during verification.");
        }
    }
}
