---
type: resolution
target: 038-039-invoicing
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 7cde815f4a4cb18e6fa1061d76a8746fd7fdaf5f
closed: 2026-08-13
---

# Resolution v1 — 038-039-invoicing

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-469 | fixed | `04eb38e` | InvoicesController now injects IStorageRouter, reads via CloudEnabled?Cloud:Local; new tests prove cloud vs local routing |
| PPW-470 | fixed | `f8daa1e` | InvoiceUploadJob's PDF save now resolves via IStorageRouter the same way; new tests (reflection-invoked UploadPendingAsync) prove routing |
| PPW-471 | fixed | `e38537d` | Unique index on Invoices.OrderId + provider-specific violation catch at both WebhooksController SaveChangesAsync sites; side effects gated behind a "did this call create it" signal; new Postgres-backed race tests |
| PPW-490 | fixed | `e38537d` | Same fix site as PPW-471: catch + retry on the InvoiceNumber unique-violation; corrected the misleading PostgresInvoiceNumberingService doc comment; new test forces the collision deterministically |
| PPW-472 | fixed | `4aa73d5` | Owner ruling: ship flag off, documented honestly. NotifyAsync no longer claims "sent"; InvoicingSettings docstring corrected; test now asserts the honest log line, not just no-throw |
| PPW-473 | fixed | `e7a1169` | Switched to DualAuthPolicy + ownership check against both UserId and GuestSessionId, matching Cart/Payments/Uploads; new tests cover guest-owns, wrong-user, wrong-guest-session |
| PPW-474 | fixed | `7539dcd` | AdminOrderService now calls CreateForOrderAsync in the same Paid branch, before SaveChangesAsync, mirroring the webhook handlers; new test asserts the call, another asserts no double-create on a later transition |
| PPW-475 | fixed | `a33e6d3` | Catch scoped to just the MarkSubmittedAsync call, logs anaf.upload-job.submitted-but-not-recorded with the AnafUploadId before rethrowing; new test proves it fires and the exception still propagates |
| PPW-476 | fixed | `e693baf` | Invoice.ClaimedAt + Anaf:ClaimTtlMinutes: atomic claim before the pipeline, released on a definite rejection, held through TTL on ambiguous outcomes; migration hand-fixed to timestamptz. See Decisions for the test-harness change. |
| PPW-477 | fixed | `d50f897` | Shared TextValidation.HasNoXmlInvalidChars rejects XML-1.0-invalid control chars on shipping-address + Register/UpdateAccount name fields; also caught AnafUnreachableException and wrapped Steps 1-2 so a bad snapshot no longer loops silently |
| PPW-478 | fixed | `94cd056` | BuildInvoiceLines now derives net line totals via VatCalculator, reconciles the residual on the last line, and derives unit price from the reconciled total; new tests prove net-not-gross and exact line-sum reconciliation |
| PPW-479 | fixed | `faa7dcb` | AdminInvoiceListQueryValidator caps Page at 1,000,000 (well inside int32 headroom at any allowed Size); new tests cover the overflow, the bound, and the zero case |
| PPW-480 | fixed | `bd4d8ba` | RetryAsync now clears XmlPayload (logging the pre-retry payload/error first) and leaves PdfStoragePath untouched per the vetted approach; extended test asserts both |
| PPW-481 | fixed | `82ab013` | Corrected the docstring's false byte-identical-when-disabled claim — doc-only, no test |
| PPW-482 | fixed | `881b101` | Added admin_user_id-tagged logs to ListAsync and GetXmlAsync, added it to RetryAsync's existing log; new test file (none existed) asserts all three |
| PPW-483 | fixed | `d24a115` | Added CreateForOrderAsync(Order, ct) overload skipping the redundant Order re-query; both webhook and AdminOrderService call sites switched to it; new tests cover create + replay through the overload |
| PPW-484 | fixed | `ed031dc` | UploadPendingAsync now loads Order (with Includes) only when Step 1 or 2 still needs it; new SQL-logging regression test proves no Orders-table query when only the ANAF step remains |
| PPW-485 | fixed | `0ccb7d6` | Checkout now rejects City/RecipientName over the real CIUS-RO limits and a combined Street+Number+Block over 150 chars (new InvoiceAddressFormatter); XML builder also truncates as a safety net for pre-existing data; 6 new tests |
| PPW-486 | fixed | `8928e09` | Per-row batch loop now catches AnafAuthException before the generic catch, logging anaf.upload-job.auth-failed and calling Sentry's IHub.CaptureEvent explicitly. See Decisions for the approach-check. |
| PPW-487 | fixed | `5450505` | AnafSpvClient logs the raw stare value at Warning when MapStatus can't classify it; the job's poll switch now logs Unknown distinctly from InProgress instead of grouping them |
| PPW-488 | fixed | `e7a9f71` | SaveOrderPaidWithInvoiceAsync gains a final DbUpdateException catch for exhausted invoice-number retries, logging manual-reconciliation-required with the pre-transition order status. See Decisions for the approach-check. |
| PPW-489 | wont-fix | — | Owner declined to fund a new ANAF lookup capability; accepts the documented invoice-number dedupe tolerance instead. See Decisions. |
| PPW-491 | fixed | `c748d40` | Partial-completion and 200-with-Errors paths were already covered by other findings' tests; added the real gap — PollSubmittedAsync's backoff-budget boundary (Rejected → MarkRejectedAsync vs MarkFailedAsync) |
| PPW-492 | fixed | `bc8e5a4` | Fixed the stale It.IsAny(Guid) stub (dead since PPW-483's overload switch); new tests assert CreateForOrderAsync runs on the Paid transition and that a throw from it leaves the order AwaitingPayment in a fresh read of the database |
| PPW-493 | fixed | `7cde815` | New real-Postgres tests (sequential numbering, year rollover, 20 concurrent callers) as a [SkippableFact] gated on ConnectionStrings__Default; skips locally (no Docker), runs for real in CI per the owner's ruling |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — storage routing bypasses | PPW-469, PPW-470 | `Controllers/InvoicesController.cs`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs` | not needed (DI wiring only) |
| B — invoice-duplication and numbering race | PPW-471, PPW-490 | `Services/Invoicing/InvoiceCreationService.cs`, `Controllers/WebhooksController.cs`, `Services/Invoicing/PostgresInvoiceNumberingService.cs`, migration | needed: ledger History (revised — catch site corrected to WebhooksController; one fix site covers both findings) |
| C — email-sent flag honesty | PPW-472 | `Services/Invoicing/InvoicePdfReadyNotifier.cs`, `Configuration/InvoicingSettings.cs` | not needed (owner gate: ship flag off, document honestly) |
| D — guest invoice access | PPW-473 | `Controllers/InvoicesController.cs` | not needed (reuses existing DualAuthPolicy pattern) |
| E — admin-paid invoice creation | PPW-474 | `Services/AdminOrderService.cs` | not needed (mirrors existing webhook call) |
| F — ANAF submit-but-not-recorded | PPW-475 | `Services/Invoicing/Anaf/InvoiceUploadJob.cs`, tests | needed: ledger History (revised — catch scoped to MarkSubmittedAsync only) |
| G — multi-replica claim/lease | PPW-476 | `Services/Invoicing/Anaf/InvoiceUploadJob.cs`, `Models/Invoice.cs`, migration, `Configuration/AnafSettings.cs` | needed: ledger History (revised — claim-release, TTL, migration type gotcha) |
| H — control-char validation | PPW-477 | `Validators/Payments/CreateOrderRequestValidator.cs`, `Validators/Account/*`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs` | needed: ledger History (revised — scope widened to account validators + PDF net + 2 uncaught paths) |
| I — UBL net-vs-gross line amounts | PPW-478 | `Services/Invoicing/InvoiceXmlBuilder.cs`, tests | needed: ledger History (revised — derivation moved to BuildInvoiceLines) |
| J — admin page-param overflow | PPW-479 | `Validators/Invoices/AdminInvoiceListQueryValidator.cs` | not needed (validation only) |
| K — admin retry XML rebuild | PPW-480 | `Services/Invoicing/InvoiceLifecycle.cs` | needed: ledger History (revised — clear XmlPayload only, log before clearing) |
| L — misleading AnafSettings docstring | PPW-481 | `Configuration/AnafSettings.cs` | not needed (doc-only) |
| M — admin audit-log ids | PPW-482 | `Controllers/AdminInvoicesController.cs` | not needed (logging only) |
| N — redundant Order re-query | PPW-483 | `Services/Invoicing/InvoiceCreationService.cs`, `Controllers/WebhooksController.cs` | not needed (query refactor only) |
| O — Order graph reload on every tick | PPW-484 | `Services/Invoicing/Anaf/InvoiceUploadJob.cs` | not needed (query refactor only) |
| P — CIUS-RO field-length limits | PPW-485 | `Validators/Payments/CreateOrderRequestValidator.cs`, `Services/Invoicing/InvoiceXmlBuilder.cs`, `Services/Invoicing/InvoiceAddressFormatter.cs` | not needed (validation only) |
| Q — ANAF auth-failure signal | PPW-486 | `Services/Invoicing/Anaf/InvoiceUploadJob.cs` | needed: ledger History (revised — drop repeat-escalation, add Sentry capture) |
| R — unrecognized ANAF status | PPW-487 | `Services/Invoicing/Anaf/InvoiceUploadJob.cs`, `Services/Invoicing/Anaf/AnafSpvClient.cs` | not needed (log line + switch branch only) |
| S — webhook race manual-reconciliation log | PPW-488 | `Controllers/WebhooksController.cs` | needed: ledger History (revised — narrow catch type, fix stale-status logging) |
| T — ANAF retry dedupe tolerance | PPW-489 | — | not needed (owner gate: accept documented tolerance, no code change) |
| U — InvoiceUploadJob test coverage | PPW-491 | `Services/Invoicing/Anaf/InvoiceUploadJob.cs`, tests | not needed (test-only) |
| V — webhook invoice-creation test coverage | PPW-492 | `Tests/Unit/Controllers/WebhooksControllerMetricsTests.cs` | not needed (test-only) |
| W — Postgres numbering test coverage | PPW-493 | `Services/Invoicing/PostgresInvoiceNumberingService.cs`, tests | not needed (owner gate: write test now, prove in CI; test-only) |

## Decisions

### Ship the flag off, documented honestly (PPW-472)

Owner chose not to build the real email integration this round. Fix: `NotifyAsync` stops
logging a `sent` event it never performs; `InvoicingSettings`'s docstring states plainly
that PDF generation and the customer endpoint are dormant while `Anaf:Enabled=false`, and
that no email is sent regardless of the flag until this is built. No new test — this is a
doc/log-text correction, not a behavior change with a failure mode to cover.

### Test harness moved off EF InMemory (PPW-476)

Adding the atomic claim required `ExecuteUpdateAsync`, which EF Core's InMemory provider does
not support. The whole `InvoiceUploadJob` test harness moved from `UseInMemoryDatabase` to a
real PostgreSQL connection, matching the project's established pattern for anything provider-sensitive.

### Approach-check outcome: drop repeat-escalation, add Sentry capture (PPW-486)

Revised: dropped "escalate on repeat" (no per-replica-safe attempt counter exists, and ADR-024
already rejected a persisted counter for this subsystem); added an explicit Sentry
`IHub.CaptureException` call, since a standalone `LogError` never reaches Sentry by this
project's own design.

### Approach-check outcome: narrow the catch, fix stale-status logging (PPW-488)

Revised: narrowed the new catch to `DbUpdateException` specifically, not bare `Exception` (which
would have mislabeled a client-disconnect `OperationCanceledException` as a payment incident);
the logged order status is snapshotted before `OrderStatusMachine.Transition` runs, since
`Transition` mutates the in-memory status before `SaveChangesAsync` — logging after a rollback
would have shown "Paid" for an order still `AwaitingPayment`.

### Accept ANAF's documented dedupe tolerance (PPW-489)

Owner chose not to fund a new ANAF lookup capability. No code change: `AnafResilienceHandler`
keeps retrying the upload POST on ambiguous errors, relying on ANAF's documented
invoice-number dedupe (`ddd-02-technical-design.md`, ADR-015 reference). Residual risk stays
recorded on the PPW-489 ledger row; status `wont-fix`.

### Write the Postgres test now, prove it only in CI (PPW-493)

This dev machine has no Docker/Postgres, so the new test cannot be proven red/green locally
— mirrors 043's PPW-169. Written as a `[SkippableFact]` gated on `ConnectionStrings__Default`
being set (same pattern as the MinIO `STORAGE_TEST_*` tests), which CI already provides
(`.github/workflows/ci.yml:74`, currently unused by any test). Skips locally with a clear
reason; first real execution happens when this PR's CI runs.
