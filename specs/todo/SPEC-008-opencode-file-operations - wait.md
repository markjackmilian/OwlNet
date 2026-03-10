# SPEC-008: OpenCode File Operations

> **Status:** Todo
> **Created:** 2026-03-07
> **Author:** owl-planner + user
> **Priority:** Medium
> **Estimated Complexity:** M

## Context

OpenCode Server provides file-related APIs that allow searching for text in files, finding files by name, reading file contents, and checking file status (tracked/modified). OwlNet needs to expose these capabilities to allow users and backend services to explore and inspect the project codebase managed by the OpenCode Server, without direct filesystem access.

These operations are scoped to the project directory that the OpenCode Server is operating on, which is linked to the OwlNet `Project` entity.

## Actors

- **User** — searches and browses project files through the OwlNet Blazor UI.
- **Backend services** — DispatchR handlers that need to inspect files as part of automated workflows.
- **OpenCode Server** — provides the file REST API.

## Functional Requirements

1. The system SHALL define an `IOpenCodeFileService` interface in the Application layer with methods for file operations.
2. The system SHALL implement `IOpenCodeFileService` in the Infrastructure layer using the OpenCode HTTP client.
3. The system SHALL support **searching for text in files** by calling `GET /find?pattern=<pat>`, returning a collection of match results with file path, line number, and matched content.
4. The system SHALL support **finding files by name** by calling `GET /find/file?query=<q>`, returning a collection of file paths. Optional parameters: `type` (file/directory), `limit` (max results, 1-200).
5. The system SHALL support **finding workspace symbols** by calling `GET /find/symbol?query=<q>`, returning a collection of symbol results.
6. The system SHALL support **listing files and directories** by calling `GET /file?path=<path>`, returning a collection of file node entries.
7. The system SHALL support **reading a file's content** by calling `GET /file/content?path=<p>`, returning the file content (type: raw or patch, plus content string).
8. The system SHALL support **getting file status** (tracked/modified files) by calling `GET /file/status`, returning a collection of file status entries.
9. The system SHALL define the following DTOs (records) in the Application layer:
   - `FileSearchResultDto` — text search match (path, line number, matched lines, submatches).
   - `FileNodeDto` — file/directory entry (name, path, type).
   - `FileContentDto` — file content (type: raw/patch, content string).
   - `FileStatusDto` — file tracking status.
   - `SymbolDto` — workspace symbol result.
10. All file operations SHALL propagate `CancellationToken` and return `Result<T>` values.
11. All file operations SHALL log structured information about the operation (search pattern, file path, result count).
12. The system SHALL enforce a reasonable timeout for file operations (default: 30 seconds).

## User Flow

### Happy Path — Search for text in project files
1. The user enters a search pattern (e.g., "IOpenCodeClient") in a search field.
2. The system calls `GET /find?pattern=IOpenCodeClient`.
3. The results are displayed: file paths, line numbers, and matched content snippets.

### Happy Path — Browse project files
1. The user navigates the file tree starting from the project root.
2. The system calls `GET /file?path=/` to list root entries.
3. The user clicks a directory to expand it (another `GET /file?path=<dir>`).
4. The user clicks a file to view its content (`GET /file/content?path=<file>`).

### Happy Path — Check modified files
1. The user wants to see which files have been modified.
2. The system calls `GET /file/status`.
3. The UI displays a list of modified/added/deleted files.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| OpenCode Server is not running | Return failure Result. UI shows connection error. |
| Search pattern returns no results | Return empty collection. UI shows "No results found". |
| File path does not exist | Return failure Result. UI shows "File not found". |
| Search pattern is empty | Validate at service level. Return failure Result "Search pattern is required". |
| File content is very large | Return the full content (server handles any limits). Log the content size. |
| Binary file requested | Return whatever the server returns (may be raw content or error). |
| Search returns many results (>1000) | Return all results. UI can paginate client-side. |
| Path traversal attempt (e.g., `../../etc/passwd`) | Pass to server as-is (server enforces its own security boundaries). |

## Out of Scope

- File editing or writing (OpenCode handles file modifications through AI agent tool calls).
- Diff operations (`/session/:id/diff`) — related to sessions, not standalone file ops.
- File watching or real-time file change notifications.
- UI components for file browsing (will be a separate UI spec).
- Git operations beyond file status.

## Acceptance Criteria

- [ ] `IOpenCodeFileService` interface is defined in the Application layer.
- [ ] Implementation calls the correct OpenCode Server endpoints in the Infrastructure layer.
- [ ] All DTOs (`FileSearchResultDto`, `FileNodeDto`, `FileContentDto`, `FileStatusDto`, `SymbolDto`) are defined as records.
- [ ] Text search calls `GET /find?pattern=<pat>` and returns structured results.
- [ ] File find calls `GET /find/file?query=<q>` with optional type and limit parameters.
- [ ] Symbol find calls `GET /find/symbol?query=<q>`.
- [ ] File listing calls `GET /file?path=<path>`.
- [ ] File content reading calls `GET /file/content?path=<p>`.
- [ ] File status calls `GET /file/status`.
- [ ] All methods propagate `CancellationToken` and return `Result<T>`.
- [ ] Structured logging for all operations.
- [ ] Unit tests cover: text search, file find, file listing, file content, file status, error scenarios.

## Dependencies

- SPEC-004 (provides HTTP client infrastructure and connection configuration).

## Open Questions

- None.
