# Epic List

## Epic 1: Project Foundation & Deployment Infrastructure
Operators can deploy, configure, monitor, upgrade, and back up simple-chat using a single Docker Compose command.
**FRs covered:** FR39, FR40, FR41, FR42, FR43, FR44, FR45
**Key deliverables:** Jason Taylor template scaffold, post-scaffold customizations (SignalR, Redis, module folders, service interfaces), Docker Compose (app + MSSQL 512MB cap + Redis), Serilog JSON logging, three-tier health checks, env var configuration, documented upgrade and backup paths.

## Epic 2: User Authentication & Account Management
Users can register, log in, maintain sessions across refreshes, and log out. The first registered user is designated as the instance administrator.
**FRs covered:** FR1, FR2, FR3, FR4, FR5
**Key deliverables:** Identity module, JWT + Redis session validation, refresh token rotation (HttpOnly cookie), bcrypt password hashing, Angular auth interceptor with refresh flow, Login component (UX-DR15), Register component (UX-DR16), first-user admin designation, rate limiting on auth endpoints.

## Epic 3: Real-Time Private Messaging
Users can have 1-on-1 real-time text conversations with full message history, with visual connection state feedback and automatic reconnection.
**FRs covered:** FR6, FR7, FR8, FR10, FR19, FR23, FR49
**Key deliverables:** Messaging module (Conversation entity with type discriminator, Message entity with bigint ID, ConversationParticipant), cursor-based message pagination, SignalR ChatHub with persist-first flow, MediatR command/notification pipeline, design token system (UX-DR1, UX-DR2), Chat Layout (UX-DR12), Conversation List Item (UX-DR10), Message Bubble (UX-DR8), Message Input (UX-DR9), Chat Window (UX-DR11), Avatar component (UX-DR3), Connection Status Banner (UX-DR6), optimistic send with checkmarks (UX-DR19), skeleton screens (UX-DR20), empty states (UX-DR22), conversation creation for DMs (UX-DR23 — single-select), draft persistence (UX-DR21), "New messages" indicator (UX-DR29), auto-reconnect with exponential backoff, session expiry UX (UX-DR28).

## Epic 4: Group Conversations
Users can create named group conversations, invite others, join/leave groups, browse available groups, and chat in groups with full history.
**FRs covered:** FR11, FR12, FR13, FR14, FR15, FR16, FR18
**Key deliverables:** Group conversation type support, group creation flow extending conversation creation (UX-DR23 — multi-select with group name prompt), join/leave mechanics, browse groups sidebar entry, system messages (group created, user joined/left), sender names displayed in group bubbles (UX-DR8 group variant).

## Epic 5: Presence & Notifications
Users can see who's online/away/offline and receive unread message counts and visual indicators for new messages across conversations.
**FRs covered:** FR20, FR21, FR22, FR24, FR25, FR26
**Key deliverables:** Presence module (Redis heartbeat 60s + TTL 90s, online_users Set), presence polling (GET /api/presence/online every 30s), client-side 5-min inactivity auto-away, Presence Indicator component (UX-DR4), Unread Badge component (UX-DR5), LastReadMessageId tracking on ConversationParticipant, online users panel (members panel), bold unread conversation names, conversation sort by LastMessageAt.

## Epic 6: File & Image Sharing
Users can upload, preview, and download files and images in any conversation, with configurable size limits and server-side thumbnail generation.
**FRs covered:** FR9, FR17, FR27, FR28, FR29, FR30
**Key deliverables:** Files module (FileAttachment entity, IFileStorageService with local disk implementation), SkiaSharp thumbnail generation, file type whitelist + magic byte validation + executable rejection, configurable max file size, File Preview component (UX-DR7), attach button integration in Message Input, upload progress indicator, file size hint near attach button.

## Epic 7: Search & Discovery
Users can search across all their conversations by message content, see which conversation results belong to, and navigate directly to the original message in context.
**FRs covered:** FR31, FR32, FR33
**Key deliverables:** MSSQL Full-Text Index on Message.Content, IMessageSearchService implementation, Search Bar component with Ctrl/Cmd+K shortcut (UX-DR13), Search Results with progressive rendering (UX-DR14), GetMessagesAround query for context jump, highlighted match with 3s fade, search state preservation on navigation.

## Epic 8: Administration
Administrators can view all users, assign roles, deactivate/reactivate accounts with immediate effect, and create default group conversations for team onboarding.
**FRs covered:** FR34, FR35, FR36, FR37, FR38
**Key deliverables:** Admin sidebar section (visible only to admins), User List component (UX-DR17), User Detail / deactivation confirmation dialog (UX-DR18), role assignment dropdown with descriptions, deactivation with Redis session delete for immediate lockout, reactivation, default group creation during initial setup.

## Epic 9: PWA, Accessibility & Cross-Cutting Polish
Users can install the app as a PWA, and all features meet responsive design and accessibility compliance standards through comprehensive audit and refinement.
**FRs covered:** FR46, FR47, FR48
**Key deliverables:** PWA manifest + service worker for cached loads and offline indicator, comprehensive keyboard navigation audit (UX-DR25), WCAG 2.1 Level A compliance audit, responsive breakpoint matrix polish (UX-DR24), skip-to-content link, prefers-reduced-motion final pass, touch target audit, contrast verification, button hierarchy consistency check (UX-DR27). Note: basic accessibility and responsive behavior are built into each preceding epic; this epic is the quality gate and final compliance verification.
