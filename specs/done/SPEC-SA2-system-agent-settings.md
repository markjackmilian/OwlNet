# SPEC: System Agent Settings — Catalogue Management UI

> **Status:** Done
> **Created:** 2026-03-10
> **Author:** owl-planner + user
> **Priority:** High (SA2 — depends on SA1)
> **Estimated Complexity:** L

## Context

After SPEC-SA1 establishes the `SystemAgent` entity and Application-layer CRUD, users need a UI to manage the system-wide agent catalogue. This spec adds a dedicated "System Agents" section to the global `/settings` page, where users can view, create, edit, and delete system agents.

The creation flow mirrors the project-level agent creation wizard (SPEC-P2): a multi-step LLM-assisted wizard (metadata form → LLM refinement conversation → preview/edit → save). The editor for existing agents mirrors the project-level agent editor (SPEC-P3): a dedicated page with a raw Markdown editor, dirty-state detection, save, and delete.

System agents are installation-wide: they are not tied to any project and are accessible to all users.

## Actors

- **Authenticated User** — Any logged-in user. All users can manage system agents (no role distinction at this time).
- **LLM Provider** — The externally configured LLM (via OpenRouter API key + model in AppSettings) used during the creation wizard.

## Functional Requirements

### Settings Page — System Agents Section

1. The `/settings` page SHALL include a new "System Agents" section (a `MudCard` or equivalent panel), positioned after the existing configuration panels.
2. The section SHALL display a header: "System Agents" with a subtitle: "Manage the installation-wide catalogue of reusable agent definitions."
3. The section SHALL display a list of all existing system agents, showing for each:
   - **Name** — the `SystemAgent.Name` identifier (monospace, bold).
   - **Display Name** — the `SystemAgent.DisplayName` human-readable label.
   - **Type badge** — a `MudChip` derived from `Mode`, using the same color scheme as SPEC-P1 FR-15 (Primary → `Color.Primary`, Subagent → `Color.Secondary`, All → `Color.Tertiary`, Unknown → `Color.Default`).
   - **Description** — truncated to a single line with ellipsis; full text on hover via `MudTooltip`.
   - **Edit action** — an icon button that navigates to `/settings/system-agents/{agentName}`.
4. The list SHALL be sorted alphabetically by `Name` (ascending).
5. The section SHALL display an "Add System Agent" button (`MudButton`, Variant.Filled, Color.Primary, StartIcon = `Icons.Material.Filled.Add`) in the section header area, always visible.
6. Clicking "Add System Agent" SHALL navigate to `/settings/system-agents/new`.
7. When the system agents list is empty, the section SHALL display an inline empty state: icon (`SmartToy`), text "No system agents defined yet.", and the "Add System Agent" button.
8. The section SHALL show a loading skeleton while the list is being fetched from the database.
9. If loading fails, the section SHALL show an inline error message with a "Retry" button.

### Creation Wizard — Route & Navigation

10. The system SHALL expose a Blazor page at route `/settings/system-agents/new`.
11. The page title SHALL be "New System Agent — Settings — OwlNet".
12. The page SHALL display a "Back" button that navigates to `/settings`.

### Creation Wizard — Step 1: Metadata Form

13. The wizard SHALL display an initial form with the following fields:
    - **Agent Type** (`MudSelect` or `MudRadioGroup`): options "Primary", "Subagent", "All". Default: "Subagent". Maps to `Mode`.
    - **Name** (`MudTextField`): the agent identifier (becomes the default filename when installed). Validation: required, 2–50 chars, pattern `^[a-zA-Z0-9-]+$`, must not already exist in the system agents catalogue.
    - **Display Name** (`MudTextField`): human-readable label. Validation: required, 2–100 chars.
    - **Description** (`MudTextField`, multiline 3–5 rows): what the agent does. Validation: required, 10–500 chars.
14. The form SHALL display a "Generate with AI" button (Variant.Filled, Color.Primary) enabled only when all validations pass.
15. The form SHALL also display a "Create Manually" button (Variant.Outlined) that bypasses the LLM and navigates directly to Step 3 (preview/edit) with a minimal pre-filled Markdown template.
16. The minimal pre-filled template for "Create Manually" SHALL be:
    ```
    ---
    description: {Description}
    mode: {mode}
    ---

    ```
    (frontmatter pre-populated from the form, body empty for the user to fill in)

### Creation Wizard — Step 2: LLM Refinement Conversation

17. Clicking "Generate with AI" SHALL transition to a chat-like conversation panel, following the same interaction model as SPEC-P2 FR-10 to FR-17:
    - The system sends the metadata (type, name, description) to the LLM via a `GenerateSystemAgentPromptCommand`.
    - The LLM acts as an "agent architect" (same system prompt as SPEC-P2 FR-30–31, reused or referenced).
    - The UI shows assistant messages (left-aligned) and user messages (right-aligned).
    - A text input + "Send" button allows the user to answer questions.
    - A "Skip & Generate" button forces immediate generation.
    - Loading indicator during LLM processing.
    - Error message + "Retry" button on LLM failure.
18. The `GenerateSystemAgentPromptCommand` SHALL reuse the same LLM infrastructure as `GenerateAgentPromptCommand` (SPEC-P2). If the LLM chat service is already implemented, this command simply calls it with the same system prompt. No new LLM infrastructure is required.

### Creation Wizard — Step 3: Preview & Edit

19. Once the LLM generates the Markdown (or the user clicks "Create Manually"), the page SHALL display the full content in an editable `MudTextField` (multiline, monospace font, full-width).
20. The user SHALL be able to freely edit both the YAML frontmatter and the body.
21. Two action buttons SHALL be displayed:
    - **"Save System Agent"** (Variant.Filled, Color.Primary) — saves to the database and navigates to `/settings`.
    - **"Back to Form"** (Variant.Outlined) — returns to Step 1 with a confirmation dialog: "Discard generated content and start over?"
22. Clicking "Save System Agent" SHALL invoke `CreateSystemAgentCommand` with the metadata from Step 1 and the content from the editor.
23. On success: navigate to `/settings` and show a success snackbar: "System agent '{name}' created successfully."
24. On failure (e.g., duplicate name, validation error): show an error snackbar; remain on Step 3.

### Editor Page — Route & Navigation

25. The system SHALL expose a Blazor page at route `/settings/system-agents/{agentName}`.
26. The `agentName` route parameter corresponds to `SystemAgent.Name`.
27. The page title SHALL be "{DisplayName} — System Agents — Settings — OwlNet".
28. The page SHALL display a "Back" button that navigates to `/settings`.
29. If no `SystemAgent` with the given `Name` exists, the page SHALL display a "System agent not found" state with a link back to `/settings`.

### Editor Page — Summary Header

30. The page SHALL display a summary header with:
    - **Name** — `SystemAgent.Name` (monospace, bold).
    - **Display Name** — `SystemAgent.DisplayName` (prominent heading).
    - **Type badge** — `MudChip` for `Mode`, same color scheme as FR-3.
    - **Description** — secondary text.
31. The summary header SHALL update after a successful save (not in real-time while typing).

### Editor Page — Editor

32. The page SHALL display the full `SystemAgent.Content` in an editable `MudTextField` (multiline, monospace font, full-width, minimum 20 lines).
33. The system SHALL track dirty state: the "Save" button is enabled only when the content differs from the last saved/loaded value.
34. The user SHALL be able to edit `DisplayName`, `Description`, and `Mode` via inline fields above the content editor (not only the raw Markdown). These fields are separate from the Markdown `Content` and are stored as dedicated database columns.

    > Rationale: unlike project agents (where all metadata lives in the frontmatter), system agents store metadata explicitly in the DB. The `Content` field is the Markdown body that gets written to the `.md` file on install. The frontmatter in `Content` is what OpenCode reads — it should be consistent with the DB fields, but the system does not auto-sync them. The user is responsible for keeping them consistent.

35. The page SHALL display a "Save" button (Variant.Filled, Color.Primary, StartIcon = `Save`) enabled only when dirty.
36. Clicking "Save" SHALL invoke `UpdateSystemAgentCommand` with the updated `DisplayName`, `Description`, `Mode`, and `Content`.
37. On save success: success snackbar "System agent '{name}' saved successfully.", summary header updated, dirty state reset.
38. On save failure: error snackbar, editor content preserved.

### Editor Page — Delete

39. The page SHALL display a "Delete" button (Variant.Outlined, Color.Error, StartIcon = `Delete`).
40. Clicking "Delete" SHALL show a confirmation dialog: "Are you sure you want to delete the system agent '{name}'? This action cannot be undone."
41. On confirmation: invoke `DeleteSystemAgentCommand`.
42. On delete success: navigate to `/settings` and show a success snackbar "System agent '{name}' deleted."
43. On delete failure: show an error snackbar; remain on the editor page.

### Editor Page — Unsaved Changes Guard

44. If the user has unsaved changes and attempts to navigate away (via "Back" button or sidebar), the system SHALL show a confirmation dialog: "You have unsaved changes. Leave without saving?"
45. On confirm: navigate away, discarding changes.
46. On cancel: remain on the editor page.

### Loading States

47. The editor page SHALL show a loading skeleton while the system agent is being fetched from the database.

## User Flow

### Happy Path — View Catalogue

1. User navigates to `/settings`.
2. The "System Agents" section loads and displays the list of system agents.
3. User sees each agent with its name, display name, type badge, and description.

### Happy Path — Create System Agent (AI-assisted)

1. User clicks "Add System Agent" in the Settings page.
2. System navigates to `/settings/system-agents/new`.
3. User fills in: Type = "Subagent", Name = "git-agent", Display Name = "Git Agent", Description = "Performs Git operations: commits, branches, diffs, and conflict resolution."
4. User clicks "Generate with AI".
5. LLM asks 2 clarifying questions.
6. User answers. LLM generates the Markdown.
7. User reviews and edits the content.
8. User clicks "Save System Agent".
9. System saves to DB. Navigates to `/settings`. Snackbar: "System agent 'git-agent' created successfully."

### Happy Path — Create System Agent (Manual)

1. User fills in the Step 1 form.
2. User clicks "Create Manually".
3. Step 3 opens with a minimal pre-filled template.
4. User writes the full agent content manually.
5. User clicks "Save System Agent".

### Happy Path — Edit System Agent

1. User clicks the edit icon for "git-agent" in the Settings list.
2. System navigates to `/settings/system-agents/git-agent`.
3. Page loads with the current content.
4. User updates the Description field and modifies the Markdown body.
5. "Save" button becomes enabled.
6. User clicks "Save". Snackbar: "System agent 'git-agent' saved successfully."

### Happy Path — Delete System Agent

1. User is on the editor page for "old-agent".
2. User clicks "Delete". Confirmation dialog appears.
3. User confirms. System deletes from DB.
4. User is navigated to `/settings`. Snackbar: "System agent 'old-agent' deleted."

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| Creating an agent with a `Name` that already exists | Validation error on the Name field: "A system agent with this name already exists." Checked both client-side and server-side. |
| LLM provider not configured | "Generate with AI" click shows error: "LLM provider is not configured. Please configure it in Settings." |
| LLM API call fails | Error message in conversation panel with "Retry" button. |
| Navigating to `/settings/system-agents/{name}` for a non-existent agent | "System agent not found" state with link back to `/settings`. |
| Saving with empty `Content` | Validation error: "Agent content cannot be empty." |
| Saving with invalid `Mode` | Validation error: "Mode must be one of: primary, subagent, all." |
| User navigates away from editor with unsaved changes | Confirmation dialog: "You have unsaved changes. Leave without saving?" |
| Database error during save | Error snackbar with descriptive message; editor content preserved. |
| Database error during delete | Error snackbar; remain on editor page. |
| "Back to Form" after LLM generation | Confirmation dialog: "Discard generated content and start over?" On confirm: return to Step 1. |

## Out of Scope

- Per-user or per-project system agent overrides.
- Versioning or history of system agent changes.
- Import/export of system agents.
- Bulk operations (bulk delete, bulk edit).
- Searching or filtering the system agents list (acceptable for V1 given small catalogue size).
- Installing a system agent into a project (see SPEC-SA3).
- Real-time streaming of LLM responses (full response at once, consistent with SPEC-P2).

## Acceptance Criteria

- [ ] `/settings` page includes a "System Agents" section with list, loading state, empty state, and error state.
- [ ] "Add System Agent" button navigates to `/settings/system-agents/new`.
- [ ] Each list row shows: Name, Display Name, Type badge (correct colors), Description (truncated), Edit icon.
- [ ] List is sorted alphabetically by `Name`.
- [ ] `/settings/system-agents/new` page exists with correct title and "Back" button.
- [ ] Step 1 form: Agent Type, Name, Display Name, Description fields with validation.
- [ ] "Generate with AI" button disabled until all validations pass.
- [ ] "Create Manually" button skips to Step 3 with pre-filled template.
- [ ] Step 2 LLM conversation: assistant/user messages, "Send" button, "Skip & Generate" button, loading indicator, error + retry.
- [ ] Step 3: editable Markdown textarea, "Save System Agent" and "Back to Form" buttons.
- [ ] "Save System Agent" invokes `CreateSystemAgentCommand`; success navigates to `/settings` with snackbar.
- [ ] `/settings/system-agents/{agentName}` page exists with correct title and "Back" button.
- [ ] Editor page shows "System agent not found" for unknown `agentName`.
- [ ] Summary header shows Name, Display Name, Type badge, Description.
- [ ] Inline fields for `DisplayName`, `Description`, `Mode` above the content editor.
- [ ] Content editor: multiline, monospace, dirty state detection.
- [ ] "Save" button enabled only when dirty; invokes `UpdateSystemAgentCommand`.
- [ ] Save success: snackbar + header update + dirty state reset.
- [ ] "Delete" button shows confirmation dialog; on confirm invokes `DeleteSystemAgentCommand`.
- [ ] Delete success: navigate to `/settings` + snackbar.
- [ ] Unsaved changes guard on navigation away from editor.
- [ ] Loading skeleton on editor page during initial fetch.
- [ ] `GenerateSystemAgentPromptCommand` defined and reuses LLM infrastructure from SPEC-P2.
- [ ] Unit tests for `GenerateSystemAgentPromptCommand` handler: LLM not configured, successful generation, question response.

## Dependencies

- **SPEC-SA1-system-agent-domain** — `SystemAgent` entity, all CRUD commands/queries, `ISystemAgentRepository`.
- **SPEC-settings-page** — `/settings` page structure and panel pattern.
- **SPEC-P2-agent-creation-wizard** — LLM chat infrastructure (`ILlmChatService` or equivalent) and system prompt to reuse.

## Open Questions

- None.
