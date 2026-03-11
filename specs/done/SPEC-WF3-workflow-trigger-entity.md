# SPEC: Workflow Trigger Entity

> **Status:** Done
> **Created:** 2026-03-10
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

OwlNet needs to automate actions when cards change status on the Kanban board. A "Workflow Trigger" defines what happens when a card transitions from one status to another: which agents are invoked and what prompt is sent to them.

Each trigger is scoped to a project, binds to a specific status transition (from → to), references one or more agents to execute in sequence, and carries a shared prompt that provides context to the agents. At runtime (future spec), when a card's status changes and matches a trigger's transition, the system will invoke the configured agents sequentially via OpenCode sessions.

This spec covers the domain entity, persistence, and business rules for workflow triggers. The UI for managing triggers is covered in SPEC-WF4-workflow-trigger-ui.

## Actors

| Actor | Description |
|-------|-------------|
| **User** | Authenticated user configuring workflow triggers for a project |
| **System** | Evaluates triggers on card status change and executes agent chains |

## Functional Requirements

### WorkflowTrigger Entity

1. The system SHALL persist workflow triggers as domain entities with the following properties:
   - **Id**: Guid, unique identifier, generated on creation.
   - **ProjectId**: foreign key to Project. Required.
   - **Name**: string, required, 1-150 characters, not blank. Human-readable label for the trigger (e.g., "Code Review on Develop → Review").
   - **FromStatusId**: foreign key to BoardStatus. Required. The source status of the transition.
   - **ToStatusId**: foreign key to BoardStatus. Required. The destination status of the transition.
   - **Prompt**: string, required, 1-10000 characters. The shared prompt/description sent to all agents when the trigger fires.
   - **IsEnabled**: bool, default true. Allows disabling a trigger without deleting it.
   - **CreatedAt**: DateTimeOffset, set to UTC on creation.
   - **UpdatedAt**: DateTimeOffset, updated on every mutation.

2. A WorkflowTrigger SHALL belong to exactly one Project.
3. FromStatusId and ToStatusId SHALL both reference BoardStatus entities that belong to the same project as the trigger.
4. FromStatusId and ToStatusId SHALL NOT be the same (a trigger must represent an actual transition).
5. Multiple triggers MAY exist for the same status transition within a project. When multiple triggers match a transition, they SHALL all be executed in the order of their creation date (oldest first).

### WorkflowTriggerAgent Entity (Join/Ordered Association)

6. The system SHALL persist the association between a trigger and its agents in a **WorkflowTriggerAgent** entity with the following properties:
   - **Id**: Guid, unique identifier.
   - **WorkflowTriggerId**: foreign key to WorkflowTrigger. Required.
   - **AgentName**: string, required, 1-200 characters. The file name (without extension) of the agent in the project's `.opencode/agents/` directory.
   - **SortOrder**: int, required. Defines the execution sequence (0-based, ascending).

7. A WorkflowTrigger SHALL have at least one associated WorkflowTriggerAgent.
8. The agents within a trigger SHALL be executed **sequentially** in SortOrder ascending order.
9. If an agent execution **fails**, the chain SHALL be **interrupted** — subsequent agents in the same trigger SHALL NOT execute.
10. The user SHALL be able to reorder agents within a trigger via drag-and-drop. The SortOrder values SHALL be updated accordingly.
11. AgentName references agents by their file name in the project's `.opencode/agents/` directory. If an agent file is renamed or deleted externally, the trigger retains the stale reference. The system SHALL surface a warning when a trigger references an agent that no longer exists (at display time, not at save time).

### Trigger Evaluation Rules

12. When a card's status changes (via manual move or automated action), the system SHALL query all **enabled** triggers for the card's project where FromStatusId matches the card's previous status and ToStatusId matches the card's new status.
13. If no matching triggers are found, no action is taken.
14. If matching triggers are found, they SHALL be queued for execution. The actual execution mechanism (OpenCode session invocation) is out of scope for this spec.

### Trigger CRUD

15. The system SHALL support creating a trigger with: Name, FromStatusId, ToStatusId, Prompt, and at least one agent with SortOrder.
16. The system SHALL support updating a trigger's Name, FromStatusId, ToStatusId, Prompt, IsEnabled flag, and agent list (add, remove, reorder).
17. The system SHALL support deleting a trigger. Deleting a trigger SHALL cascade-delete its WorkflowTriggerAgent records.
18. The system SHALL support querying all triggers for a given project, with optional filtering by enabled/disabled status.
19. The system SHALL support querying triggers that match a specific status transition (FromStatusId + ToStatusId) for trigger evaluation purposes.

## User Flow

### Creating a Trigger
1. User navigates to the Workflow page for a project.
2. User clicks "Add Trigger".
3. User enters a Name for the trigger.
4. User selects the source status (FromStatus) from a dropdown of the project's statuses.
5. User selects the destination status (ToStatus) from a dropdown of the project's statuses (excluding the selected FromStatus).
6. User writes a Prompt describing what the agents should do when this transition occurs.
7. User selects one or more agents from the project's available agents.
8. User orders the agents via drag-and-drop.
9. User saves the trigger.

### Editing a Trigger
1. User clicks on an existing trigger in the list.
2. System displays the trigger's details in an edit form.
3. User modifies any field (name, statuses, prompt, agents, order).
4. User saves changes.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| User tries to create a trigger with FromStatus = ToStatus | Reject with validation error: "Source and destination status must be different" |
| User tries to create a trigger with no agents | Reject with validation error: "At least one agent is required" |
| User tries to reference a status from a different project | Reject with domain error: "Status does not belong to this project" |
| Trigger references an agent that no longer exists on disk | Display a warning icon/badge on the trigger and on the specific agent entry. Do not block saving or execution. |
| A BoardStatus referenced by a trigger is deleted | Trigger deletion is NOT cascaded. The trigger retains the stale reference. The system SHALL surface a warning when displaying triggers with invalid status references. (Status deletion is already guarded by card usage in SPEC-WF1-board-status-management; an additional guard for trigger usage could be added — see Open Questions.) |
| User creates multiple triggers for the same transition | Allowed. All matching triggers execute in creation-date order. |
| Trigger Name is blank or exceeds 150 characters | Reject with validation error |
| Prompt is blank or exceeds 10000 characters | Reject with validation error |
| Project has no agents defined | User can still create a trigger, but the agent selection list will be empty, preventing save (requirement 7). |

## Out of Scope

- Trigger execution engine (invoking OpenCode sessions with agents and prompts). This will be a separate spec.
- Trigger execution logging/history (tracking which triggers fired, when, and their results).
- Conditional logic within triggers (e.g., "only fire if card priority is Critical").
- Trigger chaining (one trigger's completion causing another trigger to fire).
- UI for managing triggers (covered in SPEC-WF4-workflow-trigger-ui).

## Acceptance Criteria

- [ ] WorkflowTrigger entity is persisted with all specified properties (Id, ProjectId, Name, FromStatusId, ToStatusId, Prompt, IsEnabled, CreatedAt, UpdatedAt).
- [ ] WorkflowTriggerAgent entity is persisted with all specified properties (Id, WorkflowTriggerId, AgentName, SortOrder).
- [ ] Triggers belong to a project and reference only statuses from that project.
- [ ] FromStatusId and ToStatusId cannot be the same.
- [ ] A trigger must have at least one agent.
- [ ] Agents within a trigger are ordered by SortOrder and executed sequentially.
- [ ] Agent chain execution stops on first failure.
- [ ] Multiple triggers can exist for the same status transition.
- [ ] Triggers can be enabled/disabled without deletion.
- [ ] Triggers can be created, updated, and deleted.
- [ ] Trigger deletion cascades to WorkflowTriggerAgent records.
- [ ] Triggers matching a status transition can be queried efficiently.
- [ ] Stale agent references (agent file deleted/renamed) are surfaced as warnings, not errors.

## Dependencies

- **SPEC-WF1-board-status-management** — BoardStatus entities must exist for triggers to reference.
- **SPEC-WF2-board-card-entity** — Card status change mechanism must exist to evaluate triggers.
- **Agent filesystem service** (`IAgentFileService`) — to list available agents for selection and to detect stale references.
- **EF Core dual-provider migrations** — new tables for WorkflowTrigger and WorkflowTriggerAgent.

## Open Questions

1. **Status deletion guard**: Should the system also prevent deleting a BoardStatus that is referenced by a workflow trigger? Or is a warning sufficient (since the trigger can be edited to use a different status)?
2. **Trigger execution context**: When the trigger fires, should the prompt be enriched with card context (title, description, priority, previous status, new status) automatically, or should the user manually include placeholders like `{card.title}` in the prompt? This impacts SPEC-WF4-workflow-trigger-ui as well.
3. **Agent validation at save time**: Should the system validate that referenced agents exist on disk when saving a trigger, or only warn at display time? Validating at save time is stricter but may cause issues if agents are managed externally.
