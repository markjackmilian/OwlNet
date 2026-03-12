namespace OwlNet.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested entity cannot be found in the data store.
/// Callers (e.g. API middleware) should map this to an HTTP 404 Not Found response.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class
    /// with the specified error message.
    /// </summary>
    /// <param name="message">A human-readable message describing which entity was not found.</param>
    public NotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class
    /// with the specified error message and inner exception.
    /// </summary>
    /// <param name="message">A human-readable message describing which entity was not found.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
