# Epic 7: Search & Discovery

Users can search across all their conversations by message content, see which conversation results belong to, and navigate directly to the original message in context.

## Story 7.1: Full-Text Search Backend

As a user,
I want to search across all my conversations by message content,
So that I can find specific information from past discussions.

**Acceptance Criteria:**

**Given** the Messages table
**When** the MSSQL Full-Text Index is configured
**Then** a full-text index exists on Message.Content
**And** the index is created via raw SQL in an EF migration (`migrationBuilder.Sql(...)`) since EF Core does not natively support full-text index creation via Fluent API

**Given** an authenticated user
**When** a SearchMessagesQuery is dispatched with a search term, optional cursor, and limit
**Then** the IMessageSearchService implementation queries the MSSQL Full-Text Index
**And** results are filtered to only conversations the user participates in
**And** results include: messageId, conversationId, conversationName, senderDisplayName, content snippet with match context, sentAt
**And** results are returned with pagination (hasMore, nextCursor)
**And** results return within 1 second for histories up to 1 million messages (NFR2)

**Given** the search API endpoint
**When** GET `/api/messages/search?q={term}&before={cursor}&limit={n}` is called
**Then** results are returned in relevance order
**And** the endpoint requires authentication

## Story 7.2: Search UI & Progressive Results

As a user,
I want a search bar with instant results,
So that finding past messages feels fast and effortless.

**Acceptance Criteria:**

**Given** the SearchBar component (UX-DR13)
**When** rendered in the sidebar (desktop/tablet) or as an overlay (mobile)
**Then** a bordered input with search icon and "⌘K" hint text is displayed
**And** pressing Ctrl/Cmd+K from anywhere focuses the search input
**And** the component has `role="search"`

**Given** the user types a query
**When** input is debounced (300ms)
**Then** a search request is fired
**And** results stream in progressively (UX-DR14): conversation name + sender appear first (<300ms), then context preview with highlighted match (<1s)
**And** results are announced via `aria-live="polite"`

**Given** the search results list
**When** each result is displayed
**Then** it shows: conversation name, sender display name, message snippet with the match highlighted, and timestamp

**Given** no results match the query
**When** the empty state renders
**Then** a friendly message is shown: "No messages found for '{query}'. Try different keywords." (UX-DR22)

## Story 7.3: Search Context Jump

As a user,
I want to click a search result and land in the conversation at that exact message,
So that I can read the surrounding context of the found message.

**Acceptance Criteria:**

**Given** the user taps a search result
**When** the navigation occurs
**Then** the target conversation opens in the chat area
**And** a GetMessagesAround query loads ~20 messages surrounding the matched message
**And** the view scrolls to the matched message
**And** the matched message is highlighted with a subtle accent background (rgba(18, 140, 126, 0.1))
**And** the highlight fades after 3 seconds

**Given** the user is viewing a highlighted search result in context
**When** they want to return to the search results
**Then** pressing Escape or tapping a back button returns to the search results list
**And** the search state (query, results, scroll position) is preserved

**Given** the user is viewing a highlighted search result
**When** they continue scrolling naturally in the conversation
**Then** the highlight fades and normal chat behavior resumes
**And** infinite scroll works in both directions (up for older history, down for newer)
