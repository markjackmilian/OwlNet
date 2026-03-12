# SPEC: Project Board Statuses Settings Page

> **Status:** Done
> **Created:** 2026-03-12
> **Author:** owl-planner + user
> **Priority:** Medium
> **Estimated Complexity:** M

## Context

When a project is created, the global default board statuses are copied into the
project scope. Currently there is no UI to manage these project-level statuses:
users cannot add new statuses, rename existing ones, change their order, or delete
them. The backend (commands and queries) is fully implemented and scope-aware.
This spec covers the frontend page and the reuse of the existing
`DefaultBoardStatusesPanel` component (or a generalized version of it) to expose
this functionality at the project level.

## Actors

- **User** — any authenticated user with access to the project (no role restriction).

## Functional Requirements

1. The system SHALL expose a new route `/projects/{projectId}/settings` rendering
   a project settings page.
2. The settings page SHALL include a "Board Statuses" section displaying all
   statuses currently defined for the project, ordered by `SortOrder` ascending.
3. The system SHALL allow the user to **add** a new status to the project by
   entering a name and confirming; the new status SHALL be appended at the end
   of the list (highest `SortOrder + 1`).
4. The system SHALL allow the user to **rename** an existing project status inline;
   the new name SHALL be unique within the project scope (case-insensitive).
5. The system SHALL allow the user to **delete** a project status; if any card is
   currently assigned to that status, the deletion SHALL be blocked and an error
   message SHALL be displayed to the user explaining the reason.
6. The system SHALL allow the user to **reorder** project statuses via drag-and-drop;
   the new order SHALL be persisted immediately after the drop.
7. A new **"Settings"** navigation entry SHALL be added to the project-scoped
   sidebar menu, linking to `/projects/{projectId}/settings`, consistent with the
   existing entries (Board, Workflow, Agents).
8. The "Board Statuses" section SHALL reuse or generalize the existing
   `DefaultBoardStatusesPanel` Blazor component to avoid duplication of UI logic.

## User Flow

1. User opens a project and clicks **"Settings"** in the left sidebar.
2. The system navigates to `/projects/{projectId}/settings`.
3. The page displays a "Board Statuses" section with the list of current project
   statuses, each showing its name and a drag handle for reordering.
4. **Add:** User types a name in the input field and clicks "Add". The new status
   appears at the bottom of the list.
5. **Rename:** User clicks the edit icon on a status, modifies the name inline,
   and confirms. The updated name is reflected immediately.
6. **Reorder:** User drags a status to a new position. The order is saved
   automatically on drop.
7. **Delete:** User clicks the delete icon on a status and confirms the dialog.
   - If no cards are assigned: the status is removed from the list.
   - If cards are assigned: the deletion is blocked and a snackbar/error message
     informs the user (e.g., "Cannot delete: X card(s) are assigned to this status.").

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| User tries to add a status with a name already existing in the project | Inline validation error: "A status with this name already exists." |
| User tries to add a status with an empty name | Add button disabled / inline validation error |
| User tries to delete a status with assigned cards | Deletion blocked; snackbar error with card count |
| User tries to delete the last remaining status | Deletion blocked; error: "A project must have at least one status." |
| User renames a status to the same name as another status in the project | Inline validation error: "A status with this name already exists." |
| Network/server error during any operation | Snackbar error message; UI reverts to previous state |

## Out of Scope

- Managing global default statuses (covered by the existing `/settings` page).
- Resetting project statuses to global defaults.
- Any other project settings (name, description, archiving, etc.) — to be covered
  by future specs.
- Role-based access control for the settings page.
- Card reordering within kanban columns.
- Connecting the kanban board to real data (currently uses mock data).

## Acceptance Criteria

- [x] Route `/projects/{projectId}/settings` exists and is reachable.
- [x] "Settings" entry appears in the project sidebar navigation.
- [x] The page displays the correct project-level statuses (not global defaults).
- [x] User can add a new status; it appears at the end of the list.
- [x] User can rename a status; the change is persisted.
- [x] User can reorder statuses via drag-and-drop; the order is persisted.
- [x] User can delete a status with no assigned cards; it is removed.
- [x] Deleting a status with assigned cards is blocked with a clear error message.
- [x] Deleting the last status is blocked with a clear error message.
- [x] Duplicate name validation works on both add and rename.
- [x] The `DefaultBoardStatusesPanel` component (or a generalized version) is
     reused — no copy-paste duplication of drag-reorder + CRUD logic.

## Dependencies

- `AddBoardStatusCommand` — already implemented, supports `ProjectId`
- `RenameBoardStatusCommand` — already implemented
- `DeleteBoardStatusCommand` — already implemented (blocks if cards assigned)
- `ReorderBoardStatusesCommand` — already implemented, supports `ProjectId`
- `GetProjectStatusesQuery` — already implemented
- `DefaultBoardStatusesPanel.razor` — existing component to reuse/generalize

## Open Questions

- Should `DefaultBoardStatusesPanel` be **generalized into a shared component**
  (e.g., `BoardStatusesPanel` accepting a nullable `ProjectId` parameter) or
  should a **new project-specific component** be created that delegates to the
  same backend commands? This is an implementation decision for the developer.
