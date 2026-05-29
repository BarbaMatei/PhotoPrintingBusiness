---
id: 052-archive-retention
unit: 002-archive-retention
intent: 024-order-photo-archive
type: ddd-construction-bolt
status: planned
stories:
  - 001-purge-original-on-shipped
  - 002-retention-cleanup-job
created: 2026-05-27T13:10:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [051-order-photo-promotion]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 052-archive-retention

## Overview

Enforce the archive lifecycle: delete the cloud original when an order ships (configurable status), and delete the large preview + thumbnail after the configurable retention window (default 12 months from order completion).

## Stories Included

- **001-purge-original-on-shipped** (Must): delete cloud original on order → Shipped; keep large + thumb.
- **002-retention-cleanup-job** (Must): periodic 12-month cleanup of large + thumb.

## Bolt Type

**Type**: DDD Construction Bolt — `.specsmd/aidlc/templates/construction/bolt-types/ddd-construction-bolt.md`

## Dependencies

### Requires
- 051-order-photo-promotion (cloud-located photos to purge/clean).

### Enables
- Bounded, compliant archive.

## Notes

- Reuses bolt-033 cleanup-job patterns (retention config, referenced-row safety).
- Order/order-item metadata is always retained; only image blobs are removed.
