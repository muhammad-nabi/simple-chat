# Story 2.1: User Registration with First-User Admin Designation

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a new team member,
I want to register an account with my display name, email, and password,
so that I can access the chat application and start communicating with my team.

## Acceptance Criteria

1. **Given** the registration page is displayed
   **When** a user submits a valid display name, email address, and password
   **Then** a new User entity is created with the provided display name, email, and a bcrypt-hashed password (NFR7)
   **And** the user is assigned the Member role by default
   **And** the user is automatically logged in (JWT access token issued, refresh token set as HttpOnly cookie)
   **And** the user is redirected to the main conversation list view — no redirect to login page

2. **Given** no users exist in the system (fresh deployment)
   **When** the first user completes registration
   **Then** that user is automatically designated as the instance administrator (UserRole.Admin) (FR5)
   **And** subsequent registrations create Member-role users

3. **Given** the registration form
   **When** the user submits an email that is already registered
   **Then** an inline error alert is displayed above the form: "An account with this email already exists"
   **And** the form is not cleared — user input is preserved

4. **Given** the registration form (UX-DR16)
   **When** the user blurs a field with invalid input
   **Then** inline validation errors appear below the field in friendly language (e.g., "Please enter a valid email", "Password must be at least 8 characters")
   **And** the field border turns red with red helper text
   **And** the Submit button remains disabled until all fields are valid

5. **Given** the registration form
   **When** the user submits a password shorter than 8 characters
   **Then** inline validation rejects with: "Password must be at least 8 characters"
   **And** no complexity rules beyond minimum length are enforced in V1

6. **Given** the Register component layout
   **When** rendered on any viewport
   **Then** the form displays three fields: Display Name, Email, Password — each with a visible label above the field (not placeholder-only)
   **And** the Submit button shows a loading spinner during submission and is disabled to prevent double-submit
   **And** the form is accessible: labels associated with inputs, validation errors linked via aria-describedby

## Tasks / Subtasks

- [ ] Task 0: Architecture Decision — ASP.NET Core Identity vs Custom User Entity (AC: all)
  - [ ] 0.1 The codebase currently uses ASP.NET Core Identity (`IdentityDbContext<ApplicationUser>`, `UserManager<ApplicationUser>`) from the Jason Taylor template. The architecture spec defines a custom `User.cs` domain entity with `Id (long)`, bcrypt hashing, and JWT auth. **Decision required:** extend `ApplicationUser` with domain fields (DisplayName, Role, IsActive) and use Identity's infrastructure, OR create a separate domain `User` entity and bypass Identity. **Recommended: extend ApplicationUser** — ripping out Identity is high-risk scope creep; extending it preserves working infrastructure while satisfying the domain model requirements. If extending, override password hashing with bcrypt via custom `IPasswordHasher<ApplicationUser>` to satisfy architecture spec
  - [ ] 0.2 Note: `ApplicationUser : IdentityUser` currently uses `string` (GUID) for Id, while architecture specifies `long`. Accept the string Id for now — changing Identity's key type requires a full `IdentityDbContext<ApplicationUser, IdentityRole, long>` refactor. Document this as a known deviation

- [ ] Task 1: Domain Layer — User Entity & Enums (AC: #1, #2)
  - [ ] 1.1 `UserRole.cs` already exists at `src/Domain/Common/Enums/UserRole.cs` with values `Admin, Member` — **no changes needed**, verify it matches
  - [ ] 1.2 Extend `ApplicationUser` in `src/Infrastructure/Identity/ApplicationUser.cs`: add `DisplayName (string, required)`, `Role (UserRole)`, `IsActive (bool, default true)`. Keep inheriting from `IdentityUser`
  - [ ] 1.3 Create EF configuration `src/Infrastructure/Data/Configurations/ApplicationUserConfiguration.cs`: configure `DisplayName` (required, maxLength 256), `Email` (unique index `UQ_Users_Email`, maxLength 254), `Role` (required), `IsActive` (required, default true)

- [ ] Task 2: Infrastructure — Password Hashing with bcrypt (AC: #1)
  - [ ] 2.1 Add `BCrypt.Net-Next` version `4.1.0` to `Directory.Packages.props`
  - [ ] 2.2 Add `<PackageReference Include="BCrypt.Net-Next" />` to `src/Infrastructure/Infrastructure.csproj`
  - [ ] 2.3 Create `src/Infrastructure/Services/BcryptPasswordHasher.cs` implementing `IPasswordHasher<ApplicationUser>` (NOT `IAuthenticationProvider` — use Identity's extension point so `UserManager` uses bcrypt transparently). `HashPassword` → `BCrypt.Net.BCrypt.HashPassword(password)`, `VerifyHashedPassword` → `BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword)` returning `PasswordVerificationResult.Success`/`Failed`
  - [ ] 2.4 Register `BcryptPasswordHasher` in `src/Infrastructure/DependencyInjection.cs`: `services.AddScoped<IPasswordHasher<ApplicationUser>, BcryptPasswordHasher>()`  — this replaces Identity's default PBKDF2 hasher
  - [ ] 2.5 Also create `src/Infrastructure/Services/BcryptAuthenticationProvider.cs` implementing `IAuthenticationProvider` (the existing interface at `src/Application/Common/Interfaces/IAuthenticationProvider.cs`) — delegates to `BCrypt.Net.BCrypt` directly. Register in DI. This satisfies the architecture's clean interface for non-Identity contexts

- [ ] Task 3: Infrastructure — JWT Token Generation (AC: #1)
  - [ ] 3.1 Add `Microsoft.AspNetCore.Authentication.JwtBearer` version `10.0.5` to `Directory.Packages.props` and `src/Web/Web.csproj`
  - [ ] 3.2 Note: `System.IdentityModel.Tokens.Jwt (8.16.0)` is already in `Directory.Packages.props` — it is officially legacy. Add `Microsoft.IdentityModel.JsonWebTokens (8.17.0)` as the replacement. Use `JsonWebTokenHandler` (not `JwtSecurityTokenHandler`) for token creation
  - [ ] 3.3 Create `src/Application/Common/Interfaces/IJwtTokenService.cs`: `Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(string userId, string email, string displayName, UserRole role)`
  - [ ] 3.4 Create `src/Infrastructure/Services/JwtTokenService.cs` implementing `IJwtTokenService`. Access token: 30 min expiry (from `JWT_EXPIRY_MINUTES` env var), claims: sub=userId, email, name=displayName, role. Refresh token: cryptographically random 32-byte base64 string. Sign with `JWT_SECRET` env var (already configured in `.env`)
  - [ ] 3.5 Create `src/Application/Common/Interfaces/ISessionService.cs`: `Task StoreSessionAsync(string userId, string refreshToken, TimeSpan expiry)`, `Task<string?> GetSessionUserIdAsync(string refreshToken)`, `Task InvalidateSessionAsync(string refreshToken)`, `Task InvalidateAllSessionsAsync(string userId)`
  - [ ] 3.6 Create `src/Infrastructure/Services/RedisSessionService.cs` implementing `ISessionService`. Key pattern: `session:{refreshToken}` → userId. TTL: 7 days. Use `IConnectionMultiplexer` already registered in DI

- [ ] Task 4: Web Layer — JWT Authentication Middleware (AC: #1)
  - [ ] 4.1 In `src/Web/DependencyInjection.cs` or `src/Infrastructure/DependencyInjection.cs`: replace the current `IdentityConstants.ApplicationScheme` (cookie-based) authentication with JWT Bearer authentication. Configure `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` with `TokenValidationParameters`: validate issuer signing key (from `JWT_SECRET`), validate lifetime, do NOT validate issuer/audience (single-instance app)
  - [ ] 4.2 **CRITICAL:** Do NOT remove the Identity service registrations (`AddIdentityCore<ApplicationUser>`, `UserManager`, etc.) — only change the authentication scheme from cookies to JWT Bearer. Identity's user management is still needed
  - [ ] 4.3 Ensure `app.UseAuthentication()` and `app.UseAuthorization()` remain in the pipeline (already present in `Program.cs`)

- [ ] Task 5: Application Layer — Register Command (AC: #1, #2, #3, #5)
  - [ ] 5.1 Create `src/Application/Identity/Commands/Register/RegisterCommand.cs`: `public record RegisterCommand(string DisplayName, string Email, string Password) : IRequest<RegisterResponse>`
  - [ ] 5.2 Create `src/Application/Identity/Commands/Register/RegisterResponse.cs`: `public record RegisterResponse(string AccessToken, string UserId, string DisplayName, string Email, string Role)`
  - [ ] 5.3 Create `src/Application/Identity/Commands/Register/RegisterCommandValidator.cs`:
    - `Email`: NotEmpty, EmailAddress, MaximumLength(254)
    - `DisplayName`: NotEmpty, MaximumLength(256)
    - `Password`: NotEmpty, MinimumLength(8) — NO complexity rules in V1
  - [ ] 5.4 Create `src/Application/Identity/Commands/Register/RegisterCommandHandler.cs`:
    1. Check if email already exists → throw validation exception "An account with this email already exists"
    2. Determine role: query `Users.AnyAsync()` — if no users exist → `UserRole.Admin`, else → `UserRole.Member`
    3. Create `ApplicationUser` with DisplayName, Email, Role, IsActive=true
    4. Use `UserManager.CreateAsync(user, password)` — bcrypt hasher kicks in via DI
    5. Generate JWT + refresh token via `IJwtTokenService`
    6. Store session in Redis via `ISessionService`
    7. Return `RegisterResponse` with access token and user details
  - [ ] 5.5 **PII SAFETY:** The `RegisterCommand` contains a `Password` field. The existing `LoggingBehaviour` was fixed in Story 1.4 to NOT log request objects that might contain sensitive data. Verify that the `[SensitiveData]` attribute or similar exclusion mechanism is applied. If the logging behavior logs the full request by default, this MUST be addressed — never log passwords

- [ ] Task 6: Web Layer — Auth Controller (AC: #1, #2, #3)
  - [ ] 6.1 Create `src/Web/Controllers/AuthController.cs` with `[Route("api/auth")]`
  - [ ] 6.2 `[HttpPost("register")]` endpoint: accepts `RegisterRequest` DTO (displayName, email, password), dispatches `RegisterCommand` via MediatR, returns `RegisterResponse` in body
  - [ ] 6.3 Set refresh token as HttpOnly cookie: `Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTimeOffset.UtcNow.AddDays(7) })`
  - [ ] 6.4 On duplicate email (validation exception from handler), return Problem Details 400 with message "An account with this email already exists"
  - [ ] 6.5 On other validation errors, let the `ValidationBehaviour` pipeline return Problem Details 400 automatically
  - [ ] 6.6 Register endpoint must be `[AllowAnonymous]` — no authentication required

- [ ] Task 7: Database Migration (AC: #1, #2)
  - [ ] 7.1 Generate EF Core migration: `dotnet ef migrations add AddUserFields --project src/Infrastructure --startup-project src/Web`
  - [ ] 7.2 **CRITICAL from deferred-work.md:** "First domain entity migration will expose `BaseEntity.CreatedAt` as a new non-nullable `datetimeoffset` column — ensure migration includes appropriate default value or make column nullable during Story 2.1 migration generation." Check if `CreatedAt` (DateTimeOffset) appears in the migration and handle accordingly
  - [ ] 7.3 Review the generated migration — verify it adds DisplayName, Role, IsActive columns to the AspNetUsers table (Identity's table). Verify unique index on Email
  - [ ] 7.4 Verify migration is idempotent: `MigrateAsync()` on startup already handles this (Story 1.3)

- [ ] Task 8: Frontend — Angular Registration Component (AC: #4, #5, #6)
  - [ ] 8.1 Create Angular feature module: `src/Web/ClientApp/src/app/features/auth/` with routing
  - [ ] 8.2 Create `register.component.ts` with reactive form: displayName (required), email (required, email pattern), password (required, minLength 8)
  - [ ] 8.3 Implement inline validation on blur (NOT on keystroke per UX spec):
    - Display Name: "Display name is required"
    - Email: "Please enter a valid email"
    - Password: "Password must be at least 8 characters"
  - [ ] 8.4 Field styling: red border + red helper text below field on validation error. Labels visible above fields (not placeholder-only). Placeholders provide examples
  - [ ] 8.5 Submit button: disabled until all fields valid, shows loading spinner during submission, disabled during request to prevent double-submit
  - [ ] 8.6 Server error display: inline alert above form (NOT toast). On duplicate email: "An account with this email already exists". Form preserved (not cleared)
  - [ ] 8.7 Accessibility: `<label for="...">` associated with inputs, `aria-describedby` linking validation error messages to inputs, form within `<main>` landmark

- [ ] Task 9: Frontend — Auth Service & Token Management (AC: #1)
  - [ ] 9.1 Create `src/Web/ClientApp/src/app/core/services/auth.service.ts`: store access token in BehaviorSubject (service memory, NOT localStorage — XSS prevention per architecture). Expose `isAuthenticated$`, `currentUser$` observables
  - [ ] 9.2 Create `src/Web/ClientApp/src/app/core/services/api.service.ts` or use HttpClient: `POST /api/auth/register` with `{ displayName, email, password }`. HttpClient automatically sends/receives cookies (refresh token)
  - [ ] 9.3 On successful registration: store access token in AuthService, navigate to `/` (conversation list). The conversation list view doesn't exist yet — create a placeholder route that shows "Welcome! Join a conversation to get started." (per UX empty state spec)
  - [ ] 9.4 Create auth guard: redirect unauthenticated users to `/register` (or `/login` once Story 2.2 exists)

- [ ] Task 10: Frontend — Routing & App Shell (AC: #1, #6)
  - [ ] 10.1 Set up lazy-loaded routes: `/register` → RegisterComponent, `/login` → placeholder (Story 2.2), `/` → conversation list placeholder (Story 3.x)
  - [ ] 10.2 Ensure `/register` is the default route for unauthenticated users
  - [ ] 10.3 Responsive form layout: full-width on mobile, centered card on desktop (per UX form patterns: submit button full-width on mobile, right-aligned on desktop)

- [ ] Task 11: Remove Template Seed User (AC: #2)
  - [ ] 11.1 The template `ApplicationDbContextInitialiser` seeds a default user with hardcoded password "Administrator1!". **Remove this seed logic** — Story 2.1's first-user-admin designation replaces it. Keep `InitialiseAsync()` (migration) but remove `SeedAsync()` user creation
  - [ ] 11.2 Keep role seeding if present (ensure "Admin" and "Member" roles exist in AspNetRoles table)
  - [ ] 11.3 Mark deferred-work.md item "Hardcoded admin password" as resolved

- [ ] Task 12: Unit Tests (AC: all)
  - [ ] 12.1 `RegisterCommandValidatorTests.cs` — test all validation rules: empty fields, invalid email, short password, valid inputs. **100% coverage on validator required**
  - [ ] 12.2 `RegisterCommandHandlerTests.cs` — test: successful registration creates user with Member role, first-user gets Admin role, duplicate email throws, JWT tokens generated, session stored in Redis. Mock `UserManager`, `IJwtTokenService`, `ISessionService`. **100% coverage on handler required**
  - [ ] 12.3 `BcryptPasswordHasherTests.cs` — test hash and verify
  - [ ] 12.4 Test location: `tests/Application.UnitTests/Identity/Commands/Register/` — mirror source structure
  - [ ] 12.5 Test framework: **NUnit 4.5.1** (NOT xUnit — architecture spec says xUnit but codebase uses NUnit, established in Story 1.5). Use `Moq` for mocking

- [ ] Task 13: Integration Test (AC: #1, #2, #3)
  - [ ] 13.1 Create `tests/Infrastructure.IntegrationTests/Api/AuthEndpointTests.cs`
  - [ ] 13.2 Test: POST `/api/auth/register` with valid data returns 200 with JWT. Test: first user gets Admin role. Test: duplicate email returns 400 Problem Details
  - [ ] 13.3 Use `CustomWebApplicationFactory` (from Story 1.5) — Testcontainers real MSSQL + Redis already configured

- [ ] Task 14: Verification (AC: all)
  - [ ] 14.1 `dotnet build SimpleChat.slnx --configuration Release` — zero errors
  - [ ] 14.2 `dotnet test SimpleChat.slnx --filter "FullyQualifiedName~UnitTests"` — all pass
  - [ ] 14.3 `dotnet test SimpleChat.slnx --filter "FullyQualifiedName~IntegrationTests"` — all pass (requires Docker for Testcontainers)
  - [ ] 14.4 `cd src/Web/ClientApp && npx ng lint` — no errors
  - [ ] 14.5 `cd src/Web/ClientApp && npm test -- --ci` — all pass
  - [ ] 14.6 Manual: `docker compose up`, navigate to `http://localhost:8080/register`, register first user → verify admin role, register second user → verify member role

### Review Findings

_(Populated during code review)_

## Dev Notes

### CRITICAL: Existing ASP.NET Core Identity Infrastructure

The codebase uses ASP.NET Core Identity from the Jason Taylor Clean Architecture template. This is deeply integrated:
- `ApplicationDbContext : IdentityDbContext<ApplicationUser>` — creates AspNetUsers, AspNetRoles tables
- `UserManager<ApplicationUser>` — handles user creation, password hashing, email uniqueness
- `IdentityService : IIdentityService` — existing abstraction layer
- Cookie-based authentication currently configured

**The architecture spec defines a different approach** (custom `User` domain entity, direct bcrypt, no Identity). **Resolution:** extend the existing Identity infrastructure rather than replace it:
1. Add domain fields to `ApplicationUser` (DisplayName, Role, IsActive)
2. Override password hasher with bcrypt (via `IPasswordHasher<ApplicationUser>`)
3. Switch authentication from cookies to JWT Bearer
4. Add Redis session service for refresh tokens

**Known deviation from architecture:** User Id remains `string` (Identity's default) instead of `long`. Changing Identity's key type is a non-trivial refactor that doesn't add value for MVP.

### Password Hashing Strategy

The architecture specifies `BCrypt.Net-Next` for password hashing. Two integration points:
1. `IPasswordHasher<ApplicationUser>` — Identity's extension point, used by `UserManager.CreateAsync()` and `CheckPasswordAsync()`. Override this to use bcrypt so all Identity operations use bcrypt transparently
2. `IAuthenticationProvider` — the clean architecture interface (already exists). Implement with bcrypt for any direct usage outside Identity

**Do NOT use Identity's default PBKDF2 hasher** — the architecture explicitly requires bcrypt (NFR7).

### JWT Token Architecture

- **Access token:** 30 min expiry (configurable via `JWT_EXPIRY_MINUTES` env var, already in `.env`)
- **Signing key:** `JWT_SECRET` env var (already configured in `.env` and `docker-compose.yml`)
- **Refresh token:** Random 32-byte base64 string stored in Redis with 7-day TTL
- **Refresh token delivery:** HttpOnly, Secure, SameSite=Strict cookie
- **Access token delivery:** JSON response body → stored in Angular service memory (NOT localStorage)
- Use `JsonWebTokenHandler` (from `Microsoft.IdentityModel.JsonWebTokens`) not the legacy `JwtSecurityTokenHandler`

### Migration Caution

The deferred work register notes: "First domain entity migration will expose `BaseEntity.CreatedAt` as a new non-nullable `datetimeoffset` column." The `ApplicationUser` extends `IdentityUser` (not `BaseEntity`), so this may not apply directly. However, if any other entity inheriting `BaseEntity` gets migrated, watch for this. For `ApplicationUser`, add `CreatedAt` as a separate DateTimeOffset property if needed.

### Deferred Work Items Resolved by This Story

- "Hardcoded admin password 'Administrator1!' in ApplicationDbContextInitialiser" → Replaced by first-user-admin registration flow
- "Seed password externalization (Story 2.1)" from Epic 1 retro → Seed user removed entirely
- "provideHttpClient() missing XSRF/fetch options" → Partially addressed (HttpClient configured for auth requests)

### Deferred Work Items Relevant (Addressed in Later Stories)

- "JWT default secret is a committed known value" → Story 2.2 enforces secret validation
- "CORS AllowAny* not environment-gated" → Story 2.2 CORS hardening
- "provideHttpClient() missing XSRF" → Story 2.2 XSRF on HttpClient
- "Logout endpoint body check logic inverted" → Story 2.4

### Frontend UX Requirements (from UX Design Specification)

- **Form patterns:** 3 fields (Display Name, Email, Password), labels above fields, validation on blur not keystroke
- **Error display:** Inline below fields (validation), inline alert above form (server errors)
- **Submit button:** Full-width on mobile, right-aligned on desktop. Disabled until valid. Loading spinner during submission
- **No email verification gate** — user can chat immediately after registration
- **No onboarding wizard** — registration IS the onboarding
- **Auto-login** — no redirect to login page after registration
- **First-user admin** — communicated via subtle role badge (future), no configuration wizard
- **Color tokens:** Primary teal #128C7E, error red #DC3545, text primary #111B21, text secondary #667781
- **Typography:** System font stack, body 15px/400, button 14px/500, caption 12px/400
- **Spacing:** Base 4px unit scale

### NuGet Packages to Add

| Package | Version | Project | Purpose |
|---------|---------|---------|---------|
| BCrypt.Net-Next | 4.1.0 | Infrastructure | Password hashing |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.5 | Web | JWT middleware |
| Microsoft.IdentityModel.JsonWebTokens | 8.17.0 | Infrastructure | Token creation (replaces legacy System.IdentityModel.Tokens.Jwt) |

**Pre-approved exception to CLAUDE.md "don't add packages" rule** — this story explicitly requires new authentication packages.

### Naming Conventions (from architecture)

- **Commands:** `{Verb}{Noun}Command` → `RegisterCommand`
- **Handlers:** `{Command}Handler` → `RegisterCommandHandler`
- **Validators:** `{Command}Validator` → `RegisterCommandValidator`
- **DTOs:** `{Noun}Response` → `RegisterResponse`
- **Controllers:** `{Module}Controller` → `AuthController`
- **Services:** `{Implementation}Service` → `JwtTokenService`, `RedisSessionService`
- **Private fields:** `_camelCase` → `_userManager`, `_tokenService`
- **Async methods:** `Async` suffix → `GenerateTokensAsync`
- **API endpoints:** lowercase kebab-case → `POST /api/auth/register`
- **JSON fields:** camelCase → `{ "displayName", "email", "password" }`
- **Angular files:** kebab-case → `register.component.ts`, `auth.service.ts`
- **Angular services:** BehaviorSubject state, `$` suffix for observables → `isAuthenticated$`

### Project Structure Notes

Files to create:
```
src/Infrastructure/Services/BcryptPasswordHasher.cs
src/Infrastructure/Services/BcryptAuthenticationProvider.cs
src/Infrastructure/Services/JwtTokenService.cs
src/Infrastructure/Services/RedisSessionService.cs
src/Infrastructure/Data/Configurations/ApplicationUserConfiguration.cs
src/Application/Common/Interfaces/IJwtTokenService.cs
src/Application/Common/Interfaces/ISessionService.cs
src/Application/Identity/Commands/Register/RegisterCommand.cs
src/Application/Identity/Commands/Register/RegisterResponse.cs
src/Application/Identity/Commands/Register/RegisterCommandHandler.cs
src/Application/Identity/Commands/Register/RegisterCommandValidator.cs
src/Web/Controllers/AuthController.cs
src/Web/ClientApp/src/app/features/auth/register/register.component.ts
src/Web/ClientApp/src/app/features/auth/register/register.component.html
src/Web/ClientApp/src/app/features/auth/register/register.component.scss
src/Web/ClientApp/src/app/features/auth/auth.routes.ts
src/Web/ClientApp/src/app/core/services/auth.service.ts
src/Web/ClientApp/src/app/core/guards/auth.guard.ts
tests/Application.UnitTests/Identity/Commands/Register/RegisterCommandHandlerTests.cs
tests/Application.UnitTests/Identity/Commands/Register/RegisterCommandValidatorTests.cs
tests/Application.UnitTests/Infrastructure/Services/BcryptPasswordHasherTests.cs
tests/Infrastructure.IntegrationTests/Api/AuthEndpointTests.cs
```

Files to modify:
```
Directory.Packages.props — add BCrypt.Net-Next 4.1.0, JwtBearer 10.0.5, JsonWebTokens 8.17.0
src/Infrastructure/Infrastructure.csproj — add BCrypt.Net-Next, JsonWebTokens package refs
src/Web/Web.csproj — add JwtBearer package ref
src/Infrastructure/Identity/ApplicationUser.cs — add DisplayName, Role, IsActive
src/Infrastructure/DependencyInjection.cs — register BcryptPasswordHasher, JWT auth, services
src/Web/DependencyInjection.cs — add JWT Bearer authentication scheme
src/Infrastructure/Data/ApplicationDbContext.cs — ensure ApplicationUser configuration applied
src/Infrastructure/Data/ApplicationDbContextInitialiser.cs — remove seed user, keep role seeding
src/Web/ClientApp/src/app/app.routes.ts — add auth routes
_bmad-output/implementation-artifacts/deferred-work.md — mark resolved items
```

Files NOT to modify:
```
src/Web/Program.cs — authentication/authorization middleware already in pipeline
src/Domain/Common/Enums/UserRole.cs — already correct with Admin, Member
docker-compose.yml — JWT env vars already configured
.env / .env.example — JWT vars already present
```

### References

- [Source: epics/epic-2-user-authentication-account-management.md — Story 2.1 acceptance criteria]
- [Source: architecture.md — §Authentication: JWT + Redis session validation, bcrypt, IAuthenticationProvider]
- [Source: architecture.md — §Data Model: User entity (Id, Email, DisplayName, PasswordHash, Role, IsActive)]
- [Source: architecture.md — §Identity Module: file structure, command/query patterns]
- [Source: architecture.md — §Naming Conventions: PascalCase classes, _camelCase fields, kebab-case API endpoints]
- [Source: architecture.md — §Error Handling: Problem Details RFC 9457]
- [Source: architecture.md — §Testing: NUnit 4.5.1 (codebase), 100% handler/validator coverage, Testcontainers]
- [Source: prd.md — FR1 (registration), FR5 (first-user admin), NFR7 (bcrypt), NFR12 (auth enforcement)]
- [Source: ux-design-specification.md — §Form Patterns: inline validation on blur, labels above fields, submit button states]
- [Source: ux-design-specification.md — §Journey 1: Registration → First Message, auto-login, no onboarding]
- [Source: ux-design-specification.md — §Color System: primary #128C7E, error #DC3545]
- [Source: epic-1-retro-2026-03-29.md — §Epic 2 Preparation: Redis requirepass (done), timestamp standardization (done), CI verification]
- [Source: deferred-work.md — timestamp migration warning, seed password externalization]
- [Source: spec-prep-standardize-timestamps.md — BaseEntity.CreatedAt now DateTimeOffset]
- [Source: spec-prep-redis-requirepass.md — Redis auth configured, password in .env]
- [Source: 1-5-ci-pipeline-configuration.md — NUnit 4.5.1, CustomWebApplicationFactory, Testcontainers fixture]

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### Change Log

### File List
