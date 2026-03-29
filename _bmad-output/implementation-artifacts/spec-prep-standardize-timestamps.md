---
title: 'Standardize Entity Timestamps to DateTimeOffset'
type: 'refactor'
created: '2026-03-29'
status: 'done'
baseline_commit: '4e3458dc'
context: ['_bmad-output/implementation-artifacts/deferred-work.md', '_bmad-output/implementation-artifacts/epic-1-retro-2026-03-29.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `BaseEntity.CreatedAt` uses `DateTime` while `BaseAuditableEntity.Created` uses `DateTimeOffset`. This creates an inconsistent timestamp contract — future entities extending either base class will have mismatched precision and timezone semantics. The deferred work register flags this: "reconcile when entities created in Stories 2.x/3.x." Since Story 2.1 will create the first real entities, this must be resolved now before any entity migrations are generated.

**Approach:** Standardize `BaseEntity.CreatedAt` to `DateTimeOffset`. Remove the redundant `BaseAuditableEntity.Created` property since both represent creation time. Update the `AuditableEntityInterceptor` to set `CreatedAt` on the base entity instead of `Created` on the auditable entity. No migration needed — no concrete entities use these base classes yet.

## Boundaries & Constraints

**Always:** Use `DateTimeOffset` for all timestamp properties. Preserve the `init` accessor on `BaseEntity.CreatedAt` — change to `set` so the interceptor can write it. All existing tests must pass.

**Ask First:** If any concrete entity is discovered that extends these base classes (investigation found none, but verify at implementation time).

**Never:** Do not generate an EF Core migration — no database tables reference these properties yet. Do not change `Id` or domain event logic in `BaseEntity`. Do not modify `LastModified`/`LastModifiedBy` properties.

</frozen-after-approval>

## Code Map

- `src/Domain/Common/BaseEntity.cs` -- Owns `CreatedAt` (DateTime → DateTimeOffset, init → set)
- `src/Domain/Common/BaseAuditableEntity.cs` -- Remove redundant `Created`/`CreatedBy`, keep `LastModified`/`LastModifiedBy`
- `src/Infrastructure/Data/Interceptors/AuditableEntityInterceptor.cs` -- Update to set `CreatedAt` on BaseEntity instead of `Created` on BaseAuditableEntity

## Tasks & Acceptance

**Execution:**
- [x] `src/Domain/Common/BaseEntity.cs` -- Change `CreatedAt` from `DateTime` to `DateTimeOffset`, change `init` to `set` so interceptor can write it
- [x] `src/Domain/Common/BaseAuditableEntity.cs` -- Remove `Created` and `CreatedBy` properties (moved to base responsibility via interceptor). Keep `LastModified` and `LastModifiedBy`
- [x] `src/Infrastructure/Data/Interceptors/AuditableEntityInterceptor.cs` -- On Added: set `entry.Entity.CreatedAt` (BaseEntity) instead of `entry.Entity.Created`. Move `CreatedBy` logic to set on BaseAuditableEntity only. Adjust the foreach to handle both BaseEntity (for CreatedAt) and BaseAuditableEntity (for audit fields)
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- Mark the timestamp reconciliation item as resolved

**Acceptance Criteria:**
- Given `BaseEntity`, when inspected, then `CreatedAt` is type `DateTimeOffset`
- Given `BaseAuditableEntity`, when inspected, then it has no `Created` property — only `LastModified`, `LastModifiedBy`, and inherited `CreatedAt`
- Given a `BaseAuditableEntity` is saved with `EntityState.Added`, when the interceptor fires, then `CreatedAt` is set to UTC now
- Given the solution, when `dotnet build` runs, then zero errors and zero warnings related to timestamp types

## Verification

**Commands:**
- `dotnet build SimpleChat.slnx --configuration Release` -- expected: 0 errors
- `dotnet test SimpleChat.slnx` -- expected: all tests pass

## Suggested Review Order

- Entry point: `CreatedAt` standardized from `DateTime init` to `DateTimeOffset set`
  [`BaseEntity.cs:8`](../../src/Domain/Common/BaseEntity.cs#L8)

- Redundant `Created` property removed — creation timestamp now inherited from `BaseEntity`
  [`BaseAuditableEntity.cs:4`](../../src/Domain/Common/BaseAuditableEntity.cs#L4)

- Single `utcNow` captured once, new `BaseEntity` loop sets `CreatedAt` on Added
  [`AuditableEntityInterceptor.cs:40`](../../src/Infrastructure/Data/Interceptors/AuditableEntityInterceptor.cs#L40)

- Existing audit loop updated — removed `Created` assignment, reuses shared `utcNow`
  [`AuditableEntityInterceptor.cs:48`](../../src/Infrastructure/Data/Interceptors/AuditableEntityInterceptor.cs#L48)

- Deferred item marked resolved + new deferred item for future migration default value
  [`deferred-work.md:6`](deferred-work.md#L6)
