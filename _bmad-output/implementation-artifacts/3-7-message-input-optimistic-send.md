# Story 3.7: Message Input & Optimistic Send

Status: done

## Story

As a user,
I want to type and send messages with instant visual feedback,
so that the conversation feels fast and responsive.

## Acceptance Criteria

### AC 1: Message Input Component UI
- **Given** the MessageInput component (UX-DR9) is rendered
- **When** the component initializes
- **Then:**
  - A `<textarea>` with 16px border-radius and 1.5px border with teal focus highlight is displayed
  - A labeled "Attach" button is rendered but visually disabled (grayed out, no click handler) until Epic 6
  - A teal rounded-square (14px radius) Send button (44x44px minimum touch target) is visible
  - Input has `aria-label="Type a message"`
  - Send button has `aria-label="Send message"`
  - On desktop (`layoutMode === 'desktop'`), auto-focus the textarea when a conversation is opened
  - On mobile/tablet, do NOT auto-focus (preserves reading space, avoids keyboard popup)

### AC 2: Message Submission with Optimistic UI
- **Given** the user types a message and presses Enter (desktop) or taps Send
- **When** the message is submitted
- **Then:**
  - An optimistic message appears instantly in the chat window with `{ id: -Date.now(), checkmarkState: 'sending' }` — single gray checkmark (✓)
  - After server confirmation via `signalRService.sendMessage()` returning the persisted ID, the optimistic message ID is replaced and checkmarkState transitions to `'sent'` — single teal checkmark (✓)
  - Checkmark color transition: <250ms CSS transition
  - First message ever sent by user in a conversation gets a subtle scale animation (1.2x for 150ms)
  - All animations respect `prefers-reduced-motion` (static checkmarks, no animation)

### AC 3: Multiline Input Support
- **Given** the user presses Shift+Enter
- **When** typing in the message input
- **Then** a newline is inserted (message is NOT sent)

### AC 4: Draft Persistence & Restoration
- **Given** the user is typing in a conversation
- **When** they switch to a different conversation without sending
- **Then:**
  - Draft text is saved to localStorage keyed as `draft:{conversationId}` with value `{ text: string, timestamp: number }`
  - A BehaviorSubject tracks current draft state for reactive UI updates

- **Given** the user switches to a conversation that has a saved draft
- **When** the conversation loads
- **Then:**
  - Textarea is pre-populated with the draft text
  - A subtle "Draft" indicator (gray, opacity 0.6, right-aligned above input) shows and clears on first keystroke

### AC 5: Draft Cleanup
- **Given** a message is sent successfully
- **When** the server confirms persistence (optimistic message transitions to 'sent')
- **Then** the draft for that conversation is cleared from both BehaviorSubject and localStorage

### AC 6: Offline Error Handling
- **Given** the user attempts to send while disconnected (`connectionState !== 'Connected'`)
- **When** the send fails
- **Then:**
  - The message stays in the textarea (input NOT cleared)
  - Error text "Couldn't send — tap to retry" displays below input with `role="alert"` and `aria-live="assertive"`
  - Tapping the error message or re-pressing Send retries the send

## Tasks / Subtasks

- [x] Task 1: Extend Message model and MessageService for optimistic send (AC: #2, #5)
  - [x] 1.1 Add `checkmarkState?: 'sending' | 'sent'` to Message interface in `message.model.ts`
  - [x] 1.2 Add `sendMessage(conversationId: number, content: string): void` method to MessageService
  - [x] 1.3 Implement optimistic message creation: generate temp ID (`-Date.now()`), append to `_messages$`, call `signalRService.sendMessage()`, on resolve → update ID + set checkmarkState to 'sent', on reject → remove optimistic message and emit error
  - [x] 1.4 Add `_sendError$` BehaviorSubject to MessageService for send error state
  - [x] 1.5 Track `firstMessageSent` per conversationId (Map<number, boolean>) to control micro-celebration
  - [x] 1.6 Write MessageService unit tests for send flow (optimistic insert, confirmation update, error rollback)

- [x] Task 2: Create DraftService (AC: #4, #5)
  - [x] 2.1 Create `src/Web/ClientApp/src/app/features/chat/services/draft.service.ts`
  - [x] 2.2 Implement BehaviorSubject `_draft$` for current draft text
  - [x] 2.3 `saveDraft(conversationId: number, text: string): void` — save to localStorage as `draft:{conversationId}` with `{ text, timestamp: Date.now() }`
  - [x] 2.4 `loadDraft(conversationId: number): string | null` — load from localStorage, update `_draft$`
  - [x] 2.5 `clearDraft(conversationId: number): void` — remove from localStorage, clear `_draft$`
  - [x] 2.6 Write DraftService unit tests (save, load, clear, localStorage interaction)

- [x] Task 3: Create MessageInputComponent (AC: #1, #2, #3, #6)
  - [x] 3.1 Create component files at `src/Web/ClientApp/src/app/features/chat/components/message-input/`
  - [x] 3.2 Implement textarea with styling (16px border-radius, 1.5px solid border, teal focus outline)
  - [x] 3.3 Implement disabled Attach button placeholder (grayed out, `aria-label="Attach file (coming soon)"`)
  - [x] 3.4 Implement Send button (teal, 14px radius, 44x44px min, `aria-label="Send message"`)
  - [x] 3.5 Implement Enter-to-send (keydown handler: if Enter without Shift → preventDefault + send)
  - [x] 3.6 Implement Shift+Enter for newline (do nothing — textarea default behavior)
  - [x] 3.7 Implement send debounce: 300ms after send, disable button to prevent double-tap
  - [x] 3.8 Implement max-length validation: 4000 chars, show inline error if exceeded
  - [x] 3.9 Implement auto-focus on desktop only (check parent ChatLayoutComponent's `layoutMode`)
  - [x] 3.10 Implement draft save on conversation switch (subscribe to `conversationService.selectedConversation$`, on change → save current text, load new draft)
  - [x] 3.11 Implement "Draft" indicator above input
  - [x] 3.12 Implement error state below input with retry
  - [x] 3.13 Write MessageInputComponent unit tests (all ACs)

- [x] Task 4: Create CheckmarkComponent (AC: #2)
  - [x] 4.1 Create standalone component at `src/Web/ClientApp/src/app/features/chat/components/checkmark/`
  - [x] 4.2 Input: `state: 'sending' | 'sent'`, `animate: boolean` (for first-message celebration)
  - [x] 4.3 Render single ✓ — gray (#999) for 'sending', teal (#128C7E) for 'sent'
  - [x] 4.4 CSS transition: color 250ms ease
  - [x] 4.5 First-message animation: `transform: scale(1.2)` for 150ms, only when `animate` is true
  - [x] 4.6 `@media (prefers-reduced-motion: reduce)` — disable all animations
  - [x] 4.7 Write Checkmark unit tests

- [x] Task 5: Integrate into MessageBubble and ChatLayout (AC: #2, #1)
  - [x] 5.1 Update `message-bubble.component.ts/html` to display Checkmark for own messages that have `checkmarkState`
  - [x] 5.2 Update `chat-layout.component.html` to add `<app-message-input />` below `<app-chat-window />`
  - [x] 5.3 Pass `layoutMode` to MessageInputComponent (or inject ChatLayoutComponent if needed)
  - [x] 5.4 Ensure chat-window auto-scrolls when optimistic message is added (existing `shouldScrollToBottom` logic should handle this)
  - [x] 5.5 Update ChatLayoutComponent imports to include MessageInputComponent
  - [x] 5.6 Update existing tests if modified components break

- [x] Task 6: Verify end-to-end flow and run all tests
  - [x] 6.1 Verify `ng lint` passes with zero errors
  - [x] 6.2 Verify all existing tests still pass (130+ frontend tests)
  - [x] 6.3 Verify backend tests still pass (79 tests)
  - [x] 6.4 Manual smoke test: send message, verify optimistic display, checkmark transition, draft persistence

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
- `strict: true` in tsconfig — explicit types, null checks, no `null!`

**State Management:**
- BehaviorSubject with private underscore convention (`_draft$`)
- Expose as readonly Observable via `.asObservable()`
- Immutable updates: `[...existing, newItem]`, never `.push()`
- Components subscribe via `async` pipe when possible; manual `subscribe()` with cleanup via `Subscription[]` array + `ngOnDestroy()`

**Real-Time Message Send — THE CRITICAL FLOW:**
```
User presses Enter
  → MessageInputComponent calls messageService.sendMessage(conversationId, content)
    → MessageService creates optimistic message { id: -Date.now(), checkmarkState: 'sending' }
    → Appends to _messages$.next([...current, optimistic])
    → Clears textarea, clears draft
    → Calls signalRService.sendMessage(conversationId, content) → returns Promise<number> (persisted ID)
      → On resolve: find optimistic message by temp ID, replace with { ...msg, id: persistedId, checkmarkState: 'sent' }
      → On reject: remove optimistic message from _messages$, set _sendError$, restore text to textarea
```

**Important:** `signalRService.sendMessage()` already exists and returns `Promise<number>`. Do NOT create an HTTP endpoint for sending — use SignalR exclusively as the send path (persist-first flow is handled server-side in ChatHub).

### Existing Code to Reuse (DO NOT REINVENT)

| What | Where | How to Use |
|------|-------|------------|
| SignalR send | `signalRService.sendMessage(conversationId, content)` | Returns `Promise<number>` (message ID) |
| Connection state | `signalRService.connectionState` Observable | Check before send, show offline error |
| Message model | `features/chat/models/message.model.ts` | Extend with `checkmarkState` field |
| MessageService | `features/chat/services/message.service.ts` | Add `sendMessage()` method |
| Selected conversation | `conversationService.selectedConversation` Observable | Get current conversationId |
| Current user | `authService.currentUser$` Observable | Get userId for own-message detection |
| Layout mode | `ChatLayoutComponent.layoutMode` property | Desktop auto-focus check |
| Design tokens | `src/Web/ClientApp/src/styles/_tokens.scss` | Teal (#128C7E), border-radius, spacing |
| Message bubble | `components/message-bubble/` | Add Checkmark display for own messages |
| Chat layout | `components/chat-layout/chat-layout.component.html` | Add `<app-message-input />` after `<app-chat-window />` |

### Existing SignalR Service API (DO NOT MODIFY)

```typescript
// Already exists — use as-is:
async sendMessage(conversationId: number, content: string): Promise<number>
readonly connectionState: Observable<ConnectionState>  // 'Connected' | 'Reconnecting' | 'Disconnected'
readonly messageReceived: Observable<MessagePayload>
```

### Handling Duplicate Messages from SignalR

When the user sends a message, the flow is:
1. Optimistic message added to `_messages$` with temp ID
2. `signalRService.sendMessage()` called → server persists → broadcasts to all in conversation
3. The sender's own `messageReceived` observable will fire for their own message

**You MUST handle this:** When `handleIncomingMessage()` fires for a message that matches an already-optimistic message (same content + senderId + recent timestamp), do NOT add a duplicate. Instead, use the incoming server message to update the optimistic entry (replace temp ID with real ID, set checkmarkState to 'sent'). A simple approach: if the `sendMessage()` Promise resolves with the real ID, update the optimistic message immediately and skip the duplicate when it arrives via `messageReceived`.

### Project Structure Notes

**New files to create:**
```
src/Web/ClientApp/src/app/features/chat/
  ├── services/draft.service.ts
  ├── services/draft.service.spec.ts
  ├── components/message-input/
  │   ├── message-input.component.ts
  │   ├── message-input.component.html
  │   ├── message-input.component.scss
  │   └── message-input.component.spec.ts
  └── components/checkmark/
      ├── checkmark.component.ts
      ├── checkmark.component.html
      ├── checkmark.component.scss
      └── checkmark.component.spec.ts
```

**Files to modify:**
```
features/chat/models/message.model.ts           — add checkmarkState field
features/chat/services/message.service.ts        — add sendMessage(), send error state
features/chat/services/message.service.spec.ts   — add send flow tests
features/chat/components/message-bubble/         — add Checkmark display for own messages
features/chat/components/chat-layout/            — add <app-message-input /> to template, update imports
```

### Design Token Usage

All styling values come from `_tokens.scss`:
- **Teal primary:** `#128C7E` (send button bg, sent checkmark, focus outline)
- **Gray:** `#999` (sending checkmark, disabled attach button)
- **Border radius:** `16px` (textarea), `14px` (send button)
- **Border:** `1.5px solid` with teal focus
- **Touch target:** `44px × 44px` minimum (send button)
- **Spacing:** follow existing chat component patterns
- **Error text:** red (#D32F2F or existing error token)
- **Draft indicator:** gray at opacity 0.6

### Testing Standards

- **Jest** with shallow TestBed components
- Tests co-located as `.spec.ts` files
- Test naming: `should [expected behavior] when [condition]`
- Mock services with `jest.fn()` and provide in TestBed
- Verify accessibility attributes (`aria-label`, `role`, `aria-live`)
- Verify keyboard handling (Enter sends, Shift+Enter doesn't)
- Verify draft persistence (localStorage mock)
- Verify optimistic send flow (message appears instantly, checkmark transitions)
- Verify error handling (offline, validation)
- Test `prefers-reduced-motion` media query disables animations

### Previous Story Intelligence (from 3.6)

**Critical learnings to apply:**
1. **Stale HTTP responses:** Story 3.6 had a bug where fast conversation switching caused stale responses. The fix used `currentConversationId` guard. Apply same pattern: when conversation switches during an in-flight send, handle gracefully
2. **Scroll position preservation:** Already handled in ChatWindow — optimistic messages should trigger `scrollToBottom()` when user is near bottom
3. **aria-live considerations:** Story 3.6 found that `aria-live="polite"` announces prepended history loads. For message input: use `aria-live="assertive"` only on error messages, not on sent message confirmations
4. **Pure pipes for templates:** Story 3.6 created `MessageTimestampPipe` to avoid per-change-detection calls. Keep any computed values in the template lightweight
5. **Test infrastructure:** `setup-jest.ts` has been fixed (calls `setupZoneTestEnv()`), `matchMedia` mock exists in chat-layout spec — reuse pattern if needed

### Git Intelligence

**Recent commit pattern:** Each story is implemented and committed as a single commit with format `story X.Y created implemented and reviewed`. Current branch: `prep/fix-test-infrastructure`.

**Code volume per story:** ~1500-2000 insertions typical. Story 3.7 should be similar scope.

### Validation Rules

- Empty messages: prevent send (disable Send button when textarea is empty/whitespace-only)
- Max 4000 characters: validate before send, show inline character count or error near limit
- User must be a conversation participant (server enforces with 403, but prevent client-side if no conversation selected)

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-3-real-time-private-messaging.md — Story 3.7 section]
- [Source: _bmad-output/planning-artifacts/architecture.md — SignalR patterns, Message Send Flow, Frontend patterns]
- [Source: _bmad-output/planning-artifacts/ux-design.md — UX-DR9 Message Input, UX-DR19 Optimistic Send, UX-DR21 Draft Persistence]
- [Source: _bmad-output/planning-artifacts/prd.md — FR7, FR10, FR19, NFR1]
- [Source: _bmad-output/implementation-artifacts/3-6-chat-window-message-display.md — Previous story dev notes and review patches]
- [Source: _bmad-output/project-context.md — Build rules, anti-patterns, testing standards]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- All 186 frontend tests pass (56 new + 130 existing)
- All 79 backend unit tests pass
- `ng lint` passes with zero errors

### Completion Notes List

- Task 1: Extended Message model with `CheckmarkState` type and optional `checkmarkState` field. Added `sendMessage()` to MessageService implementing optimistic UI pattern: temp negative ID → append to messages$ → SignalR send → on resolve: update ID + checkmarkState to 'sent' → on reject: remove optimistic message, set error. Added `pendingMessageIds` Set to prevent duplicate messages when SignalR broadcasts back to sender. Added `firstMessageSent` Map for micro-celebration tracking. 11 new unit tests.
- Task 2: Created DraftService with localStorage persistence keyed by `draft:{conversationId}`, BehaviorSubject for reactive draft state, auto-clear on empty text. 11 unit tests.
- Task 3: Created MessageInputComponent with textarea (16px radius, 1.5px border, teal focus), disabled Attach button, teal Send button (44x44px, 14px radius), Enter-to-send, Shift+Enter newline, 300ms send debounce, 4000-char validation, desktop auto-focus, draft persistence on conversation switch with "Draft" indicator, error state with retry, full ARIA support. 24 unit tests.
- Task 4: Created CheckmarkComponent with sending (gray #999) / sent (teal #128C7E) states, 250ms color transition, scale(1.2) celebration animation for first messages, prefers-reduced-motion support. 9 unit tests.
- Task 5: Integrated Checkmark into MessageBubble for own messages with checkmarkState. Added MessageInput to ChatLayout below ChatWindow, conditional on selected conversation, passing layoutMode. All existing tests (55 across 3 components) still pass.
- Task 6: Lint clean, all tests green, no regressions.

### Review Findings

- [x] [Review][Patch] Retry broken — message text cleared before failure known [message-input.component.ts:111-141] — FIXED: store lastSentContent, restore on error, retry uses stored content
- [x] [Review][Patch] Draft cleared before server confirmation [message-input.component.ts:125-129] — FIXED: draft cleared via sendConfirmed$ subscription after server confirms
- [x] [Review][Patch] isFirstMessage never bound in chat-window template — FIXED: added [isFirstMessage] binding and isFirstSentMessage() helper in chat-window
- [x] [Review][Patch] Race: SignalR echo arrives before pendingMessageIds populated [message.service.ts:149-184] — FIXED: handleIncomingMessage now checks for optimistic match by content+senderId
- [x] [Review][Patch] tempId collision on rapid sends [message.service.ts:132] — FIXED: monotonic counter appended to timestamp
- [x] [Review][Patch] Conversation switch during in-flight send — stale callback [message.service.ts:149-165] — FIXED: capturedConversationId guard in .then()/.catch()
- [x] [Review][Patch] pendingMessageIds and firstMessageSent never cleared [message.service.ts:27] — FIXED: cleared in clearMessages()
- [x] [Review][Patch] localStorage.setItem can throw QuotaExceededError [draft.service.ts:22-25] — FIXED: try-catch wrapper
- [x] [Review][Defer] senderDisplayName hardcoded to empty string in optimistic message [message.service.ts:136] — deferred, pre-existing architectural gap for future group chats
- [x] [Review][Defer] isFirstMessageInConversation doesn't check loaded history [message.service.ts:127-129] — deferred, pre-existing design issue needs broader fix

### Change Log

- 2026-04-02: Story 3.7 implemented — message input with optimistic send, draft persistence, checkmark feedback, full accessibility

### File List

**New files (10):**
- src/Web/ClientApp/src/app/features/chat/services/draft.service.ts
- src/Web/ClientApp/src/app/features/chat/services/draft.service.spec.ts
- src/Web/ClientApp/src/app/features/chat/components/message-input/message-input.component.ts
- src/Web/ClientApp/src/app/features/chat/components/message-input/message-input.component.html
- src/Web/ClientApp/src/app/features/chat/components/message-input/message-input.component.scss
- src/Web/ClientApp/src/app/features/chat/components/message-input/message-input.component.spec.ts
- src/Web/ClientApp/src/app/features/chat/components/checkmark/checkmark.component.ts
- src/Web/ClientApp/src/app/features/chat/components/checkmark/checkmark.component.html
- src/Web/ClientApp/src/app/features/chat/components/checkmark/checkmark.component.scss
- src/Web/ClientApp/src/app/features/chat/components/checkmark/checkmark.component.spec.ts

**Modified files (7):**
- src/Web/ClientApp/src/app/features/chat/models/message.model.ts
- src/Web/ClientApp/src/app/features/chat/services/message.service.ts
- src/Web/ClientApp/src/app/features/chat/services/message.service.spec.ts
- src/Web/ClientApp/src/app/features/chat/components/message-bubble/message-bubble.component.ts
- src/Web/ClientApp/src/app/features/chat/components/message-bubble/message-bubble.component.html
- src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.ts
- src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.html
