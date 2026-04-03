# Story 4.3: Browse & Join Groups

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user,
I want to browse available group conversations and join ones that interest me,
so that I can participate in team discussions relevant to my work.

## Acceptance Criteria

1. **Given** the sidebar
   **When** the user taps "Browse Groups" (visible text label entry point)
   **Then** a list of all group conversations is displayed (excluding those the user already belongs to)
   **And** each entry shows: group name, participant count, last message preview, last activity timestamp

2. **Given** the browse groups list
   **When** the user taps "Join" on a group
   **Then** the user is added as a ConversationParticipant
   **And** a system message is added: "{user} joined the group"
   **And** the group appears in the user's conversation list
   **And** the user can see the full message history from before they joined (FR13, FR16)

3. **Given** the browse groups list
   **When** no groups are available to join
   **Then** a friendly empty state is shown: "No groups to join right now. Create one!"

## Tasks / Subtasks

- [x] Task 1: Backend - GetBrowseGroupsQuery (AC: #1)
  - [x] 1.1 Create `src/Application/Messaging/Queries/GetBrowseGroups/GetBrowseGroupsQuery.cs` — `[Authorize] public record GetBrowseGroupsQuery : IRequest<List<BrowseGroupDto>>`
  - [x] 1.2 Create `src/Application/Messaging/Queries/GetBrowseGroups/BrowseGroupDto.cs` — `public record BrowseGroupDto(long Id, string Name, int ParticipantCount, string? LastMessagePreview, DateTimeOffset? LastMessageAt)`
  - [x] 1.3 Create `src/Application/Messaging/Queries/GetBrowseGroups/GetBrowseGroupsQueryHandler.cs` — Query all Group conversations where current user is NOT a participant. Include participant count, latest message preview (truncated to 100 chars), LastMessageAt. Order by LastMessageAt descending (most active first)
  - [x] 1.4 Unit test: `GetBrowseGroupsQueryHandlerTests.cs` — test excludes user's groups, returns correct fields, handles empty result

- [x] Task 2: Backend - JoinGroupCommand (AC: #2)
  - [x] 2.1 Create `src/Application/Messaging/Commands/JoinGroup/JoinGroupCommand.cs` — `[Authorize] public record JoinGroupCommand(long ConversationId) : IRequest<Unit>`
  - [x] 2.2 Create `src/Application/Messaging/Commands/JoinGroup/JoinGroupCommandValidator.cs` — validate ConversationId > 0
  - [x] 2.3 Create `src/Application/Messaging/Commands/JoinGroup/JoinGroupCommandHandler.cs` — Verify conversation exists and is Group type (throw NotFoundException if not). Verify user is not already a participant (throw ForbiddenAccessException if already joined). Add ConversationParticipant with JoinedAt = UtcNow. Add system message "{displayName} joined the group". Save in single SaveChangesAsync call. Publish domain event or MediatR notification for SignalR broadcast
  - [x] 2.4 Unit tests: `JoinGroupCommandHandlerTests.cs` — test happy path, already-participant rejection, conversation-not-found, private-conversation rejection
  - [x] 2.5 Unit test: `JoinGroupCommandValidatorTests.cs` — ConversationId validation

- [x] Task 3: Backend - Endpoint registration (AC: #1, #2)
  - [x] 3.1 In `src/Web/Endpoints/Conversations.cs`, add `GET /api/conversations/browse` → `GetBrowseGroups` (maps to GetBrowseGroupsQuery)
  - [x] 3.2 In `src/Web/Endpoints/Conversations.cs`, add `POST /api/conversations/{conversationId:long}/join` → `JoinGroup` (maps to JoinGroupCommand)
  - [x] 3.3 Both endpoints require authorization
  - [x] 3.4 Integration test: browse returns groups user isn't in; join adds user and creates system message; browse no longer shows joined group

- [x] Task 4: Frontend - BrowseGroupsComponent (AC: #1, #3)
  - [x] 4.1 Create `src/Web/ClientApp/src/app/features/chat/components/browse-groups/browse-groups.component.ts` — standalone component with inject() DI, BehaviorSubject for loading/error states
  - [x] 4.2 Create `browse-groups.component.html` — list of available groups, each showing: group name, participant count, last message preview, last activity (relative time). "Join" button on each row. Empty state: "No groups to join right now. Create one!" Loading skeleton while fetching
  - [x] 4.3 Create `browse-groups.component.scss` — match conversation list item styling (12px vertical padding, 16px horizontal). "Join" button styled with `$color-primary` (`#128C7E`). Use design tokens consistently
  - [x] 4.4 Emit `groupJoined` event with conversation ID when join completes
  - [x] 4.5 Emit `closed` event when user navigates back
  - [x] 4.6 Jest tests: renders group list, shows empty state, join button triggers API call

- [x] Task 5: Frontend - Sidebar integration (AC: #1)
  - [x] 5.1 In `conversation-list.component.html`, add "Browse Groups" button below "New Chat" in `.list-header`. Text label, visible at all times (UX spec: "Browse Groups" entry point must be visible in the sidebar with a text label)
  - [x] 5.2 In `conversation-list.component.ts`, add `showBrowseGroups` boolean state toggle
  - [x] 5.3 Conditionally render `<app-browse-groups>` when `showBrowseGroups` is true (replaces conversation list view, similar to how new-chat-dialog works)
  - [x] 5.4 On `groupJoined` event: close browse view, reload conversations, select the joined group
  - [x] 5.5 On `closed` event: close browse view, return to conversation list

- [x] Task 6: Frontend - ConversationService integration (AC: #1, #2)
  - [x] 6.1 In `conversation.service.ts`, add `loadBrowseGroups()` — `GET /api/conversations/browse`, returns Observable<BrowseGroupDto[]>
  - [x] 6.2 In `conversation.service.ts`, add `joinGroup(conversationId: number)` — `POST /api/conversations/{conversationId}/join`, on success calls `loadConversations()` to refresh the list
  - [x] 6.3 After successful join, the ChatHub auto-joins the user to the SignalR group on next connection/reconnect (existing behavior via ChatHub.OnConnectedAsync loading all user conversations)

- [x] Task 7: Frontend - SignalR group auto-join for joined groups (AC: #2)
  - [x] 7.1 Verify that after joining a group and the conversation list refreshes, the ChatHub connection handles the new conversation. If ChatHub.OnConnectedAsync already loads all user conversations, a reconnect or `JoinConversation` call may be needed
  - [x] 7.2 In `conversation.service.ts` or `signalr.service.ts`, after successful join API response, invoke `JoinConversation` on the hub to subscribe to real-time messages immediately (without waiting for reconnect)

## Dev Notes

### What Already Exists (DO NOT Recreate)

- **Conversation entity** (`src/Domain/Messaging/Conversation.cs`) — has `Type` (Private/Group), `Name`, `CreatedById`, `LastMessageAt`, `Participants` collection, `Messages` collection
- **ConversationParticipant entity** (`src/Domain/Messaging/ConversationParticipant.cs`) — has `ConversationId`, `UserId`, `JoinedAt`, `LastReadMessageId`
- **Message entity** — has `MessageType` enum (Text, File, System) for system messages
- **GetConversationsQueryHandler** — pattern for querying conversations with participant filtering, display name resolution, message preview truncation. Reuse the `TruncatePreview` pattern (100 chars, append "...")
- **CreateConversationCommandHandler** — pattern for adding participants and system messages. Reuse the system message pattern: `MessageType.System`, `Content = "{displayName} joined the group"`, resolve display name via `IIdentityService.GetDisplayNamesByIdsAsync()`
- **ConversationListDto/ParticipantDto** (`src/Application/Messaging/Queries/GetConversations/ConversationListDto.cs`) — existing DTO pattern for conversation data
- **Conversations endpoint** (`src/Web/Endpoints/Conversations.cs`) — implements `IEndpointGroup` with `RoutePrefix = "/api/conversations"`. Add new endpoints here, do NOT create a new endpoint file
- **ChatHub** (`src/Web/Hubs/ChatHub.cs`) — has `JoinConversation(long conversationId)` method. Auto-joins all user conversations on connect
- **ConversationService** (`src/Web/ClientApp/.../services/conversation.service.ts`) — has `loadConversations()`, `createGroupConversation()`, `selectConversation()`. Add browse/join methods here
- **conversation-list component** — has `.list-header` with "New Chat" button, conditional new-chat-dialog. Add "Browse Groups" button and browse-groups component here
- **Avatar component** (`<app-avatar>`) — accepts `displayName`, `userId`, `size` inputs. Reuse for group list items
- **RelativeTimePipe** — already used in conversation list for timestamps. Reuse in browse groups list
- **UnreadBadge component** — not needed for browse groups (user hasn't joined yet)

### Architecture Compliance

- **CQRS pattern**: New query `GetBrowseGroupsQuery` for read, new command `JoinGroupCommand` for write. Handler-per-file vertical slices in feature folders
- **MediatR pipeline**: Query/command go through Logging → UnhandledException → Authorization → Validation → Performance pipeline automatically
- **Persist-first**: System message and participant row must be saved to DB before any SignalR broadcast
- **No repository pattern**: Inject `IApplicationDbContext` directly in handlers
- **Custom exceptions**: `NotFoundException` if conversation doesn't exist, `ForbiddenAccessException` if already a participant or conversation is Private
- **Problem Details errors**: Auto-mapped by ProblemDetailsExceptionHandler
- **Cross-module via MediatR**: Resolve display names via `IIdentityService`, not direct DB query on users
- **Domain events optional**: A `UserJoinedGroup` domain event could trigger the SignalR broadcast, following the `MessageSent` notification pattern. Alternatively, publish a MediatR notification from the handler

### Endpoint Registration Pattern

Follow the existing pattern in `Conversations.cs`:
```csharp
// In Map():
groupBuilder.MapGet(GetBrowseGroups, "browse")
    .RequireAuthorization();

groupBuilder.MapPost(JoinGroup, "{conversationId:long}/join")
    .RequireAuthorization();

// Static handler methods:
public static async Task<Ok<List<BrowseGroupDto>>> GetBrowseGroups(ISender sender) { ... }
public static async Task<Ok> JoinGroup(ISender sender, long conversationId) { ... }
```

### Query Implementation Guidance

**GetBrowseGroupsQueryHandler key logic:**
```
1. Get current user ID from IUser
2. Get all conversationIds where user IS a participant
3. Query Conversations where Type == Group AND Id NOT IN user's conversationIds
4. For each: count participants, get latest message preview, get LastMessageAt
5. Order by LastMessageAt DESC (most active groups first)
6. Return List<BrowseGroupDto>
```

### Command Implementation Guidance

**JoinGroupCommandHandler key logic:**
```
1. Get current user ID from IUser
2. Load conversation by ID — throw NotFoundException if null
3. Verify conversation.Type == Group — throw ForbiddenAccessException("Cannot join a private conversation") if Private
4. Check if user is already a participant — throw ForbiddenAccessException("Already a member of this group") if exists
5. Add ConversationParticipant(ConversationId, UserId, JoinedAt = UtcNow)
6. Resolve display name via IIdentityService.GetDisplayNamesByIdsAsync()
7. Add Message(ConversationId, SenderId = userId, Content = "{displayName} joined the group", MessageType = System, SentAt = UtcNow)
8. Single SaveChangesAsync call
9. Optionally publish MediatR notification for SignalR broadcast of the system message
```

### C# Critical Rules

- Commands/Queries are **records**, handlers are **classes**
- `readonly` on private fields (TreatWarningsAsErrors)
- File-scoped namespaces, explicit types (not `var`), Allman braces
- `CancellationToken` on all async handler methods
- Guard clauses: `Guard.Against.Null()` from Ardalis.GuardClauses
- Custom exceptions: `NotFoundException`, `ForbiddenAccessException` (not Result types)
- Global usings exist per project — check `GlobalUsings.cs` before adding usings
- Nullable enabled — proper null checks, no `null!`
- LF line endings, 4-space indent, final newline

### Angular Critical Rules

- `inject()` for DI (never constructor injection)
- Standalone components only, no NgModules
- New control flow: `@if`, `@for`, `@else` (not `*ngIf`, `*ngFor`)
- BehaviorSubject for state, expose via `.asObservable()`
- Single quotes, 2-space indent, kebab-case files
- `strict: true` — explicit return types, null checks
- `withCredentials` not needed for non-auth API calls (only auth endpoints need it)
- No path aliases in source code — use relative imports

### Testing Standards

**Backend unit tests (NUnit + Moq + Shouldly):**
- `[Test]`, `[TestCase]` — NOT `[Fact]`/`[Theory]`
- `result.ShouldBe(expected)` — NOT `Assert.AreEqual`
- Method naming: `MethodUnderTest_Scenario_ExpectedOutcome`
- Mirror Application folder structure: `Messaging/Queries/GetBrowseGroups/GetBrowseGroupsQueryHandlerTests.cs`
- Moq: `new Mock<IApplicationDbContext>()`, `.Setup().ReturnsAsync()`, `.Verify()`
- FluentValidation.TestHelper: `result.ShouldHaveValidationErrorFor(x => x.ConversationId)`

**Integration tests (Testcontainers — real DB):**
- Use existing `CustomWebApplicationFactory` and `TestcontainersFixture`
- `_client.GetFromJsonAsync<List<BrowseGroupDto>>("/api/conversations/browse")`
- `_client.PostAsync($"/api/conversations/{id}/join", null)`
- No mocking — real database, real HTTP pipeline

**Frontend tests (Jest):**
- TestBed with standalone component imports
- Mock ConversationService with jest.fn()
- DOM assertions: `fixture.nativeElement.querySelector()`

### Design Token References

| Token | Value | Usage |
|-------|-------|-------|
| `$color-primary` | `#128C7E` | "Join" button background |
| `$color-white` | `#FFFFFF` | "Join" button text |
| `$color-surface` | `#F0F2F5` | Browse groups panel background |
| `$color-surface-hover` | `#E4E6EB` | Group item hover state |
| `$color-text-primary` | `#111B21` | Group name text |
| `$color-text-secondary` | `#667781` | Participant count, last message preview, timestamps |
| `$space-md` | `12px` | Vertical padding inside group items |
| `$space-lg` | `16px` | Horizontal padding, section spacing |
| `$size-sm` | `0.8125rem` (13px) | Preview text, metadata |
| `$type-body` | `0.9375rem` (15px) | Group name text |
| `$radius-md` | `8px` | "Join" button border radius |

### UX Requirements

- **"Browse Groups" must be a visible text label in the sidebar** — not hidden in a menu or behind an icon (UX spec explicitly calls this out as a discoverability requirement)
- **Empty state must be warm and encouraging**: "No groups to join right now. Create one!" — matches the app's warm microcopy tone
- **Group items should show**: group name, participant count, last message preview, last activity time — information density matching the conversation list
- **Back navigation**: clear way to return to the conversation list from browse view
- **No pagination needed in V1**: small teams (5-200 users) won't have enough groups to require pagination

### Project Structure Notes

**New files to create:**
```
src/Application/Messaging/Queries/GetBrowseGroups/
  GetBrowseGroupsQuery.cs
  GetBrowseGroupsQueryHandler.cs
  BrowseGroupDto.cs

src/Application/Messaging/Commands/JoinGroup/
  JoinGroupCommand.cs
  JoinGroupCommandHandler.cs
  JoinGroupCommandValidator.cs

src/Web/ClientApp/src/app/features/chat/components/browse-groups/
  browse-groups.component.ts
  browse-groups.component.html
  browse-groups.component.scss
  browse-groups.component.spec.ts

tests/Application.UnitTests/Messaging/Queries/GetBrowseGroups/
  GetBrowseGroupsQueryHandlerTests.cs

tests/Application.UnitTests/Messaging/Commands/JoinGroup/
  JoinGroupCommandHandlerTests.cs
  JoinGroupCommandValidatorTests.cs
```

**Files to modify:**
```
src/Web/Endpoints/Conversations.cs (add 2 endpoints + using)
src/Web/ClientApp/src/app/features/chat/services/conversation.service.ts (add 2 methods)
src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.ts (add browse toggle)
src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.html (add button + browse-groups)
src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.scss (browse button styles)
tests/Infrastructure.IntegrationTests/Api/ConversationEndpointTests.cs (add browse + join tests)
```

### Previous Story Intelligence (Story 4.2)

**Key learnings from 4-2 review:**
- System message creation pattern: resolve display name via `IIdentityService.GetDisplayNamesByIdsAsync()`, create Message with `MessageType.System`, consolidate to single `SaveChangesAsync` (4-2 review found two-phase save was problematic — fixed to single save)
- System messages should NOT have `role="status"` ARIA attribute (removed in 4-2 review — causes repeated screen reader announcements)
- The `SentAt` timestamp should reuse a captured `DateTimeOffset.UtcNow` variable, not call it multiple times
- Test counts at end of 4-2: 89 backend unit tests, 276 frontend tests, 56 integration tests — do not regress
- Deferred issue: system message not published via MediatR notification (no real-time delivery to other participants) — this story should consider whether joined-group system messages need real-time broadcast
- Deferred issue: non-creator participants don't auto-join SignalR group for new conversations — this affects the join flow; after joining, the user needs to explicitly call `JoinConversation` on the hub

**Files modified in 4-2 that are relevant:**
- `CreateConversationCommandHandler.cs` — system message creation pattern (reuse for join message)
- `chat-window.component.ts/html/scss` — system message rendering (already handles "{user} joined the group" display)

### Git Intelligence

Recent commits show story-per-commit pattern. The Epic-4 branch has 4-1 and 4-2 already implemented. Both stories were created, implemented, and reviewed as atomic units.

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-4-group-conversations.md#Story 4.3]
- [Source: _bmad-output/planning-artifacts/architecture.md#API & Communication Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Message Flow Architecture]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Browse Groups entry point]
- [Source: _bmad-output/planning-artifacts/prd.md#FR13, FR16, FR18]
- [Source: _bmad-output/implementation-artifacts/4-2-group-messaging-history.md]
- [Source: _bmad-output/project-context.md]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

### Completion Notes List

- Backend: Created `GetBrowseGroupsQuery` with handler that queries Group conversations where the current user is NOT a participant. Returns group name, participant count, truncated message preview (100 chars), and LastMessageAt. Ordered by most recently active.
- Backend: Created `JoinGroupCommand` with handler that validates conversation exists + is Group type + user not already a participant, then adds ConversationParticipant + system message "{displayName} joined the group" in single SaveChangesAsync. Updates conversation.LastMessageAt.
- Backend: Created `JoinGroupCommandValidator` with ConversationId > 0 validation.
- Backend: Added `GET /api/conversations/browse` and `POST /api/conversations/{conversationId}/join` endpoints to `Conversations.cs`. Both require authorization.
- Frontend: Created `BrowseGroupsComponent` — standalone component with loading skeleton, group list with Join buttons, empty state "No groups to join right now. Create one!", back navigation.
- Frontend: Added `loadBrowseGroups()` and `joinGroup()` methods to `ConversationService`. `joinGroup` calls SignalR `JoinConversation` after successful API join for immediate real-time message reception.
- Frontend: Added "Browse Groups" visible text button to conversation list sidebar header. Clicking opens browse-groups view (replaces conversation list). On join success, returns to conversation list with joined group selected.
- Tests: 103 backend unit tests (14 new), 65 integration tests (9 new), 288 frontend tests (12 new). Zero regressions. Release build: 0 warnings. Lint: all files pass.

### Review Findings

- [x] [Review][Patch] Subscription leak in `joinGroup()` — `conversationCreated` subscription never cleaned up on error path; can emit wrong conversationId on next successful join [browse-groups.component.ts:51-55] — **FIXED**: added dual subscription (success + error) with shared cleanup function
- [x] [Review][Patch] Join error leaves all Join buttons permanently disabled — `_joiningId$` never reset to null when service error occurs [browse-groups.component.ts:47-55] — **FIXED**: error subscription resets `_joiningId$` to null
- [x] [Review][Defer] Concurrent join race condition — no DB unique constraint on (ConversationId, UserId) pair; two simultaneous join requests can both pass `alreadyParticipant` check — deferred, requires schema change
- [x] [Review][Defer] No pagination on `GetBrowseGroupsQuery` — loads all groups into memory; acceptable for small team app (5-200 users) per spec
- [x] [Review][Defer] SignalR join error silently swallowed — user won't receive real-time messages until reconnect [conversation.service.ts:122-124] — deferred, pre-existing pattern from story 3.9
- [x] [Review][Defer] Null ordering instability — groups with null `LastMessageAt` sort unpredictably in `GetBrowseGroupsQueryHandler` [GetBrowseGroupsQueryHandler.cs:82] — deferred, edge case for newly created groups with no messages
- [x] [Review][Defer] No error state display in browse groups UI — API failure shows empty list instead of error message [browse-groups.component.ts:40-43] — deferred, low impact UX polish
- [x] [Review][Defer] No SignalR broadcast for "joined the group" system message — no domain event or MediatR notification published [JoinGroupCommandHandler.cs] — deferred, same as deferred item from story 4-2 for "created the group" system message

### Change Log

- 2026-04-03: Implemented story 4-3 — browse & join groups with full-stack implementation, comprehensive test coverage.

### File List

- src/Application/Messaging/Queries/GetBrowseGroups/GetBrowseGroupsQuery.cs (new)
- src/Application/Messaging/Queries/GetBrowseGroups/BrowseGroupDto.cs (new)
- src/Application/Messaging/Queries/GetBrowseGroups/GetBrowseGroupsQueryHandler.cs (new)
- src/Application/Messaging/Commands/JoinGroup/JoinGroupCommand.cs (new)
- src/Application/Messaging/Commands/JoinGroup/JoinGroupCommandValidator.cs (new)
- src/Application/Messaging/Commands/JoinGroup/JoinGroupCommandHandler.cs (new)
- src/Web/Endpoints/Conversations.cs (modified — added browse and join endpoints)
- src/Web/ClientApp/src/app/features/chat/services/conversation.service.ts (modified — added loadBrowseGroups, joinGroup, BrowseGroupDto)
- src/Web/ClientApp/src/app/features/chat/components/browse-groups/browse-groups.component.ts (new)
- src/Web/ClientApp/src/app/features/chat/components/browse-groups/browse-groups.component.html (new)
- src/Web/ClientApp/src/app/features/chat/components/browse-groups/browse-groups.component.scss (new)
- src/Web/ClientApp/src/app/features/chat/components/browse-groups/browse-groups.component.spec.ts (new)
- src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.ts (modified — added browse groups toggle and handlers)
- src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.html (modified — added Browse Groups button and browse-groups component)
- src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.scss (modified — added browse-groups-button styles)
- tests/Application.UnitTests/Messaging/Queries/GetBrowseGroups/GetBrowseGroupsQueryHandlerTests.cs (new)
- tests/Application.UnitTests/Messaging/Commands/JoinGroup/JoinGroupCommandHandlerTests.cs (new)
- tests/Application.UnitTests/Messaging/Commands/JoinGroup/JoinGroupCommandValidatorTests.cs (new)
- tests/Infrastructure.IntegrationTests/Api/ConversationEndpointTests.cs (modified — added 9 browse/join integration tests)
