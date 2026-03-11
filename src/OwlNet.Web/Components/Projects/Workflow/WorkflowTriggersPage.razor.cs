using DispatchR.Abstractions.Send;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OwlNet.Application.BoardStatuses.Queries.GetProjectStatuses;
using OwlNet.Application.Common.Models;
using OwlNet.Application.Projects.Queries.GetProjectById;
using OwlNet.Application.WorkflowTriggers.Commands.DeleteWorkflowTrigger;
using OwlNet.Application.WorkflowTriggers.Commands.UpdateWorkflowTrigger;
using OwlNet.Application.WorkflowTriggers.Queries.GetWorkflowTriggersByProject;
using OwlNet.Web.Services;

namespace OwlNet.Web.Components.Projects.Workflow;

/// <summary>
/// Code-behind for <see cref="WorkflowTriggersPage"/>. A smart page component that
/// loads all workflow triggers for a project and presents them in a filterable list.
/// Supports quick actions: Edit (navigate), Delete (with confirmation dialog), and
/// Toggle Enable/Disable (inline update via <see cref="UpdateWorkflowTriggerCommand"/>).
///
/// Follows the same loading/validation/sync patterns as <see cref="ProjectAgentsPage"/>
/// and <c>Board.razor</c>: <c>OnParametersSetAsync</c> guard, <c>ActiveProjectService</c>
/// sync, and <c>CancellationTokenSource</c> disposal.
/// </summary>
public sealed partial class WorkflowTriggersPage : ComponentBase, IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  Injected Services
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Handler for fetching a project by its ID.</summary>
    [Inject]
    private IRequestHandler<GetProjectByIdQuery, ValueTask<Result<ProjectDto>>> GetProjectHandler { get; set; } = null!;

    /// <summary>Handler for fetching all workflow triggers belonging to a project.</summary>
    [Inject]
    private IRequestHandler<GetWorkflowTriggersByProjectQuery, ValueTask<List<WorkflowTriggerDto>>> GetTriggersHandler { get; set; } = null!;

    /// <summary>Handler for fetching all board statuses for a project (used to resolve status names).</summary>
    [Inject]
    private IRequestHandler<GetProjectStatusesQuery, ValueTask<List<BoardStatusDto>>> GetStatusesHandler { get; set; } = null!;

    /// <summary>Handler for deleting a workflow trigger permanently.</summary>
    [Inject]
    private IRequestHandler<DeleteWorkflowTriggerCommand, ValueTask<Result>> DeleteTriggerHandler { get; set; } = null!;

    /// <summary>Handler for updating a workflow trigger (used for toggle enable/disable).</summary>
    [Inject]
    private IRequestHandler<UpdateWorkflowTriggerCommand, ValueTask<Result>> UpdateTriggerHandler { get; set; } = null!;

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
    /// Used to validate the project exists and is not archived before loading triggers.
    /// </summary>
    [Parameter]
    public Guid ProjectId { get; set; }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Page-Level State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>The loaded project data, null until fetch completes.</summary>
    private ProjectDto? _project;

    /// <summary>
    /// All triggers loaded from the server (unfiltered).
    /// Client-side filtering is applied on top of this list to avoid server round-trips
    /// every time the user changes the filter selection.
    /// </summary>
    private List<WorkflowTriggerDto> _allTriggers = [];

    /// <summary>
    /// Dictionary mapping BoardStatus ID → Name for fast O(1) lookups when
    /// resolving FromStatusId/ToStatusId to human-readable names in the list.
    /// </summary>
    private Dictionary<Guid, string> _statusNames = [];

    /// <summary>True while the initial project, trigger, and status data are being fetched.</summary>
    private bool _isLoading = true;

    /// <summary>True when the project was not found or is archived.</summary>
    private bool _isNotFound;

    /// <summary>
    /// Tracks the last loaded ProjectId to skip redundant loads when the
    /// parameter hasn't changed (same guard pattern as Board.razor and ProjectAgentsPage).
    /// </summary>
    private Guid _lastLoadedProjectId;

    /// <summary>Cancellation token source for all async operations in this component.</summary>
    private readonly CancellationTokenSource _cts = new();

    // ═══════════════════════════════════════════════════════════════════════════
    //  Filter State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The currently selected filter value for the trigger list.
    /// "all" = show all triggers, "enabled" = only enabled, "disabled" = only disabled.
    /// Filtering is client-side — no server round-trip on filter change.
    /// </summary>
    private string _filterValue = "all";

    /// <summary>
    /// The filtered view of <see cref="_allTriggers"/> based on <see cref="_filterValue"/>.
    /// Recomputed whenever the filter changes or the trigger list is refreshed.
    /// </summary>
    private IEnumerable<WorkflowTriggerDto> FilteredTriggers => _filterValue switch
    {
        "enabled" => _allTriggers.Where(t => t.IsEnabled),
        "disabled" => _allTriggers.Where(t => !t.IsEnabled),
        _ => _allTriggers
    };

    // ═══════════════════════════════════════════════════════════════════════════
    //  Per-Row Action State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tracks which trigger IDs currently have a toggle operation in progress.
    /// Used to show a spinner on the toggle button and prevent double-clicks.
    /// A HashSet is used for O(1) contains checks during rendering.
    /// </summary>
    private readonly HashSet<Guid> _togglingIds = [];

    /// <summary>
    /// Tracks which trigger IDs currently have a delete operation in progress.
    /// Used to disable the delete button and show a spinner while the operation runs.
    /// </summary>
    private readonly HashSet<Guid> _deletingIds = [];

    // ═══════════════════════════════════════════════════════════════════════════
    //  Lifecycle
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads the project, triggers, and board statuses when the component initializes
    /// or when the ProjectId parameter changes (e.g., navigating between projects).
    /// Uses <c>OnParametersSetAsync</c> because the route parameter may change without
    /// the component being re-created (same page, different project ID).
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        // Guard: skip redundant loads when navigating back to the same project
        if (ProjectId == _lastLoadedProjectId)
        {
            return;
        }

        _lastLoadedProjectId = ProjectId;
        await LoadDataAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Data Loading
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fetches the project, all workflow triggers, and all board statuses in sequence.
    /// Validates the project exists and is not archived before loading triggers.
    /// Builds the <see cref="_statusNames"/> lookup dictionary from the loaded statuses.
    /// </summary>
    private async Task LoadDataAsync()
    {
        // Reset all state for a fresh load
        _isLoading = true;
        _isNotFound = false;
        _project = null;
        _allTriggers = [];
        _statusNames = [];
        StateHasChanged();

        // ── Step 1: Fetch and validate the project ───────────────────────────
        var projectQuery = new GetProjectByIdQuery { Id = ProjectId };
        var projectResult = await GetProjectHandler.Handle(projectQuery, _cts.Token);

        if (projectResult.IsFailure || projectResult.Value.IsArchived)
        {
            // Project not found or archived — show error state
            _isNotFound = true;
            _isLoading = false;
            return;
        }

        _project = projectResult.Value;

        // If the user navigated directly (bookmark, shared link), sync the active
        // project context so the topbar shows the correct project name.
        if (ActiveProjectService.ActiveProject is null
            || ActiveProjectService.ActiveProject.Id != _project.Id)
        {
            await ActiveProjectService.SetActiveProjectAsync(_project);
        }

        // ── Step 2: Load triggers and board statuses in parallel ─────────────
        // Both queries are independent — convert to Task and use Task.WhenAll
        // to execute them truly concurrently and reduce total load time.
        var triggersTask = GetTriggersHandler.Handle(
            new GetWorkflowTriggersByProjectQuery { ProjectId = ProjectId },
            _cts.Token).AsTask();

        var statusesTask = GetStatusesHandler.Handle(
            new GetProjectStatusesQuery { ProjectId = ProjectId },
            _cts.Token).AsTask();

        await Task.WhenAll(triggersTask, statusesTask);

        _allTriggers = triggersTask.Result;

        var statuses = statusesTask.Result;

        // Build a fast lookup dictionary: StatusId → StatusName.
        // Used in the view to resolve FromStatusId/ToStatusId to display names.
        _statusNames = statuses.ToDictionary(s => s.Id, s => s.Name);

        _isLoading = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Filter
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates the active filter and triggers a re-render to show the filtered list.
    /// Called when the user changes the filter selection in the MudSelect.
    /// No server round-trip — filtering is applied client-side on <see cref="_allTriggers"/>.
    /// </summary>
    /// <param name="value">The new filter value: "all", "enabled", or "disabled".</param>
    private void OnFilterChanged(string value)
    {
        _filterValue = value;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Status Name Resolution
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves a BoardStatus ID to its display name using the pre-built lookup dictionary.
    /// Returns <see langword="null"/> if the status ID is not found in the dictionary,
    /// which indicates the status was deleted after the trigger was created.
    /// </summary>
    /// <param name="statusId">The BoardStatus ID to resolve.</param>
    /// <returns>The status name, or <see langword="null"/> if not found.</returns>
    private string? GetStatusName(Guid statusId)
    {
        return _statusNames.TryGetValue(statusId, out var name) ? name : null;
    }

    /// <summary>
    /// Returns true if either the FromStatus or ToStatus referenced by the trigger
    /// no longer exists in the project's board statuses. Used to show a warning icon
    /// on the trigger row (FR-22: stale status reference warning).
    /// </summary>
    /// <param name="trigger">The trigger to check for stale status references.</param>
    private bool HasStaleStatusReference(WorkflowTriggerDto trigger)
    {
        return !_statusNames.ContainsKey(trigger.FromStatusId)
            || !_statusNames.ContainsKey(trigger.ToStatusId);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Delete (FR-9)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Deletes the specified trigger after showing a confirmation dialog.
    /// On confirm + success: removes the trigger from the local list and shows a success snackbar.
    /// On confirm + failure: shows an error snackbar and leaves the list unchanged.
    /// On cancel: no action taken.
    /// </summary>
    /// <param name="trigger">The trigger to delete.</param>
    private async Task DeleteTriggerAsync(WorkflowTriggerDto trigger)
    {
        // FR-9: Show confirmation dialog before deleting
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete Trigger",
            $"Are you sure you want to delete trigger '{trigger.Name}'? This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirmed != true)
        {
            return;
        }

        _deletingIds.Add(trigger.Id);
        StateHasChanged();

        try
        {
            var command = new DeleteWorkflowTriggerCommand { TriggerId = trigger.Id };
            var result = await DeleteTriggerHandler.Handle(command, _cts.Token);

            if (result.IsSuccess)
            {
                // Remove from local list immediately — no need to reload from server
                _allTriggers.RemoveAll(t => t.Id == trigger.Id);
                Snackbar.Add($"Trigger '{trigger.Name}' deleted.", Severity.Success);
            }
            else
            {
                Snackbar.Add($"Failed to delete trigger: {result.Error}", Severity.Error);
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
            _deletingIds.Remove(trigger.Id);
            StateHasChanged();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Toggle Enable/Disable (FR-8)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Toggles the enabled/disabled state of the specified trigger.
    /// Calls <see cref="UpdateWorkflowTriggerCommand"/> with all existing trigger data
    /// unchanged except <c>IsEnabled</c>, which is inverted.
    /// On success: updates the local list entry and shows a success snackbar.
    /// On failure: shows an error snackbar and leaves the list unchanged.
    /// </summary>
    /// <param name="trigger">The trigger whose enabled state to toggle.</param>
    private async Task ToggleTriggerAsync(WorkflowTriggerDto trigger)
    {
        if (_togglingIds.Contains(trigger.Id))
        {
            // Prevent double-click while operation is in progress
            return;
        }

        _togglingIds.Add(trigger.Id);
        StateHasChanged();

        try
        {
            // Build the update command with all existing data preserved,
            // only flipping the IsEnabled flag.
            var command = new UpdateWorkflowTriggerCommand
            {
                TriggerId = trigger.Id,
                Name = trigger.Name,
                FromStatusId = trigger.FromStatusId,
                ToStatusId = trigger.ToStatusId,
                Prompt = trigger.Prompt,
                IsEnabled = !trigger.IsEnabled,
                // Map TriggerAgents to UpdateWorkflowTriggerAgentItem, preserving sort order
                Agents = trigger.TriggerAgents
                    .Select(a => new UpdateWorkflowTriggerAgentItem(a.AgentName, a.SortOrder))
                    .ToList()
            };

            var result = await UpdateTriggerHandler.Handle(command, _cts.Token);

            if (result.IsSuccess)
            {
                // Replace the trigger in the local list with an updated record
                // so the UI reflects the new enabled state without a server reload.
                var updatedTrigger = trigger with { IsEnabled = !trigger.IsEnabled };
                var index = _allTriggers.FindIndex(t => t.Id == trigger.Id);
                if (index >= 0)
                {
                    _allTriggers[index] = updatedTrigger;
                }

                var action = updatedTrigger.IsEnabled ? "enabled" : "disabled";
                Snackbar.Add($"Trigger '{trigger.Name}' {action}.", Severity.Success);
            }
            else
            {
                Snackbar.Add($"Failed to update trigger: {result.Error}", Severity.Error);
            }
        }
        catch (OperationCanceledException)
        {
            // Component is being disposed — silently ignore
        }
        catch (Exception)
        {
            Snackbar.Add("An unexpected error occurred while updating. Please try again.", Severity.Error);
        }
        finally
        {
            _togglingIds.Remove(trigger.Id);
            StateHasChanged();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Navigation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Navigates to the trigger creation form (FR-11: "Add Trigger" CTA).
    /// </summary>
    private void NavigateToAddTrigger()
    {
        NavigationManager.NavigateTo($"/projects/{ProjectId}/workflow/new");
    }

    /// <summary>
    /// Navigates to the trigger edit form for the specified trigger (FR-8: Edit action).
    /// </summary>
    /// <param name="triggerId">The ID of the trigger to edit.</param>
    private void NavigateToEditTrigger(Guid triggerId)
    {
        NavigationManager.NavigateTo($"/projects/{ProjectId}/workflow/{triggerId}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Error State Actions
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Opens the project selector modal so the user can pick a different project.
    /// Used as the CTA on the "Project not found" error state.
    /// </summary>
    private async Task OpenProjectSelectorAsync()
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = true,
            NoHeader = false
        };

        await DialogService.ShowAsync<ProjectSelectorModal>(
            "Select a Project",
            options);
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
