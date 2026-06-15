---
id: 001-t1-fulfillment-logistics
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
status: ready
priority: must
created: 2026-06-05T12:57:50Z
assigned_bolt: 076-research-tracks
implemented: false
---

# Story: 001-t1-fulfillment-logistics

## User Story

**As the** owner deciding EU expansion
**I want** evidence that shipping prints from Romania to EU customers is viable, with real per-corridor cost/time numbers
**So that** I can confirm the RO-ship decision and know where (and at what scale) it breaks down

## Acceptance Criteria

- [ ] **Given** the RO-ship decision, **When** T1 reports, **Then** it provides realistic carrier cost AND delivery time for a representative photo-print parcel for each corridor: RO→DE, RO→FR, RO→IT, RO→ES, RO→PL, RO→HU, RO→BG (both market tiers) — actual numbers, not hand-waving
- [ ] **Given** Sameday is the current carrier, **When** T1 reports, **Then** it states exactly where Sameday's coverage ends and which carriers cover the remaining corridors
- [ ] **Given** local print-partner networks may exist, **When** T1 reports, **Then** it documents them as a costed **fallback/sensitivity** (pricing model, integration APIs, QC implications) and proposes the **market-size threshold** at which the partner model should be revisited
- [ ] **Given** competitors operate in the EU, **When** T1 reports, **Then** it includes a competitive scan of how existing EU photo-print players structure fulfillment and country presence
- [ ] **Given** any cost/carrier claim, **When** it appears, **Then** it cites a dated source (carrier rate card or equivalent)

## Technical Notes

- **Method (FR-1)**: execute as a multi-agent fan-out (parallel researchers per corridor/sub-question) + adversarial verification of headline cost claims. Not one sequential reader.
- Output: `docs/analysis/eu-expansion/track-1-fulfillment.md`.
- This is the **dominant track** — its numbers anchor the whole study. The fulfillment *model* is already decided (RO-ship); T1 *validates* it, it does not re-run a 3-way comparison.
- Throwaway: none expected (pure research).

## Dependencies

### Requires
- None (wave-parallel with T2–T7)

### Enables
- 001-synthesis-options-paper (Unit 2)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Carrier rate card not public | Use published tariff estimators / quoted brackets; flag confidence |
| Corridor has no economical option | State it explicitly; flag as a partner-fallback trigger |

## Out of Scope

- Site architecture, i18n, tax, payments (other tracks).
- Negotiating actual carrier contracts (this is research).
