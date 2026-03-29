# Story 1.5: CI Pipeline Configuration

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want an automated CI pipeline that validates code quality on every pull request,
so that broken code and regressions are caught before merging.

## Acceptance Criteria

1. **Given** a pull request is opened against the repository
   **When** the GitHub Actions CI workflow runs
   **Then** the pipeline executes in sequence: unit tests + linting → integration tests → Docker image build
   **And** the pipeline fails fast if any stage fails (subsequent stages do not run)

2. **Given** the CI pipeline runs integration tests
   **When** the tests execute
   **Then** Testcontainers provisions real MSSQL and Redis instances for integration test execution
   **And** integration tests run against actual database and cache infrastructure (not mocks)

3. **Given** a commit is pushed to the main branch
   **When** the CI workflow completes successfully
   **Then** the Docker image is built and pushed to GitHub Container Registry (ghcr.io)
   **And** the image is tagged with the commit SHA and `latest`

4. **Given** the CI pipeline configuration
   **When** a developer reviews the workflow file
   **Then** both backend (.NET) and frontend (Angular) linters are configured and enforced
   **And** the total pipeline duration should aim for under 5 minutes for the PR workflow (soft target, not a hard gate)

## Tasks / Subtasks

- [ ] Task 0: Resolve branch naming (AC: #1, #3)
  - [ ] 0.1 Check which branch is the default/main branch: run `git branch -a` and check GitHub remote. Current branch is `master` but architecture spec and sprint status reference `main`
  - [ ] 0.2 **Decision:** Either rename `master` → `main` (preferred, aligns with architecture spec) via `git branch -m master main && git push -u origin main`, OR update the CI workflow triggers to use `master`. Pick one and be consistent
  - [ ] 0.3 Update the CI workflow trigger branch name to match the actual default branch

- [ ] Task 1: Create CI workflow `.github/workflows/ci.yml` (AC: #1, #4)
  - [ ] 1.1 Create `.github/workflows/` directory
  - [ ] 1.2 Create `ci.yml` triggered on `pull_request` and `push` to the default branch (resolved in Task 0)
  - [ ] 1.3 Use a **single job with sequential steps** for Stage 1 + Stage 2, then a **dependent job** for Docker build. This avoids redundant checkout/setup across 3 separate runners and keeps the pipeline under the ~5 min target. Structure: Job 1 (`build-test`) = lint + unit tests + integration tests (all share workspace). Job 2 (`docker`) = Docker build + conditional push (separate job because it needs different permissions)
  - [ ] 1.4 Add concurrency group `ci-${{ github.ref }}` with `cancel-in-progress: true` to avoid duplicate runs on rapid pushes
  - [ ] 1.5 Add `permissions: contents: read` at workflow level, and `permissions: packages: write` on the Docker job (required for ghcr.io push with `GITHUB_TOKEN`)

- [ ] Task 2: Stage 1 — Unit tests + Linting (AC: #1, #4)
  - [ ] 2.1 **Backend lint + build:** `actions/setup-dotnet@v4` with `dotnet-version: '10.0.x'`, then `dotnet build SimpleChat.slnx --configuration Release --no-incremental` (warnings-as-errors enforced by .editorconfig/Directory.Build.props)
  - [ ] 2.2 **Backend unit tests:** `dotnet test SimpleChat.slnx --configuration Release --no-build --filter "FullyQualifiedName~UnitTests" --logger "trx;LogFileName=test-results.trx"` — runs Domain.UnitTests + Application.UnitTests only (not integration tests)
  - [ ] 2.3 **NuGet cache:** Add `actions/cache@v4` step caching `~/.nuget/packages` keyed on hash of `**/packages.lock.json` or `Directory.Packages.props`
  - [ ] 2.4 **Frontend setup:** `actions/setup-node@v4` with `node-version: 24`, then `npm ci` in `src/Web/ClientApp/` directory. Cache npm with `actions/cache@v4` caching `~/.npm` (NOT `node_modules`) keyed on hash of `src/Web/ClientApp/package-lock.json`
  - [ ] 2.5 **Frontend lint:** `npx ng lint` in `src/Web/ClientApp/` — if ESLint is not yet configured in angular.json, add `@angular-eslint/schematics` and run `ng add @angular-eslint/schematics` as a subtask, then configure the lint architect target
  - [ ] 2.6 **Frontend unit tests:** `npm test -- --ci --coverage` in `src/Web/ClientApp/` (Jest runs via `ng test`, `--ci` disables interactive mode)
  - [ ] 2.7 **Upload test artifacts:** Use `actions/upload-artifact@v4` to upload `.trx` files and Jest coverage reports — helps debug failed CI runs

- [ ] Task 3: Stage 2 — Integration tests (AC: #1, #2)
  - [ ] 3.1 Add Testcontainers NuGet packages to `Directory.Packages.props` and reference in `Infrastructure.IntegrationTests.csproj`. **Pre-approved exception to CLAUDE.md "don't add packages" rule** — this story's explicit purpose requires new test infrastructure packages. Required packages with versions (check NuGet for latest stable compatible with .NET 10): `Testcontainers` (latest 4.x), `Testcontainers.MsSql` (latest 4.x), `Testcontainers.Redis` (latest 4.x — if package doesn't exist, use `Testcontainers` base with `new ContainerBuilder().WithImage("redis:7-alpine")`). Also add `Microsoft.AspNetCore.Mvc.Testing` (match ASP.NET Core 10.0.x version) for `WebApplicationFactory<Program>`
  - [ ] 3.2 Create `tests/Infrastructure.IntegrationTests/TestcontainersFixture.cs` — NUnit `[SetUpFixture]` that starts MSSQL and Redis Testcontainers, exposes connection strings, implements `IAsyncDisposable` for cleanup
  - [ ] 3.3 Add `<ProjectReference Include="..\..\src\Web\Web.csproj" />` to `Infrastructure.IntegrationTests.csproj` — required for `WebApplicationFactory<Program>` to resolve the application entry point
  - [ ] 3.4 Create `tests/Infrastructure.IntegrationTests/CustomWebApplicationFactory.cs` — inherits `WebApplicationFactory<Program>`, overrides `ConfigureWebHost` to replace the EF Core `DbContextOptions` with Testcontainers MSSQL connection string and replace the `IConnectionMultiplexer` singleton with a new `ConnectionMultiplexer.Connect()` to the Testcontainers Redis, then calls `context.Services.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync()` to apply migrations
  - [ ] 3.5 Create a smoke integration test `tests/Infrastructure.IntegrationTests/HealthCheckIntegrationTests.cs` — verify `/health/ready` returns 200 when running against real MSSQL + Redis (validates the full startup pipeline with real dependencies)
  - [ ] 3.6 In `ci.yml`, run integration tests as a step in the same job (after unit tests): `dotnet test SimpleChat.slnx --configuration Release --no-build --filter "FullyQualifiedName~IntegrationTests" --logger "trx;LogFileName=integration-results.trx"`. Note: integration tests share the same runner as unit tests (single-job approach for speed). Docker is available on ubuntu-latest by default — Testcontainers uses `/var/run/docker.sock` automatically, no Docker-in-Docker config needed

- [ ] Task 4: Stage 3 — Docker image build (AC: #1, #3)
  - [ ] 4.1 In `ci.yml`, add Docker build stage that depends on Stage 2 passing
  - [ ] 4.2 **PR builds:** `docker build -t simplechat:pr-${{ github.sha }} .` — build only, no push (validates Dockerfile works)
  - [ ] 4.3 **Main branch pushes:** Use `docker/login-action@v3` to authenticate to ghcr.io using `${{ secrets.GITHUB_TOKEN }}` (built-in, no manual secret setup needed)
  - [ ] 4.4 **Main branch pushes:** Use `docker/build-push-action@v6` to build and push with tags: `ghcr.io/${{ github.repository }}:${{ github.sha }}` and `ghcr.io/${{ github.repository }}:latest`
  - [ ] 4.5 Add `docker/setup-buildx-action@v3` for layer caching via `cache-from: type=gha` and `cache-to: type=gha,mode=max` to speed up subsequent builds

- [ ] Task 5: Verify ESLint configuration for Angular (AC: #4)
  - [ ] 5.1 Check if `@angular-eslint` is already configured in `angular.json`. If the `lint` target does not exist, run `ng add @angular-eslint/schematics` in `src/Web/ClientApp/` to scaffold ESLint config
  - [ ] 5.2 Ensure `angular.json` has an `architect.lint` section with `@angular-eslint/builder:lint`
  - [ ] 5.3 Verify `npx ng lint` passes locally before adding to CI

- [ ] Task 6: Verification (AC: #1-#4)
  - [ ] 6.1 Validate `ci.yml` syntax with `actionlint` or by reviewing against GitHub Actions schema
  - [ ] 6.2 Commit workflow file and create a test PR to verify pipeline runs
  - [ ] 6.3 Verify fail-fast: introduce a deliberate test failure, confirm later stages are skipped
  - [ ] 6.4 Verify Testcontainers integration tests start real MSSQL + Redis and pass
  - [ ] 6.5 Verify Docker image builds successfully in CI
  - [ ] 6.6 (Main branch only) Verify image is pushed to ghcr.io with correct tags

### Review Findings

_(To be populated during code review)_

## Dev Notes

### Architecture-Specified CI/CD Pipeline Structure

The architecture document specifies this exact CI structure:

```
PR workflow:  unit tests + lint → integration tests → Docker build
Main workflow: unit tests + lint → integration tests → Docker build → push to ghcr.io
```

Files to create per architecture spec:
- `.github/workflows/ci.yml` — PR: unit + lint → integration → Docker build
- `.github/workflows/release.yml` — Main: + push to ghcr.io (or merge into ci.yml with conditional)

**Decision:** Use a single `ci.yml` with conditional push. The architecture mentions two files (`ci.yml` + `release.yml`) but the logic is identical except for the push step. A single file reduces maintenance and drift risk. The GHCR push is gated by `if: github.ref == 'refs/heads/main'` on the Docker job.

### Current Project State

**Solution file:** `SimpleChat.slnx` (modern slnx format, NOT traditional .sln — commands must use `SimpleChat.slnx` not `SimpleChat.sln`)

**.NET SDK:** 10.0.102 (specified in `global.json` with `rollForward: latestFeature`)

**Test framework:** NUnit 4.5.1 (NOT xUnit — the architecture mentions xUnit but the actual codebase uses NUnit. The CI must use NUnit-compatible test runners. `dotnet test` works with NUnit via NUnit3TestAdapter)

**Test projects (all NUnit):**
| Project | Path | Purpose | Filter Pattern |
|---------|------|---------|----------------|
| Domain.UnitTests | `tests/Domain.UnitTests/` | Domain logic tests | `FullyQualifiedName~UnitTests` |
| Application.UnitTests | `tests/Application.UnitTests/` | Behavior/handler tests | `FullyQualifiedName~UnitTests` |
| Infrastructure.IntegrationTests | `tests/Infrastructure.IntegrationTests/` | DB/Redis integration | `FullyQualifiedName~IntegrationTests` |

**Existing unit tests:** 8 passing tests (from Story 1.4) — LoggingBehaviour, PerformanceBehaviour tests

**Integration test project:** Exists but currently empty (only `GlobalUsings.cs` + csproj). Testcontainers packages and test fixtures must be added in this story.

**Frontend:** Angular 21.1.5, Node 24, Jest 30.3.0 (`@angular-builders/jest`), TypeScript 5.9.3

**Frontend test command:** `npm test` → `ng test` → Jest (via `@angular-builders/jest`)

**Frontend lint:** ESLint may NOT be configured yet — `angular.json` needs a `lint` target. If missing, `@angular-eslint/schematics` must be added. Do NOT assume `ng lint` works without verifying the lint architect target exists.

### Dockerfile (Already Complete)

The multi-stage Dockerfile exists and works (Story 1.2). It has 5 stages:
1. `restore` — .NET SDK 10.0, restores NuGet packages
2. `build` — builds .NET backend in Release mode
3. `frontend` — Node 24-alpine, builds Angular with `ng build --configuration production`
4. `publish` — publishes .NET app, copies Angular dist to wwwroot
5. `runtime` — ASP.NET 10.0 runtime, curl installed, port 8080

The CI Docker build step just runs `docker build .` — no modifications to the Dockerfile needed.

### Testcontainers Setup for Integration Tests

**GitHub Actions compatibility:** `ubuntu-latest` runners have Docker Engine pre-installed. Testcontainers .NET uses the Docker socket directly (`/var/run/docker.sock`) — no Docker-in-Docker configuration needed.

**Required NuGet packages** (check NuGet for latest stable .NET 10-compatible versions):
- `Testcontainers` (base library, latest 4.x)
- `Testcontainers.MsSql` (MSSQL container module, latest 4.x)
- `Testcontainers.Redis` (Redis container module, latest 4.x — if this specific package doesn't exist, use `new ContainerBuilder().WithImage("redis:7-alpine").WithPortBinding(6379, true)` from the base library)
- `Microsoft.AspNetCore.Mvc.Testing` (match ASP.NET 10.0.x — required for `WebApplicationFactory<Program>`)

**Pattern:** Use NUnit `[SetUpFixture]` with `[OneTimeSetUp]`/`[OneTimeTearDown]` to start/stop containers once per test run. Expose connection strings as static properties. `CustomWebApplicationFactory` overrides `ConfigureWebHost` to remove existing `DbContextOptions<ApplicationDbContext>` and `IConnectionMultiplexer` registrations and re-add them with Testcontainers connection strings. Call `MigrateAsync()` to apply EF migrations against the test database.

### GitHub Actions Workflow Structure

**Two-job design** (balances speed vs. isolation):

**Job 1: `build-test`** (single runner, sequential steps sharing workspace):
1. Checkout → Setup .NET 10 + Node 24 → Cache NuGet + npm
2. `dotnet build` (backend lint via warnings-as-errors)
3. `dotnet test --filter UnitTests` (backend unit tests)
4. `npm ci` + `ng lint` + `npm test --ci` (frontend lint + unit tests)
5. `dotnet test --filter IntegrationTests` (Testcontainers — Docker available on ubuntu-latest)
6. Upload test artifacts

**Job 2: `docker`** (depends on `build-test`, separate permissions):
1. Checkout → Setup Buildx with GHA layer cache
2. `docker build` (validates image builds)
3. If `main` branch: login to ghcr.io + push with SHA + latest tags

**Key considerations:**
- Cache NuGet at `~/.nuget/packages`, npm at `~/.npm` (NOT `node_modules`)
- `${{ secrets.GITHUB_TOKEN }}` is automatically available for ghcr.io push — no manual secret configuration needed
- Docker job needs explicit `permissions: packages: write` for GHCR push
- Use `concurrency` groups to cancel superseded PR runs

### GHCR Image Tagging

Per architecture spec, images pushed to `ghcr.io`:
- Tag with commit SHA: `ghcr.io/{owner}/{repo}:{sha}` — immutable, traceable
- Tag with `latest`: `ghcr.io/{owner}/{repo}:latest` — convenience for `docker-compose pull`
- Push only on successful main branch builds (not on PRs)

### Scope Exclusions

Scope limited to PR validation and main-branch image push. Explicitly excluded: deployment steps (manual `docker-compose pull && up`), E2E tests (Playwright deferred to Phase 2), coverage thresholds (collected but not gated), semantic versioning/release tags, branch protection rules (GitHub admin task), matrix builds, and additional secrets beyond `GITHUB_TOKEN`.

### Previous Story Intelligence (Story 1.4)

Key learnings from Story 1.4 that impact this story:

- **Solution file is `SimpleChat.slnx`** (modern format) — all `dotnet` commands must reference this, not `.sln`
- **8 unit tests exist** in Application.UnitTests (LoggingBehaviour PII safety, PerformanceBehaviour timing tests). Domain.UnitTests exists but may be empty. All must pass in CI
- **Redis `IConnectionMultiplexer` established** — Testcontainers integration tests can verify Redis connectivity using the existing DI registration
- **Health endpoints work** (`/health/startup`, `/health/live`, `/health/ready`) — the smoke integration test should verify `/health/ready` returns 200 against real MSSQL + Redis
- **`AbortOnConnectFail = false`** was set on Redis `ConfigurationOptions` — this prevents app crash when Redis isn't immediately available, relevant for Testcontainers startup timing
- **Docker healthcheck uses `/health/ready`** — CI Docker build stage validates the image builds correctly but does NOT need to run healthchecks

### Git Intelligence

Recent commits (latest first):
- `acc9097` story 1.4 implemented with review
- `627f31f` story 1.4 created with validation
- `8482609` story 1.3 implemented with review

**CRITICAL: Branch name mismatch.** All work has been on `master` but the architecture spec, sprint status, and PR conventions reference `main`. Task 0 must resolve this BEFORE writing the workflow file. Preferred resolution: rename `master` → `main` to align with architecture spec. If renaming isn't possible (e.g., no push access to remote), update workflow triggers to use `master` instead.

### Project Structure Notes

Files to create:
```
.github/workflows/ci.yml                                          # Main CI workflow
tests/Infrastructure.IntegrationTests/TestcontainersFixture.cs     # Testcontainers setup
tests/Infrastructure.IntegrationTests/CustomWebApplicationFactory.cs # WebApplicationFactory with Testcontainers
tests/Infrastructure.IntegrationTests/HealthCheckIntegrationTests.cs # Smoke test for health endpoints
```

Files to modify:
```
Directory.Packages.props                                           # Add Testcontainers + Microsoft.AspNetCore.Mvc.Testing packages
tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj # Add Testcontainers + Mvc.Testing refs + ProjectReference to Web.csproj
```

Files NOT to modify:
```
Dockerfile                    # Already complete from Story 1.2
docker-compose.yml            # Not used in CI (Testcontainers replaces Docker Compose for tests)
src/Web/Program.cs            # No changes needed
```

### References

- [Source: architecture.md — §CI/CD: GitHub Actions, PR: unit + lint → integration → Docker build. Main: + push to ghcr.io]
- [Source: architecture.md — §Docker registry: GitHub Container Registry (ghcr.io), free for public repos]
- [Source: architecture.md — §Integration testing: Testcontainers for both MSSQL and Redis]
- [Source: architecture.md — §Testing Strategy: 100% on command handlers and validators, 80%+ overall]
- [Source: architecture.md — §Project Structure: .github/workflows/ci.yml and release.yml]
- [Source: architecture.md — §Pattern Verification: CI pipeline runs both backend and frontend linters before tests]
- [Source: architecture.md — §Development Workflow: dotnet build SimpleChat.sln, ng build --configuration production]
- [Source: epics/epic-1 — Story 1.5 acceptance criteria]
- [Source: prd.md — FR43 (health check endpoints), FR44 (structured logs)]
- [Source: implementation-artifacts/1-4-structured-logging-health-endpoints.md — Redis connection established, 8 unit tests passing, health endpoints working]
- [Source: global.json — .NET SDK 10.0.102]
- [Source: package.json — Angular 21.1.5, Jest 30.3.0, Node 24]

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
