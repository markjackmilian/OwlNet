using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Application service for reading and writing global application settings.
/// Settings are stored as key-value pairs and are application-wide (not per-user).
/// </summary>
public interface IAppSettingService
{
    /// <summary>
    /// Retrieves the value of a setting identified by <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The unique key of the setting to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the setting value on success,
    /// or a failure result if the key is not found.
    /// </returns>
    Task<Result<string>> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all application settings.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a read-only list of <see cref="AppSettingDto"/>
    /// representing every persisted setting.
    /// </returns>
    Task<Result<IReadOnlyList<AppSettingDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a setting with the specified <paramref name="key"/> and <paramref name="value"/>.
    /// If the key already exists the value is updated; otherwise a new setting is created.
    /// </summary>
    /// <param name="key">The unique key of the setting to save.</param>
    /// <param name="value">The value to persist. An empty string is allowed.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure of the save operation.
    /// </returns>
    Task<Result> SaveAsync(string key, string value, CancellationToken cancellationToken = default);
}
