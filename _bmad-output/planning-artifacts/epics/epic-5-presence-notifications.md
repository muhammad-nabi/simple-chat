# Epic 5: Presence & Notifications

Users can see who's online/away/offline and receive unread message counts and visual indicators for new messages across conversations.

## Story 5.1: Presence Backend (Redis Heartbeat & TTL)

As a user,
I want the system to track who is online,
So that I can see which team members are available for conversation.

**Acceptance Criteria:**

**Given** an authenticated user with an active session
**When** the Angular app is in the foreground
**Then** it sends POST `/api/presence/heartbeat` every 60 seconds with the user's current status (Online or Away)
**And** the backend sets a Redis key `presence:{userId}` with a 90-second TTL
**And** the backend adds the userId to a Redis Set `online_users`

**Given** a user closes the browser or loses connectivity
**When** 90 seconds pass without a heartbeat
**Then** the Redis key expires automatically via TTL
**And** the user is lazily pruned from the `online_users` Set on the next read (GET `/api/presence/online` cross-references each Set member's TTL key; if expired, the member is removed from the Set before returning results)

**Given** the Angular app detects 5 minutes of client-side inactivity
**When** the next heartbeat fires
**Then** the heartbeat payload changes status to Away
**And** the user's presence state reflects Away (amber dot) for other users
**And** no notification or toast is shown to the user (silent, per "quiet confidence" principle — UX-DR4)

**Given** any authenticated user
**When** GET `/api/presence/online` is called
**Then** a list of currently online/away users is returned with their userId and status
**And** the Angular app polls this endpoint every 30 seconds

## Story 5.2: Presence UI & Online Users Panel

As a user,
I want to see presence indicators on avatars and a list of who's online,
So that I know which team members are available right now.

**Acceptance Criteria:**

**Given** the Presence Indicator component (UX-DR4)
**When** a user is Online
**Then** a green dot (#25D366, 10px circle) is displayed at the bottom-right of their avatar
**When** a user is Away
**Then** an amber dot (#F59E0B) is displayed
**When** a user is Offline
**Then** no dot is displayed (absence indicates offline)
**And** status is always conveyed via `aria-label` on the avatar, not color alone

**Given** a desktop viewport (≥1024px)
**When** the members panel is visible (240px right panel)
**Then** it displays all team members grouped by status: Online first, then Away, then Offline
**And** each entry shows Avatar with presence dot and display name

**Given** a tablet or mobile viewport
**When** the user taps the members toggle in the chat header
**Then** the members panel is shown (overlay or panel switch depending on breakpoint)

**Given** no users are currently online (edge case)
**When** the members panel renders
**Then** a friendly empty state is shown: "No one is online right now." (UX-DR22)

## Story 5.3: Unread Tracking & Notification Indicators

As a user,
I want to see unread message counts per conversation and visual indicators for new messages,
So that I know which conversations need my attention.

**Acceptance Criteria:**

**Given** a user opens a conversation
**When** the conversation renders
**Then** the user's `LastReadMessageId` on ConversationParticipant is updated to the most recent message Id (MarkAsRead command)
**And** the unread badge for that conversation clears in the sidebar

**Given** a new message arrives in a conversation the user is NOT currently viewing
**When** the SignalR event is received
**Then** the unread count for that conversation increments in the sidebar
**And** the Unread Badge component (UX-DR5) displays the count (circular 22px, teal background, white text)
**And** counts exceeding 99 display as "99+"
**And** the badge has `aria-label` with full text (e.g., "12 unread messages")

**Given** the conversation list
**When** a conversation has unread messages
**Then** the conversation name is displayed in bold (font-weight 600) (FR26)
**And** the conversation is sorted by LastMessageAt (most recent at top)

**Given** a user opens simple-chat after being away (Monday morning scenario)
**When** the conversation list loads
**Then** unread counts are accurate and render immediately (efficient query: count messages where Id > LastReadMessageId per conversation)
**And** the sidebar renders within the target of <1 second

**Given** a conversation with unread messages is opened
**When** the chat window renders
**Then** the view scrolls to the first unread message as the scroll target
