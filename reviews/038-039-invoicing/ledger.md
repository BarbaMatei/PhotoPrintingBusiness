---
type: review-ledger
target: 038-039-invoicing
updated: 2026-08-14
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
| PPW-508 | 🟡 | v2 | Exhausted invoice-number retries now answer the payment processor 200 and count as `duplicate` | `Controllers/WebhooksController.cs:414` | open | `08e7746` |
| PPW-509 | 🟡 | v3 | `CustomerEmailAttachmentSettings` docstring still says the XML, ANAF and PDF pipeline runs unconditionally | `Configuration/InvoicingSettings.cs:18` | open | `08e7746` |
| PPW-510 | 🟡 | v3 | ADR-022 left stale while the deployment guide and the decision index send an operator to it as current authority | `docs/DEPLOYMENT.md:1309` | open | `08e7746` |

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
