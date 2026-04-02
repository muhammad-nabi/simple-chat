# Story 3.9: Connection Status & Auto-Reconnect

Status: done

## Story

As a user,
I want to see my connection state and have the app reconnect automatically,
so that I'm never confused about whether my messages are being delivered.

## Acceptance Criteria

### AC 1: Banner Hidden When Connected
- **Given** the ConnectionStatusBanner component (UX-DR6)
- **When** the SignalR connection is healthy
- **Then** the banner is hidden (not rendered)

### AC 2: Brief Interruption Debounce (<3 Seconds)
- **Given** a brief connection interruption (<3 seconds)
- **When** SignalR reconnects within 3 seconds
- **Then** no banner is ever shown (3-second debounce prevents flicker anxiety)

### AC 3: Reconnecting Banner (>3 Seconds)
- **Given** a connection interruption lasting more than 3 seconds
- **When** SignalR is attempting to reconnect
- **Then** an amber banner appears at the top of the chat area: "Reconnecting..."
- **And** the banner has `role="status"` and `aria-live="assertive"`
- **And** the banner pushes content down (not an overlay)

### AC 4: Disconnected Banner (Immediate)
- **Given** the connection is fully lost (all reconnection attempts exhausted)
- **When** the disconnected state is reached
- **Then** a red banner appears immediately (no debounce): "Connection lost. Check your internet connection."
- **And** the banner has `role="status"` and `aria-live="assertive"`

### AC 5: Reconnection Backoff & State Refresh
- **Given** the SignalR reconnection sequence
- **When** retries are attempted
- **Then** exponential backoff intervals are used: [0, 2000, 5000, 10000, 30000] ms (NFR25)
- **And** the `connectionState$` observable updates to reflect: Connected, Reconnecting, or Disconnected
- **And** on successful reconnection, the client refreshes conversation state from the server

## Tasks / Subtasks

- [x] Task 1: Create ConnectionStatusBannerComponent (AC: #1, #2, #3, #4)
  - [x] 1.1 Create `src/Web/ClientApp/src/app/shared/components/connection-status/connection-status-banner.component.ts` — standalone component, inject `SignalRService`
  - [x] 1.2 Create `connection-status-banner.component.html` — `@if` conditional rendering: amber banner for Reconnecting, red banner for Disconnected, hidden for Connected
  - [x] 1.3 Create `connection-status-banner.component.scss` — amber background (`$color-warning`) for reconnecting, red background (`$color-error`) for disconnected, white text, full-width, pushes content down (not positioned absolute/fixed)
  - [x] 1.4 Implement 3-second debounce logic: subscribe to `signalRService.connectionState`, use RxJS `switchMap` + `timer(3000)` for Reconnecting state transitions only. Disconnected state shows immediately (no debounce). Connected hides immediately. Uses a local `visibleState` property that the template binds to
  - [x] 1.5 Accessibility: banner container has `role="status"` and `aria-live="assertive"`
  - [x] 1.6 Write ConnectionStatusBannerComponent unit tests: 12 tests — banner hidden when connected, banner hidden during brief reconnecting (<3s), amber banner shown after 3s reconnecting, red banner shown immediately on disconnected, banner hides on reconnection, accessibility attributes, transition edge cases

- [x] Task 2: Integrate banner into ChatLayoutComponent (AC: #1, #3, #4)
  - [x] 2.1 Import `ConnectionStatusBannerComponent` in `chat-layout.component.ts` imports array
  - [x] 2.2 Add `<app-connection-status-banner />` in `chat-layout.component.html` — placed at the top of the `.chat-area` section, above the chat header, pushes all chat content down
  - [x] 2.3 Update ChatLayoutComponent spec to include SignalRService mock with connectionState and reconnected observables

- [x] Task 3: Add reconnection state refresh logic (AC: #5)
  - [x] 3.1 In `SignalRService.registerLifecycleHandlers()` `onreconnected` callback: after emitting `'Connected'`, emits on `reconnected$` Subject
  - [x] 3.2 Created `reconnected$` Subject in SignalRService that emits when reconnection succeeds — allows consuming services to react without tight coupling
  - [x] 3.3 In `ConversationService`: subscribes to `signalRService.reconnected` and calls `loadConversations()` to refresh the conversation list with latest data
  - [x] 3.4 In `ChatWindowComponent`: on reconnected, if a conversation is active, calls `messageService.loadMessages()` to reload and fill any gap during disconnection
  - [x] 3.5 Write unit tests for reconnection refresh: 3 new tests — verify `loadConversations()` called on reconnect, verify message reload on active conversation, verify no reload without active conversation

- [x] Task 4: Verify end-to-end and run all tests
  - [x] 4.1 Verify `ng lint` passes with zero errors
  - [x] 4.2 Verify all frontend tests pass: 247 total (231 existing + 16 new)
  - [x] 4.3 Verify all backend unit tests pass: 83 (no backend changes)
  - [x] 4.4 Verify `dotnet build --configuration Release` passes: 0 warnings, 0 errors

## Dev Notes

### Critical Architecture Patterns (MUST FOLLOW)

**Angular Component Rules:**
- Standalone components only (`standalone: true`)
- Use `inject()` for DI — NEVER constructor injection
- New Angular control flow syntax: `@if`, `@for`, `@else` — NOT `*ngIf`, `*ngFor`
- 2-space indent for TypeScript, HTML, SCSS
- Single quotes in TypeScript
- kebab-case file names
- LF line endings, UTF-8, final newline on all files
- `strict: true` in tsconfig — explicit types, null checks

**State Management:**
- BehaviorSubject with private underscore convention (`_property$`)
- Expose as readonly Observable via `.asObservable()`
- Immutable updates, never `.push()`
- Components subscribe via `async` pipe when possible; manual `subscribe()` with cleanup in `ngOnDestroy()`

**Testing:**
- Jest for frontend — co-located `.spec.ts` files
- Test naming: `should [behavior] when [condition]` (frontend)
- TestBed for components with standalone component imports
- Mock SignalRService in component tests — provide a mock `connectionState` observable

### Existing Code to Reuse (DO NOT REINVENT)

| What | Where | How to Use |
|------|-------|------------|
| SignalRService | `core/signalr/signalr.service.ts` | Already has `connectionState` observable (`Connected`/`Reconnecting`/`Disconnected`), automatic reconnect with backoff [0, 2000, 5000, 10000, 30000]. Extend with `reconnected$` Subject |
| ConnectionState type | `core/signalr/signalr.service.ts:7` | `type ConnectionState = 'Connected' \| 'Reconnecting' \| 'Disconnected'` — reuse, do not redefine |
| Design tokens | `src/Web/ClientApp/src/styles/_tokens.scss` | `$color-warning: #F59E0B` (amber), `$color-error: #DC3545` (red), `$color-text-inverse: #FFFFFF`, `$z-connection-status: 500`, `$transition-fast: 150ms ease` |
| ChatLayoutComponent | `features/chat/components/chat-layout/` | Add banner above chat header in `.chat-area` section |
| ConversationService | `features/chat/services/conversation.service.ts` | Has `loadConversations()` — call on reconnect to refresh list |
| ChatWindowComponent | `features/chat/components/chat-window/` | Subscribe to reconnected$ to gap-fill messages |
| Avatar component | `shared/components/avatar/` | Example of shared component pattern to follow |
| Unread badge component | `shared/components/unread-badge/` | Example of shared component pattern to follow |

### SignalR Reconnection Is Already Configured

The existing `SignalRService` already:
1. Configures `.withAutomaticReconnect([0, 2000, 5000, 10000, 30000])` — backoff intervals
2. Has `onreconnecting()` → emits `'Reconnecting'` state
3. Has `onreconnected()` → emits `'Connected'` state
4. Has `onclose()` → nullifies connection, emits `'Disconnected'` state
5. Exposes `connectionState` as a public `Observable<ConnectionState>`

**You do NOT need to implement reconnection logic.** The only work needed is:
- Adding a `reconnected$` Subject that emits in the `onreconnected` callback (for consuming services to react)
- Building the UI banner that subscribes to `connectionState`
- Adding reconnection refresh logic in ConversationService and ChatWindowComponent

### Debounce Logic — Key Implementation Detail

The 3-second debounce applies ONLY to the `Reconnecting` state. The logic should work as:

1. Subscribe to `signalRService.connectionState`
2. When state changes to `Reconnecting`: start a 3-second timer. If state changes to `Connected` before timer fires, do nothing (brief interruption hidden). If timer fires while still `Reconnecting`, show amber banner
3. When state changes to `Disconnected`: show red banner IMMEDIATELY (no debounce)
4. When state changes to `Connected`: hide banner IMMEDIATELY

**Implementation approach:** Use RxJS operators. One clean approach:
```typescript
// In the component
private readonly signalRService = inject(SignalRService);
visibleState: ConnectionState | null = null;

// In ngOnInit or constructor:
signalRService.connectionState.pipe(
  switchMap(state => {
    if (state === 'Connected') return of(null);           // hide immediately
    if (state === 'Disconnected') return of(state);       // show immediately
    // Reconnecting: delay 3s, but cancel if state changes
    return timer(3000).pipe(map(() => state));
  }),
  distinctUntilChanged()
).subscribe(state => this.visibleState = state);
```

### Banner Placement in ChatLayout

The banner must go at the very top of the `.chat-area` section in `chat-layout.component.html`, ABOVE the `.chat-header` div. This ensures it pushes all chat content down rather than overlapping:

```html
<!-- Current structure (line 20-51 of chat-layout.component.html): -->
<section class="chat-area" ...>
  <app-connection-status-banner />    <!-- ADD HERE — above chat-header -->
  <div class="chat-header">...</div>
  <app-chat-window />
  ...
</section>
```

The banner must NOT use `position: fixed` or `position: absolute`. It should be a normal flow element that pushes content down when visible. When hidden (`@if` removes it from DOM), content returns to normal position.

### Reconnection State Refresh — Gap-Fill Pattern

On successful reconnection, two things must happen:
1. **Conversation list refresh:** Call `conversationService.loadConversations()` to get updated last messages, unread counts, any new conversations
2. **Active conversation gap-fill:** If a conversation is selected, fetch messages newer than the last displayed message ID using `getMessageHistory` with appropriate cursor. This catches any messages that arrived during the disconnection

The reconnection refresh should use the same Observable chain pattern as existing code. The `reconnected$` Subject in SignalRService provides a clean decoupling point — services subscribe to it independently.

### Project Structure Notes

**New files to create:**
```
src/Web/ClientApp/src/app/shared/components/connection-status/
  ├── connection-status-banner.component.ts
  ├── connection-status-banner.component.html
  ├── connection-status-banner.component.scss
  └── connection-status-banner.component.spec.ts
```

**Files to modify:**
```
src/Web/ClientApp/src/app/core/signalr/signalr.service.ts         — add reconnected$ Subject
src/Web/ClientApp/src/app/core/signalr/signalr.service.spec.ts     — add reconnected$ tests
src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.ts    — import banner
src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.html  — add banner element
src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.spec.ts — update imports
src/Web/ClientApp/src/app/features/chat/services/conversation.service.ts     — subscribe to reconnected$
src/Web/ClientApp/src/app/features/chat/services/conversation.service.spec.ts — test reconnect refresh
src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts    — gap-fill on reconnect
src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.spec.ts — test gap-fill
```

### Design Token Usage

- **Reconnecting banner:** `$color-warning` (#F59E0B) background, `$color-text-primary` (#111B21) or white text
- **Disconnected banner:** `$color-error` (#DC3545) background, `$color-text-inverse` (#FFFFFF) text
- **Z-index:** `$z-connection-status: 500` — already defined in tokens
- **Padding:** `$space-sm` (8px) vertical, `$space-lg` (16px) horizontal
- **Font:** `$font-size-body-sm` (13px), `$font-weight-medium` (500)
- **Transition:** `$transition-fast` (150ms ease) for banner appear/disappear
- **Text alignment:** center

### Previous Story Intelligence (from 3.8)

**Critical learnings to apply:**
1. **Subscription cleanup:** Previous stories use `Subscription[]` array with `ngOnDestroy()` cleanup, or `DestroyRef` pattern. Follow the same pattern in new components — the banner component should clean up its observable subscription
2. **Test infrastructure:** `setup-jest.ts` calls `setupZoneTestEnv()`, `matchMedia` mock exists. Reuse patterns from existing specs
3. **Shared component pattern:** Avatar and UnreadBadge in `shared/components/` are good examples of the file structure and testing patterns to follow for the new ConnectionStatusBanner
4. **Review patches from 3.8:** Race conditions with async operations were a recurring theme. The debounce logic here is inherently async — test edge cases thoroughly (rapid state changes, quick reconnect before debounce fires)
5. **Test counts to maintain:** 231 frontend tests, 83 backend unit tests. All must continue passing

### Git Intelligence

**Recent commit pattern:** Each story implemented as single commit: `story X.Y created implemented and reviewed`. Current branch: `prep/fix-test-infrastructure`.

**This story is frontend-only.** No backend changes needed — the SignalR hub already handles reconnection server-side. All work is in Angular: one new shared component + modifications to existing services and components.

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-3-real-time-private-messaging.md — Story 3.9 section]
- [Source: _bmad-output/planning-artifacts/architecture.md — SignalR connection lifecycle, exponential backoff, connectionState$ observable, connection-status component in shared/components]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md — UX-DR6 Connection Status Banner: states, debounce behavior, placement, copy, accessibility]
- [Source: _bmad-output/planning-artifacts/prd.md — FR23 (connection state visual indicator), FR49 (auto-reconnect), NFR25 (exponential backoff)]
- [Source: _bmad-output/implementation-artifacts/3-8-new-private-conversation-flow.md — Previous story dev notes, review patches, test patterns]
- [Source: _bmad-output/project-context.md — Angular standalone components, inject() DI, BehaviorSubject state, testing standards, design token usage]
- [Source: src/Web/ClientApp/src/app/core/signalr/signalr.service.ts — Existing SignalR service with connectionState observable and reconnection config]
- [Source: src/Web/ClientApp/src/styles/_tokens.scss — $color-warning, $color-error, $z-connection-status, spacing/typography tokens]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- All 247 frontend tests pass (231 existing + 16 new)
- All 83 backend unit tests pass (no backend changes)
- `ng lint` passes with zero errors
- `dotnet build --configuration Release` passes with 0 warnings, 0 errors

### Completion Notes List

- Task 1: Created ConnectionStatusBannerComponent in `shared/components/connection-status/`. Standalone component subscribing to `SignalRService.connectionState` with RxJS `switchMap` + `timer(3000)` for debounced reconnecting state. Amber banner for Reconnecting (after 3s), red banner for Disconnected (immediate), hidden for Connected. Uses design tokens for colors. Full accessibility with `role="status"` and `aria-live="assertive"`. 12 new frontend unit tests covering all states, debounce behavior, transitions, and accessibility.
- Task 2: Integrated `<app-connection-status-banner />` into ChatLayoutComponent at the top of `.chat-area` section, above the chat header. Banner pushes content down naturally (no absolute/fixed positioning). Updated chat-layout spec with SignalRService mock.
- Task 3: Added `reconnected$` Subject to SignalRService, emitted in `onreconnected` callback. ConversationService subscribes to `reconnected` and calls `loadConversations()` to refresh list. ChatWindowComponent subscribes to `reconnected` and reloads active conversation messages for gap-fill. 3 new unit tests across conversation.service and chat-window specs.
- Task 4: All validations pass — lint clean, 247 frontend tests, 83 backend unit tests, Release build succeeds.

### Review Findings

- [x] [Review][Patch] `distinctUntilChanged` can suppress banner hide on rapid reconnect cycles [connection-status-banner.component.ts:30] — Removed `distinctUntilChanged()` to prevent stale banner state during rapid Connected→Reconnecting→Connected cycles
- [x] [Review][Patch] `z-index` on statically positioned banner is dead code [connection-status-banner.component.scss:9] — Removed the `z-index: $z-connection-status` line since it has no effect without a `position` property
- [x] [Review][Defer] `loadConversations()` has no in-flight request cancellation [conversation.service.ts:51-64] — deferred, pre-existing pattern not introduced by this diff

### Change Log

- 2026-04-02: Story 3.9 implemented — connection status banner with 3s debounce, reconnection state refresh for conversations and messages

### File List

**New files (4):**
- src/Web/ClientApp/src/app/shared/components/connection-status/connection-status-banner.component.ts
- src/Web/ClientApp/src/app/shared/components/connection-status/connection-status-banner.component.html
- src/Web/ClientApp/src/app/shared/components/connection-status/connection-status-banner.component.scss
- src/Web/ClientApp/src/app/shared/components/connection-status/connection-status-banner.component.spec.ts

**Modified files (8):**
- src/Web/ClientApp/src/app/core/signalr/signalr.service.ts
- src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.ts
- src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.html
- src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.spec.ts
- src/Web/ClientApp/src/app/features/chat/services/conversation.service.ts
- src/Web/ClientApp/src/app/features/chat/services/conversation.service.spec.ts
- src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts
- src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.spec.ts
