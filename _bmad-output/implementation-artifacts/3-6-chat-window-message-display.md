# Story 3.6: Chat Window & Message Display

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user,
I want to see messages displayed as conversation bubbles with full scrollable history,
so that I can read current and past messages naturally.

## Acceptance Criteria

1. **Given** an active conversation, **When** the ChatWindow component renders (UX-DR11), **Then** the message list container has `role="log"` and `aria-live="polite"`, **And** new messages are announced to screen readers without interrupting current reading, **And** history loads via infinite scroll are NOT announced (only new real-time messages).

2. **Given** messages in the conversation, **When** the MessageBubble component renders each message (UX-DR8), **Then** own messages display with teal (#128C7E) background, white text, right-aligned, border-radius 16px with 4px bottom-right, **And** other users' messages display with white background, dark text, left-aligned, subtle shadow (0 1px 3px rgba(0,0,0,0.08)), border-radius 16px with 4px bottom-left, **And** sender name is shown in teal above other users' messages (for future group support), **And** timestamp is shown on each message in 12px caption style, **And** same-sender consecutive messages use 4px gap with no repeated sender name; different-sender messages use 12px gap with sender name shown.

3. **Given** the user is viewing a conversation and is scrolled to the bottom, **When** a new message arrives via SignalR, **Then** the chat window auto-scrolls to show the new message.

4. **Given** the user has scrolled up to read history, **When** a new message arrives via SignalR, **Then** a "New messages ↓" indicator appears at the bottom of the chat window (UX-DR29), **And** tapping the indicator scrolls to the bottom, **And** the user is NOT force-scrolled.

5. **Given** the user scrolls to the top of loaded messages, **When** more history exists (hasMore = true), **Then** a loading spinner appears at the top, **And** older messages are fetched using cursor-based pagination and prepended, **And** scroll position is preserved (user doesn't jump).

6. **Given** the user switches away from a conversation and returns, **When** the conversation is re-rendered, **Then** the previous scroll position is restored.

7. **Given** a conversation is loading for the first time, **When** messages are being fetched, **Then** skeleton screen placeholders matching bubble shapes are displayed (UX-DR20).

## Tasks / Subtasks

- [x] Task 1: Create MessageService (frontend) (AC: #1, #3, #4, #5)
  - [x] 1.1 Create `src/Web/ClientApp/src/app/features/chat/services/message.service.ts` -- BehaviorSubject<Message[]> pattern, `loadMessages(conversationId, before?)` via HttpClient GET `/api/conversations/{id}/messages`, `messages$` observable, `loading$` observable, `hasMore$` observable, `addMessage(message)` for real-time
  - [x] 1.2 Create `src/Web/ClientApp/src/app/features/chat/models/message.model.ts` -- TypeScript interface matching backend MessageDto: id, conversationId, senderId, senderDisplayName, content, sentAt, messageType
  - [x] 1.3 Wire SignalR `messageReceived` events to append new messages when matching active conversation
  - [x] 1.4 Implement scroll position cache: Map<conversationId, scrollTop> for AC #6
  - [x] 1.5 Create `src/Web/ClientApp/src/app/features/chat/services/message.service.spec.ts` -- test loading, pagination, real-time append, conversation switch

- [x] Task 2: Create MessageBubble component (AC: #2)
  - [x] 2.1 Create `src/Web/ClientApp/src/app/features/chat/components/message-bubble/message-bubble.component.ts` -- standalone, inputs: message (Message), isOwn (boolean), showSender (boolean), isConsecutive (boolean)
  - [x] 2.2 Create `message-bubble.component.html` -- own messages right-aligned (teal bg, white text), other messages left-aligned (white bg, dark text, shadow), sender name in teal (when showSender=true), timestamp in caption style
  - [x] 2.3 Create `message-bubble.component.scss` -- bubble styling using design tokens: `$color-bubble-own`, `$color-bubble-other`, `$border-radius-bubble` (16px), modified bottom corner (4px), `$font-size-caption` for timestamps, grouping gaps (4px consecutive, 12px different sender)
  - [x] 2.4 Create `message-bubble.component.spec.ts` -- test own vs other styling, sender name visibility, timestamp, consecutive grouping

- [x] Task 3: Implement ChatWindow component (AC: #1, #3, #4, #5, #6, #7)
  - [x] 3.1 Replace placeholder in `src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts` with full implementation -- inject MessageService, ConversationService, AuthService, subscribe to selectedConversation$ to load messages on switch
  - [x] 3.2 Create `chat-window.component.html` -- `role="log"` container with `aria-live="polite"`, `@for` loop over messages with MessageBubble components, skeleton loading state, "New messages ↓" indicator, empty state, loading spinner at top for history
  - [x] 3.3 Create `chat-window.component.scss` -- scrollable container (overflow-y auto, `$color-chat-bg` #FAFBFC background), message list padding, "new messages" indicator styling (fixed at bottom), skeleton bubble placeholders
  - [x] 3.4 Implement auto-scroll: detect if user is at bottom (within 50px threshold), auto-scroll on new message if at bottom, show "New messages ↓" indicator if scrolled up
  - [x] 3.5 Implement infinite scroll up: detect scroll to top, call MessageService.loadMessages with cursor, preserve scroll position after prepend (record scrollHeight before, restore after)
  - [x] 3.6 Implement scroll position save/restore per conversation via MessageService cache
  - [x] 3.7 Implement message grouping logic: compare consecutive messages' senderId to determine isConsecutive and showSender
  - [x] 3.8 Create `chat-window.component.spec.ts` -- test message rendering, auto-scroll, new message indicator, skeleton loading, empty state, scroll position preservation

- [x] Task 4: Wire ChatWindow into ChatLayout (AC: #1)
  - [x] 4.1 Update `chat-layout.component.html` to pass selected conversation context to ChatWindow (if not already wired)
  - [x] 4.2 Ensure ChatWindow only renders messages when a conversation is selected, shows "Select a conversation" placeholder otherwise

## Dev Notes

### Backend API Already Exists -- No Backend Work

The message history endpoint already exists and is fully functional:
- `GET /api/conversations/{conversationId}/messages?before={cursor}&limit={n}` -- cursor-based pagination
- Returns `MessageHistoryResponse` with `Messages` (list of MessageDto), `HasMore` (bool), `NextCursor` (long?)
- Each `MessageDto` has: id, conversationId, senderId, senderDisplayName, content, sentAt, messageType
- Default limit: 50, max: 200
- Messages returned in **descending** ID order (newest first) -- the frontend must **reverse** for display (oldest at top, newest at bottom)

### MessageService State Pattern

Follow the architecture's BehaviorSubject pattern exactly (same as ConversationService):
```typescript
@Injectable({ providedIn: 'root' })
export class MessageService {
  private readonly http = inject(HttpClient);
  private readonly signalRService = inject(SignalRService);
  private readonly authService = inject(AuthService);

  private readonly _messages$ = new BehaviorSubject<Message[]>([]);
  private readonly _loading$ = new BehaviorSubject<boolean>(false);
  private readonly _hasMore$ = new BehaviorSubject<boolean>(false);
  private readonly _error$ = new BehaviorSubject<string | null>(null);

  readonly messages$ = this._messages$.asObservable();
  readonly loading$ = this._loading$.asObservable();
  readonly hasMore$ = this._hasMore$.asObservable();
  readonly error$ = this._error$.asObservable();

  // Scroll position cache: conversationId -> scrollTop
  private readonly scrollPositions = new Map<number, number>();

  loadMessages(conversationId: number, before?: number): void { ... }
  addMessage(message: Message): void { ... }
  clearMessages(): void { ... }
  saveScrollPosition(conversationId: number, scrollTop: number): void { ... }
  getScrollPosition(conversationId: number): number | undefined { ... }
}
```

**Critical: Message ordering** -- The API returns messages in DESC order. For initial load, reverse them for display. For pagination (loading older messages), fetch with `before` cursor, reverse the batch, and prepend to existing messages.

**Real-time updates:** Subscribe to `SignalRService.messageReceived` in the service. On each `ReceiveMessage`:
1. Check if the message belongs to the currently loaded conversation
2. If yes, append it to the messages list (it's newer than anything in the list)
3. If no, ignore (ConversationService handles the conversation list update)

### Message Model Interface

```typescript
export interface Message {
  id: number;
  conversationId: number;
  senderId: string;
  senderDisplayName: string;
  content: string;
  sentAt: string;
  messageType: string;
}

export interface MessageHistoryResponse {
  messages: Message[];
  hasMore: boolean;
  nextCursor: number | null;
}
```

### MessageBubble Component Design (UX-DR8)

| Property | Own Message | Other Message |
|----------|-----------|---------------|
| Alignment | Right | Left |
| Background | `$color-bubble-own` (#128C7E) | `$color-bubble-other` (#FFFFFF) |
| Text Color | `$color-bubble-own-text` (#FFFFFF) | `$color-bubble-other-text` (#111B21) |
| Border Radius | 16px 16px 4px 16px | 16px 16px 16px 4px |
| Shadow | None | 0 1px 3px rgba(0,0,0,0.08) |
| Sender Name | Never shown | Shown in teal when showSender=true |
| Timestamp | 12px caption, rgba(255,255,255,0.7) | 12px caption, `$color-text-secondary` |

**Grouping Rules:**
- Same sender, consecutive: 4px gap (`$spacing-xs`), no sender name repeated
- Different sender: 12px gap (`$spacing-md`), sender name shown above first message
- Determine by comparing current message's senderId with previous message's senderId

**Accessibility:** Each message `<div>` within `role="log"` container. `aria-label` includes sender, content snippet, and time.

### ChatWindow Scrolling Implementation

**Auto-scroll detection:**
```typescript
// In chat-window component
private isNearBottom(): boolean {
  const el = this.scrollContainer.nativeElement;
  return el.scrollHeight - el.scrollTop - el.clientHeight < 50;
}
```

**Scroll position preservation on history load (AC #5):**
```typescript
// Before loading older messages
const previousScrollHeight = el.scrollHeight;
// After messages prepended
const newScrollHeight = el.scrollHeight;
el.scrollTop = newScrollHeight - previousScrollHeight;
```

**"New messages ↓" indicator (AC #4):**
- Show when `!isNearBottom() && newMessagesCount > 0`
- Clicking scrolls to bottom with smooth behavior
- Reset counter when user scrolls to bottom

**Scroll position save/restore (AC #6):**
- On conversation switch: save current scrollTop to MessageService.scrollPositions map
- On conversation load: after messages render, restore saved scrollTop (or scroll to bottom for first load)

### Skeleton Screen Implementation (AC #7)

Use CSS-only skeleton screens matching bubble shapes:
- 3-5 skeleton bubbles alternating left/right alignment
- Rectangular placeholders with bubble border-radius
- Varying widths (40%-70%) to simulate natural message lengths
- Use `@keyframes shimmer` with linear-gradient animation
- Wrap in `@media (prefers-reduced-motion: reduce)` to disable shimmer
- Container: `aria-busy="true"`, individual skeletons: `aria-hidden="true"`

### Existing Infrastructure to Reuse (DO NOT Recreate)

- **`SignalRService`** -- exposes `messageReceived` observable with `MessagePayload` (id, conversationId, senderId, senderDisplayName, content, sentAt, messageType). Already subscribed to by ConversationService for list updates
- **`ConversationService`** -- `selectedConversation$` observable drives which conversation's messages to load. `clearSelection()` available
- **`AuthService`** -- `currentUser$` observable has `userId` property to determine own vs other messages
- **`RelativeTimePipe`** -- already created in Story 3.5, reuse for message timestamps if desired (but message timestamps may need different format -- consider using `DatePipe` for "HH:mm" format within same day)
- **Design tokens** -- all bubble colors, spacing, border-radius, typography tokens already in `_tokens.scss`
- **Responsive mixins** -- `@include mobile {}`, `@include tablet {}`, `@include desktop {}` in `_mixins.scss`
- **`ChatLayoutComponent`** -- already renders `<app-chat-window>` in the main content area. Has `selectedConversation$` piped to template

### ChatWindow Component Structure

The chat-window currently lives at `src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts` as a placeholder. Replace it entirely.

The component should:
1. Subscribe to `ConversationService.selectedConversation$` -- when it changes, load messages for new conversation (save scroll position of old, load new)
2. Subscribe to `MessageService.messages$` -- render message bubbles
3. Subscribe to `SignalRService.messageReceived` via MessageService (handled in service layer)
4. Manage scroll container via `@ViewChild` + `ElementRef`
5. Use `AfterViewChecked` or mutation observer to handle auto-scroll after new messages render
6. Handle scroll events for: infinite scroll up, auto-scroll detection, "new messages" indicator

### Message Timestamp Format

For message bubbles, use a more detailed time format than the conversation list:
- Same day: "HH:mm" (e.g., "14:32")
- Yesterday: "Yesterday HH:mm"
- This year: "MMM d, HH:mm" (e.g., "Mar 28, 14:32")
- Older: "MMM d, yyyy HH:mm"

Use Angular's `DatePipe` or a custom pipe. Do NOT reuse `RelativeTimePipe` -- its compact format ("5m", "2h") is designed for sidebar conversation timestamps, not message bubbles.

### Build-Breaking Rules to Watch

- **Standalone components only** -- `standalone: true`, no NgModules
- **`inject()` for DI** -- never constructor injection
- **2-space indent** for TypeScript, HTML, SCSS
- **Single quotes** in TypeScript
- **kebab-case** file names, `app-` component selector prefix
- **LF line endings**, final newline on all files
- **ESLint must pass** -- `ng lint` clean
- **`strict: true`** in tsconfig -- explicit types, null checks
- **`async` pipe** in templates -- avoid manual `.subscribe()` in components where possible
- **Immutable state updates** -- use spread/concat for BehaviorSubject updates, never mutate

### Items NOT in Scope

- Message input / sending (Story 3.7)
- Optimistic send with checkmarks (Story 3.7)
- Draft persistence (Story 3.7)
- Typing indicators (Post-MVP)
- Message editing/deletion (Post-MVP)
- File/image message display (Epic 6)
- System messages (future)
- Read receipts / marking as read (not in Story 3.6 scope -- deferred)
- Presence indicators in messages (Epic 5)
- "New messages ↓" showing actual count (just show indicator)

### Previous Story Intelligence (Story 3.5)

**Key learnings from Story 3.5:**
- ConversationService uses BehaviorSubject pattern with `_loading$`, `_error$` observables -- follow same pattern for MessageService
- Avatar component exists in `shared/components/avatar/` with inputs: displayName, userId, size, presenceStatus -- can be imported if needed in message bubbles (not required per spec, but available)
- UnreadBadge component exists in `shared/components/unread-badge/`
- RelativeTimePipe exists in `shared/pipes/relative-time.pipe.ts`
- Angular `@if` and `@for` control flow syntax (Angular 21) -- continue using, not `*ngIf`/`*ngFor`
- Pre-existing `app.component.spec.ts` test failure (2 tests) -- do not try to fix
- ConversationService tracks `currentUserId` from AuthService for filtering own messages -- MessageService should do same

**Review findings from Story 3.5:**
- GroupBy+First() failed to translate to SQL -- not relevant to frontend
- `loadConversations()` had error swallowing -- fixed by adding `_error$` BehaviorSubject. Apply same pattern to MessageService
- `aria-hidden` + `aria-busy` contradiction -- use `aria-busy` on container, `aria-hidden` on individual skeleton items

### Git Intelligence

Recent commits follow pattern: `story X.Y created implemented and reviewed`. Branch pattern: `story/X.Y`. Current branch: `prep/fix-test-infrastructure`.

Files from Story 3.5 that established frontend patterns:
- `conversation.service.ts` -- BehaviorSubject state management template
- `conversation-list.component.ts` -- skeleton loading pattern, accessibility roles
- `avatar.component.ts` -- standalone component with signal inputs pattern
- `chat-layout.component.ts` -- panel management, selectedConversation$ integration

### Project Structure Notes

**New files to create:**
```
src/Web/ClientApp/src/app/features/chat/models/message.model.ts
src/Web/ClientApp/src/app/features/chat/services/message.service.ts
src/Web/ClientApp/src/app/features/chat/services/message.service.spec.ts
src/Web/ClientApp/src/app/features/chat/components/message-bubble/message-bubble.component.ts
src/Web/ClientApp/src/app/features/chat/components/message-bubble/message-bubble.component.html
src/Web/ClientApp/src/app/features/chat/components/message-bubble/message-bubble.component.scss
src/Web/ClientApp/src/app/features/chat/components/message-bubble/message-bubble.component.spec.ts
src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.html
src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.scss
src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.spec.ts
```

**Files to modify:**
```
src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts  -- replace placeholder with full implementation
```

**Files NOT to modify:**
```
src/Web/ClientApp/src/styles/_tokens.scss          -- already has all bubble colors and tokens
src/Web/ClientApp/src/styles/_mixins.scss           -- already complete
src/Web/ClientApp/src/app/core/signalr/signalr.service.ts  -- reuse as-is
src/Web/ClientApp/src/app/core/services/auth.service.ts    -- reuse as-is
src/Web/ClientApp/src/app/features/chat/services/conversation.service.ts  -- reuse as-is
src/Web/Endpoints/Conversations.cs                  -- GET messages endpoint already exists
src/Application/Messaging/Queries/GetMessageHistory/ -- backend query complete
```

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-3-real-time-private-messaging.md#Story 3.6]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Message Bubble]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Chat Window]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Message Bubble Colors (Direction B)]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Loading State Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Code Naming]
- [Source: _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines]
- [Source: _bmad-output/planning-artifacts/architecture.md#Loading States]
- [Source: _bmad-output/project-context.md#Angular Rules]
- [Source: _bmad-output/project-context.md#Testing Rules]
- [Source: _bmad-output/implementation-artifacts/3-5-conversation-list-navigation.md#Dev Notes]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

### Completion Notes List

- Message model (message.model.ts) with Message and MessageHistoryResponse interfaces matching backend DTOs
- MessageService implements BehaviorSubject pattern with messages$, loading$, hasMore$, error$ observables. Loads messages via HTTP GET with cursor-based pagination, reverses DESC-ordered API responses for chronological display. Subscribes to SignalR messageReceived to append real-time messages for active conversation only. Includes scroll position cache (Map<conversationId, scrollTop>) for conversation switch restoration
- MessageBubbleComponent: standalone component with inputs for message, isOwn, showSender, isConsecutive. Own messages right-aligned with teal bg/white text, other messages left-aligned with white bg/dark text/shadow. Sender name shown in teal above other users' first messages. Custom timestamp formatting (HH:mm today, Yesterday HH:mm, Mon d HH:mm, full date for older). Grouping via consecutive class (4px gap) vs default (12px gap)
- ChatWindowComponent: replaced placeholder with full implementation. Subscribes to ConversationService.selectedConversation$ with pairwise for conversation switch handling (saves scroll position of previous, loads new). Message list with role="log" aria-live="polite". Auto-scroll on new messages when near bottom (50px threshold). "New messages" indicator when scrolled up and new message arrives. Infinite scroll up with scroll position preservation (records scrollHeight before prepend, restores after). Skeleton loading with 5 alternating left/right bubble placeholders, shimmer animation, prefers-reduced-motion support, aria-busy/aria-hidden separation
- Fixed pre-existing test infrastructure issue: setup-jest.ts was importing zone setup but not calling setupZoneTestEnv() — all TestBed-based tests were broken. Fixed by calling the function
- Fixed pre-existing chat-layout.component.spec.ts: added matchMedia mock for jsdom environment to prevent removeEventListener errors on component destroy
- Build: 0 errors, 0 warnings (Release); Backend unit tests: 79/79 pass; Frontend tests: 130/130 pass (+34 new); Lint: All pass

### Change Log

- 2026-04-02: Story 3.6 implemented -- MessageService, MessageBubble component, ChatWindow component, test infrastructure fixes

### Review Findings

- [x] [Review][Patch] Stale HTTP responses corrupt messages on fast conversation switching [message.service.ts:loadMessages] — Fixed: added conversationId guard in HTTP callback + cancel previous request on new initial load
- [x] [Review][Patch] isLoadingHistory never reset on HTTP error, permanently blocking history loads [chat-window.component.ts:loadOlderMessages] — Fixed: added loadingHistory$ observable to service, component subscribes to reset flag on error
- [x] [Review][Patch] Scroll position never restored when returning to a conversation (AC6) [chat-window.component.ts:convSub] — Fixed: now calls getScrollPosition() and uses shouldRestoreScroll flag; shouldScrollToBottom only set when no saved position exists
- [x] [Review][Patch] aria-live="polite" announces history-load prepends to screen readers (AC1) [chat-window.component.html:message-list] — Fixed: dynamic aria-live toggles to "off" during history loads + added aria-relevant="additions"
- [x] [Review][Patch] Loading spinner never displays during pagination (AC5) [message.service.ts:loadMessages] — Fixed: added _loadingHistory$ BehaviorSubject set during pagination; template uses loadingHistory instead of loading for spinner
- [x] [Review][Patch] formatTimestamp called on every change detection cycle (performance) [message-bubble.component.html:timestamp] — Fixed: extracted to pure MessageTimestampPipe, used in template via pipe syntax
- [x] [Review][Patch] Infinite scroll triggers automatically on short message lists [chat-window.component.ts:onScroll] — Fixed: added el.scrollHeight > el.clientHeight guard so scroll-up trigger only fires when content overflows
- [x] [Review][Patch] Scroll position preservation missing original scrollTop [chat-window.component.ts:ngAfterViewChecked] — Fixed: now captures previousScrollTop alongside previousScrollHeight; formula includes both
- [x] [Review][Defer] aria-busy absent during incremental history loading [chat-window.component.html:loading-spinner] — deferred, enhancement: loading spinner has aria-label but no aria-busy on the scroll container during pagination fetches

### File List

New files:
- src/Web/ClientApp/src/app/features/chat/models/message.model.ts
- src/Web/ClientApp/src/app/features/chat/services/message.service.ts
- src/Web/ClientApp/src/app/features/chat/services/message.service.spec.ts
- src/Web/ClientApp/src/app/features/chat/components/message-bubble/message-bubble.component.ts
- src/Web/ClientApp/src/app/features/chat/components/message-bubble/message-bubble.component.html
- src/Web/ClientApp/src/app/features/chat/components/message-bubble/message-bubble.component.scss
- src/Web/ClientApp/src/app/features/chat/components/message-bubble/message-bubble.component.spec.ts
- src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.html
- src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.scss
- src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.spec.ts

Modified files:
- src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts (replaced placeholder with full implementation)
- src/Web/ClientApp/setup-jest.ts (fixed: call setupZoneTestEnv() to initialize TestBed environment)
- src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.spec.ts (added matchMedia mock for jsdom)
