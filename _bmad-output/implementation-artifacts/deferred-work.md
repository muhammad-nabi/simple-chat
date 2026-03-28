# Deferred Work

## Deferred from: code review of story-1-1 (2026-03-28)

- CORS AllowAny* not environment-gated in Program.cs — production CORS hardening belongs to Story 1.2/security
- Duplicate creation timestamps: BaseEntity.CreatedAt (DateTime) vs BaseAuditableEntity.Created (DateTimeOffset) — reconcile when entities created in Stories 2.x/3.x
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
