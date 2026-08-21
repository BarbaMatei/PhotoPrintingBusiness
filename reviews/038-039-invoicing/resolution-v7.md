---
type: resolution
target: 038-039-invoicing
version: 7
answers: pass v6 (verification — index row)
status: resolved
fixed_commit: 0ec6497
closed: 2026-08-21
---

# Resolution v7 — 038-039-invoicing

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-550 | fixed | `f602d4b` | The fallback tier's read is guarded, so a miss on every candidate tier lands on the same 404 and `invoice.pdf.blob-missing` event as a single-tier miss, now carrying `tiers_tried` and the adapter's inner cause. 3 tests, 1 proven red |
| PPW-551 | fixed | `dab1121` | New surface: `AnafOutageRegistry` plus a 2 h `AuthOutageAlertWindow` gate the credential Error and Sentry capture across ticks; a per-tick `auth-outage-continues` Warning keeps the outage visible. 5 tests, 1 proven red |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — invoice PDF tier fallback | PPW-550 | `Controllers/InvoicesController.cs`, `bolts/039-efactura-anaf/ddd-02-technical-design.md`, story 003 | needed: worklog `check-returned` (revised — sibling catch shape refuted, premise corrected, `miss_cause` added) |
| B — ANAF credential-outage alerting | PPW-551 | `Services/Invoicing/Anaf/InvoiceUploadJob.cs`, `AnafOutageRegistry.cs`, `Services/MemoryCacheOnceRegistry.cs`, `Program.cs`, `docs/DEPLOYMENT.md` | needed: worklog `check-returned` (revised — flat window, no new metric, registry-level expiry test) |

## Decisions

### The escaping exception was a wrong status, not an unlogged one (PPW-550)

The finding calls the double-miss an unlogged 500. It is not: `ExceptionHandlerMiddleware`
logs every unmapped exception at Error and captures it to Sentry. The real defect is the status
code and the loss of the greppable `invoice.pdf.blob-missing` event, which is what the fix
restores. The correction matters in one direction the finding did not state: routing this case to
the 404 path also **removes** the Sentry event it used to get. That is deliberate — the
single-tier miss shipped without one, and one missing blob should not read as two different
incidents depending on which tier was stamped.

### Only a missing file becomes a 404 (PPW-550)

The catch stays strictly `FileNotFoundException`, which both adapters already use as their
uniform "object not found" contract. Bad credentials, transport failures, non-404 S3 faults and
cancellation all still reach the middleware as 500s, because a reachable-but-broken store is an
outage, not a gone file. One wrinkle survives from the storage adapter: S3 maps a missing
**bucket** to the same 404, so `miss_cause` now carries the adapter's inner message and the fix
keeps whichever tier's miss has one. Without that, a whole-bucket misconfiguration would read as
"this one file is gone" on every request.

### A flat 2 h alert window, not one derived from the poll interval (PPW-551)

The drafted window was 6 poll intervals. The approach-check refuted it: `PollIntervalMinutes` is
valid up to 1440, so the formula could produce a 6-day window — longer than the ANAF
5-business-day submission SLA it was meant to protect. A flat 2 h is longer than the default
30-minute tick, so it dedups across ticks, and re-pages about 60 times inside 5 days. The
micro-review then killed a second rationale: the 85 h `BackoffHours` budget does not bound this,
because `IsBudgetExhausted` gates only the rejection path — an auth outage never marks an invoice
`Failed`. The window is right; the first reason given for it was false, in the code comment too.

### No new metric for the suppressed page (PPW-551)

The approach-check asked for a counter, because a suppressed Error plus no metric is three hours
of silence. The counter is not in this round: a new instrument needs a name constant, a row in
the label contract the cardinality test enumerates, and a dashboard panel to read it — and this
target already deferred PPW-528 for exactly that shape. Instead the deduped tick logs
`anaf.upload-job.auth-outage-continues` at Warning, which is the surface the deployment runbook
already uses for this job. Silence now means recovery, and the runbook says so.

### Only one site in the codebase could page (PPW-551)

The class is "a systemic failure reported once per tick instead of once per outage", and it is
common: `anaf.upload-job.batch-failed`, `status-unknown`, `unreachable` and nine other background
jobs all log per tick or per row with no window. None of them was fixed, on one fact — the
credential branch held the only Sentry capture in any background job, and alerting here is
Sentry-driven, so it was the only site that paged a human. The rest is log noise. The nearest
gap worth naming: `status-unknown` has no window while its Sameday analogue routes vendor drift
through one.

### One page per replica per window, not per outage (PPW-551)

`IMemoryCache` is per-process, so N replicas page up to N times per window and a restart
re-pages at once. That is the same limitation the two Sameday registries carry, and PPW-455
(044-045, still open) tracks the class. The claim this round makes is the narrow one, and the
runbook states the caveat.

### Parked without an owner (PPW-550)

Two questions had no owner to answer them in an unattended round, so both took the conservative
default of changing nothing. First, `AdminOrderService.cs:244` reads a blob with no guard inside
a ZIP export whose headers are already sent, so a missing file truncates the archive and appends
a 500 body to it — the same class as this finding and a worse failure, but outside the finding
set, and minting a backlog row is the owner's ruling. Second, the check argues
`InvoiceUploadJob`'s explicit Sentry capture is redundant because Error-level logs already ship
events; deleting it would undo behaviour a prior round's test pins, so it stays. A third, minor:
a storage adapter returning null instead of a stream would now read as a 404 rather than a 500 —
unreachable through the non-nullable interface, so no guard was added.

### Round scope (PPW-552)

PPW-552 is 🟡 and entered the ledger as `backlog` at reconciliation, so it is not in this round.
