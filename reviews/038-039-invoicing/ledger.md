---
type: review-ledger
target: 038-039-invoicing
updated: 2026-08-13
---

# Ledger — 038-039-invoicing

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-469 | 🔴 | v1 | Invoice PDF retrieval bypasses IStorageRouter, always reads local disk | `Controllers/InvoicesController.cs:22` | open | `e724528` |
| PPW-470 | 🔴 | v1 | Invoice PDF generation/upload bypasses IStorageRouter, always writes local disk (ADR-008) | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:156` | open | `e724528` |
| PPW-471 | 🔴 | v1 | Invoice creation is check-then-act with no DB uniqueness — concurrent webhooks can mint two fiscal invoices | `Services/Invoicing/InvoiceCreationService.cs:40` | open | `e724528` |
| PPW-472 | 🔴 | v1 | InvoicePdfReadyNotifier never sends an email regardless of the flag, logs a false "sent" event | `Services/Invoicing/InvoicePdfReadyNotifier.cs:40` | open | `e724528` |
| PPW-473 | 🔴 | v1 | Guest checkouts can never retrieve their invoice — JWT-only auth on the endpoint | `Controllers/InvoicesController.cs:16` | open | `e724528` |
| PPW-474 | 🔴 | v1 | Orders marked Paid via admin manual reconciliation never get an Invoice row | `Services/AdminOrderService.cs:139` | open | `e724528` |
| PPW-475 | 🔴 | v1 | ANAF upload success + DB commit failure is indistinguishable from never-uploaded | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:175` | open | `e724528` |
| PPW-476 | 🔴 | v1 | No claim/lease on Pending invoices — multi-replica double-submits to ANAF and double-emails | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:120` | open | `e724528` |
| PPW-477 | 🔴 | v1 | No control-character filtering on customer name/address before UBL XML serialization | `Services/Invoicing/InvoiceXmlBuilder.cs:119` | open | `e724528` |
| PPW-478 | 🔴 | v1 | UBL invoice-line amounts are gross, not tax-exclusive — lines won't reconcile with the document total | `Services/Invoicing/InvoiceXmlBuilder.cs:230` | open | `e724528` |
| PPW-479 | 🟠 | v1 | Admin invoice list `Page` param is unbounded — int32 overflow can reach `Skip()` | `Controllers/AdminInvoicesController.cs:57` | open | `e724528` |
| PPW-480 | 🟠 | v1 | Admin "retry" resubmits byte-identical XML — can never fix the failure it exists for | `Services/Invoicing/InvoiceLifecycle.cs:106` | open | `e724528` |
| PPW-481 | 🟠 | v1 | `AnafSettings` docstring's "byte-identical to baseline" claim is false when disabled | `Configuration/AnafSettings.cs:7` | open | `e724528` |
| PPW-482 | 🟠 | v1 | `AdminInvoicesController`'s audit-logging doc-comment is false; the one logged action omits the admin id | `Controllers/AdminInvoicesController.cs:14` | open | `e724528` |
| PPW-483 | 🟠 | v1 | Redundant Order re-query on every paid webhook in `InvoiceCreationService` | `Services/Invoicing/InvoiceCreationService.cs:49` | open | `e724528` |
| PPW-484 | 🟠 | v1 | `InvoiceUploadJob` worker reloads the full Order graph even when only the ANAF step remains | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:246` | open | `e724528` |
| PPW-485 | 🟠 | v1 | Checkout field-length caps are wider than the legal XML limits, with no truncation | `Validators/Payments/CreateOrderRequestValidator.cs:61` | open | `e724528` |
| PPW-486 | 🟠 | v1 | Per-row catch collapses auth failure, network failure, and code bugs into one generic log event | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:91` | open | `e724528` |
| PPW-487 | 🟠 | v1 | Unrecognized ANAF status string is silently treated as "still processing", raw value never logged | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:232` | open | `e724528` |
| PPW-488 | 🟠 | v1 | No domain-tagged log for "customer charged, order not committed" in `WebhooksController` | `Controllers/WebhooksController.cs:205` | open | `e724528` |
| PPW-489 | 🟠 | v1 | Polly retries the non-idempotent ANAF upload POST on ambiguous-outcome errors | `Services/Invoicing/Anaf/AnafResilienceHandler.cs:33` | open | `e724528` |
| PPW-490 | 🟠 | v1 | SQLite invoice numbering's MAX+1 has no transaction/lock despite the comment's safety claim | `Services/Invoicing/SqliteInvoiceNumberingService.cs:41` | open | `e724528` |
| PPW-491 | 🟠 | v1 | `InvoiceUploadJob` has zero tests despite being the most stateful new logic | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:1` | open | `e724528` |
| PPW-492 | 🟠 | v1 | Webhook tests stub invoice creation to always return null; nothing asserts it runs or that failure is handled | `Tests/Unit/Controllers/WebhooksControllerMetricsTests.cs:58` | open | `e724528` |
| PPW-493 | 🟠 | v1 | `PostgresInvoiceNumberingService` — the only prod numbering path — has no test coverage | `Services/Invoicing/PostgresInvoiceNumberingService.cs:1` | open | `e724528` |
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

## Details

### PPW-469 — Invoice PDF retrieval bypasses IStorageRouter, always reads local disk

- **What:** `InvoicesController`/`InvoiceUploadJob` inject the unkeyed `IStorageService` directly — `StorageExtensions.cs` binds that unconditionally to the "local" adapter regardless of `Storage:Provider`. With S3 configured on a multi-replica deploy, a PDF written by one replica 404s/500s on another.
- **Evidence:** `Controllers/InvoicesController.cs:20-26,65`; `Extensions/StorageExtensions.cs:29,64,75-76`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs`.
- **Suggested fix:** Inject `IStorageRouter` in both files; route via `router.CloudEnabled ? router.Cloud : router.Local`, matching every other two-tier caller. **Test shape:** integration test with `Storage:Provider=S3`, seed `PdfStoragePath` only in the S3 fake; GET invoice; assert 404+Retry-After, not an unhandled 500. Not trigger-list-shaped (DI wiring only) — no approach-check run.
- **History:**
  - v1: found — raised independently by 2 lenses (correctness + completeness-critic)

### PPW-470 — Invoice PDF generation/upload bypasses IStorageRouter, always writes local disk (ADR-008)

- **What:** Same root cause as PPW-469, write side: `InvoiceUploadJob.UploadPendingAsync` saves every PDF via the unkeyed `IStorageService` → local disk, contradicting ADR-008/`ddd-01-domain-model.md`'s "invoices never live in the local tier."
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:126,159`; `Controllers/InvoicesController.cs:22`; `memory-bank/bolts/039-efactura-anaf/ddd-01-domain-model.md:26`.
- **Suggested fix:** Same fix as PPW-469 — one `IStorageRouter` change covers both read and write sites; fix as one cluster. **Test shape:** with `Storage:Provider=S3`, assert PDF bytes land in `IStorageRouter.Cloud`, not `.Local`. Not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 2 lenses (requirements + quality) — same root cause as PPW-469, same fix cluster

### PPW-471 — Invoice creation is check-then-act with no DB uniqueness — concurrent webhooks can mint two fiscal invoices

- **What:** `CreateForOrderAsync` checks for an existing Invoice then inserts, with no unique constraint on `Invoices.OrderId`. Two near-concurrent webhook deliveries (Stripe retry-on-timeout) can each pass the check and each submit a separate legal invoice to ANAF for one order.
- **Evidence:** `Services/Invoicing/InvoiceCreationService.cs:40`; migration `20260603101910_AddVatAndInvoices.cs` (OrderId is a non-unique index only).
- **Suggested fix:** Add a unique index on `Invoices.OrderId` (new EF migration; SQLite `EnsureCreated` gets it free, Postgres needs the migration plus a pre-flight check for existing duplicate rows before it can run safely at boot). The catch does **not** belong inside `CreateForOrderAsync` — that method never calls `SaveChangesAsync` (by design, per `IInvoiceCreationService.cs:20`, so the Invoice insert rides the caller's transaction). Catch the provider-specific unique-violation (mirror `OrderService.IsIdempotencyKeyViolation`) at both `WebhooksController` `SaveChangesAsync` call sites (EuPlatesc `:206`, Stripe `:286`), detach the failed entities, re-read the winner, and gate the four side effects (email, cloud-promotion enqueue, AWB-notify, broadcast) behind a replay signal so they don't fire twice. Same catch site and pattern as PPW-490 — fix together. **Test shape:** two concurrent webhook deliveries for one order; assert exactly one Invoice row and side effects fire once. Trigger-list-shaped (concurrency model change) — **approach-check: revised** (drafted "catch inside `CreateForOrderAsync`" is a no-op; corrected to the controller call sites, with a replay signal and a migration pre-flight step).
- **History:**
  - v1: found — raised independently by 3 lenses (correctness + security + race)
  - v1: approach-check run — revised (catch site corrected to WebhooksController; replay-signal and migration pre-flight added)

### PPW-472 — InvoicePdfReadyNotifier never sends an email regardless of the flag, logs a false "sent" event

- **What:** `NotifyAsync` logs `invoice.pdf-ready.sent` and returns — no `IEmailService` call exists. `OrderEmailService` has zero Invoice references either. Flipping `Invoicing:CustomerEmailAttachments:Enabled` to true (per the settings file's own instruction) delivers nothing to customers, while the log claims success.
- **Evidence:** `Services/Invoicing/InvoicePdfReadyNotifier.cs:30-55`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:130,166`.
- **Suggested fix:** Implement the two documented integration points from `ddd-02-technical-design.md` (attach PDF to the order-confirmation email; send a real follow-up email in `NotifyAsync`) — or, if not shipping this round, change the log line so it doesn't claim `sent` and flag the settings docstring as not-yet-implemented. **Test shape:** inject a mock `IEmailService`, flag=true, call `NotifyAsync`, assert a send was invoked. Not trigger-list-shaped as a doc/log-only interim fix; becomes trigger-list-shaped only if a real email integration ships this round (new external call, not a background job/cache/retry) — no approach-check run yet, pending the owner's choice of scope.
- **History:**
  - v1: found — raised independently by 2 lenses (requirements + observability)

### PPW-473 — Guest checkouts can never retrieve their invoice — JWT-only auth on the endpoint

- **What:** `GET /api/orders/{orderId}/invoice` uses plain `[Authorize]` (JWT-only) + `GetUserIdOrNull()`, which returns null for guest-token requests — every guest gets 401, always. Every other order-scoped endpoint (Cart/Payments/Uploads) uses the dual-auth policy plus a guest-session ownership check.
- **Evidence:** `Controllers/InvoicesController.cs:16,41-51`; `Extensions/ClaimsPrincipalExtensions.cs:9-17`; `Extensions/GuestSessionExtensions.cs:10-28`.
- **Suggested fix:** Switch to `[Authorize(Policy = GuestSessionExtensions.DualAuthPolicy)]` and check ownership against both `UserId` and `GuestSessionId`, matching `CartController`/`PaymentsController`. **Test shape:** guest order (UserId=null, GuestSessionId=X), GET invoice with `X-Guest-Token` and no JWT → expect 200/404-pending, not 401. Not trigger-list-shaped (reusing an existing auth pattern).
- **History:**
  - v1: found — raised independently by 1 lens (requirements)

### PPW-474 — Orders marked Paid via admin manual reconciliation never get an Invoice row

- **What:** `AdminOrderService.UpdateStatusAsync`'s Paid branch stamps `PaidAt` and fires confirmation email/AWB notify but never calls `IInvoiceCreationService.CreateForOrderAsync` — only the two webhook handlers do. Every offline/bank-transfer-reconciled order permanently lacks a legally-required invoice.
- **Evidence:** `Services/AdminOrderService.cs:19-54,139-160`; `Controllers/WebhooksController.cs:205,285`.
- **Suggested fix:** Call `CreateForOrderAsync(order.Id, ct)` inside the same Paid branch, before `SaveChangesAsync`, mirroring the webhook handlers. **Test shape:** `UpdateStatusAsync(orderId, "Paid", ...)` with a mocked `IInvoiceCreationService`; assert it's called once. Not trigger-list-shaped (mirrors an existing call pattern).
- **History:**
  - v1: found — raised independently by 1 lens (requirements)

### PPW-475 — ANAF upload success + DB commit failure is indistinguishable from never-uploaded

- **What:** If `anafClient.UploadAsync` succeeds but `lifecycle.MarkSubmittedAsync` then throws (DB blip), the exception isn't caught locally, falls to the generic per-row catch, and logs the same event as any other failure. The invoice stays Pending and gets re-uploaded next tick — a real second POST to ANAF, invisible in logs.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:170-191`; `Services/Invoicing/InvoiceLifecycle.cs:29-42`.
- **Suggested fix:** Add a catch scoped to just the `MarkSubmittedAsync` call, logging the already-obtained `AnafUploadId` at a distinct event name before rethrowing. No durable side-channel needed — `ddd-02-technical-design.md` already documents "ANAF dedupes via InvoiceNumber" as the accepted tolerance for this class of duplicate. Needs a regression test — no test file for `InvoiceUploadJob` exists at all today. **Test shape:** fake `UploadAsync` succeeds, fake `MarkSubmittedAsync` throws; assert the distinct log event fires and the invoice stays retryable. Trigger-list-shaped (new catch/mapping layer) — **approach-check: revised** (scope confirmed to just this one call; skip durable side-channel as over-engineering; add the missing test).
- **History:**
  - v1: found — raised independently by 1 lens (observability)
  - v1: approach-check run — revised (catch scoped correctly; regression test required)

### PPW-476 — No claim/lease on Pending invoices — multi-replica double-submits to ANAF and double-emails

- **What:** Two replicas' `InvoiceUploadJob` can poll the same tick, both pick invoice X, and both proceed through XML build, PDF render, customer notify, and ANAF upload before the final CAS picks a winner — a real duplicate customer email and duplicate ANAF submission. The sibling `AwbCreator` job had this identical race, fixed via a durable per-order claim (ADR-015 amendment) — never ported here.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:71-190`; `Services/Invoicing/InvoiceLifecycle.cs:29-42`; `memory-bank/bolts/037-awb-and-tracking-jobs/adr-015-...md`.
- **Suggested fix:** Add a `ClaimedAt`/lease column (mirror `Order.AwbClaimedAt`), atomic `ExecuteUpdateAsync` claim before the pipeline starts. Must also: release the claim on a retryable-but-not-billed outcome (the AWB precedent does this; the draft initially missed it); size a sensible `Anaf:ClaimTtlMinutes` past the whole pipeline duration, not copied from AWB's 5 minutes; new Postgres migration — watch the known SQLite-`EnsureCreated`-scaffolds-`INTEGER`-via-Unix-ms-converter gotcha that needs hand-editing to `timestamptz` for Postgres (same trap as `20260728060537_AddOrderAwbClaimedAt.cs`); test against SQLite, not EF InMemory (`ExecuteUpdateAsync` isn't supported there). Note as a stated residual, not fixed here: ANAF itself is sent no idempotency key (unlike Sameday's `clientInternalReference`), so a crash-after-POST-before-claim-release window still risks a genuine duplicate submission — an ADR amendment should say so explicitly. Trigger-list-shaped (concurrency model) — **approach-check: revised** (release-on-retry, explicit TTL, migration type gotcha, SQLite test, residual noted).
- **History:**
  - v1: found — raised independently by 1 lens (race)
  - v1: approach-check run — revised (claim-release, TTL sizing, migration gotcha, test fixture, residual note added)

### PPW-477 — No control-character filtering on customer name/address before UBL XML serialization

- **What:** Neither `CreateOrderRequestValidator` nor the account name validators restrict charset on name/address fields. A verification check disproved the originally-claimed mechanism (`XmlTextWriter` does not throw on a control character — it silently emits a malformed character reference); the real risk is a malformed reference silently reaching a legally-binding e-Factura XML, and — confirmed separately — `AnafSpvClient.UploadAsync` maps any hard ANAF rejection (e.g. of malformed XML) to `AnafUnreachableException`, which is *also* uncaught locally and falls into the same silent outer catch with no `LastError` set: the "stuck Pending, invisible" failure mode is real via this second path even though the original mechanism was wrong.
- **Evidence:** `Validators/Payments/CreateOrderRequestValidator.cs:32-61`; `Validators/Account/RegisterRequestValidator.cs`/`UpdateAccountValidator.cs` (FirstName/LastName — the actual primary buyer-name source, same gap); `Services/Invoicing/Anaf/AnafSpvClient.cs:62-66`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:142-151,85-95`.
- **Suggested fix:** Reject (not silently strip) XML-1.0-invalid control characters at checkout/account input time, on **both** the shipping-address validators and the account name validators (`FirstName`/`LastName` — the primary path for logged-in buyers; `RecipientName` is guest/fallback-only). Add a defensive sanitize-and-flag net inside `InvoiceXmlBuilder` **and** `InvoicePdfDocument` (both independently re-derive buyer name) for pre-existing/legacy rows input rejection can't retroactively fix. Independently: wrap Steps 1–2 of `InvoiceUploadJob.UploadPendingAsync` in a local catch that calls `RecordPendingErrorAsync`, and add `AnafUnreachableException` to Step 3's local catch — both close the same "invisible stuck Pending" gap this finding was chasing. Trigger-list-shaped (adds a catch/mapping layer) — **approach-check: revised** (extend to account validators; reject not strip; add the PDF-side defensive net; also fix the two uncaught exception paths named above).
- **History:**
  - v1: found — raised independently by 1 lens (input-validation), not independently confirmed: guard evidence real, originally-claimed crash mechanism disproven by trace, corrected mechanism confirmed by approach-check
  - v1: approach-check run — revised (scope widened per above)

### PPW-478 — UBL invoice-line amounts are gross, not tax-exclusive — lines won't reconcile with the document total

- **What:** `OrderService.cs` sets `OrderItem.LineTotalRon`/`UnitPriceRon` from the gross (VAT-inclusive) listed price — the same value later fed into `VatCalculator.ExtractBreakdown` to derive the header's correctly-net `NetTotalRon`. But `InvoiceXmlBuilder.BuildLine` writes those same gross `OrderItem` values straight into UBL's `LineExtensionAmount`/`Price/PriceAmount`, which per UBL/CIUS-RO must be tax-exclusive. Σ(line amounts) will not equal the document's net total — an internally inconsistent legal e-invoice, found via a supplemental correctness pass dispatched to close the coverage gap PPW-497 named (the pass's original 10 lenses never checked this).
- **Evidence:** `Services/OrderService.cs:85-96,145`; `Services/Invoicing/InvoiceXmlBuilder.cs:193-244`; confirmed no existing test asserts line-vs-header reconciliation (`Tests/Unit/Services/Invoicing/InvoiceXmlBuilderTests.cs`).
- **Suggested fix:** In `BuildInvoiceLines` (not `BuildLine` — the residual-rounding adjustment needs all lines materialized first), derive each line's **net line total** via `VatCalculator` from the gross `LineTotalRon`, then derive net **unit price** as `netLineTotal / quantity` — never via an independent extraction on `UnitPriceRon` (that drifts from the line-total extraction whenever `Quantity > 1`, confirmed against the existing test fixture itself: 7×3=21 independently extracts to 5.88×3=17.64 vs 21→17.65). Per-line independent VAT extraction also drifts from the aggregate header extraction by rounding (confirmed: three 10.01-RON lines sum to 25.23 net independently vs 25.24 from the aggregate) — apply the residual to one line's net total so Σ(lines) reconciles exactly with `invoice.NetTotalRon`. Give the synthetic shipping line the same treatment (trivial, quantity always 1). **Test shape:** a new multi-item fixture (the current single-item-plus-shipping fixture coincidentally hides the drift) asserting raw `LineExtensionAmount`/`PriceAmount` values are net and Σ(lines) == header net total. Trigger-list-shaped (money-field semantics change) — **approach-check: revised** (computation must move up to `BuildInvoiceLines`; unit price derived from the reconciled line total, not independently; new test fixture required).
- **History:**
  - v1: found by a supplemental correctness pass, dispatched this pass to close PPW-497's gap — checked directly against the code, not by a manifest lens
  - v1: approach-check run — revised (derivation site and rounding-consistency corrected)

### PPW-479 — Admin invoice list `Page` param is unbounded — int32 overflow can reach `Skip()`

- **What:** `Page` has no upper bound (`Size` is capped [1,100]). `(Page-1)*Size` in unchecked int32 overflows at `page≈2^31`, wrapping negative; `Skip(negative)` reaches the DB provider unguarded, surfacing as an unhandled 500.
- **Evidence:** `Controllers/AdminInvoicesController.cs:57`; `Validators/Invoices/AdminInvoiceListQueryValidator.cs:11-17`.
- **Suggested fix:** Add an upper bound on `Page`, or compute the offset in `long`/checked arithmetic and clamp before `Skip`. **Test shape:** `GET ?page=2147483647&size=100` → expect 422, not 500. Not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (correctness)

### PPW-480 — Admin "retry" resubmits byte-identical XML — can never fix the failure it exists for

- **What:** `RetryAsync` clears `AnafUploadId`/`LastError` but leaves `XmlPayload` untouched; the worker's Step 1 skips rebuilding XML whenever it's already set, so a rejected invoice resubmits identically and fails identically forever.
- **Evidence:** `Services/Invoicing/InvoiceLifecycle.cs:98-111`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:142-151`.
- **Suggested fix:** Clear `Invoice.XmlPayload` in `RetryAsync`'s existing atomic update so the next tick rebuilds it. **Do not** clear `PdfStoragePath` — it plays no role in the ANAF path, the key is stable and gets overwritten in place anyway (no orphan risk), and clearing it risks a duplicate customer "invoice ready" notification once the email flag is enabled (Step 2 unconditionally re-notifies on every render). Log the pre-retry `XmlPayload`/`LastError` before clearing so `GET /xml`'s stated "inspect what ANAF rejected" purpose still works. Note in the docs/UI that this only helps when Seller config or code was fixed since the original build — there's no admin tool to edit Order data, so a rejection caused by bad order data will resubmit identically either way. Trigger-list-shaped (changes retry semantics) — **approach-check: revised** (drop the `PdfStoragePath` clear; add pre-clear logging).
- **History:**
  - v1: found — raised independently by 1 lens (requirements)
  - v1: approach-check run — revised (PdfStoragePath clear dropped; logging added)

### PPW-481 — `AnafSettings` docstring's "byte-identical to baseline" claim is false when disabled

- **What:** With `Anaf:Enabled=false` (default), `InvoiceCreationService`/`InvoiceLifecycle`/`InvoicesController` are wired unconditionally — a paid order still gets an Invoice DB row and the new customer endpoint returns a permanent 404+Retry-After. Neither effect is byte-identical to the pre-integration baseline, contrary to the docstring.
- **Evidence:** `Configuration/AnafSettings.cs:4-9`; `Program.cs:281-323`.
- **Suggested fix:** Update the docstring to state plainly that the Invoice row and the customer endpoint are also live while `Enabled=false` — only the ANAF wire calls are skipped. Doc-only; not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (requirements)

### PPW-482 — `AdminInvoicesController`'s audit-logging doc-comment is false; the one logged action omits the admin id

- **What:** The class doc-comment claims all operations are audit-logged with the admin's id. `ListAsync`/`GetXmlAsync` log nothing; `RetryAsync` logs invoice_id/from-status only, no admin identity.
- **Evidence:** `Controllers/AdminInvoicesController.cs:12-16,39-80,91-133,140-155`.
- **Suggested fix:** Add `User.GetUserIdOrNull()`-tagged Information logs on all three actions (pattern already used in `InvoicesController`). Not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 2 lenses (requirements + observability)

### PPW-483 — Redundant Order re-query on every paid webhook in `InvoiceCreationService`

- **What:** `WebhooksController` already loads and mutates `order` on the shared scoped DbContext; `CreateForOrderAsync` re-queries it by id — an avoidable extra SQL round trip on every successful payment.
- **Evidence:** `Services/Invoicing/InvoiceCreationService.cs:49`; `Controllers/WebhooksController.cs:169,205,261,285`.
- **Suggested fix:** Overload `CreateForOrderAsync` to accept the already-loaded `Order`. Not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (quality)

### PPW-484 — `InvoiceUploadJob` worker reloads the full Order graph even when only the ANAF step remains

- **What:** `LoadPairAsync` always does `Order.Include(Items).Include(User)` even when only Step 3 (ANAF upload, which never touches `order`) remains — wasted joins on every tick during an ANAF outage.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:132,246-257`.
- **Suggested fix:** Only load Order with includes when Steps 1–2 still need it. Not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (quality)

### PPW-485 — Checkout field-length caps are wider than the legal XML limits, with no truncation

- **What:** `RecipientName`/`Street`/`AddressLine` allow up to 255-400 chars with nothing truncating or validating against CIUS-RO field-length limits before `InvoiceXmlBuilder` embeds them — an oversized-but-valid address becomes an unfixable ANAF rejection (no edit path, cached XML resubmits identically forever, same mechanism as PPW-480).
- **Evidence:** `Validators/Payments/CreateOrderRequestValidator.cs:49-61`; `Services/Invoicing/InvoiceXmlBuilder.cs:121-124`; `Services/Invoicing/Anaf/AnafSpvClient.cs:77-84`.
- **Suggested fix:** Cap/truncate to CIUS-RO limits before XML build, or validate at checkout so bad data never reaches an unfixable Paid+Invoice state. Not trigger-list-shaped (validation only).
- **History:**
  - v1: found — raised independently by 1 lens (input-validation)

### PPW-486 — Per-row catch collapses auth failure, network failure, and code bugs into one generic log event

- **What:** `AnafAuthException` (e.g. an expiring client cert — urgent) propagates past the local catch into the same generic `anaf.upload-job.row-failed` as a self-healing transient error or a code bug, with no field distinguishing them.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:91,182-190`; `Services/Invoicing/Anaf/AnafSpvClient.cs:60,116`.
- **Suggested fix:** Add a dedicated catch for `AnafAuthException` at a distinct event name/severity (precedent: `AwbDispatcher.HandleOutcomeAsync`'s non-transient-vs-retry-scheduled split). Drop "escalates on repeat" — no per-replica-safe counter exists, and ADR-024 already rejected a persisted attempt counter for this exact subsystem; treat as urgent on first sight instead. A log-only change isn't sufficient: standalone `LogError` never reaches Sentry by this project's own design (`writeToProviders=false`) and no metric increments on this path — add an explicit `IHub.CaptureException` call in the new catch so the signal actually reaches an alerting channel. Trigger-list-shaped (new catch/mapping layer) — **approach-check: revised** (drop repeat-escalation; add explicit Sentry capture).
- **History:**
  - v1: found — raised independently by 1 lens (observability)
  - v1: approach-check run — revised (repeat-escalation dropped; Sentry capture added)

### PPW-487 — Unrecognized ANAF status string is silently treated as "still processing", raw value never logged

- **What:** `MapStatus`'s `Unknown` default discards the raw ANAF `stare` string; `PollSubmittedAsync` groups `Unknown` with `InProgress` — no log, no metric, no operator signal if ANAF returns a status value the client doesn't recognize.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:232-235`; `Services/Invoicing/Anaf/AnafSpvClient.cs:127-155`.
- **Suggested fix:** Log the raw `stare` value at Warning when `MapStatus` can't classify it; log `Unknown` distinctly from `InProgress` in the job. Not trigger-list-shaped (log line + switch-branch differentiation, no new catch/retry/job).
- **History:**
  - v1: found — raised independently by 1 lens (observability)

### PPW-488 — No domain-tagged log for "customer charged, order not committed" in `WebhooksController`

- **What:** Two concurrent webhook deliveries can both pass the pre-commit check; the loser's unique-violation `DbUpdateException` hits the generic exception-handler log, unlike the deliberate "manual reconciliation required" logging this same file already uses for adjacent scenarios.
- **Evidence:** `Controllers/WebhooksController.cs:196-217,234-236,300-302`; `Services/Invoicing/SqliteInvoiceNumberingService.cs:41-48`.
- **Suggested fix:** Wrap the span in a catch scoped to `DbUpdateException` specifically — not bare `catch(Exception)`, which would also mislabel a client-disconnect `OperationCanceledException` as a payment incident. Log the order's status captured **before** calling `OrderStatusMachine.Transition` (`order.Status` is mutated in-memory before `SaveChangesAsync`; logging it after a rollback would show "Paid" for an order that's actually still `AwaitingPayment`). Trigger-list-shaped (new catch/mapping layer) — **approach-check: revised** (narrow the catch type; fix the stale-status logging bug).
- **History:**
  - v1: found — raised independently by 1 lens (observability)
  - v1: approach-check run — revised (catch type narrowed; pre-transition status snapshot added)

### PPW-489 — Polly retries the non-idempotent ANAF upload POST on ambiguous-outcome errors

- **What:** `AnafResilienceHandler` retries the upload POST on `HttpRequestException`/5xx/408/429. If ANAF actually persisted the upload but the response was lost, the retry resends identical XML — a real duplicate submission, relying on an unverified "ANAF dedupes by invoice number" assumption.
- **Evidence:** `Services/Invoicing/Anaf/AnafResilienceHandler.cs:21-41`; `Services/Invoicing/Anaf/AnafSpvClient.cs:41-50,96-101`.
- **Suggested fix:** The originally-drafted "check `GetStatusAsync` before re-uploading" is a no-op for the case it's meant to guard — `GetStatusAsync` needs `id_incarcare`, which is only known after a successful upload response is parsed; on the ambiguous-failure path it's never set. A real fix needs either a genuinely new ANAF lookup-by-CIF/date-range capability (itself unverified against real ANAF, and `AnafResilienceHandler`'s retry is wired to the whole typed HttpClient today, so excluding just the upload endpoint needs new per-endpoint routing) or accepting the current documented tolerance (ANAF dedupes by invoice number) and doing nothing further. Flag this as an owner decision — this is a design trade-off (added latency: today's transient failure self-heals in ~7s vs. a 30-minute next-tick wait after removing retry), not something to just implement. Trigger-list-shaped (changes retry semantics) — **approach-check: revised** (drafted mitigation doesn't work; frame as owner decision, not implementation).
- **History:**
  - v1: found — raised independently by 1 lens (race)
  - v1: approach-check run — revised (proposed mitigation is a no-op for the described case; routed to owner decision)

### PPW-490 — SQLite invoice numbering's MAX+1 has no transaction/lock despite the comment's safety claim

- **What:** `NextNumberAsync`'s bare `SELECT MAX(Number)+1` has no explicit lock; two concurrent dev/SQLite webhook requests can read the same MAX and compute the same number. The losing `SaveChangesAsync` throws unhandled, rolling back the Order.Status=Paid mutation with it — a captured payment silently reverts to AwaitingPayment.
- **Evidence:** `Services/Invoicing/SqliteInvoiceNumberingService.cs:29-48`; `Controllers/WebhooksController.cs:196-217,279-297`; `Data/PhotoPrintDbContext.cs:435-438`.
- **Suggested fix:** "Wrap in an explicit transaction" doesn't close the race — SQLite's deferred transaction takes no read lock, so both requests still read the same stale MAX before either writes. The catch also doesn't belong in `InvoiceCreationService` (same misplaced-catch issue as PPW-471 — that method never calls `SaveChangesAsync`). Mirror `OrderService.cs`'s existing bounded-retry pattern instead: catch the provider-specific violation on `ix_invoices_invoice_number` at the `WebhooksController` `SaveChangesAsync` call sites, reassign the invoice number on the still-tracked entity, and retry (bounded, e.g. `MaxOrderNumberRetries`-style). Postgres is unaffected (`nextval()` is atomic) — scope to SQLite/dev only. Same catch site as PPW-471 — fix together. Trigger-list-shaped (concurrency model + retry) — **approach-check: revised** (transaction-wrap dropped as ineffective; catch site and pattern corrected to match `OrderService`'s precedent).
- **History:**
  - v1: found — raised independently by 1 lens (race)
  - v1: approach-check run — revised (catch site and mechanism corrected; unified with PPW-471's fix)

### PPW-491 — `InvoiceUploadJob` has zero tests despite being the most stateful new logic

- **What:** No test file exists for `InvoiceUploadJob` anywhere in the repo. The 3-step pipeline plus backoff-exhaustion branching is entirely unverified.
- **Evidence:** confirmed by search — no `InvoiceUploadJobTests.cs`.
- **Suggested fix:** Add unit tests for `ProcessOneAsync` covering partial completion, the backoff-budget boundary, and 200-with-Errors handling. Test-only; not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (completeness-critic), not independently confirmed — a real coverage gap, not a wrong-output claim, so there was no trace to build against it

### PPW-492 — Webhook tests stub invoice creation to always return null; nothing asserts it runs or that failure is handled

- **What:** `_invoiceCreator.CreateForOrderAsync` is mocked to unconditionally return null; nothing asserts it's actually invoked on the Paid path, nor what happens if it throws mid-`SaveChangesAsync`.
- **Evidence:** `Tests/Unit/Controllers/WebhooksControllerMetricsTests.cs:58`; `Controllers/WebhooksController.cs:196-217,279-297`.
- **Suggested fix:** Add a test asserting `CreateForOrderAsync` is called on the Paid transition, and one where it throws, asserting the order stays `AwaitingPayment` (not silently marked Paid). Test-only; not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (completeness-critic)

### PPW-493 — `PostgresInvoiceNumberingService` — the only prod numbering path — has no test coverage

- **What:** The only production numbering path (`PaymentFactory.cs` substitutes a fake for integration tests because EF InMemory can't execute its raw-SQL `nextval()`) has zero direct tests — a regression here ships untested, reproducing the dual-database gap CLAUDE.md already flags.
- **Evidence:** `Services/Invoicing/PostgresInvoiceNumberingService.cs:1-65`; `Tests/Integration/PaymentFactory.cs:170-177`.
- **Suggested fix:** Add a real/dockerized-Postgres test for `NextNumberAsync` covering year rollover and concurrent callers. Test-only; not trigger-list-shaped.
- **History:**
  - v1: found — raised independently by 1 lens (completeness-critic)
  - v1: broadened by a supplemental db-parity check (the "tests-coverage"/"db-parity" lenses named in the manifest were dropped by a key typo and never ran this pass — see PPW-497): the same untested-Postgres-only-DDL gap also covers the raw-SQL `CREATE SEQUENCE`/composite unique index at `Migrations/20260603101910_AddVatAndInvoices.cs:103-114` — no test anywhere executes this migration's Postgres path

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
