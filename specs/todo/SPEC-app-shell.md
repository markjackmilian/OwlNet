# SPEC: Application Shell

> **Status:** Todo
> **Created:** 2026-03-04
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

OwlNet needs an application shell that defines the main layout of the web application: topbar, content area, and navigation between sections. The shell is the foundation on which all vertical features (per project) and the admin section will be built. Without this structure, no other feature can be developed.

The application is multi-user with no role distinction: all authenticated users have access to all functionality, including the admin section.

## Actors

- **Authenticated user**: any user who has logged into the application.

## Functional Requirements

### Topbar

1. The topbar SHALL be visible on all pages of the application, including the admin section.
2. The topbar SHALL contain, from left to right: application logo, project selector slot, flexible spacer, username with avatar, Settings button.
3. The logo SHALL be positioned at the far left of the topbar.
4. The Settings button SHALL be represented by a recognizable icon (e.g., gear/cog) and SHALL navigate to the admin section.
5. The username and avatar SHALL be displayed next to the Settings button.
6. The project selector slot SHALL be a designated area in the topbar where the project selector component (defined in SPEC-project-selector) will be rendered.

### Admin Section

7. The admin section SHALL have a dedicated layout with a sidebar navigation menu on the left.
8. The admin section SHALL be reachable exclusively via the Settings button in the topbar.
9. The admin section SHALL be outside the project context (global application settings).
10. The admin section SHALL include an explicit button to exit and return to the main application area.
11. While in the admin section, the topbar SHALL remain visible.
12. While in the admin section, the project selector in the topbar SHALL be hidden or disabled to indicate the user is outside the project context.

### Main Content Area

13. The main content area SHALL occupy the remaining space below the topbar.
14. When a project is active, the main content area SHALL host project-specific vertical features (defined in future specs).
15. When in the admin section, the main content area SHALL be replaced by the admin layout (sidebar + admin content area).

## User Flow

### Navigate to admin section

1. User clicks the Settings button (gear icon) in the topbar.
2. The system navigates to the admin section.
3. The admin layout is displayed: sidebar on the left, admin content area on the right.
4. The topbar remains visible with the project selector hidden or disabled.

### Exit admin section

1. User clicks the explicit "Exit Admin" button (or equivalent).
2. The system navigates back to the main application area.
3. The project selector in the topbar becomes active again.
4. The main content area displays the active project context.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| User navigates directly to an admin URL | Admin section loads normally with sidebar layout |
| User clicks logo while in admin section | Define: should it exit admin and go to main area? (see Open Questions) |
| Browser back button from admin section | Should navigate back to the previous main area page |

## Out of Scope

- Admin section content (sub-pages, configuration screens) — will be defined in dedicated specs.
- Project-specific vertical features — will be defined in dedicated specs.
- Authentication and login system — the user is assumed to be already authenticated.
- Role and permission management.
- Project selector logic (modal, filtering, creation, persistence) — defined in SPEC-project-selector.
- User profile menu or dropdown from the avatar.

## Acceptance Criteria

- [ ] The topbar is visible on all pages and contains: logo, project selector slot, username with avatar, Settings button.
- [ ] The Settings button navigates to the admin section.
- [ ] The admin section displays a sidebar navigation layout.
- [ ] The project selector is hidden or disabled while in the admin section.
- [ ] An explicit button allows exiting the admin section and returning to the main area.
- [ ] The main content area occupies the full space below the topbar.
- [ ] The shell layout is responsive and functional at common screen sizes.

## Dependencies

- Authentication system (user must be already authenticated).
- SPEC-project-selector for the project selector component rendered in the topbar slot.

## Open Questions

1. **Logo click behavior:** should clicking the logo always navigate to the main area (exiting admin if needed)? Or should it have no navigation behavior?
2. **Avatar source:** where does the user avatar come from? Gravatar, upload, generated initials? For this spec it is sufficient that an avatar is displayed; details can be a dedicated spec.
3. **Admin sidebar behavior on small screens:** should it collapse to a hamburger menu, overlay, or remain always visible?
4. **Topbar project selector in admin:** should it be completely hidden or visually present but disabled (greyed out)?
