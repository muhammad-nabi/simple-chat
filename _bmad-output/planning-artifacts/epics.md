---
stepsCompleted: [step-01-validate-prerequisites, step-02-design-epics, step-03-create-stories, step-04-final-validation]
status: 'complete'
completedAt: '2026-03-28'
inputDocuments:
  - prd.md
  - architecture.md
  - ux-design-specification.md
---

# simple-chat - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for simple-chat, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: New users can register an account with email, display name, and password
FR2: Registered users can log in with email and password
FR3: Authenticated users can log out from any device
FR4: The system can maintain authenticated sessions across browser refreshes without requiring re-login
FR5: The first user to register can be designated as the instance administrator
FR6: Users can initiate a 1-on-1 private conversation with any other registered user
FR7: Users can send text messages in a private conversation
FR8: Users can view the full history of a private conversation by scrolling
FR9: Users can share files and images within a private conversation
FR10: Users can see when a private message has been delivered in real time
FR11: Users can create a new group conversation with a name
FR12: Users can invite other users to a group conversation
FR13: Users can join existing group conversations that are visible to them
FR14: Users can leave a group conversation
FR15: Users can send text messages in a group conversation
FR16: Users can view the full history of a group conversation by scrolling
FR17: Users can share files and images within a group conversation
FR18: Users can browse a list of available group conversations
FR19: Users can receive messages in real time without refreshing the page
FR20: Users can see other users' online/away/offline presence status
FR21: Users can set their own presence status (online, away)
FR22: The system can automatically update a user's presence status based on activity/inactivity
FR23: Users can see a visual indicator of their connection state (connected, reconnecting, disconnected)
FR24: Users can see unread message counts per conversation
FR25: Users can receive visual notification indicators for new messages in non-active conversations
FR26: Users can distinguish between conversations with unread messages and those without
FR27: Users can upload files to a conversation
FR28: Users can upload images to a conversation with in-line preview display
FR29: Users can download files shared in a conversation
FR30: The system can enforce a configurable maximum file size limit on uploads
FR31: Users can search across all their conversations by message content
FR32: Users can navigate to the original context of a search result
FR33: Users can see which conversation a search result belongs to
FR34: Administrators can view a list of all registered users
FR35: Administrators can assign roles to users (admin, member)
FR36: Administrators can deactivate a user account
FR37: Administrators can reactivate a previously deactivated user account
FR38: Administrators can create default group conversations during initial setup
FR39: Operators can deploy the application using a single Docker Compose file
FR40: Operators can configure the application via environment variables
FR41: The system can run database migrations automatically on application startup
FR42: Operators can upgrade the application by pulling the new image and restarting without manual migration steps
FR43: The system can expose health check endpoints for monitoring
FR44: The system can produce structured logs compatible with standard .NET logging infrastructure
FR45: Operators can back up all application data by backing up the database volume and file upload volume
FR46: Users can access all features through a responsive web interface across desktop, tablet, and mobile browsers
FR47: Users can install the application as a PWA from their browser
FR48: Users can navigate all core workflows using keyboard only
FR49: The system can automatically reconnect after temporary connection loss without user intervention

### NonFunctional Requirements

NFR1: Message send-to-display latency must be under 200ms at P99 for up to 200 concurrent connections
NFR2: Full-text search must return results within 1 second for message histories up to 1 million messages
NFR3: Initial page load (cold) must complete within 5 seconds; cached loads within 2 seconds
NFR4: Time to interactive must be under 3 seconds on modern hardware with broadband connection
NFR5: Angular production bundle size (gzipped) must be under 500KB
NFR6: Application must support 200 concurrent WebSocket connections on a single instance without degradation
NFR7: All passwords must be hashed using a modern adaptive algorithm (e.g., bcrypt, Argon2) — never stored in plaintext
NFR8: All communication between client and server must use TLS (HTTPS/WSS)
NFR9: Authentication tokens must expire and be non-replayable
NFR10: File uploads must be validated for type and size; executable files must be rejected
NFR11: All user inputs must be sanitized to prevent XSS and injection attacks
NFR12: API endpoints must enforce authentication — no unauthenticated access to user data or messages
NFR13: Deactivated users must be immediately unable to access the system or send messages
NFR14: The application must not transmit telemetry, analytics, or any data to external services
NFR15: Total stack memory footprint (app + MSSQL + Redis) must remain under 3GB for deployments up to 50 concurrent users
NFR16: The system must handle teams of 5-200 users on a single instance without architectural changes
NFR17: Database storage growth must scale linearly with message volume — no exponential index bloat
NFR18: File storage must be isolated to a single configurable volume for predictable capacity planning
NFR19: The application must meet WCAG 2.1 Level A compliance
NFR20: All interactive elements must be reachable and operable via keyboard
NFR21: Text color contrast ratios must meet a minimum of 4.5:1
NFR22: Chat message lists must use ARIA live regions to announce incoming messages to screen readers
NFR23: Focus must be programmatically managed during conversation switches and modal dialogs
NFR24: The application must survive ungraceful container restarts without data loss — all persisted messages must be recoverable from the database
NFR25: SignalR connections must automatically reconnect with exponential backoff after transient failures
NFR26: Database migrations must be idempotent — running the same migration multiple times must produce the same result
NFR27: The application must start and become healthy within 30 seconds of container launch
NFR28: The application must be deployable with a single `docker-compose up` command with no additional manual steps
NFR29: All configuration must be manageable via environment variables — no in-container file editing required
NFR30: Structured logs must be written to stdout in JSON format compatible with standard log aggregators
NFR31: Health check endpoints must report application and database connectivity status
NFR32: Application data must be fully recoverable from database volume + file upload volume backups. Redis cache is ephemeral and rebuilds on application restart — it does not require backup.
NFR33: Docker image size must be under 500MB

### Additional Requirements

- Starter template: Jason Taylor Clean Architecture Solution Template (`dotnet new ca-sln --client-framework Angular --database sqlserver --output SimpleChat`) — must be the first implementation story
- Post-scaffold customizations required: add SignalR ChatHub, add Redis configuration for presence, replace Aspire with Docker Compose (3 containers), organize features into module folders (Identity, Messaging, Presence, Files), add service interfaces (IFileStorageService, IMessageSearchService, IAuthenticationProvider, ICacheService), configure MSSQL memory limits, switch frontend tests from Karma to Jest, add Angular Core/Shared/Features folder structure
- Docker Compose deployment: 3 containers only (app + MSSQL + Redis), MSSQL memory explicitly capped at ~512MB
- Authentication: JWT with Redis session validation, short-lived access token (15-30 min) in Angular service memory, refresh token as HttpOnly/Secure/SameSite=Strict cookie (7-day expiry), refresh token rotation with reuse detection
- Password hashing: bcrypt (BCrypt.Net-Next)
- Data model: Unified Conversation entity with type discriminator (Private/Group), ConversationParticipant join table
- Message persistence: bigint identity IDs, composite index (ConversationId, Id DESC) for cursor-based pagination, nullable EditedAt and DeletedAt columns (unused in V1)
- Unread tracking: LastReadMessageId per user per conversation on ConversationParticipant
- Full-text search: MSSQL Full-Text Index on Message.Content, abstracted via IMessageSearchService
- Presence: Redis heartbeat (60s interval) + TTL (90s), Redis Set for online_users, Angular polls GET /api/presence/online every 30s, client-side 5-min inactivity triggers Away status
- File storage: local disk (Docker volume), abstracted via IFileStorageService, image thumbnails via SkiaSharp
- Real-time: single SignalR ChatHub, persist-first message flow (Hub → MediatR Command → EF Insert → MediatR Publish → SignalR Broadcast)
- Logging: Serilog with JSON structured output to stdout
- Health checks: three-tier (/health/startup, /health/live, /health/ready)
- Error responses: Problem Details (RFC 9457)
- CI/CD: GitHub Actions (PR: unit + lint → integration → Docker build; Main: + push to ghcr.io)
- Testing: xUnit + EF InMemory (unit), Testcontainers MSSQL + Redis (integration), Jest + shallow TestBed (frontend), 100% coverage on command handlers and validators, 80%+ overall
- Rate limiting: configurable per env var, per-IP AND per-username on auth endpoints
- CORS: same-origin only for V1
- XSS prevention: plain text messages + Angular template escaping (no HtmlSanitizer)
- File validation: whitelist MIME types + magic byte validation + reject executables
- SignalR auth: JWT via query string, reconnection handler refreshes token before re-establishing hub
- Session expiry UX: non-disruptive overlay for re-auth, preserving scroll position and conversation context
- API response format: no wrapper object, cursor-based pagination with hasMore, camelCase JSON fields, ISO 8601 UTC dates
- Backend: 4-project structure (Domain, Application, Infrastructure, API), namespace-based module folders, one command/query per folder
- Frontend: Core/Shared/Features pattern, lazy-loaded feature modules, BehaviorSubject state management, async pipe in templates

### UX Design Requirements

UX-DR1: Implement custom SCSS design token system (_tokens.scss) as single source of truth for all visual decisions — colors (warm teal palette), typography (system fonts), spacing (4px base unit), border radii, shadows, animation timings, breakpoints, and z-index layers
UX-DR2: Implement Direction B "Airy & Modern" visual direction — white headers, clean chat background (#FAFBFC), teal own-message bubbles (#128C7E with white text), white other-message bubbles with subtle shadow, rounded-square avatars (14px radius), generous spacing
UX-DR3: Build Avatar component (shared) — rounded-square container (14px radius), two-letter initials derived from display name, deterministic color generation from user ID (8 predefined warm background colors), three sizes (sidebar 44px, header 36px, members 30px), presence dot overlay (10px circle, bottom-right), aria-label with name + status
UX-DR4: Build Presence Indicator component (shared) — online (green #25D366 dot), away (amber #F59E0B dot), offline (hidden/no dot), status conveyed via aria-label not color alone, auto-away after 5 min client inactivity (silent, no notification)
UX-DR5: Build Unread Badge component (shared) — circular 22px, primary teal background, white text, "99+" for counts exceeding 99, aria-label with full text ("12 unread messages")
UX-DR6: Build Connection Status Banner component (shared) — connected (hidden), reconnecting (amber banner after 3s debounce), disconnected (red banner, immediate), role="status" with aria-live="assertive", pushes content down (not overlay)
UX-DR7: Build File Preview component (shared) — image variant (inline thumbnail, click to expand) and file card variant (icon + filename + size + download link), upload progress bar, error state with "Upload failed — tap to retry"
UX-DR8: Build Message Bubble component (features/chat) — own message (teal bg, white text, right-aligned), other message (white bg, dark text, left-aligned, sender name in teal), system message (gray pill, centered), checkmark states (sending: single gray, sent: single teal), same-sender grouping (4px gap, no repeated name) vs different-sender (12px gap, name shown)
UX-DR9: Build Message Input component (features/chat) — text input with 16px radius border, teal focus highlight, labeled "Attach" button, teal rounded-square send button, Enter to send (desktop), Shift+Enter for newline, draft state from localStorage with "Draft" indicator, error state "Couldn't send — tap to retry", aria-label on input and send button
UX-DR10: Build Conversation List Item component (features/chat) — avatar with presence + conversation name + message preview + timestamp + unread badge, states (default/hover/active with left teal border/unread with bold name), draft "Draft:" prefix in preview, truncation with ellipsis
UX-DR11: Build Chat Window component (features/chat) — scrollable message list with role="log" and aria-live="polite", auto-scroll to bottom on new messages (if at bottom), "New messages" indicator if scrolled up, infinite scroll up for history with skeleton loading, scroll position preserved per conversation
UX-DR12: Build Chat Layout component (features/chat) — responsive 3/2/1 panel layout via BreakpointObserver: desktop (sidebar 320px + chat flex + members 240px toggleable), tablet (sidebar 280px collapsible + chat flex), mobile (single panel with back arrow navigation), panel transitions at 250ms
UX-DR13: Build Search Bar component (features/search) — bordered input with search icon + Ctrl/Cmd+K hint, keyboard shortcut opens focused, 300ms debounce, progressive results rendering, role="search"
UX-DR14: Build Search Results component (features/search) — each result shows conversation name + sender + message snippet with highlighted match + timestamp, progressive rendering (structure <300ms, context <1s), tap navigates to conversation and scrolls to highlighted message (highlight fades after 3s)
UX-DR15: Build Login component (features/auth) — minimal form with email + password fields, inline validation on blur, primary submit button with loading spinner, server errors as inline alert above form
UX-DR16: Build Register component (features/auth) — minimal form with display name + email + password fields, inline validation on blur with friendly language, auto-login after registration (no redirect to login), first user designated admin automatically
UX-DR17: Build User List component (features/admin) — table with name, email, role, status columns, role dropdown with Admin/Member options and brief descriptions, deactivate/reactivate buttons
UX-DR18: Build User Detail / deactivation confirmation dialog — modal explaining consequences ("This will immediately block access. All messages are preserved. You can reactivate later."), destructive red confirm button, cancel option
UX-DR19: Implement optimistic UI pattern for message sending — message appears instantly with single gray checkmark ("sending"), confirms with teal checkmark on server persist (<200ms typical), first message gets subtle micro-celebration
UX-DR20: Implement skeleton screen loading pattern — gray placeholder shapes matching content layout for conversation list and message list initial loads, shimmer effect disabled under prefers-reduced-motion, spinners for incremental/action loads
UX-DR21: Implement draft message persistence — save drafts to BehaviorSubject on conversation switch, persist to localStorage (keyed by conversationId) for cross-session survival, restore with subtle "Draft" indicator, clear on successful send
UX-DR22: Implement warm empty states with friendly microcopy — "Welcome! Join a conversation to get started" (new user), "No messages yet — say hello!" (empty conversation), "No messages found for '[query]'" (search), "No one is online right now" (members panel)
UX-DR23: Implement conversation creation flow — "New Chat" button opens team member picker overlay, filter-by-name, select one person (opens/creates DM, detects existing), select multiple (prompts for group name only), presence indicators on member list
UX-DR24: Implement responsive behavior per breakpoint matrix — message bubble max-width (60% desktop, 70% tablet, 85% mobile), input auto-focus (yes desktop/tablet, no mobile), conversation preview truncation by breakpoint, search (persistent sidebar on desktop/tablet, icon-to-overlay on mobile), mobile back arrow navigation
UX-DR25: Implement keyboard navigation — Ctrl/Cmd+K (search), Enter (send), Shift+Enter (newline), Escape (close overlay), arrow keys for conversation list and search results, Tab for focus cycling, focus management on conversation switch (input on desktop, header on mobile), focus trap in modals
UX-DR26: Implement accessibility baseline — all icons with visible text labels (no exceptions), minimum 44x44px touch targets on mobile (32x32px desktop), 2px solid primary outline focus indicators, skip-to-content link, prefers-reduced-motion support, ARIA live regions on message lists, role="listbox" with aria-selected on conversation list, color never sole indicator of state
UX-DR27: Implement button hierarchy — primary (teal bg, one per view), secondary (bordered), destructive (red, only in confirmation dialogs), ghost (no border, tertiary) — all with visible text labels, minimum sizes enforced
UX-DR28: Implement session expiry UX — non-disruptive overlay for re-authentication in-place, preserving scroll position and conversation context, silent state refresh after re-auth
UX-DR29: Implement "New messages" scroll indicator — when user is scrolled up and new messages arrive, show "New messages" button that scrolls to bottom, do not force-scroll the user

### FR Coverage Map

| FR | Epic | Description |
|----|------|-------------|
| FR1 | Epic 2 | User registration |
| FR2 | Epic 2 | User login |
| FR3 | Epic 2 | User logout |
| FR4 | Epic 2 | Session persistence |
| FR5 | Epic 2 | First user admin designation |
| FR6 | Epic 3 | Initiate private conversation |
| FR7 | Epic 3 | Send text in private conversation |
| FR8 | Epic 3 | View private conversation history |
| FR9 | Epic 6 | Share files/images in private conversation |
| FR10 | Epic 3 | Real-time private message delivery |
| FR11 | Epic 4 | Create group conversation |
| FR12 | Epic 4 | Invite users to group |
| FR13 | Epic 4 | Join existing groups |
| FR14 | Epic 4 | Leave group conversation |
| FR15 | Epic 4 | Send text in group conversation |
| FR16 | Epic 4 | View group conversation history |
| FR17 | Epic 6 | Share files/images in group conversation |
| FR18 | Epic 4 | Browse available groups |
| FR19 | Epic 3 | Real-time message delivery |
| FR20 | Epic 5 | See presence status |
| FR21 | Epic 5 | Set own presence status |
| FR22 | Epic 5 | Auto-update presence on inactivity |
| FR23 | Epic 3 | Connection state indicator |
| FR24 | Epic 5 | Unread message counts |
| FR25 | Epic 5 | Visual notification indicators |
| FR26 | Epic 5 | Distinguish unread conversations |
| FR27 | Epic 6 | Upload files |
| FR28 | Epic 6 | Upload images with preview |
| FR29 | Epic 6 | Download shared files |
| FR30 | Epic 6 | Configurable file size limit |
| FR31 | Epic 7 | Search by message content |
| FR32 | Epic 7 | Navigate to search result context |
| FR33 | Epic 7 | See conversation for search result |
| FR34 | Epic 8 | View all users |
| FR35 | Epic 8 | Assign user roles |
| FR36 | Epic 8 | Deactivate user |
| FR37 | Epic 8 | Reactivate user |
| FR38 | Epic 8 | Create default groups |
| FR39 | Epic 1 | Docker Compose deployment |
| FR40 | Epic 1 | Environment variable configuration |
| FR41 | Epic 1 | Auto database migrations |
| FR42 | Epic 1 | Zero-downtime upgrade |
| FR43 | Epic 1 | Health check endpoints |
| FR44 | Epic 1 | Structured logging |
| FR45 | Epic 1 | Volume-based backup |
| FR46 | Epic 9 | Responsive web interface (audit & polish) |
| FR47 | Epic 9 | PWA installation |
| FR48 | Epic 9 | Keyboard navigation (audit & polish) |
| FR49 | Epic 3 | Auto-reconnect |

## Epic List

### Epic 1: Project Foundation & Deployment Infrastructure
Operators can deploy, configure, monitor, upgrade, and back up simple-chat using a single Docker Compose command.
**FRs covered:** FR39, FR40, FR41, FR42, FR43, FR44, FR45
**Key deliverables:** Jason Taylor template scaffold, post-scaffold customizations (SignalR, Redis, module folders, service interfaces), Docker Compose (app + MSSQL 512MB cap + Redis), Serilog JSON logging, three-tier health checks, env var configuration, documented upgrade and backup paths.

### Epic 2: User Authentication & Account Management
Users can register, log in, maintain sessions across refreshes, and log out. The first registered user is designated as the instance administrator.
**FRs covered:** FR1, FR2, FR3, FR4, FR5
**Key deliverables:** Identity module, JWT + Redis session validation, refresh token rotation (HttpOnly cookie), bcrypt password hashing, Angular auth interceptor with refresh flow, Login component (UX-DR15), Register component (UX-DR16), first-user admin designation, rate limiting on auth endpoints.

### Epic 3: Real-Time Private Messaging
Users can have 1-on-1 real-time text conversations with full message history, with visual connection state feedback and automatic reconnection.
**FRs covered:** FR6, FR7, FR8, FR10, FR19, FR23, FR49
**Key deliverables:** Messaging module (Conversation entity with type discriminator, Message entity with bigint ID, ConversationParticipant), cursor-based message pagination, SignalR ChatHub with persist-first flow, MediatR command/notification pipeline, design token system (UX-DR1, UX-DR2), Chat Layout (UX-DR12), Conversation List Item (UX-DR10), Message Bubble (UX-DR8), Message Input (UX-DR9), Chat Window (UX-DR11), Avatar component (UX-DR3), Connection Status Banner (UX-DR6), optimistic send with checkmarks (UX-DR19), skeleton screens (UX-DR20), empty states (UX-DR22), conversation creation for DMs (UX-DR23 — single-select), draft persistence (UX-DR21), "New messages" indicator (UX-DR29), auto-reconnect with exponential backoff, session expiry UX (UX-DR28).

### Epic 4: Group Conversations
Users can create named group conversations, invite others, join/leave groups, browse available groups, and chat in groups with full history.
**FRs covered:** FR11, FR12, FR13, FR14, FR15, FR16, FR18
**Key deliverables:** Group conversation type support, group creation flow extending conversation creation (UX-DR23 — multi-select with group name prompt), join/leave mechanics, browse groups sidebar entry, system messages (group created, user joined/left), sender names displayed in group bubbles (UX-DR8 group variant).

### Epic 5: Presence & Notifications
Users can see who's online/away/offline and receive unread message counts and visual indicators for new messages across conversations.
**FRs covered:** FR20, FR21, FR22, FR24, FR25, FR26
**Key deliverables:** Presence module (Redis heartbeat 60s + TTL 90s, online_users Set), presence polling (GET /api/presence/online every 30s), client-side 5-min inactivity auto-away, Presence Indicator component (UX-DR4), Unread Badge component (UX-DR5), LastReadMessageId tracking on ConversationParticipant, online users panel (members panel), bold unread conversation names, conversation sort by LastMessageAt.

### Epic 6: File & Image Sharing
Users can upload, preview, and download files and images in any conversation, with configurable size limits and server-side thumbnail generation.
**FRs covered:** FR9, FR17, FR27, FR28, FR29, FR30
**Key deliverables:** Files module (FileAttachment entity, IFileStorageService with local disk implementation), SkiaSharp thumbnail generation, file type whitelist + magic byte validation + executable rejection, configurable max file size, File Preview component (UX-DR7), attach button integration in Message Input, upload progress indicator, file size hint near attach button.

### Epic 7: Search & Discovery
Users can search across all their conversations by message content, see which conversation results belong to, and navigate directly to the original message in context.
**FRs covered:** FR31, FR32, FR33
**Key deliverables:** MSSQL Full-Text Index on Message.Content, IMessageSearchService implementation, Search Bar component with Ctrl/Cmd+K shortcut (UX-DR13), Search Results with progressive rendering (UX-DR14), GetMessagesAround query for context jump, highlighted match with 3s fade, search state preservation on navigation.

### Epic 8: Administration
Administrators can view all users, assign roles, deactivate/reactivate accounts with immediate effect, and create default group conversations for team onboarding.
**FRs covered:** FR34, FR35, FR36, FR37, FR38
**Key deliverables:** Admin sidebar section (visible only to admins), User List component (UX-DR17), User Detail / deactivation confirmation dialog (UX-DR18), role assignment dropdown with descriptions, deactivation with Redis session delete for immediate lockout, reactivation, default group creation during initial setup.

### Epic 9: PWA, Accessibility & Cross-Cutting Polish
Users can install the app as a PWA, and all features meet responsive design and accessibility compliance standards through comprehensive audit and refinement.
**FRs covered:** FR46, FR47, FR48
**Key deliverables:** PWA manifest + service worker for cached loads and offline indicator, comprehensive keyboard navigation audit (UX-DR25), WCAG 2.1 Level A compliance audit, responsive breakpoint matrix polish (UX-DR24), skip-to-content link, prefers-reduced-motion final pass, touch target audit, contrast verification, button hierarchy consistency check (UX-DR27). Note: basic accessibility and responsive behavior are built into each preceding epic; this epic is the quality gate and final compliance verification.

## Epic 1: Project Foundation & Deployment Infrastructure

Operators can deploy, configure, monitor, upgrade, and back up simple-chat using a single Docker Compose command.

### Story 1.1: Project Scaffold & Solution Structure

As an operator,
I want the project scaffolded with the correct architecture and module organization,
So that all subsequent development follows a consistent, proven structure.

**Acceptance Criteria:**

**Given** a developer runs the Jason Taylor Clean Architecture template command (`dotnet new ca-sln --client-framework Angular --database sqlserver --output SimpleChat`)
**When** the scaffold is generated
**Then** the solution contains four projects: SimpleChat.Domain, SimpleChat.Application, SimpleChat.Infrastructure, SimpleChat.API (Web)
**And** each project compiles without errors

**Given** the scaffolded solution
**When** post-scaffold customizations are applied
**Then** the Domain project contains module folders: Identity/, Messaging/, Presence/, Files/ with Entities/ and Events/ subfolders per module
**And** the Domain project contains Common/ with BaseEntity.cs (Id as long, CreatedAt) and Enums/ (ConversationType, UserRole, PresenceStatus)
**And** the Application project contains matching module folders with Commands/, Queries/, EventHandlers/ subfolders
**And** the Application project contains Common/Interfaces/ with stubs for IFileStorageService, IMessageSearchService, IAuthenticationProvider, ICacheService
**And** the Application project contains Common/Behaviors/ with ValidationBehavior and LoggingBehavior pipeline behaviors
**And** the Application project contains Common/Exceptions/ with NotFoundException and ForbiddenAccessException
**And** the Application project contains Common/Models/ with PagedResult (Items, HasMore, NextCursor)

**Given** the scaffolded Angular frontend
**When** post-scaffold customizations are applied
**Then** the Angular app contains src/app/core/, src/app/shared/, src/app/features/ directories
**And** the features directory contains empty feature module folders: auth/, chat/, admin/, search/
**And** the Angular app contains src/app/models/ for TypeScript interfaces
**And** the test runner is switched from Karma to Jest with a passing default test
**And** the Angular app builds without errors using `ng build`

### Story 1.2: Docker Compose & Container Configuration

As an operator,
I want to deploy simple-chat with a single `docker-compose up` command,
So that I can run the complete application stack without manual service configuration.

**Acceptance Criteria:**

**Given** a server with Docker and Docker Compose installed
**When** the operator runs `docker-compose up`
**Then** three containers start: the application container, MSSQL (SQL Server), and Redis
**And** the application container is built from a multi-stage Dockerfile (build → publish → runtime)
**And** the MSSQL container has its memory capped at approximately 512MB via environment variable (`MSSQL_MEMORY_LIMIT_MB` or SA settings)
**And** the Redis container starts with default configuration

**Given** the Docker Compose configuration
**When** the operator inspects the defined volumes
**Then** a named volume exists for MSSQL data persistence
**And** a named volume exists for file uploads
**And** Redis is configured as ephemeral (no persistent volume required)

**Given** the Docker Compose configuration
**When** the operator reviews environment variables
**Then** all application configuration is manageable via environment variables in the compose file
**And** sensible defaults are provided for: database connection string, Redis connection, JWT secret, max file upload size
**And** no in-container file editing is required for basic deployment (NFR29)

**Given** the built Docker image
**When** the operator checks the image size
**Then** the image is under 500MB (NFR33)

**Given** the operator wants to upgrade
**When** they run `docker-compose pull && docker-compose up -d`
**Then** the new image is pulled and containers restart with zero manual migration steps (FR42)

### Story 1.3: Database Initialization & Auto-Migration

As an operator,
I want the database to initialize and migrate automatically on application startup,
So that I never need to run manual migration commands during deployment or upgrades.

**Acceptance Criteria:**

**Given** the application container starts with a fresh (empty) MSSQL database
**When** the application startup sequence runs
**Then** Entity Framework Core `MigrateAsync()` executes automatically
**And** all pending migrations are applied to create the initial schema
**And** the application becomes healthy after migrations complete

**Given** the application has previously run migrations
**When** the application restarts (e.g., container restart or upgrade)
**Then** `MigrateAsync()` runs and detects no pending migrations
**And** the startup completes without errors (idempotent — NFR26)

**Given** a slow MSSQL startup (container still initializing)
**When** the application attempts to connect
**Then** the application retries database connectivity with backoff rather than crashing
**And** the startup health probe (`/health/startup`) reports unhealthy until migrations complete
**And** Docker healthcheck does not trigger a restart loop during initial MSSQL startup

**Given** the application startup sequence
**When** measured end-to-end from container launch to healthy status
**Then** the application becomes healthy within 30 seconds under normal conditions (NFR27)

### Story 1.4: Structured Logging & Health Endpoints

As an operator,
I want structured JSON logs and health check endpoints,
So that I can monitor the application using standard infrastructure tooling.

**Acceptance Criteria:**

**Given** the application is running
**When** any log event occurs (startup, request, error)
**Then** the log is written to stdout in JSON format using Serilog (NFR30)
**And** log entries include structured fields (timestamp, level, message template, properties)
**And** log levels follow the defined standards: Debug (detailed flow), Information (business events), Warning (recoverable issues), Error (failures), Fatal (cannot continue)
**And** logs use Serilog message templates with named parameters — never string interpolation

**Given** the application is running and healthy
**When** an HTTP GET request is sent to `/health/startup`
**Then** a 200 OK response is returned indicating initialization is complete

**Given** the application is running
**When** an HTTP GET request is sent to `/health/live`
**Then** a 200 OK response is returned indicating the process is alive

**Given** the application is running with all dependencies available
**When** an HTTP GET request is sent to `/health/ready`
**Then** a 200 OK response is returned
**And** the response includes MSSQL connectivity status
**And** the response includes Redis connectivity status

**Given** the MSSQL database is unavailable
**When** an HTTP GET request is sent to `/health/ready`
**Then** a 503 Service Unavailable response is returned indicating database connectivity failure

**Given** the Redis instance is unavailable
**When** an HTTP GET request is sent to `/health/ready`
**Then** a 503 Service Unavailable response is returned indicating cache connectivity failure

### Story 1.5: CI Pipeline Configuration

As a developer,
I want an automated CI pipeline that validates code quality on every pull request,
So that broken code and regressions are caught before merging.

**Acceptance Criteria:**

**Given** a pull request is opened against the repository
**When** the GitHub Actions CI workflow runs
**Then** the pipeline executes in sequence: unit tests + linting → integration tests → Docker image build
**And** the pipeline fails fast if any stage fails (subsequent stages do not run)

**Given** the CI pipeline runs integration tests
**When** the tests execute
**Then** Testcontainers provisions real MSSQL and Redis instances for integration test execution
**And** integration tests run against actual database and cache infrastructure (not mocks)

**Given** a commit is pushed to the main branch
**When** the CI workflow completes successfully
**Then** the Docker image is built and pushed to GitHub Container Registry (ghcr.io)
**And** the image is tagged with the commit SHA and `latest`

**Given** the CI pipeline configuration
**When** a developer reviews the workflow file
**Then** both backend (.NET) and frontend (Angular) linters are configured and enforced
**And** the total pipeline duration should aim for under 5 minutes for the PR workflow (soft target, not a hard gate)

## Epic 2: User Authentication & Account Management

Users can register, log in, maintain sessions across refreshes, and log out. The first registered user is designated as the instance administrator.

### Story 2.1: User Registration with First-User Admin Designation

As a new team member,
I want to register an account with my display name, email, and password,
So that I can access the chat application and start communicating with my team.

**Acceptance Criteria:**

**Given** the registration page is displayed
**When** a user submits a valid display name, email address, and password
**Then** a new User entity is created with the provided display name, email, and a bcrypt-hashed password (NFR7)
**And** the user is assigned the Member role by default
**And** the user is automatically logged in (JWT access token issued, refresh token set as HttpOnly cookie)
**And** the user is redirected to the main conversation list view — no redirect to login page

**Given** no users exist in the system (fresh deployment)
**When** the first user completes registration
**Then** that user is automatically designated as the instance administrator (UserRole.Admin) (FR5)
**And** subsequent registrations create Member-role users

**Given** the registration form
**When** the user submits an email that is already registered
**Then** an inline error alert is displayed above the form: "An account with this email already exists"
**And** the form is not cleared — user input is preserved

**Given** the registration form (UX-DR16)
**When** the user blurs a field with invalid input
**Then** inline validation errors appear below the field in friendly language (e.g., "Please enter a valid email", "Password must be at least 8 characters")
**And** the field border turns red with red helper text
**And** the Submit button remains disabled until all fields are valid

**Given** the registration form
**When** the user submits a password shorter than 8 characters
**Then** inline validation rejects with: "Password must be at least 8 characters"
**And** no complexity rules beyond minimum length are enforced in V1

**Given** the Register component layout
**When** rendered on any viewport
**Then** the form displays three fields: Display Name, Email, Password — each with a visible label above the field (not placeholder-only)
**And** the Submit button shows a loading spinner during submission and is disabled to prevent double-submit
**And** the form is accessible: labels associated with inputs, validation errors linked via aria-describedby

### Story 2.2: User Login & JWT Session

As a registered user,
I want to log in with my email and password,
So that I can access my conversations and messages securely.

**Acceptance Criteria:**

**Given** the login page is displayed (UX-DR15)
**When** a user submits a valid email and password
**Then** the backend verifies the password against the stored bcrypt hash
**And** a short-lived JWT access token (15-30 minute expiry) is generated and returned in the response body
**And** a refresh token is issued as an HttpOnly, Secure, SameSite=Strict cookie with 7-day expiry
**And** a Redis session entry is created with a TTL matching the refresh token expiry
**And** the Angular app stores the access token in service memory (not localStorage — eliminates XSS token theft risk)
**And** the user is redirected to the main conversation list view

**Given** a user submits incorrect credentials
**When** the login request is processed
**Then** a generic error message is displayed: "Invalid email or password" (no indication of which field is wrong)
**And** the form is not cleared — email input is preserved, password is cleared

**Given** a deactivated user attempts to log in with valid credentials
**When** the login request is processed
**Then** the login is rejected with an appropriate error message
**And** no tokens are issued (NFR13)

**Given** the login endpoint
**When** multiple failed login attempts occur
**Then** rate limiting is enforced per-IP (20 attempts/minute) AND per-username (5 attempts/minute) via configurable environment variables
**And** rate limit responses use standard 429 Too Many Requests status

**Given** the Login component layout
**When** rendered on any viewport
**Then** the form displays Email and Password fields with visible labels above each field
**And** inline validation on blur with friendly language
**And** server errors displayed as inline alert above form (not toast)
**And** Submit button with loading spinner during request

### Story 2.3: Session Persistence & Token Refresh

As an authenticated user,
I want my session to persist across browser refreshes and handle token expiry silently,
So that I don't have to log in repeatedly during my workday.

**Acceptance Criteria:**

**Given** a user has a valid session (refresh token cookie exists)
**When** the user refreshes the browser or opens a new tab
**Then** the Angular app detects the absence of an in-memory access token
**And** automatically calls the `/api/auth/refresh` endpoint using the refresh token cookie
**And** a new access token is returned and stored in service memory
**And** the user lands on the conversation list without seeing a login screen (FR4)

**Given** the access token expires during an active session
**When** an API request returns 401 Unauthorized
**Then** the Angular auth interceptor queues the failed request
**And** calls `/api/auth/refresh` to obtain a new access token
**And** retries the original request with the new token
**And** concurrent 401s are batched — only one refresh request is made (prevents thundering herd)

**Given** a refresh token is used successfully
**When** the new tokens are issued
**Then** a new refresh token is issued (rotation) and the old refresh token is invalidated
**And** the Redis session entry is updated with the new refresh token identifier

**Given** a previously used (rotated-out) refresh token is presented
**When** the refresh endpoint processes it
**Then** reuse is detected as a potential token theft
**And** all sessions for that user are invalidated (emergency lockout — all Redis session entries deleted)
**And** the user must log in again on all devices

**Given** the refresh token cookie has expired (7+ days inactive)
**When** the user opens the application
**Then** the refresh attempt fails
**And** a non-disruptive overlay is shown for re-authentication in-place (UX-DR28)
**And** after re-authentication, the conversation state refreshes silently — scroll position and context preserved

### Story 2.4: Logout & Session Invalidation

As an authenticated user,
I want to log out and have my session immediately invalidated,
So that my account is secure when I'm done using the application.

**Acceptance Criteria:**

**Given** an authenticated user
**When** the user clicks the Logout action
**Then** the backend deletes the Redis session entry for this refresh token
**And** the refresh token cookie is cleared (expired)
**And** the Angular app clears the in-memory access token and any cached application state
**And** the user is redirected to the login page

**Given** a user has active sessions on multiple devices
**When** the user logs out on one device
**Then** only that device's session is invalidated
**And** other device sessions remain active (single-device logout, not global)

**Given** an administrator deactivates a user account (covered fully in Epic 8)
**When** the deactivation is processed
**Then** all Redis session entries for that user are deleted immediately
**And** the deactivated user's next API request or SignalR message fails with 401/403
**And** the deactivated user is unable to obtain new tokens via refresh (NFR13)

**Given** the API endpoints (excluding register, login, health)
**When** any request is made without a valid JWT access token
**Then** the request is rejected with 401 Unauthorized (NFR12)
**And** no user data or messages are accessible without authentication

## Epic 3: Real-Time Private Messaging

Users can have 1-on-1 real-time text conversations with full message history, with visual connection state feedback and automatic reconnection.

### Story 3.1: Conversation & Message Data Model

As a developer,
I want the core messaging data model in place,
So that conversations and messages can be persisted and queried efficiently.

**Acceptance Criteria:**

**Given** the messaging module in the Domain layer
**When** the entities are defined
**Then** a Conversation entity exists with: Id (long), Type (ConversationType enum: Private, Group), Name (nullable — null for Private), CreatedAt, CreatedById, LastMessageAt
**And** a ConversationParticipant entity exists with: ConversationId, UserId, JoinedAt, LastReadMessageId (nullable long)
**And** a Message entity exists with: Id (bigint identity), ConversationId, SenderId, Content (string, max 4000), SentAt, MessageType (enum: Text, File, System), FileId (nullable), EditedAt (nullable), DeletedAt (nullable)
**And** EditedAt and DeletedAt are defined but unused in V1 (future-proofing at zero cost)

**Given** the EF Core entity configurations
**When** a migration is generated and applied
**Then** the Messages table has a clustered primary key on Id (append-only write pattern)
**And** a composite index exists on (ConversationId, Id DESC) for cursor-based history pagination
**And** a unique constraint exists on ConversationParticipant (ConversationId, UserId)
**And** foreign keys are configured for SenderId → Users.Id and ConversationId → Conversations.Id

**Given** the data model
**When** message IDs are generated
**Then** they use bigint identity — sequential, compact, natural cursor ordering

**Given** the entity definitions
**When** integration tests run
**Then** entity persistence round-trips are verified (create, read, query) against a real MSSQL instance via Testcontainers

### Story 3.2: Send & Persist Messages (Backend)

As a user,
I want to send text messages in a private conversation and retrieve message history,
So that I can communicate with team members and review past conversations.

**Acceptance Criteria:**

**Given** an authenticated user and a valid conversation they participate in
**When** a SendMessageCommand is dispatched via MediatR
**Then** the message is persisted to the database (EF Insert, await)
**And** a MessageSent MediatR notification is published carrying the full Message entity + ConversationId
**And** the Conversation.LastMessageAt is updated
**And** the message Id (bigint) is returned

**Given** a SendMessageCommand with empty content or content exceeding 4000 characters
**When** the command is validated by FluentValidation
**Then** a 400 Problem Details response is returned with validation errors

**Given** an authenticated user and a conversation they participate in
**When** a GetMessageHistoryQuery is dispatched with conversationId, optional `before` cursor, and `limit` (default 50)
**Then** messages are returned in descending Id order using cursor-based pagination: `WHERE ConversationId = @id AND Id < @cursor ORDER BY Id DESC`
**And** the response includes the message list, `hasMore` boolean, and `nextCursor` value
**And** each message DTO includes: id, conversationId, senderId, senderDisplayName, content, sentAt, messageType

**Given** an authenticated user wants to start a private conversation
**When** a CreateConversationCommand is dispatched with the other user's ID
**Then** the system checks if a private conversation already exists between these two users
**And** if it exists, returns the existing conversation (no duplicate DMs)
**And** if it does not exist, creates a new Conversation (Type=Private, Name=null) with both users as ConversationParticipants
**And** the conversation Id is returned

**Given** the REST API endpoints
**When** requests are made
**Then** POST `/api/conversations` creates a conversation
**And** GET `/api/conversations/{id}/messages?before={cursor}&limit={n}` returns paginated history
**And** all endpoints require authentication and verify the user is a participant

**Given** a user sends a message to a conversation they are not a participant in (e.g., left a group)
**When** the command is processed
**Then** a 403 Forbidden Problem Details response is returned

### Story 3.3: SignalR Real-Time Messaging Hub

As a user,
I want to receive messages in real time without refreshing the page,
So that conversations feel instant and natural.

**Acceptance Criteria:**

**Given** an authenticated user with a valid JWT
**When** the Angular app establishes a SignalR connection to `/hubs/chat`
**Then** the connection is authenticated via JWT passed as a query string parameter
**And** the user is added to SignalR groups for each conversation they participate in

**Given** a user sends a message via the ChatHub `SendMessage` method
**When** the hub receives the message
**Then** the hub dispatches a SendMessageCommand via MediatR (persist-first flow)
**And** after successful persistence, the MessageSent notification triggers a SignalR broadcast
**And** all participants in the conversation receive a `ReceiveMessage` event with the full message DTO
**And** the message is guaranteed durable in the database before any client sees it

**Given** the SignalR connection drops (network interruption)
**When** the client detects disconnection
**Then** the SignalR client automatically attempts reconnection with exponential backoff: [0, 2000, 5000, 10000, 30000] ms
**And** on successful reconnection, the client refreshes its JWT if expired before re-establishing the hub connection
**And** the client re-joins all conversation groups (FR49)

**Given** the client successfully reconnects after a disconnection
**When** the hub connection is re-established
**Then** the client fetches the conversation list with updated unread counts
**And** for the active conversation, fetches messages newer than the last known message ID to fill any gap
**And** the UI updates seamlessly without requiring a manual refresh

**Given** a centralized SignalRService in Angular core
**When** server events arrive (ReceiveMessage, UserOnline, UserOffline)
**Then** the service dispatches events to the appropriate domain services (MessageService, PresenceService)
**And** a `connectionState$` observable exposes the current state (Connected, Reconnecting, Disconnected)

### Story 3.4: Design Token System & Chat Layout Shell

As a user,
I want a clean, responsive chat interface,
So that I can navigate conversations comfortably on any device.

**Acceptance Criteria:**

**Given** the Angular shared styles directory
**When** the design token system is created (UX-DR1)
**Then** `_tokens.scss` defines all visual decisions: color palette (warm teal primary #128C7E, neutrals, text, semantic), typography (system font stack, type scale from 12px to 18px), spacing (4px base unit scale), border radii (sm 4px, md 8px, lg 16px, full 9999px), shadows (sm, md, lg), animation timings (fast 150ms, normal 250ms, slow 400ms), breakpoints (mobile 768px, tablet 1024px), z-index layers
**And** Direction B tokens are included: avatar-radius 14px, bubble-radius 16px, chat-bg #FAFBFC, bubble colors, input radius 16px (UX-DR2)
**And** responsive mixins are available: `@include tablet { }`, `@include mobile { }`

**Given** the chat feature module
**When** the ChatLayoutComponent is built (UX-DR12)
**Then** on desktop (≥1024px): 3-panel layout with sidebar (320px fixed), chat area (flex: 1), members panel (240px, toggleable)
**And** on tablet (768–1023px): 2-panel with collapsible sidebar (280px) + chat area
**And** on mobile (<768px): single-panel navigation — conversation list OR chat view, with back arrow in chat header
**And** panel transitions use 250ms ease timing
**And** minimum supported viewport width is 320px

**Given** the Angular routing configuration
**When** the chat feature module is loaded
**Then** it is lazy-loaded via route configuration
**And** the route is guarded by authentication (redirect to login if no session)

### Story 3.5: Conversation List & Navigation

As a user,
I want to see my conversations in a sidebar list,
So that I can quickly find and switch between active conversations.

**Acceptance Criteria:**

**Given** an authenticated user with existing conversations
**When** the conversation list loads
**Then** conversations are displayed sorted by LastMessageAt descending (most recent at top)
**And** each ConversationListItem (UX-DR10) shows: Avatar (UX-DR3) with presence dot, conversation name (other user's display name for DMs), last message preview text, timestamp, unread badge if applicable
**And** the active conversation has a left 3px teal border + surface background color

**Given** the Avatar component (UX-DR3)
**When** rendered
**Then** it displays a rounded-square container (14px border-radius) at the correct size (44px sidebar, 36px header, 30px members)
**And** shows two-letter initials derived from display name (first letter of first two words; "Sarah Chen" → "SC", "Marcus" → "M")
**And** background color is deterministically generated from user ID (pick from 8 predefined warm colors)
**And** includes `aria-label` with user name and status (e.g., "Sarah Chen (online)")

**Given** the conversation list
**When** the user taps/clicks a conversation
**Then** the chat area loads that conversation's messages
**And** on mobile, the view switches from conversation list panel to chat panel
**And** the conversation name appears in the chat header

**Given** the conversation list API
**When** GET `/api/conversations` is called
**Then** it returns the user's conversations with: id, type, name, lastMessage (preview), lastMessageAt, otherParticipants (for display name/avatar), unreadCount

**Given** the conversation list on initial load
**When** data is not yet available
**Then** skeleton screen placeholders matching the list item shape are displayed (UX-DR20)
**And** skeleton shimmer is disabled when `prefers-reduced-motion` is active

### Story 3.6: Chat Window & Message Display

As a user,
I want to see messages displayed as conversation bubbles with full scrollable history,
So that I can read current and past messages naturally.

**Acceptance Criteria:**

**Given** an active conversation
**When** the ChatWindow component renders (UX-DR11)
**Then** the message list container has `role="log"` and `aria-live="polite"`
**And** new messages are announced to screen readers without interrupting current reading
**And** history loads via infinite scroll are NOT announced (only new real-time messages)

**Given** messages in the conversation
**When** the MessageBubble component renders each message (UX-DR8)
**Then** own messages display with teal (#128C7E) background, white text, right-aligned, border-radius 16px with 4px bottom-right
**And** other users' messages display with white background, dark text, left-aligned, subtle shadow (0 1px 3px rgba(0,0,0,0.08)), border-radius 16px with 4px bottom-left
**And** sender name is shown in teal above other users' messages (for future group support)
**And** timestamp is shown on each message in 12px caption style
**And** same-sender consecutive messages use 4px gap with no repeated sender name; different-sender messages use 12px gap with sender name shown

**Given** the user is viewing a conversation and is scrolled to the bottom
**When** a new message arrives via SignalR
**Then** the chat window auto-scrolls to show the new message

**Given** the user has scrolled up to read history
**When** a new message arrives via SignalR
**Then** a "New messages ↓" indicator appears at the bottom of the chat window (UX-DR29)
**And** tapping the indicator scrolls to the bottom
**And** the user is NOT force-scrolled

**Given** the user scrolls to the top of loaded messages
**When** more history exists (hasMore = true)
**Then** a loading spinner appears at the top
**And** older messages are fetched using cursor-based pagination and prepended
**And** scroll position is preserved (user doesn't jump)

**Given** the user switches away from a conversation and returns
**When** the conversation is re-rendered
**Then** the previous scroll position is restored

**Given** a conversation is loading for the first time
**When** messages are being fetched
**Then** skeleton screen placeholders matching bubble shapes are displayed (UX-DR20)

### Story 3.7: Message Input & Optimistic Send

As a user,
I want to type and send messages with instant visual feedback,
So that the conversation feels fast and responsive.

**Acceptance Criteria:**

**Given** the MessageInput component (UX-DR9)
**When** rendered
**Then** a text input with 16px border-radius, 1.5px border, and teal focus highlight is displayed
**And** a labeled "Attach" button is rendered but visually disabled (grayed out, no click handler) until file sharing is implemented in Epic 6
**And** a teal rounded-square (14px radius) Send button is visible
**And** the input has `aria-label="Type a message"` and the send button has `aria-label="Send message"`
**And** on desktop, the input is auto-focused when a conversation is opened
**And** on mobile, the input is NOT auto-focused (preserves reading space)

**Given** the user types a message and presses Enter (desktop) or taps Send (mobile)
**When** the message is submitted
**Then** the message appears instantly in the chat window as an own-message bubble (optimistic UI — UX-DR19)
**And** a single gray checkmark (✓) indicates "sending" state
**And** after server persistence confirms (typically <200ms), the checkmark changes to teal (✓) indicating "sent"
**And** the first message ever sent by the user gets a subtle micro-celebration (slightly more pronounced checkmark animation)

**Given** the user presses Shift+Enter
**When** typing in the message input
**Then** a newline is inserted (message is not sent)

**Given** the user is typing in a conversation
**When** they switch to a different conversation without sending
**Then** the draft text is saved to BehaviorSubject state and persisted to localStorage keyed by conversationId (UX-DR21)

**Given** the user switches to a conversation that has a saved draft
**When** the conversation loads
**Then** the message input is pre-populated with the draft text
**And** a subtle "Draft" indicator is shown that clears on first keystroke

**Given** a message is sent successfully
**When** the server confirms persistence
**Then** the draft for that conversation is cleared from both BehaviorSubject and localStorage

**Given** the user attempts to send while disconnected
**When** the send fails
**Then** the message stays in the input field with "Couldn't send — tap to retry" error state
**And** the user's typed text is never lost

### Story 3.8: New Private Conversation Flow

As a user,
I want to start a private conversation by picking a team member,
So that I can quickly message anyone on my team without navigating forms.

**Acceptance Criteria:**

**Given** the sidebar
**When** the user taps the "New Chat" button (with visible text label)
**Then** a team member picker overlay opens showing all registered active users
**And** each user entry shows: Avatar with presence indicator, display name, presence status text
**And** a filter-by-name text input is available at the top for instant filtering

**Given** the team member picker
**When** the user selects one person
**Then** the system checks if a private conversation already exists with that person
**And** if it exists, the existing conversation is opened
**And** if it does not exist, a new private conversation is created and opened
**And** the message input is focused (desktop) — ready to type

**Given** a new user with no conversations
**When** they land on the conversation list
**Then** a warm empty state is displayed: "Welcome! Join a conversation to get started." with a "New Chat" button (UX-DR22)

**Given** an empty conversation (newly created, no messages)
**When** the chat window renders
**Then** a warm empty state is displayed: "No messages yet — say hello!" (UX-DR22)

### Story 3.9: Connection Status & Auto-Reconnect

As a user,
I want to see my connection state and have the app reconnect automatically,
So that I'm never confused about whether my messages are being delivered.

**Acceptance Criteria:**

**Given** the ConnectionStatusBanner component (UX-DR6)
**When** the SignalR connection is healthy
**Then** the banner is hidden (not rendered)

**Given** a brief connection interruption (<3 seconds)
**When** SignalR reconnects within 3 seconds
**Then** no banner is ever shown (3-second debounce prevents flicker anxiety)

**Given** a connection interruption lasting more than 3 seconds
**When** SignalR is attempting to reconnect
**Then** an amber banner appears at the top of the chat area: "Reconnecting..."
**And** the banner has `role="status"` and `aria-live="assertive"`
**And** the banner pushes content down (not an overlay)

**Given** the connection is fully lost (all reconnection attempts exhausted)
**When** the disconnected state is reached
**Then** a red banner appears immediately (no debounce): "Connection lost. Check your internet connection."
**And** the banner is displayed with `role="status"` and `aria-live="assertive"`

**Given** the SignalR reconnection sequence
**When** retries are attempted
**Then** exponential backoff intervals are used: [0, 2000, 5000, 10000, 30000] ms (NFR25)
**And** the connectionState$ observable updates to reflect: Connected, Reconnecting, or Disconnected
**And** on successful reconnection, the client refreshes conversation state from the server

## Epic 4: Group Conversations

Users can create named group conversations, invite others, join/leave groups, browse available groups, and chat in groups with full history.

### Story 4.1: Group Conversation Creation

As a user,
I want to create a group conversation by selecting multiple team members and giving it a name,
So that I can communicate with a subset of my team in a shared space.

**Acceptance Criteria:**

**Given** the team member picker overlay (built in Story 3.8)
**When** the user selects two or more people
**Then** a group name text input prompt appears (single field — no description, no settings, no avatar)
**And** the user types a name and presses Enter or taps "Create"
**And** a new Conversation (Type=Group, Name=provided name) is created with all selected users + the creator as ConversationParticipants
**And** a system message is added: "{creator} created the group"
**And** the group conversation opens with the message input focused

**Given** the conversation creation flow (UX-DR23)
**When** the user selects exactly one person
**Then** the system creates/opens a private DM (Epic 3 behavior)
**When** the user selects two or more people
**Then** the system prompts for a group name (group creation flow)
**And** the system infers conversation type from selection count — no type dropdown, no form

**Given** a group conversation
**When** rendered in the conversation list
**Then** the conversation name is the group name (not participant names)
**And** the avatar shows the first two letters of the group name

### Story 4.2: Group Messaging & History

As a user,
I want to send and read messages in a group conversation,
So that I can participate in team discussions with full context.

**Acceptance Criteria:**

**Given** an authenticated user who is a participant in a group conversation
**When** they send a message in the group
**Then** the message is persisted and broadcast to all group participants via SignalR
**And** all participants see the message in real time
**And** the message bubble displays the sender's name in teal above the message text (UX-DR8 group variant)
**And** same-sender consecutive messages are grouped (4px gap, name shown only on first)

**Given** a group conversation with message history
**When** the user scrolls up
**Then** older messages load via cursor-based pagination (same behavior as private messaging)
**And** each message clearly shows which participant sent it

**Given** a system event in the group (user joined, user left, group created)
**When** the event is rendered
**Then** a system message appears as a gray pill, centered in the chat stream (UX-DR8 system message variant)

### Story 4.3: Browse & Join Groups

As a user,
I want to browse available group conversations and join ones that interest me,
So that I can participate in team discussions relevant to my work.

**Acceptance Criteria:**

**Given** the sidebar
**When** the user taps "Browse Groups" (visible text label entry point)
**Then** a list of all group conversations is displayed (excluding those the user already belongs to)
**And** each entry shows: group name, participant count, last message preview, last activity timestamp

**Given** the browse groups list
**When** the user taps "Join" on a group
**Then** the user is added as a ConversationParticipant
**And** a system message is added: "{user} joined the group"
**And** the group appears in the user's conversation list
**And** the user can see the full message history from before they joined (FR13, FR16)

**Given** the browse groups list
**When** no groups are available to join
**Then** a friendly empty state is shown: "No groups to join right now. Create one!"

### Story 4.4: Invite Users & Leave Group

As a user,
I want to invite others to my group and leave groups I no longer need,
So that group membership stays relevant and manageable.

**Acceptance Criteria:**

**Given** a user is a participant in a group conversation
**When** they open the group's members panel and tap "Invite"
**Then** a user picker overlay opens showing active users not already in the group
**And** selecting one or more users adds them as ConversationParticipants
**And** a system message is added for each: "{inviter} added {user}"
**And** invited users see the group in their conversation list with full history access (FR12)

**Given** a user is a participant in a group conversation
**When** they tap "Leave Group"
**Then** the user is removed from ConversationParticipants
**And** a system message is added: "{user} left the group"
**And** the group is removed from the user's conversation list
**And** the action uses the label "Leave group" — never "Delete" (prevents confusion about data loss) (FR14)

**Given** the last participant leaves a group
**When** the leave action completes
**Then** the group conversation is preserved in the database (not deleted) — messages are never lost
**And** the group remains visible in Browse Groups and can be re-joined

## Epic 5: Presence & Notifications

Users can see who's online/away/offline and receive unread message counts and visual indicators for new messages across conversations.

### Story 5.1: Presence Backend (Redis Heartbeat & TTL)

As a user,
I want the system to track who is online,
So that I can see which team members are available for conversation.

**Acceptance Criteria:**

**Given** an authenticated user with an active session
**When** the Angular app is in the foreground
**Then** it sends POST `/api/presence/heartbeat` every 60 seconds with the user's current status (Online or Away)
**And** the backend sets a Redis key `presence:{userId}` with a 90-second TTL
**And** the backend adds the userId to a Redis Set `online_users`

**Given** a user closes the browser or loses connectivity
**When** 90 seconds pass without a heartbeat
**Then** the Redis key expires automatically via TTL
**And** the user is lazily pruned from the `online_users` Set on the next read (GET `/api/presence/online` cross-references each Set member's TTL key; if expired, the member is removed from the Set before returning results)

**Given** the Angular app detects 5 minutes of client-side inactivity
**When** the next heartbeat fires
**Then** the heartbeat payload changes status to Away
**And** the user's presence state reflects Away (amber dot) for other users
**And** no notification or toast is shown to the user (silent, per "quiet confidence" principle — UX-DR4)

**Given** any authenticated user
**When** GET `/api/presence/online` is called
**Then** a list of currently online/away users is returned with their userId and status
**And** the Angular app polls this endpoint every 30 seconds

### Story 5.2: Presence UI & Online Users Panel

As a user,
I want to see presence indicators on avatars and a list of who's online,
So that I know which team members are available right now.

**Acceptance Criteria:**

**Given** the Presence Indicator component (UX-DR4)
**When** a user is Online
**Then** a green dot (#25D366, 10px circle) is displayed at the bottom-right of their avatar
**When** a user is Away
**Then** an amber dot (#F59E0B) is displayed
**When** a user is Offline
**Then** no dot is displayed (absence indicates offline)
**And** status is always conveyed via `aria-label` on the avatar, not color alone

**Given** a desktop viewport (≥1024px)
**When** the members panel is visible (240px right panel)
**Then** it displays all team members grouped by status: Online first, then Away, then Offline
**And** each entry shows Avatar with presence dot and display name

**Given** a tablet or mobile viewport
**When** the user taps the members toggle in the chat header
**Then** the members panel is shown (overlay or panel switch depending on breakpoint)

**Given** no users are currently online (edge case)
**When** the members panel renders
**Then** a friendly empty state is shown: "No one is online right now." (UX-DR22)

### Story 5.3: Unread Tracking & Notification Indicators

As a user,
I want to see unread message counts per conversation and visual indicators for new messages,
So that I know which conversations need my attention.

**Acceptance Criteria:**

**Given** a user opens a conversation
**When** the conversation renders
**Then** the user's `LastReadMessageId` on ConversationParticipant is updated to the most recent message Id (MarkAsRead command)
**And** the unread badge for that conversation clears in the sidebar

**Given** a new message arrives in a conversation the user is NOT currently viewing
**When** the SignalR event is received
**Then** the unread count for that conversation increments in the sidebar
**And** the Unread Badge component (UX-DR5) displays the count (circular 22px, teal background, white text)
**And** counts exceeding 99 display as "99+"
**And** the badge has `aria-label` with full text (e.g., "12 unread messages")

**Given** the conversation list
**When** a conversation has unread messages
**Then** the conversation name is displayed in bold (font-weight 600) (FR26)
**And** the conversation is sorted by LastMessageAt (most recent at top)

**Given** a user opens simple-chat after being away (Monday morning scenario)
**When** the conversation list loads
**Then** unread counts are accurate and render immediately (efficient query: count messages where Id > LastReadMessageId per conversation)
**And** the sidebar renders within the target of <1 second

**Given** a conversation with unread messages is opened
**When** the chat window renders
**Then** the view scrolls to the first unread message as the scroll target

## Epic 6: File & Image Sharing

Users can upload, preview, and download files and images in any conversation, with configurable size limits and server-side thumbnail generation.

### Story 6.1: File Upload Backend & Storage

As a user,
I want to upload files to a conversation,
So that I can share documents and resources with my team.

**Acceptance Criteria:**

**Given** an authenticated user in a conversation
**When** they upload a file via POST `/api/files/upload`
**Then** the file is validated: MIME type checked against a whitelist, magic bytes verified, executable files rejected (NFR10)
**And** the file size is checked against the configurable maximum (environment variable, default e.g., 25MB)
**And** if valid, the file is stored on local disk via IFileStorageService (Docker volume)
**And** a FileAttachment entity is created: Id, FileName, ContentType, Size, StoragePath, ThumbnailPath (nullable), UploadedAt, UploadedById
**And** the FileAttachment Id is returned

**Given** the uploaded file is an image (JPEG, PNG, GIF, WebP)
**When** storage completes
**Then** a thumbnail is generated server-side using SkiaSharp and saved to ThumbnailPath
**And** the thumbnail is appropriately sized for inline chat display

**Given** a file exceeds the configurable maximum size
**When** the upload is attempted
**Then** the request is rejected with a 400 Problem Details response before the full file is received
**And** the error message includes the size limit (e.g., "File exceeds 25MB limit")

**Given** a file with a disallowed MIME type or executable signature
**When** the upload is attempted
**Then** the request is rejected with a 400 Problem Details response
**And** the rejection is logged at Warning level

### Story 6.2: File Messages & Download

As a user,
I want to send files as part of a conversation and download files others have shared,
So that shared resources are accessible in their conversation context.

**Acceptance Criteria:**

**Given** a user has uploaded a file (Story 6.1)
**When** they send a message with the file attached
**Then** a Message is created with MessageType=File and FileId referencing the FileAttachment
**And** the message is broadcast to all conversation participants via SignalR (same persist-first flow as text messages)

**Given** a file message in the chat
**When** the FilePreview component renders (UX-DR7)
**Then** image files display as an inline thumbnail (click to expand to full size)
**And** non-image files display as a file card: icon + filename + file size + download link
**And** the component is accessible with appropriate alt text and aria-labels

**Given** a user clicks download on a shared file
**When** GET `/api/files/{fileId}/download` is called
**Then** the file is streamed from disk with correct Content-Type and Content-Disposition headers
**And** the endpoint verifies the user is a participant in the conversation containing the file

### Story 6.3: File Upload UX & Attach Flow

As a user,
I want a smooth file upload experience with progress feedback,
So that I know my file is being shared and can recover from errors.

**Acceptance Criteria:**

**Given** the MessageInput component
**When** the user taps the labeled "Attach" button
**Then** a native file picker opens

**Given** a file is selected for upload
**When** the upload begins
**Then** a progress bar is displayed within the FilePreview component in the chat area
**And** an "Uploading..." label is visible
**And** the file size hint is visible near the attach button ("Files up to {max}MB")

**Given** the upload fails (network error, server error)
**When** the failure is detected
**Then** the file preview shows "Upload failed — tap to retry" (UX-DR7)
**And** tapping retry re-attempts the upload

**Given** the upload succeeds
**When** the file message is sent
**Then** the file preview transitions to its final display state (thumbnail or file card)
**And** the message follows the same optimistic send pattern (checkmark states)

## Epic 7: Search & Discovery

Users can search across all their conversations by message content, see which conversation results belong to, and navigate directly to the original message in context.

### Story 7.1: Full-Text Search Backend

As a user,
I want to search across all my conversations by message content,
So that I can find specific information from past discussions.

**Acceptance Criteria:**

**Given** the Messages table
**When** the MSSQL Full-Text Index is configured
**Then** a full-text index exists on Message.Content
**And** the index is created via raw SQL in an EF migration (`migrationBuilder.Sql(...)`) since EF Core does not natively support full-text index creation via Fluent API

**Given** an authenticated user
**When** a SearchMessagesQuery is dispatched with a search term, optional cursor, and limit
**Then** the IMessageSearchService implementation queries the MSSQL Full-Text Index
**And** results are filtered to only conversations the user participates in
**And** results include: messageId, conversationId, conversationName, senderDisplayName, content snippet with match context, sentAt
**And** results are returned with pagination (hasMore, nextCursor)
**And** results return within 1 second for histories up to 1 million messages (NFR2)

**Given** the search API endpoint
**When** GET `/api/messages/search?q={term}&before={cursor}&limit={n}` is called
**Then** results are returned in relevance order
**And** the endpoint requires authentication

### Story 7.2: Search UI & Progressive Results

As a user,
I want a search bar with instant results,
So that finding past messages feels fast and effortless.

**Acceptance Criteria:**

**Given** the SearchBar component (UX-DR13)
**When** rendered in the sidebar (desktop/tablet) or as an overlay (mobile)
**Then** a bordered input with search icon and "⌘K" hint text is displayed
**And** pressing Ctrl/Cmd+K from anywhere focuses the search input
**And** the component has `role="search"`

**Given** the user types a query
**When** input is debounced (300ms)
**Then** a search request is fired
**And** results stream in progressively (UX-DR14): conversation name + sender appear first (<300ms), then context preview with highlighted match (<1s)
**And** results are announced via `aria-live="polite"`

**Given** the search results list
**When** each result is displayed
**Then** it shows: conversation name, sender display name, message snippet with the match highlighted, and timestamp

**Given** no results match the query
**When** the empty state renders
**Then** a friendly message is shown: "No messages found for '{query}'. Try different keywords." (UX-DR22)

### Story 7.3: Search Context Jump

As a user,
I want to click a search result and land in the conversation at that exact message,
So that I can read the surrounding context of the found message.

**Acceptance Criteria:**

**Given** the user taps a search result
**When** the navigation occurs
**Then** the target conversation opens in the chat area
**And** a GetMessagesAround query loads ~20 messages surrounding the matched message
**And** the view scrolls to the matched message
**And** the matched message is highlighted with a subtle accent background (rgba(18, 140, 126, 0.1))
**And** the highlight fades after 3 seconds

**Given** the user is viewing a highlighted search result in context
**When** they want to return to the search results
**Then** pressing Escape or tapping a back button returns to the search results list
**And** the search state (query, results, scroll position) is preserved

**Given** the user is viewing a highlighted search result
**When** they continue scrolling naturally in the conversation
**Then** the highlight fades and normal chat behavior resumes
**And** infinite scroll works in both directions (up for older history, down for newer)

## Epic 8: Administration

Administrators can view all users, assign roles, deactivate/reactivate accounts with immediate effect, and create default group conversations for team onboarding.

### Story 8.1: Admin Panel & User List

As an administrator,
I want to view all registered users with their roles and status,
So that I can manage my team's access to the chat application.

**Acceptance Criteria:**

**Given** an admin-role user
**When** they view the sidebar
**Then** an "Admin" section is visible (not visible to Member-role users)
**And** tapping "Admin" navigates to the admin panel

**Given** the admin panel
**When** the User List component renders (UX-DR17)
**Then** a table displays all registered users with columns: display name, email, role (Admin/Member), status (Active/Inactive)
**And** the list is searchable/filterable by name or email

**Given** the admin panel is accessed by a non-admin user (direct URL or API)
**When** the request is processed
**Then** the API returns 403 Forbidden
**And** the Angular route guard redirects to the main chat view

**Given** the admin feature module
**When** loaded
**Then** it is lazy-loaded via Angular routing
**And** guarded by both authentication and admin-role authorization

### Story 8.2: Role Assignment

As an administrator,
I want to assign roles to users,
So that I can delegate admin responsibilities to trusted team members.

**Acceptance Criteria:**

**Given** the user list table
**When** an admin clicks the role dropdown on a user row
**Then** the options "Admin" and "Member" are displayed with brief descriptions (e.g., "Admin — can manage users and settings", "Member — standard chat access")

**Given** an admin selects a new role for a user
**When** the role change is submitted
**Then** the user's role is updated immediately in the database
**And** a subtle confirmation is shown (row updates, no toast — per "quiet confidence" principle)
**And** the change takes effect on the user's next API request (role checked from JWT or database)

**Given** an admin attempts to remove their own admin role
**When** they are the last remaining admin
**Then** the action is prevented with a clear message: "Cannot remove the last administrator"

### Story 8.3: User Deactivation & Reactivation

As an administrator,
I want to deactivate departing team members and reactivate returning ones,
So that access is controlled without losing any message history.

**Acceptance Criteria:**

**Given** an admin clicks "Deactivate" on an active user row
**When** the deactivation confirmation dialog appears (UX-DR18)
**Then** the dialog explains consequences: "This will immediately block {user}'s access. All messages are preserved. You can reactivate later."
**And** the dialog has a destructive red "Deactivate" button and a "Cancel" button
**And** focus is trapped within the modal; Escape closes it

**Given** the admin confirms deactivation
**When** the deactivation is processed
**Then** the user's IsActive flag is set to false
**And** all Redis session entries for that user are deleted immediately — instant lockout (NFR13)
**And** the user's next API request or SignalR connection attempt fails with 401/403
**And** the user list row updates to show "Inactive" status

**Given** an admin clicks "Reactivate" on an inactive user
**When** the reactivation is processed
**Then** the user's IsActive flag is set to true
**And** the user can log in again
**And** the user list row updates to show "Active" status
**And** no confirmation dialog is needed for reactivation (non-destructive action)

**Given** a deactivated user
**When** their messages are viewed by other users
**Then** all of the deactivated user's messages remain visible and intact — deactivation never deletes data

### Story 8.4: Default Group Creation

As an administrator,
I want to create default group conversations during initial setup,
So that new team members see active conversations when they first join.

**Acceptance Criteria:**

**Given** an admin user
**When** they create a group conversation from the admin panel or via the normal group creation flow
**Then** the group is created as a standard group conversation
**And** the group is visible in Browse Groups for all users to join (FR38)

**Given** a new user registers
**When** they land on the conversation list
**Then** they can see available groups via Browse Groups
**And** default groups (e.g., General, Engineering) are discoverable immediately
**And** no empty screen experience — the warm empty state with "Browse Groups" button guides them (UX-DR22)

## Epic 9: PWA, Accessibility & Cross-Cutting Polish

Users can install the app as a PWA, and all features meet responsive design and accessibility compliance standards through comprehensive audit and refinement.

### Story 9.1: PWA Manifest & Service Worker

As a user,
I want to install simple-chat as a PWA from my browser,
So that I get app-like fast access without downloading from an app store.

**Acceptance Criteria:**

**Given** a user visits simple-chat in a supported browser
**When** the PWA install criteria are met (manifest + service worker)
**Then** the browser's install prompt is available (e.g., "Add to Home Screen")
**And** the app manifest specifies: app name, short name, icons (multiple sizes), start URL, display mode (standalone), theme color (#128C7E), background color (#FFFFFF)

**Given** the service worker is registered
**When** the user revisits the app
**Then** static assets (Angular bundle, styles, fonts) are served from cache
**And** cached page loads complete within 2 seconds (NFR3)
**And** the service worker does NOT cache API responses or SignalR connections

**Given** the user has no network connectivity
**When** the PWA loads
**Then** a clear offline indicator is displayed
**And** the app does not attempt to queue messages locally (online-first design)

### Story 9.2: Comprehensive Keyboard Navigation Audit

As a keyboard-only user,
I want to navigate all core workflows without a mouse,
So that the application is fully accessible to me.

**Acceptance Criteria:**

**Given** all implemented features
**When** audited for keyboard accessibility (UX-DR25)
**Then** Ctrl/Cmd+K opens search from any context
**And** Enter sends a message in chat view
**And** Shift+Enter inserts a newline
**And** Escape closes any overlay, search panel, or modal
**And** Arrow keys (↑/↓) navigate the conversation list and search results
**And** Enter opens the selected conversation or search result
**And** Tab cycles through interactive elements in logical order
**And** focus is trapped within modals (Tab does not escape the modal)

**Given** focus management across views
**When** the user opens a conversation
**Then** focus moves to the message input (desktop) or chat header (mobile)
**When** the user opens search
**Then** focus moves to the search input
**When** a modal closes
**Then** focus returns to the element that triggered the modal

**Given** the application markup
**When** audited
**Then** a skip-to-content link is present as the first focusable element on page load
**And** all focus indicators are visible: 2px solid primary (#128C7E) outline, 2px offset

### Story 9.3: WCAG 2.1 Compliance Audit & Remediation

As a user with accessibility needs,
I want the application to meet WCAG 2.1 Level A with select AA enhancements,
So that I can use simple-chat regardless of how I interact with technology.

**Acceptance Criteria:**

**Given** all implemented components
**When** audited for color contrast (UX-DR26)
**Then** all text/background combinations meet 4.5:1 minimum (NFR21)
**And** primary text (#111B21) on white exceeds 16:1
**And** secondary text (#667781) on white meets 4.7:1
**And** primary teal (#128C7E) on white meets 4.6:1 (if below threshold, fallback #0E7A6E at ~5.2:1 is applied)
**And** color is never the sole indicator of state (unread = bold + badge, online = dot + aria-label, error = red + icon + text)

**Given** all implemented components
**When** audited for ARIA compliance
**Then** chat message lists have `role="log"` with `aria-live="polite"`
**And** conversation list has `role="listbox"` with `aria-selected` on active conversation
**And** unread badges have `aria-label` with full text
**And** presence status is conveyed via `aria-label` on avatars
**And** connection status banner has `role="status"` with `aria-live="assertive"`

**Given** all interactive elements
**When** audited for touch and click targets
**Then** mobile touch targets are minimum 44x44px (WCAG 2.5.5)
**And** desktop click targets are minimum 32x32px
**And** all icons have visible text labels (no icon-only buttons — Experience Principle #1)

**Given** the application with animations
**When** `prefers-reduced-motion` is active
**Then** all transitions are instant (0ms duration)
**And** skeleton screen shimmer animations are replaced with static gray placeholders
**And** no auto-playing or looping animations exist

### Story 9.4: Responsive Design Polish & Breakpoint Verification

As a user on any device,
I want the interface to work correctly across desktop, tablet, and mobile,
So that I have a quality experience regardless of screen size.

**Acceptance Criteria:**

**Given** all implemented features (UX-DR24)
**When** tested on desktop (≥1024px)
**Then** the 3-panel layout displays correctly: sidebar (320px), chat (flex), members (240px toggleable)
**And** message bubbles have max-width 60%
**And** keyboard shortcuts function correctly
**And** hover states are visible on interactive elements

**Given** all implemented features
**When** tested on tablet (768–1023px)
**Then** the 2-panel layout displays: collapsible sidebar (280px) + chat (flex)
**And** the members panel is accessible via toggle in chat header
**And** message bubbles have max-width 70%

**Given** all implemented features
**When** tested on mobile (<768px)
**Then** single-panel navigation works: conversation list OR chat view
**And** back arrow in chat header returns to conversation list
**And** message bubbles have max-width 85%
**And** input is NOT auto-focused on conversation open
**And** all touch targets meet 44x44px minimum
**And** search uses an icon-to-overlay pattern (not persistent in sidebar)

**Given** the minimum viewport (320px — iPhone SE)
**When** the layout renders
**Then** no horizontal scrolling occurs and all content is accessible

**Given** the button hierarchy across all views (UX-DR27)
**When** audited
**Then** maximum one primary button (teal) is visible per context
**And** all buttons have visible text labels
**And** destructive buttons (red) appear only in confirmation dialogs
