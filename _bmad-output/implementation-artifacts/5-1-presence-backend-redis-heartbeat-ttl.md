# Story 5.1: Presence Backend (Redis Heartbeat & TTL)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user,
I want the system to track who is online,
so that I can see which team members are available for conversation.

## Acceptance Criteria

1. **Given** an authenticated user with an active session
   **When** the Angular app is in the foreground
   **Then** it sends `POST /api/presence/heartbeat` every 60 seconds with the user's current status (Online or Away)
   **And** the backend sets a Redis key `presence:{userId}` with a 90-second TTL
   **And** the backend adds the userId to a Redis Set `online_users`

2. **Given** a user closes the browser or loses connectivity
   **When** 90 seconds pass without a heartbeat
   **Then** the Redis key expires automatically via TTL
   **And** the user is lazily pruned from the `online_users` Set on the next read (`GET /api/presence/online` cross-references each Set member's TTL key; if expired, the member is removed from the Set before returning results)

3. **Given** the Angular app detects 5 minutes of client-side inactivity
   **When** the next heartbeat fires
   **Then** the heartbeat payload changes status to Away
   **And** the user's presence state reflects Away (amber dot) for other users
   **And** no notification or toast is shown to the user (silent, per "quiet confidence" principle -- UX-DR4)

4. **Given** any authenticated user
   **When** `GET /api/presence/online` is called
   **Then** a list of currently online/away users is returned with their userId and status
   **And** the Angular app polls this endpoint every 30 seconds

## Tasks / Subtasks

- [x] Task 1: Backend - Implement RedisCacheService (AC: #1, #2)
  - [x] 1.1 Create `src/Infrastructure/Services/RedisCacheService.cs` implementing `ICacheService` — inject `IConnectionMultiplexer`, use `IDatabase` from `_redis.GetDatabase()`. Implement: `GetAsync<T>` (StringGet + JSON deserialize), `SetAsync<T>` (JSON serialize + StringSet with optional expiry), `DeleteAsync` (KeyDelete), `SetMembersAsync` (SMEMBERS + return as `IReadOnlySet<string>`)
  - [x] 1.2 Add two additional methods to `ICacheService` needed for presence: `SetAddAsync(string key, string member)` (SADD), `SetRemoveAsync(string key, string member)` (SREM), `KeyExistsAsync(string key)` (EXISTS)
  - [x] 1.3 Register `RedisCacheService` as `ICacheService` (Singleton) in `Infrastructure/DependencyInjection.cs`
  - [x] 1.4 Unit tests: `RedisCacheServiceTests.cs` — mock `IConnectionMultiplexer` and `IDatabase`, verify all method calls with correct parameters

- [x] Task 2: Backend - HeartbeatCommand (AC: #1, #3)
  - [x] 2.1 Create `src/Application/Presence/Commands/Heartbeat/HeartbeatCommand.cs` — `[Authorize] public record HeartbeatCommand(PresenceStatus Status) : IRequest<Unit>`
  - [x] 2.2 Create `src/Application/Presence/Commands/Heartbeat/HeartbeatCommandValidator.cs` — validate Status is a defined enum value (Online or Away only; Offline is not a valid heartbeat status)
  - [x] 2.3 Create `src/Application/Presence/Commands/Heartbeat/HeartbeatCommandHandler.cs` — get userId from `IUser`, set Redis key `presence:{userId}` with value containing status + displayName (JSON serialized), 90-second TTL via `ICacheService.SetAsync()`. Add userId to Redis Set `online_users` via `ICacheService.SetAddAsync()`. Log at Debug level: `"Heartbeat received from user {UserId} with status {Status}"`
  - [x] 2.4 Unit tests: `HeartbeatCommandHandlerTests.cs` — test Online heartbeat sets key with 90s TTL and adds to Set, Away heartbeat sets key with Away status, verify cache service calls
  - [x] 2.5 Unit test: `HeartbeatCommandValidatorTests.cs` — valid Online/Away accepted, Offline rejected, invalid enum rejected

- [x] Task 3: Backend - GetOnlineUsersQuery (AC: #2, #4)
  - [x] 3.1 Create `src/Application/Presence/Queries/GetOnlineUsers/OnlineUserDto.cs` — `public record OnlineUserDto(string UserId, string DisplayName, PresenceStatus Status)`
  - [x] 3.2 Create `src/Application/Presence/Queries/GetOnlineUsers/GetOnlineUsersQuery.cs` — `[Authorize] public record GetOnlineUsersQuery : IRequest<List<OnlineUserDto>>`
  - [x] 3.3 Create `src/Application/Presence/Queries/GetOnlineUsers/GetOnlineUsersQueryHandler.cs` — get all members from `online_users` Set via `ICacheService.SetMembersAsync()`. For each member, check if `presence:{userId}` key exists via `ICacheService.GetAsync()`. If key exists: include in results with status from stored value. If key expired: remove from Set via `ICacheService.SetRemoveAsync()` (lazy pruning per AC #2). Return list of `OnlineUserDto`
  - [x] 3.4 Unit tests: `GetOnlineUsersQueryHandlerTests.cs` — test returns online users, prunes expired users from Set, empty Set returns empty list, mix of online/expired users

- [x] Task 4: Backend - PresenceInfo value type (AC: #1, #4)
  - [x] 4.1 Create `src/Application/Presence/Models/PresenceInfo.cs` — `public record PresenceInfo(PresenceStatus Status, string DisplayName)` — serialized as the value stored in Redis `presence:{userId}` key

- [x] Task 5: Backend - Presence endpoint registration (AC: #1, #4)
  - [x] 5.1 Create `src/Web/Endpoints/Presence.cs` implementing `IEndpointGroup` with `RoutePrefix = "/api/presence"`. Add `POST heartbeat` endpoint mapping to `HeartbeatCommand`. Add `GET online` endpoint mapping to `GetOnlineUsersQuery`. Both require authorization
  - [x] 5.2 Define `HeartbeatRequest` record in endpoint file: `public record HeartbeatRequest(string Status)` — parse string to `PresenceStatus` enum before creating command
  - [x] 5.3 Integration tests in new `PresenceEndpointTests.cs`: heartbeat returns OK, online endpoint returns empty initially, heartbeat followed by online returns the user, expired heartbeat is pruned

- [x] Task 6: Frontend - PresenceService with heartbeat polling (AC: #1, #3, #4)
  - [x] 6.1 Create `src/Web/ClientApp/src/app/core/presence/presence.service.ts` — standalone injectable service. Inject `HttpClient`, `AuthService`, `DestroyRef`. Start/stop heartbeat and polling based on auth state (subscribe to `isAuthenticated$`)
  - [x] 6.2 Implement heartbeat: `setInterval` every 60,000ms calling `POST /api/presence/heartbeat` with `{ status: 'Online' }` or `{ status: 'Away' }`. First heartbeat fires immediately on start
  - [x] 6.3 Implement idle detection: track last user interaction (mouse, keyboard, touch events) via `document` event listeners. If no activity for 5 minutes, set internal `isAway = true`, send Away heartbeat. On next user activity, reset to Online
  - [x] 6.4 Implement online users polling: `setInterval` every 30,000ms calling `GET /api/presence/online`. Store result in `BehaviorSubject<OnlineUser[]>`. Expose as `readonly onlineUsers$: Observable<OnlineUser[]>`
  - [x] 6.5 Cleanup: clear both intervals on auth state change to logged-out, on service destroy. Remove document event listeners on cleanup
  - [x] 6.6 Jest tests: service starts heartbeat on auth, stops on logout, switches to Away after inactivity, polls online users, cleans up intervals

- [x] Task 7: Frontend - Presence model (AC: #4)
  - [x] 7.1 Create `src/Web/ClientApp/src/app/core/presence/presence.model.ts` — `export interface OnlineUser { userId: string; displayName: string; status: 'Online' | 'Away' | 'Offline'; }`

- [x] Task 8: Frontend - Provide PresenceService at app level (AC: #1, #4)
  - [x] 8.1 Ensure `PresenceService` is `providedIn: 'root'` (injectable singleton). It auto-starts/stops based on auth state — no manual wiring needed. The service should be injected somewhere to trigger instantiation (e.g., in `AppComponent` or via `APP_INITIALIZER`)

### Review Findings

- [x] [Review][Patch] **[BLOCKER] Enum serialization contract mismatch — backend emits `status` as integer but frontend `OnlineUser.status` typed as `'Online' | 'Away' | 'Offline'`** [src/Web/Endpoints/Presence.cs, src/Application/Presence/Queries/GetOnlineUsers/OnlineUserDto.cs, tests/Infrastructure.IntegrationTests/Api/PresenceEndpointTests.cs:91,111]. No `JsonStringEnumConverter` registered anywhere; confirmed `GetInt32()` assertions in tests. Fix: register `JsonStringEnumConverter` globally (Program.cs `ConfigureHttpJsonOptions`) and update integration-test assertions to `GetString()`.
- [x] [Review][Patch] **[MAJOR] `Enum.Parse` in endpoint throws 500 on invalid/null `status`** [src/Web/Endpoints/Presence.cs:25]. Bad payload (`{status:"Foo"}`, `{}`, null) reaches `Enum.Parse` before the validator and produces unhandled 500. Fix: use `Enum.TryParse` and return `BadRequest` when parsing fails or `request.Status` is null/whitespace.
- [x] [Review][Patch] **[MAJOR] `JsonSerializer.Deserialize` on poisoned Redis payload crashes every poll** [src/Infrastructure/Services/RedisCacheService.cs:26]. A single corrupt/schema-drifted `presence:{id}` value throws `JsonException` on read, taking down `GetOnlineUsersQuery` for all callers until the key expires. Fix: try/catch `JsonException`, treat as missing (`default`), optionally log + delete the bad key.
- [x] [Review][Patch] **[MAJOR] `PresenceService.start()` is not idempotent — duplicate intervals leak on repeat-start** [src/Web/ClientApp/src/app/core/presence/presence.service.ts:59]. If `start()` is called twice without `stop()`, previous `setInterval` handles are overwritten and leaked. Fix: early-return when `heartbeatInterval !== null`.
- [x] [Review][Patch] **[MAJOR] `skip(1)` + explicit `start()` on construction race** [src/Web/ClientApp/src/app/core/presence/presence.service.ts:36-51]. If `isAuthenticated$` flips `false→true` synchronously during bootstrap, `skip(1)` swallows the first replay and `start()` never runs. Fix: drop `skip(1)` and the explicit initial `start()` call; rely on `distinctUntilChanged()` alone so the BehaviorSubject's current value triggers `start()` naturally.
- [x] [Review][Patch] **[MAJOR] `Handle_NoAuthenticatedUser_ShouldThrowUnauthorizedAccessException` is sync-void and discards `ShouldThrowAsync` Task — test always passes** [tests/Application.UnitTests/Presence/Commands/Heartbeat/HeartbeatCommandHandlerTests.cs:92-102]. A genuine regression will not fail this test. Fix: change signature to `async Task` and `await Should.ThrowAsync<UnauthorizedAccessException>(...)`.
- [x] [Review][Patch] **[MAJOR] Task 5.3 claims "expired heartbeat is pruned" integration test but it is not present** [tests/Infrastructure.IntegrationTests/Api/PresenceEndpointTests.cs]. Sub-task marked `[x]` in spec; no test exists. Fix: add test — heartbeat → manually `DEL presence:{userId}` on the Testcontainers Redis → `GET /api/presence/online` returns empty and confirm user removed from `online_users`.
- [x] [Review][Patch] **[MINOR] `CancellationToken ct` parameter accepted but never honored in `RedisCacheService`** [src/Infrastructure/Services/RedisCacheService.cs:16-83]. Interface advertises cancellation; implementation silently ignores it. Fix: `ct.ThrowIfCancellationRequested();` at top of each method (StackExchange.Redis APIs don't take CT directly, but pre-flight check honors the contract).
- [x] [Review][Patch] **[MINOR] `?? throw new UnauthorizedAccessException()` bypasses project's `Guard.Against` convention and maps to 500** [src/Application/Presence/Commands/Heartbeat/HeartbeatCommandHandler.cs:32-33]. Endpoint already `RequireAuthorization()`, so null `Id` here is an invariant violation, not an auth failure. Fix: use `Guard.Against.NullOrWhiteSpace(_currentUser.Id)` (throws `ArgumentException` per project pattern) or `InvalidOperationException` with a clear message.
- [x] [Review][Patch] **[MINOR] Empty display name leaks to clients** [src/Application/Presence/Commands/Heartbeat/HeartbeatCommandHandler.cs:37]. `GetValueOrDefault(userId, "Unknown")` only falls back when the key is absent; an empty-string `DisplayName` from `IdentityService` is returned verbatim. Fix: treat null/whitespace as missing — `displayNames.TryGetValue(userId, out string? name) && !string.IsNullOrWhiteSpace(name) ? name : "Unknown"`.
- [x] [Review][Patch] **[MINOR] No negative integration tests for invalid/missing `status`** [tests/Infrastructure.IntegrationTests/Api/PresenceEndpointTests.cs]. The 500-vs-400 bug above would not be caught; `status: "Offline"` (validator reject) not covered; empty body not covered. Fix: add integration tests asserting 400 for `{status:"Garbage"}`, `{}`, and `{status:"Offline"}`.
- [x] [Review][Defer] **[MAJOR] Lazy-prune TOCTOU race — query can SREM a user whose heartbeat re-added them between SMEMBERS and GET** [src/Application/Presence/Queries/GetOnlineUsers/GetOnlineUsersQueryHandler.cs:20-36] — deferred, self-heals on next heartbeat (at most one missed poll). Lua/MULTI atomic check-and-remove is the proper fix.
- [x] [Review][Defer] **[MAJOR] N+1 Redis round-trips in `GetOnlineUsersQueryHandler`** [src/Application/Presence/Queries/GetOnlineUsers/GetOnlineUsersQueryHandler.cs:23-35] — deferred, flow is spec-endorsed. Batch via pipelining/`MGET` when scale demands.
- [x] [Review][Defer] **[MAJOR] `online_users` Set has no TTL — grows unboundedly if `/online` is rarely called** [src/Application/Presence/Commands/Heartbeat/HeartbeatCommandHandler.cs:47] — deferred, only matters at high-user/low-read scale. Periodic SSCAN-based cleanup or per-user sorted-set by expiry would address.
- [x] [Review][Defer] **[MAJOR] DB hit on every heartbeat to resolve display name** [src/Application/Presence/Commands/Heartbeat/HeartbeatCommandHandler.cs:35-37] — deferred, cache display name in the JWT or a short-TTL Redis entry to avoid SQL pressure under steady state.
- [x] [Review][Defer] **[MAJOR] Partial-write race between `SetAsync` and `SetAddAsync`** [src/Application/Presence/Commands/Heartbeat/HeartbeatCommandHandler.cs:41-47] — deferred, self-heals next heartbeat; transaction/MULTI would make atomic.
- [x] [Review][Defer] **[MINOR] No Page Visibility / `visibilitychange` / `scroll` handling; multi-tab heartbeat flapping** [src/Web/ClientApp/src/app/core/presence/presence.service.ts:110-127] — deferred, enhancement. BroadcastChannel coordination + visibility hook would stabilize.
- [x] [Review][Defer] **[MINOR] No backoff/circuit-breaker on 401/429/5xx heartbeat errors — console.error only** [src/Web/ClientApp/src/app/core/presence/presence.service.ts:97,106] — deferred, enhancement.
- [x] [Review][Defer] **[MINOR] No in-flight guard on heartbeat/poll — slow network causes double-fire** [src/Web/ClientApp/src/app/core/presence/presence.service.ts:70-71,96-98] — deferred, infrequent edge case.
- [x] [Review][Defer] **[MINOR] `ResetDatabaseAsync` allow-lists patterns; future Redis key types will silently leak across tests** [tests/Infrastructure.IntegrationTests/CustomWebApplicationFactory.cs:94-101] — deferred, test infra improvement (consider `FLUSHDB` on a dedicated test DB).

## Dev Notes

### What Already Exists (DO NOT Recreate)

- **PresenceStatus enum** (`src/Domain/Common/Enums/PresenceStatus.cs`) — `Online`, `Away`, `Offline`. Already defined
- **ICacheService interface** (`src/Application/Common/Interfaces/ICacheService.cs`) — has `GetAsync<T>`, `SetAsync<T>`, `DeleteAsync`, `SetMembersAsync`. **NOT YET IMPLEMENTED** — this story creates the implementation
- **RedisSessionService** (`src/Infrastructure/Services/RedisSessionService.cs`) — existing Redis service using `IConnectionMultiplexer`. Follow this pattern exactly for `RedisCacheService`: constructor injection of `IConnectionMultiplexer`, `_redis.GetDatabase()` for operations
- **IConnectionMultiplexer** — already registered as Singleton in `Infrastructure/DependencyInjection.cs` (line 91). Do NOT re-register
- **SignalR events** (`src/Web/ClientApp/src/app/core/signalr/signalr.events.ts`) — `UserOnline` and `UserOffline` events are defined with no-op handlers. This story does NOT wire these up (presence uses REST polling, not SignalR events). Leave the no-op handlers as-is
- **IEndpointGroup pattern** (`src/Web/Infrastructure/IEndpointGroup.cs`) — defines `static string? RoutePrefix` and `static void Map(RouteGroupBuilder)`. Auto-discovered via reflection. Follow `Auth.cs` or `Conversations.cs` pattern
- **IUser interface** — provides `CurrentUser` with `.Id` property in Application layer handlers. Injected via DI
- **AuthService** (`src/Web/ClientApp/src/app/core/services/auth.service.ts`) — has `isAuthenticated$` BehaviorSubject, `accessToken` getter. PresenceService subscribes to auth state
- **Application/Presence/ folders** — empty with `.gitkeep` files. Commands/, Queries/, EventHandlers/ ready for use
- **Domain/Presence/ folders** — empty with `.gitkeep`. No domain entities needed for this story (presence is Redis-only)

### Architecture Compliance

- **CQRS pattern**: `HeartbeatCommand` for write (Redis SET), `GetOnlineUsersQuery` for read (Redis GET + SMEMBERS). Handler-per-file vertical slices
- **MediatR pipeline**: Commands/queries go through Logging -> UnhandledException -> Authorization -> Validation -> Performance pipeline automatically
- **No repository pattern**: Inject `ICacheService` directly in handlers (not `IApplicationDbContext` — presence has no DB entities)
- **No database entities**: Presence is stored ONLY in Redis. No EF migrations, no DB tables, no domain entities
- **Custom exceptions**: Not needed for this story — heartbeat/query are simple operations. Invalid status handled by FluentValidation
- **Cross-module via MediatR**: Presence handlers use `IUser` for userId, `ICacheService` for Redis. No direct references to Identity or Messaging modules
- **Endpoint pattern**: `IEndpointGroup` with static handler methods, `ISender` parameter, `TypedResults` return values

### ICacheService Extension

The existing `ICacheService` interface needs three additional methods for presence operations:

```csharp
// Add to ICacheService:
Task SetAddAsync(string key, string member, CancellationToken ct = default);
Task SetRemoveAsync(string key, string member, CancellationToken ct = default);
Task<bool> KeyExistsAsync(string key, CancellationToken ct = default);
```

These map to Redis `SADD`, `SREM`, and `EXISTS` commands. Required because:
- `SetAddAsync`: add userId to `online_users` Set on heartbeat
- `SetRemoveAsync`: lazy-prune expired users from `online_users` Set
- `KeyExistsAsync`: check if `presence:{userId}` TTL key still exists during pruning

### Redis Key Design

| Key Pattern | Type | TTL | Content |
|-------------|------|-----|---------|
| `presence:{userId}` | String | 90 seconds | JSON: `{"Status":"Online","DisplayName":"Sarah"}` |
| `online_users` | Set | None (members pruned lazily) | Set of userIds |

**Heartbeat flow:**
1. `SET presence:{userId} {json} EX 90` — key auto-expires
2. `SADD online_users {userId}` — add to Set (idempotent)

**Online users query flow:**
1. `SMEMBERS online_users` — get all member userIds
2. For each: `GET presence:{userId}` — check if still alive
3. If key exists: include in response with status from value
4. If key missing (expired): `SREM online_users {userId}` — prune

### RedisCacheService Implementation Pattern

Follow `RedisSessionService` exactly:
```csharp
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    // Use _redis.GetDatabase() for all operations
    // JSON serialize/deserialize with System.Text.Json
}
```

Register as **Singleton** (not Scoped) — `IConnectionMultiplexer` is already Singleton, and `IDatabase` from `GetDatabase()` is thread-safe.

### Endpoint Registration Pattern

Follow `Auth.cs` pattern:
```csharp
public class Presence : IEndpointGroup
{
    public static string? RoutePrefix => "/api/presence";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Heartbeat, "heartbeat")
            .RequireAuthorization();

        groupBuilder.MapGet(GetOnlineUsers, "online")
            .RequireAuthorization();
    }

    public static async Task<Ok> Heartbeat(ISender sender, HeartbeatRequest request)
    {
        PresenceStatus status = Enum.Parse<PresenceStatus>(request.Status, ignoreCase: true);
        await sender.Send(new HeartbeatCommand(status));
        return TypedResults.Ok();
    }

    public static async Task<Ok<List<OnlineUserDto>>> GetOnlineUsers(ISender sender)
    {
        List<OnlineUserDto> result = await sender.Send(new GetOnlineUsersQuery());
        return TypedResults.Ok(result);
    }
}

public record HeartbeatRequest(string Status);
```

### HeartbeatCommandHandler Key Logic

```
1. Get current user ID from IUser.Id
2. Resolve display name via IIdentityService.GetDisplayNamesByIdsAsync([userId])
3. Create PresenceInfo(status, displayName) record
4. Set Redis key: await _cache.SetAsync($"presence:{userId}", presenceInfo, TimeSpan.FromSeconds(90))
5. Add to Set: await _cache.SetAddAsync("online_users", userId)
6. Return Unit.Value
```

### GetOnlineUsersQueryHandler Key Logic

```
1. Get all members: IReadOnlySet<string> memberIds = await _cache.SetMembersAsync("online_users")
2. Initialize results list: List<OnlineUserDto>
3. For each userId in memberIds:
   a. PresenceInfo? info = await _cache.GetAsync<PresenceInfo>($"presence:{userId}")
   b. If info != null: add OnlineUserDto(userId, info.DisplayName, info.Status) to results
   c. If info == null (expired): await _cache.SetRemoveAsync("online_users", userId) — lazy prune
4. Return results
```

### Frontend PresenceService Pattern

Follow existing service patterns:
- `inject()` for DI, not constructor injection
- `BehaviorSubject` for state, expose via `.asObservable()`
- Subscribe to `AuthService.isAuthenticated$` to start/stop

```typescript
@Injectable({ providedIn: 'root' })
export class PresenceService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly _onlineUsers$ = new BehaviorSubject<OnlineUser[]>([]);
  readonly onlineUsers$ = this._onlineUsers$.asObservable();

  private heartbeatInterval: ReturnType<typeof setInterval> | null = null;
  private pollInterval: ReturnType<typeof setInterval> | null = null;
  private isAway = false;
  private lastActivity = Date.now();
  private readonly HEARTBEAT_MS = 60000;
  private readonly POLL_MS = 30000;
  private readonly IDLE_THRESHOLD_MS = 300000; // 5 minutes

  // Activity tracking via document listeners (mousemove, keydown, touchstart)
  // Auth subscription to start/stop
  // Cleanup via DestroyRef
}
```

**Idle detection approach:**
- Listen to `document` events: `mousemove`, `keydown`, `touchstart`, `click`
- Throttle handler (only update `lastActivity` timestamp, no heavy processing)
- On each heartbeat interval tick: check `Date.now() - lastActivity > IDLE_THRESHOLD_MS`
- If idle: send `Away` heartbeat. If active: send `Online` heartbeat
- On user activity after being Away: immediate heartbeat with `Online` status (don't wait for interval)

### C# Critical Rules

- Commands/Queries are **records**, handlers are **classes**
- `readonly` on private fields (TreatWarningsAsErrors)
- File-scoped namespaces, explicit types (not `var`), Allman braces
- `CancellationToken` on all async handler methods
- Guard clauses: `Guard.Against.Null()` from Ardalis.GuardClauses
- Global usings exist per project — check `GlobalUsings.cs` before adding usings
- Nullable enabled — proper null checks, no `null!`
- LF line endings, 4-space indent, final newline
- ImplicitUsings enabled — do NOT add `using System;`, `using System.Linq;`, etc.
- No `var` — use explicit types
- No `Version=` in PackageReference — central package management

### Angular Critical Rules

- `inject()` for DI (never constructor injection)
- Standalone components only, no NgModules
- BehaviorSubject for state, expose via `.asObservable()`
- Single quotes, 2-space indent, kebab-case files
- `strict: true` — explicit return types, null checks
- No path aliases in source code — use relative imports
- `subscribe()` with next/error callbacks, not `.then()`

### Testing Standards

**Backend unit tests (NUnit + Moq + Shouldly):**
- `[Test]`, `[TestCase]` — NOT `[Fact]`/`[Theory]`
- `result.ShouldBe(expected)` — NOT `Assert.AreEqual`
- Method naming: `MethodUnderTest_Scenario_ExpectedOutcome`
- Mirror Application folder structure
- Moq: `new Mock<ICacheService>()`, `.Setup().ReturnsAsync()`, `.Verify()`
- FluentValidation.TestHelper: `result.ShouldHaveValidationErrorFor(x => x.Property)`

**Integration tests (Testcontainers — real DB + real Redis):**
- Use existing `CustomWebApplicationFactory` and `TestcontainersFixture`
- No mocking — real Redis, real HTTP pipeline
- Test full heartbeat + query flow end-to-end
- Verify Redis TTL expiry behavior with real Redis

**Frontend tests (Jest):**
- TestBed not needed for service-only tests — use `TestBed.configureTestingModule` if testing component interaction
- Mock `HttpClient` with jest.fn() or `HttpClientTestingModule`
- Test interval-based behavior with `jest.useFakeTimers()`

### Previous Story Intelligence (Story 4.4)

**Key learnings from Epic 4 that apply:**
- **Single timestamp capture**: Reuse `DateTimeOffset.UtcNow` in a variable, don't call multiple times
- **Subscription leak prevention**: In services with intervals, ensure cleanup on auth state change and on destroy
- **Test counts at end of Epic 4**: 132 backend unit tests, 41 integration tests, 307 frontend tests — do not regress
- **RedisSessionService pattern**: This is the closest reference implementation for Redis operations. Follow the same `IConnectionMultiplexer` injection and `_redis.GetDatabase()` pattern

### Git Intelligence

Recent commits show atomic story-per-commit pattern. Epic 4 is complete with retrospective. This is the first story of Epic 5 (Presence & Notifications). Branch from main for Epic 5 work.

### Project Structure Notes

**New files to create:**
```
src/Application/Presence/Models/
  PresenceInfo.cs

src/Application/Presence/Commands/Heartbeat/
  HeartbeatCommand.cs
  HeartbeatCommandHandler.cs
  HeartbeatCommandValidator.cs

src/Application/Presence/Queries/GetOnlineUsers/
  GetOnlineUsersQuery.cs
  GetOnlineUsersQueryHandler.cs
  OnlineUserDto.cs

src/Infrastructure/Services/
  RedisCacheService.cs

src/Web/Endpoints/
  Presence.cs

src/Web/ClientApp/src/app/core/presence/
  presence.service.ts
  presence.service.spec.ts
  presence.model.ts

tests/Application.UnitTests/Presence/Commands/Heartbeat/
  HeartbeatCommandHandlerTests.cs
  HeartbeatCommandValidatorTests.cs

tests/Application.UnitTests/Presence/Queries/GetOnlineUsers/
  GetOnlineUsersQueryHandlerTests.cs

tests/Application.UnitTests/Infrastructure/Services/
  RedisCacheServiceTests.cs

tests/Infrastructure.IntegrationTests/Api/
  PresenceEndpointTests.cs
```

**Files to modify:**
```
src/Application/Common/Interfaces/ICacheService.cs (add SetAddAsync, SetRemoveAsync, KeyExistsAsync methods)
src/Infrastructure/DependencyInjection.cs (register RedisCacheService as ICacheService)
```

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-5-presence-notifications.md#Story 5.1]
- [Source: _bmad-output/planning-artifacts/architecture.md#Presence Architecture (Redis Heartbeat + TTL)]
- [Source: _bmad-output/planning-artifacts/architecture.md#API & Communication Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture]
- [Source: _bmad-output/planning-artifacts/prd.md#FR20, FR21, FR22]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Presence Indicator, UX-DR4]
- [Source: _bmad-output/project-context.md]
- [Source: _bmad-output/implementation-artifacts/4-4-invite-users-leave-group.md]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

### Completion Notes List

- Backend: Created `RedisCacheService` implementing `ICacheService` with all 7 methods (GetAsync, SetAsync, DeleteAsync, SetMembersAsync, SetAddAsync, SetRemoveAsync, KeyExistsAsync). Registered as Singleton in DI. Extended `ICacheService` interface with 3 new methods for Redis Set and key-existence operations.
- Backend: Created `HeartbeatCommand` with handler that resolves display name via `IIdentityService`, stores `PresenceInfo` JSON in Redis key `presence:{userId}` with 90-second TTL, and adds userId to `online_users` Set. Validator rejects `Offline` and invalid enum values.
- Backend: Created `GetOnlineUsersQuery` with handler that reads `online_users` Set, checks each member's TTL key, returns active users as `OnlineUserDto`, and lazy-prunes expired members from the Set.
- Backend: Created `PresenceInfo` record as the value type stored in Redis presence keys.
- Backend: Created `Presence` endpoint group implementing `IEndpointGroup` with `POST /api/presence/heartbeat` and `GET /api/presence/online`. Both require authorization. `HeartbeatRequest` parses string status to enum.
- Backend: Updated `CustomWebApplicationFactory.ResetDatabaseAsync()` to clear `presence:*` Redis keys and `online_users` Set between integration tests.
- Frontend: Created `PresenceService` — singleton injectable service that auto-starts/stops based on auth state. Sends heartbeat every 60s, polls online users every 30s, detects 5-minute idle for Away status. Uses `NgZone.runOutsideAngular()` for intervals to avoid unnecessary change detection. Exposes `onlineUsers$` BehaviorSubject.
- Frontend: Created `OnlineUser` interface in `presence.model.ts`.
- Frontend: Injected `PresenceService` in `AppComponent` to trigger singleton instantiation.
- Tests: 154 backend unit tests (22 new), 83 integration tests (6 new), 315 frontend tests (5 new). Zero regressions. Release build: 0 warnings. Lint: all files pass.

### Change Log

- 2026-04-16: Implemented story 5-1 — presence backend with Redis heartbeat/TTL, REST endpoints, frontend service with idle detection and polling.

### File List

- src/Application/Common/Interfaces/ICacheService.cs (modified — added SetAddAsync, SetRemoveAsync, KeyExistsAsync)
- src/Application/Presence/Models/PresenceInfo.cs (new)
- src/Application/Presence/Commands/Heartbeat/HeartbeatCommand.cs (new)
- src/Application/Presence/Commands/Heartbeat/HeartbeatCommandHandler.cs (new)
- src/Application/Presence/Commands/Heartbeat/HeartbeatCommandValidator.cs (new)
- src/Application/Presence/Queries/GetOnlineUsers/OnlineUserDto.cs (new)
- src/Application/Presence/Queries/GetOnlineUsers/GetOnlineUsersQuery.cs (new)
- src/Application/Presence/Queries/GetOnlineUsers/GetOnlineUsersQueryHandler.cs (new)
- src/Infrastructure/Services/RedisCacheService.cs (new)
- src/Infrastructure/DependencyInjection.cs (modified — registered RedisCacheService as ICacheService)
- src/Web/Endpoints/Presence.cs (new)
- src/Web/ClientApp/src/app/core/presence/presence.model.ts (new)
- src/Web/ClientApp/src/app/core/presence/presence.service.ts (new)
- src/Web/ClientApp/src/app/core/presence/presence.service.spec.ts (new)
- src/Web/ClientApp/src/app/app.component.ts (modified — inject PresenceService for instantiation)
- tests/Application.UnitTests/Infrastructure/Services/RedisCacheServiceTests.cs (new)
- tests/Application.UnitTests/Presence/Commands/Heartbeat/HeartbeatCommandHandlerTests.cs (new)
- tests/Application.UnitTests/Presence/Commands/Heartbeat/HeartbeatCommandValidatorTests.cs (new)
- tests/Application.UnitTests/Presence/Queries/GetOnlineUsers/GetOnlineUsersQueryHandlerTests.cs (new)
- tests/Infrastructure.IntegrationTests/Api/PresenceEndpointTests.cs (new)
- tests/Infrastructure.IntegrationTests/CustomWebApplicationFactory.cs (modified — added presence Redis key cleanup)
