using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="ICliService"/> that executes CLI commands
/// on the host operating system using <see cref="Process"/>. Handles process creation,
/// timeout enforcement, and stream capture. On Windows, commands are wrapped with
/// <c>cmd.exe /c</c> to support <c>.cmd</c>/<c>.bat</c> shims; on Linux/macOS,
/// <c>/bin/sh -c</c> is used.
/// </summary>
public sealed class CliService : ICliService
{
    private readonly ILogger<CliService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CliService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for structured diagnostic output.</param>
    public CliService(ILogger<CliService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CliResult> RunCommandAsync(
        string command,
        string arguments = "",
        int timeoutMs = 10_000,
        CancellationToken cancellationToken = default)
    {
        var (fileName, wrappedArguments) = BuildPlatformCommand(command, arguments);

        _logger.LogDebug(
            "Starting CLI command {Command} with arguments {Arguments} (timeout {TimeoutMs}ms)",
            command, arguments, timeoutMs);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = wrappedArguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var timeoutCts = new CancellationTokenSource(timeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        Process? process = null;
        try
        {
            process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
            var errorTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

            var output = (await outputTask).Trim();
            var error = (await errorTask).Trim();

            _logger.LogInformation(
                "CLI command {Command} completed with exit code {ExitCode}",
                command, process.ExitCode);

            return new CliResult(process.ExitCode, output, error);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "CLI command {Command} was cancelled by the caller", command);
            TryKillProcess(process);
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "CLI command {Command} timed out after {TimeoutMs}ms",
                command, timeoutMs);

            TryKillProcess(process);

            return new CliResult(
                -1,
                string.Empty,
                $"Command execution timed out after {timeoutMs}ms.");
        }
        catch (Win32Exception ex)
        {
            _logger.LogError(
                ex,
                "CLI command {Command} could not be started — the command was not found or is not executable",
                command);

            return new CliResult(
                -1,
                string.Empty,
                $"Command '{command}' was not found or could not be started: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unexpected error occurred while executing CLI command {Command}",
                command);

            TryKillProcess(process);

            return new CliResult(
                -1,
                string.Empty,
                $"An unexpected error occurred while executing '{command}': {ex.Message}");
        }
        finally
        {
            process?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to kill the specified process and its entire process tree.
    /// This is a best-effort operation — the process may have already exited
    /// between the <see cref="Process.HasExited"/> check and the
    /// <see cref="Process.Kill(bool)"/> call, so all exceptions are swallowed.
    /// </summary>
    /// <param name="process">The process to kill, or <see langword="null"/> if it was never started.</param>
    private static void TryKillProcess(Process? process)
    {
        try
        {
            if (process is not null && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort — process may have already exited between the check and the kill call.
        }
    }

    /// <summary>
    /// Builds the platform-specific file name and arguments for process execution.
    /// On Windows, wraps with <c>cmd.exe /c</c>; on Linux/macOS, wraps with <c>/bin/sh -c</c>.
    /// </summary>
    /// <param name="command">The original command to execute.</param>
    /// <param name="arguments">The original arguments for the command.</param>
    /// <returns>A tuple of the shell file name and the wrapped argument string.</returns>
    private static (string FileName, string Arguments) BuildPlatformCommand(
        string command,
        string arguments)
    {
        var fullCommand = string.IsNullOrWhiteSpace(arguments)
            ? command
            : $"{command} {arguments}";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ("cmd.exe", $"/c {fullCommand}");
        }

        return ("/bin/sh", $"-c \"{fullCommand}\"");
    }
}
