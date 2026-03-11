namespace OwlNet.Domain.Common;

/// <summary>
/// Represents the outcome of a domain operation that does not return a value.
/// Use <see cref="Success"/> and <see cref="Failure"/> factory methods to create instances.
/// </summary>
/// <remarks>
/// This type is intentionally separate from <c>OwlNet.Application.Common.Models.Result</c>
/// to preserve the Domain layer's zero-dependency constraint. Domain entities and methods
/// that need to communicate success/failure use <see cref="DomainResult"/> instead.
/// </remarks>
public sealed class DomainResult
{
    private DomainResult(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message describing why the operation failed.
    /// Returns <see cref="string.Empty"/> when <see cref="IsSuccess"/> is <see langword="true"/>.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Creates a successful <see cref="DomainResult"/>.
    /// </summary>
    /// <returns>A <see cref="DomainResult"/> representing a successful operation.</returns>
    public static DomainResult Success() => new(true, string.Empty);

    /// <summary>
    /// Creates a failed <see cref="DomainResult"/> with the specified error message.
    /// </summary>
    /// <param name="error">A non-empty error message describing the failure.</param>
    /// <returns>A <see cref="DomainResult"/> representing a failed operation.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="error"/> is <see langword="null"/> or whitespace.
    /// </exception>
    public static DomainResult Failure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new(false, error);
    }
}
