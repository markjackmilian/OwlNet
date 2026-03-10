# SPEC: System Agent — Domain, Persistence & Application Layer

> **Status:** Done
> **Created:** 2026-03-10
> **Author:** owl-planner + user
> **Priority:** High (SA1 — prerequisite for SA2 and SA3)
> **Estimated Complexity:** M

## Context

OwlNet needs a catalogue of reusable, installation-wide AI agent definitions ("system agents") that exist independently of any single project. A system agent is a named, versioned agent definition (YAML frontmatter + Markdown body) stored in the database. It acts as a template: when a user wants to use it in a project, it is copied as a `.md` file into the project's `.opencode/agents/` directory (see SPEC-SA3).

This spec establishes the foundational layer: the `SystemAgent` domain entity, EF Core persistence (dual-provider: SQLite + SQL Server), and the full Application-layer CRUD surface (commands and queries via DispatchR). SPEC-SA2 (Settings UI) and SPEC-SA3 (project installation) depend on this spec.

## Actors

- **Application layer** — Commands and queries consumed by the Settings UI (SPEC-SA2) and the project install flow (SPEC-SA3).
- **Infrastructure layer** — EF Core persistence of `SystemAgent` records.

## Functional Requirements

### Domain Entity

1. The system SHALL define a `SystemAgent` entity in the Domain layer with the following properties:
   - `Id` (Guid) — primary key, generated on creation.
   - `Name` (string) — the agent identifier, used as the default filename when installed (without `.md` extension). Validation: required, 2–50 characters, alphanumeric characters and hyphens only, no spaces.
   - `DisplayName` (string) — a human-readable label shown in the UI. Required, 2–100 characters.
   - `Description` (string) — short summary of the agent's purpose. Required, 10–500 characters.
   - `Mode` (string) — the OpenCode agent mode: `"primary"`, `"subagent"`, or `"all"`. Required.
   - `Content` (string) — the full Markdown content of the agent file, including YAML frontmatter and body. Required, not empty.
   - `CreatedAt` (DateTimeOffset) — timestamp set on creation, never updated.
   - `UpdatedAt` (DateTimeOffset) — timestamp updated on every save.

2. The `SystemAgent` entity SHALL NOT contain business logic beyond property validation. It is a plain entity.

### Persistence

3. The system SHALL add a `SystemAgents` `DbSet<SystemAgent>` to the existing `AppDbContext`.
4. The system SHALL define an `IEntityTypeConfiguration<SystemAgent>` in the Infrastructure layer with:
   - Table name: `SystemAgents`.
   - `Id`: primary key.
   - `Name`: required, max length 50, unique index.
   - `DisplayName`: required, max length 100.
   - `Description`: required, max length 500.
   - `Mode`: required, max length 20.
   - `Content`: required, column type `TEXT` (no max length restriction).
   - `CreatedAt` and `UpdatedAt`: required.
5. The system SHALL generate EF Core migrations for **both** providers (SQLite and SQL Server) following the dual-provider migration workflow.

### Application Layer — DTOs

6. The system SHALL define a `SystemAgentDto` record in the Application layer with the following properties:
   - `Id` (Guid)
   - `Name` (string)
   - `DisplayName` (string)
   - `Description` (string)
   - `Mode` (string)
   - `Content` (string)
   - `CreatedAt` (DateTimeOffset)
   - `UpdatedAt` (DateTimeOffset)

### Application Layer — Queries

7. The system SHALL define a `GetAllSystemAgentsQuery` record (no properties). Its handler SHALL return `Result<IReadOnlyList<SystemAgentDto>>` with all system agents sorted alphabetically by `Name` (ascending).

8. The system SHALL define a `GetSystemAgentByIdQuery` record with property `Id` (Guid). Its handler SHALL:
   a. Look up the `SystemAgent` by `Id`.
   b. If not found, return `Result.Failure("System agent not found.")`.
   c. Return `Result<SystemAgentDto>` on success.

### Application Layer — Commands

9. The system SHALL define a `CreateSystemAgentCommand` record with properties:
   - `Name` (string)
   - `DisplayName` (string)
   - `Description` (string)
   - `Mode` (string)
   - `Content` (string)

10. The `CreateSystemAgentCommand` handler SHALL:
    a. Validate the command via `CreateSystemAgentCommandValidator`.
    b. Check that no `SystemAgent` with the same `Name` already exists; if so, return `Result.Failure("A system agent with this name already exists.")`.
    c. Create a new `SystemAgent` entity, set `CreatedAt` and `UpdatedAt` to `DateTimeOffset.UtcNow`.
    d. Persist it via the repository.
    e. Return `Result<Guid>` with the new entity's `Id`.

11. The system SHALL define a `CreateSystemAgentCommandValidator` (FluentValidation) that validates:
    - `Name`: required, 2–50 chars, matches pattern `^[a-zA-Z0-9-]+$`.
    - `DisplayName`: required, 2–100 chars.
    - `Description`: required, 10–500 chars.
    - `Mode`: required, must be one of `"primary"`, `"subagent"`, `"all"`.
    - `Content`: required, not empty or whitespace.

12. The system SHALL define an `UpdateSystemAgentCommand` record with properties:
    - `Id` (Guid)
    - `DisplayName` (string)
    - `Description` (string)
    - `Mode` (string)
    - `Content` (string)

    > Note: `Name` is intentionally excluded from updates — it is immutable after creation (it is the stable identifier used as the default filename).

13. The `UpdateSystemAgentCommand` handler SHALL:
    a. Validate the command via `UpdateSystemAgentCommandValidator`.
    b. Look up the `SystemAgent` by `Id`; if not found, return `Result.Failure("System agent not found.")`.
    c. Update the mutable properties and set `UpdatedAt` to `DateTimeOffset.UtcNow`.
    d. Persist the changes.
    e. Return `Result` indicating success or failure.

14. The system SHALL define an `UpdateSystemAgentCommandValidator` that validates:
    - `Id`: not empty.
    - `DisplayName`: required, 2–100 chars.
    - `Description`: required, 10–500 chars.
    - `Mode`: required, must be one of `"primary"`, `"subagent"`, `"all"`.
    - `Content`: required, not empty or whitespace.

15. The system SHALL define a `DeleteSystemAgentCommand` record with property `Id` (Guid). Its handler SHALL:
    a. Look up the `SystemAgent` by `Id`; if not found, return `Result.Failure("System agent not found.")`.
    b. Delete the entity from the database.
    c. Return `Result` indicating success or failure.

### Infrastructure — Repository

16. The system SHALL define an `ISystemAgentRepository` interface in the Application layer with the following methods:
    - `Task<IReadOnlyList<SystemAgent>> GetAllAsync(CancellationToken cancellationToken)`
    - `Task<SystemAgent?> GetByIdAsync(Guid id, CancellationToken cancellationToken)`
    - `Task<SystemAgent?> GetByNameAsync(string name, CancellationToken cancellationToken)`
    - `Task AddAsync(SystemAgent agent, CancellationToken cancellationToken)`
    - `Task UpdateAsync(SystemAgent agent, CancellationToken cancellationToken)`
    - `Task DeleteAsync(SystemAgent agent, CancellationToken cancellationToken)`

17. The `SystemAgentRepository` implementation SHALL be registered in `AddInfrastructure()`.

## User Flow

This spec has no direct UI. It is consumed by SPEC-SA2 and SPEC-SA3.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| `CreateSystemAgentCommand` with a duplicate `Name` | Handler returns `Result.Failure("A system agent with this name already exists.")` |
| `UpdateSystemAgentCommand` with a non-existent `Id` | Handler returns `Result.Failure("System agent not found.")` |
| `DeleteSystemAgentCommand` with a non-existent `Id` | Handler returns `Result.Failure("System agent not found.")` |
| `GetSystemAgentByIdQuery` with a non-existent `Id` | Handler returns `Result.Failure("System agent not found.")` |
| `Content` field is very large (e.g., 50KB) | Stored and retrieved without truncation. `TEXT` column type in both providers supports this. |
| Concurrent creation of two agents with the same `Name` | The unique index on `Name` ensures a database-level constraint violation; the handler catches this and returns a failure result. |

## Out of Scope

- UI for managing system agents (see SPEC-SA2).
- Installing a system agent into a project (see SPEC-SA3).
- Per-user or per-project system agent overrides.
- Versioning or history of system agent changes.
- Import/export of system agents.
- LLM-assisted generation of system agents (may be added later, analogous to SPEC-P2).

## Acceptance Criteria

- [ ] `SystemAgent` entity defined in the Domain layer with all required properties.
- [ ] `IEntityTypeConfiguration<SystemAgent>` defined in the Infrastructure layer with correct column mappings and unique index on `Name`.
- [ ] `SystemAgents` DbSet added to `AppDbContext`.
- [ ] EF Core migrations generated for both SQLite and SQL Server providers.
- [ ] `SystemAgentDto` record defined in the Application layer.
- [ ] `GetAllSystemAgentsQuery` + handler return `Result<IReadOnlyList<SystemAgentDto>>` sorted by `Name`.
- [ ] `GetSystemAgentByIdQuery` + handler return `Result<SystemAgentDto>` or failure if not found.
- [ ] `CreateSystemAgentCommand` + handler + validator: creates entity, enforces unique `Name`, returns `Result<Guid>`.
- [ ] `UpdateSystemAgentCommand` + handler + validator: updates mutable fields, `Name` is immutable.
- [ ] `DeleteSystemAgentCommand` + handler: deletes entity or returns failure if not found.
- [ ] `ISystemAgentRepository` interface defined in Application layer with all required methods.
- [ ] `SystemAgentRepository` implementation registered in `AddInfrastructure()`.
- [ ] Unit tests for `CreateSystemAgentCommand` handler: happy path, duplicate name, validation failure.
- [ ] Unit tests for `UpdateSystemAgentCommand` handler: happy path, not found, validation failure.
- [ ] Unit tests for `DeleteSystemAgentCommand` handler: happy path, not found.
- [ ] Unit tests for `GetAllSystemAgentsQuery` handler: returns sorted list, empty list.
- [ ] Unit tests for `GetSystemAgentByIdQuery` handler: happy path, not found.

## Dependencies

- **SPEC-settings-page** — `AppDbContext` and EF Core dual-provider infrastructure already in place.
- **SPEC-001-project-crud** — Result pattern, repository pattern conventions to follow.

## Open Questions

- None.
