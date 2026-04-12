# Deferred Work

## Deferred from: code review of story-1-1 (2026-03-28)

- CORS AllowAny* not environment-gated in Program.cs — production CORS hardening belongs to Story 1.2/security
- ~~Duplicate creation timestamps: BaseEntity.CreatedAt (DateTime) vs BaseAuditableEntity.Created (DateTimeOffset) — reconcile when entities created in Stories 2.x/3.x~~ **RESOLVED** (prep-standardize-timestamps, 2026-03-29): Standardized to DateTimeOffset on BaseEntity.CreatedAt, removed redundant Created from BaseAuditableEntity
- DB initializer calls EnsureDeletedAsync then EnsureCreatedAsync — destroys data every startup, Story 1.3 covers proper DB initialization
- ~~Hardcoded admin password "Administrator1!" in ApplicationDbContextInitialiser — template default, should be externalized before production~~ **RESOLVED** (Story 2.1, 2026-03-30): Seed user removed entirely; first-user admin designation via RegisterCommand
- .GetAwaiter().GetResult() in DispatchDomainEventsInterceptor sync path — deadlock risk under load
- Domain events dispatched before SaveChanges commits — side-effects fire for uncommitted state
- Domain event handler exception leaves events partially cleared — no retry possible for failed events
- Role authorization doesn't trim whitespace in comma-separated roles — "Admin, Editor" split produces " Editor" with leading space
- Seed user creation result not checked — silent failure if password policy rejects
- Logout endpoint body check logic inverted — any JSON body passes, no body returns 401
- PerformanceBehaviour Stopwatch not reset between calls — elapsed time accumulates across requests
- Auditable entity interceptor writes null UserId for system/background operations
- Logging behaviour logs entire request object including potential PII (passwords, tokens)
- ExceptionHandler middleware registered after static files middleware — file-serving exceptions bypass custom handler
- provideHttpClient() missing XSRF/fetch options — address when auth is implemented (Story 2.2)
- Production bundle budget thresholds too permissive (1MB warn / 5MB error vs Angular default 500kB / 1MB)
- No dark-mode token set — picoColorScheme script implied dark mode was planned but no token overrides exist

## Deferred from: code review of story-1-2 (2026-03-29)

- Dockerfile has no USER directive — app runs as root in container; add non-root user for security hardening
- Special characters in MSSQL_SA_PASSWORD (e.g., `"`, `$`, `;`) could break the MSSQL healthcheck quoting or the connection string delimiter parsing
- ~~Redis has no authentication (requirepass not set) — any container on the network can read/write; add auth before production~~ **RESOLVED** (prep-redis-requirepass, 2026-03-29): Added requirepass via Docker Compose command, password in .env, propagated through connection strings
- JWT default secret is a committed known value in docker-compose.yml — enforce secret validation when JWT auth is implemented (Story 2.2)
- AC5 upgrade path requires `image:` tag for registry pull — add when CI/CD pipeline pushes to a container registry (Story 1.5)

## Deferred from: code review of story-1-3 (2026-03-29)

- ~~Seed data hardcoded password "Administrator1!" now runs in all environments (not just Development) — externalization deferred to Story 2.x per spec~~ **RESOLVED** (Story 2.1, 2026-03-30): Seed user removed entirely; first-user admin designation via RegisterCommand

## Deferred from: code review of story-1-4 (2026-03-29)

- Redis connection string read independently in two DI files (Infrastructure/DependencyInjection.cs + Web/DependencyInjection.cs) — DRY violation across project boundaries, risk of divergence if one fallback changes
- No explicit timeout on DB/Redis health checks — Docker curl timeout (3s) could expire before ASP.NET health check completes if dependency is slow but alive; operational tuning concern
- PII safety test in RequestLoggerTests uses brittle negative string match (`!v.ToString()!.Contains("TestRequest {")`) — depends on serialization format, consider positive assertion

## Deferred from: code review of story-1-5 (2026-03-29)

- Redis `ConnectionMultiplexer` instance registered via `AddSingleton(instance)` in CustomWebApplicationFactory won't be disposed by DI — cosmetic in test context since process exits after tests and Testcontainer is disposed independently

## Deferred from: prep-standardize-timestamps review (2026-03-29)

- First domain entity migration will expose `BaseEntity.CreatedAt` as a new non-nullable `datetimeoffset` column — ensure migration includes appropriate default value or make column nullable during Story 2.1 migration generation

## Deferred from: prep-redis-requirepass review (2026-03-29)

- Redis password passed via `--requirepass` CLI arg is visible in `docker inspect` and process list — use Docker secrets or config file for production hardening
- Redis connection string with password embedded — special characters in future passwords could break StackExchange.Redis `ConfigurationOptions.Parse()` delimiter parsing

## Deferred from: code review of story-2-1 (2026-03-30)

- TOCTOU race condition on first-user-admin designation — two concurrent registrations on empty DB can both get Admin role; no transaction/lock/constraint. Single-instance MVP, sub-millisecond window; revisit if multi-instance deployment
- Redis failure after user creation orphans user record — user exists in SQL but has no session and cannot re-register; Story 2.2 login endpoint provides fallback
- CORS `SetIsOriginAllowed(_ => true)` + `AllowCredentials()` allows any origin to make cookie-bearing requests — Story 2.2 covers CORS hardening
- Auth state in memory-only BehaviorSubject lost on page refresh — Story 2.3 covers session persistence and token refresh
- Hardcoded JWT secret `CHANGE-THIS-IN-PRODUCTION-min-32-chars!!` in appsettings.json with no startup validation — Story 2.2 covers JWT secret validation
- Non-atomic Redis session operations in `StoreSessionAsync` (3 separate commands) — orphaned tracking key is cosmetic, session key itself works; low risk for MVP
- No rate limiting on `/api/auth/register` endpoint — anonymous endpoint allows unlimited account creation and email enumeration; cross-cutting concern for all public endpoints
- Email enumeration via duplicate-email error message — spec (AC #3) explicitly requires "An account with this email already exists"; accept as design decision
- JWT issuer/audience validation disabled — spec says single-instance app, skip validation; revisit if multi-instance deployment needed
- No refresh endpoint — refresh token stored in HttpOnly cookie and Redis but no `/api/auth/refresh` endpoint exists; Story 2.3 covers this
- BcryptPasswordHasher never returns `SuccessRehashNeeded` — no bcrypt work factor migration needed for MVP; revisit if work factor is increased

## Deferred from: code review of story-2-2 (2026-03-30)

- Client logout doesn't invalidate server session or clear HttpOnly cookie — AuthService.logout() only clears in-memory state; Redis session and refresh token cookie persist for up to 7 days. Story 2.4 covers logout endpoint.
- ResetDatabaseAsync hardcoded SQL (`DELETE FROM [AspNetUserRoles]; DELETE FROM [AspNetUsers]`) will break with FK violations when domain tables referencing Users are added (e.g., ChatMessages in Epic 3). Update when domain entities are created.
- Refresh token expiry (7 days) hardcoded independently in 4 locations (LoginCommandHandler, RegisterCommandHandler, SetRefreshTokenCookie x2) with no shared constant or config value — risk of silent drift if one is changed without updating others.

## Deferred from: code review of story-2-3 (2026-03-30)

- `logout()` does not call server-side session invalidation — client-only state clearing leaves Redis session alive; Story 2.4 covers logout endpoint
- Race condition (TOCTOU) in refresh token rotation — concurrent requests with same token can both succeed; sub-millisecond window, negligible for single-instance MVP. Fix with Redis WATCH/MULTI transaction or distributed lock when scaling to multi-instance.

## Deferred from: code review of story-2-4 (2026-03-30)

- No logging in `LogoutCommandHandler` when Redis fails — handler has no ILogger; if `InvalidateSessionAsync` throws, exception propagates as 500 with no application-level log. Consistent with existing pattern (LoginCommandHandler also lacks logging).

## Deferred from: code review of story-3-1 (2026-04-02)

- `ConversationParticipant.LastReadMessageId` can reference a `Message.Id` from a different conversation — no FK constraint or application-level guard exists; future unread-tracking implementation (Epic 5) must validate that `LastReadMessageId` belongs to the same `ConversationId`
- Content/FileId cross-field invariants not enforced at data layer — a `Text` message can have empty content, a `File` message can have null `FileId`; Story 3.2 command handlers must enforce `MessageType`-specific validation rules

## Deferred from: review of prep-fix-test-infrastructure (2026-03-31)

- `ResetDatabaseAsync` dynamic SQL: if DELETE phase throws, NOCHECK CONSTRAINT ALL remains disabled for remainder of test session — FK enforcement silently off for subsequent tests. Low practical risk (DELETE on empty Identity tables won't fail), but no transaction/error-handling wrapper exists. Consider wrapping in TRY/CATCH/ROLLBACK if test suite grows complex.

## Deferred from: code review of story-3-2 (2026-04-02)

- Inconsistent CancellationToken on pre-existing IIdentityService methods — new methods (GetDisplayNamesByIdsAsync, UserExistsAsync) accept CancellationToken but older methods (FindUserByIdAsync, CheckPasswordAsync, etc.) do not. Standardize when touching these methods next.
- Race condition in CreateConversation — check-then-create for private conversations has no DB-level uniqueness constraint. Sub-millisecond window on single-instance MVP. Proper fix: unique composite index on private conversation user pairs; add during Epic 4 (group conversations) or dedicated hardening pass.

## Deferred from: code review of story-3.4 (2026-04-02)

- `100vh` causes layout overflow on mobile browsers (iOS Safari, Chrome Android) — address bar overlap hides bottom content. Modern fix: `100dvh`. Address during Epic 9 responsive polish (Story 9.4).
- Desktop-to-mobile resize loses view context — user viewing chat on desktop gets sent to conversation list when resizing to mobile because `activePanel` defaults to `'list'`. UX refinement for responsive polish.

## Deferred from: code review of story-3.3 (2026-04-02)

- No message-gap fill or conversation-list refresh on reconnect (AC4) — explicitly deferred per Task 5.3 to Stories 3.4-3.7 when MessageService and conversation state management are built

## Deferred from: code review of story-3.5 (2026-04-02)

- `loadConversations()` has no concurrent call guard — multiple rapid calls (e.g., multiple SignalR messages for unknown conversations) fire parallel HTTP requests with no cancellation; race condition where older response overwrites newer. Minor at current scale.
- No pagination on GetConversations endpoint — unbounded query loads all conversations, participants, and latest messages. Acceptable at current scale; add cursor/offset pagination when conversation counts grow.
- Selected conversation object becomes stale after SignalR update — `_selectedConversation$` holds old reference while `_conversations$` gets updated copy. No current consumer of stale properties but will cause bugs when future code reads `selectedConversation$` for `unreadCount` or `lastMessagePreview`.
- Unit tests (GetConversationsQueryHandlerTests) use NUnit `Assert.That` instead of Shouldly — follows existing test file patterns in this project; pre-existing convention inconsistency.

## Deferred from: code review of story-3.6 (2026-04-02)

- `aria-busy` absent during incremental history loading — loading spinner has `aria-label` but no `aria-busy` on the scroll container during pagination fetches; accessibility enhancement for screen reader users

## Deferred from: code review of story-3.7 (2026-04-02)

- senderDisplayName hardcoded to empty string in optimistic message [message.service.ts:136] — optimistic message uses `senderDisplayName: ''`; in private chats own messages don't show sender so invisible, but future group chats will show blank name. Server echo is suppressed by pendingMessageIds so it never gets corrected.
- isFirstMessageInConversation doesn't check loaded history [message.service.ts:127-129] — method returns true for any conversation not yet in firstMessageSent map, even if history has prior messages from this user. Celebrate animation fires incorrectly. Needs broader fix: seed from loaded message history or track server-side.

## Deferred from: code review of story-3.8 (2026-04-02)

- Reload `GET /api/conversations` after conversation create can overwrite newer state pushed by SignalR [conversation.service.ts:82-97] — same pattern as pre-existing `loadConversations()` race condition deferred in story 3.5; fix both together with merge-based state update or `switchMap`
- `DisplayName` nullable from DB could cause frontend `.toLowerCase()` crash in filter [IdentityService.cs:186] — pre-existing data integrity concern; coalesce to `UserName` or `Id` in the EF projection

## Deferred from: code review of story-3.9 (2026-04-02)

- `loadConversations()` has no in-flight request cancellation [conversation.service.ts:51-64] — unlike MessageService.loadMessages() which cancels prior requests, loadConversations() creates overlapping HTTP subscriptions on rapid reconnects; pre-existing pattern (see also story 3.5 deferred item)

## Deferred from: code review of 4-1-group-conversation-creation (2026-04-03)

- Mutable `List<string>` in record command (`CreateConversationCommand.ParticipantIds`) should be `IReadOnlyList<string>` — breaks record value semantics; pre-existing pattern across codebase commands
- No group conversation duplicate check — unlike private conversations, groups with identical name and members can be created repeatedly; acceptable for MVP, revisit if user feedback indicates issue
- No loading/spinner state on Create Group button during submission — spec calls for "Loading state on submit (spinner in button)" but risk is low since dialog closes on success; add as UX polish pass

## Deferred from: code review of 4-2-group-messaging-history (2026-04-03)

- System message not published via MediatR notification (no real-time delivery to other participants) — becomes relevant when SignalR group-join for non-creators is addressed
- Non-creator participants don't auto-join SignalR group for new conversations — pre-existing architecture limitation from Epic 3
- System message appears in `lastMessagePreview` on conversation list sidebar — pre-existing query behavior in GetConversationsQueryHandler
- No in-flight guard for concurrent create conversation requests — pre-existing pattern, same as deferred in story 3.5 and 3.8
- System message counted as unread for non-creator participants — `GetConversationsQueryHandler` counts system messages in unread count; defer to Epic 5 (Story 5-3) which overhauls unread logic

## Deferred from: code review of 4-3-browse-join-groups (2026-04-03)

- Concurrent join race condition — no DB unique constraint on (ConversationId, UserId); two simultaneous join requests can both pass `alreadyParticipant` check and create duplicate participant rows; requires schema migration to add unique index
- No pagination on `GetBrowseGroupsQuery` — loads all non-joined group conversations into memory; acceptable for small team app (5-200 users) per spec; revisit if group count grows significantly
- SignalR join error silently swallowed in `conversation.service.ts` joinGroup() — user won't receive real-time messages until reconnect; pre-existing pattern (same as story 3.9 deferred item)
- Null ordering instability in `GetBrowseGroupsQueryHandler` — groups with null `LastMessageAt` sort unpredictably; edge case for newly created groups with no messages
- No error state display in browse groups UI — API failure shows empty list instead of error message; low impact UX polish item
- No SignalR broadcast for "joined the group" system message — no domain event or MediatR notification published; same gap as "created the group" deferred from story 4-2

## Deferred from: code review of 4-4-invite-users-leave-group (2026-04-03)

- Invited user never joins SignalR group in current session — no mechanism to add invited user's active connection to SignalR group; pre-existing pattern (no domain event broadcast for V1); user must reconnect to receive real-time messages
- InviteToGroup validates user existence with N+1 identity service calls — `UserExistsAsync` called per user in a loop; batch API would reduce round-trips; performance optimization for large invite lists
- Concurrent double-leave can create duplicate system messages — no unique constraint on (ConversationId, UserId) removal; same deferred pattern as join race in story 4-3
- Invite picker uses stale existingMemberIds snapshot — member list captured when panel opens, not refreshed when invite dialog opens; backend silently filters already-participants so no data corruption
- GetGroupMembers has no pagination — loads all participants unbounded; acceptable for small groups, revisit if group sizes grow
- Leave system message SenderId set to leaving user's ID instead of null/sentinel — display uses Content string not SenderId for system messages; cosmetic inconsistency

## Deferred from: review of spec-fix-message-loading-state (2026-04-12)

- Reconnect handler in chat-window.component.ts calls `loadMessages()` without `clearMessages()` first — `pendingMessageIds` from pre-disconnect optimistic sends not cleared; reconnect-delivered duplicates may be silently dropped
- `clearMessages()` wipes `firstMessageSent` map for ALL conversations — not scoped to current conversation; `isFirstMessageInConversation()` returns true incorrectly after any conversation switch
- Same-conversation re-selection triggers unnecessary `clearMessages()` + `loadMessages()` — no `prev.id === current.id` guard in pairwise subscription; causes flash of empty state
- `clearMessages()` clears `pendingMessageIds` while optimistic-send `.then()` / `.catch()` callbacks are still in-flight — message may appear as duplicate after switching away and back
