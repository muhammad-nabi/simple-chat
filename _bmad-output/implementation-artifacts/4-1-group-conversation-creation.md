# Story 4.1: Group Conversation Creation

Status: done

## Story

As a team member,
I want to create a group conversation by selecting multiple team members and providing a group name,
so that I can communicate with several people at once in an organized channel.

## Acceptance Criteria (BDD)

### AC1: Multi-Select in New Chat Dialog
**Given** I click the "New Chat" button in the sidebar
**When** I select more than one team member in the member picker
**Then** a group name input field appears
**And** I can type a group name and press Enter/Create to create the group

### AC2: Group Conversation Created with Correct Type
**Given** I have selected 2+ team members and entered a group name
**When** I submit the create group form
**Then** a new Conversation is created with `Type = Group` and the provided name
**And** all selected users plus myself are added as ConversationParticipants
**And** I am navigated into the new group conversation with message input focused

### AC3: Group Appears in Conversation List
**Given** a group conversation has been created
**When** any participant views their conversation list
**Then** the group appears with the group name (not participant names)
**And** it is sorted by `LastMessageAt` like all other conversations

### AC4: SignalR Group Joining
**Given** a group conversation is created
**When** the creator submits the form
**Then** the creator joins the SignalR group for the new conversation
**And** other participants who are online also join the SignalR group (on next conversation list refresh)

### AC5: Validation Rules
**Given** I am creating a group conversation
**Then** the group name is required and cannot be empty
**And** the group name has a maximum length (e.g., 100 characters)
**And** at least 2 other participants must be selected (3 total including creator)
**And** the Create button is disabled until the group name is valid

### AC6: Backend Validation
**Given** a CreateConversation command with `Type = Group`
**When** the handler processes it
**Then** it validates all participant user IDs exist
**And** it validates the creator is authenticated
**And** it validates the group name is not empty
**And** it creates the conversation and participant records in a single transaction

## Tasks / Subtasks

- [x] Task 1: Backend - Extend CreateConversationCommand for group creation (AC: #2, #6)
  - [x] 1.1 Add `ParticipantIds` (list of string) and `GroupName` (string, nullable) to CreateConversationCommand
  - [x] 1.2 Update CreateConversationCommandValidator: when ParticipantIds has >1 entry, require GroupName not empty, max 100 chars; validate at least 2 participant IDs
  - [x] 1.3 Update CreateConversationCommandHandler: branch on participant count — if >1 participant, create with Type=Group, set Name=GroupName, add all participants + creator
  - [x] 1.4 Update or add unit tests for group creation path
- [x] Task 2: Backend - Update API endpoint for group creation (AC: #2, #6)
  - [x] 2.1 Update POST `/api/conversations` endpoint to accept new request shape (participantIds array + groupName)
  - [x] 2.2 Ensure backward compatibility: existing private conversation creation (single otherUserId) still works
  - [x] 2.3 Add integration tests for group conversation creation endpoint
- [x] Task 3: Frontend - Extend New Chat Dialog for multi-select (AC: #1, #5)
  - [x] 3.1 Add multi-select capability to NewChatDialogComponent: track selected members array, toggle selection on click
  - [x] 3.2 Show selected members as chips/tags above the search field
  - [x] 3.3 When >1 member selected, show group name input field with validation
  - [x] 3.4 Update dialog output: emit array of user IDs + group name when creating group
  - [x] 3.5 Add unit tests for multi-select behavior and group name validation
- [x] Task 4: Frontend - Extend ConversationService for group creation (AC: #2, #4)
  - [x] 4.1 Add `createGroupConversation(participantIds: string[], groupName: string)` method to ConversationService
  - [x] 4.2 Call POST `/api/conversations` with new payload shape
  - [x] 4.3 On success: join SignalR group, reload conversations, select new conversation, emit conversationCreated$
  - [x] 4.4 Add unit tests for group creation service method
- [x] Task 5: Frontend - Update ConversationListComponent flow (AC: #1, #3)
  - [x] 5.1 Update ConversationListComponent to handle group creation flow from dialog
  - [x] 5.2 Ensure group conversations display with group name (already handled by getDisplayName)
  - [x] 5.3 Add unit tests for the updated flow

### Review Findings

- [x] [Review][Decision] Enter key in group name input does not submit the form — FIXED: added `onGroupNameKeyDown` handler and `(keydown)` binding on group name input
- [x] [Review][Decision] `updateOn: 'blur'` delays Create button enablement — FIXED: changed to default `updateOn: 'change'` for real-time validation
- [x] [Review][Defer] No loading/spinner state on Create button during submission — deferred, low risk for MVP (dialog closes on success)
- [x] [Review][Patch] Duplicate participant IDs not rejected — FIXED: added `.Distinct().Count()` validator rule
- [x] [Review][Patch] No upper bound on ParticipantIds list size — FIXED: added max 50 participants validator rule
- [x] [Review][Patch] Empty/whitespace strings in ParticipantIds not validated — FIXED: added `.All(id => !string.IsNullOrWhiteSpace(id))` validator rule
- [x] [Review][Patch] Whitespace-only GroupName passes backend validation — FIXED: added `.Must(name.Trim().Length > 0)` rule + handler trims before storing
- [x] [Review][Patch] Both OtherUserId and ParticipantIds can be supplied simultaneously — FIXED: added mutual exclusion validator rule
- [x] [Review][Defer] Mutable `List<string>` in record command should be `IReadOnlyList` — record value semantics broken for collection field [CreateConversationCommand.cs] — deferred, pre-existing pattern in codebase
- [x] [Review][Defer] No group conversation duplicate check — unlike private conversations, groups with identical name/members can be created repeatedly [CreateConversationCommandHandler.cs] — deferred, acceptable for MVP; revisit if user feedback indicates issue

## Dev Notes

### Architecture & Patterns

**Backend Command Pattern — Extend, Don't Duplicate:**
The existing `CreateConversationCommand` and handler already create private conversations. EXTEND this command to support groups rather than creating a separate `CreateGroupConversationCommand`. The unified Conversation entity with type discriminator means the same command can handle both types:
- Private: single `otherUserId` (existing flow, keep working)
- Group: multiple `participantIds` + `groupName` (new flow)

**Key Files to Modify (Backend):**
- `src/Application/Messaging/Commands/CreateConversation/CreateConversationCommand.cs` — add optional `ParticipantIds` and `GroupName` fields
- `src/Application/Messaging/Commands/CreateConversation/CreateConversationCommandHandler.cs` — add group creation branch
- `src/Application/Messaging/Commands/CreateConversation/CreateConversationCommandValidator.cs` — add group validation rules
- `src/Web/Endpoints/Conversations.cs` — update request DTO to accept group fields

**Key Files to Modify (Frontend):**
- `src/Web/ClientApp/src/app/features/chat/components/new-chat-dialog/new-chat-dialog.component.ts` — add multi-select + group name input
- `src/Web/ClientApp/src/app/features/chat/components/new-chat-dialog/new-chat-dialog.component.html` — UI for multi-select and group name
- `src/Web/ClientApp/src/app/features/chat/components/new-chat-dialog/new-chat-dialog.component.scss` — styling for chips and group name field
- `src/Web/ClientApp/src/app/features/chat/services/conversation.service.ts` — add createGroupConversation method
- `src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.ts` — handle group creation output

### Critical Implementation Rules

**C# Rules:**
- Commands/Queries are `record` types; handlers are classes
- Guard clauses at method entry: `Guard.Against.Null()`, `Guard.Against.NullOrEmpty()`
- Custom exceptions, not Result types: throw `ValidationException`, `NotFoundException`
- CancellationToken on all async handler methods
- No repository pattern — inject `IApplicationDbContext` directly
- `readonly` on private fields (build fails otherwise)
- File-scoped namespaces: `namespace Foo;`
- Explicit types, not `var`
- Allman brace style (opening brace on new line)

**Angular/TypeScript Rules:**
- `inject()` for DI — NEVER constructor injection
- Standalone components only — no NgModules
- New control flow: `@if`, `@for`, `@else` — NOT `*ngIf`, `*ngFor`
- BehaviorSubject for state, expose via `.asObservable()`
- Single quotes for strings, 2-space indent, kebab-case files
- Reactive forms with FormBuilder for group name input
- `strict: true` — explicit types, null checks, no implicit any

**Validation:**
- FluentValidation on backend: validators co-located with commands
- Inline validation on frontend: on blur, red border + helper text
- Group name: required, max 100 chars
- Participants: at least 2 other users (3 total including creator)

### Existing Code to Reuse

**DO NOT reinvent these — they already exist and work:**
- `ConversationType.Group` enum value — already defined in `src/Domain/Common/Enums/ConversationType.cs`
- `Conversation` entity — already supports `Type` and `Name` fields for groups
- `ConversationParticipant` entity — already supports multiple participants per conversation
- `ConversationListDto` — already returns `Type`, `Name`, `OtherParticipants` for groups
- `getDisplayName()` in ConversationService — already returns `conversation.name` for groups
- `SignalRService.joinConversation()` — already works for any conversation ID
- Design tokens in `_tokens.scss` — use existing color, spacing, typography tokens
- Avatar and UnreadBadge shared components — already handle conversation display
- `NewChatDialogComponent` — extend this, don't create a new dialog component
- `UserService` — already provides team member list

### UX Requirements

**User Flow (from UX spec):**
1. User taps "New Chat" button in sidebar
2. Team member picker overlay opens (existing NewChatDialogComponent)
3. User selects multiple members (tap to toggle selection)
4. When >1 member selected, group name input appears (single text field)
5. User types group name, presses Enter or clicks Create
6. Group created — opens with message input focused

**Design Decisions:**
- User picks people, not conversation types — system infers Private vs Group from selection count
- Group name is the ONLY required field — no description, no settings, no avatar
- Filter-as-you-type for name search (already implemented)
- After creation, land directly in conversation with input focused
- Selected members shown as chips/tags above search field for visual clarity

**Form Pattern:**
- Inline validation on blur
- Group name label always visible above field, placeholder provides example (e.g., "Engineering Team")
- Create button: primary style, disabled until name is valid
- Loading state on submit (spinner in button)

**Accessibility:**
- Keyboard navigation for member selection (already implemented in NewChatDialog)
- Group name input must be focusable and have proper aria-label
- Selected member chips should be removable via keyboard (Backspace/Delete)

### Previous Story Intelligence (Epic 3)

**From Story 3.9 (most recent):**
- All 247 frontend tests pass — do not break existing tests
- Subscription cleanup: use `Subscription[]` with `ngOnDestroy()` or `DestroyRef` pattern
- SignalRService has `reconnected$` Subject for reconnection events
- ConversationService already handles `handleIncomingMessage` for any conversation type
- Review patches removed dead code and unnecessary `distinctUntilChanged()` — keep code lean

**Test counts to maintain:** 247 frontend tests, 83 backend unit tests must all continue to pass after changes.

### Testing Requirements

**Backend Unit Tests:**
- Test group creation with valid data (multiple participants, valid name)
- Test validation: empty group name, too-long group name, <2 participants
- Test that all participants are added as ConversationParticipants
- Test that creator is included as participant
- Test that conversation Type is set to Group
- Test backward compatibility: private conversation creation still works
- Use NUnit (`[Test]`, `[TestCase]`), Moq, Shouldly
- Mirror folder structure: `Application.UnitTests/Messaging/Commands/CreateConversation/`

**Backend Integration Tests:**
- Test POST `/api/conversations` with group payload returns success
- Test group appears in GET `/api/conversations` for all participants
- Test group name and type in response
- Use Testcontainers, CustomWebApplicationFactory

**Frontend Unit Tests:**
- Test multi-select toggle in NewChatDialog
- Test group name input appears when >1 member selected
- Test group name validation (required, max length)
- Test chip rendering for selected members
- Test ConversationService.createGroupConversation() makes correct API call
- Test conversation list displays group name correctly
- Use Jest, TestBed with standalone components

### Project Structure Notes

- All new backend code goes in existing Messaging module folders — no new module creation
- Frontend changes are within existing features/chat/ and the existing NewChatDialog component
- No new shared components needed — extend existing ones
- No database schema changes — Conversation entity already supports Group type
- No new packages/dependencies required

### References

- [Source: _bmad-output/planning-artifacts/prd.md — FR11-FR18 Group Chat Requirements]
- [Source: _bmad-output/planning-artifacts/architecture.md — Messaging Module Structure, Conversation Entity, API Patterns]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md — Journey 3 New Conversation Creation, Create Group Form]
- [Source: _bmad-output/project-context.md — Technology Stack, Critical Implementation Rules]
- [Source: _bmad-output/implementation-artifacts/3-9-connection-status-auto-reconnect.md — Previous Story Learnings]

## Change Log

- 2026-04-03: Story 4.1 implemented — group conversation creation (backend + frontend)

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

- Backend build: 0 warnings, 0 errors
- Backend unit tests: 87 passed (83 existing + 4 new)
- Frontend lint: All files pass
- Frontend tests: 267 passed (247 existing + 20 new)
- No regressions introduced

### Completion Notes List

- Extended CreateConversationCommand with optional ParticipantIds and GroupName fields (backward compatible)
- Added group creation branch in handler: validates participants exist, creates Group-type conversation, adds all participants + creator
- Updated FluentValidation: conditional rules for private vs group mode
- Updated POST /api/conversations endpoint to accept new request shape
- Added 4 integration tests for group conversation creation
- Extended NewChatDialogComponent with multi-select: chips, group name input, Create Group button
- Added GroupCreationResult interface and groupCreated output event
- Added createGroupConversation method to ConversationService (refactored shared handleConversationCreated)
- Wired ConversationListComponent to handle groupCreated event
- All acceptance criteria satisfied

### File List

**Modified:**
- src/Application/Messaging/Commands/CreateConversation/CreateConversationCommand.cs
- src/Application/Messaging/Commands/CreateConversation/CreateConversationCommandHandler.cs
- src/Application/Messaging/Commands/CreateConversation/CreateConversationCommandValidator.cs
- src/Web/Endpoints/Conversations.cs
- src/Web/ClientApp/src/app/features/chat/components/new-chat-dialog/new-chat-dialog.component.ts
- src/Web/ClientApp/src/app/features/chat/components/new-chat-dialog/new-chat-dialog.component.html
- src/Web/ClientApp/src/app/features/chat/components/new-chat-dialog/new-chat-dialog.component.scss
- src/Web/ClientApp/src/app/features/chat/components/new-chat-dialog/new-chat-dialog.component.spec.ts
- src/Web/ClientApp/src/app/features/chat/services/conversation.service.ts
- src/Web/ClientApp/src/app/features/chat/services/conversation.service.spec.ts
- src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.ts
- src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.html
- src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.spec.ts
- tests/Application.UnitTests/Messaging/Commands/CreateConversation/CreateConversationCommandHandlerTests.cs
- tests/Infrastructure.IntegrationTests/Api/ConversationEndpointTests.cs
