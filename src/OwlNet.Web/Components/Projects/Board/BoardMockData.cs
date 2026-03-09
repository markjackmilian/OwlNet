namespace OwlNet.Web.Components.Projects.Board;

/// <summary>
/// Provides static mock data for the project Kanban board.
/// Returns a fresh list on each call so the board resets on page refresh.
/// </summary>
public static class BoardMockData
{
    /// <summary>
    /// Creates a new list of <see cref="BoardCardItem"/> instances representing a realistic
    /// software project board. Each call returns a fresh list so drag-and-drop mutations
    /// do not persist across page refreshes.
    /// </summary>
    /// <returns>A new list containing at least 15 cards distributed across all five columns.</returns>
    public static List<BoardCardItem> CreateCards() =>
    [
        // ── Backlog (4 cards) ──────────────────────────────────────────────

        new BoardCardItem
        {
            Id = 1,
            Title = "Add CSV export to the reports dashboard",
            Description = """
                Allow users to **download report data** as a `.csv` file.

                - Add an *Export* button to the toolbar
                - Support filtering by `date range` before export
                - Include column headers matching the grid display
                """,
            Priority = CardPriority.Medium,
            Status = BoardStatus.Backlog
        },
        new BoardCardItem
        {
            Id = 2,
            Title = "Investigate slow query on project listing endpoint",
            Description = """
                The `GET /api/projects` endpoint takes **over 3 seconds** when the user has 50+ projects.

                - Profile the EF Core query with *SQL Server Profiler*
                - Check for missing indexes on `ProjectMember.UserId`
                - Consider adding `AsNoTracking()` for read-only paths
                """,
            Priority = CardPriority.High,
            Status = BoardStatus.Backlog
        },
        new BoardCardItem
        {
            Id = 3,
            Title = "Design onboarding wizard for new team members",
            Description = """
                New users are confused by the initial setup flow. We need a **step-by-step wizard** that covers:

                - Creating their *profile* and uploading an avatar
                - Joining or creating a `project`
                - Understanding the Kanban board workflow
                """,
            Priority = CardPriority.Low,
            Status = BoardStatus.Backlog
        },
        new BoardCardItem
        {
            Id = 4,
            Title = "Upgrade MudBlazor to latest stable release",
            Description = """
                We are **two minor versions behind** on MudBlazor. The latest release includes:

                - Bug fixes for `MudDataGrid` server-side paging
                - New *MudTimeline* component we could use for activity feeds
                - Performance improvements in `MudAutocomplete`
                """,
            Priority = CardPriority.Low,
            Status = BoardStatus.Backlog
        },

        // ── To Evaluate (3 cards) ─────────────────────────────────────────

        new BoardCardItem
        {
            Id = 5,
            Title = "Evaluate real-time notifications via SignalR",
            Description = """
                Product wants **live notifications** when a card is moved or a comment is added.

                - Spike a `SignalR` hub for board change events
                - Measure *latency* and connection overhead
                - Determine if we need a dedicated notification service
                """,
            Priority = CardPriority.Medium,
            Status = BoardStatus.ToEvaluate
        },
        new BoardCardItem
        {
            Id = 6,
            Title = "Assess accessibility compliance for WCAG 2.1 AA",
            Description = """
                Run an **accessibility audit** on all primary user flows.

                - Use `axe-core` browser extension for automated checks
                - Manually test *keyboard navigation* through the Kanban board
                - Document any color contrast failures in the current theme
                """,
            Priority = CardPriority.High,
            Status = BoardStatus.ToEvaluate
        },
        new BoardCardItem
        {
            Id = 7,
            Title = "Spike: Replace polling with server-sent events for dashboard",
            Description = """
                The dashboard currently polls every `10 seconds` for updated metrics. Evaluate whether **server-sent events** (SSE) would reduce server load.

                - Compare *bandwidth usage* between polling and SSE
                - Check browser compatibility requirements
                - Prototype with a single metric widget
                """,
            Priority = CardPriority.Low,
            Status = BoardStatus.ToEvaluate
        },

        // ── Develop (4 cards) ──────────────────────────────────────────────

        new BoardCardItem
        {
            Id = 8,
            Title = "Fix authentication timeout on mobile devices",
            Description = """
                Users on **iOS Safari** report being logged out after `5 minutes` of inactivity.

                - Investigate token refresh logic in `AuthStateProvider`
                - Check *session storage* behavior on mobile browsers
                - Add automatic retry mechanism for expired tokens
                """,
            Priority = CardPriority.Critical,
            Status = BoardStatus.Develop
        },
        new BoardCardItem
        {
            Id = 9,
            Title = "Implement drag-and-drop card reordering within columns",
            Description = """
                Cards within the same column should be **reorderable** via drag-and-drop.

                - Use `MudDropContainer` with *sortable* zones
                - Persist the `SortOrder` property on each card
                - Animate the card movement with a smooth CSS transition
                """,
            Priority = CardPriority.High,
            Status = BoardStatus.Develop
        },
        new BoardCardItem
        {
            Id = 10,
            Title = "Build user avatar component with fallback initials",
            Description = """
                Create a reusable **avatar component** that displays:

                - The user's *profile image* when available
                - Colored `initials` as a fallback (e.g., **JD** for Jane Doe)
                - A generic icon for users with no name set
                """,
            Priority = CardPriority.Medium,
            Status = BoardStatus.Develop
        },
        new BoardCardItem
        {
            Id = 11,
            Title = "Add dark mode toggle to the application header",
            Description = """
                Implement a **theme toggle** button in the `MudAppBar`.

                - Persist the user's preference in *local storage*
                - Respect the system `prefers-color-scheme` on first visit
                - Ensure all custom components adapt to both palettes
                """,
            Priority = CardPriority.Medium,
            Status = BoardStatus.Develop
        },

        // ── Review (2 cards) ───────────────────────────────────────────────

        new BoardCardItem
        {
            Id = 12,
            Title = "Refactor validation pipeline to use FluentValidation",
            Description = """
                Replace manual `if` checks in command handlers with **FluentValidation** rules.

                - Create `AbstractValidator<T>` for each command in `Application/`
                - Wire up the `ValidationBehavior` pipeline in *DispatchR*
                - Remove inline validation from `Handle()` methods
                """,
            Priority = CardPriority.High,
            Status = BoardStatus.Review
        },
        new BoardCardItem
        {
            Id = 13,
            Title = "Add unit tests for project creation workflow",
            Description = """
                The `CreateProjectCommandHandler` has **zero test coverage**. Add tests for:

                - Happy path: valid command returns `ProjectDto`
                - Validation failure: missing `Name` throws *ValidationException*
                - Duplicate detection: existing project name returns `Conflict` result
                - Edge case: `Description` at max length (`500 chars`)
                """,
            Priority = CardPriority.Medium,
            Status = BoardStatus.Review
        },

        // ── Done (3 cards) ─────────────────────────────────────────────────

        new BoardCardItem
        {
            Id = 14,
            Title = "Set up CI pipeline with GitHub Actions",
            Description = """
                Configured a **GitHub Actions** workflow that runs on every PR:

                - `dotnet build` with *TreatWarningsAsErrors* enabled
                - `dotnet test` with code coverage collection
                - Artifact upload for the `coverage.cobertura.xml` report
                """,
            Priority = CardPriority.High,
            Status = BoardStatus.Done
        },
        new BoardCardItem
        {
            Id = 15,
            Title = "Configure Serilog structured logging",
            Description = """
                Replaced the default **Microsoft.Extensions.Logging** with *Serilog*.

                - Added `Console` and `File` sinks in `appsettings.json`
                - Configured `RequestLoggingMiddleware` for HTTP request logs
                - Enriched logs with `CorrelationId` from the `X-Correlation-ID` header
                """,
            Priority = CardPriority.Medium,
            Status = BoardStatus.Done
        },
        new BoardCardItem
        {
            Id = 16,
            Title = "Fix broken navigation after logout redirect",
            Description = """
                After logging out, users were stuck on a **blank page** because the `NavigationManager` tried to redirect to a *protected route*.

                - Updated `LogoutAsync()` to navigate to `/login` explicitly
                - Added a `RedirectToLogin` component for unauthenticated access
                - Verified the fix on `Chrome`, `Firefox`, and *Edge*
                """,
            Priority = CardPriority.Critical,
            Status = BoardStatus.Done
        }
    ];
}
