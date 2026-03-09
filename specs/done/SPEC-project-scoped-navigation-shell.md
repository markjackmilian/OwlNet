# SPEC: Project-Scoped Navigation Shell

> **Status:** Done
> **Created:** 2026-03-09
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

The current OwlNet navigation is a generic application menu (Home, Tasks, Users, APIs, Settings, etc.) that does not reflect the project-centric nature of the application. Since every meaningful action in OwlNet happens within the context of a selected project, the sidebar navigation must become project-scoped: it should only appear when a project is active and show only project-relevant navigation items (Dashboard, Board, Settings).

Global application settings, currently accessible via a sidebar link, must move to an icon button in the topbar to remain reachable regardless of project context. The OwlNet brand header currently in the sidebar must also relocate to the topbar.

This spec restructures the navigation shell (sidebar + topbar) to establish the project-centric navigation pattern that all future features will build upon.

## Actors

- **Authenticated User** — Any logged-in user with or without an active project selected.

## Functional Requirements

### Sidebar — Project-Scoped Navigation

1. The sidebar (MudDrawer) SHALL only be visible when an active project is selected.
2. When no project is selected, the sidebar SHALL be completely hidden (not rendered or collapsed to zero width).
3. When a project is active, the sidebar SHALL display exactly three navigation items, in this order:
   - **Dashboard** — icon: Dashboard, route: `/projects/{projectId}`
   - **Board** — icon: ViewKanban (or similar), route: `/projects/{projectId}/board`
   - **Settings** — icon: Settings, route: `/projects/{projectId}/settings`
4. The navigation items SHALL use the active project's ID from the `ActiveProjectService` to construct their routes dynamically.
5. The active navigation item SHALL be visually highlighted based on the current URL (using `NavLinkMatch`).
6. The sidebar SHALL continue to support mini/expanded mode (existing MudDrawer behavior) when visible.

### Sidebar — Removed Content

7. All current generic navigation items (Home, Tasks, Users, APIs, Subscription, Settings, Help & Support) SHALL be removed from the sidebar.
8. The "Account" section (Register/Login links for unauthenticated users) SHALL be removed from the sidebar. Authentication is handled by the auth gate (SPEC-enforce-auth-gate).
9. The drawer footer (user profile, dark/light toggle, logout button) SHALL be removed from the sidebar. These elements are not visible when no project is selected, and for simplicity they are removed entirely in this iteration.
10. The brand header ("OwlNet" logo/icon) SHALL be removed from the sidebar.

### Topbar — Additions

11. The OwlNet brand (owl icon + "OwlNet" text) SHALL be displayed on the left side of the topbar, before the project selector.
12. A settings icon button (gear icon) SHALL be added to the right side of the topbar, navigating to `/settings` (global application settings page) on click.
13. The settings icon button SHALL be positioned in the right-side group of the topbar, before the existing OpenCode status indicator.

### Topbar — Hamburger Menu

14. The hamburger menu icon button SHALL only be visible when an active project is selected (since the sidebar is hidden otherwise).
15. When no project is selected, the hamburger button SHALL not be rendered.

### Topbar — Layout Order

16. The topbar left side SHALL display, in order: hamburger toggle (if project active), OwlNet brand, project selector button.
17. The topbar right side SHALL display, in order: settings icon button, OpenCode status indicator, dark mode toggle, notifications bell, user avatar.

### Placeholder Pages

18. The system SHALL provide a Board page at route `/projects/{projectId:guid}/board` that displays a placeholder message (e.g., "Board — Coming soon") with the project name.
19. The system SHALL provide a Project Settings page at route `/projects/{projectId:guid}/settings` that displays a placeholder message (e.g., "Project Settings — Coming soon") with the project name.
20. Both placeholder pages SHALL validate that the `projectId` corresponds to an existing, non-archived project. If not, they SHALL display a "Project not found" message consistent with the project dashboard behavior (SPEC-002).
21. Both placeholder pages SHALL set the project as the active project in the `ActiveProjectService` if navigated to directly (consistent with SPEC-002 dashboard behavior).

## User Flow

### Happy Path — User with Active Project

1. User logs in. No project is selected.
2. Topbar shows: OwlNet brand, "Select a project" button, settings gear, OpenCode status, dark mode, notifications, avatar. No hamburger. No sidebar visible.
3. User clicks "Select a project", picks "Project Alpha".
4. Sidebar appears with three items: Dashboard, Board, Settings. Hamburger button appears in topbar.
5. User clicks "Dashboard" — navigates to `/projects/{id}`, Dashboard item is highlighted.
6. User clicks "Board" — navigates to `/projects/{id}/board`, Board item is highlighted. Page shows "Board — Coming soon".
7. User clicks "Settings" in sidebar — navigates to `/projects/{id}/settings`, Settings item is highlighted. Page shows "Project Settings — Coming soon".
8. User clicks gear icon in topbar — navigates to `/settings` (global app settings).

### Happy Path — Switch Project

1. User has "Project Alpha" active, sidebar is visible.
2. User clicks project selector in topbar, selects "Project Beta".
3. Sidebar items update their routes to use Project Beta's ID.
4. User is navigated to `/projects/{betaId}` (Project Beta dashboard).

### Happy Path — No Project Selected

1. User logs in or active project is cleared (e.g., project archived).
2. Sidebar is hidden. Hamburger is hidden.
3. Topbar shows brand, project selector placeholder, and right-side icons.
4. User can still click gear icon to access global settings.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| Active project is archived while user is on a project page | Sidebar hides, hamburger hides, user sees "Project not found" or is redirected to home |
| User navigates directly to `/projects/{id}/board` with valid project | Project is set as active, sidebar appears with Board highlighted |
| User navigates directly to `/projects/{id}/board` with invalid project | "Project not found" message displayed, sidebar remains hidden |
| User navigates to `/settings` (global) with no active project | Page loads normally, sidebar remains hidden (global settings are project-independent) |
| User resizes browser window | Sidebar responsive behavior (mini/expanded) works as before when visible |
| User navigates to `/projects/{id}/settings` then clicks gear icon in topbar | Navigates from project settings to global settings — different pages, no conflict |

## Out of Scope

- Board page functionality (kanban, cards, columns) — future spec.
- Project Settings page functionality (project configuration options) — future spec.
- Relocation of drawer footer elements (user profile, dark/light toggle) to topbar or elsewhere — may be addressed in a future spec.
- Home page or global landing page redesign.
- Role-based visibility of navigation items.
- Breadcrumb navigation.

## Acceptance Criteria

- [ ] Sidebar is completely hidden when no project is selected.
- [ ] Sidebar shows exactly three items (Dashboard, Board, Settings) when a project is active.
- [ ] Sidebar navigation routes are dynamically constructed using the active project's ID.
- [ ] Active navigation item is visually highlighted based on current URL.
- [ ] All previous generic navigation items are removed from the sidebar.
- [ ] Brand header and drawer footer are removed from the sidebar.
- [ ] OwlNet brand (icon + text) appears on the left side of the topbar.
- [ ] Settings gear icon button in topbar navigates to `/settings`.
- [ ] Hamburger menu button is hidden when no project is selected.
- [ ] Hamburger menu button is visible and functional when a project is active.
- [ ] Board placeholder page exists at `/projects/{projectId}/board` and displays "Coming soon" message.
- [ ] Project Settings placeholder page exists at `/projects/{projectId}/settings` and displays "Coming soon" message.
- [ ] Both placeholder pages validate the project ID and show "Project not found" for invalid/archived projects.
- [ ] Both placeholder pages set the active project when navigated to directly.
- [ ] Navigating between projects updates sidebar routes correctly.
- [ ] Global settings page (`/settings`) remains fully functional and accessible via topbar icon.

## Dependencies

- **SPEC-001-project-crud** — Active project context, `ActiveProjectService`, project selector modal.
- **SPEC-002-project-dashboard** — Project dashboard page at `/projects/{projectId}` (already exists).
- **SPEC-settings-page** — Global settings page at `/settings` (already exists).

## Open Questions

1. Should the sidebar display the active project name as a header/title above the three navigation items, or is the topbar project indicator sufficient?
2. Should the dark mode toggle (currently in drawer footer) be preserved in the topbar, or is the existing topbar dark mode icon button sufficient? (Currently both exist — one in topbar, one in drawer footer.)
