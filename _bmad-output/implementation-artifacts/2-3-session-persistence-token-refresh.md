# Story 2.3: Session Persistence & Token Refresh

Status: ready-for-dev

## Story

As an authenticated user,
I want my session to persist across browser refreshes and handle token expiry silently,
so that I don't have to log in repeatedly during my workday.

## Acceptance Criteria

1. **Given** a user has a valid session (refresh token cookie exists), **When** the user refreshes the browser or opens a new tab, **Then** the Angular app detects the absence of an in-memory access token, automatically calls `/api/auth/refresh` using the refresh token cookie, a new access token is returned and stored in service memory, and the user lands on the conversation list without seeing a login screen (FR4).

2. **Given** the access token expires during an active session, **When** an API request returns 401 Unauthorized, **Then** the auth interceptor queues the failed request, calls `/api/auth/refresh` to obtain a new access token, retries the original request with the new token, and concurrent 401s are batched — only one refresh request is made (prevents thundering herd).

3. **Given** a refresh token is used successfully, **When** new tokens are issued, **Then** a new refresh token is issued (rotation) and the old refresh token is invalidated, and the Redis session entry is updated with the new refresh token identifier.

4. **Given** a previously used (rotated-out) refresh token is presented, **When** the refresh endpoint processes it, **Then** reuse is detected as potential token theft, all sessions for that user are invalidated (emergency lockout — all Redis session entries deleted), and the user must log in again on all devices.

5. **Given** the refresh token cookie has expired (7+ days inactive), **When** the user opens the application, **Then** the refresh attempt fails, the user is redirected to the login page, and any previously typed content is not lost if applicable.

## Tasks / Subtasks

- [ ] Task 1: Backend RefreshTokenCommand + Handler (AC: #1, #3, #4)
  - [ ] 1.1 Create `src/Application/Identity/Commands/RefreshToken/RefreshTokenCommand.cs` — record with `string RefreshToken` property, returns `RefreshTokenResponse`
  - [ ] 1.2 Create `src/Application/Identity/Commands/RefreshToken/RefreshTokenResponse.cs` — record with `string AccessToken`, `string RefreshToken`, `string UserId`, `string DisplayName`, `string Email`, `string Role`
  - [ ] 1.3 Create `src/Application/Identity/Commands/RefreshToken/RefreshTokenCommandHandler.cs`:
    1. Validate refresh token exists in Redis via `ISessionService.GetSessionUserIdAsync(refreshToken)`
    2. If null → throw `UnauthorizedAccessException("Invalid or expired refresh token")`
    3. Load user via `IIdentityService.FindUserByIdAsync(userId)` (new method — see Task 2)
    4. If user is null or `!user.IsActive` → invalidate session, throw `UnauthorizedAccessException`
    5. Generate new tokens via `IJwtTokenService.GenerateTokens()`
    6. **Invalidate old refresh token** via `ISessionService.InvalidateSessionAsync(oldRefreshToken)`
    7. **Store new session** via `ISessionService.StoreSessionAsync(userId, newRefreshToken, TimeSpan.FromDays(7))`
    8. Return `RefreshTokenResponse` with new access token, new refresh token, and user details
  - [ ] 1.4 Create `src/Application/Identity/Commands/RefreshToken/RefreshTokenCommandValidator.cs` — validate `RefreshToken` is NotEmpty
  - [ ] 1.5 Implement token reuse detection (AC #4): Before invalidating old token, check if it's already been invalidated (returns null from Redis). If the token was already used (not found in Redis but was a valid format), call `ISessionService.InvalidateAllSessionsAsync(userId)` to delete all sessions for that user. **Note:** This requires tracking the userId associated with a used token — store a short-lived "used token" Redis key (e.g., `used-refresh:{token}` → userId with 7-day TTL) when rotating, so reuse of rotated tokens can identify the user for emergency lockout.

- [ ] Task 2: IIdentityService — Add FindUserByIdAsync (AC: #1, #3)
  - [ ] 2.1 Add `Task<UserDto?> FindUserByIdAsync(string userId)` to `src/Application/Common/Interfaces/IIdentityService.cs` — returns a DTO with Id, DisplayName, Email, Role, IsActive
  - [ ] 2.2 Implement in `src/Infrastructure/Identity/IdentityService.cs` using `UserManager.FindByIdAsync(userId)`
  - [ ] 2.3 Create `UserDto` record if not already existing, in `src/Application/Identity/` or in the existing identity DTOs location

- [ ] Task 3: ISessionService — Add Used Token Tracking (AC: #4)
  - [ ] 3.1 Add `Task MarkTokenAsUsedAsync(string refreshToken, string userId, TimeSpan expiry)` to `ISessionService`
  - [ ] 3.2 Add `Task<string?> GetUsedTokenUserIdAsync(string refreshToken)` to `ISessionService`
  - [ ] 3.3 Implement in `RedisSessionService.cs`:
    - `MarkTokenAsUsedAsync`: SET `used-refresh:{token}` → userId with TTL (7 days)
    - `GetUsedTokenUserIdAsync`: GET `used-refresh:{token}` → userId or null

- [ ] Task 4: Backend Refresh Endpoint (AC: #1, #3, #4, #5)
  - [ ] 4.1 Add `POST /api/auth/refresh` endpoint in `src/Web/Endpoints/Auth.cs`
  - [ ] 4.2 Extract refresh token from `context.Request.Cookies["refresh_token"]`
  - [ ] 4.3 If no cookie present → return 401
  - [ ] 4.4 Dispatch `RefreshTokenCommand` via MediatR
  - [ ] 4.5 On success: set new refresh token cookie using `SetRefreshTokenCookie()` (already exists), return new access token + user details in response body
  - [ ] 4.6 On `UnauthorizedAccessException`: clear the refresh_token cookie (expire it), return 401
  - [ ] 4.7 Endpoint must be `[AllowAnonymous]` — the refresh token cookie IS the authentication
  - [ ] 4.8 Apply rate limiting: reuse existing "auth" per-IP policy (20/min). Do NOT apply per-user rate limiting (user is not yet authenticated on this endpoint)

- [ ] Task 5: Angular AuthService — Add Refresh & App Init (AC: #1, #2)
  - [ ] 5.1 Add `refresh()` method to `auth.service.ts`: POST `/api/auth/refresh` with `withCredentials: true` (cookies sent automatically), no body needed
  - [ ] 5.2 On success: call `setSession()` with response data (same as login/register)
  - [ ] 5.3 On failure (401): call `logout()` to clear state, navigate to `/login`
  - [ ] 5.4 Add `initializeAuth()` method: calls `refresh()` — returns Observable<boolean> indicating success/failure. This is the app initializer hook.
  - [ ] 5.5 Add `refreshInProgress$` BehaviorSubject to track whether a refresh is in flight (used by interceptor to queue requests)
  - [ ] 5.6 Export a `refreshAccessToken$` that is a shared (multicasted) Observable wrapping the refresh HTTP call — so concurrent callers share the same in-flight request

- [ ] Task 6: Angular Auth Interceptor — 401 Retry with Refresh (AC: #2)
  - [ ] 6.1 Modify `auth.interceptor.ts` to catch 401 responses (except from `/api/auth/refresh` itself — avoid infinite loop)
  - [ ] 6.2 On 401: check if a refresh is already in progress via `AuthService.refreshInProgress$`
  - [ ] 6.3 If no refresh in progress: trigger `AuthService.refreshAccessToken$` and wait for completion, then retry original request with new token
  - [ ] 6.4 If refresh already in progress: wait for `AuthService.refreshAccessToken$` to complete, then retry original request with new token
  - [ ] 6.5 If refresh fails (401 from refresh endpoint): call `AuthService.logout()`, redirect to `/login`, do NOT retry
  - [ ] 6.6 Exclude `/api/auth/refresh` from both token injection AND 401 retry logic (but DO include `withCredentials: true` for cookie)

- [ ] Task 7: Angular APP_INITIALIZER — Session Restore on Load (AC: #1)
  - [ ] 7.1 Create an app initializer factory in `app.config.ts` that calls `AuthService.initializeAuth()`
  - [ ] 7.2 Register via `provideAppInitializer()` (Angular 21 pattern) or `APP_INITIALIZER` token — blocks app rendering until auth state is resolved
  - [ ] 7.3 On refresh success: user lands on requested route (conversation list or deep link) without seeing login
  - [ ] 7.4 On refresh failure (no cookie, expired, invalid): allow app to load, auth guard redirects to login — this is NOT an error state
  - [ ] 7.5 **CRITICAL:** The initializer must NOT throw or reject — a failed refresh is normal (user not logged in). Return resolved promise/completed observable regardless.

- [ ] Task 8: Auth Guard Enhancement (AC: #1)
  - [ ] 8.1 The existing `auth.guard.ts` already checks `isAuthenticated$.value`. After APP_INITIALIZER runs, this will be correctly set. Verify no changes needed.
  - [ ] 8.2 If the guard fires before initializer completes (edge case), ensure it waits for `isAuthenticated$` to emit a value (use `firstValueFrom` or `filter` to skip initial null state if applicable)

- [ ] Task 9: Unit Tests (AC: all)
  - [ ] 9.1 `RefreshTokenCommandValidatorTests.cs` — valid token, empty token
  - [ ] 9.2 `RefreshTokenCommandHandlerTests.cs`:
    - Successful refresh: returns new tokens, old token invalidated, new session stored, used token marked
    - Expired/invalid token: throws UnauthorizedAccessException
    - Deactivated user: session invalidated, throws UnauthorizedAccessException
    - Token reuse detected: all user sessions invalidated, throws UnauthorizedAccessException
    - User not found: throws UnauthorizedAccessException
  - [ ] 9.3 Test location: `tests/Application.UnitTests/Identity/Commands/RefreshToken/`
  - [ ] 9.4 Test framework: **NUnit 4.5.1** with **Moq** (matches existing test infrastructure — NOT xUnit)

- [ ] Task 10: Integration Tests (AC: #1, #3, #4, #5)
  - [ ] 10.1 POST `/api/auth/refresh` with valid refresh token cookie → 200 + new access token + new refresh cookie
  - [ ] 10.2 POST `/api/auth/refresh` with expired/invalid cookie → 401 + cookie cleared
  - [ ] 10.3 POST `/api/auth/refresh` with no cookie → 401
  - [ ] 10.4 POST `/api/auth/refresh` with rotated-out (reused) token → 401 + all user sessions invalidated
  - [ ] 10.5 POST `/api/auth/refresh` for deactivated user → 401 + session invalidated
  - [ ] 10.6 Sequential refresh: login → refresh → use new token on protected endpoint → 200
  - [ ] 10.7 Test location: `tests/Infrastructure.IntegrationTests/Api/AuthEndpointTests.cs` (extend existing file)

## Dev Notes

### Existing Infrastructure to Reuse (DO NOT Recreate)

- **JwtTokenService** (`src/Infrastructure/Services/JwtTokenService.cs`): `GenerateTokens(userId, email, displayName, role)` returns `(AccessToken, RefreshToken)`. Access token: 30-min expiry. Refresh token: random 32-byte base64. Reuse directly.
- **RedisSessionService** (`src/Infrastructure/Services/RedisSessionService.cs`): `StoreSessionAsync`, `GetSessionUserIdAsync`, `InvalidateSessionAsync`, `InvalidateAllSessionsAsync`. Redis key pattern: `session:{refreshToken}` → userId, `user-sessions:{userId}` → set of refresh tokens. Reuse directly, extend for used-token tracking.
- **Auth.cs** (`src/Web/Endpoints/Auth.cs`): Already has `SetRefreshTokenCookie()` helper method for HttpOnly Secure SameSite=Strict cookie. Reuse for refresh endpoint.
- **AuthService** (Angular `src/Web/ClientApp/src/app/core/services/auth.service.ts`): Already has `setSession()`, `logout()`, `accessToken` getter, `isAuthenticated$`, `user$` BehaviorSubjects. Extend with `refresh()` and `initializeAuth()`.
- **auth.interceptor.ts** (`src/Web/ClientApp/src/app/core/services/auth.interceptor.ts`): Functional `HttpInterceptorFn`. Currently adds Bearer token and skips auth endpoints. Extend with 401 retry logic.
- **IIdentityService** (`src/Application/Common/Interfaces/IIdentityService.cs`): Already has `FindUserByEmailAsync`, `CheckPasswordAsync`. Add `FindUserByIdAsync`.
- **IdentityService** (`src/Infrastructure/Identity/IdentityService.cs`): Implements IIdentityService. Add implementation for `FindUserByIdAsync`.

### Critical Implementation Patterns

**Refresh Token Rotation Flow:**
```
1. Extract refresh_token from HttpOnly cookie
2. Look up userId: ISessionService.GetSessionUserIdAsync(refreshToken)
3. If null → check if this is a reused token:
   a. GetUsedTokenUserIdAsync(refreshToken) → userId?
   b. If userId found → TOKEN REUSE DETECTED → InvalidateAllSessionsAsync(userId) → 401
   c. If not found → simply expired/invalid → 401
4. Load user by userId → verify IsActive
5. Generate new tokens: IJwtTokenService.GenerateTokens()
6. Mark old token as used: MarkTokenAsUsedAsync(oldToken, userId, 7 days)
7. Invalidate old session: InvalidateSessionAsync(oldRefreshToken)
8. Store new session: StoreSessionAsync(userId, newRefreshToken, 7 days)
9. Set new refresh cookie, return new access token
```

**Angular 401 Interceptor Pattern (prevents thundering herd):**
```typescript
// In auth.interceptor.ts
// Key pattern: share a single refresh observable across concurrent 401s
let refreshInProgress: Observable<any> | null = null;

// On 401 (not from /api/auth/refresh):
if (!refreshInProgress) {
  refreshInProgress = authService.refresh().pipe(
    finalize(() => refreshInProgress = null),
    shareReplay(1)
  );
}
return refreshInProgress.pipe(
  switchMap(() => next(req.clone({ setHeaders: { Authorization: `Bearer ${authService.accessToken}` } })))
);
```

**APP_INITIALIZER Pattern (Angular 21):**
```typescript
// In app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    // ... existing providers
    {
      provide: APP_INITIALIZER,
      useFactory: (authService: AuthService) => () =>
        firstValueFrom(authService.initializeAuth().pipe(catchError(() => of(false)))),
      deps: [AuthService],
      multi: true
    }
  ]
};
```

**Cookie Clearing on Failed Refresh (Backend):**
```csharp
context.Response.Cookies.Append("refresh_token", "", new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict,
    Expires = DateTimeOffset.UtcNow.AddDays(-1) // Expire immediately
});
```

### Items Deferred from Previous Stories to Address

| Item | Source | Action in This Story |
|------|--------|---------------------|
| No refresh endpoint | Story 2.1, 2.2 deferred | Implement `/api/auth/refresh` — primary scope of this story |
| Auth state lost on page refresh | Story 2.1 deferred | APP_INITIALIZER calls refresh on load |
| 401 interceptor with token refresh retry | Story 2.2 deferred | Enhance auth.interceptor.ts with 401 handling |
| Refresh token expiry hardcoded in 4 locations | Story 2.2 review finding | Extract to shared constant `RefreshTokenExpiryDays = 7` in a `TokenConstants.cs` file in Application layer, and use from all locations |

### Items NOT in Scope (Deferred to Later Stories)

- Logout endpoint (`POST /api/auth/logout`) → Story 2.4
- Non-disruptive re-authentication overlay (UX-DR28) → Deferred to future UX polish; this story redirects to login on expired refresh
- SignalR authentication with JWT → Epic 3 (when SignalR hub is implemented)
- `prefers-reduced-motion` considerations → cross-cutting, deferred to Epic 9

### Project Structure Notes

**New files to create:**
```
src/Application/Identity/Commands/RefreshToken/
  RefreshTokenCommand.cs
  RefreshTokenResponse.cs
  RefreshTokenCommandHandler.cs
  RefreshTokenCommandValidator.cs
src/Application/Identity/Constants/
  TokenConstants.cs                    (shared expiry constant)
tests/Application.UnitTests/Identity/Commands/RefreshToken/
  RefreshTokenCommandValidatorTests.cs
  RefreshTokenCommandHandlerTests.cs
```

**Files to modify:**
```
src/Application/Common/Interfaces/IIdentityService.cs    — add FindUserByIdAsync
src/Application/Common/Interfaces/ISessionService.cs     — add MarkTokenAsUsedAsync, GetUsedTokenUserIdAsync
src/Infrastructure/Identity/IdentityService.cs           — implement FindUserByIdAsync
src/Infrastructure/Services/RedisSessionService.cs       — implement used-token tracking
src/Web/Endpoints/Auth.cs                                — add refresh endpoint
src/Web/ClientApp/src/app/core/services/auth.service.ts  — add refresh(), initializeAuth()
src/Web/ClientApp/src/app/core/services/auth.interceptor.ts — add 401 retry logic
src/Web/ClientApp/src/app/app.config.ts                  — add APP_INITIALIZER
tests/Infrastructure.IntegrationTests/Api/AuthEndpointTests.cs — add refresh tests
```

**Optionally refactor (if addressing deferred hardcoded expiry):**
```
src/Application/Identity/Commands/Login/LoginCommandHandler.cs    — use TokenConstants
src/Application/Identity/Commands/Register/RegisterCommandHandler.cs — use TokenConstants
src/Web/Endpoints/Auth.cs                                          — use TokenConstants for cookie expiry
```

### Security Requirements

- Refresh token rotation is MANDATORY — never return the same refresh token twice (OWASP recommendation)
- Token reuse detection must trigger emergency lockout (all sessions for that user deleted)
- Refresh endpoint must clear the cookie on failure — do not leave stale cookies that trigger repeated failed refreshes
- Deactivated users must fail refresh immediately (NFR13)
- Refresh endpoint is `[AllowAnonymous]` — authentication is via the cookie, not the JWT
- Rate limit the refresh endpoint to prevent abuse (per-IP only, same "auth" policy)

### UX Requirements

- Page refresh must be invisible to authenticated users — no flash of login screen
- APP_INITIALIZER blocks rendering until auth state is resolved
- Failed refresh (expired session) is not an error — silently redirect to login via auth guard
- 401 retry must be transparent to the user — no error flash, no re-submission of forms

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-2-user-authentication-account-management.md#Story 2.3]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication & Security]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication Flow]
- [Source: _bmad-output/planning-artifacts/prd.md#FR4]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR9, NFR12, NFR13]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Session expiry UX]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md]
- [Source: _bmad-output/implementation-artifacts/2-2-user-login-jwt-session.md]
- [Source: _bmad-output/implementation-artifacts/2-1-user-registration-with-first-user-admin-designation.md]

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### Change Log

### File List
