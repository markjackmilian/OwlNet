using System.Text.RegularExpressions;
using DispatchR.Abstractions.Send;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using OwlNet.Application.Agents.Commands.GenerateAgentPrompt;
using OwlNet.Application.Agents.Commands.SaveAgent;
using OwlNet.Application.Agents.Models;
using OwlNet.Application.Common.Models;
using OwlNet.Application.Projects.Queries.GetProjectById;
using OwlNet.Web.Services;

namespace OwlNet.Web.Components.Projects.Agents;

/// <summary>
/// Code-behind for <see cref="CreateAgentPage"/>. A smart page component that guides
/// the user through creating a new AI agent definition file via an LLM-assisted wizard.
/// The wizard has three steps:
///   Step 1 — Initial Form: Collect agent type, name, and description.
///   Step 2 — LLM Refinement Conversation: Chat-like Q&amp;A with the LLM.
///   Step 3 — Preview &amp; Edit: Review and edit the generated Markdown, then save.
/// </summary>
public sealed partial class CreateAgentPage : ComponentBase, IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  Injected Services
    // ═══════════════════════════════════════════════════════════════════════════

    [Inject] private IRequestHandler<GetProjectByIdQuery, ValueTask<Result<ProjectDto>>> GetProjectHandler { get; set; } = null!;
    [Inject] private IRequestHandler<GenerateAgentPromptCommand, ValueTask<Result<AgentGenerationResponseDto>>> GenerateAgentHandler { get; set; } = null!;
    [Inject] private IRequestHandler<SaveAgentCommand, ValueTask<Result<string>>> SaveAgentHandler { get; set; } = null!;
    [Inject] private ActiveProjectService ActiveProjectService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Constants
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Regex pattern for valid agent names: alphanumeric + hyphens, must start/end with alphanumeric.</summary>
    private const string AgentNamePattern = @"^[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?$";

    /// <summary>Compiled regex for agent name validation (avoids recompilation on each validation call).</summary>
    private static readonly Regex AgentNameRegex = new(AgentNamePattern, RegexOptions.Compiled);

    // ═══════════════════════════════════════════════════════════════════════════
    //  Wizard Step Enum
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Represents the three steps of the agent creation wizard.</summary>
    private enum WizardStep { Form, Conversation, Preview }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Parameters
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>The project ID extracted from the URL route segment.</summary>
    [Parameter]
    public Guid ProjectId { get; set; }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Page-Level State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>The loaded project data, null until fetch completes.</summary>
    private ProjectDto? _project;

    /// <summary>True while the initial project data is being fetched.</summary>
    private bool _isLoading = true;

    /// <summary>True when the project was not found or is archived.</summary>
    private bool _isNotFound;

    /// <summary>Tracks the last loaded ProjectId to skip redundant loads when the parameter hasn't changed.</summary>
    private Guid _lastLoadedProjectId;

    /// <summary>The current wizard step the user is on.</summary>
    private WizardStep _currentStep = WizardStep.Form;

    /// <summary>Cancellation token source for all async operations in this component.</summary>
    private readonly CancellationTokenSource _cts = new();

    /// <summary>Reference to the ErrorBoundary for recovering from rendering errors.</summary>
    private ErrorBoundary? _errorBoundary;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Step 1 — Form State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Reference to the MudForm for programmatic validation.</summary>
    private MudForm _form = null!;

    /// <summary>Tracks whether all form fields pass validation.</summary>
    private bool _formIsValid;

    /// <summary>The selected agent type — "primary" or "subagent" (default).</summary>
    private string _agentType = "subagent";

    /// <summary>The agent name entered by the user (becomes the filename).</summary>
    private string _agentName = string.Empty;

    /// <summary>The agent description entered by the user.</summary>
    private string _description = string.Empty;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Step 2 — Conversation State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The full conversation history sent to the LLM with each request.
    /// Contains only "assistant" and "user" role messages.
    /// </summary>
    private readonly List<ConversationMessage> _conversationHistory = [];

    /// <summary>
    /// Display messages for the chat UI. Includes conversation history entries
    /// plus UI-only messages (e.g., error messages with retry buttons).
    /// </summary>
    private readonly List<DisplayMessage> _displayMessages = [];

    /// <summary>The user's current input in the chat text field.</summary>
    private string _userInput = string.Empty;

    /// <summary>True while an LLM request is in progress (disables input, shows spinner).</summary>
    private bool _isProcessing;

    /// <summary>Reference to the chat container for auto-scroll behavior.</summary>
    private MudPaper? _chatContainer;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Step 3 — Preview State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>The generated (and potentially user-edited) Markdown content.</summary>
    private string _generatedMarkdown = string.Empty;

    /// <summary>True while the save operation is in progress.</summary>
    private bool _isSaving;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Display Message Record
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Internal record for rendering chat messages. Extends ConversationMessage
    /// with an IsError flag for error-state messages that show a Retry button.
    /// </summary>
    /// <param name="Role">The message role — "assistant" or "user".</param>
    /// <param name="Content">The text content of the message.</param>
    /// <param name="IsError">Whether this message represents an error (shows Retry button).</param>
    private sealed record DisplayMessage(string Role, string Content, bool IsError = false);

    // ═══════════════════════════════════════════════════════════════════════════
    //  Lifecycle
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads the project when the component initializes or when the ProjectId
    /// parameter changes. Uses OnParametersSetAsync because the route parameter
    /// may change without the component being re-created.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        if (ProjectId == _lastLoadedProjectId)
        {
            return;
        }

        _lastLoadedProjectId = ProjectId;
        await LoadProjectAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Data Loading
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fetches the project by ID and validates it exists and is not archived.
    /// Sets the project as active in the topbar context if needed (FR-3).
    /// </summary>
    private async Task LoadProjectAsync()
    {
        _isLoading = true;
        _isNotFound = false;
        _project = null;
        StateHasChanged();

        var query = new GetProjectByIdQuery { Id = ProjectId };
        var result = await GetProjectHandler.Handle(query, _cts.Token);

        if (result.IsFailure || result.Value.IsArchived)
        {
            // Project not found or archived — show error state (FR-2)
            _isNotFound = true;
            _isLoading = false;
            return;
        }

        _project = result.Value;

        // FR-3: If the user navigated directly (bookmark, shared link), sync
        // the active project context so the topbar shows the correct project.
        if (ActiveProjectService.ActiveProject is null
            || ActiveProjectService.ActiveProject.Id != _project.Id)
        {
            await ActiveProjectService.SetActiveProjectAsync(_project);
        }

        _isLoading = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Step 1 — Form Validation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates the agent name field. Returns null if valid, or an error message string.
    /// Rules: required, 2-50 characters, alphanumeric + hyphens only, must start/end
    /// with a letter or number.
    /// </summary>
    private static string? ValidateAgentName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null; // Required attribute handles empty case
        }

        if (value.Length < 2)
        {
            return "Agent name must be at least 2 characters.";
        }

        if (value.Length > 50)
        {
            return "Agent name must be 50 characters or fewer.";
        }

        if (!AgentNameRegex.IsMatch(value))
        {
            return "Agent name can only contain letters, numbers, and hyphens. Must start and end with a letter or number.";
        }

        return null;
    }

    /// <summary>
    /// Validates the description field. Returns null if valid, or an error message string.
    /// Rules: required, 10-500 characters.
    /// </summary>
    private static string? ValidateDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null; // Required attribute handles empty case
        }

        if (value.Length < 10)
        {
            return "Description must be at least 10 characters.";
        }

        if (value.Length > 500)
        {
            return "Description must be 500 characters or fewer.";
        }

        return null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Step 1 → Step 2 Transition
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles the "Generate" button click. Validates the form, then sends the
    /// initial agent data to the LLM and transitions to the conversation step.
    /// (FR-7, FR-8, FR-10)
    /// </summary>
    private async Task HandleGenerateClickAsync()
    {
        // Double-check form validity before proceeding
        await _form.ValidateAsync();
        if (!_formIsValid)
        {
            return;
        }

        // Clear any previous conversation state (in case user came back from preview)
        _conversationHistory.Clear();
        _displayMessages.Clear();
        _userInput = string.Empty;
        _generatedMarkdown = string.Empty;

        // Transition to conversation step and send initial request to LLM
        _currentStep = WizardStep.Conversation;
        await SendToLlmAsync(forceGenerate: false);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Step 2 — Conversation Actions
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sends the user's typed message to the LLM along with the full conversation
    /// history. Adds the user message to both the display and history lists.
    /// (FR-12, FR-13)
    /// </summary>
    private async Task SendUserMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(_userInput))
        {
            return;
        }

        var userMessage = _userInput.Trim();
        _userInput = string.Empty;

        // Add user message to conversation history and display
        _conversationHistory.Add(new ConversationMessage("user", userMessage));
        _displayMessages.Add(new DisplayMessage("user", userMessage));

        await SendToLlmAsync(forceGenerate: false);
    }

    /// <summary>
    /// Handles Enter key press in the chat input to send the message.
    /// Shift+Enter allows multiline input without sending.
    /// </summary>
    private async Task HandleChatKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey && !_isProcessing && !string.IsNullOrWhiteSpace(_userInput))
        {
            await SendUserMessageAsync();
        }
    }

    /// <summary>
    /// Bypasses further LLM questions and forces immediate generation.
    /// Sends a force-generate request with all information gathered so far.
    /// (FR-12 — "Skip &amp; Generate" button)
    /// </summary>
    private async Task SkipAndGenerateAsync()
    {
        _displayMessages.Add(new DisplayMessage("user", "(Skipping further questions — generate now)"));
        await SendToLlmAsync(forceGenerate: true);
    }

    /// <summary>
    /// Retries the last LLM request after a failure. Removes the error message
    /// from the display list and resends with the current conversation state.
    /// (FR-17)
    /// </summary>
    private async Task RetryLastRequestAsync()
    {
        // Remove the last error message from display
        if (_displayMessages.Count > 0 && _displayMessages[^1].IsError)
        {
            _displayMessages.RemoveAt(_displayMessages.Count - 1);
        }

        await SendToLlmAsync(forceGenerate: false);
    }

    /// <summary>
    /// Core method that sends a request to the LLM via the GenerateAgentPromptCommand.
    /// Handles the response by either adding questions to the conversation or
    /// transitioning to the preview step with the generated markdown.
    /// </summary>
    /// <param name="forceGenerate">
    /// When true, instructs the LLM to skip questions and generate immediately.
    /// </param>
    private async Task SendToLlmAsync(bool forceGenerate)
    {
        _isProcessing = true;
        StateHasChanged();

        try
        {
            var command = new GenerateAgentPromptCommand
            {
                AgentType = _agentType,
                AgentName = _agentName,
                AgentDescription = _description,
                ConversationHistory = _conversationHistory.ToList().AsReadOnly(),
                ForceGenerate = forceGenerate
            };

            var result = await GenerateAgentHandler.Handle(command, _cts.Token);

            if (result.IsFailure)
            {
                // Handle LLM errors — show in conversation with retry option (FR-17)
                // Special case: LLM not configured → include link to settings
                var errorContent = result.Error.Contains("not configured", StringComparison.OrdinalIgnoreCase)
                    ? $"{result.Error} Go to Settings to configure it."
                    : $"Failed to get a response: {result.Error}. Please try again.";

                _displayMessages.Add(new DisplayMessage("assistant", errorContent, IsError: true));
                return;
            }

            var response = result.Value;

            // Add the assistant's message to both conversation history and display
            _conversationHistory.Add(new ConversationMessage("assistant", response.AssistantMessage));
            _displayMessages.Add(new DisplayMessage("assistant", response.AssistantMessage));

            // Determine next action based on response type
            if (response.ResponseType == AgentGenerationResponseType.GeneratedMarkdown
                && response.GeneratedMarkdown is not null)
            {
                // LLM has generated the final markdown — transition to preview (FR-18)
                _generatedMarkdown = response.GeneratedMarkdown;
                _currentStep = WizardStep.Preview;
            }
            // If ResponseType is Questions, stay on conversation step — the questions
            // are already displayed via the AssistantMessage added above.
        }
        catch (OperationCanceledException)
        {
            // Component is being disposed — silently ignore
        }
        catch (Exception)
        {
            // Unexpected error — show generic error message with retry (FR-17)
            _displayMessages.Add(new DisplayMessage(
                "assistant",
                "An unexpected error occurred. Please try again.",
                IsError: true));
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Step 3 — Preview & Save Actions
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Saves the agent definition file to the project's .opencode/agents/ directory.
    /// On success, navigates to the agent list and shows a success snackbar.
    /// On failure, shows an error snackbar and stays on the preview page.
    /// (FR-22)
    /// </summary>
    private async Task SaveAgentAsync()
    {
        if (string.IsNullOrWhiteSpace(_generatedMarkdown))
        {
            Snackbar.Add("Agent content cannot be empty.", Severity.Warning);
            return;
        }

        _isSaving = true;
        StateHasChanged();

        try
        {
            var command = new SaveAgentCommand
            {
                ProjectId = ProjectId,
                AgentName = _agentName,
                Content = _generatedMarkdown
            };

            var result = await SaveAgentHandler.Handle(command, _cts.Token);

            if (result.IsSuccess)
            {
                // FR-22c: Navigate to agent list and show success snackbar
                Snackbar.Add($"Agent '{_agentName}' created successfully.", Severity.Success);
                NavigationManager.NavigateTo($"/projects/{ProjectId}/agents");
            }
            else
            {
                // FR-22d: Show error snackbar, remain on preview page
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

    /// <summary>
    /// Handles the "Back to Form" button click. Shows a confirmation dialog
    /// before discarding the generated content and returning to Step 1.
    /// (FR-21)
    /// </summary>
    private async Task HandleBackToFormAsync()
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Discard Generated Content",
            "Discard generated content and start over?",
            yesText: "Discard",
            cancelText: "Cancel");

        if (confirmed == true)
        {
            // Reset all state and return to Step 1 with form cleared
            ResetWizardState();
            _currentStep = WizardStep.Form;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Navigation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Navigates back to the agent list page for the current project (FR-5).
    /// </summary>
    private void NavigateToAgentList()
    {
        NavigationManager.NavigateTo($"/projects/{ProjectId}/agents");
    }

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
    //  State Reset
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resets all wizard state to initial values. Called when the user confirms
    /// "Back to Form" from the preview step.
    /// </summary>
    private void ResetWizardState()
    {
        _agentType = "subagent";
        _agentName = string.Empty;
        _description = string.Empty;
        _formIsValid = false;
        _conversationHistory.Clear();
        _displayMessages.Clear();
        _userInput = string.Empty;
        _generatedMarkdown = string.Empty;
        _isProcessing = false;
        _isSaving = false;
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
