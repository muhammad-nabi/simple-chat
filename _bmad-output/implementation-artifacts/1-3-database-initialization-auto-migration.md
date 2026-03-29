# Story 1.3: Database Initialization & Auto-Migration

Status: ready-for-dev

## Story

As an operator,
I want the database to initialize and migrate automatically on application startup,
so that I never need to run manual migration commands during deployment or upgrades.

## Acceptance Criteria

1. **Given** the application container starts with a fresh (empty) MSSQL database
   **When** the application startup sequence runs
   **Then** Entity Framework Core `MigrateAsync()` executes automatically
   **And** all pending migrations are applied to create the initial schema
   **And** the application becomes healthy after migrations complete

2. **Given** the application has previously run migrations
   **When** the application restarts (e.g., container restart or upgrade)
   **Then** `MigrateAsync()` runs and detects no pending migrations
   **And** the startup completes without errors (idempotent — NFR26)

3. **Given** a slow MSSQL startup (container still initializing)
   **When** the application attempts to connect
   **Then** the application retries database connectivity with backoff rather than crashing
   **And** the startup health probe (`/health/startup`) reports unhealthy until migrations complete
   **And** Docker healthcheck does not trigger a restart loop during initial MSSQL startup

4. **Given** the application startup sequence
   **When** measured end-to-end from container launch to healthy status
   **Then** the application becomes healthy within 30 seconds under normal conditions (NFR27)

## Tasks / Subtasks

- [ ] Task 1: Replace `EnsureCreatedAsync` with EF Core Migrations (AC: #1, #2)
  - [ ] 1.1 Rewrite `ApplicationDbContextInitialiser.InitialiseAsync()` to call `_context.Database.MigrateAsync()` instead of `EnsureDeletedAsync()` + `EnsureCreatedAsync()`
  - [ ] 1.2 Remove the `EnsureDeletedAsync()` call entirely — this destroys data on every startup (deferred-work.md item)
  - [ ] 1.3 Keep `SeedAsync()` method — existing idempotency checks are already present (role/user existence guards); verify they work correctly and add `IdentityResult` checking with `ILogger` on `CreateAsync` calls
  - [ ] 1.4 Generate the initial EF Core migration: `dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/Web --output-dir Data/Migrations`
  - [ ] 1.5 Verify the generated migration creates all Identity tables plus any configured entities
  - [ ] 1.6 Verify `MigrateAsync()` is idempotent — running it twice produces no errors (NFR26)

- [ ] Task 2: Enable connection retry on DbContext (AC: #3)
  - [ ] 2.1 In `src/Infrastructure/DependencyInjection.cs`, add `EnableRetryOnFailure()` to the `UseSqlServer()` call with: `maxRetryCount: 6`, `maxRetryDelay: TimeSpan.FromSeconds(30)`, `errorNumbersToAdd: null`
  - [ ] 2.2 Verify the retry strategy uses EF Core's built-in `SqlServerRetryingExecutionStrategy` (exponential backoff with jitter — no extra packages needed)

- [ ] Task 3: Make database initialization run in all environments (AC: #1)
  - [ ] 3.1 In `src/Web/Program.cs`, remove the `if (app.Environment.IsDevelopment())` guard around `await app.InitialiseDatabaseAsync()` — migrations must run in Production for zero-ops upgrades (FR41, FR42)
  - [ ] 3.2 Add try-catch with `ILogger` around the migration call to log migration failures clearly before rethrowing
  - [ ] 3.3 After `await app.InitialiseDatabaseAsync()`, resolve the singleton health check and mark ready: `app.Services.GetRequiredService<StartupHealthCheck>().MarkReady()`

- [ ] Task 4: Add startup health probe (AC: #3, #4)
  - [ ] 4.1 Create `src/Web/HealthChecks/StartupHealthCheck.cs` implementing `IHealthCheck` — returns `Healthy` only after migrations complete, `Unhealthy` during startup
  - [ ] 4.2 Use a simple `bool _migrationCompleted` flag (thread-safe via `volatile` or similar) set after `MigrateAsync()` succeeds
  - [ ] 4.3 Register in `src/Web/DependencyInjection.cs` — two-step: `services.AddSingleton<StartupHealthCheck>()` THEN `services.AddHealthChecks().AddCheck<StartupHealthCheck>("startup", tags: new[] { "startup" })` — singleton-first ensures `MarkReady()` and health check use the same instance
  - [ ] 4.4 Map health endpoint in `Program.cs` EARLY in the pipeline (before `UseExceptionHandler`): `app.MapHealthChecks("/health/startup", new HealthCheckOptions { Predicate = check => check.Tags.Contains("startup") })`
  - [ ] 4.5 NOTE: `/health/live` and `/health/ready` endpoints are Story 1.4 scope — do NOT implement them here

- [ ] Task 5: Update Docker healthcheck configuration (AC: #3, #4)
  - [ ] 5.1 In `docker-compose.yml`, uncomment the app health check, point it at `/health/startup`, and use `wget` (NOT `curl` — curl is not installed in the `aspnet:10.0` runtime image): `["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health/startup"]`
  - [ ] 5.2 Set healthcheck parameters: `interval: 5s`, `timeout: 3s`, `retries: 10`, `start_period: 40s` — the `start_period` gives MSSQL time to initialize before the app even starts checking
  - [ ] 5.3 Verify Docker does NOT restart the app container during initial MSSQL startup (the `start_period` + `depends_on: service_healthy` on mssql should prevent this)

- [ ] Task 6: Verification (AC: #1, #2, #3, #4)
  - [ ] 6.1 Fresh start: `docker compose down -v && docker compose up` — app creates schema via migration, becomes healthy
  - [ ] 6.2 Restart: `docker compose restart app` — app detects no pending migrations, starts without errors
  - [ ] 6.3 Slow MSSQL: Stop mssql, start app, then start mssql — app retries and eventually connects
  - [ ] 6.4 Startup time: Measure time from container launch to `/health/startup` returning 200 — must be under 30s
  - [ ] 6.5 All existing .NET tests still pass (5/5 from Story 1.2)
  - [ ] 6.6 Verify `dotnet ef migrations list` shows the InitialCreate migration

## Dev Notes

### Critical: Replace EnsureCreated with MigrateAsync

The current `ApplicationDbContextInitialiser` at `src/Infrastructure/Data/ApplicationDbContextInitialiser.cs` is **DANGEROUS for production**:
- Line 41: `Database.EnsureDeletedAsync()` — **deletes the entire database on every startup**
- Line 42: `Database.EnsureCreatedAsync()` — creates schema without migration tracking

This was flagged in `_bmad-output/implementation-artifacts/deferred-work.md`:
> "DB initializer calls EnsureDeletedAsync then EnsureCreatedAsync — destroys data every startup, Story 1.3 covers proper DB initialization"

**Fix:** Replace both calls with a single `await _context.Database.MigrateAsync()`. This:
- Creates the database if it doesn't exist
- Applies all pending migrations
- Is idempotent — safe to run on every startup (NFR26)
- Tracks which migrations have been applied in `__EFMigrationsHistory` table

### EF Core Migration Generation

Generate the initial migration from the repo root:
```bash
dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/Web --output-dir Data/Migrations
```

This will create migration files in `src/Infrastructure/Data/Migrations/`. The migration should include all Identity tables (from `IdentityDbContext<ApplicationUser>`) plus any entity configurations applied via `OnModelCreating`.

**Important:** The project uses `Microsoft.EntityFrameworkCore.Design` v10.0.5 (already in `Directory.Packages.props` line 22) which is required for the `dotnet ef` CLI tool. Make sure `dotnet-ef` tool is installed globally or locally.

### Connection Retry Configuration

Current state in `src/Infrastructure/DependencyInjection.cs` line 26: `UseSqlServer(connectionString)` with **no retry policy**.

Add `EnableRetryOnFailure()`:
```csharp
options.UseSqlServer(connectionString, sqlOptions =>
    sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 6,
        maxRetryDelay: TimeSpan.FromSeconds(30),
        errorNumbersToAdd: null));
```

This uses EF Core's built-in `SqlServerRetryingExecutionStrategy` with exponential backoff and jitter. No additional NuGet packages needed.

Note: The existing `options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))` on line 27 suppresses warnings about model changes not captured in migrations. Keep it — the initial migration will capture the full model. Be aware it exists so future model changes aren't silently missed.

<!-- Note: EnableRetryOnFailure is incompatible with explicit BeginTransaction — use CreateExecutionStrategy if needed in future stories -->

### Startup Health Check Pattern

Create a simple health check that tracks migration completion:

```csharp
// src/Web/HealthChecks/StartupHealthCheck.cs
public class StartupHealthCheck : IHealthCheck
{
    private volatile bool _isReady;

    public void MarkReady() => _isReady = true;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        return Task.FromResult(_isReady
            ? HealthCheckResult.Healthy("Migrations complete")
            : HealthCheckResult.Unhealthy("Migrations pending"));
    }
}
```

**Registration (two-step, order matters):**
1. `services.AddSingleton<StartupHealthCheck>()` — registers the singleton instance
2. `services.AddHealthChecks().AddCheck<StartupHealthCheck>("startup", tags: new[] { "startup" })` — `AddCheck<T>` resolves from DI, so it picks up the same singleton

**Wiring `MarkReady()` in Program.cs:**
After `await app.InitialiseDatabaseAsync()`, resolve from the root container (NOT the scoped provider inside the initialiser):
```csharp
app.Services.GetRequiredService<StartupHealthCheck>().MarkReady();
```
The initialiser runs inside `CreateScope()` — placing `MarkReady()` in Program.cs keeps the health check wiring in the composition root.

**Endpoint placement:** Map `/health/startup` EARLY in the middleware pipeline (before `UseExceptionHandler`) so it responds even during application errors.

**Scope:** Only `/health/startup` is implemented in this story. `/health/live` and `/health/ready` are Story 1.4. The `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` package is already installed (`Directory.Packages.props` line 24) for use in Story 1.4.

### Database Initialization Must Run in All Environments

Current `Program.cs` line 15-18 wraps `InitialiseDatabaseAsync()` in `if (app.Environment.IsDevelopment())`. This means **Production gets no database initialization** — the app would crash on a fresh deployment.

Remove the development-only guard. The architecture mandates zero-ops upgrades: `docker-compose pull && docker-compose up -d` must "just work" (FR41, FR42).

### Seed Data Idempotency

The current `TrySeedAsync()` already has idempotency guards — it checks `_roleManager.Roles.All(r => r.Name != administratorRole.Name)` and `_userManager.Users.All(u => u.UserName != administrator.UserName)` before creating. These checks are sufficient.

**What to fix:** The `CreateAsync` results are not checked. Add `IdentityResult` checking with `ILogger`:
```csharp
var result = await _userManager.CreateAsync(administrator, "Administrator1!");
if (!result.Succeeded)
{
    _logger.LogWarning("Failed to create seed user: {Errors}",
        string.Join(", ", result.Errors.Select(e => e.Description)));
}
```

Hardcoded password externalization is deferred to Story 2.x — nothing in production consumes this seed user yet.

### Scope Exclusions

- `/health/live`, `/health/ready` -> Story 1.4
- Serilog structured logging -> Story 1.4
- docker-compose.yml service definitions unchanged (only healthcheck uncommented)
- No new NuGet packages (all exist in Directory.Packages.props)
- Seed password externalization -> Story 2.x
- Multi-instance migration locking -> not needed (single-instance MVP)

### Previous Story Intelligence (Story 1.2)

- App `depends_on` mssql with `condition: service_healthy` — app won't start until MSSQL is healthy
- App healthcheck is **commented out** in `docker-compose.yml` lines 26-32 — uncomment in this story, targeting `/health/startup`
- The commented healthcheck uses `curl` which is NOT in the runtime image — must switch to `wget`
- MSSQL `2022-latest` runs via Rosetta on ARM Mac (expected platform warning, not blocking)
- MSSQL needs OS memory headroom above the 512MB internal cap — don't set container memory limits

### Git Intelligence

Recent commits (latest first):
- `1e8400d` story 1.2 implemented with review
- `5258f96` story 1.2 created and verified
- `485b8ac` story 1.1 implemented with review

Story 1.1 scaffolded 132 files using Jason Taylor Clean Architecture template. Story 1.2 added Docker infrastructure. No migration files exist yet.

### Project Structure Notes

Files modified by this story:
```
src/Infrastructure/Data/ApplicationDbContextInitialiser.cs  # Replace EnsureCreated with MigrateAsync
src/Infrastructure/DependencyInjection.cs                   # Add EnableRetryOnFailure
src/Web/Program.cs                                          # Remove dev-only guard on DB init
src/Web/DependencyInjection.cs                              # Register health checks
docker-compose.yml                                          # Uncomment app healthcheck
```

Files created by this story:
```
src/Web/HealthChecks/StartupHealthCheck.cs                  # Startup health check
src/Infrastructure/Data/Migrations/*_InitialCreate.cs       # EF Core migration (auto-generated)
src/Infrastructure/Data/Migrations/*_InitialCreate.Designer.cs
src/Infrastructure/Data/Migrations/ApplicationDbContextModelSnapshot.cs
```

### References

- [Source: architecture.md — §Migration strategy: EF MigrateAsync() on startup, single-instance constraint]
- [Source: architecture.md — §Three-tier health checks: /health/startup, /health/live, /health/ready]
- [Source: architecture.md — §Infrastructure layer: AppDbContext, no repository pattern, DbContext in handlers]
- [Source: architecture.md — §EnableRetryOnFailure for connection resilience]
- [Source: epics/epic-1 — Story 1.3 acceptance criteria, lines 71-99]
- [Source: prd.md — FR41 (auto migrations), FR42 (zero-step upgrades), NFR26 (idempotent migrations), NFR27 (30s startup)]
- [Source: implementation-artifacts/1-2-docker-compose-container-configuration.md — Docker config, healthcheck patterns, MSSQL init]
- [Source: implementation-artifacts/deferred-work.md — EnsureDeleted destroys data, hardcoded admin password]
- [Source: Microsoft Learn — EF Core Connection Resiliency](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency)
- [Source: Microsoft Learn — ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0)

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### Change Log

### Review Findings

### File List
