# SPEC: Agent Creation Wizard

> **Status:** Todo
> **Created:** 2026-03-10
> **Author:** owl-planner + user
> **Priority:** High (P2 — depends on P1)
> **Estimated Complexity:** L

## Context

After SPEC-P1 establishes the agent list page and the `IAgentFileService` infrastructure, users need a way to create new OpenCode-compatible agents. Rather than requiring users to manually write Markdown files with YAML frontmatter, OwlNet provides an LLM-assisted creation wizard that guides the user through a conversational refinement process.

The wizard collects initial agent parameters (type, name, description), then uses the application's configured LLM (via `ILlmProviderService`) to generate a high-quality agent definition. The LLM acts as an "agent architect": it can ask clarifying questions to produce a more targeted and specific agent, or proceed directly to generation if the initial description is sufficiently clear. The user can answer questions, skip the refinement, or manually edit the final output before saving.

The generated file follows the OpenCode agent Markdown format: YAML frontmatter (description, mode, model, temperature, tools, permissions, etc.) followed by a detailed system prompt body.

## Actors

- **Authenticated User** — Any logged-in user with an active project selected, creating a new agent.
- **LLM Provider** — The externally configured LLM (via OpenRouter API key + model stored in AppSettings) that generates and refines the agent definition.

## Functional Requirements

### Page & Routing

1. The system SHALL expose a Blazor page at route `/projects/{projectId:guid}/agents/new`.
2. The page SHALL validate that the `projectId` corresponds to an existing, non-archived project. If not, it SHALL display a "Project not found" state consistent with other project pages.
3. The page SHALL set the active project in `ActiveProjectService` when navigated to directly.
4. The page title SHALL be "New Agent — {ProjectName} — OwlNet".
5. The page SHALL display a "Back" button (or breadcrumb link) that navigates to `/projects/{projectId}/agents` (the agent list page).

### Step 1 — Initial Form

6. The page SHALL display an initial form with the following fields:
    - **Agent Type** — a `MudSelect` or `MudRadioGroup` with options: "Primary", "Subagent" (default selected). Maps to the `mode` frontmatter field (`"primary"`, `"subagent"`).
    - **Agent Name** — a `MudTextField` for the agent identifier (becomes the filename). Validation: required, alphanumeric characters and hyphens only, no spaces, 2-50 characters, must not already exist in the project's `.opencode/agents/` folder.
    - **Description** — a `MudTextField` multiline (3-5 rows) for the user's initial description of what the agent should do. Validation: required, 10-500 characters.
7. The form SHALL display a "Generate" button (`MudButton`, Variant.Filled, Color.Primary) that is enabled only when all validation passes.
8. Clicking "Generate" SHALL transition the page to Step 2 (LLM Refinement Conversation).
9. The "All" mode option SHALL NOT be shown in the type selector — it is an advanced option that can be set manually in the editor (SPEC-P3) if needed.

### Step 2 — LLM Refinement Conversation

10. After clicking "Generate", the system SHALL send the initial form data (type, name, description) to the LLM via a dedicated Application layer command.
11. The LLM SHALL be instructed via a system prompt to act as an "agent architect" that:
    a. Analyzes the user's description and agent type.
    b. Decides whether clarifying questions are needed to produce a better, more targeted agent.
    c. If questions are needed: responds with a structured list of questions.
    d. If no questions are needed (description is already clear and specific): proceeds directly to generating the final agent Markdown.
12. The UI SHALL display a chat-like conversation panel with:
    - **Assistant messages** — displayed as left-aligned bubbles (or cards) showing the LLM's questions or status messages.
    - **User messages** — displayed as right-aligned bubbles showing the user's answers.
    - A **text input field** (`MudTextField`) at the bottom with a "Send" button for the user to type answers.
    - A **"Skip & Generate"** button that allows the user to bypass further questions and force the LLM to generate the agent with the information gathered so far.
13. Each user answer SHALL be sent back to the LLM along with the full conversation history (all previous questions and answers) so the LLM has full context.
14. The LLM MAY ask multiple rounds of questions. There is no hard limit on the number of rounds. The LLM decides when it has enough information to generate the agent.
15. When the LLM decides it has sufficient information (either autonomously or because the user clicked "Skip & Generate"), it SHALL generate the final agent Markdown content.
16. During LLM processing (waiting for response), the UI SHALL show a loading indicator (e.g., typing dots animation, disabled input field, spinner on the send button).
17. If the LLM call fails (network error, API error, timeout), the system SHALL display an error message in the conversation panel (e.g., "Failed to get a response. Please try again.") with a "Retry" button that resends the last request.

### Step 3 — Preview & Edit

18. Once the LLM generates the final Markdown, the page SHALL transition to a preview/edit view.
19. The preview SHALL display the generated Markdown content in an editable `MudTextField` (multiline, monospace font, full-width, sufficient height to show the entire content without excessive scrolling).
20. The user SHALL be able to freely edit the Markdown content — both the YAML frontmatter and the body text.
21. The page SHALL display two action buttons:
    - **"Save Agent"** (`MudButton`, Variant.Filled, Color.Primary) — saves the file and navigates to the agent list.
    - **"Back to Form"** (`MudButton`, Variant.Outlined) — returns to Step 1, discarding the generated content. A confirmation dialog SHALL be shown: "Discard generated content and start over?"
22. Clicking "Save Agent" SHALL:
    a. Save the Markdown content to `{Project.Path}/.opencode/agents/{agentName}.md` via the `SaveAgentCommand`.
    b. Create the `.opencode/agents/` directory structure if it does not exist.
    c. On success: navigate to `/projects/{projectId}/agents` and show a success snackbar: "Agent '{agentName}' created successfully."
    d. On failure: show an error snackbar with the error message; remain on the preview page.

### Application Layer — Commands & DTOs

23. The system SHALL define a `GenerateAgentPromptCommand` record with properties:
    - `AgentType` (string: `"primary"` or `"subagent"`)
    - `AgentName` (string)
    - `AgentDescription` (string)
    - `ConversationHistory` (IReadOnlyList<ConversationMessage>) — the full Q&A history so far (may be empty on first call)
    - `ForceGenerate` (bool) — true when the user clicks "Skip & Generate"
24. `ConversationMessage` SHALL be a record with: `Role` (string: `"assistant"` or `"user"`), `Content` (string).
25. The `GenerateAgentPromptCommand` handler SHALL:
    a. Retrieve the LLM configuration via `ILlmProviderService.GetConfigurationAsync()`.
    b. If the LLM is not configured, return `Result.Failure("LLM provider is not configured. Please configure it in Settings.")`.
    c. Build the LLM request with the system prompt (agent architect instructions), the initial agent data, and the conversation history.
    d. Call the LLM API and parse the response.
    e. Return `Result<AgentGenerationResponseDto>`.
26. `AgentGenerationResponseDto` SHALL be a record with:
    - `ResponseType` (enum `AgentGenerationResponseType`: `Questions` or `GeneratedMarkdown`)
    - `Questions` (IReadOnlyList<string>?, nullable) — populated when `ResponseType` is `Questions`
    - `GeneratedMarkdown` (string?, nullable) — populated when `ResponseType` is `GeneratedMarkdown`
    - `AssistantMessage` (string) — the full assistant message text for display in the conversation UI
27. The system SHALL define a `SaveAgentCommand` record with properties:
    - `ProjectId` (Guid)
    - `AgentName` (string)
    - `Content` (string — the full Markdown content)
28. The `SaveAgentCommand` handler SHALL:
    a. Retrieve the project to obtain `Project.Path`.
    b. Validate the project exists and is not archived.
    c. Validate the agent name (alphanumeric + hyphens, 2-50 chars).
    d. Validate that no file with the same name already exists (to prevent accidental overwrites during creation).
    e. Call `IAgentFileService.WriteAgentAsync(projectPath, agentName, content, cancellationToken)`.
    f. Return `Result<string>` with the saved file path on success.
29. A `SaveAgentCommandValidator` SHALL validate: `AgentName` is not empty, matches the allowed pattern, and is 2-50 characters; `Content` is not empty; `ProjectId` is not empty.

### LLM System Prompt

30. The system prompt for the agent architect LLM SHALL instruct it to:
    a. Understand the OpenCode agent format (YAML frontmatter fields: `description`, `mode`, `model`, `temperature`, `tools`, `permissions`, `steps`, `hidden`, `color`, `top_p`; plus the Markdown body as the system prompt).
    b. Analyze the user's initial description and agent type.
    c. Ask targeted, concise questions (max 3-5 per round) to understand: the agent's specific role and responsibilities, what tools it needs access to (read/write/edit/bash), what it should NOT do (constraints), what tone/style it should use, whether it needs a specific model or temperature.
    d. When ready to generate, produce a complete `.md` file with proper YAML frontmatter and a detailed, well-structured system prompt body.
    e. Respond in a structured JSON format so the handler can parse it reliably:
       - For questions: `{"type": "questions", "message": "...", "questions": ["...", "..."]}`
       - For generation: `{"type": "markdown", "message": "...", "content": "---\ndescription: ...\n---\n..."}`
    f. When `ForceGenerate` is true, skip any further questions and generate the best possible agent with the information available.
31. The system prompt SHALL be defined as a constant or embedded resource in the Application layer, not hardcoded in the handler method body.

### LLM Infrastructure

32. The `ILlmProviderService` interface SHALL be extended (or a new `ILlmChatService` interface created) with a method to send a chat completion request with system prompt and message history, returning the assistant's response text. This is needed because the current `ILlmProviderService` only handles configuration and connection verification, not actual chat completions.
33. The LLM chat implementation SHALL use the configured OpenRouter API key and model from AppSettings.
34. The LLM chat request SHALL include a `temperature` parameter (suggested: 0.4 for balanced creativity/precision).
35. The LLM chat response SHALL be parsed to extract the structured JSON. If the response is not valid JSON, the system SHALL attempt to extract JSON from the response text (e.g., from a code block), and if that fails, treat the entire response as a generation result.

## User Flow

### Happy Path — With Refinement Questions

1. User is on the agent list page and clicks "Add Agent".
2. System navigates to `/projects/{projectId}/agents/new`.
3. User fills in: Type = "Subagent", Name = "code-reviewer", Description = "Reviews code for quality and best practices".
4. User clicks "Generate".
5. System sends the data to the LLM. Loading indicator appears.
6. LLM responds with 3 questions: "What programming languages should this agent focus on?", "Should it suggest fixes or only identify issues?", "Are there specific coding standards it should enforce?"
7. Questions appear as an assistant message in the conversation panel.
8. User types: "Focus on C# and TypeScript. Only identify issues, don't suggest fixes. Follow our AGENTS.md coding standards." and clicks "Send".
9. System sends the answer + history to the LLM. Loading indicator appears.
10. LLM decides it has enough info and generates the agent Markdown.
11. The conversation shows a final assistant message: "I've generated your agent. Review and edit below."
12. The preview/edit area appears with the generated Markdown.
13. User reviews the content, makes a small edit to the system prompt.
14. User clicks "Save Agent".
15. System saves to `{Project.Path}/.opencode/agents/code-reviewer.md`.
16. User is navigated to the agent list page. Success snackbar: "Agent 'code-reviewer' created successfully."

### Happy Path — No Questions Needed

1. User fills in: Type = "Primary", Name = "build", Description = "Full-featured development agent with all tools enabled for writing, editing, and running code. Should have access to all filesystem and bash operations."
2. User clicks "Generate".
3. LLM determines the description is clear enough and generates the Markdown directly (no questions).
4. Preview/edit area appears immediately.
5. User clicks "Save Agent".

### Happy Path — User Skips Questions

1. User fills in form and clicks "Generate".
2. LLM asks questions.
3. User doesn't want to answer and clicks "Skip & Generate".
4. System sends a force-generate request to the LLM.
5. LLM generates the best possible agent with the initial description only.
6. Preview/edit area appears.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| LLM provider not configured (no API key or model) | "Generate" button click shows error: "LLM provider is not configured. Please configure it in Settings." with a link to `/settings`. |
| LLM API call fails (network error, timeout) | Error message in conversation: "Failed to get a response. Please try again." with a "Retry" button. |
| LLM returns invalid/unparseable response | System treats the response as raw text and displays it as an assistant message. User can click "Skip & Generate" to retry. |
| Agent name already exists in the project | Validation error on the name field: "An agent with this name already exists." Checked both client-side (on form submit) and server-side (in SaveAgentCommand). |
| Agent name contains invalid characters | Validation error: "Agent name can only contain letters, numbers, and hyphens." |
| User navigates away during conversation | No auto-save. Content is lost. Standard browser behavior. |
| User clicks "Back to Form" after generation | Confirmation dialog: "Discard generated content and start over?" On confirm: return to Step 1 with form cleared. |
| Empty Markdown content on save attempt | Validation error: "Agent content cannot be empty." |
| Filesystem write error (permissions, disk full) | Error snackbar: "Failed to save agent. Check filesystem permissions." |
| Project path no longer exists on disk | `WriteAgentAsync` creates the `.opencode/agents/` directory; if the parent path doesn't exist, error: "Project workspace directory not found." |
| Very long LLM response | The textarea is scrollable; no truncation. |
| User refreshes the page during Step 2 or 3 | Conversation state is lost. User returns to Step 1 (empty form). This is acceptable for V1. |

## Out of Scope

- Persisting conversation state across page refreshes or navigation (in-memory only for V1).
- Model selector in the wizard (uses the globally configured model).
- Template library or pre-built agent templates.
- Importing agents from external sources.
- Batch creation of multiple agents.
- Real-time streaming of LLM responses (full response returned at once for V1).
- Agent testing or validation (verifying the generated agent works correctly).
- Editing existing agents (see SPEC-P3).

## Acceptance Criteria

- [ ] Page exists at `/projects/{projectId:guid}/agents/new` with proper project validation.
- [ ] Page title is "New Agent — {ProjectName} — OwlNet".
- [ ] "Back" navigation returns to the agent list page.
- [ ] Step 1 form displays: Agent Type (Primary/Subagent), Agent Name, Description.
- [ ] Agent Name validates: required, alphanumeric + hyphens, 2-50 chars, no duplicates.
- [ ] Description validates: required, 10-500 chars.
- [ ] "Generate" button is disabled until all validations pass.
- [ ] Clicking "Generate" sends data to LLM and shows loading indicator.
- [ ] LLM questions are displayed as assistant messages in a chat-like UI.
- [ ] User can type answers and send them via text input + "Send" button.
- [ ] "Skip & Generate" button forces the LLM to produce output immediately.
- [ ] Conversation history is maintained and sent with each LLM request.
- [ ] Generated Markdown is displayed in an editable textarea.
- [ ] User can freely edit the generated content (frontmatter + body).
- [ ] "Save Agent" writes the file to `{Project.Path}/.opencode/agents/{name}.md`.
- [ ] Directory `.opencode/agents/` is created if it doesn't exist.
- [ ] Duplicate agent name is rejected with a clear error message.
- [ ] On save success: navigate to agent list + success snackbar.
- [ ] On save failure: error snackbar, remain on preview page.
- [ ] LLM not configured: clear error with link to settings page.
- [ ] LLM API failure: error in conversation with "Retry" button.
- [ ] "Back to Form" shows confirmation dialog before discarding generated content.
- [ ] `GenerateAgentPromptCommand` + handler exist in Application layer.
- [ ] `SaveAgentCommand` + handler + validator exist in Application layer.
- [ ] `AgentGenerationResponseDto` and `ConversationMessage` records defined.
- [ ] LLM system prompt is externalized as a constant/resource, not inline.
- [ ] `ILlmProviderService` extended (or new `ILlmChatService`) with chat completion method.
- [ ] Unit tests for `SaveAgentCommand` handler: happy path, duplicate name, invalid name, project not found.
- [ ] Unit tests for `GenerateAgentPromptCommand` handler: LLM not configured, successful generation, question response parsing.

## Dependencies

- **SPEC-P1-agent-list-page** — `IAgentFileService` (WriteAgentAsync, GetAgentAsync for duplicate check), `AgentFileDto`, agent list page for navigation.
- **SPEC-llm-provider-config** — `ILlmProviderService` for LLM configuration retrieval.
- **SPEC-001-project-crud** — Project entity, repository, `ActiveProjectService`.
- OpenRouter API (external) — for LLM chat completions.

## Open Questions

1. Should the LLM system prompt be stored as an embedded resource file (`.txt`) or as a C# string constant? Recommendation: embedded resource for easier editing and readability.
2. Should we support streaming LLM responses in V1 (tokens appearing progressively) or is full-response-at-once acceptable? Current decision: full response for V1, streaming can be added later.
3. Should the conversation panel auto-scroll to the latest message? Recommendation: yes, auto-scroll on new messages.
4. Should the "Generate" step validate agent name uniqueness immediately (async check against filesystem) or only at save time? Recommendation: validate at both points — async check on "Generate" click for early feedback, and again on "Save" for safety.
