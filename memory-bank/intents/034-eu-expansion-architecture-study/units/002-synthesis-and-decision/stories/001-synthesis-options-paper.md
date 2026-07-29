---
id: 001-synthesis-options-paper
unit: 002-synthesis-and-decision
intent: 034-eu-expansion-architecture-study
status: ready
priority: must
created: 2026-06-05T12:57:50Z
assigned_bolt: 083-synthesis-and-decision
implemented: false
---

# Story: 001-synthesis-options-paper

## User Story

**As the** owner deciding EU expansion
**I want** the seven findings synthesized into 2–3 coherent, costed architecture options with a clear recommendation
**So that** I can make one informed decision instead of wading through seven research documents

## Acceptance Criteria

- [ ] **Given** all 7 track findings, **When** synthesis runs, **Then** it produces 2–3 **coherent bundles** — each a site-architecture + i18n approach that fit together on the fixed RO-ship / one-brand / multi-currency baseline (never a menu of independent picks)
- [ ] **Given** each bundle, **When** it is presented, **Then** it is costed (one-off effort + recurring operational cost) and states what it forecloses and what it keeps open
- [ ] **Given** the partner-fulfillment fallback, **When** the paper is composed, **Then** one bundle stress-tests that sensitivity so the path is not foreclosed
- [ ] **Given** the analysis, **When** the paper is written, **Then** an explicit **recommendation** is included, **separated from** the "owner must decide" list
- [ ] **Given** the deliverable, **When** complete, **Then** it exists at `docs/analysis/eu-expansion-architecture-study.md` and every regulatory claim it relies on traces to a verified Unit-1 source

## Technical Notes

- This is the spike-bolt's **explore** stage (synthesize → draft options paper), ending at a ⛔ checkpoint for owner review before finalizing.
- Reference intent 033's env triad when costing deployment-topology impact.
- Do NOT decide here — produce the options + recommendation; the decision is story 002.

## Dependencies

### Requires
- All 7 Unit-1 track findings (001-t1 … 007-t7)

### Enables
- 002-owner-decision-adr

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Findings conflict across tracks | Surface the conflict; do not paper over it |
| A bundle needs data no track produced | Commission a targeted follow-up or flag the gap |

## Out of Scope

- Making the decision (story 002); authoring implementation briefs (Unit 3).
