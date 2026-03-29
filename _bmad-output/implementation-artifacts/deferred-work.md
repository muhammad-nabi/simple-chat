# Deferred Work

## Deferred from: code review of story-1-1 (2026-03-28)

- CORS AllowAny* not environment-gated in Program.cs — production CORS hardening belongs to Story 1.2/security
- ~~Duplicate creation timestamps: BaseEntity.CreatedAt (DateTime) vs BaseAuditableEntity.Created (DateTimeOffset) — reconcile when entities created in Stories 2.x/3.x~~ **RESOLVED** (prep-standardize-timestamps, 2026-03-29): Standardized to DateTimeOffset on BaseEntity.CreatedAt, removed redundant Created from BaseAuditableEntity
- DB initializer calls EnsureDeletedAsync then EnsureCreatedAsync — destroys data every startup, Story 1.3 covers proper DB initialization
- Hardcoded admin password "Administrator1!" in ApplicationDbContextInitialiser — template default, should be externalized before production
- .GetAwaiter().GetResult() in DispatchDomainEventsInterceptor sync path — deadlock risk under load
- Domain events dispatched before SaveChanges commits — side-effects fire for uncommitted state
- Domain event handler exception leaves events partially cleared — no retry possible for failed events
- Role authorization doesn't trim whitespace in comma-separated roles — "Admin, Editor" split produces " Editor" with leading space
- Seed user creation result not checked — silent failure if password policy rejects
- Logout endpoint body check logic inverted — any JSON body passes, no body returns 401
- PerformanceBehaviour Stopwatch not reset between calls — elapsed time accumulates across requests
- Auditable entity interceptor writes null UserId for system/background operations
- Logging behaviour logs entire request object including potential PII (passwords, tokens)
- ExceptionHandler middleware registered after static files middleware — file-serving exceptions bypass custom handler
- provideHttpClient() missing XSRF/fetch options — address when auth is implemented (Story 2.2)
- Production bundle budget thresholds too permissive (1MB warn / 5MB error vs Angular default 500kB / 1MB)
- No dark-mode token set — picoColorScheme script implied dark mode was planned but no token overrides exist

## Deferred from: code review of story-1-2 (2026-03-29)

- Dockerfile has no USER directive — app runs as root in container; add non-root user for security hardening
- Special characters in MSSQL_SA_PASSWORD (e.g., `"`, `$`, `;`) could break the MSSQL healthcheck quoting or the connection string delimiter parsing
- ~~Redis has no authentication (requirepass not set) — any container on the network can read/write; add auth before production~~ **RESOLVED** (prep-redis-requirepass, 2026-03-29): Added requirepass via Docker Compose command, password in .env, propagated through connection strings
- JWT default secret is a committed known value in docker-compose.yml — enforce secret validation when JWT auth is implemented (Story 2.2)
- AC5 upgrade path requires `image:` tag for registry pull — add when CI/CD pipeline pushes to a container registry (Story 1.5)

## Deferred from: code review of story-1-3 (2026-03-29)

- Seed data hardcoded password "Administrator1!" now runs in all environments (not just Development) — externalization deferred to Story 2.x per spec

## Deferred from: code review of story-1-4 (2026-03-29)

- Redis connection string read independently in two DI files (Infrastructure/DependencyInjection.cs + Web/DependencyInjection.cs) — DRY violation across project boundaries, risk of divergence if one fallback changes
- No explicit timeout on DB/Redis health checks — Docker curl timeout (3s) could expire before ASP.NET health check completes if dependency is slow but alive; operational tuning concern
- PII safety test in RequestLoggerTests uses brittle negative string match (`!v.ToString()!.Contains("TestRequest {")`) — depends on serialization format, consider positive assertion

## Deferred from: code review of story-1-5 (2026-03-29)

- Redis `ConnectionMultiplexer` instance registered via `AddSingleton(instance)` in CustomWebApplicationFactory won't be disposed by DI — cosmetic in test context since process exits after tests and Testcontainer is disposed independently

## Deferred from: prep-standardize-timestamps review (2026-03-29)

- First domain entity migration will expose `BaseEntity.CreatedAt` as a new non-nullable `datetimeoffset` column — ensure migration includes appropriate default value or make column nullable during Story 2.1 migration generation

## Deferred from: prep-redis-requirepass review (2026-03-29)

- Redis password passed via `--requirepass` CLI arg is visible in `docker inspect` and process list — use Docker secrets or config file for production hardening
- Redis connection string with password embedded — special characters in future passwords could break StackExchange.Redis `ConfigurationOptions.Parse()` delimiter parsing
