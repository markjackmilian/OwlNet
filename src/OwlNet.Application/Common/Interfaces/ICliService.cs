using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Provides the ability to execute CLI commands on the host operating system
/// and capture their output. Implementations handle process creation, timeout
/// enforcement, and stream capture.
/// </summary>
public interface ICliService
{
    /// <summary>
    /// Runs a CLI command on the host machine and returns the captured result.
    /// </summary>
    /// <param name="command">
    /// The command or executable to run (e.g. <c>"dotnet"</c>, <c>"git"</c>).
    /// </param>
    /// <param name="arguments">
    /// The arguments to pass to the command. Defaults to an empty string.
    /// </param>
    /// <param name="timeoutMs">
    /// The maximum time in milliseconds to wait for the process to exit before
    /// killing it. Defaults to 10 000 ms (10 seconds). Use 120 000 ms for
    /// long-running operations such as installations.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation. When triggered, the process is killed
    /// and an <see cref="OperationCanceledException"/> is thrown.
    /// </param>
    /// <returns>
    /// A <see cref="CliResult"/> containing the exit code, captured standard output,
    /// and captured standard error. The method does not throw for expected failures
    /// such as a non-zero exit code or a missing command; those conditions are
    /// communicated through the returned <see cref="CliResult"/>.
    /// </returns>
    /// <remarks>
    /// The method returns <see cref="Task{TResult}"/> rather than <c>ValueTask</c> because
    /// process I/O operations always allocate and complete asynchronously.
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is triggered.
    /// </exception>
    Task<CliResult> RunCommandAsync(
        string command,
        string arguments = "",
        int timeoutMs = 10_000,
        CancellationToken cancellationToken = default);
}
