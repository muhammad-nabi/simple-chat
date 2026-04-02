# Deferred Work

## Deferred from: code review of story-1-1 (2026-03-28)

- CORS AllowAny* not environment-gated in Program.cs — production CORS hardening belongs to Story 1.2/security
- ~~Duplicate creation timestamps: BaseEntity.CreatedAt (DateTime) vs BaseAuditableEntity.Created (DateTimeOffset) — reconcile when entities created in Stories 2.x/3.x~~ **RESOLVED** (prep-standardize-timestamps, 2026-03-29): Standardized to DateTimeOffset on BaseEntity.CreatedAt, removed redundant Created from BaseAuditableEntity
- DB initializer calls EnsureDeletedAsync then EnsureCreatedAsync — destroys data every startup, Story 1.3 covers proper DB initialization
- ~~Hardcoded admin password "Administrator1!" in ApplicationDbContextInitialiser — template default, should be externalized before production~~ **RESOLVED** (Story 2.1, 2026-03-30): Seed user removed entirely; first-user admin designation via RegisterCommand
- .GetAwaiter().GetResult() in DispatchDomainEventsInterceptor sync path — deadlock risk under load
- Domain events dispatched before SaveChanges commits — side-effects fire for uncommitted state
- Domain event handler exception leaves events partially cleared — no retry possible for failed events
- Role authorization doesn't trim whitespace in comma-separated roles — "Admin, Editor" split produces " Editor" with leading space
- Seed user creation result not checked — silent failure if password policy rejects
- Logout endpoint body check logic inverted — any JSON body passes, no body returns 401
- PerformanceBehaviour Stopwatch not reset between calls — elapsed time accumulates across requests
- Auditable entity interceptor writes null UserId for system/background operations
- Logging behaviour logs entire request object including potential PII (passwords, tokens)
- ExceptionHandler middleware registered after static files middleware — file-serving exceptions bypass custom handler
- provideHttpClient() missing XSRF/fetch options — address when auth is implemented (Story 2.2)
- Production bundle budget thresholds too permissive (1MB warn / 5MB error vs Angular default 500kB / 1MB)
- No dark-mode token set — picoColorScheme script implied dark mode was planned but no token overrides exist

## Deferred from: code review of story-1-2 (2026-03-29)

- Dockerfile has no USER directive — app runs as root in container; add non-root user for security hardening
- Special characters in MSSQL_SA_PASSWORD (e.g., `"`, `$`, `;`) could break the MSSQL healthcheck quoting or the connection string delimiter parsing
- ~~Redis has no authentication (requirepass not set) — any container on the network can read/write; add auth before production~~ **RESOLVED** (prep-redis-requirepass, 2026-03-29): Added requirepass via Docker Compose command, password in .env, propagated through connection strings
- JWT default secret is a committed known value in docker-compose.yml — enforce secret validation when JWT auth is implemented (Story 2.2)
- AC5 upgrade path requires `image:` tag for registry pull — add when CI/CD pipeline pushes to a container registry (Story 1.5)

## Deferred from: code review of story-1-3 (2026-03-29)

- ~~Seed data hardcoded password "Administrator1!" now runs in all environments (not just Development) — externalization deferred to Story 2.x per spec~~ **RESOLVED** (Story 2.1, 2026-03-30): Seed user removed entirely; first-user admin designation via RegisterCommand

## Deferred from: code review of story-1-4 (2026-03-29)

- Redis connection string read independently in two DI files (Infrastructure/DependencyInjection.cs + Web/DependencyInjection.cs) — DRY violation across project boundaries, risk of divergence if one fallback changes
- No explicit timeout on DB/Redis health checks — Docker curl timeout (3s) could expire before ASP.NET health check completes if dependency is slow but alive; operational tuning concern
- PII safety test in RequestLoggerTests uses brittle negative string match (`!v.ToString()!.Contains("TestRequest {")`) — depends on serialization format, consider positive assertion

## Deferred from: code review of story-1-5 (2026-03-29)

- Redis `ConnectionMultiplexer` instance registered via `AddSingleton(instance)` in CustomWebApplicationFactory won't be disposed by DI — cosmetic in test context since process exits after tests and Testcontainer is disposed independently

## Deferred from: prep-standardize-timestamps review (2026-03-29)

- First domain entity migration will expose `BaseEntity.CreatedAt` as a new non-nullable `datetimeoffset` column — ensure migration includes appropriate default value or make column nullable during Story 2.1 migration generation

## Deferred from: prep-redis-requirepass review (2026-03-29)

- Redis password passed via `--requirepass` CLI arg is visible in `docker inspect` and process list — use Docker secrets or config file for production hardening
- Redis connection string with password embedded — special characters in future passwords could break StackExchange.Redis `ConfigurationOptions.Parse()` delimiter parsing

## Deferred from: code review of story-2-1 (2026-03-30)

- TOCTOU race condition on first-user-admin designation — two concurrent registrations on empty DB can both get Admin role; no transaction/lock/constraint. Single-instance MVP, sub-millisecond window; revisit if multi-instance deployment
- Redis failure after user creation orphans user record — user exists in SQL but has no session and cannot re-register; Story 2.2 login endpoint provides fallback
- CORS `SetIsOriginAllowed(_ => true)` + `AllowCredentials()` allows any origin to make cookie-bearing requests — Story 2.2 covers CORS hardening
- Auth state in memory-only BehaviorSubject lost on page refresh — Story 2.3 covers session persistence and token refresh
- Hardcoded JWT secret `CHANGE-THIS-IN-PRODUCTION-min-32-chars!!` in appsettings.json with no startup validation — Story 2.2 covers JWT secret validation
- Non-atomic Redis session operations in `StoreSessionAsync` (3 separate commands) — orphaned tracking key is cosmetic, session key itself works; low risk for MVP
- No rate limiting on `/api/auth/register` endpoint — anonymous endpoint allows unlimited account creation and email enumeration; cross-cutting concern for all public endpoints
- Email enumeration via duplicate-email error message — spec (AC #3) explicitly requires "An account with this email already exists"; accept as design decision
- JWT issuer/audience validation disabled — spec says single-instance app, skip validation; revisit if multi-instance deployment needed
- No refresh endpoint — refresh token stored in HttpOnly cookie and Redis but no `/api/auth/refresh` endpoint exists; Story 2.3 covers this
- BcryptPasswordHasher never returns `SuccessRehashNeeded` — no bcrypt work factor migration needed for MVP; revisit if work factor is increased

## Deferred from: code review of story-2-2 (2026-03-30)

- Client logout doesn't invalidate server session or clear HttpOnly cookie — AuthService.logout() only clears in-memory state; Redis session and refresh token cookie persist for up to 7 days. Story 2.4 covers logout endpoint.
- ResetDatabaseAsync hardcoded SQL (`DELETE FROM [AspNetUserRoles]; DELETE FROM [AspNetUsers]`) will break with FK violations when domain tables referencing Users are added (e.g., ChatMessages in Epic 3). Update when domain entities are created.
- Refresh token expiry (7 days) hardcoded independently in 4 locations (LoginCommandHandler, RegisterCommandHandler, SetRefreshTokenCookie x2) with no shared constant or config value — risk of silent drift if one is changed without updating others.

## Deferred from: code review of story-2-3 (2026-03-30)

- `logout()` does not call server-side session invalidation — client-only state clearing leaves Redis session alive; Story 2.4 covers logout endpoint
- Race condition (TOCTOU) in refresh token rotation — concurrent requests with same token can both succeed; sub-millisecond window, negligible for single-instance MVP. Fix with Redis WATCH/MULTI transaction or distributed lock when scaling to multi-instance.

## Deferred from: code review of story-2-4 (2026-03-30)

- No logging in `LogoutCommandHandler` when Redis fails — handler has no ILogger; if `InvalidateSessionAsync` throws, exception propagates as 500 with no application-level log. Consistent with existing pattern (LoginCommandHandler also lacks logging).

## Deferred from: code review of story-3-1 (2026-04-02)

- `ConversationParticipant.LastReadMessageId` can reference a `Message.Id` from a different conversation — no FK constraint or application-level guard exists; future unread-tracking implementation (Epic 5) must validate that `LastReadMessageId` belongs to the same `ConversationId`
- Content/FileId cross-field invariants not enforced at data layer — a `Text` message can have empty content, a `File` message can have null `FileId`; Story 3.2 command handlers must enforce `MessageType`-specific validation rules

## Deferred from: review of prep-fix-test-infrastructure (2026-03-31)

- `ResetDatabaseAsync` dynamic SQL: if DELETE phase throws, NOCHECK CONSTRAINT ALL remains disabled for remainder of test session — FK enforcement silently off for subsequent tests. Low practical risk (DELETE on empty Identity tables won't fail), but no transaction/error-handling wrapper exists. Consider wrapping in TRY/CATCH/ROLLBACK if test suite grows complex.

## Deferred from: code review of story-3-2 (2026-04-02)

- Inconsistent CancellationToken on pre-existing IIdentityService methods — new methods (GetDisplayNamesByIdsAsync, UserExistsAsync) accept CancellationToken but older methods (FindUserByIdAsync, CheckPasswordAsync, etc.) do not. Standardize when touching these methods next.
- Race condition in CreateConversation — check-then-create for private conversations has no DB-level uniqueness constraint. Sub-millisecond window on single-instance MVP. Proper fix: unique composite index on private conversation user pairs; add during Epic 4 (group conversations) or dedicated hardening pass.
