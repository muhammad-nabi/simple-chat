---
project_name: 'simple-chat'
user_name: 'Nabi'
date: '2026-03-31'
sections_completed: ['technology_stack', 'language_specific', 'framework_specific', 'testing', 'code_quality_style', 'workflow', 'critical_dont_miss']
status: 'complete'
optimized_for_llm: true
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

### Backend
- .NET 10 (SDK 10.0.102) / C# 14 — target framework `net10.0`
- TreatWarningsAsErrors enabled — every nullable warning, unused variable, or missing await is a build failure
- Nullable enabled — use proper null checks, no `null!` shortcuts
- ImplicitUsings enabled — do NOT add `using System;`, `using System.Linq;`, `using System.Threading.Tasks;` etc.
- ASP.NET Core Minimal API with IEndpointGroup pattern
- Entity Framework Core 10.x — code-first, auto-migration on startup, SQL Server provider
- MediatR 14.1.0 — in-process CQRS bus + notification fan-out
- FluentValidation 12.1.1 — command validation via MediatR pipeline behaviors
- AutoMapper 16.1.1, Ardalis.GuardClauses 5.0.0
- BCrypt.Net-Next 4.1.0 (password hashing)
- Serilog.AspNetCore 10.0.0 — structured JSON logging to stdout
- StackExchange.Redis 2.12.4 — sessions, presence, caching
- SignalR — single ChatHub, real-time messaging. Hub auth via JWT in query string (not headers)
- Scalar.AspNetCore 2.13.13 — API documentation at /scalar

### Frontend
- Angular 21.1.5 / TypeScript 5.9.3
- RxJS 7.8.1, Zone.js 0.16.0
- Standalone components only — do NOT generate NgModule-based code or use @NgModule
- BehaviorSubject state management (not NgRx)

### Testing
- **NUnit 4.5.1** (NOT xUnit) — use `[Test]`, `[TestCase]`, NOT `[Fact]`/`[Theory]`
- Moq 4.20.72 + **Shouldly 4.3.0** — use `result.ShouldBe(expected)`, NOT `Assert.AreEqual`
- Testcontainers 4.11.0 — use existing TestcontainersFixture and CustomWebApplicationFactory. Database reset uses custom dynamic SQL (not Respawn)
- Jest 30.3.0 + jest-preset-angular 16.1.1 (frontend) — use existing jest config, do not generate fresh configs

### Infrastructure & Build
- Docker Compose: exactly 3 containers (app + MSSQL + Redis) — no additional containers in V1
- MSSQL memory capped at ~512MB — 3GB total stack budget is a hard constraint
- Central package management via Directory.Packages.props — NEVER add Version= to PackageReference in .csproj files
- Directory.Build.props sets TargetFramework centrally — do NOT add <TargetFramework> in individual .csproj files
- ArtifactsPath redirects build output to `artifacts/` — not default bin/obj folders
- Starter template: Clean.Architecture.Solution.Template v10.8.0

## Critical Implementation Rules

### Language-Specific Rules

#### C# Rules
- **Commands/Queries/Responses are records** — `public record LoginCommand(...) : IRequest<LoginResponse>`. Handlers are classes
- **Domain entities are abstract classes** inheriting from BaseEntity → BaseAuditableEntity
- **Global usings exist per project** — check `GlobalUsings.cs` in each project before adding usings. Application imports MediatR, AutoMapper, FluentValidation, EF Core, Guard Clauses
- **Guard clauses at method entry** — use `Guard.Against.Null()`, `Guard.Against.AnonymousMethod()` from Ardalis.GuardClauses (already in global usings)
- **Custom exceptions, not Result types** — throw `ValidationException`, `NotFoundException`, `ForbiddenAccessException`, `UnauthorizedAccessException`. ProblemDetailsExceptionHandler maps these to RFC 9110 HTTP responses
- **Anti-enumeration on auth** — always throw generic "Invalid email or password." on auth failures, never reveal which field is wrong
- **DI extension methods use `IHostApplicationBuilder`** — `public static void AddInfrastructureServices(this IHostApplicationBuilder builder)`, NOT `IServiceCollection`
- **CancellationToken on all async handler methods** — `Task<T> Handle(TCommand request, CancellationToken cancellationToken)`
- **EF configurations use Fluent API** — `IEntityTypeConfiguration<T>` classes in Infrastructure/Data/Configurations, auto-discovered via `ApplyConfigurationsFromAssembly()`
- **Interceptors for cross-cutting** — AuditableEntityInterceptor (Created/Modified timestamps), DispatchDomainEventsInterceptor. Injected as `ISaveChangesInterceptor` via DI

#### TypeScript/Angular Rules
- **`inject()` for all DI** — never use constructor injection. `private http = inject(HttpClient);`
- **BehaviorSubject for state** — expose as `readonly` Observable via `.asObservable()`. Use `.value` for synchronous access
- **No path aliases in source code** — import directly from relative paths. Jest config has `@app/` and `@environments/` aliases for test resolution only
- **Functional patterns everywhere** — `HttpInterceptorFn` (not class interceptors), `CanActivateFn` (not class guards), `APP_INITIALIZER` with `firstValueFrom()`
- **subscribe() with next/error callbacks** — not `.then()` promises. Check `HttpErrorResponse.status` for error handling
- **shareReplay(1) + finalize()** for deduplication — see refresh token pattern in AuthService
- **Standalone components only** — `standalone: true`, import dependencies directly in component `imports` array
- **Reactive forms with FormBuilder** — `this.fb.group({...}, { updateOn: 'blur' })`
- **Current pattern: no Signals, no OnPush, no takeUntilDestroyed** — codebase uses BehaviorSubject + manual subscribe. Follow existing pattern unless team decides to adopt these

### Framework-Specific Rules

#### Clean Architecture + CQRS
- **4-project structure** — Domain → Application → Infrastructure → Web. Dependencies point inward only
- **Feature folders by domain module** — Identity/, Messaging/, Presence/, Files/ within each layer. Modules are namespace folders, not separate assemblies
- **Handler-per-file vertical slices** — each command/query gets its own folder: `[Feature]/Commands/[Name]/` containing Command.cs, Handler.cs, and optionally Validator.cs and Response.cs
- **MediatR pipeline behavior order** — Logging → UnhandledException → Authorization → Validation → Performance. Do not reorder or skip
- **Authorization via `[Authorize]` attribute on commands** — AuthorizationBehaviour checks roles/policies before handler executes. Endpoints use `.AllowAnonymous()` or `.RequireAuthorization()`
- **ValidationBehaviour runs FluentValidation** — validators co-located with commands, exceptions thrown as `ValidationException` with field errors
- **PerformanceBehaviour logs warnings >500ms** — keep handlers fast, offload heavy work
- **Cross-module communication via MediatR only** — no direct references between module namespaces
- **Domain events** — inherit from `BaseEvent : INotification`, dispatched by `DispatchDomainEventsInterceptor` on EF SaveChanges

#### Minimal API Endpoints
- **Implement `IEndpointGroup`** — define `static string? RoutePrefix` and `static void Map(RouteGroupBuilder)`. Auto-discovered via reflection
- **Request/Response DTOs defined in endpoint file** — Web-layer DTOs (e.g., `LoginClientResponse`) are separate from Application-layer responses (e.g., `LoginResponse`)
- **Static handler methods** — receive `ISender`, `HttpContext`, and request DTO as parameters. Return `TypedResults.Ok()`, `TypedResults.Unauthorized()` etc.
- **Handler method names become OpenAPI operationIds** — name them clearly for typed client generation
- **Rate limiting** — two policies: `"auth"` (IP-based, 20/min), `"auth-per-user"` (email-based, 5/min). Apply via `.RequireRateLimiting()`

#### EF Core
- **DbContext inherits IdentityDbContext<ApplicationUser>** and implements `IApplicationDbContext` interface (defined in Application layer)
- **No repository pattern** — inject `IApplicationDbContext` directly into handlers
- **Fluent API configurations only** — `IEntityTypeConfiguration<T>` in Infrastructure/Data/Configurations/, auto-discovered via `ApplyConfigurationsFromAssembly()`
- **Auto-migration on startup** — `MigrateAsync()` runs in all environments. Single-instance constraint: do not run multiple instances concurrently during migration
- **Database initializer seeds roles** (Admin, Member) — first registered user gets Admin role
- **Interceptors injected via DI** — `options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())`

#### Angular Frontend
- **Core/Shared/Features organization** — core/ for singleton services/guards, features/ for lazy-loaded feature components, shared/components/ for reusable UI
- **Lazy-loaded routes** — `loadComponent: () => import(...)` in app.routes.ts. Auth guard on protected routes
- **Functional providers in app.config.ts** — `provideRouter()`, `provideHttpClient(withFetch(), withInterceptors([...]))`, `APP_INITIALIZER` for session restore
- **Auth flow** — APP_INITIALIZER restores session via refresh token before routing activates. Interceptor adds Bearer token, retries on 401, redirects to /login on refresh failure
- **`withCredentials: true`** on auth HTTP calls for cookie-based refresh tokens
- **`isLoggingOut` flag** — prevents interceptor retry loop during logout
- **`IUser` / `CurrentUser` pattern** — Web layer registers `CurrentUser` as scoped `IUser`. Inject `IUser` in Application layer handlers to get authenticated user context — do NOT access `HttpContext.User` directly from Application layer

### Testing Rules

#### Unit Tests (Application.UnitTests, Domain.UnitTests)
- **NUnit framework** — `[Test]`, `[TestCase]`, `[SetUp]`, `[OneTimeSetUp]`. Global using in `GlobalUsings.cs`
- **Arrange/Act/Assert pattern** — standard pattern with `// Arrange`, `// Act`, `// Assert` comments. Optional on trivial tests
- **Moq for mocking** — `new Mock<IInterface>()`, `.Setup().ReturnsAsync()`, `.Verify(x => ..., Times.Once)`, access via `.Object`
- **Shouldly for assertions** — `result.ShouldBe(expected)`, `collection.ShouldBeEmpty()`. Use Shouldly over NUnit's `Assert.That` where possible
- **FluentValidation.TestHelper** — `result.ShouldHaveValidationErrorFor(x => x.Property)` for validator tests
- **Mocks initialized in `[SetUp]`** — fresh mocks per test, handler constructed with `.Object` references
- **Test method naming** — `MethodUnderTest_Scenario_ExpectedOutcome` (e.g., `Handle_ValidCredentials_ShouldReturnTokensAndUserInfo`)
- **Test class naming** — `{TargetClass}Tests` (e.g., `LoginCommandHandlerTests`)
- **Mirror Application folder structure** — `Identity/Commands/Login/LoginCommandHandlerTests.cs` matches source path
- **Coverage targets** — 100% on command handlers and validators, 80%+ overall. CI runs `--coverage` on frontend tests

#### Integration Tests (Infrastructure.IntegrationTests)
- **Testcontainers for real MSSQL + Redis** — `TestcontainersFixture` as `[SetUpFixture]` with `[OneTimeSetUp]`/`[OneTimeTearDown]`
- **CustomWebApplicationFactory** — overrides connection strings to point at test containers, replaces DbContext registration
- **Per-test database reset** — `ResetDatabaseAsync()` in `[SetUp]` uses dynamic SQL to clear all tables (not Respawn). Also clears Redis `session:*` keys
- **Factory + client per test** — `new CustomWebApplicationFactory()` and `_factory.CreateClient()` in `[SetUp]`, dispose in `[TearDown]`
- **Direct HttpClient calls** — `_client.PostAsJsonAsync()`, `response.Content.ReadFromJsonAsync<JsonElement>()`
- **Cookie handling** — extract refresh tokens from `Set-Cookie` header for auth flow tests
- **No mocking in integration tests** — real database, real Redis, real HTTP pipeline

#### Frontend Tests (Jest)
- **Jest 30 + jest-preset-angular** — existing `jest.config.js` with `moduleNameMapper` for `@app/` and `@environments/` aliases
- **TestBed for components** — `TestBed.configureTestingModule({ imports: [Component, ...] })` with standalone components
- **Minimal coverage currently** — only `app.component.spec.ts` exists. Follow existing pattern when adding new tests
- **DOM assertions** — `fixture.nativeElement.querySelector()` + `expect().toBeTruthy()`

### Code Quality & Style Rules

#### C# Naming Conventions (enforced via .editorconfig)
- **PascalCase** — types, namespaces, methods, properties, events, public fields, constants, enums, local functions
- **camelCase** — local variables, local constants, parameters
- **_camelCase** — private fields (prefix with underscore)
- **s_camelCase** — private static fields (prefix with `s_`)
- **IPascalCase** — interfaces (prefix with `I`)
- **TPascalCase** — type parameters (prefix with `T`)
- **Namespace must match folder path** — `dotnet_style_namespace_match_folder = true`. A file in `Application/Identity/Commands/Login/` must use that namespace

#### C# Build-Breaking Rules (warning level + TreatWarningsAsErrors)
- **`readonly` on private fields** — if a field can be readonly, it MUST be. `dotnet_style_readonly_field = true:warning` — build fails otherwise
- **`static` on local functions** — local functions that don't capture state MUST be `static`. `csharp_prefer_static_local_function = true:warning` — build fails otherwise
- **All nullable warnings are errors** — proper null checks required, no `null!` shortcuts

#### C# Style Preferences (suggestion level — follow for consistency)
- **File-scoped namespaces** — `namespace Foo;` not `namespace Foo { ... }`
- **Primary constructors preferred** — use when type is apparent
- **Explicit types over `var`** — `csharp_style_var_*` = false (silent, but follow for consistency)
- **Pattern matching** — prefer `is`, `not`, switch expressions over casts/null checks
- **Expression-bodied** — use for accessors, indexers, lambdas, properties. NOT for constructors, methods, operators, local functions
- **Braces preferred** — `csharp_prefer_braces = true`
- **Allman brace style** — opening brace on new line (`csharp_new_line_before_open_brace = all`)
- **No `this.` qualification** — omit on fields, properties, methods, events
- **Sort system usings first**, no unnecessary using directives
- **Unused parameters flagged** — `dotnet_code_quality_unused_parameters = all`

#### Formatting (prevents noisy diffs)
- **LF line endings** — `end_of_line = lf` for C# files. NEVER generate CRLF
- **Insert final newline** — all files must end with a newline character
- **4-space indent** for C#, **2-space indent** for XML/JSON/JS/Angular/SCSS
- **UTF-8 encoding** — no BOM except for `.razor`/`.cshtml` files

#### Angular/TypeScript Style
- **`strict: true`** in tsconfig — explicit return types, null checks, no implicit any
- **Single quotes** for TypeScript strings
- **2-space indent**, UTF-8, trim trailing whitespace
- **kebab-case** for file names (`login.component.ts`, `auth.service.ts`)
- **Component selector** — `app-kebab-case` prefix
- **SCSS** for component styles
- **ESLint enforced** — `angular-eslint` 21.3.1 + `eslint` 10.x. Code must pass `ng lint`

### Development Workflow Rules

#### Branch & Commit Conventions
- **Branch naming** — `{type}/{description}` (e.g., `prep/fix-test-infrastructure`, `story/2.4`)
- **Commit message pattern** — `story X.Y implemented with review`, `chore: description`, `epic N retro`
- **Never commit directly to main** — all changes via feature branches + PR
- **Story-driven workflow** — stories are created, validated, implemented, then reviewed in sequence

#### CI Pipeline (GitHub Actions — `.github/workflows/ci.yml`)
- **Triggers** — PR to main + push to main. Concurrency: cancel in-progress PR builds
- **Build & Test job:**
  1. `dotnet build --configuration Release --no-incremental` (warnings = errors)
  2. `dotnet test --filter "FullyQualifiedName~UnitTests"` (backend unit tests)
  3. `npm ci` + `npx ng lint` + `npm test -- --ci --coverage` (frontend)
  4. `dotnet test --filter "FullyQualifiedName~IntegrationTests"` (Testcontainers — needs Docker)
- **Docker job** — builds after tests pass. PR: build only. Main push: build + push to ghcr.io
- **All code must pass:** backend build (warnings-as-errors), backend unit tests, frontend lint, frontend tests, integration tests, Docker build

#### Docker & Deployment
- **Multi-stage Dockerfile** — restore → build → frontend (node:24-alpine) → publish → runtime (aspnet:10.0)
- **App runs on port 8080** — `ASPNETCORE_URLS=http://+:8080`
- **3 containers only** — app, mssql (2022-latest), redis (7-alpine)
- **All config via environment variables** — connection strings, JWT secret, file upload settings
- **Health checks** — app: `curl http://localhost:8080/health/ready`, mssql: sqlcmd ping, redis: redis-cli ping
- **Volumes** — `mssql-data` (persistent DB), `upload-data` (file uploads at `/app/uploads`)
- **MSSQL requires `MSSQL_SA_PASSWORD` env var** — no default, must be set
- **Redis password** — defaults to `SimpleChat_Redis_Dev!` in dev

#### Local Development
- **Node 24** required for Angular frontend
- **.NET SDK 10.0.102** required for backend
- **Docker required** for integration tests (Testcontainers)

### Critical Don't-Miss Rules

#### Anti-Patterns to Avoid
- **No repository pattern** — do NOT create repository classes. Inject `IApplicationDbContext` directly into MediatR handlers. Each handler IS a single-purpose repository method
- **No NgModules** — do NOT create Angular modules. All components are standalone with direct imports
- **No constructor injection in Angular** — use `inject()` function, not constructor parameters
- **No `var` keyword** — use explicit types in C# (project convention)
- **No `.Result` or `.Wait()`** — always use `async`/`await` for async operations
- **No `Version=` in PackageReference** — central package management handles all versions
- **No additional Docker containers** — 3 containers only (app + MSSQL + Redis) for V1
- **No xUnit attributes** — this project uses NUnit (`[Test]`, not `[Fact]`)

#### Security Rules
- **Anti-enumeration** — auth error messages must be generic ("Invalid email or password."), never reveal which field failed
- **HttpOnly + Secure + SameSite=Strict** — refresh token cookies must always have all three flags
- **JWT in memory only** — access tokens stored in Angular service memory (BehaviorSubject), never localStorage/sessionStorage
- **`withCredentials: true`** — required on all auth-related HTTP calls for cookie transmission
- **Refresh token rotation** — new refresh token on each use, old one invalidated. Reuse detection triggers emergency lockout
- **File validation** — whitelist MIME types + magic byte validation + reject executables
- **Parameterized queries only** — never concatenate SQL strings (EF Core handles this, but raw SQL must use parameters)

#### Performance Gotchas
- **MSSQL memory cap** — SQL Server defaults to consuming 80% of host memory. Docker Compose caps at ~512MB. Do not remove this constraint
- **Single-instance for MVP** — no horizontal scaling, no Redis SignalR backplane, no distributed locks
- **Persist-first message flow** — messages must be saved to DB before broadcasting via SignalR. Never broadcast without persisting first
- **Cursor-based pagination** — use `WHERE Id < @cursor` pattern, not OFFSET/FETCH for message history
- **Angular lazy loading** — all feature routes must use `loadComponent`. Bundle size target <500KB

---

## Usage Guidelines

**For AI Agents:**
- Read this file before implementing any code
- Follow ALL rules exactly as documented
- When in doubt, prefer the more restrictive option
- Check existing code patterns when this document doesn't cover a specific case

**For Humans:**
- Keep this file lean and focused on agent needs
- Update when technology stack changes
- Review quarterly for outdated rules
- Remove rules that become obvious over time

Last Updated: 2026-03-31
