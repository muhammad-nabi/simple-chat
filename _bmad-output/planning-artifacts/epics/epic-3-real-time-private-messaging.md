# Epic 3: Real-Time Private Messaging

Users can have 1-on-1 real-time text conversations with full message history, with visual connection state feedback and automatic reconnection.

## Story 3.1: Conversation & Message Data Model

As a developer,
I want the core messaging data model in place,
So that conversations and messages can be persisted and queried efficiently.

**Acceptance Criteria:**

**Given** the messaging module in the Domain layer
**When** the entities are defined
**Then** a Conversation entity exists with: Id (long), Type (ConversationType enum: Private, Group), Name (nullable — null for Private), CreatedAt, CreatedById, LastMessageAt
**And** a ConversationParticipant entity exists with: ConversationId, UserId, JoinedAt, LastReadMessageId (nullable long)
**And** a Message entity exists with: Id (bigint identity), ConversationId, SenderId, Content (string, max 4000), SentAt, MessageType (enum: Text, File, System), FileId (nullable), EditedAt (nullable), DeletedAt (nullable)
**And** EditedAt and DeletedAt are defined but unused in V1 (future-proofing at zero cost)

**Given** the EF Core entity configurations
**When** a migration is generated and applied
**Then** the Messages table has a clustered primary key on Id (append-only write pattern)
**And** a composite index exists on (ConversationId, Id DESC) for cursor-based history pagination
**And** a unique constraint exists on ConversationParticipant (ConversationId, UserId)
**And** foreign keys are configured for SenderId → Users.Id and ConversationId → Conversations.Id

**Given** the data model
**When** message IDs are generated
**Then** they use bigint identity — sequential, compact, natural cursor ordering

**Given** the entity definitions
**When** integration tests run
**Then** entity persistence round-trips are verified (create, read, query) against a real MSSQL instance via Testcontainers

## Story 3.2: Send & Persist Messages (Backend)

As a user,
I want to send text messages in a private conversation and retrieve message history,
So that I can communicate with team members and review past conversations.

**Acceptance Criteria:**

**Given** an authenticated user and a valid conversation they participate in
**When** a SendMessageCommand is dispatched via MediatR
**Then** the message is persisted to the database (EF Insert, await)
**And** a MessageSent MediatR notification is published carrying the full Message entity + ConversationId
**And** the Conversation.LastMessageAt is updated
**And** the message Id (bigint) is returned

**Given** a SendMessageCommand with empty content or content exceeding 4000 characters
**When** the command is validated by FluentValidation
**Then** a 400 Problem Details response is returned with validation errors

**Given** an authenticated user and a conversation they participate in
**When** a GetMessageHistoryQuery is dispatched with conversationId, optional `before` cursor, and `limit` (default 50)
**Then** messages are returned in descending Id order using cursor-based pagination: `WHERE ConversationId = @id AND Id < @cursor ORDER BY Id DESC`
**And** the response includes the message list, `hasMore` boolean, and `nextCursor` value
**And** each message DTO includes: id, conversationId, senderId, senderDisplayName, content, sentAt, messageType

**Given** an authenticated user wants to start a private conversation
**When** a CreateConversationCommand is dispatched with the other user's ID
**Then** the system checks if a private conversation already exists between these two users
**And** if it exists, returns the existing conversation (no duplicate DMs)
**And** if it does not exist, creates a new Conversation (Type=Private, Name=null) with both users as ConversationParticipants
**And** the conversation Id is returned

**Given** the REST API endpoints
**When** requests are made
**Then** POST `/api/conversations` creates a conversation
**And** GET `/api/conversations/{id}/messages?before={cursor}&limit={n}` returns paginated history
**And** all endpoints require authentication and verify the user is a participant

**Given** a user sends a message to a conversation they are not a participant in (e.g., left a group)
**When** the command is processed
**Then** a 403 Forbidden Problem Details response is returned

## Story 3.3: SignalR Real-Time Messaging Hub

As a user,
I want to receive messages in real time without refreshing the page,
So that conversations feel instant and natural.

**Acceptance Criteria:**

**Given** an authenticated user with a valid JWT
**When** the Angular app establishes a SignalR connection to `/hubs/chat`
**Then** the connection is authenticated via JWT passed as a query string parameter
**And** the user is added to SignalR groups for each conversation they participate in

**Given** a user sends a message via the ChatHub `SendMessage` method
**When** the hub receives the message
**Then** the hub dispatches a SendMessageCommand via MediatR (persist-first flow)
**And** after successful persistence, the MessageSent notification triggers a SignalR broadcast
**And** all participants in the conversation receive a `ReceiveMessage` event with the full message DTO
**And** the message is guaranteed durable in the database before any client sees it

**Given** the SignalR connection drops (network interruption)
**When** the client detects disconnection
**Then** the SignalR client automatically attempts reconnection with exponential backoff: [0, 2000, 5000, 10000, 30000] ms
**And** on successful reconnection, the client refreshes its JWT if expired before re-establishing the hub connection
**And** the client re-joins all conversation groups (FR49)

**Given** the client successfully reconnects after a disconnection
**When** the hub connection is re-established
**Then** the client fetches the conversation list with updated unread counts
**And** for the active conversation, fetches messages newer than the last known message ID to fill any gap
**And** the UI updates seamlessly without requiring a manual refresh

**Given** a centralized SignalRService in Angular core
**When** server events arrive (ReceiveMessage, UserOnline, UserOffline)
**Then** the service dispatches events to the appropriate domain services (MessageService, PresenceService)
**And** a `connectionState$` observable exposes the current state (Connected, Reconnecting, Disconnected)

## Story 3.4: Design Token System & Chat Layout Shell

As a user,
I want a clean, responsive chat interface,
So that I can navigate conversations comfortably on any device.

**Acceptance Criteria:**

**Given** the Angular shared styles directory
**When** the design token system is created (UX-DR1)
**Then** `_tokens.scss` defines all visual decisions: color palette (warm teal primary #128C7E, neutrals, text, semantic), typography (system font stack, type scale from 12px to 18px), spacing (4px base unit scale), border radii (sm 4px, md 8px, lg 16px, full 9999px), shadows (sm, md, lg), animation timings (fast 150ms, normal 250ms, slow 400ms), breakpoints (mobile 768px, tablet 1024px), z-index layers
**And** Direction B tokens are included: avatar-radius 14px, bubble-radius 16px, chat-bg #FAFBFC, bubble colors, input radius 16px (UX-DR2)
**And** responsive mixins are available: `@include tablet { }`, `@include mobile { }`

**Given** the chat feature module
**When** the ChatLayoutComponent is built (UX-DR12)
**Then** on desktop (≥1024px): 3-panel layout with sidebar (320px fixed), chat area (flex: 1), members panel (240px, toggleable)
**And** on tablet (768–1023px): 2-panel with collapsible sidebar (280px) + chat area
**And** on mobile (<768px): single-panel navigation — conversation list OR chat view, with back arrow in chat header
**And** panel transitions use 250ms ease timing
**And** minimum supported viewport width is 320px

**Given** the Angular routing configuration
**When** the chat feature module is loaded
**Then** it is lazy-loaded via route configuration
**And** the route is guarded by authentication (redirect to login if no session)

## Story 3.5: Conversation List & Navigation

As a user,
I want to see my conversations in a sidebar list,
So that I can quickly find and switch between active conversations.

**Acceptance Criteria:**

**Given** an authenticated user with existing conversations
**When** the conversation list loads
**Then** conversations are displayed sorted by LastMessageAt descending (most recent at top)
**And** each ConversationListItem (UX-DR10) shows: Avatar (UX-DR3) with presence dot, conversation name (other user's display name for DMs), last message preview text, timestamp, unread badge if applicable
**And** the active conversation has a left 3px teal border + surface background color

**Given** the Avatar component (UX-DR3)
**When** rendered
**Then** it displays a rounded-square container (14px border-radius) at the correct size (44px sidebar, 36px header, 30px members)
**And** shows two-letter initials derived from display name (first letter of first two words; "Sarah Chen" → "SC", "Marcus" → "M")
**And** background color is deterministically generated from user ID (pick from 8 predefined warm colors)
**And** includes `aria-label` with user name and status (e.g., "Sarah Chen (online)")

**Given** the conversation list
**When** the user taps/clicks a conversation
**Then** the chat area loads that conversation's messages
**And** on mobile, the view switches from conversation list panel to chat panel
**And** the conversation name appears in the chat header

**Given** the conversation list API
**When** GET `/api/conversations` is called
**Then** it returns the user's conversations with: id, type, name, lastMessage (preview), lastMessageAt, otherParticipants (for display name/avatar), unreadCount

**Given** the conversation list on initial load
**When** data is not yet available
**Then** skeleton screen placeholders matching the list item shape are displayed (UX-DR20)
**And** skeleton shimmer is disabled when `prefers-reduced-motion` is active

## Story 3.6: Chat Window & Message Display

As a user,
I want to see messages displayed as conversation bubbles with full scrollable history,
So that I can read current and past messages naturally.

**Acceptance Criteria:**

**Given** an active conversation
**When** the ChatWindow component renders (UX-DR11)
**Then** the message list container has `role="log"` and `aria-live="polite"`
**And** new messages are announced to screen readers without interrupting current reading
**And** history loads via infinite scroll are NOT announced (only new real-time messages)

**Given** messages in the conversation
**When** the MessageBubble component renders each message (UX-DR8)
**Then** own messages display with teal (#128C7E) background, white text, right-aligned, border-radius 16px with 4px bottom-right
**And** other users' messages display with white background, dark text, left-aligned, subtle shadow (0 1px 3px rgba(0,0,0,0.08)), border-radius 16px with 4px bottom-left
**And** sender name is shown in teal above other users' messages (for future group support)
**And** timestamp is shown on each message in 12px caption style
**And** same-sender consecutive messages use 4px gap with no repeated sender name; different-sender messages use 12px gap with sender name shown

**Given** the user is viewing a conversation and is scrolled to the bottom
**When** a new message arrives via SignalR
**Then** the chat window auto-scrolls to show the new message

**Given** the user has scrolled up to read history
**When** a new message arrives via SignalR
**Then** a "New messages ↓" indicator appears at the bottom of the chat window (UX-DR29)
**And** tapping the indicator scrolls to the bottom
**And** the user is NOT force-scrolled

**Given** the user scrolls to the top of loaded messages
**When** more history exists (hasMore = true)
**Then** a loading spinner appears at the top
**And** older messages are fetched using cursor-based pagination and prepended
**And** scroll position is preserved (user doesn't jump)

**Given** the user switches away from a conversation and returns
**When** the conversation is re-rendered
**Then** the previous scroll position is restored

**Given** a conversation is loading for the first time
**When** messages are being fetched
**Then** skeleton screen placeholders matching bubble shapes are displayed (UX-DR20)

## Story 3.7: Message Input & Optimistic Send

As a user,
I want to type and send messages with instant visual feedback,
So that the conversation feels fast and responsive.

**Acceptance Criteria:**

**Given** the MessageInput component (UX-DR9)
**When** rendered
**Then** a text input with 16px border-radius, 1.5px border, and teal focus highlight is displayed
**And** a labeled "Attach" button is rendered but visually disabled (grayed out, no click handler) until file sharing is implemented in Epic 6
**And** a teal rounded-square (14px radius) Send button is visible
**And** the input has `aria-label="Type a message"` and the send button has `aria-label="Send message"`
**And** on desktop, the input is auto-focused when a conversation is opened
**And** on mobile, the input is NOT auto-focused (preserves reading space)

**Given** the user types a message and presses Enter (desktop) or taps Send (mobile)
**When** the message is submitted
**Then** the message appears instantly in the chat window as an own-message bubble (optimistic UI — UX-DR19)
**And** a single gray checkmark (✓) indicates "sending" state
**And** after server persistence confirms (typically <200ms), the checkmark changes to teal (✓) indicating "sent"
**And** the first message ever sent by the user gets a subtle micro-celebration (slightly more pronounced checkmark animation)

**Given** the user presses Shift+Enter
**When** typing in the message input
**Then** a newline is inserted (message is not sent)

**Given** the user is typing in a conversation
**When** they switch to a different conversation without sending
**Then** the draft text is saved to BehaviorSubject state and persisted to localStorage keyed by conversationId (UX-DR21)

**Given** the user switches to a conversation that has a saved draft
**When** the conversation loads
**Then** the message input is pre-populated with the draft text
**And** a subtle "Draft" indicator is shown that clears on first keystroke

**Given** a message is sent successfully
**When** the server confirms persistence
**Then** the draft for that conversation is cleared from both BehaviorSubject and localStorage

**Given** the user attempts to send while disconnected
**When** the send fails
**Then** the message stays in the input field with "Couldn't send — tap to retry" error state
**And** the user's typed text is never lost

## Story 3.8: New Private Conversation Flow

As a user,
I want to start a private conversation by picking a team member,
So that I can quickly message anyone on my team without navigating forms.

**Acceptance Criteria:**

**Given** the sidebar
**When** the user taps the "New Chat" button (with visible text label)
**Then** a team member picker overlay opens showing all registered active users
**And** each user entry shows: Avatar with presence indicator, display name, presence status text
**And** a filter-by-name text input is available at the top for instant filtering

**Given** the team member picker
**When** the user selects one person
**Then** the system checks if a private conversation already exists with that person
**And** if it exists, the existing conversation is opened
**And** if it does not exist, a new private conversation is created and opened
**And** the message input is focused (desktop) — ready to type

**Given** a new user with no conversations
**When** they land on the conversation list
**Then** a warm empty state is displayed: "Welcome! Join a conversation to get started." with a "New Chat" button (UX-DR22)

**Given** an empty conversation (newly created, no messages)
**When** the chat window renders
**Then** a warm empty state is displayed: "No messages yet — say hello!" (UX-DR22)

## Story 3.9: Connection Status & Auto-Reconnect

As a user,
I want to see my connection state and have the app reconnect automatically,
So that I'm never confused about whether my messages are being delivered.

**Acceptance Criteria:**

**Given** the ConnectionStatusBanner component (UX-DR6)
**When** the SignalR connection is healthy
**Then** the banner is hidden (not rendered)

**Given** a brief connection interruption (<3 seconds)
**When** SignalR reconnects within 3 seconds
**Then** no banner is ever shown (3-second debounce prevents flicker anxiety)

**Given** a connection interruption lasting more than 3 seconds
**When** SignalR is attempting to reconnect
**Then** an amber banner appears at the top of the chat area: "Reconnecting..."
**And** the banner has `role="status"` and `aria-live="assertive"`
**And** the banner pushes content down (not an overlay)

**Given** the connection is fully lost (all reconnection attempts exhausted)
**When** the disconnected state is reached
**Then** a red banner appears immediately (no debounce): "Connection lost. Check your internet connection."
**And** the banner is displayed with `role="status"` and `aria-live="assertive"`

**Given** the SignalR reconnection sequence
**When** retries are attempted
**Then** exponential backoff intervals are used: [0, 2000, 5000, 10000, 30000] ms (NFR25)
**And** the connectionState$ observable updates to reflect: Connected, Reconnecting, or Disconnected
**And** on successful reconnection, the client refreshes conversation state from the server
