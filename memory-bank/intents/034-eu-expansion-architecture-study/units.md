---
intent: 034-eu-expansion-architecture-study
phase: inception
status: units-decomposed
updated: 2026-06-05T12:57:50Z
---

# EU Expansion — Architecture Study - Unit Decomposition

> **Decomposition note.** This is a research-only intent, so the standard
> `full-stack-web` backend/frontend DDD decomposition does **not** apply. Units are
> organized by the research workflow (per brief §8): research → synthesis/decision →
> implementation briefs. All bolts are spike-bolts except Unit 3 (simple, docs-only).

## Units Overview

This intent decomposes into **3 units**:

### Unit 1: 001-research-tracks

**Description**: The seven parallel research tracks (T1–T7). Each produces an independent,
sourced findings document. T1–T6 are web research (mutually independent, conflict-free —
ideal wave-parallel candidates); T7 is a repo-bound seam audit (no web). The multi-agent
method (FR-1) — parallel per-track researchers + adversarial verification of high-stakes
claims — is a cross-cutting requirement applied within every track story.

**Stories** (7):
- 001-t1-fulfillment-logistics
- 002-t2-site-url-architecture
- 003-t3-frontend-i18n
- 004-t4-backend-localization
- 005-t5-tax-invoicing-compliance
- 006-t6-payments-checkout
- 007-t7-codebase-seam-audit

**Deliverables**: 7 findings docs under `docs/analysis/eu-expansion/track-<n>-<slug>.md` (D1).

**Dependencies**: Depends on — none. Depended by — Unit 2.

**Estimated Complexity**: L (seven substantial research efforts, each a fan-out)

**Assigned Requirements**: FR-1 (cross-cutting method), FR-2…FR-8.

### Unit 2: 002-synthesis-and-decision

**Description**: Consume all track findings; compose 2–3 coherent candidate architectures
(site architecture + i18n on the fixed RO-ship / one-brand / multi-currency baseline);
produce the options paper (D2) with recommendation separated from the owner-decision list;
run the ⛔ owner decision checkpoint; record the chosen bundle + rejected options as an ADR
(D3). Maps onto a spike-bolt's Stage-2 "document" + human checkpoint.

**Stories** (2):
- 001-synthesis-options-paper
- 002-owner-decision-adr

**Deliverables**: `docs/analysis/eu-expansion-architecture-study.md` (D2); new ADR in
`memory-bank/standards/decision-index.md` (D3).

**Dependencies**: Depends on — Unit 1 (all 7 findings). Depended by — Unit 3.

**Estimated Complexity**: M

**Assigned Requirements**: FR-9, FR-10.

### Unit 3: 003-implementation-briefs

**Description**: Translate the ADR into concrete readiness requirements (seam preparation
only — no translations), authored in the same style as the source research brief, as the
inception feed for the implementation intent(s). Split into multiple briefs if the decision
splits the work. Docs only.

**Stories** (1):
- 001-author-implementation-briefs

**Deliverables**: `docs/planning/i18n-readiness-brief-<date>.md` (+ more if needed) (D4).

**Dependencies**: Depends on — Unit 2 (the ADR). Depended by — a future implementation
intent (via inception).

**Estimated Complexity**: S

**Assigned Requirements**: FR-11.

## Requirement-to-Unit Mapping

- **FR-1** (multi-agent research method) → cross-cutting in `001-research-tracks` (every story)
- **FR-2** (T1 fulfillment) → `001-research-tracks`
- **FR-3** (T2 site architecture) → `001-research-tracks`
- **FR-4** (T3 frontend i18n) → `001-research-tracks`
- **FR-5** (T4 backend localization) → `001-research-tracks`
- **FR-6** (T5 tax/compliance) → `001-research-tracks`
- **FR-7** (T6 payments) → `001-research-tracks`
- **FR-8** (T7 seam audit) → `001-research-tracks`
- **FR-9** (synthesis / options paper D2) → `002-synthesis-and-decision`
- **FR-10** (owner decision / ADR D3) → `002-synthesis-and-decision`
- **FR-11** (implementation brief(s) D4) → `003-implementation-briefs`

## Unit Dependency Graph

```text
[001-research-tracks] ──► [002-synthesis-and-decision] ──► [003-implementation-briefs]
   (T1–T7, parallel)         (D2 options → ⛔ → D3 ADR)        (D4 briefs)
```

## Execution Order

Hard internal ordering (each consumes the previous):

1. **Unit 1** — research tracks T1–T7. T1–T6 run wave-parallel across 2–3 instances
   (docs-only, conflict-free); T7 runs alongside (repo-bound, read-only). Each is its own
   spike-bolt → wave-parallel candidates for the bolt-parallel-planner.
2. **Unit 2** — synthesis + options paper + ⛔ owner decision + ADR. One spike-bolt.
3. **Unit 3** — author implementation brief(s) from the ADR. One simple (docs-only) bolt.
