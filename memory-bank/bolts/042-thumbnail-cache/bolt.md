---
id: 042-thumbnail-cache
unit: 001-thumbnail-cache
intent: 019-thumbnail-cache-and-cloud-storage
type: simple-construction-bolt
status: complete
stories:
  - 001-thumbnail-path-schema
  - 002-persist-thumbnail-on-first-request
  - 003-imagesharp-max-pixels
created: 2026-05-25T10:30:00Z
started: 2026-05-27T11:00:00Z
completed: 2026-05-27T11:45:00Z
current_stage: null
stages_completed:
  - name: plan
    completed: 2026-05-27T11:10:00Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-05-27T11:30:00Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-05-27T11:45:00Z
    artifact: test-walkthrough.md

requires_bolts: [012-photo-upload-backend, 040-containers-and-pipelines]
enables_bolts: [043-cloud-storage-provider]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 042-thumbnail-cache

## Overview

Schema + cache-on-first-request + ImageSharp bomb protection.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — controller patch, regeneration policy, MaxPixels values |
| 2 | Implement | Migration, controller change, `Program.cs` MaxPixels |
| 3 | Test | Two-call counter test, missing-file regeneration test, pixel-bomb rejection test |

## Dependencies

- **Requires**: 012-photo-upload-backend, 040-containers-and-pipelines.
- **Enables**: 043-cloud-storage-provider.
