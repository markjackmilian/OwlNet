using Microsoft.AspNetCore.Components;
using MudBlazor;
using OwlNet.Application.Common.Constants;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Web.Components.Settings;

/// <summary>
/// Code-behind for <see cref="OpenCodeServerPanel"/>. Manages server URL configuration,
/// connection testing, and server process lifecycle (start/stop) with real-time status updates.
/// </summary>
public sealed partial class OpenCodeServerPanel : ComponentBase, IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  Injected Services
    // ═══════════════════════════════════════════════════════════════════════════

    [Inject] private IAppSettingService AppSettingService { get; set; } = null!;
    [Inject] private IOpenCodeServerManager ServerManager { get; set; } = null!;
    [Inject] private IOpenCodeClient OpenCodeClient { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    // ═══════════════════════════════════════════════════════════════════════════
    //  State — form fields, loading flags, and test result
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>The server URL currently displayed in the text field.</summary>
    private string _serverUrl = OpenCodeConstants.DefaultServerUrl;

    /// <summary>The last saved URL from the database. Used to detect unsaved changes.</summary>
    private string _savedUrl = OpenCodeConstants.DefaultServerUrl;

    /// <summary>True while the saved URL is being loaded on initialization.</summary>
    private bool _isLoading = true;

    /// <summary>True while the URL is being persisted to the database.</summary>
    private bool _isSaving;

    /// <summary>True while a test connection request is in progress.</summary>
    private bool _isTesting;

    /// <summary>
    /// The result of the last "Test Connection" attempt.
    /// Null until the user clicks "Test Connection" for the first time.
    /// Cleared when the URL field value changes to avoid showing stale results.
    /// </summary>
    private Result<OpenCodeHealthResult>? _testResult;

    /// <summary>
    /// Cancellation token source scoped to this component's lifetime.
    /// Cancelled in <see cref="Dispose"/> to abort any in-flight service calls
    /// if the user navigates away from the Settings page.
    /// </summary>
    private readonly CancellationTokenSource _cts = new();

    // ═══════════════════════════════════════════════════════════════════════════
    //  Computed Properties — derived UI state for button visibility and disabling
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Whether any async operation is currently in progress.</summary>
    private bool IsAnyOperationRunning => _isSaving || _isTesting
        || ServerManager.CurrentStatus.State == OpenCodeServerState.Starting
        || ServerManager.CurrentStatus.State == OpenCodeServerState.Stopping;

    /// <summary>Whether the form fields should be disabled during async operations.</summary>
    private bool IsFieldDisabled => _isSaving || _isTesting;

    /// <summary>Whether the URL in the text field is valid (non-empty and valid URI format).</summary>
    private bool IsUrlValid => !string.IsNullOrWhiteSpace(_serverUrl) && ValidateUrl(_serverUrl) is null;

    /// <summary>
    /// Test Connection button is disabled when the URL is empty/invalid or any async operation is running.
    /// </summary>
    private bool IsTestDisabled => !IsUrlValid || IsAnyOperationRunning;

    /// <summary>
    /// Save button is disabled when the URL is empty/invalid, unchanged, or any async operation is running.
    /// </summary>
    private bool IsSaveDisabled => !IsUrlValid || IsAnyOperationRunning;

    /// <summary>
    /// Start/Stop buttons are disabled during Starting, Stopping, or other async operations.
    /// </summary>
    private bool IsStartStopDisabled => IsAnyOperationRunning;

    /// <summary>
    /// Show the Start Server button when the server is Stopped, Error, or Unknown.
    /// </summary>
    private bool ShowStartButton => ServerManager.CurrentStatus.State
        is OpenCodeServerState.Stopped
        or OpenCodeServerState.Error
        or OpenCodeServerState.Unknown;

    /// <summary>
    /// Show the Stop Server button when the server is Running or Starting.
    /// </summary>
    private bool ShowStopButton => ServerManager.CurrentStatus.State
        is OpenCodeServerState.Running
        or OpenCodeServerState.Starting;

    /// <summary>
    /// Show the URL change warning when the URL has been modified in the text field
    /// but the server is still running on the previously saved URL.
    /// </summary>
    private bool ShowUrlChangeWarning =>
        _serverUrl != _savedUrl
        && ServerManager.CurrentStatus.State == OpenCodeServerState.Running;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Lifecycle — load settings and subscribe to status changes
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// On first load, subscribes to server status change events and loads the
    /// saved URL from the database. Then triggers an initial status refresh.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        // Subscribe to real-time status updates from the server manager
        ServerManager.OnStatusChanged += OnServerStatusChanged;

        // Load the saved URL from the database
        await LoadSettingsAsync();

        // Trigger an initial status check so the badge reflects the current state
        await ServerManager.RefreshStatusAsync(_cts.Token);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Status Change Handler — re-render when server state changes
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by <see cref="IOpenCodeServerManager.OnStatusChanged"/> when the server
    /// state changes (e.g., from Starting to Running). Triggers a UI re-render
    /// on the Blazor synchronization context.
    /// </summary>
    private void OnServerStatusChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Load Settings — fetches saved server URL from the database
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reads the <c>OpenCode:ServerUrl</c> setting from <see cref="IAppSettingService"/>.
    /// Falls back to <see cref="OpenCodeConstants.DefaultServerUrl"/> if no setting is saved.
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        _isLoading = true;

        var result = await AppSettingService.GetByKeyAsync(
            OpenCodeConstants.ServerUrlSettingKey, _cts.Token);

        if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value))
        {
            _serverUrl = result.Value;
            _savedUrl = result.Value;
        }
        else
        {
            // No saved setting — use the default URL
            _serverUrl = OpenCodeConstants.DefaultServerUrl;
            _savedUrl = OpenCodeConstants.DefaultServerUrl;
        }

        _isLoading = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Field Change Handler — clear stale test result on URL change
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when the server URL text field value changes. Updates the field value
    /// and clears any stale test connection result from a previous URL.
    /// </summary>
    private void OnServerUrlChanged(string value)
    {
        _serverUrl = value;
        _testResult = null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  URL Validation — ensures the URL is a valid absolute HTTP/HTTPS URI
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates the server URL format. Returns null if valid, or an error message if invalid.
    /// Used by the <see cref="MudTextField{T}"/> Validation parameter for inline feedback.
    /// </summary>
    /// <param name="url">The URL string to validate.</param>
    /// <returns>Null if valid; an error message string if invalid.</returns>
    private static string? ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "Server URL is required";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "Invalid URL format";

        // Only allow HTTP and HTTPS schemes for the server URL
        if (uri.Scheme is not ("http" or "https"))
            return "URL must use http:// or https://";

        return null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Test Connection — checks health against the current text field URL
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sends a health check request to the URL currently in the text field (not the saved URL).
    /// Updates <see cref="_testResult"/> with the outcome, displayed as a success or error chip.
    /// </summary>
    private async Task TestConnectionAsync()
    {
        _isTesting = true;
        _testResult = null;
        StateHasChanged();

        _testResult = await OpenCodeClient.HealthCheckAsync(_serverUrl, _cts.Token);

        _isTesting = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Save URL — persists the server URL to the database
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Saves the current server URL to the database via <see cref="IAppSettingService"/>.
    /// On success, updates <see cref="_savedUrl"/> and shows a success snackbar.
    /// On failure, shows an error snackbar with the failure reason.
    /// </summary>
    private async Task SaveUrlAsync()
    {
        _isSaving = true;
        StateHasChanged();

        var result = await AppSettingService.SaveAsync(
            OpenCodeConstants.ServerUrlSettingKey, _serverUrl, _cts.Token);

        if (result.IsSuccess)
        {
            _savedUrl = _serverUrl;
            Snackbar.Add("Server URL saved successfully.", Severity.Success);
        }
        else
        {
            Snackbar.Add(result.Error is { Length: > 0 } error
                ? error
                : "Failed to save server URL.", Severity.Error);
        }

        _isSaving = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Start Server — starts the OpenCode Server process
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Starts the OpenCode Server process via <see cref="IOpenCodeServerManager"/>.
    /// The server manager reads the configured URL from settings to determine the port.
    /// Shows a snackbar on success or failure.
    /// </summary>
    private async Task StartServerAsync()
    {
        var result = await ServerManager.StartServerAsync(_cts.Token);

        if (result.IsSuccess)
        {
            Snackbar.Add("OpenCode Server started.", Severity.Success);
        }
        else
        {
            Snackbar.Add(result.Error is { Length: > 0 } error
                ? $"Failed to start server: {error}"
                : "Failed to start server.", Severity.Error);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Stop Server — stops the OpenCode Server process
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Stops the OpenCode Server process via <see cref="IOpenCodeServerManager"/>.
    /// Shows a snackbar on success or failure.
    /// </summary>
    private async Task StopServerAsync()
    {
        var result = await ServerManager.StopServerAsync(_cts.Token);

        if (result.IsSuccess)
        {
            Snackbar.Add("OpenCode Server stopped.", Severity.Success);
        }
        else
        {
            Snackbar.Add(result.Error is { Length: > 0 } error
                ? $"Failed to stop server: {error}"
                : "Failed to stop server.", Severity.Error);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Cleanup — unsubscribe events and cancel in-flight operations
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Unsubscribes from server status change events and cancels any in-flight
    /// service calls when the component is disposed (e.g., user navigates away).
    /// </summary>
    public void Dispose()
    {
        ServerManager.OnStatusChanged -= OnServerStatusChanged;
        _cts.Cancel();
        _cts.Dispose();
    }
}
