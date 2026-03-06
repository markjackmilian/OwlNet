# SPEC: Add Path Property to Project Entity

> **Status:** Todo
> **Created:** 2026-03-06
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

The Project entity currently has no reference to the filesystem location where the project physically resides. Adding a mandatory `Path` property allows OwlNet to know where each project's folder is located on the local machine. This is foundational for any future feature that needs to interact with the project's files (e.g., code analysis, file browsing, git integration).

Since OwlNet runs as a Blazor Server app on the developer's local machine, the server filesystem corresponds to the user's filesystem.

The application is currently in early development, so no data migration strategy is needed — existing data can be wiped.

## Actors

- **Developer (User):** Creates and manages projects through the UI.

## Functional Requirements

1. The `Project` entity SHALL have a `Path` property of type `string`, representing the absolute filesystem path of the project folder.
2. The `Path` property SHALL be required (non-null, non-empty, non-whitespace).
3. The `Path` property SHALL have a maximum length of 500 characters.
4. The `Path` property SHALL be immutable after creation — it cannot be modified via the Update flow.
5. The `Path` value SHALL be unique across all projects (including archived ones), enforced at both the database level (unique index) and the application level (duplicate check before creation).
6. The `Path` value SHALL be trimmed of leading and trailing whitespace before persistence.
7. The `Project.Create()` factory method SHALL accept a `path` parameter and validate it (non-empty, max length).
8. The `CreateProjectCommand` SHALL include a required `Path` property.
9. The `CreateProjectCommandHandler` SHALL verify that no other project already uses the same path before creating the project.
10. The `CreateProjectCommandHandler` SHALL verify that the directory exists on the filesystem before creating the project. If the directory does not exist, it SHALL return a failure result with a descriptive error message.
11. The `CreateProjectCommandValidator` SHALL validate that `Path` is not empty and does not exceed 500 characters.
12. The `ProjectDto` SHALL include the `Path` property.
13. The `ProjectConfiguration` (EF Core) SHALL configure `Path` as required, with max length 500 and a unique index.
14. EF Core migrations SHALL be generated for both SQLite and SQL Server providers.
15. The `IProjectRepository` SHALL expose a method to check for duplicate paths (e.g., `ExistsWithPathAsync`), following the same pattern as the existing `ExistsWithNameAsync`.

## User Flow

1. User opens the Project Selector Modal and clicks "New Project."
2. User fills in the **Name** field (existing behavior).
3. User fills in the **Path** field — a text input where the user types or pastes the absolute folder path.
4. User optionally fills in the **Description** field (existing behavior).
5. User clicks "Create."
6. System validates all fields (name, path, description).
7. System checks that no other project uses the same name or path.
8. System checks that the directory exists on the filesystem.
9. On success: project is created, snackbar confirms, modal returns to list view.
10. On failure: appropriate error message is displayed inline on the form (e.g., "Path already in use by another project", "Directory does not exist").

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| Path is empty or whitespace | Validation error: "Path is required." |
| Path exceeds 500 characters | Validation error: "Path must not exceed 500 characters." |
| Path is already used by another project (active or archived) | Creation fails with error: "A project with this path already exists." |
| Directory does not exist on the filesystem | Creation fails with error: "The specified directory does not exist." |
| Path contains only trailing/leading spaces but is otherwise valid | Spaces are trimmed; validation proceeds on trimmed value. |
| User tries to edit Path on an existing project | The Path field is not displayed or is displayed as read-only in the Edit view. It cannot be changed. |
| Path uses forward slashes vs backslashes | Accepted as-is — no normalization. The OS will interpret it. |

## Out of Scope

- Folder picker component (browser-native or custom server-side) — will be considered in a future spec.
- Path normalization (e.g., resolving `..`, converting `/` to `\`, removing trailing separators).
- Validation of path permissions (read/write access to the folder).
- Any feature that reads or interacts with the contents of the project folder.
- Data migration for existing projects — database will be recreated from scratch.

## Acceptance Criteria

- [ ] `Project` entity has an immutable `Path` property, set only via `Create()`.
- [ ] `Path` is required, max 500 chars, trimmed, and unique.
- [ ] `Project.Create()` validates `Path` (non-empty, max length) and throws `ArgumentException` on invalid input.
- [ ] `CreateProjectCommand` includes `Path`; handler checks for duplicate path and directory existence.
- [ ] `CreateProjectCommandValidator` validates `Path` (not empty, max 500).
- [ ] `ProjectDto` includes `Path`.
- [ ] `IProjectRepository` has `ExistsWithPathAsync` method; implementation checks all projects (including archived).
- [ ] EF Core configuration has `Path` as required, max 500, with unique index.
- [ ] Migrations generated for both SQLite and SQL Server.
- [ ] Project creation form includes a text input for `Path`, positioned between Name and Description.
- [ ] Edit form does NOT allow modifying `Path` (read-only display or hidden).
- [ ] Error messages for duplicate path and non-existent directory are shown inline on the form.
- [ ] All existing unit tests updated; new tests cover: domain validation of Path, handler duplicate-path check, handler directory-existence check.
- [ ] Solution builds with zero warnings and all tests pass.

## Dependencies

- Filesystem access (`System.IO.Directory.Exists`) — used in the handler to verify directory existence. An abstraction (e.g., `IFileSystem` interface) is recommended for testability.

## Open Questions

None — all questions resolved during brainstorming.
