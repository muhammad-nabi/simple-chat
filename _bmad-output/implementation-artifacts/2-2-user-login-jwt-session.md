# Story 2.2: User Login & JWT Session

Status: ready-for-dev

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

- [ ] Task 1: Backend LoginCommand + Handler (AC: #1, #2, #3)
  - [ ] 1.1 Create `LoginCommand.cs` in `src/Application/Identity/Commands/Login/` with Email and Password properties
  - [ ] 1.2 Create `LoginCommandValidator.cs` — validate Email (required, valid format) and Password (required, min 8 chars)
  - [ ] 1.3 Create `LoginCommandHandler.cs` — verify credentials via UserManager, check IsActive, generate tokens via IJwtTokenService, store session via ISessionService, return LoginResponse (same shape as RegisterResponse: AccessToken, RefreshToken, UserId, DisplayName, Email, Role)
  - [ ] 1.4 Handle deactivated users: after password verification succeeds, check `user.IsActive == false` → return generic "Invalid email or password" error (same as wrong password)
- [ ] Task 2: Backend Login Endpoint (AC: #1, #2, #3)
  - [ ] 2.1 Add `POST /api/auth/login` endpoint in `src/Web/Endpoints/Auth.cs` — AllowAnonymous, same cookie-setting pattern as register
  - [ ] 2.2 Return 200 with AccessToken in body on success, set refresh token as HttpOnly cookie
  - [ ] 2.3 Return 401 with generic error on invalid credentials or deactivated user
- [ ] Task 3: Rate Limiting (AC: #4)
  - [ ] 3.1 Add `Microsoft.AspNetCore.RateLimiting` middleware in `Program.cs`
  - [ ] 3.2 Configure fixed-window rate limiter with two policies: `login-per-ip` (20/min) and `login-per-user` (5/min based on request body email)
  - [ ] 3.3 Read limits from configuration: `RateLimit:LoginPerMinutePerIp` and `RateLimit:LoginPerMinutePerUser` with defaults 20 and 5
  - [ ] 3.4 Apply rate limiting to `/api/auth/login` endpoint (and `/api/auth/register` — deferred from 2-1)
  - [ ] 3.5 Return 429 Too Many Requests when limit exceeded
- [ ] Task 4: JWT Secret Validation (AC: #6)
  - [ ] 4.1 Add startup check in `Program.cs` or DI registration: if `Jwt:Secret` equals the placeholder value, log a warning via ILogger
- [ ] Task 5: CORS Hardening (deferred from Story 2-1)
  - [ ] 5.1 Replace `SetIsOriginAllowed(_ => true)` with explicit origin whitelist from configuration (`Cors:AllowedOrigins`)
  - [ ] 5.2 In development, default to `https://localhost:*` patterns; in production, require explicit configuration
- [ ] Task 6: Angular LoginComponent (AC: #1, #2, #5)
  - [ ] 6.1 Create `LoginComponent` in `src/Web/ClientApp/src/app/features/auth/login/` — mirror RegisterComponent patterns
  - [ ] 6.2 Reactive form: Email (required, email validator), Password (required, minlength 8)
  - [ ] 6.3 Inline validation on blur (same pattern as RegisterComponent — `markAsTouched` on blur, red border + helper text)
  - [ ] 6.4 On submit: call AuthService.login(), on success navigate to `/`, on 401 show inline alert "Invalid email or password" above form, on 429 show "Too many login attempts. Please try again later."
  - [ ] 6.5 Password field cleared on server error, email preserved
  - [ ] 6.6 Submit button disabled until form valid, shows spinner during request
- [ ] Task 7: AuthService Login Method (AC: #1)
  - [ ] 7.1 Add `login(request: LoginRequest)` method to `AuthService` — POST `/api/auth/login` with `withCredentials: true`
  - [ ] 7.2 On success: call `setSession()` same as register flow
  - [ ] 7.3 Add `LoginRequest` interface: `{ email: string, password: string }`
- [ ] Task 8: Route Updates (AC: #1, #5)
  - [ ] 8.1 Update `app.routes.ts`: change `'login'` path from redirect-to-register to load `LoginComponent`
  - [ ] 8.2 Update `authGuard` to redirect unauthenticated users to `/login` instead of `/register`
  - [ ] 8.3 Add "Already have an account? Log in" link on RegisterComponent, "Don't have an account? Register" link on LoginComponent
- [ ] Task 9: HTTP Interceptor for Bearer Token (AC: #1)
  - [ ] 9.1 Create `auth.interceptor.ts` in `src/Web/ClientApp/src/app/core/services/` — functional interceptor using `HttpInterceptorFn`
  - [ ] 9.2 Read access token from `AuthService.accessToken` getter, add `Authorization: Bearer {token}` header to all API requests (except `/api/auth/login` and `/api/auth/register`)
  - [ ] 9.3 Register interceptor in `app.config.ts` via `provideHttpClient(withInterceptors([authInterceptor]))`
- [ ] Task 10: Unit Tests
  - [ ] 10.1 `LoginCommandValidatorTests` — valid input, missing email, invalid email, short password
  - [ ] 10.2 `LoginCommandHandlerTests` — successful login, wrong password returns error, user not found returns error, deactivated user returns error, tokens generated correctly, session stored in Redis
- [ ] Task 11: Integration Tests
  - [ ] 11.1 POST `/api/auth/login` with valid credentials → 200 + access token + refresh cookie
  - [ ] 11.2 POST `/api/auth/login` with wrong password → 401 generic error
  - [ ] 11.3 POST `/api/auth/login` with non-existent email → 401 generic error
  - [ ] 11.4 POST `/api/auth/login` with deactivated user → 401 generic error
  - [ ] 11.5 Rate limiting triggers 429 after threshold exceeded

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

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
