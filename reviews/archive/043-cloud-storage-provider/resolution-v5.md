---
type: resolution
target: 043-cloud-storage-provider
version: 5
answers: review-v5.md
status: resolved
fixed_commit: 2d02b13
closed: 2026-07-22
---

# Resolution v5 — 043-cloud-storage-provider

## Findings

| D# | Status | Commit | Note |
|---|---|---|---|
| D36 | fixed | `2d02b13` | Closing the lightbox now clears the photo id as well as the image source, and the refresh re-reads that id when the fetch resolves. A regression test opens, closes, then fires a grid error and asserts the modal stays closed. |
| D38 | fixed | `036ba05` | Unroutable cloud rows are excluded in the candidate query itself, so the window advances to rows that can be deleted. The operator signal survives as a count taken only when cloud is off. |
| D37 | deferred | — | Part of the sweep design item. Testing the periodic re-scan needs an interval seam, which belongs with the redesign rather than a patch here. See Decisions. |
| D35 | deferred | — | Part of the sweep design item, and the same root cause as D9 and D27. See Decisions. |
| D46 | deferred | — | Part of the sweep design item. A give-up marker is a schema and query change that belongs with the redesign. See Decisions. |
| D47 | deferred | — | Part of the sweep design item. Either document the restart requirement or re-read the setting each sweep. See Decisions. |
| D45 | backlog | — | Standalone Low. The ZIP pre-flight should throw a mapped domain error with a 409 or 422 status and log the cloud-off reason as a warning. |
| D43 | backlog | — | Low. A 401 for a non-authenticated visitor should show a retryable error or redirect, rather than relying on the interceptor to navigate. |
| D42 | backlog | — | Low. Show a neutral reloading state on the first image error and keep the reload message for after the one refresh attempt fails. |
| D48 | backlog | — | Low, narrow. Reset the lightbox failure flag on every open or refresh assignment rather than only when the URL string changes. |
| D40 | backlog | — | Low, coverage. Add a spec that fires a second image error and asserts the photos endpoint was called exactly twice. |
| D41 | backlog | — | Low, coverage. Add a spec that fires a tab keydown and asserts the event was prevented and focus stayed on the close button. |
| D39 | backlog | — | Low, test quality. Seed one stuck paid order still on local storage in both guard tests, so removing a guard reddens them. |
| D44 | backlog | — | Cleanup. Disable retry while a load is in flight and switch to a cancelling, destroy-aware subscription. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Lightbox state reset on close and at resolve time (`2d02b13`) | D36 | `UI/…/order-detail-page.ts` | not needed (a state-reset patch inside an existing flow) |
| B — Cleanup candidate query filter and diagnostic count (`036ba05`) | D38 | `BackgroundJobs/UploadCleanupJob.cs` | not needed (a query filter and one gated count) |
| C — Deferred as one design item | D35, D37, D46, D47 | — | not needed (no code changed) |
| D — Sent to backlog | D39, D40, D41, D42, D43, D44, D45, D48 | — | not needed (no code changed) |

## Decisions

### The sweep cluster is one design item, not four patches (D35, D37, D46, D47)

All four are symptoms of one under-built mechanism: the promotion sweep the previous round converted
from boot-only to periodic. It needs dedup against work already in flight, an interval seam that can
be tested, a give-up marker for permanently failed orders, and awareness of a configuration change at
runtime. Its concurrency half is the same root cause as the already-deferred D9 and D27, which the
concurrency-token work owns, and that work gets one adversarial design pass on the recovery model
before any code. Fixing these here as four separate patches is exactly the fix-generativity the stop
rule exists to end: the last two rounds each produced a fresh crop of defects from their own fixes.

### The batch-starvation edge was mis-called during the previous round (D38)

While fixing D24 the same person judged this edge out of scope, on the grounds that it needs at least
a full batch of aged cloud rows. The blinded lens showed the stall is real once that population
exists, because the skip ran after the fetch and never marked the rows, so the same oldest rows
re-filled the window every sweep. The correct fix is the query-level exclusion, which is what the
review recommended and what should have been done the first time. The operator warning is kept as a
count taken only when cloud is off, so the filter does not silently remove the signal.

### The close-during-refresh case was fixed too, beyond what was reported (D36)

The reported scenario was closing the lightbox before the error arrives. Reading the code showed a
second way in: closing it while the refresh is still in flight. Both are closed by clearing the photo
id on close and re-reading it when the fetch resolves. Only the first case has a spec; the second is
recorded as D90.

### The remaining Lows and the Cleanup go to backlog (D39 to D45, D48)

None is a regression and none is serious, so under the severity-based stop rule they do not re-arm the
loop. They are a standalone unmapped-error Low, the polish and coverage left by the URL refresh, and
one hinted guest-authentication Low. They wait for the backlog groomer, the next bolt touching that
area, or the certification pass. This makes the round patch-grade — no High fixed, no mechanism added
or converted, no design changed — so it does not call for another delta pass.
