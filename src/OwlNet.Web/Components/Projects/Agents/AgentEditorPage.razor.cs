using DispatchR.Abstractions.Send;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using OwlNet.Application.Agents.Commands.DeleteAgent;
using OwlNet.Application.Agents.Commands.UpdateAgent;
using OwlNet.Application.Agents.Queries.GetAgentFile;
using OwlNet.Application.Common.Models;
using OwlNet.Application.Projects.Queries.GetProjectById;
using OwlNet.Web.Services;

namespace OwlNet.Web.Components.Projects.Agents;

/// <summary>
/// Code-behind for <see cref="AgentEditorPage"/>. A smart page component that loads
/// an existing agent's Markdown file and presents it in a full-text editor. The user
/// can view the agent summary, edit the raw content, save changes, or delete the agent.
///
/// Follows the same loading/validation/sync patterns as <see cref="CreateAgentPage"/>
/// and <see cref="ProjectAgentsPage"/>.
/// </summary>
public sealed partial class AgentEditorPage : ComponentBase, IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  Injected Services
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Handler for fetching a project by its ID.</summary>
    [Inject]
    private IRequestHandler<GetProjectByIdQuery, ValueTask<Result<ProjectDto>>> GetProjectHandler { get; set; } = null!;

    /// <summary>Handler for fetching a single agent file's content and parsed frontmatter.</summary>
    [Inject]
    private IRequestHandler<GetAgentFileQuery, ValueTask<Result<AgentFileDto>>> GetAgentHandler { get; set; } = null!;

    /// <summary>Handler for overwriting an agent file's content on the filesystem.</summary>
    [Inject]
    private IRequestHandler<UpdateAgentCommand, ValueTask<Result>> UpdateAgentHandler { get; set; } = null!;

    /// <summary>Handler for deleting an agent file from the filesystem.</summary>
    [Inject]
    private IRequestHandler<DeleteAgentCommand, ValueTask<Result>> DeleteAgentHandler { get; set; } = null!;

    /// <summary>Service for tracking and syncing the currently active project in the topbar.</summary>
    [Inject]
    private ActiveProjectService ActiveProjectService { get; set; } = null!;

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
    /// The project ID extracted from the URL route segment.
    /// Used to validate the project exists and is not archived before loading the agent.
    /// </summary>
    [Parameter]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// The agent name extracted from the URL route segment.
    /// Corresponds to the agent filename without the <c>.md</c> extension.
    /// </summary>
    [Parameter]
    public string AgentName { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Page-Level State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>The loaded project data, null until fetch completes.</summary>
    private ProjectDto? _project;

    /// <summary>The loaded agent file data, null until fetch completes.</summary>
    private AgentFileDto? _agent;

    /// <summary>True while the initial project and agent data are being fetched (FR-33).</summary>
    private bool _isLoading = true;

    /// <summary>True when the project was not found or is archived (FR-3).</summary>
    private bool _isProjectNotFound;

    /// <summary>True when the agent file was not found at the expected path (FR-4).</summary>
    private bool _isAgentNotFound;

    /// <summary>
    /// Tracks the last loaded ProjectId to skip redundant loads when the
    /// parameter hasn't changed (same guard pattern as CreateAgentPage).
    /// </summary>
    private Guid _lastLoadedProjectId;

    /// <summary>
    /// Tracks the last loaded AgentName to skip redundant loads when the
    /// parameter hasn't changed.
    /// </summary>
    private string _lastLoadedAgentName = string.Empty;

    /// <summary>Cancellation token source for all async operations in this component.</summary>
    private readonly CancellationTokenSource _cts = new();

    /// <summary>Reference to the ErrorBoundary for recovering from rendering errors.</summary>
    private ErrorBoundary? _errorBoundary;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Editor State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The current content in the editor text field. Updated in real-time as the
    /// user types (via <c>Immediate="true"</c> on the MudTextField).
    /// </summary>
    private string _editorContent = string.Empty;

    /// <summary>
    /// The content as it was last loaded from or saved to the filesystem.
    /// Used as the baseline for dirty state detection.
    /// </summary>
    private string _originalContent = string.Empty;

    /// <summary>
    /// Computed dirty state: true when the editor content differs from the
    /// last saved/loaded content. Controls the Save button's enabled state (FR-12, FR-14).
    /// </summary>
    private bool _isDirty => _editorContent != _originalContent;

    /// <summary>True while a save operation is in progress (disables Save button, shows spinner).</summary>
    private bool _isSaving;

    /// <summary>True while a delete operation is in progress (disables Delete button, shows spinner).</summary>
    private bool _isDeleting;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Summary Header State (FR-7, FR-8)
    //  These values are updated only after a successful save, not in real-time.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The agent's mode value from the frontmatter (e.g., "primary", "subagent", "all").
    /// Used to determine the type badge color and text.
    /// </summary>
    private string _agentMode = string.Empty;

    /// <summary>
    /// The agent's description from the frontmatter. Displayed as secondary text
    /// below the agent name in the summary header.
    /// </summary>
    private string _agentDescription = string.Empty;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Lifecycle
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads the project and agent when the component initializes or when route
    /// parameters change. Uses <c>OnParametersSetAsync</c> because the route
    /// parameters may change without the component being re-created (same page,
    /// different agent). Guards against redundant loads when parameters haven't changed.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        // Skip redundant loads when navigating back to the same agent
        if (ProjectId == _lastLoadedProjectId && AgentName == _lastLoadedAgentName)
        {
            return;
        }

        _lastLoadedProjectId = ProjectId;
        _lastLoadedAgentName = AgentName;
        await LoadDataAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Data Loading
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fetches the project and agent data in sequence. Validates the project exists
    /// and is not archived, then loads the agent file content and frontmatter.
    /// Sets the appropriate error state if either lookup fails.
    /// </summary>
    private async Task LoadDataAsync()
    {
        // Reset all state for a fresh load
        _isLoading = true;
        _isProjectNotFound = false;
        _isAgentNotFound = false;
        _project = null;
        _agent = null;
        _editorContent = string.Empty;
        _originalContent = string.Empty;
        _agentMode = string.Empty;
        _agentDescription = string.Empty;
        StateHasChanged();

        // ── Step 1: Fetch and validate the project ───────────────────────────
        var projectQuery = new GetProjectByIdQuery { Id = ProjectId };
        var projectResult = await GetProjectHandler.Handle(projectQuery, _cts.Token);

        if (projectResult.IsFailure || projectResult.Value.IsArchived)
        {
            // Project not found or archived — show error state (FR-3)
            _isProjectNotFound = true;
            _isLoading = false;
            return;
        }

        _project = projectResult.Value;

        // FR-5: If the user navigated directly (bookmark, shared link), sync
        // the active project context so the topbar shows the correct project.
        if (ActiveProjectService.ActiveProject is null
            || ActiveProjectService.ActiveProject.Id != _project.Id)
        {
            await ActiveProjectService.SetActiveProjectAsync(_project);
        }

        // ── Step 2: Fetch the agent file ─────────────────────────────────────
        var agentQuery = new GetAgentFileQuery
        {
            ProjectId = ProjectId,
            AgentName = AgentName
        };
        var agentResult = await GetAgentHandler.Handle(agentQuery, _cts.Token);

        if (agentResult.IsFailure)
        {
            // Agent file not found — show error state (FR-4)
            _isAgentNotFound = true;
            _isLoading = false;
            return;
        }

        // ── Step 3: Populate editor and summary state ────────────────────────
        _agent = agentResult.Value;
        _editorContent = _agent.RawContent;
        _originalContent = _agent.RawContent;
        _agentMode = _agent.Mode;
        _agentDescription = _agent.Description;

        _isLoading = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Save (FR-13 to FR-17)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Saves the current editor content to the agent file on the filesystem.
    /// On success: shows a success snackbar, resets dirty state, and refreshes
    /// the summary header by re-fetching the agent to get re-parsed frontmatter.
    /// On failure: shows an error snackbar and preserves the editor content.
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
            var command = new UpdateAgentCommand
            {
                ProjectId = ProjectId,
                AgentName = AgentName,
                Content = _editorContent
            };

            var result = await UpdateAgentHandler.Handle(command, _cts.Token);

            if (result.IsSuccess)
            {
                // FR-16: Success — show snackbar and reset dirty state
                Snackbar.Add($"Agent '{AgentName}' saved successfully.", Severity.Success);
                _originalContent = _editorContent;

                // FR-8: Re-fetch the agent to get re-parsed frontmatter values.
                // This is simpler and more reliable than parsing YAML client-side.
                var refreshQuery = new GetAgentFileQuery
                {
                    ProjectId = ProjectId,
                    AgentName = AgentName
                };
                var refreshResult = await GetAgentHandler.Handle(refreshQuery, _cts.Token);

                if (refreshResult.IsSuccess)
                {
                    _agent = refreshResult.Value;
                    _agentMode = _agent.Mode;
                    _agentDescription = _agent.Description;
                }
                // If the refresh fails, the summary header keeps its previous values.
                // This is acceptable — the file was saved successfully.
            }
            else
            {
                // FR-17: Failure — show error snackbar, editor content preserved
                Snackbar.Add($"Failed to save agent: {result.Error}", Severity.Error);
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
    //  Delete (FR-18 to FR-22)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Deletes the agent file after showing a confirmation dialog.
    /// On confirm + success: navigates to the agent list and shows a success snackbar.
    /// On confirm + failure: shows an error snackbar and remains on the editor page.
    /// On cancel: no action taken.
    /// </summary>
    private async Task DeleteAsync()
    {
        // FR-19: Show confirmation dialog before deleting
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete Agent",
            $"Are you sure you want to delete the agent '{AgentName}'? This action cannot be undone.",
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
            var command = new DeleteAgentCommand
            {
                ProjectId = ProjectId,
                AgentName = AgentName
            };

            var result = await DeleteAgentHandler.Handle(command, _cts.Token);

            if (result.IsSuccess)
            {
                // FR-21: Navigate to agent list and show success snackbar
                Snackbar.Add($"Agent '{AgentName}' deleted.", Severity.Success);
                NavigationManager.NavigateTo($"/projects/{ProjectId}/agents");
            }
            else
            {
                // FR-22: Show error snackbar, remain on editor page
                Snackbar.Add($"Failed to delete agent: {result.Error}", Severity.Error);
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
    //  Navigation (FR-23 to FR-26)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Navigates back to the agent list page. If the editor has unsaved changes,
    /// shows a confirmation dialog to prevent accidental data loss (FR-24, FR-25, FR-26).
    /// </summary>
    private async Task NavigateBackAsync()
    {
        if (_isDirty)
        {
            // FR-24: Show confirmation dialog when there are unsaved changes
            var confirmed = await DialogService.ShowMessageBoxAsync(
                "Unsaved Changes",
                "You have unsaved changes. Leave without saving?",
                yesText: "Leave",
                cancelText: "Stay");

            if (confirmed != true)
            {
                // FR-26: User chose to stay — remain on the editor page
                return;
            }
        }

        // FR-25: Navigate to the agent list (changes discarded if dirty)
        NavigateToAgentList();
    }

    /// <summary>
    /// Navigates to the agent list page for the current project.
    /// Used by both the back button and the "project not found" error state.
    /// </summary>
    private void NavigateToAgentList()
    {
        NavigationManager.NavigateTo($"/projects/{ProjectId}/agents");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Type Badge Helpers (FR-7)
    //  Same logic as ProjectAgentsPage — maps mode values to chip colors/text.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the MudBlazor chip color for the given agent mode value.
    /// Primary → Color.Primary, Subagent → Color.Secondary,
    /// All → Color.Tertiary, anything else → Color.Default.
    /// </summary>
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
