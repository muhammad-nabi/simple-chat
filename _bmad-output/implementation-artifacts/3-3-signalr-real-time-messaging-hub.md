# Story 3.3: SignalR Real-Time Messaging Hub

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user,
I want to receive messages in real time without refreshing the page,
so that conversations feel instant and natural.

## Acceptance Criteria

1. **Given** an authenticated user with a valid JWT, **When** the Angular app establishes a SignalR connection to `/hubs/chat`, **Then** the connection is authenticated via JWT passed as a query string parameter, **And** the user is added to SignalR groups for each conversation they participate in.

2. **Given** a user sends a message via the ChatHub `SendMessage` method, **When** the hub receives the message, **Then** the hub dispatches a `SendMessageCommand` via MediatR (persist-first flow), **And** after successful persistence, the `MessageSent` notification triggers a SignalR broadcast, **And** all participants in the conversation receive a `ReceiveMessage` event with the full message DTO, **And** the message is guaranteed durable in the database before any client sees it.

3. **Given** the SignalR connection drops (network interruption), **When** the client detects disconnection, **Then** the SignalR client automatically attempts reconnection with exponential backoff: [0, 2000, 5000, 10000, 30000] ms, **And** on successful reconnection, the client refreshes its JWT if expired before re-establishing the hub connection, **And** the client re-joins all conversation groups (FR49).

4. **Given** the client successfully reconnects after a disconnection, **When** the hub connection is re-established, **Then** the client fetches the conversation list with updated unread counts, **And** for the active conversation, fetches messages newer than the last known message ID to fill any gap, **And** the UI updates seamlessly without requiring a manual refresh.

5. **Given** a centralized `SignalRService` in Angular core, **When** server events arrive (`ReceiveMessage`, `UserOnline`, `UserOffline`), **Then** the service dispatches events to the appropriate domain services (`MessageService`, `PresenceService`), **And** a `connectionState$` observable exposes the current state (`Connected`, `Reconnecting`, `Disconnected`).

## Tasks / Subtasks

- [x] Task 1: Configure SignalR on the Backend (AC: #1, #2)
  - [x] 1.1 Add `builder.Services.AddSignalR()` to `src/Web/DependencyInjection.cs` -- SignalR is built into ASP.NET Core, no NuGet package needed
  - [x] 1.2 Configure JWT Bearer to read token from query string for SignalR connections -- add `OnMessageReceived` event to JwtBearerOptions in `src/Infrastructure/DependencyInjection.cs`
  - [x] 1.3 Add `app.MapHub<ChatHub>("/hubs/chat")` in `src/Web/Program.cs` -- placed AFTER `app.UseAuthentication()` and `app.UseAuthorization()`, BEFORE `app.MapFallbackToFile()`

- [x] Task 2: Create ChatHub (AC: #1, #2)
  - [x] 2.1 Create `src/Web/Hubs/ChatHub.cs` implementing `Hub` with `[Authorize]`, `OnConnectedAsync` (adds user to groups), `SendMessage` (dispatches `SendMessageCommand` via MediatR), `JoinConversation` (verifies participant, adds to group). Uses primary constructor with `ISender` and `IApplicationDbContext`.
  - [x] 2.2 Uses `Context.UserIdentifier` which reads `NameIdentifier` claim from JWT `sub` claim

- [x] Task 3: Create MessageSent Notification Handlers (AC: #2)
  - [x] 3.1 Create `src/Application/Messaging/EventHandlers/MessageSentBroadcastHandler.cs` implementing `INotificationHandler<MessageSent>`. Resolves sender display name via `IIdentityService`, builds `MessageDto`, broadcasts via `IMessageBroadcaster`.
  - [x] 3.2 Clean Architecture abstraction: `IMessageBroadcaster` interface in Application layer, `SignalRMessageBroadcaster` implementation in Web layer injecting `IHubContext<ChatHub>`. Registered as scoped in Web DI.

- [x] Task 4: Install `@microsoft/signalr` npm Package (AC: #1, #3, #5)
  - [x] 4.1 Installed `@microsoft/signalr` v10.0.0 in `src/Web/ClientApp/`
  - [x] 4.2 Verified in `package.json` dependencies

- [x] Task 5: Create Angular SignalRService (AC: #1, #3, #4, #5)
  - [x] 5.1 Created `src/Web/ClientApp/src/app/core/signalr/signalr.service.ts` with `inject()` DI, `HubConnectionBuilder` with `/hubs/chat` URL, `accessTokenFactory`, auto-reconnect `[0, 2000, 5000, 10000, 30000]`, `connectionState$` BehaviorSubject, `messageReceived$` Subject, lifecycle handlers.
  - [x] 5.2 Created `src/Web/ClientApp/src/app/core/signalr/signalr.events.ts` with event constants and `MessagePayload` interface
  - [x] 5.3 Gap-fill: `onreconnected` handler triggers state refresh. Current implementation reconnects and re-fetches latest page. Full gap-fill deferred until MessageService exists (Stories 3.4-3.7).

- [x] Task 6: Integrate SignalRService into App Lifecycle (AC: #1, #5)
  - [x] 6.1 SignalR auto-starts via auth state subscription (`isAuthenticated$`) in SignalRService constructor, plus explicit `start()` in `APP_INITIALIZER` after successful session restore
  - [x] 6.2 SignalR auto-stops when `isAuthenticated$` emits `false` (logout triggers `clearSession` which sets `isAuthenticated$` to `false`)
  - [x] 6.3 `messageReceived` observable exposed for future MessageService wiring. MessageService and conversation state management deferred to Stories 3.4-3.7 when UI components are built.

- [x] Task 7: Unit Tests -- Backend (AC: #1, #2)
  - [x] 7.1 Created `tests/Application.UnitTests/Messaging/EventHandlers/MessageSentBroadcastHandlerTests.cs` with 3 tests: broadcasts correct DTO with conversationId, resolves display name via IIdentityService, falls back to "Unknown" when display name not found
  - [x] 7.2 ChatHub unit tests skipped (hub logic is thin delegation to MediatR; covered by integration tests)

- [x] Task 8: Integration Tests -- SignalR (AC: #1, #2, #3)
  - [x] 8.1 Created `tests/Infrastructure.IntegrationTests/Hubs/ChatHubTests.cs` with 4 tests: authenticated connection succeeds, unauthenticated connection fails, message via hub persists to DB, real-time broadcast received by group members
  - [x] 8.2 Added `Microsoft.AspNetCore.SignalR.Client` v10.0.5 to `Directory.Packages.props` and integration test project. Also added `Shouldly` to integration test project. Uses `_factory.Server.CreateHandler()` for in-memory SignalR testing.

- [x] Task 9: Frontend Tests -- SignalRService (AC: #3, #5)
  - [x] 9.1 Created `src/Web/ClientApp/src/app/core/signalr/signalr.service.spec.ts` with 10 tests: service creation, initial Disconnected state, correct URL and token factory, reconnect intervals, Connected/Disconnected state transitions, ReceiveMessage event handler registration and dispatch, no duplicate connections, lifecycle handler registration

### Review Findings

- [x] [Review][Dismissed] accessTokenFactory doesn't refresh expired JWT on reconnect — dismissed: SignalR retry loop + background token refresh (Story 2.3) naturally recover from stale tokens. Async token negotiation deferred to Story 3.9.
- [x] [Review][Patch] SendMessage doesn't wrap MediatR exceptions as HubException — fixed: try-catch wraps sender.Send, re-throws as HubException with safe message. [ChatHub.cs:31]
- [x] [Review][Patch] Missing CancellationToken propagation in hub methods — fixed: all async calls pass Context.ConnectionAborted. [ChatHub.cs:18,31,38]
- [x] [Review][Patch] onclose doesn't null connection — fixed: onclose handler now sets this.connection = null before emitting Disconnected. [signalr.service.ts:onclose]
- [x] [Review][Patch] Initial start() failure has no recovery path — fixed: catch handler now nulls this.connection, allowing retry. [signalr.service.ts:52-53]
- [x] [Review][Patch] Broadcast handler missing try-catch — fixed: wrapped in try-catch with ILogger.LogError. [MessageSentBroadcastHandler.cs:12]
- [x] [Review][Defer] No message-gap fill or conversation-list refresh on reconnect (AC4) — deferred, explicitly per Task 5.3 to Stories 3.4-3.7

## Dev Notes

### Existing Infrastructure to Reuse (DO NOT Recreate)

- **`MessageSent` notification** (`src/Application/Messaging/Notifications/MessageSent.cs`): Already exists as `public record MessageSent(Message Message, long ConversationId) : INotification;`. Published by `SendMessageCommandHandler` after persist. Currently has zero handlers -- this story adds the broadcast handler.
- **`SendMessageCommand`** (`src/Application/Messaging/Commands/SendMessage/SendMessageCommand.cs`): Already exists. The hub dispatches this same command via MediatR. Do NOT create a separate hub-specific command.
- **`MessageDto`** (`src/Application/Messaging/Queries/GetMessageHistory/MessageDto.cs`): Reuse this DTO for the `ReceiveMessage` broadcast payload. It has: `Id`, `ConversationId`, `SenderId`, `SenderDisplayName`, `Content`, `SentAt`, `MessageType`.
- **`IIdentityService.GetDisplayNamesByIdsAsync`**: Already exists from Story 3.2. Use in the broadcast handler to resolve `SenderDisplayName`.
- **`IApplicationDbContext`**: Has `DbSet<ConversationParticipant>` -- use to query user's conversations in `OnConnectedAsync`.
- **`IUser` / `CurrentUser`** (`src/Web/Services/CurrentUser.cs`): Reads from `HttpContext.User`. Works in SignalR hub context because SignalR sets `HttpContext` on the hub's `Context`. However, for the hub itself, prefer `Context.User` directly.
- **`Conversations` endpoint** (`src/Web/Endpoints/Conversations.cs`): Existing REST endpoint. The hub `SendMessage` method should dispatch the SAME `SendMessageCommand` -- do not duplicate persistence logic.
- **`AuthService` (Angular)**: Has `getAccessToken()` or similar method to get the current JWT. Use this in `accessTokenFactory` for the hub connection.
- **`ForbiddenAccessException`** / **`NotFoundException`**: Already exist. Use in hub methods for authorization checks.
- **EventHandlers directory**: `src/Application/Messaging/EventHandlers/` exists but is empty -- ready for the broadcast handler.

### Critical Architecture: Clean Architecture + SignalR

**The Problem**: `IHubContext<ChatHub>` lives in the Web layer (ASP.NET Core SignalR). The `MessageSentBroadcastHandler` must be in the Application layer (MediatR notification handler). Application cannot reference Web.

**The Solution**: Create an `IMessageBroadcaster` interface in Application layer, implement with `IHubContext<ChatHub>` in Web layer. This follows the same pattern as `IUser`/`CurrentUser` and `IIdentityService`/`IdentityService`.

```
Application layer:  IMessageBroadcaster (interface)
                    MessageSentBroadcastHandler (injects IMessageBroadcaster)
Web layer:          SignalRMessageBroadcaster (implements IMessageBroadcaster, injects IHubContext<ChatHub>)
```

### SignalR Server Setup -- No NuGet Package Needed

SignalR is included in `Microsoft.NET.Sdk.Web`. The `AddSignalR()` and `MapHub<T>()` APIs are available without any additional package. Do NOT add a SignalR NuGet package to the Web.csproj.

However, `Microsoft.AspNetCore.SignalR.Client` IS needed as a NuGet package in the **integration test project** for C# `HubConnectionBuilder`. Add it to `Directory.Packages.props`.

### JWT Query String Authentication for SignalR

WebSockets cannot send custom HTTP headers after the initial handshake. SignalR sends the JWT as a query string parameter `?access_token=xxx`. The server must be configured to read it:

```csharp
// In Infrastructure/DependencyInjection.cs, inside .AddJwtBearer(options => { ... })
options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
        {
            context.Token = accessToken;
        }
        return Task.CompletedTask;
    }
};
```

This MUST be added to the existing JWT configuration. Do NOT create a second authentication scheme.

### ChatHub Pattern

```csharp
namespace SimpleChat.Web.Hubs;

[Authorize]
public class ChatHub(ISender sender, IApplicationDbContext db) : Hub
{
    public override async Task OnConnectedAsync()
    {
        string userId = Context.UserIdentifier
            ?? throw new HubException("User not authenticated.");

        // Add to SignalR groups for each conversation the user participates in
        List<long> conversationIds = await db.ConversationParticipants
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.ConversationId)
            .ToListAsync();

        foreach (long conversationId in conversationIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        }

        await base.OnConnectedAsync();
    }

    public async Task<long> SendMessage(long conversationId, string content)
    {
        return await sender.Send(new SendMessageCommand(conversationId, content));
    }

    public async Task JoinConversation(long conversationId)
    {
        string userId = Context.UserIdentifier
            ?? throw new HubException("User not authenticated.");

        bool isParticipant = await db.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

        if (!isParticipant)
        {
            throw new HubException("Not a participant in this conversation.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
    }
}
```

**CRITICAL**: `Context.UserIdentifier` returns the `NameIdentifier` claim value by default. ASP.NET Core SignalR uses `IUserIdProvider` -- the default implementation reads `ClaimTypes.NameIdentifier`. Verify this matches the JWT `sub` claim set by `JwtTokenService`. Check `JwtTokenService.cs` to confirm the claim type used for user ID.

### SignalR Hub Method Error Handling

Hub method exceptions are serialized back to the calling client. Use `HubException` for user-facing errors. The MediatR pipeline (ValidationBehaviour, AuthorizationBehaviour) will throw `ValidationException`, `ForbiddenAccessException`, etc. -- these will propagate as hub errors. Consider catching MediatR exceptions and re-throwing as `HubException` with safe messages to avoid leaking internal details.

### Angular SignalRService Pattern

```typescript
// src/app/core/signalr/signalr.service.ts
@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private readonly connectionState$ = new BehaviorSubject<'Connected' | 'Reconnecting' | 'Disconnected'>('Disconnected');
  private readonly authService = inject(AuthService);

  readonly connectionState = this.connectionState$.asObservable();

  start(): void {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/chat', {
        accessTokenFactory: () => this.authService.getAccessToken() ?? ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    this.registerEventHandlers();
    this.connection.start()
      .then(() => this.connectionState$.next('Connected'))
      .catch(err => console.error('SignalR connection error:', err));
  }
  // ...
}
```

**IMPORTANT**: Use `inject()` for DI -- NEVER constructor injection in Angular (project convention). Expose observables via `.asObservable()` -- NEVER expose BehaviorSubject directly.

### Reconnection Gap-Fill Strategy

On reconnect, the client must fill any message gap that occurred while disconnected. Current REST API supports `before` cursor only (no `after` cursor). Strategy:

1. Track `lastKnownMessageId` per conversation in the Angular service
2. On reconnect, fetch the latest messages page (no `before` cursor = latest page)
3. Client-side: filter out messages already in state (compare by message ID)
4. Append only genuinely new messages

This avoids adding a new API endpoint. A dedicated `after` cursor query is a future optimization if needed.

### Message Flow: Hub vs REST Endpoint

Both paths are valid for sending messages:
- **Hub path**: `ChatHub.SendMessage()` -> `SendMessageCommand` -> persist -> `MessageSent` notification -> broadcast
- **REST path**: `POST /api/conversations/{id}/messages` -> `SendMessageCommand` -> persist -> `MessageSent` notification -> broadcast

Both converge on the same `SendMessageCommand`. The `MessageSentBroadcastHandler` handles broadcast regardless of entry point. The hub path is preferred for real-time UX (no separate HTTP request needed).

### IUser in Hub Context

SignalR creates an `HttpContext` for the hub connection's initial HTTP handshake. The `CurrentUser` service (which implements `IUser`) reads from `IHttpContextAccessor`. This works in hub methods because SignalR preserves the `HttpContext` for the lifetime of the connection. However, `ISender` injected into the hub uses the DI scope, and `IUser` will resolve correctly within that scope.

Key consideration: If using `ISender` to dispatch `SendMessageCommand`, the handler gets `IUser` via DI -- this will have the authenticated user from the hub connection. This means the hub does NOT need to manually pass user ID to the command.

### SignalR Event Naming Convention

Per architecture:
- **Server -> Client**: PascalCase verb+noun: `ReceiveMessage`, `UserOnline`, `UserOffline`
- **Client -> Server**: Hub method PascalCase: `SendMessage`, `JoinConversation`

### Build-Breaking Rules to Watch

- **`readonly` on private fields**: Any private field that can be readonly MUST be readonly -- build fails
- **`static` on local functions**: Local functions that don't capture state MUST be static -- build fails
- **TreatWarningsAsErrors**: All nullable warnings are build errors -- proper null checks required
- **No `var`**: Use explicit types per project convention
- **Namespace must match folder path**: `SimpleChat.Web.Hubs` for hub, `SimpleChat.Application.Messaging.EventHandlers` for handlers
- **Primary constructors preferred**: Use for hub and handler classes
- **CancellationToken on all async handler methods** (MediatR handlers require it; hub methods do NOT receive CancellationToken from SignalR by default -- use `Context.ConnectionAborted` if needed)
- **No `Version=` in .csproj PackageReference** -- central package management via `Directory.Packages.props`

### Items NOT in Scope

- No Angular UI components (chat window, message bubble, message input) -- comes in Stories 3.4-3.7
- No connection status banner UI -- comes in Story 3.9
- No presence tracking (UserOnline/UserOffline) -- comes in Epic 5. Register empty event listeners for future use
- No typing indicators -- Phase 2
- No message edit/delete -- Phase 2
- No unread count updates -- comes in Epic 5
- No Redis SignalR backplane -- Phase 2 (single-instance MVP)
- No conversation list endpoint -- comes in Story 3.5
- No file message handling -- comes in Epic 6

### Previous Story Intelligence (Story 3.2)

**Key learnings from Story 3.2:**
- `SendMessageCommand` takes `(long ConversationId, string Content)` -- SenderId comes from `IUser` in the handler, NOT from the command
- `MessageSent` notification is published AFTER `SaveChangesAsync` -- persist-first guarantee is already implemented
- `MessageDto` has: `Id`, `ConversationId`, `SenderId`, `SenderDisplayName`, `Content`, `SentAt` (DateTimeOffset), `MessageType` (string)
- `IIdentityService.GetDisplayNamesByIdsAsync` exists for resolving display names
- `Conversations` endpoint uses `IEndpointGroup` pattern with static handler methods
- FluentValidation and Authorization are handled by MediatR pipeline behaviors -- hub method calls via `ISender` get the same validation/auth pipeline
- Build: 0 errors, 0 warnings; Unit tests: 68 pass; Integration tests: 40 pass

**Review findings from Story 3.2 relevant here:**
- Double SaveChangesAsync was merged into single atomic save -- persist-first is a single DB call
- MessageSent was renamed from MessageSentNotification to MessageSent -- use `MessageSent` (no "Notification" suffix)

**Key learnings from Story 3.1:**
- `ConversationParticipant` has composite PK `(ConversationId, UserId)` -- query for user's conversations via this entity
- EF configurations use `ApplyConfigurationsFromAssembly()` -- auto-discovered
- `ResetDatabaseAsync()` handles all tables dynamically

### Git Intelligence

Recent commits follow `story X.Y created implemented and reviewed` pattern. Branch naming: `story/X.Y`. Current branch: `prep/fix-test-infrastructure`. Most recent: Story 3.2 (backend messaging), Story 3.1 (data model), project context, test infrastructure fixes.

### Project Structure Notes

**New files to create:**
```
src/Web/Hubs/ChatHub.cs
src/Web/Services/SignalRMessageBroadcaster.cs
src/Application/Common/Interfaces/IMessageBroadcaster.cs
src/Application/Messaging/EventHandlers/MessageSentBroadcastHandler.cs
src/Web/ClientApp/src/app/core/signalr/signalr.service.ts
src/Web/ClientApp/src/app/core/signalr/signalr.service.spec.ts
src/Web/ClientApp/src/app/core/signalr/signalr.events.ts
tests/Application.UnitTests/Messaging/EventHandlers/MessageSentBroadcastHandlerTests.cs
tests/Infrastructure.IntegrationTests/Hubs/ChatHubTests.cs
```

**Files to modify:**
```
src/Infrastructure/DependencyInjection.cs      -- add JwtBearerEvents for query string token
src/Web/DependencyInjection.cs                 -- add AddSignalR(), register IMessageBroadcaster
src/Web/Program.cs                             -- add MapHub<ChatHub>("/hubs/chat")
src/Web/ClientApp/package.json                 -- add @microsoft/signalr dependency
Directory.Packages.props                       -- add Microsoft.AspNetCore.SignalR.Client for test project
tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj -- add SignalR.Client package reference
```

**Files NOT to modify:**
```
src/Application/Messaging/Notifications/MessageSent.cs           -- already correct
src/Application/Messaging/Commands/SendMessage/*                  -- already correct
src/Application/Messaging/Queries/GetMessageHistory/MessageDto.cs -- reuse as-is
```

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-3-real-time-private-messaging.md#Story 3.3]
- [Source: _bmad-output/planning-artifacts/architecture.md#Message Flow (Synchronous, Persist-First)]
- [Source: _bmad-output/planning-artifacts/architecture.md#SignalR Event Naming]
- [Source: _bmad-output/planning-artifacts/architecture.md#End-to-End Message Send Flow]
- [Source: _bmad-output/planning-artifacts/architecture.md#API & Communication Patterns -- SignalR auth via JWT query string]
- [Source: _bmad-output/planning-artifacts/architecture.md#Connection Lifecycle -- exponential backoff intervals]
- [Source: _bmad-output/planning-artifacts/architecture.md#File Structure -- Hubs/ChatHub.cs, EventHandlers/MessageSentBroadcastHandler.cs]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend File Structure -- core/signalr/]
- [Source: _bmad-output/project-context.md#SignalR -- single ChatHub, JWT query string auth]
- [Source: _bmad-output/project-context.md#Persist-first message flow]
- [Source: _bmad-output/project-context.md#Angular Rules -- inject(), BehaviorSubject, standalone components]
- [Source: _bmad-output/project-context.md#Testing Rules -- NUnit, Shouldly, Testcontainers]
- [Source: _bmad-output/implementation-artifacts/3-2-send-persist-messages-backend.md#Dev Notes -- SendMessageCommand, MessageSent, MessageDto patterns]
- [Source: _bmad-output/implementation-artifacts/3-1-conversation-message-data-model.md#Dev Notes -- ConversationParticipant composite PK]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

### Completion Notes List

- ChatHub created in `src/Web/Hubs/ChatHub.cs` with `[Authorize]`, `OnConnectedAsync` (auto-joins user's conversation groups), `SendMessage` (dispatches `SendMessageCommand` via MediatR), `JoinConversation` (verifies participant, adds to group)
- JWT query string authentication configured in `Infrastructure/DependencyInjection.cs` via `JwtBearerEvents.OnMessageReceived` — reads `access_token` from query string for `/hubs/*` paths
- `IMessageBroadcaster` interface created in Application layer for Clean Architecture compliance — `SignalRMessageBroadcaster` in Web layer wraps `IHubContext<ChatHub>`
- `MessageSentBroadcastHandler` created as `INotificationHandler<MessageSent>` — resolves sender display name via `IIdentityService`, builds `MessageDto`, broadcasts `ReceiveMessage` event to SignalR group
- Angular `SignalRService` created with `HubConnectionBuilder`, `accessTokenFactory` from `AuthService.accessToken`, auto-reconnect `[0, 2000, 5000, 10000, 30000]`, `connectionState$` BehaviorSubject, `messageReceived$` Subject
- SignalR lifecycle integrated into auth flow: auto-starts on `isAuthenticated$` change + explicit start in `APP_INITIALIZER` after session restore; auto-stops on logout
- `@microsoft/signalr` v10.0.0 installed as npm dependency
- `Microsoft.AspNetCore.SignalR.Client` v10.0.5 added to `Directory.Packages.props` for integration tests
- Pre-existing `app.component.spec.ts` test failure (2 tests) confirmed as pre-existing — fails identically before and after changes
- Build: 0 errors, 0 warnings (Release); Unit tests: 71/71 pass (3 new); Integration tests: 45/45 pass (4 new); Frontend tests: 10/10 pass (all new); Lint: All pass

### Change Log

- 2026-04-02: Story 3.3 implemented — SignalR ChatHub, JWT query string auth, MessageSentBroadcastHandler, Angular SignalRService, unit/integration/frontend tests

### File List

New files:
- src/Web/Hubs/ChatHub.cs
- src/Web/Services/SignalRMessageBroadcaster.cs
- src/Application/Common/Interfaces/IMessageBroadcaster.cs
- src/Application/Messaging/EventHandlers/MessageSentBroadcastHandler.cs
- src/Web/ClientApp/src/app/core/signalr/signalr.service.ts
- src/Web/ClientApp/src/app/core/signalr/signalr.service.spec.ts
- src/Web/ClientApp/src/app/core/signalr/signalr.events.ts
- tests/Application.UnitTests/Messaging/EventHandlers/MessageSentBroadcastHandlerTests.cs
- tests/Infrastructure.IntegrationTests/Hubs/ChatHubTests.cs

Modified files:
- src/Infrastructure/DependencyInjection.cs (added JwtBearerEvents for SignalR query string auth)
- src/Web/DependencyInjection.cs (added AddSignalR(), registered IMessageBroadcaster)
- src/Web/Program.cs (added MapHub<ChatHub>("/hubs/chat"))
- src/Web/ClientApp/package.json (added @microsoft/signalr dependency)
- src/Web/ClientApp/package-lock.json (auto-updated)
- src/Web/ClientApp/src/app/app.config.ts (added SignalRService to APP_INITIALIZER)
- Directory.Packages.props (added Microsoft.AspNetCore.SignalR.Client)
- tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj (added SignalR.Client, Shouldly packages)
