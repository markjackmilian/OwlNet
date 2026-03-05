using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;
using OwlNet.Infrastructure.Persistence;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IAppSettingService"/> that persists
/// application settings as key-value pairs in the database via Entity Framework Core.
/// All methods return <see cref="Result"/> or <see cref="Result{T}"/> and never throw
/// exceptions to the caller.
/// </summary>
public sealed class AppSettingService : IAppSettingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AppSettingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppSettingService"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="logger">The logger instance for structured diagnostic output.</param>
    public AppSettingService(
        ApplicationDbContext dbContext,
        ILogger<AppSettingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving setting with key {SettingKey}", key);

            var setting = await _dbContext.AppSettings
                .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

            if (setting is null)
            {
                _logger.LogWarning("Setting with key {SettingKey} was not found", key);
                return Result<string>.Failure($"Setting with key '{key}' was not found.");
            }

            _logger.LogDebug(
                "Successfully retrieved setting {SettingKey}", key);

            return Result<string>.Success(setting.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve setting {SettingKey}", key);
            return Result<string>.Failure(
                $"An error occurred while retrieving setting '{key}'.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AppSettingDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving all application settings");

            var settings = await _dbContext.AppSettings
                .AsNoTracking()
                .OrderBy(s => s.Key)
                .Select(s => new AppSettingDto(s.Key, s.Value, s.UpdatedAt))
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully retrieved {SettingCount} application settings",
                settings.Count);

            return Result<IReadOnlyList<AppSettingDto>>.Success(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve application settings");
            return Result<IReadOnlyList<AppSettingDto>>.Failure(
                "An error occurred while retrieving application settings.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Saving setting {SettingKey}", key);

            var existing = await _dbContext.AppSettings
                .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

            if (existing is not null)
            {
                _logger.LogDebug(
                    "Setting {SettingKey} already exists, updating value", key);
                existing.UpdateValue(value);
            }
            else
            {
                _logger.LogDebug(
                    "Setting {SettingKey} does not exist, creating new entry", key);
                var setting = AppSetting.Create(key, value);
                _dbContext.AppSettings.Add(setting);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully saved setting {SettingKey}", key);

            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while saving setting {SettingKey}", key);
            return Result.Failure(
                $"A database error occurred while saving setting '{key}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save setting {SettingKey}", key);
            return Result.Failure(
                $"An unexpected error occurred while saving setting '{key}'.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveBatchAsync(
        IReadOnlyList<KeyValuePair<string, string>> settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Saving batch of {SettingCount} settings", settings.Count);

            foreach (var (key, value) in settings)
            {
                var existing = await _dbContext.AppSettings
                    .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

                if (existing is not null)
                {
                    _logger.LogDebug(
                        "Setting {SettingKey} already exists, updating value", key);
                    existing.UpdateValue(value);
                }
                else
                {
                    _logger.LogDebug(
                        "Setting {SettingKey} does not exist, creating new entry", key);
                    var setting = AppSetting.Create(key, value);
                    _dbContext.AppSettings.Add(setting);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully saved batch of {SettingCount} settings", settings.Count);

            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while saving batch of {SettingCount} settings",
                settings.Count);
            return Result.Failure("A database error occurred while saving settings.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save batch of {SettingCount} settings",
                settings.Count);
            return Result.Failure("An unexpected error occurred while saving settings.");
        }
    }
}
