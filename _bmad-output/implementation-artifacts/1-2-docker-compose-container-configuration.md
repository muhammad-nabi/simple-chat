# Story 1.2: Docker Compose & Container Configuration

Status: ready-for-dev

## Story

As an operator,
I want to deploy simple-chat with a single `docker-compose up` command,
so that I can run the complete application stack without manual service configuration.

## Acceptance Criteria

1. **Given** a server with Docker and Docker Compose installed
   **When** the operator runs `docker-compose up`
   **Then** three containers start: the application container, MSSQL (SQL Server), and Redis
   **And** the application container is built from a multi-stage Dockerfile (build -> publish -> runtime)
   **And** the MSSQL container has its internal memory capped at 512MB via `sp_configure 'max server memory'` (executed by an init script on first startup)
   **And** the Redis container starts with default configuration

2. **Given** the Docker Compose configuration
   **When** the operator inspects the defined volumes
   **Then** a named volume exists for MSSQL data persistence
   **And** a named volume exists for file uploads
   **And** Redis is configured as ephemeral (no persistent volume required)

3. **Given** the Docker Compose configuration
   **When** the operator reviews environment variables
   **Then** all application configuration is manageable via environment variables in the compose file
   **And** sensible defaults are provided for: database connection string, Redis connection, JWT secret, max file upload size
   **And** no in-container file editing is required for basic deployment (NFR29)

4. **Given** the built Docker image
   **When** the operator checks the image size
   **Then** the image is under 500MB (NFR33)

5. **Given** the operator wants to upgrade
   **When** they run `docker-compose pull && docker-compose up -d`
   **Then** the new image is pulled and containers restart with zero manual migration steps (FR42)

## Tasks / Subtasks

- [ ] Task 1: Create multi-stage Dockerfile (AC: #1, #4)
  - [ ] 1.1 Create `Dockerfile` at repo root with multi-stage build (restore, build, frontend, publish, runtime)
  - [ ] 1.2 Stage 1 (restore): Use `mcr.microsoft.com/dotnet/sdk:10.0` — copy `.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `nuget.config`, all `.csproj` files, then `dotnet restore`
  - [ ] 1.3 Stage 2 (build): Copy all source, run `dotnet build -c Release --no-restore`
  - [ ] 1.4 Stage 3 (frontend): Use `node:22-alpine` — copy `src/Web/ClientApp/`, run `npm ci && npm run build -- --configuration production`
  - [ ] 1.5 Stage 4 (publish): Run `dotnet publish src/Web/Web.csproj -c Release --no-build /p:SkipSpaPublish=true -o /app/publish` (see PublishRunWebpack note below)
  - [ ] 1.6 Stage 4b: Copy Angular dist output (`dist/browser/`) from frontend stage into `/app/publish/wwwroot/`
  - [ ] 1.7 Stage 5 (runtime): Use `mcr.microsoft.com/dotnet/aspnet:10.0` — copy published output, set `ASPNETCORE_URLS=http://+:8080`, expose port 8080, entrypoint `dotnet SimpleChat.Web.dll`
  - [ ] 1.8 Add `.dockerignore` at repo root (exclude `node_modules`, `bin`, `obj`, `.git`, `_bmad*`, `tests`)
  - [ ] 1.9 Modify `src/Web/Web.csproj`: add `Condition="'$(SkipSpaPublish)' != 'true'"` to the `PublishRunWebpack` target (prevents target from firing in Docker where Node.js is unavailable)
  - [ ] 1.10 Verify `docker build` produces image under 500MB

- [ ] Task 2: Create docker-compose.yml (AC: #1, #2, #3)
  - [ ] 2.1 Create `docker-compose.yml` at repo root with 3 services: `app`, `mssql`, `redis`
  - [ ] 2.2 `app` service: build from `.` (Dockerfile), port mapping `8080:8080`, `depends_on: { mssql: { condition: service_healthy }, redis: { condition: service_healthy } }`, environment variables for connection strings and config, volume mount `upload-data:/app/uploads`, `restart: unless-stopped`
  - [ ] 2.3 `mssql` service: image `mcr.microsoft.com/mssql/server:2022-latest`, `ACCEPT_EULA=Y`, `MSSQL_SA_PASSWORD` from `.env`, `deploy.resources.limits.memory: 768m`, named volume `mssql-data:/var/opt/mssql`, healthcheck using `/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$$MSSQL_SA_PASSWORD" -No -Q "SELECT 1"`, `restart: unless-stopped`
  - [ ] 2.4 `redis` service: image `redis:7-alpine`, no persistent volume (ephemeral), healthcheck using `redis-cli ping`, `restart: unless-stopped`
  - [ ] 2.5 Define named volumes: `mssql-data`, `upload-data`
  - [ ] 2.6 Define network: `simplechat-net` (bridge)
  - [ ] 2.7 Set all app environment variable defaults via `${VAR:-default}` syntax referencing `.env`

- [ ] Task 3: Create MSSQL memory cap init script (AC: #1)
  - [ ] 3.1 Create `docker/mssql-init.sh` — waits for SQL Server to start, then executes `sp_configure 'show advanced options', 1; RECONFIGURE; sp_configure 'max server memory (MB)', 512; RECONFIGURE;`
  - [ ] 3.2 Mount script in compose via `command` override or entrypoint wrapper that runs the init script in background after SQL Server starts

- [ ] Task 4: Create docker-compose.override.yml for development (AC: #3)
  - [ ] 4.1 Create `docker-compose.override.yml` with development port mappings (MSSQL 1433, Redis 6379 exposed to host)
  - [ ] 4.2 Add development-friendly environment overrides (verbose logging, relaxed settings)

- [ ] Task 5: Create `.env.example` and `.env` (AC: #3)
  - [ ] 5.1 Create `.env.example` at repo root documenting all compose variables with descriptions and safe defaults
  - [ ] 5.2 Create `.env` with working development defaults (copy of `.env.example`)
  - [ ] 5.3 Add `.env` to `.gitignore` (keep `.env.example` tracked)

- [ ] Task 6: Verification (AC: #1, #2, #3, #4, #5)
  - [ ] 6.1 Run `docker-compose build` — image builds successfully
  - [ ] 6.2 Run `docker-compose up` — all 3 containers start, mssql and redis healthchecks pass, app starts
  - [ ] 6.3 Verify MSSQL memory cap: `docker exec` into mssql container and run `sqlcmd -Q "SELECT value_in_use FROM sys.configurations WHERE name = 'max server memory (MB)'"` — should return 512
  - [ ] 6.4 Verify named volumes `mssql-data` and `upload-data` are created
  - [ ] 6.5 Verify Redis has no persistent volume
  - [ ] 6.6 Verify Docker image size < 500MB via `docker images`
  - [ ] 6.7 Verify `restart: unless-stopped` is set on all services
  - [ ] 6.8 Verify app container responds on port 8080 (may return errors until Story 1.3/1.4 — that's expected)
  - [ ] 6.9 Verify `dotnet publish` still works outside Docker (the `SkipSpaPublish` condition only fires when explicitly set)

## Dev Notes

### Multi-Stage Dockerfile Pattern

The Dockerfile MUST use multi-stage builds to keep image size under 500MB. The architecture specifies: restore -> build -> publish backend + frontend -> runtime image.

**Critical: `PublishRunWebpack` target must be disabled in Docker.** The `Web.csproj` has a `PublishRunWebpack` MSBuild target (`AfterTargets="ComputeFilesToPublish"`) that runs `npm install` and `npm run build` during `dotnet publish`. This target fires even with `--no-build` because it hooks into the publish pipeline, not the build pipeline. Since Node.js is not available in the .NET SDK stage, this WILL fail.

**Fix:** Add `Condition="'$(SkipSpaPublish)' != 'true'"` to the `<Target Name="PublishRunWebpack">` element in `src/Web/Web.csproj`. Then pass `/p:SkipSpaPublish=true` in the Dockerfile's `dotnet publish` command. The Angular build is handled by a separate Node stage instead. This preserves `dotnet publish` behavior outside Docker (the condition defaults to false).

**Runtime base image:** Use `mcr.microsoft.com/dotnet/aspnet:10.0` (NOT the SDK image). This is ~220MB and contains only the ASP.NET Core runtime.

**Assembly name:** The output DLL is `SimpleChat.Web.dll` (see `Web.csproj`: `<AssemblyName>SimpleChat.Web</AssemblyName>`).

**SPA static files:** The Angular build output goes to `dist/browser/` (Angular 21 default). Copy this into `wwwroot/` in the publish output. The app already has `app.UseFileServer()` and `app.MapFallbackToFile("index.html")` in `Program.cs` to serve the SPA.

### Docker Compose Configuration

**3 containers only** — no additional infrastructure (architecture constraint):
- `app` — the .NET application serving both API and Angular SPA
- `mssql` — SQL Server 2022 with memory capped at 512MB
- `redis` — Redis 7 Alpine, ephemeral (no volume)

**MSSQL memory cap is critical.** SQL Server defaults to consuming 80% of host memory. The architecture mandates capping at ~512MB to stay within the 3GB total stack budget.

**WARNING: `MSSQL_MEMORY_LIMIT_MB` is NOT a real SQL Server Docker env var.** SQL Server will ignore it. The correct approach is two-layered:
1. **Container-level:** Set `deploy.resources.limits.memory: 768m` in compose (gives OS headroom above the 512MB SQL internal cap)
2. **SQL Server-level:** Run `sp_configure 'max server memory (MB)', 512; RECONFIGURE;` via an init script after SQL Server starts

Create `docker/mssql-init.sh` that:
- Waits for SQL Server to accept connections (loop with `sqlcmd -Q "SELECT 1"`)
- Runs `sp_configure 'show advanced options', 1; RECONFIGURE;`
- Runs `sp_configure 'max server memory (MB)', 512; RECONFIGURE;`
- The compose file runs this script via a `command` that starts SQL Server in background, runs init, then waits on the SQL Server process

**Healthcheck for MSSQL service:**
```yaml
healthcheck:
  test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$$MSSQL_SA_PASSWORD" -No -Q "SELECT 1" || exit 1
  interval: 10s
  timeout: 5s
  retries: 5
  start_period: 30s
```

**Healthcheck for Redis service:**
```yaml
healthcheck:
  test: ["CMD", "redis-cli", "ping"]
  interval: 10s
  timeout: 3s
  retries: 3
```

**App `depends_on` with health conditions:**
```yaml
depends_on:
  mssql:
    condition: service_healthy
  redis:
    condition: service_healthy
```

**App container healthcheck (placeholder):** Add a commented-out healthcheck in the app service definition targeting `/health/ready`. Health endpoints don't exist yet (Story 1.4). Include a comment: `# Uncomment after Story 1.4 implements health endpoints`.

**Docker network:** Define an explicit bridge network `simplechat-net` and attach all services to it for clear isolation and documentation.

**Connection string format for Docker:** The app connects to MSSQL via the Docker service name `mssql` (not `localhost`). Default connection string:
```
Server=mssql,1433;Database=SimpleChatDb;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True;MultipleActiveResultSets=true
```

**Redis connection for Docker:** The app connects to Redis via the Docker service name `redis`. Default: `redis:6379`.

**File upload volume:** Mount a named volume `upload-data` to a path inside the app container (e.g., `/app/uploads`). The `IFileStorageService` implementation in a future story will use this path. Expose the path via `FileUpload__StoragePath` env var.

### Environment Variables

All config MUST be overridable via environment variables (NFR29). Use ASP.NET Core's `__` (double underscore) convention for nested config:

**App environment variables:**

| Variable | Default | Purpose |
|----------|---------|---------|
| `ConnectionStrings__SimpleChatDb` | (see connection string above) | MSSQL connection string |
| `Redis__ConnectionString` | `redis:6379` | Redis connection |
| `Jwt__Secret` | `CHANGE-THIS-IN-PRODUCTION-min-32-chars!!` | JWT signing key |
| `Jwt__ExpiryMinutes` | `30` | Access token lifetime |
| `FileUpload__MaxSizeMB` | `10` | Max upload file size |
| `FileUpload__StoragePath` | `/app/uploads` | File storage path in container |
| `ASPNETCORE_ENVIRONMENT` | `Production` | .NET environment |

**MSSQL environment variables:**

| Variable | Default | Purpose |
|----------|---------|---------|
| `ACCEPT_EULA` | `Y` | **Required** — legal acceptance for SQL Server |
| `MSSQL_SA_PASSWORD` | (from `.env` file) | SA password — must meet complexity: 8+ chars, upper+lower+digit+symbol |

**SA password security:** The SA password MUST NOT be hardcoded in `docker-compose.yml`. Use `${MSSQL_SA_PASSWORD}` referencing a `.env` file. The `.env` file is gitignored. Provide `.env.example` with a placeholder and documentation. The connection string in the app service references the same `${MSSQL_SA_PASSWORD}` variable.

**Restart policy:** All services use `restart: unless-stopped` to survive host reboots and container crashes (supports NFR24).

### .dockerignore

Must exclude to keep build context small and avoid leaking secrets:
```
**/bin/
**/obj/
**/node_modules/
.git/
_bmad*/
_bmad-output/
tests/
**/.vs/
*.md
!README.md
```

### What This Story Does NOT Do

- Does NOT implement health check endpoints — that's Story 1.4
- Does NOT implement auto-migration on startup — that's Story 1.3
- Does NOT add Serilog or structured logging — that's Story 1.4
- Does NOT modify `Program.cs` or add service registrations — the only source code change is adding a `Condition` attribute to the `PublishRunWebpack` target in `Web.csproj`
- Does NOT configure CI/CD pipeline — that's Story 1.5
- Does NOT add Redis or SignalR packages to the .NET project — those come in later stories when needed
- Does NOT configure CORS for production — deferred from Story 1.1 review, to be addressed when auth is implemented

### Previous Story Intelligence (Story 1.1)

**Key learnings from Story 1.1 implementation:**
- Template used `.slnx` format (not `.sln`) — Dockerfile must reference `SimpleChat.slnx`
- Angular output goes to `src/Web/ClientApp/dist/browser/` (Angular 21 default output path)
- SDK version is .NET 10.0.102 (see `global.json`)
- Central package management is used (`Directory.Packages.props`) — `dotnet restore` needs `nuget.config` to clear non-standard global sources
- The `Web.csproj` `PublishRunWebpack` target auto-builds Angular on `dotnet publish` — in Docker, this must be disabled via a `Condition` attribute because Node.js is not in the SDK stage
- Jest replaced Karma for Angular testing — irrelevant for Docker build but note `jest.config.js` exists
- `package-lock.json` exists — use `npm ci` (not `npm install`) for reproducible builds

**Files from Story 1.1 relevant to this story:**
- `SimpleChat.slnx` — solution file referenced in restore/build
- `Directory.Build.props` — sets `net10.0` TFM
- `Directory.Packages.props` — central package versions
- `nuget.config` — NuGet source configuration
- `global.json` — SDK version constraint
- `src/Web/Web.csproj` — contains `PublishRunWebpack` target and `SpaRoot` property
- `src/Web/ClientApp/package.json` — Angular build scripts
- `src/Web/ClientApp/package-lock.json` — lockfile for `npm ci`
- `src/Web/Program.cs` — `UseFileServer()` + `MapFallbackToFile("index.html")` already handle SPA serving
- `src/Web/appsettings.json` — current connection string uses `(localdb)`, will be overridden by Docker env vars

**Review findings deferred to this story:**
- CORS `AllowAny*` not environment-gated in `Program.cs` — noted but NOT in scope for this story (will be addressed with auth in Story 2.x)

### Git Intelligence

Recent commits show Story 1.1 scaffolded 132 files. The project compiles (`dotnet build`) and Angular builds (`ng build`). The solution uses `.slnx` format. No Docker-related files exist yet.

### Project Structure Notes

New files created by this story:
```
Dockerfile                          # repo root
.dockerignore                       # repo root
docker-compose.yml                  # repo root
docker-compose.override.yml         # repo root
.env.example                        # repo root (tracked in git)
.env                                # repo root (gitignored — copy of .env.example with working defaults)
docker/mssql-init.sh                # MSSQL memory cap init script
```

Modified files:
```
src/Web/Web.csproj                  # Add Condition to PublishRunWebpack target
.gitignore                          # Add .env entry
```

This matches the architecture document's project structure which shows these files at the repo root level.

### References

- [Source: architecture.md — Infrastructure & Deployment decisions table, §Docker Compose, §Development Workflow]
- [Source: architecture.md — §Complete Project Directory Structure: Dockerfile, docker-compose.yml, docker-compose.override.yml at root]
- [Source: architecture.md — NFR28-33 coverage: docker-compose up, env var config, multi-stage Dockerfile]
- [Source: epics/epic-1 — Story 1.2 acceptance criteria]
- [Source: prd.md — NFR29 (no in-container editing), NFR33 (image < 500MB)]
- [Source: implementation-artifacts/1-1-project-scaffold-solution-structure.md — .slnx format, SDK version, Angular output path, nuget.config]
- [Source: implementation-artifacts/deferred-work.md — CORS deferred item noted]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
