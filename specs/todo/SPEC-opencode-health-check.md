# SPEC: OpenCode Health Check

> **Status:** Todo
> **Created:** 2026-03-05
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

OwlNet depends on OpenCode (CLI tool) to function correctly. The user must be able to verify from the Settings page whether OpenCode is installed on the machine and, if not, initiate the installation directly from the interface. OpenCode is installed globally via NPM with `npm i -g opencode-ai` and its presence is verified with `opencode --version`.

## Actors

- **User** — any application user.
- **Host operating system** — the machine running OwlNet (CLI command execution).

## Functional Requirements

1. The system SHALL display a dedicated "OpenCode" section in the Settings page as a MudBlazor card.
2. The system SHALL automatically verify the presence of OpenCode when the section loads, by executing the command `opencode --version` on the host machine.
3. If OpenCode is installed, the system SHALL display a **green** status badge/chip with the label "Installed" and the detected version (the text returned by `opencode --version`).
4. If OpenCode is NOT installed (command not found or execution error), the system SHALL display a **red** status badge/chip with the label "Not installed".
5. When OpenCode is not installed, the system SHALL display an "Install via NPM" button that executes the command `npm i -g opencode-ai` on the host machine.
6. Before executing the installation, the system SHALL verify that NPM is available on the machine (by executing `npm --version`).
7. If NPM is not available, the system SHALL display an error message and a textual guide with instructions to install Node.js/NPM manually (link to https://nodejs.org/).
8. During the NPM installation, the system SHALL show a progress indicator (spinner) and disable the install button.
9. If the NPM installation succeeds, the system SHALL automatically refresh the status by re-running `opencode --version` and display the green badge with the version.
10. If the NPM installation fails, the system SHALL display an error message with the command output and a manual installation guide (textual instructions: "Open a terminal and run: `npm i -g opencode-ai`").
11. The system SHALL expose a service in the Infrastructure layer for executing CLI commands on the host machine, with an interface defined in the Application layer.
12. The system SHALL enforce a timeout for CLI command execution (maximum 120 seconds for installation, 10 seconds for version check).

## User Flow

### Happy Path — OpenCode already installed
1. The user opens the Settings page.
2. The OpenCode section shows a verification spinner.
3. The system executes `opencode --version`, receives a response (e.g., "0.1.53").
4. The card displays a green badge "Installed — v0.1.53".

### Happy Path — Successful installation
1. The user opens the Settings page.
2. The OpenCode section shows a red badge "Not installed" and an "Install via NPM" button.
3. The user clicks "Install via NPM".
4. The system verifies the presence of NPM (ok), then executes `npm i -g opencode-ai`.
5. Spinner active during installation.
6. Installation completed. The system executes `opencode --version`.
7. The card updates: green badge "Installed — v0.1.53". Confirmation snackbar.

### Fallback — NPM not available
1. The user clicks "Install via NPM".
2. The system checks for NPM: not found.
3. The card shows an error alert: "NPM not found. Install Node.js from https://nodejs.org/ and try again."

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| `opencode --version` times out (>10s) | Treat as "Not installed", show warning |
| `npm i -g opencode-ai` times out (>120s) | Show error "Installation timed out. Try again or install manually." |
| `npm i -g opencode-ai` fails with permission error | Show the error output and suggest running the command manually with elevated privileges |
| `opencode --version` returns unexpected output | Display the raw text as the version (no sophisticated parsing) |
| CLI command cannot be executed (e.g., `System.ComponentModel.Win32Exception`) | Show generic error "Unable to execute the command. Verify that the terminal is accessible." |

## Out of Scope

- Updating OpenCode to a newer version.
- Verifying a minimum OpenCode version.
- Uninstalling OpenCode.
- Configuring OpenCode (config files, CLI parameters).

## Acceptance Criteria

- [ ] The OpenCode section is visible in the Settings page as a dedicated card.
- [ ] On page load, the OpenCode status is verified automatically.
- [ ] If installed: green badge with version. If not installed: red badge with install button.
- [ ] The "Install via NPM" button first checks for NPM presence.
- [ ] If NPM is absent: error message with link to nodejs.org.
- [ ] During installation: spinner active, button disabled.
- [ ] Successful installation: automatic status refresh with green badge.
- [ ] Failed installation: error message with output and manual guide.
- [ ] Timeouts are enforced for both commands (10s version check, 120s installation).
- [ ] The CLI service is defined as an interface in the Application layer and implemented in the Infrastructure layer.
- [ ] Unit tests cover the CLI service (process mocking) and main scenarios.

## Dependencies

- SPEC-settings-page (the section lives within the Settings page).

## Open Questions

- None.
