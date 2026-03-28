---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
status: 'complete'
completedAt: '2026-03-28'
lastStep: 8
inputDocuments:
  - prd.md
  - product-brief-simple-chat.md
  - product-brief-simple-chat-distillate.md
  - implementation-readiness-report-2026-03-27.md
workflowType: 'architecture'
project_name: 'simple-chat'
user_name: 'Nabi'
date: '2026-03-28'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**
49 FRs across 10 capability areas. The core architectural challenge is that messaging (private + group) accounts for 13 FRs but shares underlying infrastructure — message persistence, real-time delivery, file attachments, and history retrieval. This argues for a unified messaging domain with a `Conversation` entity using a type discriminator (Private/Group) and a `ConversationParticipant` join table, rather than separate private/group implementations. Real-time communication (5 FRs) and notifications (3 FRs) are tightly coupled to messaging and share a MediatR notification event bus. Administration (5 FRs) is lightweight user CRUD with role assignment, naturally co-located with Identity. Deployment & operations (7 FRs) shape the deployment architecture — which is a first-class product feature, not an afterthought.

**Non-Functional Requirements:**
33 NFRs drive specific architectural decisions:
- **Performance (NFR1-6):** P99 <200ms message latency → synchronous persist-first flow through SignalR hub, minimize server-side processing in the hot path. 200 concurrent WebSocket connections → single SignalR hub design. <500KB Angular bundle → lazy-loaded feature modules, no heavy UI framework.
- **Security (NFR7-14):** Adaptive password hashing (bcrypt/Argon2), token expiry, file type validation, XSS prevention, no telemetry. Baked into architecture, not bolted on.
- **Scalability (NFR15-18):** <3GB total stack memory for 50 users is the binding constraint. **MSSQL memory must be explicitly capped** (e.g., 512MB) in Docker Compose defaults — SQL Server defaults to consuming 80% of host memory. This is a first-class architectural decision.
- **Reliability (NFR24-27):** Idempotent EF migrations on startup with documented single-instance constraint to prevent concurrent migration execution. Automatic SignalR reconnection with exponential backoff. <30s startup time.
- **Operability (NFR28-33):** Single `docker-compose up` deployment, env-var-only configuration, JSON structured logs to stdout, health endpoints. The Docker Compose file is a product deliverable.

**Scale & Complexity:**

- Primary domain: Full-stack web application (Angular SPA + .NET Core API + SignalR + MSSQL + Redis)
- Complexity level: Medium
- Estimated architectural components: 4 backend modules (Identity, Messaging, Presence, Files), 4 frontend feature modules (Auth, Chat, Admin, Search), plus infrastructure services

### Technical Constraints & Dependencies

- **.NET Core** backend — non-negotiable, it's the product positioning
- **Angular** frontend with Core/Shared/Features architecture
- **MSSQL (SQL Server)** with explicit memory limits in Docker Compose
- **Redis** — presence heartbeat/TTL storage + application caching; future SignalR backplane (Phase 2)
- **SignalR** — single ChatHub for real-time messaging, notifications; presence tracked separately via REST heartbeat
- **Entity Framework Core** — code-first, automatic migrations on startup, DbContext injected directly into CQRS handlers (no repository pattern)
- **MediatR** — in-process CQRS bus for commands/queries + notification fan-out (replaces need for external message broker)
- **FluentValidation** — command validation via MediatR pipeline behaviors
- **Docker Compose** — 3 containers only (app + MSSQL + Redis), no additional infrastructure
- **Single instance for MVP** — no horizontal scaling, no Redis backplane, no distributed locks
- **Local file storage** — Docker volume for uploads, abstracted via `IFileStorageService` for future cloud storage

### Architecture Patterns & Key Decisions

**Modular Monolith + Clean Architecture + CQRS:**
- 4 projects: Domain → Application → Infrastructure → API (dependencies point inward)
- 4 modules: Identity (auth + users + admin), Messaging (conversations + messages), Presence (heartbeat + status), Files (upload + storage + thumbnails)
- Modules are namespace-based folders within each layer, not separate assemblies
- CQRS is logical (separate command/query models in code), not physical (same database)
- Cross-module communication via MediatR, not direct references

**No Repository Pattern:**
- EF Core DbContext injected directly into command/query handlers
- Each handler IS a single-purpose "repository method" — no wrapper tax
- Infrastructure abstractions only at real seams: `IFileStorageService`, `IMessageSearchService`, `IAuthenticationProvider`, `ICacheService`

**Message Flow (Synchronous, Persist-First):**
- User → SignalR Hub → MediatR Command → EF Insert (await) → MediatR Publish `MessageSent` → SignalR broadcast + update `LastMessageAt`
- Guarantees: any message seen in real-time is already durable in the database
- MediatR notifications provide in-process fan-out without external message broker

**Presence Architecture (Redis Heartbeat + TTL):**
- Angular app calls `POST /api/presence/heartbeat` every 60 seconds
- Redis key `presence:{userId}` with 90-second TTL + Redis Set `online_users`
- Angular polls `GET /api/presence/online` every 30 seconds
- Away detection: client-side inactivity (5 min) changes heartbeat payload status
- Offline detection: automatic via Redis TTL expiry with lazy pruning on read

**Message Persistence:**
- `Message.Id` as bigint identity — sequential writes, compact indexes, natural cursor ordering
- Composite index on `(ConversationId, Id DESC)` for cursor-based history pagination
- MSSQL Full-Text Index on `Message.Content` for search (abstracted via `IMessageSearchService`)
- Unread tracking via `LastReadMessageId` on `ConversationParticipant` — no per-message read receipts
- Future-proofing: nullable `EditedAt` and `DeletedAt` columns, unused in V1

**Frontend Architecture (Angular):**
- Core/Shared/Features pattern with lazy-loaded feature modules (Auth, Chat, Admin, Search)
- State management: BehaviorSubject services (not NgRx) — migration path to NgRx exists if needed
- Single `SignalRService` in core dispatches events to domain services
- Responsive `ChatLayoutComponent` using `BreakpointObserver` for desktop/tablet/mobile layouts
- Accessibility: `role="log"` + `aria-live="polite"` on message lists for screen reader support

**Testing Strategy:**
- Backend: xUnit + EF InMemory (unit), Testcontainers MSSQL (integration)
- Frontend: Jest + TestBed (unit, shallow component tests)
- Coverage targets: 100% on command handlers and validators, 80%+ overall
- CI pipeline: parallel unit tests + lint → integration tests → Docker build (~4 min)
- E2E tests deferred to Phase 2 (Playwright)

### Cross-Cutting Concerns Identified

- **Authentication & Authorization:** JWT or cookie sessions (decision pending), role-based access (admin/member), enforced on every API endpoint and SignalR hub method
- **Real-Time Event Distribution:** MediatR `MessageSent` notification fans out to SignalR broadcast handler and conversation timestamp update handler — extensible for future handlers (webhooks, link previews)
- **File Storage Abstraction:** `IFileStorageService` interface in Application layer — local disk implementation in V1, cloud storage swap in future
- **Search Abstraction:** `IMessageSearchService` interface — MSSQL Full-Text in V1, Elasticsearch/Meilisearch swap path preserved
- **Unread State Management:** Read cursors (`LastReadMessageId`) on `ConversationParticipant` — single query for unread counts, no write amplification
- **Connection Lifecycle:** SignalR reconnection with exponential backoff `[0, 2000, 5000, 10000, 30000]`, connection state exposed to UI via `connectionState$` observable
- **Observability:** Structured JSON logging to stdout, health endpoints for app + database connectivity, all modules participate consistently
- **Input Validation:** FluentValidation in MediatR pipeline — validates commands before handlers execute, co-located with command definitions

## Starter Template Evaluation

### Primary Technology Domain

Full-stack web application: .NET 10 backend (ASP.NET Core + SignalR + EF Core + MSSQL) with Angular 21 frontend, based on project requirements analysis.

### Starter Options Considered

| Template | Version | .NET | Frontend | CQRS/MediatR | Database | Active |
|----------|---------|------|----------|-------------|----------|--------|
| Jason Taylor Clean Architecture | 10.6.1 | .NET 10 | Angular/React | Yes | SQL Server/PostgreSQL/SQLite | Yes |
| Ardalis Clean Architecture | 11.0.0 | .NET 10 | None (API only) | Yes (FastEndpoints) | Configurable | Yes |
| Manual scaffold | N/A | .NET 10 | Angular 21 CLI | Manual setup | Manual setup | N/A |

### Selected Starter: Jason Taylor's Clean Architecture Solution Template

**Rationale for Selection:**
- Directly provides our chosen architecture: Clean Architecture + CQRS + MediatR + FluentValidation + EF Core
- Angular frontend scaffolding included with ASP.NET Core proxy integration
- SQL Server support built-in with EF Core database initialization
- Vertical slice feature organization matches our handler-per-file approach
- Most popular .NET Clean Architecture template (~17K GitHub stars) — well-documented, community-supported
- Active maintenance on .NET 10, production-proven

**Initialization Command:**

```bash
# Install template
dotnet new install Clean.Architecture.Solution.Template

# Create solution with Angular + SQL Server
dotnet new ca-sln --client-framework Angular --database sqlserver --output SimpleChat
```

**Architectural Decisions Provided by Starter:**

**Language & Runtime:**
- .NET 10 (LTS) with C# 14
- Angular 21 with TypeScript
- ASP.NET Core Minimal API

**Build Tooling:**
- .NET SDK build pipeline
- Angular CLI for frontend
- OpenAPI spec generation at build time

**Testing Framework:**
- xUnit for backend unit and integration tests
- Integration test infrastructure with WebApplicationFactory

**Code Organization:**
- 4-project structure: Domain, Application, Infrastructure, Web (API)
- Vertical slice feature folders with command/query + validator + handler
- MediatR pipeline behaviors for cross-cutting concerns (validation, logging)

**Development Experience:**
- Scalar UI for API exploration at `/scalar`
- Angular dev server with proxy to ASP.NET Core
- EF Core migrations with database initialization on startup

**Customizations Required Post-Scaffold:**
- Add SignalR ChatHub and real-time infrastructure
- Add Redis configuration for presence (heartbeat + TTL)
- Replace Aspire with Docker Compose (3 containers: app + MSSQL + Redis)
- Organize features into module folders (Identity, Messaging, Presence, Files)
- Add `IFileStorageService`, `IMessageSearchService`, `IAuthenticationProvider`, `ICacheService` interfaces
- Configure MSSQL memory limits in Docker Compose defaults
- Switch frontend tests from Karma to Jest
- Add Angular Core/Shared/Features folder structure

**Note:** Project initialization using this command should be the first implementation story.

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (Block Implementation):**
- Authentication: JWT with Redis session validation, refresh token rotation, HttpOnly cookie storage
- Data model: Unified Conversation entity with type discriminator
- Message persistence: Bigint identity, cursor-based pagination
- Real-time: Single SignalR ChatHub, persist-first message flow
- Presence: Redis heartbeat + TTL (60s heartbeat, 90s TTL)
- Search: MSSQL Full-Text Index via `IMessageSearchService` abstraction
- Deployment: Docker Compose (3 containers), MSSQL memory capped

**Important Decisions (Shape Architecture):**
- No repository pattern — DbContext in CQRS handlers
- MediatR in-process notification fan-out (no message broker)
- BehaviorSubject state management in Angular (not NgRx)
- Serilog structured JSON logging
- bcrypt password hashing
- SkiaSharp for image thumbnail generation
- Problem Details (RFC 9457) error responses
- Plain text messages with Angular template escaping (no HtmlSanitizer)
- Three-tier health checks (startup/liveness/readiness)

**Deferred Decisions (Post-MVP):**
- Multi-instance scaling (Redis SignalR backplane — Phase 2)
- LDAP/OAuth authentication providers (Phase 2)
- Cloud file storage backend (Phase 2)
- Elasticsearch/Meilisearch search replacement (Phase 2+)
- NgRx state management migration (if needed)
- E2E testing framework selection (Playwright — Phase 2)
- Markdown/rich text message support (Phase 2+)

### Data Architecture

| Decision | Choice | Version | Rationale |
|----------|--------|---------|-----------|
| Database | MSSQL (SQL Server) | Latest Docker image | Native .NET ecosystem, EF Core first-class support |
| ORM | Entity Framework Core | 10.x (via starter) | Code-first, auto-migration, mature SQL Server provider |
| Message ID type | bigint identity | — | Sequential writes, compact indexes, natural cursor ordering |
| Primary index | Clustered on `Id` | — | Append-only write pattern, optimal for sequential inserts |
| History index | `(ConversationId, Id DESC)` | — | Cursor-based pagination: `WHERE ConversationId = @id AND Id < @cursor` |
| Full-text search | MSSQL Full-Text Index | — | Built-in, no additional infrastructure, handles 1M+ messages |
| Search abstraction | `IMessageSearchService` | — | Swap path to Elasticsearch/Meilisearch without rewriting messaging module |
| Caching | Redis | Latest Docker image | Presence storage, session validation, application cache |
| MSSQL memory | Capped at ~512MB via Docker Compose env | — | Prevent SQL Server from consuming entire host memory (3GB budget) |
| Migration strategy | EF `MigrateAsync()` on startup | — | Zero-ops upgrade: `docker-compose pull && up`. Single-instance constraint documented |
| Unread tracking | Read cursors (`LastReadMessageId` per user per conversation) | — | Single row update on conversation focus, efficient count query |
| Future-proofing | Nullable `EditedAt`, `DeletedAt` on Message | — | Zero cost now, avoids schema redesign for Phase 2 edit/delete |

### Authentication & Security

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Authentication | JWT with Redis session validation | Stateless transport + instant revocation via Redis session delete (NFR13) |
| Access token | Short-lived JWT (15-30 min), stored in Angular service memory | In-memory storage eliminates XSS token theft risk |
| Refresh token | HttpOnly, Secure, SameSite=Strict cookie, 7-day expiry | Browser manages cookie automatically, not accessible to JS |
| Refresh rotation | New refresh token issued on each use, old token invalidated | OWASP-recommended; reuse detection triggers emergency lockout (delete all user sessions) |
| Refresh flow | Angular interceptor queues concurrent 401s, calls `/api/auth/refresh`, retries original requests | Prevents thundering herd on token expiry |
| SignalR auth | JWT via query string; reconnection handler refreshes token before re-establishing hub | Ensures reconnection gets valid token |
| Session store | Redis with TTL matching refresh token expiry | Already in stack for presence; session delete = immediate lockout |
| Password hashing | bcrypt (`BCrypt.Net-Next`) | Battle-tested, excellent .NET support, satisfies NFR7 |
| XSS prevention | Plain text messages + Angular template escaping | Messages are text, not HTML. Angular's default escaping handles rendering. No HtmlSanitizer needed — eliminates XSS by design, not by sanitization. Raw content preserved in DB |
| File validation | Whitelist MIME types + magic byte validation + reject executables | NFR10 compliance, defense in depth |
| API auth enforcement | All endpoints require auth except register, login, health | NFR12 compliance |
| Admin authorization | Role-based: `[Authorize(Roles = "Admin")]` | Simple, explicit, fits 2-role model (admin/member) |
| Deactivation enforcement | Delete Redis session on deactivate → immediate lockout | NFR13: deactivated users immediately unable to access |
| Rate limiting | Configurable per environment variable; per-IP AND per-username on auth endpoints | Avoids shared-IP lockout behind NAT/proxy; per-username protects individual accounts |
| Rate limit defaults | `RateLimit__LoginPerMinutePerIp=20`, `RateLimit__LoginPerMinutePerUser=5` | Generous IP limit for shared networks, strict per-user for credential stuffing protection |
| CORS policy | Same-origin only for V1 — no CORS headers | Angular served from same .NET host in production. Explicitly documented to prevent wildcard CORS additions |

### API & Communication Patterns

| Decision | Choice | Rationale |
|----------|--------|-----------|
| API style | REST (Minimal API) + SignalR | REST for CRUD, SignalR for real-time. No GraphQL needed |
| API documentation | OpenAPI + Scalar UI | Provided by starter, auto-generated at build time |
| Error format | Problem Details (RFC 9457) | ASP.NET Core built-in, FluentValidation maps automatically |
| Message flow | SignalR Hub → MediatR Command → EF Insert → MediatR Publish → SignalR Broadcast | Persist-first guarantees durability before real-time delivery |
| In-process events | MediatR `INotification` fan-out | Replaces message broker, extensible for future handlers |
| SignalR hub topology | Single `ChatHub` | One connection per client, handles messages + notifications |
| Presence API | REST heartbeat + polling (separate from SignalR) | Decoupled, simpler, tolerant of flaky WebSocket connections |

### Frontend Architecture

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Framework | Angular 21 with TypeScript | PRD requirement, starter provides scaffolding |
| Architecture | Core / Shared / Features pattern | Standard scalable Angular structure |
| State management | BehaviorSubject services + `async` pipe | Sufficient for 4 state slices, NgRx migration path preserved |
| Feature loading | Lazy-loaded modules per route | Keeps initial bundle under 500KB gzipped (NFR5) |
| CSS | Component-scoped SCSS + shared design tokens | Full control, small bundle, no framework dependency |
| Responsive layout | `ChatLayoutComponent` + `BreakpointObserver` | Desktop (3-panel), tablet (2-panel), mobile (single-panel) |
| Real-time | Single `SignalRService` in core, dispatches to domain services | Centralized connection lifecycle, clean event routing |
| Accessibility | `role="log"` + `aria-live="polite"` on message lists | NFR22: screen reader announces incoming messages, not history loads |
| Testing | Jest (replacing Karma) + shallow TestBed components | Faster test runs, better DX |
| UI library | None — custom components | Bundle size control, chat-specific UX needs |
| Session expiry UX | Non-disruptive overlay on expired session, not hard redirect to login | User re-authenticates in-place, preserving scroll position and conversation context |
| Connection status display | 3s debounce before showing "Reconnecting..." indicator | Prevents anxiety-inducing flicker on brief reconnections; disconnected state shows immediately |

### Infrastructure & Deployment

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Containerization | Docker Compose: 3 containers (app + MSSQL + Redis) | Product positioning: `docker-compose up` to chatting in 15 min |
| Image thumbnails | SkiaSharp | Cross-platform .NET, works in Docker Linux containers |
| Logging | Serilog with JSON to stdout | De facto .NET structured logging, satisfies NFR30 |
| Health checks | Three-tier: `/health/startup` (init complete), `/health/live` (process alive), `/health/ready` (dependencies up) | Proper Docker orchestration — prevents restart loops during slow MSSQL startup |
| Docker healthcheck | Target `/health/startup` initially, `/health/ready` ongoing | Startup probe waits for migrations; readiness checks MSSQL + Redis connectivity |
| CI/CD | GitHub Actions | PR: unit + lint → integration → Docker build. Main: + push to ghcr.io |
| Configuration | Environment variables override `appsettings.json` defaults | NFR29: no in-container file editing |
| Docker registry | GitHub Container Registry (ghcr.io) | Free for public repos, integrated with GitHub Actions |
| Backup strategy | Database volume + file upload volume | NFR32: Redis is ephemeral, rebuilds on restart |
| Integration testing | Testcontainers for both MSSQL and Redis | Tests JWT session validation flow, presence TTL, and database queries against real infrastructure |

### Decision Impact Analysis

**Implementation Sequence:**
1. Scaffold project from Jason Taylor template (starter)
2. Configure Docker Compose (MSSQL + Redis + app with memory limits, 3-tier health checks)
3. Set up Identity module (registration, login, JWT + Redis sessions, refresh token rotation)
4. Set up Messaging module (Conversation model, message persistence, cursor pagination)
5. Add SignalR ChatHub (real-time delivery, MediatR integration, JWT auth)
6. Add Presence module (Redis heartbeat + TTL, online users API)
7. Add Files module (upload, SkiaSharp thumbnails, storage)
8. Add Search (MSSQL Full-Text Index, `IMessageSearchService`)
9. Build Angular frontend (Core/Shared/Features, responsive layout, auth interceptor with refresh flow)
10. Add Admin features (user list, role assignment, deactivation with immediate session invalidation)

**Cross-Component Dependencies:**
- Identity must be implemented first — all other modules depend on authenticated user context
- Redis configuration is shared between Identity (sessions), Presence (heartbeat), and infrastructure (health checks)
- Messaging module depends on Identity (sender/participant validation) and Files (attachments)
- SignalR hub depends on Identity (JWT auth) and Messaging (message commands)
- Search depends on Messaging (full-text index on Messages table)
- Frontend auth interceptor depends on the refresh token endpoint being available
- Docker Compose health checks depend on all three health endpoints being implemented

## Implementation Patterns & Consistency Rules

### Pattern Categories Defined

**Critical Conflict Points Identified:** 25+ areas where AI agents could make different choices across naming, structure, format, communication, and process patterns.

### Naming Patterns

**Database Naming Conventions:**

| Element | Convention | Example |
|---------|-----------|---------|
| Tables | PascalCase, plural | `Messages`, `Conversations`, `Users` |
| Columns | PascalCase | `ConversationId`, `SentAt`, `LastReadMessageId` |
| Primary keys | `Id` (always) | `Message.Id`, `User.Id` |
| Foreign keys | `{Entity}Id` | `SenderId`, `ConversationId` |
| Indexes | `IX_{Table}_{Columns}` | `IX_Messages_ConversationId_Id` |
| Unique constraints | `UQ_{Table}_{Column}` | `UQ_Users_Email` |

EF Core configuration handles mapping — entity properties are PascalCase C# conventions, EF maps to matching column names.

**API Naming Conventions:**

| Element | Convention | Example |
|---------|-----------|---------|
| Endpoints | Lowercase, plural, kebab-case for multi-word | `/api/conversations`, `/api/online-users` |
| Route parameters | camelCase in curly braces | `/api/conversations/{conversationId}/messages` |
| Query parameters | camelCase | `?before=123&limit=50` |
| API prefix | Always `/api/` for REST, `/hubs/` for SignalR | `/api/auth/login`, `/hubs/chat` |

**Backend Code Naming (.NET):**

| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `SendMessageCommand`, `MessageService` |
| Interfaces | `I` prefix + PascalCase | `IFileStorageService`, `ICacheService` |
| Public properties | PascalCase | `ConversationId`, `SentAt` |
| Private fields | `_camelCase` | `_db`, `_mediator`, `_logger` |
| Methods | PascalCase | `Handle()`, `SendMessageAsync()` |
| Async methods | `Async` suffix | `StoreAsync()`, `SearchAsync()` |
| Constants | PascalCase | `MaxFileSize`, `HeartbeatIntervalSeconds` |
| Commands/Queries | `{Verb}{Noun}Command/Query` | `SendMessageCommand`, `GetMessageHistoryQuery` |
| Handlers | `{Command/Query}Handler` | `SendMessageCommandHandler` |
| Validators | `{Command}Validator` | `SendMessageCommandValidator` |
| DTOs | `{Noun}Dto` or `{Noun}Response` | `MessageDto`, `ConversationResponse` |
| Domain events | `{Noun}{PastTenseVerb}` | `MessageSent`, `UserDeactivated` |

**Frontend Code Naming (Angular/TypeScript):**

| Element | Convention | Example |
|---------|-----------|---------|
| Components | kebab-case files, PascalCase classes | `message-bubble.component.ts`, `MessageBubbleComponent` |
| Services | kebab-case files, PascalCase classes | `message.service.ts`, `MessageService` |
| Interfaces/Models | PascalCase, no `I` prefix | `Message`, `Conversation`, `User` |
| Properties | camelCase | `conversationId`, `sentAt` |
| Observables | `$` suffix | `conversations$`, `connectionState$` |
| Event handlers | `on{Event}` | `onScrollToTop()`, `onSendMessage()` |
| Directives | `app` prefix | `appInfiniteScroll` |
| Pipes | camelCase | `relativeTime`, `fileSize` |
| Feature modules | kebab-case folder | `features/chat/`, `features/admin/` |

### Structure Patterns

**Backend Project Organization:**

```
src/
├── SimpleChat.Domain/
│   ├── {Module}/              # Identity, Messaging, Presence, Files
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   └── Events/            # Domain events (MediatR INotification)
│   └── Common/                # Shared base classes, enums
│
├── SimpleChat.Application/
│   ├── {Module}/
│   │   ├── Commands/
│   │   │   └── {CommandName}/
│   │   │       ├── {CommandName}Command.cs
│   │   │       ├── {CommandName}CommandHandler.cs
│   │   │       └── {CommandName}CommandValidator.cs
│   │   ├── Queries/
│   │   │   └── {QueryName}/
│   │   │       ├── {QueryName}Query.cs
│   │   │       ├── {QueryName}QueryHandler.cs
│   │   │       └── {QueryName}Dto.cs
│   │   └── EventHandlers/
│   ├── Common/
│   │   ├── Interfaces/        # IFileStorageService, etc.
│   │   ├── Behaviors/         # MediatR pipeline behaviors
│   │   └── Exceptions/
│   └── DependencyInjection.cs
│
├── SimpleChat.Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/    # EF entity configurations
│   │   └── Migrations/
│   ├── Services/
│   │   ├── LocalFileStorageService.cs
│   │   ├── SqlFullTextSearchService.cs
│   │   ├── RedisCacheService.cs
│   │   └── BcryptAuthenticationProvider.cs
│   └── DependencyInjection.cs
│
└── SimpleChat.API/
    ├── Controllers/
    ├── Hubs/
    │   └── ChatHub.cs
    ├── Middleware/
    └── Program.cs
```

**One command/query per folder.** Each command or query gets its own folder containing the request, handler, validator (if command), and DTO (if query).

**Frontend Organization:**

```
src/app/
├── core/           # Singleton services — imported once in AppModule
├── shared/         # Reusable components — imported by feature modules
├── features/       # Lazy-loaded route-bound modules
├── models/         # TypeScript interfaces matching backend DTOs
├── app.component.ts
├── app.routes.ts
└── app.module.ts
```

**Test co-location:** Angular tests (`.spec.ts`) co-located with source files. Backend tests in separate projects (`SimpleChat.UnitTests`, `SimpleChat.IntegrationTests`) mirroring source structure.

### Format Patterns

**API Response Formats:**

Successful responses return data directly — **no wrapper object.** Paginated responses include `hasMore` and cursor fields. Errors always use Problem Details (RFC 9457).

**Data Exchange Formats:**

| Element | Convention | Example |
|---------|-----------|---------|
| JSON field names | camelCase | `conversationId`, `sentAt`, `hasMore` |
| Dates | ISO 8601 UTC strings | `"2026-03-28T10:30:00Z"` |
| Booleans | `true`/`false` | `"hasMore": true` |
| Nulls | Omit optional fields when null | — |
| IDs | Numbers (matching bigint) | `"id": 42`, not `"id": "42"` |
| Enums in API | String values | `"type": "Group"`, `"status": "Online"` |
| Pagination | Cursor-based with `before`/`after` + `limit` + `hasMore` | Never offset-based |

### Communication Patterns

**MediatR Event Naming:**

| Type | Convention | Example |
|------|-----------|---------|
| Commands | `{Verb}{Noun}Command` | `SendMessageCommand`, `CreateConversationCommand` |
| Queries | `Get{Noun}Query` or `{Verb}{Noun}Query` | `GetMessageHistoryQuery`, `SearchMessagesQuery` |
| Domain events | `{Noun}{PastTenseVerb}` | `MessageSent`, `UserDeactivated`, `ConversationCreated` |

**Event payload rule:** Domain events carry the full entity or DTO — handlers should not need to re-query the database.

**SignalR Event Naming:**

| Direction | Convention | Example |
|-----------|-----------|---------|
| Server → Client | `{Verb}{Noun}` PascalCase | `ReceiveMessage`, `UserOnline`, `UserOffline` |
| Client → Server | Hub method PascalCase | `SendMessage`, `JoinConversation` |

**Angular State Management Rules:**
- BehaviorSubject services are the single source of truth
- State updates are immutable: `[...existing, newItem]`, not `existing.push()`
- Components subscribe via `async` pipe — never manual `.subscribe()` in components
- Services expose `Observable` via `.asObservable()` — never expose BehaviorSubject directly

### Process Patterns

**Error Handling:**

Backend:
- FluentValidation errors → 400 Problem Details (automatic via MediatR pipeline)
- `NotFoundException` → 404 Problem Details
- `UnauthorizedAccessException` → 403 Problem Details
- Unhandled exceptions → 500 Problem Details (generic message, full details in Serilog)
- Never expose stack traces or internal details in API responses

Frontend:
- HTTP errors caught in services, not components
- 401 → auth interceptor handles refresh flow silently
- 400 (validation) → service returns error details, component displays inline
- 500 → global `NotificationService` → toast/snackbar display
- SignalR errors → `SignalRService` handles reconnection, exposes `connectionState$`

**Logging Standards:**

| Level | Usage | Example |
|-------|-------|---------|
| `Debug` | Detailed flow for development | `"Loading messages for conversation {ConversationId}"` |
| `Information` | Business events | `"User {UserId} sent message in conversation {ConversationId}"` |
| `Warning` | Recoverable issues | `"File upload rejected: {FileName} exceeds {MaxSize}MB"` |
| `Error` | Failures requiring attention | `"Failed to deliver message via SignalR: {Error}"` |
| `Fatal` | Application cannot continue | `"Database migration failed: {Error}"` |

Always use Serilog message templates with named parameters — never string interpolation.

**Loading States:**
- Each feature service manages its own loading state
- Components bind to loading state via `async` pipe
- Loading indicators are local to the requesting component (no global spinner)
- Skeleton screens for initial loads; spinners for actions (send, upload)

### Enforcement Guidelines

**All AI Agents MUST:**

1. Follow naming conventions exactly as specified — no exceptions, no "improvements"
2. Place files in the correct module/folder per the structure patterns
3. Use Problem Details for all error responses
4. Use cursor-based pagination (never offset-based)
5. Use MediatR for all command/query dispatch — never call services directly from controllers/hubs
6. Use Serilog structured logging with message templates — never string interpolation
7. Keep command/query handlers under 50 lines — extract domain services if logic grows
8. Use `async` pipe in Angular templates — never manual `.subscribe()` in components
9. Use immutable state updates in BehaviorSubject services
10. Co-locate Angular tests with source files; backend tests mirror source structure in test projects

**Pattern Verification:**
- PR reviews check naming conventions, file placement, and error response format
- Linting rules enforce code naming (ESLint for Angular, .editorconfig for .NET)
- CI pipeline runs both backend and frontend linters before tests

### Pattern Examples

**Good — Backend command structure:**

```csharp
// Application/Messaging/Commands/SendMessage/SendMessageCommand.cs
public record SendMessageCommand(long ConversationId, long SenderId, string Content) : IRequest<long>;

// Application/Messaging/Commands/SendMessage/SendMessageCommandHandler.cs
public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, long>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;
    public async Task<long> Handle(SendMessageCommand request, CancellationToken ct) { /* ... */ }
}

// Application/Messaging/Commands/SendMessage/SendMessageCommandValidator.cs
public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.ConversationId).GreaterThan(0);
    }
}
```

**Good — Angular service pattern:**

```typescript
@Injectable({ providedIn: 'root' })
export class ConversationService {
  private conversations$ = new BehaviorSubject<Conversation[]>([]);
  getConversations(): Observable<Conversation[]> {
    return this.conversations$.asObservable();
  }
  loadConversations(): void {
    this.http.get<Conversation[]>('/api/conversations')
      .subscribe(convs => this.conversations$.next(convs));
  }
}
```

**Anti-Patterns (NEVER do these):**

```csharp
// WRONG: Repository wrapper around DbContext
public class MessageRepository : IMessageRepository { /* just wraps DbContext */ }

// WRONG: String interpolation in logs
_logger.LogInformation($"User {userId} logged in"); // Loses structured data

// WRONG: Offset pagination
app.MapGet("/api/messages", (int page, int pageSize) => ...);
```

```typescript
// WRONG: Manual subscribe in component (memory leak)
ngOnInit() { this.service.getData().subscribe(d => this.data = d); }

// WRONG: Mutable state update
this.messages$.value.push(msg); // Mutates existing array

// WRONG: Exposing BehaviorSubject publicly
public conversations$ = new BehaviorSubject<Conversation[]>([]); // External .next() possible
```

## Project Structure & Boundaries

### Complete Project Directory Structure

```
SimpleChat/
├── .github/
│   └── workflows/
│       ├── ci.yml                          # PR: unit + lint → integration → Docker build
│       └── release.yml                     # Main: + push to ghcr.io
├── .editorconfig                           # .NET naming/formatting enforcement
├── .gitignore
├── docker-compose.yml                      # Production: app + MSSQL (512MB cap) + Redis
├── docker-compose.override.yml             # Development overrides (ports, volumes)
├── Dockerfile                              # Multi-stage: build → publish → runtime
├── SimpleChat.sln
│
├── src/
│   ├── SimpleChat.Domain/
│   │   ├── SimpleChat.Domain.csproj
│   │   ├── Common/
│   │   │   ├── BaseEntity.cs               # Id (long), CreatedAt, shared base
│   │   │   └── Enums/
│   │   │       ├── ConversationType.cs      # Private, Group
│   │   │       ├── UserRole.cs              # Admin, Member
│   │   │       └── PresenceStatus.cs        # Online, Away, Offline
│   │   ├── Identity/
│   │   │   └── Entities/
│   │   │       └── User.cs                  # Id, Email, DisplayName, PasswordHash, Role, IsActive
│   │   ├── Messaging/
│   │   │   ├── Entities/
│   │   │   │   ├── Conversation.cs          # Id, Type, Name, CreatedAt, CreatedById, LastMessageAt
│   │   │   │   ├── ConversationParticipant.cs  # ConversationId, UserId, JoinedAt, LastReadMessageId
│   │   │   │   └── Message.cs              # Id, ConversationId, SenderId, Content, SentAt, MessageType, FileId, EditedAt?, DeletedAt?
│   │   │   └── Events/
│   │   │       ├── MessageSent.cs           # INotification — carries full Message + ConversationId
│   │   │       └── ConversationCreated.cs
│   │   ├── Presence/
│   │   │   └── Events/
│   │   │       └── UserStatusChanged.cs
│   │   └── Files/
│   │       └── Entities/
│   │           └── FileAttachment.cs        # Id, FileName, ContentType, Size, StoragePath, ThumbnailPath, UploadedAt, UploadedById
│   │
│   ├── SimpleChat.Application/
│   │   ├── SimpleChat.Application.csproj
│   │   ├── DependencyInjection.cs           # MediatR, FluentValidation, pipeline behaviors
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IFileStorageService.cs   # StoreAsync, DeleteAsync, GetStreamAsync
│   │   │   │   ├── IMessageSearchService.cs # SearchAsync(userId, term, cursor, limit)
│   │   │   │   ├── IAuthenticationProvider.cs # HashPassword, VerifyPassword
│   │   │   │   └── ICacheService.cs         # GetAsync, SetAsync, DeleteAsync, SetMembersAsync
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs    # FluentValidation pipeline behavior
│   │   │   │   └── LoggingBehavior.cs       # Serilog request/response logging
│   │   │   ├── Exceptions/
│   │   │   │   ├── NotFoundException.cs
│   │   │   │   └── ForbiddenAccessException.cs
│   │   │   └── Models/
│   │   │       └── PagedResult.cs           # Items, HasMore, NextCursor
│   │   ├── Identity/
│   │   │   ├── Commands/
│   │   │   │   ├── Register/
│   │   │   │   │   ├── RegisterCommand.cs
│   │   │   │   │   ├── RegisterCommandHandler.cs
│   │   │   │   │   └── RegisterCommandValidator.cs
│   │   │   │   ├── Login/
│   │   │   │   │   ├── LoginCommand.cs
│   │   │   │   │   ├── LoginCommandHandler.cs
│   │   │   │   │   └── LoginCommandValidator.cs
│   │   │   │   ├── RefreshToken/
│   │   │   │   │   ├── RefreshTokenCommand.cs
│   │   │   │   │   └── RefreshTokenCommandHandler.cs
│   │   │   │   ├── Logout/
│   │   │   │   │   ├── LogoutCommand.cs
│   │   │   │   │   └── LogoutCommandHandler.cs
│   │   │   │   ├── DeactivateUser/
│   │   │   │   │   ├── DeactivateUserCommand.cs
│   │   │   │   │   └── DeactivateUserCommandHandler.cs
│   │   │   │   ├── ReactivateUser/
│   │   │   │   │   ├── ReactivateUserCommand.cs
│   │   │   │   │   └── ReactivateUserCommandHandler.cs
│   │   │   │   └── AssignRole/
│   │   │   │       ├── AssignRoleCommand.cs
│   │   │   │       └── AssignRoleCommandHandler.cs
│   │   │   └── Queries/
│   │   │       ├── GetUserProfile/
│   │   │       │   ├── GetUserProfileQuery.cs
│   │   │       │   ├── GetUserProfileQueryHandler.cs
│   │   │       │   └── UserProfileDto.cs
│   │   │       └── ListUsers/
│   │   │           ├── ListUsersQuery.cs
│   │   │           ├── ListUsersQueryHandler.cs
│   │   │           └── UserListDto.cs
│   │   ├── Messaging/
│   │   │   ├── Commands/
│   │   │   │   ├── SendMessage/
│   │   │   │   │   ├── SendMessageCommand.cs
│   │   │   │   │   ├── SendMessageCommandHandler.cs
│   │   │   │   │   └── SendMessageCommandValidator.cs
│   │   │   │   ├── CreateConversation/
│   │   │   │   │   ├── CreateConversationCommand.cs
│   │   │   │   │   ├── CreateConversationCommandHandler.cs
│   │   │   │   │   └── CreateConversationCommandValidator.cs
│   │   │   │   ├── JoinConversation/
│   │   │   │   │   ├── JoinConversationCommand.cs
│   │   │   │   │   └── JoinConversationCommandHandler.cs
│   │   │   │   ├── LeaveConversation/
│   │   │   │   │   ├── LeaveConversationCommand.cs
│   │   │   │   │   └── LeaveConversationCommandHandler.cs
│   │   │   │   └── MarkAsRead/
│   │   │   │       ├── MarkAsReadCommand.cs
│   │   │   │       └── MarkAsReadCommandHandler.cs
│   │   │   ├── Queries/
│   │   │   │   ├── GetMessageHistory/
│   │   │   │   │   ├── GetMessageHistoryQuery.cs
│   │   │   │   │   ├── GetMessageHistoryQueryHandler.cs
│   │   │   │   │   └── MessageDto.cs
│   │   │   │   ├── GetMessagesAround/
│   │   │   │   │   ├── GetMessagesAroundQuery.cs
│   │   │   │   │   └── GetMessagesAroundQueryHandler.cs
│   │   │   │   ├── GetConversations/
│   │   │   │   │   ├── GetConversationsQuery.cs
│   │   │   │   │   ├── GetConversationsQueryHandler.cs
│   │   │   │   │   └── ConversationDto.cs
│   │   │   │   ├── GetConversationMembers/
│   │   │   │   │   ├── GetConversationMembersQuery.cs
│   │   │   │   │   └── GetConversationMembersQueryHandler.cs
│   │   │   │   ├── SearchMessages/
│   │   │   │   │   ├── SearchMessagesQuery.cs
│   │   │   │   │   ├── SearchMessagesQueryHandler.cs
│   │   │   │   │   └── SearchResultDto.cs
│   │   │   │   └── BrowseGroups/
│   │   │   │       ├── BrowseGroupsQuery.cs
│   │   │   │       ├── BrowseGroupsQueryHandler.cs
│   │   │   │       └── GroupDto.cs
│   │   │   └── EventHandlers/
│   │   │       ├── MessageSentBroadcastHandler.cs    # SignalR broadcast
│   │   │       └── MessageSentTimestampHandler.cs    # Update LastMessageAt
│   │   ├── Presence/
│   │   │   ├── Commands/
│   │   │   │   └── Heartbeat/
│   │   │   │       ├── HeartbeatCommand.cs
│   │   │   │       └── HeartbeatCommandHandler.cs
│   │   │   └── Queries/
│   │   │       └── GetOnlineUsers/
│   │   │           ├── GetOnlineUsersQuery.cs
│   │   │           ├── GetOnlineUsersQueryHandler.cs
│   │   │           └── OnlineUserDto.cs
│   │   └── Files/
│   │       ├── Commands/
│   │       │   └── UploadFile/
│   │       │       ├── UploadFileCommand.cs
│   │       │       ├── UploadFileCommandHandler.cs
│   │       │       └── UploadFileCommandValidator.cs
│   │       └── Queries/
│   │           └── GetFile/
│   │               ├── GetFileQuery.cs
│   │               └── GetFileQueryHandler.cs
│   │
│   ├── SimpleChat.Infrastructure/
│   │   ├── SimpleChat.Infrastructure.csproj
│   │   ├── DependencyInjection.cs
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── UserConfiguration.cs
│   │   │   │   ├── ConversationConfiguration.cs
│   │   │   │   ├── ConversationParticipantConfiguration.cs
│   │   │   │   ├── MessageConfiguration.cs          # Includes IX_Messages_ConversationId_Id, Full-Text Index
│   │   │   │   └── FileAttachmentConfiguration.cs
│   │   │   └── Migrations/
│   │   ├── Services/
│   │   │   ├── LocalFileStorageService.cs
│   │   │   ├── SqlFullTextSearchService.cs
│   │   │   ├── BcryptAuthenticationProvider.cs
│   │   │   ├── RedisCacheService.cs
│   │   │   └── RedisSessionService.cs
│   │   └── ImageProcessing/
│   │       └── SkiaSharpThumbnailService.cs
│   │
│   └── SimpleChat.API/
│       ├── SimpleChat.API.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── ConversationsController.cs
│       │   ├── MessagesController.cs
│       │   ├── PresenceController.cs
│       │   ├── FilesController.cs
│       │   └── AdminController.cs
│       ├── Hubs/
│       │   └── ChatHub.cs
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       └── HealthChecks/
│           ├── StartupHealthCheck.cs
│           ├── ReadinessHealthCheck.cs
│           └── LivenessHealthCheck.cs
│
├── src/web/                                         # Angular frontend
│   ├── angular.json
│   ├── package.json
│   ├── tsconfig.json
│   ├── jest.config.ts
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/
│   │   │   │   ├── auth/
│   │   │   │   │   ├── auth.service.ts
│   │   │   │   │   ├── auth.service.spec.ts
│   │   │   │   │   ├── auth.guard.ts
│   │   │   │   │   └── auth.interceptor.ts
│   │   │   │   ├── signalr/
│   │   │   │   │   ├── signalr.service.ts
│   │   │   │   │   ├── signalr.service.spec.ts
│   │   │   │   │   └── signalr.events.ts
│   │   │   │   ├── presence/
│   │   │   │   │   ├── presence.service.ts
│   │   │   │   │   └── presence.service.spec.ts
│   │   │   │   ├── notification/
│   │   │   │   │   ├── notification.service.ts
│   │   │   │   │   └── notification.service.spec.ts
│   │   │   │   └── core.module.ts
│   │   │   ├── shared/
│   │   │   │   ├── components/
│   │   │   │   │   ├── avatar/
│   │   │   │   │   ├── presence-indicator/
│   │   │   │   │   ├── file-preview/
│   │   │   │   │   ├── connection-status/
│   │   │   │   │   └── unread-badge/
│   │   │   │   ├── pipes/
│   │   │   │   │   ├── relative-time.pipe.ts
│   │   │   │   │   └── file-size.pipe.ts
│   │   │   │   ├── directives/
│   │   │   │   │   └── infinite-scroll.directive.ts
│   │   │   │   └── shared.module.ts
│   │   │   ├── features/
│   │   │   │   ├── auth/
│   │   │   │   │   ├── components/
│   │   │   │   │   │   ├── login/
│   │   │   │   │   │   └── register/
│   │   │   │   │   ├── auth.routes.ts
│   │   │   │   │   └── auth.module.ts
│   │   │   │   ├── chat/
│   │   │   │   │   ├── components/
│   │   │   │   │   │   ├── chat-layout/
│   │   │   │   │   │   ├── conversation-list/
│   │   │   │   │   │   ├── chat-window/
│   │   │   │   │   │   ├── message-bubble/
│   │   │   │   │   │   ├── message-input/
│   │   │   │   │   │   └── online-users/
│   │   │   │   │   ├── services/
│   │   │   │   │   │   ├── message.service.ts
│   │   │   │   │   │   ├── message.service.spec.ts
│   │   │   │   │   │   ├── conversation.service.ts
│   │   │   │   │   │   └── conversation.service.spec.ts
│   │   │   │   │   ├── chat.routes.ts
│   │   │   │   │   └── chat.module.ts
│   │   │   │   ├── admin/
│   │   │   │   │   ├── components/
│   │   │   │   │   │   ├── user-list/
│   │   │   │   │   │   ├── user-detail/
│   │   │   │   │   │   └── group-management/
│   │   │   │   │   ├── admin.routes.ts
│   │   │   │   │   └── admin.module.ts
│   │   │   │   └── search/
│   │   │   │       ├── components/
│   │   │   │       │   ├── search-bar/
│   │   │   │       │   └── search-results/
│   │   │   │       ├── search.routes.ts
│   │   │   │       └── search.module.ts
│   │   │   ├── models/
│   │   │   │   ├── message.model.ts
│   │   │   │   ├── conversation.model.ts
│   │   │   │   ├── user.model.ts
│   │   │   │   └── presence.model.ts
│   │   │   ├── app.component.ts
│   │   │   ├── app.routes.ts
│   │   │   └── app.module.ts
│   │   ├── styles/
│   │   │   ├── _variables.scss
│   │   │   ├── _mixins.scss
│   │   │   └── styles.scss
│   │   ├── assets/
│   │   │   └── icons/
│   │   ├── manifest.webmanifest
│   │   └── service-worker.js
│   └── proxy.conf.json
│
├── tests/
│   ├── SimpleChat.UnitTests/
│   │   ├── SimpleChat.UnitTests.csproj
│   │   ├── Application/
│   │   │   ├── Identity/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── RegisterCommandHandlerTests.cs
│   │   │   │   │   ├── LoginCommandHandlerTests.cs
│   │   │   │   │   └── RegisterCommandValidatorTests.cs
│   │   │   │   └── Queries/
│   │   │   │       └── ListUsersQueryHandlerTests.cs
│   │   │   ├── Messaging/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── SendMessageCommandHandlerTests.cs
│   │   │   │   │   ├── SendMessageCommandValidatorTests.cs
│   │   │   │   │   └── CreateConversationCommandHandlerTests.cs
│   │   │   │   └── Queries/
│   │   │   │       ├── GetMessageHistoryQueryHandlerTests.cs
│   │   │   │       └── SearchMessagesQueryHandlerTests.cs
│   │   │   ├── Presence/
│   │   │   │   └── Commands/
│   │   │   │       └── HeartbeatCommandHandlerTests.cs
│   │   │   └── Files/
│   │   │       └── Commands/
│   │   │           └── UploadFileCommandHandlerTests.cs
│   │   └── Domain/
│   │       ├── Messaging/
│   │       │   ├── ConversationTests.cs
│   │       │   └── MessageTests.cs
│   │       └── Identity/
│   │           └── UserTests.cs
│   │
│   └── SimpleChat.IntegrationTests/
│       ├── SimpleChat.IntegrationTests.csproj
│       ├── Infrastructure/
│       │   ├── TestDbContextFactory.cs
│       │   └── TestRedisFixture.cs
│       ├── Persistence/
│       │   ├── MessagePersistenceTests.cs
│       │   ├── ConversationPersistenceTests.cs
│       │   └── MigrationTests.cs
│       ├── Api/
│       │   ├── AuthEndpointTests.cs
│       │   ├── MessageEndpointTests.cs
│       │   ├── ConversationEndpointTests.cs
│       │   └── AdminEndpointTests.cs
│       ├── SignalR/
│       │   └── ChatHubTests.cs
│       └── Sessions/
│           └── SessionValidationTests.cs
│
└── docs/
    └── README.md
```

### Architectural Boundaries

**API Boundaries:**

| Boundary | Endpoints | Auth Required | Notes |
|----------|-----------|---------------|-------|
| Auth | `POST /api/auth/register`, `login`, `refresh`, `logout` | No (register, login), Yes (logout) | Refresh via HttpOnly cookie |
| Conversations | `GET/POST /api/conversations`, `GET /{id}/messages`, `GET /{id}/members` | Yes | Cursor pagination on messages |
| Messages | `GET /api/messages/search` | Yes | Full-text search across user's conversations |
| Presence | `POST /api/presence/heartbeat`, `GET /api/presence/online` | Yes | Redis-backed, 60s/30s polling |
| Files | `POST /api/files/upload`, `GET /api/files/{id}` | Yes | SkiaSharp thumbnails on upload |
| Admin | `GET /api/admin/users`, `PUT /{id}/role`, `POST /{id}/deactivate`, `POST /{id}/reactivate` | Yes (Admin role) | Deactivation deletes Redis session |
| Health | `GET /health/startup`, `/health/live`, `/health/ready` | No | Docker orchestration probes |
| SignalR | `/hubs/chat` | Yes (JWT query string) | Single hub: SendMessage, JoinConversation |

**Module Boundaries (Backend):**

| Module | Owns | Communicates Via | Never Directly References |
|--------|------|------------------|--------------------------|
| Identity | Users, Roles, Sessions, Auth tokens | MediatR commands/queries | Messaging, Presence, Files |
| Messaging | Conversations, Messages, Participants, Unread state | MediatR + publishes `MessageSent` | Identity, Files (except FK) |
| Presence | Online status (Redis only, no DB entities) | MediatR, Redis directly | Identity, Messaging |
| Files | FileAttachments, Storage, Thumbnails | MediatR, `IFileStorageService` | Identity, Messaging |

**Data Boundaries:**

| Store | Owns | Persistence | Backup Required |
|-------|------|-------------|-----------------|
| MSSQL | Users, Conversations, Messages, Participants, FileAttachments | Durable, volume-mounted | Yes |
| Redis | Presence keys, Session data, Refresh tokens, Application cache | Ephemeral, rebuilds on restart | No |
| Local Disk | Uploaded files, Generated thumbnails | Volume-mounted | Yes |

### Requirements to Structure Mapping

| FR Category | FRs | Backend Module | Frontend Feature | Key Files |
|-------------|-----|---------------|-----------------|-----------|
| Registration & Auth | FR1-FR5 | Identity | features/auth | AuthController, Register/Login commands, auth.service.ts |
| Private Messaging | FR6-FR10 | Messaging | features/chat | ConversationsController, SendMessage command, message.service.ts |
| Group Chat | FR11-FR18 | Messaging | features/chat | ConversationsController, CreateConversation/Join/Leave commands |
| Real-Time | FR19-FR23 | Messaging + Presence | core/signalr | ChatHub, signalr.service.ts, connection-status component |
| Notifications | FR24-FR26 | Messaging | features/chat | MarkAsRead command, unread-badge component |
| File Sharing | FR27-FR30 | Files | features/chat | FilesController, UploadFile command, file-preview component |
| Search | FR31-FR33 | Messaging | features/search | MessagesController, SearchMessages query, search-results component |
| Administration | FR34-FR38 | Identity | features/admin | AdminController, DeactivateUser/AssignRole commands |
| Deployment & Ops | FR39-FR45 | Infrastructure | — | docker-compose.yml, Program.cs, HealthChecks/ |
| Cross-Cutting | FR46-FR49 | All | All | ChatLayoutComponent, manifest.webmanifest, signalr.service.ts |

### Data Flow

**Message Send Flow (end-to-end):**

```
Angular MessageInput → SignalR Hub.SendMessage()
  → ChatHub validates caller, creates SendMessageCommand
    → MediatR pipeline: ValidationBehavior → LoggingBehavior → Handler
      → SendMessageCommandHandler:
        1. Verify sender is conversation participant (DbContext)
        2. Insert Message into MSSQL (await SaveChangesAsync)
        3. Publish MessageSent notification (MediatR)
          → MessageSentBroadcastHandler: SignalR → Group(conversationId) → ReceiveMessage
          → MessageSentTimestampHandler: Update Conversation.LastMessageAt
        4. Return MessageId to caller

Angular SignalRService.on('ReceiveMessage') → messageService.addIncoming()
  → BehaviorSubject update → async pipe → chat-window re-renders
```

**Authentication Flow:**

```
Login: Angular → POST /api/auth/login
  → LoginCommandHandler: verify credentials (bcrypt)
    → Generate JWT (15-30 min) + Refresh token (7 day)
    → Store session in Redis (TTL = 7 days)
    → Return JWT in body, refresh token as HttpOnly cookie

Token Refresh: Angular auth.interceptor detects 401
  → Queue pending requests
  → POST /api/auth/refresh (cookie sent automatically)
    → Validate refresh token in Redis, rotate token
    → Return new JWT, set new cookie
  → Replay queued requests with new JWT

Deactivation: Admin → POST /api/admin/users/{id}/deactivate
  → Set User.IsActive = false
  → Delete ALL Redis sessions for user → immediate lockout
```

**Presence Flow:**

```
Angular presenceService 60s interval → POST /api/presence/heartbeat
  → SET presence:{userId} {status, displayName} EX 90 (Redis)
  → SADD online_users {userId} (Redis Set)

Angular presenceService 30s poll → GET /api/presence/online
  → SMEMBERS online_users → prune expired → Return OnlineUserDto[]

Away: Angular detects 5min inactivity → heartbeat with status:"Away"
Offline: Redis TTL expires after 90s → removed from Set on next read
```

### Development Workflow Integration

**Local Development:**
- `docker-compose -f docker-compose.yml -f docker-compose.override.yml up` starts MSSQL + Redis
- `dotnet run --project src/SimpleChat.API` starts backend with hot reload
- `cd src/web && ng serve --proxy-config proxy.conf.json` starts Angular with API proxy
- EF migrations run automatically on backend startup

**Build Process:**
- `dotnet build SimpleChat.sln` compiles all backend projects
- `cd src/web && ng build --configuration production` produces optimized Angular bundle
- Dockerfile multi-stage: restore → build → publish backend + frontend → runtime image

**Deployment:**
- `docker-compose up -d` on target host
- Dockerfile copies Angular dist into .NET wwwroot (same-origin serving)
- Environment variables in docker-compose.yml override all config
- Upgrade: `docker-compose pull && docker-compose up -d` — migrations run on startup

## Architecture Validation Results

### Coherence Validation ✅

**Decision Compatibility:** All technology choices are compatible — .NET 10 + EF Core 10 + MSSQL + Redis + SignalR (first-party Microsoft ecosystem), Angular 21 + TypeScript + SignalR JS client, Jason Taylor template v10.6.1 on .NET 10 with MediatR + FluentValidation + EF Core + Angular. bcrypt (BCrypt.Net-Next) and SkiaSharp have active .NET 10 support. Serilog + ASP.NET Core health checks are standard ecosystem libraries.

**Pattern Consistency:** CQRS naming (`{Verb}{Noun}Command/Query`) consistent throughout. Backend PascalCase / Frontend camelCase follows .NET + Angular conventions. API endpoints (lowercase, plural) align with REST standards. Database PascalCase aligns with EF Core defaults. Problem Details error format used consistently for all error types.

**Structure Alignment:** 4-project Clean Architecture maps to dependency rules. 4 modules map to distinct domain boundaries. Frontend Core/Shared/Features aligns with lazy loading. Test structure mirrors source structure. No contradictions found.

### Requirements Coverage Validation ✅

**Functional Requirements: 49/49 covered**

| FR Category | FRs | Status | Architectural Support |
|-------------|-----|--------|----------------------|
| Registration & Auth | FR1-FR5 | ✅ | Identity module, JWT + Redis sessions, first-user admin |
| Private Messaging | FR6-FR10 | ✅ | Messaging module, Conversation(Type=Private), SignalR delivery |
| Group Chat | FR11-FR18 | ✅ | Messaging module, Conversation(Type=Group), BrowseGroups query |
| Real-Time | FR19-FR23 | ✅ | SignalR ChatHub, Presence module (heartbeat + Redis), connection-status |
| Notifications | FR24-FR26 | ✅ | MarkAsRead command, LastReadMessageId cursor, unread-badge |
| File Sharing | FR27-FR30 | ✅ | Files module, IFileStorageService, UploadFileCommandValidator |
| Search | FR31-FR33 | ✅ | MSSQL Full-Text, SearchMessages, GetMessagesAround (context jump) |
| Administration | FR34-FR38 | ✅ | Identity module, admin endpoints, Deactivate/Reactivate/AssignRole |
| Deployment & Ops | FR39-FR45 | ✅ | Docker Compose, env vars, EF auto-migration, health checks, Serilog |
| Cross-Cutting | FR46-FR49 | ✅ | Responsive ChatLayout, PWA manifest, keyboard nav, SignalR reconnection |

**Non-Functional Requirements: 33/33 covered**

| NFR Category | NFRs | Status | Architectural Support |
|-------------|------|--------|----------------------|
| Performance | NFR1-6 | ✅ | Persist-first flow (<200ms), Full-Text Index (<1s), lazy loading (<500KB), single hub (200 connections) |
| Security | NFR7-14 | ✅ | bcrypt, TLS, JWT expiry+rotation, file validation, plain text+Angular escape, auth enforcement, Redis session delete, no telemetry |
| Scalability | NFR15-18 | ✅ | MSSQL 512MB cap, single instance 200 users, bigint sequential inserts, single file volume |
| Accessibility | NFR19-23 | ✅ | role="log"+aria-live, keyboard nav, design tokens for contrast, focus management |
| Reliability | NFR24-27 | ✅ | Volume-mounted MSSQL, SignalR exponential backoff, idempotent migrations, StartupHealthCheck |
| Operability | NFR28-33 | ✅ | docker-compose up, env var config, Serilog JSON stdout, 3-tier health checks, volume backup, multi-stage Dockerfile |

### Implementation Readiness Validation ✅

**Decision Completeness:** All critical technology choices documented with versions. Authentication flow fully specified. Data model entities defined. API endpoints catalogued. Four infrastructure abstractions defined.

**Structure Completeness:** 120+ files explicitly mapped. Every command, query, handler, validator, and DTO has a specific location. Backend and frontend test files mirror source structure.

**Pattern Completeness:** Naming conventions cover all areas with examples. Good examples AND anti-patterns provided. 10 enforcement rules defined. Error handling, logging, and loading state patterns fully specified.

### Gap Analysis Results

**Critical Gaps: None.**

**Important Gaps (non-blocking):**
1. **UX Design document** — Architecture covers accessibility requirements, but detailed UX specs would strengthen frontend implementation. Known gap from Implementation Readiness Report.
2. **PWA caching strategy** — Angular's default `@angular/service-worker` with default caching sufficient for V1. Detailed rules refined during implementation.
3. **Message content length limit** — 4000 characters used in validator example. Should be configurable via environment variable (`Message__MaxLength=4000`).

**Nice-to-Have Gaps:**
- No Mermaid architecture diagrams (all info captured in text/tables)
- No API versioning strategy (not needed for V1)

### Architecture Completeness Checklist

**✅ Requirements Analysis**
- [x] Project context thoroughly analyzed (49 FRs, 33 NFRs categorized)
- [x] Scale and complexity assessed (Medium, 5-200 users, single instance)
- [x] Technical constraints identified (.NET ecosystem, 3GB memory budget, Docker Compose)
- [x] Cross-cutting concerns mapped (auth, real-time, files, unread tracking, observability, validation)

**✅ Architectural Decisions**
- [x] Critical decisions documented with versions (23 decisions across 5 categories)
- [x] Technology stack fully specified (.NET 10, Angular 21, EF Core 10, MSSQL, Redis, SignalR)
- [x] Integration patterns defined (MediatR, SignalR events, REST APIs)
- [x] Performance considerations addressed (MSSQL memory cap, cursor pagination, full-text search, lazy loading)
- [x] Security architecture complete (JWT + Redis sessions, refresh rotation, bcrypt, file validation)

**✅ Implementation Patterns**
- [x] Naming conventions established (database, API, backend, frontend — all with examples)
- [x] Structure patterns defined (one command per folder, Core/Shared/Features)
- [x] Communication patterns specified (MediatR events, SignalR events, state management rules)
- [x] Process patterns documented (error handling, logging levels, loading states)
- [x] Enforcement guidelines defined (10 rules + verification approach)

**✅ Project Structure**
- [x] Complete directory structure defined (120+ files across backend, frontend, tests)
- [x] Component boundaries established (4 backend modules, 4 frontend features)
- [x] Integration points mapped (API boundaries, module communication, data flow diagrams)
- [x] Requirements to structure mapping complete (all 49 FRs → specific files/directories)

### Architecture Readiness Assessment

**Overall Status: READY FOR IMPLEMENTATION**

**Confidence Level:** High

**Key Strengths:**
- Complete FR and NFR coverage — every requirement has architectural support
- Consistent patterns across backend and frontend — AI agents have clear rules
- Pragmatic decisions — no over-engineering, each abstraction justified by real need
- Clear data flows for critical paths (message send, auth, presence)
- Extensibility paths documented for Phase 2 (LDAP, cloud storage, Elasticsearch, multi-instance)

**Areas for Future Enhancement:**
- UX Design document for detailed frontend specifications
- Mermaid architecture diagrams for visual documentation
- API versioning strategy if external consumers added post-MVP
- Load testing plan to validate NFR1 and NFR6 targets

### Implementation Handoff

**AI Agent Guidelines:**
- Follow all architectural decisions exactly as documented
- Use implementation patterns consistently across all components
- Respect project structure and boundaries — every file has a designated location
- Refer to this document for all architectural questions
- When in doubt, follow the anti-pattern examples to know what NOT to do

**First Implementation Priority:**
```bash
dotnet new install Clean.Architecture.Solution.Template
dotnet new ca-sln --client-framework Angular --database sqlserver --output SimpleChat
```
Then configure Docker Compose with MSSQL memory limits, add Redis container, and implement Identity module first.

