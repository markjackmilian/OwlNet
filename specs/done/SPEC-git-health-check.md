# SPEC: Git Health Check Panel

> **Status:** Todo
> **Created:** 2026-03-06
> **Author:** owl-planner + user
> **Priority:** Medium
> **Estimated Complexity:** S

## Context

The Settings page already includes a health check panel for OpenCode that verifies its installation via CLI. Git is a critical dependency for OwlNet's workflow, but there is currently no visibility into whether it is installed on the host machine. Adding a Git health check panel gives users immediate feedback about Git availability, following the same pattern already established for OpenCode.

## Actors

- **User**: any authenticated user accessing the Settings page.

## Functional Requirements

1. The system SHALL execute `git --version` via `ICliService` on panel initialization to determine if Git is installed.
2. The system SHALL use a 10-second timeout for the CLI command execution.
3. When the command succeeds (exit code 0), the system SHALL extract the version string from the command output and display it to the user.
4. When the command fails (non-zero exit code or timeout), the system SHALL display a "Not installed" status to the user.
5. The system SHALL display a link to `https://git-scm.com` when Git is not installed, so the user can navigate to the official download page.
6. The system SHALL NOT attempt to install Git automatically under any circumstance.
7. The system SHALL cancel any in-flight CLI call when the component is disposed (e.g., user navigates away).

## User Flow

1. User navigates to the Settings page (`/settings`).
2. The Git Health Check panel loads and shows a "Checking..." state with a progress indicator.
3. The system executes `git --version` via `ICliService`.
4. **If Git is installed**: the panel displays a success chip (green) with the detected version (e.g., "Installed — v2.43.0").
5. **If Git is not installed**: the panel displays an error chip (red) with "Not installed" and a link to `https://git-scm.com`.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| `git --version` times out (>10s) | Treat as not installed; show "Not installed" status with link |
| `git --version` returns exit code 0 but empty output | Show success chip with "Installed" without version detail |
| User navigates away while check is in progress | Cancel the CLI call via CancellationToken; no error shown |
| `git` command not found (Win32Exception / command not on PATH) | Treat as not installed; show "Not installed" status with link |

## Out of Scope

- Automatic installation of Git.
- Minimum version validation.
- Git configuration checks (e.g., user.name, user.email).
- Any interaction with Git repositories or Git operations.

## Acceptance Criteria

- [ ] A `GitHealthCheckPanel` component exists in `Components/Settings/`.
- [ ] The panel is rendered on the Settings page alongside the existing OpenCode panel.
- [ ] When Git is installed, a green success chip displays the version string.
- [ ] When Git is not installed, a red error chip displays "Not installed" with a clickable link to `https://git-scm.com`.
- [ ] The CLI call uses a 10-second timeout.
- [ ] The component implements `IDisposable` and cancels in-flight CLI calls on disposal.
- [ ] The panel follows the same visual structure as `OpenCodeHealthCheckPanel` (MudCard with avatar, title, subtitle, chip-based status).

## Dependencies

- `ICliService` (Application layer) — already exists.
- `OpenCodeHealthCheckPanel` — used as reference pattern (no code dependency).

## Open Questions

None.
