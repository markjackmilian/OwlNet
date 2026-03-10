using System.Text.RegularExpressions;
using DispatchR.Abstractions.Send;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using OwlNet.Application.Agents.Models;
using OwlNet.Application.Common.Models;
using OwlNet.Application.SystemAgents.Commands.CreateSystemAgent;
using OwlNet.Application.SystemAgents.Commands.GenerateSystemAgentPrompt;

namespace OwlNet.Web.Components.Settings.SystemAgents;

/// <summary>
/// Code-behind for <see cref="CreateSystemAgentPage"/>. A smart settings page component
/// that guides the user through creating a new system agent definition via an LLM-assisted
/// wizard. The wizard has three steps:
///   Step 1 — Metadata Form: Collect agent type, name, display name, and description.
///   Step 2 — LLM Refinement Conversation: Chat-like Q&amp;A with the LLM to refine the
///             agent definition before generation.
///   Step 3 — Preview &amp; Edit: Review and edit the generated Markdown, then save.
///
/// Unlike project agents, system agents are global to the OwlNet installation and are
/// not scoped to a project. This page has no loading state — it starts directly at Step 1.
/// </summary>
public sealed partial class CreateSystemAgentPage : ComponentBase, IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  Injected Services
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Handler for sending the initial metadata and conversation to the LLM.</summary>
    [Inject]
    private IRequestHandler<GenerateSystemAgentPromptCommand, ValueTask<Result<AgentGenerationResponseDto>>> GenerateSystemAgentHandler { get; set; } = null!;

    /// <summary>Handler for persisting the final system agent definition.</summary>
    [Inject]
    private IRequestHandler<CreateSystemAgentCommand, ValueTask<Result<Guid>>> CreateSystemAgentHandler { get; set; } = null!;

    /// <summary>Used to navigate back to settings on success or via the Back button.</summary>
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    /// <summary>Used to show the "Discard generated content?" confirmation dialog.</summary>
    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    /// <summary>Used to show success and error toast notifications.</summary>
    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Constants
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Source-generated regex for valid agent names: alphanumeric + hyphens, must start
    /// and end with an alphanumeric character. Matches the spec requirement ^[a-zA-Z0-9-]+$.
    /// The stricter start/end rule prevents names like "-my-agent-" which are confusing.
    /// Using [GeneratedRegex] instead of new Regex(..., RegexOptions.Compiled) so the
    /// pattern is compiled at build time rather than at first call.
    /// </summary>
    [GeneratedRegex(@"^[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?$")]
    private static partial Regex AgentNameRegex();

    // ═══════════════════════════════════════════════════════════════════════════
    //  Wizard Step Enum
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Represents the three steps of the system agent creation wizard.</summary>
    private enum WizardStep
    {
        /// <summary>Step 1: Metadata form — agent type, name, display name, description.</summary>
        Form,

        /// <summary>Step 2: LLM refinement conversation — chat-based Q&amp;A.</summary>
        Conversation,

        /// <summary>Step 3: Preview and edit the generated Markdown before saving.</summary>
        Preview
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Page-Level State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>The current wizard step the user is on. Starts at Step 1 (Form).</summary>
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

    /// <summary>Tracks whether all form fields pass validation. Bound to MudForm's IsValid.</summary>
    private bool _formIsValid;

    /// <summary>
    /// The selected agent type — "primary", "subagent" (default), or "all".
    /// System agents support "all" mode unlike project agents.
    /// </summary>
    private string _agentType = "subagent";

    /// <summary>
    /// The stable agent identifier entered by the user. Becomes the default filename ({name}.md).
    /// Must be 2–50 characters, letters/numbers/hyphens only.
    /// </summary>
    private string _agentName = string.Empty;

    /// <summary>
    /// The human-readable display label entered by the user. Shown in the UI.
    /// Must be 2–100 characters.
    /// </summary>
    private string _displayName = string.Empty;

    /// <summary>
    /// The agent description entered by the user. Describes the agent's purpose.
    /// Sent to the LLM as context. Must be 10–500 characters.
    /// </summary>
    private string _description = string.Empty;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Step 2 — Conversation State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The full conversation history sent to the LLM with each request.
    /// Contains only "assistant" and "user" role messages (no system messages).
    /// </summary>
    private readonly List<ConversationMessage> _conversationHistory = [];

    /// <summary>
    /// Display messages for the chat UI. Mirrors conversation history but also
    /// includes UI-only messages (e.g., error messages with Retry buttons).
    /// </summary>
    private readonly List<DisplayMessage> _displayMessages = [];

    /// <summary>The user's current input in the chat text field.</summary>
    private string _userInput = string.Empty;

    /// <summary>True while an LLM request is in progress (disables input, shows typing indicator).</summary>
    private bool _isProcessing;

    /// <summary>Reference to the chat scroll container for potential auto-scroll behavior.</summary>
    private MudPaper? _chatContainer;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Step 3 — Preview State
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The generated (or manually templated) Markdown content. Editable by the user
    /// before saving. Populated either by the LLM response or the minimal template.
    /// </summary>
    private string _generatedMarkdown = string.Empty;

    /// <summary>True while the save operation is in progress.</summary>
    private bool _isSaving;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Display Message Record
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Internal record for rendering chat messages in the conversation UI.
    /// Extends the concept of ConversationMessage with an IsError flag for
    /// error-state messages that show a Retry button instead of plain text.
    /// </summary>
    /// <param name="Role">The message role — "assistant" or "user".</param>
    /// <param name="Content">The text content of the message.</param>
    /// <param name="IsError">Whether this message represents an error (shows Retry button).</param>
    private sealed record DisplayMessage(string Role, string Content, bool IsError = false);

    // ═══════════════════════════════════════════════════════════════════════════
    //  Step 1 — Form Validation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates the agent name field. Returns null if valid, or an error message string.
    /// Rules: required, 2–50 characters, alphanumeric + hyphens only, must start and end
    /// with a letter or number (prevents leading/trailing hyphens).
    /// </summary>
    /// <param name="value">The current field value to validate.</param>
    /// <returns>Null if valid; an error message string if invalid.</returns>
    private static string? ValidateAgentName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // Required attribute handles the empty case — return null to avoid duplicate messages
            return null;
        }

        if (value.Length < 2)
        {
            return "Name must be at least 2 characters.";
        }

        if (value.Length > 50)
        {
            return "Name must be 50 characters or fewer.";
        }

        if (!AgentNameRegex().IsMatch(value))
        {
            return "Name can only contain letters, numbers, and hyphens. Must start and end with a letter or number.";
        }

        return null;
    }

    /// <summary>
    /// Validates the display name field. Returns null if valid, or an error message string.
    /// Rules: required, 2–100 characters.
    /// </summary>
    /// <param name="value">The current field value to validate.</param>
    /// <returns>Null if valid; an error message string if invalid.</returns>
    private static string? ValidateDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // Required attribute handles the empty case
            return null;
        }

        if (value.Length < 2)
        {
            return "Display name must be at least 2 characters.";
        }

        if (value.Length > 100)
        {
            return "Display name must be 100 characters or fewer.";
        }

        return null;
    }

    /// <summary>
    /// Validates the description field. Returns null if valid, or an error message string.
    /// Rules: required, 10–500 characters.
    /// </summary>
    /// <param name="value">The current field value to validate.</param>
    /// <returns>Null if valid; an error message string if invalid.</returns>
    private static string? ValidateDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // Required attribute handles the empty case
            return null;
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
    //  Step 1 — Button Handlers
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles the "Generate with AI" button click. Validates the form, then sends the
    /// initial metadata to the LLM and transitions to the conversation step. (FR-14)
    /// </summary>
    private async Task HandleGenerateClickAsync()
    {
        // Double-check form validity before proceeding (belt-and-suspenders guard)
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

    /// <summary>
    /// Handles the "Create Manually" button click. Bypasses the LLM entirely and
    /// pre-populates the preview editor with a minimal frontmatter template derived
    /// from the form values. Transitions directly to Step 3. (FR-15, FR-16)
    /// </summary>
    private async Task HandleCreateManuallyClick()
    {
        // Double-check form validity before proceeding
        await _form.ValidateAsync();
        if (!_formIsValid)
        {
            return;
        }

        // FR-16: Build the minimal template with frontmatter pre-populated from the form.
        // The body is intentionally left empty for the user to fill in manually.
        _generatedMarkdown = $"""
            ---
            description: {_description}
            mode: {_agentType}
            ---

            """;

        _currentStep = WizardStep.Preview;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Step 2 — Conversation Actions
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sends the user's typed message to the LLM along with the full conversation
    /// history. Adds the user message to both the display and history lists. (FR-17)
    /// </summary>
    private async Task SendUserMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(_userInput))
        {
            return;
        }

        var userMessage = _userInput.Trim();
        _userInput = string.Empty;

        // Add user message to conversation history (sent to LLM) and display list (shown in UI)
        _conversationHistory.Add(new ConversationMessage("user", userMessage));
        _displayMessages.Add(new DisplayMessage("user", userMessage));

        await SendToLlmAsync(forceGenerate: false);
    }

    /// <summary>
    /// Handles Enter key press in the chat input to send the message.
    /// Shift+Enter allows multiline input without triggering a send.
    /// </summary>
    /// <param name="e">The keyboard event arguments.</param>
    private async Task HandleChatKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey && !_isProcessing && !string.IsNullOrWhiteSpace(_userInput))
        {
            await SendUserMessageAsync();
        }
    }

    /// <summary>
    /// Bypasses further LLM questions and forces immediate generation with all
    /// information gathered so far. (FR-17 — "Skip &amp; Generate" button)
    /// </summary>
    private async Task SkipAndGenerateAsync()
    {
        // Add a synthetic user message to make the skip action visible in the chat
        _displayMessages.Add(new DisplayMessage("user", "(Skipping further questions — generate now)"));
        await SendToLlmAsync(forceGenerate: true);
    }

    /// <summary>
    /// Retries the last LLM request after a failure. Removes the error message
    /// from the display list and resends with the current conversation state. (FR-17)
    /// </summary>
    private async Task RetryLastRequestAsync()
    {
        // Remove the last error message from display before retrying
        if (_displayMessages.Count > 0 && _displayMessages[^1].IsError)
        {
            _displayMessages.RemoveAt(_displayMessages.Count - 1);
        }

        await SendToLlmAsync(forceGenerate: false);
    }

    /// <summary>
    /// Core method that sends a request to the LLM via <see cref="GenerateSystemAgentPromptCommand"/>.
    /// Handles the response by either adding questions to the conversation (stay on Step 2)
    /// or transitioning to the preview step with the generated Markdown (go to Step 3).
    /// </summary>
    /// <param name="forceGenerate">
    /// When true, instructs the LLM to skip further questions and generate immediately.
    /// </param>
    private async Task SendToLlmAsync(bool forceGenerate)
    {
        _isProcessing = true;
        StateHasChanged();

        try
        {
            var command = new GenerateSystemAgentPromptCommand
            {
                AgentType = _agentType,
                AgentName = _agentName,
                AgentDescription = _description,
                ConversationHistory = _conversationHistory.ToList().AsReadOnly(),
                ForceGenerate = forceGenerate
            };

            var result = await GenerateSystemAgentHandler.Handle(command, _cts.Token);

            if (result.IsFailure)
            {
                // Handle LLM errors — show in conversation with retry option (FR-17)
                // Special case: LLM not configured → include a hint to visit settings
                var errorContent = result.Error.Contains("not configured", StringComparison.OrdinalIgnoreCase)
                    ? $"{result.Error} Go to Settings to configure it."
                    : $"Failed to get a response: {result.Error}. Please try again.";

                _displayMessages.Add(new DisplayMessage("assistant", errorContent, IsError: true));
                return;
            }

            var response = result.Value;

            // Add the assistant's message to both conversation history and display list
            _conversationHistory.Add(new ConversationMessage("assistant", response.AssistantMessage));
            _displayMessages.Add(new DisplayMessage("assistant", response.AssistantMessage));

            // Determine next action based on response type
            if (response.ResponseType == AgentGenerationResponseType.GeneratedMarkdown
                && response.GeneratedMarkdown is not null)
            {
                // LLM has generated the final Markdown — transition to preview step (FR-18)
                _generatedMarkdown = response.GeneratedMarkdown;
                _currentStep = WizardStep.Preview;
            }
            // If ResponseType is Questions, stay on conversation step — the questions
            // are already displayed via the AssistantMessage added above.
        }
        catch (OperationCanceledException)
        {
            // Component is being disposed — silently ignore the cancellation
        }
        catch (Exception)
        {
            // Unexpected error — show generic error message with retry option (FR-17)
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
    /// Saves the system agent definition via <see cref="CreateSystemAgentCommand"/>.
    /// On success, navigates to /settings and shows a success snackbar. (FR-22 to FR-24)
    /// On failure, shows an error snackbar and remains on Step 3.
    /// </summary>
    private async Task SaveSystemAgentAsync()
    {
        if (string.IsNullOrWhiteSpace(_generatedMarkdown))
        {
            Snackbar.Add("System agent content cannot be empty.", Severity.Warning);
            return;
        }

        _isSaving = true;
        StateHasChanged();

        try
        {
            var command = new CreateSystemAgentCommand
            {
                Name = _agentName,
                DisplayName = _displayName,
                Description = _description,
                Mode = _agentType,
                Content = _generatedMarkdown
            };

            var result = await CreateSystemAgentHandler.Handle(command, _cts.Token);

            if (result.IsSuccess)
            {
                // FR-23: Navigate to settings and show success snackbar
                Snackbar.Add($"System agent '{_agentName}' created successfully.", Severity.Success);
                NavigationManager.NavigateTo("/settings");
            }
            else
            {
                // FR-24: Show error snackbar, remain on preview page
                Snackbar.Add($"Failed to save system agent: {result.Error}", Severity.Error);
            }
        }
        catch (OperationCanceledException)
        {
            // Component is being disposed — silently ignore the cancellation
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
    /// Handles the "Back to Form" button click. Shows a confirmation dialog before
    /// discarding the generated content and returning to Step 1. (FR-21)
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
            // Reset all wizard state and return to Step 1 with the form cleared
            ResetWizardState();
            _currentStep = WizardStep.Form;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Navigation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Navigates back to the settings page. If the user has progressed past Step 1
    /// (i.e., is in the conversation or preview step), shows a confirmation dialog
    /// to prevent accidental loss of in-progress work. (FR-12)
    /// </summary>
    private async Task NavigateToSettingsAsync()
    {
        if (_currentStep != WizardStep.Form)
        {
            var confirmed = await DialogService.ShowMessageBoxAsync(
                "Leave Wizard",
                "Leave the wizard? Any progress will be lost.",
                yesText: "Leave",
                cancelText: "Stay");

            if (confirmed != true)
            {
                return;
            }
        }

        NavigationManager.NavigateTo("/settings");
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
    /// "Back to Form" from the preview step, ensuring a clean slate for re-entry.
    /// </summary>
    private void ResetWizardState()
    {
        _agentType = "subagent";
        _agentName = string.Empty;
        _displayName = string.Empty;
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
    /// <remarks>
    /// Cancels and disposes the component-scoped CancellationTokenSource to ensure
    /// any in-flight LLM or save operations are cleanly abandoned when the component
    /// is removed from the render tree.
    /// </remarks>
    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
