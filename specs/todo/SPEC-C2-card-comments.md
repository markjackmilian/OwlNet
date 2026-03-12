# SPEC: Card Comments

> **Status:** Todo
> **Created:** 2026-03-12
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

Cards are the central work item in OwlNet. To support collaboration and traceability, cards need a comment thread. Comments can be written by a human user or posted by an AI agent as part of a workflow trigger execution. Both types are stored in the same `CardComment` entity, distinguished by an `AuthorType` field.

Comments are **immutable** once created — neither human nor agent comments can be edited or deleted. This ensures a reliable audit trail of all activity on a card.

This spec covers the domain entity, persistence, and Application layer (CQRS). The UI for displaying and adding comments is covered in SPEC-C6 (Card Detail Dialog).

## Actors

| Actor | Description |
|-------|-------------|
| **User** | Authenticated user posting a comment on a card |
| **Agent** | AI agent posting a comment as part of a workflow trigger execution |
| **System** | Enforces immutability and validation rules |

## Functional Requirements

### CardComment Entity

1. The system SHALL persist card comments as domain entities with the following properties:
   - **Id**: Guid, unique identifier, generated on creation.
   - **CardId**: foreign key to `Card`. Required.
   - **Content**: string, required, 1–10,000 characters, not blank. Supports Markdown.
   - **AuthorType**: enum (`Human` | `Agent`). Required. Indicates whether the comment was written by a user or an AI agent.
   - **AuthorId**: string, required when `AuthorType = Human`. Contains the authenticated user's ID. NULL when `AuthorType = Agent`.
   - **AgentName**: string, required when `AuthorType = Agent`. Contains the agent's identifier (name of the `.md` agent file, without extension). NULL when `AuthorType = Human`.
   - **WorkflowTriggerId**: Guid?, nullable. Foreign key to `WorkflowTrigger`. Set when the comment is posted by an agent as part of a trigger execution. NULL for human comments and for agent comments posted outside a trigger context.
   - **CreatedAt**: `DateTimeOffset`, set to UTC on creation.

2. `CardComment` records SHALL be **immutable** (append-only). Once created, no field SHALL be updatable.
3. A `CardComment` SHALL belong to exactly one `Card`.
4. When `AuthorType = Human`, `AuthorId` SHALL be non-null and `AgentName` SHALL be null.
5. When `AuthorType = Agent`, `AgentName` SHALL be non-null and `AuthorId` SHALL be null.
6. Deleting a `Card` SHALL cascade-delete all its `CardComment` records.
7. Deleting a `WorkflowTrigger` SHALL NOT cascade-delete comments that reference it — the `WorkflowTriggerId` FK SHALL be set to null on trigger deletion (set-null behavior).

### Card Entity — Comment Navigation

8. The `Card` entity SHALL expose a navigation property `IReadOnlyList<CardComment> Comments`.
9. The `Card` entity SHALL expose a method `AddComment(content, authorType, authorId?, agentName?, workflowTriggerId?)` that:
   - Validates that `AuthorId` is provided when `AuthorType = Human`.
   - Validates that `AgentName` is provided when `AuthorType = Agent`.
   - Creates and appends a new `CardComment` to the collection.
   - Does NOT update `Card.UpdatedAt` — comments are a separate concern from card data.

### Application Layer — CQRS

10. The system SHALL implement the following commands and queries for `CardComment`:
    - `AddHumanCommentCommand` — CardId, Content, AuthorId → adds a human comment.
    - `AddAgentCommentCommand` — CardId, Content, AgentName, WorkflowTriggerId? → adds an agent comment.
    - `GetCardCommentsQuery` — CardId → returns `IReadOnlyList<CardCommentDto>` ordered by `CreatedAt` ascending (oldest first, chronological thread).

11. The `CardCommentDto` record SHALL contain:
    - `Id` (Guid)
    - `CardId` (Guid)
    - `Content` (string, raw Markdown)
    - `AuthorType` (`CommentAuthorType` enum: `Human` | `Agent`)
    - `AuthorId` (string?, null for agent comments)
    - `AgentName` (string?, null for human comments)
    - `WorkflowTriggerId` (Guid?, null if not from a trigger)
    - `WorkflowTriggerName` (string?, denormalized name of the trigger, null if trigger was deleted or not applicable)
    - `CreatedAt` (`DateTimeOffset`)

12. `AddHumanCommentCommand` SHALL validate:
    - `CardId` references an existing card.
    - `Content` is not blank and does not exceed 10,000 characters.
    - `AuthorId` is not blank.

13. `AddAgentCommentCommand` SHALL validate:
    - `CardId` references an existing card.
    - `Content` is not blank and does not exceed 10,000 characters.
    - `AgentName` is not blank.

### Persistence

14. The system SHALL add an EF Core configuration for `CardComment` using Fluent API exclusively.
15. Cascade delete from `Card` to `CardComment` SHALL be configured.
16. The `WorkflowTriggerId` FK SHALL use set-null on trigger deletion.
17. The system SHALL generate dual-provider migrations (SQLite + SQL Server) for the new `CardComments` table.
18. The `CardComment` table SHALL have an index on `(CardId, CreatedAt)` to support efficient chronological retrieval.

## User Flow

*Note: The UI for this feature is covered in SPEC-C6. This spec covers only the domain and application layer.*

### Conceptual Flow — Human Comment
1. User opens a card's detail dialog (SPEC-C6).
2. User types a comment in the comment input area.
3. User submits the comment.
4. System calls `AddHumanCommentCommand` with the card ID, content, and the authenticated user's ID.
5. Comment appears at the bottom of the comment thread.

### Conceptual Flow — Agent Comment
1. A workflow trigger fires during a card status change (SPEC-C8).
2. The agent produces output that includes a comment for the card.
3. System calls `AddAgentCommentCommand` with the card ID, content, agent name, and trigger ID.
4. Comment appears in the card's comment thread, visually distinguished as an agent comment.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| User submits an empty comment | Reject with validation error: "Comment cannot be empty." |
| Comment content exceeds 10,000 characters | Reject with validation error: "Comment cannot exceed 10,000 characters." |
| `AddAgentCommentCommand` is called with no `AgentName` | Reject with validation error: "Agent name is required for agent comments." |
| `AddHumanCommentCommand` is called with no `AuthorId` | Reject with validation error: "Author ID is required for human comments." |
| Card does not exist | Reject with not-found error. |
| Workflow trigger is deleted after agent posted a comment referencing it | Comment is retained; `WorkflowTriggerId` is set to null; `WorkflowTriggerName` in DTO returns null. |
| Card is deleted | All comments are cascade-deleted. |

## Out of Scope

- Editing or deleting comments (comments are immutable by design).
- Reactions or emoji responses to comments.
- Threaded replies (comments are a flat list).
- Mentions (`@user`) or notifications.
- Comment search or full-text search.
- Pagination of comments (all comments for a card are loaded at once; pagination can be added in a future spec if needed).
- UI for displaying and adding comments (covered in SPEC-C6).

## Acceptance Criteria

- [ ] `CardComment` entity is persisted with all specified properties.
- [ ] `AuthorType` enum (`Human` | `Agent`) is defined and used correctly.
- [ ] Human comments have `AuthorId` set and `AgentName` null.
- [ ] Agent comments have `AgentName` set and `AuthorId` null.
- [ ] `WorkflowTriggerId` is nullable and uses set-null on trigger deletion.
- [ ] Comments are immutable — no update or delete commands exist.
- [ ] `Card.AddComment()` enforces author type constraints.
- [ ] `AddHumanCommentCommand` and `AddAgentCommentCommand` are implemented and tested.
- [ ] `GetCardCommentsQuery` returns comments ordered by `CreatedAt` ascending.
- [ ] `CardCommentDto` includes denormalized `WorkflowTriggerName` (null-safe).
- [ ] Card deletion cascades to all its comments.
- [ ] EF Core configuration uses Fluent API exclusively.
- [ ] Index on `(CardId, CreatedAt)` is defined.
- [ ] Dual-provider migrations (SQLite + SQL Server) are generated and applied cleanly.

## Dependencies

- **SPEC-WF2-board-card-entity** — `Card` entity must exist.
- **SPEC-WF3-workflow-trigger-entity** — `WorkflowTrigger` entity must exist (for FK reference).
- **EF Core dual-provider migrations** — new table `CardComments`.

## Open Questions

1. **Comment length**: 10,000 characters is generous for a human comment but may be appropriate for agent output. Should human and agent comments have different max lengths?
2. **Comment loading strategy**: Should `GetCardCommentsQuery` always load all comments, or should it support pagination (offset/limit) from the start? For now, all comments are loaded — pagination can be added later.
