---
id: 053-order-history-photos
unit: 003-order-history-photos
intent: 024-order-photo-archive
type: simple-construction-bolt
status: in-progress
stories:
  - 001-order-photos-endpoint
  - 002-order-detail-photo-grid
created: 2026-05-27T13:10:00Z
started: 2026-05-29T14:00:00Z
completed: null
current_stage: implement
stages_completed:
  - name: plan
    completed: 2026-05-29T14:10:00Z
    artifact: implementation-plan.md

requires_bolts: [051-order-photo-promotion]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 053-order-history-photos

## Overview

Let logged-in customers review the photos they ordered: a backend endpoint returning presigned large-preview + thumbnail URLs, and an order-detail thumbnail grid with a full-size lightbox.

## Stories Included

- **001-order-photos-endpoint** (Must): `GET /api/orders/{id}/photos` → presigned URLs, owner/claim auth.
- **002-order-detail-photo-grid** (Must): FE thumbnail grid → large-preview lightbox.

## Bolt Type

**Type**: Simple Construction Bolt — `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`
*(Frontend-led unit + thin read endpoint → 3-stage simple bolt.)*

## Dependencies

### Requires
- 051-order-photo-promotion (cloud large preview + thumbnail to serve).

### Enables
- Customer order-history review (intent 024 goal).

## Notes

- Reuse the intent-010 lightbox + intent-012 shared loading/empty components if present.
- Registered users + claimed guest orders only (guest tokenized access deferred).
