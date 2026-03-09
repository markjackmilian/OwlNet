using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Constants;
using OwlNet.Application.Common.Interfaces;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Background service that manages the OpenCode Server lifecycle:
/// auto-starts the server on application startup, periodically polls
/// the health endpoint to keep the status current, and gracefully
/// stops the server on application shutdown.
/// </summary>
public sealed class OpenCodeServerHostedService : BackgroundService
{
    private readonly IOpenCodeServerManager _serverManager;
    private readonly ILogger<OpenCodeServerHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeServerHostedService"/> class.
    /// </summary>
    /// <param name="serverManager">The singleton server manager that controls the OpenCode Server process.</param>
    /// <param name="logger">The logger instance for structured diagnostic output.</param>
    public OpenCodeServerHostedService(
        IOpenCodeServerManager serverManager,
        ILogger<OpenCodeServerHostedService> logger)
    {
        _serverManager = serverManager;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background work: auto-starts the OpenCode Server, then enters a
    /// periodic polling loop that refreshes the server status every
    /// <see cref="OpenCodeConstants.DefaultPollingIntervalSeconds"/> seconds.
    /// </summary>
    /// <param name="stoppingToken">A token that signals when the host is shutting down.</param>
    /// <returns>A <see cref="Task"/> representing the long-running background operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OpenCode Server hosted service starting");

        // Phase 1: Auto-start attempt
        try
        {
            _logger.LogInformation("Attempting to auto-start OpenCode Server");
            var startResult = await _serverManager.StartServerAsync(stoppingToken);

            if (startResult.IsSuccess)
            {
                _logger.LogInformation("OpenCode Server auto-started successfully");
            }
            else
            {
                _logger.LogWarning(
                    "OpenCode Server auto-start failed: {Error}. The server can be started manually from Settings",
                    startResult.Error);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during OpenCode Server auto-start");
        }

        // Phase 2: Periodic health check polling
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(OpenCodeConstants.DefaultPollingIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await _serverManager.RefreshStatusAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during OpenCode Server health check poll");
            }
        }

        _logger.LogInformation("OpenCode Server hosted service stopping");
    }

    /// <summary>
    /// Called when the host is performing a graceful shutdown. Stops the OpenCode Server
    /// process before completing the base shutdown sequence.
    /// </summary>
    /// <param name="cancellationToken">A token that signals when the shutdown grace period expires.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous shutdown operation.</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OpenCode Server hosted service shutting down — stopping server");

        try
        {
            await _serverManager.StopServerAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while stopping OpenCode Server during shutdown");
        }

        await base.StopAsync(cancellationToken);
    }
}
