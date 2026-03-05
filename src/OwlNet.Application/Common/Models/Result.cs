namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents the outcome of an operation that does not return a value.
/// Use <see cref="Success"/> and <see cref="Failure"/> factory methods to create instances.
/// </summary>
public sealed class Result
{
    private Result(bool isSuccess, string error)
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
    /// Returns <see cref="string.Empty"/> when <see cref="IsSuccess"/> is <c>true</c>.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Creates a successful <see cref="Result"/>.
    /// </summary>
    /// <returns>A <see cref="Result"/> representing a successful operation.</returns>
    public static Result Success() => new(true, string.Empty);

    /// <summary>
    /// Creates a failed <see cref="Result"/> with the specified error message.
    /// </summary>
    /// <param name="error">A non-empty error message describing the failure.</param>
    /// <returns>A <see cref="Result"/> representing a failed operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="error"/> is null or whitespace.</exception>
    public static Result Failure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new(false, error);
    }
}

/// <summary>
/// Represents the outcome of an operation that returns a value of type <typeparamref name="T"/>.
/// Use <see cref="Success"/> and <see cref="Failure"/> factory methods to create instances.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, string error)
    {
        IsSuccess = isSuccess;
        _value = value;
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
    /// Gets the value produced by a successful operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when accessing <see cref="Value"/> on a failure result.
    /// Check <see cref="IsSuccess"/> before accessing this property.
    /// </exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failure result.");

    /// <summary>
    /// Gets the error message describing why the operation failed.
    /// Returns <see cref="string.Empty"/> when <see cref="IsSuccess"/> is <c>true</c>.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Creates a successful <see cref="Result{T}"/> with the specified value.
    /// </summary>
    /// <param name="value">The value produced by the successful operation.</param>
    /// <returns>A <see cref="Result{T}"/> representing a successful operation.</returns>
    public static Result<T> Success(T value) => new(true, value, string.Empty);

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/> with the specified error message.
    /// </summary>
    /// <param name="error">A non-empty error message describing the failure.</param>
    /// <returns>A <see cref="Result{T}"/> representing a failed operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="error"/> is null or whitespace.</exception>
    public static Result<T> Failure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new(false, default, error);
    }
}
