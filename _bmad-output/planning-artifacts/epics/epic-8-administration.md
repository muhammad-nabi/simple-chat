# Epic 8: Administration

Administrators can view all users, assign roles, deactivate/reactivate accounts with immediate effect, and create default group conversations for team onboarding.

## Story 8.1: Admin Panel & User List

As an administrator,
I want to view all registered users with their roles and status,
So that I can manage my team's access to the chat application.

**Acceptance Criteria:**

**Given** an admin-role user
**When** they view the sidebar
**Then** an "Admin" section is visible (not visible to Member-role users)
**And** tapping "Admin" navigates to the admin panel

**Given** the admin panel
**When** the User List component renders (UX-DR17)
**Then** a table displays all registered users with columns: display name, email, role (Admin/Member), status (Active/Inactive)
**And** the list is searchable/filterable by name or email

**Given** the admin panel is accessed by a non-admin user (direct URL or API)
**When** the request is processed
**Then** the API returns 403 Forbidden
**And** the Angular route guard redirects to the main chat view

**Given** the admin feature module
**When** loaded
**Then** it is lazy-loaded via Angular routing
**And** guarded by both authentication and admin-role authorization

## Story 8.2: Role Assignment

As an administrator,
I want to assign roles to users,
So that I can delegate admin responsibilities to trusted team members.

**Acceptance Criteria:**

**Given** the user list table
**When** an admin clicks the role dropdown on a user row
**Then** the options "Admin" and "Member" are displayed with brief descriptions (e.g., "Admin — can manage users and settings", "Member — standard chat access")

**Given** an admin selects a new role for a user
**When** the role change is submitted
**Then** the user's role is updated immediately in the database
**And** a subtle confirmation is shown (row updates, no toast — per "quiet confidence" principle)
**And** the change takes effect on the user's next API request (role checked from JWT or database)

**Given** an admin attempts to remove their own admin role
**When** they are the last remaining admin
**Then** the action is prevented with a clear message: "Cannot remove the last administrator"

## Story 8.3: User Deactivation & Reactivation

As an administrator,
I want to deactivate departing team members and reactivate returning ones,
So that access is controlled without losing any message history.

**Acceptance Criteria:**

**Given** an admin clicks "Deactivate" on an active user row
**When** the deactivation confirmation dialog appears (UX-DR18)
**Then** the dialog explains consequences: "This will immediately block {user}'s access. All messages are preserved. You can reactivate later."
**And** the dialog has a destructive red "Deactivate" button and a "Cancel" button
**And** focus is trapped within the modal; Escape closes it

**Given** the admin confirms deactivation
**When** the deactivation is processed
**Then** the user's IsActive flag is set to false
**And** all Redis session entries for that user are deleted immediately — instant lockout (NFR13)
**And** the user's next API request or SignalR connection attempt fails with 401/403
**And** the user list row updates to show "Inactive" status

**Given** an admin clicks "Reactivate" on an inactive user
**When** the reactivation is processed
**Then** the user's IsActive flag is set to true
**And** the user can log in again
**And** the user list row updates to show "Active" status
**And** no confirmation dialog is needed for reactivation (non-destructive action)

**Given** a deactivated user
**When** their messages are viewed by other users
**Then** all of the deactivated user's messages remain visible and intact — deactivation never deletes data

## Story 8.4: Default Group Creation

As an administrator,
I want to create default group conversations during initial setup,
So that new team members see active conversations when they first join.

**Acceptance Criteria:**

**Given** an admin user
**When** they create a group conversation from the admin panel or via the normal group creation flow
**Then** the group is created as a standard group conversation
**And** the group is visible in Browse Groups for all users to join (FR38)

**Given** a new user registers
**When** they land on the conversation list
**Then** they can see available groups via Browse Groups
**And** default groups (e.g., General, Engineering) are discoverable immediately
**And** no empty screen experience — the warm empty state with "Browse Groups" button guides them (UX-DR22)
