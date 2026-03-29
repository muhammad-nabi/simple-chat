# Story 1.4: Structured Logging & Health Endpoints

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want structured JSON logs and health check endpoints,
so that I can monitor the application using standard infrastructure tooling.

## Acceptance Criteria

1. **Given** the application is running
   **When** any log event occurs (startup, request, error)
   **Then** the log is written to stdout in JSON format using Serilog (NFR30)
   **And** log entries include structured fields (timestamp, level, message template, properties)
   **And** log levels follow the defined standards: Debug (detailed flow), Information (business events), Warning (recoverable issues), Error (failures), Fatal (cannot continue)
   **And** logs use Serilog message templates with named parameters — never string interpolation

2. **Given** the application is running and healthy
   **When** an HTTP GET request is sent to `/health/startup`
   **Then** a 200 OK response is returned indicating initialization is complete

3. **Given** the application is running
   **When** an HTTP GET request is sent to `/health/live`
   **Then** a 200 OK response is returned indicating the process is alive

4. **Given** the application is running with all dependencies available
   **When** an HTTP GET request is sent to `/health/ready`
   **Then** a 200 OK response is returned
   **And** the response includes MSSQL connectivity status
   **And** the response includes Redis connectivity status

5. **Given** the MSSQL database is unavailable
   **When** an HTTP GET request is sent to `/health/ready`
   **Then** a 503 Service Unavailable response is returned indicating database connectivity failure

6. **Given** the Redis instance is unavailable
   **When** an HTTP GET request is sent to `/health/ready`
   **Then** a 503 Service Unavailable response is returned indicating cache connectivity failure

## Tasks / Subtasks

- [x] Task 1: Add Serilog NuGet packages and configure structured logging (AC: #1)
  - [x] 1.1 Add Serilog packages to `Directory.Packages.props`: `Serilog.AspNetCore` (includes Console sink, enrichers). Check NuGet for latest stable version compatible with .NET 10
  - [x] 1.2 In `src/Web/Program.cs`, replace default logging with Serilog bootstrap using `UseSerilog()` on the host builder
  - [x] 1.3 Configure Serilog to write JSON to stdout via `WriteTo.Console(new RenderedCompactJsonFormatter())` or `CompactJsonFormatter()`
  - [x] 1.4 Add enrichers: `Enrich.FromLogContext()`, `Enrich.WithMachineName()`, `Enrich.WithEnvironmentName()`
  - [x] 1.5 Set minimum log levels: Default=Information, Microsoft=Warning, Microsoft.Hosting.Lifetime=Information, Microsoft.EntityFrameworkCore=Warning
  - [x] 1.6 Add Serilog request logging middleware: `app.UseSerilogRequestLogging()` — place after health endpoints but before other middleware to avoid noisy health check logs
  - [x] 1.7 Update `appsettings.json` — remove the default `Logging` section (Serilog replaces it); optionally add `Serilog` configuration section for environment-specific overrides. **Create** `appsettings.Development.json` (does not exist yet) with Serilog MinimumLevel overridden to Debug for development

- [x] Task 2: Add `/health/live` liveness endpoint (AC: #2, #3)
  - Note: AC #2 (`/health/startup` returns 200) is already satisfied by Story 1.3 — no implementation needed, just verified in Task 6
  - [x] 2.1 Create `src/Web/HealthChecks/LivenessHealthCheck.cs` implementing `IHealthCheck` — always returns `HealthCheckResult.Healthy("Process is alive")`. This is a simple process-alive check (no dependency verification)
  - [x] 2.2 Register in `src/Web/DependencyInjection.cs`: `services.AddHealthChecks().AddCheck<LivenessHealthCheck>("liveness", tags: new[] { "live" })`
  - [x] 2.3 Map endpoint in `src/Web/Program.cs`: `app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") })` — place near existing `/health/startup` mapping

- [x] Task 3: Add `/health/ready` readiness endpoint with dependency checks (AC: #4, #5, #6)
  - [x] 3.1 Register EF Core database health check in `src/Web/DependencyInjection.cs`: `.AddDbContextCheck<ApplicationDbContext>("database", tags: new[] { "ready" })` — uses the already-installed `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` package (Directory.Packages.props line 24)
  - [x] 3.2 **Redis is NOT yet registered in the application.** Before adding the health check: (a) Add `StackExchange.Redis` package to `Directory.Packages.props`, (b) In `src/Infrastructure/DependencyInjection.cs`, register `IConnectionMultiplexer` as a singleton using `ConnectionMultiplexer.Connect()` with the connection string from configuration (key: `Redis__ConnectionString` or `ConnectionStrings:Redis`), (c) Add `AspNetCore.HealthChecks.Redis` package to `Directory.Packages.props`, (d) Register Redis health check: `.AddRedis(connectionString, name: "redis", tags: new[] { "ready" })`. The `Redis__ConnectionString=redis:6379` env var exists in `docker-compose.yml` but no C# code consumes it yet
  - [x] 3.3 Map endpoint in `src/Web/Program.cs`: `app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })` — place near other health endpoints
  - [x] 3.4 Configure response writer to output JSON with individual check statuses for `/health/ready` — use `ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse` from `AspNetCore.HealthChecks.UI.Client` package OR write a minimal custom JSON writer to avoid extra package

- [x] Task 4: Update Docker healthcheck to use `/health/ready` for ongoing checks (AC: #4, #5, #6)
  - [x] 4.1 In `docker-compose.yml`, update the app healthcheck from `/health/startup` to `/health/ready` for ongoing container health monitoring. Architecture specifies: "Target `/health/startup` initially, `/health/ready` ongoing" — since Docker healthcheck doesn't support probe switching, use `/health/ready` (it implicitly passes when app is ready; startup probe is handled by `start_period`)
  - [x] 4.2 Verify `start_period: 40s` is sufficient to cover migration time before readiness checks begin

- [x] Task 5: Fix deferred logging issues (AC: #1)
  - [x] 5.1 Fix PII leak in `src/Application/Common/Behaviours/LoggingBehaviour.cs` — the `{@Request}` destructuring logs entire request objects including passwords/tokens. Replace with safe logging: log only the request type name, not the full object. [Deferred from Story 1.1 review]
  - [x] 5.2 Fix PII leak in `src/Application/Common/Behaviours/PerformanceBehaviour.cs` — same `{@Request}` destructuring issue as LoggingBehaviour. Remove or replace with safe logging of request type name only. [Deferred from Story 1.1 review]
  - [x] 5.3 Fix `src/Application/Common/Behaviours/PerformanceBehaviour.cs` — Stopwatch is never reset between calls; `_timer.ElapsedMilliseconds` accumulates. Use `_timer.Restart()` instead of `_timer.Start()` — `Restart()` resets and starts in one call. [Deferred from Story 1.1 review]

- [x] Task 6: Verification (AC: #1-#6)
  - [x] 6.1 `docker compose up` — verify JSON structured logs appear on stdout from app container
  - [x] 6.2 Verify log entries contain: timestamp, level, message template, properties as structured fields
  - [x] 6.3 `curl http://localhost:8080/health/startup` — returns 200 with healthy status
  - [x] 6.4 `curl http://localhost:8080/health/live` — returns 200 with healthy status
  - [x] 6.5 `curl http://localhost:8080/health/ready` — returns 200 with MSSQL and Redis status included
  - [x] 6.6 Stop MSSQL container → `curl /health/ready` returns 503
  - [x] 6.7 Stop Redis container → `curl /health/ready` returns 503
  - [x] 6.8 All existing .NET tests still pass
  - [x] 6.9 Verify no PII (passwords, tokens) appears in log output during login/registration flows (when implemented, verify with seed user creation logs for now)

### Review Findings

- [x] [Review][Defer] Redis connection string read in two DI files (Infrastructure + Web) — DRY violation across project boundaries, risk of divergence if one fallback changes. Centralize in future refactor. — deferred, cross-project code smell
- [x] [Review][Defer] No explicit health check timeout on DB/Redis checks — Docker curl timeout (3s) could expire before ASP.NET health check completes if dependency is slow but alive. Operational tuning for future. — deferred, operational concern
- [x] [Review][Defer] PII safety test uses brittle negative string match `!v.ToString()!.Contains("TestRequest {")` — depends on serialization format. Consider positive assertion on expected log content. — deferred, minor test robustness

## Dev Notes

### Serilog Integration Pattern

The architecture mandates Serilog with JSON to stdout (NFR30). The current codebase uses default ASP.NET Core logging only — no Serilog packages exist yet.

**Program.cs integration pattern** (two-stage Serilog bootstrap):
```csharp
// Stage 1: Bootstrap logger for startup errors
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Stage 2: Full Serilog configuration
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter()));
```

This replaces the default `Logging` section in appsettings.json. Serilog takes over all logging — `ILogger<T>` injections automatically use Serilog.

**Serilog request logging middleware:** Place `app.UseSerilogRequestLogging()` AFTER health endpoint mappings to avoid logging every 5-second Docker healthcheck poll. The architecture file specifies `RequestLoggingMiddleware.cs` in the Middleware folder — Serilog's built-in request logging middleware replaces the need for a custom one.

### Three-Tier Health Check Architecture

The architecture defines three health endpoints with distinct purposes:

| Endpoint | Purpose | Checks | Tags |
|----------|---------|--------|------|
| `/health/startup` | Init complete (migrations done) | `StartupHealthCheck` (exists from Story 1.3) | `startup` |
| `/health/live` | Process alive | Always healthy (simple ping) | `live` |
| `/health/ready` | Dependencies available | MSSQL + Redis connectivity | `ready` |

**Tag-based filtering:** Each `MapHealthChecks` call uses `Predicate = check => check.Tags.Contains("tag")` to run only relevant checks per endpoint.

**Existing infrastructure:**
- `StartupHealthCheck.cs` exists at `src/Web/HealthChecks/` — singleton with `MarkReady()` pattern (Story 1.3)
- `AddHealthChecks()` already called in `src/Web/DependencyInjection.cs` line 38
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` already in `Directory.Packages.props` line 24

### Redis Registration (New — Not Yet in Codebase)

**Redis is NOT yet registered in the application.** The `docker-compose.yml` defines `Redis__ConnectionString=redis:6379` as an environment variable, and a Redis container runs alongside the app, but no C# code consumes Redis yet. The `StackExchange.Redis` package is NOT in `Directory.Packages.props`.

This story must establish the Redis connection foundation:
1. Add `StackExchange.Redis` package to `Directory.Packages.props`
2. In `src/Infrastructure/DependencyInjection.cs`, register `IConnectionMultiplexer` as a singleton:
```csharp
var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString")
    ?? configuration.GetValue<string>("Redis__ConnectionString")
    ?? "localhost:6379";
services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));
```
3. Add `AspNetCore.HealthChecks.Redis` package to `Directory.Packages.props` for the health check

**Configuration:** Docker Compose passes `Redis__ConnectionString=redis:6379` which ASP.NET Core configuration binds as `Redis:ConnectionString`. Use `configuration["Redis:ConnectionString"]` to read it.

Future stories (Presence, Caching) will use this `IConnectionMultiplexer` registration.

### Readiness Response Format

For `/health/ready`, include individual check statuses in the response. Options:
1. **Custom JSON writer** (minimal, no extra package):
```csharp
options.ResponseWriter = async (context, report) =>
{
    context.Response.ContentType = "application/json";
    var result = JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description
        })
    });
    await context.Response.WriteAsync(result);
};
```
2. **`AspNetCore.HealthChecks.UI.Client`** package — provides `UIResponseWriter.WriteHealthCheckUIResponse`. Heavier dependency for a simple need.

Prefer option 1 (custom writer) to avoid unnecessary package dependencies.

### Deferred Work Items to Address

Two items from the Story 1.1 code review are directly related to logging and should be fixed in this story:

1. **PII in LoggingBehaviour** (`src/Application/Common/Behaviours/LoggingBehaviour.cs`):
   - Current line 32-33 logs `{@Request}` which destructures the entire request object
   - Fix: Remove `{@Request}` from the log template, or replace with `{RequestType}` logging only `typeof(TRequest).Name`
   - This is a security concern — passwords and tokens would appear in logs

2. **Stopwatch accumulation in PerformanceBehaviour** (`src/Application/Common/Behaviours/PerformanceBehaviour.cs`):
   - Stopwatch initialized once in constructor, never reset
   - Fix: Call `_timer.Restart()` instead of `_timer.Start()` — `Restart()` resets and starts in one call

### Log Level Standards (Architecture Compliance)

| Level | Usage | Example |
|-------|-------|---------|
| `Debug` | Detailed flow for development | `"Loading messages for conversation {ConversationId}"` |
| `Information` | Business events | `"User {UserId} sent message in conversation {ConversationId}"` |
| `Warning` | Recoverable issues | `"File upload rejected: {FileName} exceeds {MaxSize}MB"` |
| `Error` | Failures requiring attention | `"Failed to deliver message via SignalR: {Error}"` |
| `Fatal` | Application cannot continue | `"Database migration failed: {Error}"` |

**Anti-pattern (from architecture.md):**
```csharp
// WRONG: String interpolation in logs — loses structured data
_logger.LogInformation($"User {userId} logged in");

// CORRECT: Message template with named parameters
_logger.LogInformation("User {UserId} logged in", userId);
```

### Scope Exclusions

- No custom `RequestLoggingMiddleware.cs` — Serilog's `UseSerilogRequestLogging()` replaces this need
- No custom `ExceptionHandlingMiddleware.cs` changes — existing exception handler is sufficient; Serilog captures unhandled exceptions via its integration
- No log aggregator integration (ELK, Seq, etc.) — JSON to stdout is the architecture choice; operators pipe stdout to their preferred tool
- No metrics/tracing (OpenTelemetry) — out of scope for MVP
- Frontend logging — not in scope
- Admin monitoring UI — not in scope (UX spec mentions admin components but those are Epic 8)

### Previous Story Intelligence (Story 1.3)

Key learnings from Story 1.3 that impact this story:

- **Health endpoint placement:** `/health/startup` is mapped EARLY in the pipeline (before `UseExceptionHandler`) in `Program.cs` lines 28-31. New health endpoints (`/health/live`, `/health/ready`) should follow the same pattern
- **StartupHealthCheck is a singleton** registered in two steps: `AddSingleton<StartupHealthCheck>()` then `AddCheck<StartupHealthCheck>()` — the DI resolves the same instance. Follow this pattern for any new singleton health checks
- **`curl` is installed** in the Docker runtime image (added in Story 1.3 for healthcheck) — available for all health endpoint testing
- **Docker healthcheck timing:** `start_period: 40s` covers MSSQL startup + migration time. Switching to `/health/ready` must still respect this start period
- **EF Core retry policy** is configured (`EnableRetryOnFailure`) — the DB health check should work correctly even with transient connection issues
- **`try-catch` around DB initialization** in Program.cs — app stays running but unhealthy if DB fails. The `/health/ready` endpoint will independently verify DB connectivity

### Git Intelligence

Recent commits (latest first):
- `8482609` story 1.3 implemented with review
- `cd98441` story 1.3 created and validated
- `1e8400d` story 1.2 implemented with review

Story 1.3 modified 14 files including Program.cs, DependencyInjection.cs, docker-compose.yml, and created the StartupHealthCheck. Story 1.4 will modify several of the same files — work from the current HEAD state.

### Project Structure Notes

Files to modify:
```
Directory.Packages.props                              # Add Serilog.AspNetCore, StackExchange.Redis, AspNetCore.HealthChecks.Redis
src/Web/Program.cs                                    # Serilog bootstrap, request logging, new health endpoint mappings
src/Web/DependencyInjection.cs                        # Register liveness + readiness health checks
src/Infrastructure/DependencyInjection.cs             # Register IConnectionMultiplexer (Redis not yet registered)
src/Application/Common/Behaviours/LoggingBehaviour.cs # Fix PII leak
src/Application/Common/Behaviours/PerformanceBehaviour.cs # Fix PII leak + Stopwatch accumulation
docker-compose.yml                                    # Update healthcheck to /health/ready
src/Web/appsettings.json                              # Replace Logging section with Serilog config
```

Files to create:
```
src/Web/HealthChecks/LivenessHealthCheck.cs           # Simple always-healthy check
src/Web/appsettings.Development.json                  # Development Serilog overrides (Debug level) — does not exist yet
```

Existing files NOT to modify:
```
src/Web/HealthChecks/StartupHealthCheck.cs            # Already correct from Story 1.3
```

### References

- [Source: architecture.md — §Logging Standards table: log levels Debug/Information/Warning/Error/Fatal]
- [Source: architecture.md — §Decision table: Serilog with JSON to stdout, satisfies NFR30]
- [Source: architecture.md — §Health checks: Three-tier /health/startup, /health/live, /health/ready]
- [Source: architecture.md — §Docker healthcheck: Target /health/startup initially, /health/ready ongoing]
- [Source: architecture.md — §Code structure: HealthChecks/ folder with StartupHealthCheck, ReadinessHealthCheck, LivenessHealthCheck]
- [Source: architecture.md — §Middleware: RequestLoggingMiddleware.cs in Web project]
- [Source: architecture.md — §Anti-pattern: string interpolation in logs loses structured data]
- [Source: prd.md — FR43 (health check endpoints), FR44 (structured logs)]
- [Source: prd.md — NFR30 (JSON stdout logs), NFR31 (health + DB connectivity status)]
- [Source: epics/epic-1 — Story 1.4 acceptance criteria]
- [Source: implementation-artifacts/1-3-database-initialization-auto-migration.md — StartupHealthCheck pattern, Docker healthcheck, curl availability]
- [Source: implementation-artifacts/deferred-work.md — PII in LoggingBehaviour, Stopwatch accumulation in PerformanceBehaviour]
- [Source: Directory.Packages.props line 24 — Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore already installed]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- Build error: `WithMachineName()` / `WithEnvironmentName()` require separate `Serilog.Enrichers.Environment` package. Resolved by using `Enrich.WithProperty()` instead.
- Build error: Redis `ConnectionMultiplexer.Connect()` fails during OpenAPI doc generation when Redis is unavailable. Resolved by setting `AbortOnConnectFail = false` via `ConfigurationOptions`.
- Test error: `RequestHandlerDelegate<TResponse>` in MediatR 14 takes a `CancellationToken` parameter. Fixed lambda signature in tests.

### Completion Notes List

- Integrated Serilog.AspNetCore 10.0.0 with two-stage bootstrap logging and RenderedCompactJsonFormatter for JSON stdout output
- Configured Serilog minimum levels: Information default, Warning for Microsoft/EF Core namespaces
- Created appsettings.Development.json with Debug minimum level for development
- Added UseSerilogRequestLogging() after health endpoints to avoid noisy healthcheck logs
- Created LivenessHealthCheck (always healthy) mapped to /health/live
- Registered EF Core database health check and Redis health check for /health/ready endpoint
- Established Redis IConnectionMultiplexer singleton in Infrastructure DI (foundation for future Presence/Caching stories)
- Custom JSON response writer for /health/ready shows individual check statuses
- Updated Docker healthcheck from /health/startup to /health/ready
- Fixed PII leak in LoggingBehaviour — removed {@Request} destructuring that would log passwords/tokens
- Fixed PII leak in PerformanceBehaviour — same {@Request} destructuring removal
- Fixed Stopwatch accumulation in PerformanceBehaviour — changed Start() to Restart()
- Added unit tests for PerformanceBehaviour (no-warning on fast requests, no time accumulation)
- Added PII safety test for LoggingBehaviour
- All 8 unit tests pass, full solution builds clean

### Change Log

- 2026-03-29: Story 1.4 implemented — Serilog structured logging, three-tier health endpoints, Redis connection foundation, PII fixes

### File List

Modified:
- Directory.Packages.props (added Serilog.AspNetCore, StackExchange.Redis, AspNetCore.HealthChecks.Redis)
- src/Web/Web.csproj (added Serilog.AspNetCore, AspNetCore.HealthChecks.Redis package refs)
- src/Infrastructure/Infrastructure.csproj (added StackExchange.Redis package ref)
- src/Web/Program.cs (Serilog bootstrap, UseSerilogRequestLogging, /health/live, /health/ready mappings)
- src/Web/DependencyInjection.cs (liveness + readiness health check registration)
- src/Infrastructure/DependencyInjection.cs (Redis IConnectionMultiplexer singleton)
- src/Web/appsettings.json (replaced Logging section with Serilog config)
- docker-compose.yml (healthcheck /health/startup -> /health/ready)
- src/Application/Common/Behaviours/LoggingBehaviour.cs (removed PII-leaking {@Request})
- src/Application/Common/Behaviours/PerformanceBehaviour.cs (removed PII-leaking {@Request}, fixed Stopwatch with Restart())
- tests/Application.UnitTests/Common/Behaviours/RequestLoggerTests.cs (added PII safety test)

Created:
- src/Web/HealthChecks/LivenessHealthCheck.cs
- src/Web/appsettings.Development.json
- tests/Application.UnitTests/Common/Behaviours/PerformanceBehaviourTests.cs
