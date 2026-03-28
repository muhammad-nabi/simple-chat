# Story 1.1: Project Scaffold & Solution Structure

Status: done

## Story

As an operator,
I want the project scaffolded with the correct architecture and module organization,
so that all subsequent development follows a consistent, proven structure.

## Acceptance Criteria

1. **Given** a developer runs the Jason Taylor Clean Architecture template command
   **When** the scaffold is generated
   **Then** the solution contains four projects: SimpleChat.Domain, SimpleChat.Application, SimpleChat.Infrastructure, SimpleChat.API (Web)
   **And** each project compiles without errors

2. **Given** the scaffolded solution
   **When** post-scaffold customizations are applied
   **Then** the Domain project contains module folders: Identity/, Messaging/, Presence/, Files/ with Entities/ and Events/ subfolders per module
   **And** the Domain project contains Common/ with BaseEntity.cs (Id as long, CreatedAt) and Enums/ (ConversationType, UserRole, PresenceStatus)
   **And** the Application project contains matching module folders with Commands/, Queries/, EventHandlers/ subfolders
   **And** the Application project contains Common/Interfaces/ with stubs for IFileStorageService, IMessageSearchService, IAuthenticationProvider, ICacheService
   **And** the Application project contains Common/Behaviors/ with ValidationBehavior and LoggingBehavior pipeline behaviors
   **And** the Application project contains Common/Exceptions/ with NotFoundException and ForbiddenAccessException
   **And** the Application project contains Common/Models/ with PagedResult (Items, HasMore, NextCursor)

3. **Given** the scaffolded Angular frontend
   **When** post-scaffold customizations are applied
   **Then** the Angular app contains src/app/core/, src/app/shared/, src/app/features/ directories
   **And** the features directory contains empty feature module folders: auth/, chat/, admin/, search/
   **And** the Angular app contains src/app/models/ for TypeScript interfaces
   **And** the test runner is switched from Karma to Jest with a passing default test
   **And** the Angular app builds without errors using `ng build`

## Tasks / Subtasks

- [x] Task 1: Scaffold from Jason Taylor Clean Architecture template (AC: #1)
  - [x] 1.1 Install template: `dotnet new install Clean.Architecture.Solution.Template`
  - [x] 1.2 Generate solution: `dotnet new ca-sln --client-framework Angular --database sqlserver --output SimpleChat`
  - [x] 1.3 Verify .NET 10 TFM in all `.csproj` files (`<TargetFramework>net10.0</TargetFramework>`)
  - [x] 1.4 Verify Angular 21 in `src/web/package.json` (`@angular/core` version)
  - [x] 1.5 Remove .NET Aspire orchestration projects and references (template includes Aspire by default — must be removed before any builds)
  - [x] 1.6 Remove template sample code (WeatherForecast entity, commands, queries, controller, and related registrations)
  - [x] 1.7 Create `.editorconfig` at solution root for .NET naming/formatting enforcement
  - [x] 1.8 Verify all four projects compile: `dotnet build SimpleChat.sln`

- [x] Task 2: Backend Domain layer post-scaffold customization (AC: #2)
  - [x] 2.1 Create module folders: Identity/, Messaging/, Presence/, Files/ each with Entities/, ValueObjects/, and Events/ subfolders
  - [x] 2.2 Create Common/BaseEntity.cs with `Id` (long) and `CreatedAt` (DateTime)
  - [x] 2.3 Create Common/Enums/: ConversationType.cs (Private, Group), UserRole.cs (Admin, Member), PresenceStatus.cs (Online, Away, Offline)

- [x] Task 3: Backend Application layer post-scaffold customization (AC: #2)
  - [x] 3.1 Create module folders: Identity/, Messaging/, Presence/, Files/ each with Commands/, Queries/, EventHandlers/ subfolders
  - [x] 3.2 Create Common/Interfaces/ with stub interfaces: IFileStorageService, IMessageSearchService, IAuthenticationProvider, ICacheService, ICurrentUserService
  - [x] 3.3 Verify Common/Behaviors/ has ValidationBehavior and LoggingBehavior (template may provide these)
  - [x] 3.4 Create Common/Exceptions/: NotFoundException.cs, ForbiddenAccessException.cs
  - [x] 3.5 Create Common/Models/PagedResult.cs with Items, HasMore, NextCursor properties

- [x] Task 4: Angular frontend post-scaffold customization (AC: #3)
  - [x] 4.1 Create directory structure: src/app/core/, src/app/shared/components/, src/app/features/
  - [x] 4.2 Create empty feature module folders: auth/, chat/, admin/, search/ under features/
  - [x] 4.3 Create src/app/models/ directory for TypeScript interfaces
  - [x] 4.4 Switch test runner from Karma to Jest
  - [x] 4.5 Create design token file: src/styles/_tokens.scss with all design tokens (match UX spec exactly)
  - [x] 4.6 Create responsive mixins: src/styles/_mixins.scss (includes reduced-motion mixin)
  - [x] 4.7 Create src/styles/styles.scss with token import, minimal CSS reset, and skip-to-content link
  - [x] 4.8 Verify `ng build` completes without errors

- [x] Task 5: Verification (AC: #1, #2, #3)
  - [x] 5.1 Run `dotnet build SimpleChat.sln` — zero errors
  - [x] 5.2 Run `dotnet test SimpleChat.sln` — zero failures (template default tests)
  - [x] 5.3 Run `ng build` from src/web/ — zero errors
  - [x] 5.4 Run Jest default test — passing

## Dev Notes

### Starter Template

Use **Jason Taylor Clean Architecture Solution Template v10.6.1**:

```bash
dotnet new install Clean.Architecture.Solution.Template
dotnet new ca-sln --client-framework Angular --database sqlserver --output SimpleChat
```

This provides: MediatR, FluentValidation, EF Core, OpenAPI + Scalar UI, xUnit test projects, WebApplicationFactory. The template generates the 4-project structure automatically.

**Required runtime:** .NET 10 (LTS) with C# 14, Angular 21 with TypeScript. Verify these versions in `.csproj` and `package.json` after scaffolding.

**Aspire removal:** The template includes .NET Aspire orchestration by default. You MUST remove Aspire projects (`*.AppHost`, `*.ServiceDefaults`) and all Aspire package references from the solution before proceeding. This project uses Docker Compose (Story 1.2), not Aspire.

**Sample code removal:** The template generates a `WeatherForecast` example vertical slice (entity, commands, queries, controller). Remove all of it cleanly — check `Program.cs` for related service registrations.

### Backend Architecture — Dependency Rule

```
Domain ← Application ← Infrastructure ← API
```

All dependencies point inward. Domain has zero dependencies. Application references Domain only. Infrastructure implements Application interfaces.

### Backend Project Structure (Target State)

```
src/SimpleChat.Domain/
├── Common/
│   ├── BaseEntity.cs                 # Id (long), CreatedAt
│   └── Enums/
│       ├── ConversationType.cs       # Private, Group
│       ├── UserRole.cs               # Admin, Member
│       └── PresenceStatus.cs         # Online, Away, Offline
├── Identity/
│   ├── Entities/
│   ├── ValueObjects/
│   └── Events/
├── Messaging/
│   ├── Entities/
│   ├── ValueObjects/
│   └── Events/
├── Presence/
│   ├── Entities/
│   ├── ValueObjects/
│   └── Events/
└── Files/
    ├── Entities/
    ├── ValueObjects/
    └── Events/

src/SimpleChat.Application/
├── DependencyInjection.cs            # MediatR, FluentValidation registration
├── Common/
│   ├── Interfaces/
│   │   ├── IFileStorageService.cs
│   │   ├── IMessageSearchService.cs
│   │   ├── IAuthenticationProvider.cs
│   │   ├── ICacheService.cs
│   │   └── ICurrentUserService.cs
│   ├── Behaviors/
│   │   ├── ValidationBehavior.cs
│   │   └── LoggingBehavior.cs
│   ├── Exceptions/
│   │   ├── NotFoundException.cs
│   │   └── ForbiddenAccessException.cs
│   └── Models/
│       └── PagedResult.cs            # Items, HasMore, NextCursor
├── Identity/
│   ├── Commands/
│   ├── Queries/
│   └── EventHandlers/
├── Messaging/
│   ├── Commands/
│   ├── Queries/
│   └── EventHandlers/
├── Presence/
│   ├── Commands/
│   ├── Queries/
│   └── EventHandlers/
└── Files/
    ├── Commands/
    ├── Queries/
    └── EventHandlers/
```

### Frontend Angular Structure (Target State)

```
src/web/src/app/
├── core/                             # Singleton services, provided in root
├── shared/
│   └── components/                   # Reusable components across features
├── features/
│   ├── auth/                         # Login, Register
│   ├── chat/                         # Chat layout, conversations, messages
│   ├── admin/                        # User management
│   └── search/                       # Search bar, results
├── models/                           # TypeScript interfaces
├── app.component.ts
├── app.config.ts                     # Application configuration (providers)
└── app.routes.ts                     # Route definitions with lazy loading

src/web/src/styles/
├── _tokens.scss                      # All design tokens
├── _mixins.scss                      # Responsive mixins
└── styles.scss                       # Global: import tokens, minimal reset, skip-to-content
```

### Interface Stubs

All interfaces go in `SimpleChat.Application.Common.Interfaces` namespace.

**IFileStorageService.cs:**
```csharp
namespace SimpleChat.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> StoreAsync(Stream stream, string fileName, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    Task<Stream> GetStreamAsync(string path, CancellationToken ct = default);
}
```

**IMessageSearchService.cs:**
```csharp
namespace SimpleChat.Application.Common.Interfaces;

public interface IMessageSearchService
{
    Task<PagedResult<MessageSearchResult>> SearchAsync(long userId, string term, long? cursor, int limit, CancellationToken ct = default);
}
```

**IAuthenticationProvider.cs:**
```csharp
namespace SimpleChat.Application.Common.Interfaces;

public interface IAuthenticationProvider
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
```

**ICacheService.cs:**
```csharp
namespace SimpleChat.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<IReadOnlySet<string>> SetMembersAsync(string key, CancellationToken ct = default);
}
```

**ICurrentUserService.cs:**
```csharp
namespace SimpleChat.Application.Common.Interfaces;

public interface ICurrentUserService
{
    long? UserId { get; }
    bool IsAuthenticated { get; }
}
```

### BaseEntity.cs

```csharp
namespace SimpleChat.Domain.Common;

public abstract class BaseEntity
{
    public long Id { get; private set; }
    public DateTime CreatedAt { get; init; }
}
```

### PagedResult.cs

```csharp
namespace SimpleChat.Application.Common.Models;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public bool HasMore { get; init; }
    public long? NextCursor { get; init; }
}
```

### Design Tokens (_tokens.scss)

These must match the UX design specification exactly. This is the single source of truth for all visual decisions — no hardcoded values in component SCSS.

```scss
// Colors — warm teal palette
$color-primary: #128C7E;
$color-primary-light: #25D366;
$color-primary-dark: #075E54;
$color-error: #DC3545;
$color-warning: #F59E0B;
$color-success: #25D366;

// Surfaces
$color-background: #FFFFFF;
$color-surface: #F0F2F5;
$color-surface-hover: #E4E6EB;
$color-surface-active: #D1D5DB;
$color-divider: #E5E7EB;
$color-chat-bg: #FAFBFC;

// Text
$color-text-primary: #111B21;
$color-text-secondary: #667781;
$color-text-tertiary: #8696A0;
$color-text-inverse: #FFFFFF;

// Chat bubbles
$color-bubble-own: #128C7E;
$color-bubble-own-text: #FFFFFF;
$color-bubble-other: #FFFFFF;
$color-bubble-other-text: #111B21;
$color-bubble-system: #F0F2F5;
$color-unread: #128C7E;

// Typography — system font stack, no web fonts
$font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
$font-size-caption: 12px;
$font-size-body-sm: 13px;
$font-size-body: 14px;
$font-size-body-lg: 15px;
$font-size-heading: 18px;
$font-weight-normal: 400;
$font-weight-medium: 500;
$font-weight-bold: 600;

// Spacing — 4px base unit
$space-xs: 4px;
$space-sm: 8px;
$space-md: 12px;
$space-lg: 16px;
$space-xl: 24px;
$space-2xl: 32px;

// Border radius
$radius-sm: 4px;
$radius-md: 8px;
$radius-lg: 16px;
$radius-full: 9999px;
$avatar-radius: 14px;
$bubble-radius: 16px;
$input-radius: 16px;

// Shadows (values from UX spec)
$shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.05);
$shadow-md: 0 2px 8px rgba(0, 0, 0, 0.08);
$shadow-lg: 0 4px 16px rgba(0, 0, 0, 0.12);

// Transitions (with easing)
$transition-fast: 150ms ease;
$transition-normal: 250ms ease;
$transition-slow: 400ms ease;

// Breakpoints
$breakpoint-mobile: 768px;
$breakpoint-tablet: 1024px;

// Z-index layers
$z-dropdown: 100;
$z-modal-backdrop: 200;
$z-modal: 300;
$z-toast: 400;
$z-connection-status: 500;
```

### Responsive Mixins (_mixins.scss)

```scss
@use 'tokens' as t;

@mixin mobile {
  @media (max-width: #{t.$breakpoint-mobile - 1px}) { @content; }
}

@mixin tablet {
  @media (min-width: #{t.$breakpoint-mobile}) and (max-width: #{t.$breakpoint-tablet - 1px}) { @content; }
}

@mixin desktop {
  @media (min-width: #{t.$breakpoint-tablet}) { @content; }
}

@mixin reduced-motion {
  @media (prefers-reduced-motion: reduce) { @content; }
}
```

### Global Styles (styles.scss)

```scss
@use 'tokens' as t;

// Minimal CSS reset
*,
*::before,
*::after {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

html {
  font-family: t.$font-family;
  font-size: t.$font-size-body;
  color: t.$color-text-primary;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

body {
  background-color: t.$color-background;
  min-height: 100vh;
}

// Skip-to-content link (accessibility baseline)
.skip-to-content {
  position: absolute;
  left: -9999px;
  top: 0;
  z-index: 9999;
  padding: t.$space-sm t.$space-lg;
  background: t.$color-primary;
  color: t.$color-text-inverse;
  font-weight: t.$font-weight-medium;
  text-decoration: none;

  &:focus {
    left: 0;
  }
}

// Focus indicator (global baseline)
:focus-visible {
  outline: 2px solid t.$color-primary;
  outline-offset: 2px;
}
```

The root `AppComponent` template must include: `<a class="skip-to-content" href="#main-content">Skip to content</a>` as the first element.

### Switching Karma to Jest

1. Remove Karma packages: `npm uninstall karma karma-chrome-launcher karma-coverage karma-jasmine karma-jasmine-html-reporter`
2. Remove `karma.conf.js` and any `test.ts` karma bootstrap file
3. Install Jest: `npm install --save-dev jest @types/jest jest-preset-angular @angular-builders/jest`
4. Create `jest.config.ts` at `src/web/jest.config.ts` (see content below)
5. Update `angular.json`: replace the `test` architect builder with `@angular-builders/jest:run`
6. Update `tsconfig.spec.json`: replace `"jasmine"` with `"jest"` in `compilerOptions.types`
7. Verify a default test passes with `ng test`

**jest.config.ts:**
```typescript
import type { Config } from 'jest';

const config: Config = {
  preset: 'jest-preset-angular',
  setupFilesAfterSetup: ['<rootDir>/setup-jest.ts'],
  testPathIgnorePatterns: ['<rootDir>/node_modules/', '<rootDir>/dist/'],
  moduleNameMapper: {
    '^@app/(.*)$': '<rootDir>/src/app/$1',
    '^@environments/(.*)$': '<rootDir>/src/environments/$1',
  },
};

export default config;
```

**setup-jest.ts** (create at `src/web/setup-jest.ts`):
```typescript
import 'jest-preset-angular/setup-jest';
```

### Naming Conventions

| Element | Convention | Example |
|---|---|---|
| C# class/interface | PascalCase | `IFileStorageService`, `BaseEntity` |
| C# private field | _camelCase | `_dbContext` |
| C# public property | PascalCase | `CreatedAt` |
| Enum values | PascalCase | `ConversationType.Private` |
| Folder names | PascalCase (backend) | `Commands/`, `Entities/` |
| Angular files | kebab-case | `auth.service.ts`, `message.model.ts` |
| SCSS tokens | $kebab-case | `$color-primary` |

### Critical Constraints

- **No UI framework** — no Angular Material, PrimeNG, or utility CSS. Custom components only. This is a load-bearing decision tied to <500KB gzipped bundle target (NFR5).
- **@angular/cdk** is the only external Angular UI dependency (needed for BreakpointObserver in ChatLayout).
- **Standalone components** — Angular 21 defaults to standalone components. Use `app.config.ts` with `provideRouter()` and `provideHttpClient()`, not NgModules. Feature modules use lazy-loaded routes, not `NgModule` declarations.
- **No repository pattern** — EF Core `AppDbContext` is injected directly into command/query handlers.
- **ViewEncapsulation.Emulated** (Angular default) for all components.
- Tokens imported in each component SCSS: `@use '../../../styles/tokens' as t;`
- **No hardcoded values** in component SCSS — every color, spacing, radius, shadow, and transition must reference a token.

### What NOT to Do

- Do NOT create entity implementations (User, Conversation, Message) — those are Story 3.1 and Epic 2
- Do NOT create AppDbContext or EF configurations — that's Story 1.3
- Do NOT create controllers or API endpoints — those come in later stories
- Do NOT create Docker Compose files — that's Story 1.2
- Do NOT add Serilog or health checks — that's Story 1.4
- Do NOT create CI pipeline — that's Story 1.5
- Do NOT install @angular/pwa yet — that's Epic 9
- Do NOT create actual Angular components — only the folder structure
- Do NOT modify `Program.cs` beyond what the template provides (startup changes belong to Stories 1.3 and 1.4)
- Do NOT add service registrations in `DependencyInjection.cs` for infrastructure services (file storage, cache, etc.) — those belong to later stories when implementations exist
- Do NOT install Angular Material, PrimeNG, or any UI component library

### Project Structure Notes

- The Jason Taylor template provides the base 4-project solution structure. Post-scaffold work is adding module organization within that structure.
- The template includes .NET Aspire by default — this MUST be removed (we use Docker Compose instead).
- The template includes a WeatherForecast sample vertical slice — remove all sample code cleanly.
- The template includes MediatR and FluentValidation registration in `DependencyInjection.cs` — verify and preserve this.
- The template may include some Common/ infrastructure — inspect and extend rather than replace.
- Tests directory comes from the template with xUnit — keep the structure.
- Create `.editorconfig` at the solution root for .NET code style enforcement (PascalCase public members, _camelCase private fields). The CI pipeline (Story 1.5) will depend on this for lint enforcement.

### References

- [Source: architecture.md — Technical Stack, §4 Code Structure, §6 Domain Model]
- [Source: architecture.md — §7 API Surface, §8 Database Architecture, §11 Testing Strategy]
- [Source: prd.md — FR39-FR45, NFR5, NFR27-NFR33]
- [Source: ux-design-specification.md — Design Tokens, Direction B, Component Architecture]
- [Source: epics/epic-1-project-foundation-deployment-infrastructure.md — Story 1.1 acceptance criteria]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.6 (1M context)

### Debug Log References
- Template v10.8.0 generated .slnx format (not .sln) — adapted all references
- Template used TodoList/TodoItem as primary sample (not WeatherForecast as story mentioned) — removed all sample code
- global.json specified SDK 10.0.201 but 10.0.102 installed — updated to match
- Central package management required nuget.config to clear global sources (Hangfire, Telerik)
- Aspire.Microsoft.EntityFrameworkCore.SqlServer replaced with Microsoft.EntityFrameworkCore.SqlServer
- jest-preset-angular v16 moved setup file from `setup-jest` to `setup-env/zone` — adapted import
- jest.config.ts not recognized by @angular-builders/jest — switched to jest.config.js
- tsconfig.spec.json needed `isolatedModules: true` to work with `bundler` moduleResolution for Angular v21 package exports
- `NotFoundException` conflict between custom and Ardalis.GuardClauses — resolved with explicit using alias
- `setupFilesAfterSetup` typo in story spec — corrected to `setupFilesAfterEnv`

### Completion Notes List
- Scaffolded from Jason Taylor Clean Architecture Solution Template v10.8.0
- .NET 10 (TFM net10.0), Angular 21.1.5, TypeScript 5.9.3
- Removed .NET Aspire orchestration (AppHost, ServiceDefaults, Shared, TestAppHost, .aspire)
- Removed all template sample code (TodoList, TodoItem, WeatherForecast, Colour, Counter, etc.)
- Created Domain module structure: Identity, Messaging, Presence, Files with Entities/ValueObjects/Events subfolders
- Updated BaseEntity: Id (long, private set), CreatedAt (DateTime, init)
- Created enums: ConversationType, UserRole, PresenceStatus
- Created Application module structure: Identity, Messaging, Presence, Files with Commands/Queries/EventHandlers subfolders
- Created stub interfaces: IFileStorageService, IMessageSearchService, IAuthenticationProvider, ICacheService, ICurrentUserService
- Created NotFoundException, verified ForbiddenAccessException exists, verified ValidationBehaviour and LoggingBehaviour exist
- Created PagedResult<T> model with Items, HasMore, NextCursor
- Converted Angular app from NgModules to standalone components (app.config.ts, app.routes.ts)
- Created Angular directory structure: core/, shared/components/, features/{auth,chat,admin,search}/, models/
- Switched from Karma/Jasmine to Jest with jest-preset-angular
- Created design tokens (_tokens.scss), responsive mixins (_mixins.scss), and global styles (styles.scss)
- AppComponent includes skip-to-content accessibility link
- All .NET projects compile: 0 errors, 0 warnings
- dotnet test: 5 passed (RequestLogger + ValidationException tests)
- ng build: successful (240.83 KB initial bundle)

### Review Findings

#### Chunk 1: Backend

- [x] [Review][Defer] #1 CORS AllowAny* not environment-gated [src/Web/Program.cs] — deferred, production CORS belongs to Story 1.2/security hardening
- [x] [Review][Defer] #2 Duplicate creation timestamps: BaseEntity.CreatedAt vs BaseAuditableEntity.Created — deferred, reconcile when entities created in Stories 2.x/3.x
- [x] [Review][Defer] #3 DB initializer calls EnsureDeletedAsync [src/Infrastructure/Data/ApplicationDbContextInitialiser.cs] — deferred, Story 1.3 covers DB initialization
- [x] [Review][Patch] #4 README still references Aspire and TodoList sample code [README.md] — fixed
- [x] [Review][Patch] #5 Web.http contains only sample API requests [src/Web/Web.http] — fixed
- [x] [Review][Patch] #6 proxy.conf.js contains /weatherforecast entries [src/Web/ClientApp/proxy.conf.js] — fixed
- [x] [Review][Patch] #7 IEndpointGroup.cs XML comment references TodoLists [src/Web/Infrastructure/IEndpointGroup.cs:8] — fixed
- [x] [Review][Patch] #8 Application.csproj includes bogus MSBuild packages [src/Application/Application.csproj] — fixed
- [x] [Review][Defer] #9 Hardcoded admin password "Administrator1!" in seeder [src/Infrastructure/Data/ApplicationDbContextInitialiser.cs] — deferred, template default
- [x] [Review][Defer] #10 .GetAwaiter().GetResult() in DispatchDomainEventsInterceptor sync path [src/Infrastructure/Data/Interceptors/DispatchDomainEventsInterceptor.cs] — deferred, template code
- [x] [Review][Defer] #11 Domain events dispatched before SaveChanges commits [src/Infrastructure/Data/Interceptors/DispatchDomainEventsInterceptor.cs] — deferred, template pattern
- [x] [Review][Defer] #12 Domain event handler exception leaves events partially cleared [src/Infrastructure/Data/Interceptors/DispatchDomainEventsInterceptor.cs] — deferred, template code
- [x] [Review][Defer] #13 Role authorization doesn't trim whitespace in comma-separated roles [src/Application/Common/Behaviours/AuthorizationBehaviour.cs] — deferred, template code
- [x] [Review][Defer] #14 Seed user creation result not checked [src/Infrastructure/Data/ApplicationDbContextInitialiser.cs] — deferred, template code
- [x] [Review][Defer] #15 Logout endpoint body check logic inverted [src/Web/Endpoints/Users.cs] — deferred, template code
- [x] [Review][Defer] #16 PerformanceBehaviour Stopwatch not reset between calls [src/Application/Common/Behaviours/PerformanceBehaviour.cs] — deferred, template code
- [x] [Review][Defer] #17 Auditable entity interceptor writes null UserId for system operations [src/Infrastructure/Data/Interceptors/AuditableEntityInterceptor.cs] — deferred, template code
- [x] [Review][Defer] #18 Logging behaviour logs entire request object (PII risk) [src/Application/Common/Behaviours/LoggingBehaviour.cs] — deferred, template code
- [x] [Review][Defer] #19 ExceptionHandler middleware registered after static files middleware [src/Web/Program.cs] — deferred, template code

#### Chunk 2: Frontend

- [x] [Review][Patch] #20 Google Fonts CDN links in index.html violate "no web fonts" constraint [src/Web/ClientApp/src/index.html] — fixed
- [x] [Review][Patch] #21 Title "Clean Architecture" — stale template title [src/Web/ClientApp/src/index.html] — fixed
- [x] [Review][Patch] #22 PicoCSS picoColorScheme script remnant in index.html [src/Web/ClientApp/src/index.html] — fixed
- [x] [Review][Patch] #23 ClientApp README still references Karma [src/Web/ClientApp/README.md] — fixed
- [x] [Review][Patch] #24 Proxy target undefined when Aspire env vars missing — added localhost fallback [src/Web/ClientApp/proxy.conf.js] — fixed
- [x] [Review][Patch] #25 Favicon MIME type mismatch: .png declared as image/svg+xml [src/Web/ClientApp/src/index.html] — fixed
- [x] [Review][Patch] #26 strict: false in tsconfig.json — changed to true [src/Web/ClientApp/tsconfig.json] — fixed
- [x] [Review][Defer] #27 provideHttpClient() missing XSRF/fetch options — deferred, address in Story 2.2 (auth)
- [x] [Review][Defer] #28 Production bundle budget thresholds too permissive (1MB/5MB) — deferred, pre-existing template
- [x] [Review][Defer] #29 No dark-mode token set — deferred, not in scope for Story 1.1
- Jest: 2 tests passed (AppComponent creation + skip-to-content link)

### Change Log
- 2026-03-28: Story 1.1 implementation complete — project scaffolded, Aspire removed, sample code removed, domain/application layers customized, Angular frontend restructured with standalone components and Jest

### File List
- SimpleChat.slnx (modified — removed Aspire projects)
- Directory.Build.props (from template, net10.0 TFM)
- Directory.Packages.props (modified — removed Aspire packages, added EF Core SqlServer)
- global.json (modified — SDK version 10.0.102)
- nuget.config (new — single source nuget.org)
- .editorconfig (from template)
- .gitignore (from template)
- src/Domain/Domain.csproj
- src/Domain/GlobalUsings.cs (modified)
- src/Domain/Common/BaseEntity.cs (modified — Id long, CreatedAt DateTime)
- src/Domain/Common/BaseAuditableEntity.cs (from template)
- src/Domain/Common/BaseEvent.cs (from template)
- src/Domain/Common/ValueObject.cs (from template)
- src/Domain/Common/Enums/ConversationType.cs (new)
- src/Domain/Common/Enums/UserRole.cs (new)
- src/Domain/Common/Enums/PresenceStatus.cs (new)
- src/Domain/Identity/Entities/.gitkeep (new)
- src/Domain/Identity/ValueObjects/.gitkeep (new)
- src/Domain/Identity/Events/.gitkeep (new)
- src/Domain/Messaging/Entities/.gitkeep (new)
- src/Domain/Messaging/ValueObjects/.gitkeep (new)
- src/Domain/Messaging/Events/.gitkeep (new)
- src/Domain/Presence/Entities/.gitkeep (new)
- src/Domain/Presence/ValueObjects/.gitkeep (new)
- src/Domain/Presence/Events/.gitkeep (new)
- src/Domain/Files/Entities/.gitkeep (new)
- src/Domain/Files/ValueObjects/.gitkeep (new)
- src/Domain/Files/Events/.gitkeep (new)
- src/Application/Application.csproj
- src/Application/GlobalUsings.cs (from template)
- src/Application/DependencyInjection.cs (from template)
- src/Application/Common/Interfaces/IApplicationDbContext.cs (modified — removed Todo DbSets)
- src/Application/Common/Interfaces/IIdentityService.cs (from template)
- src/Application/Common/Interfaces/IUser.cs (from template)
- src/Application/Common/Interfaces/IFileStorageService.cs (new)
- src/Application/Common/Interfaces/IMessageSearchService.cs (new)
- src/Application/Common/Interfaces/IAuthenticationProvider.cs (new)
- src/Application/Common/Interfaces/ICacheService.cs (new)
- src/Application/Common/Interfaces/ICurrentUserService.cs (new)
- src/Application/Common/Behaviours/ValidationBehaviour.cs (from template)
- src/Application/Common/Behaviours/LoggingBehaviour.cs (from template)
- src/Application/Common/Behaviours/AuthorizationBehaviour.cs (from template)
- src/Application/Common/Behaviours/PerformanceBehaviour.cs (from template)
- src/Application/Common/Behaviours/UnhandledExceptionBehaviour.cs (from template)
- src/Application/Common/Exceptions/ForbiddenAccessException.cs (from template)
- src/Application/Common/Exceptions/ValidationException.cs (from template)
- src/Application/Common/Exceptions/NotFoundException.cs (new)
- src/Application/Common/Models/Result.cs (from template)
- src/Application/Common/Models/PagedResult.cs (new)
- src/Application/Common/Models/MessageSearchResult.cs (new)
- src/Application/Common/Security/AuthorizeAttribute.cs (from template)
- src/Application/Identity/Commands/.gitkeep (new)
- src/Application/Identity/Queries/.gitkeep (new)
- src/Application/Identity/EventHandlers/.gitkeep (new)
- src/Application/Messaging/Commands/.gitkeep (new)
- src/Application/Messaging/Queries/.gitkeep (new)
- src/Application/Messaging/EventHandlers/.gitkeep (new)
- src/Application/Presence/Commands/.gitkeep (new)
- src/Application/Presence/Queries/.gitkeep (new)
- src/Application/Presence/EventHandlers/.gitkeep (new)
- src/Application/Files/Commands/.gitkeep (new)
- src/Application/Files/Queries/.gitkeep (new)
- src/Application/Files/EventHandlers/.gitkeep (new)
- src/Infrastructure/Infrastructure.csproj (modified — removed Aspire, added EF Core SqlServer)
- src/Infrastructure/GlobalUsings.cs (modified — removed Shared reference)
- src/Infrastructure/DependencyInjection.cs (modified — removed Aspire, fixed connection string)
- src/Infrastructure/Data/ApplicationDbContext.cs (modified — removed Todo DbSets)
- src/Infrastructure/Data/ApplicationDbContextInitialiser.cs (modified — removed sample seed data)
- src/Infrastructure/Data/Interceptors/AuditableEntityInterceptor.cs (from template)
- src/Infrastructure/Data/Interceptors/DispatchDomainEventsInterceptor.cs (from template)
- src/Infrastructure/Identity/ApplicationUser.cs (from template)
- src/Infrastructure/Identity/IdentityService.cs (from template)
- src/Infrastructure/Identity/IdentityResultExtensions.cs (from template)
- src/Web/Web.csproj (modified — removed ServiceDefaults reference)
- src/Web/Program.cs (modified — removed Aspire calls)
- src/Web/DependencyInjection.cs (from template)
- src/Web/appsettings.json (from template)
- src/Web/Services/CurrentUser.cs (from template)
- src/Web/Endpoints/Users.cs (from template)
- src/Web/Infrastructure/IEndpointGroup.cs (from template)
- src/Web/Infrastructure/EndpointRouteBuilderExtensions.cs (from template)
- src/Web/Infrastructure/WebApplicationExtensions.cs (from template)
- src/Web/Infrastructure/MethodInfoExtensions.cs (from template)
- src/Web/Infrastructure/ProblemDetailsExceptionHandler.cs (modified — added NotFoundException alias)
- src/Web/Infrastructure/ApiExceptionOperationTransformer.cs (from template)
- src/Web/Infrastructure/IdentityApiOperationTransformer.cs (from template)
- src/Web/ClientApp/package.json (modified — removed sample deps, switched to Jest)
- src/Web/ClientApp/angular.json (modified — Jest builder, updated styles path)
- src/Web/ClientApp/tsconfig.spec.json (modified — Jest types, isolatedModules)
- src/Web/ClientApp/jest.config.js (new)
- src/Web/ClientApp/setup-jest.ts (new)
- src/Web/ClientApp/src/main.ts (modified — standalone bootstrap)
- src/Web/ClientApp/src/app/app.component.ts (modified — standalone with skip-to-content)
- src/Web/ClientApp/src/app/app.component.spec.ts (new — Jest test)
- src/Web/ClientApp/src/app/app.config.ts (new)
- src/Web/ClientApp/src/app/app.routes.ts (new)
- src/Web/ClientApp/src/app/core/.gitkeep (new)
- src/Web/ClientApp/src/app/shared/components/.gitkeep (new)
- src/Web/ClientApp/src/app/features/auth/.gitkeep (new)
- src/Web/ClientApp/src/app/features/chat/.gitkeep (new)
- src/Web/ClientApp/src/app/features/admin/.gitkeep (new)
- src/Web/ClientApp/src/app/features/search/.gitkeep (new)
- src/Web/ClientApp/src/app/models/.gitkeep (new)
- src/Web/ClientApp/src/styles/_tokens.scss (new)
- src/Web/ClientApp/src/styles/_mixins.scss (new)
- src/Web/ClientApp/src/styles/styles.scss (new)
- tests/Application.UnitTests/Common/Behaviours/RequestLoggerTests.cs (modified — removed sample dependency)
- tests/Application.UnitTests/Common/Exceptions/ValidationExceptionTests.cs (from template)
- tests/Domain.UnitTests/Domain.UnitTests.csproj (from template)
- tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj (from template)
