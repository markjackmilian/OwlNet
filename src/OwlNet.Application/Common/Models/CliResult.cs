namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents the result of executing a CLI command on the host machine.
/// Captures the process exit code, standard output, and standard error streams.
/// </summary>
/// <param name="ExitCode">The process exit code. Zero typically indicates success.</param>
/// <param name="Output">The captured standard output text, trimmed of leading and trailing whitespace.</param>
/// <param name="Error">The captured standard error text, trimmed of leading and trailing whitespace.</param>
public sealed record CliResult(int ExitCode, string Output, string Error)
{
    /// <summary>
    /// Gets a value indicating whether the command executed successfully (exit code is zero).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;
}
