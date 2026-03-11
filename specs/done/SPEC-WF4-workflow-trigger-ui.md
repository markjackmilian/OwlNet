# SPEC: Workflow Trigger UI

> **Status:** Done
> **Created:** 2026-03-10
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

OwlNet needs a dedicated "Workflow" page within each project's navigation to allow users to manage workflow triggers visually. This page is the primary interface for creating, editing, deleting, enabling/disabling, and reviewing triggers that automate agent invocations on card status transitions.

The page also includes an "Improve Prompt" feature that uses an LLM to refine the user's prompt in a one-shot action.

This spec covers only the UI/UX layer. The underlying domain entities and business rules are defined in SPEC-WF3-workflow-trigger-entity.

## Actors

| Actor | Description |
|-------|-------------|
| **User** | Authenticated user managing workflow triggers for a project |

## Functional Requirements

### Navigation

1. The system SHALL add a **"Workflow"** menu item to the project-scoped sidebar navigation, positioned after "Board" and before "Agents".
2. The menu item SHALL use a Material Design icon appropriate for automation/workflow (e.g., `Icons.Material.Filled.AccountTree` or `Icons.Material.Filled.Bolt`).
3. The route SHALL be `/projects/{projectId}/workflow`.

### Trigger List View

4. The Workflow page SHALL display a list of all workflow triggers for the current project.
5. Each trigger in the list SHALL display:
   - Trigger name.
   - Status transition label: "{FromStatus} → {ToStatus}".
   - Number of agents configured.
   - Enabled/disabled state (visual toggle or badge).
   - Warning icon if any referenced agent or status no longer exists.
6. The list SHALL support filtering by enabled/disabled state.
7. The list SHALL display an empty state with a clear call-to-action when no triggers exist (e.g., "No workflow triggers configured. Create your first trigger to automate agent actions on card transitions.").
8. Each trigger row SHALL provide quick actions: Edit, Delete, Toggle Enable/Disable.
9. Deleting a trigger SHALL require confirmation via a dialog: "Are you sure you want to delete trigger '{name}'? This action cannot be undone."

### Trigger Create/Edit Form

10. The system SHALL provide a form (dialog or dedicated page) for creating and editing triggers with the following fields:
    - **Name**: text input, required.
    - **From Status**: dropdown populated with the project's board statuses, required.
    - **To Status**: dropdown populated with the project's board statuses (excluding the selected From Status), required.
    - **Prompt**: multi-line text area, required. Supports long-form text (up to 10000 characters).
    - **Agents**: multi-select from the project's available agents (loaded from `.opencode/agents/`), at least one required.
    - **Agent Order**: drag-and-drop list to define execution sequence of selected agents.
    - **Enabled**: toggle switch, default on.

11. The From Status and To Status dropdowns SHALL dynamically exclude each other's selected value to prevent selecting the same status for both.
12. The Agents selection SHALL display each agent's name and description (from the agent file's frontmatter). If no agents exist in the project, the selection area SHALL display: "No agents available. Create agents first from the Agents page."
13. The form SHALL validate all fields before allowing save, displaying inline validation errors.
14. On successful save, the user SHALL be returned to the trigger list with a success notification (snackbar).

### Improve Prompt (LLM-Assisted)

15. The trigger form SHALL include an **"Improve Prompt"** button adjacent to the Prompt text area.
16. The button SHALL be enabled only when the Prompt field contains at least 10 characters.
17. When clicked, the system SHALL:
    a. Send the current prompt text to the configured LLM service.
    b. Display a loading indicator on the button (disable it, show spinner).
    c. Receive the improved prompt from the LLM.
    d. Display the improved prompt in a **preview area** below the original prompt, clearly labeled as "Suggested improvement".
18. The user SHALL have two actions on the preview:
    - **Accept**: replaces the current prompt with the improved version.
    - **Discard**: dismisses the suggestion and keeps the original prompt.
19. The LLM prompt improvement request SHALL include context about the trigger: the From Status name, To Status name, and the selected agent names, so the LLM can produce a contextually relevant improvement.
20. If the LLM call fails (timeout, service unavailable, no LLM configured), the system SHALL display an error notification (snackbar) and leave the original prompt unchanged.

### Warning Indicators

21. If a trigger references an agent that no longer exists in the project's `.opencode/agents/` directory, the system SHALL display a warning icon next to the agent name in both the list view and the edit form, with a tooltip: "Agent '{name}' not found in project".
22. If a trigger references a BoardStatus that no longer exists, the system SHALL display a warning icon next to the status label with a tooltip: "Status no longer exists".

### Loading and Feedback

23. All async operations (loading triggers, saving, deleting, LLM calls) SHALL display appropriate loading feedback (skeleton loaders for initial load, button spinners for actions).
24. Success and error outcomes SHALL be communicated via MudBlazor Snackbar notifications.
25. The save button SHALL be disabled while a save operation is in progress to prevent double submission.

## User Flow

### Viewing Triggers
1. User clicks "Workflow" in the project sidebar.
2. System loads and displays the list of triggers for the project.
3. User sees each trigger with its name, transition, agent count, and status.

### Creating a Trigger
1. User clicks "Add Trigger" button.
2. System displays the trigger creation form.
3. User fills in Name, selects From Status and To Status.
4. User writes a prompt describing the desired agent behavior.
5. (Optional) User clicks "Improve Prompt". System calls LLM, shows suggestion. User accepts or discards.
6. User selects one or more agents from the available list.
7. User reorders agents via drag-and-drop if needed.
8. User clicks "Save".
9. System validates, persists, and returns to the list with a success snackbar.

### Editing a Trigger
1. User clicks "Edit" on a trigger row.
2. System displays the edit form pre-populated with the trigger's current data.
3. User modifies fields as needed.
4. User clicks "Save".
5. System validates, persists, and returns to the list with a success snackbar.

### Deleting a Trigger
1. User clicks "Delete" on a trigger row.
2. System shows confirmation dialog.
3. User confirms.
4. System deletes the trigger and refreshes the list with a success snackbar.

### Toggling Enable/Disable
1. User clicks the enable/disable toggle on a trigger row.
2. System immediately persists the change.
3. Visual state updates (e.g., disabled triggers appear dimmed).

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| Project has no board statuses configured | From/To Status dropdowns are empty. Form cannot be saved. Display hint: "Configure board statuses first." |
| Project has only one board status | To Status dropdown will be empty after From Status is selected. Form cannot be saved. Display hint: "At least two statuses are needed for a workflow trigger." |
| Project has no agents | Agent selection area shows empty state with link to Agents page. Form cannot be saved (min 1 agent required). |
| LLM service is not configured | "Improve Prompt" button is hidden or disabled with tooltip: "Configure an LLM provider in Settings to use this feature." |
| LLM call times out | Show error snackbar: "Prompt improvement failed — please try again." Original prompt is unchanged. |
| User navigates away with unsaved changes | Show browser/Blazor confirmation: "You have unsaved changes. Are you sure you want to leave?" |
| Trigger list is very long (50+ triggers) | List should handle scrolling gracefully. Pagination or virtual scrolling if needed (see Open Questions). |

## Out of Scope

- Trigger execution monitoring or logs (viewing which triggers fired and their results).
- Visual workflow builder (drag-and-drop flow diagram of status transitions).
- Bulk operations on triggers (enable/disable all, delete multiple).
- Trigger import/export.
- Prompt template library (reusable prompt snippets).

## Acceptance Criteria

- [ ] "Workflow" menu item appears in the project sidebar between "Board" and "Agents".
- [ ] Workflow page is accessible at `/projects/{projectId}/workflow`.
- [ ] Trigger list displays all project triggers with name, transition, agent count, and enabled state.
- [ ] Empty state is shown when no triggers exist.
- [ ] Triggers can be created via a form with Name, From Status, To Status, Prompt, and Agent selection.
- [ ] From/To Status dropdowns are populated from the project's board statuses and prevent same-status selection.
- [ ] Agents are selectable from the project's available agents with drag-and-drop ordering.
- [ ] "Improve Prompt" button sends the prompt to the LLM with trigger context and displays the suggestion.
- [ ] User can accept or discard the LLM-suggested prompt improvement.
- [ ] "Improve Prompt" is disabled when prompt is shorter than 10 characters or no LLM is configured.
- [ ] Triggers can be edited, deleted (with confirmation), and toggled enabled/disabled.
- [ ] Stale agent and status references display warning indicators.
- [ ] All operations show appropriate loading feedback and snackbar notifications.
- [ ] Form validation prevents saving invalid triggers (missing fields, same from/to status, no agents).
- [ ] Unsaved changes trigger a navigation guard.

## Dependencies

- **SPEC-WF3-workflow-trigger-entity** — domain entities, CRUD operations, and business rules.
- **SPEC-WF1-board-status-management** — project board statuses for dropdown population.
- **Agent filesystem service** (`IAgentFileService`) — to list available agents.
- **LLM service** (`ILlmChatService`) — for the "Improve Prompt" feature.
- **Project sidebar navigation** (`NavMenu.razor`) — to add the new menu item.

## Open Questions

1. **Form layout**: Should the create/edit form be a full page (`/projects/{projectId}/workflow/new` and `/projects/{projectId}/workflow/{triggerId}`) or a dialog/drawer overlay? Full page gives more space for the prompt editor; dialog keeps context.
2. **Pagination**: Is pagination needed for the trigger list, or is a simple scrollable list sufficient? How many triggers per project are expected in practice?
3. **Prompt editor richness**: Should the prompt text area be a plain textarea or a richer editor with Markdown preview? The prompt will be sent to agents as plain text, but Markdown formatting might help readability.
4. **Agent selection UX**: Should agent selection be a checkbox list, a transfer list (available → selected), or a searchable multi-select dropdown? The choice depends on how many agents a project typically has.
