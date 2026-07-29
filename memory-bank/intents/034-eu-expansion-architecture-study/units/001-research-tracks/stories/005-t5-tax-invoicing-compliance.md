---
id: 005-t5-tax-invoicing-compliance
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
status: ready
priority: must
created: 2026-06-05T12:57:50Z
assigned_bolt: 080-research-tracks
implemented: false
---

# Story: 005-t5-tax-invoicing-compliance

## User Story

**As the** owner deciding EU expansion
**I want** a concrete account of EU VAT/OSS, e-invoicing mandates, and multi-currency obligations for B2C selling into the target markets
**So that** I know exactly what the tax/invoicing code must change and what compliance is mandatory in 2026

## Acceptance Criteria

- [ ] **Given** B2C distance selling, **When** T5 reports, **Then** it explains EU OSS (One-Stop-Shop) VAT: registration, the EU-wide threshold, per-country VAT rate application, and reporting cadence — current to **2026** (reject pre-2021/pre-OSS sources)
- [ ] **Given** the codebase's `VatCalculator` (bolt 038) assumes Romanian VAT, **When** T5 reports, **Then** it states concretely what changes for both market tiers (HU/BG and DE/FR/IT/ES)
- [ ] **Given** ANAF e-Factura (bolt 039) is RO-only, **When** T5 reports, **Then** it states what (if anything) is mandated for **B2C** sellers in each target market in 2026
- [ ] **Given** multi-currency is decided, **When** T5 reports, **Then** it covers EUR + PLN/HUF/CZK/BGN pricing, display, and settlement implications for invoicing
- [ ] **Given** consumer law, **When** T5 reports, **Then** it notes per-country deltas affecting checkout copy (e.g. withdrawal-rights wording) without a legal rabbit hole
- [ ] **Given** every regulatory/tax claim, **When** it appears, **Then** it carries an official source (europa.eu / national tax authority), a date, and an **adversarial-verification verdict**

## Technical Notes

- **Method (FR-1)**: parallel researchers (OSS/VAT, e-invoicing per country, multi-currency, consumer law) + a **mandatory independent adversarial-verification agent** for every VAT/OSS/e-invoicing claim before it can enter the options paper. Tax rules change; a stale claim poisons the decision.
- Output: `docs/analysis/eu-expansion/track-5-tax-compliance.md`.
- Highest-stakes track for verification rigor.

## Dependencies

### Requires
- None (wave-parallel)

### Enables
- 001-synthesis-options-paper (Unit 2)
- Tightly related to T6 (payments) — settlement currency

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| A target country mandates B2C e-invoicing in 2026 | Flag prominently; size the impact vs e-Factura (bolt 039) |
| OSS threshold interaction with RO domestic VAT | Explain the boundary clearly |

## Out of Scope

- Implementing VAT/invoice changes; GDPR (already handled EU-wide).
