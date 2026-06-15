---
id: 002-owner-decision-adr
unit: 002-synthesis-and-decision
intent: 034-eu-expansion-architecture-study
status: ready
priority: must
created: 2026-06-05T12:57:50Z
assigned_bolt: 083-synthesis-and-decision
implemented: false
---

# Story: 002-owner-decision-adr

## User Story

**As the** owner deciding EU expansion
**I want** my chosen architecture recorded as an ADR with the rejected options and reasons
**So that** the decision is durable, traceable, and ready to drive implementation

## Acceptance Criteria

- [ ] **Given** the options paper (D2), **When** the ⛔ human checkpoint runs, **Then** the owner reviews it, may ask follow-up questions, and makes an explicit decision — the agent does **not** auto-decide
- [ ] **Given** the owner's decision, **When** the ADR is written, **Then** it records the **chosen bundle** AND the **rejected options with reasons**
- [ ] **Given** the deliverable, **When** complete, **Then** the ADR is appended to `memory-bank/standards/decision-index.md`, dated 2026, and cross-references the options paper
- [ ] **Given** no explicit owner decision yet, **When** checked, **Then** no ADR exists (the ADR exists only after the decision)

## Technical Notes

- This is the spike-bolt's **document** stage: finalize D2 → ⛔ owner decision → record ADR (D3).
- The ⛔ checkpoint is a hard stop; budget for a round of owner Q&A before the decision.
- Follow the existing ADR format/conventions already used in `decision-index.md`.

## Dependencies

### Requires
- 001-synthesis-options-paper (the finalized D2)

### Enables
- 001-author-implementation-briefs (Unit 3) — the ADR is its input

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Owner picks a hybrid not exactly matching a bundle | Record the actual decision precisely; note deltas from the nearest bundle |
| Owner defers the decision | No ADR; capture open questions; do not proceed to Unit 3 |

## Out of Scope

- Authoring implementation briefs (Unit 3); any implementation.
