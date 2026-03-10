using DispatchR.Abstractions.Send;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using OwlNet.Application.Common.Models;
using OwlNet.Application.SystemAgents.Commands.DeleteSystemAgent;
using OwlNet.Application.SystemAgents.Commands.UpdateSystemAgent;
using OwlNet.Application.SystemAgents.Queries.GetSystemAgentByName;

namespace OwlNet.Web.Components.Settings.SystemAgents;

/// <summary>
/// Code-behind for <see cref="SystemAgentEditorPage"/>. A smart page component that loads
/// an existing system agent from the database and presents it in a full-text editor.
/// The user can view the agent summary, edit the inline fields (DisplayName, Description,
/// Mode) and the raw Markdown content, save all changes, or delete the agent.
///
/// Follows the same loading/validation/dirty-state patterns as
/// <see cref="OwlNet.Web.Components.Projects.Agents.AgentEditorPage"/>, adapted for
/// system agents which are stored in the database rather than the filesystem.
/// </summary>
public sealed partial class SystemAgentEditorPage : ComponentBase, IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  Injected Services
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Handler for fetching a single system agent by its unique name.</summary>
    [Inject]
    private IRequestHandler<GetSystemAgentByNameQuery, ValueTask<Result<SystemAgentDto>>> GetSystemAgentHandler { get; set; } = null!;

    /// <summary>Handler for updating a system agent's mutable properties in the database.</summary>
    [Inject]
    private IRequestHandler<UpdateSystemAgentCommand, ValueTask<Result>> UpdateSystemAgentHandler { get; set; } = null!;

    /// <summary>Handler for permanently deleting a system agent from the database.</summary>
    [Inject]
    private IRequestHandler<DeleteSystemAgentCommand, ValueTask<Result>> DeleteSystemAgentHandler { get; set; } = null!;

    /// <summary>Blazor navigation manager for programmatic URL navigation.</summary>
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    /// <summary>MudBlazor dialog service for showing confirmation dialogs.</summary>
    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    /// <summary>MudBlazor snackbar service for showing toast notifications.</summary>
    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Parameters
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The agent name extracted from the URL route segment (FR-26).
    /// Corresponds to <c>SystemAgent.Name</c> — the stable, immutable identifier.
    /// </summary>
    [Parameter]
    public string AgentName { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Page-Level State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>The loaded system agent data, null until fetch completes.</summary>
    private SystemAgentDto? _agent;

    /// <summary>True while the initial agent data is being fetched from the database (FR-47).</summary>
    private bool _isLoading = true;

    /// <summary>True when no system agent with the given Name was found in the database (FR-29).</summary>
    private bool _isAgentNotFound;

    /// <summary>
    /// Tracks the last loaded AgentName to skip redundant loads when the
    /// parameter hasn't changed (same guard pattern as AgentEditorPage).
    /// </summary>
    private string _lastLoadedAgentName = string.Empty;

    /// <summary>Cancellation token source for all async operations in this component.</summary>
    private readonly CancellationTokenSource _cts = new();

    /// <summary>Reference to the ErrorBoundary for recovering from rendering errors.</summary>
    private ErrorBoundary? _errorBoundary;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Editor State — inline fields (FR-34)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The current value of the DisplayName field. Updated in real-time as the
    /// user types (via <c>Immediate="true"</c> on the MudTextField).
    /// </summary>
    private string _displayName = string.Empty;

    /// <summary>
    /// The DisplayName as it was last loaded from or saved to the database.
    /// Used as the baseline for dirty state detection.
    /// </summary>
    private string _originalDisplayName = string.Empty;

    /// <summary>
    /// The current value of the Description field. Updated in real-time as the
    /// user types (via <c>Immediate="true"</c> on the MudTextField).
    /// </summary>
    private string _description = string.Empty;

    /// <summary>
    /// The Description as it was last loaded from or saved to the database.
    /// Used as the baseline for dirty state detection.
    /// </summary>
    private string _originalDescription = string.Empty;

    /// <summary>
    /// The current value of the Mode select. One of "primary", "subagent", or "all".
    /// </summary>
    private string _mode = string.Empty;

    /// <summary>
    /// The Mode as it was last loaded from or saved to the database.
    /// Used as the baseline for dirty state detection.
    /// </summary>
    private string _originalMode = string.Empty;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Editor State — content (FR-32, FR-33)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The current content in the editor text field. Updated in real-time as the
    /// user types (via <c>Immediate="true"</c> on the MudTextField).
    /// </summary>
    private string _editorContent = string.Empty;

    /// <summary>
    /// The content as it was last loaded from or saved to the database.
    /// Used as the baseline for dirty state detection.
    /// </summary>
    private string _originalContent = string.Empty;

    /// <summary>
    /// Computed dirty state: true when any editable field differs from its last
    /// saved/loaded value. Controls the Save button's enabled state (FR-33, FR-35).
    /// Checks all four mutable fields: DisplayName, Description, Mode, and Content.
    /// </summary>
    private bool _isDirty =>
        _editorContent != _originalContent ||
        _displayName != _originalDisplayName ||
        _description != _originalDescription ||
        _mode != _originalMode;

    /// <summary>True while a save operation is in progress (disables Save button, shows spinner).</summary>
    private bool _isSaving;

    /// <summary>True while a delete operation is in progress (disables Delete button, shows spinner).</summary>
    private bool _isDeleting;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Summary Header State (FR-30, FR-31)
    //  These values are updated only after a successful save, not in real-time.
    //  They reflect the last persisted state shown in the read-only header.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The DisplayName shown in the summary header. Updated only on successful save (FR-31).
    /// </summary>
    private string _summaryDisplayName = string.Empty;

    /// <summary>
    /// The Mode shown in the summary header badge. Updated only on successful save (FR-31).
    /// </summary>
    private string _summaryMode = string.Empty;

    /// <summary>
    /// The Description shown in the summary header. Updated only on successful save (FR-31).
    /// </summary>
    private string _summaryDescription = string.Empty;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Lifecycle
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads the system agent when the component initializes or when route
    /// parameters change. Uses <c>OnParametersSetAsync</c> because the route
    /// parameter may change without the component being re-created (same page,
    /// different agent). Guards against redundant loads when the parameter
    /// hasn't changed.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        // Skip redundant loads when navigating back to the same agent
        if (AgentName == _lastLoadedAgentName)
        {
            return;
        }

        _lastLoadedAgentName = AgentName;
        await LoadDataAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Data Loading
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fetches the system agent from the database by name. Sets the appropriate
    /// error state if the agent is not found, or populates all editor and summary
    /// state fields on success.
    /// </summary>
    private async Task LoadDataAsync()
    {
        // Reset all state for a fresh load
        _isLoading = true;
        _isAgentNotFound = false;
        _agent = null;
        _editorContent = string.Empty;
        _originalContent = string.Empty;
        _displayName = string.Empty;
        _originalDisplayName = string.Empty;
        _description = string.Empty;
        _originalDescription = string.Empty;
        _mode = string.Empty;
        _originalMode = string.Empty;
        _summaryDisplayName = string.Empty;
        _summaryMode = string.Empty;
        _summaryDescription = string.Empty;
        StateHasChanged();

        // ── Fetch the system agent by name ───────────────────────────────────
        var query = new GetSystemAgentByNameQuery { Name = AgentName };
        var result = await GetSystemAgentHandler.Handle(query, _cts.Token);

        if (result.IsFailure)
        {
            // Agent not found — show the not-found state (FR-29)
            _isAgentNotFound = true;
            _isLoading = false;
            return;
        }

        // ── Populate editor and summary state ────────────────────────────────
        _agent = result.Value;

        // Inline field state — editable values
        _displayName = _agent.DisplayName;
        _originalDisplayName = _agent.DisplayName;
        _description = _agent.Description;
        _originalDescription = _agent.Description;
        _mode = _agent.Mode;
        _originalMode = _agent.Mode;

        // Content editor state
        _editorContent = _agent.Content;
        _originalContent = _agent.Content;

        // Summary header state — reflects the last persisted values (FR-31)
        _summaryDisplayName = _agent.DisplayName;
        _summaryMode = _agent.Mode;
        _summaryDescription = _agent.Description;

        _isLoading = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Save (FR-35 to FR-38)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Saves all current editor values (DisplayName, Description, Mode, Content)
    /// to the database via <see cref="UpdateSystemAgentCommand"/>.
    /// On success: shows a success snackbar, resets dirty state, and updates the
    /// summary header to reflect the newly saved values (FR-37).
    /// On failure: shows an error snackbar and preserves all editor values (FR-38).
    /// </summary>
    private async Task SaveAsync()
    {
        if (!_isDirty || _isSaving)
        {
            return;
        }

        _isSaving = true;
        StateHasChanged();

        try
        {
            var command = new UpdateSystemAgentCommand
            {
                Id = _agent!.Id,
                DisplayName = _displayName,
                Description = _description,
                Mode = _mode,
                Content = _editorContent
            };

            var result = await UpdateSystemAgentHandler.Handle(command, _cts.Token);

            if (result.IsSuccess)
            {
                // FR-37: Success — show snackbar, reset dirty state, update summary header
                Snackbar.Add($"System agent '{AgentName}' saved successfully.", Severity.Success);

                // Reset all original values to the newly saved values
                _originalDisplayName = _displayName;
                _originalDescription = _description;
                _originalMode = _mode;
                _originalContent = _editorContent;

                // Update the summary header to reflect the saved values (FR-31, FR-37)
                _summaryDisplayName = _displayName;
                _summaryMode = _mode;
                _summaryDescription = _description;

                // Update the cached DTO so the page title reflects the new DisplayName
                _agent = _agent with
                {
                    DisplayName = _displayName,
                    Description = _description,
                    Mode = _mode,
                    Content = _editorContent
                };
            }
            else
            {
                // FR-38: Failure — show error snackbar, editor content preserved
                Snackbar.Add($"Failed to save system agent: {result.Error}", Severity.Error);
            }
        }
        catch (OperationCanceledException)
        {
            // Component is being disposed — silently ignore
        }
        catch (Exception)
        {
            Snackbar.Add("An unexpected error occurred while saving. Please try again.", Severity.Error);
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Delete (FR-39 to FR-43)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Deletes the system agent after showing a confirmation dialog (FR-40).
    /// On confirm + success: navigates to /settings and shows a success snackbar (FR-42).
    /// On confirm + failure: shows an error snackbar and remains on the editor page (FR-43).
    /// On cancel: no action taken.
    /// </summary>
    private async Task DeleteAsync()
    {
        // FR-40: Show confirmation dialog before deleting
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete System Agent",
            $"Are you sure you want to delete the system agent '{AgentName}'? This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirmed != true)
        {
            return;
        }

        _isDeleting = true;
        StateHasChanged();

        try
        {
            var command = new DeleteSystemAgentCommand { Id = _agent!.Id };
            var result = await DeleteSystemAgentHandler.Handle(command, _cts.Token);

            if (result.IsSuccess)
            {
                // FR-42: Navigate to /settings and show success snackbar
                Snackbar.Add($"System agent '{AgentName}' deleted.", Severity.Success);
                NavigationManager.NavigateTo("/settings");
            }
            else
            {
                // FR-43: Show error snackbar, remain on editor page
                Snackbar.Add($"Failed to delete system agent: {result.Error}", Severity.Error);
            }
        }
        catch (OperationCanceledException)
        {
            // Component is being disposed — silently ignore
        }
        catch (Exception)
        {
            Snackbar.Add("An unexpected error occurred while deleting. Please try again.", Severity.Error);
        }
        finally
        {
            _isDeleting = false;
            StateHasChanged();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Navigation (FR-28, FR-44 to FR-46)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Navigates back to /settings. If the editor has unsaved changes, shows a
    /// confirmation dialog to prevent accidental data loss (FR-44, FR-45, FR-46).
    /// </summary>
    private async Task NavigateBackAsync()
    {
        if (_isDirty)
        {
            // FR-44: Show confirmation dialog when there are unsaved changes
            var confirmed = await DialogService.ShowMessageBoxAsync(
                "Unsaved Changes",
                "You have unsaved changes. Leave without saving?",
                yesText: "Leave",
                cancelText: "Stay");

            if (confirmed != true)
            {
                // FR-46: User chose to stay — remain on the editor page
                return;
            }
        }

        // FR-45: Navigate to /settings (changes discarded if dirty)
        NavigationManager.NavigateTo("/settings");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Type Badge Helpers (FR-30)
    //  Maps mode values to MudChip colors and display text.
    //  Same color scheme as the system agents list page (FR-3 equivalent).
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the MudBlazor chip color for the given agent mode value.
    /// Primary → Color.Primary, Subagent → Color.Secondary,
    /// All → Color.Tertiary, anything else → Color.Default.
    /// </summary>
    /// <param name="mode">The agent mode string (e.g., "primary", "subagent", "all").</param>
    private static Color GetChipColor(string mode) => mode.ToLowerInvariant() switch
    {
        "primary" => Color.Primary,
        "subagent" => Color.Secondary,
        "all" => Color.Tertiary,
        _ => Color.Default
    };

    /// <summary>
    /// Returns the display text for the given agent mode value.
    /// Maps known modes to title-cased labels; unknown/empty modes show "Unknown".
    /// </summary>
    /// <param name="mode">The agent mode string (e.g., "primary", "subagent", "all").</param>
    private static string GetChipText(string mode) => mode.ToLowerInvariant() switch
    {
        "primary" => "Primary",
        "subagent" => "Subagent",
        "all" => "All",
        _ => "Unknown"
    };

    // ═══════════════════════════════════════════════════════════════════════════
    //  Error Recovery
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Recovers from a rendering error by resetting the ErrorBoundary.
    /// Called from the ErrorContent "Try Again" button.
    /// </summary>
    private void RecoverFromError()
    {
        _errorBoundary?.Recover();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Cleanup
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
