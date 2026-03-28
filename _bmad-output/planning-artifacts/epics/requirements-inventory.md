# Requirements Inventory

## Functional Requirements

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

## NonFunctional Requirements

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

## Additional Requirements

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

## UX Design Requirements

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

## FR Coverage Map

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
