---
unit: 002-synthesis-and-decision
intent: 034-eu-expansion-architecture-study
phase: inception
status: ready
unit_type: research
default_bolt_type: spike-bolt
created: 2026-06-05T12:57:50Z
updated: 2026-06-05T12:57:50Z
---

# Unit Brief: Synthesis & Decision

## Purpose

Turn seven independent findings docs into a decision. Compose 2–3 coherent candidate
architectures, cost them, present them to the owner (⛔ human checkpoint), and record the
chosen bundle + rejected options as an ADR. This unit converts evidence into an actionable,
recorded decision.

## Scope

### In Scope
- A dedicated synthesis stage consuming all 7 track findings.
- The options paper (D2): 2–3 **coherent bundles** (site architecture + i18n on the fixed
  RO-ship / one-brand / multi-currency baseline), each costed (one-off + recurring), each
  stating what it forecloses / keeps open; one bundle stress-tests the partner fallback.
- Recommendation **separated from** the explicit "owner must decide" list.
- The ⛔ owner-decision human checkpoint.
- The ADR (D3) recording the chosen bundle + rejected options with reasons.

### Out of Scope
- Gathering new primary research (that's Unit 1; this unit may commission a targeted
  follow-up only if synthesis exposes a gap).
- Authoring implementation briefs (Unit 3).
- Auto-deciding: the ADR is written only after the explicit owner decision.

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-9 | Synthesis → options paper (D2): coherent costed bundles | Must |
| FR-10 | Owner decision → ADR (D3): ⛔ human checkpoint | Must |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Synthesize bundles | Compose coherent architecture options from track findings | 7 findings docs | 2–3 costed bundles |
| Cost an option | One-off effort + recurring operational cost | A candidate bundle | Cost lines + forecloses/keeps-open |
| Owner decision | ⛔ human checkpoint; owner picks (after follow-ups) | Options paper | Chosen bundle |
| Record ADR | Capture decision + rejected options + reasons | Owner decision | ADR entry in decision-index |

---

## Story Summary

- **Total Stories**: 2
- **Must Have**: 2

### Stories

- [ ] **001-synthesis-options-paper**: Synthesize findings → options paper (D2) — Must — Planned
- [ ] **002-owner-decision-adr**: ⛔ Owner decision → ADR (D3) — Must — Planned

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001-research-tracks | Synthesis consumes all 7 findings |

### Depended By
| Unit | Reason |
|------|--------|
| 003-implementation-briefs | Briefs are authored from the ADR |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| Owner (human) | The decision itself | Decision blocks until made — by design |

---

## Technical Context

### Integration Points
Writes ADR to `memory-bank/standards/decision-index.md`; options paper to
`docs/analysis/eu-expansion-architecture-study.md`.

---

## Constraints

- Bundles must be coherent (fulfillment + site architecture + i18n that fit together),
  never a menu of independent picks. Fulfillment is fixed (RO-ship); bundles vary on the
  site-architecture + i18n axes; one bundle stress-tests the partner fallback.
- Each option costed (one-off effort + recurring).
- ADR written **only** after the explicit owner decision.
- Reference intent 033's env triad when costing deployment-topology impact.

---

## Success Criteria

### Functional
- [ ] Options paper exists with 2–3 coherent, costed bundles.
- [ ] Recommendation separated from the "owner must decide" list.
- [ ] ADR exists in decision-index.md, dated, recording chosen + rejected options.

### Non-Functional
- [ ] No regulatory claim enters D2 without a verified source from Unit 1.

### Quality
- [ ] An informed owner can decide from D2 alone (plus follow-up Q&A).

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 083-synthesis-and-decision | spike-bolt | 001-synthesis-options-paper, 002-owner-decision-adr | Stage 1 explore = synthesize bundles + draft D2; Stage 2 document = finalize D2, ⛔ owner decides, record ADR D3 |

---

## Notes

The spike-bolt shape maps exactly: explore (synthesize → draft options paper, ⛔ checkpoint
for owner review) → document (finalize D2 → ⛔ owner decision → ADR D3). The owner may ask
follow-up questions before deciding; budget for a round of Q&A at the checkpoint.
