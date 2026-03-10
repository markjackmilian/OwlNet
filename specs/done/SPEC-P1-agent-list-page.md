# SPEC: Agent List Page

> **Status:** Done
> **Created:** 2026-03-10
> **Author:** owl-planner + user
> **Priority:** High (P1 — prerequisite for P2 and P3)
> **Estimated Complexity:** M

## Context

OwlNet projects use OpenCode as their AI coding agent runtime. OpenCode supports custom agents defined as `.md` files with YAML frontmatter stored under `.opencode/agents/` relative to the project workspace path (`Project.Path`). Currently, the project-scoped sidebar shows a "Settings" item (a placeholder page at `/projects/{id}/settings`) as its third navigation entry.

This spec replaces that placeholder "Settings" sidebar item with a fully functional "Agents" page, giving users a clear view of which agents are configured for the active project. It also establishes the `IAgentFileService` abstraction and the `GetProjectAgentsQuery` handler that SPEC-P2 and SPEC-P3 will build upon.

This is the foundational spec (P1) that must be completed before the agent creation wizard (SPEC-P2) and agent editor (SPEC-P3) can be implemented.

## Actors

- **Authenticated User** — Any logged-in user with an active project selected.

## Functional Requirements

### Navigation — Sidebar

1. The system SHALL replace the "Settings" sidebar item in the project-scoped navigation with an "Agents" item.
2. The "Agents" sidebar item SHALL use the `SmartToy` Material Design icon (or equivalent robot/AI icon available in `Icons.Material.Filled`).
3. The "Agents" sidebar item SHALL link to `/projects/{projectId}/agents`.
4. The previous "Settings" sidebar item and its route (`/projects/{id}/settings`) SHALL be removed from the project-scoped sidebar. The `ProjectSettings.razor` placeholder page SHALL be deleted.
5. The global settings icon button in the topbar (gear icon → `/settings`) SHALL remain untouched.

### Page & Routing

6. The Agents page SHALL be accessible at the route `/projects/{projectId:guid}/agents`.
7. The page SHALL set the active project in `ActiveProjectService` when navigated to directly (e.g., via bookmark), consistent with the pattern used by the Dashboard and Board pages.
8. The page SHALL validate that the `projectId` corresponds to an existing, non-archived project. If not, it SHALL display a "Project not found" message consistent with other project pages.
9. The page title SHALL be "Agents — {ProjectName} — OwlNet".

### Agent Discovery & Display

10. The system SHALL load agents by reading `.md` files from the directory `{Project.Path}/.opencode/agents/` using `IAgentFileService`.
11. If the `.opencode/agents/` directory does not exist, the system SHALL treat this as an empty agent list (zero agents) and show the empty state UI — it SHALL NOT show an error.
12. The system SHALL parse the YAML frontmatter of each `.md` file to extract: `description` (string), `mode` (string: `"primary"`, `"subagent"`, or `"all"`). The agent name is derived from the filename without the `.md` extension.
13. If a file's frontmatter is missing or malformed, the system SHALL still include the agent in the list with empty/default values for the missing fields, and SHALL NOT throw an exception.
14. The page SHALL display agents in a `MudList` or `MudDataGrid` with the following columns:
    - **Name** — the filename without extension (monospace or bold font)
    - **Type** — a `MudChip` badge derived from the `mode` field: "Primary" (mode=`primary`), "Subagent" (mode=`subagent`), "All" (mode=`all`). If mode is unrecognized or missing, display "Unknown" with a neutral color.
    - **Description** — the `description` frontmatter field, truncated to a single line with ellipsis if too long
    - **Actions** — a button or icon to navigate to the agent editor page (`/projects/{projectId}/agents/{agentName}`)
15. The Type badge SHALL use distinct `MudBlazor` chip colors: Primary → `Color.Primary`, Subagent → `Color.Secondary`, All → `Color.Tertiary`, Unknown → `Color.Default`.
16. The agents list SHALL be sorted alphabetically by name (ascending) by default.
17. Each row SHALL be clickable (the entire row, not just the actions button) and SHALL navigate to `/projects/{projectId}/agents/{agentName}`.

### Empty State

18. When the agents list is empty (no `.md` files found, or directory does not exist), the page SHALL display an empty state panel containing:
    - A large icon (e.g., `SmartToy` or `PrecisionManufacturing`)
    - A heading: "No agents configured yet"
    - A subtitle: "Create your first agent to extend the capabilities of this project."
    - A prominent `MudButton` with variant `Filled` and color `Primary`: "Add Agent" that navigates to `/projects/{projectId}/agents/new`
19. The empty state SHALL be centered within the page content area.

### Add Agent Action

20. The page SHALL display an "Add Agent" action button (e.g., `MudFab` or `MudButton` with `StartIcon="Icons.Material.Filled.Add"`) visible at all times (not only in empty state), positioned in the page header or as a floating action button.
21. Clicking "Add Agent" SHALL navigate to `/projects/{projectId}/agents/new` (the creation wizard, defined in SPEC-P2).

### Loading State

22. The page SHALL display a loading skeleton (e.g., `MudSkeleton` rows) while the agent files are being read from the filesystem.

### Application Layer — Query

23. The system SHALL provide a `GetProjectAgentsQuery` record in the Application layer with property `ProjectId` (Guid).
24. The `GetProjectAgentsQuery` handler SHALL:
    a. Retrieve the project from the repository to obtain `Project.Path`.
    b. If the project does not exist or is archived, return `Result.Failure("Project not found.")`.
    c. Call `IAgentFileService.GetAgentsAsync(projectPath, cancellationToken)` to retrieve the list of agents.
    d. Return `Result<IReadOnlyList<AgentFileDto>>`.
25. The `AgentFileDto` record SHALL be defined in the Application layer with the following properties:
    - `FileName` (string) — filename without `.md` extension (the agent name/identifier)
    - `FilePath` (string) — full absolute path to the `.md` file
    - `Mode` (string) — value of the `mode` frontmatter field, or empty string if absent
    - `Description` (string) — value of the `description` frontmatter field, or empty string if absent
    - `RawContent` (string) — full raw file content including frontmatter

### Infrastructure Layer — IAgentFileService

26. The system SHALL define an `IAgentFileService` interface in the Application layer with the following methods:
    - `Task<IReadOnlyList<AgentFileDto>> GetAgentsAsync(string projectPath, CancellationToken cancellationToken)`
    - `Task<AgentFileDto?> GetAgentAsync(string projectPath, string agentName, CancellationToken cancellationToken)`
    - `Task WriteAgentAsync(string projectPath, string agentName, string content, CancellationToken cancellationToken)`
    - `Task DeleteAgentAsync(string projectPath, string agentName, CancellationToken cancellationToken)`
27. The `AgentFileService` implementation SHALL:
    a. Construct the agents directory path as `Path.Combine(projectPath, ".opencode", "agents")`.
    b. Return an empty list if the directory does not exist (no exception thrown).
    c. Parse YAML frontmatter by detecting the `---` delimiters and extracting key-value pairs. Frontmatter parsing errors SHALL be caught and result in default/empty field values.
    d. For `WriteAgentAsync`, create the `.opencode/agents/` directory structure if it does not exist before writing the file.
    e. For `DeleteAgentAsync`, verify the file exists before attempting deletion; return silently if the file does not exist.
28. `AgentFileService` SHALL be registered in the Infrastructure DI extension (`AddInfrastructure()`).
29. The existing `IFileSystem` interface SHALL be extended with the additional methods needed by `AgentFileService` (e.g., `GetFiles`, `FileExists`, `ReadAllTextAsync`, `WriteAllTextAsync`, `CreateDirectory`, `DeleteFile`). This keeps all filesystem access behind the testable abstraction.

## User Flow

### Happy Path — View Agents List

1. User has an active project selected ("Project Alpha").
2. User clicks "Agents" in the sidebar.
3. Page navigates to `/projects/{projectId}/agents`.
4. Loading skeleton is displayed briefly.
5. The system reads `.opencode/agents/` inside `Project.Path` and finds 3 `.md` files.
6. The agents list renders with 3 rows: Name, Type badge, Description, and an edit action button.
7. User sees agents sorted alphabetically.

### Happy Path — Empty State

1. User selects a newly created project with no `.opencode/agents/` folder.
2. User clicks "Agents" in the sidebar.
3. Page loads. No agents found.
4. Empty state is shown: icon, "No agents configured yet" heading, subtitle, and "Add Agent" button.

### Happy Path — Navigate to Agent Editor

1. User is on the Agents page with a list of agents.
2. User clicks on the row for "code-reviewer".
3. User is navigated to `/projects/{projectId}/agents/code-reviewer`.

### Happy Path — Navigate to Add Agent

1. User is on the Agents page (with or without existing agents).
2. User clicks "Add Agent" button.
3. User is navigated to `/projects/{projectId}/agents/new`.

### Happy Path — Direct URL Navigation

1. User navigates directly to `/projects/{projectId}/agents` via a bookmark.
2. `ActiveProjectService` sets the project as active.
3. Sidebar appears with "Agents" highlighted.
4. Page loads and displays the agent list normally.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| `Project.Path` directory does not exist on disk | `GetAgentsAsync` returns empty list; empty state shown. No error surfaced to the user. |
| `.opencode/agents/` directory does not exist | Same as above — empty state shown, no error. |
| A `.md` file has no YAML frontmatter | File is included in the list with empty `Mode` and `Description`; displayed as "Unknown" type. |
| A `.md` file has malformed YAML frontmatter | Frontmatter parsing error is caught; file included with default/empty fields. No exception propagated. |
| A file in the agents directory has an extension other than `.md` | Non-`.md` files are ignored. |
| An empty `.md` file (zero bytes) | Included in list with empty fields; `RawContent` is empty string. |
| `projectId` in URL does not correspond to any project | "Project not found" message shown, consistent with other project pages. |
| `projectId` in URL corresponds to an archived project | "Project not found" message shown (archived treated as not found). |
| Filesystem read error (permissions, I/O error) | Error is caught; `Result.Failure` returned with a generic error message. Page shows an error alert: "Failed to load agents. Please try again." |
| Very long agent name or description | Name truncated visually in the table cell; full value visible on hover via `MudTooltip`. |
| Agent `mode` value is not one of the three known values | Type badge shows "Unknown" with `Color.Default`. |

## Out of Scope

- Agent creation wizard (see SPEC-P2).
- Agent editor / detail page (see SPEC-P3).
- Project Settings page (removed by this spec; global settings remain at `/settings`).
- Agent execution or invocation from within OwlNet.
- Filtering or searching the agents list.
- Agent reordering or drag-and-drop.
- Support for non-standard OpenCode agent locations or file formats.
- Version history or diffing of agent files.
- Database persistence of agent metadata (filesystem is the sole source of truth).

## Acceptance Criteria

- [ ] "Settings" sidebar item replaced with "Agents" (SmartToy icon, `/projects/{id}/agents` route).
- [ ] `ProjectSettings.razor` placeholder page and its route removed.
- [ ] Global settings gear in topbar continues to navigate to `/settings` unchanged.
- [ ] `ProjectAgentsPage.razor` exists at route `/projects/{projectId:guid}/agents`.
- [ ] Page sets active project when navigated to directly via URL.
- [ ] Page shows "Project not found" for invalid or archived project IDs.
- [ ] Page title is "Agents — {ProjectName} — OwlNet".
- [ ] `GetProjectAgentsQuery` and its handler exist in the Application layer and return `Result<IReadOnlyList<AgentFileDto>>`.
- [ ] `AgentFileDto` record exists with all required properties.
- [ ] `IAgentFileService` interface defined with all four methods.
- [ ] `AgentFileService` implementation registered in `AddInfrastructure()`.
- [ ] `IFileSystem` extended with necessary filesystem methods.
- [ ] Agent files are read from `{Project.Path}/.opencode/agents/*.md`.
- [ ] Non-`.md` files in the directory are ignored.
- [ ] Missing or non-existent directory results in empty list, not an error.
- [ ] Malformed frontmatter does not throw; defaults to empty fields.
- [ ] Agents list displays Name, Type badge, Description, and Actions columns.
- [ ] Type badges use correct MudBlazor colors per mode value.
- [ ] Unknown/missing mode displays "Unknown" badge with neutral color.
- [ ] Agent rows are sorted alphabetically by name.
- [ ] Clicking a row navigates to `/projects/{projectId}/agents/{agentName}`.
- [ ] "Add Agent" button is always visible and navigates to `/projects/{projectId}/agents/new`.
- [ ] Empty state panel shown when no agents exist with heading, subtitle, and "Add Agent" button.
- [ ] Loading skeleton shown during initial data fetch.
- [ ] Filesystem read errors surface a user-friendly error alert (not an exception page).
- [ ] Unit tests for `GetProjectAgentsQuery` handler covering: happy path, project not found, empty directory, malformed frontmatter.
- [ ] Unit tests for `AgentFileService` covering: directory not found, valid files, non-`.md` files filtered, frontmatter parsing.

## Dependencies

- **SPEC-project-scoped-navigation-shell** — Sidebar structure being modified.
- **SPEC-001-project-crud** — `ActiveProjectService`, project repository, project validation pattern.
- **SPEC-002-project-dashboard** — "Project not found" UI pattern reused.
- `IFileSystem` / `FileSystemService` — existing Infrastructure service (to be extended).
- MudBlazor `MudDataGrid` or `MudList`, `MudChip`, `MudSkeleton`, `MudFab`.

## Open Questions

1. Should the agents page use `MudDataGrid` (with sorting/pagination built-in) or a simpler `MudList` for the initial implementation? Recommendation: `MudList` with `MudListItem` for simplicity, since the agent count per project is typically small (< 20).
2. When the project's `Path` does not point to a valid directory on disk (e.g., the workspace was moved or deleted), should the page show the empty state or a more specific warning? Current decision: show empty state (same as no agents).
