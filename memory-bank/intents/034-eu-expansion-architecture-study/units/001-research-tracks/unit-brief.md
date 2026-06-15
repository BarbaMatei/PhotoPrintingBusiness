---
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
phase: inception
status: ready
unit_type: research
default_bolt_type: spike-bolt
created: 2026-06-05T12:57:50Z
updated: 2026-06-05T12:57:50Z
---

# Unit Brief: Research Tracks (T1–T7)

## Purpose

Produce the evidence base for the EU-expansion decision: seven independent, sourced
findings documents covering fulfillment, site architecture, frontend i18n, backend
localization, tax/compliance, payments, and the codebase seam audit. This unit is the
factual foundation that Unit 2 synthesizes into options.

## Scope

### In Scope
- Seven findings docs (D1) under `docs/analysis/eu-expansion/track-<n>-<slug>.md`.
- The **multi-agent research method** (FR-1) applied within every track: parallel
  clean-context researchers + adversarial verification of high-stakes regulatory/tax claims.
- Sourced, dated claims; verification verdicts inline for high-stakes claims.

### Out of Scope
- Synthesis / composing options (Unit 2).
- Any production code or translations.
- Re-opening settled owner decisions (RO-ship, one brand, multi-currency).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Multi-agent research method (parallel per-track + adversarial verification) — cross-cutting | Must |
| FR-2 | T1 — Fulfillment & logistics (RO-ship validation, per-corridor numbers) | Must |
| FR-3 | T2 — Site & URL architecture (one brand; tied to intent 033 triad) | Must |
| FR-4 | T3 — Frontend i18n (Angular 21) | Must |
| FR-5 | T4 — Backend localization (.NET; deferred-culture trap) | Must |
| FR-6 | T5 — Tax, invoicing & compliance (OSS VAT, multi-currency, both tiers) | Must |
| FR-7 | T6 — Payments & checkout (Stripe local methods, multi-currency) | Must |
| FR-8 | T7 — Codebase seam audit (repo-bound, counts + top-10) | Must |

---

## Domain Concepts

### Key Entities
N/A — research unit. The "entities" are knowledge artifacts (findings docs), not domain objects.

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Track research | Multi-agent fan-out researching one dimension | Research questions + sources | Findings doc with cited, dated claims |
| Adversarial verify | Independent agent confirms/refutes high-stakes claims | A regulatory/tax claim | Verdict (confirmed/refuted + source) |
| Seam audit (T7) | Repo-bound scan for hardcoded RO/RON/`ro-RO`/coupling | This repository (read-only) | Counts per area + top-10 spots |

---

## Story Summary

- **Total Stories**: 7
- **Must Have**: 7
- **Should Have**: 0
- **Could Have**: 0

### Stories

- [ ] **001-t1-fulfillment-logistics**: T1 fulfillment & logistics — Must — Planned
- [ ] **002-t2-site-url-architecture**: T2 site & URL architecture — Must — Planned
- [ ] **003-t3-frontend-i18n**: T3 frontend i18n (Angular 21) — Must — Planned
- [ ] **004-t4-backend-localization**: T4 backend localization (.NET) — Must — Planned
- [ ] **005-t5-tax-invoicing-compliance**: T5 tax/invoicing/compliance — Must — Planned
- [ ] **006-t6-payments-checkout**: T6 payments & checkout — Must — Planned
- [ ] **007-t7-codebase-seam-audit**: T7 codebase seam audit (repo-bound) — Must — Planned

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| None | First unit |

### Depended By
| Unit | Reason |
|------|--------|
| 002-synthesis-and-decision | Synthesis consumes all 7 findings |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| Official web sources | VAT/OSS, carrier rates, payment coverage, i18n maturity | Medium (currency of info — reject pre-OSS sources) |
| This repository (read-only) | T7 seam audit | Low |

---

## Technical Context

### Suggested Technology
N/A — research deliverables are Markdown docs. Throwaway prototypes (e.g. a ~20-line
Angular 21 i18n build experiment) allowed per spike rules; archived/deleted, never merged.

### Integration Points
N/A (knowledge-out unit).

### Data Storage
| Data | Type | Volume | Retention |
|------|------|--------|-----------|
| Findings docs | Markdown | 7 docs | Permanent (in repo docs/) |

---

## Constraints

- Multi-agent fan-out is mandatory — not one sequential reader. The method travels with each story.
- T7 is repo-bound: no web research.
- Spike bolts time-boxed; boxes proposed at bolt-plan.
- Every regulatory/tax/legal claim: source + date + adversarial-verification verdict.

---

## Success Criteria

### Functional
- [ ] All 7 findings docs exist with sourced, dated claims.
- [ ] High-stakes claims show their adversarial-verification verdict.
- [ ] T1 has actual per-corridor cost/time numbers for both market tiers.
- [ ] T7 has file/occurrence counts per area + named top-10 spots.

### Non-Functional
- [ ] Official sources for all regulatory claims; reject pre-2021/pre-OSS material.
- [ ] Zero production-code changes.

### Quality
- [ ] Each doc readable standalone; claims traceable to sources.

---

## Bolt Suggestions

One spike-bolt **per track** to maximize wave-parallelism (T1–T6 conflict-free, T7
repo-bound — all docs-only):

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 076-research-tracks | spike-bolt | 001-t1 | T1 fulfillment findings |
| 077-research-tracks | spike-bolt | 002-t2 | T2 site-architecture findings |
| 078-research-tracks | spike-bolt | 003-t3 | T3 frontend-i18n findings |
| 079-research-tracks | spike-bolt | 004-t4 | T4 backend-localization findings |
| 080-research-tracks | spike-bolt | 005-t5 | T5 tax/compliance findings |
| 081-research-tracks | spike-bolt | 006-t6 | T6 payments findings |
| 082-research-tracks | spike-bolt | 007-t7 | T7 seam audit (repo-bound) |

---

## Notes

For the bolt-parallel-planner: bolts 076–081 are mutually independent and conflict-free
(each writes a distinct `track-<n>-*.md`); 082 (T7) is repo-bound and read-only — also
conflict-free. Ideal wave-parallel batch across 2–3 instances. Each construction instance
running a track should itself use multi-agent fan-out (parallel web researchers + verifier
agents) per the method requirement.
