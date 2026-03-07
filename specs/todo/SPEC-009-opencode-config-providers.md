# SPEC-009: OpenCode Config and Providers

> **Status:** Todo
> **Created:** 2026-03-07
> **Author:** owl-planner + user
> **Priority:** Medium
> **Estimated Complexity:** M

## Context

OpenCode Server exposes APIs to read and update its configuration, and to manage LLM providers (list available providers, check connected providers, manage authentication). OwlNet needs to interact with these APIs to allow users to view the OpenCode Server configuration, see which AI providers are available and connected, and potentially manage provider authentication — all from within the OwlNet UI.

This is complementary to the existing `ILlmProviderService` in OwlNet (which manages OpenRouter configuration). This spec covers the OpenCode Server's own provider and config management, which is a separate concern.

## Actors

- **User** — views and manages OpenCode Server configuration and providers from the OwlNet Settings page.
- **Backend services** — query provider information to determine available models for prompt requests.
- **OpenCode Server** — provides the config and provider REST APIs.

## Functional Requirements

1. The system SHALL define an `IOpenCodeConfigService` interface in the Application layer with methods for configuration and provider management.
2. The system SHALL implement `IOpenCodeConfigService` in the Infrastructure layer using the OpenCode HTTP client.
3. The system SHALL support **getting the current configuration** by calling `GET /config`, returning a `OpenCodeConfigDto` record.
4. The system SHALL support **listing available providers** by calling `GET /provider`, returning a collection of provider information including: all providers, default models, and connected provider IDs.
5. The system SHALL support **getting provider authentication methods** by calling `GET /provider/auth`, returning a dictionary of provider IDs to their supported authentication methods.
6. The system SHALL support **listing available agents** by calling `GET /agent`, returning a collection of `OpenCodeAgentDto` records (agent ID, name, description).
7. The system SHALL support **listing available commands** by calling `GET /command`, returning a collection of `OpenCodeCommandDto` records.
8. The system SHALL define the following DTOs (records) in the Application layer:
   - `OpenCodeConfigDto` — server configuration summary.
   - `OpenCodeProviderDto` — provider information (ID, name, models).
   - `OpenCodeProviderSummaryDto` — aggregated provider info (all providers, defaults, connected list).
   - `OpenCodeAgentDto` — agent information (ID, name, description).
   - `OpenCodeCommandDto` — command information (name, description, arguments).
   - `OpenCodeProviderAuthMethodDto` — authentication method for a provider.
9. All operations SHALL propagate `CancellationToken` and return `Result<T>` values.
10. All operations SHALL log structured information about the operation.
11. The system SHALL enforce a standard timeout for config/provider calls (default: 30 seconds).

## User Flow

### Happy Path — View available providers
1. The user opens the Settings page or a provider configuration panel.
2. The system calls `GET /provider` to load provider information.
3. The UI displays: list of available providers, which ones are connected (have valid credentials), and default models.

### Happy Path — View available agents
1. The user opens a session creation dialog or agent selector.
2. The system calls `GET /agent` to load available agents.
3. The UI displays agent options (e.g., "build", "plan") with descriptions.

### Happy Path — View server configuration
1. The user opens the Settings page.
2. The system calls `GET /config` to load the current OpenCode Server configuration.
3. The UI displays relevant configuration details (model, provider, etc.).

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| OpenCode Server is not running | Return failure Result. UI shows connection error. |
| No providers are connected | Return the full provider list with empty connected list. UI shows "No providers connected" warning. |
| Unknown provider ID in response | Include in the DTO as-is. Do not filter unknown providers. |
| Config endpoint returns unexpected format | Log warning, return failure Result. |
| Agent list is empty | Return empty collection. UI shows "No agents available". |

## Out of Scope

- Updating OpenCode Server configuration from OwlNet (`PATCH /config`). Read-only for this phase.
- Managing provider OAuth flows (`/provider/{id}/oauth/authorize` and `/provider/{id}/oauth/callback`).
- Setting provider authentication credentials (`PUT /auth/:id`).
- MCP server management (`GET /mcp`, `POST /mcp`).
- LSP and formatter status.
- UI components for provider/config management (will be a separate UI spec).

## Acceptance Criteria

- [ ] `IOpenCodeConfigService` interface is defined in the Application layer.
- [ ] Implementation calls the correct OpenCode Server endpoints in the Infrastructure layer.
- [ ] All DTOs are defined as records in the Application layer.
- [ ] Get config calls `GET /config` and returns `Result<OpenCodeConfigDto>`.
- [ ] List providers calls `GET /provider` and returns `Result<OpenCodeProviderSummaryDto>`.
- [ ] Get provider auth methods calls `GET /provider/auth` and returns `Result<IReadOnlyDictionary<string, IReadOnlyList<OpenCodeProviderAuthMethodDto>>>`.
- [ ] List agents calls `GET /agent` and returns `Result<IReadOnlyList<OpenCodeAgentDto>>`.
- [ ] List commands calls `GET /command` and returns `Result<IReadOnlyList<OpenCodeCommandDto>>`.
- [ ] All methods propagate `CancellationToken` and return `Result<T>`.
- [ ] Structured logging for all operations.
- [ ] Unit tests cover: get config, list providers, list agents, list commands, error scenarios.

## Dependencies

- SPEC-004 (provides HTTP client infrastructure and connection configuration).

## Open Questions

- None.
