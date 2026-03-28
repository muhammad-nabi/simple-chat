# Story 1.1: Project Scaffold & Solution Structure

Status: ready-for-dev

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

- [ ] Task 1: Scaffold from Jason Taylor Clean Architecture template (AC: #1)
  - [ ] 1.1 Install template: `dotnet new install Clean.Architecture.Solution.Template`
  - [ ] 1.2 Generate solution: `dotnet new ca-sln --client-framework Angular --database sqlserver --output SimpleChat`
  - [ ] 1.3 Verify all four projects compile: `dotnet build SimpleChat.sln`

- [ ] Task 2: Backend Domain layer post-scaffold customization (AC: #2)
  - [ ] 2.1 Create module folders: Identity/, Messaging/, Presence/, Files/ each with Entities/ and Events/ subfolders
  - [ ] 2.2 Create Common/BaseEntity.cs with `Id` (long) and `CreatedAt` (DateTime)
  - [ ] 2.3 Create Common/Enums/: ConversationType.cs (Private, Group), UserRole.cs (Admin, Member), PresenceStatus.cs (Online, Away, Offline)

- [ ] Task 3: Backend Application layer post-scaffold customization (AC: #2)
  - [ ] 3.1 Create module folders: Identity/, Messaging/, Presence/, Files/ each with Commands/, Queries/, EventHandlers/ subfolders
  - [ ] 3.2 Create Common/Interfaces/ with stub interfaces: IFileStorageService, IMessageSearchService, IAuthenticationProvider, ICacheService
  - [ ] 3.3 Verify Common/Behaviors/ has ValidationBehavior and LoggingBehavior (template may provide these)
  - [ ] 3.4 Create Common/Exceptions/: NotFoundException.cs, ForbiddenAccessException.cs
  - [ ] 3.5 Create Common/Models/PagedResult.cs with Items, HasMore, NextCursor properties

- [ ] Task 4: Angular frontend post-scaffold customization (AC: #3)
  - [ ] 4.1 Create directory structure: src/app/core/, src/app/shared/components/, src/app/features/
  - [ ] 4.2 Create empty feature module folders: auth/, chat/, admin/, search/ under features/
  - [ ] 4.3 Create src/app/models/ directory for TypeScript interfaces
  - [ ] 4.4 Switch test runner from Karma to Jest
  - [ ] 4.5 Create design token file: src/styles/_tokens.scss with all design tokens
  - [ ] 4.6 Create responsive mixins: src/styles/_mixins.scss
  - [ ] 4.7 Verify `ng build` completes without errors

- [ ] Task 5: Verification (AC: #1, #2, #3)
  - [ ] 5.1 Run `dotnet build SimpleChat.sln` — zero errors
  - [ ] 5.2 Run `ng build` from src/web/ — zero errors
  - [ ] 5.3 Run Jest default test — passing

## Dev Notes

### Starter Template

Use **Jason Taylor Clean Architecture Solution Template v10.6.1**:

```bash
dotnet new install Clean.Architecture.Solution.Template
dotnet new ca-sln --client-framework Angular --database sqlserver --output SimpleChat
```

This provides: MediatR, FluentValidation, EF Core, OpenAPI + Scalar UI, xUnit test projects, WebApplicationFactory. The template generates the 4-project structure automatically.

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
│   └── Events/
├── Messaging/
│   ├── Entities/
│   └── Events/
├── Presence/
│   └── Events/
└── Files/
    └── Entities/

src/SimpleChat.Application/
├── DependencyInjection.cs            # MediatR, FluentValidation registration
├── Common/
│   ├── Interfaces/
│   │   ├── IFileStorageService.cs
│   │   ├── IMessageSearchService.cs
│   │   ├── IAuthenticationProvider.cs
│   │   └── ICacheService.cs
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
├── core/                             # Singleton services, imported once in AppModule
├── shared/
│   └── components/                   # Reusable components across features
├── features/
│   ├── auth/                         # Login, Register
│   ├── chat/                         # Chat layout, conversations, messages
│   ├── admin/                        # User management
│   └── search/                       # Search bar, results
├── models/                           # TypeScript interfaces
├── app.component.ts
├── app.routes.ts
└── app.module.ts

src/web/src/styles/
├── _tokens.scss                      # All design tokens
├── _mixins.scss                      # Responsive mixins
└── styles.scss                       # Global: import tokens, minimal reset, skip-to-content
```

### Interface Stubs

**IFileStorageService.cs:**
```csharp
public interface IFileStorageService
{
    Task<string> StoreAsync(Stream stream, string fileName, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    Task<Stream> GetStreamAsync(string path, CancellationToken ct = default);
}
```

**IMessageSearchService.cs:**
```csharp
public interface IMessageSearchService
{
    Task<PagedResult<MessageSearchResult>> SearchAsync(long userId, string term, long? cursor, int limit, CancellationToken ct = default);
}
```

**IAuthenticationProvider.cs:**
```csharp
public interface IAuthenticationProvider
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
```

**ICacheService.cs:**
```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<IReadOnlySet<string>> SetMembersAsync(string key, CancellationToken ct = default);
}
```

### BaseEntity.cs

```csharp
public abstract class BaseEntity
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### PagedResult.cs

```csharp
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public bool HasMore { get; init; }
    public long? NextCursor { get; init; }
}
```

### Design Tokens (_tokens.scss)

```scss
// Colors — warm teal palette
$color-primary: #128C7E;
$color-primary-light: #25D366;
$color-primary-dark: #075E54;
$color-error: #DC3545;
$color-warning: #F59E0B;

// Surfaces & text
$color-text-primary: #111B21;
$color-text-secondary: #667781;
$color-chat-bg: #FAFBFC;
$color-bubble-own: #128C7E;
$color-bubble-other: #FFFFFF;

// Typography — system font stack, no web fonts
$font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
$font-size-caption: 12px;
$font-size-body-sm: 13px;
$font-size-body: 14px;
$font-size-body-lg: 15px;
$font-size-heading: 18px;

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

// Shadows
$shadow-sm: 0 1px 3px rgba(0, 0, 0, 0.08);
$shadow-md: 0 2px 8px rgba(0, 0, 0, 0.1);
$shadow-lg: 0 4px 16px rgba(0, 0, 0, 0.12);

// Transitions
$transition-fast: 150ms;
$transition-normal: 250ms;
$transition-slow: 400ms;

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
```

### Switching Karma to Jest

1. Remove Karma packages: `karma`, `karma-chrome-launcher`, `karma-coverage`, `karma-jasmine`, `karma-jasmine-html-reporter`
2. Remove `karma.conf.js`
3. Install Jest: `npm install --save-dev jest @types/jest jest-preset-angular`
4. Create `jest.config.ts` at `src/web/jest.config.ts`
5. Update `angular.json` to use `@angular-builders/jest:run` for the test architect
6. Update `tsconfig.spec.json` to use Jest types instead of Jasmine
7. Verify a default test passes

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
- **No repository pattern** — EF Core `AppDbContext` is injected directly into command/query handlers.
- **ViewEncapsulation.Emulated** (Angular default) for all components.
- Tokens imported in each component SCSS: `@use '../../../styles/tokens' as t;`

### What NOT to Do

- Do NOT create entity implementations (User, Conversation, Message) — those are Story 3.1 and Epic 2
- Do NOT create AppDbContext or EF configurations — that's Story 1.3
- Do NOT create controllers or API endpoints — those come in later stories
- Do NOT create Docker Compose files — that's Story 1.2
- Do NOT add Serilog or health checks — that's Story 1.4
- Do NOT create CI pipeline — that's Story 1.5
- Do NOT install @angular/pwa yet — that's Epic 9
- Do NOT create actual Angular components — only the folder structure and module shells

### Project Structure Notes

- The Jason Taylor template provides the base 4-project solution structure. Post-scaffold work is adding module organization within that structure.
- The template includes MediatR and FluentValidation registration in `DependencyInjection.cs` — verify and preserve this.
- The template may include some Common/ infrastructure — inspect and extend rather than replace.
- Tests directory comes from the template with xUnit — keep the structure.

### References

- [Source: architecture.md — Technical Stack, §4 Code Structure, §6 Domain Model]
- [Source: architecture.md — §7 API Surface, §8 Database Architecture, §11 Testing Strategy]
- [Source: prd.md — FR39-FR45, NFR5, NFR27-NFR33]
- [Source: ux-design-specification.md — Design Tokens, Direction B, Component Architecture]
- [Source: epics/epic-1-project-foundation-deployment-infrastructure.md — Story 1.1 acceptance criteria]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
