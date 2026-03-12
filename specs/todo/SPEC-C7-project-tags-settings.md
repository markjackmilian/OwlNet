# SPEC: Project Tag Vocabulary Settings

> **Status:** Todo
> **Created:** 2026-03-12
> **Author:** owl-planner + user
> **Priority:** Medium
> **Estimated Complexity:** M

## Context

Cards can be tagged with labels drawn from a project-scoped vocabulary (SPEC-C1). Before users can assign tags to cards, the vocabulary must be populated. This spec adds a "Tags" section to the project settings page (already introduced by `SPEC-project-board-statuses-settings` at `/projects/{projectId}/settings`), allowing users to manage the tag vocabulary: add, rename, change color, and delete tags.

The backend CQRS commands and queries for `ProjectTag` are already defined in SPEC-C1. This spec covers only the UI layer.

## Actors

| Actor | Description |
|-------|-------------|
| **User** | Authenticated user managing the tag vocabulary for a project |

## Functional Requirements

### Settings Page Integration

1. The existing `/projects/{projectId}/settings` page SHALL gain a new **"Tags"** section, displayed below the existing "Board Statuses" section.
2. The "Tags" section SHALL have a heading "Tags" and a brief description: "Define the tag vocabulary for this project. Tags can be assigned to cards to categorize and filter work."

### Tag List Display

3. The Tags section SHALL display all tags currently defined for the project, retrieved via `GetProjectTagsQuery`, ordered by name ascending.
4. Each tag entry SHALL display:
   - A **color swatch** (a small colored circle using `ProjectTagDto.Color`, or a neutral gray if no color is set).
   - The **tag name**.
   - An **edit icon button** to rename the tag or change its color.
   - A **delete icon button** to remove the tag.
5. If no tags exist, the section SHALL display a muted empty state: "No tags defined yet. Add your first tag below."

### Add Tag

6. Below the tag list, an inline **add form** SHALL allow the user to create a new tag:
   - A text input for the tag name (required, 1–50 characters).
   - A color picker input for the tag color (optional). The color picker SHALL allow entering a hex code or selecting from a small preset palette.
   - An "Add" button that submits the form.
7. The "Add" button SHALL be disabled when the name input is empty.
8. On successful creation (via `CreateProjectTagCommand`), the new tag SHALL appear in the list immediately and the form SHALL reset.
9. If the name already exists in the project (case-insensitive), an inline validation error SHALL be displayed: "A tag with this name already exists."

### Edit Tag (Rename / Change Color)

10. Clicking the edit icon on a tag SHALL open an inline edit form (or a small popover/dialog) pre-populated with the tag's current name and color.
11. The user can modify the name and/or color and confirm.
12. On confirmation, the system SHALL call `UpdateProjectTagCommand`.
13. On success, the tag list updates immediately.
14. On failure, the edit form remains open with a snackbar error.
15. Uniqueness validation applies on rename: if the new name conflicts with another tag in the project, an inline error SHALL be shown.

### Delete Tag

16. Clicking the delete icon on a tag SHALL show a confirmation dialog: "Delete tag '{name}'? It will be removed from all cards that use it. This action cannot be undone."
17. On confirmation, the system SHALL call `DeleteProjectTagCommand`.
18. On success, the tag is removed from the list immediately.
19. On failure, a snackbar error is shown.
20. There is **no guard** on deletion based on card usage — deletion is always allowed and silently removes the tag from all cards (cascade delete, per SPEC-C1 requirement 5).

### Loading and Feedback

21. The Tags section SHALL display a loading skeleton while `GetProjectTagsQuery` is in flight.
22. All mutating operations (add, edit, delete) SHALL show loading feedback on the relevant button (spinner, disabled state).
23. Success outcomes for add and delete are reflected immediately in the list (no explicit success snackbar needed — the UI change is sufficient feedback). Edit success shows no snackbar.
24. Failures SHALL always show a snackbar error message.

## User Flow

### Add a Tag
1. User navigates to `/projects/{projectId}/settings`.
2. User scrolls to the "Tags" section.
3. User types "backend" in the name input, picks a blue color.
4. User clicks "Add".
5. Tag "backend" (blue) appears in the list. Form resets.

### Rename a Tag
1. User clicks the edit icon on the "backend" tag.
2. Inline edit form appears with current name and color.
3. User changes name to "Backend Services".
4. User confirms.
5. Tag updates in the list.

### Delete a Tag
1. User clicks the delete icon on the "backend" tag.
2. Confirmation dialog: "Delete tag 'backend'? It will be removed from all cards that use it."
3. User confirms.
4. Tag disappears from the list. All cards that had this tag silently lose it.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| User adds a tag with a name already in the project (case-insensitive) | Inline validation error: "A tag with this name already exists." |
| User adds a tag with an empty name | Add button disabled; no submission. |
| User adds a tag with a name exceeding 50 characters | Inline validation error: "Tag name cannot exceed 50 characters." |
| User enters an invalid hex color | Inline validation error: "Please enter a valid hex color (e.g., #FF5733)." |
| User renames a tag to a name that conflicts with another tag | Inline validation error: "A tag with this name already exists." |
| Delete command fails (server error) | Snackbar error; tag remains in the list. |
| Project has a very large number of tags (50+) | List scrolls within the section; no pagination needed at this scale. |

## Out of Scope

- Global/system-wide tags shared across projects.
- Tag ordering or drag-and-drop reordering (tags are always sorted alphabetically).
- Tag usage statistics (how many cards use each tag).
- Bulk tag operations (delete all, merge tags).
- Tag import/export.
- Tag filtering or search within the settings page (not needed at typical tag counts).

## Acceptance Criteria

- [ ] "Tags" section appears on `/projects/{projectId}/settings` below "Board Statuses".
- [ ] Tag list displays all project tags ordered by name, with color swatch, name, edit, and delete controls.
- [ ] Empty state is shown when no tags exist.
- [ ] Add form allows creating a tag with name and optional color.
- [ ] Add button is disabled when name is empty.
- [ ] Duplicate name validation works on add (case-insensitive).
- [ ] New tag appears in the list immediately after creation; form resets.
- [ ] Edit form pre-populates with current name and color.
- [ ] Rename validates uniqueness; duplicate name shows inline error.
- [ ] Edit success updates the tag in the list immediately.
- [ ] Delete shows confirmation dialog with tag name.
- [ ] Delete removes the tag from the list immediately on success.
- [ ] All operations show loading feedback on the relevant button.
- [ ] Failures show snackbar error messages.
- [ ] Loading skeleton is shown while the tag list loads.

## Dependencies

- **SPEC-C1-card-entity-enrichment** — `CreateProjectTagCommand`, `UpdateProjectTagCommand`, `DeleteProjectTagCommand`, `GetProjectTagsQuery` must be implemented.
- **SPEC-project-board-statuses-settings** — The `/projects/{projectId}/settings` page must exist to add the Tags section to it.

## Open Questions

1. **Color picker UX**: Should the color picker be a full `<input type="color">` HTML element, a hex text input, or a small preset palette of ~12 colors? A preset palette is simpler and avoids browser inconsistencies. Implementation decision for the developer.
2. **No-color default**: When a tag has no color set, should the swatch show a neutral gray, or should the system assign a random color from the palette on creation? Currently specified as neutral gray (no auto-assignment).
