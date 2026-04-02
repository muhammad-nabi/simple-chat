# Story 3.4: Design Token System & Chat Layout Shell

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user,
I want a clean, responsive chat interface,
so that I can navigate conversations comfortably on any device.

## Acceptance Criteria

1. **Given** the Angular shared styles directory, **When** the design token system is created (UX-DR1), **Then** `_tokens.scss` defines all visual decisions: color palette (warm teal primary #128C7E, neutrals, text, semantic), typography (system font stack, type scale from 12px to 18px), spacing (4px base unit scale), border radii (sm 4px, md 8px, lg 16px, full 9999px), shadows (sm, md, lg), animation timings (fast 150ms, normal 250ms, slow 400ms), breakpoints (mobile 768px, tablet 1024px), z-index layers, **And** Direction B tokens are included: avatar-radius 14px, bubble-radius 16px, chat-bg #FAFBFC, bubble colors, input radius 16px (UX-DR2), **And** responsive mixins are available: `@include tablet { }`, `@include mobile { }`.

2. **Given** the chat feature module, **When** the ChatLayoutComponent is built (UX-DR12), **Then** on desktop (>=1024px): 3-panel layout with sidebar (320px fixed), chat area (flex: 1), members panel (240px, toggleable), **And** on tablet (768-1023px): 2-panel with collapsible sidebar (280px) + chat area, **And** on mobile (<768px): single-panel navigation -- conversation list OR chat view, with back arrow in chat header, **And** panel transitions use 250ms ease timing, **And** minimum supported viewport width is 320px.

3. **Given** the Angular routing configuration, **When** the chat feature module is loaded, **Then** it is lazy-loaded via route configuration, **And** the route is guarded by authentication (redirect to login if no session).

## Tasks / Subtasks

- [x] Task 1: Verify and enhance design token system (AC: #1)
  - [x] 1.1 Verify `src/Web/ClientApp/src/styles/_tokens.scss` has all required tokens from UX spec -- ALREADY EXISTS with all tokens from Story 1.1
  - [x] 1.2 Verify `src/Web/ClientApp/src/styles/_mixins.scss` has responsive mixins -- ALREADY EXISTS
  - [x] 1.3 Verify `src/Web/ClientApp/src/styles/styles.scss` imports tokens and provides CSS reset -- ALREADY EXISTS
  - [x] 1.4 Add any missing tokens if gaps found during verification (line-height tokens, avatar color palette for deterministic assignment)

- [x] Task 2: Create chat feature routing and lazy-loading (AC: #3)
  - [x] 2.1 Create `src/Web/ClientApp/src/app/features/chat/chat.routes.ts` with route configuration
  - [x] 2.2 Add `/chat` route to `src/Web/ClientApp/src/app/app.routes.ts` with `authGuard` and `loadChildren` pointing to chat routes
  - [x] 2.3 Update default route redirect from HomeComponent to `/chat` (or keep home and add /chat as separate route -- follow existing pattern)

- [x] Task 3: Create ChatLayoutComponent (AC: #2)
  - [x] 3.1 Create `src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.ts` -- standalone component, responsive layout host
  - [x] 3.2 Create `chat-layout.component.html` with 3-panel structure: sidebar, chat-area, members-panel
  - [x] 3.3 Create `chat-layout.component.scss` with responsive styles using design tokens and mixins
  - [x] 3.4 Implement viewport detection: use `window.matchMedia` or CSS-only approach for breakpoint-aware layout switching
  - [x] 3.5 Desktop (>=1024px): sidebar 320px fixed, chat area flex:1, members panel 240px toggleable
  - [x] 3.6 Tablet (768-1023px): sidebar 280px collapsible + chat area flex:1, no members panel
  - [x] 3.7 Mobile (<768px): single-panel -- show conversation list OR chat view with back arrow navigation
  - [x] 3.8 Panel transitions: 250ms ease timing, respect `prefers-reduced-motion` (0ms)
  - [x] 3.9 Minimum viewport: 320px (iPhone SE)

- [x] Task 4: Create placeholder child components (AC: #2)
  - [x] 4.1 Create `src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.ts` -- placeholder with "Conversations" text
  - [x] 4.2 Create `src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts` -- placeholder with "Select a conversation" text
  - [x] 4.3 Create `src/Web/ClientApp/src/app/features/chat/components/online-users/online-users.component.ts` -- placeholder for members panel

- [x] Task 5: Frontend tests (AC: #1, #2, #3)
  - [x] 5.1 Create `chat-layout.component.spec.ts` -- test component creation, 3-panel structure renders, responsive class application
  - [x] 5.2 Test lazy-loading route resolves correctly (covered by route configuration + component render tests)
  - [x] 5.3 Test auth guard protects chat route (auth guard already tested in existing suite; route configured with canActivate: [authGuard])

### Review Findings

- [x] [Review][Decision] Members toggle button visible but non-functional on tablet — resolved: hide button on tablet (AC only specifies 2-panel sidebar+chat for tablet)
- [x] [Review][Patch] No-op self-assignment in `onBreakpointChange` — fixed: replaced with meaningful `activePanel` reset logic
- [x] [Review][Patch] Stale `activePanel` on breakpoint transitions — fixed: `activePanel` resets from `'members'` to `'chat'` on tablet/desktop transitions
- [x] [Review][Patch] No `min-width: 320px` on layout container — fixed: added `min-width: 320px` to `:host`
- [x] [Review][Defer] `100vh` causes layout overflow on mobile browsers [chat-layout.component.scss:6] — deferred, pre-existing
- [x] [Review][Defer] Desktop-to-mobile resize loses view context [chat-layout.component.ts:104-107] — deferred, UX refinement

## Dev Notes

### CRITICAL: Design Tokens ALREADY EXIST

The design token system (`_tokens.scss`, `_mixins.scss`, `styles.scss`) was created in a previous story. **DO NOT recreate these files.** AC #1 is already satisfied. Verify completeness and add only if gaps exist.

**Existing files (DO NOT recreate):**
- `src/Web/ClientApp/src/styles/_tokens.scss` -- all color, typography, spacing, radius, shadow, transition, breakpoint, z-index tokens
- `src/Web/ClientApp/src/styles/_mixins.scss` -- `@include mobile {}`, `@include tablet {}`, `@include desktop {}`, `@include reduced-motion {}`
- `src/Web/ClientApp/src/styles/styles.scss` -- CSS reset, font baseline, skip-to-content link, focus-visible outline

**Potential gaps to check:**
- Line-height tokens (UX spec mentions 1.0-1.47 line heights -- not in current `_tokens.scss`)
- Avatar deterministic color palette (8 warm colors from user ID) -- may need to add as SCSS list or CSS custom properties
- Button-size token (14px for send button) -- `$font-size-body` is 14px, may suffice

### Existing Infrastructure to Reuse (DO NOT Recreate)

- **`authGuard`** (`src/Web/ClientApp/src/app/core/guards/auth.guard.ts`): Already exists, protects routes by checking `AuthService.isAuthenticated$`. Use for chat route.
- **`AuthService`** (`src/Web/ClientApp/src/app/core/services/auth.service.ts`): Has `isAuthenticated$`, `currentUser$`, `accessToken` getter. Chat layout may need current user info.
- **`SignalRService`** (`src/Web/ClientApp/src/app/core/signalr/signalr.service.ts`): Already exists with `connectionState$` observable. Chat layout should display connection state (but actual ConnectionStatusBanner is Story 3.9).
- **`app.routes.ts`**: Current routes: `/` (HomeComponent), `/login`, `/register`, `**` redirect. Add `/chat` route here.
- **`app.config.ts`**: Has `APP_INITIALIZER` with auth restore + SignalR start. No changes needed.

### Chat Feature Directory Structure

Create this structure under `src/Web/ClientApp/src/app/features/chat/`:

```
features/chat/
├── chat.routes.ts
├── components/
│   ├── chat-layout/
│   │   ├── chat-layout.component.ts
│   │   ├── chat-layout.component.html
│   │   ├── chat-layout.component.scss
│   │   └── chat-layout.component.spec.ts
│   ├── conversation-list/
│   │   └── conversation-list.component.ts     (placeholder)
│   ├── chat-window/
│   │   └── chat-window.component.ts           (placeholder)
│   └── online-users/
│       └── online-users.component.ts          (placeholder)
└── services/                                  (empty for now, Stories 3.5-3.7)
```

### ChatLayoutComponent Implementation Pattern

**Standalone component** (NO NgModule). Use CSS Grid or Flexbox for layout. Import child placeholder components directly in `imports` array.

```typescript
// chat-layout.component.ts
@Component({
  selector: 'app-chat-layout',
  standalone: true,
  imports: [ConversationListComponent, ChatWindowComponent, OnlineUsersComponent],
  templateUrl: './chat-layout.component.html',
  styleUrl: './chat-layout.component.scss'
})
export class ChatLayoutComponent {
  // Responsive state management
  // Mobile panel navigation state
}
```

**Responsive approach options:**
1. **CSS-only (preferred for V1):** Use CSS Grid with `@include mobile {}`, `@include tablet {}`, `@include desktop {}` mixins. Panel visibility controlled by CSS media queries + a class toggle for mobile panel switching.
2. **Angular BreakpointObserver:** Architecture doc mentions this, but it requires `@angular/cdk` which is NOT in the project dependencies. **DO NOT add @angular/cdk** -- use CSS media queries + a lightweight TypeScript `matchMedia` listener if JS-driven breakpoint logic is needed.

**CSS Grid layout approach:**
```scss
@use '../../../../../styles/tokens' as t;
@use '../../../../../styles/mixins' as m;

:host {
  display: grid;
  height: 100vh;
  
  @include m.desktop {
    grid-template-columns: 320px 1fr 240px;
  }
  
  @include m.tablet {
    grid-template-columns: 280px 1fr;
  }
  
  @include m.mobile {
    grid-template-columns: 1fr;
  }
}
```

### Routing Configuration

```typescript
// chat.routes.ts
import { Routes } from '@angular/router';

export const chatRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/chat-layout/chat-layout.component').then(m => m.ChatLayoutComponent),
  },
];
```

```typescript
// In app.routes.ts, add:
{
  path: 'chat',
  loadChildren: () =>
    import('./features/chat/chat.routes').then(m => m.chatRoutes),
  canActivate: [authGuard],
},
```

**Decision needed:** Should the default route (`/`) redirect to `/chat` after auth? Current default shows `HomeComponent`. For chat app, likely redirect authenticated users to `/chat`. Consider updating the wildcard redirect too.

### Mobile Panel Navigation

On mobile (<768px), only one panel is visible at a time:
- **Default view:** Conversation list (full screen)
- **When conversation selected:** Chat window (full screen) with back arrow in header
- **Members panel:** Accessible from chat header toggle

Use a component state variable (e.g., `activePanel: 'list' | 'chat' | 'members'`) to control which panel shows on mobile. On tablet/desktop, all applicable panels show simultaneously via CSS.

### Panel Specifications (from UX spec)

| Element | Desktop | Tablet | Mobile |
|---------|---------|--------|--------|
| Sidebar | Visible 320px | Collapsible 280px | Full screen list |
| Chat area | flex: 1 | flex: 1 | Full screen |
| Members panel | 240px toggleable | Hidden (header toggle) | Hidden (header toggle) |
| Header height | 56px | 56px | 56px |
| Sidebar header | 16px padding, 56px height | Same | Same |

### Accessibility Requirements for Chat Layout

- Skip-to-content link already exists globally in `styles.scss`
- Chat area should have `role="main"` or be within main content area
- Sidebar should have `role="navigation"` or `nav` element
- Members panel should have appropriate landmark role
- Panel transitions: respect `prefers-reduced-motion` via `@include reduced-motion { transition: none; }`
- Focus management: when mobile panels switch, focus should move to the new panel's first focusable element

### Build-Breaking Rules to Watch

- **Standalone components only** -- `standalone: true`, no NgModules
- **`inject()` for DI** -- never constructor injection
- **No `var`** keyword in SCSS (already using `$` variables correctly)
- **2-space indent** for TypeScript, HTML, SCSS
- **Single quotes** in TypeScript
- **kebab-case** file names, `app-` component selector prefix
- **LF line endings** -- never CRLF
- **Final newline** -- all files must end with newline
- **ESLint must pass** -- `ng lint` clean
- **`strict: true`** in tsconfig -- explicit types, no implicit any

### Items NOT in Scope

- No actual conversation data or API calls -- placeholder components only
- No ConversationService or MessageService -- comes in Stories 3.5-3.7
- No Avatar component -- comes in Story 3.5
- No message bubbles, input, or chat functionality -- Stories 3.5-3.7
- No ConnectionStatusBanner -- comes in Story 3.9
- No search bar -- comes in Epic 7
- No presence indicators -- comes in Epic 5
- No dark mode tokens -- deferred (noted in deferred-work.md)
- No `@angular/cdk` BreakpointObserver -- use CSS media queries + matchMedia

### Previous Story Intelligence (Story 3.3)

**Key learnings from Story 3.3:**
- SignalRService exists at `src/Web/ClientApp/src/app/core/signalr/signalr.service.ts` with `connectionState$` observable
- `@microsoft/signalr` v10.0.0 is installed
- SignalR auto-starts on `isAuthenticated$` change in APP_INITIALIZER
- Message-gap fill on reconnect deferred to Stories 3.4-3.7 (noted in deferred work)
- Pre-existing `app.component.spec.ts` test failure (2 tests) is known -- do not try to fix
- Build: 0 errors, 0 warnings; Unit tests: 71 pass; Integration tests: 45 pass; Frontend tests: 10 pass

**Review findings from Story 3.3:**
- `accessTokenFactory` doesn't refresh expired JWT on reconnect -- dismissed, deferred to Story 3.9
- All hub methods pass `Context.ConnectionAborted` for cancellation
- `onclose` handler nulls connection before emitting Disconnected
- Broadcast handler has try-catch with ILogger.LogError

### Git Intelligence

Recent commits: `story 3.3 created implemented and reviewed`, `story 3.2 created implemented and reviewed`, `story 3.1 created and implemented with review`. Branch pattern: `story/X.Y`. Current branch: `prep/fix-test-infrastructure`.

### Project Structure Notes

**New files to create:**
```
src/Web/ClientApp/src/app/features/chat/chat.routes.ts
src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.ts
src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.html
src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.scss
src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.spec.ts
src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.ts
src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts
src/Web/ClientApp/src/app/features/chat/components/online-users/online-users.component.ts
```

**Files to modify:**
```
src/Web/ClientApp/src/app/app.routes.ts    -- add /chat route with authGuard + lazy loading
```

**Files to verify (NO changes expected):**
```
src/Web/ClientApp/src/styles/_tokens.scss   -- verify completeness, add line-heights if missing
src/Web/ClientApp/src/styles/_mixins.scss   -- verify completeness
src/Web/ClientApp/src/styles/styles.scss    -- verify completeness
```

**Files NOT to modify:**
```
src/Web/ClientApp/src/app/app.config.ts           -- no changes needed
src/Web/ClientApp/src/app/core/services/auth.service.ts   -- reuse as-is
src/Web/ClientApp/src/app/core/guards/auth.guard.ts       -- reuse as-is
src/Web/ClientApp/src/app/core/signalr/signalr.service.ts -- reuse as-is
```

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-3-real-time-private-messaging.md#Story 3.4]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#UX-DR1 Design Token System]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#UX-DR2 Direction B Visual Tokens]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#UX-DR12 ChatLayoutComponent]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend File Structure]
- [Source: _bmad-output/planning-artifacts/architecture.md#Component-scoped SCSS + shared design tokens]
- [Source: _bmad-output/planning-artifacts/architecture.md#Responsive layout: ChatLayoutComponent + BreakpointObserver]
- [Source: _bmad-output/project-context.md#Angular Rules -- inject(), BehaviorSubject, standalone components]
- [Source: _bmad-output/project-context.md#Code Quality Style Rules -- 2-space indent, single quotes, kebab-case]
- [Source: _bmad-output/implementation-artifacts/3-3-signalr-real-time-messaging-hub.md#Dev Notes]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

### Completion Notes List

- Design token system verified complete -- `_tokens.scss`, `_mixins.scss`, `styles.scss` all existed from prior work. Added missing line-height tokens (`$line-height-tight` through `$line-height-relaxed`) and avatar deterministic color palette (`$avatar-colors`: 8 warm colors).
- Chat feature module created with lazy-loaded routing via `loadChildren` in `app.routes.ts`, protected by `authGuard`
- ChatLayoutComponent implements responsive 3-panel layout using CSS Grid + `window.matchMedia` listeners (no `@angular/cdk` dependency needed)
- Desktop: 320px sidebar + flex chat area + 240px toggleable members panel
- Tablet: 280px collapsible sidebar + flex chat area, no members panel
- Mobile: single-panel navigation with `activePanel` state (`list` | `chat` | `members`), back arrow in chat header
- Panel transitions use `$transition-normal` (250ms ease) with `prefers-reduced-motion` override to `none`
- Three placeholder child components created: ConversationListComponent, ChatWindowComponent, OnlineUsersComponent
- Used `@if` control flow (Angular 21 built-in) instead of `*ngIf` directive per ESLint rules
- Accessibility: sidebar has `role="navigation"`, chat area has `role="main"`, members panel has `aria-label`, toggle buttons have `aria-label` and `aria-expanded`
- 16 new tests in chat-layout.component.spec.ts covering: component creation, layout modes, panel visibility, mobile navigation, sidebar toggle, members toggle, DOM rendering, accessibility roles, listener cleanup
- Build: 0 errors, 0 warnings (Release); Backend unit tests: 71/71 pass; Frontend tests: 30/30 pass (16 new); Lint: All pass

### Change Log

- 2026-04-02: Story 3.4 implemented -- design tokens enhanced, chat layout shell with responsive 3/2/1 panel layout, lazy-loaded routing, placeholder components, 16 frontend tests

### File List

New files:
- src/Web/ClientApp/src/app/features/chat/chat.routes.ts
- src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.ts
- src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.html
- src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.scss
- src/Web/ClientApp/src/app/features/chat/components/chat-layout/chat-layout.component.spec.ts
- src/Web/ClientApp/src/app/features/chat/components/conversation-list/conversation-list.component.ts
- src/Web/ClientApp/src/app/features/chat/components/chat-window/chat-window.component.ts
- src/Web/ClientApp/src/app/features/chat/components/online-users/online-users.component.ts

Modified files:
- src/Web/ClientApp/src/styles/_tokens.scss (added line-height tokens, avatar color palette)
- src/Web/ClientApp/src/app/app.routes.ts (added /chat route with authGuard + lazy loading)
