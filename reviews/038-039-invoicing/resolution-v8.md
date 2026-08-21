---
type: resolution
target: 038-039-invoicing
version: 8
answers: pass v7 (verification — index row)
status: resolved
fixed_commit: 2daf61e
closed: 2026-08-21
---

# Resolution v8 — 038-039-invoicing

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-553 | fixed | `e782189` | New surface: the window is now `max(2 h, 4 × PollIntervalMinutes)`, so no legal cadence outruns it, and the outage Warning carries both numbers. 3 tests, 2 proven red |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — ANAF auth-outage window sizing | PPW-553 | `Services/Invoicing/Anaf/InvoiceUploadJob.cs`, `docs/DEPLOYMENT.md` | needed: worklog `check-returned` (revised — 2 intervals is a no-op, a cap re-creates the defect, the drafted red test was green) |

## Decisions

### Four poll intervals with a two-hour floor, and no cap (PPW-553)

The drafted window was `Clamp(2 × interval, 2 h, 12 h)`. The approach-check refuted every part.

- **2 intervals dedups nothing.** A row that just failed is excluded from the next tick by the
  cooldown at `InvoiceUploadJob.cs:83`, so consecutive auth attempts on it are up to *two*
  intervals apart. A 2-interval window expires exactly when the next attempt arrives.
- **A 12 h cap re-creates the finding.** Above a 720-minute interval the window is shorter than
  one tick, which is the reported defect verbatim. The cap was also not derivable: the repo
  states the deadline as 5 business days, never in hours.
- **No cap is needed.** `AnafSettingsValidator.cs:39` bounds the interval at 1440 minutes, so
  4 intervals can never exceed 96 h — inside the 120 h that 5 business days give, and the job is
  only registered when `Anaf:Enabled` is true, which is also when that rule applies.

So the window is `max(2 h, 4 × interval)`: 4 clears the cooldown's worst case with margin and
keeps the shipped 120/30 ratio, so the default deployment's behaviour is unchanged. The floor
also keeps the window positive — `MemoryCache` rejects a non-positive expiry, and that throw
would land inside the auth `catch` and lose the page altogether. This supersedes the flat-window
reasoning recorded in resolution v7.

### No configuration-disagreement warning, because the formula removes the disagreement (PPW-553)

The drafted fix added a boot Warning when the interval outran the window. The check killed it on
two grounds: a monotone formula has no disagreement left to report, and the Warning was
unreachable by any test, because every test enters the job by reflection at `ProcessBatchAsync`
or `RunTickAsync` and nothing has ever asserted the `anaf.upload-job.started` line. Shipping an
untested signal to describe a configuration the fix now handles would have been an admission
that the fix was partial. Instead `interval_minutes` joins `alert_window_minutes` on the
`auth-outage-continues` Warning, which an existing test already reaches, so an operator can check
the pair in the logs. No counter was added, on the same PPW-528 precedent v7 recorded.

### Two corrections to the finding (PPW-553)

The Evidence line cites `Configuration/AnafSettingsValidator.cs:39-40`; the file is at
`Validators/AnafSettingsValidator.cs:39-40`. More important, the finding's suggested test —
drive two ticks above the window and assert one Error — is **green against the defect** as
written: two back-to-back `ProcessBatchAsync` calls are milliseconds apart, so any window at all
suppresses the second page. The red proof only exists once `MemoryCacheOptions.Clock` is advanced
by the configured interval between the ticks, which is what the two new failing tests do.

### What the micro-review caught, and what stays uncovered (PPW-553)

One finding, at nit level: the red test's comment claimed its 360-minute advance modelled the
per-row cooldown, which that harness cannot exercise — the mocked `IInvoiceLifecycle` never
writes `LastError`, and the job's `TimeProvider` stays `TimeProvider.System` while only the cache
clock moves. The comment was reworded rather than the test rebuilt: the two halves of the
argument are each already covered, the cooldown by
`ProcessBatchAsync_RowThatJustFailed_IsSkippedUntilItsCooldownExpires` and the window by the
three new tests. Nothing pins the two together in one test, which is the honest gap for a
re-reviewer to weigh. The review also confirmed no third site of the class, no surviving doc
claim of the old flat window, no overflow across `1..1440` or even at `int.MaxValue`, and that
the moved `Math.Max(1, …)` leaves the `PeriodicTimer` and the `retryNotBefore` cooldown
unchanged.

### Parked without an owner (PPW-553)

Two questions had no owner to answer them in an unattended run, so both took the conservative
default of changing nothing.

- **Narrowing `Anaf:PollIntervalMinutes`.** The finding offers rejecting the disagreeing
  configuration in the validator as an alternative. That refuses config which boots today, and
  ADR-023 treats the cadence as an operator lever, so it is a capability removal needing a
  ruling. Not done; the derived window makes every value in `1..1440` safe instead.
- **The Sameday twin.** `ShipmentTrackingJob.cs:22` is a flat 30-minute window while
  `SamedaySettingsValidator.cs:47-48` bounds `TrackingIntervalMinutes` only at `>= 1` — no
  maximum at all, so the identical defect is unbounded there. It is the only other site of the
  class: the sweep found no third outage-alert window in any background job. Fixing another
  feature's alerting is outside this finding set, and minting a backlog row is the owner's
  ruling, so it is recorded here and left alone.

### The auditor's unpushed-commit error stands (PPW-553)

The records auditor hard-fails this round: `fixed_commit` `2daf61e` is reachable from no pushed
ref, so the evidence is single-machine. Clearing it needs a push of the branch, which no owner
authorised — two earlier rounds pushed it to a public remote without their say-so. A local tag
would satisfy the check while defeating its purpose, so none was made, and the auditor itself was
left untouched. The error is left standing and recorded in the metrics notes, and it clears the
moment the owner authorises a push. Round 7's line passed the same check only because of one of
those unauthorised pushes.
