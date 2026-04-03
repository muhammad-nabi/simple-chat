# Story 4.2: Group Messaging & History

Status: done

## Story

As a user,
I want to send and read messages in a group conversation,
so that I can participate in team discussions with full context.

## Acceptance Criteria

1. **Given** an authenticated user who is a participant in a group conversation
   **When** they send a message in the group
   **Then** the message is persisted and broadcast to all group participants via SignalR
   **And** all participants see the message in real time
   **And** the message bubble displays the sender's name in teal above the message text (UX-DR8 group variant)
   **And** same-sender consecutive messages are grouped (4px gap, name shown only on first)

2. **Given** a group conversation with message history
   **When** the user scrolls up
   **Then** older messages load via cursor-based pagination (same behavior as private messaging)
   **And** each message clearly shows which participant sent it

3. **Given** a system event in the group (user joined, user left, group created)
   **When** the event is rendered
   **Then** a system message appears as a gray pill, centered in the chat stream (UX-DR8 system message variant)

## Tasks / Subtasks

- [x] Task 1: Backend - Add system message on group creation (AC: #3)
  - [x] 1.1 In `CreateConversationCommandHandler`, after creating a Group conversation, insert a `Message` with `MessageType.System`, `SenderId = creatorId`, `Content = "{displayName} created the group"`
  - [x] 1.2 Add unit test verifying system message is created for Group conversations but NOT for Private conversations

- [x] Task 2: Frontend - System message rendering (AC: #3)
  - [x] 2.1 In `chat-window.component.html`, add conditional rendering for `message.messageType === 'System'` — render as centered gray pill instead of `<app-message-bubble>`
  - [x] 2.2 Add `.system-message` styles in `chat-window.component.scss`: centered text, `#667781` text color, `#E4E6EB` background, `$radius-full` (9999px) border radius, inline-block, `$space-sm` vertical margin, `$size-sm` (13px) font size
  - [x] 2.3 System messages must break consecutive grouping — update `shouldShowSender()` and `isConsecutiveMessage()` to treat system messages as group-breakers
  - [x] 2.4 Add tests for system message rendering and grouping behavior

- [x] Task 3: Frontend - Group-aware sender name display (AC: #1, #2)
  - [x] 3.1 Update `shouldShowSender()` in `chat-window.component.ts` to only return `true` when `selectedConversation.type === 'Group'` — private conversations should NOT show sender names (you already know who you're talking to)
  - [x] 3.2 Verify `isConsecutiveMessage()` correctly groups same-sender messages in group context
  - [x] 3.3 Add tests for group vs private sender display logic

- [x] Task 4: Frontend - Sender name teal styling for groups (AC: #1)
  - [x] 4.1 In `message-bubble.component.scss`, ensure `.sender-name` uses `$color-primary` (`#128C7E`) — verified already correct
  - [x] 4.2 Verify consecutive same-sender messages have 4px gap (`.consecutive` class) — verified `.message-wrapper.consecutive` margin is `$space-xs` (4px)

- [x] Task 5: Verification - Group messaging end-to-end (AC: #1, #2)
  - [x] 5.1 Verify `SendMessageCommand` works correctly for group conversations (participant check, broadcast to all members) — confirmed via integration test
  - [x] 5.2 Verify `GetMessageHistory` returns messages with `senderDisplayName` and `messageType` for group conversations — confirmed pagination works identically to private conversations
  - [x] 5.3 Add integration test: send message in group conversation, verify broadcast includes `senderDisplayName` and `messageType`

## Dev Notes

### What Already Exists (DO NOT Recreate)

The messaging infrastructure from Epic 3 is **fully reusable** for group conversations:

- **SendMessageCommand** (`src/Application/Messaging/Commands/SendMessage/`) — verifies participant membership via `ConversationParticipants` table, works for both Private and Group. No changes needed.
- **GetMessageHistory** (`src/Application/Messaging/Queries/GetMessageHistory/`) — cursor-based pagination with `Before` param, resolves `senderDisplayName` via `IIdentityService.GetDisplayNamesByIdsAsync()`. Returns `messageType` as string. No changes needed.
- **ChatHub** (`src/Web/Hubs/ChatHub.cs`) — auto-joins all user's conversations on connect, `JoinConversation()` for new conversations. `SignalRMessageBroadcaster` broadcasts to SignalR group by conversationId. No changes needed.
- **MessageService** (`src/Web/ClientApp/.../services/message.service.ts`) — optimistic send, pagination, scroll position management. No changes needed.
- **ConversationService** (`src/Web/ClientApp/.../services/conversation.service.ts`) — already has `createGroupConversation()`, handles incoming messages for all conversation types.
- **message-bubble component** — already has `showSender`, `isConsecutive`, and `isFirstMessage` inputs. Template already renders sender name conditionally with `@if (showSender && !isOwn)`.
- **chat-window component** — already has `shouldShowSender()`, `isConsecutiveMessage()`, infinite scroll, and scroll position management.

### What Needs to Change

**Backend (1 file):**
- `CreateConversationCommandHandler.cs` — add system message insertion after group creation

**Frontend (4 files):**
- `chat-window.component.ts` — update `shouldShowSender()` to be group-aware, update `isConsecutiveMessage()` for system message breaks
- `chat-window.component.html` — add system message rendering path before the `@for` message loop
- `chat-window.component.scss` — add `.system-message` styles
- `message-bubble.component.scss` — verify/update sender name teal color

### Architecture Compliance

- **Message flow**: Persist-first through SignalR — `SendMessageCommand → EF Insert → MessageSent notification → SignalR broadcast`. This is unchanged for group messages.
- **Cursor-based pagination**: `WHERE Id < @cursor ORDER BY Id DESC TAKE(limit+1)` — same for groups.
- **MediatR dispatch**: All commands/queries through MediatR pipeline. Do not bypass.
- **No repository pattern**: Inject `IApplicationDbContext` directly in handlers.
- **Problem Details errors**: FluentValidation → 400, NotFoundException → 404, ForbiddenAccessException → 403.

### C# Critical Rules

- Commands/Queries are **records**, handlers are **classes**
- `readonly` on private fields (TreatWarningsAsErrors)
- File-scoped namespaces, explicit types (not `var`), Allman braces
- `CancellationToken` on all async handler methods
- Guard clauses: `Guard.Against.Null()`, `Guard.Against.NullOrEmpty()`
- Custom exceptions (not Result types)
- Handler naming: `{Command/Query}Handler`

### Angular Critical Rules

- `inject()` for DI (never constructor injection)
- Standalone components only, no NgModules
- New control flow: `@if`, `@for`, `@else` (not `*ngIf`, `*ngFor`)
- BehaviorSubject for state, expose via `.asObservable()`
- Single quotes, 2-space indent, kebab-case files
- Reactive forms with FormBuilder
- `strict: true` in tsconfig

### Testing Standards

**Backend unit tests (NUnit):**
- Arrange/Act/Assert, Moq for mocking, Shouldly for assertions
- Method naming: `MethodUnderTest_Scenario_ExpectedOutcome`
- Mirror Application folder structure
- Existing test file: `CreateConversationCommandHandlerTests.cs` (add system message test here)

**Frontend tests (Jest):**
- TestBed for components with standalone imports
- DOM assertions with `fixture.nativeElement`
- Existing test files: `chat-window.component.spec.ts`, `message-bubble.component.spec.ts`

### Design Token References

| Token | Value | Usage |
|-------|-------|-------|
| `$color-primary` | `#128C7E` | Sender name text in group bubbles |
| `$color-text-secondary` | `#667781` | System message text color |
| `$color-surface-hover` | `#E4E6EB` | System message pill background |
| `$radius-full` | `9999px` | System message pill border radius |
| `$space-xs` | `4px` | Consecutive message gap |
| `$space-sm` | `8px` | System message vertical margin |
| `$size-sm` | `0.8125rem` (13px) | System message font size |

### Project Structure Notes

All files follow the existing structure. No new files needed — only modifications to existing files:

**Backend:**
- `src/Application/Messaging/Commands/CreateConversation/CreateConversationCommandHandler.cs`
- `tests/Application.UnitTests/Messaging/Commands/CreateConversation/CreateConversationCommandHandlerTests.cs`

**Frontend:**
- `src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts`
- `src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.html`
- `src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.scss`
- `src/Web/ClientApp/src/app/features/chat/components/message-bubble/message-bubble.component.scss`

**Integration tests (optional but recommended):**
- `tests/Infrastructure.IntegrationTests/Endpoints/ConversationEndpointTests.cs`

### Previous Story Intelligence (Story 4.1)

**Key learnings from 4-1 review:**
- Enter key handling in inputs needs explicit `(keydown.enter)` handler
- Validation: always check for duplicates, whitespace-only strings, mutual exclusion of fields
- The `CreateConversationCommand` was extended (not duplicated) to handle both Private and Group — same pattern should be followed
- Test count: 87 backend passed, 267 frontend passed — do not regress

**Files modified in 4-1 that are relevant:**
- `CreateConversationCommandHandler.cs` — extended for Group type (this is where system message should be added)
- `new-chat-dialog.component.ts` — multi-select with chips and group name
- `conversation.service.ts` — `createGroupConversation()` method added

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-4-group-conversations.md#Story 4.2]
- [Source: _bmad-output/planning-artifacts/architecture.md#Message Flow Architecture]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Design Direction Decision — UX-DR8]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Design System Foundation — Design Tokens]
- [Source: _bmad-output/planning-artifacts/prd.md#FR15, FR16, FR19]
- [Source: _bmad-output/implementation-artifacts/4-1-group-conversation-creation.md]
- [Source: _bmad-output/project-context.md]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

### Completion Notes List

- Backend: Added system message creation in `CreateConversationCommandHandler.HandleGroupConversation()` — after group conversation is saved, resolves creator display name via `IIdentityService.GetDisplayNamesByIdsAsync()` and inserts a `Message` with `MessageType.System` and content "{displayName} created the group". Two SaveChangesAsync calls: one for conversation+participants, one for system message.
- Frontend: Added system message rendering as centered gray pill (`<div class="system-message" role="status">`) in chat-window template, using `$color-surface-hover` background and `$color-text-secondary` text with `$radius-full` border radius.
- Frontend: Made `shouldShowSender()` group-aware — returns `false` for Private conversations (no sender names needed), `true` for Group conversations when sender changes. System messages break consecutive grouping.
- Frontend: Added `isSystemMessage()` and `isGroupConversation()` helper methods to chat-window component.
- Verification: Sender name teal styling (`$color-primary`) and consecutive gap (`$space-xs` = 4px) were already correctly implemented.
- Tests: 89 backend unit tests (2 new), 276 frontend tests (9 new), 56 integration tests (3 new). Zero regressions. Release build: 0 warnings. Lint: all files pass.

### Change Log

- 2026-04-03: Implemented story 4-2 — group messaging display with system messages, group-aware sender names, and comprehensive test coverage.

### Review Findings

- [x] [Review][Defer] System message counted as unread for non-creator participants — deferred to Epic 5 (Story 5-3: Unread Tracking) which overhauls unread logic; single spurious badge clears on open, acceptable for MVP
- [x] [Review][Patch] Two-phase `SaveChangesAsync` not atomic — **FIXED**: consolidated to single save using navigation property for Message→Conversation [CreateConversationCommandHandler.cs]
- [x] [Review][Patch] System message `SentAt` uses second `DateTimeOffset.UtcNow` — **FIXED**: reuses captured `now` variable [CreateConversationCommandHandler.cs]
- [x] [Review][Patch] `role="status"` on system message divs causes repeated screen reader announcements — **FIXED**: removed ARIA role [chat-window.component.html]
- [x] [Review][Defer] System message not published via MediatR notification (no real-time delivery to other participants) — deferred, becomes relevant when SignalR group-join for non-creators is addressed
- [x] [Review][Defer] Non-creator participants don't auto-join SignalR group for new conversations — deferred, pre-existing architecture limitation
- [x] [Review][Defer] System message appears in `lastMessagePreview` on conversation list sidebar — deferred, pre-existing query behavior
- [x] [Review][Defer] No in-flight guard for concurrent create requests — deferred, pre-existing pattern

### File List

- src/Application/Messaging/Commands/CreateConversation/CreateConversationCommandHandler.cs (modified — added system message creation for group conversations)
- src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts (modified — group-aware shouldShowSender, isConsecutiveMessage with system message breaks, new helper methods)
- src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.html (modified — system message rendering as centered pill)
- src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.scss (modified — added .system-message styles)
- src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.spec.ts (modified — 11 new tests for system messages, group-aware sender display, group detection)
- tests/Application.UnitTests/Messaging/Commands/CreateConversation/CreateConversationCommandHandlerTests.cs (modified — 2 new tests for system message creation, updated setup with default mocks)
- tests/Infrastructure.IntegrationTests/Api/ConversationEndpointTests.cs (modified — 3 new integration tests for group messaging with system messages)
