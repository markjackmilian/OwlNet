# SPEC: Project Dashboard

> **Status:** Todo
> **Created:** 2026-03-05
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

When a user selects a project from the topbar selector (see SPEC-001-project-crud), they need a landing page that contextualizes the application to that project. This dashboard serves as the project's home — a visual summary of project health and activity.

Since the underlying entities (cards, issues, sprints, etc.) do not exist yet, this dashboard will use **static mock data** to establish the layout, component structure, and visual patterns. This allows the team to validate the UX early and incrementally replace mock data with real data as features are built.

The dashboard also hosts the **favorite toggle** for the project, which is needed by SPEC-003-project-favorites.

## Actors

- **Authenticated User** — Any logged-in user with an active project selected.

## Functional Requirements

### Navigation & Routing

1. The dashboard SHALL be accessible at the route `/projects/{projectId:guid}`.
2. When a user selects a project from the topbar selector, the application SHALL navigate to `/projects/{projectId}`.
3. If the `projectId` in the URL does not match an existing, non-archived project, the system SHALL display a "Project not found" message with a call-to-action to select a different project.
4. If the user navigates to this route directly (e.g., bookmark, shared link), the system SHALL set the project as the active project in the topbar context (syncing with sessionStorage).

### Page Header

5. The dashboard SHALL display a page header section containing:
   - The project **name** as a prominent title.
   - The project **description** below the name (or "No description" in muted text if empty).
   - A **favorite toggle** icon (star) — filled when favorited, outlined when not.
   - The project **creation date** in a human-readable format.
6. Clicking the favorite toggle SHALL immediately persist the favorite state (toggle `IsFavorited` on the Project entity) and provide visual feedback (icon change + snackbar).

### Dashboard Widgets — Summary Cards (Top Row)

7. The dashboard SHALL display a row of **summary statistic cards** showing at-a-glance project metrics. Each card SHALL have an icon, a label, a numeric value, and a subtle trend indicator (up/down/neutral). The cards are:
   - **Open Issues** — e.g., 12, trend: +3 this week
   - **Cards To Do** — e.g., 24, trend: neutral
   - **Cards Done** — e.g., 87, trend: +5 this week
   - **Completion Rate** — e.g., 78%, trend: +2%

8. All values in summary cards SHALL be static mock data, hardcoded in the component.

### Dashboard Widgets — Charts (Middle Row)

9. The dashboard SHALL display a **Development Trend** chart showing a line/area chart of activity over the last 30 days (mock data). The chart SHALL use MudBlazor's charting components (`MudChart`).
10. The dashboard SHALL display a **Card Distribution** chart showing a donut/pie chart of card statuses (e.g., To Do, In Progress, Done, Blocked) with mock data.

### Dashboard Widgets — Lists (Bottom Row)

11. The dashboard SHALL display a **Recent Issues** widget showing a list of the 5 most recent mock issues. Each item SHALL display: issue title, status chip (Open/Closed), priority indicator (color-coded), and a relative timestamp (e.g., "2 hours ago").
12. The dashboard SHALL display a **Recent Activity** widget showing a timeline of the 5 most recent mock activity entries. Each entry SHALL display: an action description (e.g., "Card 'Setup CI' moved to Done"), an actor name, and a relative timestamp.

### Layout & Responsiveness

13. The dashboard layout SHALL use a responsive grid system (`MudGrid`):
    - **Summary cards**: 4 columns on desktop (md+), 2 columns on tablet (sm), 1 column on mobile (xs).
    - **Charts**: 2 columns on desktop, 1 column on tablet and mobile.
    - **Lists**: 2 columns on desktop, 1 column on tablet and mobile.
14. All widgets SHALL be encapsulated as individual Blazor components for maintainability and future replacement of mock data with real data.
15. The dashboard SHALL show a loading skeleton while the project data is being fetched on initial load.

### Favorite Toggle (Backend)

16. The Project entity SHALL be extended with an `IsFavorited` boolean property, defaulting to `false`.
17. The system SHALL provide a `ToggleProjectFavoriteCommand` (DispatchR) that toggles the `IsFavorited` flag on a project.
18. A new EF Core migration SHALL be generated for both providers to add the `IsFavorited` column.

## User Flow

### Happy Path — Land on Dashboard

1. User clicks a project in the topbar selector modal.
2. Modal closes. Topbar updates to show the project name.
3. Browser navigates to `/projects/{projectId}`.
4. Dashboard loads: skeleton shimmer appears briefly.
5. Header shows project name, description, star icon (unfilled), creation date.
6. Summary cards, charts, and lists render with mock data.

### Happy Path — Toggle Favorite

1. User is on the project dashboard.
2. User clicks the star icon in the header.
3. Star fills immediately (optimistic UI).
4. Snackbar shows "Added to favorites".
5. If the user clicks again, star unfills. Snackbar shows "Removed from favorites".

### Fallback — Invalid Project URL

1. User navigates to `/projects/{invalidId}`.
2. Page shows "Project not found" with a button "Select a project".
3. Clicking the button opens the topbar project selector modal.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| URL contains a valid GUID but project does not exist | "Project not found" message with CTA to select another project |
| URL contains a valid GUID but project is archived | "Project not found" message (archived projects are treated as not found) |
| URL contains an invalid GUID format | Blazor routing returns 404 / NotFound page |
| Favorite toggle fails (network error) | Revert star icon to previous state, show error snackbar "Could not update favorite. Please try again." |
| User navigates to dashboard with no project selected (direct URL) | Project is loaded by ID from URL, set as active in context |
| Browser window resized | Dashboard grid reflows responsively without layout breakage |

## Out of Scope

- Real data for any widget (all data is static mock).
- Project settings/edit page (future spec when more properties are defined).
- Interactive widgets (clicking an issue, filtering cards, etc.).
- Widget customization or reordering by the user.
- Per-user favorites (favorites are global for now).
- Dashboard for different project types or vertical-specific widgets.

## Acceptance Criteria

- [ ] Route `/projects/{projectId}` renders the project dashboard.
- [ ] Selecting a project from the topbar selector navigates to the dashboard.
- [ ] Page header displays project name, description, creation date, and favorite star.
- [ ] Favorite toggle persists to database via `ToggleProjectFavoriteCommand` and provides immediate visual feedback.
- [ ] `IsFavorited` property added to Project entity with EF Core migration for both providers.
- [ ] Four summary statistic cards render with mock data and trend indicators.
- [ ] Development Trend chart renders with mock 30-day data.
- [ ] Card Distribution donut/pie chart renders with mock status data.
- [ ] Recent Issues list shows 5 mock items with title, status chip, priority, and timestamp.
- [ ] Recent Activity timeline shows 5 mock entries with description, actor, and timestamp.
- [ ] Each widget is an isolated Blazor component (not inline in the page).
- [ ] Layout is responsive across desktop, tablet, and mobile breakpoints.
- [ ] Loading skeleton displays while project data is fetched.
- [ ] Invalid or archived project ID in URL shows "Project not found" with CTA.
- [ ] Direct navigation to a valid project URL sets it as active in topbar context.
- [ ] Unit tests cover `ToggleProjectFavoriteCommand` handler (happy path + project not found).

## Dependencies

- **SPEC-001-project-crud** — Project entity, CQRS infrastructure, topbar selector, and active project context must exist.
- MudBlazor charting components (`MudChart`).
- DispatchR (established by SPEC-001-project-crud).

## Open Questions

1. Should the dashboard have a "quick actions" section (e.g., "Create card", "Open issue") even as disabled/placeholder buttons, to hint at future functionality?
2. When real data replaces mock data in the future, should the mock components be preserved as fallback/demo mode, or removed entirely?
