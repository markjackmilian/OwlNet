# SPEC: Project Favorites in Selector

> **Status:** Todo
> **Created:** 2026-03-05
> **Author:** owl-planner + user
> **Priority:** Medium
> **Estimated Complexity:** S

## Context

SPEC-project-crud introduces the topbar project selector modal with a filterable list of projects. SPEC-project-dashboard adds the `IsFavorited` property to the Project entity and the toggle mechanism via the star icon on the project dashboard.

This spec completes the favorites experience by surfacing favorited projects at the top of the selector modal, giving users faster access to their most important projects.

## Actors

- **Authenticated User** — Any logged-in user.

## Functional Requirements

1. The `GetAllProjectsQuery` SHALL return projects ordered by: favorited projects first (alphabetically by name), then non-favorited projects (alphabetically by name).
2. In the project selector modal, favorited projects SHALL appear in a visually distinct **"Favorites" section** at the top of the list, separated from the rest by a section header label ("Favorites") and a subtle visual divider.
3. Non-favorited projects SHALL appear below under an "All Projects" section header.
4. Each project item in the modal list SHALL display a filled star icon if favorited, providing a visual cue consistent with the dashboard toggle.
5. If no projects are favorited, the "Favorites" section SHALL NOT be displayed — only the "All Projects" section is shown.
6. When filtering/searching projects in the modal, the favorites-first ordering SHALL be preserved within the filtered results. If no favorited projects match the search, the "Favorites" section header SHALL NOT appear in the results.
7. When a project's favorite status changes (via the dashboard toggle), the selector modal SHALL reflect the updated ordering the next time it is opened.

## User Flow

### Happy Path — Favorites Shown in Selector

1. User has previously favorited "Project Alpha" and "Project Beta" from their dashboards.
2. User clicks the topbar project indicator to open the selector modal.
3. Modal opens. At the top: "Favorites" header, then "Project Alpha" and "Project Beta" (both with filled star icons).
4. Below a divider: "All Projects" header, then remaining non-favorited projects listed alphabetically.
5. User clicks "Project Alpha" to select it.

### Happy Path — Search with Favorites

1. User opens the selector modal. Favorites section shows 3 projects.
2. User types "beta" in the search field.
3. Filtered results show: "Favorites" section with "Project Beta" (if it matches), then "All Projects" section with any other matches.

### Edge Case — No Favorites

1. User has no favorited projects.
2. User opens the selector modal.
3. No "Favorites" section is displayed. Only "All Projects" with all projects listed alphabetically.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| No projects are favorited | "Favorites" section is hidden, only "All Projects" is shown |
| All projects are favorited | "All Projects" section is empty or hidden, only "Favorites" section shown |
| Search matches only non-favorited projects | "Favorites" section hidden in results, only "All Projects" section shown |
| Search matches only favorited projects | Only "Favorites" section shown in results |
| Project is unfavorited while modal is closed | Next modal open reflects the change |

## Out of Scope

- Favorite toggle from within the selector modal (toggle only happens on the project dashboard).
- Per-user favorites (favorites are global for now).
- Drag-and-drop reordering of favorites.
- Limiting the number of favorites.

## Acceptance Criteria

- [ ] Selector modal displays a "Favorites" section at the top when favorited projects exist.
- [ ] Favorited projects show a filled star icon in the list.
- [ ] Non-favorited projects appear under "All Projects" section below.
- [ ] Both sections maintain alphabetical ordering by name.
- [ ] "Favorites" section is hidden when no projects are favorited.
- [ ] Search/filter preserves favorites-first ordering and conditionally shows/hides section headers.
- [ ] Favorite changes made on the dashboard are reflected in the selector on next open.

## Dependencies

- **SPEC-project-crud** — Selector modal, project list, search functionality.
- **SPEC-project-dashboard** — `IsFavorited` property on Project entity, `ToggleProjectFavoriteCommand`.

## Open Questions

None — this spec is fully defined by its dependencies.
