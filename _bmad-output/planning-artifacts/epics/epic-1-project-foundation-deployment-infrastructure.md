# Epic 1: Project Foundation & Deployment Infrastructure

Operators can deploy, configure, monitor, upgrade, and back up simple-chat using a single Docker Compose command.

## Story 1.1: Project Scaffold & Solution Structure

As an operator,
I want the project scaffolded with the correct architecture and module organization,
So that all subsequent development follows a consistent, proven structure.

**Acceptance Criteria:**

**Given** a developer runs the Jason Taylor Clean Architecture template command (`dotnet new ca-sln --client-framework Angular --database sqlserver --output SimpleChat`)
**When** the scaffold is generated
**Then** the solution contains four projects: SimpleChat.Domain, SimpleChat.Application, SimpleChat.Infrastructure, SimpleChat.API (Web)
**And** each project compiles without errors

**Given** the scaffolded solution
**When** post-scaffold customizations are applied
**Then** the Domain project contains module folders: Identity/, Messaging/, Presence/, Files/ with Entities/ and Events/ subfolders per module
**And** the Domain project contains Common/ with BaseEntity.cs (Id as long, CreatedAt) and Enums/ (ConversationType, UserRole, PresenceStatus)
**And** the Application project contains matching module folders with Commands/, Queries/, EventHandlers/ subfolders
**And** the Application project contains Common/Interfaces/ with stubs for IFileStorageService, IMessageSearchService, IAuthenticationProvider, ICacheService
**And** the Application project contains Common/Behaviors/ with ValidationBehavior and LoggingBehavior pipeline behaviors
**And** the Application project contains Common/Exceptions/ with NotFoundException and ForbiddenAccessException
**And** the Application project contains Common/Models/ with PagedResult (Items, HasMore, NextCursor)

**Given** the scaffolded Angular frontend
**When** post-scaffold customizations are applied
**Then** the Angular app contains src/app/core/, src/app/shared/, src/app/features/ directories
**And** the features directory contains empty feature module folders: auth/, chat/, admin/, search/
**And** the Angular app contains src/app/models/ for TypeScript interfaces
**And** the test runner is switched from Karma to Jest with a passing default test
**And** the Angular app builds without errors using `ng build`

## Story 1.2: Docker Compose & Container Configuration

As an operator,
I want to deploy simple-chat with a single `docker-compose up` command,
So that I can run the complete application stack without manual service configuration.

**Acceptance Criteria:**

**Given** a server with Docker and Docker Compose installed
**When** the operator runs `docker-compose up`
**Then** three containers start: the application container, MSSQL (SQL Server), and Redis
**And** the application container is built from a multi-stage Dockerfile (build → publish → runtime)
**And** the MSSQL container has its memory capped at approximately 512MB via environment variable (`MSSQL_MEMORY_LIMIT_MB` or SA settings)
**And** the Redis container starts with default configuration

**Given** the Docker Compose configuration
**When** the operator inspects the defined volumes
**Then** a named volume exists for MSSQL data persistence
**And** a named volume exists for file uploads
**And** Redis is configured as ephemeral (no persistent volume required)

**Given** the Docker Compose configuration
**When** the operator reviews environment variables
**Then** all application configuration is manageable via environment variables in the compose file
**And** sensible defaults are provided for: database connection string, Redis connection, JWT secret, max file upload size
**And** no in-container file editing is required for basic deployment (NFR29)

**Given** the built Docker image
**When** the operator checks the image size
**Then** the image is under 500MB (NFR33)

**Given** the operator wants to upgrade
**When** they run `docker-compose pull && docker-compose up -d`
**Then** the new image is pulled and containers restart with zero manual migration steps (FR42)

## Story 1.3: Database Initialization & Auto-Migration

As an operator,
I want the database to initialize and migrate automatically on application startup,
So that I never need to run manual migration commands during deployment or upgrades.

**Acceptance Criteria:**

**Given** the application container starts with a fresh (empty) MSSQL database
**When** the application startup sequence runs
**Then** Entity Framework Core `MigrateAsync()` executes automatically
**And** all pending migrations are applied to create the initial schema
**And** the application becomes healthy after migrations complete

**Given** the application has previously run migrations
**When** the application restarts (e.g., container restart or upgrade)
**Then** `MigrateAsync()` runs and detects no pending migrations
**And** the startup completes without errors (idempotent — NFR26)

**Given** a slow MSSQL startup (container still initializing)
**When** the application attempts to connect
**Then** the application retries database connectivity with backoff rather than crashing
**And** the startup health probe (`/health/startup`) reports unhealthy until migrations complete
**And** Docker healthcheck does not trigger a restart loop during initial MSSQL startup

**Given** the application startup sequence
**When** measured end-to-end from container launch to healthy status
**Then** the application becomes healthy within 30 seconds under normal conditions (NFR27)

## Story 1.4: Structured Logging & Health Endpoints

As an operator,
I want structured JSON logs and health check endpoints,
So that I can monitor the application using standard infrastructure tooling.

**Acceptance Criteria:**

**Given** the application is running
**When** any log event occurs (startup, request, error)
**Then** the log is written to stdout in JSON format using Serilog (NFR30)
**And** log entries include structured fields (timestamp, level, message template, properties)
**And** log levels follow the defined standards: Debug (detailed flow), Information (business events), Warning (recoverable issues), Error (failures), Fatal (cannot continue)
**And** logs use Serilog message templates with named parameters — never string interpolation

**Given** the application is running and healthy
**When** an HTTP GET request is sent to `/health/startup`
**Then** a 200 OK response is returned indicating initialization is complete

**Given** the application is running
**When** an HTTP GET request is sent to `/health/live`
**Then** a 200 OK response is returned indicating the process is alive

**Given** the application is running with all dependencies available
**When** an HTTP GET request is sent to `/health/ready`
**Then** a 200 OK response is returned
**And** the response includes MSSQL connectivity status
**And** the response includes Redis connectivity status

**Given** the MSSQL database is unavailable
**When** an HTTP GET request is sent to `/health/ready`
**Then** a 503 Service Unavailable response is returned indicating database connectivity failure

**Given** the Redis instance is unavailable
**When** an HTTP GET request is sent to `/health/ready`
**Then** a 503 Service Unavailable response is returned indicating cache connectivity failure

## Story 1.5: CI Pipeline Configuration

As a developer,
I want an automated CI pipeline that validates code quality on every pull request,
So that broken code and regressions are caught before merging.

**Acceptance Criteria:**

**Given** a pull request is opened against the repository
**When** the GitHub Actions CI workflow runs
**Then** the pipeline executes in sequence: unit tests + linting → integration tests → Docker image build
**And** the pipeline fails fast if any stage fails (subsequent stages do not run)

**Given** the CI pipeline runs integration tests
**When** the tests execute
**Then** Testcontainers provisions real MSSQL and Redis instances for integration test execution
**And** integration tests run against actual database and cache infrastructure (not mocks)

**Given** a commit is pushed to the main branch
**When** the CI workflow completes successfully
**Then** the Docker image is built and pushed to GitHub Container Registry (ghcr.io)
**And** the image is tagged with the commit SHA and `latest`

**Given** the CI pipeline configuration
**When** a developer reviews the workflow file
**Then** both backend (.NET) and frontend (Angular) linters are configured and enforced
**And** the total pipeline duration should aim for under 5 minutes for the PR workflow (soft target, not a hard gate)
