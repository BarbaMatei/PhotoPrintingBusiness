---
type: resolution
target: 038-039-invoicing
version: 6
answers: review-v6.md
status: in-progress
fixed_commit:
closed:
---

# Resolution v6 — 038-039-invoicing

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-513 | fixed | `8917f9f` | Index expression now extracts the year from the timestamp pinned to UTC, which is immutable. Landed on the Postgres-only branch, whose own plan deletes this migration — the fix must be carried into the regenerated baseline |
| PPW-521 | fixed | `8917f9f` | Truncate is null-tolerant and never cuts between surrogate halves; 8 tests, both guards proven red on revert |
| PPW-535 | fixed | `8917f9f` | Same method as PPW-521, so the surrogate split was closed in the same change rather than left in the backlog |
| PPW-524 | deferred | — | Owner ruled the missing SPA consumer out of scope for this round. See Decisions |
| PPW-516 | deferred | — | Approach-check advised deferral and named four reasons; the strongest is that a Stripe 500 risks the endpoint being disabled. See Decisions |
| PPW-512 | deferred | — | Approach-check found the drafted locker fallback cannot work and raises a legal question about what address a fiscal document may carry. See Decisions |
| PPW-520 | deferred | — | Approach-check declined to assert a decimal-scale rule it could not cite from the repo, and the acceptance criterion that would settle it is unmet. See Decisions |
| PPW-522 | deferred | — | Approach-check showed the drafted fix is a silent no-op, and split the finding in two. See Decisions |
| PPW-514 | deferred | — | Not started before the round paused; no approach-check was needed for it. See Decisions |
| PPW-515 | deferred | — | Approach-check corrected the layer — the guard belongs in ExecuteAsync, not only the ANAF client, since storage cancellation reaches the same exit. Not implemented before the pause |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — upload job resilience | PPW-515, PPW-522, PPW-532, PPW-533 | `Services/Invoicing/Anaf/InvoiceUploadJob.cs`, `AnafSpvClient.cs`, `InvoiceLifecycle.cs` | needed: worklog `check-returned` (revised — all four sub-fixes corrected) |
| B — webhook metric safety | PPW-516, PPW-527, PPW-528 | `Controllers/WebhooksController.cs`, `Observability/MetricNames.cs` | needed: worklog `check-returned` (revised — wrapper double-counts; label renamed) |
| C — UBL address and price | PPW-512, PPW-520, PPW-521 | `Services/Invoicing/InvoiceXmlBuilder.cs`, `InvoiceAddressFormatter.cs` | needed: worklog `check-returned` (revised — two owner decisions surfaced) |
| D — storage tier | PPW-517, PPW-523 | `Controllers/InvoicesController.cs`, `Models/Invoice.cs`, migration | needed: worklog `check-returned` (revised — backfill unimplementable) |
| E — migration expression | PPW-513 | `Migrations/20260603101910_AddVatAndInvoices.cs` | not needed (one-line SQL) |
| F — remaining rows | PPW-514, PPW-518, PPW-519, PPW-523, PPW-525, PPW-527, PPW-528, PPW-529, PPW-530, PPW-531, PPW-532, PPW-533, PPW-534 | various | not started — round paused |

## Decisions

### Round paused on a concurrent data-stack rewrite (PPW-513)

The owner moved to `feat/postgres-only-data-stack` mid-round. Its design deletes every file under
`Migrations/` and regenerates one Npgsql baseline, then removes SQLite along with
`SqliteInvoiceNumberingService` and the SQLite arms of the unique-violation classifiers. That
reshapes PPW-529, PPW-530, PPW-531 and PPW-540, makes PPW-516 unreachable, and would collide with
the column PPW-517 wants. Continuing would mean fixing several rows twice. The PPW-513 fix landed
on that branch and edits a file the plan deletes: the immutable expression must be carried into
the regenerated baseline or the boot abort returns.

### The buyer address on a locker order is a legal question (PPW-512)

The drafted fix does not work. `EasyboxLocker` carries no postal code and nothing in the repo can
supply one, so the mandatory element stays empty; the one precedent is a `"000000"` sentinel used
for shipping, which would be fabricated data on a fiscal document. Substituting the locker address
also asserts the buyer lives at a parcel locker. A buyer-owned `SavedAddress` exists but only for
registered users, and whether it is legally this invoice's address is not ours to decide. Throwing
instead is currently worse: the row stays `Pending`, and admin retry accepts only `Rejected` or
`Failed`, so the invoice becomes permanently unbuildable. The guard must therefore land together
with the PPW-522 escalation, and the address source needs an owner ruling.

### No decimal-scale change without a validator (PPW-520)

The check declined to assert what EN 16931 permits for a unit price, because the repo documents
only "two decimal places" for emitted amounts and says nothing about a separate scale or about
`BaseQuantity`. Story 001 requires the output to validate against a bundled schema in the tests;
that criterion is unmet, so no local check can adjudicate any option. The guard test can land now
because it carries no spec risk. Changing what is emitted waits for either the validator or an
owner ruling.

### The upload-job terminal state needs its own change (PPW-522)

`MarkFailedAsync` guards on the row already being `Submitted`, so calling it for a build failure on
a `Pending` row affects zero rows and returns false — the drafted fix changes nothing in
production, and would have looked green because the job tests mock `IInvoiceLifecycle`. The check
also found the budget counts wall-clock since creation rather than attempts, so a transient storage
blip after a long idle period would terminally fail a healthy invoice. Split: the batch-starvation
half can ship, the terminal-state half needs a new transition, a real attempt counter and an ADR
superseding ADR-024.
