---
unit: 003-implementation-briefs
intent: 034-eu-expansion-architecture-study
phase: inception
status: ready
unit_type: docs
default_bolt_type: simple-construction-bolt
created: 2026-06-05T12:57:50Z
updated: 2026-06-05T12:57:50Z
---

# Unit Brief: Implementation Briefs

## Purpose

Close the research→implementation loop. Translate the ADR (D3) into concrete readiness
requirements — authored as the inception feed for the real implementation intent(s), in the
same style as the source research brief. Seam preparation only; no translations.

## Scope

### In Scope
- One or more implementation briefs (D4) at `docs/planning/i18n-readiness-brief-<date>.md`.
- Concrete readiness requirements derived from the ADR (which seams to prepare, in what
  order, with what acceptance criteria) — explicitly enough to hand to the inception agent.
- Splitting into multiple briefs if the decision splits the work (e.g. infra-readiness vs
  i18n-seam-readiness vs multi-currency-readiness).

### Out of Scope
- The translations themselves (Phase 5 prepares architecture only).
- Any production code or deployment.
- Re-deciding anything — the brief executes the ADR, it does not revisit it.

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-11 | Implementation brief(s) (D4) — inception feed, seam prep only | Must |

---

## Story Summary

- **Total Stories**: 1
- **Must Have**: 1

### Stories

- [ ] **001-author-implementation-briefs**: Author D4 brief(s) from the ADR — Must — Planned

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 002-synthesis-and-decision | Briefs are authored from the ADR |

### Depended By
| Unit | Reason |
|------|--------|
| (future implementation intent, via inception) | D4 is its inception feed |

---

## Technical Context

### Integration Points
Writes to `docs/planning/`. Mirrors the structure of
`docs/planning/eu-expansion-research-brief-2026-06-05.md` (the feed for THIS intent).

---

## Constraints

- Docs only — no code. Bolt type is `simple-construction-bolt` per the brief, but its
  output is documentation, not software.
- Each brief must be complete enough to hand to the inception agent with no extra context.

---

## Success Criteria

### Functional
- [ ] At least one implementation brief exists at `docs/planning/i18n-readiness-brief-<date>.md`.
- [ ] The brief(s) translate the ADR into concrete, ordered readiness requirements.
- [ ] Seam prep only — no translation work specified.

### Quality
- [ ] An inception agent can create the implementation intent(s) from D4 alone.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 084-implementation-briefs | simple-construction-bolt | 001-author-implementation-briefs | Author D4 implementation brief(s) from the ADR |

---

## Notes

This unit completes the loop described in the source brief: its output (D4) is fed back into
the inception agent to create the implementation intent(s). Keep the readiness scope strictly
seam-preparation (Phase 5), and explicitly state that deployment remains Phase 6.
