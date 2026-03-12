# SPEC: Workflow Trigger Execution Engine

> **Status:** Todo
> **Created:** 2026-03-12
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** L

## Context

The workflow trigger infrastructure is fully defined (SPEC-WF3): triggers are stored in the database, bound to status transitions, and linked to ordered agent chains. The OpenCode integration services are also implemented (`IOpenCodeSessionService`, `IOpenCodeMessageService`, `IOpenCodeEventService`).

What is missing is the **execution engine**: the service that, when a card changes status, finds matching triggers, invokes each trigger's agent chain via OpenCode sessions, collects the output, and posts results back to the card as comments and/or attachments.

This spec defines the execution pipeline, the contract between the board/dialog and the engine, and the persistence of execution results. The engine is invoked from two places: the board (SPEC-C4) and the card detail dialog (SPEC-C6), both after a successful `ChangeCardStatusCommand`.

## Actors

| Actor | Description |
|-------|-------------|
| **System** | Orchestrates trigger evaluation and agent chain execution |
| **Agent** | AI agent invoked via OpenCode session; produces text output |
| **User** | Passive observer; sees execution results as card comments and attachments |

## Functional Requirements

### Trigger Evaluation

1. The system SHALL expose a service `IWorkflowTriggerExecutionService` in the Application layer with a single method:
   ```
   ExecuteTriggersAsync(cardId, previousStatusId, newStatusId, changedBy, cancellationToken)
   ```
2. This method SHALL:
   a. Call `GetWorkflowTriggersByTransitionQuery` to retrieve all **enabled** triggers for the card's project matching `previousStatusId → newStatusId`.
   b. If no triggers match, return immediately (no-op).
   c. For each matching trigger (ordered by creation date ascending), execute the trigger's agent chain sequentially (see requirements below).
3. The caller (board or dialog) SHALL invoke `ExecuteTriggersAsync` **fire-and-forget** — it does not await the result. The method runs asynchronously in the background.

### Prompt Enrichment

4. Before invoking any agent, the system SHALL enrich the trigger's prompt with card context by substituting the following placeholders (if present in the prompt text):
   - `{card.number}` → the card's sequential number (e.g., `42`).
   - `{card.title}` → the card's title.
   - `{card.description}` → the card's description (raw Markdown).
   - `{card.priority}` → the card's priority as a string (e.g., `High`).
   - `{card.previousStatus}` → the name of the previous status.
   - `{card.newStatus}` → the name of the new status.
5. If a placeholder is present in the prompt but the corresponding value is null or empty, the placeholder SHALL be replaced with an empty string (no error).
6. The enriched prompt SHALL be the actual text sent to the OpenCode session. The original trigger prompt stored in the database is never modified.

### Agent Chain Execution (per Trigger)

7. For each trigger, the system SHALL create a **new OpenCode session** (via `IOpenCodeSessionService`) scoped to the card's project path.
8. Within the session, agents SHALL be invoked **sequentially** in `SortOrder` ascending order.
9. For each agent in the chain:
   a. Send the enriched prompt to the session via `IOpenCodeMessageService`, specifying the agent by name.
   b. Stream and collect the agent's response via `IOpenCodeEventService`.
   c. If the agent produces a non-empty text response, the system SHALL post it as an **agent comment** on the card via `AddAgentCommentCommand` (with `AgentName` and `WorkflowTriggerId`).
   d. If the agent produces a response that is identified as a document (see requirement 10), the system SHALL also save it as a **card attachment** via `AddCardAttachmentCommand`.
10. A response is classified as a **document attachment** if it meets either of these conditions:
    - The response begins with a Markdown frontmatter block (`---` ... `---`) containing a `filename:` key.
    - The response is longer than 2,000 characters (heuristic: long responses are treated as documents).
    - In both cases, the `FileName` is taken from the frontmatter `filename:` value if present, otherwise generated as `{agent-name}-output-{timestamp}.md`.
11. If an agent execution **fails** (OpenCode session error, timeout, or agent returns an error response), the chain SHALL be **interrupted** for that trigger. Subsequent agents in the same trigger SHALL NOT execute.
12. On agent chain failure, the system SHALL post a **system comment** on the card indicating the failure: agent comment with `AgentName = "system"` and content: "Workflow trigger '{triggerName}' failed during agent '{agentName}': {errorMessage}."
13. After all agents in a trigger have executed (or the chain was interrupted), the OpenCode session SHALL be closed.

### Execution Isolation

14. Each trigger executes in its **own OpenCode session**. Multiple triggers matching the same transition run in separate sessions, sequentially (one trigger at a time, not in parallel).
15. A failure in one trigger's execution SHALL NOT prevent subsequent triggers from executing.

### Execution Context — Card Data

16. Before executing any trigger, the system SHALL load the full card entity (including title, description, priority, status names) to populate the prompt placeholders. This requires a `GetCardByIdQuery` (or equivalent) that returns the card with its current and previous status names.

### No Execution History Entity

17. This spec does NOT introduce a dedicated `TriggerExecutionLog` entity. Execution results are captured entirely through card comments (SPEC-C2) and card attachments (SPEC-C3). The comment thread on a card serves as the execution audit trail.

### Service Registration

18. `IWorkflowTriggerExecutionService` SHALL be registered in the Infrastructure layer (implementation) and Application layer (interface).
19. The implementation SHALL depend on: `IWorkflowTriggerRepository`, `ICardRepository`, `IBoardStatusRepository`, `IOpenCodeSessionService`, `IOpenCodeMessageService`, `IOpenCodeEventService`, and the DispatchR mediator (to dispatch `AddAgentCommentCommand` and `AddCardAttachmentCommand`).

## User Flow

### Happy Path — Trigger Fires on Card Move
1. User drags card `#12` from "Develop" to "Review" on the board.
2. Board calls `ChangeCardStatusCommand` → success.
3. Board calls `ExecuteTriggersAsync(cardId, developStatusId, reviewStatusId, userId)` — fire-and-forget.
4. Engine finds one matching trigger: "Code Review on Develop → Review".
5. Engine enriches the prompt: `{card.title}` → "Implement login endpoint", etc.
6. Engine creates an OpenCode session for the project.
7. Engine invokes agent `owl-reviewer` with the enriched prompt.
8. Agent streams its response (a code review document).
9. Response is > 2,000 characters → saved as attachment `owl-reviewer-output-{timestamp}.md`.
10. Response is also posted as an agent comment on the card.
11. Session is closed.
12. User opens the card detail dialog and sees the new comment and attachment.

### Error Path — Agent Fails
1. Same as above, but the OpenCode session returns an error.
2. Engine posts a system comment on the card: "Workflow trigger 'Code Review on Develop → Review' failed during agent 'owl-reviewer': Session error."
3. No attachment is created.
4. User sees the failure comment in the card detail dialog.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| No triggers match the transition | No-op; no sessions created |
| Trigger references an agent that no longer exists on disk | OpenCode session returns an error; chain interrupted; failure comment posted on card |
| OpenCode server is not running | `IOpenCodeSessionService` throws; failure comment posted on card |
| Agent response is empty | No comment or attachment is created for that agent; chain continues to next agent |
| Prompt contains unknown placeholders (e.g., `{card.foo}`) | Placeholder is left as-is in the enriched prompt (no error) |
| Card is deleted while execution is in progress | `AddAgentCommentCommand` and `AddCardAttachmentCommand` will fail with not-found; engine logs the error and stops gracefully |
| Multiple triggers match the same transition | Each trigger executes sequentially in its own session; failure of one does not block others |
| Trigger is disabled between evaluation and execution start | The trigger was already retrieved as enabled; execution proceeds (no re-check at execution time) |

## Out of Scope

- Parallel trigger execution (triggers always run sequentially).
- Dedicated `TriggerExecutionLog` entity or execution history page.
- Manual trigger invocation (triggers only fire on card status changes from the board or card detail dialog).
- Trigger retry on failure (no automatic retry).
- Conditional trigger logic (e.g., "only fire if priority is Critical").
- Real-time execution progress streaming to the UI (the board shows a static indicator; results are visible when the user opens the card detail dialog).
- Cancellation of in-progress trigger execution by the user.

## Acceptance Criteria

- [ ] `IWorkflowTriggerExecutionService` interface is defined in the Application layer.
- [ ] Implementation is registered in the Infrastructure layer.
- [ ] `ExecuteTriggersAsync` retrieves enabled triggers matching the given transition.
- [ ] Prompt placeholders (`{card.title}`, `{card.description}`, `{card.priority}`, `{card.number}`, `{card.previousStatus}`, `{card.newStatus}`) are substituted correctly.
- [ ] Unknown placeholders are left as-is without error.
- [ ] Each trigger creates a new OpenCode session scoped to the project path.
- [ ] Agents within a trigger execute sequentially in `SortOrder` order.
- [ ] Agent response is posted as an agent comment via `AddAgentCommentCommand`.
- [ ] Long responses (> 2,000 chars) or responses with frontmatter `filename:` are also saved as attachments via `AddCardAttachmentCommand`.
- [ ] Agent chain is interrupted on failure; subsequent agents in the same trigger do not execute.
- [ ] Failure posts a system comment on the card with trigger name, agent name, and error message.
- [ ] Failure of one trigger does not prevent subsequent triggers from executing.
- [ ] OpenCode session is closed after each trigger's chain completes or fails.
- [ ] Callers (board, dialog) invoke the service fire-and-forget (no await on the result).
- [ ] Empty agent responses produce no comment or attachment.
- [ ] Unit tests cover: no-match no-op, successful single agent, multi-agent chain, chain interruption on failure, prompt enrichment.

## Dependencies

- **SPEC-WF3-workflow-trigger-entity** — `WorkflowTrigger`, `WorkflowTriggerAgent`, `GetWorkflowTriggersByTransitionQuery`.
- **SPEC-WF2-board-card-entity** — `Card` entity, `ICardRepository`.
- **SPEC-C2-card-comments** — `AddAgentCommentCommand`.
- **SPEC-C3-card-attachments** — `AddCardAttachmentCommand`.
- **SPEC-004-opencode-client-core** — `IOpenCodeSessionService`, `IOpenCodeMessageService`, `IOpenCodeEventService`.
- **SPEC-C4-board-wiring** — Board invokes `ExecuteTriggersAsync` after `ChangeCardStatusCommand`.
- **SPEC-C6-card-detail-dialog** — Dialog invokes `ExecuteTriggersAsync` after status change from dropdown.

## Open Questions

1. **Prompt placeholder syntax**: The `{card.field}` placeholder syntax is defined here. Should the trigger creation UI (SPEC-WF4) be updated to document and suggest these placeholders? This would improve usability but requires a UI change to an already-implemented spec.
2. **Document classification heuristic**: The 2,000-character threshold for classifying a response as a document attachment is a heuristic. Should this threshold be configurable (e.g., via `AppSettings`)? Currently hardcoded.
3. **Fire-and-forget error visibility**: Since execution is fire-and-forget, errors are only visible via card comments. Should there also be a background error log (e.g., Serilog structured log) for operational monitoring? Recommended yes — implementation detail for the developer.
4. **Session reuse**: Should all agents within a single trigger share one session (conversation context is preserved between agents), or should each agent get a fresh session? Currently specified as one session per trigger (shared context). This is a significant design decision affecting agent behavior.
