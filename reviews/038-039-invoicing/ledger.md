---
type: review-ledger
target: 038-039-invoicing
updated: 2026-08-21
---

# Ledger — 038-039-invoicing

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-469 | 🔴 | v1 | Invoice PDF retrieval bypasses IStorageRouter, always reads local disk | `Controllers/InvoicesController.cs:22` | verified | `11dfb8e` |
| PPW-470 | 🔴 | v1 | Invoice PDF generation/upload bypasses IStorageRouter, always writes local disk (ADR-008) | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:156` | verified | `11dfb8e` |
| PPW-471 | 🔴 | v1 | Invoice creation is check-then-act with no DB uniqueness — concurrent webhooks can mint two fiscal invoices | `Services/Invoicing/InvoiceCreationService.cs:40` | verified | `11dfb8e` |
| PPW-472 | 🔴 | v1 | InvoicePdfReadyNotifier never sends an email regardless of the flag, logs a false "sent" event | `Services/Invoicing/InvoicePdfReadyNotifier.cs:40` | verified | `11dfb8e` |
| PPW-473 | 🔴 | v1 | Guest checkouts can never retrieve their invoice — JWT-only auth on the endpoint | `Controllers/InvoicesController.cs:16` | verified | `11dfb8e` |
| PPW-474 | 🔴 | v1 | Orders marked Paid via admin manual reconciliation never get an Invoice row | `Services/AdminOrderService.cs:139` | verified | `11dfb8e` |
| PPW-475 | 🔴 | v1 | ANAF upload success + DB commit failure is indistinguishable from never-uploaded | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:175` | verified | `11dfb8e` |
| PPW-476 | 🔴 | v1 | No claim/lease on Pending invoices — multi-replica double-submits to ANAF and double-emails | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:120` | verified | `11dfb8e` |
| PPW-477 | 🔴 | v1 | No control-character filtering on customer name/address before UBL XML serialization | `Services/Invoicing/InvoiceXmlBuilder.cs:119` | verified | `11dfb8e` |
| PPW-478 | 🔴 | v1 | UBL invoice-line amounts are gross, not tax-exclusive — lines won't reconcile with the document total | `Services/Invoicing/InvoiceXmlBuilder.cs:230` | verified | `11dfb8e` |
| PPW-479 | 🟠 | v1 | Admin invoice list `Page` param is unbounded — int32 overflow can reach `Skip()` | `Controllers/AdminInvoicesController.cs:57` | verified | `11dfb8e` |
| PPW-480 | 🟠 | v1 | Admin "retry" resubmits byte-identical XML — can never fix the failure it exists for | `Services/Invoicing/InvoiceLifecycle.cs:106` | verified | `11dfb8e` |
| PPW-481 | 🟠 | v1 | `AnafSettings` docstring's "byte-identical to baseline" claim is false when disabled | `Configuration/AnafSettings.cs:7` | verified | `08e7746` |
| PPW-482 | 🟠 | v1 | `AdminInvoicesController`'s audit-logging doc-comment is false; the one logged action omits the admin id | `Controllers/AdminInvoicesController.cs:14` | verified | `11dfb8e` |
| PPW-483 | 🟠 | v1 | Redundant Order re-query on every paid webhook in `InvoiceCreationService` | `Services/Invoicing/InvoiceCreationService.cs:49` | verified | `08e7746` |
| PPW-484 | 🟠 | v1 | `InvoiceUploadJob` worker reloads the full Order graph even when only the ANAF step remains | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:246` | verified | `11dfb8e` |
| PPW-485 | 🟠 | v1 | Checkout field-length caps are wider than the legal XML limits, with no truncation | `Validators/Payments/CreateOrderRequestValidator.cs:61` | verified | `11dfb8e` |
| PPW-486 | 🟠 | v1 | Per-row catch collapses auth failure, network failure, and code bugs into one generic log event | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:91` | verified | `11dfb8e` |
| PPW-487 | 🟠 | v1 | Unrecognized ANAF status string is silently treated as "still processing", raw value never logged | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:232` | verified | `11dfb8e` |
| PPW-488 | 🟠 | v1 | No domain-tagged log for "customer charged, order not committed" in `WebhooksController` | `Controllers/WebhooksController.cs:205` | verified | `11dfb8e` |
| PPW-489 | 🟠 | v1 | Polly retries the non-idempotent ANAF upload POST on ambiguous-outcome errors | `Services/Invoicing/Anaf/AnafResilienceHandler.cs:33` | wont-fix | `11dfb8e` |
| PPW-490 | 🟠 | v1 | SQLite invoice numbering's MAX+1 has no transaction/lock despite the comment's safety claim | `Services/Invoicing/SqliteInvoiceNumberingService.cs:41` | verified | `11dfb8e` |
| PPW-491 | 🟠 | v1 | `InvoiceUploadJob` has zero tests despite being the most stateful new logic | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:1` | verified | `11dfb8e` |
| PPW-492 | 🟠 | v1 | Webhook tests stub invoice creation to always return null; nothing asserts it runs or that failure is handled | `Tests/Unit/Controllers/WebhooksControllerMetricsTests.cs:58` | verified | `11dfb8e` |
| PPW-493 | 🟠 | v1 | `PostgresInvoiceNumberingService` — the only prod numbering path — has no test coverage | `Services/Invoicing/PostgresInvoiceNumberingService.cs:1` | fixed | `11dfb8e` |
| PPW-494 | 🟡 | v1 | Cloned retry `HttpRequestMessage` in `AnafAuthHandler` is never disposed | `Services/Invoicing/Anaf/AnafAuthHandler.cs:43` | backlog | `e724528` |
| PPW-495 | 🟡 | v1 | `status=""` is rejected by the query validator but treated as "no filter" by the controller | `Validators/Invoices/AdminInvoiceListQueryValidator.cs:19` | backlog | `e724528` |
| PPW-496 | 🟡 | v1 | No backfill path for orders already Paid before this deploy | `Controllers/AdminInvoicesController.cs:1` | backlog | `e724528` |
| PPW-497 | 🟡 | v1 | Discovery manifest omitted ~24 changed files, including the VAT math itself | `Services/VatCalculator.cs:1` | backlog | `e724528` |
| PPW-498 | ⚪ | v1 | Polly retry pipeline in `AnafResilienceHandler` never disposes intermediate failed responses | `Services/Invoicing/Anaf/AnafResilienceHandler.cs:33` | backlog | `e724528` |
| PPW-499 | ⚪ | v1 | `AnafAuthHandler.CloneAsync` duplicates `SamedayAuthHandler`'s request-cloning logic verbatim | `Services/Invoicing/Anaf/AnafAuthHandler.cs:61` | backlog | `e724528` |
| PPW-500 | ⚪ | v1 | Response-status classification duplicated between `AnafSpvClient.UploadAsync` and `GetStatusAsync` | `Services/Invoicing/Anaf/AnafSpvClient.cs:59` | backlog | `e724528` |
| PPW-501 | ⚪ | v1 | Buyer-name fallback logic duplicated between `InvoiceXmlBuilder` and the PDF renderer | `Services/Invoicing/InvoiceXmlBuilder.cs:104` | backlog | `e724528` |
| PPW-502 | ⚪ | v1 | Invoice entity config uses a literal `"Sqlite"` string instead of the `DbProviders.Sqlite` constant | `Data/PhotoPrintDbContext.cs:428` | backlog | `e724528` |
| PPW-503 | ⚪ | v1 | `PostgresInvoiceNumberingService` interpolates the sequence name into raw SQL with no in-service validation | `Services/Invoicing/PostgresInvoiceNumberingService.cs:43` | backlog | `e724528` |
| PPW-504 | ⚪ | v1 | `OrderDetailDto` grew 3 required fields with no lens covering the frontend contract | `DTOs/Orders/OrderDetailDto.cs:5` | backlog | `e724528` |
| PPW-505 | 🟡 | v1 | Fiscal-year numbering constraint can disagree between Postgres and .NET at a Dec 31/Jan 1 boundary | `Migrations/20260603101910_AddVatAndInvoices.cs:111` | backlog | `e724528` |
| PPW-506 | 🟠 | v2 | Config comment and rollout runbook still promise the customer invoice email that does not exist | `docs/DEPLOYMENT.md:1409` | verified | `08e7746` |
| PPW-507 | 🟡 | v2 | New `Anaf:ClaimTtlMinutes` knob has no config default entry and no deployment documentation | `Configuration/AnafSettings.cs:44` | verified | `08e7746` |
| PPW-508 | 🟡 | v2 | Exhausted invoice-number retries now answer the payment processor 200 and count as `duplicate` | `Controllers/WebhooksController.cs:414` | verified | `07b0c1b` |
| PPW-509 | 🟡 | v3 | `CustomerEmailAttachmentSettings` docstring still says the XML, ANAF and PDF pipeline runs unconditionally | `Configuration/InvoicingSettings.cs:18` | open | `08e7746` |
| PPW-510 | 🟡 | v3 | ADR-022 left stale while the deployment guide and the decision index send an operator to it as current authority | `docs/DEPLOYMENT.md:1309` | open | `08e7746` |
| PPW-511 | 🟡 | v5 | EuPlatesc coverage waived twice on a removal that no work item tracks, against a standard that forbids the divergence | `memory-bank/standards/definition-of-done.md:52` | open | `07b0c1b` |
| PPW-512 | 🔴 | v6 | Easybox orders emit e-Factura with empty mandatory buyer address elements | `Services/Invoicing/InvoiceXmlBuilder.cs:121` | verified | `2979ea0` |
| PPW-513 | 🔴 | v6 | uq_invoices_series_year_number index expression is not IMMUTABLE, so Postgres aborts Migrate() at prod boot | `Migrations/20260603101910_AddVatAndInvoices.cs:112` | verified | `2979ea0` |
| PPW-514 | 🔴 | v6 | Exhausted-retry rollback reload discards the processor transaction id and the Error log omits it | `Controllers/WebhooksController.cs:427` | verified | `2979ea0` |
| PPW-515 | 🔴 | v6 | ANAF client-side timeout escapes as OperationCanceledException and stops the upload worker, unreachable by tests | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:81` | verified | `2979ea0` |
| PPW-516 | 🟠 | v6 | Exhausted invoice-number retry answers the payment processor 200, killing its last retry | `Controllers/WebhooksController.cs:304` | deferred | `2979ea0` |
| PPW-517 | 🟠 | v6 | Invoice PDF tier is chosen from the live CloudEnabled flag with no per-row StorageLocation, so a Provider flip orphans stored PDFs | `Controllers/InvoicesController.cs:68` | verified | `2979ea0` |
| PPW-518 | 🟠 | v6 | Admin manual mark-Paid inserts an Invoice with none of the webhook path's unique-violation protections, and its creation is fully mocked in tests | `Services/AdminOrderService.cs:148` | verified | `2979ea0` |
| PPW-519 | 🟠 | v6 | RetryAsync wipes XmlPayload, destroying the submitted-XML snapshot and diverging it from the kept PDF | `Services/Invoicing/InvoiceLifecycle.cs:120` | disputed | `2979ea0` |
| PPW-520 | 🟠 | v6 | Per-line PriceAmount x InvoicedQuantity no longer equals LineExtensionAmount, and nothing asserts it | `Services/Invoicing/InvoiceXmlBuilder.cs:219` | deferred | `2979ea0` |
| PPW-521 | 🟠 | v6 | InvoiceAddressFormatter.Truncate throws NRE on a null City/Street that the validators accept | `Services/Invoicing/InvoiceAddressFormatter.cs:12` | verified | `2979ea0` |
| PPW-522 | 🟠 | v6 | Unbuildable invoice stays Pending forever and starves the upload batch | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:206` | verified | `2979ea0` |
| PPW-523 | 🟠 | v6 | Missing invoice PDF blob surfaces as an unlogged generic 500 with no distinct event | `Controllers/InvoicesController.cs:69` | verified | `2979ea0` |
| PPW-524 | 🟠 | v6 | The whole invoicing feature has no SPA consumer and no lens covered the frontend | `Controllers/InvoicesController.cs:1` | deferred | `2979ea0` |
| PPW-525 | 🟠 | v6 | Guest invoice access is defeated by the unchanged guest-session lifetime and the never-implemented order transfer | `Controllers/InvoicesController.cs:52` | deferred | `2979ea0` |
| PPW-526 | 🟠 | v6 | EuPlatesc paid leg's new three-state outcome and its rollback have no endpoint-driven test | `Controllers/WebhooksController.cs:205` | wont-fix | `2979ea0` |
| PPW-527 | 🟠 | v6 | Only the classified exhaust path is metric-safe; other invoice-creation failures still escape RecordPaymentWebhook | `Controllers/WebhooksController.cs:390` | verified | `2979ea0` |
| PPW-528 | 🟠 | v6 | Charged-but-unpaid order emits the same metric label as a routine card decline | `Controllers/WebhooksController.cs:389` | deferred | `2979ea0` |
| PPW-529 | 🟠 | v6 | No test applies the migration chain — the unique-index DDL is only ever proven via EnsureCreated from the model | `Tests/Integration/PostgresInvoiceNumberingServiceIntegrationTests.cs:79` | verified | `2979ea0` |
| PPW-530 | 🟠 | v6 | MakeInvoiceOrderIdUnique creates the unique index with no dedupe step, so duplicate rows fail prod boot | `Migrations/20260813093709_MakeInvoiceOrderIdUnique.cs:17` | false-positive | `2979ea0` |
| PPW-531 | 🟠 | v6 | Unique-violation classifiers cover only 2 of 3 Invoices unique indexes and their Npgsql arms are untested | `Controllers/WebhooksController.cs:460` | verified | `2979ea0` |
| PPW-532 | 🟠 | v6 | One ANAF credential failure fans out into up to 50 Error logs and Sentry captures per tick | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:85` | verified | `2979ea0` |
| PPW-533 | 🟠 | v6 | ANAF auth failures leave LastError blank, so the admin invoice list shows no reason | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:82` | verified | `2979ea0` |
| PPW-534 | 🟠 | v6 | invoice.creation.allocated is logged pre-commit on every retry attempt, so logs show phantom invoice numbers | `Services/Invoicing/InvoiceCreationService.cs:96` | verified | `2979ea0` |
| PPW-535 | 🟡 | v6 | Truncate can split a UTF-16 surrogate pair, wedging the invoice in Pending forever | `Services/Invoicing/InvoiceAddressFormatter.cs:13` | verified | `2979ea0` |
| PPW-536 | 🟡 | v6 | RetryAsync resets every ANAF field except ClaimedAt, which the success path never releases either | `Services/Invoicing/InvoiceLifecycle.cs:117` | backlog | `1c217f4` |
| PPW-537 | 🟡 | v6 | Residual reconciliation is unguarded — negative line amount, silently absorbed snapshot mismatch, crash on an empty line list | `Services/Invoicing/InvoiceXmlBuilder.cs:213` | backlog | `1c217f4` |
| PPW-538 | 🟡 | v6 | Upload batch query ignores ClaimedAt, unlike the existing AWB claim precedent | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:65` | backlog | `1c217f4` |
| PPW-539 | 🟡 | v6 | New ClaimedAt column and unique index never land on an existing dev SQLite database | `Program.cs:358` | backlog | `1c217f4` |
| PPW-540 | 🟡 | v6 | Postgres numbering tests draw a random year and assert absolute sequence values, so they collide and leak sequences | `Tests/Integration/PostgresInvoiceNumberingServiceIntegrationTests.cs:16` | backlog | `1c217f4` |
| PPW-541 | 🟡 | v6 | claim-lost log asserts "another worker" for causes it cannot distinguish | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:141` | backlog | `1c217f4` |
| PPW-542 | 🟡 | v6 | submitted-but-not-recorded logs Error twice and gets no Sentry capture | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:226` | backlog | `1c217f4` |
| PPW-543 | 🟡 | v6 | LastError is persisted before the exception is logged, so a DB blip loses the root cause | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:206` | backlog | `1c217f4` |
| PPW-544 | ⚪ | v6 | New Must rules have no WithMessage, so 400s carry English default messages | `Validators/Payments/CreateOrderRequestValidator.cs:32` | backlog | `1c217f4` |
| PPW-545 | ⚪ | v6 | CreateForOrderAsync(Guid) has no production caller left | `Services/Invoicing/IInvoiceCreationService.cs:24` | backlog | `1c217f4` |
| PPW-546 | ⚪ | v6 | Retry pre-read pulls the whole XmlPayload from the DB just to log its length | `Services/Invoicing/InvoiceLifecycle.cs:104` | backlog | `1c217f4` |
| PPW-547 | ⚪ | v6 | data-stack standard never mentions the Invoices table it must describe | `memory-bank/standards/data-stack.md:55` | backlog | `1c217f4` |
| PPW-548 | ⚪ | v6 | ADR-023/decision-index still credit CAS for multi-replica safety, now superseded by the ClaimedAt lease | `memory-bank/standards/decision-index.md:34` | backlog | `1c217f4` |
| PPW-549 | ⚪ | v6 | Unknown ANAF status warns twice and the job's line drops the diagnostic fields | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:309` | backlog | `1c217f4` |
| PPW-550 | 🟠 | v6 | InvoicesController's tier-mismatch fallback re-throws an unhandled FileNotFoundException on a double-tier blob miss | `Controllers/InvoicesController.cs:84-94` | verified | `0ec6497` |
| PPW-551 | 🟠 | v6 | ANAF credential-failure log/capture dedup resets every tick, unlike the sibling cross-tick outage window | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:92` | verified | `0ec6497` |
| PPW-552 | 🟡 | v6 | PPW-515's fix orphaned `AnafUnreachableException`'s XML doc comment | `Services/Invoicing/Anaf/AnafExceptions.cs:32-47` | backlog | `2979ea0` |
| PPW-553 | 🟠 | v7 | The 2 h ANAF auth-outage window has no floor tied to PollIntervalMinutes, so a validator-legal interval above it defeats the dedup | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:17` | open | `0ec6497` |
| PPW-554 | 🟡 | v7 | The bucket-versus-key miss-cause preference has no regression test | `Controllers/InvoicesController.cs:83-87` | backlog | `0ec6497` |

## Details

### PPW-469 — Invoice PDF retrieval bypasses IStorageRouter, always reads local disk

- **What:** `InvoicesController`/`InvoiceUploadJob` inject the unkeyed `IStorageService` directly — `StorageExtensions.cs` binds that unconditionally to the "local" adapter regardless of `Storage:Provider`. With S3 configured on a multi-replica deploy, a PDF written by one replica 404s/500s on another.
- **Evidence:** `Controllers/InvoicesController.cs:20-26,65`; `Extensions/StorageExtensions.cs:29,64,75-76`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs`.
- **Suggested fix:** Inject `IStorageRouter` in both files; route via `router.CloudEnabled ? router.Cloud : router.Local`, matching every other two-tier caller. **Test shape:** integration test with `Storage:Provider=S3`, seed `PdfStoragePath` only in the S3 fake; GET invoice; assert 404+Retry-After, not an unhandled 500. Not trigger-list-shaped (DI wiring only) — no approach-check run.
- **History:**
  - v1: found — raised independently by 2 lenses (correctness + completeness-critic)
  - v2: verified @`11dfb8e` — revert-and-rerun (router read forced back to `Local`): only `GetInvoiceAsync_CloudEnabled_ReadsFromCloudAdapterNotLocal` went red, 7 siblings green. No other site injects `IStorageService` directly

### PPW-470 — Invoice PDF generation/upload bypasses IStorageRouter, always writes local disk (ADR-008)

- **What:** Same root cause as PPW-469, write side: `InvoiceUploadJob.UploadPendingAsync` saves every PDF via the unkeyed `IStorageService` → local disk, contradicting ADR-008/`ddd-01-domain-model.md`'s "invoices never live in the local tier."
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:126,159`; `Controllers/InvoicesController.cs:22`; `memory-bank/bolts/039-efactura-anaf/ddd-01-domain-model.md:26`.
- **Suggested fix:** Same fix as PPW-469 — one `IStorageRouter` change covers both read and write sites; fix as one cluster. **Test shape:** with `Storage:Provider=S3`, assert PDF bytes land in `IStorageRouter.Cloud`, not `.Local`. Not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 2 lenses (requirements + quality) — same root cause as PPW-469, same fix cluster
  - v2: verified @`11dfb8e` — revert-and-rerun (job storage forced back to `Local`): only `UploadPendingAsync_CloudEnabled_SavesPdfToCloudAdapterNotLocal` went red, 12 siblings green

### PPW-471 — Invoice creation is check-then-act with no DB uniqueness — concurrent webhooks can mint two fiscal invoices

- **What:** `CreateForOrderAsync` checks for an existing Invoice then inserts, with no unique constraint on `Invoices.OrderId`. Two near-concurrent webhook deliveries (Stripe retry-on-timeout) can each pass the check and each submit a separate legal invoice to ANAF for one order.
- **Evidence:** `Services/Invoicing/InvoiceCreationService.cs:40`; migration `20260603101910_AddVatAndInvoices.cs` (OrderId is a non-unique index only).
- **Suggested fix:** Add a unique index on `Invoices.OrderId` (new EF migration; SQLite `EnsureCreated` gets it free, Postgres needs the migration plus a pre-flight check for existing duplicate rows before it can run safely at boot). The catch does **not** belong inside `CreateForOrderAsync` — that method never calls `SaveChangesAsync` (by design, per `IInvoiceCreationService.cs:20`, so the Invoice insert rides the caller's transaction). Catch the provider-specific unique-violation (mirror `OrderService.IsIdempotencyKeyViolation`) at both `WebhooksController` `SaveChangesAsync` call sites (EuPlatesc `:206`, Stripe `:286`), detach the failed entities, re-read the winner, and gate the four side effects (email, cloud-promotion enqueue, AWB-notify, broadcast) behind a replay signal so they don't fire twice. Same catch site and pattern as PPW-490 — fix together. **Test shape:** two concurrent webhook deliveries for one order; assert exactly one Invoice row and side effects fire once. Trigger-list-shaped (concurrency model change) — **approach-check: revised** (drafted "catch inside `CreateForOrderAsync`" is a no-op; corrected to the controller call sites, with a replay signal and a migration pre-flight step).
- **History:**
  - v1: found — raised independently by 3 lenses (correctness + security + race)
  - v1: approach-check run — revised (catch site corrected to WebhooksController; replay-signal and migration pre-flight added)
  - v2: verified @`11dfb8e` — revert-and-rerun (`.IsUnique()` dropped from the OrderId index): only `ConcurrentDeliveriesForSameOrder_LoserGetsClassifiableViolation_ExactlyOneInvoicePersists` went red. Two residuals: the approach-check's Postgres duplicate-row pre-flight before `Program.cs:387` `Migrate()` was never implemented (unreachable on this deploy — the Invoices table itself is new), and no test drives the controller's own OrderId-violation catch or asserts the four side effects fire once, which was the stated test shape

### PPW-472 — InvoicePdfReadyNotifier never sends an email regardless of the flag, logs a false "sent" event

- **What:** `NotifyAsync` logs `invoice.pdf-ready.sent` and returns — no `IEmailService` call exists. `OrderEmailService` has zero Invoice references either. Flipping `Invoicing:CustomerEmailAttachments:Enabled` to true (per the settings file's own instruction) delivers nothing to customers, while the log claims success.
- **Evidence:** `Services/Invoicing/InvoicePdfReadyNotifier.cs:30-55`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:130,166`.
- **Suggested fix:** Implement the two documented integration points from `ddd-02-technical-design.md` (attach PDF to the order-confirmation email; send a real follow-up email in `NotifyAsync`) — or, if not shipping this round, change the log line so it doesn't claim `sent` and flag the settings docstring as not-yet-implemented. **Test shape:** inject a mock `IEmailService`, flag=true, call `NotifyAsync`, assert a send was invoked. Not trigger-list-shaped as a doc/log-only interim fix; becomes trigger-list-shaped only if a real email integration ships this round (new external call, not a background job/cache/retry) — no approach-check run yet, pending the owner's choice of scope.
- **History:**
  - v1: found — raised independently by 2 lenses (requirements + observability)
  - v2: verified @`11dfb8e` — revert-and-rerun on the notifier: only `When_flag_enabled_notifier_does_not_claim_it_sent_anything` went red. The code leg holds; the identical false promise survives in the config and deployment docs — PPW-506

### PPW-473 — Guest checkouts can never retrieve their invoice — JWT-only auth on the endpoint

- **What:** `GET /api/orders/{orderId}/invoice` uses plain `[Authorize]` (JWT-only) + `GetUserIdOrNull()`, which returns null for guest-token requests — every guest gets 401, always. Every other order-scoped endpoint (Cart/Payments/Uploads) uses the dual-auth policy plus a guest-session ownership check.
- **Evidence:** `Controllers/InvoicesController.cs:16,41-51`; `Extensions/ClaimsPrincipalExtensions.cs:9-17`; `Extensions/GuestSessionExtensions.cs:10-28`.
- **Suggested fix:** Switch to `[Authorize(Policy = GuestSessionExtensions.DualAuthPolicy)]` and check ownership against both `UserId` and `GuestSessionId`, matching `CartController`/`PaymentsController`. **Test shape:** guest order (UserId=null, GuestSessionId=X), GET invoice with `X-Guest-Token` and no JWT → expect 200/404-pending, not 401. Not trigger-list-shaped (reusing an existing auth pattern).
- **History:**
  - v1: found — raised independently by 1 lens (requirements)
  - v2: verified @`11dfb8e` — revert-and-rerun: `GetInvoiceAsync_GuestOwnsOrder_ReturnsFile` and `GetInvoiceAsync_GuestSessionDoesNotMatch_ReturnsForbid` went red, 6 siblings green. Residual: the `DualAuthPolicy` attribute itself is exercised by no test — the unit tests bypass authorization

### PPW-474 — Orders marked Paid via admin manual reconciliation never get an Invoice row

- **What:** `AdminOrderService.UpdateStatusAsync`'s Paid branch stamps `PaidAt` and fires confirmation email/AWB notify but never calls `IInvoiceCreationService.CreateForOrderAsync` — only the two webhook handlers do. Every offline/bank-transfer-reconciled order permanently lacks a legally-required invoice.
- **Evidence:** `Services/AdminOrderService.cs:19-54,139-160`; `Controllers/WebhooksController.cs:205,285`.
- **Suggested fix:** Call `CreateForOrderAsync(order.Id, ct)` inside the same Paid branch, before `SaveChangesAsync`, mirroring the webhook handlers. **Test shape:** `UpdateStatusAsync(orderId, "Paid", ...)` with a mocked `IInvoiceCreationService`; assert it's called once. Not trigger-list-shaped (mirrors an existing call pattern).
- **History:**
  - v1: found — raised independently by 1 lens (requirements)
  - v2: verified @`11dfb8e` — revert-and-rerun (creation call deleted): only `UpdateStatusAsync_AwaitingPaymentToPaid_StampsPaidAtAndEnqueuesAwb` went red, 34 siblings green. Residual: this sibling save site has no unique-violation catch, unlike the webhook path

### PPW-475 — ANAF upload success + DB commit failure is indistinguishable from never-uploaded

- **What:** If `anafClient.UploadAsync` succeeds but `lifecycle.MarkSubmittedAsync` then throws (DB blip), the exception isn't caught locally, falls to the generic per-row catch, and logs the same event as any other failure. The invoice stays Pending and gets re-uploaded next tick — a real second POST to ANAF, invisible in logs.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:170-191`; `Services/Invoicing/InvoiceLifecycle.cs:29-42`.
- **Suggested fix:** Add a catch scoped to just the `MarkSubmittedAsync` call, logging the already-obtained `AnafUploadId` at a distinct event name before rethrowing. No durable side-channel needed — `ddd-02-technical-design.md` already documents "ANAF dedupes via InvoiceNumber" as the accepted tolerance for this class of duplicate. Needs a regression test — no test file for `InvoiceUploadJob` exists at all today. **Test shape:** fake `UploadAsync` succeeds, fake `MarkSubmittedAsync` throws; assert the distinct log event fires and the invoice stays retryable. Trigger-list-shaped (new catch/mapping layer) — **approach-check: revised** (scope confirmed to just this one call; skip durable side-channel as over-engineering; add the missing test).
- **History:**
  - v1: found — raised independently by 1 lens (observability)
  - v1: approach-check run — revised (catch scoped correctly; regression test required)
  - v2: verified @`11dfb8e` — revert-and-rerun: only `UploadPendingAsync_AnafSucceedsButMarkSubmittedFails_LogsDistinctlyAndRethrows` went red, 12 siblings green

### PPW-476 — No claim/lease on Pending invoices — multi-replica double-submits to ANAF and double-emails

- **What:** Two replicas' `InvoiceUploadJob` can poll the same tick, both pick invoice X, and both proceed through XML build, PDF render, customer notify, and ANAF upload before the final CAS picks a winner — a real duplicate customer email and duplicate ANAF submission. The sibling `AwbCreator` job had this identical race, fixed via a durable per-order claim (ADR-015 amendment) — never ported here.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:71-190`; `Services/Invoicing/InvoiceLifecycle.cs:29-42`; `memory-bank/bolts/037-awb-and-tracking-jobs/adr-015-...md`.
- **Suggested fix:** Add a `ClaimedAt`/lease column (mirror `Order.AwbClaimedAt`), atomic `ExecuteUpdateAsync` claim before the pipeline starts. Must also: release the claim on a retryable-but-not-billed outcome (the AWB precedent does this; the draft initially missed it); size a sensible `Anaf:ClaimTtlMinutes` past the whole pipeline duration, not copied from AWB's 5 minutes; new Postgres migration — watch the known SQLite-`EnsureCreated`-scaffolds-`INTEGER`-via-Unix-ms-converter gotcha that needs hand-editing to `timestamptz` for Postgres (same trap as `20260728060537_AddOrderAwbClaimedAt.cs`); test against SQLite, not EF InMemory (`ExecuteUpdateAsync` isn't supported there). Note as a stated residual, not fixed here: ANAF itself is sent no idempotency key (unlike Sameday's `clientInternalReference`), so a crash-after-POST-before-claim-release window still risks a genuine duplicate submission — an ADR amendment should say so explicitly. Trigger-list-shaped (concurrency model) — **approach-check: revised** (release-on-retry, explicit TTL, migration type gotcha, SQLite test, residual noted).
- **History:**
  - v1: found — raised independently by 1 lens (race)
  - v1: approach-check run — revised (claim-release, TTL sizing, migration gotcha, test fixture, residual note added)
  - v2: verified @`11dfb8e` — revert-and-rerun (claim-lost guard deleted): only `UploadPendingAsync_RowAlreadyClaimedWithinTtl_SkipsWithoutCallingAnaf` went red. TTL is sized right (10 min against a worst-case pass of roughly 2 min of ANAF retries) and `anaf.upload-job.claim-lost` is the signal, but the new key is absent from config and deployment docs — PPW-507

### PPW-477 — No control-character filtering on customer name/address before UBL XML serialization

- **What:** Neither `CreateOrderRequestValidator` nor the account name validators restrict charset on name/address fields. A verification check disproved the originally-claimed mechanism (`XmlTextWriter` does not throw on a control character — it silently emits a malformed character reference); the real risk is a malformed reference silently reaching a legally-binding e-Factura XML, and — confirmed separately — `AnafSpvClient.UploadAsync` maps any hard ANAF rejection (e.g. of malformed XML) to `AnafUnreachableException`, which is *also* uncaught locally and falls into the same silent outer catch with no `LastError` set: the "stuck Pending, invisible" failure mode is real via this second path even though the original mechanism was wrong.
- **Evidence:** `Validators/Payments/CreateOrderRequestValidator.cs:32-61`; `Validators/Account/RegisterRequestValidator.cs`/`UpdateAccountValidator.cs` (FirstName/LastName — the actual primary buyer-name source, same gap); `Services/Invoicing/Anaf/AnafSpvClient.cs:62-66`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:142-151,85-95`.
- **Suggested fix:** Reject (not silently strip) XML-1.0-invalid control characters at checkout/account input time, on **both** the shipping-address validators and the account name validators (`FirstName`/`LastName` — the primary path for logged-in buyers; `RecipientName` is guest/fallback-only). Add a defensive sanitize-and-flag net inside `InvoiceXmlBuilder` **and** `InvoicePdfDocument` (both independently re-derive buyer name) for pre-existing/legacy rows input rejection can't retroactively fix. Independently: wrap Steps 1–2 of `InvoiceUploadJob.UploadPendingAsync` in a local catch that calls `RecordPendingErrorAsync`, and add `AnafUnreachableException` to Step 3's local catch — both close the same "invisible stuck Pending" gap this finding was chasing. Trigger-list-shaped (adds a catch/mapping layer) — **approach-check: revised** (extend to account validators; reject not strip; add the PDF-side defensive net; also fix the two uncaught exception paths named above).
- **History:**
  - v1: found — raised independently by 1 lens (input-validation), not independently confirmed: guard evidence real, originally-claimed crash mechanism disproven by trace, corrected mechanism confirmed by approach-check
  - v1: approach-check run — revised (scope widened per above)
  - v2: verified @`11dfb8e` — two legs, both red on revert: neutralising `TextValidation.HasNoXmlInvalidChars` reddened 7 validator tests; disarming the Step 1-2 and `AnafUnreachableException` catches reddened `UploadPendingAsync_XmlBuildThrows_...` and `UploadPendingAsync_AnafUnreachable_...`. Residual: the approach-check also required a sanitize net inside `InvoiceXmlBuilder` and `InvoicePdfDocument` for legacy rows — neither got one

### PPW-478 — UBL invoice-line amounts are gross, not tax-exclusive — lines won't reconcile with the document total

- **What:** `OrderService.cs` sets `OrderItem.LineTotalRon`/`UnitPriceRon` from the gross (VAT-inclusive) listed price — the same value later fed into `VatCalculator.ExtractBreakdown` to derive the header's correctly-net `NetTotalRon`. But `InvoiceXmlBuilder.BuildLine` writes those same gross `OrderItem` values straight into UBL's `LineExtensionAmount`/`Price/PriceAmount`, which per UBL/CIUS-RO must be tax-exclusive. Σ(line amounts) will not equal the document's net total — an internally inconsistent legal e-invoice, found via a supplemental correctness pass dispatched to close the coverage gap PPW-497 named (the pass's original 10 lenses never checked this).
- **Evidence:** `Services/OrderService.cs:85-96,145`; `Services/Invoicing/InvoiceXmlBuilder.cs:193-244`; confirmed no existing test asserts line-vs-header reconciliation (`Tests/Unit/Services/Invoicing/InvoiceXmlBuilderTests.cs`).
- **Suggested fix:** In `BuildInvoiceLines` (not `BuildLine` — the residual-rounding adjustment needs all lines materialized first), derive each line's **net line total** via `VatCalculator` from the gross `LineTotalRon`, then derive net **unit price** as `netLineTotal / quantity` — never via an independent extraction on `UnitPriceRon` (that drifts from the line-total extraction whenever `Quantity > 1`, confirmed against the existing test fixture itself: 7×3=21 independently extracts to 5.88×3=17.64 vs 21→17.65). Per-line independent VAT extraction also drifts from the aggregate header extraction by rounding (confirmed: three 10.01-RON lines sum to 25.23 net independently vs 25.24 from the aggregate) — apply the residual to one line's net total so Σ(lines) reconciles exactly with `invoice.NetTotalRon`. Give the synthetic shipping line the same treatment (trivial, quantity always 1). **Test shape:** a new multi-item fixture (the current single-item-plus-shipping fixture coincidentally hides the drift) asserting raw `LineExtensionAmount`/`PriceAmount` values are net and Σ(lines) == header net total. Trigger-list-shaped (money-field semantics change) — **approach-check: revised** (computation must move up to `BuildInvoiceLines`; unit price derived from the reconciled line total, not independently; new test fixture required).
- **History:**
  - v1: found by a supplemental correctness pass, dispatched this pass to close PPW-497's gap — checked directly against the code, not by a manifest lens
  - v1: approach-check run — revised (derivation site and rounding-consistency corrected)
  - v2: verified @`11dfb8e` — revert-and-rerun: `Line_extension_amount_is_net_not_gross` and `Sum_of_line_extension_amounts_reconciles_exactly_with_header_net_total` went red, 11 siblings green

### PPW-479 — Admin invoice list `Page` param is unbounded — int32 overflow can reach `Skip()`

- **What:** `Page` has no upper bound (`Size` is capped [1,100]). `(Page-1)*Size` in unchecked int32 overflows at `page≈2^31`, wrapping negative; `Skip(negative)` reaches the DB provider unguarded, surfacing as an unhandled 500.
- **Evidence:** `Controllers/AdminInvoicesController.cs:57`; `Validators/Invoices/AdminInvoiceListQueryValidator.cs:11-17`.
- **Suggested fix:** Add an upper bound on `Page`, or compute the offset in `long`/checked arithmetic and clamp before `Skip`. **Test shape:** `GET ?page=2147483647&size=100` → expect 422, not 500. Not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (correctness)
  - v2: verified @`11dfb8e` — revert-and-rerun: only `PageNearIntMax_FailsValidation` went red

### PPW-480 — Admin "retry" resubmits byte-identical XML — can never fix the failure it exists for

- **What:** `RetryAsync` clears `AnafUploadId`/`LastError` but leaves `XmlPayload` untouched; the worker's Step 1 skips rebuilding XML whenever it's already set, so a rejected invoice resubmits identically and fails identically forever.
- **Evidence:** `Services/Invoicing/InvoiceLifecycle.cs:98-111`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:142-151`.
- **Suggested fix:** Clear `Invoice.XmlPayload` in `RetryAsync`'s existing atomic update so the next tick rebuilds it. **Do not** clear `PdfStoragePath` — it plays no role in the ANAF path, the key is stable and gets overwritten in place anyway (no orphan risk), and clearing it risks a duplicate customer "invoice ready" notification once the email flag is enabled (Step 2 unconditionally re-notifies on every render). Log the pre-retry `XmlPayload`/`LastError` before clearing so `GET /xml`'s stated "inspect what ANAF rejected" purpose still works. Note in the docs/UI that this only helps when Seller config or code was fixed since the original build — there's no admin tool to edit Order data, so a rejection caused by bad order data will resubmit identically either way. Trigger-list-shaped (changes retry semantics) — **approach-check: revised** (drop the `PdfStoragePath` clear; add pre-clear logging).
- **History:**
  - v1: found — raised independently by 1 lens (requirements)
  - v1: approach-check run — revised (PdfStoragePath clear dropped; logging added)
  - v2: verified @`11dfb8e` — revert-and-rerun: both `Retry_from_terminal_state_resets_to_Pending_and_clears_fields` cases went red, 7 siblings green

### PPW-481 — `AnafSettings` docstring's "byte-identical to baseline" claim is false when disabled

- **What:** With `Anaf:Enabled=false` (default), `InvoiceCreationService`/`InvoiceLifecycle`/`InvoicesController` are wired unconditionally — a paid order still gets an Invoice DB row and the new customer endpoint returns a permanent 404+Retry-After. Neither effect is byte-identical to the pre-integration baseline, contrary to the docstring.
- **Evidence:** `Configuration/AnafSettings.cs:4-9`; `Program.cs:281-323`.
- **Suggested fix:** Update the docstring to state plainly that the Invoice row and the customer endpoint are also live while `Enabled=false` — only the ANAF wire calls are skipped. Doc-only; not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (requirements)
  - v2: REOPENED @`11dfb8e` — the replacement docstring is false in a new way. It claims the "XML/PDF build" stays live while `Anaf:Enabled=false`, but `InvoiceUploadJob` is the only caller of `IInvoiceXmlBuilder`/`IInvoicePdfRenderer` and is registered only inside `Program.cs:299` `if (anafEnabled)`. The original false claim also survives verbatim at `appsettings.json:95` ("production-identical to baseline")
  - v2: fix round — docstring now states the real behaviour: Invoice row created, no XML or PDF built, download endpoint always 404s. Swept the same claim out of appsettings.json, the flag table, the validator docstring and the pre-flight step
  - v3: verified @`08e7746` — every clause checked against code, not against the resolution. `InvoiceUploadJob` is the only caller of `IInvoiceXmlBuilder`/`IInvoicePdfRenderer` and is registered only inside `Program.cs:299`; `InvoicesController.GetInvoiceAsync` returns 404 + `Retry-After` whenever `PdfStoragePath` is empty, which it always is with the flag off. Class check found one site the sweep missed — PPW-509

### PPW-482 — `AdminInvoicesController`'s audit-logging doc-comment is false; the one logged action omits the admin id

- **What:** The class doc-comment claims all operations are audit-logged with the admin's id. `ListAsync`/`GetXmlAsync` log nothing; `RetryAsync` logs invoice_id/from-status only, no admin identity.
- **Evidence:** `Controllers/AdminInvoicesController.cs:12-16,39-80,91-133,140-155`.
- **Suggested fix:** Add `User.GetUserIdOrNull()`-tagged Information logs on all three actions (pattern already used in `InvoicesController`). Not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 2 lenses (requirements + observability)
  - v2: verified @`11dfb8e` — revert-and-rerun: all three of `ListAsync_LogsAdminUserId`, `GetXmlAsync_LogsAdminUserId`, `RetryAsync_LogsAdminUserId` went red

### PPW-483 — Redundant Order re-query on every paid webhook in `InvoiceCreationService`

- **What:** `WebhooksController` already loads and mutates `order` on the shared scoped DbContext; `CreateForOrderAsync` re-queries it by id — an avoidable extra SQL round trip on every successful payment.
- **Evidence:** `Services/Invoicing/InvoiceCreationService.cs:49`; `Controllers/WebhooksController.cs:169,205,261,285`.
- **Suggested fix:** Overload `CreateForOrderAsync` to accept the already-loaded `Order`. Not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (quality)
  - v2: REOPENED @`11dfb8e` — the regression test cannot go red for the defect it names. Reintroducing the re-query (the `Order` overload delegating to the `Guid` overload) left all 6 `InvoiceCreationServiceTests` green: `Order_overload_creates_invoice_without_reloading_the_order` asserts only that an invoice is created, never that the Orders table went unqueried. PPW-484 used SQL logging for the same claim
  - v2: fix round — test now captures EF SQL and asserts no `FROM "Orders"` query. Proven red by reintroducing the delegation the verification pass used, then green
  - v3: verified @`08e7746` — revert-and-rerun (Order overload delegating to the Guid overload): only `Order_overload_creates_invoice_without_reloading_the_order` went red, 29 siblings green. Class check clean — both production callers (`WebhooksController.cs:397`, `AdminOrderService.cs:148`) use the Order overload; only tests reach the Guid one

### PPW-484 — `InvoiceUploadJob` worker reloads the full Order graph even when only the ANAF step remains

- **What:** `LoadPairAsync` always does `Order.Include(Items).Include(User)` even when only Step 3 (ANAF upload, which never touches `order`) remains — wasted joins on every tick during an ANAF outage.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:132,246-257`.
- **Suggested fix:** Only load Order with includes when Steps 1–2 still need it. Not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (quality)
  - v2: verified @`11dfb8e` — revert-and-rerun (`needsOrder` forced true): only `UploadPendingAsync_XmlAndPdfAlreadyBuilt_SkipsOrderReloadAndProceedsToUpload` went red, 12 siblings green

### PPW-485 — Checkout field-length caps are wider than the legal XML limits, with no truncation

- **What:** `RecipientName`/`Street`/`AddressLine` allow up to 255-400 chars with nothing truncating or validating against CIUS-RO field-length limits before `InvoiceXmlBuilder` embeds them — an oversized-but-valid address becomes an unfixable ANAF rejection (no edit path, cached XML resubmits identically forever, same mechanism as PPW-480).
- **Evidence:** `Validators/Payments/CreateOrderRequestValidator.cs:49-61`; `Services/Invoicing/InvoiceXmlBuilder.cs:121-124`; `Services/Invoicing/Anaf/AnafSpvClient.cs:77-84`.
- **Suggested fix:** Cap/truncate to CIUS-RO limits before XML build, or validate at checkout so bad data never reaches an unfixable Paid+Invoice state. Not trigger-list-shaped (validation only).
- **History:**
  - v1: found — raised independently by 1 lens (input-validation)
  - v2: verified @`11dfb8e` — revert-and-rerun (truncation neutralised, caps restored): 6 tests went red across the validator and the XML builder, 33 siblings green

### PPW-486 — Per-row catch collapses auth failure, network failure, and code bugs into one generic log event

- **What:** `AnafAuthException` (e.g. an expiring client cert — urgent) propagates past the local catch into the same generic `anaf.upload-job.row-failed` as a self-healing transient error or a code bug, with no field distinguishing them.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:91,182-190`; `Services/Invoicing/Anaf/AnafSpvClient.cs:60,116`.
- **Suggested fix:** Add a dedicated catch for `AnafAuthException` at a distinct event name/severity (precedent: `AwbDispatcher.HandleOutcomeAsync`'s non-transient-vs-retry-scheduled split). Drop "escalates on repeat" — no per-replica-safe counter exists, and ADR-024 already rejected a persisted attempt counter for this exact subsystem; treat as urgent on first sight instead. A log-only change isn't sufficient: standalone `LogError` never reaches Sentry by this project's own design (`writeToProviders=false`) and no metric increments on this path — add an explicit `IHub.CaptureException` call in the new catch so the signal actually reaches an alerting channel. Trigger-list-shaped (new catch/mapping layer) — **approach-check: revised** (drop repeat-escalation; add explicit Sentry capture).
- **History:**
  - v1: found — raised independently by 1 lens (observability)
  - v1: approach-check run — revised (repeat-escalation dropped; Sentry capture added)
  - v2: verified @`11dfb8e` — revert-and-rerun: only `ProcessBatchAsync_AnafAuthFails_LogsDistinctlyAndCapturesToSentry` went red. Residual: `IHub` is registered only when `Sentry:Enabled=true`, so with Sentry off the capture no-ops and no metric backs it

### PPW-487 — Unrecognized ANAF status string is silently treated as "still processing", raw value never logged

- **What:** `MapStatus`'s `Unknown` default discards the raw ANAF `stare` string; `PollSubmittedAsync` groups `Unknown` with `InProgress` — no log, no metric, no operator signal if ANAF returns a status value the client doesn't recognize.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:232-235`; `Services/Invoicing/Anaf/AnafSpvClient.cs:127-155`.
- **Suggested fix:** Log the raw `stare` value at Warning when `MapStatus` can't classify it; log `Unknown` distinctly from `InProgress` in the job. Not trigger-list-shaped (log line + switch-branch differentiation, no new catch/retry/job).
- **History:**
  - v1: found — raised independently by 1 lens (observability)
  - v2: verified @`11dfb8e` — revert-and-rerun: `GetStatus_unrecognized_stare_logs_the_raw_value_at_warning` and `PollSubmittedAsync_UnrecognizedStatus_LogsDistinctlyFromInProgressAndDoesNotTransition` went red, 21 siblings green

### PPW-488 — No domain-tagged log for "customer charged, order not committed" in `WebhooksController`

- **What:** Two concurrent webhook deliveries can both pass the pre-commit check; the loser's unique-violation `DbUpdateException` hits the generic exception-handler log, unlike the deliberate "manual reconciliation required" logging this same file already uses for adjacent scenarios.
- **Evidence:** `Controllers/WebhooksController.cs:196-217,234-236,300-302`; `Services/Invoicing/SqliteInvoiceNumberingService.cs:41-48`.
- **Suggested fix:** Wrap the span in a catch scoped to `DbUpdateException` specifically — not bare `catch(Exception)`, which would also mislabel a client-disconnect `OperationCanceledException` as a payment incident. Log the order's status captured **before** calling `OrderStatusMachine.Transition` (`order.Status` is mutated in-memory before `SaveChangesAsync`; logging it after a rollback would show "Paid" for an order that's actually still `AwaitingPayment`). Trigger-list-shaped (new catch/mapping layer) — **approach-check: revised** (narrow the catch type; fix the stale-status logging bug).
- **History:**
  - v1: found — raised independently by 1 lens (observability)
  - v1: approach-check run — revised (catch type narrowed; pre-transition status snapshot added)
  - v2: verified @`11dfb8e` — revert-and-rerun (exhausted catch disarmed): only `SaveOrderPaidWithInvoiceAsync_InvoiceNumberCollisionExhaustsRetries_LogsManualReconciliationAndReturnsFalse` went red. The catch also swallows the exception, which changes the webhook's answer to the processor — PPW-508

### PPW-489 — Polly retries the non-idempotent ANAF upload POST on ambiguous-outcome errors

- **What:** `AnafResilienceHandler` retries the upload POST on `HttpRequestException`/5xx/408/429. If ANAF actually persisted the upload but the response was lost, the retry resends identical XML — a real duplicate submission, relying on an unverified "ANAF dedupes by invoice number" assumption.
- **Evidence:** `Services/Invoicing/Anaf/AnafResilienceHandler.cs:21-41`; `Services/Invoicing/Anaf/AnafSpvClient.cs:41-50,96-101`.
- **Suggested fix:** The originally-drafted "check `GetStatusAsync` before re-uploading" is a no-op for the case it's meant to guard — `GetStatusAsync` needs `id_incarcare`, which is only known after a successful upload response is parsed; on the ambiguous-failure path it's never set. A real fix needs either a genuinely new ANAF lookup-by-CIF/date-range capability (itself unverified against real ANAF, and `AnafResilienceHandler`'s retry is wired to the whole typed HttpClient today, so excluding just the upload endpoint needs new per-endpoint routing) or accepting the current documented tolerance (ANAF dedupes by invoice number) and doing nothing further. Flag this as an owner decision — this is a design trade-off (added latency: today's transient failure self-heals in ~7s vs. a 30-minute next-tick wait after removing retry), not something to just implement. Trigger-list-shaped (changes retry semantics) — **approach-check: revised** (drafted mitigation doesn't work; frame as owner decision, not implementation).
- **History:**
  - v1: found — raised independently by 1 lens (race)
  - v1: approach-check run — revised (proposed mitigation is a no-op for the described case; routed to owner decision)
  - v2: newly affirmed @`11dfb8e` — `AnafResilienceHandler.cs` is byte-identical since `e724528`; `AnafSpvClient.cs` changed only by the +5-line unrecognized-status warning in `GetStatusAsync`, off the upload path. The upload POST still carries no idempotency key and is still retried on ambiguous outcomes. Ruling stands

### PPW-490 — SQLite invoice numbering's MAX+1 has no transaction/lock despite the comment's safety claim

- **What:** `NextNumberAsync`'s bare `SELECT MAX(Number)+1` has no explicit lock; two concurrent dev/SQLite webhook requests can read the same MAX and compute the same number. The losing `SaveChangesAsync` throws unhandled, rolling back the Order.Status=Paid mutation with it — a captured payment silently reverts to AwaitingPayment.
- **Evidence:** `Services/Invoicing/SqliteInvoiceNumberingService.cs:29-48`; `Controllers/WebhooksController.cs:196-217,279-297`; `Data/PhotoPrintDbContext.cs:435-438`.
- **Suggested fix:** "Wrap in an explicit transaction" doesn't close the race — SQLite's deferred transaction takes no read lock, so both requests still read the same stale MAX before either writes. The catch also doesn't belong in `InvoiceCreationService` (same misplaced-catch issue as PPW-471 — that method never calls `SaveChangesAsync`). Mirror `OrderService.cs`'s existing bounded-retry pattern instead: catch the provider-specific violation on `ix_invoices_invoice_number` at the `WebhooksController` `SaveChangesAsync` call sites, reassign the invoice number on the still-tracked entity, and retry (bounded, e.g. `MaxOrderNumberRetries`-style). Postgres is unaffected (`nextval()` is atomic) — scope to SQLite/dev only. Same catch site as PPW-471 — fix together. Trigger-list-shaped (concurrency model + retry) — **approach-check: revised** (transaction-wrap dropped as ineffective; catch site and pattern corrected to match `OrderService`'s precedent).
- **History:**
  - v1: found — raised independently by 1 lens (race)
  - v1: approach-check run — revised (catch site and mechanism corrected; unified with PPW-471's fix)
  - v2: verified @`11dfb8e` — revert-and-rerun (retry budget set to 0): only `SaveOrderPaidWithInvoiceAsync_InvoiceNumberCollision_RetriesWithFreshNumber` went red, 3 siblings green

### PPW-491 — `InvoiceUploadJob` has zero tests despite being the most stateful new logic

- **What:** No test file exists for `InvoiceUploadJob` anywhere in the repo. The 3-step pipeline plus backoff-exhaustion branching is entirely unverified.
- **Evidence:** confirmed by search — no `InvoiceUploadJobTests.cs`.
- **Suggested fix:** Add unit tests for `ProcessOneAsync` covering partial completion, the backoff-budget boundary, and 200-with-Errors handling. Test-only; not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (completeness-critic), not independently confirmed — a real coverage gap, not a wrong-output claim, so there was no trace to build against it
  - v2: verified @`11dfb8e` — mutation-and-rerun (`IsBudgetExhausted` forced false): only `PollSubmittedAsync_RejectedBudgetExhausted_MarksFailedNotRejected` went red, 12 siblings green

### PPW-492 — Webhook tests stub invoice creation to always return null; nothing asserts it runs or that failure is handled

- **What:** `_invoiceCreator.CreateForOrderAsync` is mocked to unconditionally return null; nothing asserts it's actually invoked on the Paid path, nor what happens if it throws mid-`SaveChangesAsync`.
- **Evidence:** `Tests/Unit/Controllers/WebhooksControllerMetricsTests.cs:58`; `Controllers/WebhooksController.cs:196-217,279-297`.
- **Suggested fix:** Add a test asserting `CreateForOrderAsync` is called on the Paid transition, and one where it throws, asserting the order stays `AwaitingPayment` (not silently marked Paid). Test-only; not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (completeness-critic)
  - v2: verified @`11dfb8e` — mutation-and-rerun (invoice creation short-circuited out of the save path): both `Stripe_succeeded_for_an_awaiting_order_invokes_invoice_creation` and `Stripe_succeeded_when_invoice_creation_throws_leaves_order_awaiting_payment_in_the_database` went red, 14 siblings green

### PPW-493 — `PostgresInvoiceNumberingService` — the only prod numbering path — has no test coverage

- **What:** The only production numbering path (`PaymentFactory.cs` substitutes a fake for integration tests because EF InMemory can't execute its raw-SQL `nextval()`) has zero direct tests — a regression here ships untested, reproducing the dual-database gap CLAUDE.md already flags.
- **Evidence:** `Services/Invoicing/PostgresInvoiceNumberingService.cs:1-65`; `Tests/Integration/PaymentFactory.cs:170-177`.
- **Suggested fix:** Add a real/dockerized-Postgres test for `NextNumberAsync` covering year rollover and concurrent callers. Test-only; not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (completeness-critic)
  - v1: broadened by a supplemental db-parity check (the "tests-coverage"/"db-parity" lenses named in the manifest were dropped by a key typo and never ran this pass — see PPW-497): the same untested-Postgres-only-DDL gap also covers the raw-SQL `CREATE SEQUENCE`/composite unique index at `Migrations/20260603101910_AddVatAndInvoices.cs:103-114` — no test anywhere executes this migration's Postgres path
  - v2: NOT verified, stays `fixed` @`11dfb8e` — the three `[SkippableFact]` tests skip on this machine (no Postgres server; `psql` is installed but nothing listens on 5432), so no red-green proof exists. The gate is real: `ci.yml` runs a `postgres:16-alpine` service and sets `ConnectionStrings__Default` on the test step, so first execution is this PR's CI. The broadened half of the finding is untouched — no test executes the migration's Postgres DDL

### PPW-494 — Cloned retry `HttpRequestMessage` in `AnafAuthHandler` is never disposed

- **What:** `CloneAsync` builds a new `HttpRequestMessage`+content on every 401 retry; it's sent but never disposed in either the success or re-401-throw branch — a leak under a sustained 401 storm.
- **Evidence:** `Services/Invoicing/Anaf/AnafAuthHandler.cs:41-53,61-90`.
- **Suggested fix:** Dispose `retry` in a `finally` once `base.SendAsync(retry, ...)` returns.
- **History:**
  - v1: found — raised independently by 1 lens (correctness) — 🟡, entered ledger as `backlog` per README router

### PPW-495 — `status=""` is rejected by the query validator but treated as "no filter" by the controller

- **What:** An empty `?status=` query value 400s at the validator (`Enum.TryParse("")` fails) even though the controller's own `IsNullOrWhiteSpace` check would treat it as unfiltered.
- **Evidence:** `Validators/Invoices/AdminInvoiceListQueryValidator.cs:19-21`; `Controllers/AdminInvoicesController.cs:47-48`.
- **Suggested fix:** Change the `Must` predicate to accept `IsNullOrWhiteSpace` the same way the controller does.
- **History:**
  - v1: found — raised independently by 1 lens (input-validation) — 🟡, entered ledger as `backlog` per README router

### PPW-496 — No backfill path for orders already Paid before this deploy

- **What:** Orders that reached Paid before this feature ships (or via any path skipping the webhook code) never get an Invoice row, and no admin tool can create one after the fact — `/retry` requires an existing invoice id.
- **Evidence:** `Controllers/AdminInvoicesController.cs:1-156`; `Services/Invoicing/InvoiceCreationService.cs:37-82`.
- **Suggested fix:** Confirm with product/legal whether historical paid orders need retroactive invoices; if so, add an admin/CLI backfill command (pattern: `Cli/BackfillCommand.cs`).
- **History:**
  - v1: found — raised independently by 1 lens (completeness-critic) — 🟡, entered ledger as `backlog` per README router

### PPW-497 — Discovery manifest omitted ~24 changed files, including the VAT math itself

- **What:** Two independent dispatch gaps this pass: (1) the `changedFiles` list handed to lenses omitted `VatCalculator.cs`, `OrderService.cs`'s VAT wiring, both numbering-service implementations, and other real changed files — most were still reached by lenses reading the full diff/exploring the repo, but `VatCalculator.cs`'s own rounding correctness got no dedicated pass; (2) two requested lens keys (`tests`, `db-migration-parity`) were typos for the script's actual keys (`tests-coverage`, `db-parity`) and were silently dropped — only 8 of the intended 10 lenses ran.
- **Evidence:** `git diff main --name-only` (517 files, all ~12 named files real); `reviews/lib/discovery-review.wf.js`'s `LENS_LIBRARY` keys vs. this pass's requested `lenses` array.
- **Suggested fix:** Process fix, not a code fix: regenerate the lens manifest from the actual diff before handing it to lenses in future passes (treat any hand-written list as a floor, not a ceiling); have the loop-driver/synthesizer validate requested lens keys against `LENS_LIBRARY` and fail loudly on an unrecognized key instead of silently dropping it.
- **History:**
  - v1: found by the completeness-critic lens, not independently confirmed; severity lowered from high to low this pass after supplemental checks closed both concerns directly: `VatCalculator.cs`'s rounding math is clean and the db-parity check found the migration itself sound — but the two checks surfaced two new real defects, **PPW-478** and **PPW-505**, and broadened **PPW-493**'s evidence

### PPW-498 — Polly retry pipeline in `AnafResilienceHandler` never disposes intermediate failed responses

- **What:** `AddRetry`'s `ShouldHandle` triggers a retry on 5xx/408/429 with no `OnRetry` callback disposing the failed `HttpResponseMessage` before the next attempt — a minor per-retry resource leak.
- **Evidence:** `Services/Invoicing/Anaf/AnafResilienceHandler.cs:33`.
- **Suggested fix:** Add an `OnRetry` callback disposing `outcome.Result` when non-null.
- **History:**
  - v1: found by the correctness lens — not independently checked (cleanup findings get no skeptic); ⚪, entered ledger as `backlog` per README router

### PPW-499 — `AnafAuthHandler.CloneAsync` duplicates `SamedayAuthHandler`'s request-cloning logic verbatim

- **What:** Two byte-for-byte identical ~30-line request-cloning implementations; a future correction has to be applied twice or drifts.
- **Evidence:** `Services/Invoicing/Anaf/AnafAuthHandler.cs:61-90`; `Services/Sameday/SamedayAuthHandler.cs:83-114`.
- **Suggested fix:** Extract a shared HTTP request-clone helper used by both handlers.
- **History:**
  - v1: found by the quality lens — not independently checked (cleanup findings get no skeptic); ⚪, entered ledger as `backlog` per README router

### PPW-500 — Response-status classification duplicated between `AnafSpvClient.UploadAsync` and `GetStatusAsync`

- **What:** The 401/5xx/timeout/not-success checks and `<Errors>` extraction are copy-pasted between `UploadAsync` and `GetStatusAsync`.
- **Evidence:** `Services/Invoicing/Anaf/AnafSpvClient.cs:59-66,115-122`.
- **Suggested fix:** Extract a shared `EnsureAnafSuccess`/error-extraction helper used by both methods.
- **History:**
  - v1: found by the quality lens — not independently checked (cleanup findings get no skeptic); ⚪, entered ledger as `backlog` per README router

### PPW-501 — Buyer-name fallback logic duplicated between `InvoiceXmlBuilder` and the PDF renderer

- **What:** `BuildCustomerParty` and `InvoicePdfDocument.ComposeBuyer` both re-implement the same guest-check + name-fallback chain; a fix to one can be missed in the other, letting the PDF and the legal XML disagree.
- **Evidence:** `Services/Invoicing/InvoiceXmlBuilder.cs:104-137`; `Services/Invoicing/InvoicePdfDocument.cs:81-95`.
- **Suggested fix:** Extract a shared `ResolveBuyerName(Order)` helper used by both. Related to PPW-477's fix (same two call sites need the sanitize net) — worth doing together.
- **History:**
  - v1: found by the quality lens — not independently checked (cleanup findings get no skeptic); ⚪, entered ledger as `backlog` per README router

### PPW-502 — Invoice entity config uses a literal `"Sqlite"` string instead of the `DbProviders.Sqlite` constant

- **What:** Line 428 checks a literal provider-name string while the Order and OrderItem blocks 78/39 lines above already use `DbProviders.Sqlite` for the identical check in the same file.
- **Evidence:** `Data/PhotoPrintDbContext.cs:428` (cf. lines 350, 389).
- **Suggested fix:** Replace the literal with `DbProviders.Sqlite`.
- **History:**
  - v1: found by the quality lens — not independently checked (cleanup findings get no skeptic); ⚪, entered ledger as `backlog` per README router

### PPW-503 — `PostgresInvoiceNumberingService` interpolates the sequence name into raw SQL with no in-service validation

- **What:** `series` is safe today only because its sole caller is boot-validated config (`^[A-Z]{2,10}$`); the service itself doesn't re-validate the character set, so a future caller passing a request/admin-supplied series becomes a SQL-injection point via an unparameterized identifier.
- **Evidence:** `Services/Invoicing/PostgresInvoiceNumberingService.cs:43`.
- **Suggested fix:** Assert the same `[A-Z]{2,10}` pattern inside `NextNumberAsync` itself, not just at the config layer.
- **History:**
  - v1: found by the input-validation lens — not independently checked (cleanup findings get no skeptic); ⚪, entered ledger as `backlog` per README router

### PPW-505 — Fiscal-year numbering constraint can disagree between Postgres and .NET at a Dec 31/Jan 1 boundary

- **What:** The raw-SQL composite unique index computes fiscal year via Postgres `EXTRACT(YEAR FROM "IssuedAt")`, which resolves against the server's session `TimeZone` setting, while the app computes year via .NET `DateTimeOffset.Year` — no `TimeZone`/`SET TIME ZONE` pinning exists anywhere in the API. If the Postgres server's timezone setting ever differs from the offset used to set `IssuedAt`, the two years could disagree right at a Dec 31/Jan 1 boundary. Likely moot in practice (most managed Postgres defaults to UTC) but unconfirmed — found via the supplemental db-parity check PPW-497 named.
- **Evidence:** `Migrations/20260603101910_AddVatAndInvoices.cs:111-114`; `Services/Invoicing/InvoiceNumber.cs:11`.
- **Suggested fix:** Pin the Postgres connection's `TimeZone` to UTC explicitly (connection string or `SET TIME ZONE`), or compute the constraint's year from a UTC-normalized expression matching the app's calculation.
- **History:**
  - v1: found by a supplemental db-parity check, dispatched this pass to close PPW-497's lens-key gap — an edge case that hasn't been checked in practice, low likelihood; 🟡, entered ledger as `backlog` per README router

### PPW-504 — `OrderDetailDto` grew 3 required fields with no lens covering the frontend contract

- **What:** `NetTotalRon`/`VatRon`/`VatRate` were added to the DTO; this PR is backend-only, so no lens checked whether the Angular order-detail page needs updating to surface the VAT breakdown Romanian retail display conventions typically expect.
- **Evidence:** `DTOs/Orders/OrderDetailDto.cs:5`.
- **Suggested fix:** Confirm with the owner whether the SPA order-detail view should surface the new fields; file a follow-up frontend bolt if so.
- **History:**
  - v1: found by the completeness-critic lens — not independently checked (cleanup findings get no skeptic); ⚪, entered ledger as `backlog` per README router

### PPW-506 — Config comment and rollout runbook still promise the customer invoice email that does not exist

- **What:** PPW-472's fix corrected the C# docstring to say no email is sent, but the two documents an operator actually reads still say the opposite. `appsettings.json`'s `Invoicing` comment says "Flip to true after the production inspection week"; the deployment flag table repeats it; and the rollout runbook's step 6 states that after the flip "Customers now receive the PDF attached to order-confirmation emails ... or via a follow-up 'your invoice is ready' email". No send path exists.
- **Evidence:** `docs/DEPLOYMENT.md:1409,1309`; `appsettings.json:107`; `Services/Invoicing/InvoicePdfReadyNotifier.cs:40-45`.
- **Suggested fix:** Say in all three places what the corrected docstring says — the flag currently changes nothing customer-visible — or gate the runbook step behind the email integration landing. Doc-only.
- **History:**
  - v2: found by the verification pass asking whether PPW-472's fix held as a class; the code leg held, the doc leg did not
  - v2: fix round — corrected the config comment, the flag table, rollout step 6, both rollback paths, the alert-table row and the notifier docstring. ADR-022 and ddd-02 left as point-in-time bolt records; `decision-index.md` corrected because it is a standard
  - v3: verified @`08e7746` — each corrected claim checked against `InvoicePdfReadyNotifier`, which logs `invoice.pdf-ready.suppressed` when the flag is off and `invoice.pdf-ready.no-email-integration` when it is on, and sends nothing either way; the log names quoted in rollout step 6 match. Two disagreements recorded, not disputed: the fix round's claim to have corrected every operator-facing site is wrong (PPW-509), and leaving ADR-022 frozen is right only if the docs stop citing it as current (PPW-510)

### PPW-507 — New `Anaf:ClaimTtlMinutes` knob has no config default entry and no deployment documentation

- **What:** PPW-476's claim/lease introduced an operational knob whose value decides how long a crashed worker strands an invoice. It exists only as a C# default of 10; `appsettings.json`'s `Anaf` block does not list it, the deployment flag table and env-var block do not mention it, and no validator bounds it — a zero or negative value is silently clamped to 1 minute in code, below the pipeline's own duration.
- **Evidence:** `Configuration/AnafSettings.cs:44`; `appsettings.json:94-105`; `docs/DEPLOYMENT.md:1306-1309,1376-1381`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:132`.
- **Suggested fix:** Add the key to `appsettings.json` with its default, add a row to the deployment flag table naming what happens if it is set below one pass, and bound it in the ANAF settings validator.
- **History:**
  - v2: found by the verification pass's new-surface check on PPW-476's added mechanism — sized default, signal and failure-mode tests are present; documentation is the missing leg
  - v2: fix round — key added to appsettings.json, bounded 2–1440 in AnafSettingsValidator with 5 new tests, and documented with its failure mode in the deployment flag section; the job's own floor was raised from 1 to 2 to match
  - v3: verified @`08e7746` — revert-and-rerun (validator bound deleted): only the 4 `ClaimTtlMinutes_out_of_range_fails` cases went red, 26 siblings green. The job's raised floor is inert, not a regression: all 16 `InvoiceUploadJobTests` and `InvoicePdfReadyNotifierTests` stay green, every test uses 10, and the validator rejects anything under 2 whenever the worker is registered at all

### PPW-508 — Exhausted invoice-number retries now answer the payment processor 200 and count as `duplicate`

- **What:** PPW-488 asked for a domain-tagged log on the "customer charged, order not committed" path. The fix also swallows the `DbUpdateException` and returns false, so the handler skips its side effects, records the webhook as `duplicate`, and answers 200. Before the fix the exception propagated to a 500 and the processor retried, which a fresh invoice number would very likely have satisfied; now the order stays `AwaitingPayment` with no retry and the label hides the failure from the webhook error rate.
- **Evidence:** `Controllers/WebhooksController.cs:414-421,204-206,284-286`; `Observability/MetricNames.cs:53-58`.
- **Suggested fix:** Keep the log, then rethrow (or answer 500) so the processor's own retry still runs; if the swallow is deliberate, label the metric `failed`, not `duplicate`. Reachable only on the SQLite numbering path — Postgres `nextval()` cannot collide — so dev-grade impact today.
- **History:**
  - v2: found by the verification pass's regression check on PPW-488's fix diff — a behaviour change the finding did not ask for and the resolution's Decisions block does not mention
  - v2: fix round — relabelled to `failed` via a three-state PaidSaveOutcome; the approach-check refuted the drafted rethrow because `duplicate` sits in SLO 3's success numerator and an escaping throw records no metric at all (PPW-397). Rollback now reloads the entity rather than unwinding fields, after the micro-review found `UpdatedAt` was missed
  - v3: REOPENED @`08e7746` — the source is right, two of its three legs cannot go red. Relabel: putting both call sites back to `created ? ok : duplicate` while keeping the helper reintroduces the exact defect and leaves all 23 webhook controller tests green, measured; `ResultLabelFor_maps_each_outcome_to_its_slo_label` only invokes the private helper by reflection, and nothing asserts the label the handler records. Rollback: the test invokes `SaveOrderPaidWithInvoiceAsync` directly and never applies the Paid transition first, so `Status`, `PaidAt` and `UpdatedAt` are already at their rolled-back values — deleting the whole `ReloadAsync` block leaves the test green, measured, including the `UpdatedAt` assertion the micro-review added to pin it. Sentry capture holds: removing it reddens exactly one test. Third defect in the same mechanism: the rollback's catch excludes `OperationCanceledException`, so a cancelled token during the reload escapes the helper and `RecordPaymentWebhook` never runs — the PPW-397 hole the approach-check refused a rethrow to avoid
  - v3: fix round — both vacuous legs replaced by one test that drives `StripeWebhookAsync` end to end on SQLite and reads the metric through `MetricCapture`; each leg proven red separately (mislabel put back, then reload block deleted). The reload catch no longer excludes `OperationCanceledException`, closing the third defect
  - v4: REOPENED @`0a250b9` — two of the three legs now hold. Reverting the Stripe call site to `created ? ok : duplicate` reddens only `StripeWebhook_WhenInvoiceNumberRetriesExhaust_RecordsFailedNotDuplicateAndLeavesOrderUnpaid`, 23 green. Deleting the `ReloadAsync` block reddens the same test on `Status`; a partial unwind that leaves `UpdatedAt` behind reddens it on `UpdatedAt`, so the pin the v2 micro-review added is live at last. Two gaps remain. The EuPlatesc call site is unproven: reverting that site alone leaves all 24 green, so one of the two identical sites carries no test. The cancellation leg ships production code with no test, and the reason given for that is false. Forcing `ReloadAsync` to throw a cancellation needs no fake provider: set the `Sentry.IHub` mock this test file already builds to cancel a `CancellationTokenSource` from its `CaptureEvent` callback, which fires immediately before the reload. Measured — green with the fix, red without it, `OperationCanceledException` escaping `ReloadAsync` via `SqliteCommand.ExecuteReaderAsync`, in about 45 lines reusing existing helpers. Disagreement, not disputed: the resolution says both vacuous proofs were replaced, but `ResultLabelFor_maps_each_outcome_to_its_slo_label` was kept and survives the Stripe revert, so it still proves the helper and never the call sites. Class check clean otherwise — no-oping `RecordPaymentWebhook` reddens 14 of 24 tests here, and the only metric-asserting survivor is `Stripe_unhandled_event_type_records_nothing`, which asserts absence by design
  - v4: fix round — cancellation proof added exactly as the pass described: the Sentry mock cancels a token source from its capture callback, so the token lands inside `ReloadAsync`. Red with the old `OperationCanceledException` filter restored, green without it. The EuPlatesc coverage gap is dropped on the owner's mid-round ruling that the integration is being removed and only Stripe will remain; a drafted test for that site was discarded rather than committed
  - v5: verified @`07b0c1b` — all three legs redden on their own revert, each failing set predicted before the run and matched exactly. Label: the Stripe call site back to `created ? ok : duplicate` reddens only `StripeWebhook_WhenInvoiceNumberRetriesExhaust_RecordsFailedNotDuplicateAndLeavesOrderUnpaid`, 24 green. Rollback: deleting the `ReloadAsync` block reddens that test on `Status` and the new cancellation test on its missing log; the partial unwind that keeps `UpdatedAt` reddens on `UpdatedAt`, so that pin bites. Cancellation: restoring `when (reloadEx is not OperationCanceledException)` reddens `SaveOrderPaidWithInvoiceAsync_WhenRollbackReloadIsCancelled_StillReturnsWithoutThrowing` with `System.OperationCanceledException` raised at `WebhooksController.cs:434` through `EntityEntry.ReloadAsync` → `GetDatabaseValuesAsync` → `SqliteCommand.ExecuteReaderAsync`, so the cancellation lands inside the reload, not earlier or later. The proof is not incidental. Its two assertions are separately load-bearing: an escaping throw fails the await, and the `rollback-reload-failed` assertion fails whenever the reload succeeds. It cannot rot into a vacuous test either, because deleting or moving the Sentry capture that triggers the cancel makes the reload succeed and the test red. Stable 5 of 5 alone and green in every 25-test run; the cancel fires synchronously inside the capture callback, so there is no race window. Class check repeated independently: no-oping `RecordPaymentWebhook` reddens 14 of 25, and the only metric-asserting survivor is `Stripe_unhandled_event_type_records_nothing`, which asserts absence by design. `ResultLabelFor_maps_each_outcome_to_its_slo_label` survives both call-site reverts, confirming the v4 disagreement; it is harmless beside the endpoint test. Regression: the round is test-only, the `InvokeSaveAsync` overload split leaves its 3 other callers green, and `MetricCapture` gates on an `AsyncLocal` token so the parallel metrics class cannot pollute it. The EuPlatesc gap is confirmed still live and not reopened, per the owner ruling — reverting `WebhooksController.cs:205` alone leaves all 25 green; the untracked removal that ruling rests on is PPW-511

### PPW-509 — `CustomerEmailAttachmentSettings` docstring still says the XML, ANAF and PDF pipeline runs unconditionally

- **What:** The settings docstring reads "the full pipeline runs (XML build, ANAF upload, PDF render, storage write)". That is false whenever `Anaf:Enabled` is false, which is the shipped default. It now contradicts the `AnafSettings` docstring in the same assembly, which PPW-481's fix corrected to say no XML or PDF is built. `Program.cs`'s registration comment makes the weaker version of the same claim, calling only the upload pipeline conditional.
- **Evidence:** `Configuration/InvoicingSettings.cs:15-20`; `Configuration/AnafSettings.cs:3-7`; `Program.cs:281-283,299`.
- **Suggested fix:** Cut the pipeline sentence from the settings docstring and keep the sentence PPW-472 added, which is true. Reword the `Program.cs` comment to say the builders are registered but only the worker calls them. Doc-only.
- **History:**
  - v3: found by the verification pass's class check on PPW-481 and PPW-506 — the false sentence is original bolt-039 text, not fix-caused, but two fix rounds edited this file and left it. The fix round's claim to have corrected every operator-facing site does not hold

### PPW-510 — ADR-022 left stale while the deployment guide and the decision index send an operator to it as current authority

- **What:** ADR-022 still says the flag gates a real customer email and that XML build, ANAF upload, PDF render and storage write run regardless of it. Both statements are false. Keeping the ADR frozen as a bolt record is the right convention, but three live documents route a reader to it: the deployment guide's flag table cites it for the flag, its rollout section opens "per ADR-022", and the decision index tells the reader to open the ADR when flipping the flag "to recall what side effect is gated". The ADR carries no marker saying it is out of date.
- **Evidence:** `memory-bank/bolts/039-efactura-anaf/adr-022-dual-write-rollout-via-feature-flag.md:54-70`; `docs/DEPLOYMENT.md:1309,1411`; `memory-bank/standards/decision-index.md:43`.
- **Suggested fix:** Either add one superseded line at the head of the ADR pointing at the decision index, or drop the "use this ADR to recall what side effect is gated" clause and the two citations that present it as current. Doc-only.
- **History:**
  - v3: found by the verification pass reviewing the fix round's decision to keep ADR-022 and ddd-02 as point-in-time records. The decision is agreed; the routing into it is the defect

### PPW-511 — EuPlatesc coverage waived twice on a removal that no work item tracks, against a standard that forbids the divergence

- **What:** `definition-of-done.md` defect class 2 names Stripe/EuPlatesc as a pair whose every behaviour is "either verified symmetric or documented divergent". Two rounds have now left the EuPlatesc arm neither, both waived on the same removal: PPW-233 on 2026-07-27 and PPW-508 on 2026-08-14. The divergence is written only in review resolution files, which are not standards, and no work item tracks the removal — not the backlog, not `memory-bank/bolts/`, not `memory-bank/intents/`. Meanwhile the integration is fully live: `POST /api/webhooks/euplatesc`, `IEuPlatescService` registered, and its credentials required in Production. If the removal never lands, both coverage gaps stand forever with nothing recording that they were accepted. Measured: reverting the EuPlatesc call site alone leaves all 25 scoped tests green.
- **Evidence:** `memory-bank/standards/definition-of-done.md:52-53`; `memory-bank/standards/system-architecture.md:90-92`; `Program.cs:211-214,223`; `Controllers/WebhooksController.cs:141,205`.
- **Suggested fix:** Record the removal as one work item, and until it lands add one line to `definition-of-done.md` class 2 naming EuPlatesc coverage as an accepted divergence with its expiry. `system-architecture.md` still presents EuPlatesc as a current payment backend, so it needs the same line — CLAUDE.md requires a standard to be updated in the change that alters what it describes. Docs and tracking only; no code change, and the removal ruling itself is not in question.
- **History:**
  - v5: found by the verification pass checking whether the owner ruling that dropped PPW-508 EuPlatesc leg is recorded anywhere as work. It is not, and the standard that mandates the coverage still reads as if it were being met

### PPW-512 — Easybox orders emit e-Factura with empty mandatory buyer address elements

- **What:** Customer picks Easybox: CheckoutStateService.setEasyboxContact sends street/number/city/county/postalCode = "". CreateOrderRequestValidator's Easybox branch has no NotEmpty on those fields, and BuildCustomerParty embeds ShippingAddress unconditionally, so the XML carries empty StreetName/CityName/PostalZone (BT-50/BT-52) and ANAF rejects every locker order's invoice.
- **Evidence:** `src/PhotoPrint.UI/src/app/core/services/checkout-state.service.ts:42`; `Validators/Payments/CreateOrderRequestValidator.cs:31`; `Services/OrderService.cs:156`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:178`; `Services/Invoicing/InvoiceXmlBuilder.cs:115`; `Services/Invoicing/InvoiceAddressFormatter.cs:10`
- **Suggested fix:** Fall back to order.EasyboxLocker's address/city/postal code when ShippingAddress is blank (or require the fields for Easybox), and reject a blank buyer address in the builder. Suggested test: InvoiceXmlBuilder_EasyboxOrderWithLockerOnlyAddress_EmitsBuyerAddress: arrange Easybox order, ShippingAddress with recipient/phone only; act Build; assert StreetName, CityName, PostalZone all non-empty.
- **History:**
  - v6: found by the delta pass — raised by correctness (convergence 1), verdict confirmed
  - v6: fix round — the builder now refuses an empty StreetName/CityName/PostalZone instead of filing them blank; the address a locker order should carry is an open owner question, so locker orders stay uninvoiceable
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `8b79a5a`) reddened `InvoiceXmlBuilderTests`, restore greened it

### PPW-513 — uq_invoices_series_year_number index expression is not IMMUTABLE, so Postgres aborts Migrate() at prod boot

- **What:** IssuedAt is timestamptz, and EXTRACT(YEAR FROM timestamptz) is STABLE (timezone-dependent), not IMMUTABLE. On the first Postgres boot, Program.cs Database.Migrate() runs this Sql() and Postgres raises "functions in index expression must be marked IMMUTABLE"; the migration rolls back, the API fails to start, and the two new migrations never apply.
- **Evidence:** `Migrations/20260603101910_AddVatAndInvoices.cs:111`; `Migrations/20260603101910_AddVatAndInvoices.cs:63`; `Migrations/20260603101910_AddVatAndInvoices.cs:103`; `Program.cs:383`; `Tests/Integration/PostgresInvoiceNumberingServiceIntegrationTests.cs:66`
- **Suggested fix:** Make the expression immutable: EXTRACT(YEAR FROM ("IssuedAt" AT TIME ZONE 'UTC'))::int, and cover it with a migrate-on-Postgres test. Suggested test: SkippableFact on PostgresFixture: Migrations_ApplyCleanlyToPostgres — arrange a fresh Npgsql context on the CI Postgres, act db.Database.Migrate(), assert no throw (currently throws PostgresException 42P17 IMMUTABLE).
- **History:**
  - v6: found by the delta pass — raised by db-parity (convergence 1), verdict confirmed
  - v6: fix round — expression pinned to UTC; the fix carried into the regenerated InitialPostgres baseline and every Postgres-backed test now migrates through it
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `8917f9f`) reddened `InvoiceAddressFormatterTests`, restore greened it
  - v6: the line above misattributes the proof. Commit `8917f9f` carries three findings, and `InvoiceAddressFormatterTests` covers only string truncation, so it cannot detect a migration defect
  - v6: re-proven by hand at the baseline the fix now lives in — dropping `AT TIME ZONE 'UTC'` from `Migrations/20260820133204_InitialPostgres.cs:743` failed all 3 `MigrationChainTests` inside `Migrate()`; restoring it returned 3 of 3 green

### PPW-514 — Exhausted-retry rollback reload discards the processor transaction id and the Error log omits it

- **What:** EuPlatesc IPN pays order X; the invoice-number retry exhausts. The reload at line 434 discards the in-memory EuPlatescTransactionId, and the Error log carries only order_id plus previous_status (always AwaitingPayment, so no information). The ep_id of the captured charge then exists nowhere in our logs or DB.
- **Evidence:** `Controllers/WebhooksController.cs:199`; `Controllers/WebhooksController.cs:203`; `Controllers/WebhooksController.cs:423`; `Controllers/WebhooksController.cs:434`; `Services/OrderService.cs:395`; `Services/Invoicing/InvoiceCreationService.cs:60`
- **Suggested fix:** Log ep_id / PaymentIntent id, order_number and amount in this Error line (drop the tautological previous_status) before the reload discards them. Suggested test: ExhaustedInvoiceRetry_PreservesProcessorTransactionId: arrange EuPlatesc IPN (ep_id=EP123) with numbering always returning a colliding number; act POST /api/webhooks/euplatesc; assert order.EuPlatescTransactionId or the exhausted log line contains "EP123".
- **History:**
  - v6: found by the delta pass — raised by observability (convergence 1), verdict confirmed
  - v6: fix round — order number, total and both processor references logged before the rollback reload discards them
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `6aabff9`) reddened `WebhooksControllerInvoiceRaceTests`, restore greened it

### PPW-515 — ANAF client-side timeout escapes as OperationCanceledException and stops the upload worker, unreachable by tests

- **What:** ANAF SPV hangs; the 30s HttpClient timeout throws TaskCanceledException. AnafSpvClient catches only HttpRequestException; every new catch filters `is not OperationCanceledException`; the batch loop rethrows. It escapes ExecuteAsync -> default StopHost -> API shuts down. Job tests call ProcessBatchAsync by reflection and never simulate a timeout.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:81`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:96`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:216`; `Services/Invoicing/Anaf/AnafSpvClient.cs:53`; `Services/Invoicing/Anaf/AnafResilienceHandler.cs:38`; `Program.cs:317`
- **Suggested fix:** In AnafSpvClient wrap SendAsync's OperationCanceledException as AnafUnreachableException when the caller token is not cancelled; add a ScriptedHttpMessageHandler test that throws TaskCanceledException. Suggested test: ProcessBatchAsync_ClientTimesOut_DoesNotPropagate: fake IAnafSpvClient.UploadAsync throws new TaskCanceledException("timeout", new TimeoutException()); invoke ProcessBatchAsync with an uncancelled token; assert no exception escapes and the invoice records LastError and releases its claim.
- **History:**
  - v6: found by the delta pass — raised by tests-coverage (convergence 1), verdict confirmed
  - v6: fix round — guard moved to the tick per the approach-check, since storage and DB cancellation reach the same exit; an upload timeout holds its claim
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `42e5988`) reddened `InvoiceUploadJobTests`, restore greened it; note: no Sentry/metric signal covers a persisting timeout, and the fix orphaned `AnafUnreachableException`'s doc comment (PPW-552)

### PPW-516 — Exhausted invoice-number retry answers the payment processor 200, killing its last retry

- **What:** Four consecutive InvoiceNumber collisions (SQLite MAX+1 path, or any provider under load) previously let DbUpdateException escape to ExceptionHandlerMiddleware -> 500 -> Stripe/EuPlatescu redelivers and the transient collision clears. Now it returns NumberExhausted, the handler still returns 200/`BuildIpnResponse`, and the charge stays permanently unpaid with only a log + Sentry event.
- **Evidence:** `Controllers/WebhooksController.cs:423 (exhausted branch returns NumberExhausted)`; `Controllers/WebhooksController.cs:434 (ReloadAsync discards Paid transition)`; `Controllers/WebhooksController.cs:136 (return Ok() regardless of outcome)`; `Controllers/WebhooksController.cs:247 (EuPlatesc BuildIpnResponse 200)`; `Services/Invoicing/PostgresInvoiceNumberingService.cs:47 (CREATE SEQUENCE START 1)`; `Services/Invoicing/SqliteInvoiceNumberingService.cs:35 (unlocked MAX+1)`
- **Suggested fix:** On NumberExhausted return a 5xx (or EuPlatescu's error body) so the processor redelivers, instead of acking success. Suggested test: StripeWebhook_WhenInvoiceNumberRetriesExhausted_DoesNotAckDelivery: arrange numbering to return a colliding number every call; POST a signed payment_intent.succeeded to the real endpoint; assert 5xx (retryable) and order still AwaitingPayment.
- **History:**
  - v6: found by the delta pass — raised by correctness (convergence 1), verdict confirmed
  - v6: re-raises a decided item, PPW-508. Prior decision, verbatim: v2 approach-check refuted a rethrow and the owner-side ruling stood: an escaping throw records no metric at all (PPW-397), every EuPlatesc branch answers a signed ack, and duplicate sat in SLO 3 success numerator. Resolved by relabelling to failed, deliberately keeping the 200.
  - v6: correction to the line above, which is labelled verbatim but paraphrases. PPW-508s v2 History line reads: "relabelled to `failed` via a three-state PaidSaveOutcome; the approach-check refuted the drafted rethrow because `duplicate` sits in SLO 3s success numerator and an escaping throw records no metric at all (PPW-397)".
  - v6: re-affirmed @`2979ea0` — SQLite is dropped and Postgres `nextval()` is atomic, so the collision path is unreachable in normal operation; the resolution's "See Decisions" pointer names no PPW-516 decision block, and the exhaustion branch is not dead code

### PPW-517 — Invoice PDF tier is chosen from the live CloudEnabled flag with no per-row StorageLocation, so a Provider flip orphans stored PDFs

- **What:** PDFs written by InvoiceUploadJob while Storage:Provider=local; operator flips to S3. Every existing invoice now resolves to the cloud adapter, the key is absent, and GetStreamAsync throws (500, not 404). Uploads avoid this via Upload.StorageLocation + IStorageRouter.For; Invoice has no such column, and tests only cover cloud-on/cloud-off within one process.
- **Evidence:** `Controllers/InvoicesController.cs:68`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:125`; `Models/Invoice.cs:44`; `Services/S3StorageService.cs:119`; `Middleware/ExceptionHandlerMiddleware.cs:10`; `Controllers/UploadsController.cs:185`
- **Suggested fix:** Add Invoice.StorageLocation stamped at write time and read via _storageRouter.For(invoice.StorageLocation); map a missing object to 404. Suggested test: InvoicesControllerTests.GetInvoiceAsync_KeyMissingInActiveTier_Returns404: arrange CloudEnabled=true, invoice row with PdfStoragePath, cloud GetStreamAsync throws FileNotFoundException; act GET; assert NotFoundResult with Retry-After — currently the exception escapes.
- **History:**
  - v6: found by the delta pass — raised by correctness, completeness-critic, tests-coverage (convergence 3), verdict confirmed
  - v6: fix round — Invoice.StorageLocation stamped with the path in one save; the read treats it as a preference with a fallback. No config-derived backfill: a migration cannot read configuration
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `67df511`) reddened `InvoicesControllerTests`, restore greened it; note: the fallback path is untested and its double-miss case reopens PPW-523's failure mode (PPW-550)

### PPW-518 — Admin manual mark-Paid inserts an Invoice with none of the webhook path's unique-violation protections, and its creation is fully mocked in tests

- **What:** Admin marks a late-webhook order Paid while the real webhook delivery commits its invoice in between load and SaveChanges. Postgres raises 23505 on ix_invoices_order_id; AdminOrderService has no classifier or retry, so SaveChanges throws, the admin gets a 500 and the whole status change is lost — the webhook path handles the same collision gracefully.
- **Evidence:** `Services/AdminOrderService.cs:148`
- **Suggested fix:** Share the webhook's violation classification/retry helper (or catch the OrderId violation and treat it as already-invoiced) in the admin mark-paid path.
- **History:**
  - v6: found by the delta pass — raised by correctness, completeness-critic, tests-coverage, db-parity (convergence 4), verdict confirmed
  - v6: fix round — classifiers extracted to InvoiceUniqueViolation and reused, proven against real Postgres because EF InMemory raises no unique violation
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `084c579`) reddened `AdminOrderServiceTests`, restore greened it

### PPW-519 — RetryAsync wipes XmlPayload, destroying the submitted-XML snapshot and diverging it from the kept PDF

- **What:** Ops inspects a Rejected invoice, clicks Retry: XmlPayload is set to null. GET /api/admin/invoices/{id}/xml — the endpoint whose stated purpose is inspecting what went to ANAF — now 404s until the next worker tick (up to 30 min), and permanently if the rebuild then throws (null City, missing ShippingAddress).
- **Evidence:** `Services/Invoicing/InvoiceLifecycle.cs:120`
- **Suggested fix:** Keep the payload in a separate column/log before nulling it, or clear it only after the rebuild succeeds.
- **History:**
  - v6: found by the delta pass — raised by correctness, completeness-critic, tests-coverage (convergence 3), verdict confirmed
  - v6: re-raises a decided item, PPW-480. Prior decision, verbatim: v1 fix round cleared XmlPayload deliberately, per a vetted approach-check, so a retry rebuilds the XML rather than resubmitting a stale payload; PdfStoragePath was left untouched.
  - v6: correction to the line above, which is labelled verbatim but paraphrases. PPW-480 states its decision in its Suggested-fix line: "Clear `Invoice.XmlPayload` in `RetryAsync`s existing atomic update so the next tick rebuilds it", with "approach-check: revised (drop the `PdfStoragePath` clear; add pre-clear logging)".
  - v6: fix round — disputed. Clearing XmlPayload is PPW-480s vetted decision; the alternative reintroduces the defect that fixed
  - v6: re-affirmed @`2979ea0` — code still clears XmlPayload, leaves PdfStoragePath untouched, and logs the pre-clear length, exactly per PPW-480's vetted decision

### PPW-520 — Per-line PriceAmount x InvoicedQuantity no longer equals LineExtensionAmount, and nothing asserts it

- **What:** The suite's own fixture (quantity 3, gross 21.00) now emits LineExtensionAmount 17.65 with PriceAmount 5.88 — 5.88x3 = 17.64. The residual reconciliation test only checks the header sum; no test asserts line-level consistency and no CIUS-RO/EN16931 schema or validator runs anywhere, so ANAF is the first validator to see it.
- **Evidence:** `Services/Invoicing/InvoiceXmlBuilder.cs:219`
- **Suggested fix:** Assert PriceAmount x quantity == LineExtensionAmount (or emit BaseQuantity), and add an offline XSD/Schematron check over a built document.
- **History:**
  - v6: found by the delta pass — raised by correctness, completeness-critic, tests-coverage (convergence 3), verdict confirmed
  - v6: fix round — deferred. The repo documents only two decimals for emitted amounts and story 001s schema check was never built, so nothing local can adjudicate the options
  - v6: re-affirmed @`2979ea0` — BuildInvoiceLines still rounds netUnitPrice independently of netTotal, and no XSD/Schematron validator exists anywhere in the repo to adjudicate

### PPW-521 — InvoiceAddressFormatter.Truncate throws NRE on a null City/Street that the validators accept

- **What:** ShippingAddressSnapshot.City is `string = null!`; FluentValidation MaximumLength and Must(HasNoXmlInvalidChars) both pass on null and Easybox has no NotEmpty. POST an order omitting "city" -> City is null -> Truncate(addr.City, 50) dereferences .Length -> NullReferenceException in the worker. Pre-delta `new XElement(..., addr.City)` tolerated null.
- **Evidence:** `Services/Invoicing/InvoiceAddressFormatter.cs:12`; `Services/Invoicing/InvoiceXmlBuilder.cs:121`; `Validators/Payments/CreateOrderRequestValidator.cs:35`; `Program.cs:113`; `Filters/ValidationFilter.cs:11`; `Models/ShippingAddressSnapshot.cs:8`
- **Suggested fix:** Take `string?` in Truncate and return string.Empty for null/short input; add NotNull/NotEmpty on the Easybox address fields.
- **History:**
  - v6: found by the delta pass — raised by correctness (convergence 1), verdict plausible
  - v6: fix round — Truncate is null-tolerant, both guards proven red
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `8917f9f`) reddened `InvoiceAddressFormatterTests`, restore greened it

### PPW-522 — Unbuildable invoice stays Pending forever and starves the upload batch

- **What:** An invoice whose XML/PDF build always throws (null City NRE, missing ShippingAddress) now gets RecordPendingErrorAsync + claim release and stays Pending with no escalation to Failed. Its old CreatedAt sorts first in `OrderBy(CreatedAt).Take(MaxBatchSize)`; once 50 such rows exist, no newly paid order is ever submitted to ANAF.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:62`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:206`; `Services/Invoicing/InvoiceLifecycle.cs:44`; `Services/Invoicing/InvoiceLifecycle.cs:84`; `Services/Invoicing/InvoiceXmlBuilder.cs:116`; `Controllers/AdminInvoicesController.cs:105`
- **Suggested fix:** Escalate a repeated build failure to Failed (reuse the BackoffHours budget check), or exclude rows whose LastError is set and UpdatedAt is recent from the batch query. Suggested test: InvoiceUploadJobTests "starved_batch_never_reaches_newer_invoice": MaxBatchSize=1; invoice A (older CreatedAt, xmlBuilder throws) + invoice B (newer, buildable). Run two ticks. Assert UploadAsync never called for B, and A never MarkFailed.
- **History:**
  - v6: found by the delta pass — raised by correctness (convergence 1), verdict confirmed
  - v6: fix round — starvation half only: a row that just errored waits one poll interval. The drafted terminal state was a no-op against a CAS expecting Submitted, and needs an ADR superseding ADR-024
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `03f71c5`) reddened `InvoiceUploadJobTests`, restore greened it; note: fixes only the small-N case — a population of 50+ broken rows still rotates through the batch roughly every other tick

### PPW-523 — Missing invoice PDF blob surfaces as an unlogged generic 500 with no distinct event

- **What:** Storage:Provider flips to S3 after invoices were rendered to local disk. Every customer download throws FileNotFoundException here; unmapped, it becomes a 500 whose only log is the middleware's generic "Unhandled exception" line with no invoice id or storage key, plus a Sentry event per request — indistinguishable from a real S3 outage.
- **Evidence:** `Controllers/InvoicesController.cs:69`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:125`; `Services/S3StorageService.cs:119`; `Services/LocalStorageService.cs:74`; `Middleware/ExceptionHandlerMiddleware.cs:11`; `Middleware/ExceptionHandlerMiddleware.cs:139`
- **Suggested fix:** Catch FileNotFoundException, log a distinct Warning with invoice_id and key (mirroring UploadService's uploads.original.missing_file), and return 404 with Retry-After. Suggested test: GetInvoice_WhenPdfBlobMissing_Returns404: seed owned order + Invoice with PdfStoragePath set to a key absent from the configured store; GET /api/orders/{id}/invoice; assert 404 (today 500) and a distinct invoices.pdf_blob_missing log event.
- **History:**
  - v6: found by the delta pass — raised by observability (convergence 1), verdict confirmed
  - v6: fix round — invoice.pdf.blob-missing plus a 404 with no Retry-After, distinct from the not-yet-rendered case
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `7e4a215`) reddened `InvoicesControllerTests`, restore greened it; note: holds for a single-tier miss, but PPW-517's later fallback reopens the same failure mode for a double-tier miss (PPW-550)

### PPW-524 — The whole invoicing feature has no SPA consumer and no lens covered the frontend

- **What:** grep for invoice|factur across src/PhotoPrint.UI/src/app returns zero non-spec hits. Customer download, admin list, retry and XML endpoints have no caller, so the new dual-auth and admin audit logging can never be exercised in the product; a shipped bolt delivers no user-visible invoicing.
- **Evidence:** `Controllers/InvoicesController.cs:16`; `Controllers/InvoicesController.cs:40`; `Controllers/AdminInvoicesController.cs:19`; `Controllers/AdminInvoicesController.cs:92`; `Controllers/AdminInvoicesController.cs:145`; `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:63`
- **Suggested fix:** Confirm with the owner whether UI is a later bolt; if so record it explicitly, otherwise add the customer download link and admin invoice page.
- **History:**
  - v6: found by the delta pass — raised by completeness-critic (convergence 1), verdict plausible
  - v6: fix round — deferred on the owner ruling that the missing SPA consumer is out of scope
  - v6: re-affirmed @`2979ea0` — the owner ruling parking this out of scope is on record (worklog.jsonl round 5 gate-closed, 2026-08-20)

### PPW-525 — Guest invoice access is defeated by the unchanged guest-session lifetime and the never-implemented order transfer

- **What:** GuestSession.IsValid = !IsExpired && !IsClaimed with a 7-day TTL; GuestSessionService.ClaimAsync leaves "Order transfer: deferred" so Order.UserId stays null. A guest who registers, or waits 8 days, gets 401/403 forever on their own invoice PDF that must be retained for years.
- **Evidence:** `Controllers/InvoicesController.cs:52`; `Models/GuestSession.cs:16`; `Services/GuestSessionService.cs:53`; `Authentication/GuestAuthenticationHandler.cs:44`; `BackgroundJobs/GuestSessionCleanupJob.cs:52`; `Extensions/ClaimsPrincipalExtensions.cs:12`
- **Suggested fix:** Transfer Order.UserId (and clear GuestSessionId) in ClaimAsync, or authorise invoice reads by a long-lived per-order token instead of the guest session. Suggested test: GetInvoice_AfterGuestClaimsSession_ReturnsPdfToRegisteredUser: arrange guest order+invoice PDF, call ClaimAsync(G, U), act GET /api/orders/{id}/invoice with U's JWT, assert 200 — currently 403 (and 401 with the guest token).
- **History:**
  - v6: found by the delta pass — raised by completeness-critic (convergence 1), verdict confirmed
  - v6: fix round — deferred: needs the order-transfer capability that was never built
  - v6: re-affirmed @`2979ea0` — GuestSessionService.ClaimAsync still never transfers Order.UserId, and no alternate access path exists

### PPW-526 — EuPlatesc paid leg's new three-state outcome and its rollback have no endpoint-driven test

- **What:** Only the Stripe leg is driven end to end. Inject "EuPlatesc records Ok regardless of outcome", or leave EuPlatescTransactionId set after the rollback, and no test goes red — its metrics tests stub CreateForOrderAsync to return null, so AlreadyInvoiced/NumberExhausted never occur there.
- **Evidence:** `Controllers/WebhooksController.cs:205`; `Controllers/WebhooksController.cs:385`; `Controllers/WebhooksController.cs:392`; `Tests/Unit/Controllers/WebhooksControllerMetricsTests.cs:59`; `Tests/Unit/Controllers/WebhooksControllerInvoiceRaceTests.cs:252`; `Tests/Integration/PaymentControllerIntegrationTests.cs:541`
- **Suggested fix:** Mirror StripeWebhook_WhenInvoiceNumberRetriesExhaust_... for EuPlatescIpnAsync: assert the failed label and that Status, PaidAt and EuPlatescTransactionId are all rolled back. Suggested test: EuPlatesc_ipn_when_invoice_number_retries_exhaust: seed a rival invoice with AlwaysSameInvoiceNumbering on SQLite, POST signed action=0 IPN; assert metric label failed (no duplicate/ok), order back to AwaitingPayment, PaidAt and EuPlatescTransactionId null.
- **History:**
  - v6: found by the delta pass — raised by completeness-critic, tests-coverage (convergence 2), verdict confirmed
  - v6: re-raises a decided item, PPW-508. Prior decision, verbatim: Owner ruled 2026-08-20 that EuPlatesc is being removed and only Stripe will remain, so this coverage was waived rather than written; PPW-511 records that the removal is tracked nowhere.
  - v6: correction to the line above, which is labelled verbatim but paraphrases. The ruling is recorded in the v4 resolution and on the PPW-511 row; PPW-508 carries no quotable History line for it.
  - v6: fix round — wont-fix on the owner ruling that EuPlatesc is being removed; PPW-511 tracks that the removal is untracked
  - v6: re-affirmed @`2979ea0` — the owner ruling stands, and the untracked-removal gap stays tracked via open PPW-511

### PPW-527 — Only the classified exhaust path is metric-safe; other invoice-creation failures still escape RecordPaymentWebhook

- **What:** Numbering service down, or an FK/NOT NULL DbUpdateException, throws out of SaveOrderPaidWithInvoiceAsync — no catch matches, RecordPaymentWebhook never runs, the charge vanishes from the SLO metric. The new test Stripe_succeeded_when_invoice_creation_throws... pins that escape and asserts DB state only, never the metric.
- **Evidence:** `Controllers/WebhooksController.cs:397`; `Controllers/WebhooksController.cs:403`; `Controllers/WebhooksController.cs:286`; `Controllers/WebhooksController.cs:122`; `Services/Invoicing/PostgresInvoiceNumberingService.cs:59`; `Tests/Unit/Controllers/WebhooksControllerMetricsTests.cs:356`
- **Suggested fix:** Wrap the helper call so any exception records a failed webhook metric before rethrowing, and assert the metric in that test. Suggested test: Reuse Stripe_succeeded_when_invoice_creation_throws...: arrange same (creator throws), add `using var metrics = Capture()`, act, then assert metrics.For(PaymentWebhookTotal, Result=Failed) has count 1 — currently empty.
- **History:**
  - v6: found by the delta pass — raised by completeness-critic (convergence 1), verdict confirmed
  - v6: fix round — an unclassified invoice-creation failure now records the webhook before rethrowing; cancellation stays deliberately unrecorded
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `b108c25`) reddened `WebhooksControllerMetricsTests`, restore greened it; note: the cancellation guard is unconditional, weaker than the `AwbCreator` precedent it claims to match

### PPW-528 — Charged-but-unpaid order emits the same metric label as a routine card decline

- **What:** Invoice numbering breaks; every paid webhook charges the customer, leaves the order AwaitingPayment and emits result=failed — the identical label emitted for every payment_intent.payment_failed and every EuPlatesc non-zero action. In the Grafana webhook panel the incident hides inside normal decline volume; no alertable series isolates it.
- **Evidence:** `d:\photo printing website\src\PhotoPrint.API\Controllers\WebhooksController.cs:389`; `d:\photo printing website\src\PhotoPrint.API\Controllers\WebhooksController.cs:423`; `d:\photo printing website\src\PhotoPrint.API\Controllers\WebhooksController.cs:342`; `d:\photo printing website\src\PhotoPrint.API\Controllers\WebhooksController.cs:302`; `d:\photo printing website\src\PhotoPrint.API\Observability\MetricNames.cs:58`; `d:\photo printing website\src\PhotoPrint.API\Services\Invoicing\SqliteInvoiceNumberingService.cs:6`
- **Suggested fix:** Add a distinct WebhookResultValues entry (e.g. invoice_number_exhausted) to MetricNames and LabelContract, use it here, and graph/alert on it separately. Suggested test: WebhookMetricsTests.ExhaustedInvoiceNumberEmitsDistinctResult: arrange colliding invoice number + AwaitingPayment order; act POST /api/webhooks/stripe payment_intent.succeeded with a MeterListener; assert result tag differs from the payment_failed decline's "failed".
- **History:**
  - v6: found by the delta pass — raised by observability (convergence 1), verdict confirmed
  - v6: fix round — deferred: the new value is only useful with a dashboard panel this round should not add
  - v6: re-affirmed @`2979ea0` — MetricNames.WebhookResultValues still has no distinct exhausted-numbering value; a fixer judgment call, not an owner ruling

### PPW-529 — No test applies the migration chain — the unique-index DDL is only ever proven via EnsureCreated from the model

- **What:** PostgresFixture opens a Npgsql context and calls only CREATE SEQUENCE/nextval — no Migrate() or EnsureCreated. A CI run is green against a real Postgres while the Invoices DDL (new unique index, ClaimedAt timestamptz, the expression index above) is never executed; the first prod boot is still the first execution.
- **Evidence:** `Tests/Integration/PostgresInvoiceNumberingServiceIntegrationTests.cs:79`; `Tests/Unit/Data/UploadThumbnailPathMigrationTests.cs:48`; `Migrations/20260603101910_AddVatAndInvoices.cs:103`; `Migrations/20260813102545_AddInvoiceClaimedAt.cs:14`; `Program.cs:387`; `.github/workflows/ci.yml:74`
- **Suggested fix:** Add an IAsyncLifetime that runs db.Database.Migrate() once against the CI container (and a test asserting ix_invoices_order_id is unique via pg_index). Suggested test: PostgresMigrationChainTests.Migrate_OnPostgres_CreatesInvoicesSchema: SkippableFact on the CI Postgres; act db.Database.Migrate(); assert uq_invoices_series_year_number in pg_indexes and ClaimedAt is timestamptz. Reddens today with 42P17 "functions in index expression must be marked IMMUTABLE".
- **History:**
  - v6: found by the delta pass — raised by completeness-critic, tests-coverage, db-parity (convergence 3), verdict confirmed
  - v6: fix round — PostgresTestDatabase migrates, so every Postgres test applies the real chain; three tests pin that and the composite index
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `8aea0b7`) reddened its three named test classes (`TestOrders`, `MigrationChainTests`, `WebhooksControllerInvoiceRaceTests`), restore greened them
  - v6: naming correction — `TestOrders` is a fixture helper and holds no tests of its own; the tests that reddened are `MigrationChainTests` and `WebhooksControllerInvoiceRaceTests`

### PPW-530 — MakeInvoiceOrderIdUnique creates the unique index with no dedupe step, so duplicate rows fail prod boot

- **What:** Any environment whose Invoices table already holds two rows for one OrderId — possible precisely because the index was non-unique before this fix — fails CREATE UNIQUE INDEX. Migrations run at boot in prod, so the API never starts. No test applies the chain to a DB pre-seeded with duplicates.
- **Evidence:** `Migrations/20260813093709_MakeInvoiceOrderIdUnique.cs:17`; `Migrations/20260603101910_AddVatAndInvoices.cs:93`; `Program.cs:387`; `Services/Invoicing/InvoiceCreationService.cs:39`; `Services/Invoicing/InvoiceCreationService.cs:61`
- **Suggested fix:** Delete-or-report duplicates in Up before CreateIndex, and add a test that migrates to the previous migration, inserts two invoices for one order, then migrates to head. Suggested test: MigrationChain_DedupesInvoicesBeforeUniqueIndex: arrange a Postgres (Testcontainers) migrated to 20260728105412, insert two Invoices sharing one OrderId; act Migrate(); assert no throw and one row survives. Reddens today with 23505.
- **History:**
  - v6: found by the delta pass — raised by tests-coverage (convergence 1), verdict confirmed
  - v6: fix round — false positive now: the migration it names was deleted by the Postgres-only squash, and one baseline builds the index on an empty database
  - v6: re-affirmed @`2979ea0` — the named migration file is confirmed deleted; InitialPostgres builds the table and its unique index together, so no dedupe scenario can occur

### PPW-531 — Unique-violation classifiers cover only 2 of 3 Invoices unique indexes and their Npgsql arms are untested

- **What:** On Postgres a duplicate (Series, year, Number) insert can report constraint uq_invoices_series_year_number (a Postgres-only expression index). Both classifiers return false, the DbUpdateException escapes the paid-webhook handler as a 500 with no retry and no metric label. Race tests only run SQLite, so the whole PostgresException arm is unexercised.
- **Evidence:** `Controllers/WebhooksController.cs:460`; `Controllers/WebhooksController.cs:202`; `Services/Invoicing/InvoiceCreationService.cs:76`; `Services/Invoicing/InvoiceNumber.cs:11`; `Migrations/20260603101910_AddVatAndInvoices.cs:112`; `Tests/Unit/Controllers/WebhooksControllerInvoiceRaceTests.cs:31`
- **Suggested fix:** Treat uq_invoices_series_year_number as an InvoiceNumber violation too, and add a Postgres-backed test that provokes a real 23505 for each index.
- **History:**
  - v6: found by the delta pass — raised by tests-coverage, db-parity (convergence 2), verdict plausible
  - v6: fix round — the composite index is classified as a number collision, proven by a real violation naming the constraint
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `8aea0b7`) reddened its three named test classes (`TestOrders`, `MigrationChainTests`, `WebhooksControllerInvoiceRaceTests`), restore greened them
  - v6: naming correction — `TestOrders` is a fixture helper and holds no tests of its own; the tests that reddened are `MigrationChainTests` and `WebhooksControllerInvoiceRaceTests`

### PPW-532 — One ANAF credential failure fans out into up to 50 Error logs and Sentry captures per tick

- **What:** The ANAF cert expires. Each of up to MaxBatchSize=50 pending invoices throws AnafAuthException, so every tick emits 50 identical Error logs and 50 Sentry captures for one root cause. Sentry rate-limits and, per the note in Program.cs, silently drops events on 429 — including unrelated errors.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:62`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:85`; `Services/Invoicing/Anaf/AnafSpvClient.cs:60`; `Services/Invoicing/Anaf/AnafAuthHandler.cs:50`; `Configuration/AnafSettings.cs:27`; `Program.cs:62`
- **Suggested fix:** Break out of the batch loop on the first AnafAuthException (or capture once per tick behind a flag) and let the next tick retry. Suggested test: InvoiceUploadJobTests.Auth_failure_reports_once_per_tick: seed 3 Pending invoices, IAnafSpvClient.UploadAsync always throws AnafAuthException, run one tick, assert IHub.CaptureEvent/CaptureException invoked once (currently 3) and one Error log.
- **History:**
  - v6: found by the delta pass — raised by observability (convergence 1), verdict confirmed
  - v6: fix round — one credential failure logs and captures once per tick and summarises the rest
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `5300b78`) reddened `InvoiceUploadJobTests`, restore greened it

### PPW-533 — ANAF auth failures leave LastError blank, so the admin invoice list shows no reason

- **What:** Credentials are revoked; every pending invoice hits this catch. Unlike the upload-errors, unreachable and build-failed branches, it never calls RecordPendingErrorAsync, so the admin list shows rows stuck Pending with an empty LastError column — the ops-facing surface says nothing while nothing progresses for days.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:82`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:235`; `Services/Invoicing/Anaf/AnafSpvClient.cs:60`; `Services/Invoicing/InvoiceLifecycle.cs:44`; `Controllers/AdminInvoicesController.cs:71`; `Tests/Unit/Services/Invoicing/Anaf/InvoiceUploadJobTests.cs:341`
- **Suggested fix:** Call lifecycle.RecordPendingErrorAsync(row.Id, ex.Message) in the AnafAuthException catch so the admin list shows why the invoice is stuck. Suggested test: Extend InvoiceUploadJobTests.ProcessBatchAsync_AnafAuthFails...: after InvokeProcessBatchAsync with UploadAsync throwing AnafAuthException, read the seeded invoice from SQLite and assert LastError is non-empty (and ClaimedAt released). Reddens today.
- **History:**
  - v6: found by the delta pass — raised by observability (convergence 1), verdict confirmed
  - v6: fix round — a status-aware RecordErrorAsync covers Submitted rows, the dominant case
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `5300b78`) reddened `InvoiceUploadJobTests`, restore greened it; note: the Submitted-row claim holds one row at a time across ticks, not every row every tick

### PPW-534 — invoice.creation.allocated is logged pre-commit on every retry attempt, so logs show phantom invoice numbers

- **What:** A number collision retries three times: four "invoice.creation.allocated invoice_number=..." lines are logged, each before SaveChanges, three for numbers never persisted and possibly issued to another order. The retry, exhausted and duplicate-race lines omit invoice_number, so a gap-free-numbering audit built from logs shows phantom and double-issued numbers.
- **Evidence:** `Services/Invoicing/InvoiceCreationService.cs:94`; `Services/Invoicing/InvoiceCreationService.cs:96`; `Controllers/WebhooksController.cs:392`; `Controllers/WebhooksController.cs:420`; `Controllers/WebhooksController.cs:427`; `Tests/Unit/Controllers/WebhooksControllerInvoiceRaceTests.cs:210`
- **Suggested fix:** Log allocation after the successful commit (or mark it provisional), and include invoice_number in the collision-retry, exhausted and duplicate-race lines. Suggested test: Extend WebhooksControllerInvoiceRaceTests exhaust case: arrange winner invoice + AlwaysSameInvoiceNumbering, act webhook, assert logs contain exactly one "invoice.creation.allocated" (currently 4) while only the winner's invoice row exists.
- **History:**
  - v6: found by the delta pass — raised by observability (convergence 1), verdict confirmed
  - v6: fix round — renamed to invoice.creation.number-attempted, because a retry may discard the logged number
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `8aea0b7`) reddened its three named test classes (`TestOrders`, `MigrationChainTests`, `WebhooksControllerInvoiceRaceTests`), restore greened them; note: the collision-retry, exhausted and duplicate-race log lines still omit invoice_number
  - v6: naming correction — `TestOrders` is a fixture helper and holds no tests of its own; the tests that reddened are `MigrationChainTests` and `WebhooksControllerInvoiceRaceTests`

### PPW-535 — Truncate can split a UTF-16 surrogate pair, wedging the invoice in Pending forever

- **What:** Buyer name 201 chars with an emoji straddling index 200: value[..200] leaves a lone surrogate, XmlTextWriter.Save throws, the worker records LastError, releases the claim, and retries the same failure every tick forever. All three truncation tests use plain 'a'/'b'/'c'.
- **Evidence:** `Services/Invoicing/InvoiceAddressFormatter.cs:13`
- **Suggested fix:** Back off to a full code point (and strip lone surrogates) when truncating; add a surrogate-pair boundary test.
- **History:**
  - v6: found by the delta pass — raised by completeness-critic (convergence 1), verdict unverified-low
  - v6: fix round — closed with PPW-521; same method
  - v6: verified @`2979ea0` — revert-and-rerun (isolated worktree, commit `8917f9f`) reddened `InvoiceAddressFormatterTests`, restore greened it

### PPW-536 — RetryAsync resets every ANAF field except ClaimedAt, which the success path never releases either

- **What:** UploadPendingAsync leaves ClaimedAt set when submission succeeds (only the error paths release it). An admin retry within Anaf:ClaimTtlMinutes (default 10) of that claim flips status to Pending but leaves the stale timestamp, so the job's claim WHERE matches nothing and logs claim-lost — the retry silently does nothing for a whole poll interval (default 30 min).
- **Evidence:** `Services/Invoicing/InvoiceLifecycle.cs:117`
- **Suggested fix:** Add .SetProperty(i => i.ClaimedAt, (DateTimeOffset?)null) to RetryAsync, and release the claim on the success path too.
- **History:**
  - v6: found by the delta pass — raised by correctness, db-parity (convergence 2), verdict unverified-low

### PPW-537 — Residual reconciliation is unguarded — negative line amount, silently absorbed snapshot mismatch, crash on an empty line list

- **What:** An order whose snapshot disagrees with its items — e.g. a pre-bolt-038 backfilled row with NetTotalRon=0 (per AddVatAndInvoices' backfill note) — gives rate 0, netTotals = gross per line, residual = -TotalRon. `netTotals[^1] += residual` silently writes a negative LineExtensionAmount and negative PriceAmount instead of failing loudly.
- **Evidence:** `Services/Invoicing/InvoiceXmlBuilder.cs:213`
- **Suggested fix:** Throw when |residual| exceeds a few bani (or when any adjusted net goes negative) rather than folding an arbitrary delta into the last line.
- **History:**
  - v6: found by the delta pass — raised by correctness, tests-coverage (convergence 2), verdict unverified-low

### PPW-538 — Upload batch query ignores ClaimedAt, unlike the existing AWB claim precedent

- **What:** With a backlog larger than MaxBatchSize, every replica selects the same head rows and skips them as claim-lost, so extra replicas add no throughput. A revoked cert makes AnafAuthException fire once per row: up to 50 Sentry events and 50 auth attempts per tick. AwbRetryJob filters AwbClaimedAt inside its query.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:65`
- **Suggested fix:** Add the ClaimedAt/TTL predicate to the batch query, and break out of the batch loop on the first AnafAuthException.
- **History:**
  - v6: found by the delta pass — raised by completeness-critic (convergence 1), verdict unverified-low

### PPW-539 — New ClaimedAt column and unique index never land on an existing dev SQLite database

- **What:** A developer with an existing photoPrint-dev.db: EnsureCreated() no-ops and the self-heal only counts 5 core tables, so Invoices lacks ClaimedAt. Every Invoices query (webhook existing-invoice check, admin list, PDF download) then fails with SQLite "no such column: i.ClaimedAt" until the file is deleted.
- **Evidence:** `Program.cs:358`
- **Suggested fix:** Extend the startup completeness check to probe pragma_table_info('Invoices') for ClaimedAt and drop/recreate (or ALTER) when missing.
- **History:**
  - v6: found by the delta pass — raised by db-parity (convergence 1), verdict unverified-low

### PPW-540 — Postgres numbering tests draw a random year and assert absolute sequence values, so they collide and leak sequences

- **What:** RandomYear() picks from 1000 values and sequences persist for the container's life. If SequentialCalls draws 3117 while YearRolls draws 3116 (yearB = 3117), the second test's first call returns 2 and the assertion fails — a ~1%-per-run flake that a re-run "fixes", eroding trust in the only Postgres proof.
- **Evidence:** `Tests/Integration/PostgresInvoiceNumberingServiceIntegrationTests.cs:16`
- **Suggested fix:** Make the sequence key unique per test (unique uppercase series letters, or DROP SEQUENCE in a finally) instead of relying on an unused random year.
- **History:**
  - v6: found by the delta pass — raised by tests-coverage, db-parity (convergence 2), verdict unverified-low

### PPW-541 — claim-lost log asserts "another worker" for causes it cannot distinguish

- **What:** An admin retries a Rejected invoice; InvoiceLifecycle.RetryAsync clears every field except ClaimedAt, so a worker restart inside the TTL skips the row and logs "another worker holds a fresh claim" on a single-replica deployment. The operator hunts a second worker that does not exist; a deleted row or a status change logs the same sentence.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:141`
- **Suggested fix:** Clear ClaimedAt in InvoiceLifecycle.RetryAsync, and log the row's actual ClaimedAt and status in claim-lost rather than asserting a cause.
- **History:**
  - v6: found by the delta pass — raised by observability (convergence 1), verdict unverified-low

### PPW-542 — submitted-but-not-recorded logs Error twice and gets no Sentry capture

- **What:** MarkSubmittedAsync's DB write fails after ANAF accepted the upload. This Error is logged, then rethrown, so the batch loop logs a second Error (row-failed) for the same incident — double-counted by any log-based alert — while this worst partial state (ANAF holds the invoice, row stays Pending, next tick re-uploads) gets no Sentry capture and no metric.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:226`
- **Suggested fix:** Log once (don't rethrow after logging) and capture this partial state to Sentry or a dedicated metric, as the auth branch does.
- **History:**
  - v6: found by the delta pass — raised by observability (convergence 1), verdict unverified-low

### PPW-543 — LastError is persisted before the exception is logged, so a DB blip loses the root cause

- **What:** The PDF renderer throws; RecordPendingErrorAsync on this line then fails because the DB connection dropped. The renderer exception is never logged — only the batch loop's row-failed with the DB exception — and ReleaseClaimAsync on line 208 never runs, so the row is skipped for a full TTL with no clue to the real cause.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:206`
- **Suggested fix:** Log the caught exception before persisting LastError, and wrap the RecordPendingErrorAsync call in its own try/catch so the claim release still runs.
- **History:**
  - v6: found by the delta pass — raised by observability (convergence 1), verdict unverified-low

### PPW-544 — New Must rules have no WithMessage, so 400s carry English default messages

- **What:** A control character in Street returns FluentValidation's default "The specified condition was not met for 'Street'." in an otherwise Romanian API surface; the SPA shows English text. Same for Number, Block, City, County, PostalCode, RecipientName.
- **Evidence:** `Validators/Payments/CreateOrderRequestValidator.cs:32`
- **Suggested fix:** Add a Romanian .WithMessage("... conține caractere nevalide.") to each HasNoXmlInvalidChars rule, as UpdateAccountValidator already does.
- **History:**
  - v6: found by the delta pass — raised by correctness (convergence 1), verdict unverified-cleanup

### PPW-545 — CreateForOrderAsync(Guid) has no production caller left

- **What:** Both webhook paths and AdminOrderService now use the Order overload; the Guid overload is exercised only by InvoiceCreationServiceTests, so its order-missing guard and idempotency path are dead code that future callers may pick up believing it is the supported entry point.
- **Evidence:** `Services/Invoicing/IInvoiceCreationService.cs:24`
- **Suggested fix:** Delete the Guid overload and its interface member, or document it as test-only.
- **History:**
  - v6: found by the delta pass — raised by correctness, completeness-critic (convergence 2), verdict unverified-cleanup

### PPW-546 — Retry pre-read pulls the whole XmlPayload from the DB just to log its length

- **What:** Each admin retry selects i.XmlPayload — a full UBL invoice document, hundreds of KB — over the wire so it can log XmlPayload?.Length, immediately discarding it.
- **Evidence:** `Services/Invoicing/InvoiceLifecycle.cs:104`
- **Suggested fix:** Project the length instead: .Select(i => new { XmlLength = i.XmlPayload!.Length, i.LastError }) — both providers translate it to length().
- **History:**
  - v6: found by the delta pass — raised by db-parity (convergence 1), verdict unverified-cleanup

### PPW-547 — data-stack standard never mentions the Invoices table it must describe

- **What:** The DB standard lists "Entities (17 DbSets)" without Invoice (the context now has 18) and has zero mentions of invoicing: not the new unique index on OrderId, not the ClaimedAt claim column, not the only SEQUENCE and only raw-SQL expression index in the schema. The next agent reading it for DB routing misses the whole invoicing surface.
- **Evidence:** `memory-bank/standards/data-stack.md:55`
- **Suggested fix:** Add Invoice to the entity list and a short invoicing paragraph covering the sequence, the expression index, the unique indexes and ClaimedAt.
- **History:**
  - v6: found by the delta pass — raised by db-parity (convergence 1), verdict unverified-cleanup

### PPW-548 — ADR-023/decision-index still credit CAS for multi-replica safety, now superseded by the ClaimedAt lease

- **What:** A future agent reads the decision index, designs against "CAS gives multi-replica safety", and removes or duplicates the ClaimedAt claim. CLAUDE.md requires the standard stating a contract to be updated in the same change; the code comment was rewritten but the index was not.
- **Evidence:** `memory-bank/standards/decision-index.md:34`
- **Suggested fix:** Update the ADR-023 summary (and note the Anaf:ClaimTtlMinutes lease alongside ADR-015's Orders.AwbClaimedAt precedent).
- **History:**
  - v6: found by the delta pass — raised by completeness-critic (convergence 1), verdict unverified-cleanup

### PPW-549 — Unknown ANAF status warns twice and the job's line drops the diagnostic fields

- **What:** ANAF returns an unmapped "stare". AnafSpvClient line 134 warns with the stare value and the job warns again without it, so one event produces two Warnings; the job's line carries neither stare nor AnafUploadId, and no metric or Failed escalation exists, so the invoice re-polls forever with no countable signal.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:309`
- **Suggested fix:** Drop the job's duplicate warning or include stare and AnafUploadId, and count unknown statuses on the ANAF status metric.
- **History:**
  - v6: found by the delta pass — raised by observability (convergence 1), verdict unverified-cleanup

### PPW-550 — InvoicesController's tier-mismatch fallback re-throws an unhandled FileNotFoundException on a double-tier blob miss

- **What:** PPW-517's fallback calls the other tier's GetStreamAsync with no try/catch of its own; when the blob is missing from both tiers the second FileNotFoundException escapes to the generic middleware as an unlogged 500, reintroducing what PPW-523 fixed for that case. No test covers the fallback branch.
- **Evidence:** `Controllers/InvoicesController.cs:84-94`; `Services/LocalStorageService.cs:74`; `Services/S3StorageService.cs:119`; `Middleware/ExceptionHandlerMiddleware.cs:140-167`.
- **Suggested fix:** Wrap the fallback's GetStreamAsync in its own try/catch (or loop over candidate tiers) so a double-miss also lands on the invoice.pdf.blob-missing 404 path; add a test for it.
- **History:**
  - v6: found by the v6 verification pass's fix-diff review of clusters C/D/E — fix-generated by PPW-517
  - v7: fix round — the fallback read is guarded, so a miss on every candidate tier answers the same 404 and blob-missing event as a single-tier miss. Two corrections to the finding: the escaping exception was logged by the middleware, so the defect was the status code and the lost event, not silence; and routing this case to the 404 deliberately drops the Sentry event it used to get, matching the single-tier miss. The log now carries tiers_tried and the adapter's inner cause, because S3 maps a missing bucket to the same 404 as a missing key
  - v7: verified @`0ec6497` — revert-and-rerun (isolated worktree, commit `f602d4b`) reddened `InvoicesControllerTests`, restore greened it; note: the follow-up's bucket-versus-key cause preference carries no test of its own (PPW-554)

### PPW-551 — ANAF credential-failure log/capture dedup resets every tick, unlike the sibling cross-tick outage window

- **What:** PPW-532's authFailed flag is a local variable reset on every call to ProcessBatchAsync, so a multi-day ANAF credential outage still emits one Error log and one Sentry capture per tick for its whole duration. ShipmentTrackingJob/TrackingStopRegistry.MarkOutageOnce already solves the identical problem with a cross-tick outage window, and is unused here.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:92`; `Services/Sameday/TrackingStopRegistry.cs:26-28`; `Services/Sameday/ShipmentTrackingJob.cs:157-166`.
- **Suggested fix:** Reuse or mirror TrackingStopRegistry.MarkOutageOnce's cross-tick outage window for the ANAF credential-failure log/capture.
- **History:**
  - v6: found by the v6 verification pass's fix-diff review of clusters A/B — not fix-generated: the within-tick fan-out was worse before the fix (up to 50 events per tick vs. 1), so this is a residual gap the fix narrowed rather than a defect it created
  - v7: fix round — a new AnafOutageRegistry gates the Error and the Sentry capture behind a flat 2 h window; the shared MemoryCacheOnceRegistry base moved out of the Sameday namespace to carry it. The approach-check refuted deriving the window from PollIntervalMinutes, valid up to 1440, which could outlive the 5-business-day submission SLA it protects. No counter was added, on the PPW-528 precedent; the deduped tick logs auth-outage-continues at Warning instead, so silence means recovery. One page per replica per window, not per outage — the per-process limit PPW-455 tracks. Class sweep: this was the only Sentry capture in any background job, so no other site paged; the remaining per-tick Error and Warning sites, including anaf.upload-job.batch-failed and status-unknown, are log noise and stay as they are
  - v7: verified @`0ec6497` — the script refused this row as `rename-in-fix`, because the fix commit moved the shared registry base, so the proof was run by hand: forcing the outage gate open in the isolated worktree failed `ProcessBatchAsync_AuthStillFailingOnTheNextTick_DoesNotPageASecondTime`, and restoring it returned 21 of 21 green in `InvoiceUploadJobTests`
  - v7: note — the window is a flat 2 h with no floor tied to the poll interval, which a validator-legal interval defeats (PPW-553); the outage key is the bare string `auth`, so a different credential cause inside the window folds into the first, matching the accepted Sameday pattern

### PPW-552 — PPW-515's fix orphaned `AnafUnreachableException`'s XML doc comment

- **What:** PPW-515's fix inserted AnafUploadTimeoutException between AnafUnreachableException's pre-existing doc comment and the class it describes, with no blank line between the two blocks. AnafUnreachableException now has no doc comment, and the orphaned paragraph reads as if it documents the new type instead.
- **Evidence:** `Services/Invoicing/Anaf/AnafExceptions.cs:32-47`.
- **Suggested fix:** Move the new one-line summary above the new class and restore the original block immediately above AnafUnreachableException.
- **History:**
  - v6: found by the v6 verification pass's fix-diff review of clusters A/B — fix-generated by PPW-515; 🟡, entered ledger as `backlog` per README router

### PPW-553 — The 2 h ANAF auth-outage window has no floor tied to PollIntervalMinutes, so a validator-legal interval above it defeats the dedup

- **What:** `AuthOutageAlertWindow` is a flat 2 h constant while `AnafSettingsValidator` accepts `PollIntervalMinutes` up to 1440. Any configured interval above 120 minutes makes each tick land outside the previous window, so the credential Error and the Sentry capture fire every tick again and the dedup PPW-551 added silently stops working. Nothing warns that the two settings disagree.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:17`; `Configuration/AnafSettingsValidator.cs:39-40`.
- **Suggested fix:** Derive the window from the poll interval with a floor and a cap — for example the greater of 2 h and two poll intervals, capped well inside the submission deadline — or reject the disagreeing configuration in the validator. Suggested test: configure `PollIntervalMinutes` above the window, drive two ticks with a failing credential, and assert one Error, not two.
- **History:**
  - v7: found by the v7 verification pass's fix-diff review — fix-generated by PPW-551, whose own mechanism carries the gap

### PPW-554 — The bucket-versus-key miss-cause preference has no regression test

- **What:** The miss-cause preference that distinguishes a missing S3 bucket from a missing key was added by a micro-review follow-up and no test covers it. Reverting that follow-up leaves the whole suite green, and this exact logic was already wrong once in the same round.
- **Evidence:** `Controllers/InvoicesController.cs:83-87`.
- **Suggested fix:** Add one test per cause, asserting the logged event carries the adapter's inner cause for a missing bucket and for a missing key.
- **History:**
  - v7: found by the v7 verification pass's fix-diff review — fix-generated by PPW-550's follow-up; 🟡, entered ledger as `backlog` per README router
