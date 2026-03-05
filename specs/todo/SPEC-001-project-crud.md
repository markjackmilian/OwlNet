# SPEC: Project CRUD

> **Status:** Todo
> **Created:** 2026-03-05
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

The Project is the central entity in OwlNet. The entire application revolves around working within the context of a selected project. Before any project-specific features can be built (dashboard, configuration, vertical features), the system needs the ability to create, read, update, and archive projects.

This spec covers the full stack: domain entity, application layer (CQRS handlers via DispatchR), infrastructure persistence, and the Blazor UI for managing projects through a topbar selector with an integrated CRUD modal.

Note: this is the first feature to use DispatchR CQRS handlers (commands/queries), establishing the pattern for all future features.

## Actors

- **Authenticated User** — Any logged-in user. All users have equal permissions (no role-based access control for now).

## Functional Requirements

### Entity & Validation

1. The system SHALL persist a Project entity with the following properties: `Id` (Guid), `Name` (string), `Description` (string, optional), `IsArchived` (bool), `CreatedAt` (DateTimeOffset), `UpdatedAt` (DateTimeOffset).
2. The `Name` SHALL be required, between 3 and 100 characters, and globally unique (case-insensitive).
3. The `Description` SHALL be optional plain text, maximum 500 characters.
4. The `IsArchived` property SHALL default to `false` on creation.

### Commands & Queries (DispatchR CQRS)

5. The system SHALL provide a `CreateProjectCommand` that creates a new project and returns the created project's ID.
6. The system SHALL provide an `UpdateProjectCommand` that updates the name and/or description of an existing, non-archived project.
7. The system SHALL provide an `ArchiveProjectCommand` that sets `IsArchived = true` on an existing project (soft delete).
8. The system SHALL provide a `RestoreProjectCommand` that sets `IsArchived = false` on an archived project.
9. The system SHALL provide a `GetAllProjectsQuery` that returns all non-archived projects, ordered by name ascending.
10. The system SHALL provide a `GetProjectByIdQuery` that returns a single project by its ID, regardless of archived status.
11. All commands SHALL validate input using FluentValidation. Validation failures SHALL return a `Result.Failure` with a descriptive error message.
12. Attempting to create or rename a project with a name that already exists (case-insensitive) SHALL return a `Result.Failure` with message "A project with this name already exists."
13. Attempting to update or archive a project that does not exist SHALL return a `Result.Failure` with message "Project not found."

### UI — Topbar Project Indicator

14. The `MudAppBar` SHALL display the name of the currently active project. If no project is selected, it SHALL display "Select a project" as placeholder text.
15. Clicking the project name/placeholder in the topbar SHALL open the Project Selector Modal.

### UI — Project Selector Modal

16. The modal SHALL display a searchable list of all non-archived projects, loaded on open.
17. The search field SHALL filter projects client-side by name (case-insensitive, contains match) with immediate feedback as the user types.
18. Each project item in the list SHALL display the project name and description (truncated if needed).
19. Clicking a project in the list SHALL select it as the active project, close the modal, and update the topbar indicator.
20. The modal SHALL include a "New Project" button that switches the modal view to a creation form.
21. Each project item SHALL have an actions menu (three-dot icon or similar) with options: "Edit" and "Archive".
22. Selecting "Edit" SHALL switch the modal view to an edit form pre-filled with the project's current data.
23. Selecting "Archive" SHALL show a confirmation dialog ("Are you sure you want to archive '{ProjectName}'? You can restore it later."). On confirm, the project is archived and removed from the list.
24. The creation and edit forms SHALL contain fields for Name (required) and Description (optional), with a "Save" and "Cancel" button.
25. The "Save" button SHALL be disabled while the form is invalid or a save operation is in progress.
26. Validation errors from the backend (e.g., duplicate name) SHALL be displayed inline in the form.
27. Successful creation, update, or archival SHALL show a success snackbar notification (`ISnackbar`).

### Active Project Context

28. The currently active project ID SHALL be persisted in the browser's `sessionStorage` so it survives page refreshes within the same browser tab.
29. On application load, if a previously selected project ID is found in `sessionStorage`, the system SHALL verify the project still exists and is not archived. If valid, it SHALL restore it as the active project. If invalid, it SHALL clear the stored ID and show the "Select a project" placeholder.
30. When the active project is archived (by the current user), the system SHALL clear the active project context and show the "Select a project" placeholder.

## User Flow

### Happy Path — Select Existing Project

1. User logs in and lands on the dashboard.
2. Topbar shows "Select a project" (no project active yet).
3. User clicks the topbar project indicator.
4. Modal opens showing a list of all projects.
5. User clicks on "Project Alpha".
6. Modal closes. Topbar now shows "Project Alpha".
7. The app is now contextualized to Project Alpha.

### Happy Path — Create New Project

1. User clicks the topbar project indicator.
2. Modal opens. User clicks "New Project".
3. Form appears with Name and Description fields.
4. User enters "Project Beta" and a description, clicks "Save".
5. Snackbar shows "Project created successfully".
6. Modal returns to the list view. "Project Beta" appears in the list.
7. User can click it to select it, or close the modal.

### Happy Path — Edit Project

1. User opens the project selector modal.
2. User clicks the actions menu on "Project Alpha", selects "Edit".
3. Form appears pre-filled with current name and description.
4. User changes the description, clicks "Save".
5. Snackbar shows "Project updated successfully".
6. Modal returns to list view with updated data.

### Happy Path — Archive Project

1. User opens the project selector modal.
2. User clicks the actions menu on "Project Alpha", selects "Archive".
3. Confirmation dialog appears: "Are you sure you want to archive 'Project Alpha'? You can restore it later."
4. User confirms. Snackbar shows "Project archived".
5. "Project Alpha" disappears from the list.
6. If "Project Alpha" was the active project, topbar reverts to "Select a project".

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| User creates a project with a name that already exists | Form shows inline error: "A project with this name already exists." |
| User submits form with name shorter than 3 characters | Form shows inline validation error on the Name field |
| User submits form with name longer than 100 characters | Form shows inline validation error on the Name field |
| User submits form with description longer than 500 characters | Form shows inline validation error on the Description field |
| Active project is archived by current user | Active context cleared, topbar shows placeholder |
| Session storage contains ID of a deleted/archived project | On load, context is cleared, placeholder shown |
| Network error during save operation | Snackbar shows error: "An error occurred. Please try again." Form remains open with data preserved |
| User opens modal with no projects in the system | List area shows empty state message: "No projects yet. Create your first project!" with prominent "New Project" button |
| User searches with no matching results | List shows "No projects match your search." |

## Out of Scope

- Project dashboard page (see SPEC-002-project-dashboard).
- Project favorites / pinning (see SPEC-003-project-favorites).
- Role-based permissions on projects.
- Project members or team assignment.
- Additional project properties beyond Name and Description.
- Archived projects management UI (listing/restoring archived projects — future spec).
- Multi-tab synchronization of active project context.

## Acceptance Criteria

- [ ] Project entity exists in Domain with factory method, validation, and archive/restore methods.
- [ ] EF Core configuration exists for both SQLite and SQL Server providers with migrations generated.
- [ ] All CQRS handlers (Create, Update, Archive, Restore, GetAll, GetById) are implemented with DispatchR and return `Result`/`Result<T>`.
- [ ] FluentValidation validators exist for Create and Update commands.
- [ ] Topbar displays active project name or "Select a project" placeholder.
- [ ] Clicking topbar indicator opens the project selector modal.
- [ ] Modal displays filterable list of non-archived projects.
- [ ] User can create a new project from the modal.
- [ ] User can edit an existing project from the modal.
- [ ] User can archive a project with confirmation from the modal.
- [ ] Active project ID is persisted in sessionStorage and restored on page load.
- [ ] Validation errors (client and server) are displayed inline in forms.
- [ ] Success/error notifications use ISnackbar.
- [ ] All handlers and entity methods have unit tests covering happy path, validation failures, and edge cases.
- [ ] UI shows appropriate loading states during async operations.
- [ ] Empty state is handled when no projects exist.

## Dependencies

- DispatchR mediator (already wired in `Program.cs`, but this will be the first feature to use CQRS handlers — establishes the pattern).
- ASP.NET Core Identity (user must be authenticated).
- EF Core dual-provider setup (already in place).
- MudBlazor (already in place).

## Open Questions

1. Should the "New Project" action in the modal automatically select the newly created project as active, or should the user explicitly click it to select?
2. Should there be a way to access archived projects from this modal (e.g., a toggle "Show archived") or should that be deferred to a future admin/management spec?
