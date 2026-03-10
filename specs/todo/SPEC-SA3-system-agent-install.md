# SPEC: System Agent Install — Add System Agent to Project

> **Status:** Todo
> **Created:** 2026-03-10
> **Author:** owl-planner + user
> **Priority:** High (SA3 — depends on SA1 and SA2)
> **Estimated Complexity:** S

## Context

After SPEC-SA1 establishes the system agent catalogue and SPEC-SA2 provides the UI to manage it, users need a way to install a system agent into a specific project. This spec adds an "Add System Agent" button to the existing `ProjectAgentsPage` (`/projects/{projectId}/agents`). Clicking it opens a modal that lists all available system agents; the user selects one, optionally renames it, and confirms. The system copies the agent's `Content` as a `.md` file into the project's `.opencode/agents/` directory.

If a file with the same name already exists in the project, the system warns the user and asks for confirmation before overwriting.

## Actors

- **Authenticated User** — Any logged-in user with an active project selected.

## Functional Requirements

### ProjectAgentsPage — New Button

1. The `ProjectAgentsPage` header area SHALL display a second button alongside the existing "Add Agent" button: **"Add System Agent"** (Variant.Outlined, Color.Secondary, StartIcon = `Icons.Material.Filled.LibraryAdd`).
2. The "Add System Agent" button SHALL be visible at all times (not only in empty state), in the same header row as "Add Agent".
3. Clicking "Add System Agent" SHALL open the Install System Agent modal dialog.
4. If the system agents catalogue is empty (no system agents defined), the button SHALL still be shown but clicking it SHALL open the modal displaying an empty state message: "No system agents available. Add some in Settings first." with a link to `/settings`.

### Install System Agent Modal

5. The modal SHALL have the title "Add System Agent".
6. The modal SHALL display a list of all available system agents, showing for each:
   - **Name** — `SystemAgent.Name` (monospace, bold).
   - **Display Name** — `SystemAgent.DisplayName`.
   - **Type badge** — `MudChip` for `Mode`, same color scheme as SPEC-P1 FR-15.
   - **Description** — truncated to a single line with ellipsis; full text on hover via `MudTooltip`.
7. The list SHALL be sorted alphabetically by `Name` (ascending).
8. The user SHALL select exactly one system agent from the list (single selection, e.g., clicking a row highlights it).
9. After selecting a system agent, the modal SHALL display a **"File Name"** text field pre-populated with `SystemAgent.Name` (without `.md` extension). The user MAY edit this value to rename the agent file before installation. Validation: required, 2–50 chars, pattern `^[a-zA-Z0-9-]+$`.
10. The modal SHALL display two action buttons in the footer:
    - **"Install"** (Variant.Filled, Color.Primary) — enabled only when a system agent is selected and the file name is valid.
    - **"Cancel"** (Variant.Outlined) — closes the modal without any action.
11. The modal SHALL show a loading skeleton while the system agents list is being fetched.
12. If loading the system agents list fails, the modal SHALL show an error message with a "Retry" button.

### Conflict Detection

13. Before writing the file, the system SHALL check whether a file named `{fileName}.md` already exists in `{Project.Path}/.opencode/agents/`.
14. If the file already exists, the system SHALL display a confirmation dialog (either inline in the modal or as a nested dialog): "An agent named '{fileName}' already exists in this project. Do you want to overwrite it?"
15. If the user confirms overwrite: proceed with installation (overwrite the file).
16. If the user cancels: return to the modal with the file name field still editable, so the user can choose a different name.

### Installation

17. Clicking "Install" (after conflict resolution if needed) SHALL invoke the `InstallSystemAgentCommand`.
18. The `InstallSystemAgentCommand` handler SHALL:
    a. Retrieve the `SystemAgent` by `Id` from the repository.
    b. Retrieve the project by `ProjectId` to obtain `Project.Path`.
    c. Validate the project exists and is not archived.
    d. Validate the `FileName` (required, 2–50 chars, pattern `^[a-zA-Z0-9-]+$`).
    e. Check for file conflict: if `{Project.Path}/.opencode/agents/{fileName}.md` exists and `AllowOverwrite` is `false`, return `Result.Failure("conflict")` with a specific error code so the UI can show the confirmation dialog.
    f. Write the `SystemAgent.Content` to `{Project.Path}/.opencode/agents/{fileName}.md` via `IAgentFileService.WriteAgentAsync(projectPath, fileName, content, cancellationToken)`.
    g. Return `Result` indicating success or failure.
19. The `InstallSystemAgentCommand` record SHALL have properties:
    - `SystemAgentId` (Guid)
    - `ProjectId` (Guid)
    - `FileName` (string) — the target filename without `.md` extension (may differ from `SystemAgent.Name`).
    - `AllowOverwrite` (bool) — `false` on first attempt; `true` after user confirms overwrite.
20. On installation success: close the modal, show a success snackbar on the `ProjectAgentsPage`: "System agent '{fileName}' installed successfully.", and refresh the agent list.
21. On installation failure (non-conflict): show an error snackbar inside the modal; keep the modal open.

### Loading & Feedback

22. During the installation operation, the "Install" button SHALL show a loading spinner and be disabled to prevent double-submission.

## User Flow

### Happy Path — Install Without Conflict

1. User is on the `ProjectAgentsPage` for "Project Alpha".
2. User clicks "Add System Agent".
3. Modal opens. Loading skeleton briefly shown.
4. Modal displays 3 system agents: "code-reviewer", "git-agent", "test-writer".
5. User clicks on "git-agent" row — it becomes highlighted/selected.
6. "File Name" field shows "git-agent" (pre-populated).
7. User leaves the name as-is and clicks "Install".
8. System checks: no file named `git-agent.md` exists in the project. No conflict.
9. System writes `{Project.Path}/.opencode/agents/git-agent.md` with the content of the "git-agent" system agent.
10. Modal closes. Snackbar: "System agent 'git-agent' installed successfully."
11. Agent list refreshes and shows "git-agent" in the list.

### Happy Path — Install With Rename

1. User selects "code-reviewer" from the modal.
2. User changes the "File Name" field to "my-reviewer".
3. User clicks "Install".
4. System writes `{Project.Path}/.opencode/agents/my-reviewer.md`.
5. Modal closes. Snackbar: "System agent 'my-reviewer' installed successfully."

### Happy Path — Install With Conflict, Overwrite Confirmed

1. User selects "git-agent" from the modal.
2. "File Name" field shows "git-agent".
3. User clicks "Install".
4. System detects `git-agent.md` already exists in the project.
5. Confirmation dialog: "An agent named 'git-agent' already exists in this project. Do you want to overwrite it?"
6. User clicks "Overwrite".
7. System writes the file, overwriting the existing one.
8. Modal closes. Snackbar: "System agent 'git-agent' installed successfully."

### Happy Path — Install With Conflict, User Renames

1. Same as above, but at step 6 the user clicks "Cancel" on the overwrite dialog.
2. User is returned to the modal with the "File Name" field editable.
3. User changes the name to "git-agent-v2".
4. User clicks "Install". No conflict. File written.

### Edge Case — Empty Catalogue

1. User clicks "Add System Agent".
2. Modal opens and shows: "No system agents available. Add some in Settings first." with a link to `/settings`.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| System agents catalogue is empty | Modal shows empty state with link to `/settings`. "Install" button not shown. |
| Loading system agents list fails | Modal shows error message with "Retry" button. |
| File already exists and `AllowOverwrite` is false | Handler returns a conflict result; UI shows overwrite confirmation dialog. |
| File already exists and user confirms overwrite | Handler called again with `AllowOverwrite = true`; file is overwritten. |
| `FileName` contains invalid characters | Validation error on the field: "File name can only contain letters, numbers, and hyphens." "Install" button disabled. |
| `FileName` is empty | Validation error: "File name is required." "Install" button disabled. |
| Project not found or archived | Handler returns failure; error snackbar in modal. |
| `SystemAgentId` not found in DB | Handler returns failure; error snackbar in modal. |
| Filesystem write error (permissions, disk full) | Error snackbar in modal: "Failed to install agent. Check filesystem permissions." Modal stays open. |
| Project path does not exist on disk | `WriteAgentAsync` creates the `.opencode/agents/` directory; if the parent path doesn't exist, error snackbar. |
| User clicks "Cancel" | Modal closes. No file written. Agent list unchanged. |

## Out of Scope

- Bulk installation of multiple system agents at once (V1: one at a time).
- Updating an already-installed agent when the system agent catalogue entry changes (no sync mechanism in V1).
- Tracking which system agents are installed in which projects.
- Uninstalling a system agent from a project (use the existing "Delete" action on the agent editor page, SPEC-P3).
- Previewing the system agent content before installation.
- Filtering or searching the system agents list in the modal.

## Acceptance Criteria

- [ ] "Add System Agent" button (Variant.Outlined, Color.Secondary) appears in the `ProjectAgentsPage` header alongside "Add Agent".
- [ ] Button is always visible (not only in empty state).
- [ ] Clicking the button opens the Install System Agent modal.
- [ ] Modal title is "Add System Agent".
- [ ] Modal shows loading skeleton while fetching system agents.
- [ ] Modal shows empty state with `/settings` link when catalogue is empty.
- [ ] Modal shows error + retry when loading fails.
- [ ] System agents list shows: Name, Display Name, Type badge (correct colors), Description (truncated with tooltip).
- [ ] List is sorted alphabetically by `Name`.
- [ ] Single-selection: clicking a row highlights it.
- [ ] "File Name" field pre-populated with selected agent's `Name`; user can edit it.
- [ ] "File Name" validates: required, 2–50 chars, alphanumeric + hyphens.
- [ ] "Install" button enabled only when an agent is selected and file name is valid.
- [ ] "Cancel" button closes the modal without action.
- [ ] Conflict detection: if file exists and `AllowOverwrite = false`, overwrite confirmation dialog is shown.
- [ ] Overwrite confirmed: file is overwritten; modal closes; success snackbar shown.
- [ ] Overwrite cancelled: user returns to modal with editable file name field.
- [ ] Successful installation: modal closes, success snackbar, agent list refreshes.
- [ ] Installation failure (non-conflict): error snackbar in modal; modal stays open.
- [ ] "Install" button shows loading spinner during operation.
- [ ] `InstallSystemAgentCommand` + handler defined in Application layer with all properties.
- [ ] Handler correctly resolves conflict via `AllowOverwrite` flag.
- [ ] Handler writes file via `IAgentFileService.WriteAgentAsync`.
- [ ] Unit tests for `InstallSystemAgentCommand` handler: happy path, conflict without overwrite, conflict with overwrite, system agent not found, project not found, invalid file name.

## Dependencies

- **SPEC-SA1-system-agent-domain** — `SystemAgent` entity, `GetAllSystemAgentsQuery`, `ISystemAgentRepository`.
- **SPEC-P1-agent-list-page** — `IAgentFileService.WriteAgentAsync`, `ProjectAgentsPage` (modified by this spec), `AgentFileDto`.
- **SPEC-001-project-crud** — Project entity, repository, `ActiveProjectService`.

## Open Questions

- None.
