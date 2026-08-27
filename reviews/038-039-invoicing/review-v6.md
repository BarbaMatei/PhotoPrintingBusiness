---
type: review
target: 038-039-invoicing
version: 6
supersedes: null
commit: 1c217f4
branch: feat/bolt-038-vat-calculation
pass-type: delta-discovery
date: 2026-08-20
lenses: [correctness, completeness-critic, tests-coverage, db-parity, observability]
lenses-not-run: [security, requirements, quality, input-validation, race, frontend-ux]
verdict: request-changes
blockers: [PPW-512, PPW-513, PPW-514, PPW-515]
findings: { high: 4, medium: 19, low: 9, cleanup: 6, refuted: 0 }
tests: { dotnet: "315/315", frontend: "n/a — backend-only delta" }
---

# Review v6 — 038-039-invoicing

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-512 | 🔴 | Easybox orders emit e-Factura with empty mandatory buyer address elements | `Services/Invoicing/InvoiceXmlBuilder.cs:121` | yes |
| PPW-513 | 🔴 | uq_invoices_series_year_number index expression is not IMMUTABLE, so Postgres aborts Migrate() at prod boot | `Migrations/20260603101910_AddVatAndInvoices.cs:112` | yes |
| PPW-514 | 🔴 | Exhausted-retry rollback reload discards the processor transaction id and the Error log omits it | `Controllers/WebhooksController.cs:427` | yes |
| PPW-515 | 🔴 | ANAF client-side timeout escapes as OperationCanceledException and stops the upload worker, unreachable by tests | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:81` | yes |
| PPW-516 | 🟠 | Exhausted invoice-number retry answers the payment processor 200, killing its last retry | `Controllers/WebhooksController.cs:304` | yes |
| PPW-517 | 🟠 | Invoice PDF tier is chosen from the live CloudEnabled flag with no per-row StorageLocation, so a Provider flip orphans stored PDFs | `Controllers/InvoicesController.cs:68` | yes |
| PPW-518 | 🟠 | Admin manual mark-Paid inserts an Invoice with none of the webhook path's unique-violation protections, and its creation is fully mocked in tests | `Services/AdminOrderService.cs:148` | yes |
| PPW-519 | 🟠 | RetryAsync wipes XmlPayload, destroying the submitted-XML snapshot and diverging it from the kept PDF | `Services/Invoicing/InvoiceLifecycle.cs:120` | yes |
| PPW-520 | 🟠 | Per-line PriceAmount x InvoicedQuantity no longer equals LineExtensionAmount, and nothing asserts it | `Services/Invoicing/InvoiceXmlBuilder.cs:219` | yes |
| PPW-521 | 🟠 | InvoiceAddressFormatter.Truncate throws NRE on a null City/Street that the validators accept | `Services/Invoicing/InvoiceAddressFormatter.cs:12` | yes |
| PPW-522 | 🟠 | Unbuildable invoice stays Pending forever and starves the upload batch | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:206` | yes |
| PPW-523 | 🟠 | Missing invoice PDF blob surfaces as an unlogged generic 500 with no distinct event | `Controllers/InvoicesController.cs:69` | yes |
| PPW-524 | 🟠 | The whole invoicing feature has no SPA consumer and no lens covered the frontend | `Controllers/InvoicesController.cs:1` | yes |
| PPW-525 | 🟠 | Guest invoice access is defeated by the unchanged guest-session lifetime and the never-implemented order transfer | `Controllers/InvoicesController.cs:52` | yes |
| PPW-526 | 🟠 | the legacy processor paid leg's new three-state outcome and its rollback have no endpoint-driven test | `Controllers/WebhooksController.cs:205` | yes |
| PPW-527 | 🟠 | Only the classified exhaust path is metric-safe; other invoice-creation failures still escape RecordPaymentWebhook | `Controllers/WebhooksController.cs:390` | yes |
| PPW-528 | 🟠 | Charged-but-unpaid order emits the same metric label as a routine card decline | `Controllers/WebhooksController.cs:389` | yes |
| PPW-529 | 🟠 | No test applies the migration chain — the unique-index DDL is only ever proven via EnsureCreated from the model | `Tests/Integration/PostgresInvoiceNumberingServiceIntegrationTests.cs:79` | yes |
| PPW-530 | 🟠 | MakeInvoiceOrderIdUnique creates the unique index with no dedupe step, so duplicate rows fail prod boot | `Migrations/20260813093709_MakeInvoiceOrderIdUnique.cs:17` | yes |
| PPW-531 | 🟠 | Unique-violation classifiers cover only 2 of 3 Invoices unique indexes and their Npgsql arms are untested | `Controllers/WebhooksController.cs:460` | yes |
| PPW-532 | 🟠 | One ANAF credential failure fans out into up to 50 Error logs and Sentry captures per tick | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:85` | yes |
| PPW-533 | 🟠 | ANAF auth failures leave LastError blank, so the admin invoice list shows no reason | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:82` | yes |
| PPW-534 | 🟠 | invoice.creation.allocated is logged pre-commit on every retry attempt, so logs show phantom invoice numbers | `Services/Invoicing/InvoiceCreationService.cs:96` | yes |
| PPW-535 | 🟡 | Truncate can split a UTF-16 surrogate pair, wedging the invoice in Pending forever | `Services/Invoicing/InvoiceAddressFormatter.cs:13` | no |
| PPW-536 | 🟡 | RetryAsync resets every ANAF field except ClaimedAt, which the success path never releases either | `Services/Invoicing/InvoiceLifecycle.cs:117` | no |
| PPW-537 | 🟡 | Residual reconciliation is unguarded — negative line amount, silently absorbed snapshot mismatch, crash on an empty line list | `Services/Invoicing/InvoiceXmlBuilder.cs:213` | no |
| PPW-538 | 🟡 | Upload batch query ignores ClaimedAt, unlike the existing AWB claim precedent | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:65` | no |
| PPW-539 | 🟡 | New ClaimedAt column and unique index never land on an existing dev SQLite database | `Program.cs:358` | no |
| PPW-540 | 🟡 | Postgres numbering tests draw a random year and assert absolute sequence values, so they collide and leak sequences | `Tests/Integration/PostgresInvoiceNumberingServiceIntegrationTests.cs:16` | no |
| PPW-541 | 🟡 | claim-lost log asserts "another worker" for causes it cannot distinguish | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:141` | no |
| PPW-542 | 🟡 | submitted-but-not-recorded logs Error twice and gets no Sentry capture | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:226` | no |
| PPW-543 | 🟡 | LastError is persisted before the exception is logged, so a DB blip loses the root cause | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:206` | no |
| PPW-544 | ⚪ | New Must rules have no WithMessage, so 400s carry English default messages | `Validators/Payments/CreateOrderRequestValidator.cs:32` | no |
| PPW-545 | ⚪ | CreateForOrderAsync(Guid) has no production caller left | `Services/Invoicing/IInvoiceCreationService.cs:24` | no |
| PPW-546 | ⚪ | Retry pre-read pulls the whole XmlPayload from the DB just to log its length | `Services/Invoicing/InvoiceLifecycle.cs:104` | no |
| PPW-547 | ⚪ | data-stack standard never mentions the Invoices table it must describe | `memory-bank/standards/data-stack.md:55` | no |
| PPW-548 | ⚪ | ADR-023/decision-index still credit CAS for multi-replica safety, now superseded by the ClaimedAt lease | `memory-bank/standards/decision-index.md:34` | no |
| PPW-549 | ⚪ | Unknown ANAF status warns twice and the job's line drops the diagnostic fields | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:309` | no |

## Refuted

None — every finding this pass raised survived its adversarial check.

## Notes for the fixer

Request changes. Four 🔴 rows, each reachable in production, none of them found by the eight
lenses of the first pass: PPW-512, PPW-513, PPW-514, PPW-515. Take them first.

PPW-513 is the one to start on — it is the only row here that prevents the application from
starting at all, and the only one whose absence from every prior pass has a structural cause
rather than an accidental one. Its fix and PPW-529 belong in the same change: the reason nothing
caught it is that no test applies the migration chain to Postgres.

Two clusters group naturally. Storage tier: PPW-517 and PPW-523 share a cause and a file.
Webhook metric safety: PPW-516, PPW-527 and PPW-528 are three views of one gap, and the earlier
work on that path closed only the classified case.

PPW-516, PPW-519 and PPW-526 re-raise decisions already taken. Read the prior ruling on each
ledger row before changing anything — two of them were settled by an approach-check and one by an
owner ruling, and re-opening any of those three is a decision, not a fix.

PPW-521, PPW-524 and PPW-531 carry `plausible`, not `confirmed`. Confirm each against the code
before writing a fix; one of them may not survive contact.
