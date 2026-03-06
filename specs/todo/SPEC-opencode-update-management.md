# SPEC: OpenCode Update Management

> **Status:** Todo
> **Created:** 2026-03-06
> **Author:** owl-planner + user
> **Priority:** Medium
> **Estimated Complexity:** S

## Context

The OpenCode Health Check panel in Settings currently detects whether OpenCode is installed and allows the user to install it via NPM. However, there is no way for the user to know if a newer version of OpenCode is available or to trigger an update from the UI. Users must manually check and update via the terminal. This spec adds update detection and one-click update capability to the existing panel, helping users keep OpenCode up to date.

## Actors

- **User**: Any authenticated user accessing the Settings page.

## Functional Requirements

1. The system SHALL display a "Check for updates" button when OpenCode is in the `Installed` state.
2. When the user clicks "Check for updates", the system SHALL retrieve the latest published version of OpenCode by running `npm view opencode-ai version` with a 15-second timeout.
3. The system SHALL compare the installed version (from `opencode --version`) with the latest published version from NPM.
4. If the installed version matches the latest version, the system SHALL display a green chip with the text "Installed — v{version} (latest)".
5. If the installed version is older than the latest version, the system SHALL display a `Color.Warning` (amber) chip with the text "Installed — v{installedVersion} → v{latestVersion} available".
6. If the installed version is older than the latest version, the system SHALL display an "Update" button next to or below the version chip.
7. When the user clicks "Update", the system SHALL run `npm i -g opencode-ai@latest` with a 120-second timeout.
8. While the update is in progress, the system SHALL show a spinner with the text "Updating OpenCode..." and disable the "Update" button.
9. After a successful update, the system SHALL re-run `opencode --version` to verify the new installed version, update the displayed version, and show a success snackbar "OpenCode updated successfully!".
10. If the update command fails, the system SHALL display a red alert with the error output, manual fallback instructions (`npm i -g opencode-ai@latest`), and a "Retry" button.
11. If the `npm view` command fails (network error, timeout, NPM not available), the system SHALL display an inline warning message "Unable to check for updates. Check your internet connection." and a "Try Again" button. The existing installed version chip SHALL remain visible and green.
12. The "Check for updates" button SHALL NOT be visible when OpenCode is not installed (i.e., only visible in the `Installed` state or after a successful update check).

## User Flow

### Happy Path — Update Available

1. User navigates to `/settings`.
2. System detects OpenCode is installed, displays green chip "Installed — v1.2.3".
3. User clicks "Check for updates".
4. System shows a loading indicator while querying NPM.
5. System finds a newer version (v1.3.0), displays amber chip "Installed — v1.2.3 → v1.3.0 available" and an "Update" button.
6. User clicks "Update".
7. System shows spinner with "Updating OpenCode..." and disables the button.
8. Update completes successfully.
9. System re-checks the installed version, displays green chip "Installed — v1.3.0 (latest)", and shows a success snackbar.

### Happy Path — Already Up to Date

1. User navigates to `/settings`.
2. System detects OpenCode is installed, displays green chip "Installed — v1.3.0".
3. User clicks "Check for updates".
4. System queries NPM, finds the same version.
5. System displays green chip "Installed — v1.3.0 (latest)". No "Update" button is shown.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| NPM not reachable (network error) | Warning message "Unable to check for updates. Check your internet connection." + "Try Again" button. Installed chip remains green. |
| `npm view` times out (>15s) | Same as network error behavior. |
| `npm i -g opencode-ai@latest` fails | Red alert with error output + manual instructions + "Retry" button. |
| `npm i -g opencode-ai@latest` times out (>120s) | Red alert with timeout message + manual instructions + "Retry" button. |
| User navigates away during update check or update | CancellationToken from existing CTS cancels in-flight CLI calls (existing behavior). |
| Version string format mismatch (unexpected output from `npm view`) | Treat as update check failure, show warning message. |
| OpenCode not installed | "Check for updates" button is not rendered. Only install flow is available (existing behavior). |

## Out of Scope

- Automatic/periodic update checks (background polling, scheduled checks).
- Displaying release notes or changelogs.
- Updating other tools or packages (only OpenCode).
- Changes to the LLM Provider Config panel or other Settings sections.
- Persisting update check results to the database.
- Notification badges or indicators outside the Settings page.

## Acceptance Criteria

- [ ] "Check for updates" button is visible only when OpenCode is installed.
- [ ] Clicking "Check for updates" runs `npm view opencode-ai version` and compares with the installed version.
- [ ] When up to date, chip shows green "Installed — v{version} (latest)".
- [ ] When outdated, chip shows amber "Installed — v{installed} → v{latest} available" with an "Update" button.
- [ ] Clicking "Update" runs `npm i -g opencode-ai@latest` with loading state and disabled button.
- [ ] Successful update re-checks version, shows green chip with new version, and displays success snackbar.
- [ ] Failed update shows red alert with error details, manual instructions, and retry option.
- [ ] Network/timeout errors during update check show warning message with retry option without losing the installed status.
- [ ] Navigation away during any async operation cancels in-flight CLI calls cleanly.

## Dependencies

- **SPEC-opencode-health-check** (done) — This spec extends the existing panel built from that spec.
- `ICliService` — Existing CLI execution service used for all commands.

## Open Questions

None — all details resolved during brainstorming.
