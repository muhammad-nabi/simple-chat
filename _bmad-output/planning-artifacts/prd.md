---
stepsCompleted: [step-01-init, step-02-discovery, step-02b-vision, step-02c-executive-summary, step-03-success, step-04-journeys, step-05-domain, step-06-innovation, step-07-project-type, step-08-scoping, step-09-functional, step-10-nonfunctional, step-11-polish, step-12-complete]
inputDocuments:
  - product-brief-simple-chat.md
  - product-brief-simple-chat-distillate.md
documentCounts:
  briefs: 2
  research: 0
  brainstorming: 0
  projectDocs: 0
classification:
  projectType: web_app
  domain: general
  complexity: low-medium
  projectContext: greenfield
workflowType: 'prd'
---

# Product Requirements Document - simple-chat

**Author:** Nabi
**Date:** 2026-03-27

## Executive Summary

simple-chat is a self-hostable real-time chat application for teams that want internal communication without operational overhead. Built on .NET Core and Angular, it delivers private messaging, group chat, file sharing, presence, search, and notifications through a single Docker Compose deployment. It targets small-to-mid-sized teams (5-200 users) who are caught between overengineered open-source platforms with shrinking free tiers and expensive enterprise SaaS — and serves as the first self-hosted chat tool native to the Microsoft ecosystem.

The product bets on deliberate simplicity as a competitive advantage. Competitors have evolved into platforms: Mattermost requires ~4GB RAM and now caps free message history at 10K; Rocket.Chat demands a MongoDB replica set and soft-caps at 25 users; Zulip needs 5 separate services. simple-chat stays a tool — a single `docker-compose up` deployment, under 3GB total for 50 users, from `docker-compose up` to chatting in under 15 minutes.

### What Makes This Special

**The .NET gap is real.** Every self-hosted chat tool today runs on Go, Python, or Node.js. For organizations already operating .NET infrastructure, simple-chat eliminates the need to introduce and maintain foreign runtimes — fitting existing operational tooling, monitoring, and team skills.

**Simplicity is the moat, not a limitation.** No plugin marketplace, no AI assistants, no video conferencing, no omnichannel routing. The feature set is intentionally bounded. Users choose simple-chat because it does less, not in spite of it.

**No artificial constraints.** Full message history, no user caps, no feature gating. Data sovereignty by default — every byte stays on infrastructure the team controls, with no telemetry or external dependencies. Compliance-friendly (GDPR, HIPAA, government) by architecture rather than policy.

## Project Classification

- **Type:** Web application (Angular SPA + PWA, .NET Core modular monolith backend with Clean Architecture + CQRS, MSSQL, SignalR real-time)
- **Domain:** General (internal team communications)
- **Complexity:** Low-medium (straightforward chat domain; real-time messaging, presence, file sharing, and self-hosted deployment add operational complexity)
- **Context:** Greenfield — new product, no existing codebase

## Success Criteria

### User Success

- **Frictionless onboarding:** Teams go from `docker-compose up` to active chatting in under 15 minutes with no specialized infrastructure knowledge required
- **Invisible operations:** After deployment, the application requires zero ongoing maintenance for normal operation — "deploy it, run it, forget about it"
- **Core workflow completeness:** Users never need to leave simple-chat to accomplish basic team communication — private messages, group chat, file sharing, and search cover daily needs without workarounds
- **Responsive across devices:** The web UI works seamlessly across desktop and mobile browsers, with PWA install providing an app-like experience

### Business Success

- **Adoption:** First 100 active deployments within 6 months of public release (measured via GitHub stars, Docker Hub pulls, and community self-reporting — no telemetry)
- **Community visibility:** Listed on Awesome-Selfhosted, visible in .NET community channels (blogs, conferences, r/selfhosted, Hacker News)
- **Community traction:** Active GitHub issue/discussion participation indicating real-world usage and contributor interest
- **Ecosystem positioning:** Recognized as the go-to self-hosted chat option for .NET shops

### Technical Success

- **Real-time performance:** P99 message delivery latency under 200ms via SignalR
- **Resource efficiency:** Total memory footprint under 3GB for teams up to 50 users (app + MSSQL + Redis) — roughly half of Mattermost's typical footprint
- **Deployment simplicity:** Single Docker Compose file with application container, database, and Redis cache — no manual service configuration required
- **Reliability:** Application handles ungraceful restarts without data loss; full message history persistence guaranteed

### Measurable Outcomes

| Metric | Target | Measurement |
|--------|--------|-------------|
| Time to first message | < 15 minutes | From `docker-compose up` to sent message |
| P99 message latency | < 200ms | SignalR delivery measurement |
| Memory (50 users) | < 3GB total | Container resource monitoring (app + MSSQL + Redis) |
| Docker image size | < 500MB | Build artifact |
| Active deployments (6mo) | 100+ | GitHub stars, Docker pulls, community reports |

## User Journeys

### Journey 1: Sarah the Team Lead — "We just need to talk to each other"

**Who:** Sarah runs a 20-person design agency. They've been using Slack's free tier, but the 90-day message history limit means they keep losing project context. She's looked at Mattermost but balked at the infrastructure requirements — they don't have a DevOps person.

**Opening Scene:** Sarah spends 30 minutes searching Slack for a client conversation from four months ago. It's gone. She Googles "simple self-hosted chat" and finds simple-chat on Awesome-Selfhosted.

**Rising Action:** She reads the README, sees "Docker Compose," and decides to try it on their existing Linux box that runs a few internal tools. She runs `docker-compose up`, opens the browser, creates an admin account, and invites her team via a registration link.

**Climax:** Within 15 minutes, three team members are chatting. Sarah searches for a message from last week — instant results. She realizes she'll never lose message history again. No user cap. No feature nag screens.

**Resolution:** Two months later, the entire agency uses simple-chat daily. Sarah hasn't touched the server since deployment. It just works. She recommends it to a freelancer friend who runs a small team.

**Capabilities revealed:** Registration flow, invite mechanism, group chat creation, message search, Docker Compose deployment, zero-maintenance operation.

---

### Journey 2: Marcus the .NET Developer — "Finally, something that fits our stack"

**Who:** Marcus is a senior developer at a mid-sized company running .NET microservices on Azure. The team uses Rocket.Chat internally, and he's the one who maintains it — reluctantly. Every upgrade means dealing with MongoDB replica set issues and Node.js version conflicts that nobody else on the team understands.

**Opening Scene:** Rocket.Chat pushes another breaking change in v7.x that requires a MongoDB migration. Marcus spends half a day fixing it and thinks "there has to be something simpler."

**Rising Action:** He discovers simple-chat, sees ".NET Core + Angular," and immediately clones the repo. The Docker Compose file is straightforward — one app container, one database. He spins it up in their Azure environment alongside their existing .NET services. The application logs look like every other .NET app they run — familiar structured logging, standard health endpoints.

**Climax:** When something looks off in production, Marcus debugs it using the same tools and patterns he uses for every other .NET service. No context-switching to a foreign runtime. He adds it to their existing monitoring dashboard in minutes.

**Resolution:** The team migrates from Rocket.Chat over a week. Marcus removes MongoDB from their infrastructure. The ops burden drops to near-zero because simple-chat is just another .NET service in their pipeline.

**Capabilities revealed:** Standard .NET health endpoints, structured logging, familiar deployment patterns, database simplicity, operational monitoring compatibility.

---

### Journey 3: Alex the Admin — "Deploy and forget, but I need the basics"

**Who:** Alex is an IT generalist at a 50-person company. Not a developer — handles networking, desktops, and a handful of Docker services. The CTO asked them to set up an internal chat tool that the company controls.

**Opening Scene:** Alex reads the simple-chat deployment guide. Three containers: app, database, and Redis — all managed by a single Docker Compose file. No reverse proxy required for basic setup. They copy the Docker Compose file and adjust the environment variables.

**Rising Action:** After deployment, Alex creates the first admin account, sets up a few default groups (General, Engineering, Marketing), and sends the registration URL to the team. A few users sign up with wrong display names — Alex fixes them from the admin panel. One person needs to be deactivated after leaving the company — two clicks.

**Climax:** The CTO asks "do we need to back this up?" Alex checks — it's a single database volume. They add it to their existing backup script. Done.

**Resolution:** Six months later, Alex has logged into the admin panel exactly three times — all for user management. No patches that broke things. No service restarts. The chat tool is the least demanding thing in their infrastructure.

**Capabilities revealed:** Admin panel (user management, role assignment, user deactivation), default group creation, single-volume backup strategy, environment variable configuration, minimal admin surface.

---

### Journey 4: Priya the New Team Member — "Day one, where does the team talk?"

**Who:** Priya just joined a 30-person engineering team as a junior developer. Her onboarding doc says "register on simple-chat for team communication" with a URL.

**Opening Scene:** Priya clicks the registration link, creates an account with her name and email, and sets a password. She lands in the app and immediately sees the team's group channels listed.

**Rising Action:** She joins #engineering and #general. She can see the full message history — scrolls back through last week's discussions to catch up on what the team is working on. She finds a conversation about the codebase she'll be working on, which saves her from asking questions that were already answered.

**Climax:** She needs to ask her assigned mentor a quick question. She starts a private message, shares a screenshot of an error she's seeing, and gets a reply within seconds. The interaction feels instant — no lag, no "connecting..." spinners.

**Resolution:** By end of day one, Priya feels caught up on team context without having scheduled a single meeting. She's already part of three group conversations and knows where to find answers. The tool was invisible — it didn't require a tutorial or onboarding walkthrough.

**Capabilities revealed:** Self-registration, channel discovery and joining, full message history browsing, private messaging, image/file sharing in context, real-time delivery, intuitive UI requiring no training.

---

### Journey Requirements Summary

| Capability Area | Revealed By | Priority |
|----------------|-------------|----------|
| Docker Compose deployment (app + db + Redis) | Sarah, Alex | MVP |
| User registration & invite flow | Sarah, Priya | MVP |
| Group chat (create, join, browse) | Sarah, Alex, Priya | MVP |
| 1-on-1 private messaging | Priya | MVP |
| Full message history & search | Sarah, Priya | MVP |
| File & image sharing | Priya | MVP |
| Real-time message delivery | Priya, Marcus | MVP |
| Admin panel (users, roles, deactivation) | Alex | MVP |
| User presence/status | All | MVP |
| PWA / responsive UI | Sarah | MVP |
| Standard .NET logging & health endpoints | Marcus | MVP |
| Environment variable configuration | Alex | MVP |
| Single-volume backup compatibility | Alex | MVP |
| Notifications | All | MVP |

## Product Scope & Phased Development

### MVP Strategy & Philosophy

**MVP Approach:** Problem-solving MVP — deliver the core communication workflow completely and reliably. The MVP must answer one question: "Can a team deploy this and replace their existing chat tool for daily use?"

This is not a platform MVP or a feature-demo MVP. Every capability in Phase 1 must work well enough that a team of 20 people would use it as their primary chat tool without falling back to Slack or email.

**Resource Requirements:** Small team (1-3 developers). The .NET Core + Angular stack is well-documented with mature tooling. No novel technology risk — the challenge is execution quality, not technical feasibility.

### MVP Feature Set (Phase 1)

**Core User Journeys Supported:**
- Sarah (Team Lead): Deploy, invite team, search history — full journey
- Marcus (.NET Dev): Deploy alongside existing .NET services, standard ops tooling — full journey
- Alex (Admin): Deploy, manage users, backup — full journey
- Priya (New Member): Register, browse history, chat, share files — full journey

**Must-Have Capabilities:**
- User registration and local authentication (email + password)
- 1-on-1 private messaging
- Group chat (create, manage, join/leave, browse)
- Real-time message delivery via SignalR
- User presence/status (online, away, offline)
- In-app notifications with unread counts
- File and image sharing (upload, display, download)
- Full-text message search across all conversations
- Full message history persistence (no limits)
- Responsive web UI (desktop, tablet, mobile breakpoints)
- Basic PWA support (app manifest + service worker caching for fast loads; push notifications deferred to Phase 2)
- Admin panel — tightly scoped: list users, assign roles (admin/member), deactivate/reactivate accounts. No audit logs, usage stats, or bulk operations in MVP.
- Docker Compose deployment (app container + MSSQL + Redis)
- Environment variable configuration
- Standard .NET structured logging and health endpoints
- **Zero-friction upgrade path:** automatic database migrations on application startup; upgrade is `docker-compose pull && docker-compose up -d` — no manual migration steps, no downtime for small teams

### Post-MVP Features

**Phase 2 (Growth):**
- PWA push notifications via service worker
- Webhook integrations for DevOps workflows (CI/CD notifications, monitoring alerts)
- LDAP/Active Directory authentication
- SQLite option for small deployments (< 10 users, single container)
- Message reactions/emoji responses
- Typing indicators
- Message editing and deletion
- User profile customization (avatar, display name, status message)
- Azure Marketplace and DigitalOcean 1-Click deployment templates
- SignalR Redis backplane for multi-instance scaling behind load balancers
- Admin panel enhancements (audit logs, bulk operations, usage stats)

**Phase 3 (Expansion):**
- Data import tools (Slack export, Mattermost migration)
- OAuth/social login providers
- Message threading
- Pinned messages and bookmarks
- Channel categories/organization
- Community governance model for roadmap decisions
- Railway and additional deployment marketplace listings

### Risk Mitigation Strategy

**Technical Risks:**
- *SignalR at scale (50-200 concurrent connections):* Mitigated by using proven SignalR patterns. Load testing at 200 concurrent users is a pre-release gate. Fallback transports (SSE, Long Polling) handle environments where WebSockets are blocked. **Single-instance architecture for MVP** — multi-instance requires sticky sessions or a Redis backplane, explicitly deferred to Phase 2.
- *File storage growth:* Mitigated by configurable max file size. Documented guidance for volume management and backup. No unlimited storage promise — practical defaults with admin visibility.
- *Database choice:* MSSQL as the default eliminates ambiguity. SQLite deferred to Phase 2 to keep MVP deployment path singular and well-tested.
- *Upgrade path:* Mitigated by running Entity Framework migrations automatically on application startup. Upgrade process is a single `docker-compose pull && docker-compose up -d` command. This is a differentiator — competitors are notorious for painful upgrades.
- *Admin panel scope creep:* Mitigated by explicitly bounding MVP admin to three operations: list users, assign roles, deactivate accounts. All enhancements deferred to Phase 2.

**Market Risks:**
- *"Too simple" perception:* Mitigated by positioning — simplicity is the feature, not a limitation. README and landing page must communicate this clearly. Target audience self-selects.
- *Discovery challenge:* Mitigated by multi-channel distribution — Awesome-Selfhosted PR, r/selfhosted posts, .NET community blog posts, Hacker News launch. First 100 deployments is the validation target.

**Resource Risks:**
- *Smaller team than planned:* The MVP feature set is achievable by a single senior .NET/Angular developer. The stack is conventional — no specialized expertise required beyond standard web development.
- *Scope pressure:* The "V1 excludes" list from the product brief is the scope firewall. Webhooks, plugins, video, federation, E2E encryption, LDAP — all explicitly deferred. Any feature request gets checked against this list first.

## Web Application Specific Requirements

### Technical Architecture Considerations

**Application Model:** SPA (Angular) communicating with .NET Core backend via REST API + SignalR WebSocket connections. The backend follows a **modular monolith** architecture using **Clean Architecture** with **CQRS** (Command Query Responsibility Segregation). Logically separated modules (e.g., Chat, Identity, Admin, Files) within a single deployable unit, each with distinct command and query paths. Clean Architecture layers (Domain → Application → Infrastructure → API) enforce dependency inversion and keep business logic independent of frameworks and data access. Modules communicate via in-process interfaces, with the architecture designed to allow future extraction into separate services if scaling demands it.

**Real-Time Architecture:**
- SignalR hub for message delivery, presence updates, and notifications
- WebSocket as primary transport, with Server-Sent Events and Long Polling as automatic fallbacks
- Connection lifecycle management: reconnection handling with exponential backoff, connection state UI indicators
- Presence system: track online/away/offline status via SignalR connection events, broadcast to connected clients

**Authentication Model:**
- Local account registration (email + password) for V1
- JWT or cookie-based session management
- No OAuth/social login in V1 (deferred with LDAP/SSO to post-MVP)

**File Handling:**
- File and image uploads stored on local disk (Docker volume)
- Configurable maximum file size
- Image thumbnails/previews generated server-side for chat display
- Storage backend designed for future extensibility (cloud storage post-MVP)

**Caching:** Redis for application-level caching where needed (e.g., presence state, session data, frequently accessed queries). Redis also serves as the SignalR backplane in Phase 2 for multi-instance scaling, consolidating both caching and real-time infrastructure into a single dependency.

**Database:** MSSQL (SQL Server) for all MVP deployments — native to the .NET ecosystem, consistent with the project's Microsoft-stack positioning. SQLite option for small teams (< 10 users) deferred to Phase 2 to keep the deployment path singular and well-tested.

**Offline & Connection Resilience:** PWA shows clear offline indicator; messages are not queued locally — reconnection re-syncs state from server. SignalR automatic reconnection with exponential backoff ensures transient failures are handled transparently. UI reflects connection state (connected/reconnecting/disconnected).

**Notification Strategy:** In-app notification indicators (unread counts, visual badges) for MVP. Browser push notifications via service worker deferred to Phase 2.

### Browser Support Matrix

| Browser | Minimum Version | Notes |
|---------|----------------|-------|
| Chrome | Latest 2 major | Primary development target |
| Firefox | Latest 2 major | Full support |
| Edge | Latest 2 major | Chromium-based |
| Safari | Latest 2 major | iOS and macOS |
| IE11 | Not supported | — |

### Responsive Design

- **Desktop (1024px+):** Full layout — sidebar with conversation list, main chat area, optional member/detail panel
- **Tablet (768-1023px):** Collapsible sidebar, full chat area
- **Mobile (< 768px):** Single-panel navigation — conversation list or chat view, swipe/tap to switch
- **PWA:** Installable via browser manifest, service worker caching for fast subsequent loads, offline state indicator

### Performance Architecture

Service worker caching enables sub-2s cached page loads. Angular production build targets < 500KB gzipped to keep cold loads under 5s. SignalR WebSocket transport provides sub-200ms message delivery. Full-text search indexed by MSSQL targets sub-1s response times. See NFR1-NFR6 for specific measurable targets.

### Accessibility Architecture

Keyboard navigation for all core workflows (send message, switch conversations, search). ARIA live regions on chat message lists announce incoming messages to screen readers. Programmatic focus management on conversation switches and modal dialogs. See NFR19-NFR23 for specific compliance targets and measurable criteria.

## Functional Requirements

### User Registration & Authentication

- **FR1:** New users can register an account with email, display name, and password
- **FR2:** Registered users can log in with email and password
- **FR3:** Authenticated users can log out from any device
- **FR4:** The system can maintain authenticated sessions across browser refreshes without requiring re-login
- **FR5:** The first user to register can be designated as the instance administrator

### Private Messaging

- **FR6:** Users can initiate a 1-on-1 private conversation with any other registered user
- **FR7:** Users can send text messages in a private conversation
- **FR8:** Users can view the full history of a private conversation by scrolling
- **FR9:** Users can share files and images within a private conversation
- **FR10:** Users can see when a private message has been delivered in real time

### Group Chat

- **FR11:** Users can create a new group conversation with a name
- **FR12:** Users can invite other users to a group conversation
- **FR13:** Users can join existing group conversations that are visible to them
- **FR14:** Users can leave a group conversation
- **FR15:** Users can send text messages in a group conversation
- **FR16:** Users can view the full history of a group conversation by scrolling
- **FR17:** Users can share files and images within a group conversation
- **FR18:** Users can browse a list of available group conversations

### Real-Time Communication

- **FR19:** Users can receive messages in real time without refreshing the page
- **FR20:** Users can see other users' online/away/offline presence status
- **FR21:** Users can set their own presence status (online, away)
- **FR22:** The system can automatically update a user's presence status based on activity/inactivity
- **FR23:** Users can see a visual indicator of their connection state (connected, reconnecting, disconnected)

### Notifications

- **FR24:** Users can see unread message counts per conversation
- **FR25:** Users can receive visual notification indicators for new messages in non-active conversations
- **FR26:** Users can distinguish between conversations with unread messages and those without

### File & Image Sharing

- **FR27:** Users can upload files to a conversation
- **FR28:** Users can upload images to a conversation with in-line preview display
- **FR29:** Users can download files shared in a conversation
- **FR30:** The system can enforce a configurable maximum file size limit on uploads

### Search

- **FR31:** Users can search across all their conversations by message content
- **FR32:** Users can navigate to the original context of a search result
- **FR33:** Users can see which conversation a search result belongs to

### Administration

- **FR34:** Administrators can view a list of all registered users
- **FR35:** Administrators can assign roles to users (admin, member)
- **FR36:** Administrators can deactivate a user account
- **FR37:** Administrators can reactivate a previously deactivated user account
- **FR38:** Administrators can create default group conversations during initial setup

### Deployment & Operations

- **FR39:** Operators can deploy the application using a single Docker Compose file
- **FR40:** Operators can configure the application via environment variables
- **FR41:** The system can run database migrations automatically on application startup
- **FR42:** Operators can upgrade the application by pulling the new image and restarting without manual migration steps
- **FR43:** The system can expose health check endpoints for monitoring
- **FR44:** The system can produce structured logs compatible with standard .NET logging infrastructure
- **FR45:** Operators can back up all application data by backing up the database volume and file upload volume

### Cross-Cutting Capabilities

- **FR46:** Users can access all features through a responsive web interface across desktop, tablet, and mobile browsers
- **FR47:** Users can install the application as a PWA from their browser
- **FR48:** Users can navigate all core workflows using keyboard only
- **FR49:** The system can automatically reconnect after temporary connection loss without user intervention

## Non-Functional Requirements

### Performance

- **NFR1:** Message send-to-display latency must be under 200ms at P99 for up to 200 concurrent connections
- **NFR2:** Full-text search must return results within 1 second for message histories up to 1 million messages
- **NFR3:** Initial page load (cold) must complete within 5 seconds; cached loads within 2 seconds
- **NFR4:** Time to interactive must be under 3 seconds on modern hardware with broadband connection
- **NFR5:** Angular production bundle size (gzipped) must be under 500KB
- **NFR6:** Application must support 200 concurrent WebSocket connections on a single instance without degradation

### Security

- **NFR7:** All passwords must be hashed using a modern adaptive algorithm (e.g., bcrypt, Argon2) — never stored in plaintext
- **NFR8:** All communication between client and server must use TLS (HTTPS/WSS)
- **NFR9:** Authentication tokens must expire and be non-replayable
- **NFR10:** File uploads must be validated for type and size; executable files must be rejected
- **NFR11:** All user inputs must be sanitized to prevent XSS and injection attacks
- **NFR12:** API endpoints must enforce authentication — no unauthenticated access to user data or messages
- **NFR13:** Deactivated users must be immediately unable to access the system or send messages
- **NFR14:** The application must not transmit telemetry, analytics, or any data to external services

### Scalability

- **NFR15:** Total stack memory footprint (app + MSSQL + Redis) must remain under 3GB for deployments up to 50 concurrent users
- **NFR16:** The system must handle teams of 5-200 users on a single instance without architectural changes
- **NFR17:** Database storage growth must scale linearly with message volume — no exponential index bloat
- **NFR18:** File storage must be isolated to a single configurable volume for predictable capacity planning

### Accessibility

- **NFR19:** The application must meet WCAG 2.1 Level A compliance
- **NFR20:** All interactive elements must be reachable and operable via keyboard
- **NFR21:** Text color contrast ratios must meet a minimum of 4.5:1
- **NFR22:** Chat message lists must use ARIA live regions to announce incoming messages to screen readers
- **NFR23:** Focus must be programmatically managed during conversation switches and modal dialogs

### Reliability

- **NFR24:** The application must survive ungraceful container restarts without data loss — all persisted messages must be recoverable from the database
- **NFR25:** SignalR connections must automatically reconnect with exponential backoff after transient failures
- **NFR26:** Database migrations must be idempotent — running the same migration multiple times must produce the same result
- **NFR27:** The application must start and become healthy within 30 seconds of container launch

### Operability

- **NFR28:** The application must be deployable with a single `docker-compose up` command with no additional manual steps
- **NFR29:** All configuration must be manageable via environment variables — no in-container file editing required
- **NFR30:** Structured logs must be written to stdout in JSON format compatible with standard log aggregators
- **NFR31:** Health check endpoints must report application and database connectivity status
- **NFR32:** Application data must be fully recoverable from database volume + file upload volume backups. Redis cache is ephemeral and rebuilds on application restart — it does not require backup.
- **NFR33:** Docker image size must be under 500MB
