# SPEC: Card Entity Enrichment — Tags

> **Status:** Done
> **Created:** 2026-03-12
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

The `Card` entity (introduced in SPEC-WF2-board-card-entity) covers the essential fields for a Kanban work item. To support categorization and filtering, cards need a tagging system. Tags are drawn from a project-scoped vocabulary (`ProjectTag`) so that naming is consistent across all cards within a project.

This spec introduces the `ProjectTag` entity, the `CardTag` join entity, and the enrichment of the `Card` entity with a tag collection. It also covers the full Application layer (CQRS commands and queries) and dual-provider EF Core migrations.

No UI is included in this spec — UI for tag management is covered in SPEC-C7, and tag assignment on cards is covered in SPEC-C5 and SPEC-C6.

## Actors

| Actor | Description |
|-------|-------------|
| **User** | Authenticated user managing tags for a project and assigning them to cards |
| **System** | Enforces tag vocabulary constraints and referential integrity |

## Functional Requirements

### ProjectTag Entity

1. The system SHALL persist project tags as domain entities with the following properties:
   - **Id**: Guid, unique identifier, generated on creation.
   - **ProjectId**: foreign key to `Project`. Required.
   - **Name**: string, required, 1–50 characters, not blank. Unique within the project scope (case-insensitive).
   - **Color**: string, optional, hex color code (e.g., `#FF5733`), max 7 characters. Used for visual display.
   - **CreatedAt**: `DateTimeOffset`, set to UTC on creation.
   - **UpdatedAt**: `DateTimeOffset`, updated on every mutation.

2. A `ProjectTag` SHALL belong to exactly one `Project`.
3. Tag names SHALL be unique within a project (case-insensitive comparison). The same name MAY exist in different projects.
4. The system SHALL support the following operations on `ProjectTag`:
   - **Create**: add a new tag to a project's vocabulary.
   - **Rename**: update the tag's name (uniqueness re-validated).
   - **Update color**: update the tag's color.
   - **Delete**: remove a tag from the vocabulary. Deletion SHALL cascade-remove all `CardTag` associations for that tag (the tag is removed from all cards that use it).
5. Deleting a `ProjectTag` SHALL NOT be blocked by card usage — the tag is simply removed from all cards silently (cascade delete on `CardTag`).

### CardTag Entity (Join)

6. The system SHALL persist the association between a card and its tags in a **`CardTag`** join entity with the following properties:
   - **CardId**: foreign key to `Card`. Required.
   - **TagId**: foreign key to `ProjectTag`. Required.
   - The composite `(CardId, TagId)` SHALL be the primary key (no separate Guid Id).

7. A `Card` MAY have zero or more tags.
8. A tag MAY be assigned to zero or more cards within the same project.
9. The system SHALL enforce that a tag assigned to a card belongs to the same project as the card. Cross-project tag assignment SHALL be rejected.

### Card Entity — Tag Navigation

10. The `Card` entity SHALL expose a navigation property `IReadOnlyList<CardTag> Tags`.
11. The `Card` entity SHALL expose methods:
    - `AddTag(tagId, tagProjectId)` — validates same-project constraint, adds `CardTag` if not already present (idempotent).
    - `RemoveTag(tagId)` — removes the `CardTag` association if present (no-op if not present).
12. `UpdatedAt` on the `Card` SHALL be refreshed when tags are added or removed.

### Application Layer — ProjectTag CQRS

13. The system SHALL implement the following commands and queries for `ProjectTag`:
    - `CreateProjectTagCommand` — Name, Color?, ProjectId → returns `Guid` (new tag Id).
    - `UpdateProjectTagCommand` — TagId, Name?, Color? → updates provided fields.
    - `DeleteProjectTagCommand` — TagId → deletes tag and cascades to `CardTag`.
    - `GetProjectTagsQuery` — ProjectId → returns `IReadOnlyList<ProjectTagDto>` ordered by Name ascending.

### Application Layer — Card Tag Assignment CQRS

14. The system SHALL implement the following commands for card tag management:
    - `AddTagToCardCommand` — CardId, TagId → adds the tag to the card.
    - `RemoveTagFromCardCommand` — CardId, TagId → removes the tag from the card.

15. The existing `CardDto` SHALL be enriched with a `Tags` property: `IReadOnlyList<ProjectTagDto> Tags`.
16. The existing `GetCardsByProjectQuery` handler SHALL include tags in the returned `CardDto` records (eager-loaded).

### Persistence

17. The system SHALL add EF Core configurations for `ProjectTag` and `CardTag`.
18. The system SHALL generate dual-provider migrations (SQLite + SQL Server) for the new tables.
19. The `CardTag` table SHALL use a composite primary key `(CardId, TagId)`.
20. Deleting a `ProjectTag` SHALL cascade-delete its `CardTag` rows (configured via EF Fluent API).
21. Deleting a `Card` SHALL cascade-delete its `CardTag` rows.

## User Flow

This spec has no direct UI flow — tag assignment UI is covered in SPEC-C5 and SPEC-C6, and tag vocabulary management UI is covered in SPEC-C7.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| User creates a tag with a name already existing in the project (case-insensitive) | Reject with validation error: "A tag with this name already exists in this project." |
| User assigns a tag from a different project to a card | Reject with domain error: "Tag does not belong to this project." |
| User assigns the same tag to a card twice | Idempotent: no error, no duplicate `CardTag` row. |
| User removes a tag from a card that does not have it | No-op: no error. |
| User deletes a `ProjectTag` that is assigned to 10 cards | Tag is deleted; all 10 `CardTag` associations are cascade-deleted silently. |
| Tag name is blank or exceeds 50 characters | Reject with validation error. |
| Tag color is provided but is not a valid hex code | Reject with validation error: "Color must be a valid hex color code (e.g., #FF5733)." |

## Out of Scope

- UI for managing the project tag vocabulary (covered in SPEC-C7).
- UI for assigning tags to cards (covered in SPEC-C5 and SPEC-C6).
- Tag-based filtering on the board (future spec).
- Global/system-wide tags shared across projects.
- Tag ordering or pinning.
- Tag usage statistics (how many cards use a tag).

## Acceptance Criteria

- [ ] `ProjectTag` entity is persisted with all specified properties (Id, ProjectId, Name, Color, CreatedAt, UpdatedAt).
- [ ] `CardTag` join entity is persisted with composite PK `(CardId, TagId)`.
- [ ] Tag names are unique within a project (case-insensitive); duplicates are rejected.
- [ ] Cross-project tag assignment is rejected by domain validation.
- [ ] Assigning the same tag twice is idempotent (no error, no duplicate row).
- [ ] Deleting a `ProjectTag` cascade-deletes all its `CardTag` associations.
- [ ] Deleting a `Card` cascade-deletes its `CardTag` associations.
- [ ] `Card.AddTag()` and `Card.RemoveTag()` work correctly and update `UpdatedAt`.
- [ ] `CreateProjectTagCommand`, `UpdateProjectTagCommand`, `DeleteProjectTagCommand`, `GetProjectTagsQuery` are implemented and tested.
- [ ] `AddTagToCardCommand` and `RemoveTagFromCardCommand` are implemented and tested.
- [ ] `CardDto` includes a `Tags` property populated by `GetCardsByProjectQuery`.
- [ ] EF Core configurations use Fluent API exclusively (no data annotations).
- [ ] Dual-provider migrations (SQLite + SQL Server) are generated and applied cleanly.

## Dependencies

- **SPEC-WF2-board-card-entity** — `Card` entity must exist.
- **SPEC-001-project-crud** — `Project` entity must exist.
- **EF Core dual-provider migrations** — new tables `ProjectTags` and `CardTags`.

## Open Questions

1. **Color field**: Should `Color` be a required field (forcing users to always pick a color), or optional (defaulting to a neutral gray in the UI)? Currently specified as optional.
2. **Tag limit per card**: Should there be a maximum number of tags per card (e.g., max 10)? No limit is specified for now.
3. **Tag limit per project**: Should there be a maximum number of tags per project vocabulary? No limit is specified for now.
