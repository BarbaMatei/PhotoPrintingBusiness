---
id: 082-research-tracks
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
type: spike-bolt
status: planned
stories:
  - 007-t7-codebase-seam-audit
created: 2026-06-05T12:57:50Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 6h

requires_bolts: []
enables_bolts: [083-synthesis-and-decision]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 1
  testing_scope: 1
---

# Bolt: 082-research-tracks

## Overview

Spike for **T7 — Codebase seam audit**. Repo-bound — **no web research**, read-only.

## Objective

Produce `docs/analysis/eu-expansion/track-7-seam-audit.md`: where RO/RON/`ro-RO` is hardcoded
(Angular, backend messages, emails, invoice PDFs, legal pages, SEO/meta), ANAF/Sameday/EuPlatesc
coupling seams, and currency hardcoding sized as its own area — with file/occurrence counts
per area and the top-10 heaviest spots; notes wave bolts 058/067/069 add to the bill.

## Stories Included

- **007-t7-codebase-seam-audit**: T7 codebase seam audit (Must)

## Bolt Type

**Type**: Spike Bolt (research — knowledge out, no code)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/spike-bolt.md`

## Stages

- [ ] **1. explore**: Pending → repo search by area; raw counts (⛔ human checkpoint)
- [ ] **2. document**: Pending → `track-7-seam-audit.md` (⛔ human checkpoint)

## Dependencies

### Requires
- None (wave-parallel; conflict-free — read-only)

### Enables
- 083-synthesis-and-decision

## Success Criteria

- [ ] File/occurrence counts per area + named top-10 spots
- [ ] Currency hardcoding sized separately; coupling seams identified
- [ ] Wave bolts 058/067/069 retrofit additions noted

## Notes

**Time-box: 6h.** Repo-bound: use Grep/Glob, no web. May fan out parallel agents by area
(frontend/backend/emails/invoices/legal/SEO). Writes **no** code.
