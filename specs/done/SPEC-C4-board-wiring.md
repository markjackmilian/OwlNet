# SPEC: Board Wiring — Real Data

> **Status:** Done
> **Created:** 2026-03-12
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** L

## Context

The Kanban board page (SPEC-project-board-page) is fully implemented as a Blazor component but runs entirely on static mock data (`BoardMockData`, `BoardCardItem`, web-layer enums). The domain entities, Application CQRS layer, and Infrastructure repositories for `Card` and `BoardStatus` are already implemented (SPEC-WF2).

This spec replaces all mock data with real data from the database, wires drag-and-drop to `ChangeCardStatusCommand`, and integrates workflow trigger evaluation on every status change. The trigger execution engine itself (invoking OpenCode) is covered in SPEC-C8 — this spec only ensures that matching triggers are identified and queued/invoked after a status change.

The board must also handle the case where a project has a custom set of statuses (not the fixed 5 from the mock), rendering columns dynamically from `GetProjectStatusesQuery`.

## Actors

| Actor | Description |
|-------|-------------|
| **User** | Authenticated user viewing and interacting with the board |
| **System** | Loads real data, persists status changes, evaluates workflow triggers |

## Functional Requirements

### Data Loading

1. On page load, the board SHALL call `GetProjectStatusesQuery` to retrieve the project's statuses ordered by `SortOrder` ascending. Columns SHALL be rendered dynamically from this list (not hardcoded to 5 columns).
2. On page load, the board SHALL call `GetCardsByProjectQuery` to retrieve all cards for the project. The query result includes `StatusId`, `Priority`, `Title`, `Description`, and `Tags`.
3. The board SHALL display a loading skeleton while both queries are in flight. The skeleton SHALL mimic the column layout.
4. If the project has no statuses configured, the board SHALL display an empty state: "No board statuses configured. Go to Settings to add statuses." with a link to `/projects/{projectId}/settings`.
5. If the project has statuses but no cards, each column SHALL display the existing empty-column placeholder (dashed border, "Drop cards here").

### Mock Data Removal

6. The web-layer mock data classes (`BoardMockData`, `BoardCardItem`) SHALL be removed.
7. The web-layer `BoardStatus` enum and `CardPriority` enum SHALL be removed and replaced with the domain `CardPriority` enum and `BoardStatusDto` from the Application layer.
8. The `KanbanBoard` and `KanbanCard` components SHALL be updated to work with `CardDto` instead of `BoardCardItem`.

### Card Display

9. Each card on the board SHALL display:
   - Priority badge (using `CardDto.Priority`).
   - Title (using `CardDto.Title`).
   - Description rendered as Markdown, visually clamped to 2–3 lines (using `CardDto.Description`).
   - Tag chips: each tag in `CardDto.Tags` SHALL be displayed as a small colored chip below the description. If there are more than 3 tags, display the first 3 and a "+N more" indicator.
   - Card number (`CardDto.Number`) displayed as a small muted label (e.g., `#42`).
10. Cards SHALL be grouped into columns by `CardDto.StatusId` matching `BoardStatusDto.Id`.

### Drag-and-Drop — Status Change

11. When a user drops a card into a different column, the system SHALL:
    a. Immediately update the card's column in the UI (optimistic update).
    b. Call `ChangeCardStatusCommand` with the card ID, the new status ID, the authenticated user's ID, and `ChangeSource = Manual`.
    c. If the command fails, revert the card to its original column and display a snackbar error.
12. Dropping a card into the same column SHALL be a no-op (no command called, no UI change).
13. During the async status change operation, the dragged card SHALL display a subtle loading indicator (e.g., reduced opacity) to signal the operation is in progress.

### Workflow Trigger Evaluation on Drop

14. After a successful `ChangeCardStatusCommand`, the system SHALL call `GetWorkflowTriggersByTransitionQuery` with the card's previous status ID and new status ID to find matching enabled triggers.
15. If matching triggers are found, the system SHALL invoke the trigger execution pipeline (SPEC-C8). This invocation is fire-and-forget from the board's perspective — the board does NOT wait for trigger completion.
16. If no matching triggers are found, no action is taken.
17. The board SHALL display a visual indicator on the card when a trigger is being executed (e.g., a small animated icon). This indicator is removed when the trigger execution completes or fails (notification via a board-level event or polling — implementation detail for the developer).

### Column Header

18. Each column header SHALL display:
    - The status name (from `BoardStatusDto.Name`).
    - A live count of cards in that column.
    - An "Add card" icon button that opens the card creation dialog (SPEC-C5).
    - A color indicator. Since statuses are now dynamic, the color SHALL be assigned deterministically from a fixed palette based on the status's `SortOrder` (e.g., index 0 = blue, 1 = orange, 2 = green, etc.).

### Board Refresh

19. After a card is successfully created (via SPEC-C5 dialog), the board SHALL refresh its card list to include the new card without a full page reload.
20. After a card is updated or deleted (via SPEC-C6 dialog), the board SHALL refresh accordingly.

## User Flow

### Happy Path — View Board with Real Data
1. User navigates to `/projects/{projectId}/board`.
2. System loads project statuses and cards from the database.
3. Board renders with dynamic columns and real cards.
4. User sees card numbers, titles, priorities, tags, and Markdown descriptions.

### Happy Path — Move a Card
1. User drags card `#12` from "Backlog" to "Develop".
2. Card moves immediately (optimistic update).
3. System persists the status change via `ChangeCardStatusCommand`.
4. System queries for matching workflow triggers.
5. If a trigger matches, the trigger execution pipeline is invoked asynchronously.
6. A small animated icon appears on the card to indicate trigger execution.
7. When execution completes, the icon disappears.

### Error Path — Status Change Fails
1. User drags a card to a new column.
2. Card moves immediately (optimistic update).
3. Server returns an error (e.g., network failure).
4. Card reverts to its original column.
5. Snackbar displays: "Failed to move card. Please try again."

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| Project has no statuses | Empty state with link to Settings page |
| Project has statuses but no cards | Columns render with empty-column placeholder |
| Status change command fails (network/server error) | Optimistic update reverted; snackbar error displayed |
| Card belongs to a status that no longer exists in the project | Card is displayed in an "Unknown" column or excluded from the board with a warning (implementation decision) |
| Two users move the same card simultaneously | Last write wins on the domain entity; both history records are preserved (per SPEC-WF2) |
| Workflow trigger evaluation fails | Board is unaffected; trigger failure is logged; no snackbar shown (trigger execution is fire-and-forget) |
| Project has more than 10 statuses | Board scrolls horizontally; column layout is preserved |

## Out of Scope

- Card creation form (covered in SPEC-C5).
- Card detail dialog (covered in SPEC-C6).
- Trigger execution engine (covered in SPEC-C8).
- Board filtering by priority, tag, or assignee (future spec).
- Card ordering within a column (beyond the current `AllowReorder` behavior — ordering is not persisted).
- Swimlanes or grouping.
- Real-time multi-user board updates (future spec).

## Acceptance Criteria

- [ ] Board loads project statuses dynamically from `GetProjectStatusesQuery` (not hardcoded).
- [ ] Board loads real cards from `GetCardsByProjectQuery`.
- [ ] Mock data classes (`BoardMockData`, `BoardCardItem`, web-layer enums) are removed.
- [ ] `KanbanCard` component uses `CardDto` (domain model).
- [ ] Cards display number, title, priority, Markdown description (clamped), and tag chips.
- [ ] Columns are rendered dynamically; count and color are correct.
- [ ] Drag-and-drop calls `ChangeCardStatusCommand` on column change.
- [ ] Optimistic update reverts on command failure with snackbar error.
- [ ] Dropping into the same column is a no-op.
- [ ] After successful status change, `GetWorkflowTriggersByTransitionQuery` is called.
- [ ] Matching triggers are forwarded to the execution pipeline (SPEC-C8) asynchronously.
- [ ] Card displays a loading indicator during trigger execution.
- [ ] "Add card" button in column header opens the card creation dialog (SPEC-C5).
- [ ] Board refreshes after card creation, update, or deletion.
- [ ] Loading skeleton is shown during initial data load.
- [ ] Empty state is shown when project has no statuses.

## Dependencies

- **SPEC-WF2-board-card-entity** — `Card` entity, `GetCardsByProjectQuery`, `ChangeCardStatusCommand`.
- **SPEC-WF1-board-status-management** — `BoardStatus` entity, `GetProjectStatusesQuery`.
- **SPEC-WF3-workflow-trigger-entity** — `GetWorkflowTriggersByTransitionQuery`.
- **SPEC-C1-card-entity-enrichment** — `CardDto.Tags` must be available.
- **SPEC-C5-card-create** — "Add card" button integration.
- **SPEC-C8-trigger-execution** — Trigger execution pipeline to invoke after status change.
- **SPEC-project-board-page** — Existing board components to refactor.

## Open Questions

1. **Card ordering within columns**: The current board uses `AllowReorder="true"` on `MudDropZone`. Should the reorder position be persisted to the database (requires a `SortOrder` field on `Card`)? Currently out of scope — order is not persisted.
2. **Trigger execution indicator**: How should the board know when a trigger has finished executing? Options: (a) polling, (b) SignalR/Blazor event, (c) no indicator (fire-and-forget with no feedback). This is an implementation decision for SPEC-C8 to clarify.
3. **Color palette for dynamic columns**: Should the color palette be defined in the theme or hardcoded in the component? Implementation decision for the developer.
