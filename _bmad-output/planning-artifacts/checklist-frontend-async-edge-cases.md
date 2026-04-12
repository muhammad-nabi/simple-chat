# Frontend Async Edge Case Checklist

**Purpose:** Embed in frontend-heavy story specs to catch async bugs during implementation, not review.
**Origin:** Epic 3 + Epic 4 retrospectives. Every item below caused a real bug or review patch.

---

## Subscription Lifecycle

- [ ] All `subscribe()` calls tracked in `Subscription[]` or `Subscription` and cleaned up in `ngOnDestroy()`
- [ ] No orphaned subscriptions on error paths (if error handler exists, it must also clean up)
- [ ] No subscriptions created inside other subscriptions without cleanup (flatten with RxJS operators instead)

## Observable Composition

- [ ] Never wrap `HttpClient` calls in manual `new Observable()` — use `pipe()` + `tap()` for side effects
- [ ] Use `shareReplay(1)` + `finalize()` for deduplication where needed (see AuthService refresh pattern)
- [ ] No `.subscribe()` inside `.subscribe()` — use `switchMap`, `concatMap`, or `mergeMap`

## Stale Closure Guards

- [ ] Event handlers that reference component state use current values, not stale closure captures
- [ ] BehaviorSubject `.value` used for synchronous state access inside callbacks
- [ ] Long-lived callbacks (SignalR handlers, intervals) re-read state on each invocation

## In-Flight Request Management

- [ ] Concurrent duplicate requests guarded (disable button, track in-flight state, or use `switchMap`)
- [ ] In-flight state reset on BOTH success AND error paths
- [ ] Loading indicators tied to in-flight state, not manual booleans set separately

## Fast User Action Races

- [ ] Rapid click/tap on buttons does not trigger duplicate API calls
- [ ] Navigation during pending request does not cause stale state writes
- [ ] Form submission disabled while request is pending

## SignalR Edge Cases

- [ ] Echo deduplication: messages sent by current user and echoed back via SignalR are not rendered twice
- [ ] Reconnection: `reconnected$` handler re-subscribes to any data that may have changed during disconnect
- [ ] Join/leave errors handled (at minimum logged, not silently swallowed in production-facing code)
- [ ] New conversation SignalR groups joined immediately after creation/join, not deferred to reconnect

## Error Path State Reset

- [ ] UI state (loading spinners, disabled buttons, in-flight flags) reset on error, not only on success
- [ ] Error state displayed to user when API call fails (not silent empty state)
- [ ] Retry/dismiss path available from error state

---

**How to use:** Copy relevant sections into story spec under "### Async Edge Case Checklist". Dev checks items during implementation. Reviewer verifies all applicable items are addressed.
