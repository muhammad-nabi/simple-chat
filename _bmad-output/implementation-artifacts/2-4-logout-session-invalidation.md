# Story 2.4: Logout & Session Invalidation

Status: done

## Story

As an authenticated user,
I want to log out and have my session immediately invalidated,
so that my account is secure when I'm done using the application.

## Acceptance Criteria

1. **Given** an authenticated user, **When** the user clicks the Logout action, **Then** the backend deletes the Redis session entry for this refresh token, **And** the refresh token cookie is cleared (expired), **And** the Angular app clears the in-memory access token and any cached application state, **And** the user is redirected to the login page.

2. **Given** a user has active sessions on multiple devices, **When** the user logs out on one device, **Then** only that device's session is invalidated, **And** other device sessions remain active (single-device logout, not global).

3. **Given** an administrator deactivates a user account (covered fully in Epic 8), **When** the deactivation is processed, **Then** all Redis session entries for that user are deleted immediately, **And** the deactivated user's next API request or SignalR message fails with 401/403, **And** the deactivated user is unable to obtain new tokens via refresh (NFR13).

4. **Given** the API endpoints (excluding register, login, health), **When** any request is made without a valid JWT access token, **Then** the request is rejected with 401 Unauthorized (NFR12), **And** no user data or messages are accessible without authentication.

## Tasks / Subtasks

- [x] Task 1: Backend LogoutCommand + Handler (AC: #1, #2)
  - [x] 1.1 Create `src/Application/Identity/Commands/Logout/LogoutCommand.cs` — record with `string RefreshToken` property
  - [x] 1.2 Create `src/Application/Identity/Commands/Logout/LogoutCommandHandler.cs`:
    1. Call `ISessionService.InvalidateSessionAsync(refreshToken)` to delete the single Redis session entry
    2. This invalidates only the current device's session (AC #2) — `InvalidateSessionAsync` removes the token from `session:{refreshToken}` and from the `user-sessions:{userId}` set
    3. No need to load user or validate — if the token doesn't exist, the operation is a no-op (idempotent)
  - [x]1.3 Create `src/Application/Identity/Commands/Logout/LogoutCommandValidator.cs` — validate `RefreshToken` is NotEmpty

- [x]Task 2: Backend Logout Endpoint (AC: #1, #2)
  - [x]2.1 Add `POST /api/auth/logout` endpoint in `src/Web/Endpoints/Auth.cs`
  - [x]2.2 Endpoint requires authentication — add `[Authorize]` (NOT AllowAnonymous like refresh)
  - [x]2.3 Extract refresh token from `context.Request.Cookies["refresh_token"]`
  - [x]2.4 Dispatch `LogoutCommand` via MediatR
  - [x]2.5 Clear the refresh token cookie using existing `ClearRefreshTokenCookie()` helper
  - [x]2.6 Return 200 OK (no body needed)
  - [x]2.7 If no cookie present — still return 200 (idempotent; client-side state will be cleared regardless)
  - [x]2.8 Apply rate limiting: reuse existing "auth" per-IP policy (20/min)

- [x]Task 3: Angular AuthService — Add Server-Side Logout (AC: #1)
  - [x]3.1 Modify `logout()` method in `auth.service.ts` to call `POST /api/auth/logout` with `{ withCredentials: true }` BEFORE clearing local state
  - [x]3.2 The server call should be fire-and-forget (best-effort) — if it fails (e.g., network error, already logged out), still proceed with local cleanup
  - [x]3.3 After the server call (or on failure), clear local state: set `accessToken$` to `null`, `user$` to `null`, `isAuthenticated$` to `false`
  - [x]3.4 Navigate to `/login` after clearing state
  - [x]3.5 Ensure any in-flight refresh observable (`refreshInFlight$`) is cancelled/ignored after logout

- [x]Task 4: Logout UI Trigger (AC: #1)
  - [x]4.1 Add a Logout button/link in the appropriate location. Per UX spec: this should be in the user's profile area or a settings menu. For MVP with no full layout yet, add it to the `HomeComponent` or wherever the authenticated user lands (currently `/` route → HomeComponent)
  - [x]4.2 The logout action calls `AuthService.logout()`
  - [x]4.3 No confirmation dialog needed — logout is not destructive (UX spec: "Modals only for destructive/irreversible actions like admin deactivation")
  - [x]4.4 Button should be accessible: visible text label ("Log out"), keyboard-navigable

- [x]Task 5: Auth Interceptor — Exclude Logout from 401 Retry (AC: #1)
  - [x]5.1 Add `/api/auth/logout` to the AUTH_URLS constant in `auth.interceptor.ts` so it is excluded from 401 retry logic
  - [x]5.2 The logout endpoint requires auth, but if the token is expired during logout, the interceptor should NOT attempt to refresh and retry — just let the logout fail silently (the server call is best-effort)

- [x]Task 6: Unit Tests (AC: #1, #2)
  - [x]6.1 `LogoutCommandValidatorTests.cs` — 2 tests: valid token, empty token
  - [x]6.2 `LogoutCommandHandlerTests.cs` — 2 tests:
    - Successful logout: `InvalidateSessionAsync` called with correct refresh token
    - Idempotent: handler completes without error when token doesn't exist in Redis
  - [x]6.3 Test location: `tests/Application.UnitTests/Identity/Commands/Logout/`
  - [x]6.4 Test framework: **NUnit 4.5.1** with **Moq** (matches existing test infrastructure)

- [x]Task 7: Integration Tests (AC: #1, #2)
  - [x]7.1 POST `/api/auth/logout` with valid auth + refresh cookie → 200 + cookie cleared
  - [x]7.2 POST `/api/auth/logout` without auth token → 401 (endpoint requires authentication)
  - [x]7.3 POST `/api/auth/logout` with auth but no cookie → 200 (idempotent)
  - [x]7.4 Logout then attempt refresh with same token → 401 (session invalidated)
  - [x]7.5 Logout on device A, device B session still valid (if testable — requires two separate login sessions)
  - [x]7.6 Test location: `tests/Infrastructure.IntegrationTests/Api/AuthEndpointTests.cs` (extend existing file)

## Dev Notes

### Existing Infrastructure to Reuse (DO NOT Recreate)

- **ISessionService** (`src/Application/Common/Interfaces/ISessionService.cs`): Already has `InvalidateSessionAsync(refreshToken)` which removes the session from both `session:{refreshToken}` key AND the `user-sessions:{userId}` set. Also has `InvalidateAllSessionsAsync(userId)` for admin deactivation. Reuse directly — NO new session service methods needed.
- **RedisSessionService** (`src/Infrastructure/Services/RedisSessionService.cs`): Full implementation of `InvalidateSessionAsync` already exists and handles single-device logout correctly by removing only the specific token.
- **Auth.cs** (`src/Web/Endpoints/Auth.cs`): Already has `ClearRefreshTokenCookie()` helper method. Reuse for logout endpoint.
- **AuthService** (Angular `src/Web/ClientApp/src/app/core/services/auth.service.ts`): Already has `logout()` method that clears local state. Extend to call server first.
- **auth.interceptor.ts** (`src/Web/ClientApp/src/app/core/services/auth.interceptor.ts`): Already has `AUTH_URLS` constant for endpoints excluded from token injection and 401 retry. Add logout URL to this list.
- **TokenConstants** (`src/Application/Identity/Constants/TokenConstants.cs`): Shared constant for refresh token expiry — not directly needed for logout, but established pattern.
- **LoginCommand/RegisterCommand patterns** — Follow the same CQRS structure for LogoutCommand.

### Critical Implementation Patterns

**Logout Flow (end-to-end):**
```
Angular Logout Button → AuthService.logout()
  → POST /api/auth/logout (with credentials: true for cookie)
    → LogoutCommandHandler:
      1. ISessionService.InvalidateSessionAsync(refreshToken)
         → Redis: DEL session:{refreshToken}
         → Redis: SREM user-sessions:{userId} refreshToken
      2. Return success
    → Endpoint: ClearRefreshTokenCookie() → set expired cookie
    → Return 200
  → Angular: clear accessToken$, user$, isAuthenticated$
  → Angular: router.navigate(['/login'])
```

**Key Design Decision — Single-Device Logout (AC #2):**
`InvalidateSessionAsync(refreshToken)` deletes ONLY the session for the specific refresh token being used on the current device. Other devices have different refresh tokens (due to token rotation on each refresh) and are unaffected. This is NOT `InvalidateAllSessionsAsync(userId)` which is the nuclear option used for admin deactivation and token theft detection.

**Key Design Decision — Best-Effort Server Call:**
The Angular `logout()` method should attempt the server call but not block on it. If the server is unreachable:
- Local state is still cleared (user appears logged out immediately)
- The refresh token cookie will eventually expire (7 days)
- The Redis session will also expire via TTL (7 days)
- This matches industry practice (e.g., if user is offline, they can still "log out" locally)

**Auth Interceptor Exclusion:**
The `/api/auth/logout` URL must be added to `AUTH_URLS` in `auth.interceptor.ts`. This constant controls which URLs skip Bearer token injection AND 401 retry logic. However, note that the logout endpoint DOES require auth (unlike register/login/refresh which are AllowAnonymous). The interceptor should still add the Bearer token to the logout request. The exclusion from AUTH_URLS should be specifically for the 401 RETRY logic only — if logout gets a 401, do NOT try to refresh and retry, just let it fail.

**IMPORTANT: Review the current AUTH_URLS implementation carefully.** Currently AUTH_URLS is used to skip BOTH token injection AND 401 retry. For the logout endpoint, we need token injection (it requires auth) but NOT 401 retry. Options:
1. Split AUTH_URLS into two separate lists: one for skipping token injection, one for skipping 401 retry
2. Or: do NOT add logout to AUTH_URLS. Instead, handle the logout 401 case inside the `logout()` method itself (catch the error and proceed with local cleanup regardless)

**Recommended approach: Option 2** — Keep AUTH_URLS as-is. The `logout()` method already handles failures gracefully (fire-and-forget), so a 401 on the logout call will be caught by the error handler in `logout()` before the interceptor's 401 retry logic kicks in. Actually — the interceptor WILL try to refresh on 401 from logout. To prevent this, the simplest fix is:
- Add a `isLoggingOut` flag to AuthService
- Set it to `true` before calling the logout endpoint
- In the interceptor, check `authService.isLoggingOut` and skip 401 retry if true
- OR: simply add `/api/auth/logout` to a separate "no-retry" list in the interceptor

**Simplest correct approach:** Add logout URL to the 401-retry exclusion check in the interceptor (where it checks `req.url.endsWith('/api/auth/refresh')`). Change the check to also exclude `/api/auth/logout`. This is a one-line change.

### Admin Deactivation (AC #3) — Scope Boundary

AC #3 references admin deactivation. The architecture defines this in Epic 8 (Administration). For this story:
- The existing `ISessionService.InvalidateAllSessionsAsync(userId)` already handles the "delete ALL sessions" requirement
- The `RefreshTokenCommandHandler` (Story 2.3) already checks `user.IsActive` and rejects refresh for deactivated users
- No new work needed in this story for deactivation — it's already handled by existing infrastructure
- AC #3 is included for completeness and verification, not for new implementation

### API Auth Enforcement (AC #4) — Verification Only

AC #4 states all endpoints (except register, login, health) require valid JWT. This is already enforced:
- ASP.NET Core's `[Authorize]` attribute is the default (applied globally or per endpoint)
- Only register, login, refresh, and health endpoints have `[AllowAnonymous]`
- No new work needed — verify existing behavior with integration tests

### Items Deferred from Previous Stories

| Item | Source | Action in This Story |
|------|--------|---------------------|
| `logout()` does not call server-side session invalidation | Story 2.3 review finding (blind) | **Primary scope** — implement server-side logout endpoint and wire Angular `logout()` to call it |
| Logout endpoint (`POST /api/auth/logout`) | Story 2.3 deferred items | **Primary scope** — implement the endpoint |

### Items NOT in Scope (Deferred to Later Stories)

- Admin deactivation UI and endpoint → Epic 8 (Administration)
- "Log out all devices" option for end users → Post-MVP consideration
- Non-disruptive re-authentication overlay (UX-DR28) → Deferred to future UX polish
- SignalR disconnect on logout → Epic 3 (when SignalR hub is implemented; for now, no SignalR exists to disconnect)
- Draft message preservation on logout → Epic 3+ (no message drafts exist yet)

### Project Structure Notes

**New files to create:**
```
src/Application/Identity/Commands/Logout/
  LogoutCommand.cs
  LogoutCommandHandler.cs
  LogoutCommandValidator.cs
tests/Application.UnitTests/Identity/Commands/Logout/
  LogoutCommandValidatorTests.cs
  LogoutCommandHandlerTests.cs
```

**Files to modify:**
```
src/Web/Endpoints/Auth.cs                                — add POST /api/auth/logout endpoint
src/Web/ClientApp/src/app/core/services/auth.service.ts  — modify logout() to call server first
src/Web/ClientApp/src/app/core/services/auth.interceptor.ts — exclude logout URL from 401 retry
tests/Infrastructure.IntegrationTests/Api/AuthEndpointTests.cs — add logout integration tests
```

### Security Requirements

- Logout endpoint MUST require authentication (`[Authorize]`) — prevents unauthenticated session invalidation attacks
- Logout MUST clear the refresh token cookie — leaving a stale cookie causes repeated failed refresh attempts
- Logout MUST invalidate the Redis session — clearing only the cookie is insufficient (token could be replayed from another client)
- Single-device logout only — do NOT call `InvalidateAllSessionsAsync` (that's for admin deactivation and token theft)
- Logout should be idempotent — calling it twice with the same token should not error

### UX Requirements

- No confirmation dialog for logout (UX spec: modals only for destructive/irreversible actions)
- User is redirected to login page immediately after logout
- Logout button should have a visible text label ("Log out"), not icon-only (UX anti-pattern: icon-only toolbars)
- Logout should work even if the server is unreachable (local state cleared regardless)

### Testing Requirements

- **Unit tests:** NUnit 4.5.1 + Moq (match existing pattern in `tests/Application.UnitTests/`)
- **Integration tests:** Extend `tests/Infrastructure.IntegrationTests/Api/AuthEndpointTests.cs`
- **Known issue:** Some integration tests involving registration may fail due to pre-existing Identity password validator issue (password "password123" returns 400). Use a password that meets all requirements in tests.

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-2-user-authentication-account-management.md#Story 2.4]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication & Security]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication Flow]
- [Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries — POST /api/auth/logout]
- [Source: _bmad-output/planning-artifacts/prd.md#FR3 — Authenticated users can log out from any device]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR12, NFR13]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Emotional Design — modals only for destructive actions]
- [Source: _bmad-output/implementation-artifacts/2-3-session-persistence-token-refresh.md#Review Findings — logout deferred]
- [Source: _bmad-output/implementation-artifacts/2-3-session-persistence-token-refresh.md#Items NOT in Scope]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

### Completion Notes List

- LogoutCommand + Handler + Validator created following LoginCommand CQRS patterns; handler calls `InvalidateSessionAsync` for single-device logout (not `InvalidateAllSessionsAsync`)
- POST `/api/auth/logout` endpoint added to Auth.cs with `RequireAuthorization()` + "auth" rate limiting; extracts refresh token from HttpOnly cookie, dispatches LogoutCommand, clears cookie via existing `ClearRefreshTokenCookie()` helper; idempotent — returns 200 even if no cookie present
- Angular `AuthService.logout()` modified to call server-side logout endpoint (best-effort, fire-and-forget) before clearing local state; added `isLoggingOut` flag to prevent interceptor from retrying 401 during logout; added `clearSession()` private method with router navigation to `/login`
- HomeComponent updated with "Log out" button that calls `AuthService.logout()`; accessible with visible text label, keyboard-navigable
- Auth interceptor enhanced to check `authService.isLoggingOut` flag — skips 401 refresh-retry during logout to prevent infinite loops; AUTH_URLS kept unchanged since logout needs Bearer token injection (unlike login/register/refresh which are AllowAnonymous)
- 4 new unit tests (2 validator + 2 handler), all passing; 49 total unit tests pass with 0 regressions
- 5 new integration tests written for logout flow: valid logout, unauthenticated logout rejected, idempotent logout without cookie, logout-then-refresh invalidation, multi-device single-device logout verification
- Angular and backend both build cleanly with 0 errors

### Review Findings

- [x] [Review][Patch] `isLoggingOut` flag not guaranteed to reset if HTTP POST hangs — added `finalize()` operator to reset flag [auth.service.ts:120]
- [x] [Review][Patch] Double-click on `logout()` has no re-entrant guard — added early return if `isLoggingOut` is already true [auth.service.ts:111]
- [x] [Review][Patch] In-flight refresh can overwrite cleared session state after logout — added `isLoggingOut` guard in `setSession()` [auth.service.ts:137]
- [x] [Review][Patch] Redundant identical OR assertion in integration test — fixed to check `refresh_token=;` AND `expires=` [AuthEndpointTests.cs:422]
- [x] [Review][Defer] No logging in `LogoutCommandHandler` when Redis fails — deferred, pre-existing pattern (no logging in LoginCommandHandler either)

### Change Log

- 2026-03-30: Story 2.4 implemented — server-side logout endpoint, Angular logout with server call, logout UI button, interceptor exclusion during logout

### File List

New files:
- src/Application/Identity/Commands/Logout/LogoutCommand.cs
- src/Application/Identity/Commands/Logout/LogoutCommandHandler.cs
- src/Application/Identity/Commands/Logout/LogoutCommandValidator.cs
- tests/Application.UnitTests/Identity/Commands/Logout/LogoutCommandValidatorTests.cs
- tests/Application.UnitTests/Identity/Commands/Logout/LogoutCommandHandlerTests.cs

Modified files:
- src/Web/Endpoints/Auth.cs (added POST /api/auth/logout endpoint, Logout method)
- src/Web/ClientApp/src/app/core/services/auth.service.ts (server-side logout call, isLoggingOut flag, clearSession method)
- src/Web/ClientApp/src/app/core/services/auth.interceptor.ts (skip 401 retry during logout)
- src/Web/ClientApp/src/app/features/home/home.component.ts (added Log out button)
- tests/Infrastructure.IntegrationTests/Api/AuthEndpointTests.cs (added 5 logout integration tests)
