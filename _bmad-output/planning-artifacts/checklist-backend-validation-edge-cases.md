# Backend Validation Edge Case Checklist

**Purpose:** Embed in backend command story specs to catch validation gaps during implementation, not review.
**Origin:** Epic 4 retrospective. Story 4.1 had 5 validation patches; this checklist covers every category.

---

## Input Sanitization

- [ ] String fields trimmed before storage (whitespace-only strings rejected or trimmed)
- [ ] Empty/null strings rejected where field is required
- [ ] Max length enforced on all string inputs (group names, messages, display names)

## Collection Inputs

- [ ] Duplicate entries in lists rejected or deduplicated (e.g., `.Distinct().Count()` validation)
- [ ] Empty collections rejected when at least one item is required
- [ ] Upper bound on collection size enforced (e.g., max 50 participants)
- [ ] Individual items in collection validated (no empty/whitespace strings, no null entries)

## Mutual Exclusion

- [ ] Mutually exclusive fields cannot both be supplied (e.g., `OtherUserId` vs `ParticipantIds`)
- [ ] Conditional validation rules documented: "When X is set, Y must/must not be set"

## ID and Reference Validation

- [ ] Entity IDs validated as positive (> 0)
- [ ] Referenced entities verified to exist before use (throw `NotFoundException`)
- [ ] User IDs verified to exist via `IIdentityService` before creating relationships
- [ ] Current user verified as authorized participant where required (throw `ForbiddenAccessException`)

## Type and State Guards

- [ ] Conversation type checked (Group vs Private) before type-specific operations
- [ ] Duplicate state transitions rejected (e.g., already a participant, already joined)
- [ ] Operation preconditions validated (e.g., cannot invite to a private conversation)

## Concurrency Considerations

- [ ] Document whether a unique constraint exists to prevent duplicate rows from concurrent requests
- [ ] If no constraint: document the race window and risk level explicitly in deferred items
- [ ] Single `SaveChangesAsync` call per handler (avoid two-phase saves — atomicity concern from Story 4.2)

## Timestamp Discipline

- [ ] Single `DateTimeOffset.UtcNow` captured at start of handler, reused for all timestamps
- [ ] Never call `UtcNow` multiple times in the same handler (inconsistent timestamps — Story 4.2 finding)

---

**How to use:** Copy relevant sections into story spec under "### Validation Edge Case Checklist". Dev checks items during implementation. Reviewer verifies all applicable items are addressed.
