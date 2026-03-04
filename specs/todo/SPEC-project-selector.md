# SPEC: Project Selector

> **Status:** Todo
> **Created:** 2026-03-04
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

OwlNet is a project-centric application: all vertical features operate within the context of an active project. Users need a fast, accessible way to switch between projects and create new ones without leaving their current workflow. The project selector is a core navigation component embedded in the topbar (defined in SPEC-app-shell) that manages the active project context.

## Actors

- **Authenticated user**: any user who has logged into the application.

## Functional Requirements

### Project Selector Chip (Topbar)

1. When a project is active, the selector SHALL display the project name in a clickable chip in the topbar.
2. When no project exists (first access or all projects deleted), the selector SHALL display an "Add Project" button instead of the chip.
3. Clicking the chip SHALL open the project selection modal.
4. Clicking the "Add Project" button SHALL open the project selection modal with the creation form immediately visible.

### Project Selection Modal

5. The modal SHALL display a list of all projects belonging to the user.
6. The modal SHALL include a text filter field to search projects by name.
7. The filter SHALL update the list in real-time as the user types.
8. The modal SHALL include a "+" button (or equivalent) to initiate quick project creation.
9. Clicking a project in the list SHALL set it as the active project, close the modal, and update the topbar chip.
10. The modal SHALL be dismissible without making a selection (click outside, close button, Escape key).
11. When dismissed without selection, the previously active project SHALL remain unchanged.
12. If the project list is empty, the modal SHALL display a message such as "No projects found" with the "+" button prominently visible.
13. If the filter produces no results, the modal SHALL display a message such as "No projects match your search".

### Quick Project Creation

14. Quick project creation SHALL require only the project name.
15. The project name SHALL be mandatory; submitting an empty name SHALL display an inline validation error.
16. Upon successful creation, the new project SHALL be immediately set as the active project.
17. Upon successful creation, the modal SHALL close and the topbar SHALL update with the new project name.

### Active Project Persistence

18. The active project SHALL be persisted per user across sessions.
19. On login or application load, the system SHALL automatically select the last project the user worked on.
20. If the last worked-on project is no longer available (deleted, removed), the system SHALL treat it as "no project selected".

### Landing Page (No Project)

21. If the user has no projects at all, the system SHALL display a landing page in the main content area with a call-to-action to create the first project.
22. The landing page SHALL replace the main content area; the topbar SHALL remain visible.
23. After the user creates a project from the landing page, the system SHALL set it as active and navigate to the main content area.

## User Flow

### First access (no projects)

1. User logs into the application.
2. The system detects the user has no projects.
3. The topbar shows the "Add Project" button instead of a chip.
4. The main content area shows the landing page with a CTA to create the first project.
5. User clicks the CTA (or the "Add Project" button in the topbar).
6. The system prompts for a project name.
7. User enters the name and confirms.
8. The system creates the project, sets it as active, updates the topbar chip, and shows the main content area.

### Returning access (project exists)

1. User logs into the application.
2. The system loads the last worked-on project automatically.
3. The topbar displays the chip with the project name.
4. The main content area shows the active project's features.

### Switch project

1. User clicks the project chip in the topbar.
2. The modal opens with the project list and filter field.
3. User optionally types in the filter to narrow results.
4. User clicks the desired project.
5. The modal closes, the topbar updates with the new project name, the content area refreshes for the new project context.

### Quick create project

1. User clicks the project chip in the topbar.
2. The modal opens.
3. User clicks the "+" button.
4. A name input field appears (inline in the modal or as a sub-view).
5. User enters the project name and confirms.
6. The system creates the project, sets it as active, closes the modal, and updates the topbar.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| First access, no projects exist | Show landing page with CTA; topbar shows "Add Project" button |
| Last worked-on project was deleted | Treat as "no project selected"; show "Add Project" button and landing page |
| Quick creation with empty name | Inline validation error: name is required |
| Quick creation with duplicate name | See Open Questions |
| Modal dismissed without selection | No change; previously active project remains active |
| Project list is empty in modal | Show "No projects found" message with "+" button |
| Filter produces no results | Show "No projects match your search" message |
| Network error during project creation | Display error message in the modal; do not close the modal |
| Network error during project switch | Display error message; keep the previous project active |

## Out of Scope

- Advanced project management (edit, delete, project details beyond name) — will be defined in dedicated specs.
- Project-specific vertical features (what happens inside a project) — will be defined in dedicated specs.
- Project sharing or collaboration features.
- Project categories, tags, or grouping.
- Topbar layout and admin section — defined in SPEC-app-shell.

## Acceptance Criteria

- [ ] When a project is active, the topbar displays a chip with the project name.
- [ ] Clicking the chip opens the project selection modal.
- [ ] The modal displays all user projects in a filterable list.
- [ ] Typing in the filter field narrows the project list in real-time.
- [ ] Clicking a project in the list sets it as active, closes the modal, and updates the topbar.
- [ ] The "+" button in the modal allows creating a new project with only a name.
- [ ] Quick-created projects become immediately active.
- [ ] The active project is persisted across sessions (last worked-on project is restored on login).
- [ ] When no projects exist, a landing page with CTA is displayed.
- [ ] When no projects exist, the topbar shows an "Add Project" button instead of the chip.
- [ ] The modal can be dismissed without making changes.
- [ ] Validation prevents creating a project with an empty name.

## Dependencies

- SPEC-app-shell for the topbar slot where the project selector is rendered.
- Authentication system (user must be already authenticated).
- Project entity with at least a "Name" field available at the domain/persistence level.

## Open Questions

1. **Duplicate project names:** is it allowed to create two projects with the same name? If not, what error message should be displayed?
2. **Maximum project name length:** is there a limit? (e.g., 100 characters)
3. **Project list ordering:** how should projects be sorted in the modal? Alphabetically, by last accessed, by creation date?
4. **Maximum number of projects per user:** is there a limit?
