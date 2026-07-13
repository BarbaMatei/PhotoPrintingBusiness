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

## Bundled scope (documented retroactively — REQ-4, review 042-v1)

The branch `feat/bolt-042-thumbnail-cache` also carried two change-sets that had **no
story or AC** and existed only as commit messages. The review flagged that a reviewer
approving "bolt 042" would unknowingly ship an auth-behavior change. They are documented
here (the minimum REQ-4 accepts); a clean split into their own bolts is left to the owner
as a process decision.

- **Change B — guest-auth self-heal.** A stale/expired guest token now self-heals instead
  of logging the user out: the `errorInterceptor` clears the token on a 401 for an
  unauthenticated caller (never navigates a guest to `/auth/login`), and the format
  selector re-inits + retries once (upload and restored-preview paths), deduping concurrent
  inits. Files: `error.interceptor.ts`, `format-selector-page.ts`.
  *Now covered by tests:* `error.interceptor.spec.ts` (guest/anon 401 branches — TEST-1/FE-3)
  and `format-selector-page.spec.ts` (dedup + retry — FE-1/FE-2/FE-4).
- **Change C — dev-warning silencing.** HTTPS-redirect registered non-dev only, static
  files served only when `wwwroot` exists, EF split-query default. Files: `Program.cs`,
  `SecurityExtensions.cs`. No behavior change in production; reduces dev/test log noise.

**AC (retroactive) for change B:** an unauthenticated 401 clears any guest token and does
not navigate; a guest/anonymous session that expires mid-flow re-inits and the failed
upload/preview is retried exactly once; concurrent `ensureGuestSession()` callers share one
init. All are enforced by the specs listed above.
