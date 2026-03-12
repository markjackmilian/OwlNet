# SPEC: Card Creation

> **Status:** Done
> **Created:** 2026-03-12
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

Cards are the primary work item in OwlNet. Users need to create cards from two entry points: the "Add card" button in a board column header (pre-filling the status to that column), and a global "New card" button accessible from anywhere within a project. Both entry points open the same creation dialog.

This spec covers the card creation UI only. The underlying `CreateCardCommand` is already implemented (SPEC-WF2). This spec adds tag selection to the creation flow (requiring SPEC-C1 to be implemented first).

## Actors

| Actor | Description |
|-------|-------------|
| **User** | Authenticated user creating a new card within a project |

## Functional Requirements

### Entry Points

1. The board column header "Add card" button (introduced as non-functional in SPEC-project-board-page and wired in SPEC-C4) SHALL open the card creation dialog with the **Status pre-filled** to the column's `BoardStatus`.
2. A **"New Card"** button SHALL be added to the project board page header area (top-right of the board, above the columns). This button opens the card creation dialog with no pre-filled status (user must select one).
3. Both entry points open the **same** `CreateCardDialog` component.

### Card Creation Dialog

4. The dialog SHALL be a `MudDialog` with the title "New Card".
5. The dialog SHALL contain the following fields:

   | Field | Type | Required | Constraints |
   |-------|------|----------|-------------|
   | **Title** | Text input | Yes | 1–200 characters |
   | **Status** | Dropdown | Yes | Populated from project's `BoardStatusDto` list, ordered by `SortOrder`. Pre-filled when opened from a column header. |
   | **Priority** | Dropdown or segmented button | Yes | Values: Critical, High, Medium, Low. Default: Medium. |
   | **Description** | Multi-line text area | No | Max 5,000 characters. Supports Markdown (plain text input; no live preview in this dialog). |
   | **Tags** | Multi-select chip input | No | Populated from the project's `ProjectTagDto` list (SPEC-C1). Displays tag name and color. |

6. The **Status dropdown** SHALL display all project statuses ordered by `SortOrder`. If the dialog was opened from a column header, the corresponding status SHALL be pre-selected and the dropdown SHALL still be editable (user can change it).
7. The **Tags field** SHALL display a searchable chip input. The user types to filter the project's tag vocabulary and selects one or more tags. If the project has no tags, the field SHALL display a muted hint: "No tags defined. Add tags in Project Settings."
8. The dialog SHALL have two action buttons:
   - **"Create"** (primary): submits the form.
   - **"Cancel"** (secondary): closes the dialog without saving.
9. The "Create" button SHALL be disabled while the form is invalid or while the creation request is in progress.
10. While the creation request is in progress, the "Create" button SHALL show a loading spinner.

### Validation

11. The form SHALL validate fields inline (on blur and on submit attempt):
    - Title: required, max 200 characters.
    - Status: required.
    - Priority: required (always has a default, so effectively always valid).
    - Description: max 5,000 characters (no minimum).
    - Tags: no validation (optional, no limit enforced in UI).

### Submission

12. On submit, the system SHALL call `CreateCardCommand` with: Title, Description (nullable), Priority, ProjectId (from active project context), and the selected StatusId.
13. After successful creation, the system SHALL call `AddTagToCardCommand` for each selected tag (if any). These calls SHALL be made sequentially after the card is created.
14. On success:
    - The dialog SHALL close.
    - A success snackbar SHALL be displayed: "Card #{number} created."
    - The board SHALL refresh to display the new card in the appropriate column (via a callback or event to the parent board component).
15. On failure:
    - The dialog SHALL remain open.
    - A snackbar error SHALL be displayed with the server error message.
    - The form fields SHALL retain their current values.

## User Flow

### From Column Header
1. User clicks the "Add card" icon in the "Develop" column header.
2. `CreateCardDialog` opens with Status pre-set to "Develop".
3. User types a title: "Implement login endpoint".
4. User selects Priority: High.
5. User optionally adds a description and selects tags.
6. User clicks "Create".
7. System creates the card, assigns tags, closes the dialog.
8. Snackbar: "Card #43 created."
9. Board refreshes; new card appears in the "Develop" column.

### From Global "New Card" Button
1. User clicks "New Card" in the board header.
2. `CreateCardDialog` opens with Status empty.
3. User fills in all fields including Status selection.
4. User clicks "Create".
5. Same outcome as above.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| User submits with empty title | Inline validation error: "Title is required." Create button remains disabled. |
| Title exceeds 200 characters | Inline validation error: "Title cannot exceed 200 characters." |
| Description exceeds 5,000 characters | Inline validation error: "Description cannot exceed 5,000 characters." |
| User submits without selecting a status | Inline validation error: "Status is required." |
| Project has no statuses | Status dropdown is empty; form cannot be submitted; hint: "Configure board statuses in Settings." |
| Tag assignment fails after card creation | Card is created successfully; snackbar warning: "Card created, but some tags could not be assigned." Board refreshes with the card (without the failed tags). |
| Server error during card creation | Dialog stays open; snackbar error with message. |
| User presses Escape or clicks outside the dialog | Dialog closes; no card is created (standard MudDialog behavior). |

## Out of Scope

- Inline card creation directly in the column (no inline input row — always a dialog).
- Card duplication.
- Card templates.
- Assigning a card to a user at creation time.
- Setting a due date at creation time.
- Live Markdown preview in the description field.
- Creating new tags from within the creation dialog (user must go to Settings first).

## Acceptance Criteria

- [ ] "Add card" button in each column header opens `CreateCardDialog` with the column's status pre-filled.
- [ ] "New Card" button in the board header opens `CreateCardDialog` with no pre-filled status.
- [ ] Dialog contains: Title, Status dropdown, Priority selector, Description textarea, Tags multi-select.
- [ ] Status dropdown is populated from `GetProjectStatusesQuery` ordered by `SortOrder`.
- [ ] Priority defaults to "Medium".
- [ ] Tags multi-select is populated from `GetProjectTagsQuery`; shows hint if no tags exist.
- [ ] Inline validation fires on blur and on submit attempt for all required fields.
- [ ] "Create" button is disabled while form is invalid or request is in progress.
- [ ] Successful creation calls `CreateCardCommand` then `AddTagToCardCommand` for each selected tag.
- [ ] Success snackbar shows "Card #{number} created."
- [ ] Board refreshes after successful creation.
- [ ] Dialog stays open and shows error snackbar on failure.
- [ ] Tag assignment failure after card creation shows a warning snackbar (card is not rolled back).

## Dependencies

- **SPEC-WF2-board-card-entity** — `CreateCardCommand` must be implemented.
- **SPEC-C1-card-entity-enrichment** — `GetProjectTagsQuery` and `AddTagToCardCommand` must be implemented.
- **SPEC-WF1-board-status-management** — `GetProjectStatusesQuery` must be implemented.
- **SPEC-C4-board-wiring** — Board must be wired to real data to receive the refresh callback.

## Open Questions

1. **Tag creation shortcut**: Should the tags field allow creating a new tag inline (type a name not in the list → prompt to create it)? Currently out of scope — user must go to Settings. This could be a UX improvement in a future spec.
2. **Default status**: When opened from the "New Card" global button (no pre-fill), should the status default to the first status (sort order 0, typically "Backlog"), or remain empty forcing an explicit selection? Currently specified as empty (explicit selection required).
