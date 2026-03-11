using DispatchR.Abstractions.Send;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OwlNet.Application.BoardStatuses.Queries.GetProjectStatuses;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Application.Projects.Queries.GetProjectAgents;
using OwlNet.Application.Projects.Queries.GetProjectById;
using OwlNet.Application.WorkflowTriggers.Commands.CreateWorkflowTrigger;
using OwlNet.Application.WorkflowTriggers.Commands.UpdateWorkflowTrigger;
using OwlNet.Application.WorkflowTriggers.Queries.GetWorkflowTriggersByProject;
using OwlNet.Web.Services;

namespace OwlNet.Web.Components.Projects.Workflow;

/// <summary>
/// Code-behind for <see cref="WorkflowTriggerFormPage"/>. A smart page component that
/// handles both creating and editing workflow triggers for a project.
///
/// In <b>create mode</b> (<c>TriggerId == Guid.Empty</c>): presents a blank form and
/// dispatches <see cref="CreateWorkflowTriggerCommand"/> on save.
///
/// In <b>edit mode</b> (<c>TriggerId != Guid.Empty</c>): loads the existing trigger,
/// pre-populates all form fields, and dispatches <see cref="UpdateWorkflowTriggerCommand"/>
/// on save.
///
/// Features:
/// - Manual inline validation with per-field error messages.
/// - "Improve Prompt" via <see cref="ILlmChatService"/> with accept/discard preview.
/// - Unsaved-changes guard on the Cancel/Back button.
/// - Stale agent detection: agents referenced by the trigger that no longer exist
///   on the filesystem are shown with a warning icon.
///
/// Follows the same loading/validation/sync patterns as <see cref="WorkflowTriggersPage"/>
/// and <c>AgentEditorPage</c>: <c>OnParametersSetAsync</c> guard, <c>ActiveProjectService</c>
/// sync, and <c>CancellationTokenSource</c> disposal.
/// </summary>
public sealed partial class WorkflowTriggerFormPage : ComponentBase, IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  Injected Services
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Handler for fetching a project by its ID.</summary>
    [Inject]
    private IRequestHandler<GetProjectByIdQuery, ValueTask<Result<ProjectDto>>> GetProjectHandler { get; set; } = null!;

    /// <summary>Handler for fetching all workflow triggers belonging to a project (used in edit mode to load the existing trigger).</summary>
    [Inject]
    private IRequestHandler<GetWorkflowTriggersByProjectQuery, ValueTask<List<WorkflowTriggerDto>>> GetTriggersHandler { get; set; } = null!;

    /// <summary>Handler for fetching all board statuses for a project (populates From/To Status dropdowns).</summary>
    [Inject]
    private IRequestHandler<GetProjectStatusesQuery, ValueTask<List<BoardStatusDto>>> GetStatusesHandler { get; set; } = null!;

    /// <summary>Handler for fetching all agent files available in the project (populates the agent checklist).</summary>
    [Inject]
    private IRequestHandler<GetProjectAgentsQuery, ValueTask<Result<IReadOnlyList<AgentFileDto>>>> GetAgentsHandler { get; set; } = null!;

    /// <summary>Handler for creating a new workflow trigger (create mode).</summary>
    [Inject]
    private IRequestHandler<CreateWorkflowTriggerCommand, ValueTask<Result<Guid>>> CreateTriggerHandler { get; set; } = null!;

    /// <summary>Handler for updating an existing workflow trigger (edit mode).</summary>
    [Inject]
    private IRequestHandler<UpdateWorkflowTriggerCommand, ValueTask<Result>> UpdateTriggerHandler { get; set; } = null!;

    /// <summary>LLM provider service used to check whether an LLM is configured before enabling "Improve Prompt".</summary>
    [Inject]
    private ILlmProviderService LlmProviderService { get; set; } = null!;

    /// <summary>LLM chat service used to send the "Improve Prompt" request.</summary>
    [Inject]
    private ILlmChatService LlmChatService { get; set; } = null!;

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
    //  Route Parameters
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The project ID extracted from the URL route segment.
    /// Used to validate the project exists and is not archived before loading form data.
    /// </summary>
    [Parameter]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// The trigger ID extracted from the URL route segment.
    /// <see cref="Guid.Empty"/> when the page is accessed via the <c>/new</c> route (create mode).
    /// A non-empty GUID when accessed via <c>/{triggerId}</c> (edit mode).
    /// </summary>
    [Parameter]
    public Guid TriggerId { get; set; }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Page-Level State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>The loaded project data, null until fetch completes.</summary>
    private ProjectDto? _project;

    /// <summary>
    /// The existing trigger loaded in edit mode. Null in create mode or while loading.
    /// Used to pre-populate form fields and detect stale agent references.
    /// </summary>
    private WorkflowTriggerDto? _existingTrigger;

    /// <summary>All board statuses for the project, used to populate the From/To Status dropdowns.</summary>
    private List<BoardStatusDto> _statuses = [];

    /// <summary>All agent files available in the project, used to populate the agent checklist.</summary>
    private IReadOnlyList<AgentFileDto> _availableAgents = [];

    /// <summary>True while the initial project, status, and agent data are being fetched.</summary>
    private bool _isLoading = true;

    /// <summary>True when the project was not found or is archived.</summary>
    private bool _isNotFound;

    /// <summary>
    /// Tracks the last loaded ProjectId to skip redundant loads when the
    /// parameter hasn't changed (same guard pattern as WorkflowTriggersPage).
    /// </summary>
    private Guid _lastLoadedProjectId;

    /// <summary>
    /// Tracks the last loaded TriggerId to skip redundant loads when the
    /// parameter hasn't changed (handles navigating between different triggers).
    /// </summary>
    private Guid _lastLoadedTriggerId;

    /// <summary>Cancellation token source for all async operations in this component.</summary>
    private readonly CancellationTokenSource _cts = new();

    // ═══════════════════════════════════════════════════════════════════════════
    //  Form Field State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>The trigger name field value. Required, max 150 characters.</summary>
    private string _name = string.Empty;

    /// <summary>The selected "From Status" ID. Guid.Empty means no selection.</summary>
    private Guid _fromStatusId = Guid.Empty;

    /// <summary>The selected "To Status" ID. Guid.Empty means no selection.</summary>
    private Guid _toStatusId = Guid.Empty;

    /// <summary>The prompt text field value. Required, max 10000 characters.</summary>
    private string _prompt = string.Empty;

    /// <summary>
    /// Whether the trigger is enabled. Defaults to true for new triggers.
    /// Only meaningful in edit mode — new triggers are always created enabled.
    /// </summary>
    private bool _isEnabled = true;

    /// <summary>
    /// The set of agent names currently checked in the agent checklist.
    /// Drives the <see cref="_orderedAgentNames"/> list.
    /// </summary>
    private List<string> _selectedAgentNames = [];

    /// <summary>
    /// The ordered list of selected agent names, used by <see cref="WorkflowTriggerAgentOrderList"/>.
    /// Maintained in sync with <see cref="_selectedAgentNames"/>:
    /// - Checking an agent appends it to the end of this list.
    /// - Unchecking an agent removes it from this list.
    /// - The user can reorder via the ▲/▼ buttons in the order list component.
    /// </summary>
    private List<string> _orderedAgentNames = [];

    /// <summary>
    /// The set of agent names referenced by the existing trigger (edit mode) that
    /// no longer exist in the project's .opencode/agents/ directory.
    /// These agents are shown with a warning icon in both the checklist and the order list.
    /// </summary>
    private HashSet<string> _staleAgentNames = [];

    // ═══════════════════════════════════════════════════════════════════════════
    //  Validation State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Per-field validation error messages. Keys are field names (e.g., "name", "fromStatus").
    /// Populated by <see cref="ValidateForm"/> and cleared on each save attempt.
    /// </summary>
    private Dictionary<string, string> _validationErrors = [];

    // ═══════════════════════════════════════════════════════════════════════════
    //  Save State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>True while a save operation is in progress. Disables the Save button and shows a spinner.</summary>
    private bool _isSaving;

    /// <summary>
    /// True when the user has modified any form field since the page loaded.
    /// Used by the unsaved-changes guard on the Cancel button.
    /// </summary>
    private bool _isDirty;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Improve Prompt State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>True when the configured LLM provider is ready to accept requests.</summary>
    private bool _isLlmConfigured;

    /// <summary>True while the "Improve Prompt" LLM request is in flight.</summary>
    private bool _isImprovingPrompt;

    /// <summary>
    /// The improved prompt text returned by the LLM. Displayed in the suggestion preview area.
    /// Null or empty when no suggestion is pending.
    /// </summary>
    private string _suggestedPrompt = string.Empty;

    /// <summary>
    /// True when a prompt suggestion is available and the preview area should be shown.
    /// Set to true after a successful LLM call; set to false when the user accepts or discards.
    /// </summary>
    private bool _showPromptSuggestion;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Computed Properties
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// True when the page is in create mode (TriggerId is Guid.Empty).
    /// False when in edit mode (TriggerId is a non-empty GUID).
    /// </summary>
    private bool IsCreateMode => TriggerId == Guid.Empty;

    /// <summary>
    /// The statuses available for the "To Status" dropdown.
    /// Excludes the currently selected "From Status" to prevent selecting the same status for both.
    /// </summary>
    private IEnumerable<BoardStatusDto> ToStatusOptions =>
        _statuses.Where(s => s.Id != _fromStatusId);

    // ═══════════════════════════════════════════════════════════════════════════
    //  Lifecycle
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads the project, board statuses, agents, and (in edit mode) the existing trigger
    /// when the component initializes or when route parameters change.
    /// Uses <c>OnParametersSetAsync</c> because the route parameters may change without
    /// the component being re-created (same page, different trigger ID).
    /// Guards against redundant loads when parameters haven't changed.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        // Guard: skip redundant loads when navigating back to the same trigger/project
        if (ProjectId == _lastLoadedProjectId && TriggerId == _lastLoadedTriggerId)
        {
            return;
        }

        _lastLoadedProjectId = ProjectId;
        _lastLoadedTriggerId = TriggerId;
        await LoadDataAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Data Loading
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fetches the project, board statuses, agents, and (in edit mode) the existing trigger.
    /// Validates the project exists and is not archived.
    /// In edit mode, pre-populates all form fields from the loaded trigger data.
    /// Calculates stale agent names (agents referenced by the trigger that no longer exist).
    /// </summary>
    private async Task LoadDataAsync()
    {
        // Reset all state for a fresh load
        _isLoading = true;
        _isNotFound = false;
        _project = null;
        _existingTrigger = null;
        _statuses = [];
        _availableAgents = [];
        _staleAgentNames = [];
        _validationErrors = [];
        _isDirty = false;
        _showPromptSuggestion = false;
        _suggestedPrompt = string.Empty;
        ResetFormFields();
        StateHasChanged();

        // ── Step 1: Fetch and validate the project ───────────────────────────
        var projectResult = await GetProjectHandler.Handle(
            new GetProjectByIdQuery { Id = ProjectId },
            _cts.Token);

        if (projectResult.IsFailure || projectResult.Value.IsArchived)
        {
            _isNotFound = true;
            _isLoading = false;
            return;
        }

        _project = projectResult.Value;

        // Sync the active project context so the topbar shows the correct project name
        // when the user navigates directly via bookmark or shared link.
        if (ActiveProjectService.ActiveProject is null
            || ActiveProjectService.ActiveProject.Id != _project.Id)
        {
            await ActiveProjectService.SetActiveProjectAsync(_project);
        }

        // ── Step 2: Load board statuses, agents, and LLM config in parallel ──
        // All three queries are independent — convert to Task and use Task.WhenAll
        // to execute them truly concurrently and reduce total load time.
        var statusesTask = GetStatusesHandler.Handle(
            new GetProjectStatusesQuery { ProjectId = ProjectId },
            _cts.Token).AsTask();

        var agentsTask = GetAgentsHandler.Handle(
            new GetProjectAgentsQuery { ProjectId = ProjectId },
            _cts.Token).AsTask();

        var llmConfigTask = LlmProviderService.GetConfigurationAsync(_cts.Token);

        await Task.WhenAll(statusesTask, agentsTask, llmConfigTask);

        _statuses = statusesTask.Result;

        var agentsResult = agentsTask.Result;
        _availableAgents = agentsResult.IsSuccess ? agentsResult.Value : [];

        var llmConfig = llmConfigTask.Result;
        _isLlmConfigured = llmConfig.IsSuccess && llmConfig.Value.IsConfigured;

        // ── Step 3: In edit mode, load the existing trigger and pre-populate ──
        if (!IsCreateMode)
        {
            var allTriggers = await GetTriggersHandler.Handle(
                new GetWorkflowTriggersByProjectQuery { ProjectId = ProjectId },
                _cts.Token);

            // Find the specific trigger by ID from the full list
            _existingTrigger = allTriggers.FirstOrDefault(t => t.Id == TriggerId);

            if (_existingTrigger is null)
            {
                // Trigger not found — may have been deleted by another session or URL is invalid
                _isNotFound = true;
                _isLoading = false;
                return;
            }

            PopulateFormFromTrigger(_existingTrigger);
        }

        _isLoading = false;
    }

    /// <summary>
    /// Resets all form fields to their default (empty/false) values.
    /// Called at the start of <see cref="LoadDataAsync"/> to ensure a clean slate.
    /// </summary>
    private void ResetFormFields()
    {
        _name = string.Empty;
        _fromStatusId = Guid.Empty;
        _toStatusId = Guid.Empty;
        _prompt = string.Empty;
        _isEnabled = true;
        _selectedAgentNames = [];
        _orderedAgentNames = [];
    }

    /// <summary>
    /// Pre-populates all form fields from an existing <see cref="WorkflowTriggerDto"/>.
    /// Also calculates <see cref="_staleAgentNames"/> — agents referenced by the trigger
    /// that no longer exist in the project's .opencode/agents/ directory.
    /// </summary>
    /// <param name="trigger">The existing trigger to populate the form from.</param>
    private void PopulateFormFromTrigger(WorkflowTriggerDto trigger)
    {
        _name = trigger.Name;
        _fromStatusId = trigger.FromStatusId;
        _toStatusId = trigger.ToStatusId;
        _prompt = trigger.Prompt;
        _isEnabled = trigger.IsEnabled;

        // Restore agent selection and order from the trigger's agent list,
        // sorted by SortOrder to preserve the user's previously configured sequence.
        var orderedAgents = trigger.TriggerAgents
            .OrderBy(a => a.SortOrder)
            .Select(a => a.AgentName)
            .ToList();

        _orderedAgentNames = orderedAgents;
        _selectedAgentNames = new List<string>(orderedAgents);

        // Calculate stale agents: names in the trigger that are not in the available agents list.
        // These are shown with a warning icon in the checklist and order list (FR-21).
        var availableNames = _availableAgents.Select(a => a.FileName).ToHashSet();
        _staleAgentNames = _selectedAgentNames
            .Where(name => !availableNames.Contains(name))
            .ToHashSet();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Status Dropdown Handlers
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles the From Status dropdown value change.
    /// Updates <see cref="_fromStatusId"/> and clears <see cref="_toStatusId"/> if it
    /// now equals the new From Status (to prevent same-status selection).
    /// </summary>
    /// <param name="value">The newly selected From Status ID.</param>
    private void OnFromStatusChanged(Guid value)
    {
        _fromStatusId = value;

        // If the current To Status is the same as the newly selected From Status,
        // clear it to force the user to pick a valid To Status.
        if (_toStatusId == _fromStatusId)
        {
            _toStatusId = Guid.Empty;
        }

        MarkDirty();
    }

    /// <summary>
    /// Handles the To Status dropdown value change.
    /// Updates <see cref="_toStatusId"/> and marks the form as dirty.
    /// </summary>
    /// <param name="value">The newly selected To Status ID.</param>
    private void OnToStatusChanged(Guid value)
    {
        _toStatusId = value;
        MarkDirty();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Agent Checklist Interaction
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles a checkbox toggle in the agent checklist.
    /// When an agent is checked, it is appended to the end of <see cref="_orderedAgentNames"/>.
    /// When unchecked, it is removed from <see cref="_orderedAgentNames"/>.
    /// The <see cref="_selectedAgentNames"/> set is kept in sync.
    /// </summary>
    /// <param name="agentName">The agent file name (without .md extension) being toggled.</param>
    /// <param name="isChecked">True if the agent was just checked; false if unchecked.</param>
    private void OnAgentCheckChanged(string agentName, bool isChecked)
    {
        if (isChecked)
        {
            if (!_selectedAgentNames.Contains(agentName))
            {
                _selectedAgentNames.Add(agentName);
                _orderedAgentNames.Add(agentName);
            }
        }
        else
        {
            _selectedAgentNames.Remove(agentName);
            _orderedAgentNames.Remove(agentName);
        }

        MarkDirty();
    }

    /// <summary>
    /// Handles the <see cref="WorkflowTriggerAgentOrderList.AgentNamesChanged"/> callback.
    /// Updates <see cref="_orderedAgentNames"/> with the new order provided by the child component.
    /// </summary>
    /// <param name="newOrder">The reordered list of agent names from the order list component.</param>
    private void OnAgentOrderChanged(List<string> newOrder)
    {
        _orderedAgentNames = newOrder;
        MarkDirty();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Dirty State Tracking
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Marks the form as dirty (having unsaved changes).
    /// Called whenever the user modifies any form field.
    /// </summary>
    private void MarkDirty() => _isDirty = true;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Validation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates all form fields and populates <see cref="_validationErrors"/> with
    /// per-field error messages for any invalid fields.
    /// </summary>
    /// <returns>True if all fields are valid; false if any validation errors were found.</returns>
    private bool ValidateForm()
    {
        _validationErrors = [];

        // Name: required, max 150 characters
        if (string.IsNullOrWhiteSpace(_name))
        {
            _validationErrors["name"] = "Name is required.";
        }
        else if (_name.Length > 150)
        {
            _validationErrors["name"] = "Name must be 150 characters or fewer.";
        }

        // From Status: required
        if (_fromStatusId == Guid.Empty)
        {
            _validationErrors["fromStatus"] = "From Status is required.";
        }

        // To Status: required
        if (_toStatusId == Guid.Empty)
        {
            _validationErrors["toStatus"] = "To Status is required.";
        }

        // From and To must differ
        if (_fromStatusId != Guid.Empty && _toStatusId != Guid.Empty
            && _fromStatusId == _toStatusId)
        {
            _validationErrors["toStatus"] = "To Status must be different from From Status.";
        }

        // Prompt: required, max 10000 characters
        if (string.IsNullOrWhiteSpace(_prompt))
        {
            _validationErrors["prompt"] = "Prompt is required.";
        }
        else if (_prompt.Length > 10000)
        {
            _validationErrors["prompt"] = "Prompt must be 10,000 characters or fewer.";
        }

        // At least one agent must be selected
        if (_selectedAgentNames.Count == 0)
        {
            _validationErrors["agents"] = "At least one agent must be selected.";
        }

        return _validationErrors.Count == 0;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Save
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates the form and, if valid, dispatches either
    /// <see cref="CreateWorkflowTriggerCommand"/> (create mode) or
    /// <see cref="UpdateWorkflowTriggerCommand"/> (edit mode).
    /// On success: navigates to the trigger list with a success snackbar.
    /// On failure: shows an error snackbar and remains on the form.
    /// </summary>
    private async Task SaveAsync()
    {
        if (_isSaving)
        {
            return;
        }

        // Run validation — show inline errors if any field is invalid
        if (!ValidateForm())
        {
            return;
        }

        _isSaving = true;
        StateHasChanged();

        try
        {
            if (IsCreateMode)
            {
                await CreateTriggerAsync();
            }
            else
            {
                await UpdateTriggerAsync();
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

    /// <summary>
    /// Dispatches <see cref="CreateWorkflowTriggerCommand"/> with the current form values.
    /// On success: navigates to the trigger list with a success snackbar.
    /// On failure: shows an error snackbar.
    /// </summary>
    private async Task CreateTriggerAsync()
    {
        // Build the ordered agent list with zero-based SortOrder values
        var agents = _orderedAgentNames
            .Select((name, index) => new CreateWorkflowTriggerAgentItem(name, index))
            .ToList();

        var command = new CreateWorkflowTriggerCommand
        {
            ProjectId = ProjectId,
            Name = _name.Trim(),
            FromStatusId = _fromStatusId,
            ToStatusId = _toStatusId,
            Prompt = _prompt.Trim(),
            Agents = agents
        };

        var result = await CreateTriggerHandler.Handle(command, _cts.Token);

        if (result.IsSuccess)
        {
            Snackbar.Add($"Trigger '{_name.Trim()}' created successfully.", Severity.Success);
            _isDirty = false;
            NavigationManager.NavigateTo($"/projects/{ProjectId}/workflow");
        }
        else
        {
            Snackbar.Add($"Failed to create trigger: {result.Error}", Severity.Error);
        }
    }

    /// <summary>
    /// Dispatches <see cref="UpdateWorkflowTriggerCommand"/> with the current form values.
    /// On success: navigates to the trigger list with a success snackbar.
    /// On failure: shows an error snackbar.
    /// </summary>
    private async Task UpdateTriggerAsync()
    {
        // Build the ordered agent list with zero-based SortOrder values
        var agents = _orderedAgentNames
            .Select((name, index) => new UpdateWorkflowTriggerAgentItem(name, index))
            .ToList();

        var command = new UpdateWorkflowTriggerCommand
        {
            TriggerId = TriggerId,
            Name = _name.Trim(),
            FromStatusId = _fromStatusId,
            ToStatusId = _toStatusId,
            Prompt = _prompt.Trim(),
            IsEnabled = _isEnabled,
            Agents = agents
        };

        var result = await UpdateTriggerHandler.Handle(command, _cts.Token);

        if (result.IsSuccess)
        {
            Snackbar.Add($"Trigger '{_name.Trim()}' updated successfully.", Severity.Success);
            _isDirty = false;
            NavigationManager.NavigateTo($"/projects/{ProjectId}/workflow");
        }
        else
        {
            Snackbar.Add($"Failed to update trigger: {result.Error}", Severity.Error);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Improve Prompt (FR-15 to FR-20)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sends the current prompt to the LLM for improvement.
    /// Includes context about the trigger (From/To status names and selected agents)
    /// so the LLM can produce a contextually relevant improvement (FR-19).
    /// On success: shows the suggestion preview area.
    /// On failure: shows an error snackbar (FR-20).
    /// </summary>
    private async Task ImprovePromptAsync()
    {
        if (_isImprovingPrompt || !_isLlmConfigured || _prompt.Length < 10)
        {
            return;
        }

        _isImprovingPrompt = true;
        _showPromptSuggestion = false;
        StateHasChanged();

        try
        {
            // Resolve status names for context — fall back to ID string if status was deleted
            var fromStatusName = _statuses.FirstOrDefault(s => s.Id == _fromStatusId)?.Name
                ?? _fromStatusId.ToString("N")[..8];
            var toStatusName = _statuses.FirstOrDefault(s => s.Id == _toStatusId)?.Name
                ?? _toStatusId.ToString("N")[..8];

            var systemPrompt =
                "You are an expert at writing clear, actionable prompts for AI coding agents. " +
                "Improve the following prompt to be more specific, actionable, and contextually relevant. " +
                "Return only the improved prompt text, without any explanation or preamble.";

            var userMessage = $"Improve this workflow trigger prompt for a transition from " +
                $"'{fromStatusName}' to '{toStatusName}' using agents: " +
                $"{string.Join(", ", _selectedAgentNames)}.\n\nOriginal prompt:\n{_prompt}";

            var messages = new List<ChatMessage>
            {
                new("user", userMessage)
            };

            var result = await LlmChatService.SendChatCompletionAsync(
                systemPrompt,
                messages,
                temperature: 0.4,
                cancellationToken: _cts.Token);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value))
            {
                _suggestedPrompt = result.Value;
                _showPromptSuggestion = true;
            }
            else
            {
                Snackbar.Add("Prompt improvement failed — please try again.", Severity.Error);
            }
        }
        catch (OperationCanceledException)
        {
            // Component is being disposed — silently ignore
        }
        catch (Exception)
        {
            Snackbar.Add("Prompt improvement failed — please try again.", Severity.Error);
        }
        finally
        {
            _isImprovingPrompt = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Accepts the LLM-suggested prompt improvement by replacing the current prompt
    /// with the suggested text and dismissing the preview area.
    /// </summary>
    private void AcceptSuggestedPrompt()
    {
        _prompt = _suggestedPrompt;
        _showPromptSuggestion = false;
        _suggestedPrompt = string.Empty;
        MarkDirty();
    }

    /// <summary>
    /// Discards the LLM-suggested prompt improvement and dismisses the preview area.
    /// The original prompt is left unchanged.
    /// </summary>
    private void DiscardSuggestedPrompt()
    {
        _showPromptSuggestion = false;
        _suggestedPrompt = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Navigation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Navigates back to the trigger list. If the form has unsaved changes,
    /// shows a confirmation dialog to prevent accidental data loss (FR-edge: unsaved changes guard).
    /// </summary>
    private async Task NavigateBackAsync()
    {
        if (_isDirty)
        {
            var confirmed = await DialogService.ShowMessageBoxAsync(
                "Unsaved Changes",
                "You have unsaved changes. Are you sure you want to leave?",
                yesText: "Leave",
                cancelText: "Stay");

            if (confirmed != true)
            {
                return;
            }
        }

        NavigationManager.NavigateTo($"/projects/{ProjectId}/workflow");
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
