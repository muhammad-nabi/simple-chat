# Epic 2: User Authentication & Account Management

Users can register, log in, maintain sessions across refreshes, and log out. The first registered user is designated as the instance administrator.

## Story 2.1: User Registration with First-User Admin Designation

As a new team member,
I want to register an account with my display name, email, and password,
So that I can access the chat application and start communicating with my team.

**Acceptance Criteria:**

**Given** the registration page is displayed
**When** a user submits a valid display name, email address, and password
**Then** a new User entity is created with the provided display name, email, and a bcrypt-hashed password (NFR7)
**And** the user is assigned the Member role by default
**And** the user is automatically logged in (JWT access token issued, refresh token set as HttpOnly cookie)
**And** the user is redirected to the main conversation list view — no redirect to login page

**Given** no users exist in the system (fresh deployment)
**When** the first user completes registration
**Then** that user is automatically designated as the instance administrator (UserRole.Admin) (FR5)
**And** subsequent registrations create Member-role users

**Given** the registration form
**When** the user submits an email that is already registered
**Then** an inline error alert is displayed above the form: "An account with this email already exists"
**And** the form is not cleared — user input is preserved

**Given** the registration form (UX-DR16)
**When** the user blurs a field with invalid input
**Then** inline validation errors appear below the field in friendly language (e.g., "Please enter a valid email", "Password must be at least 8 characters")
**And** the field border turns red with red helper text
**And** the Submit button remains disabled until all fields are valid

**Given** the registration form
**When** the user submits a password shorter than 8 characters
**Then** inline validation rejects with: "Password must be at least 8 characters"
**And** no complexity rules beyond minimum length are enforced in V1

**Given** the Register component layout
**When** rendered on any viewport
**Then** the form displays three fields: Display Name, Email, Password — each with a visible label above the field (not placeholder-only)
**And** the Submit button shows a loading spinner during submission and is disabled to prevent double-submit
**And** the form is accessible: labels associated with inputs, validation errors linked via aria-describedby

## Story 2.2: User Login & JWT Session

As a registered user,
I want to log in with my email and password,
So that I can access my conversations and messages securely.

**Acceptance Criteria:**

**Given** the login page is displayed (UX-DR15)
**When** a user submits a valid email and password
**Then** the backend verifies the password against the stored bcrypt hash
**And** a short-lived JWT access token (15-30 minute expiry) is generated and returned in the response body
**And** a refresh token is issued as an HttpOnly, Secure, SameSite=Strict cookie with 7-day expiry
**And** a Redis session entry is created with a TTL matching the refresh token expiry
**And** the Angular app stores the access token in service memory (not localStorage — eliminates XSS token theft risk)
**And** the user is redirected to the main conversation list view

**Given** a user submits incorrect credentials
**When** the login request is processed
**Then** a generic error message is displayed: "Invalid email or password" (no indication of which field is wrong)
**And** the form is not cleared — email input is preserved, password is cleared

**Given** a deactivated user attempts to log in with valid credentials
**When** the login request is processed
**Then** the login is rejected with an appropriate error message
**And** no tokens are issued (NFR13)

**Given** the login endpoint
**When** multiple failed login attempts occur
**Then** rate limiting is enforced per-IP (20 attempts/minute) AND per-username (5 attempts/minute) via configurable environment variables
**And** rate limit responses use standard 429 Too Many Requests status

**Given** the Login component layout
**When** rendered on any viewport
**Then** the form displays Email and Password fields with visible labels above each field
**And** inline validation on blur with friendly language
**And** server errors displayed as inline alert above form (not toast)
**And** Submit button with loading spinner during request

## Story 2.3: Session Persistence & Token Refresh

As an authenticated user,
I want my session to persist across browser refreshes and handle token expiry silently,
So that I don't have to log in repeatedly during my workday.

**Acceptance Criteria:**

**Given** a user has a valid session (refresh token cookie exists)
**When** the user refreshes the browser or opens a new tab
**Then** the Angular app detects the absence of an in-memory access token
**And** automatically calls the `/api/auth/refresh` endpoint using the refresh token cookie
**And** a new access token is returned and stored in service memory
**And** the user lands on the conversation list without seeing a login screen (FR4)

**Given** the access token expires during an active session
**When** an API request returns 401 Unauthorized
**Then** the Angular auth interceptor queues the failed request
**And** calls `/api/auth/refresh` to obtain a new access token
**And** retries the original request with the new token
**And** concurrent 401s are batched — only one refresh request is made (prevents thundering herd)

**Given** a refresh token is used successfully
**When** the new tokens are issued
**Then** a new refresh token is issued (rotation) and the old refresh token is invalidated
**And** the Redis session entry is updated with the new refresh token identifier

**Given** a previously used (rotated-out) refresh token is presented
**When** the refresh endpoint processes it
**Then** reuse is detected as a potential token theft
**And** all sessions for that user are invalidated (emergency lockout — all Redis session entries deleted)
**And** the user must log in again on all devices

**Given** the refresh token cookie has expired (7+ days inactive)
**When** the user opens the application
**Then** the refresh attempt fails
**And** a non-disruptive overlay is shown for re-authentication in-place (UX-DR28)
**And** after re-authentication, the conversation state refreshes silently — scroll position and context preserved

## Story 2.4: Logout & Session Invalidation

As an authenticated user,
I want to log out and have my session immediately invalidated,
So that my account is secure when I'm done using the application.

**Acceptance Criteria:**

**Given** an authenticated user
**When** the user clicks the Logout action
**Then** the backend deletes the Redis session entry for this refresh token
**And** the refresh token cookie is cleared (expired)
**And** the Angular app clears the in-memory access token and any cached application state
**And** the user is redirected to the login page

**Given** a user has active sessions on multiple devices
**When** the user logs out on one device
**Then** only that device's session is invalidated
**And** other device sessions remain active (single-device logout, not global)

**Given** an administrator deactivates a user account (covered fully in Epic 8)
**When** the deactivation is processed
**Then** all Redis session entries for that user are deleted immediately
**And** the deactivated user's next API request or SignalR message fails with 401/403
**And** the deactivated user is unable to obtain new tokens via refresh (NFR13)

**Given** the API endpoints (excluding register, login, health)
**When** any request is made without a valid JWT access token
**Then** the request is rejected with 401 Unauthorized (NFR12)
**And** no user data or messages are accessible without authentication
