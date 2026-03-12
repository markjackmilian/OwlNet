using DispatchR.Abstractions.Send;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OwlNet.Application.Common.Models;
using OwlNet.Application.Projects.Queries.GetProjectById;
using OwlNet.Web.Services;

namespace OwlNet.Web.Components.Projects.Settings;

/// <summary>
/// Code-behind for <see cref="ProjectSettingsPage"/>. A smart page component that
/// loads and validates a project, then renders the settings UI for that project.
///
/// Currently contains a single section: Board Statuses (FR-2), which is delegated
/// to the <c>BoardStatusesPanel</c> component with the project ID scope.
///
/// Follows the same loading/validation/sync patterns as <see cref="WorkflowTriggersPage"/>
/// and <c>ProjectAgentsPage</c>: <c>OnParametersSetAsync</c> guard with
/// <c>_lastLoadedProjectId</c>, <c>ActiveProjectService</c> sync, and
/// <c>CancellationTokenSource</c> disposal.
///
/// Route: <c>/projects/{ProjectId:guid}/settings</c>
///
/// Spec reference: SPEC-project-board-statuses-settings.md (FR-1, FR-2)
/// </summary>
public sealed partial class ProjectSettingsPage : ComponentBase, IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  Injected Services
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Handler for fetching a project by its ID to validate existence and archived state.</summary>
    [Inject]
    private IRequestHandler<GetProjectByIdQuery, ValueTask<Result<ProjectDto>>> GetProjectHandler { get; set; } = null!;

    /// <summary>Service for tracking and syncing the currently active project in the topbar.</summary>
    [Inject]
    private ActiveProjectService ActiveProjectService { get; set; } = null!;

    /// <summary>MudBlazor dialog service for showing the project selector modal.</summary>
    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Parameters
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The project ID extracted from the URL route segment.
    /// Used to validate the project exists and is not archived before rendering settings.
    /// </summary>
    [Parameter]
    public Guid ProjectId { get; set; }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Page-Level State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>The loaded project data, null until fetch completes successfully.</summary>
    private ProjectDto? _project;

    /// <summary>True while the project data is being fetched from the server.</summary>
    private bool _isLoading = true;

    /// <summary>True when the project was not found or is archived.</summary>
    private bool _isNotFound;

    /// <summary>
    /// Tracks the last loaded ProjectId to skip redundant loads when the
    /// parameter hasn't changed (same guard pattern as WorkflowTriggersPage,
    /// Board.razor, and ProjectAgentsPage).
    /// </summary>
    private Guid _lastLoadedProjectId;

    /// <summary>
    /// Cancellation token source for all async operations in this component.
    /// Cancelled and disposed in <see cref="Dispose"/> to abort any in-flight
    /// handler calls if the user navigates away from the page.
    /// </summary>
    private readonly CancellationTokenSource _cts = new();

    // ═══════════════════════════════════════════════════════════════════════════
    //  Lifecycle
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads the project when the component initializes or when the ProjectId
    /// parameter changes (e.g., navigating between projects).
    /// Uses <c>OnParametersSetAsync</c> because the route parameter may change
    /// without the component being re-created (same page, different project ID).
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
    /// Fetches the project and validates it exists and is not archived.
    /// On success, syncs the <see cref="ActiveProjectService"/> so the topbar
    /// shows the correct project name (handles direct navigation via bookmark
    /// or shared link).
    /// </summary>
    private async Task LoadDataAsync()
    {
        // Reset all state for a fresh load
        _isLoading = true;
        _isNotFound = false;
        _project = null;
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

        // ── Step 2: Sync active project context ──────────────────────────────
        // If the user navigated directly (bookmark, shared link), sync the active
        // project context so the topbar shows the correct project name.
        if (ActiveProjectService.ActiveProject is null
            || ActiveProjectService.ActiveProject.Id != _project.Id)
        {
            await ActiveProjectService.SetActiveProjectAsync(_project);
        }

        _isLoading = false;
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
