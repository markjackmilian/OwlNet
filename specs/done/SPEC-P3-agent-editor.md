# SPEC: Agent Editor

> **Status:** Todo
> **Created:** 2026-03-10
> **Author:** owl-planner + user
> **Priority:** Medium (P3 — depends on P1, can be implemented in parallel with P2)
> **Estimated Complexity:** M

## Context

After SPEC-P1 provides the agent list page and SPEC-P2 provides the creation wizard, users need a way to view and edit existing agent files. This spec defines a dedicated editor page where users can open an agent's Markdown file, view a parsed summary (name, type, description), edit the full content (YAML frontmatter + system prompt body), save changes, or delete the agent.

The editor operates directly on the filesystem — the `.md` file in `{Project.Path}/.opencode/agents/` is the single source of truth. All changes are written back to the same file. The editor provides a full-text editing experience (not a structured form) because advanced users need to control all frontmatter fields (model, temperature, tools, permissions, etc.) that go beyond what the creation wizard generates.

## Actors

- **Authenticated User** — Any logged-in user with an active project selected, viewing or editing an existing agent.

## Functional Requirements

### Page & Routing

1. The system SHALL expose a Blazor page at route `/projects/{projectId:guid}/agents/{agentName}`.
2. The `agentName` route parameter SHALL correspond to the agent filename without the `.md` extension.
3. The page SHALL validate that the `projectId` corresponds to an existing, non-archived project. If not, it SHALL display a "Project not found" state consistent with other project pages.
4. The page SHALL validate that the agent file exists at `{Project.Path}/.opencode/agents/{agentName}.md`. If not, it SHALL display an "Agent not found" state with a link back to the agent list.
5. The page SHALL set the active project in `ActiveProjectService` when navigated to directly.
6. The page title SHALL be "{AgentName} — Agents — {ProjectName} — OwlNet".

### Agent Summary Header

7. The page SHALL display a summary header above the editor with:
    - **Agent Name** — displayed prominently (e.g., `Typo.h5` or `Typo.h6`), derived from the filename.
    - **Type Badge** — a `MudChip` showing the `mode` value (Primary / Subagent / All / Unknown), using the same color scheme as the agent list page (SPEC-P1 FR-15).
    - **Description** — the `description` from the frontmatter, displayed as secondary text below the name.
8. The summary header SHALL update dynamically when the user modifies the frontmatter in the editor and the content is saved (not in real-time as the user types — only after save).

### Editor

9. The page SHALL display the full raw Markdown content (YAML frontmatter + body) in an editable `MudTextField` (multiline, monospace font, full-width).
10. The editor SHALL be sized to use the available vertical space (e.g., `Lines="20"` minimum, or CSS-driven to fill the viewport).
11. The user SHALL be able to edit any part of the content: YAML frontmatter fields, frontmatter structure, and the Markdown body text.
12. The system SHALL track whether the current editor content differs from the last saved/loaded content (dirty state detection).

### Save

13. The page SHALL display a "Save" button (`MudButton`, Variant.Filled, Color.Primary, with `StartIcon` of `Save`).
14. The "Save" button SHALL be enabled only when there are unsaved changes (dirty state).
15. Clicking "Save" SHALL write the editor content to `{Project.Path}/.opencode/agents/{agentName}.md` via the `UpdateAgentCommand`, overwriting the existing file.
16. On save success: show a success snackbar "Agent '{agentName}' saved successfully." and update the summary header with the new frontmatter values. Reset the dirty state.
17. On save failure: show an error snackbar with the error message. The editor content SHALL remain unchanged (no data loss).

### Delete

18. The page SHALL display a "Delete" button (`MudButton`, Variant.Outlined, Color.Error, with `StartIcon` of `Delete`).
19. Clicking "Delete" SHALL show a confirmation dialog: "Are you sure you want to delete the agent '{agentName}'? This action cannot be undone."
20. On confirmation: the system SHALL delete the file `{Project.Path}/.opencode/agents/{agentName}.md` via the `DeleteAgentCommand`.
21. On delete success: navigate to `/projects/{projectId}/agents` and show a success snackbar "Agent '{agentName}' deleted."
22. On delete failure: show an error snackbar with the error message. Remain on the editor page.

### Navigation & Unsaved Changes Guard

23. The page SHALL display a "Back" button (or breadcrumb) that navigates to `/projects/{projectId}/agents`.
24. If the user has unsaved changes and attempts to navigate away (via "Back" button, sidebar link, or browser navigation), the system SHALL show a confirmation dialog: "You have unsaved changes. Leave without saving?"
25. If the user confirms: navigate away, discarding changes.
26. If the user cancels: remain on the editor page.

### Application Layer — Commands & Queries

27. The system SHALL define a `GetAgentFileQuery` record with properties: `ProjectId` (Guid), `AgentName` (string).
28. The `GetAgentFileQuery` handler SHALL:
    a. Retrieve the project to obtain `Project.Path`.
    b. Validate the project exists and is not archived.
    c. Call `IAgentFileService.GetAgentAsync(projectPath, agentName, cancellationToken)`.
    d. If the agent file is not found, return `Result.Failure("Agent not found.")`.
    e. Return `Result<AgentFileDto>` with the full file content and parsed frontmatter.
29. The system SHALL define an `UpdateAgentCommand` record with properties: `ProjectId` (Guid), `AgentName` (string), `Content` (string).
30. The `UpdateAgentCommand` handler SHALL:
    a. Retrieve the project to obtain `Project.Path`.
    b. Validate the project exists and is not archived.
    c. Validate the content is not empty.
    d. Call `IAgentFileService.WriteAgentAsync(projectPath, agentName, content, cancellationToken)` to overwrite the file.
    e. Return `Result` indicating success or failure.
31. The system SHALL define a `DeleteAgentCommand` record with properties: `ProjectId` (Guid), `AgentName` (string).
32. The `DeleteAgentCommand` handler SHALL:
    a. Retrieve the project to obtain `Project.Path`.
    b. Validate the project exists and is not archived.
    c. Call `IAgentFileService.DeleteAgentAsync(projectPath, agentName, cancellationToken)`.
    d. Return `Result` indicating success or failure.

### Loading State

33. The page SHALL display a loading state (skeleton) while the agent file is being read from the filesystem.

## User Flow

### Happy Path — View and Edit Agent

1. User is on the agent list page and clicks on the "code-reviewer" agent row.
2. System navigates to `/projects/{projectId}/agents/code-reviewer`.
3. Page shows loading skeleton briefly.
4. System reads `{Project.Path}/.opencode/agents/code-reviewer.md`.
5. Summary header displays: Name "code-reviewer", Type badge "Subagent", Description "Reviews code for quality and best practices".
6. Editor displays the full Markdown content (frontmatter + body).
7. User modifies the system prompt body to add a new focus area.
8. "Save" button becomes enabled (dirty state detected).
9. User clicks "Save".
10. File is overwritten. Success snackbar: "Agent 'code-reviewer' saved successfully."
11. "Save" button becomes disabled again (clean state).

### Happy Path — Delete Agent

1. User is viewing the "old-helper" agent in the editor.
2. User clicks "Delete".
3. Confirmation dialog: "Are you sure you want to delete the agent 'old-helper'? This action cannot be undone."
4. User clicks "Confirm".
5. File is deleted. User is navigated to the agent list page. Snackbar: "Agent 'old-helper' deleted."

### Happy Path — Navigate Away with Unsaved Changes

1. User is editing an agent and has made changes (dirty state).
2. User clicks "Back" to return to the agent list.
3. Confirmation dialog: "You have unsaved changes. Leave without saving?"
4. User clicks "Leave" → navigated to agent list, changes discarded.
5. (Alternative) User clicks "Stay" → remains on editor page with changes intact.

### Happy Path — Direct URL Navigation

1. User navigates directly to `/projects/{projectId}/agents/code-reviewer`.
2. Active project is set. Sidebar appears with "Agents" highlighted.
3. Agent file is loaded and displayed in the editor.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| Agent file does not exist (invalid `agentName` in URL) | "Agent not found" state with icon, message, and link back to agent list. |
| Project not found or archived | "Project not found" state consistent with other project pages. |
| Agent file is deleted externally while user is viewing it | Save attempt fails with error: "Agent file not found. It may have been deleted externally." |
| Agent file is modified externally while user is editing | No conflict detection in V1 — user's save overwrites the external changes. |
| User saves empty content | Validation error: "Agent content cannot be empty." Save button remains enabled. |
| User saves content with invalid YAML frontmatter | File is saved as-is (the editor does not validate YAML syntax — this is the user's responsibility). OpenCode will handle parsing errors at runtime. |
| Filesystem write error (permissions, disk full) | Error snackbar: "Failed to save agent. Check filesystem permissions." Editor content preserved. |
| Filesystem delete error | Error snackbar: "Failed to delete agent." Remain on editor page. |
| Very large agent file (> 10KB) | Editor handles it normally — `MudTextField` supports large text. May add a character count indicator. |
| Agent name contains URL-unsafe characters | The route parameter `agentName` only supports alphanumeric + hyphens (same constraint as creation). If a file with other characters exists, it won't be routable — it will still appear in the list (SPEC-P1) but clicking it may fail. This is acceptable since the creation wizard (SPEC-P2) enforces the naming constraint. |

## Out of Scope

- Syntax highlighting for YAML or Markdown in the editor.
- Live preview of rendered Markdown alongside the editor.
- Split-pane editor (raw + preview side by side).
- Version history, undo/redo beyond browser-native textarea behavior.
- Renaming an agent (changing the filename) — user must delete and recreate.
- Structured form editing of individual frontmatter fields (the editor is raw text).
- Conflict detection when the file is modified externally.
- Agent validation (checking if the agent definition is valid for OpenCode).

## Acceptance Criteria

- [ ] Page exists at `/projects/{projectId:guid}/agents/{agentName}` with proper project and agent validation.
- [ ] Page title is "{AgentName} — Agents — {ProjectName} — OwlNet".
- [ ] Page sets active project when navigated to directly.
- [ ] "Agent not found" state shown when the agent file does not exist.
- [ ] Summary header displays: agent name, type badge (with correct color), description.
- [ ] Full Markdown content displayed in editable multiline monospace textarea.
- [ ] Dirty state detection: "Save" button enabled only when content differs from loaded content.
- [ ] "Save" writes content to the correct file path via `UpdateAgentCommand`.
- [ ] Save success: snackbar + summary header updated + dirty state reset.
- [ ] Save failure: error snackbar, editor content preserved.
- [ ] "Delete" shows confirmation dialog before deleting.
- [ ] Delete success: navigate to agent list + snackbar.
- [ ] Delete failure: error snackbar, remain on page.
- [ ] "Back" button navigates to `/projects/{projectId}/agents`.
- [ ] Unsaved changes guard: confirmation dialog when navigating away with dirty state.
- [ ] Loading skeleton shown during initial file read.
- [ ] `GetAgentFileQuery` + handler exist and return `Result<AgentFileDto>`.
- [ ] `UpdateAgentCommand` + handler exist and write file via `IAgentFileService`.
- [ ] `DeleteAgentCommand` + handler exist and delete file via `IAgentFileService`.
- [ ] Unit tests for `GetAgentFileQuery` handler: happy path, agent not found, project not found.
- [ ] Unit tests for `UpdateAgentCommand` handler: happy path, empty content, project not found.
- [ ] Unit tests for `DeleteAgentCommand` handler: happy path, project not found.
- [ ] Clicking an agent row in the list page (SPEC-P1) navigates to this editor page.

## Dependencies

- **SPEC-P1-agent-list-page** — `IAgentFileService` (GetAgentAsync, WriteAgentAsync, DeleteAgentAsync), `AgentFileDto`, agent list page for back navigation, row click navigation.
- **SPEC-001-project-crud** — Project entity, repository, `ActiveProjectService`.

## Open Questions

1. Should the editor provide a "Format" or "Validate YAML" button to help users catch frontmatter syntax errors before saving? Recommendation: not in V1, but could be a nice enhancement later.
2. Should the summary header update in real-time as the user types (by parsing frontmatter on every keystroke) or only after save? Current decision: only after save, for simplicity and performance.
3. Should keyboard shortcut Ctrl+S trigger save? Recommendation: yes, if feasible with Blazor Server — nice UX improvement.
