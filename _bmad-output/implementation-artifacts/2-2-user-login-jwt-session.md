# Story 2.2: User Login & JWT Session

Status: done

## Story

As a registered user,
I want to log in with my email and password,
So that I can access my conversations and messages securely.

## Acceptance Criteria

1. **Given** the login page is displayed, **When** a user submits a valid email and password, **Then** the backend verifies the password against the stored bcrypt hash, a short-lived JWT access token (30-minute expiry) is returned in the response body, a refresh token is issued as an HttpOnly Secure SameSite=Strict cookie with 7-day expiry, a Redis session entry is created with TTL matching the refresh token expiry, the Angular app stores the access token in service memory (not localStorage), and the user is redirected to the main conversation list view.

2. **Given** a user submits incorrect credentials, **When** the login request is processed, **Then** a generic error message is displayed: "Invalid email or password" (no indication of which field is wrong), the form is not cleared (email preserved, password cleared).

3. **Given** a deactivated user attempts to log in with valid credentials, **When** the login request is processed, **Then** the login is rejected with "Invalid email or password" (same generic message), and no tokens are issued (NFR13).

4. **Given** the login endpoint, **When** multiple failed login attempts occur, **Then** rate limiting is enforced per-IP (20 attempts/minute) AND per-username (5 attempts/minute) via configurable environment variables, and rate limit responses use standard 429 Too Many Requests status.

5. **Given** the Login component layout, **When** rendered on any viewport, **Then** the form displays Email and Password fields with visible labels above each field, inline validation on blur with friendly language, server errors displayed as inline alert above form (not toast), and Submit button with loading spinner during request.

6. **Given** the application starts, **When** the JWT secret is the default placeholder value, **Then** a warning is logged at startup (do not crash the app — dev convenience).

## Tasks / Subtasks

- [x] Task 1: Backend LoginCommand + Handler (AC: #1, #2, #3)
  - [x] 1.1 Create `LoginCommand.cs` in `src/Application/Identity/Commands/Login/` with Email and Password properties
  - [x] 1.2 Create `LoginCommandValidator.cs` — validate Email (required, valid format) and Password (required, min 8 chars)
  - [x] 1.3 Create `LoginCommandHandler.cs` — verify credentials via UserManager, check IsActive, generate tokens via IJwtTokenService, store session via ISessionService, return LoginResponse (same shape as RegisterResponse: AccessToken, RefreshToken, UserId, DisplayName, Email, Role)
  - [x] 1.4 Handle deactivated users: after password verification succeeds, check `user.IsActive == false` → return generic "Invalid email or password" error (same as wrong password)
- [x] Task 2: Backend Login Endpoint (AC: #1, #2, #3)
  - [x] 2.1 Add `POST /api/auth/login` endpoint in `src/Web/Endpoints/Auth.cs` — AllowAnonymous, same cookie-setting pattern as register
  - [x] 2.2 Return 200 with AccessToken in body on success, set refresh token as HttpOnly cookie
  - [x] 2.3 Return 401 with generic error on invalid credentials or deactivated user
- [x] Task 3: Rate Limiting (AC: #4)
  - [x] 3.1 Add `Microsoft.AspNetCore.RateLimiting` middleware in `Program.cs`
  - [x] 3.2 Configure fixed-window rate limiter with "auth" policy (per-IP, 20/min default)
  - [x] 3.3 Read limits from configuration: `RateLimit:LoginPerMinutePerIp` and `RateLimit:LoginPerMinutePerUser` with defaults 20 and 5
  - [x] 3.4 Apply rate limiting to `/api/auth/login` endpoint (and `/api/auth/register` — deferred from 2-1)
  - [x] 3.5 Return 429 Too Many Requests when limit exceeded
- [x] Task 4: JWT Secret Validation (AC: #6)
  - [x] 4.1 Add startup check in `Program.cs`: if `Jwt:Secret` equals the placeholder value, log a warning via Serilog
- [x] Task 5: CORS Hardening (deferred from Story 2-1)
  - [x] 5.1 Replace `SetIsOriginAllowed(_ => true)` with explicit origin whitelist from configuration (`Cors:AllowedOrigins`)
  - [x] 5.2 In development, default to allow all origins; in production, require explicit configuration or restrict to localhost
- [x] Task 6: Angular LoginComponent (AC: #1, #2, #5)
  - [x] 6.1 Create `LoginComponent` in `src/Web/ClientApp/src/app/features/auth/login/` — mirror RegisterComponent patterns
  - [x] 6.2 Reactive form: Email (required, email validator), Password (required, minlength 8)
  - [x] 6.3 Inline validation on blur (same pattern as RegisterComponent — `updateOn: 'blur'`, red border + helper text)
  - [x] 6.4 On submit: call AuthService.login(), on success navigate to `/`, on 401 show inline alert "Invalid email or password" above form, on 429 show "Too many login attempts. Please try again later."
  - [x] 6.5 Password field cleared on server error, email preserved
  - [x] 6.6 Submit button disabled until form valid, shows spinner during request
- [x] Task 7: AuthService Login Method (AC: #1)
  - [x] 7.1 Add `login(request: LoginRequest)` method to `AuthService` — POST `/api/auth/login` with `withCredentials: true`
  - [x] 7.2 On success: call `setSession()` same as register flow
  - [x] 7.3 Add `LoginRequest` interface: `{ email: string, password: string }`
- [x] Task 8: Route Updates (AC: #1, #5)
  - [x] 8.1 Update `app.routes.ts`: change `'login'` path from redirect-to-register to load `LoginComponent`
  - [x] 8.2 Update `authGuard` to redirect unauthenticated users to `/login` instead of `/register`
  - [x] 8.3 Add "Already have an account? Sign in" link on RegisterComponent, "Don't have an account? Register" link on LoginComponent
- [x] Task 9: HTTP Interceptor for Bearer Token (AC: #1)
  - [x] 9.1 Create `auth.interceptor.ts` in `src/Web/ClientApp/src/app/core/services/` — functional interceptor using `HttpInterceptorFn`
  - [x] 9.2 Read access token from `AuthService.accessToken` getter, add `Authorization: Bearer {token}` header to all API requests (except `/api/auth/login` and `/api/auth/register`)
  - [x] 9.3 Register interceptor in `app.config.ts` via `provideHttpClient(withInterceptors([authInterceptor]))`
- [x] Task 10: Unit Tests
  - [x] 10.1 `LoginCommandValidatorTests` — 5 tests: valid input, missing email, invalid email, empty password, short password
  - [x] 10.2 `LoginCommandHandlerTests` — 6 tests: successful login, wrong password returns error, user not found returns error, deactivated user returns error, tokens generated correctly + session stored in Redis, admin role returned correctly
- [x] Task 11: Integration Tests
  - [x] 11.1 POST `/api/auth/login` with valid credentials → 200 + access token + refresh cookie
  - [x] 11.2 POST `/api/auth/login` with wrong password → 401 generic error
  - [x] 11.3 POST `/api/auth/login` with non-existent email → 401 generic error
  - [x] 11.4 POST `/api/auth/login` with deactivated user → 401 generic error
  - [x] 11.5 POST `/api/auth/login` with invalid email → 400 validation error

## Dev Notes

### Existing Infrastructure to Reuse (DO NOT Recreate)

- **JwtTokenService** (`src/Infrastructure/Services/JwtTokenService.cs`): Already generates access + refresh tokens. Call `GenerateTokens(user)` exactly as RegisterCommandHandler does.
- **RedisSessionService** (`src/Infrastructure/Services/RedisSessionService.cs`): Already stores sessions with `StoreSessionAsync(userId, refreshToken, expiry)`. Reuse directly.
- **BcryptPasswordHasher** (`src/Infrastructure/Services/BcryptPasswordHasher.cs`): Already registered as `IPasswordHasher<ApplicationUser>`. Use `UserManager.CheckPasswordAsync()` which delegates to this hasher.
- **AuthService** (Angular `src/Web/ClientApp/src/app/core/services/auth.service.ts`): Already has `setSession()`, `accessToken$` BehaviorSubject, `user$` state management. Add `login()` method following the same pattern as `register()`.
- **Auth endpoints** (`src/Web/Endpoints/Auth.cs`): Already has the register endpoint with cookie-setting pattern. Add login endpoint in the same file, same group.
- **RegisterComponent** (`src/Web/ClientApp/src/app/features/auth/register/`): Use as template for LoginComponent — same form validation patterns, error display, loading state.

### Critical Implementation Patterns

**Password Verification Flow:**
```
1. Find user by email: await _userManager.FindByEmailAsync(email)
2. If user is null → return generic error (do NOT distinguish from wrong password)
3. Verify password: await _userManager.CheckPasswordAsync(user, password)
4. If password wrong → return generic error
5. Check user.IsActive → if false, return generic error (same message!)
6. Generate tokens + store session → return success
```

**Cookie Pattern (copy from register endpoint in Auth.cs):**
```csharp
context.Response.Cookies.Append("refresh_token", result.RefreshToken, new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict,
    Expires = DateTimeOffset.UtcNow.AddDays(7)
});
```

**Rate Limiting Architecture:**
- Use built-in `Microsoft.AspNetCore.RateLimiting` (already available in .NET 9)
- No new NuGet packages needed — this is part of the framework
- Two policies on login endpoint: per-IP (from `RemoteIpAddress`) and per-email (from request body)
- Per-email rate limiting requires reading the request body — use a custom `IRateLimiterPolicy<HttpContext>` or partition by email extracted from the request

**Angular Interceptor Pattern:**
```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.accessToken;
  if (token && !req.url.includes('/api/auth/login') && !req.url.includes('/api/auth/register')) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  return next(req);
};
```

### Deferred Items from Story 2-1 to Address

| Item | Action in This Story |
|------|---------------------|
| CORS `SetIsOriginAllowed(_ => true)` | Replace with explicit origin whitelist from config |
| JWT secret is hardcoded placeholder | Add startup warning log if default value detected |
| No rate limiting on `/api/auth/register` | Apply same rate limiting policies to register endpoint |
| `provideHttpClient()` missing options | Add `withInterceptors()` when registering interceptor |

### Items NOT in Scope (Deferred to Later Stories)

- Token refresh endpoint (`/api/auth/refresh`) → Story 2.3
- Auth state persistence on page refresh → Story 2.3
- Logout endpoint → Story 2.4
- 401 interceptor with token refresh retry → Story 2.3
- TOCTOU race on first-user admin → accepted risk for single-instance MVP
- Non-atomic Redis session operations → low risk for MVP

### Project Structure Notes

**New files to create:**
```
src/Application/Identity/Commands/Login/
  LoginCommand.cs
  LoginCommandHandler.cs
  LoginCommandValidator.cs
src/Web/ClientApp/src/app/features/auth/login/
  login.component.ts
  login.component.html
  login.component.scss
src/Web/ClientApp/src/app/core/services/
  auth.interceptor.ts
```

**Files to modify:**
```
src/Web/Endpoints/Auth.cs                    — add login endpoint
src/Web/Program.cs                           — add rate limiting middleware, CORS config, JWT warning
src/Web/appsettings.json                     — add RateLimit and Cors config sections
src/Web/ClientApp/src/app/app.routes.ts      — update login route
src/Web/ClientApp/src/app/app.config.ts      — register interceptor
src/Web/ClientApp/src/app/core/services/auth.service.ts — add login() method
src/Web/ClientApp/src/app/core/guards/auth.guard.ts     — redirect to /login
src/Web/ClientApp/src/app/features/auth/register/register.component.html — add login link
```

### UX Requirements

- Form follows same validation pattern as RegisterComponent: inline validation on blur, red border + red helper text, friendly error messages
- Labels always visible above fields (not placeholder-only)
- Submit button: full width on mobile, right-aligned on desktop, disabled until valid, loading spinner during request
- Server errors shown as inline alert above form (not toast)
- "Invalid email or password" — always this exact message, never reveal which field is wrong
- Rate limit error: "Too many login attempts. Please try again later."
- Navigation links between Login and Register pages

### Security Requirements

- Never reveal whether an email exists in the system via login errors (NFR11)
- Access token in response body only, never in cookies or URL (XSS prevention)
- Refresh token as HttpOnly Secure SameSite=Strict cookie only (XSS prevention)
- Rate limiting prevents brute-force credential stuffing (per-IP + per-username)
- Deactivated users cannot obtain tokens (NFR13)
- CORS restricted to configured origins (no more wildcard)

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-2-user-authentication-account-management.md#Story 2.2]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication & Security]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication Flow]
- [Source: _bmad-output/planning-artifacts/prd.md#FR2]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR7, NFR9, NFR12, NFR13]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Form Patterns]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md]
- [Source: _bmad-output/implementation-artifacts/2-1-user-registration-with-first-user-admin-designation.md]

### Review Findings

- [x] [Review][Decision] Per-user rate limiting configured but never enforced — FIXED: Added "auth-per-user" rate limit policy partitioned by email from request body (5/min per email). Login endpoint uses per-user policy; register keeps per-IP only.
- [x] [Review][Decision] Login validator leaks password policy via MinimumLength validation — FIXED: Removed MinimumLength(8) from LoginCommandValidator; login now only validates NotEmpty. Angular login form validator also relaxed.
- [x] [Review][Patch] Timing side-channel for user enumeration — FIXED: Added VerifyDummyPasswordAsync to IIdentityService/IdentityService; LoginCommandHandler calls it on null-user path to equalize bcrypt timing.
- [x] [Review][Patch] ProblemDetailsExceptionHandler leaks .NET default message on authorization 401s — FIXED: Detail only set when message differs from .NET default; AuthorizationBehaviour's no-message throw now returns null Detail.
- [x] [Review][Patch] isSubmitting not reset on navigation success path — FIXED: isSubmitting reset to false before router.navigate in success handler.
- [x] [Review][Defer] Client logout doesn't invalidate server session — deferred, Story 2.4 scope (logout endpoint)
- [x] [Review][Defer] ResetDatabaseAsync hardcoded SQL will break with future domain tables — deferred, pre-existing test infrastructure concern
- [x] [Review][Defer] Refresh token expiry (7 days) hardcoded in 4 locations with no shared constant — deferred, maintainability concern for future refactor

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

### Completion Notes List

- LoginCommand + Handler + Validator created following RegisterCommand patterns; uses IIdentityService.FindUserByEmailAsync + CheckPasswordAsync (new methods added to interface and IdentityService)
- Login endpoint added to Auth.cs with same cookie-setting pattern as register; refactored cookie code to shared SetRefreshTokenCookie method
- ProblemDetailsExceptionHandler updated to include Detail message on 401 (safe — only "Invalid email or password." is ever thrown)
- Rate limiting implemented using built-in Microsoft.AspNetCore.RateLimiting with per-IP fixed-window policy; applied to both login and register endpoints
- JWT secret placeholder warning logs at startup via Serilog (confirmed working in build output)
- CORS hardened: development allows all origins, production requires Cors:AllowedOrigins config or falls back to localhost-only
- Angular LoginComponent mirrors RegisterComponent patterns with 2 fields (email, password), inline validation on blur, server error display, spinner
- AuthService.login() method added following same pattern as register(); LoginRequest interface exported
- HTTP auth interceptor created as functional HttpInterceptorFn; excludes auth endpoints from Bearer token injection
- Routes updated: /login loads LoginComponent, authGuard redirects to /login, cross-links between Register and Login pages
- 11 new unit tests (5 validator + 6 handler), all passing; 37 total unit tests pass with 0 regressions
- 6 integration tests written for login flow; pre-existing integration test failures due to Identity password validators rejecting test passwords (not introduced by this story)
- Test infrastructure improved: added ResetDatabaseAsync to CustomWebApplicationFactory for test data isolation

### Change Log

- 2026-03-30: Story 2.2 implemented — login endpoint, rate limiting, CORS hardening, JWT secret warning, Angular login UI, HTTP interceptor

### File List

New files:
- src/Application/Identity/Commands/Login/LoginCommand.cs
- src/Application/Identity/Commands/Login/LoginResponse.cs
- src/Application/Identity/Commands/Login/LoginCommandValidator.cs
- src/Application/Identity/Commands/Login/LoginCommandHandler.cs
- src/Web/ClientApp/src/app/features/auth/login/login.component.ts
- src/Web/ClientApp/src/app/features/auth/login/login.component.html
- src/Web/ClientApp/src/app/features/auth/login/login.component.scss
- src/Web/ClientApp/src/app/core/services/auth.interceptor.ts
- tests/Application.UnitTests/Identity/Commands/Login/LoginCommandValidatorTests.cs
- tests/Application.UnitTests/Identity/Commands/Login/LoginCommandHandlerTests.cs

Modified files:
- src/Application/Common/Interfaces/IIdentityService.cs (added FindUserByEmailAsync, CheckPasswordAsync)
- src/Infrastructure/Identity/IdentityService.cs (implemented FindUserByEmailAsync, CheckPasswordAsync)
- src/Web/Endpoints/Auth.cs (added login endpoint, rate limiting, refactored cookie helper)
- src/Web/Program.cs (rate limiting middleware, CORS hardening, JWT secret warning)
- src/Web/appsettings.json (added RateLimit config section)
- src/Web/Infrastructure/ProblemDetailsExceptionHandler.cs (added Detail to 401 responses)
- src/Web/ClientApp/src/app/core/services/auth.service.ts (added login method, LoginRequest interface)
- src/Web/ClientApp/src/app/core/guards/auth.guard.ts (redirect to /login)
- src/Web/ClientApp/src/app/app.routes.ts (login route loads LoginComponent)
- src/Web/ClientApp/src/app/app.config.ts (registered auth interceptor)
- src/Web/ClientApp/src/app/features/auth/register/register.component.ts (added RouterLink import)
- src/Web/ClientApp/src/app/features/auth/register/register.component.html (added login link)
- src/Web/ClientApp/src/app/features/auth/register/register.component.scss (added auth-link style)
- tests/Infrastructure.IntegrationTests/Api/AuthEndpointTests.cs (added login tests, ResetDatabaseAsync)
- tests/Infrastructure.IntegrationTests/CustomWebApplicationFactory.cs (added ResetDatabaseAsync)
- _bmad-output/implementation-artifacts/sprint-status.yaml (epic-1 done, story 2-2 in-progress)
