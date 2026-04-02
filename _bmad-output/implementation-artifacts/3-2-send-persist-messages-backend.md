# Story 3.2: Send & Persist Messages (Backend)

Status: done

## Story

As a user,
I want to send text messages in a private conversation and retrieve message history,
so that I can communicate with team members and review past conversations.

## Acceptance Criteria

1. **Given** an authenticated user and a valid conversation they participate in, **When** a `SendMessageCommand` is dispatched via MediatR, **Then** the message is persisted to the database (EF Insert, await), **And** a `MessageSent` MediatR notification is published carrying the full Message entity + ConversationId, **And** the `Conversation.LastMessageAt` is updated, **And** the message Id (bigint) is returned.

2. **Given** a `SendMessageCommand` with empty content or content exceeding 4000 characters, **When** the command is validated by FluentValidation, **Then** a 400 Problem Details response is returned with validation errors.

3. **Given** an authenticated user and a conversation they participate in, **When** a `GetMessageHistoryQuery` is dispatched with `conversationId`, optional `before` cursor, and `limit` (default 50), **Then** messages are returned in descending Id order using cursor-based pagination: `WHERE ConversationId = @id AND Id < @cursor ORDER BY Id DESC`, **And** the response includes the message list, `hasMore` boolean, and `nextCursor` value, **And** each message DTO includes: `id`, `conversationId`, `senderId`, `senderDisplayName`, `content`, `sentAt`, `messageType`.

4. **Given** an authenticated user wants to start a private conversation, **When** a `CreateConversationCommand` is dispatched with the other user's ID, **Then** the system checks if a private conversation already exists between these two users, **And** if it exists, returns the existing conversation (no duplicate DMs), **And** if it does not exist, creates a new Conversation (Type=Private, Name=null) with both users as ConversationParticipants, **And** the conversation Id is returned.

5. **Given** the REST API endpoints, **When** requests are made, **Then** `POST /api/conversations` creates a conversation, **And** `GET /api/conversations/{id}/messages?before={cursor}&limit={n}` returns paginated history, **And** all endpoints require authentication and verify the user is a participant.

6. **Given** a user sends a message to a conversation they are not a participant in, **When** the command is processed, **Then** a 403 Forbidden Problem Details response is returned.

## Tasks / Subtasks

- [x] Task 1: Create SendMessageCommand + Handler + Validator (AC: #1, #2, #6)
  - [x] 1.1 Create `src/Application/Messaging/Commands/SendMessage/SendMessageCommand.cs` -- record: `public record SendMessageCommand(long ConversationId, string Content) : IRequest<long>;` (SenderId comes from IUser, NOT from the command)
  - [x] 1.2 Create `src/Application/Messaging/Commands/SendMessage/SendMessageCommandValidator.cs` -- FluentValidation: Content NotEmpty + MaximumLength(4000), ConversationId GreaterThan(0)
  - [x] 1.3 Create `src/Application/Messaging/Commands/SendMessage/SendMessageCommandHandler.cs`:
    - Inject `IApplicationDbContext`, `IUser`, `IPublisher` (MediatR)
    - Verify user is participant: query `ConversationParticipants` where `ConversationId` and `UserId == currentUser.Id` -- throw `ForbiddenAccessException` if not found
    - Create `Message` entity: ConversationId, SenderId = currentUser.Id, Content, SentAt = DateTimeOffset.UtcNow, MessageType = MessageType.Text
    - Add to `_db.Messages`, call `SaveChangesAsync`
    - Update `Conversation.LastMessageAt` = message.SentAt, call `SaveChangesAsync`
    - Publish `MessageSent` notification via `IPublisher`
    - Return message.Id

- [x] Task 2: Create MessageSent Notification (AC: #1)
  - [x] 2.1 Create `src/Application/Messaging/Notifications/MessageSent.cs` -- record implementing `INotification`: carries full `Message` entity + `ConversationId`
  - [x] 2.2 NOTE: No notification handlers in this story -- SignalR broadcast handler comes in Story 3.3. The notification is published but has no consumers yet. This is by design -- MediatR notifications with zero handlers are valid and don't throw.

- [x] Task 3: Create GetMessageHistoryQuery + Handler + DTO (AC: #3, #6)
  - [x] 3.1 Create `src/Application/Messaging/Queries/GetMessageHistory/GetMessageHistoryQuery.cs` -- record: `public record GetMessageHistoryQuery(long ConversationId, long? Before, int Limit = 50) : IRequest<MessageHistoryResponse>;`
  - [x] 3.2 Create `src/Application/Messaging/Queries/GetMessageHistory/MessageDto.cs` -- record: Id, ConversationId, SenderId, SenderDisplayName, Content, SentAt (DateTimeOffset), MessageType (string)
  - [x] 3.3 Create `src/Application/Messaging/Queries/GetMessageHistory/MessageHistoryResponse.cs` -- record: Messages (List<MessageDto>), HasMore (bool), NextCursor (long?)
  - [x] 3.4 Create `src/Application/Messaging/Queries/GetMessageHistory/GetMessageHistoryQueryHandler.cs`:
    - Inject `IApplicationDbContext`, `IUser`
    - Verify user is participant (same check as SendMessage) -- throw `ForbiddenAccessException`
    - Query messages: `_db.Messages.Where(m => m.ConversationId == query.ConversationId)`, apply cursor if `Before` is set: `.Where(m => m.Id < query.Before)`, order by `Id descending`, take `Limit + 1` (extra row to determine `HasMore`)
    - Join with Identity to get `SenderDisplayName` -- use `UserManager<ApplicationUser>` or add `IIdentityService.GetDisplayNamesByIds(IEnumerable<string> userIds)` method. See Dev Notes for approach.
    - Map to `MessageDto`, set `HasMore = results.Count > Limit`, set `NextCursor` = last message Id if HasMore

- [x] Task 4: Create CreateConversationCommand + Handler + Validator (AC: #4)
  - [x] 4.1 Create `src/Application/Messaging/Commands/CreateConversation/CreateConversationCommand.cs` -- record: `public record CreateConversationCommand(string OtherUserId) : IRequest<long>;`
  - [x] 4.2 Create `src/Application/Messaging/Commands/CreateConversation/CreateConversationCommandValidator.cs` -- OtherUserId NotEmpty
  - [x] 4.3 Create `src/Application/Messaging/Commands/CreateConversation/CreateConversationCommandHandler.cs`:
    - Inject `IApplicationDbContext`, `IUser`, `IIdentityService`
    - Validate OtherUserId is a real user via `IIdentityService` -- throw `NotFoundException` if not
    - Prevent self-conversation: if `OtherUserId == currentUser.Id`, throw `ValidationException`
    - Find existing: query for a Private conversation where BOTH users are participants (see Dev Notes for query pattern)
    - If found, return existing conversation Id
    - If not found, create new Conversation (Type=Private, Name=null, CreatedById=currentUser.Id, CreatedAt=now), add both ConversationParticipants, SaveChangesAsync, return new Id

- [x] Task 5: Create Conversations API Endpoint (AC: #5)
  - [x] 5.1 Create `src/Web/Endpoints/Conversations.cs` implementing `IEndpointGroup`:
    - `RoutePrefix = "/api/conversations"`
    - `Map()` registers:
      - `MapPost("/", CreateConversation)` -- dispatches `CreateConversationCommand`, returns `Ok(new { id })`. Requires auth.
      - `MapPost("/{conversationId:long}/messages", SendMessage)` -- accepts `SendMessageRequest { Content }` from body + conversationId from route. Dispatches `SendMessageCommand`. Returns `Ok(new { id })`. Requires auth.
      - `MapGet("/{conversationId:long}/messages", GetMessageHistory)` -- accepts conversationId from route + `before` and `limit` from query string. Dispatches `GetMessageHistoryQuery`. Returns `Ok(response)`. Requires auth.
  - [x] 5.2 Define request/response DTOs in the endpoint file: `CreateConversationRequest { OtherUserId }`, `SendMessageRequest { Content }`, etc.

- [x] Task 6: Add IIdentityService Method for Display Names (AC: #3)
  - [x] 6.1 Add `Task<Dictionary<string, string>> GetDisplayNamesByIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken)` to `IIdentityService` interface
  - [x] 6.2 Implement in `IdentityService` using `UserManager<ApplicationUser>` -- query users by Ids, return dictionary of userId -> displayName
  - [x] 6.3 Add `Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken)` to `IIdentityService` if not already present -- needed for CreateConversation validation

- [x] Task 7: Add [Authorize] Attribute to Commands (AC: #5, #6)
  - [x] 7.1 Add `[Authorize]` attribute to `SendMessageCommand`, `GetMessageHistoryQuery`, and `CreateConversationCommand` -- this triggers AuthorizationBehaviour in the MediatR pipeline
  - [x] 7.2 Verify `RequireAuthorization()` is chained on all endpoint routes in Conversations.cs

- [x] Task 8: Unit Tests (AC: #1-6)
  - [x] 8.1 Create `tests/Application.UnitTests/Messaging/Commands/SendMessage/SendMessageCommandHandlerTests.cs`:
    - Test: Valid message persists and returns Id
    - Test: Non-participant gets ForbiddenAccessException
    - Test: LastMessageAt is updated on conversation
    - Test: MessageSent notification is published
  - [x] 8.2 Create `tests/Application.UnitTests/Messaging/Commands/SendMessage/SendMessageCommandValidatorTests.cs`:
    - Test: Empty content fails validation
    - Test: Content > 4000 chars fails validation
    - Test: Valid content passes
    - Test: ConversationId <= 0 fails
  - [x] 8.3 Create `tests/Application.UnitTests/Messaging/Queries/GetMessageHistory/GetMessageHistoryQueryHandlerTests.cs`:
    - Test: Returns messages in descending Id order
    - Test: Cursor-based pagination works (before parameter)
    - Test: HasMore/NextCursor correct when more messages exist
    - Test: Non-participant gets ForbiddenAccessException
    - Test: Default limit is 50
  - [x] 8.4 Create `tests/Application.UnitTests/Messaging/Commands/CreateConversation/CreateConversationCommandHandlerTests.cs`:
    - Test: Creates new conversation when none exists
    - Test: Returns existing conversation when duplicate
    - Test: Self-conversation prevented
    - Test: Invalid user Id throws NotFoundException

- [x] Task 9: Integration Tests (AC: #1-6)
  - [x] 9.1 Create `tests/Infrastructure.IntegrationTests/Endpoints/ConversationEndpointTests.cs`:
    - Test: POST /api/conversations creates new private conversation (201/200 with id)
    - Test: POST /api/conversations with same users returns existing conversation
    - Test: POST /api/conversations/{id}/messages persists message
    - Test: GET /api/conversations/{id}/messages returns paginated history
    - Test: Cursor pagination returns correct page
    - Test: Unauthenticated request returns 401
    - Test: Non-participant request returns 403
    - Test: Invalid content returns 400 with validation errors

### Review Findings

- [x] [Review][Defer] Race condition in CreateConversation — concurrent requests can create duplicate private conversations. Sub-millisecond window on single-instance MVP; proper fix is unique composite index in future migration. [CreateConversationCommandHandler.cs:42-48] — deferred, needs DB schema change
- [x] [Review][Patch] Double SaveChangesAsync in SendMessage — merged into single atomic save. [SendMessageCommandHandler.cs]
- [x] [Review][Patch] No validator for GetMessageHistoryQuery — added GetMessageHistoryQueryValidator with Limit range 1-200. [GetMessageHistoryQueryValidator.cs]
- [x] [Review][Patch] Two SaveChangesAsync in CreateConversation — merged into single save using navigation property collection. [CreateConversationCommandHandler.cs]
- [x] [Review][Patch] Whitespace-only content passes SendMessage validation — added Must(!IsNullOrWhiteSpace) check. [SendMessageCommandValidator.cs]
- [x] [Review][Patch] Missing integration test for GET message history by non-participant returning 403 — added. [ConversationEndpointTests.cs]
- [x] [Review][Patch] Notification renamed from MessageSentNotification to MessageSent per spec. [MessageSent.cs]
- [x] [Review][Defer] Inconsistent CancellationToken on pre-existing IIdentityService methods — deferred, pre-existing [IIdentityService.cs]

## Dev Notes

### Existing Infrastructure to Reuse (DO NOT Recreate)

- **`IApplicationDbContext`** (`src/Application/Common/Interfaces/IApplicationDbContext.cs`): Already has `DbSet<Conversation>`, `DbSet<ConversationParticipant>`, `DbSet<Message>` from Story 3.1. Inject directly into handlers.
- **`IUser`** (`src/Application/Common/Interfaces/IUser.cs`): Provides `string? Id` and `List<string>? Roles` for the authenticated user. Inject this to get the current user -- do NOT access HttpContext directly.
- **`IIdentityService`** (`src/Application/Common/Interfaces/IIdentityService.cs`): Existing interface for identity operations. Add new methods here for user lookup.
- **`ForbiddenAccessException`** (`src/Application/Common/Exceptions/ForbiddenAccessException.cs`): Already exists, maps to 403 Problem Details via `ProblemDetailsExceptionHandler`.
- **`NotFoundException`** (`src/Application/Common/Exceptions/NotFoundException.cs`): Already exists, maps to 404.
- **`ValidationException`** (`src/Application/Common/Exceptions/ValidationException.cs`): Already exists, maps to 400 with field errors.
- **`AuthorizeAttribute`** (`src/Application/Common/Security/AuthorizeAttribute.cs`): Apply to command/query records for pipeline-level auth checks.
- **`AuthorizationBehaviour`**: MediatR pipeline behavior that checks `[Authorize]` attributes automatically.
- **`ValidationBehaviour`**: Picks up FluentValidation validators automatically -- just create the validator class.
- **MediatR `IPublisher`**: Use for publishing notifications. Inject `IPublisher` (not `IMediator`) when you only need to publish notifications.
- **`BaseEntity`** (`src/Domain/Common/BaseEntity.cs`): Message inherits from this, provides `Id` (long) and `CreatedAt`.
- **`ConversationType` enum**: Already exists with Private and Group values.
- **`MessageType` enum**: Already exists with Text, File, System values.
- **Endpoint helpers** (`src/Web/Infrastructure/EndpointRouteBuilderExtensions.cs`): Provides `MapGet`, `MapPost` overloads that auto-derive OpenAPI operation names from handler method names.

### Critical Implementation Patterns

**Command/Query Record Pattern (follow existing Identity module):**
```csharp
// Commands are records implementing IRequest<TResponse>
// Sender ID is NOT in the command -- it comes from IUser in the handler
[Authorize]
public record SendMessageCommand(long ConversationId, string Content) : IRequest<long>;
```

**Handler Pattern (follow existing LoginCommandHandler):**
```csharp
public class SendMessageCommandHandler(
    IApplicationDbContext db,
    IUser currentUser,
    IPublisher publisher) : IRequestHandler<SendMessageCommand, long>
{
    public async Task<long> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify participation
        // 2. Create and persist entity
        // 3. Update conversation timestamp
        // 4. Publish notification
        // 5. Return Id
    }
}
```

**Participant Verification Pattern:**
```csharp
// Use AnyAsync for efficient existence check
bool isParticipant = await db.ConversationParticipants
    .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == currentUser.Id, cancellationToken);
if (!isParticipant)
    throw new ForbiddenAccessException();
```

**Find-or-Create Private Conversation Pattern:**
```csharp
// Find existing private conversation between two users
// A private conversation exists if BOTH users are participants in a Private type conversation
long? existingId = await db.Conversations
    .Where(c => c.Type == ConversationType.Private)
    .Where(c => c.Participants.Any(p => p.UserId == currentUserId))
    .Where(c => c.Participants.Any(p => p.UserId == otherUserId))
    .Select(c => (long?)c.Id)
    .FirstOrDefaultAsync(cancellationToken);
```

**Cursor-Based Pagination Query Pattern:**
```csharp
IQueryable<Message> query = db.Messages
    .Where(m => m.ConversationId == request.ConversationId);

if (request.Before.HasValue)
    query = query.Where(m => m.Id < request.Before.Value);

// Take Limit + 1 to check HasMore
List<Message> messages = await query
    .OrderByDescending(m => m.Id)
    .Take(request.Limit + 1)
    .ToListAsync(cancellationToken);

bool hasMore = messages.Count > request.Limit;
if (hasMore)
    messages.RemoveAt(messages.Count - 1); // Remove the extra row
```

**Resolving SenderDisplayName (cross-module without direct reference):**
The Message entity has `SenderId` (string) but no navigation property to `ApplicationUser` (Clean Architecture -- Domain cannot reference Infrastructure). To get display names:
1. Add `GetDisplayNamesByIdsAsync` to `IIdentityService` (Application layer interface)
2. Implement in `IdentityService` (Infrastructure) using `UserManager<ApplicationUser>`
3. In the query handler: collect unique sender IDs from the message page, batch-fetch display names, map into DTOs

```csharp
List<string> senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
Dictionary<string, string> displayNames = await identityService
    .GetDisplayNamesByIdsAsync(senderIds, cancellationToken);
```

**MediatR Notification Pattern (MessageSent):**
```csharp
// In Application/Messaging/Notifications/MessageSent.cs
public record MessageSent(Message Message, long ConversationId) : INotification;

// In handler, after SaveChangesAsync:
await publisher.Publish(new MessageSent(message, message.ConversationId), cancellationToken);
```

**Endpoint Pattern (follow existing Auth.cs):**
```csharp
public class Conversations : IEndpointGroup
{
    public static string? RoutePrefix => "/api/conversations";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", CreateConversation)
            .RequireAuthorization();
        group.MapPost("/{conversationId:long}/messages", SendMessage)
            .RequireAuthorization();
        group.MapGet("/{conversationId:long}/messages", GetMessageHistory)
            .RequireAuthorization();
    }

    // Static handler methods receive ISender + params
    private static async Task<IResult> CreateConversation(
        ISender sender, CreateConversationRequest request)
    {
        long id = await sender.Send(new CreateConversationCommand(request.OtherUserId));
        return TypedResults.Ok(new { id });
    }
    // ...
}
```

### IUser.Id is string (ASP.NET Identity)

`IUser.Id` is `string?` (ASP.NET Identity uses GUID strings). All user ID comparisons and foreign keys use `string`. The `SenderId`, `UserId`, `CreatedById` fields on messaging entities are all `string`.

### Unit Test Pattern (follow existing Identity tests)

```csharp
// Mocks initialized in [SetUp], handler constructed with .Object references
// Use Shouldly assertions: result.ShouldBe(expected)
// Use Mock<IApplicationDbContext> with DbSet mocking for EF queries
// Test naming: Handle_Scenario_ExpectedOutcome
```

**Mocking DbSet for EF queries in unit tests:**
EF Core's `DbSet<T>` can be tricky to mock for async LINQ queries. Use the `MockDbSet` helper pattern -- create an in-memory queryable that supports async enumeration. Check if the project already has a helper for this (e.g., `TestDbSetExtensions` or similar). If not, create a simple async queryable wrapper.

### Integration Test Pattern (follow existing AuthEndpointTests.cs)

```csharp
[SetUp]
public async Task SetUp()
{
    _factory = new CustomWebApplicationFactory();
    await _factory.ResetDatabaseAsync();
    _client = _factory.CreateClient();
}

[TearDown]
public void TearDown()
{
    _client?.Dispose();
    _factory?.Dispose();
}
```

For authenticated requests in integration tests:
1. Register a test user via POST /api/auth/register
2. Login via POST /api/auth/login to get JWT access token
3. Set `_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token)`

To create test conversations and participants, either:
- Use the API endpoints (POST /api/conversations) -- preferred for integration tests
- Or access DbContext directly via DI scope for setup

### Build-Breaking Rules to Watch

- **`readonly` on private fields**: Any private field that can be readonly MUST be readonly -- build fails
- **`static` on local functions**: Local functions that don't capture state MUST be static -- build fails
- **TreatWarningsAsErrors**: All nullable warnings are build errors -- proper null checks required
- **No `var`**: Use explicit types per project convention
- **Namespace must match folder path**: e.g., `SimpleChat.Application.Messaging.Commands.SendMessage`
- **Primary constructors preferred**: Use for handler classes where type is apparent
- **CancellationToken on all async handler methods**
- **Explicit types, not `var`**

### Items NOT in Scope

- No SignalR hub or real-time broadcast -- comes in Story 3.3
- No frontend components -- comes in Stories 3.4-3.7
- No file/image attachment handling -- comes in Epic 6
- No full-text search endpoint -- comes in Epic 7
- No unread count tracking updates -- comes in Epic 5
- No conversation list endpoint -- comes in Story 3.5 (only create conversation + message history in this story)
- No typing indicators -- Phase 2 feature
- No message edit/delete -- Phase 2 feature

### Previous Story Intelligence (Story 3.1)

**Key learnings from Story 3.1:**
- `Conversation` inherits `BaseEntity` (NOT `BaseAuditableEntity`) -- it has its own `CreatedById` field
- `Message` inherits `BaseEntity` -- `SentAt` is the domain timestamp, `CreatedAt` from BaseEntity is also set by interceptor (both will have near-identical values)
- `ConversationParticipant` is standalone class (no BaseEntity) with composite PK
- Foreign keys to users are `string` type (ASP.NET Identity)
- `ApplicationDbContext` uses `ApplyConfigurationsFromAssembly()` -- configurations auto-discovered
- `ResetDatabaseAsync()` handles all tables dynamically -- no update needed
- EF configurations use Fluent API in `Infrastructure/Data/Configurations/`
- Build: 0 errors, 0 warnings; Unit tests: 49 pass; Integration tests: 32 pass

**Review findings deferred to this story:**
- Content/FileId cross-field invariants not enforced at data layer -- Text msg allows empty content, File msg allows null FileId. **Validation belongs in this story's command handlers.** SendMessageCommand should validate Content is not empty for Text messages.

### Git Intelligence

Recent commits show story-driven workflow: each story is created then implemented with review. Branch naming follows `story/X.Y` pattern. Most recent work: Story 3.1 (data model), project context creation, test infrastructure fixes.

### Project Structure Notes

**New files to create:**
```
src/Application/Messaging/Commands/SendMessage/SendMessageCommand.cs
src/Application/Messaging/Commands/SendMessage/SendMessageCommandHandler.cs
src/Application/Messaging/Commands/SendMessage/SendMessageCommandValidator.cs
src/Application/Messaging/Commands/CreateConversation/CreateConversationCommand.cs
src/Application/Messaging/Commands/CreateConversation/CreateConversationCommandHandler.cs
src/Application/Messaging/Commands/CreateConversation/CreateConversationCommandValidator.cs
src/Application/Messaging/Queries/GetMessageHistory/GetMessageHistoryQuery.cs
src/Application/Messaging/Queries/GetMessageHistory/GetMessageHistoryQueryHandler.cs
src/Application/Messaging/Queries/GetMessageHistory/MessageDto.cs
src/Application/Messaging/Queries/GetMessageHistory/MessageHistoryResponse.cs
src/Application/Messaging/Notifications/MessageSent.cs
src/Web/Endpoints/Conversations.cs
tests/Application.UnitTests/Messaging/Commands/SendMessage/SendMessageCommandHandlerTests.cs
tests/Application.UnitTests/Messaging/Commands/SendMessage/SendMessageCommandValidatorTests.cs
tests/Application.UnitTests/Messaging/Commands/CreateConversation/CreateConversationCommandHandlerTests.cs
tests/Application.UnitTests/Messaging/Queries/GetMessageHistory/GetMessageHistoryQueryHandlerTests.cs
tests/Infrastructure.IntegrationTests/Endpoints/ConversationEndpointTests.cs
```

**Files to modify:**
```
src/Application/Common/Interfaces/IIdentityService.cs  -- add GetDisplayNamesByIdsAsync, UserExistsAsync
src/Infrastructure/Identity/IdentityService.cs          -- implement new methods
```

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-3-real-time-private-messaging.md#Story 3.2]
- [Source: _bmad-output/planning-artifacts/architecture.md#Architecture Patterns -- Message Flow (Synchronous, Persist-First)]
- [Source: _bmad-output/planning-artifacts/architecture.md#API & Communication Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation Patterns -- Pattern Examples]
- [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture -- Cursor-based pagination, Message ID type]
- [Source: _bmad-output/planning-artifacts/architecture.md#Naming Patterns -- API and Backend naming conventions]
- [Source: _bmad-output/project-context.md#Clean Architecture + CQRS -- handler-per-file, MediatR pipeline]
- [Source: _bmad-output/project-context.md#Anti-Patterns -- No repository pattern]
- [Source: _bmad-output/project-context.md#Testing Rules -- NUnit, Shouldly, Testcontainers]
- [Source: _bmad-output/implementation-artifacts/3-1-conversation-message-data-model.md#Dev Notes -- entity design decisions, review findings]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

### Completion Notes List

- SendMessageCommand with handler and FluentValidation validator created -- persists message, updates Conversation.LastMessageAt, publishes MessageSentNotification via MediatR IPublisher
- MessageSentNotification record created implementing INotification -- no handlers yet (Story 3.3 will add SignalR broadcast handler)
- GetMessageHistoryQuery with handler and DTOs created -- cursor-based pagination using `Id < @cursor ORDER BY Id DESC`, Take(limit+1) pattern for HasMore detection, batch display name resolution via IIdentityService
- CreateConversationCommand with handler and validator created -- find-or-create pattern for private conversations, prevents self-conversation and validates user existence
- Conversations endpoint created implementing IEndpointGroup -- POST /api/conversations, POST /api/conversations/{id}/messages, GET /api/conversations/{id}/messages with RequireAuthorization()
- IIdentityService extended with GetDisplayNamesByIdsAsync and UserExistsAsync methods, implemented in IdentityService using UserManager queries
- [Authorize] attribute applied to all command/query records for MediatR pipeline authorization
- Participant verification in handlers throws ForbiddenAccessException (maps to 403 Problem Details)
- TestAsyncQueryProvider/TestAsyncEnumerable/TestAsyncEnumerator helper classes created for mocking EF Core async LINQ in unit tests
- Build: 0 errors, 0 warnings; Unit tests: 68/68 pass (19 new + 49 existing); Integration tests: 40/40 pass (8 new + 32 existing)

### Change Log

- 2026-04-02: Story 3.2 implemented -- messaging backend commands/queries, REST endpoints, unit and integration tests

### File List

New files:
- src/Application/Messaging/Commands/SendMessage/SendMessageCommand.cs
- src/Application/Messaging/Commands/SendMessage/SendMessageCommandHandler.cs
- src/Application/Messaging/Commands/SendMessage/SendMessageCommandValidator.cs
- src/Application/Messaging/Commands/CreateConversation/CreateConversationCommand.cs
- src/Application/Messaging/Commands/CreateConversation/CreateConversationCommandHandler.cs
- src/Application/Messaging/Commands/CreateConversation/CreateConversationCommandValidator.cs
- src/Application/Messaging/Queries/GetMessageHistory/GetMessageHistoryQuery.cs
- src/Application/Messaging/Queries/GetMessageHistory/GetMessageHistoryQueryHandler.cs
- src/Application/Messaging/Queries/GetMessageHistory/MessageDto.cs
- src/Application/Messaging/Queries/GetMessageHistory/MessageHistoryResponse.cs
- src/Application/Messaging/Notifications/MessageSentNotification.cs
- src/Web/Endpoints/Conversations.cs
- tests/Application.UnitTests/Messaging/Commands/SendMessage/SendMessageCommandHandlerTests.cs
- tests/Application.UnitTests/Messaging/Commands/SendMessage/SendMessageCommandValidatorTests.cs
- tests/Application.UnitTests/Messaging/Commands/CreateConversation/CreateConversationCommandHandlerTests.cs
- tests/Application.UnitTests/Messaging/Queries/GetMessageHistory/GetMessageHistoryQueryHandlerTests.cs
- tests/Infrastructure.IntegrationTests/Api/ConversationEndpointTests.cs

Modified files:
- src/Application/Common/Interfaces/IIdentityService.cs (added GetDisplayNamesByIdsAsync, UserExistsAsync)
- src/Infrastructure/Identity/IdentityService.cs (implemented new methods)
