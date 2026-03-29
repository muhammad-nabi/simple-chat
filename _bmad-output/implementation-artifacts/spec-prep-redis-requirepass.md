---
title: 'Add Redis requirepass Authentication'
type: 'chore'
created: '2026-03-29'
status: 'done'
baseline_commit: '24c46cf'
context: ['_bmad-output/implementation-artifacts/deferred-work.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Redis has no authentication — any container on the Docker network can read/write the cache. Story 2.2 will store JWT refresh tokens in Redis, making unauthenticated access a session-theft vector.

**Approach:** Add `requirepass` to Redis via Docker Compose command override, propagate the password through the connection string in `.env`, and update both DI registrations and the Testcontainers fixture to use password-authenticated connections.

## Boundaries & Constraints

**Always:** Use `.env` variable for the Redis password — never hardcode in docker-compose.yml. Connection string format must be `host:port,password=xxx`. Health check must authenticate. All existing tests must pass.

**Ask First:** If Redis password needs to be injected differently for production vs development scenarios.

**Never:** Do not change Redis image version. Do not add Redis persistence (volumes). Do not modify application logic beyond connection configuration.

</frozen-after-approval>

## Code Map

- `docker-compose.yml` -- Redis service: add `command: redis-server --requirepass`, update app env var connection string
- `.env` -- Add `REDIS_PASSWORD` variable
- `.env.example` -- Document `REDIS_PASSWORD` variable
- `src/Infrastructure/DependencyInjection.cs:56` -- Redis connection already parses ConfigurationOptions; password flows via connection string
- `src/Web/DependencyInjection.cs:38` -- Health check uses raw connection string; password must be included
- `tests/Infrastructure.IntegrationTests/TestcontainersFixture.cs:24` -- Testcontainer needs password configuration
- `tests/Infrastructure.IntegrationTests/CustomWebApplicationFactory.cs:16` -- UseSetting override gets password from fixture

## Tasks & Acceptance

**Execution:**
- [x] `docker-compose.yml` -- Add `command: redis-server --requirepass ${REDIS_PASSWORD:-SimpleChat_Redis_Dev!}` to redis service. Update app environment: `Redis__ConnectionString=redis:6379,password=${REDIS_PASSWORD:-SimpleChat_Redis_Dev!}`. Update redis healthcheck to authenticate: `redis-cli -a` with the password, redirect stderr to suppress warning
- [x] `.env` -- Add `REDIS_PASSWORD=SimpleChat_Redis_Dev!`
- [x] `.env.example` -- Add `REDIS_PASSWORD=SimpleChat_Redis_Dev!` with comment explaining it should be changed in production
- [x] `src/Infrastructure/DependencyInjection.cs` -- No change needed; `ConfigurationOptions.Parse()` already handles `password=xxx` in connection string
- [x] `src/Web/DependencyInjection.cs` -- No change needed; `AddRedis()` accepts full connection string with password
- [x] `tests/Infrastructure.IntegrationTests/TestcontainersFixture.cs` -- Configure Redis testcontainer with a password using `.WithCommand("redis-server", "--requirepass", "testpassword")` and append `,password=testpassword` to the exposed connection string
- [x] `tests/Infrastructure.IntegrationTests/CustomWebApplicationFactory.cs` -- No change needed; already uses `TestcontainersFixture.RedisConnectionString` which will include password
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- Mark Redis auth deferred item as resolved

**Acceptance Criteria:**
- Given Docker Compose starts, when redis-cli connects without password, then connection is rejected (NOAUTH)
- Given the app container starts, when it connects to Redis with the configured password, then connection succeeds and `/health/ready` returns 200
- Given the Testcontainers integration tests run, when Redis is provisioned, then it requires authentication and tests pass
- Given `dotnet build`, then zero errors

## Verification

**Commands:**
- `dotnet build SimpleChat.slnx --configuration Release` -- expected: 0 errors
- `dotnet test SimpleChat.slnx --filter "FullyQualifiedName~UnitTests"` -- expected: all pass

## Suggested Review Order

- Redis now requires password — `--requirepass` via env var with fallback default
  [`docker-compose.yml:57`](../../docker-compose.yml#L57)

- App connection string includes password, same variable/fallback as server
  [`docker-compose.yml:11`](../../docker-compose.yml#L11)

- Healthcheck authenticates via env var, stderr suppressed to avoid noise
  [`docker-compose.yml:59`](../../docker-compose.yml#L59)

- Dev override documents auth requirement for local redis-cli access
  [`docker-compose.override.yml:17`](../../docker-compose.override.yml#L17)

- REDIS_PASSWORD variable documented for operators
  [`.env.example:18`](../../.env.example#L18)

- Test Redis container starts with requirepass, connection string appends password
  [`TestcontainersFixture.cs:28`](../../tests/Infrastructure.IntegrationTests/TestcontainersFixture.cs#L28)

- Deferred item resolved + new deferred items for production hardening
  [`deferred-work.md:27`](deferred-work.md#L27)
