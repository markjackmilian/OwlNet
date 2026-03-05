# SPEC: Enforce Authentication Gate

> **Status:** Todo
> **Created:** 2026-03-05
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** S

## Context

OwlNet already has a complete ASP.NET Core Identity setup: login page, registration page, cookie authentication, `AuthorizeRouteView` in `Routes.razor`, and a `RedirectToLogin` component. However, the application is currently accessible without authentication because the HTTP middleware pipeline in `Program.cs` is missing `UseAuthentication()` and `UseAuthorization()` calls. Without these middleware components, `HttpContext.User` is never populated during static SSR, and the Blazor authorization infrastructure has no authentication state to enforce.

This spec addresses the configuration gap so that all application pages require authentication, while login and registration remain freely accessible.

## Actors

- **Anonymous user** — anyone who has not yet logged in or registered.
- **Authenticated user** — any registered user with a valid session.

## Functional Requirements

1. The system SHALL add `UseAuthentication()` and `UseAuthorization()` middleware to the HTTP pipeline in `Program.cs`, positioned after `UseAntiforgery()` and before `MapRazorComponents`.
2. The system SHALL redirect any anonymous user attempting to access a protected page to `/Account/Login` with a `returnUrl` query parameter preserving the originally requested URL.
3. The system SHALL allow anonymous access to all Account pages under `/Account/*` (Login, Register, ForgotPassword, ResetPassword, Lockout, and their confirmation pages) without requiring authentication.
4. The system SHALL allow registered users to log in with email and password, and upon successful login redirect them to the `returnUrl` or the home page (`/`).
5. The system SHALL support the "Remember Me" option on the login page using Identity's default persistent cookie behavior (14-day expiration).
6. The system SHALL allow anyone to register with email and password without email confirmation — the user can log in immediately after registration.
7. The system SHALL ensure that the `IdentityNoOpEmailSender` does not block the registration flow — `RegisterConfirmation` page SHALL detect the no-op sender and display a direct login link instead of asking the user to check their email.

## User Flow

### Login (happy path)

1. Anonymous user navigates to any page (e.g., `/`).
2. System redirects to `/Account/Login?returnUrl=%2F`.
3. User enters email and password, optionally checks "Remember me".
4. User clicks "Log in".
5. System validates credentials and creates an authentication cookie.
6. System redirects to the original `returnUrl` (or `/` if none).

### Registration (happy path)

1. Anonymous user navigates to `/Account/Register` (or clicks "Register" link on login page).
2. User enters email, password, and password confirmation.
3. User clicks "Register".
4. System creates the user account.
5. System redirects to a confirmation page with a direct link to log in (no email verification required).
6. User clicks the link, logs in with the newly created credentials.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| Anonymous user accesses `/` | Redirect to `/Account/Login?returnUrl=%2F` |
| Anonymous user accesses `/settings` | Redirect to `/Account/Login?returnUrl=%2Fsettings` |
| Anonymous user accesses `/Account/Login` | Page renders normally (no redirect loop) |
| Anonymous user accesses `/Account/Register` | Page renders normally |
| User logs in with "Remember Me" checked, closes browser, reopens | Session persists — user is still authenticated |
| User logs in with "Remember Me" unchecked, closes browser, reopens | Session expires — user must log in again |
| Registration with already-used email | Error message displayed on registration form |
| Registration with weak password (below Identity defaults) | Validation error displayed on registration form |
| Login with incorrect credentials | Error message "Invalid login attempt" displayed |
| Authenticated user navigates to `/Account/Login` | Page renders (standard Identity behavior — no forced redirect away) |

## Out of Scope

- Role-based authorization (no roles exist; all users have equal access).
- Email confirmation or real email sending.
- Custom password policy configuration (Identity defaults are acceptable).
- Custom cookie expiration configuration (Identity defaults are acceptable).
- Admin user seeding.
- External authentication providers (Google, Microsoft, etc.).
- Two-factor authentication enforcement.
- Account lockout policy configuration.
- `ICurrentUserService` or Application-layer auth abstractions.

## Acceptance Criteria

- [ ] Navigating to `/` without authentication redirects to `/Account/Login`.
- [ ] Navigating to `/settings` without authentication redirects to `/Account/Login?returnUrl=%2Fsettings`.
- [ ] `/Account/Login` is accessible without authentication.
- [ ] `/Account/Register` is accessible without authentication.
- [ ] After successful login, user is redirected to the `returnUrl`.
- [ ] After successful login with "Remember Me", closing and reopening the browser preserves the session.
- [ ] After successful registration, user can immediately log in without email confirmation.
- [ ] An authenticated user can access all application pages (`/`, `/settings`, etc.).
- [ ] The application builds with zero warnings.

## Dependencies

- ASP.NET Core Identity (already configured in Infrastructure layer).
- Existing Account pages (already present in Web layer).

## Open Questions

None — all details have been clarified.
