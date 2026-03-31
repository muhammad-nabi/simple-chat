---
title: 'Fix test infrastructure — password validator + dynamic table reset'
type: 'chore'
created: '2026-03-31'
status: 'done'
baseline_commit: 'f21e830'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Integration tests fail because ASP.NET Identity's default password validator rejects test passwords (`password123` lacks uppercase/special chars), and `ResetDatabaseAsync` uses hardcoded `DELETE FROM [AspNetUserRoles]; DELETE FROM [AspNetUsers]` which will break with FK violations when Epic 3 adds domain tables referencing Users. Both are CRITICAL prep tasks from the Epic 2 retrospective.

**Approach:** Relax Identity password options in `CustomWebApplicationFactory` for the test environment only. Replace hardcoded delete SQL with `EnsureDeletedAsync()` + `MigrateAsync()` for schema-agnostic test isolation.

## Boundaries & Constraints

**Always:** Changes scoped to test project only — production Identity configuration untouched. All existing integration tests must pass after changes. Redis cleanup logic preserved.

**Ask First:** If any test needs password changes beyond relaxing the validator (e.g., tests that explicitly validate password complexity).

**Never:** Modify production `DependencyInjection.cs` or Identity configuration. Add new test dependencies.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Password `password123` accepted | Register with `password123` in tests | Identity accepts, user created | N/A |
| Reset with only Identity tables | DB has only AspNet* tables | DB dropped and recreated via migrations | N/A |
| Reset with future domain tables | DB has AspNet* + Conversations + Messages | DB dropped and recreated via migrations — no FK issues | N/A |

</frozen-after-approval>

## Code Map

- `tests/Infrastructure.IntegrationTests/CustomWebApplicationFactory.cs` -- Test DI overrides, `ResetDatabaseAsync`, `InitialiseDatabaseAsync`
- `tests/Infrastructure.IntegrationTests/Api/AuthEndpointTests.cs` -- Consumer of `ResetDatabaseAsync`, uses `password123` throughout
- `tests/Infrastructure.IntegrationTests/HealthCheckIntegrationTests.cs` -- Consumer of `InitialiseDatabaseAsync`
- `src/Infrastructure/DependencyInjection.cs` -- Production Identity config (READ ONLY — do not modify)

## Tasks & Acceptance

**Execution:**
- [x] `tests/Infrastructure.IntegrationTests/CustomWebApplicationFactory.cs` -- Add `Configure<IdentityOptions>` in `ConfigureServices` to relax password requirements (no uppercase, no digit, no special char, min length 6) -- Fixes password rejection in tests
- [x] `tests/Infrastructure.IntegrationTests/CustomWebApplicationFactory.cs` -- Replace `ResetDatabaseAsync` body: dynamic SQL clears all tables (disable FK constraints, delete, re-enable) -- Schema-agnostic reset that survives new tables

**Acceptance Criteria:**
- Given the test environment, when registering with password `password123`, then Identity accepts the password and the user is created successfully
- Given any database state with any combination of tables, when `ResetDatabaseAsync` is called, then all tables are dropped and recreated from migrations with no FK violation errors
- Given the full integration test suite, when `dotnet test` is run, then all tests pass (0 failures)

## Verification

**Commands:**
- `dotnet test tests/Infrastructure.IntegrationTests/ --verbosity normal` -- expected: all tests pass, 0 failures

## Suggested Review Order

- Entry point: password relaxation for test DI — fixes `password123` rejection
  [`CustomWebApplicationFactory.cs:46`](CustomWebApplicationFactory.cs#L46)

- Dynamic SQL replaces hardcoded DELETE — schema-agnostic reset via constraint toggle
  [`CustomWebApplicationFactory.cs:72`](CustomWebApplicationFactory.cs#L72)
