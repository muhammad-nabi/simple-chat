# Story 3.1: Conversation & Message Data Model

Status: done

## Story

As a developer,
I want the core messaging data model in place,
so that conversations and messages can be persisted and queried efficiently.

## Acceptance Criteria

1. **Given** the messaging module in the Domain layer, **When** the entities are defined, **Then** a `Conversation` entity exists with: `Id` (long), `Type` (ConversationType enum: Private, Group), `Name` (nullable -- null for Private), `CreatedAt`, `CreatedById`, `LastMessageAt`.

2. **Given** the messaging entities, **When** they are defined, **Then** a `ConversationParticipant` entity exists with: `ConversationId`, `UserId`, `JoinedAt`, `LastReadMessageId` (nullable long).

3. **Given** the messaging entities, **When** they are defined, **Then** a `Message` entity exists with: `Id` (bigint identity), `ConversationId`, `SenderId`, `Content` (string, max 4000), `SentAt`, `MessageType` (enum: Text, File, System), `FileId` (nullable), `EditedAt` (nullable), `DeletedAt` (nullable).

4. **Given** the EF Core entity configurations, **When** a migration is generated and applied, **Then** the `Messages` table has a clustered primary key on `Id` (append-only write pattern), **And** a composite index exists on `(ConversationId, Id DESC)` for cursor-based history pagination, **And** a unique constraint exists on `ConversationParticipants` (`ConversationId`, `UserId`), **And** foreign keys are configured for `SenderId` -> `Users.Id` and `ConversationId` -> `Conversations.Id`.

5. **Given** the data model, **When** message IDs are generated, **Then** they use bigint identity -- sequential, compact, natural cursor ordering.

6. **Given** the entity definitions, **When** integration tests run, **Then** entity persistence round-trips are verified (create, read, query) against a real MSSQL instance via Testcontainers.

## Tasks / Subtasks

- [x] Task 1: Create Domain Entities (AC: #1, #2, #3)
  - [x] 1.1 Create `MessageType` enum in `src/Domain/Common/Enums/MessageType.cs` with values: Text, File, System
  - [x] 1.2 Create `Conversation` entity in `src/Domain/Messaging/Conversation.cs` -- inherits `BaseEntity` (NOT BaseAuditableEntity; uses its own `CreatedById` and `LastMessageAt` fields). Properties: `Type` (ConversationType), `Name` (string?), `CreatedById` (string -- AspNetCore Identity uses string IDs), `LastMessageAt` (DateTimeOffset?). Navigation properties: `Participants` (ICollection\<ConversationParticipant\>), `Messages` (ICollection\<Message\>). NOTE: `Id` (long) and `CreatedAt` (DateTimeOffset) come from BaseEntity
  - [x] 1.3 Create `ConversationParticipant` entity in `src/Domain/Messaging/ConversationParticipant.cs` -- standalone class (no BaseEntity inheritance; this is a join table). Properties: `ConversationId` (long), `UserId` (string), `JoinedAt` (DateTimeOffset), `LastReadMessageId` (long?). Navigation property: `Conversation` (Conversation)
  - [x] 1.4 Create `Message` entity in `src/Domain/Messaging/Message.cs` -- inherits `BaseEntity`. Properties: `ConversationId` (long), `SenderId` (string), `Content` (string), `SentAt` (DateTimeOffset), `MessageType` (MessageType), `FileId` (long?), `EditedAt` (DateTimeOffset?), `DeletedAt` (DateTimeOffset?). Navigation property: `Conversation` (Conversation). NOTE: `Id` (long) from BaseEntity provides the bigint identity

- [x] Task 2: Create EF Core Configurations (AC: #4, #5)
  - [x] 2.1 Create `src/Infrastructure/Data/Configurations/ConversationConfiguration.cs` implementing `IEntityTypeConfiguration<Conversation>`:
    - `Id` configured as identity column (EF default for long)
    - `Type` required, stored as int
    - `Name` optional, max length 256
    - `CreatedById` required, max length 450 (ASP.NET Identity user ID length)
    - `LastMessageAt` optional
    - `CreatedAt` required (from BaseEntity)
    - Table name: use EF default (Conversations via DbSet name)
  - [x] 2.2 Create `src/Infrastructure/Data/Configurations/ConversationParticipantConfiguration.cs` implementing `IEntityTypeConfiguration<ConversationParticipant>`:
    - Composite primary key on (`ConversationId`, `UserId`) -- this also serves as the unique constraint
    - `JoinedAt` required
    - `LastReadMessageId` optional
    - Foreign key: `ConversationId` -> `Conversations.Id` with cascade delete
    - `UserId` max length 450
  - [x] 2.3 Create `src/Infrastructure/Data/Configurations/MessageConfiguration.cs` implementing `IEntityTypeConfiguration<Message>`:
    - `Id` as bigint identity, clustered primary key (EF default behavior for long PK)
    - `ConversationId` required, foreign key to `Conversations.Id` with cascade delete
    - `SenderId` required, max length 450
    - `Content` required, max length 4000
    - `SentAt` required
    - `MessageType` required, stored as int
    - `FileId` optional
    - `EditedAt` optional
    - `DeletedAt` optional
    - Composite index on (`ConversationId`, `Id` DESC) named `IX_Messages_ConversationId_Id`

- [x] Task 3: Register DbSets and Update Interfaces (AC: #4)
  - [x] 3.1 Add `DbSet<Conversation> Conversations` property to `IApplicationDbContext` interface in `src/Application/Common/Interfaces/IApplicationDbContext.cs`
  - [x] 3.2 Add `DbSet<ConversationParticipant> ConversationParticipants` property to `IApplicationDbContext`
  - [x] 3.3 Add `DbSet<Message> Messages` property to `IApplicationDbContext`
  - [x] 3.4 Add corresponding `DbSet` properties to `ApplicationDbContext` in `src/Infrastructure/Data/ApplicationDbContext.cs`

- [x] Task 4: Generate EF Migration (AC: #4, #5)
  - [x] 4.1 Run `dotnet ef migrations add AddMessagingEntities --project src/Infrastructure --startup-project src/Web` from solution root
  - [x] 4.2 Review the generated migration to verify: Conversations table, ConversationParticipants table with composite PK, Messages table with bigint identity PK, composite descending index on Messages, foreign keys
  - [x] 4.3 Verify the migration applies cleanly: `dotnet ef database update --project src/Infrastructure --startup-project src/Web`

- [x] Task 5: Integration Tests (AC: #6)
  - [x] 5.1 Create `tests/Infrastructure.IntegrationTests/Data/MessagingEntityTests.cs`
  - [x] 5.2 Test: Create a Conversation entity, persist, read back -- verify all properties round-trip correctly
  - [x] 5.3 Test: Add ConversationParticipants to a Conversation, persist, query back -- verify composite PK works and unique constraint prevents duplicate (ConversationId, UserId)
  - [x] 5.4 Test: Create Messages in a Conversation, persist, query with cursor-based pagination (`WHERE ConversationId = @id AND Id < @cursor ORDER BY Id DESC`) -- verify ordering and filtering work correctly
  - [x] 5.5 Test: Verify cascade delete -- deleting a Conversation removes its participants and messages
  - [x] 5.6 Test: Verify `LastReadMessageId` can be null and can be set to a valid message Id
  - [x] 5.7 Test framework: NUnit 4.5.1 + Shouldly 4.3.0, using existing `TestcontainersFixture` and `CustomWebApplicationFactory`
  - [x] 5.8 Update `ResetDatabaseAsync()` if needed -- the dynamic SQL approach should handle new tables automatically, but verify Redis key cleanup patterns include any new keys (none expected for this story)

### Review Findings

- [x] [Review][Dismiss] `Assert.ThrowsAsync` not awaited — NUnit's `Assert.ThrowsAsync<T>` returns `T` directly (handles async internally), not `Task<T>`; original code is correct
- [x] [Review][Defer] `LastReadMessageId` can reference message from a different conversation — no FK or app-level guard; needs validation in future unread-tracking story
- [x] [Review][Defer] Content/FileId cross-field invariants not enforced at data layer — `Text` msg allows empty content, `File` msg allows null FileId; validation belongs in Story 3.2 command handlers

## Dev Notes

### Existing Infrastructure to Reuse (DO NOT Recreate)

- **`BaseEntity`** (`src/Domain/Common/BaseEntity.cs`): Provides `Id` (long), `CreatedAt` (DateTimeOffset), and domain events. Conversation and Message should inherit from this. ConversationParticipant should NOT inherit (it's a join entity with composite PK).
- **`ConversationType` enum** (`src/Domain/Common/Enums/ConversationType.cs`): Already exists with `Private` and `Group` values. DO NOT create a new one.
- **`ApplicationDbContext`** (`src/Infrastructure/Data/ApplicationDbContext.cs`): Uses `ApplyConfigurationsFromAssembly()` -- new configurations will be auto-discovered. No manual registration needed.
- **`AuditableEntityInterceptor`** (`src/Infrastructure/Data/Interceptors/AuditableEntityInterceptor.cs`): Sets `CreatedAt` on BaseEntity. This interceptor will automatically set `CreatedAt` for Conversation and Message entities since they inherit BaseEntity. However, Message uses `SentAt` (not `CreatedAt`) as the user-facing timestamp -- `CreatedAt` will still be set by the interceptor for audit purposes.
- **`DispatchDomainEventsInterceptor`**: Dispatches domain events on SaveChanges. Available for future use (e.g., MessageSent event in Story 3.2).
- **`CustomWebApplicationFactory`** (`tests/Infrastructure.IntegrationTests/CustomWebApplicationFactory.cs`): Handles database setup with Testcontainers. `ResetDatabaseAsync()` uses dynamic SQL to clear ALL tables -- it will handle new messaging tables automatically.
- **`TestcontainersFixture`**: Provides real MSSQL + Redis containers for integration tests.
- **`ApplicationUser`** (`src/Infrastructure/Identity/ApplicationUser.cs`): Uses string Id (ASP.NET Identity default). Foreign keys `CreatedById`, `SenderId`, `UserId` must be string type to match.

### Critical Implementation Patterns

**Entity Design Decisions:**

1. **Conversation inherits BaseEntity, NOT BaseAuditableEntity**: Conversation has its own `CreatedById` (the user who created it) and doesn't need `LastModified`/`LastModifiedBy` tracking -- `LastMessageAt` serves as the activity timestamp. Using BaseAuditableEntity would add unwanted `CreatedBy`, `LastModified`, `LastModifiedBy` fields that conflict with the domain model.

2. **Message inherits BaseEntity**: Message uses `SentAt` as the domain timestamp. `CreatedAt` from BaseEntity will also be set by the AuditableEntityInterceptor -- this is fine, both will have nearly identical values. `SentAt` is the field used in queries and display.

3. **ConversationParticipant is a standalone class**: It's a join entity with a composite primary key (`ConversationId`, `UserId`). It does NOT inherit from BaseEntity because: (a) it doesn't need a separate `Id` column, (b) the composite key IS the unique identifier, (c) no domain events needed.

4. **String foreign keys for user references**: ASP.NET Identity uses `string` for user IDs (GUIDs stored as strings). `CreatedById`, `SenderId`, and `UserId` must all be `string`, not `long`.

5. **EditedAt and DeletedAt**: Defined as nullable properties on Message but unused in V1. Zero-cost future-proofing per architecture decision. Do NOT add any logic for these fields in this story.

6. **FileId**: Nullable long on Message for future file attachment support (Epic 6). No FK constraint configured in this story -- the Files table doesn't exist yet.

**EF Configuration Patterns (follow existing `ApplicationUserConfiguration.cs` style):**

```csharp
// Pattern: IEntityTypeConfiguration<T> in Infrastructure/Data/Configurations/
// Auto-discovered via ApplyConfigurationsFromAssembly() -- no manual registration needed
// Use Fluent API, NOT data annotations
// Use HasDatabaseName() for named indexes/constraints (per architecture naming: IX_{Table}_{Columns})
```

**Index Naming Convention** (from architecture):
- Indexes: `IX_{Table}_{Columns}` -- e.g., `IX_Messages_ConversationId_Id`
- Unique constraints: `UQ_{Table}_{Column}` -- e.g., `UQ_ConversationParticipants_ConversationId_UserId`

**Composite Descending Index:**
```csharp
// For cursor-based pagination: WHERE ConversationId = @id AND Id < @cursor ORDER BY Id DESC
builder.HasIndex(m => new { m.ConversationId, m.Id })
    .IsDescending(false, true)  // ConversationId ASC, Id DESC
    .HasDatabaseName("IX_Messages_ConversationId_Id");
```

**Composite Primary Key for ConversationParticipant:**
```csharp
builder.HasKey(cp => new { cp.ConversationId, cp.UserId });
```

### IApplicationDbContext Update Pattern

The `IApplicationDbContext` interface currently only has `SaveChangesAsync`. Add DbSet properties following the pattern:

```csharp
public interface IApplicationDbContext
{
    DbSet<Conversation> Conversations { get; }
    DbSet<ConversationParticipant> ConversationParticipants { get; }
    DbSet<Message> Messages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

This requires adding `using SimpleChat.Domain.Messaging;` and `using Microsoft.EntityFrameworkCore;` to the interface file. Check `GlobalUsings.cs` in the Application project -- `Microsoft.EntityFrameworkCore` is already in global usings, so only the domain namespace import is needed.

### Integration Test Pattern

Follow existing test patterns in `tests/Infrastructure.IntegrationTests/`:

```csharp
// Per-test setup pattern (from AuthEndpointTests.cs)
[SetUp]
public async Task SetUp()
{
    _factory = new CustomWebApplicationFactory();
    await _factory.ResetDatabaseAsync();
    // For entity tests: get DbContext directly instead of HttpClient
}
```

For data model tests, access `ApplicationDbContext` directly via DI scope rather than going through HTTP endpoints:

```csharp
using var scope = _factory.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
```

To create test users (needed for FK references), use the `UserManager<ApplicationUser>`:

```csharp
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var user = new ApplicationUser { UserName = "test@test.com", Email = "test@test.com", DisplayName = "Test User" };
await userManager.CreateAsync(user, "TestPass123!");
```

### Build-Breaking Rules to Watch

- **`readonly` on private fields**: Any private field that can be readonly MUST be readonly -- build fails otherwise
- **`static` on local functions**: Local functions that don't capture state MUST be static -- build fails otherwise
- **TreatWarningsAsErrors**: All nullable warnings are build errors -- proper null checks required
- **No `var`**: Use explicit types per project convention
- **Namespace must match folder path**: `SimpleChat.Domain.Messaging` for domain entities, `SimpleChat.Infrastructure.Data.Configurations` for configs

### Items NOT in Scope

- No CQRS commands/queries -- those come in Story 3.2
- No SignalR hub -- that comes in Story 3.3
- No API endpoints -- those come in Story 3.2
- No `IMessageSearchService` implementation -- comes in Epic 7
- No file storage -- comes in Epic 6
- No domain events -- those come in Story 3.2 (MessageSent notification)
- No navigation properties to `ApplicationUser` from messaging entities -- `ApplicationUser` is in Infrastructure layer; Domain entities cannot reference it (Clean Architecture dependency rule). Foreign keys use string IDs without navigation properties to the user entity.

### Project Structure Notes

**New files to create:**
```
src/Domain/Common/Enums/MessageType.cs
src/Domain/Messaging/Conversation.cs
src/Domain/Messaging/ConversationParticipant.cs
src/Domain/Messaging/Message.cs
src/Infrastructure/Data/Configurations/ConversationConfiguration.cs
src/Infrastructure/Data/Configurations/ConversationParticipantConfiguration.cs
src/Infrastructure/Data/Configurations/MessageConfiguration.cs
src/Infrastructure/Data/Migrations/<timestamp>_AddMessagingEntities.cs  (generated)
tests/Infrastructure.IntegrationTests/Data/MessagingEntityTests.cs
```

**Files to modify:**
```
src/Application/Common/Interfaces/IApplicationDbContext.cs  -- add DbSet properties
src/Infrastructure/Data/ApplicationDbContext.cs             -- add DbSet properties
```

**Files NOT to modify:**
```
src/Domain/Common/Enums/ConversationType.cs   -- already exists, leave as-is
src/Domain/Common/BaseEntity.cs               -- do not modify
src/Domain/Common/BaseAuditableEntity.cs      -- not used by messaging entities
```

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-3-real-time-private-messaging.md#Story 3.1]
- [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture -- Message ID type, History index, Unread tracking, Future-proofing]
- [Source: _bmad-output/planning-artifacts/architecture.md#Architecture Patterns -- Message Persistence section]
- [Source: _bmad-output/planning-artifacts/architecture.md#Naming Patterns -- Database Naming Conventions]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation Sequence -- Step 4: Messaging module]
- [Source: _bmad-output/project-context.md#EF Core -- Fluent API configurations, IEntityTypeConfiguration, auto-discovered]
- [Source: _bmad-output/project-context.md#Clean Architecture + CQRS -- 4-project structure, feature folders by domain module]
- [Source: _bmad-output/project-context.md#Anti-Patterns -- No repository pattern, inject IApplicationDbContext directly]
- [Source: _bmad-output/project-context.md#Testing Rules -- NUnit, Shouldly, Testcontainers]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

### Completion Notes List

- MessageType enum created with Text, File, System values in Domain/Common/Enums
- Conversation entity inherits BaseEntity (not BaseAuditableEntity) with ConversationType, Name (nullable), CreatedById (string for ASP.NET Identity), LastMessageAt; navigation properties to Participants and Messages collections
- ConversationParticipant is a standalone class (no BaseEntity) with composite PK (ConversationId, UserId), JoinedAt, LastReadMessageId (nullable long); navigation to Conversation
- Message entity inherits BaseEntity with ConversationId, SenderId (string), Content (max 4000), SentAt, MessageType, FileId (nullable), EditedAt (nullable), DeletedAt (nullable); navigation to Conversation
- Three EF Core Fluent API configurations created following existing ApplicationUserConfiguration pattern; auto-discovered via ApplyConfigurationsFromAssembly()
- ConversationParticipantConfiguration uses composite PK and cascade delete FK to Conversations
- MessageConfiguration includes composite descending index IX_Messages_ConversationId_Id for cursor-based pagination (ConversationId ASC, Id DESC)
- IApplicationDbContext updated with DbSet<Conversation>, DbSet<ConversationParticipant>, DbSet<Message> properties
- ApplicationDbContext updated with corresponding DbSet properties using Set<T>() pattern
- EF Migration AddMessagingEntities generated and verified: 3 tables, bigint identity PKs, composite PK, composite descending index, cascade delete FKs
- 6 integration tests created covering: Conversation round-trip, participant persistence with composite key, duplicate key constraint enforcement, cursor-based message pagination, cascade delete, LastReadMessageId nullable/settable
- Existing ConversationType enum reused (not recreated)
- Dynamic ResetDatabaseAsync() handles new tables automatically — no update needed
- Build: 0 errors, 0 warnings; Unit tests: 49/49 pass; Integration tests: 32/32 pass (6 new + 26 existing)

### Change Log

- 2026-04-02: Story 3.1 implemented — messaging data model (Conversation, ConversationParticipant, Message entities), EF configurations with indexes, migration, and integration tests

### File List

New files:
- src/Domain/Common/Enums/MessageType.cs
- src/Domain/Messaging/Conversation.cs
- src/Domain/Messaging/ConversationParticipant.cs
- src/Domain/Messaging/Message.cs
- src/Infrastructure/Data/Configurations/ConversationConfiguration.cs
- src/Infrastructure/Data/Configurations/ConversationParticipantConfiguration.cs
- src/Infrastructure/Data/Configurations/MessageConfiguration.cs
- src/Infrastructure/Data/Migrations/20260402073509_AddMessagingEntities.cs
- src/Infrastructure/Data/Migrations/20260402073509_AddMessagingEntities.Designer.cs
- tests/Infrastructure.IntegrationTests/Data/MessagingEntityTests.cs

Modified files:
- src/Application/Common/Interfaces/IApplicationDbContext.cs (added DbSet properties)
- src/Infrastructure/Data/ApplicationDbContext.cs (added DbSet properties)
- src/Infrastructure/Data/Migrations/ApplicationDbContextModelSnapshot.cs (auto-updated by EF)
