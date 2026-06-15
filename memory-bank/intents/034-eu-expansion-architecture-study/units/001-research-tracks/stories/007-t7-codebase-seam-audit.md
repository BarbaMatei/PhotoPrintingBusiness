---
id: 007-t7-codebase-seam-audit
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
status: ready
priority: must
created: 2026-06-05T12:57:50Z
assigned_bolt: 082-research-tracks
implemented: false
---

# Story: 007-t7-codebase-seam-audit

## User Story

**As the** team that will build EU readiness
**I want** an honest, quantified map of where Romania/RON/`ro-RO` is hardcoded and where per-country variation seams will be
**So that** the localization + multi-currency retrofit is sized realistically before any implementation intent is created

## Acceptance Criteria

- [ ] **Given** this repository (read-only), **When** T7 reports, **Then** it locates hardcoded Romanian/`ro-RO`/RON across: Angular templates/components, backend messages, email templates, invoice PDF strings, legal pages, SEO/meta tags
- [ ] **Given** the local-currency decision, **When** T7 reports, **Then** currency hardcoding (RON assumptions, formatting) is sized as its **own area**
- [ ] **Given** integration coupling, **When** T7 reports, **Then** it identifies ANAF/Sameday/EuPlatesc coupling points that become per-country variation seams
- [ ] **Given** the audit, **When** T7 reports, **Then** it gives **file/occurrence counts per area** and names the **top-10 heaviest retrofit spots**
- [ ] **Given** upcoming planned work, **When** T7 reports, **Then** it notes which wave bolts (058, 067, 069) will add to the retrofit bill

## Technical Notes

- **Method**: **repo-bound — NO web research.** Use code search (Grep/Glob) across the repo. May still fan out parallel agents by area (frontend / backend / emails / invoices / legal / SEO) for thoroughness.
- Output: `docs/analysis/eu-expansion/track-7-seam-audit.md`.
- This is the only track that reads the codebase; it writes **no** code.

## Dependencies

### Requires
- None (wave-parallel; conflict-free — read-only)

### Enables
- 001-synthesis-options-paper (Unit 2) — supplies the retrofit cost basis

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Romanian text embedded in non-obvious places (seed data, enums) | Count separately; flag as easily-missed |
| A seam is already partially abstracted | Note it as lower-cost; don't double-count |

## Out of Scope

- Web research (other tracks); fixing any seam; production-code changes.
