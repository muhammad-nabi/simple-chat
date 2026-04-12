---
title: 'Fix message loading state management bugs'
type: 'bugfix'
created: '2026-04-12'
status: 'done'
baseline_commit: '8712e61'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Two related bugs in the chat message loading pipeline cause (1) the skeleton loader to stay visible indefinitely when first opening a conversation, and (2) stale messages from a previous conversation to flash when switching between conversations. Both stem from incomplete state cleanup in the message loading flow.

**Approach:** Reset `_loading$` in the stale-response guard path so the skeleton always clears, and call `clearMessages()` before `loadMessages()` when switching between conversations so old messages don't bleed through.

## Boundaries & Constraints

**Always:** Preserve the existing stale-response guard logic (line 76 check) — it correctly prevents data corruption. Keep the `pairwise()` subscription pattern. Keep scroll-position save/restore behavior intact.

**Ask First:** Any changes to the HTTP request cancellation logic (`loadSubscription?.unsubscribe()`).

**Never:** Change the message API contract. Add new services or components. Refactor beyond the two specific bugs.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Create new group | User creates group via dialog | Chat opens, skeleton clears, system message visible | N/A |
| Join group from Browse | User clicks Join | Group opens, skeleton clears, messages load | N/A |
| Switch conversations | Click conv A then conv B | B's messages shown immediately, no flash of A's messages | N/A |
| Rapid switching | Click A → B → C quickly | Only C's messages visible, loading clears | Stale responses discarded silently |
| API error on load | Network failure during load | Skeleton clears, error state shown | `_loading$.next(false)` in error handler |

</frozen-after-approval>

## Code Map

- `src/Web/ClientApp/src/app/features/chat/services/message.service.ts` -- Message loading, caching, and state management. Contains the stale-response guard (line 76) and loading flag lifecycle.
- `src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts` -- Subscribes to conversation selection changes via `pairwise()`. Controls when `loadMessages()` and `clearMessages()` are called.

## Tasks & Acceptance

**Execution:**
- [x] `src/Web/ClientApp/src/app/features/chat/services/message.service.ts` -- In `loadMessages()`, add `this._loading$.next(false)` before the early return in the stale-response guard (line 76–78) so the skeleton always clears even when a response is discarded.
- [x] `src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts` -- In the `pairwise()` subscription, call `this.messageService.clearMessages()` before `this.messageService.loadMessages(current.id)` when `current` is non-null, so old messages are removed immediately on switch.
- [x] `src/Web/ClientApp/src/app/features/chat/services/message.service.spec.ts` -- Add/update tests: verify `_loading$` emits `false` when a stale response is discarded; verify `clearMessages()` resets `_messages$` to empty array.
- [x] `src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.spec.ts` -- Add test: verify `clearMessages()` is called before `loadMessages()` when switching between two non-null conversations.

**Acceptance Criteria:**
- Given a newly created group conversation, when the chat window opens for the first time, then messages load and the skeleton disappears without needing to navigate away and back.
- Given the user is viewing conversation A, when they click conversation B, then A's messages are immediately cleared and B's messages are shown (no flash of A's content).
- Given rapid conversation switching (A → B → C), when all responses arrive, then only C's messages are displayed and loading state is resolved.

## Verification

**Commands:**
- `cd src/Web/ClientApp && npx jest --testPathPattern="message.service|chat-window" --no-coverage` -- expected: all tests pass including new ones
- `cd src/Web/ClientApp && npx jest --no-coverage` -- expected: full suite passes with 0 regressions

### Review Findings

- [x] [Review][Defer] Reconnect handler doesn't call clearMessages() before loadMessages() — deferred, pre-existing
- [x] [Review][Defer] Same-conversation re-click causes unnecessary clear+reload (no prev.id === current.id guard) — deferred, pre-existing
- [x] [Review][Defer] Pre-selected conversation via joinGroup/createConversation may not trigger loadMessages on mount — deferred, pre-existing

## Suggested Review Order

- Stale-response guard now resets loading flags before early return — the core fix for skeleton-stuck bug
  [`message.service.ts:77`](../../src/Web/ClientApp/src/app/features/chat/services/message.service.ts#L77)

- Messages cleared before loading on conversation switch — prevents stale message bleed-through
  [`chat-window.component.ts:98`](../../src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts#L98)

- Test: loading$ resets to false when stale response is discarded
  [`message.service.spec.ts:170`](../../src/Web/ClientApp/src/app/features/chat/services/message.service.spec.ts#L170)

- Test: no stale messages between clear and new load
  [`message.service.spec.ts:194`](../../src/Web/ClientApp/src/app/features/chat/services/message.service.spec.ts#L194)

- Test: clearMessages() called before loadMessages() on conversation switch
  [`chat-window.component.spec.ts:173`](../../src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.spec.ts#L173)
