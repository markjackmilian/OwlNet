# SPEC: Relax Project Uniqueness to Active-Only

> **Status:** Done
> **Created:** 2026-03-12
> **Author:** owl-planner + user
> **Priority:** Medium
> **Estimated Complexity:** M

## Context

Currently, both `Name` and `Path` uniqueness constraints on Project are enforced globally,
including archived projects. This prevents creating a new active project with the same name
or path as an archived one, even though archived projects are permanently inactive and no
longer "own" those values. The constraints should be relaxed to apply only among active
(non-archived) projects.

## Actors

- **User** — creates and manages projects via the UI.

## Functional Requirements

1. The system SHALL allow creating a new project with a `Name` already used by an archived project.
2. The system SHALL allow creating a new project with a `Path` already used by an archived project.
3. The system SHALL reject creating a new project with a `Name` already used by another active (non-archived) project.
4. The system SHALL reject creating a new project with a `Path` already used by another active (non-archived) project.
5. The system SHALL reject updating a project's `Name` to one already used by another active project (excluding itself).
6. The database unique index on `Projects.Name` SHALL be replaced with a filtered unique index covering only rows where `IsArchived = false`.
7. The database unique index on `Projects.Path` SHALL be replaced with a filtered unique index covering only rows where `IsArchived = false`.
8. The `ExistsWithNameAsync` repository method SHALL only consider active projects (i.e. filter by `IsArchived = false`).
9. The `ExistsWithPathAsync` repository method SHALL only consider active projects (i.e. filter by `IsArchived = false`).

## User Flow

This change is transparent to the user. The only visible difference is:

1. User attempts to create a project with a name or path previously used by an archived project.
2. System accepts the input (no duplicate error shown).
3. Project is created successfully.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| New project with same Name as an archived project | Allowed — creation succeeds |
| New project with same Path as an archived project | Allowed — creation succeeds |
| New project with same Name as an active project | Rejected — "A project with this name already exists." |
| New project with same Path as an active project | Rejected — "A project with this path already exists." |
| Update project Name to same as another active project | Rejected — "A project with this name already exists." |
| Two archived projects with the same Name | Allowed — no constraint on archived projects |
| Two archived projects with the same Path | Allowed — no constraint on archived projects |

## Out of Scope

- Restore/reactivation of archived projects (not supported by the system).
- Any changes to the Archive flow itself.
- UI changes — error messages remain identical, only the condition that triggers them changes.

## Acceptance Criteria

- [ ] A project can be created with the same `Name` as an archived project.
- [ ] A project can be created with the same `Path` as an archived project.
- [ ] Creating a project with the same `Name` as an active project is still rejected.
- [ ] Creating a project with the same `Path` as an active project is still rejected.
- [ ] Updating a project's `Name` to match another active project is still rejected.
- [ ] `ExistsWithNameAsync` filters by `IsArchived = false`.
- [ ] `ExistsWithPathAsync` filters by `IsArchived = false`.
- [ ] EF Core migrations exist for both SQLite and SQL Server replacing the global unique indexes with filtered unique indexes on `IsArchived = false`.
- [ ] All existing tests pass; new tests cover the relaxed-uniqueness scenarios.

## Dependencies

- EF Core migrations for both SQLite and SQL Server providers.
- `ProjectConfiguration.cs` — unique index definitions.
- `ProjectRepository.cs` — `ExistsWithNameAsync` and `ExistsWithPathAsync`.
- `CreateProjectCommandHandler.cs` — duplicate checks.
- `UpdateProjectCommandHandler.cs` — duplicate name check.

## Open Questions

- None.
