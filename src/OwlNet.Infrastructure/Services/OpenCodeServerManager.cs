using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Constants;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Singleton service that manages the lifecycle of the OpenCode Server process.
/// The server is started as a child process of the OwlNet application and is
/// terminated when the application shuts down or when explicitly stopped.
/// Uses <see cref="IServiceScopeFactory"/> to resolve scoped/transient services per-call
/// (e.g., <see cref="IAppSettingService"/>, <see cref="IOpenCodeClient"/>) to avoid captive
/// dependency issues — this class is a singleton, but those dependencies have shorter lifetimes.
/// </summary>
public sealed class OpenCodeServerManager : IOpenCodeServerManager, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OpenCodeServerManager> _logger;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    private Process? _serverProcess;
    private OpenCodeServerStatus _currentStatus = OpenCodeServerStatus.CreateUnknown();

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeServerManager"/> class.
    /// </summary>
    /// <param name="scopeFactory">The scope factory used to resolve scoped/transient services such as
    /// <see cref="IAppSettingService"/> and <see cref="IOpenCodeClient"/> per-call.</param>
    /// <param name="logger">The logger instance for structured diagnostic output.</param>
    public OpenCodeServerManager(
        IServiceScopeFactory scopeFactory,
        ILogger<OpenCodeServerManager> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public OpenCodeServerStatus CurrentStatus => _currentStatus;

    /// <inheritdoc />
    public event Action? OnStatusChanged;

    /// <inheritdoc />
    public async Task<Result> StartServerAsync(CancellationToken cancellationToken = default)
    {
        if (!await _operationLock.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken))
        {
            return Result.Failure("Another server operation is already in progress.");
        }

        try
        {
            // If the managed process is still alive, verify health and return early
            if (_serverProcess is not null && !_serverProcess.HasExited)
            {
                _logger.LogInformation(
                    "OpenCode Server process is already running (PID {ProcessId})",
                    _serverProcess.Id);
                return Result.Success();
            }

            // Resolve scoped/transient services for this operation
            using var scope = _scopeFactory.CreateScope();
            var openCodeClient = scope.ServiceProvider.GetRequiredService<IOpenCodeClient>();
            var appSettingService = scope.ServiceProvider.GetRequiredService<IAppSettingService>();

            // Read the configured server URL from application settings
            var serverUrl = await GetServerUrlAsync(appSettingService, cancellationToken);

            if (!TryExtractPort(serverUrl, out var port))
            {
                _logger.LogError(
                    "Cannot extract port from configured server URL {ServerUrl}", serverUrl);
                return Result.Failure($"Cannot extract port from server URL: {serverUrl}");
            }

            UpdateStatus(OpenCodeServerStatus.CreateStarting(serverUrl));

            // Build the platform-specific command to launch the server
            var (fileName, arguments) = BuildProcessCommand(port);

            _logger.LogInformation(
                "Starting OpenCode Server on port {Port} (command: {FileName} {Arguments})",
                port, fileName, arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _serverProcess = new Process { StartInfo = startInfo };

            try
            {
                _serverProcess.Start();

                // Discard stdout/stderr asynchronously to prevent buffer deadlock
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start OpenCode Server process");
                _serverProcess.Dispose();
                _serverProcess = null;
                UpdateStatus(OpenCodeServerStatus.CreateError($"Failed to start: {ex.Message}"));
                return Result.Failure($"Failed to start OpenCode Server: {ex.Message}");
            }

            _logger.LogInformation(
                "OpenCode Server process started (PID {ProcessId})", _serverProcess.Id);

            // Poll the health endpoint until the server is ready (up to 10 attempts, 1 s apart)
            return await WaitForServerHealthyAsync(serverUrl, openCodeClient, cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result> StopServerAsync(CancellationToken cancellationToken = default)
    {
        if (!await _operationLock.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken))
        {
            return Result.Failure("Another server operation is already in progress.");
        }

        try
        {
            if (_serverProcess is null || _serverProcess.HasExited)
            {
                _logger.LogInformation("OpenCode Server process is not running");
                _serverProcess?.Dispose();
                _serverProcess = null;
                UpdateStatus(OpenCodeServerStatus.CreateStopped());
                return Result.Success();
            }

            UpdateStatus(OpenCodeServerStatus.CreateStopping());

            var processId = _serverProcess.Id;
            _logger.LogInformation(
                "Stopping OpenCode Server process (PID {ProcessId})", processId);

            try
            {
                _serverProcess.Kill(entireProcessTree: true);
                await _serverProcess.WaitForExitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error while stopping OpenCode Server process (PID {ProcessId})",
                    processId);
            }
            finally
            {
                _serverProcess.Dispose();
                _serverProcess = null;
            }

            UpdateStatus(OpenCodeServerStatus.CreateStopped());
            _logger.LogInformation("OpenCode Server process stopped");
            return Result.Success();
        }
        finally
        {
            _operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<OpenCodeServerStatus> RefreshStatusAsync(CancellationToken cancellationToken = default)
    {
        // Use a short timeout to avoid blocking the polling loop.
        // If Start/Stop is in progress, just return the current status without updating.
        if (!await _operationLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
        {
            _logger.LogDebug("RefreshStatusAsync skipped — another operation is in progress");
            return _currentStatus;
        }

        try
        {
            // Resolve scoped/transient services for this operation
            using var scope = _scopeFactory.CreateScope();
            var openCodeClient = scope.ServiceProvider.GetRequiredService<IOpenCodeClient>();
            var appSettingService = scope.ServiceProvider.GetRequiredService<IAppSettingService>();

            var serverUrl = await GetServerUrlAsync(appSettingService, cancellationToken);
            var healthResult = await openCodeClient.HealthCheckAsync(serverUrl, cancellationToken);

            if (healthResult.IsSuccess && healthResult.Value.IsHealthy)
            {
                UpdateStatus(OpenCodeServerStatus.CreateRunning(
                    healthResult.Value.Version ?? "unknown", serverUrl));
            }
            else if (_serverProcess is not null && !_serverProcess.HasExited)
            {
                // Managed process is alive but not responding — error state
                var errorMessage = healthResult.IsFailure
                    ? healthResult.Error
                    : "Server not healthy";
                UpdateStatus(OpenCodeServerStatus.CreateError(errorMessage));
            }
            else
            {
                // No managed process or it has exited — clean up and mark stopped
                if (_serverProcess is not null)
                {
                    _serverProcess.Dispose();
                    _serverProcess = null;
                }

                UpdateStatus(OpenCodeServerStatus.CreateStopped());
            }

            return _currentStatus;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        if (_serverProcess is not null && !_serverProcess.HasExited)
        {
            try
            {
                _serverProcess.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best effort — process may have already exited.
            }
        }

        _serverProcess?.Dispose();
        _operationLock.Dispose();
    }

    // ── Private helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Polls the OpenCode Server health endpoint until it reports healthy,
    /// up to a maximum of 10 attempts with a 1-second delay between each.
    /// </summary>
    /// <param name="serverUrl">The server URL to health-check.</param>
    /// <param name="openCodeClient">The HTTP client resolved from a scoped service provider.</param>
    /// <param name="cancellationToken">A token to cancel the polling loop.</param>
    private async Task<Result> WaitForServerHealthyAsync(
        string serverUrl,
        IOpenCodeClient openCodeClient,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;
        const int delayMs = 1000;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await Task.Delay(delayMs, cancellationToken);

            // If the process died while we were waiting, bail out early
            if (_serverProcess is null || _serverProcess.HasExited)
            {
                _logger.LogWarning(
                    "OpenCode Server process exited unexpectedly during startup health check");
                _serverProcess?.Dispose();
                _serverProcess = null;
                UpdateStatus(OpenCodeServerStatus.CreateError(
                    "Server process exited unexpectedly during startup."));
                return Result.Failure("Server process exited unexpectedly during startup.");
            }

            var healthResult = await openCodeClient.HealthCheckAsync(serverUrl, cancellationToken);

            if (healthResult.IsSuccess && healthResult.Value.IsHealthy)
            {
                var version = healthResult.Value.Version ?? "unknown";
                UpdateStatus(OpenCodeServerStatus.CreateRunning(version, serverUrl));
                _logger.LogInformation(
                    "OpenCode Server is healthy after {Attempts} attempt(s) (Version: {Version})",
                    attempt, version);
                return Result.Success();
            }

            _logger.LogDebug(
                "OpenCode Server not yet healthy (attempt {Attempt}/{MaxAttempts})",
                attempt, maxAttempts);
        }

        _logger.LogWarning(
            "OpenCode Server process started but health check failed after {MaxAttempts} attempts",
            maxAttempts);
        UpdateStatus(OpenCodeServerStatus.CreateError(
            "Server started but not responding to health checks."));
        return Result.Failure("Server started but not responding to health checks.");
    }

    /// <summary>
    /// Updates the current status and raises the <see cref="OnStatusChanged"/> event.
    /// </summary>
    private void UpdateStatus(OpenCodeServerStatus newStatus)
    {
        _currentStatus = newStatus;
        OnStatusChanged?.Invoke();
    }

    /// <summary>
    /// Reads the OpenCode Server URL from application settings.
    /// Falls back to <see cref="OpenCodeConstants.DefaultServerUrl"/> if the setting is not found.
    /// </summary>
    /// <param name="appSettingService">The app setting service resolved from a scoped service provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    private static async Task<string> GetServerUrlAsync(
        IAppSettingService appSettingService,
        CancellationToken cancellationToken)
    {
        var result = await appSettingService.GetByKeyAsync(
            OpenCodeConstants.ServerUrlSettingKey, cancellationToken);
        return result.IsSuccess ? result.Value : OpenCodeConstants.DefaultServerUrl;
    }

    /// <summary>
    /// Attempts to extract the port number from a URL string.
    /// </summary>
    /// <param name="url">The URL to parse (e.g., <c>"http://127.0.0.1:4096"</c>).</param>
    /// <param name="port">When this method returns, contains the extracted port number, or 0 if extraction failed.</param>
    /// <returns><see langword="true"/> if a valid port was extracted; otherwise, <see langword="false"/>.</returns>
    private static bool TryExtractPort(string url, out int port)
    {
        port = 0;
        try
        {
            var uri = new Uri(url);
            port = uri.Port;
            return port > 0;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Builds the platform-specific file name and arguments for launching the OpenCode Server.
    /// On Windows, wraps with <c>cmd.exe /c</c>; on Linux/macOS, wraps with <c>/bin/sh -c</c>.
    /// </summary>
    /// <param name="port">The port number to pass to <c>opencode serve --port</c>.</param>
    /// <returns>A tuple of the shell file name and the wrapped argument string.</returns>
    private static (string FileName, string Arguments) BuildProcessCommand(int port)
    {
        var openCodeCommand = $"opencode serve --port {port}";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ("cmd.exe", $"/c {openCodeCommand}");
        }

        return ("/bin/sh", $"-c \"{openCodeCommand}\"");
    }
}
