# Epic 4: Group Conversations

Users can create named group conversations, invite others, join/leave groups, browse available groups, and chat in groups with full history.

## Story 4.1: Group Conversation Creation

As a user,
I want to create a group conversation by selecting multiple team members and giving it a name,
So that I can communicate with a subset of my team in a shared space.

**Acceptance Criteria:**

**Given** the team member picker overlay (built in Story 3.8)
**When** the user selects two or more people
**Then** a group name text input prompt appears (single field — no description, no settings, no avatar)
**And** the user types a name and presses Enter or taps "Create"
**And** a new Conversation (Type=Group, Name=provided name) is created with all selected users + the creator as ConversationParticipants
**And** a system message is added: "{creator} created the group"
**And** the group conversation opens with the message input focused

**Given** the conversation creation flow (UX-DR23)
**When** the user selects exactly one person
**Then** the system creates/opens a private DM (Epic 3 behavior)
**When** the user selects two or more people
**Then** the system prompts for a group name (group creation flow)
**And** the system infers conversation type from selection count — no type dropdown, no form

**Given** a group conversation
**When** rendered in the conversation list
**Then** the conversation name is the group name (not participant names)
**And** the avatar shows the first two letters of the group name

## Story 4.2: Group Messaging & History

As a user,
I want to send and read messages in a group conversation,
So that I can participate in team discussions with full context.

**Acceptance Criteria:**

**Given** an authenticated user who is a participant in a group conversation
**When** they send a message in the group
**Then** the message is persisted and broadcast to all group participants via SignalR
**And** all participants see the message in real time
**And** the message bubble displays the sender's name in teal above the message text (UX-DR8 group variant)
**And** same-sender consecutive messages are grouped (4px gap, name shown only on first)

**Given** a group conversation with message history
**When** the user scrolls up
**Then** older messages load via cursor-based pagination (same behavior as private messaging)
**And** each message clearly shows which participant sent it

**Given** a system event in the group (user joined, user left, group created)
**When** the event is rendered
**Then** a system message appears as a gray pill, centered in the chat stream (UX-DR8 system message variant)

## Story 4.3: Browse & Join Groups

As a user,
I want to browse available group conversations and join ones that interest me,
So that I can participate in team discussions relevant to my work.

**Acceptance Criteria:**

**Given** the sidebar
**When** the user taps "Browse Groups" (visible text label entry point)
**Then** a list of all group conversations is displayed (excluding those the user already belongs to)
**And** each entry shows: group name, participant count, last message preview, last activity timestamp

**Given** the browse groups list
**When** the user taps "Join" on a group
**Then** the user is added as a ConversationParticipant
**And** a system message is added: "{user} joined the group"
**And** the group appears in the user's conversation list
**And** the user can see the full message history from before they joined (FR13, FR16)

**Given** the browse groups list
**When** no groups are available to join
**Then** a friendly empty state is shown: "No groups to join right now. Create one!"

## Story 4.4: Invite Users & Leave Group

As a user,
I want to invite others to my group and leave groups I no longer need,
So that group membership stays relevant and manageable.

**Acceptance Criteria:**

**Given** a user is a participant in a group conversation
**When** they open the group's members panel and tap "Invite"
**Then** a user picker overlay opens showing active users not already in the group
**And** selecting one or more users adds them as ConversationParticipants
**And** a system message is added for each: "{inviter} added {user}"
**And** invited users see the group in their conversation list with full history access (FR12)

**Given** a user is a participant in a group conversation
**When** they tap "Leave Group"
**Then** the user is removed from ConversationParticipants
**And** a system message is added: "{user} left the group"
**And** the group is removed from the user's conversation list
**And** the action uses the label "Leave group" — never "Delete" (prevents confusion about data loss) (FR14)

**Given** the last participant leaves a group
**When** the leave action completes
**Then** the group conversation is preserved in the database (not deleted) — messages are never lost
**And** the group remains visible in Browse Groups and can be re-joined
