---
type: review
target: 038-039-invoicing
version: 1
supersedes: null
commit: e724528
branch: feat/bolt-038-vat-calculation
pass-type: discovery
date: 2026-08-13
lenses: [correctness, security, requirements, quality, input-validation, observability, race, completeness-critic]
lenses-not-run: [tests-coverage, db-parity]
verdict: request-changes
blockers: [PPW-469, PPW-470, PPW-471, PPW-472, PPW-473, PPW-474, PPW-475, PPW-476, PPW-477, PPW-478]
findings: { high: 10, medium: 15, low: 5, cleanup: 7, refuted: 1 }
tests: { dotnet: "146/146", frontend: "n/a — backend-only change" }
---

# Review v1 — 038-039-invoicing

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-469 | 🔴 | Invoice PDF retrieval bypasses IStorageRouter, always reads local disk | `Controllers/InvoicesController.cs:22` | yes |
| PPW-470 | 🔴 | Invoice PDF generation/upload bypasses IStorageRouter, always writes local disk (ADR-008) | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:156` | yes |
| PPW-471 | 🔴 | Invoice creation is check-then-act with no DB uniqueness — concurrent webhooks can mint two fiscal invoices | `Services/Invoicing/InvoiceCreationService.cs:40` | yes |
| PPW-472 | 🔴 | InvoicePdfReadyNotifier never sends an email regardless of the flag, logs a false "sent" event | `Services/Invoicing/InvoicePdfReadyNotifier.cs:40` | yes |
| PPW-473 | 🔴 | Guest checkouts can never retrieve their invoice — JWT-only auth on the endpoint | `Controllers/InvoicesController.cs:16` | yes |
| PPW-474 | 🔴 | Orders marked Paid via admin manual reconciliation never get an Invoice row | `Services/AdminOrderService.cs:139` | yes |
| PPW-475 | 🔴 | ANAF upload success + DB commit failure is indistinguishable from never-uploaded | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:175` | yes |
| PPW-476 | 🔴 | No claim/lease on Pending invoices — multi-replica double-submits to ANAF and double-emails | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:120` | yes |
| PPW-477 | 🔴 | No control-character filtering on customer name/address before UBL XML serialization | `Services/Invoicing/InvoiceXmlBuilder.cs:119` | yes |
| PPW-478 | 🔴 | UBL invoice-line amounts are gross, not tax-exclusive — lines won't reconcile with the document total | `Services/Invoicing/InvoiceXmlBuilder.cs:230` | yes |
| PPW-479 | 🟠 | Admin invoice list `Page` param is unbounded — int32 overflow can reach `Skip()` | `Controllers/AdminInvoicesController.cs:57` | yes |
| PPW-480 | 🟠 | Admin "retry" resubmits byte-identical XML — can never fix the failure it exists for | `Services/Invoicing/InvoiceLifecycle.cs:106` | yes |
| PPW-481 | 🟠 | `AnafSettings` docstring's "byte-identical to baseline" claim is false when disabled | `Configuration/AnafSettings.cs:7` | yes |
| PPW-482 | 🟠 | `AdminInvoicesController`'s audit-logging doc-comment is false; the one logged action omits the admin id | `Controllers/AdminInvoicesController.cs:14` | yes |
| PPW-483 | 🟠 | Redundant Order re-query on every paid webhook in `InvoiceCreationService` | `Services/Invoicing/InvoiceCreationService.cs:49` | yes |
| PPW-484 | 🟠 | `InvoiceUploadJob` worker reloads the full Order graph even when only the ANAF step remains | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:246` | yes |
| PPW-485 | 🟠 | Checkout field-length caps are wider than the legal XML limits, with no truncation | `Validators/Payments/CreateOrderRequestValidator.cs:61` | yes |
| PPW-486 | 🟠 | Per-row catch collapses auth failure, network failure, and code bugs into one generic log event | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:91` | yes |
| PPW-487 | 🟠 | Unrecognized ANAF status string is silently treated as "still processing", raw value never logged | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:232` | yes |
| PPW-488 | 🟠 | No domain-tagged log for "customer charged, order not committed" in `WebhooksController` | `Controllers/WebhooksController.cs:205` | yes |
| PPW-489 | 🟠 | Polly retries the non-idempotent ANAF upload POST on ambiguous-outcome errors | `Services/Invoicing/Anaf/AnafResilienceHandler.cs:33` | yes |
| PPW-490 | 🟠 | SQLite invoice numbering's MAX+1 has no transaction/lock despite the comment's safety claim | `Services/Invoicing/SqliteInvoiceNumberingService.cs:41` | yes |
| PPW-491 | 🟠 | `InvoiceUploadJob` has zero tests despite being the most stateful new logic | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:1` | yes |
| PPW-492 | 🟠 | Webhook tests stub invoice creation to always return null; nothing asserts it runs or that failure is handled | `Tests/Unit/Controllers/WebhooksControllerMetricsTests.cs:58` | yes |
| PPW-493 | 🟠 | `PostgresInvoiceNumberingService` — the only prod numbering path — has no test coverage | `Services/Invoicing/PostgresInvoiceNumberingService.cs:1` | yes |
| PPW-494 | 🟡 | Cloned retry `HttpRequestMessage` in `AnafAuthHandler` is never disposed | `Services/Invoicing/Anaf/AnafAuthHandler.cs:43` | no |
| PPW-495 | 🟡 | `status=""` is rejected by the query validator but treated as "no filter" by the controller | `Validators/Invoices/AdminInvoiceListQueryValidator.cs:19` | no |
| PPW-496 | 🟡 | No backfill path for orders already Paid before this deploy | `Controllers/AdminInvoicesController.cs:1` | no |
| PPW-497 | 🟡 | Discovery manifest omitted ~24 changed files, including the VAT math itself | `Services/VatCalculator.cs:1` | no |
| PPW-505 | 🟡 | Fiscal-year numbering constraint can disagree between Postgres and .NET at a Dec 31/Jan 1 boundary | `Migrations/20260603101910_AddVatAndInvoices.cs:111` | no |
| PPW-498 | ⚪ | Polly retry pipeline in `AnafResilienceHandler` never disposes intermediate failed responses | `Services/Invoicing/Anaf/AnafResilienceHandler.cs:33` | no |
| PPW-499 | ⚪ | `AnafAuthHandler.CloneAsync` duplicates `SamedayAuthHandler`'s request-cloning logic verbatim | `Services/Invoicing/Anaf/AnafAuthHandler.cs:61` | no |
| PPW-500 | ⚪ | Response-status classification duplicated between `AnafSpvClient.UploadAsync` and `GetStatusAsync` | `Services/Invoicing/Anaf/AnafSpvClient.cs:59` | no |
| PPW-501 | ⚪ | Buyer-name fallback logic duplicated between `InvoiceXmlBuilder` and the PDF renderer | `Services/Invoicing/InvoiceXmlBuilder.cs:104` | no |
| PPW-502 | ⚪ | Invoice entity config uses a literal `"Sqlite"` string instead of the `DbProviders.Sqlite` constant | `Data/PhotoPrintDbContext.cs:428` | no |
| PPW-503 | ⚪ | `PostgresInvoiceNumberingService` interpolates the sequence name into raw SQL with no in-service validation | `Services/Invoicing/PostgresInvoiceNumberingService.cs:43` | no |
| PPW-504 | ⚪ | `OrderDetailDto` grew 3 required fields with no lens covering the frontend contract | `DTOs/Orders/OrderDetailDto.cs:5` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| `InvoiceXmlBuilder.VatRateFromInvoice` recomputes the VAT rate by division instead of reusing `Order.VatRate`, risking a rounding drift | The only real caller (`InvoiceUploadJob.LoadPairAsync`) loads `Invoice` and `Order` on the same tracked DbContext with no `AsNoTracking`; EF Core's automatic relationship fixup populates `invoice.Order` from FK/PK matching regardless of `.Include`, so the exact-rate branch runs every time, not the derived-division fallback the suspicion assumed. |

## Notes for the fixer

- **PPW-471 and PPW-490 share one root cause and one fix site** — see both ledger rows' History for the corrected catch site. Fix as one cluster.
- **PPW-469 and PPW-470 are one fix** (inject `IStorageRouter` in both files) — same cluster.
- **PPW-477, PPW-478, and PPW-501 all touch `InvoiceXmlBuilder`/`InvoicePdfDocument`'s customer/line-amount building** — worth sequencing together to avoid three separate passes over the same file.
- Every 🔴 and 🟠 finding whose suggested fix is trigger-list-shaped already carries an **approach-check verdict** in its ledger History line (10 findings, including PPW-478 — all came back `revised`, not `cleared`; read the corrected approach before implementing, not just the original suggested fix). Read PPW-478's before touching `BuildInvoiceLines`.
- `tests-coverage` and `db-parity` are **owed manifest lenses** (PPW-497) — a future certification pass must fold them in as full lenses, not just the two single-agent supplemental checks this pass ran to make an immediate call on the VAT math and the migration.
- Test run this pass was **scoped**, not the full suite (146/146 passed) — namespaces: `Invoicing`, `VatCalculator`, the three new config validators, `WebhooksControllerMetrics`, `OrderServiceIdempotencyConcurrency`. Run the same scope plus whatever a fix touches, per this repo's testing rule — never the full suite by default.
