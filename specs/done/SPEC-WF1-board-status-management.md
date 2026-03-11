# SPEC: Board Status Management

> **Status:** Todo
> **Created:** 2026-03-10
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

OwlNet's Kanban board currently uses a hardcoded `BoardStatus` enum in the Web layer with mock data and no persistence. To support workflow triggers (which react to card state transitions) and a flexible board, statuses must become a managed, persisted domain concept.

Statuses must be available at two levels: a **global default set** (applied to all new projects) and a **per-project override** (each project can customize its own statuses independently). The system ships with five default statuses: Backlog, ToEvaluate, Develop, Review, and Done.

## Actors

| Actor | Description |
|-------|-------------|
| **User** | Authenticated user managing board configuration |
| **System** | Provides default statuses on project creation |

## Functional Requirements

1. The system SHALL persist board statuses as domain entities with the following properties: unique identifier (Guid), name (string, 1-100 chars, not blank), sort order (int), and a flag indicating whether it is a system default.
2. The system SHALL maintain a **global set of default statuses** that are not tied to any specific project. These serve as templates.
3. The system SHALL seed five global default statuses on first run: Backlog (order 0), ToEvaluate (order 1), Develop (order 2), Review (order 3), Done (order 4).
4. When a new project is created, the system SHALL automatically copy the current global default statuses into the project as **project-level statuses**, preserving names and sort order.
5. Each project SHALL have its own independent set of statuses. Changes to a project's statuses SHALL NOT affect global defaults or other projects.
6. The user SHALL be able to **add** new statuses to a project, specifying a name and sort order.
7. The user SHALL be able to **rename** any project-level status, including those originally copied from defaults.
8. The user SHALL be able to **delete** a project-level status, provided no cards in the project currently use that status. If cards exist with that status, the system SHALL reject the deletion and inform the user.
9. The user SHALL be able to **reorder** project-level statuses. The sort order defines the left-to-right column order on the Kanban board.
10. The user SHALL be able to manage **global default statuses** (add, rename, delete, reorder) from the application's global settings. Changes to global defaults only affect **future** project creations, not existing projects.
11. Status names SHALL be unique within a project scope (case-insensitive). Status names SHALL be unique within the global defaults scope (case-insensitive).
12. The system SHALL expose the project's ordered list of statuses for use by the Board UI and Workflow Trigger features.

## User Flow (Project-Level)

1. User navigates to project settings or board configuration.
2. System displays the current list of statuses for the project, ordered by sort order.
3. User can drag-and-drop statuses to reorder them.
4. User can click "Add Status" to create a new status (enters name, placed at the end by default).
5. User can click on a status name to rename it inline.
6. User can click a delete icon on a status to remove it. If cards use that status, a warning is shown and deletion is blocked.
7. All changes are persisted immediately (or via explicit save — see Open Questions).

## User Flow (Global Defaults)

1. User navigates to global Settings page.
2. System displays a "Default Board Statuses" section with the current global defaults.
3. User can add, rename, delete, and reorder default statuses using the same interactions as project-level management.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| User tries to add a status with a name that already exists in the project | Reject with validation error: "A status with this name already exists" |
| User tries to delete a status that has cards assigned to it | Reject with error: "Cannot delete status — N cards are currently in this status. Move them first." |
| User tries to add a status with blank or whitespace-only name | Reject with validation error: "Status name is required" |
| User tries to add a status with name exceeding 100 characters | Reject with validation error: "Status name must be 100 characters or fewer" |
| Global defaults are modified after projects already exist | Existing projects are not affected; only new projects get the updated defaults |
| A project has zero statuses (all deleted) | The board displays an empty state prompting the user to add statuses |
| Two users edit statuses concurrently | Last-write-wins (optimistic concurrency — see Open Questions) |

## Out of Scope

- Board UI rendering of columns based on statuses (will use the ordered status list, but column rendering is an existing Board concern).
- Card entity and card-status assignment (covered in SPEC-WF2-board-card-entity).
- Workflow triggers reacting to status changes (covered in SPEC-WF3-workflow-trigger-entity).
- Per-status color or icon customization (can be added in a future spec).
- Status grouping or categories (e.g., "In Progress" grouping multiple statuses).

## Acceptance Criteria

- [ ] Global default statuses are seeded on first application run (Backlog, ToEvaluate, Develop, Review, Done).
- [ ] Creating a new project copies global defaults into project-level statuses.
- [ ] Project-level statuses can be added, renamed, deleted, and reordered independently.
- [ ] Deleting a status in use by cards is rejected with a clear error message.
- [ ] Status names are validated for uniqueness (case-insensitive) within their scope.
- [ ] Global default statuses can be managed from the global Settings page.
- [ ] Changes to global defaults do not retroactively affect existing projects.
- [ ] The ordered list of project statuses is available for consumption by Board and Workflow features.
- [ ] All status operations are persisted to the database.

## Dependencies

- **Project entity** (`OwlNet.Domain.Entities.Project`) — statuses belong to a project.
- **EF Core dual-provider migrations** — new tables for global and project-level statuses.
- **Global Settings page** (`/settings`) — needs a new section for default status management.

## Open Questions

1. **Save mode for reordering**: Should reorder changes be persisted on each drag-and-drop action (auto-save) or require an explicit "Save" button? Auto-save is simpler UX but generates more DB writes.
2. **Concurrency**: Is optimistic concurrency (last-write-wins) acceptable for status management, or do we need conflict detection?
3. **UI location for project-level status management**: Should this be a dedicated "Settings" sub-page within the project, or integrated directly into the Board page (e.g., a settings panel/drawer)?
