# Epic 9: PWA, Accessibility & Cross-Cutting Polish

Users can install the app as a PWA, and all features meet responsive design and accessibility compliance standards through comprehensive audit and refinement.

## Story 9.1: PWA Manifest & Service Worker

As a user,
I want to install simple-chat as a PWA from my browser,
So that I get app-like fast access without downloading from an app store.

**Acceptance Criteria:**

**Given** a user visits simple-chat in a supported browser
**When** the PWA install criteria are met (manifest + service worker)
**Then** the browser's install prompt is available (e.g., "Add to Home Screen")
**And** the app manifest specifies: app name, short name, icons (multiple sizes), start URL, display mode (standalone), theme color (#128C7E), background color (#FFFFFF)

**Given** the service worker is registered
**When** the user revisits the app
**Then** static assets (Angular bundle, styles, fonts) are served from cache
**And** cached page loads complete within 2 seconds (NFR3)
**And** the service worker does NOT cache API responses or SignalR connections

**Given** the user has no network connectivity
**When** the PWA loads
**Then** a clear offline indicator is displayed
**And** the app does not attempt to queue messages locally (online-first design)

## Story 9.2: Comprehensive Keyboard Navigation Audit

As a keyboard-only user,
I want to navigate all core workflows without a mouse,
So that the application is fully accessible to me.

**Acceptance Criteria:**

**Given** all implemented features
**When** audited for keyboard accessibility (UX-DR25)
**Then** Ctrl/Cmd+K opens search from any context
**And** Enter sends a message in chat view
**And** Shift+Enter inserts a newline
**And** Escape closes any overlay, search panel, or modal
**And** Arrow keys (↑/↓) navigate the conversation list and search results
**And** Enter opens the selected conversation or search result
**And** Tab cycles through interactive elements in logical order
**And** focus is trapped within modals (Tab does not escape the modal)

**Given** focus management across views
**When** the user opens a conversation
**Then** focus moves to the message input (desktop) or chat header (mobile)
**When** the user opens search
**Then** focus moves to the search input
**When** a modal closes
**Then** focus returns to the element that triggered the modal

**Given** the application markup
**When** audited
**Then** a skip-to-content link is present as the first focusable element on page load
**And** all focus indicators are visible: 2px solid primary (#128C7E) outline, 2px offset

## Story 9.3: WCAG 2.1 Compliance Audit & Remediation

As a user with accessibility needs,
I want the application to meet WCAG 2.1 Level A with select AA enhancements,
So that I can use simple-chat regardless of how I interact with technology.

**Acceptance Criteria:**

**Given** all implemented components
**When** audited for color contrast (UX-DR26)
**Then** all text/background combinations meet 4.5:1 minimum (NFR21)
**And** primary text (#111B21) on white exceeds 16:1
**And** secondary text (#667781) on white meets 4.7:1
**And** primary teal (#128C7E) on white meets 4.6:1 (if below threshold, fallback #0E7A6E at ~5.2:1 is applied)
**And** color is never the sole indicator of state (unread = bold + badge, online = dot + aria-label, error = red + icon + text)

**Given** all implemented components
**When** audited for ARIA compliance
**Then** chat message lists have `role="log"` with `aria-live="polite"`
**And** conversation list has `role="listbox"` with `aria-selected` on active conversation
**And** unread badges have `aria-label` with full text
**And** presence status is conveyed via `aria-label` on avatars
**And** connection status banner has `role="status"` with `aria-live="assertive"`

**Given** all interactive elements
**When** audited for touch and click targets
**Then** mobile touch targets are minimum 44x44px (WCAG 2.5.5)
**And** desktop click targets are minimum 32x32px
**And** all icons have visible text labels (no icon-only buttons — Experience Principle #1)

**Given** the application with animations
**When** `prefers-reduced-motion` is active
**Then** all transitions are instant (0ms duration)
**And** skeleton screen shimmer animations are replaced with static gray placeholders
**And** no auto-playing or looping animations exist

## Story 9.4: Responsive Design Polish & Breakpoint Verification

As a user on any device,
I want the interface to work correctly across desktop, tablet, and mobile,
So that I have a quality experience regardless of screen size.

**Acceptance Criteria:**

**Given** all implemented features (UX-DR24)
**When** tested on desktop (≥1024px)
**Then** the 3-panel layout displays correctly: sidebar (320px), chat (flex), members (240px toggleable)
**And** message bubbles have max-width 60%
**And** keyboard shortcuts function correctly
**And** hover states are visible on interactive elements

**Given** all implemented features
**When** tested on tablet (768–1023px)
**Then** the 2-panel layout displays: collapsible sidebar (280px) + chat (flex)
**And** the members panel is accessible via toggle in chat header
**And** message bubbles have max-width 70%

**Given** all implemented features
**When** tested on mobile (<768px)
**Then** single-panel navigation works: conversation list OR chat view
**And** back arrow in chat header returns to conversation list
**And** message bubbles have max-width 85%
**And** input is NOT auto-focused on conversation open
**And** all touch targets meet 44x44px minimum
**And** search uses an icon-to-overlay pattern (not persistent in sidebar)

**Given** the minimum viewport (320px — iPhone SE)
**When** the layout renders
**Then** no horizontal scrolling occurs and all content is accessible

**Given** the button hierarchy across all views (UX-DR27)
**When** audited
**Then** maximum one primary button (teal) is visible per context
**And** all buttons have visible text labels
**And** destructive buttons (red) appear only in confirmation dialogs
