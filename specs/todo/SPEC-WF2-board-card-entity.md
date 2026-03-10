# SPEC: Board Card Entity

> **Status:** Todo
> **Created:** 2026-03-10
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

OwlNet's Kanban board currently operates on mock data defined in the Web layer (`BoardMockData`, `BoardCardItem`). To support persistent cards, status change tracking, and workflow triggers, cards must become a domain entity with full persistence.

This spec introduces the Card entity, its relationship to Project and BoardStatus, and a CardStatusHistory table that tracks every status transition with timestamp and actor information.

## Actors

| Actor | Description |
|-------|-------------|
| **User** | Authenticated user creating and managing cards on the board |
| **System/Agent** | Automated actor that can change card status via workflow triggers |

## Functional Requirements

### Card Entity

1. The system SHALL persist cards as domain entities with the following properties:
   - **Id**: Guid, unique identifier, generated on creation.
   - **Title**: string, required, 1-200 characters, not blank.
   - **Description**: string, optional, max 5000 characters. Supports Markdown.
   - **Priority**: enum (Critical, High, Medium, Low). Required. Fixed enum, not customizable.
   - **StatusId**: foreign key to the project's BoardStatus entity. Required.
   - **ProjectId**: foreign key to Project. Required.
   - **CreatedAt**: DateTimeOffset, set to UTC on creation.
   - **UpdatedAt**: DateTimeOffset, updated on every mutation.

2. A Card SHALL belong to exactly one Project (many-to-one relationship).
3. A Card SHALL reference exactly one BoardStatus from its owning project's status set.
4. When a Card is created, its status SHALL default to the project's status with sort order 0 (typically "Backlog"). If no statuses exist for the project, card creation SHALL be rejected.
5. The Card entity SHALL enforce a `ChangeStatus` method that:
   a. Accepts the new status ID, the actor identifier (string), and the change source (Manual or Trigger).
   b. Validates that the new status belongs to the same project as the card.
   c. Records the transition in CardStatusHistory.
   d. Updates the card's StatusId and UpdatedAt.

### Card Status History

6. The system SHALL persist status transitions in a **CardStatusHistory** entity with the following properties:
   - **Id**: Guid, unique identifier.
   - **CardId**: foreign key to Card. Required.
   - **PreviousStatusId**: foreign key to BoardStatus. Nullable (null for the initial status assignment on card creation).
   - **NewStatusId**: foreign key to BoardStatus. Required.
   - **ChangedAt**: DateTimeOffset, UTC timestamp of the transition.
   - **ChangedBy**: string, required. Contains the user ID for manual changes or an agent/trigger identifier for automated changes.
   - **ChangeSource**: enum (Manual, Trigger). Indicates whether the change was performed by a user or by an automated workflow trigger.

7. Every status change on a card — including the initial assignment at creation — SHALL produce a CardStatusHistory record.
8. The system SHALL record the initial card creation as a history entry with PreviousStatusId = null, NewStatusId = the default status, ChangedBy = the creating user's ID, and ChangeSource = Manual.
9. CardStatusHistory records SHALL be immutable (append-only). They SHALL NOT be updated or deleted.
10. The system SHALL provide a query to retrieve the full status history of a card, ordered by ChangedAt descending (most recent first).

### Card CRUD

11. The system SHALL support creating a card with Title, Description (optional), and Priority. ProjectId is inferred from context. Status is assigned automatically per requirement 4.
12. The system SHALL support updating a card's Title, Description, and Priority. Status changes are handled exclusively through the `ChangeStatus` method (requirement 5).
13. The system SHALL support deleting a card. Deleting a card SHALL cascade-delete its CardStatusHistory records.
14. The system SHALL support querying all cards for a given project, with the ability to filter by status and/or priority.

## User Flow

### Creating a Card
1. User is on the project Board page.
2. User clicks "Add Card" (or equivalent action).
3. User enters Title, optional Description, and selects Priority.
4. System creates the card with the project's default status (sort order 0).
5. System records the initial status assignment in CardStatusHistory.
6. Card appears in the appropriate board column.

### Changing Card Status (Manual)
1. User drags a card from one column to another on the board.
2. System calls `ChangeStatus` with the new status, the user's ID, and ChangeSource = Manual.
3. System records the transition in CardStatusHistory.
4. Card moves to the new column.

### Viewing Status History
1. User opens a card's detail view.
2. System displays the card's status history as a timeline, showing: previous status → new status, timestamp, who made the change, and whether it was manual or automated.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| User creates a card in a project with no statuses | Reject with error: "Cannot create card — project has no statuses configured" |
| User tries to change card status to a status from a different project | Reject with domain error: "Status does not belong to this project" |
| User tries to change card status to its current status | No-op: no history record created, no error raised |
| User sets card status to a status that is later deleted | Card retains the status reference. Status deletion is blocked by SPEC-WF1-board-status-management if cards use it. |
| Card Title is blank or exceeds 200 characters | Reject with validation error |
| Card Description exceeds 5000 characters | Reject with validation error |
| Concurrent status changes on the same card | Optimistic concurrency: last write wins on the card entity. Both history records are preserved. |

## Out of Scope

- Board UI rendering and drag-and-drop interaction (existing Board page concern, to be adapted).
- Card assignment to users (can be added in a future spec).
- Card comments or attachments.
- Card due dates or time tracking.
- Workflow triggers reacting to status changes (covered in SPEC-WF3-workflow-trigger-entity).
- Card search or full-text search.

## Acceptance Criteria

- [ ] Card entity is persisted in the database with all specified properties.
- [ ] Cards belong to a project and reference a project-level BoardStatus.
- [ ] New cards are created with the project's default status (sort order 0).
- [ ] Every status change produces an immutable CardStatusHistory record.
- [ ] CardStatusHistory records include previous status, new status, timestamp, actor, and change source (Manual/Trigger).
- [ ] Initial card creation produces a history record with null previous status.
- [ ] Changing to the same status is a no-op (no error, no history record).
- [ ] Cross-project status assignment is rejected by domain validation.
- [ ] Cards can be created, updated (title/description/priority), and deleted.
- [ ] Card deletion cascades to CardStatusHistory.
- [ ] Cards can be queried by project with optional status and priority filters.
- [ ] Status history can be retrieved for a card, ordered by most recent first.

## Dependencies

- **SPEC-WF1-board-status-management** — BoardStatus entity must exist for cards to reference statuses.
- **Project entity** (`OwlNet.Domain.Entities.Project`) — cards belong to a project.
- **EF Core dual-provider migrations** — new tables for Card and CardStatusHistory.

## Open Questions

1. **Card numbering**: Should cards have a human-readable sequential number within a project (e.g., PROJ-1, PROJ-2) in addition to the Guid ID? This is common in issue trackers but adds complexity.
2. **Soft delete vs hard delete**: Should card deletion be a soft delete (IsDeleted flag) to preserve history, or a hard delete with cascading history removal?
