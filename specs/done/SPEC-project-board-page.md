# SPEC: Project Board Page

> **Status:** Done
> **Created:** 2026-03-09
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

The project navigation shell (SPEC-project-scoped-navigation-shell) introduces a "Board" link in the sidebar pointing to `/projects/{projectId}/board`, currently a placeholder page. This spec defines the actual Board page: a Kanban-style drag-and-drop board with five fixed columns representing the project workflow stages.

The board uses MudBlazor's `MudDropContainer<T>` / `MudDropZone<T>` components for drag-and-drop functionality. All data is static mock data for now — the backend entities, persistence, and real data loading will be defined in future specs.

The existing Home page (`/`) contains a demo Kanban board with 3 columns. The project Board is a separate, project-scoped page with a different column structure and workflow. The Home page remains as-is (project dashboard info will be detailed separately).

## Actors

- **Authenticated User** — Any logged-in user with an active project selected.

## Functional Requirements

### Page & Routing

1. The Board page SHALL be accessible at the route `/projects/{projectId:guid}/board`.
2. The page SHALL validate that the `projectId` corresponds to an existing, non-archived project. If not, it SHALL display a "Project not found" message consistent with SPEC-002.
3. The page SHALL set the project as the active project in the `ActiveProjectService` if navigated to directly (e.g., bookmark, shared link).
4. The page title SHALL be "Board — {ProjectName} — OwlNet".

### Kanban Board — Columns

5. The board SHALL display exactly five columns, in this fixed order:
   - **Backlog** — items not yet scheduled for evaluation
   - **To Evaluate** — items pending triage or estimation
   - **Develop** — items actively being worked on
   - **Review** — items awaiting review or approval
   - **Done** — completed items
6. Each column SHALL display a header containing:
   - A distinct color indicator (colored bar or background) unique to each column
   - The column name
   - A live count of cards currently in the column
   - An "Add card" icon button (non-functional in this version, visual only)
7. The five columns SHALL be rendered using `MudDropZone<T>` components inside a `MudDropContainer<T>`.
8. Columns SHALL be laid out horizontally with equal flex distribution. On narrow viewports, columns SHALL scroll horizontally.

### Kanban Board — Cards

9. Each card SHALL display the following information:
   - **Priority badge** — a colored pill/chip indicating the card's priority level (e.g., Critical, High, Medium, Low)
   - **Title** — prominent, bold text (max 2 lines, truncated with ellipsis)
   - **Description** — rendered as Markdown, visually clamped to 2-3 lines with overflow hidden
10. Cards SHALL be draggable between columns using MudBlazor's drag-and-drop system.
11. Dropping a card into a different column SHALL update the card's status to match the target column.
12. Cards SHALL support reordering within the same column (`AllowReorder="true"`).
13. Cards SHALL provide visual feedback during drag operations:
    - The dragged card SHALL appear semi-transparent at its original position
    - The target drop zone SHALL show a visual highlight when a card hovers over it
    - The card being dragged SHALL have a subtle rotation and shadow effect
14. Cards SHALL be clickable. Clicking a card SHALL open a modal dialog displaying a placeholder message: "Card Detail — Coming soon" along with the card's title.

### Card Priority Levels

15. The board SHALL support the following priority levels, each with a distinct visual color:
    - **Critical** — red tones
    - **High** — orange tones
    - **Medium** — blue tones
    - **Low** — green/neutral tones

### Mock Data

16. The board SHALL be populated with static mock data: at least 12-15 cards distributed across all five columns, with a realistic mix of priorities.
17. Mock card titles and descriptions SHALL be realistic and varied (not placeholder "Lorem ipsum" text). Descriptions SHALL include basic Markdown formatting (bold, lists, inline code) to validate the Markdown rendering.

### Markdown Rendering

18. Card descriptions SHALL render Markdown to HTML for display. The rendered output SHALL support at minimum: bold, italic, inline code, and unordered lists.
19. The Markdown rendering in the card view SHALL be truncated/clamped visually (CSS line-clamp) to prevent cards from becoming excessively tall.

### UI Polish

20. Cards SHALL have smooth hover effects (subtle elevation change or border highlight).
21. The board SHALL have a minimum height to prevent visual collapse when columns are empty.
22. Empty columns SHALL display a subtle placeholder area indicating where cards can be dropped (e.g., a dashed border or muted text "Drop cards here").
23. The "Add card" button in each column header SHALL be visually present but non-functional. It SHALL show a tooltip "Coming soon" on hover.
24. The board layout SHALL occupy the full available width of the content area.

### Loading State

25. The board SHALL display a loading skeleton while the project data is being validated on initial load. The skeleton SHALL mimic the column layout with placeholder card shapes.

## User Flow

### Happy Path — View and Interact with Board

1. User selects a project and clicks "Board" in the sidebar.
2. Page loads with a brief skeleton state.
3. Board renders with 5 columns, each containing mock cards.
4. User drags a card from "Backlog" to "To Evaluate".
5. Card moves to the new column. Column counts update immediately.
6. User clicks on a card.
7. A modal opens showing "Card Detail — Coming soon" with the card title.
8. User closes the modal.

### Happy Path — Direct Navigation

1. User navigates directly to `/projects/{projectId}/board` via bookmark.
2. Project is set as active in `ActiveProjectService`.
3. Sidebar appears with "Board" highlighted.
4. Board renders normally.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| URL contains a valid GUID but project does not exist | "Project not found" message with CTA to select another project |
| URL contains a valid GUID but project is archived | "Project not found" message (archived = not found) |
| URL contains an invalid GUID format | Blazor routing returns 404 / NotFound page |
| User drags card to the same column | Card stays in place, no status change, no error |
| Column has zero cards | Empty drop zone with placeholder visual (dashed border or hint text) |
| Browser window is narrow (< 960px) | Columns scroll horizontally, no wrapping |
| Card description contains very long Markdown | Rendered output is visually clamped; card height remains consistent |
| User clicks "Add card" button | Tooltip shows "Coming soon"; no action taken |

## Out of Scope

- Backend entities, persistence, or API for cards/board data (future spec).
- Real data loading — all data is static mock.
- Card detail page or functional card detail modal (future spec).
- Functional "Add card" capability (future spec).
- Filtering, sorting, or search on the board (future spec).
- Column customization (adding, removing, renaming, reordering columns).
- Card assignments, due dates, labels, attachments, or comments.
- Board header with filters/tabs (future spec — board is "pure" for now).
- Swimlanes or grouping within columns.
- Persistence of card positions or column state across page refreshes (mock data resets).

## Acceptance Criteria

- [ ] Board page is accessible at `/projects/{projectId}/board`.
- [ ] Page validates project ID and shows "Project not found" for invalid/archived projects.
- [ ] Page sets active project when navigated to directly.
- [ ] Five columns render in correct order: Backlog, To Evaluate, Develop, Review, Done.
- [ ] Each column header shows column name, card count, color indicator, and "Add card" button.
- [ ] Cards display priority badge, title, and Markdown-rendered description.
- [ ] Cards are draggable between columns; dropping updates the card's column status.
- [ ] Cards are reorderable within the same column.
- [ ] Drag-and-drop provides visual feedback (opacity, highlight, rotation).
- [ ] Clicking a card opens a placeholder modal with card title and "Coming soon" message.
- [ ] "Add card" button shows "Coming soon" tooltip and is non-functional.
- [ ] Empty columns show a drop placeholder visual.
- [ ] Board has at least 12-15 mock cards with realistic content and mixed priorities.
- [ ] Mock card descriptions include Markdown formatting that renders correctly.
- [ ] Markdown in card descriptions is visually clamped (line-clamp).
- [ ] Loading skeleton displays during initial project validation.
- [ ] Board is responsive: horizontal scroll on narrow viewports.
- [ ] Card hover effects are smooth and polished.
- [ ] Board is a self-contained Blazor component (not inline in the page).

## Dependencies

- **SPEC-project-scoped-navigation-shell** — Sidebar "Board" link and route setup.
- **SPEC-001-project-crud** — `ActiveProjectService`, project validation.
- **SPEC-002-project-dashboard** — Consistent "Project not found" pattern.
- MudBlazor `MudDropContainer<T>` / `MudDropZone<T>` components.
- A Markdown-to-HTML rendering approach (e.g., Markdig library or a lightweight Blazor Markdown component).

## Open Questions

1. Should the Markdown rendering use Markdig (server-side) or a JavaScript-based renderer? Markdig is already a common .NET choice and avoids JS interop.
2. When real data replaces mock data in the future, should the mock data be preserved as a demo/seed mode, or removed entirely?
3. Should the card click modal show the full Markdown description (un-clamped) as a preview of the future card detail, or strictly just "Coming soon"?
