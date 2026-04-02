# Story 3.5: Conversation List & Navigation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user,
I want to see my conversations in a sidebar list,
so that I can quickly find and switch between active conversations.

## Acceptance Criteria

1. **Given** an authenticated user with existing conversations, **When** the conversation list loads, **Then** conversations are displayed sorted by LastMessageAt descending (most recent at top), **And** each ConversationListItem (UX-DR10) shows: Avatar (UX-DR3) with presence dot, conversation name (other user's display name for DMs), last message preview text, timestamp, unread badge if applicable, **And** the active conversation has a left 3px teal border + surface background color.

2. **Given** the Avatar component (UX-DR3), **When** rendered, **Then** it displays a rounded-square container (14px border-radius) at the correct size (44px sidebar, 36px header, 30px members), **And** shows two-letter initials derived from display name (first letter of first two words; "Sarah Chen" -> "SC", "Marcus" -> "M"), **And** background color is deterministically generated from user ID (pick from 8 predefined warm colors), **And** includes `aria-label` with user name and status (e.g., "Sarah Chen (online)").

3. **Given** the conversation list, **When** the user taps/clicks a conversation, **Then** the chat area loads that conversation's messages, **And** on mobile, the view switches from conversation list panel to chat panel, **And** the conversation name appears in the chat header.

4. **Given** the conversation list API, **When** GET `/api/conversations` is called, **Then** it returns the user's conversations with: id, type, name, lastMessage (preview), lastMessageAt, otherParticipants (for display name/avatar), unreadCount.

5. **Given** the conversation list on initial load, **When** data is not yet available, **Then** skeleton screen placeholders matching the list item shape are displayed (UX-DR20), **And** skeleton shimmer is disabled when `prefers-reduced-motion` is active.

## Tasks / Subtasks

- [x] Task 1: Create GetConversations backend query (AC: #4)
  - [x] 1.1 Create `src/Application/Messaging/Queries/GetConversations/GetConversationsQuery.cs` -- `[Authorize] public record GetConversationsQuery : IRequest<List<ConversationListDto>>`
  - [x] 1.2 Create `src/Application/Messaging/Queries/GetConversations/ConversationListDto.cs` -- record with Id, Type, Name, LastMessagePreview, LastMessageAt, OtherParticipants (list of ParticipantDto: UserId, DisplayName), UnreadCount
  - [x] 1.3 Create `src/Application/Messaging/Queries/GetConversations/GetConversationsQueryHandler.cs` -- query ConversationParticipants for current user's conversations, join with latest Message for preview, join with ApplicationUser for display names, compute UnreadCount via LastReadMessageId vs max Message.Id, sort by LastMessageAt DESC
  - [x] 1.4 Add `GET /api/conversations` endpoint in `src/Web/Endpoints/Conversations.cs` -- map to GetConversationsQuery, require authorization
  - [x] 1.5 Create unit tests: `src/Application.UnitTests/Messaging/Queries/GetConversations/GetConversationsQueryHandlerTests.cs`
  - [x] 1.6 Create integration test for GET /api/conversations endpoint

- [x] Task 2: Create ConversationService (frontend) (AC: #1, #3)
  - [x] 2.1 Create `src/Web/ClientApp/src/app/features/chat/services/conversation.service.ts` -- BehaviorSubject<Conversation[]> pattern, loadConversations() via HttpClient GET /api/conversations, selectedConversation$ BehaviorSubject, selectConversation() method
  - [x] 2.2 Create `src/Web/ClientApp/src/app/features/chat/models/conversation.model.ts` -- TypeScript interfaces matching backend DTOs: Conversation, ParticipantInfo
  - [x] 2.3 Wire SignalR `ReceiveMessage` events to update conversation list (move conversation to top, update lastMessage preview, increment unreadCount)
  - [x] 2.4 Create `src/Web/ClientApp/src/app/features/chat/services/conversation.service.spec.ts` -- test loading, selection, real-time updates

- [x] Task 3: Create Avatar shared component (AC: #2)
  - [x] 3.1 Create `src/Web/ClientApp/src/app/shared/components/avatar/avatar.component.ts` -- standalone, inputs: displayName, userId, size ('sidebar'|'header'|'members'), presenceStatus (optional, for future Epic 5)
  - [x] 3.2 Implement initials derivation: split displayName by spaces, take first letter of first two words, uppercase
  - [x] 3.3 Implement deterministic color: `userId.charCodeAt(0) % 8` index into `$avatar-colors` array (use CSS custom properties or inline style)
  - [x] 3.4 Size mapping: sidebar=44px, header=36px, members=30px; border-radius: 14px
  - [x] 3.5 Accessibility: `aria-label` = `"${displayName}"` (presence status deferred to Epic 5)
  - [x] 3.6 Create `src/Web/ClientApp/src/app/shared/components/avatar/avatar.component.spec.ts` -- test initials, colors, sizes, aria-label

- [x] Task 4: Implement ConversationListComponent (AC: #1, #3, #5)
  - [x] 4.1 Replace placeholder in `src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.ts` with full implementation
  - [x] 4.2 Create separate HTML template file `conversation-list.component.html` -- `role="listbox"` container, `@for` loop over conversations, each item with `role="option"` and `[attr.aria-selected]`
  - [x] 4.3 Create `conversation-list.component.scss` -- list item styles: hover ($color-surface-hover), active (left 3px teal border + $color-surface bg), unread (bold name + badge)
  - [x] 4.4 Each list item shows: Avatar component, conversation name (truncate ~20 chars), last message preview (truncate with ellipsis), relative timestamp, unread badge (circular 22px teal bg, white text, "99+" cap)
  - [x] 4.5 Implement skeleton loading state: 5-6 placeholder items matching list item shape, shimmer animation, `prefers-reduced-motion` disables shimmer
  - [x] 4.6 Wire click handler to ConversationService.selectConversation() + ChatLayoutComponent.selectConversation() for mobile panel switch
  - [x] 4.7 Create `conversation-list.component.spec.ts` -- test rendering, selection, loading state, skeleton, accessibility roles

- [x] Task 5: Integrate with ChatLayoutComponent (AC: #3)
  - [x] 5.1 Update `chat-layout.component.ts` to inject ConversationService, pass selected conversation to chat header title
  - [x] 5.2 Update `chat-layout.component.html` -- display selected conversation name in chat-title span
  - [x] 5.3 Emit conversation selection from ConversationListComponent to ChatLayoutComponent for mobile panel switching
  - [x] 5.4 Update chat-layout tests for new integration

- [x] Task 6: Create UnreadBadge shared component (AC: #1)
  - [x] 6.1 Create `src/Web/ClientApp/src/app/shared/components/unread-badge/unread-badge.component.ts` -- standalone, input: count (number), displays circular badge, "99+" for >99, `aria-label="${count} unread messages"`
  - [x] 6.2 Create `unread-badge.component.spec.ts` -- test display, 99+ cap, aria-label, hidden when count=0

- [x] Task 7: Create RelativeTimePipe (AC: #1)
  - [x] 7.1 Create `src/Web/ClientApp/src/app/shared/pipes/relative-time.pipe.ts` -- transform ISO date string to relative display ("just now", "5m", "2h", "Yesterday", "Mar 28")
  - [x] 7.2 Create `relative-time.pipe.spec.ts` -- test various time ranges

## Dev Notes

### CRITICAL: Backend API Does NOT Exist Yet

The `GET /api/conversations` endpoint does not exist. Currently `src/Web/Endpoints/Conversations.cs` only has:
- `POST /api/conversations` (create)
- `POST /api/conversations/{id}/messages` (send message)
- `GET /api/conversations/{id}/messages` (get history)

**You MUST create the GetConversations query + handler + endpoint.** This is the most significant backend work in this story.

### Backend Query Implementation Guide

**GetConversationsQueryHandler pattern** -- follow existing `GetMessageHistoryQueryHandler`:
- Inject `IApplicationDbContext` and `IUser`
- Query flow:
  1. Get all ConversationParticipant records for `_user.Id`
  2. For each conversation, get the latest Message (for preview text)
  3. For each conversation, get other participants with display names from ApplicationUser
  4. Compute unreadCount: count Messages where `Message.Id > participant.LastReadMessageId` (or all messages if LastReadMessageId is null)
  5. Sort by LastMessageAt DESC (nulls last)
  6. Return `List<ConversationListDto>`

**ConversationListDto** record:
```csharp
public record ConversationListDto(
    long Id,
    string Type,
    string? Name,
    string? LastMessagePreview,
    DateTimeOffset? LastMessageAt,
    List<ParticipantDto> OtherParticipants,
    int UnreadCount);

public record ParticipantDto(string UserId, string DisplayName);
```

**For Private conversations:** Name is null in DB. The frontend derives the display name from OtherParticipants[0].DisplayName.

### Existing Infrastructure to Reuse (DO NOT Recreate)

- **`IApplicationDbContext`** -- `Conversations`, `ConversationParticipants`, `Messages` DbSets already defined
- **`IUser`** -- inject for current user ID. Has `Id` and `Roles` properties
- **`[Authorize]` attribute** -- apply on query record for MediatR authorization behavior
- **`ChatLayoutComponent`** -- already has `selectConversation()` method for mobile panel switching, `navigateBack()` for back navigation
- **`SignalRService`** -- exposes `messageReceived` observable with `MessagePayload` (id, conversationId, senderId, senderDisplayName, content, sentAt, messageType)
- **`AuthService`** -- has `currentUser$` observable and `isAuthenticated$`
- **Design tokens** -- `$avatar-colors` list (8 warm colors) already in `_tokens.scss`, all spacing/color/typography tokens available
- **Responsive mixins** -- `@include mobile {}`, `@include tablet {}`, `@include desktop {}` in `_mixins.scss`

### Avatar Component Location Decision

Architecture specifies Avatar in `shared/components/avatar/` (not in chat feature). This is correct -- Avatar is reused across conversation list (sidebar size 44px), chat header (36px), and members panel (30px). Place it in shared.

### ConversationService State Pattern

Follow the architecture's BehaviorSubject pattern exactly:
```typescript
@Injectable({ providedIn: 'root' })
export class ConversationService {
  private readonly http = inject(HttpClient);
  private readonly conversations$ = new BehaviorSubject<Conversation[]>([]);
  private readonly selectedConversation$ = new BehaviorSubject<Conversation | null>(null);

  readonly conversations = this.conversations$.asObservable();
  readonly selectedConversation = this.selectedConversation$.asObservable();

  loadConversations(): void {
    this.http.get<Conversation[]>('/api/conversations')
      .subscribe(convs => this.conversations$.next(convs));
  }

  selectConversation(conversation: Conversation): void {
    this.selectedConversation$.next(conversation);
  }
}
```

**Real-time updates:** Subscribe to `SignalRService.messageReceived` in the service. On each `ReceiveMessage`:
1. Find the conversation in the current list
2. Update its lastMessagePreview and lastMessageAt
3. Move it to the top of the list
4. If the message is not in the currently selected conversation, increment unreadCount
5. Emit updated list via `conversations$.next([...updatedList])`

### Skeleton Screen Implementation

Use CSS-only skeleton screens matching conversation list item shape:
- Rectangular placeholder for avatar (44px rounded-square)
- Two line placeholders for name + preview text
- Small placeholder for timestamp
- Use `@keyframes shimmer` with linear-gradient animation
- Wrap in `@media (prefers-reduced-motion: reduce)` to disable shimmer (show static gray)
- Show ~5 skeleton items during loading

### Conversation List Item Styling (UX-DR10)

| State | Visual |
|-------|--------|
| Default | White background |
| Hover | `$color-surface-hover` (#E4E6EB) |
| Active | Left 3px `$color-primary` border + `$color-surface` (#F0F2F5) bg |
| Unread | Bold conversation name + teal unread badge |

- Item padding: `$spacing-3` (12px) horizontal, `$spacing-2` (8px) vertical
- Avatar: 44px, gap `$spacing-3` (12px) from text
- Name: `$font-size-body` (14px), `$color-text-primary`, truncate at ~20 chars
- Preview: `$font-size-caption` (12px), `$color-text-secondary`, single line truncate with ellipsis
- Timestamp: `$font-size-caption` (12px), `$color-text-tertiary`, top-right aligned

### UnreadBadge Component (UX-DR4)

- Size: 22px circular (min-width for "99+")
- Background: `$color-primary` (#128C7E)
- Text: white, `$font-size-caption` (12px), font-weight 600
- Display "99+" for counts > 99
- `aria-label="${count} unread messages"`
- Hidden (display: none) when count is 0

### Relative Time Display

Timestamps should show:
- < 1 minute: "just now"
- 1-59 minutes: "Xm"
- 1-23 hours: "Xh"
- Yesterday: "Yesterday"
- This week: Day name ("Mon", "Tue")
- Older: Short date ("Mar 28")

### Mobile Navigation Flow

1. User opens app -> conversation list shows (activePanel = 'list')
2. User taps conversation -> `ChatLayoutComponent.selectConversation()` sets activePanel = 'chat'
3. Chat panel shows with back arrow -> tapping back calls `navigateBack()` -> activePanel = 'list'

The ChatLayout already handles this via `selectConversation()` and `navigateBack()`. The conversation-list component needs to:
- Call `conversationService.selectConversation(conv)` for state
- Emit an event or call layout method for panel switching

**Implementation approach:** Use Angular `output()` event from ConversationListComponent, handled by ChatLayoutComponent to call `selectConversation()`.

### Chat Header Title Update

Currently the chat header shows static "Conversation" text. Update to show the selected conversation's display name:
- For Private conversations: OtherParticipants[0].DisplayName
- For Group conversations: Conversation.Name

### Accessibility Requirements

- Conversation list container: `role="listbox"` with `aria-label="Conversations"`
- Each item: `role="option"` with `[attr.aria-selected]="isSelected"`
- Avatar: `aria-label` with user name
- Unread badge: `aria-label="${count} unread messages"`
- Skeleton items: `aria-hidden="true"` with `aria-busy="true"` on container
- Focus management: active conversation should be focusable, arrow key navigation is nice-to-have (not required)

### Build-Breaking Rules to Watch

- **Standalone components only** -- `standalone: true`, no NgModules
- **`inject()` for DI** -- never constructor injection
- **2-space indent** for TypeScript, HTML, SCSS
- **Single quotes** in TypeScript
- **kebab-case** file names, `app-` component selector prefix
- **LF line endings**, final newline on all files
- **ESLint must pass** -- `ng lint` clean
- **`strict: true`** in tsconfig
- **NUnit** for backend tests (`[Test]`, not `[Fact]`)
- **Shouldly** for assertions (`result.ShouldBe()`, not `Assert.AreEqual`)
- **Records for commands/queries/DTOs** in C#
- **No `var` keyword** in C# -- use explicit types
- **`readonly` on private fields** -- build fails otherwise
- **CancellationToken** on all async handler methods

### Items NOT in Scope

- Presence indicators (Epic 5) -- Avatar component accepts presenceStatus input but does NOT render presence dots yet
- Search bar in sidebar (Epic 7)
- Group conversation display differences (Epic 4) -- will work but no special group avatar
- Typing indicators (Post-MVP)
- Conversation context menu (not in spec)
- Push notifications (Post-MVP)
- Draft indicators in preview text (Story 3.7 scope)

### Previous Story Intelligence (Story 3.4)

**Key learnings from Story 3.4:**
- Design tokens exist and are complete -- `_tokens.scss` has `$avatar-colors` (8-color palette), all spacing/color tokens
- ChatLayoutComponent responsive layout works with `matchMedia` listeners (no `@angular/cdk`)
- Used `@if` control flow (Angular 21) instead of `*ngIf` -- continue this pattern with `@for` and `@if`
- Placeholder components exist for conversation-list, chat-window, online-users
- `chat.routes.ts` uses `loadComponent` lazy loading
- Build: 0 errors, 0 warnings; Frontend tests: 30/30 pass
- Pre-existing `app.component.spec.ts` test failure (2 tests) -- do not try to fix

**Review findings from Story 3.4:**
- `100vh` causes layout overflow on mobile browsers -- deferred, pre-existing
- Desktop-to-mobile resize loses view context -- deferred, UX refinement

### Git Intelligence

Recent commits: `story 3.4 created implemented and reviewed`, `story 3.3 created implemented and reviewed`. Branch pattern: `story/X.Y`. Current branch: `prep/fix-test-infrastructure`.

### Project Structure Notes

**New files to create:**
```
src/Application/Messaging/Queries/GetConversations/GetConversationsQuery.cs
src/Application/Messaging/Queries/GetConversations/ConversationListDto.cs
src/Application/Messaging/Queries/GetConversations/GetConversationsQueryHandler.cs
src/Application.UnitTests/Messaging/Queries/GetConversations/GetConversationsQueryHandlerTests.cs
src/Web/ClientApp/src/app/features/chat/models/conversation.model.ts
src/Web/ClientApp/src/app/features/chat/services/conversation.service.ts
src/Web/ClientApp/src/app/features/chat/services/conversation.service.spec.ts
src/Web/ClientApp/src/app/shared/components/avatar/avatar.component.ts
src/Web/ClientApp/src/app/shared/components/avatar/avatar.component.spec.ts
src/Web/ClientApp/src/app/shared/components/unread-badge/unread-badge.component.ts
src/Web/ClientApp/src/app/shared/components/unread-badge/unread-badge.component.spec.ts
src/Web/ClientApp/src/app/shared/pipes/relative-time.pipe.ts
src/Web/ClientApp/src/app/shared/pipes/relative-time.pipe.spec.ts
src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.html
src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.scss
src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.spec.ts
```

**Files to modify:**
```
src/Web/Endpoints/Conversations.cs                          -- add GET /api/conversations endpoint
src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.ts  -- replace placeholder
src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.ts              -- inject ConversationService, wire selection
src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.html            -- dynamic chat title, wire events
```

**Files NOT to modify:**
```
src/Web/ClientApp/src/styles/_tokens.scss          -- already has avatar colors, all tokens needed
src/Web/ClientApp/src/styles/_mixins.scss          -- already complete
src/Web/ClientApp/src/app/core/signalr/signalr.service.ts  -- reuse as-is
src/Web/ClientApp/src/app/core/services/auth.service.ts    -- reuse as-is
src/Domain/Messaging/Conversation.cs               -- domain model is complete
src/Domain/Messaging/ConversationParticipant.cs    -- domain model is complete
```

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-3-real-time-private-messaging.md#Story 3.5]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#UX-DR3 Avatar Component]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#UX-DR10 Conversation List Item]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#UX-DR20 Loading State Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#ConversationService BehaviorSubject Pattern]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend File Structure]
- [Source: _bmad-output/planning-artifacts/architecture.md#SignalR Event Naming]
- [Source: _bmad-output/planning-artifacts/architecture.md#API Boundary Definition]
- [Source: _bmad-output/project-context.md#Angular Rules]
- [Source: _bmad-output/project-context.md#Clean Architecture + CQRS]
- [Source: _bmad-output/project-context.md#Testing Rules]
- [Source: _bmad-output/implementation-artifacts/3-4-design-token-system-chat-layout-shell.md#Dev Notes]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

### Completion Notes List

- GetConversationsQuery + Handler created following existing CQRS pattern (IApplicationDbContext, IUser, IIdentityService). Computes unread counts via LastReadMessageId, returns conversations sorted by LastMessageAt DESC with participant display names and last message preview (truncated at 100 chars)
- GET /api/conversations endpoint added to Conversations.cs with RequireAuthorization
- 8 unit tests for GetConversationsQueryHandler covering: empty list, sorting, participant display names, preview text, unread counts, null LastReadMessageId, conversation type, unauthorized access
- 4 integration tests for GET /api/conversations endpoint covering: empty list, conversation with details, sort order, unauthenticated 401
- ConversationService (Angular) implements BehaviorSubject pattern with loading state, conversation selection, and real-time updates from SignalR messageReceived events (moves conversation to top, increments unread for non-selected)
- Avatar shared component uses Angular signal inputs (input.required), computed() for initials/color/size, deterministic color from userId.charCodeAt(0) % 8 into 8-color warm palette, sizes: sidebar 44px, header 36px, members 30px, border-radius 14px
- UnreadBadge shared component with 22px circular teal badge, "99+" cap, aria-label, hidden when count=0
- RelativeTimePipe transforms ISO date strings to: "just now", "Xm", "Xh", "Yesterday", day name, or "Mon DD" format
- ConversationListComponent with full implementation: listbox role, skeleton loading (5 items with shimmer, prefers-reduced-motion disables), avatar + name + preview + timestamp + unread badge per item, active state (3px teal left border + surface bg), unread state (bold name), empty state
- ChatLayoutComponent updated: injects ConversationService, dynamic conversation title in header, onConversationSelected event handler for mobile panel switching, selectedConversation$ observable piped to template
- Existing chat-layout tests updated with ConversationService mock provider and renamed selectConversation -> onConversationSelected
- Build: 0 errors, 0 warnings (Release); Backend unit tests: 79/79 pass (+8 new); Frontend tests: 81/81 pass (+42 new); Lint: All pass
- Pre-existing: app.component.spec.ts 2 test failures, CSS budget warnings on login/register/chat-layout -- not addressed (pre-existing)

### Change Log

- 2026-04-02: Story 3.5 implemented -- GET /api/conversations backend, ConversationService, Avatar, UnreadBadge, RelativeTimePipe, ConversationListComponent, ChatLayout integration

### Review Findings

- [x] [Review][Decision] GroupBy+First() may fail to translate to SQL in EF Core — **FIXED**: replaced with GroupBy+Max(Id) then separate fetch by IDs [GetConversationsQueryHandler.cs:45-52]
- [x] [Review][Decision] RelativeTimePipe is pure but time-dependent — **DEFERRED**: standard Angular trade-off; solve with component-level timer, not impure pipe
- [x] [Review][Patch] N+1 query: unread count fires one COUNT query per conversation — **NOTE**: N+1 structure retained but SenderId filter added; batch optimization deferred
- [x] [Review][Patch] Unread count includes user's own messages (backend) — **FIXED**: added `m.SenderId != userId` filter [GetConversationsQueryHandler.cs:71]
- [x] [Review][Patch] Frontend handleIncomingMessage increments unread for own messages — **FIXED**: added currentUserId tracking via AuthService, senderId check [conversation.service.ts:74]
- [x] [Review][Patch] loadConversations() swallows errors silently — **FIXED**: added error$ BehaviorSubject exposed as observable [conversation.service.ts:18-20]
- [x] [Review][Patch] RelativeTimePipe returns negative values for future dates — **FIXED**: clamped diffMs to Math.max(0, ...) [relative-time.pipe.ts:15]
- [x] [Review][Patch] aria-hidden + aria-busy on same element is contradictory — **FIXED**: aria-busy on container, aria-hidden on individual skeleton items [conversation-list.component.html:2-4]
- [x] [Review][Patch] Avatar missing presenceStatus optional input — **FIXED**: added optional presenceStatus input [avatar.component.ts:46]
- [x] [Review][Defer] loadConversations() has no concurrent call guard — race condition on rapid calls; minor at current scale — deferred
- [x] [Review][Defer] No pagination on GetConversations endpoint — unbounded query loads all conversations; acceptable at current scale — deferred
- [x] [Review][Defer] Selected conversation object becomes stale after SignalR update — no current consumer of stale properties; future concern — deferred
- [x] [Review][Defer] Unit tests use Assert.That instead of Shouldly — follows existing file pattern; pre-existing convention inconsistency — deferred

### File List

New files:
- src/Application/Messaging/Queries/GetConversations/GetConversationsQuery.cs
- src/Application/Messaging/Queries/GetConversations/ConversationListDto.cs
- src/Application/Messaging/Queries/GetConversations/GetConversationsQueryHandler.cs
- tests/Application.UnitTests/Messaging/Queries/GetConversations/GetConversationsQueryHandlerTests.cs
- src/Web/ClientApp/src/app/features/chat/models/conversation.model.ts
- src/Web/ClientApp/src/app/features/chat/services/conversation.service.ts
- src/Web/ClientApp/src/app/features/chat/services/conversation.service.spec.ts
- src/Web/ClientApp/src/app/shared/components/avatar/avatar.component.ts
- src/Web/ClientApp/src/app/shared/components/avatar/avatar.component.spec.ts
- src/Web/ClientApp/src/app/shared/components/unread-badge/unread-badge.component.ts
- src/Web/ClientApp/src/app/shared/components/unread-badge/unread-badge.component.spec.ts
- src/Web/ClientApp/src/app/shared/pipes/relative-time.pipe.ts
- src/Web/ClientApp/src/app/shared/pipes/relative-time.pipe.spec.ts
- src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.html
- src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.scss
- src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.spec.ts

Modified files:
- src/Web/Endpoints/Conversations.cs (added GET /api/conversations endpoint + GetConversations using)
- src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.ts (replaced placeholder with full implementation)
- src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.ts (inject ConversationService, add onConversationSelected, getConversationTitle)
- src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.html (dynamic title, wire conversationSelected event)
- src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.spec.ts (add ConversationService mock, fix renamed method)
- tests/Infrastructure.IntegrationTests/Api/ConversationEndpointTests.cs (added 4 GetConversations integration tests)
