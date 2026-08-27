---
type: review-ledger
target: 038-039-invoicing
updated: 2026-08-24
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
| PPW-509 | 🟡 | v3 | `CustomerEmailAttachmentSettings` docstring still says the XML, ANAF and PDF pipeline runs unconditionally | `Configuration/InvoicingSettings.cs:18` | verified | `ec29613` |
| PPW-510 | 🟡 | v3 | ADR-022 left stale while the deployment guide and the decision index send an operator to it as current authority | `docs/DEPLOYMENT.md:1309` | verified | `ec29613` |
| PPW-511 | 🟡 | v5 | EuPlatesc coverage waived twice on a removal that no work item tracks, against a standard that forbids the divergence | `memory-bank/standards/definition-of-done.md:52` | fixed | `06fd2b1` |
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
| PPW-524 | 🟠 | v6 | The whole invoicing feature has no SPA consumer and no lens covered the frontend | `Controllers/InvoicesController.cs:1` | verified | `5324a1c` |
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
| PPW-553 | 🟠 | v7 | The 2 h ANAF auth-outage window has no floor tied to PollIntervalMinutes, so a validator-legal interval above it defeats the dedup | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:17` | verified | `2daf61e` |
| PPW-554 | 🟡 | v7 | The bucket-versus-key miss-cause preference has no regression test | `Controllers/InvoicesController.cs:83-87` | backlog | `0ec6497` |
| PPW-557 | 🔴 | v9 | New mandatory-address guard makes every Easybox order permanently un-invoiceable | `Services/Invoicing/InvoiceXmlBuilder.cs:131` | verified | `f769e22` |
| PPW-558 | 🔴 | v9 | Anonymous Stripe webhook buffers an unbounded request body into a string before any signature check | `Controllers/WebhooksController.cs:69` | verified | `f769e22` |
| PPW-559 | 🔴 | v9 | Upload-timeout branch holds a claim that always expires before the next tick, so the same invoice is re-uploaded to ANAF | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:345` | verified | `f769e22` |
| PPW-560 | 🟠 | v9 | Squashed InitialPostgres baseline has no upgrade path: a database that ran the deleted chain cannot boot | `Migrations/20260820133204_InitialPostgres.cs:10` | verified | `56eb9be` |
| PPW-561 | 🟡 | v9 | PostgresTestDatabase catch-all turns any CREATE DATABASE failure into "no PostgreSQL server", with no retry | `Tests/Helpers/PostgresTestDatabase.cs:33` | backlog | `c8d6bb4` |
| PPW-562 | 🟠 | v9 | PostgresTestDatabase is per-test, not per-class: about 100 real databases plus full migration chains per run | `Tests/Helpers/PostgresTestDatabase.cs:25` | fixed | `4dd6763` |
| PPW-563 | 🟡 | v9 | Removing the skip guard hard-fails every Postgres-backed test, and the default credentials do not match docker-compose | `Tests/Helpers/PostgresTestDatabase.cs:28` | backlog | `c8d6bb4` |
| PPW-564 | 🟠 | v9 | Admin Paid path swallows the invoice-already-created race but still fires Paid side effects and overwrites the webhook's PaidAt | `Services/AdminOrderService.cs:425` | verified | `f769e22` |
| PPW-565 | 🟠 | v9 | Changed files no lens owns: EF model snapshot and Designers, Sameday registry, both .csproj, ci.yml | `Migrations/PhotoPrintDbContextModelSnapshot.cs:1` | verified | `f769e22` |
| PPW-566 | 🟠 | v9 | AnafSpvClient timeout-versus-shutdown classifier is untested, and Polly retries inside the 30 s budget misclassify definite failures | `Services/Invoicing/Anaf/AnafSpvClient.cs:56` | verified | `f769e22` |
| PPW-567 | 🟡 | v9 | Exhausted invoice-number collision retry escapes AdminOrderService with the order still tracked Paid | `Services/AdminOrderService.cs:417` | verified | `f769e22` |
| PPW-568 | 🟡 | v9 | Admin manual-Paid retry loop: only the happy retry is tested, the exhausted and already-invoiced branches are not | `Services/AdminOrderService.cs:414` | verified | `f769e22` |
| PPW-569 | 🟡 | v9 | CREATE SEQUENCE IF NOT EXISTS is not race-safe and only the ft_2026 sequence is seeded | `Services/Invoicing/PostgresInvoiceNumberingService.cs:46` | verified | `88f5ee6` |
| PPW-570 | 🟡 | v9 | PostgresTestDatabase contexts omit the split-query behaviour production configures | `Tests/Helpers/PostgresTestDatabase.cs:53` | backlog | `c8d6bb4` |
| PPW-571 | 🟡 | v9 | PostgresTestDatabase.Dispose clears every Npgsql pool in the process while parallel test classes hold their own databases | `Tests/Helpers/PostgresTestDatabase.cs:99` | backlog | `c8d6bb4` |
| PPW-572 | 🟡 | v9 | MemoryCacheOnceRegistry.MarkOnce is a non-atomic read-then-write despite promising first-caller-only | `Services/MemoryCacheOnceRegistry.cs:23` | backlog | `c8d6bb4` |
| PPW-573 | 🟡 | v9 | data-stack standard and the deployment guide left stale by the migration squash and the provider removal | `memory-bank/standards/data-stack.md:29` | backlog | `c8d6bb4` |
| PPW-574 | ⚪ | v9 | InvoiceAddressFormatter.Truncate with maxLength 0 indexes before the string start and throws IndexOutOfRangeException | `Services/Invoicing/InvoiceAddressFormatter.cs:20` | backlog | `c8d6bb4` |
| PPW-575 | ⚪ | v9 | PostalZone is truncated with the borrowed CityNameMaxLength constant | `Services/Invoicing/InvoiceXmlBuilder.cs:122` | backlog | `c8d6bb4` |
| PPW-576 | ⚪ | v9 | Blob-missing log omits the stamped storage tier, so a cloud-off misconfiguration reads as a lost file | `Controllers/InvoicesController.cs:122` | backlog | `c8d6bb4` |
| PPW-577 | ⚪ | v9 | Dead DatabaseProvider environment entry left in the Dockerfile, .env.example and both compose files | `Dockerfile:42` | backlog | `c8d6bb4` |
| PPW-578 | 🟠 | v10 | Order-number sequence is created check-then-act, so two first orders of a year fail on a catalogue unique index | `Services/OrderNumberService.cs:37` | verified | `88f5ee6` |
| PPW-579 | 🔴 | v12 | Static ro-RO culture in InvoicePdfDocument throws on the Alpine production image, wedging every invoice PDF | `Services/Invoicing/InvoicePdfDocument.cs:19` | verified | `06fd2b1` |
| PPW-580 | 🔴 | v12 | One MaxBatchSize batch mixes cooldown-exempt Submitted polls with Pending uploads, so stuck polls starve new invoices out of filing | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:102` | wont-fix | `090873d` |
| PPW-581 | 🔴 | v12 | Expired or revoked ANAF credentials never reach the auth-outage alert; they fan out as N generic row-failed errors per tick | `Services/Invoicing/Anaf/AnafTokenProvider.cs:109` | wont-fix | `090873d` |
| PPW-582 | 🔴 | v12 | Confirmation page races the payment webhook and redirects the paying customer home | `src/app/features/orders/pages/confirmation-page.ts:208` | fixed | `901f8a2` |
| PPW-583 | 🔴 | v12 | Switching payment tabs destroys the Stripe card element but leaves the pay button enabled | `src/app/features/checkout/pages/payment-step.ts:196` | fixed | `06fd2b1` |
| PPW-584 | 🔴 | v12 | SPA never sends an Idempotency-Key and PaymentStep mints a fresh order on every mount | `src/app/core/services/payment.service.ts:18` | fixed | `901f8a2` |
| PPW-585 | 🟠 | v12 | Recapitulare hides the new fiscal address for locker orders, and an unchanged spec pins that behaviour | `src/app/features/checkout/pages/review-step.spec.ts:126` | verified | `c03f99a` |
| PPW-586 | 🟠 | v12 | Neither invoice controller has an HTTP-pipeline test, so endpoint authorization and DualAuth guest ownership are unverified | `Tests/Unit/Controllers/InvoicesControllerTests.cs:52` | verified | `8950624` |
| PPW-587 | 🟠 | v12 | A permanent HTTP 4xx content rejection is classified as unreachable/transient, so the row retries forever and is never parked | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:355` | verified | `32d4eee` |
| PPW-588 | 🟠 | v12 | Unknown-outcome budget covers only client timeouts, so AnafUnreachableException gets unlimited blind re-POSTs | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:355` | verified | `32d4eee` |
| PPW-589 | 🟡 | v12 | nextval commits outside the insert transaction, so a lost duplicate-delivery race permanently burns a fiscal invoice number | `Services/Invoicing/PostgresInvoiceNumberingService.cs:40` | backlog | `090873d` |
| PPW-590 | 🟡 | v12 | PollSubmittedAsync takes no claim, so every replica polls every Submitted row on every tick | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:421` | backlog | `090873d` |
| PPW-591 | 🟠 | v12 | No setval reconciliation: an invoice sequence that lags the Invoices table wedges every paid order | `Services/Invoicing/PostgresInvoiceNumberingService.cs:40` | verified | `72202c0` |
| PPW-592 | 🟠 | v12 | ANAF-supplied index_incarcare is accepted unvalidated into a varchar(100) column, turning a filed invoice into a blind re-upload loop | `Services/Invoicing/Anaf/AnafSpvClient.cs:91` | verified | `add7611` |
| PPW-593 | 🟡 | v12 | Admin retry's Rejected/Failed status whitelist has no test; only the 409-free happy path is covered | `Tests/Unit/Controllers/AdminInvoicesControllerTests.cs:75` | backlog | `090873d` |
| PPW-594 | 🟡 | v12 | The new Invoice.StorageLocation stamp is never asserted after a PDF save | `Tests/Unit/Services/Invoicing/Anaf/InvoiceUploadJobTests.cs:304` | backlog | `090873d` |
| PPW-595 | 🟡 | v12 | QuestPDF licence is set by the test class itself, so the production licence wiring is unverified | `Tests/Unit/Services/Invoicing/InvoicePdfRendererTests.cs:23` | backlog | `090873d` |
| PPW-596 | 🟠 | v12 | No admin access to an invoice PDF, so FR-5's role override and the inspection-week runbook are undelivered | `Controllers/InvoicesController.cs:58` | verified | `e3f4bb8` |
| PPW-597 | 🟠 | v12 | Invoice-by-email (FR-5, story 003) is not implemented while ddd-02 describes it as shipped | `Services/Invoicing/InvoicePdfReadyNotifier.cs:31` | verified | `5324a1c`, `beb7732` |
| PPW-598 | 🟠 | v12 | Admin retry never re-renders the PDF, contradicting the documented fix-forward-and-re-render rollback | `Services/Invoicing/InvoiceLifecycle.cs:165` | verified | `add7611` |
| PPW-599 | 🟠 | v12 | Documented batch-retry SQL in DEPLOYMENT.md reposts the identical rejected XML and re-parks on the first timeout | `docs/DEPLOYMENT.md:1531` | verified | `add7611` |
| PPW-600 | 🔴 | v12 | FR-4's exponential backoff (1h/4h/16h/64h) never runs — Rejected is terminal until an admin acts | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:99` | verified | `6977d5b` |
| PPW-601 | 🟡 | v12 | system-architecture.md was never updated for the invoicing feature, breaking the descriptive-standards rule | `memory-bank/standards/system-architecture.md:83` | backlog | `090873d` |
| PPW-602 | 🟠 | v12 | Invoice 404 advertises Retry-After 30 seconds although the PDF can be a 30-minute poll interval away | `Controllers/InvoicesController.cs:68` | verified | `add7611` |
| PPW-603 | 🟡 | v12 | The poll leg has no catch, so an ANAF outage logs Error row-failed there while the upload leg logs Warning unreachable | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:414` | backlog | `090873d` |
| PPW-604 | 🔴 | v12 | No metric marks a stuck or retrying invoice, so the sole ANAF panel goes blind during an outage | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:344` | verified | `32d4eee` |
| PPW-605 | 🟠 | v12 | Manual admin mark-Paid issues a fiscal invoice with no log naming the admin | `Services/AdminOrderService.cs:154` | verified | `add7611` |
| PPW-606 | ⚪ | v12 | Only the pre-commit attempted invoice number is logged; the committed number is never logged | `Services/Invoicing/InvoiceCreationService.cs:98` | backlog | `090873d` |
| PPW-607 | 🟠 | v12 | Admin- and config-sourced fields (invoice line name) reach the UBL XML with no control-char guard and no truncation | `Services/Invoicing/InvoiceXmlBuilder.cs:204` | verified | `166230a` |
| PPW-608 | 🟠 | v12 | Admin cannot mark an order Paid by hand — NEXT_STATUSES has no AwaitingPayment entry | `src/app/features/admin/pages/order-detail/admin-order-detail-page.ts:19` | verified | `add7611` |
| PPW-609 | 🟠 | v12 | One generic error string blames the cart for every payment failure, and EuPlatesc failures are silent | `src/app/features/checkout/pages/payment-step.ts:188` | fixed | `06fd2b1` |
| PPW-610 | 🟡 | v12 | The invoice-number-exhausted 409 message is replaced by a generic admin failure toast | `src/app/features/admin/pages/order-detail/admin-order-detail-page.ts:133` | backlog | `090873d` |
| PPW-611 | 🟠 | v12 | SPA still sends the deprecated shippingCostRon, so every checkout logs a tampering warning | `src/app/core/models/payment.model.ts:8` | fixed | `2acda1f` |
| PPW-612 | 🟠 | v12 | Checkout address form mirrors only the phone rule, so the new fiscal-address length/charset caps surface as a 400 at the payment step | `src/app/features/checkout/pages/delivery-step.ts:336` | verified | `5cd48a5` |
| PPW-613 | 🟠 | v12 | VAT is never shown in the SPA although the API now returns NetTotalRon/VatRon/VatRate | `src/app/core/models/order.model.ts:32` | verified | `6812453` |
| PPW-614 | 🟠 | v12 | Hardcoded 20/25 RON shipping defaults with no error handling can differ from the invoiced total | `src/app/features/checkout/pages/delivery-step.ts:327` | verified | `6812453` |
| PPW-615 | 🟠 | v12 | A non-succeeded, non-error Stripe result leaves the user stranded with no feedback | `src/app/features/checkout/pages/payment-step.ts:221` | fixed | `901f8a2` |
| PPW-616 | 🟠 | v12 | Saved addresses allow City 100 while checkout caps it at 50, and the new prefill copies them in | `Validators/Account/SavedAddressValidator.cs:26` | verified | `5cd48a5` |
| PPW-617 | 🟡 | v12 | The paid-transition invoice retry/rollback state machine is implemented twice with divergent guards and no shared test | `Services/AdminOrderService.cs:437` | backlog | `090873d` |
| PPW-618 | 🟡 | v12 | Cloud tier and the new cross-tier fallback read are proven only against fakes | `Controllers/InvoicesController.cs:99` | backlog | `090873d` |
| PPW-619 | 🟡 | v12 | OrderNumberService's manually opened DbConnection is never closed, pinning it for the rest of the scope | `Services/OrderNumberService.cs:34` | backlog | `090873d` |
| PPW-620 | 🟡 | v12 | Admin invoice paging orders by a non-unique CreatedAt with no unique tiebreaker | `Controllers/AdminInvoicesController.cs:57` | backlog | `090873d` |
| PPW-621 | 🟡 | v12 | Per-customer invoice PDF is cached for a year with no revalidation | `Controllers/InvoicesController.cs:132` | backlog | `090873d` |
| PPW-622 | 🟡 | v12 | Buyer fiscal address survives logout in sessionStorage and prefills the next account | `src/app/core/services/auth.service.ts:174` | backlog | `090873d` |
| PPW-623 | 🟡 | v12 | EuPlatesc IPN fingerprint is verified with a non-fixed-time string compare | `Services/EuPlatescService.cs:92` | backlog | `090873d` |
| PPW-624 | 🟡 | v12 | ANAF response body is read into memory with no size cap and then persisted unbounded | `Services/Invoicing/Anaf/AnafSpvClient.cs:73` | backlog | `090873d` |
| PPW-625 | 🟡 | v12 | The PDF-ready notification fires inside the render-once branch, so a throw there loses it permanently | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:293` | backlog | `090873d` |
| PPW-626 | 🟡 | v12 | Cloud blob is orphaned when the storage tier flips between a failed path-stamp and the retry | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:287` | backlog | `090873d` |
| PPW-627 | 🟡 | v12 | Vat:Rate accepts unlimited decimal places while Orders.VatRate is numeric(5,4) and rounds silently | `Validators/VatSettingsValidator.cs:23` | backlog | `090873d` |
| PPW-628 | 🟡 | v12 | Migration Down() drops only invoice_seq_ft_2026, so lazily-created year sequences survive a rebuild and skip numbers | `Migrations/20260820133204_InitialPostgres.cs:752` | backlog | `090873d` |
| PPW-629 | 🟡 | v12 | Admin invoice ListAsync output is never asserted — paging, ordering, status filter and the Orders join are unverified | `Tests/Unit/Controllers/AdminInvoicesControllerTests.cs:39` | backlog | `090873d` |
| PPW-630 | 🟡 | v12 | Quarterly gap-audit query uses session-timezone EXTRACT while the unique index uses AT TIME ZONE 'UTC' | `docs/DEPLOYMENT.md:1498` | backlog | `090873d` |
| PPW-631 | 🟡 | v12 | Bolt-038 test report cites a migration that no longer exists and misstates numbering test coverage | `memory-bank/bolts/038-vat-calculation/ddd-03-test-report.md:54` | backlog | `090873d` |
| PPW-632 | 🟡 | v12 | Customer-facing blob-missing error is English and carries no correlationId, against api-conventions | `Controllers/InvoicesController.cs:126` | backlog | `090873d` |
| PPW-633 | 🟡 | v12 | Full fiscal address is now mandatory for Easybox orders — a customer-visible scope change with no story or AC | `Validators/Payments/CreateOrderRequestValidator.cs:23` | backlog | `090873d` |
| PPW-634 | 🟡 | v12 | Lazy creation of a fiscal-year invoice sequence is completely silent | `Data/PostgresSequences.cs:23` | backlog | `090873d` |
| PPW-635 | 🟡 | v12 | Polly retry pipeline has no OnRetry logging, so a degrading ANAF is invisible | `Services/Invoicing/Anaf/AnafResilienceHandler.cs:33` | backlog | `090873d` |
| PPW-636 | 🟡 | v12 | A garbage HTTP 200 body is reported as the same unreachable incident as a network outage | `Services/Invoicing/Anaf/AnafSpvClient.cs:179` | backlog | `090873d` |
| PPW-637 | 🟡 | v12 | Unhandled-Stripe-event line is LogDebug under an Information floor, so it never emits | `Controllers/WebhooksController.cs:149` | backlog | `090873d` |
| PPW-638 | 🟡 | v12 | Fulfilment ZIP entry name interpolates an unsanitized product name | `Services/AdminOrderService.cs:249` | backlog | `090873d` |
| PPW-639 | 🟡 | v12 | Upload quota is enforced for guests only; registered users are uncapped | `Services/UploadService.cs:67` | backlog | `090873d` |
| PPW-640 | 🟡 | v12 | /checkout/recapitulare has no delivery-complete guard and mislabels a null method as courier | `src/app/features/checkout/pages/review-step.ts:41` | backlog | `090873d` |
| PPW-641 | 🟡 | v12 | No admin UI for the invoice list, ANAF retry, or UBL XML endpoints | `src/app/features/admin/admin.routes.ts:8` | backlog | `090873d` |
| PPW-642 | 🟡 | v12 | logout() resets returnUrl, so a mid-checkout token expiry dumps the user at the upload page | `src/app/core/services/auth.service.ts:179` | backlog | `090873d` |
| PPW-643 | 🟡 | v12 | Two unbounded subscriptions in ReviewStep.ngOnInit | `src/app/features/checkout/pages/review-step.ts:196` | backlog | `090873d` |
| PPW-644 | 🟡 | v12 | Order ZIP blob URL is revoked synchronously after click, which can abort the download | `src/app/core/services/admin.service.ts:92` | backlog | `090873d` |
| PPW-645 | 🟡 | v12 | A DDL DO-block runs before every number allocation instead of once per series/year | `Services/Invoicing/PostgresInvoiceNumberingService.cs:38` | backlog | `090873d` |
| PPW-646 | 🟡 | v12 | Polling loads the whole invoice row, including XmlPayload, to read two fields | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:421` | backlog | `090873d` |
| PPW-647 | ⚪ | v12 | AddInvoiceUnknownUploadOutcomes leaves a permanent DEFAULT 0 that the model does not declare | `Migrations/20260821110018_AddInvoiceUnknownUploadOutcomes.cs:18` | backlog | `090873d` |
| PPW-648 | ⚪ | v12 | The VAT rounding-mode test mostly asserts decimal.Round's own behaviour and never pins the net-side mode | `Tests/Unit/Services/VatCalculatorTests.cs:57` | backlog | `090873d` |
| PPW-649 | ⚪ | v12 | metrics.md still marks invoice_anaf_status_total as future and never incremented | `memory-bank/operations/metrics.md:69` | verified | `6977d5b` |
| PPW-650 | ⚪ | v12 | Story 001's AC to document shipping as VAT-inclusive in decision-index.md is not done | `Services/VatCalculator.cs:14` | backlog | `090873d` |
| PPW-651 | ⚪ | v12 | Both admin retry-refusal branches log nothing despite the class's audit-logged claim | `Controllers/AdminInvoicesController.cs:123` | backlog | `090873d` |
| PPW-652 | ⚪ | v12 | Paid webhook spends two extra round-trips re-loading order relations it could have Included | `Controllers/WebhooksController.cs:402` | backlog | `090873d` |
| PPW-653 | ⚪ | v12 | Duplicated ANAF status triage with a provably dead branch, repeated in both client methods | `Services/Invoicing/Anaf/AnafSpvClient.cs:67` | backlog | `090873d` |
| PPW-654 | ⚪ | v12 | Migration hardcodes invoice_seq_ft_2026, duplicating a name the service derives from config | `Migrations/20260820133204_InitialPostgres.cs:746` | backlog | `090873d` |
| PPW-655 | ⚪ | v12 | Runtime Math.Max clamps duplicate ANAF ranges the settings validator already enforces, with a divergent floor | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:41` | backlog | `090873d` |
| PPW-656 | ⚪ | v12 | Third copy of the mandatory-address field list in checkout-state.service.ts | `src/app/core/services/checkout-state.service.ts:51` | backlog | `090873d` |
| PPW-657 | ⚪ | v12 | Lens manifest omits three changed files and names one that did not change | `Migrations/20260821110018_AddInvoiceUnknownUploadOutcomes.cs:1` | backlog | `090873d` |
| PPW-659 | 🔴 | v15 | Not-yet-due Rejected invoices fill the upload batch and starve Pending uploads | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:103` | verified | `9527eba` |
| PPW-660 | 🔴 | v15 | A succeeded webhook on an order already moved to PaymentFailed leaves the customer charged and the order unfulfillable | `Controllers/WebhooksController.cs:242` | verified | `c60260c` |
| PPW-661 | 🔴 | v15 | Checkout idempotency key is never retired after a paid order, so the next checkout is redirected to the old order and the new basket deleted | `src/app/core/services/checkout-attempt.service.ts:49` | verified | `2302bec` |
| PPW-662 | 🔴 | v15 | Retry after a declined card reuses the same client secret whose order the failure webhook already moved to PaymentFailed | `src/app/features/checkout/pages/payment-step.ts:208` | verified | `2302bec` |
| PPW-663 | 🔴 | v15 | EuPlatesc columns removed by editing the already-applied baseline migration, so existing databases keep Orders.PaymentProcessor NOT NULL | `Migrations/20260820133204_InitialPostgres.cs:216` | verified | `de1b70d` |
| PPW-664 | 🟠 | v15 | Automatic rejection-resubmit nulls PdfStoragePath, revoking the customer's invoice | `Services/Invoicing/InvoiceLifecycle.cs:200` | verified | `9527eba` |
| PPW-665 | 🟠 | v15 | Any non-2xx 4xx from ANAF maps to content-rejected and permanently parks the invoice as Failed | `Services/Invoicing/Anaf/AnafSpvClient.cs:74` | verified | `5d84a5e` |
| PPW-666 | 🟠 | v15 | OrderService frees the idempotency key on a fresh PaymentFailed order while its PaymentIntent is still chargeable | `Services/OrderService.cs:129` | verified | `c60260c` |
| PPW-667 | 🟠 | v15 | ANAF 429/503 counts as an unknown upload outcome and parks the invoice after 3 ticks | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:441` | verified | `5d84a5e` |
| PPW-668 | 🟠 | v15 | Two concurrent same-key payment requests both call Stripe and one turns a 409 into a 500 | `Controllers/PaymentsController.cs:81` | verified | `18f0b1c` |
| PPW-669 | 🟠 | v15 | Post-commit webhook side effects are lost for good when one throws, because the retry hits the already-paid guard | `Controllers/WebhooksController.cs:201` | verified | `18f0b1c` |
| PPW-670 | 🟠 | v15 | One transient poll failure replaces the payment-submitted screen with "order not found" | `src/app/features/orders/pages/confirmation-page.ts:287` | verified | `2302bec` |
| PPW-671 | 🟠 | v15 | combinedStreetLength group error is never rendered, so Continue is disabled with no explanation | `src/app/features/checkout/pages/delivery-step.ts:384` | verified | `2302bec` |
| PPW-672 | 🟠 | v15 | Settle-poll setTimeout is never cancelled on destroy, so a late poll clears a newer basket | `src/app/features/orders/pages/confirmation-page.ts:282` | verified | `2302bec` |
| PPW-673 | 🟠 | v15 | Invoice download uses a detached anchor and revokes the object URL in the same tick as click() | `src/app/features/orders/pages/confirmation-page.ts:322` | verified | `2302bec` |
| PPW-674 | 🟠 | v15 | Admin cross-customer invoice read can be logged with an empty admin id | `Controllers/InvoicesController.cs:72` | verified | `5d84a5e` |
| PPW-675 | 🟠 | v15 | Pooled test database is reused without checking the migration chain actually applied | `Tests/Helpers/PostgresTestDatabase.cs:227` | verified | `18f0b1c` |
| PPW-676 | 🟡 | v15 | Content-rejected branch ignores a lost park CAS: claim stays held, no LastError, metric still counted | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:435` | verified | `5d84a5e` |
| PPW-677 | 🟡 | v15 | Webhook AlreadyInvoiced return leaves the uncommitted Paid transition on the scoped context | `Controllers/WebhooksController.cs:328` | backlog | `5fca3cf` |
| PPW-678 | 🟡 | v15 | Invoice number allocated outside the transaction that inserts the row, against the numbering service's contract | `Services/Invoicing/InvoiceCreationService.cs:93` | backlog | `5fca3cf` |
| PPW-679 | 🟡 | v15 | After the 10-poll budget the payment-confirming spinner spins forever with no terminal message | `src/app/features/orders/pages/confirmation-page.ts:280` | verified | `2302bec` |
| PPW-680 | 🟡 | v15 | canContinue ignores shippingCostsReady, so a restored session proceeds on a stale shipping cost | `src/app/features/checkout/pages/delivery-step.ts:393` | verified | `2302bec` |
| PPW-681 | 🟡 | v15 | Non-owner invoice PDF served with a one-year immutable browser cache | `Controllers/InvoicesController.cs:149` | backlog | `5fca3cf` |
| PPW-682 | 🟡 | v15 | ResetForTest deletes the migration's 42 EasyboxLocker seed rows and never restores them | `Tests/Helpers/PostgresTestDatabase.cs:106` | backlog | `5fca3cf` |
| PPW-683 | 🟡 | v15 | DropAllForeignKeys does not mark the pooled database dirty, so a constraint-free schema can be handed on | `Tests/Helpers/PostgresTestDatabase.cs:166` | backlog | `5fca3cf` |
| PPW-684 | 🟡 | v15 | Test-database sweep is scoped to its own salt, so pools from other worktrees are never reclaimed | `Tests/Helpers/PostgresTestDatabase.cs:292` | backlog | `5fca3cf` |
| PPW-685 | 🟡 | v15 | ResetSequences drops every public sequence the migration script did not literally CREATE, including identity-owned ones | `Tests/Helpers/PostgresTestDatabase.cs:128` | backlog | `5fca3cf` |
| PPW-686 | ⚪ | v15 | MaxBatchSize is used unclamped unlike the upload job's other settings | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:109` | verified | `9527eba` |

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
  - v12: re-raised by the certification pass — correctness, security, tests-coverage, convergence 3, verdict re-raise. Prior decision: PPW-489 wont-fix: owner accepted the retry posture; handler unchanged since e724528. Matched on same handler, same blind re-POST mechanism as the decided row

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
  - v12: re-raised by the certification pass — correctness, convergence 1, verdict confirmed. Prior decision: backlog — triaged as a minor and deferred inside this target. Matched on same undisposed retried response in the same handler. SendAsync→Pipeline.ExecuteAsync calls base.SendAsync 3x: attempts 1-2 return 503, matched by ShouldHandle (line 41), Polly retries without touching the discarded HttpResponseMessage; attempt 3 returns 200 and is returned. Polly v8's retry strategy never disposes a superseded Outcome.Result — that's documented as the caller's responsibility, and this handler (and its Sameday sibling, whose OnRetry only logs) does neither. The two 503 responses/content streams leak until GC finalization.

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
  - v12: re-raised by the certification pass — quality, convergence 1, verdict confirmed. Prior decision: backlog — triaged as a minor and deferred inside this target. Matched on same buyer-name duplication between the XML builder and the PDF; the pass adds that the PDF never truncates the name the XML caps at 200. Confirmed by direct comparison: PDF lines 84-88 duplicate InvoiceXmlBuilder lines 108-114's guest-label/name-fallback chain verbatim (literal "Persoană fizică" vs its GuestBuyerName const), and PDF lines 98-100 re-code InvoiceAddressFormatter.FormatStreetName's join instead of calling it. Present divergence exists today: XML truncates buyerName to PartyNameMaxLength (200) via InvoiceAddressFormatter.Truncate; PDF never truncates, so a RecipientName/FirstName+LastName over 200 chars already renders full in PDF but truncated in XML for the same invoice. A future edit to GuestBuyerName, the fallback order, or FormatStreetName's join in one file silently diverges from the other.

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
  - v12: re-raised by the certification pass — db-parity, convergence 1, verdict re-raise. Prior decision: PPW-505 backlog: v1 cleanup, backlogged; the named migration was later deleted by the squash. Matched on same UTC-versus-Romanian fiscal-year disagreement, now at the squashed migration's line

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
  - v13: fixed @`ec29613` — the pipeline sentence is gone from the settings docstring, which now points at `AnafSettings` for what the ANAF flag governs, and the registration comment says the builders are wired but only the worker calls them
  - v13: verified by reading — the docstring and the registration comment were re-read against `AnafSettings` and the worker's call sites

### PPW-510 — ADR-022 left stale while the deployment guide and the decision index send an operator to it as current authority

- **What:** ADR-022 still says the flag gates a real customer email and that XML build, ANAF upload, PDF render and storage write run regardless of it. Both statements are false. Keeping the ADR frozen as a bolt record is the right convention, but three live documents route a reader to it: the deployment guide's flag table cites it for the flag, its rollout section opens "per ADR-022", and the decision index tells the reader to open the ADR when flipping the flag "to recall what side effect is gated". The ADR carries no marker saying it is out of date.
- **Evidence:** `memory-bank/bolts/039-efactura-anaf/adr-022-dual-write-rollout-via-feature-flag.md:54-70`; `docs/DEPLOYMENT.md:1309,1411`; `memory-bank/standards/decision-index.md:43`.
- **Suggested fix:** Either add one superseded line at the head of the ADR pointing at the decision index, or drop the "use this ADR to recall what side effect is gated" clause and the two citations that present it as current. Doc-only.
- **History:**
  - v3: found by the verification pass reviewing the fix round's decision to keep ADR-022 and ddd-02 as point-in-time records. The decision is agreed; the routing into it is the defect
  - v13: fixed @`ec29613` — the ADR keeps its frozen-record convention and gains one note at the head naming both statements that no longer hold and sending the reader to the decision index
  - v13: verified by reading — the ADR's note names both false statements and the decision index carries the current summary

### PPW-511 — EuPlatesc coverage waived twice on a removal that no work item tracks, against a standard that forbids the divergence

- **What:** `definition-of-done.md` defect class 2 names Stripe/EuPlatesc as a pair whose every behaviour is "either verified symmetric or documented divergent". Two rounds have now left the EuPlatesc arm neither, both waived on the same removal: PPW-233 on 2026-07-27 and PPW-508 on 2026-08-14. The divergence is written only in review resolution files, which are not standards, and no work item tracks the removal — not the backlog, not `memory-bank/bolts/`, not `memory-bank/intents/`. Meanwhile the integration is fully live: `POST /api/webhooks/euplatesc`, `IEuPlatescService` registered, and its credentials required in Production. If the removal never lands, both coverage gaps stand forever with nothing recording that they were accepted. Measured: reverting the EuPlatesc call site alone leaves all 25 scoped tests green.
- **Evidence:** `memory-bank/standards/definition-of-done.md:52-53`; `memory-bank/standards/system-architecture.md:90-92`; `Program.cs:211-214,223`; `Controllers/WebhooksController.cs:141,205`.
- **Suggested fix:** Record the removal as one work item, and until it lands add one line to `definition-of-done.md` class 2 naming EuPlatesc coverage as an accepted divergence with its expiry. `system-architecture.md` still presents EuPlatesc as a current payment backend, so it needs the same line — CLAUDE.md requires a standard to be updated in the change that alters what it describes. Docs and tracking only; no code change, and the removal ruling itself is not in question.
- **History:**
  - v5: found by the verification pass checking whether the owner ruling that dropped PPW-508 EuPlatesc leg is recorded anywhere as work. It is not, and the standard that mandates the coverage still reads as if it were being met
  - v13: fixed @`06fd2b1` — the removal this row existed to track happened in PR #13, so the untracked-removal gap it named is closed

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
  - v12: re-raised by the certification pass — race, convergence 1, verdict re-raise. Prior decision: PPW-516 deferred: re-affirmed at v6: Postgres nextval is atomic and SQLite is gone, so the collision path is unreachable in normal use. Matched on same exhausted-retry 200 answer to the payment processor

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
  - v12: re-raised by the certification pass — completeness-critic, convergence 1, verdict re-raise. Prior decision: PPW-519 disputed: re-affirmed at v6: this is a vetted prior decision (PPW-480), not a defect. Matched on same retry wiping the submitted XML snapshot

### PPW-520 — Per-line PriceAmount x InvoicedQuantity no longer equals LineExtensionAmount, and nothing asserts it

- **What:** The suite's own fixture (quantity 3, gross 21.00) now emits LineExtensionAmount 17.65 with PriceAmount 5.88 — 5.88x3 = 17.64. The residual reconciliation test only checks the header sum; no test asserts line-level consistency and no CIUS-RO/EN16931 schema or validator runs anywhere, so ANAF is the first validator to see it.
- **Evidence:** `Services/Invoicing/InvoiceXmlBuilder.cs:219`
- **Suggested fix:** Assert PriceAmount x quantity == LineExtensionAmount (or emit BaseQuantity), and add an offline XSD/Schematron check over a built document.
- **History:**
  - v6: found by the delta pass — raised by correctness, completeness-critic, tests-coverage (convergence 3), verdict confirmed
  - v6: fix round — deferred. The repo documents only two decimals for emitted amounts and story 001s schema check was never built, so nothing local can adjudicate the options
  - v6: re-affirmed @`2979ea0` — BuildInvoiceLines still rounds netUnitPrice independently of netTotal, and no XSD/Schematron validator exists anywhere in the repo to adjudicate
  - v12: re-raised by the certification pass — requirements, completeness-critic, convergence 2, verdict re-raise. Prior decision: PPW-520 deferred: re-affirmed at v6: depends on an undocumented rounding rule and no XSD validator exists in the repo. Matched on same per-line price-times-quantity mismatch, now with the missing schema validation named

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
  - v12: re-raised by the certification pass — frontend-ux, convergence 1, verdict re-raise. Prior decision: PPW-524 deferred: owner ruled it out of scope at v6; the frontend-ux lens runs for the first time in this pass. Matched on same missing SPA consumer for the invoice endpoint
  - v13: verified — the mechanical red leg is not trustworthy here — this row's commit touches a file a later commit in the round touched again, so a file-level revert can break compilation and read as red. Re-proved by hand in isolation: renaming the download button's class reddened one `confirmation-page` spec, restored green

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
  - v9: re-raised by the delta pass — completeness-critic, convergence 1, skeptics skipped as a decided re-raise. Prior decision, verbatim from the v6 line above: "fix round — wont-fix on the owner ruling that EuPlatesc is being removed; PPW-511 tracks that the removal is untracked". Re-affirmed at `c8d6bb4`: the ruling stands, PPW-511 is still open, and the new fact this pass adds is that both webhook legs now share one failure-metric wrapper whose EuPlatesc half answers a different response contract — still waived with the integration
  - v12: re-raised by the certification pass — tests-coverage, convergence 1, verdict re-raise. Prior decision: PPW-526 wont-fix: owner ruled EuPlatesc is being removed; PPW-511 tracks that the removal is written down nowhere. Matched on same untested EuPlatesc paid leg
  - v13: the waiver this row rests on is now moot — PR #13 deleted the EuPlatesc webhook leg whose coverage was waived, so there is no untested code left behind the ruling. Status stays wont-fix, the owner ruling that closed it

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
  - v12: re-raised by the certification pass — quality, convergence 1, verdict unverified-cleanup. Prior decision: backlog — triaged as a minor and deferred inside this target. Matched on same caller-less Guid overload

### PPW-546 — Retry pre-read pulls the whole XmlPayload from the DB just to log its length

- **What:** Each admin retry selects i.XmlPayload — a full UBL invoice document, hundreds of KB — over the wire so it can log XmlPayload?.Length, immediately discarding it.
- **Evidence:** `Services/Invoicing/InvoiceLifecycle.cs:104`
- **Suggested fix:** Project the length instead: .Select(i => new { XmlLength = i.XmlPayload!.Length, i.LastError }) — both providers translate it to length().
- **History:**
  - v6: found by the delta pass — raised by db-parity (convergence 1), verdict unverified-cleanup
  - v12: re-raised by the certification pass — quality, convergence 1, verdict unverified-cleanup. Prior decision: backlog — triaged as a minor and deferred inside this target. Matched on same full XML pre-read to log its length

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
  - v9: re-raised by the delta pass — correctness and completeness-critic, convergence 2, skeptics skipped as a decided re-raise. Prior decision, verbatim from the v6 line above: "🟡, entered ledger as `backlog` per README router". Re-affirmed as `backlog` at `c8d6bb4`, severity unchanged; the pass adds that the orphaned paragraph promises "the next tick retries on the natural schedule", the opposite of the hold-the-claim behaviour PPW-559 now indicts

### PPW-553 — The 2 h ANAF auth-outage window has no floor tied to PollIntervalMinutes, so a validator-legal interval above it defeats the dedup

- **What:** `AuthOutageAlertWindow` is a flat 2 h constant while `AnafSettingsValidator` accepts `PollIntervalMinutes` up to 1440. Any configured interval above 120 minutes makes each tick land outside the previous window, so the credential Error and the Sentry capture fire every tick again and the dedup PPW-551 added silently stops working. Nothing warns that the two settings disagree.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:17`; `Configuration/AnafSettingsValidator.cs:39-40`.
- **Suggested fix:** Derive the window from the poll interval with a floor and a cap — for example the greater of 2 h and two poll intervals, capped well inside the submission deadline — or reject the disagreeing configuration in the validator. Suggested test: configure `PollIntervalMinutes` above the window, drive two ticks with a failing credential, and assert one Error, not two.
- **History:**
  - v7: found by the v7 verification pass's fix-diff review — fix-generated by PPW-551, whose own mechanism carries the gap
  - v8: fix round — the window is now `max(2 h, 4 × PollIntervalMinutes)`, computed once in the constructor, so it widens with the interval and no value the validator accepts can outrun it. The approach-check refuted the drafted `2 × interval` clamped at 12 h twice over: the per-row cooldown at line 83 excludes a just-failed row from the next tick, so consecutive auth attempts sit up to two intervals apart and a two-interval window expires exactly when the next attempt lands; and a 12 h cap left the defect verbatim above a 720-minute interval. No cap is needed, because the validator's 1440-minute maximum already bounds the window at 96 h, inside the 120 h that 5 business days give. The drafted boot-time Warning about disagreeing settings was dropped: a monotone formula leaves no disagreement to report, and no test reaches the job's `ExecuteAsync`; `interval_minutes` joins `alert_window_minutes` on the `auth-outage-continues` Warning instead, which an existing test already reaches. Two corrections to the finding: the validator is at `Validators/AnafSettingsValidator.cs`, not `Configuration/`, and the suggested test is green against the defect unless the MemoryCache clock is advanced between the ticks. Class sweep: the only other outage-alert window in any background job is `ShipmentTrackingJob.cs:22`, flat 30 minutes against a `TrackingIntervalMinutes` with no maximum at all — parked, since fixing another feature's alerting is outside this finding set
  - v8: verified @`2daf61e` — revert-and-rerun (isolated worktree, commit `e782189`) reddened `InvoiceUploadJobTests`, restore greened it. The fix-diff review re-derived the 4× multiplier and the 96 h ceiling from the code and confirmed both, and found the follow-up commit changed a comment only
  - v8: note — the parked Sameday twin at `ShipmentTrackingJob.cs:22` is worse than this defect ever was, because `TrackingIntervalMinutes` has no maximum and no cooldown offsets its flat 30-minute window; and the bare `auth` outage key can now mask a second, distinct credential failure for up to 96 h instead of 2 h, with the row's own `LastError` the only live signal

### PPW-554 — The bucket-versus-key miss-cause preference has no regression test

- **What:** The miss-cause preference that distinguishes a missing S3 bucket from a missing key was added by a micro-review follow-up and no test covers it. Reverting that follow-up leaves the whole suite green, and this exact logic was already wrong once in the same round.
- **Evidence:** `Controllers/InvoicesController.cs:83-87`.
- **Suggested fix:** Add one test per cause, asserting the logged event carries the adapter's inner cause for a missing bucket and for a missing key.
- **History:**
  - v7: found by the v7 verification pass's fix-diff review — fix-generated by PPW-550's follow-up; 🟡, entered ledger as `backlog` per README router
  - v12: PPW-618 re-opens this row's premise — the S3 adapter attaches the same inner exception for a missing key as for a missing bucket, so the preference this row would pin cannot separate the two causes

### PPW-557 — New mandatory-address guard makes every Easybox order permanently un-invoiceable

- **What:** The guard PPW-512's fix added refuses a blank buyer address, but nothing fills one for a locker order, so Build() throws after the customer is charged. The PDF is never rendered, `GET /api/orders/{id}/invoice` answers 404 for ever, and the row retries every tick with no exit.
- **Evidence:** `Services/Invoicing/InvoiceXmlBuilder.cs:131` throws; `src/PhotoPrint.UI/src/app/core/services/checkout-state.service.ts:39` sends street, city and postal code as empty strings; `Validators/Payments/CreateOrderRequestValidator.cs:32` only length-bounds them for Easybox; `Services/OrderService.cs:156` stores that snapshot verbatim; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:267` builds and catches at 294; `Controllers/InvoicesController.cs:66` returns the permanent 404.
- **Suggested fix:** Give a locker order a real fiscal buyer address before the XML is built — at order or invoice creation, not derived inside Build() — keep the guard, and unstick the rows already looping. **Files:** `InvoiceXmlBuilder.cs:131`, `Anaf/InvoiceUploadJob.cs:267`, `InvoiceLifecycle.cs:44`, `CreateOrderRequestValidator.cs:32`, `OrderService.cs:156`, `checkout-state.service.ts:39`. **Path:** Easybox checkout sends a contact-only snapshot, the order is paid, Build throws, LastError is set and the status stays Pending, the claim is released, the next tick repeats, and PdfStoragePath is never set. **Test shape:** `InvoiceXmlBuilderTests.Build_EasyboxOrder_WithContactOnlySnapshot_Succeeds` — Easybox order whose ShippingAddress carries recipient and phone only, act Build, assert no throw and a buyer PostalAddress; reddens today with "has no buyer address". **Trigger-list-shaped:** no by the list, but which address a locker order may carry is an open owner question, so the pre-check ran anyway.
- **History:**
  - v9: found by the delta pass — correctness, convergence 1, not hinted, verdict confirmed with a built trace. Fix-generated by PPW-512, whose v6 resolution parked "what address a locker order should carry" as an owner question and shipped the guard regardless
  - v9: Approach pre-check: refuted (EasyboxLocker carries no postal code at all and is not loaded on the invoicing path, so "fill from the locker" cannot work; the one existing locker-address derivation invents the sentinel "000000", which on a fiscal document is worse than refusing; and the drafted test would invert three tests that lock the throw in deliberately. The fix belongs upstream at order or invoice creation, must also unstick the already-looping rows through a path the admin retry cannot reach today, and must guard `Services/Invoicing/InvoicePdfDocument.cs:90`, which has no check of its own)
  - v9 fix round: the round parked the decision, not the defect. No code moved; the row stays `open` because which fiscal address a parcel-locker order may carry is unanswered. The resolution records it as the round's parked item and stands `in-progress`
  - v9: the Evidence line's "retries every tick with no exit" overstates the cadence — the per-row cooldown at `InvoiceUploadJob.cs:96` already spaces attempts one poll interval apart, so the observed cost was a permanent 404 plus one failed build per interval. Corrected here rather than in the block, which the doc gate holds append-only
  - v9: fix round — the owner delegated the ruling and it was implemented in full: locker checkout collects the same fiscal address as home delivery through the same form and the same server rules, and an invoice that can never be built parks `Pending → Failed` with a reason saying a retry will not help, through the same transition PPW-559's budget park uses. A second approach-check ran (`revised`) after the earlier refutation. No already-paid locker order is repaired by this; whether the locker's own postal address would be legally acceptable for those is an open question for the owner
  - v10: verified @`f769e22` — the five backend suites reddened on reverting both fix commits and greened on restore. The three frontend suites could not run in the isolated worktree, so the checkout gate was proven by hand in the main checkout: dropping the fiscal-address term from the Easybox branch of `isDeliveryComplete` failed exactly the three locker-gate tests, and restoring it returned 3 of 3 files green

### PPW-558 — Anonymous Stripe webhook buffers an unbounded request body into a string before any signature check

- **What:** The Stripe webhook action is anonymous and disables the request-size limit, and the handler reads the whole body into a string before the signature is verified. One unauthenticated multi-gigabyte POST can exhaust the API's memory.
- **Evidence:** `Controllers/WebhooksController.cs:69` (anonymous plus the disabled size limit), `:73` (the body is read to end), `:84` (the signature is checked afterwards); `Extensions/SecurityExtensions.cs:59` limits requests per IP, not bytes; `Caddyfile:1` and `docker-compose.prod.yml:10` set no body maximum anywhere at the edge.
- **Suggested fix:** Reject an oversized body inside the action, before verification, using the exception the middleware already maps to 413, and keep an attribute limit as the byte-level backstop. **Files:** `WebhooksController.cs:69`, `:73`, `:84`, `Extensions/SecurityExtensions.cs:59`, `Caddyfile:1`, `docker-compose.prod.yml:10`. **Path:** anonymous POST with a bogus signature and a multi-gigabyte body, no size cap anywhere, the body is buffered into a string, and only then is the signature rejected. **Test shape:** `WebhooksControllerTests.StripeWebhook_RejectsOversizedBody` — a body stream that never ends and no Content-Length, assert the 413-mapped exception and that the verifier was never called. **Trigger-list-shaped:** yes (adds a limiter) — approach pre-check run.
- **History:**
  - v9: found by the delta pass — security, convergence 1, not hinted, verdict confirmed with a built trace. Not fix-generated: the attributes date from the initial commit; it surfaced now because the `security` lens had not run on this target since v1
  - v9: Approach pre-check: revised (the drafted attribute alone answers 500 plus a Sentry capture, not 413, because the exception middleware maps exact types and Kestrel's oversize exception is not in the map; enforce in the action with the existing `RequestEntityTooLargeException` at a 1 MB cap — 256 KB risks rejecting a genuine event and buying a three-day Stripe retry — keep the attribute as the byte backstop that also covers a chunked body with no Content-Length, and fix two sibling sites in the same change: the EuPlatesc IPN materialises its whole form before checking its signature, and `Filters/DetectLegacyShippingCostFilter.cs:29` calls EnableBuffering with no limit on the payment endpoints, reachable with a free guest token. The drafted unit test can never pass, because the size filter never runs on a direct action call and TestHost does not surface the limit either)
  - v9 fix round: the Evidence line's read-to-end citation is off by two — at `c8d6bb4`, `:73` opens the `StreamReader` and `:75` is the `ReadToEndAsync` call. Corrected here rather than in the block, which the doc gate holds append-only
  - v9: fix round — the action reads at most 1 MB and throws the 413-mapped exception before the signature check; the EuPlatesc IPN and DetectLegacyShippingCostFilter are capped in the same change, and the middleware now answers Kestrel’s own status instead of a 500 plus a Sentry capture
  - v10: verified @`f769e22` — revert-and-rerun over both fix commits reddened its four suites, restore greened them; the fix-diff review found no other anonymous endpoint buffering a body before authenticating

### PPW-559 — Upload-timeout branch holds a claim that always expires before the next tick, so the same invoice is re-uploaded to ANAF

- **What:** On an upload timeout the row keeps its claim instead of releasing it, but the claim lives 10 minutes by default and the next tick is 30 minutes away. The row is re-claimed and the same invoice number is submitted to ANAF a second time, with nothing reconciling the first, unknown-outcome attempt.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:343` (the timeout branch), `:220` (the error write touches LastError and UpdatedAt only), `:94` (the batch guard); `Configuration/AnafSettings.cs:27` (claim 10 min, poll 30 min); `Services/Invoicing/Anaf/AnafSpvClient.cs:37` sends no idempotency key; `Services/Invoicing/InvoiceLifecycle.cs:44`.
- **Suggested fix:** Persist an explicit outcome-unknown hold that outlives the poll interval and has a stated exit, instead of relying on the claim. **Files:** `Anaf/InvoiceUploadJob.cs:343`, `:220`, `:94`, `InvoiceLifecycle.cs:44`, `Configuration/AnafSettings.cs:27`, `Anaf/AnafSpvClient.cs:37`. **Path:** tick T times out and keeps the claim, tick T+30 is skipped by the per-row cooldown, tick T+60 passes both the cooldown and the expired-claim guard and re-posts the cached payload under the same invoice number. **Test shape:** `InvoiceUploadJobTests.UploadTimeout_ThenLaterTick_ReuploadsSameInvoice` — Pending invoice, client throws the upload-timeout exception, run a tick, advance the clock 60 minutes, run again, assert the upload was called once; today it is called twice. **Trigger-list-shaped:** yes (changes the claim's concurrency model and retry semantics) — approach pre-check run.
- **History:**
  - v9: found by the delta pass — race, convergence 1, not hinted, verdict confirmed with a built trace. Fix-generated by PPW-515, whose resolution stated "an upload timeout holds its claim rather than re-uploading"; this pass shows that claim is false, because the claim expires two ticks before the row is next considered
  - v9: Approach pre-check: revised (every field the draft names is unusable — UpdatedAt is overwritten by the error write on the line before, no ANAF-status member for a hold exists, and both named exits are absent, because admin retry rejects a Pending row and the failure transition needs a Submitted one; ClaimedAt is the only field with no other consumer, and a future-dated hold there lies to the claim-lost log. The load-bearing skip today is the 30-minute per-row cooldown, not the 10-minute claim, so releasing the claim would not change the re-post timing and a longer hold only re-times a blind re-post: fix by capping blind re-posts, giving a held row an operator exit, and settling the invoice-number dedupe premise the job's own comment asserts. A legal configuration re-posts after 2 minutes, the auth handler can replay one body up to 8 times inside a single call, and the drafted test is red for the wrong reason unless it uses the real lifecycle and an advanceable clock)
  - v9: fix round — uploads ANAF never confirmed are counted on the row, which parks as Failed at Anaf:MaxUnknownUploadOutcomes (3) where the admin retry reaches it and resets both count and claim; the claim is left to expire, because releasing it lets a co-replica re-post seconds later
  - v10: verified @`f769e22` — revert-and-rerun over all three fix commits reddened its three suites, restore greened them; the shared park path is one method serving this row and PPW-557

### PPW-560 — Squashed InitialPostgres baseline has no upgrade path: a database that ran the deleted chain cannot boot

- **What:** A PostgreSQL database whose migration history still names the three deleted migrations sees the squashed baseline as pending. Boot migrates unconditionally, the baseline's first CREATE TABLE fails with 42P07, and the API never starts.
- **Evidence:** `Migrations/20260820133204_InitialPostgres.cs:12` is a plain CREATE TABLE; `Program.cs:332` migrates with no applied-migrations check and no try/catch; `Tests/Integration/MigrationChainTests.cs:15` only ever migrates a fresh database; `docs/DEPLOYMENT.md:157`; `memory-bank/standards/data-stack.md:29`.
- **Suggested fix:** Document and script a one-time migration-history reseed to the two surviving ids, or make the baseline's first steps tolerate existing objects. **Files:** `20260820133204_InitialPostgres.cs:12`, `Program.cs:332`, `Program.cs:93`, `Tests/Helpers/PostgresTestDatabase.cs:29`, `docs/DEPLOYMENT.md:157`, `memory-bank/standards/data-stack.md:29`. **Path:** a database carrying the deleted ids, boot-time Migrate, 42P07 on the first table, rolled-back migration, API down. **Test shape:** seed a migration-history row for a deleted id in a fresh test database, then Migrate and assert the failure is either handled or documented as impossible. **Trigger-list-shaped:** no (a one-time script plus documentation) — no approach pre-check run.
- **History:**
  - v9: found by the delta pass — correctness, db-parity and completeness-critic, convergence 3, hinted, verdict plausible. The trace found the mechanism real but the state unreachable today: no deployed PostgreSQL exists, dev ran SQLite through EnsureCreated so the deleted ids were never recorded, and every test database is created fresh — only a hand-seeded history row reproduces the abort. The runbook section naming the deleted migrations is stale either way
  - v13: fixed @`56eb9be` — the unreachable half is recorded, not scripted: no database has ever run the deleted chain (dev used SQLite through EnsureCreated, which writes no history rows), so §7 now states the real three-migration chain and gives the `__EFMigrationsHistory` reseed to run if such a database ever appears
  - v13: verified by reading — a documented recovery procedure, checked against the three migration ids the assembly actually carries

### PPW-561 — PostgresTestDatabase catch-all turns any CREATE DATABASE failure into "no PostgreSQL server", with no retry

- **What:** The helper's constructor rethrows every Npgsql failure as "These tests need a reachable PostgreSQL server". A wrong password or an exhausted connection limit is reported as an unreachable server, with no retry, so a transient failure becomes a hard failure with a misleading message.
- **Evidence:** `Tests/Helpers/PostgresTestDatabase.cs:33`, `:35` (the catch and its message), `:113` (the admin connection opens here); about 28 construction sites, e.g. `Tests/Unit/Services/Invoicing/Anaf/InvoiceUploadJobTests.cs:33`, `Tests/Unit/Services/OrderNumberServicePostgresTests.cs:14`, `Tests/Integration/ObservabilityHostCollection.cs:7`.
- **Suggested fix:** Keep the unreachable-server message for connection-level failures only, and report the real SQLSTATE otherwise; retry only where a transient cause is real. **Files:** `PostgresTestDatabase.cs:33`, `:35`, `:113`. **Path:** a live server with wrong credentials answers 28P01, the catch relabels it "unreachable", and the developer looks in the wrong place. **Test shape:** `PostgresTestDatabaseTests.BadCredentials_ReportsAuthFailure` — a live server with a bad password, construct the helper, assert the message names authentication or the inner SQLSTATE; reddens today. **Trigger-list-shaped:** yes (adds a retry and backoff) — approach pre-check run with PPW-562 and PPW-563 as one cluster.
- **History:**
  - v9: found by the delta pass — correctness, convergence 1, hinted, verdict confirmed with a built trace. The trace corrected the claimed cause: the concurrent-create leg is not constructible, because nothing connects to the template database; the reachable mislabels are a bad password and an exhausted connection limit
  - v9: Approach pre-check: revised (drop the retry entirely — the duplicate-name code cannot occur against a per-run unique database name and could never succeed on a retry of the same name, the concurrent-create code needs a session in the template database that nothing here opens, and the one genuinely transient code, too-many-connections, is caused by PPW-562 and disappears with it, while retrying a bad password or a missing privilege only makes an honest error slow. Ship the message split alone — a server that answered is reachable, so report its SQLSTATE — and wrap the migrate call, which sits outside the try and leaks a database plus its connections whenever it throws. Land together with PPW-563, whose default the message names)
  - v12: owner regraded 🟠 to 🟡 on 2026-08-24 and moved it to backlog — a developer papercut with no CI or production exposure; the peer merge fixed the connection leak

### PPW-562 — PostgresTestDatabase is per-test, not per-class: about 100 real databases plus full migration chains per run

- **What:** The helper is a plain instance field, and xUnit builds a new class instance per test, so every test creates a database, migrates the 809-line baseline with its 42 seeded rows, and drops it. That is about 100 cycles per run on a machine where a full run already saturates.
- **Evidence:** `Tests/Helpers/PostgresTestDatabase.cs:25` (the field), `:49` (Migrate), `:99` (Dispose drops and clears pools); `Tests/Unit/Services/Invoicing/InvoiceLifecycleTests.cs:18`; `Tests/Unit/Services/Invoicing/Anaf/InvoiceUploadJobTests.cs:448`; `Migrations/20260820133204_InitialPostgres.cs:690`.
- **Suggested fix:** Share one database per test class and reset state between tests, or create each from one already-migrated template; correct the "per test class" claim in the CI workflow and the data-stack standard. **Files:** `PostgresTestDatabase.cs:25`, `:49`, `:99`, `InvoiceLifecycleTests.cs:18`, `InvoiceUploadJobTests.cs:448`, `.github/workflows/ci.yml:73`. **Path:** one construction per test method, each a create, a full migrate and a drop, serialised against everything else on the machine. **Test shape:** count constructions in one class — a static counter asserted equal to 1 in each test; reaches 8 today in the lifecycle class. **Trigger-list-shaped:** yes (changes the test suite's concurrency model and resource budget) — approach pre-check run with PPW-561 and PPW-563 as one cluster.
- **History:**
  - v9: found by the delta pass — completeness-critic, convergence 1, hinted, verdict confirmed with a built trace. The trace raised the count from the claimed 53 to about 100 and added that Dispose clears every pool in the process, which compounds the cost for whatever is running in parallel
  - v9: Approach pre-check: revised (it cannot be applied class-uniformly — one Sameday class drops the Orders table in 3 of its tests and the migration test's whole premise is a fresh database, so both need splitting or exempting, and two constructors run schema changes that must move into the shared fixture. "Reset between tests" hides two requirements: three families of standalone sequences must be restarted, including ones created lazily at run time, and the 42 seeded locker rows must survive, so truncating everything with restarted identities is the wrong primitive. The already-migrated-template variant races across parallel collections and fights connection pooling, so share per class and reset. Acceptance check: run a class twice in one process and see the two existing absolute-number tests still pass)
  - v12: fixed @`4dd6763` by a peer branch, not by this loop — the merge of chore/faster-relational-tests replaced the per-test full migrate with per-class fixtures and pooled reuse; a verification pass still owes it a revert-and-rerun

### PPW-563 — Removing the skip guard hard-fails every Postgres-backed test, and the default credentials do not match docker-compose

- **What:** Without the connection environment variable the helper falls back to postgres/postgres, while the repository's own compose stack starts PostgreSQL with the fototipar account. A developer running the tests against it now gets errors in about 15 test classes where the suite used to skip.
- **Evidence:** `Tests/Helpers/PostgresTestDatabase.cs:16` (the default admin connection string), `:33` (the failure is rethrown), `:99` (process-wide pool clearing); `docker-compose.yml:15`; `Tests/Unit/Services/Invoicing/InvoiceLifecycleTests.cs:18`; `Tests/Integration/S3StorageServiceIntegrationTests.cs:47` keeps the only remaining skip guard.
- **Suggested fix:** Match the default admin credentials to the repository's own compose stack, and scope pool clearing to this database. **Files:** `PostgresTestDatabase.cs:16`, `:33`, `:99`, `docker-compose.yml:15`. **Path:** compose starts the fototipar account, the helper connects as postgres, CREATE DATABASE fails with an authentication error, and every test in the Postgres-backed classes errors rather than skipping. **Test shape:** `PostgresTestDatabaseDefaultsTests.Default_admin_credentials_match_dev_compose` — parse the compose credentials, read the helper's default admin connection string, assert they agree; reddens today. **Trigger-list-shaped:** no (a default value and a narrower pool call) — covered by the cluster pre-check run for PPW-561 and PPW-562.
- **History:**
  - v9: found by the delta pass — completeness-critic, convergence 1, hinted, verdict confirmed with a built trace. The trace judged the pool-clearing half weak, because only idle connections are discarded, so parallel classes lose pooling rather than correctness; that half is PPW-571
  - v9: Approach pre-check: revised (the compose credentials are one of three local credential sets in the repository — the gitignored development settings use a third account, which is the real dev-box convention — so defaulting to compose swaps one wrong default for another. Guess nothing: fail fast naming the environment variable and both in-repo candidates, or restore a presence-gated skip together with a CI assertion that nothing skipped, because about 117 relational tests would otherwise vanish silently on a future workflow edit. Delete the process-wide pool clear rather than scoping it, since the drop already forces disconnection and it was never needed for correctness. CI sets the variable explicitly, so no default change can break it)
  - v12: re-raised by the certification pass — completeness-critic, convergence 1, hinted, verdict re-raise. Prior decision: PPW-563 open: v9 named it; same owner decision as PPW-561. Matched on same Postgres test-database helper gap; the finding also restates PPW-561, PPW-562 and PPW-571
  - v12: owner regraded 🟠 to 🟡 on 2026-08-24 and moved it to backlog — local-only: CI is unaffected, tests hard-fail instead of skipping

### PPW-564 — Admin Paid path swallows the invoice-already-created race but still fires Paid side effects and overwrites the webhook's PaidAt

- **What:** When the webhook wins the race, the admin save hits the invoice unique index, the catch detaches the invoice and commits anyway. The customer gets a second confirmation email, the paid-notification runs twice, and the order's paid timestamp no longer matches the invoice's issue timestamp.
- **Evidence:** `Services/AdminOrderService.cs:119` (the order is read as awaiting payment), `:144` (the transition stamps a second timestamp), `:164` (the side effects fire), `:425` (the catch detaches and saves); `Controllers/WebhooksController.cs:412` shows the webhook's three-state outcome suppressing exactly these; `Services/Invoicing/InvoiceCreationService.cs:59`.
- **Suggested fix:** Give the admin path the same outcome result as the webhook, and on the already-invoiced outcome drop the stale timestamp and skip the two side effects. **Files:** `AdminOrderService.cs:119`, `:144`, `:164`, `:425`, `WebhooksController.cs:412`, `InvoiceCreationService.cs:59`. **Path:** admin reads awaiting payment, the webhook commits paid plus the invoice plus its email, the admin insert violates the index, the catch commits the admin's own timestamp, and the confirmation email and paid notification fire a second time. **Test shape:** `AdminOrderServiceTests.UpdateStatus_Paid_LosesRaceToWebhook_KeepsPaidAtAndSkipsEmail` — a fake creation service commits paid plus an invoice through a second context; assert the webhook's timestamp survives and neither side effect ran. **Trigger-list-shaped:** yes (changes the concurrency model and gates events) — approach pre-check run.
- **History:**
  - v9: found by the delta pass — race, convergence 1, not hinted, verdict confirmed with a built trace. Fix-generated by PPW-518, which added this catch and its retry loop but returned void where the webhook path returns an outcome
  - v9: Approach pre-check: revised (an outcome fed only by the catch misses the wider and likelier window — the pre-save existence query returns the committed invoice as an unchanged entity, so nothing throws and the admin timestamp is committed anyway; gate on the entity's state as well. The reload must replace the second save, not follow it, or it re-reads the value it was meant to drop. A reload discards nothing legitimate on this branch, but must stay Paid-only. Suppress the email and the paid notification; keep the broadcast, the purge hook and the response. The webhook's outcome type is private and pinned by a test that reflects on it, so give this path its own, and decide the exhausted branch explicitly rather than letting a default answer 200 for an unpaid order. The drafted test cannot redden: these tests run on EF InMemory with no unique index, and the violation classifier only recognises the PostgreSQL exception, so the proof needs the real Postgres helper)
  - v9: fix round — the path has its own outcome and gates on the invoice entity being Unchanged as well as on the index violation, then reloads instead of saving a second time, so the winner’s PaidAt still matches the invoice’s issue time; the confirmation email and the paid notification are skipped, the broadcast and the response kept
  - v10: verified @`f769e22` — revert-and-rerun reddened `AdminOrderServiceTests`, restore greened it; the review confirmed all three `PaidAt` write sites are now gated

### PPW-565 — Changed files no lens owns: EF model snapshot and Designers, Sameday registry, both .csproj, ci.yml

- **What:** The pass's own file manifest omitted the rewritten EF model snapshot, two designer files, a relocated Sameday base class, the dropped SQLite package in both project files and the new CI environment variable. Snapshot-versus-model drift would make the next scaffolded migration emit wrong SQL, unnoticed.
- **Evidence:** `Migrations/PhotoPrintDbContextModelSnapshot.cs:335`; `Migrations/20260821054658_AddInvoiceStorageLocation.Designer.cs:15`; `Data/PhotoPrintDbContext.cs:402`; `src/PhotoPrint.API/PhotoPrint.API.csproj:19`; `Tests/Helpers/PostgresTestDatabase.cs:14`; `Services/Sameday/TrackingStopRegistry.cs:9`.
- **Suggested fix:** Add a pending-model-changes assertion to the migration test so drift fails a run, and cover the relocated Sameday base class with its own tests. **Files:** `PhotoPrintDbContextModelSnapshot.cs:335`, `Tests/Integration/MigrationChainTests.cs:18`, `Data/PhotoPrintDbContext.cs:402`, `Services/Sameday/TrackingStopRegistry.cs:9`. **Path:** no lens owned these files, and no test or hook compares the snapshot with the model, so a future scaffolded migration inherits whatever drift exists. **Test shape:** assert `HasPendingModelChanges()` is false in the migration test — it needs no database and exists nowhere in the repository today. **Trigger-list-shaped:** no (adds an assertion and tests) — no approach pre-check run.
- **History:**
  - v9: found by the delta pass — completeness-critic, convergence 1, not hinted, verdict plausible. The trace found no drift today: the pending-model-changes command reports none, the snapshot matches the newest designer file, no SQLite usage remains, and the CI variable matches the helper's name — so the live defect is the missing guard and the manifest bookkeeping, not wrong SQL. Recurs the class PPW-497 named at v1
  - v9: fix round — the migration test now asserts HasPendingModelChanges() is false, so drift that adds no column fails a run; proven red with a throwaway Invoice property, and the relocated Sameday base class is covered through both subclasses
  - v10: verified @`f769e22` — test-only, so no revert leg applies; the review confirmed the assertion is present and fails on real drift

### PPW-566 — AnafSpvClient timeout-versus-shutdown classifier is untested, and Polly retries inside the 30 s budget misclassify definite failures

- **What:** The retry pipeline runs inside the client's own 30-second timeout, so three slow server errors end as a timeout and are labelled outcome-unknown, holding the claim, while identical fast errors are labelled unreachable and release it. No test covers either branch.
- **Evidence:** `Services/Invoicing/Anaf/AnafSpvClient.cs:56` (the classifier); `Services/Invoicing/Anaf/AnafResilienceHandler.cs:25` (3 retries, 1+2+4 s); `Program.cs:299` (the 30 s client timeout); `Services/Invoicing/Anaf/InvoiceUploadJob.cs:343` (what the label decides); `Configuration/AnafSettings.cs:41`; `Tests/Unit/Services/Invoicing/Anaf/AnafSpvClientTests.cs:35` has six tests, none about timeout or cancellation.
- **Suggested fix:** Test the classifier through a real client with a stub handler for both the timeout and the shutdown-cancellation branch, and give each attempt its own timeout so retry exhaustion stays a definite failure. **Files:** `AnafSpvClient.cs:56`, `AnafResilienceHandler.cs:25`, `Program.cs:299`, `Anaf/InvoiceUploadJob.cs:343`, `AnafSpvClientTests.cs:35`. **Path:** three slow 500s consume the budget, the outer timeout fires mid-backoff, the caller token is not cancelled, the label says outcome-unknown, and the row holds its claim. **Test shape:** `AnafSpvClientTests.Upload_after_retried_500s_hits_client_timeout` — a handler that always answers 500 behind the retry handler with a 2 s client timeout, assert the unreachable exception; today it throws the timeout one. **Trigger-list-shaped:** yes (changes retry semantics) — approach pre-check run with PPW-559 as one cluster.
- **History:**
  - v9: found by the delta pass — completeness-critic, convergence 1, not hinted, verdict confirmed with a built trace. Fix-generated by PPW-515, which introduced this classifier; adjacent to PPW-489's accepted retry tolerance but not a re-raise of it, because the fix asked for here is a per-attempt timeout, not the removal of retries
  - v9: Approach pre-check: revised (the real-client classifier tests are right and needed; the per-attempt timeout is not — the outer 30-second ceiling still fires unless each attempt is capped near 5 seconds, and the rejection Polly raises is neither retried nor caught anywhere, so it escapes to the batch loop's generic catch, which writes no LastError and releases no claim: the cooldown is then bypassed and the invoice is re-posted on the very next tick, strictly worse than today. Adding it to the retry predicate instead produces duplicate posts of the same invoice number inside one call. Split the tests by cause rather than by timing, and add one asserting the client never leaks an unclassified exception; the scripted handler needs at least four canned responses, and the retry pipeline is a static field with a real delay, so the test is timing-sensitive as written)
  - v9: fix round — the classifier is tested through a real client and a stub handler, split by cause on upload and poll, plus one test that no wire outcome escapes unclassified; the per-attempt timeout is deliberately not built, because the rejection it raises escapes to a catch that releases no claim
  - v10: verified @`f769e22` — test-only, so no revert leg applies; the review confirmed the classifier is exercised through a real client

### PPW-567 — Exhausted invoice-number collision retry escapes AdminOrderService with the order still tracked Paid

- **What:** Four consecutive number collisions on the manual Paid path let the database exception escape as a 500. Unlike the webhook path, the order is not reloaded, so the request context still holds a Paid order, and nothing is captured for triage.
- **Evidence:** `Services/AdminOrderService.cs:417`
- **Suggested fix:** Mirror the webhook's terminal catch: log the order number and total, capture the exception, reload the order and answer a conflict rather than a bare 500.
- **History:**
  - v9: found by the delta pass — correctness and race, convergence 2, verdict unverified-low (a delta pass runs no skeptic on 🟡). Fix-generated by PPW-518, which added this retry loop
  - v9: pulled out of the backlog and fixed in round 9's cluster D, because it is the same mechanism as PPW-564 in the same method; status moved `backlog` → `open` so verification covers it (resolution-v9, commit `ba8e628`)
  - v9: fix round — exhausted retries log the order number, total and both payment identifiers at Error, capture to Sentry through an optional hub, roll the transition back with a reload whose own failure is swallowed, and answer 409 rather than a Paid-looking 200
  - v10: verified @`f769e22` — revert-and-rerun reddened its suite, restore greened it; the exhausted branch answers 409 and rolls back

### PPW-568 — Admin manual-Paid retry loop: only the happy retry is tested, the exhausted and already-invoiced branches are not

- **What:** The already-invoiced branch runs a second save inside a catch with no retry, and the exhausted branch escapes as a 500 with no reconciliation-grade log. Neither branch has a test, so both are free to change behaviour unnoticed.
- **Evidence:** `Services/AdminOrderService.cs:414`
- **Suggested fix:** Add a test per branch, and log the order number with the payment identifiers plus a capture when the retries are exhausted, as the webhook path does.
- **History:**
  - v9: found by the delta pass — completeness-critic, convergence 1, verdict unverified-low. Fix-generated by PPW-518; the behaviour half of the same code is PPW-567
  - v9: pulled out of the backlog and fixed in round 9's cluster D alongside PPW-564 and PPW-567; status moved `backlog` → `open` so verification covers it (resolution-v9, commit `573dfb8`)
  - v9: fix round — every branch of the loop is covered in one Postgres-backed class, AdminOrderServicePaidRaceTests: both lost-race windows, the exhausted branch, the Sentry capture, both rollback-reload failures, the happy retry, and the container resolution of the optional hub
  - v10: verified @`f769e22` — test-only, so no revert leg applies; the review confirmed every branch is covered

### PPW-569 — CREATE SEQUENCE IF NOT EXISTS is not race-safe and only the ft_2026 sequence is seeded

- **What:** The baseline seeds only the 2026 sequence, so the first invoice of 2027 runs the create statement. Two webhook deliveries at the rollover both pass the existence check and one fails on a catalogue unique index, which is not the exception the retry loop catches, so it escapes as a 500 and the order stays unpaid.
- **Evidence:** `Services/Invoicing/PostgresInvoiceNumberingService.cs:46`
- **Suggested fix:** Catch the duplicate-object failure from the create and fall through to drawing the next value, and drop the comment claiming a concurrent create is safe.
- **History:**
  - v9: found by the delta pass — db-parity, convergence 1, verdict unverified-low. Not fix-generated: the statement dates from the bolt's own numbering commit. Adjacent to PPW-505, which covers the year-boundary constraint rather than the create
  - v10: pulled out of the backlog and fixed beside PPW-578, the same defect at the order-number site; status moved `backlog` → `fixed` so verification covers it (resolution-v10, commit `de1a4cb`)
  - v10: the comment claiming a concurrent `IF NOT EXISTS` is safe was checked and is false. A test that makes this caller lose the create race reddens on `23505: duplicate key value violates unique constraint "pg_class_relname_nsp_index"`. The comment is gone and the create runs inside a guarded `DO` block

### PPW-570 — PostgresTestDatabase contexts omit the split-query behaviour production configures

- **What:** Production registers PostgreSQL with split queries; the test helper does not. Every Postgres-backed test therefore runs one joined query where production issues several non-atomic ones, so a multi-collection include with a row limit can return mismatched children in production and never in a test.
- **Evidence:** `Tests/Helpers/PostgresTestDatabase.cs:53`
- **Suggested fix:** Mirror the production registration in the helper by setting the same query-splitting behaviour.
- **History:**
  - v9: found by the delta pass — db-parity, convergence 1, hinted, verdict unverified-low

### PPW-571 — PostgresTestDatabase.Dispose clears every Npgsql pool in the process while parallel test classes hold their own databases

- **What:** Test collections run in parallel and 16 classes use this helper. When one class finishes it discards the pooled connections of every other Postgres-backed class still running, which produces order-dependent flakes that a rerun hides.
- **Evidence:** `Tests/Helpers/PostgresTestDatabase.cs:99`
- **Suggested fix:** Clear only this database's pool — one data source per instance, disposed with it, or a pool clear scoped to this connection string.
- **History:**
  - v9: found by the delta pass — race, convergence 1, hinted, verdict unverified-low. This is the half the PPW-563 trace judged weak, kept separate because its fix is its own

### PPW-572 — MemoryCacheOnceRegistry.MarkOnce is a non-atomic read-then-write despite promising first-caller-only

- **What:** Two callers with the same key can both see nothing cached and both be told they are first, so a once-only alert pages twice. All three current subclasses run on single background loops, so it is latent — but the base is now a shared abstraction any request-path caller will trust.
- **Evidence:** `Services/MemoryCacheOnceRegistry.cs:23`
- **Suggested fix:** Make the mark atomic, and state the real guarantee in the interface documentation instead of promising the first call only.
- **History:**
  - v9: found by the delta pass — race, convergence 1, verdict unverified-low. Not fix-generated: the mechanism came with the Sameday tracking registry; PPW-551's fix only moved the base out of that namespace, which widened who may rely on it
  - v12: re-raised by the certification pass — race, convergence 1, verdict confirmed. Prior decision: backlog — triaged as a minor and deferred inside this target. Matched on same non-atomic MarkOnce read-then-write, now with a concurrent caller named. ShipmentTrackingJob.RunOneTickAsync (line 111) runs Task.WhenAll(inWindowIds.Select(PollOneAsync)), gated only by a SemaphoreSlim(5) (MaxConcurrentSamedayCalls default 5). If Sameday credentials are rotated, up to 5 orders' PollOneAsync tasks throw SamedayAuthException concurrently (line 157) and each calls _stop.MarkOutageOnce("auth", OutageAlertWindow) → same literal key "auth", genuinely parallel threadpool tasks. MarkOnce (MemoryCacheOnceRegistry.cs:23-26) does TryGetValue then Set with no lock; two tasks can both miss and both Set, both returning true, so LogError at ShipmentTrackingJob.cs:162 fires more than once for the single "auth" outage window — the exact race the finding describes, just via the outage-key path rather than the per-order path.

### PPW-573 — data-stack standard and the deployment guide left stale by the migration squash and the provider removal

- **What:** Both documents still say the chain holds a single migration, while it now holds two. Both compose files still set a database-provider variable the application no longer reads, so an operator who changes it sees no effect.
- **Evidence:** `memory-bank/standards/data-stack.md:29`; `docs/DEPLOYMENT.md:164`; `docker-compose.yml:44`; `docker-compose.prod.yml:34`
- **Suggested fix:** Name both migrations in both documents, and delete the dead provider entries from both compose files.
- **History:**
  - v9: found by the delta pass — completeness-critic, convergence 1, verdict unverified-low. Distinct from PPW-547, which covers the same standard's missing invoicing content; overlaps PPW-577 on the dead provider entries, so fix them once

### PPW-574 — InvoiceAddressFormatter.Truncate with maxLength 0 indexes before the string start and throws IndexOutOfRangeException

- **What:** A non-empty value with a maximum length of 0 reaches the surrogate check with an index of minus one and throws instead of returning an empty string. Unreachable today, because every caller passes a positive constant, but the method is now advertised as tolerant of edges.
- **Evidence:** `Services/Invoicing/InvoiceAddressFormatter.cs:20`
- **Suggested fix:** Return an empty string when the maximum length is zero or less, before the surrogate check.
- **History:**
  - v9: found by the delta pass — correctness, convergence 1, verdict unverified-cleanup. Fix-generated by PPW-535, whose surrogate-pair guard introduced the index

### PPW-575 — PostalZone is truncated with the borrowed CityNameMaxLength constant

- **What:** The request validator bounds a postal code to 20 characters, but the XML builder truncates it with the 50-character city constant. The constant states the wrong limit, and a legacy or admin-seeded 60-character code is silently cut to 50 rather than to 20 or rejected.
- **Evidence:** `Services/Invoicing/InvoiceXmlBuilder.cs:122`
- **Suggested fix:** Add a postal-code constant of 20, matching the validator, and use it here.
- **History:**
  - v9: found by the delta pass — correctness, convergence 1, verdict unverified-cleanup. Fix-generated by PPW-485, which introduced the truncation constants
  - v12: re-raised by the certification pass — correctness, input-validation, quality, convergence 3, verdict unverified-cleanup. Prior decision: backlog — triaged as a minor and deferred inside this target. Matched on same postal-code truncation with the city constant

### PPW-576 — Blob-missing log omits the stamped storage tier, so a cloud-off misconfiguration reads as a lost file

- **What:** A row stamped for cloud storage while the local provider is configured is unroutable, but the log line for a missing blob carries no stamped tier, so it is indistinguishable from a genuinely deleted file. The ZIP-export path treats the same state as an explicit configuration error.
- **Evidence:** `Controllers/InvoicesController.cs:122`
- **Suggested fix:** Add the stamped tier to the blob-missing log line, as the tier-mismatch line already carries, or log an unroutable-tier event when the stamped tier is unreachable.
- **History:**
  - v9: found by the delta pass — correctness, convergence 1, hinted, verdict unverified-cleanup. Fix-generated by PPW-517, which introduced the per-row stamp that the log line PPW-523 added was never extended to carry

### PPW-577 — Dead DatabaseProvider environment entry left in the Dockerfile, .env.example and both compose files

- **What:** The application no longer reads the provider variable, but four files still set it. An operator who edits it sees no effect, and its presence implies a provider switch that no longer exists.
- **Evidence:** `Dockerfile:42`; `.env.example:13`; `docker-compose.yml:44`; `docker-compose.prod.yml:34`
- **Suggested fix:** Delete the provider entry from all four files.
- **History:**
  - v9: found by the delta pass — db-parity, convergence 1, verdict unverified-cleanup. Not fix-generated: the entries date from the initial commit and were left behind by the PostgreSQL-only refactor. Overlaps PPW-573's second half — one change covers both

### PPW-578 — Order-number sequence is created check-then-act, so two first orders of a year fail on a catalogue unique index

- **What:** The per-year order sequence is created lazily inside a `DO` block that tests `pg_sequences` and then creates. Two callers placing the first order of a year both see the sequence missing and both create it. The loser fails on PostgreSQL's own catalogue uniqueness and order creation answers 500. The window opens at every New Year and at the first deploy — on the money path.
- **Evidence:** `Services/OrderNumberService.cs:37`; a full backend run (1446 passed, 1 failed of 1457) failed `Tests/Unit/Services/OrderNumberServicePostgresTests.cs:56` with `23505: duplicate key value violates unique constraint "pg_class_relname_nsp_index"`, and the class re-run alone passed
- **Suggested fix:** Create the sequence inside a PL/pgSQL block that swallows only the duplicate errors, shared with the invoice numbering site, which carries the same defect as PPW-569.
- **History:**
  - v10: found by the driver outside any pass, while preparing the v11 certification, and fixed on the owner's delegated authority in round 10 together with PPW-569
  - v10: fix round — both sites create through `PostgresSequences.EnsureAsync`, whose `DO` block swallows `42P07`, `42710` and `23505` and only when the name then holds a sequence; three tests make a caller lose the race deterministically, all red on the real `23505` first (resolution-v10, commits `de1a4cb` and `4d6bc6d`)

### PPW-579 — Static ro-RO culture in InvoicePdfDocument throws on the Alpine production image, wedging every invoice PDF

- **What:** Dockerfile runs on mcr aspnet:8.0-alpine, which has no icu-libs and enables globalization-invariant mode. First PDF render: CultureInfo.GetCultureInfo("ro-RO") throws CultureNotFoundException inside the type initializer. The worker's generic catch records LastError, releases the claim, retries every tick forever — no PDF, no ANAF upload, customer download 404s permanently.
- **Evidence:** `Services/Invoicing/InvoicePdfDocument.cs:19`
- **Suggested fix:** Install icu-libs and set DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false in the Dockerfile (or use an -extra base), and add a test that asserts the renderer works under invariant globalization. **Files:** `Services/Invoicing/InvoicePdfDocument.cs:19`, `Services/Invoicing/InvoicePdfRenderer.cs:23`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:281`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:315`, `Dockerfile:28`, `PhotoPrint.API.csproj:3`. **Path:** Reproduced: with DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1, InvoicePdfRendererTests fails with TypeInitializationException -> CultureNotFoundException "Only the invariant culture is supported... ro-ro is an invalid culture identifier" at line 19. Dockerfile's aspnet:8.0-alpine adds no icu-libs; csproj sets no InvariantGlobalization; no compose override. In prod: InvoiceUploadJob:281 Render throws, generic catch :315 records LastError, releases claim, returns — XML built, PDF and ANAF upload never happen, retried every tick forever. **Test shape:** InvoicePdfRendererTests.Renders_under_invariant_globalization: arrange a child dotnet run (or test host) with DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1; act Render(order, invoice, seller); assert bytes start "%PDF" — today throws TypeInitializationException/CultureNotFoundException. **Trigger-list-shaped:** no (a base-image and configuration change plus a renderer test) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — correctness, convergence 1, verdict confirmed
  - v12: fix round — the runtime stage installs `icu-libs` and `icu-data-full`, the stage's `ENV` turns the base image's invariant flag off, and the API's runtimeconfig pins `System.Globalization.Invariant` false, which a probe showed outranks the environment variable; the culture stays `ro-RO` on the owner's ruling. Two tests hold the shipped runtimeconfig and the runtime stage and were red on the revert; a third reads the renderer's culture by reflection and reddens in any invariant host (resolution-v12, commits `8e71c63` and `ed3ce30`)
  - v13: verified @`06fd2b1` — revert-and-rerun (isolated worktree, commits `8e71c63` and `ed3ce30`) reddened `InvoicePdfCultureTests` and the restore greened it. The image package names remain unproven: no Docker on this machine, so a wrong name fails the deploy image build rather than shipping broken

### PPW-580 — One MaxBatchSize batch mixes cooldown-exempt Submitted polls with Pending uploads, so stuck polls starve new invoices out of filing

- **What:** A poll that throws (400 on stareMesaj) or returns an unrecognised `stare` never writes LastError, so the cooldown filter never excludes it, and no admin path leaves Submitted (retry 409s it). 50 such rows accumulate, are oldest-first, fill Take(MaxBatchSize) every tick, and every newly paid order's invoice stays Pending past the 5-day deadline.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:102`
- **Suggested fix:** Query Pending and Submitted with separate caps, and record LastError/UpdatedAt on a failed or Unknown poll so the cooldown applies to Submitted rows too. **Files:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:98`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:157`, `Services/Invoicing/Anaf/AnafSpvClient.cs:129`, `Services/Invoicing/InvoiceLifecycle.cs:38`, `Controllers/AdminInvoicesController.cs:105`, `Configuration/AnafSettings.cs:31`. **Path:** MarkSubmittedAsync nulls LastError, so every Submitted row is cooldown-exempt forever. Seed 50 old Submitted rows whose stareMesaj returns 400: AnafSpvClient throws AnafUnreachableException, the batch loop's generic catch only logs — no LastError, no status change. Admin retry 409s Submitted; nothing else moves it. Each tick OrderBy(CreatedAt).Take(50) refills with the same 50, so a newer paid order's invoice stays Pending indefinitely. **Test shape:** StuckSubmittedPollsDoNotStarvePending: arrange 50 old Submitted rows (AnafUploadId set) with GetStatusAsync throwing AnafUnreachableException(400) plus one newer Pending row; run two ticks; assert UploadAsync called for the Pending invoice / it leaves Pending. **Trigger-list-shaped:** yes (changes the batch's resource budget and the per-row cooldown semantics) — approach pre-check run.
- **History:**
  - v12: found by the certification pass — race, convergence 1, verdict confirmed
  - v12: Approach pre-check: revised (the separate caps are right but aimed at the wrong trigger — 50 healthy in-progress rows starve Pending with no failure at all, because the poll writes nothing on an in-progress answer, and ordering by CreatedAt is the root cause. Order the Submitted query by UpdatedAt ascending and stamp UpdatedAt on every poll outcome, instead of using LastError as a scheduler. Record genuine poll failures through `IInvoiceLifecycle.RecordErrorAsync`, never a raw `ExecuteUpdateAsync`, or four existing tests break on the cooldown they do not advance. Per-tick ANAF fan-out becomes the sum of both caps, so the MaxBatchSize doc comment changes with it. claim-lost and row-missing stay exempt. The suite is real PostgreSQL per test and `Build` takes no batch-size parameter, so seed two Submitted rows and one Pending, not fifty)
  - v12: owner ruled wont-fix on 2026-08-22 — reaching it needs the tax authority to keep erroring on stareMesaj until 50 rows are stuck, and he accepts that risk rather than pay for the fix now; the consequence if it happens is that newly paid invoices stop being filed

### PPW-581 — Expired or revoked ANAF credentials never reach the auth-outage alert; they fan out as N generic row-failed errors per tick

- **What:** Anaf:ClientSecret is rotated. Token endpoint returns 400 invalid_client. RefreshAsync throws AnafUnreachableException (not AnafAuthException), so the job's catch(AnafAuthException) never runs: no Sentry page, authFailed stays false, no LastError, no cooldown. Every batch row logs anaf.upload-job.row-failed — up to MaxBatchSize Errors per tick, forever.
- **Evidence:** `Services/Invoicing/Anaf/AnafTokenProvider.cs:109`
- **Suggested fix:** Throw AnafAuthException for 401/403/invalid_client from the token endpoint and for a LoadCert failure; keep AnafUnreachableException for 5xx/transport only. **Files:** `Services/Invoicing/Anaf/AnafTokenProvider.cs:109`, `Services/Invoicing/Anaf/AnafAuthHandler.cs:57`, `Services/Invoicing/Anaf/AnafSpvClient.cs:111`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:157`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:355`, `Program.cs:302`. **Path:** Rotated secret: token endpoint answers 400. AnafTokenProvider.cs:109 throws AnafUnreachableException, thrown from AttachTokenAsync (AnafAuthHandler.cs:57) before any SPV call, so no 401 path and no AnafAuthException. For a Submitted row PollSubmittedAsync doesn't catch it: it escapes to InvoiceUploadJob.cs:157 as row-failed, authFailed stays false, no Sentry, no outage cooldown, repeated for every Submitted row each tick. Pending rows differ: they hit the unreachable catch (line 355), so LastError IS set there — that detail of the finding is wrong; the missing auth alert is real. **Test shape:** AnafTokenProvider_RejectedCredentials_ThrowsAnafAuthException: arrange stub token endpoint returning 400 invalid_client; act GetAccessTokenAsync; assert AnafAuthException (today AnafUnreachableException). Plus job test: two Submitted rows, one auth alert expected, currently two row-failed errors. **Trigger-list-shaped:** yes (re-maps an exception layer that drives alerting and cooldown) — approach pre-check run.
- **History:**
  - v12: found by the certification pass — observability, convergence 1, verdict confirmed
  - v12: Approach pre-check: revised (the re-map does reach the alert on every leg — nothing swallows it — but classify by status, not by the error body the provider deliberately never reads: 400, 401 and 403 are auth; 408, 429, 5xx and transport stay unreachable, or a rate-limit answer pages as a dead credential. `AnafAuthException` has one constructor, hardcoding "ANAF returned 401 twice", so LastError and Sentry would lie about a rejected secret or a certificate failure until it takes a reason and an inner exception. The token client sits outside the Polly pipeline, SPV 403 is still unclassified, and the batch loop still needs its own unreachable arm. The drafted unit test is unwritable — the provider builds its own HttpClient and loads a real certificate — and the job-level test passes with the fix reverted)
  - v12: owner ruled wont-fix on 2026-08-22 — reaching it needs revoked or expired ANAF credentials, and he accepts that risk; the consequence if it happens is that a credential outage pages nobody and shows only generic row failures

### PPW-582 — Confirmation page races the payment webhook and redirects the paying customer home

- **What:** Stripe confirmCardPayment resolves 'succeeded'; payment-step.ts:222-224 resets checkout state, clears the cart, navigates to /comanda/{id}/confirmare. Only WebhooksController sets Paid, so the immediate GET /orders/{id} usually returns AwaitingPayment -> router.navigate(['/']). Customer sees the homepage: no order number, no receipt, cart already emptied. confirmation-page.spec.ts never tests AwaitingPayment.
- **Evidence:** `src/app/features/orders/pages/confirmation-page.ts:208`
- **Suggested fix:** Poll GET /orders/{id} with backoff (e.g. 10x every 2s) while status is AwaitingPayment, showing a 'confirming payment' state; only redirect after the budget expires. **Files:** `src/app/features/orders/pages/confirmation-page.ts:208`, `src/app/features/checkout/pages/payment-step.ts:221`, `src/app/core/services/payment.service.ts:29`, `Controllers/OrdersController.cs:54`, `Controllers/WebhooksController.cs:219`, `Services/OrderService.cs:154`. **Path:** payment-step.ts:222-224 resets checkout, clears cart, navigates on client-side 'succeeded'. Paid is set only by WebhooksController:219/315 (plus admin/seed); GET /orders/{id} (OrdersController:54-63) is a plain DB read with no Stripe sync. So while the webhook is in flight the order is still AwaitingPayment, and confirmation-page.ts:208-211 falls through to router.navigate(['/']) — paid customer lands on the homepage, cart already emptied, no order number. **Test shape:** confirmation-page.spec.ts: "keeps the customer on the page while payment is still settling" — arrange getOrder returning status 'AwaitingPayment'; act ngOnInit; assert router.navigate not called with ['/'] (page waits/retries instead). **Trigger-list-shaped:** yes (adds a polling retry and a page state machine) — approach pre-check run.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict confirmed
  - v12: Approach pre-check: revised (polling fixes only the signed-in half: the order read is JWT-only, so a guest customer gets 401 and ten polls delete the guest token ten times. Close the server hole first — dual auth on a status read that checks the guest session, or a processor reconcile inside the read. Retry only a 200 that still says AwaitingPayment; abort at once on 401, 403, 404, 429 and PaymentFailed; on budget expiry show the order number, never the homepage. Gate the wait on an explicit payment-submitted marker set before the checkout reset, or a stale or foreign URL waits too. Zoneless: an rxjs timer writing signals under `takeUntilDestroyed`, and fake timers in the spec, because `fakeAsync` does not exist in this app)
  - v12 fix round: fixed @`901f8a2` — a new guest-readable payment-status endpoint, which the confirmation page reads up to ten times over half a minute while the webhook lands, instead of reading the signed-in-only order and sending a paying customer home. Its pre-check was right that the SPA alone could not fix it

### PPW-583 — Switching payment tabs destroys the Stripe card element but leaves the pay button enabled

- **What:** mountCardElement() mounts into #stripe-card-element, which lives inside *ngIf="activeTab()==='stripe'". User clicks the EuPlatesc tab (NgIf removes the div and the Stripe iframe with it), then clicks back. A new empty div renders, mountCardElement is never called again, yet stripeReady() is still true so 'Plătește acum' is enabled with no card field. Payment is unusable until a full page reload.
- **Evidence:** `src/app/features/checkout/pages/payment-step.ts:196`
- **Suggested fix:** Keep the Stripe panel in the DOM (hide with CSS/[hidden]) or re-mount the card element on switchTab('stripe'); reset stripeReady when unmounting. **Files:** `src/app/features/checkout/pages/payment-step.ts:46`, `src/app/features/checkout/pages/payment-step.ts:51`, `src/app/features/checkout/pages/payment-step.ts:54`, `src/app/features/checkout/pages/payment-step.ts:185`, `src/app/features/checkout/pages/payment-step.ts:196`, `src/app/features/checkout/pages/payment-step.ts:204`. **Path:** initStripe -> intent arrives -> mountCardElement mounts into #stripe-card-element and sets stripeReady=true. Click EuPlatesc: *ngIf (line 46) destroys the div and Stripe's iframe. Click Stripe: a fresh empty div renders; switchTab (204) only sets the signal — nothing remounts, no effect/afterRender exists. stripeReady stays true, so 'Plătește acum' is enabled with no card field. Worse: payWithStripe passes its guard (cardElement non-null) and awaits confirmCardPayment un-try/caught, so the rejection leaves stripeLoading stuck true. **Test shape:** payment-step.spec.ts: 'remounts the card element when returning to the Stripe tab' — inject fake stripeInstance/clientSecret, mount, switchTab('euplatesc')+detectChanges, switchTab('stripe')+detectChanges; expect card.mount called twice (currently once). **Trigger-list-shaped:** yes (a checkout state machine and an element lifecycle) — approach pre-check run.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict confirmed
  - v12: Approach pre-check: revised (keep the panel in the DOM, but `[hidden]` alone is a no-op because the panel class sets `display: flex`, and a remount without a destroy leaks an orphaned Stripe iframe per toggle. Mount through a view-child element reference with an idempotent guard. The same commit must wrap the confirm call in try/catch/finally: the stuck spinner is reachable without switching tabs, and an unhandled rejection breaks the new spec's stability wait. The drafted spec cannot run — there is no seam for a fake Stripe, so the module needs mocking; today the card element is mounted zero times in specs, and two existing specs pass for the wrong reason)
  - v13: fixed @`06fd2b1` — retired by deletion, not repaired: PR #13 removed the second payment processor, so the checkout has no tab switcher and no destroyed-element path. Nothing verifies it, because the surface it described no longer exists

### PPW-584 — SPA never sends an Idempotency-Key and PaymentStep mints a fresh order on every mount

- **What:** IdempotencyKeyFilter treats a missing header as the transitional case, so OrderService skips dedup entirely. payment-step ngOnInit posts createStripeIntent unconditionally. Open /checkout/plata in two tabs, or click Înapoi then Plătește acum: two Orders + two PaymentIntents for one cart, two order numbers burned; pay in both and you get a double charge and two invoice numbers/ANAF submissions for one purchase.
- **Evidence:** `src/app/core/services/payment.service.ts:18`
- **Suggested fix:** Mint a UUID per checkout attempt in CheckoutStateService, send it as the Idempotency-Key header on both payment endpoints, and reuse the existing order when PaymentStep remounts. **Files:** `src/app/core/services/payment.service.ts:18`, `src/app/features/checkout/pages/payment-step.ts:161`, `src/app/features/checkout/pages/payment-step.ts:181`, `Filters/IdempotencyKeyFilter.cs:37`, `Services/OrderService.cs:105`, `Services/OrderService.cs:165`. **Path:** No UI file mentions Idempotency-Key (no interceptor), so PaymentService posts none. IdempotencyKeyFilter stores null; OrderService line 105 skips dedup entirely and there is no reuse of an existing AwaitingPayment order. PaymentStep.ngOnInit calls initStripe → createStripeIntent on every mount. Two tabs on /checkout/plata, or Înapoi then re-enter: two orders, two order numbers, two PaymentIntents; confirming both double-charges. **Test shape:** API: CreateOrderAsync twice with same cart and idempotencyKey=null asserts two Orders/order numbers — reddens once a key is required or an open AwaitingPayment order is reused. UI: PaymentStep mounted twice asserts one createStripeIntent call. **Trigger-list-shaped:** yes (introduces an idempotency key scheme across two endpoints) — approach pre-check run.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict confirmed
  - v12: Approach pre-check: revised (the server dedupe already exists and is tested; the storage choice is the defect — the checkout state service persists to sessionStorage, so two tabs still mint two orders. Put the key in its own localStorage entry, one per attempt and processor, cleared on success, on login, on logout, on a guest-token clear and on any 409; nesting it inside the guest session defeats the guest-token clear rule. Requiring the header without a SQLite arm in the violation classifier turns every dev-mode collision into a 500, and replay has no status check, so a stale key replays a paid order's secret. Checkout handles no 409 anywhere, so the key alone converts a silent double order into a 24-hour dead end)
  - v12 fix round: fixed @`901f8a2` — the checkout mints one key per basket in localStorage, reuses it across mounts, and clears it when the order settles; the server answers a settled key with a 409 naming that order and the page sends the customer to it. The pre-check ruled out sessionStorage, which a second tab does not share

### PPW-585 — Recapitulare hides the new fiscal address for locker orders, and an unchanged spec pins that behaviour

- **What:** Easybox buyer types a fiscal address at delivery (now required), reaches /checkout/recapitulare. review-step.ts:43 gates the address line on method==='Courier', so the address never appears on the final confirm screen. A mistyped county is paid for and printed on the legal invoice. review-step.spec.ts:126 asserts the address must stay hidden.
- **Evidence:** `src/app/features/checkout/pages/review-step.spec.ts:126`
- **Suggested fix:** Render the address on recapitulare for both methods (labelled billing for Easybox) and retarget the spec; refresh its Easybox fixtures that still use shippingAddress: null. **Files:** `src/app/features/checkout/pages/review-step.spec.ts:126`, `src/app/features/checkout/pages/review-step.ts:43`, `src/app/features/checkout/pages/delivery-step.ts:357`, `src/app/features/checkout/pages/delivery-step.ts:483`, `src/app/core/services/checkout-state.service.ts:39`, `src/app/features/checkout/pages/payment-step.ts:251`. **Path:** Pick Easybox, locker l1, fill address (Timiș/Timișoara). canContinue (delivery-step.ts:357) requires the address; continue() calls setEasyboxAddress, which keeps it. payment-step.ts:251 posts shippingAddress for Easybox too, so it lands on the invoice. But review-step.ts:43 gates the line on method==='Courier', so recapitulare shows only "Easybox — Box A". The spec's own premise is false: setMethod (line 26) clears shippingAddress, so no "leftover courier address" exists — line 146/147 pin real hiding. **Test shape:** ReviewStep spec "renders the fiscal address for an Easybox order": arrange Easybox state with shippingAddress (Timișoara); act createFixture; assert .delivery-summary textContent contains 'Str. Fantoma' and 'Timișoara' — reddens today. **Trigger-list-shaped:** no (renders a value already in state and retargets a spec) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — frontend-ux, completeness-critic, convergence 2, verdict confirmed
  - v12: owner regraded 🔴 to 🟠 on 2026-08-22 — the driver checked the code with him and the consequence is that a locker customer cannot proof-read the address printed on the invoice, not money or data loss
  - v13: fixed @`c03f99a` — the recap renders the invoiced address for locker orders too, and the spec that pinned the old behaviour was retargeted
  - v13: verified — revert-and-rerun held, and the row's files were touched by no later commit in the round

### PPW-586 — Neither invoice controller has an HTTP-pipeline test, so endpoint authorization and DualAuth guest ownership are unverified

- **What:** All 11 InvoicesControllerTests hand-build a ControllerContext with a synthetic ClaimsPrincipal, so [Authorize(Policy=DualAuthPolicy)] and the guest-token pipeline never run. A guest token that fails to project GuestSessionId would 403 or leak another order's invoice, and nothing reddens. Same for /api/admin/invoices (3 tests, all logging-only).
- **Evidence:** `Tests/Unit/Controllers/InvoicesControllerTests.cs:52`
- **Suggested fix:** Add WebApplicationFactory tests hitting GET /api/orders/{id}/invoice as guest, wrong guest, owner, and anonymous, and the admin list/retry/xml routes with and without the Admin role. **Files:** `Tests/Unit/Controllers/InvoicesControllerTests.cs:49`, `Controllers/InvoicesController.cs:56`, `Extensions/ClaimsPrincipalExtensions.cs:12`, `Extensions/GuestSessionExtensions.cs:22`, `Authentication/GuestAuthenticationHandler.cs:49`, `src/app/core/interceptors/guest.interceptor.ts:16`. **Path:** Real gap, though not the claimed leak. DualAuth lists both schemes, so ASP.NET authenticates each and merges identities. Send a valid Bearer JWT plus a still-valid X-Guest-Token to GET /api/orders/{id}/invoice for the caller's own order: the merged principal carries guest_session_id, so GetUserIdOrNull returns null (ClaimsPrincipalExtensions.cs:12), order.GuestSessionId is null, owns=false -> 403 on your own invoice. Unit tests build one synthetic identity, so nothing reddens. Shipped SPA suppresses the guest header when logged in, limiting exposure to non-SPA callers. **Test shape:** InvoiceEndpointIntegrationTests.GetInvoice_JwtPlusValidGuestToken_ReturnsPdf: arrange user order + invoice + valid GuestSession; act GET /api/orders/{id}/invoice with Bearer and X-Guest-Token headers; assert 200, not 403. **Trigger-list-shaped:** yes (changes how a merged guest-plus-user identity resolves ownership) — approach pre-check run.
- **History:**
  - v12: found by the certification pass — tests-coverage, completeness-critic, convergence 2, verdict confirmed
  - v12: Approach pre-check: revised (the 403 is real and deterministic, but the fix belongs in the shared claims extension, whose claim scan makes every both-headers caller lose its user id — cart scope, and worse, order attribution, where a signed-in user carrying a stale guest token creates a guest order today. Make the read identity-scoped rather than deleting the guard, or guest-only requests start writing a session id into the user column. The existing factory chain boots the real pipeline, but the 200 leg needs a seed helper for a stored PDF, and the admin retry route can only be tested for 401 and 403 on the in-memory provider because its update runs as raw SQL. Any test-authentication override deletes the defect)
  - v12: owner regraded 🔴 to 🟠 on 2026-08-22 — the 403 needs a caller sending a valid JWT and a live guest token together, which the shipped SPA never does, so it is unreachable through the site itself
  - v13: fixed @`8950624` — six tests through the real pipeline: 401 anonymous, 403 for another customer, 403 for another guest session, 404 for the guest owner and for the order owner (with the Retry-After), and the admin role override. Reverting the admin branch reddens the last one
  - v13: verified by reading — the commit changes only test files, so nothing can be reverted to redden it. Its own admin case was proved by reverting the production branch it covers (see PPW-596)

### PPW-587 — A permanent HTTP 4xx content rejection is classified as unreachable/transient, so the row retries forever and is never parked

- **What:** ANAF returns 400 for content it will always reject. AnafSpvClient maps every non-2xx to AnafUnreachableException, so the job only writes LastError and stays Pending — no counter, no parking, no Failed metric. The invoice retries every cooldown indefinitely, and once more than MaxBatchSize such rows exist they head every batch and starve healthy invoices.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:355`
- **Suggested fix:** Split 4xx (except 408/429) into its own exception and park the row as Failed via ParkUnbuildableAsync, mirroring the not-buildable path. **Files:** `Services/Invoicing/Anaf/AnafSpvClient.cs:70`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:355`, `Services/Invoicing/InvoiceLifecycle.cs:44`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:98`, `Services/Invoicing/InvoiceLifecycle.cs:111`, `Services/Invoicing/Anaf/AnafExceptions.cs:44`. **Path:** Pending invoice, XML/PDF already built. ANAF answers 400 (bad CIF, duplicate number): AnafSpvClient.cs:70-71 throws AnafUnreachableException(httpStatus:400). Job's catch at line 355 calls RecordPendingErrorAsync — InvoiceLifecycle.cs:44-55 writes only LastError+UpdatedAt, keeps Pending, no UnknownUploadOutcomes bump, no Failed metric — then releases the claim. Next tick past the cooldown re-selects the row (line 98). No watchdog parks Pending elsewhere, so it loops forever. **Test shape:** InvoiceUploadJobTests.Upload_rejected_with_http_400_is_parked_after_budget: arrange fake IAnafSpvClient always throwing AnafUnreachableException(endpoint, httpStatus:400); act — run MaxUnknownUploadOutcomes+1 ticks; assert invoice.AnafStatus == Failed. Today it stays Pending forever. **Trigger-list-shaped:** yes (splits the exception mapping and adds a parking rule) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — correctness, convergence 1, verdict confirmed
  - v13: fixed @`32d4eee` — a non-success HTTP status that is not 408/429/5xx now raises `AnafContentRejectedException`, and the job parks the row as Failed instead of retrying a document ANAF has refused
  - v13: verified — the mechanical red leg is not trustworthy here — this row's commit touches a file a later commit in the round touched again, so a file-level revert can break compilation and read as red. Re-proved by hand in isolation: disabling only the `AnafContentRejectedException` catch reddened the parking test, restored green

### PPW-588 — Unknown-outcome budget covers only client timeouts, so AnafUnreachableException gets unlimited blind re-POSTs

- **What:** ANAF's gateway registers the submission then answers 502. AnafResilienceHandler re-POSTs the same non-idempotent upload 3x, then AnafSpvClient maps it to AnafUnreachableException, which only records a pending error, so every tick re-uploads forever. UnknownUploadOutcomes never increments. Result: many duplicate e-Factura filings under one invoice number.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:355`
- **Suggested fix:** Treat 5xx/unreachable on the upload endpoint as an unknown outcome too (count it against MaxUnknownUploadOutcomes), and exclude POST upload from the Polly retry set. **Files:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:355`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:364`, `Services/Invoicing/Anaf/AnafSpvClient.cs:68`, `Services/Invoicing/Anaf/AnafSpvClient.cs:181`, `Services/Invoicing/Anaf/AnafResilienceHandler.cs:36`, `Services/Invoicing/InvoiceLifecycle.cs:111`. **Path:** Upload POSTs (non-idempotent, no idempotency key). ANAF answers 502 after registering it: Polly retries 3x (AnafResilienceHandler:36), then AnafSpvClient:68 throws AnafUnreachableException. InvoiceUploadJob:355-363 only calls RecordPendingErrorAsync and releases the claim — UnknownUploadOutcomes untouched, status stays Pending. No other code parks Pending rows, so every tick re-POSTs forever. Sharper variant: a 200 body that fails XML parse (AnafSpvClient:181) — upload definitely accepted — takes the same unlimited path. **Test shape:** UploadJob_Reposts_Forever_On_Gateway_5xx: fake IAnafSpvClient.UploadAsync always throws AnafUnreachableException(httpStatus:502); run MaxUnknownUploadOutcomes+1 ticks; assert invoice parked Failed and UnknownUploadOutcomes>0 — today it stays Pending with 0 and uploads every tick. **Trigger-list-shaped:** yes (changes retry semantics and the blind-repost budget) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — race, convergence 1, verdict confirmed. Fix-generated by PPW-559
  - v13: fixed @`32d4eee` — an upload-leg outage spends the blind-repost budget through `RecordUnknownUploadOutcomeAsync` and **keeps the claim**: a row whose ANAF answer nobody has cannot be re-posted by a second replica
  - v13: verified — the mechanical red leg is not trustworthy here — this row's commit touches a file a later commit in the round touched again, so a file-level revert can break compilation and read as red. Re-proved by hand in isolation: releasing the claim on the unreachable branch reddened the keeps-the-claim test, restored green

### PPW-589 — nextval commits outside the insert transaction, so a lost duplicate-delivery race permanently burns a fiscal invoice number

- **What:** Stripe delivers payment_intent.succeeded twice concurrently. Both requests pass the OrderId existence check and each calls nextval (41, 42) in its own autocommit statement. One commits 41; the loser hits ix_invoices_order_id, returns AlreadyInvoiced, and 42 is gone. The FT-2026 series now has a gap nothing reclaims or explains to ANAF.
- **Evidence:** `Services/Invoicing/PostgresInvoiceNumberingService.cs:40`
- **Suggested fix:** Allocate the number inside an explicit transaction that also inserts the row, or reconcile gaps: record burned numbers and reuse them on the next allocation. **Files:** `Services/Invoicing/PostgresInvoiceNumberingService.cs:40`, `Services/Invoicing/InvoiceCreationService.cs:77`, `Controllers/WebhooksController.cs:304`, `Controllers/WebhooksController.cs:449`, `Controllers/WebhooksController.cs:460`, `memory-bank/bolts/038-vat-calculation/adr-020-postgres-sequence-for-invoice-numbering-accept-gap-on-rollback.md:39`. **Path:** Mechanically real. Two Stripe deliveries both pass the status check (WebhooksController.cs:304), each allocates 41/42 via nextval (non-transactional, so unrollbackable), one SaveChanges wins, the loser detaches and returns AlreadyInvoiced (line 466) — 42 burnt. But this is exactly ADR-020's explicitly accepted gap-on-rollback, mitigated by a mandated quarterly gap audit plus accountant note; the ADR forbids reopening it. So: real gap, not a defect, and "nothing explains it to ANAF" is false. **Test shape:** None should be written: a test asserting a gap-free series contradicts ADR-020. Demonstrating the burn needs real Postgres (nextval absent from the EF InMemory default) and would assert the accepted behaviour, not a regression. **Trigger-list-shaped:** yes (changes the fiscal-number allocation scheme) — the row needs an owner ruling against ADR-020 before any fix — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — race, convergence 1, verdict confirmed
  - v12: owner regraded 🟠 to 🟡 on 2026-08-24 and moved it to backlog — the driver verified it is ADR-020 accepted trade-off, already audited, not an open defect

### PPW-590 — PollSubmittedAsync takes no claim, so every replica polls every Submitted row on every tick

- **What:** Two API replicas run the job. Both batches contain the same Submitted rows; the file-level comment says ClaimedAt+TTL is the multi-replica guard, but only UploadPendingAsync claims. With 50 submitted rows each replica issues 50 stareMesaj calls per tick; ANAF answers 429, Polly retries, and the burst compounds instead of resolving.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:421`
- **Suggested fix:** Apply the same ClaimedAt+TTL compare-and-swap before polling a Submitted row, and skip the row when the claim is lost. **Files:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:98`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:421`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:157`, `Services/Invoicing/InvoiceLifecycle.cs:38`, `Services/Invoicing/Anaf/AnafResilienceHandler.cs:33`, `Configuration/AnafSettings.cs:31`. **Path:** 50 Submitted rows; MarkSubmittedAsync nulls LastError, so the batch filter (line 98-100, no ClaimedAt predicate) selects all 50 on both replicas. PollSubmittedAsync only reads the row, then calls GetStatusAsync — no claim, no CAS. 100 stareMesaj calls/tick; each 429 is retried 3x (1/2/4s) by AnafResilienceHandler. The resulting AnafUnreachableException hits the generic catch (line 157), which only logs — LastError stays null, so no cooldown; every tick repeats. **Test shape:** InvoiceUploadJobTests: ProcessBatchAsync_SubmittedRowClaimedByPeerWithinTtl_SkipsPollWithoutCallingAnaf — seed Submitted row (anafUploadId, ClaimedAt = now-2min), run ProcessBatchAsync, assert AnafClient.Verify(GetStatusAsync, Times.Never). Mirrors the existing UploadPendingAsync_RowAlreadyClaimedWithinTtl test; reddens today. **Trigger-list-shaped:** yes (extends the claim-and-lease concurrency model to the poll leg) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — race, convergence 1, verdict confirmed. Fix-generated by PPW-476
  - v12: owner regraded 🟠 to 🟡 on 2026-08-24 and moved it to backlog — dormant while production runs one API instance; it wakes if replicas are configured

### PPW-591 — No setval reconciliation: an invoice sequence that lags the Invoices table wedges every paid order

- **What:** Prod Invoices rows are restored (pg_dump --data-only, or copied to staging) without their sequences. invoice_seq_ft_2026 is at 1 while rows 1..500 exist. Every paid order draws 1,2,3,4 — all rejected by uq_invoices_series_year_number — exhausts the 4 retries in WebhooksController:468, returns NumberExhausted. Customer charged, order not Paid, forever.
- **Evidence:** `Services/Invoicing/PostgresInvoiceNumberingService.cs:40`
- **Suggested fix:** On numbering exhaustion (or at boot) run setval(seq, max(Number)) for the series/year from Invoices; add a Postgres test seeding rows ahead of the sequence. **Files:** `Services/Invoicing/PostgresInvoiceNumberingService.cs:38`, `Data/PostgresSequences.cs:25`, `Migrations/20260820133204_InitialPostgres.cs:746`, `Services/Invoicing/InvoiceCreationService.cs:77`, `Controllers/WebhooksController.cs:468`, `Services/AdminOrderService.cs:449`. **Path:** State: invoices hold FT/2026 numbers 1..500; sequence invoice_seq_ft_2026 sits at 1 (migration creates it START 1; EnsureAsync only CREATE IF NOT EXISTS; no setval or MAX(number) reconciliation exists anywhere). Paid webhook: nextval yields 1,2,3,4 across attempts 0-3, each SaveChanges hits uq_invoices_series_year_number, attempt 3 returns NumberExhausted — charged, order not Paid. Self-heals only after ~125 such burned orders, not "forever". **Test shape:** Postgres integration test NumberingLaggingSequenceWedgesPaidTransition: seed invoices FT/2026 numbers 1..5, setval sequence to 1, run the paid save path, assert NumberExhausted and order not Paid. **Trigger-list-shaped:** yes (adds a self-heal step to the numbering key scheme) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — db-parity, convergence 1, verdict confirmed
  - v13: fixed @`72202c0` — a taken-number collision now runs `setval` past `MAX("Number")` for the series and the UTC year of `IssuedAt`, mirroring the unique index; three Postgres tests cover lagging, ahead-of-table and a foreign year
  - v13: verified — the mechanical red leg is not trustworthy here — this row's commit touches a file a later commit in the round touched again, so a file-level revert can break compilation and read as red. Re-proved by hand in isolation: returning early from `ReconcileWithStoredInvoicesAsync` reddened the lagging-sequence Postgres test, restored green

### PPW-592 — ANAF-supplied index_incarcare is accepted unvalidated into a varchar(100) column, turning a filed invoice into a blind re-upload loop

- **What:** ANAF (or a proxy/error page that still parses as XML) returns index_incarcare longer than 100 chars. MarkSubmittedAsync's ExecuteUpdate throws 22001 on Postgres; InvoiceUploadJob:341 rethrows to the batch catch (line 157) which only logs. Row stays Pending, AnafUploadId null, UnknownUploadOutcomes not incremented, claim not released — next tick re-files the same invoice number. Unbounded duplicates.
- **Evidence:** `Services/Invoicing/Anaf/AnafSpvClient.cs:91`
- **Suggested fix:** Reject or truncate index_incarcare above the column width in AnafSpvClient, and treat a MarkSubmittedAsync failure as an unknown-outcome (increment the blind-repost budget) rather than a plain rethrow. **Files:** `Services/Invoicing/Anaf/AnafSpvClient.cs:91`, `Services/Invoicing/InvoiceLifecycle.cs:37`, `Data/PhotoPrintDbContext.cs:399`, `Migrations/20260820133204_InitialPostgres.cs:380`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:341`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:100`. **Path:** Line 91 only null/whitespace-checks index_incarcare. MarkSubmittedAsync writes it to character varying(100) (InitialPostgres.cs:380) via ExecuteUpdate — EF skips validation, Postgres raises 22001. Job:341 rethrows; PostgresException matches none of the catches at 347/355/364, so batch catch:157 only logs. Row stays Pending, LastError null, so filter:100 keeps it batch-eligible; claim never released, so re-upload every ClaimTtl (min 2 min) forever. Prod-only — SQLite/InMemory ignore length. **Test shape:** InvoiceUploadJobTests: MarkSubmittedAsync mock throws DbUpdateException after a successful upload. Assert invoice stays Pending with LastError null, ClaimedAt still set, UnknownUploadOutcomes 0 — proving the next tick re-files. Plus AnafSpvClient rejects a 101-char index_incarcare. **Trigger-list-shaped:** yes (adds input rejection plus an unknown-outcome mapping) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — db-parity, input-validation, convergence 2, verdict confirmed
  - v13: fixed @`add7611` — the client rejects an `index_incarcare` wider than `Invoice.AnafUploadIdMaxLength`, and a failed status write after a successful upload counts as an unknown outcome instead of rethrowing into a re-file loop
  - v13: verified — revert-and-rerun held; the only file this row shares with a later commit is `docs/DEPLOYMENT.md`, which is neither compiled nor a test input

### PPW-593 — Admin retry's Rejected/Failed status whitelist has no test; only the 409-free happy path is covered

- **What:** Delete the status check in RetryAsync and all three tests still pass. An admin clicking retry on an Accepted invoice flips it to Pending (InvoiceLifecycle.RetryAsync accepts any expected status), and the worker re-files the same invoice number to ANAF: duplicate e-Factura.
- **Evidence:** `Tests/Unit/Controllers/AdminInvoicesControllerTests.cs:75`
- **Suggested fix:** Add controller tests: Accepted/Submitted/Pending each return 409 with error invoice-not-retryable, and lifecycle.RetryAsync is never called. Add one for the CAS-lost 409. **Files:** `Tests/Unit/Controllers/AdminInvoicesControllerTests.cs:75`, `Controllers/AdminInvoicesController.cs:105`, `Services/Invoicing/InvoiceLifecycle.cs:179`, `Services/Invoicing/IInvoiceLifecycle.cs:54`, `Tests/Unit/Services/Invoicing/Anaf/InvoiceUploadJobTests.cs:662`. **Path:** Delete AdminInvoicesController.cs:105-113 and all three tests still pass — the only retry test seeds AnafStatus=Rejected and asserts a log line. No other test (unit or integration) hits the endpoint or the "invoice-not-retryable" branch. Without the check, an Accepted invoice passes expected=Accepted to InvoiceLifecycle.RetryAsync:179, whose CAS matches, flipping it to Pending and clearing XmlPayload/AnafUploadId, so the worker re-files the same invoice number. **Test shape:** RetryAsync_Returns409_ForAcceptedInvoice: arrange invoice with AnafStatus=Accepted; act controller.RetryAsync; assert ConflictObjectResult carrying error "invoice-not-retryable" and lifecycle.RetryAsync never invoked (Moq Verify never). **Trigger-list-shaped:** no (controller tests only) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — tests-coverage, convergence 1, verdict confirmed
  - v12: owner regraded 🟠 to 🟡 on 2026-08-24 and moved it to backlog — a test gap only; the shipped whitelist behaves correctly today

### PPW-594 — The new Invoice.StorageLocation stamp is never asserted after a PDF save

- **What:** Remove `invoice.StorageLocation = storageRouter.CloudEnabled ? Cloud : Local` and both tier tests pass — they only Verify which adapter got SaveAsync. Every row then stays Local(0) while bytes sit in S3, so every download takes a guaranteed local miss plus a tier-mismatch warning, and a genuinely lost blob is indistinguishable in the logs.
- **Evidence:** `Tests/Unit/Services/Invoicing/Anaf/InvoiceUploadJobTests.cs:304`
- **Suggested fix:** Re-read the row in both tier tests and assert StorageLocation equals Cloud/Local respectively, alongside PdfStoragePath. **Files:** `Tests/Unit/Services/Invoicing/Anaf/InvoiceUploadJobTests.cs:304`, `Tests/Unit/Services/Invoicing/Anaf/InvoiceUploadJobTests.cs:320`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:216`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:287`, `Models/Invoice.cs:47`, `Controllers/InvoicesController.cs:76`. **Path:** Delete InvoiceUploadJob.cs:287 (`invoice.StorageLocation = CloudEnabled ? Cloud : Local`). Line 216 still picks the Cloud adapter, so both tier tests (lines 315-316, 331) stay green — they only Verify SaveAsync on the mocks and never reload the row. The persisted row keeps the entity default Local (Invoice.cs:47) while bytes are in S3. InvoicesController.cs:76 then reads Local first: guaranteed miss, then a tier-mismatch warning at 109. No test in the suite asserts the job's stamp. **Test shape:** UploadPendingAsync_CloudEnabled_StampsTheRowWithTheCloudTier: arrange Build(cloudEnabled: true) + SeedOrderAndInvoice; act InvokeUploadPendingAsync; assert reloaded row.StorageLocation == StorageLocation.Cloud (and a cloudEnabled:false twin asserting Local). **Trigger-list-shaped:** no (assertions added to two existing tests) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — tests-coverage, convergence 1, hinted, verdict confirmed. Fix-generated by PPW-517
  - v12: owner regraded 🟠 to 🟡 on 2026-08-24 and moved it to backlog — a test gap only; the fallback read self-heals even the hypothetical regression

### PPW-595 — QuestPDF licence is set by the test class itself, so the production licence wiring is unverified

- **What:** Delete `QuestPDF.Settings.License = ...` from Program.cs:262 and nothing reddens: this static ctor sets it for the renderer tests and InvoiceUploadJobTests mocks IInvoicePdfRenderer. In production GeneratePdf throws, the job's generic catch records a pending error, and every invoice loops Pending with a permanent 404 download.
- **Evidence:** `Tests/Unit/Services/Invoicing/InvoicePdfRendererTests.cs:23`
- **Suggested fix:** Assert QuestPDF.Settings.License is configured after the host boots (WebApplicationFactory), and stop setting it in the test's static constructor. **Files:** `Tests/Unit/Services/Invoicing/InvoicePdfRendererTests.cs:23`, `Program.cs:262`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:281`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:315`, `Tests/Unit/Services/Invoicing/Anaf/InvoiceUploadJobTests.cs:109`, `Controllers/InvoicesController.cs:123`. **Path:** Empirically verified: QuestPDF 2024.12.3 GeneratePdf throws System.Exception ("Welcome to QuestPDF!") when Settings.License is unset. Only two files touch the licence: Program.cs:262 and this static ctor. Delete Program.cs:262 → renderer tests still pass (own static ctor), InvoiceUploadJobTests:109 mocks IInvoicePdfRenderer, no integration test renders through boot. Production: Render at InvoiceUploadJob.cs:281 throws, generic catch at :315 calls RecordPendingErrorAsync, invoice loops Pending, PDF never stored. **Test shape:** BootSetsQuestPdfLicence: arrange — set QuestPDF.Settings.License = null; act — construct WebApplicationFactory&lt;Program&gt; and force boot (CreateClient); assert — Settings.License == LicenseType.Community. Reddens when Program.cs:262 is deleted, immune to the renderer tests' static ctor ordering. **Trigger-list-shaped:** no (a boot test plus removing a test-only licence set) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — tests-coverage, convergence 1, verdict confirmed
  - v12: owner regraded 🟠 to 🟡 on 2026-08-24 and moved it to backlog — a test gap only; the production licence wiring is untested, not wrong

### PPW-596 — No admin access to an invoice PDF, so FR-5's role override and the inspection-week runbook are undelivered

- **What:** Admin follows DEPLOYMENT.md:1470 ("download a few via GET /api/orders/{id}/invoice") during the dual-write inspection week. Ownership check compares only UserId/GuestSessionId with no role bypass, and no admin PDF route exists, so every customer invoice answers 403. The one week designed to catch bad PDFs before customers see them cannot be executed.
- **Evidence:** `Controllers/InvoicesController.cs:58`
- **Suggested fix:** Allow Admin role to bypass the ownership check (or add GET /api/admin/invoices/{id}/pdf), with an audit log line; add a test for admin fetching another user's invoice. **Files:** `Controllers/InvoicesController.cs:56`, `Controllers/InvoicesController.cs:58`, `Controllers/AdminInvoicesController.cs:145`, `Extensions/GuestSessionExtensions.cs:22`, `memory-bank/intents/016-romanian-vat-efactura/requirements.md:68`, `docs/DEPLOYMENT.md:1470`. **Path:** Admin logs in (JWT with role Admin) and calls GET /api/orders/{customerOrderId}/invoice. DualAuth passes (RequireAuthenticatedUser only), then InvoicesController.cs:56-58 computes owns from UserId/GuestSessionId alone — no IsInRole("Admin") anywhere — so order.UserId != admin id yields Forbid() → 403. AdminInvoicesController offers only list, retry, and /xml; grep for "application/pdf" hits one route. So no admin path to any customer PDF, contradicting FR-5 requirements.md:68 and the DEPLOYMENT.md:1470 spot-check step. **Test shape:** GetInvoice_AdminOnAnotherUsersOrder_ReturnsPdf: arrange paid order owned by user A with rendered invoice PDF; act GET /api/orders/{A.orderId}/invoice with Admin-role JWT; assert 200 and application/pdf. Today returns 403. **Trigger-list-shaped:** no (an authorization branch and a route) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — requirements, convergence 1, verdict confirmed
  - v13: fixed @`e3f4bb8` — an Admin may read a customer invoice and the read is logged as `invoice.pdf.admin-read`
  - v13: verified — the mechanical red leg is not trustworthy here — this row's commit touches a file a later commit in the round touched again, so a file-level revert can break compilation and read as red. Re-proved by hand in isolation: dropping `!isAdmin` from the ownership guard reddened `GetInvoice_AdminOnACustomersOrder_IsNotForbidden`, restored green

### PPW-597 — Invoice-by-email (FR-5, story 003) is not implemented while ddd-02 describes it as shipped

- **What:** A guest pays; the invoice PDF is rendered but the confirmation email neither attaches it nor mentions it, and the notifier only logs. Guests have no order-list API (OrdersController is JWT-only), so once the checkout page is closed the legally required invoice is unreachable. ddd-02:184-195 still claims an IEmailService façade sends it.
- **Evidence:** `Services/Invoicing/InvoicePdfReadyNotifier.cs:31`
- **Suggested fix:** Either implement the attachment/follow-up send, or mark story 003/FR-5's email criterion explicitly descoped in ddd-02 and the story file, and give guests a retrieval path. **Files:** `Services/Invoicing/InvoicePdfReadyNotifier.cs:31`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:293`, `Services/IEmailSender.cs:5`, `EmailTemplates/OrderConfirmed.cshtml:1`, `Controllers/InvoicesController.cs:17`, `Controllers/OrdersController.cs:10`. **Path:** with one correction. Guest pays; InvoiceUploadJob:293 renders the PDF then calls NotifyAsync, which only logs — flag off logs "suppressed", flag on logs "no-email-integration". No IEmailService is even injected, IEmailSender.SendAsync has no attachment parameter, and OrderConfirmed.cshtml never mentions factura/invoice. But the invoice is not unreachable by API: InvoicesController:17 uses DualAuthPolicy and matches GuestSessionId. It is unreachable in practice — no UI code calls it and OrdersController:10 is JWT-only. **Test shape:** InvoicePdfReadyNotifierTests: flag enabled, NotifyAsync(invoice, order) → assert IEmailService received a send naming the invoice number for order.CustomerEmail. Red today (no IEmailService dependency, log-only body). **Trigger-list-shaped:** yes (adds an outbound email event, or removes a promised capability) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — requirements, convergence 1, verdict confirmed
  - v13: fixed @`5324a1c`, `beb7732` — the contradiction is resolved on the doc side plus a real route: ddd-02 now says the email is unshipped, and the confirmation page has a download button reading `GET /api/orders/{id}/invoice`. Story 003 (invoice by email) stays open as scope, not as a false claim
  - v13: verified — the mechanical red leg is not trustworthy here — this row's commit touches a file a later commit in the round touched again, so a file-level revert can break compilation and read as red. Re-proved by hand in isolation: renaming the download button's class reddened one `confirmation-page` spec. The doc half (ddd-02) is verified by reading, not by a test

### PPW-598 — Admin retry never re-renders the PDF, contradicting the documented fix-forward-and-re-render rollback

- **What:** A PDF-renderer bug ships; ops fix it and follow DEPLOYMENT.md:1482 ("fix forward and re-render via POST /api/admin/invoices/{id}/retry"). RetryAsync deliberately keeps PdfStoragePath, and UploadPendingAsync only renders when that field is empty, so the broken PDF is never regenerated and customers keep downloading it.
- **Evidence:** `Services/Invoicing/InvoiceLifecycle.cs:165`
- **Suggested fix:** Clear PdfStoragePath (and StorageLocation) on retry so the worker re-renders, or correct DEPLOYMENT §15.7 to state that retry cannot re-render and give the real procedure. **Files:** `Services/Invoicing/InvoiceLifecycle.cs:165`, `Services/Invoicing/InvoiceLifecycle.cs:178`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:247`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:279`, `Controllers/AdminInvoicesController.cs:105`, `docs/DEPLOYMENT.md:1482`. **Path:** Invoice Rejected, PdfStoragePath="invoices/FT-1.pdf" holding bytes from the buggy renderer. Ops deploy the fix, POST retry: RetryAsync (line 178) nulls XmlPayload but leaves PdfStoragePath, status Pending. Worker tick reaches UploadPendingAsync; line 279 `if (string.IsNullOrEmpty(invoice.PdfStoragePath))` is false, so Render is never called and no new bytes are stored. Customers keep downloading the stale PDF via InvoicesController line 92. Worse: an Accepted invoice 409s at AdminInvoicesController line 105, so the documented path fails outright. **Test shape:** InvoiceUploadJobTests.Retry_ReRendersPdf: arrange Failed invoice with PdfStoragePath set and a counting IInvoicePdfRenderer; act RetryAsync then one worker tick; assert Render called once and stored bytes replaced. Reddens today (zero calls). **Trigger-list-shaped:** yes (changes what the retry resets, so it changes retry semantics) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — requirements, convergence 1, verdict confirmed
  - v13: fixed @`add7611` — `RetryAsync` clears `PdfStoragePath`, so a fixed renderer actually re-renders; the test that pinned the kept path was retargeted
  - v13: verified — revert-and-rerun held, and the retargeted lifecycle test asserts the cleared path directly

### PPW-599 — Documented batch-retry SQL in DEPLOYMENT.md reposts the identical rejected XML and re-parks on the first timeout

- **What:** After fixing a Seller:Cui typo, ops run the §15.9 batch UPDATE on 200 Failed invoices. It clears only AnafStatus/AnafUploadId/LastError, so XmlPayload survives and the worker's "build if empty" guard skips rebuilding — ANAF gets the same rejected XML. UnknownUploadOutcomes also stays at its cap, so one timeout re-parks the row immediately.
- **Evidence:** `docs/DEPLOYMENT.md:1531`
- **Suggested fix:** Add "XmlPayload" = NULL, "UnknownUploadOutcomes" = 0, "ClaimedAt" = NULL to the documented SQL, or replace it with a loop over the retry endpoint. **Files:** `docs/DEPLOYMENT.md:1531`, `Services/Invoicing/InvoiceLifecycle.cs:163`, `Services/Invoicing/InvoiceLifecycle.cs:119`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:247`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:267`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:364`. **Path:** `RetryAsync` (InvoiceLifecycle.cs:180-188) also nulls `XmlPayload` and zeroes `UnknownUploadOutcomes`; the §15.9 SQL sets neither. So: Failed row keeps its old-CUI XML → `UploadPendingAsync`'s `if (string.IsNullOrEmpty(invoice.XmlPayload))` (InvoiceUploadJob.cs:267) skips the rebuild, reposting the rejected XML after the Seller:Cui fix. And a row parked at outcomes=3 stays 3, so one `AnafUploadTimeoutException` increments to 4 ≥ max (InvoiceLifecycle.cs:119-138) and re-parks on the first timeout. **Test shape:** InvoiceUploadJobTests.DocumentedBatchRetryFieldsAlone_RepostsStaleXmlAndParksOnFirstTimeout: seed Failed row (stale XmlPayload, UnknownUploadOutcomes=3); set only AnafStatus/AnafUploadId/LastError as §15.9 does; run tick with timeout client; assert xmlBuilder never called and status==Failed. **Trigger-list-shaped:** no (a documented SQL statement) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — requirements, convergence 1, verdict confirmed
  - v13: fixed @`add7611` — the documented batch SQL now also clears `XmlPayload`, `PdfStoragePath`, `UnknownUploadOutcomes` and `ClaimedAt`, with a comment saying why each one matters
  - v13: verified by reading — a documented SQL statement has nothing to redden; it was checked field by field against `RetryAsync`

### PPW-600 — FR-4's exponential backoff (1h/4h/16h/64h) never runs — Rejected is terminal until an admin acts

- **What:** ANAF rejects an invoice for a transient validation reason. The batch query selects only Pending|Submitted, so the Rejected row is never picked up again: no 1h/4h/16h/64h retry occurs and the documented Rejected→Failed transition is unreachable. Unless an admin notices, the 5-business-day submission deadline lapses silently.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:99`
- **Suggested fix:** Either include Rejected rows on a backoff schedule derived from BackoffHours, or amend FR-4/story 002 and ddd-01's state table to state that rejections are admin-retry-only. **Files:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:99`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:440`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:462`, `Services/Invoicing/InvoiceLifecycle.cs:97`, `Configuration/AnafSettings.cs:33`, `Controllers/AdminInvoicesController.cs:105`. **Path:** Invoice created, uploaded, Submitted. Tick N: GetStatusAsync returns Rejected; elapsed < 85h so MarkRejectedAsync sets AnafStatus=Rejected. Tick N+1..∞: the batch Where only matches Pending|Submitted, so the row is never selected again — no 1h/4h/16h/64h re-upload. BackoffHours is used solely as a sum (IsBudgetExhausted) at rejection time. MarkFailedAsync CASes on Submitted, so Rejected→Failed is unreachable; only the admin retry endpoint frees the row. **Test shape:** InvoiceUploadJobTests.RejectedInvoice_IsRetriedAfterBackoff: arrange Submitted invoice + stub returning Rejected; act tick, advance clock 2h, tick; assert AnafStatus==Submitted and UploadAsync called twice. Reddens — row stays Rejected, upload called once. **Trigger-list-shaped:** yes (adds a backoff schedule to a background job, or amends the contract) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — requirements, convergence 1, verdict confirmed
  - v12: owner regraded 🟠 to 🔴 on 2026-08-24 — with PPW-604 it means an ANAF outage can let the filing deadline lapse while the only dashboard panel shows nothing wrong
  - v13: fixed @`6977d5b` — Rejected rows re-enter the batch on cumulative `BackoffHours` milestones from `CreatedAt` (ADR-024, no persisted counter) and reach Failed through `GiveUpOnRejectedAsync` when the schedule is spent. Reverting the dispatch reddens two of the three new tests
  - v13: verified — removing the Rejected dispatch case reddened two of the three new tests (the third is the negative control), restored green

### PPW-601 — system-architecture.md was never updated for the invoicing feature, breaking the descriptive-standards rule

- **What:** An agent routed here by CLAUDE.md for "storage, jobs, auth, payments" sees a 9-job table without InvoiceUploadJob, reads "every read/write/delete routes by Upload.StorageLocation" (Invoice.StorageLocation and InvoiceStorageKeys now exist too), and "queueing is in-process… no durable queue table" — false for the DB-polling ANAF worker. Payment-success list omits invoice-number allocation.
- **Evidence:** `memory-bank/standards/system-architecture.md:83`
- **Suggested fix:** Add InvoiceUploadJob to the job table, note Invoice.StorageLocation + invoices/{yyyy}/{MM} keys in the storage section, and add invoice creation to the Paid side-effect list. **Files:** `memory-bank/standards/system-architecture.md:74-87`, `memory-bank/standards/system-architecture.md:60-61`, `memory-bank/standards/system-architecture.md:99-100`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:52-104`, `Models/Invoice.cs:47`, `Controllers/WebhooksController.cs:40`. **Path:** Every clause checks out. InvoiceUploadJob is a DB-polling BackgroundService (PeriodicTimer over db.Invoices with ClaimedAt/TTL claims) — a durable queue table, contradicting "in-process, no durable queue table". Invoice.StorageLocation:47 and InvoiceStorageKeys exist; invoice PDFs route by storageRouter.CloudEnabled, not Upload.StorageLocation. WebhooksController allocates invoice numbers on payment success, absent from the doc's list. The job table says 9 and also omits AwbDispatcher/AwbRetryJob/ShipmentTrackingJob. **Test shape:** Doc-gate lint: assert every BackgroundService/IHostedService type name in src/PhotoPrint.API appears in system-architecture.md's job table. Reddens today on InvoiceUploadJob, AwbDispatcher, AwbRetryJob, ShipmentTrackingJob. **Trigger-list-shaped:** no (a standards document) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — requirements, convergence 1, verdict confirmed
  - v12: owner regraded 🟠 to 🟡 on 2026-08-24 and moved it to backlog — paperwork only; no runtime behaviour differs

### PPW-602 — Invoice 404 advertises Retry-After 30 seconds although the PDF can be a 30-minute poll interval away

- **What:** Customer pays and clicks "download invoice" immediately. The only producer of XML/PDF is the 30-minute-poll worker, so the endpoint 404s for up to 30 minutes while telling the client to retry in 30 seconds. The intent's performance NFR (p95 Paid→PDF stored < 10 s) is silently unmet and unmeasured.
- **Evidence:** `Controllers/InvoicesController.cs:68`
- **Suggested fix:** Send a Retry-After matching Anaf:PollIntervalMinutes, and either kick the worker on the Paid transition or restate the NFR in the bolt docs. **Files:** `Controllers/InvoicesController.cs:68`, `Controllers/WebhooksController.cs:449`, `Services/Invoicing/InvoiceCreationService.cs:90`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:279`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:54`, `appsettings.json:102`. **Path:** Webhook `SaveOrderPaidWithInvoiceAsync` (line 449) inserts the Invoice with `AnafStatus=Pending` and no `PdfStoragePath`. The only renderer is `InvoiceUploadJob.UploadPendingAsync` Step 2 (line 279-289), driven by a `PeriodicTimer` at `PollIntervalMinutes` = 30 (appsettings.json:102, AnafSettings default). So: pay, GET the invoice, hit line 66 -> `Retry-After: 30` (seconds) while the PDF is up to 30 minutes away; a failed row adds another cooldown interval. **Test shape:** InvoicesControllerTests.Invoice404_RetryAfter_MatchesProducerInterval: arrange paid order + Pending invoice, PollIntervalMinutes=30; act GET invoice; assert Retry-After seconds >= 1800. Reddens today (returns 30). **Trigger-list-shaped:** yes (either re-times the producer or wakes the background job) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — requirements, convergence 1, verdict confirmed
  - v13: fixed @`add7611` — the 404 carries a `Retry-After` of `Anaf:PollIntervalMinutes`, so the hint matches the only producer of the PDF
  - v13: verified — revert-and-rerun held, and the new controller test pins the header to 1800 seconds

### PPW-603 — The poll leg has no catch, so an ANAF outage logs Error row-failed there while the upload leg logs Warning unreachable

- **What:** ANAF returns 503 for an hour. Pending rows: LogWarning anaf.upload-job.unreachable + LastError set + cooldown applies. Submitted rows: GetStatusAsync throws AnafUnreachableException, uncaught in PollSubmittedAsync, so the batch loop logs Error anaf.upload-job.row-failed, records no LastError, so the row is re-polled with no cooldown every tick.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:414`
- **Suggested fix:** Catch AnafUnreachableException/AnafUploadException in PollSubmittedAsync: RecordErrorAsync on the row and log anaf.upload-job.unreachable at Warning, matching the upload leg. **Files:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:425`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:157`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:100`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:355`, `Services/Invoicing/Anaf/AnafSpvClient.cs:128`, `Services/Invoicing/InvoiceLifecycle.cs:38`. **Path:** Submitted row, AnafUploadId set, LastError null (MarkSubmittedAsync:38 nulls it). ANAF answers 503: GetStatusAsync (AnafSpvClient:128) throws AnafUnreachableException. PollSubmittedAsync:425 has no catch; ProcessOneAsync doesn't either, so the batch loop's generic catch (line 157) logs Error "row-failed" and no lifecycle call runs — LastError stays null, UpdatedAt unchanged. Query line 100 admits LastError==null unconditionally, so it re-polls every tick, no cooldown. Upload leg (355) logs Warning + records the error. **Test shape:** InvoiceUploadJobTests.PollOutage_RecordsErrorAndCoolsDown: seed Submitted invoice with AnafUploadId; fake IAnafSpvClient.GetStatusAsync throws AnafUnreachableException(503); run one tick; assert LastError non-null and no Error-level "row-failed" log. Reddens today (LastError null). **Trigger-list-shaped:** yes (adds a catch layer that drives the cooldown) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — observability, convergence 1, verdict confirmed
  - v12: owner regraded 🟠 to 🟡 on 2026-08-24 and moved it to backlog — needs an ANAF outage and costs log noise plus a wrong level, nothing more

### PPW-604 — No metric marks a stuck or retrying invoice, so the sole ANAF panel goes blind during an outage

- **What:** ANAF is unreachable for three days with 400 invoices Pending. invoice_anaf_status_total never increments (only submitted/accepted/rejected/failed do), so the dashboard expression accepted/(status!=pending) has a zero denominator and shows no data — identical to a healthy shop with nothing to submit, while the 5-day filing deadline passes.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:344`
- **Suggested fix:** Add a retry/stuck label value (or an anaf_submission_attempt_total counter with an outcome label) incremented on the unreachable, upload-errors and auth branches. **Files:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:355`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:344`, `Services/Invoicing/Anaf/AnafSpvClient.cs:68`, `Observability/MetricNames.cs:77`, `ops/dashboards/fototipar-overview.json:310`, `Services/Invoicing/Anaf/AnafOutageRegistry.cs:11`. **Path:** 400 Pending invoices; AnafSpvClient.UploadAsync throws AnafUnreachableException (line 68, HTTP 5xx) every tick for 3 days. InvoiceUploadJob's catch (line 355) only calls RecordPendingErrorAsync + LogWarning + ReleaseClaim — no counter, no Sentry, no MarkOutageOnce. Note line 344 labels a *successful* submit "pending", so during the outage no label ever increments. Dashboard expr (fototipar-overview.json:310) divides by status!="pending": empty denominator, "No Data" — identical to an idle shop. **Test shape:** AnafOutageEmitsMetric: arrange 1 Pending invoice + IAnafSpvClient throwing AnafUnreachableException; act one job tick; assert a collected invoice_anaf_status_total sample with a non-pending/retrying status. Reddens now (zero measurements). **Trigger-list-shaped:** no (a counter and a dashboard expression) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — observability, convergence 1, verdict confirmed
  - v12: owner regraded 🟠 to 🔴 on 2026-08-24 — with PPW-600 it means an ANAF outage is invisible: no metric marks a stuck or retrying invoice
  - v13: fixed @`32d4eee` — a `retrying` value joins `invoice_anaf_status_total` on the unreachable, upload-error and auth branches; the cardinality budget was raised deliberately from 4 to 5 and metrics.md no longer calls the instrument future
  - v13: verified — the mechanical red leg is not trustworthy here — this row's commit touches a file a later commit in the round touched again, so a file-level revert can break compilation and read as red. Re-proved by hand in isolation: removing only the `retrying` emission reddened the metric test. Renaming the constant does **not** redden it: the assertion reads the same constant, so the label value itself is pinned only by the cardinality budget and metrics.md

### PPW-605 — Manual admin mark-Paid issues a fiscal invoice with no log naming the admin

- **What:** An admin PATCHes /api/admin/orders/{id}/status to Paid. A real invoice number is allocated and an Invoice row committed. Neither AdminOrdersController nor AdminOrderService writes an audit line, and no metric fires. Asked later who issued FT-2026-000123 outside the payment processors, logs cannot answer.
- **Evidence:** `Services/AdminOrderService.cs:154`
- **Suggested fix:** Log admin.order.mark-paid with admin_user_id, order_id and the committed invoice_number, matching AdminInvoicesController's admin_user_id convention. **Files:** `Controllers/AdminOrdersController.cs:52`, `Services/AdminOrderService.cs:154`, `Services/AdminOrderService.cs:428`, `Services/Invoicing/InvoiceCreationService.cs:96`, `Extensions/SerilogExtensions.cs:9`, `Controllers/AdminInvoicesController.cs:74`. **Path:** Admin (user U) PATCHes /status Paid on an order with PaidAt null. UpdateStatusAsync:149-155 calls SaveWithInvoiceAsync, which commits Invoice FT-2026-000123. Emitted logs: InvoiceCreationService:96 (order_id, invoice_number) and the default Serilog request log (method/path/status/elapsed only — SerilogExtensions adds no user enricher). No admin_user_id anywhere, no metric. Contrast AdminInvoicesController:74, which logs admin_user_id even for a list. **Test shape:** AdminOrderStatusTests.MarkPaid_LogsIssuingAdmin: arrange admin-authenticated PATCH status=Paid on unpaid order with a capturing ILogger; act; assert a log event carries admin_user_id plus the new invoice_number. Reddens — no such event exists. **Trigger-list-shaped:** no (one audit log line) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — observability, convergence 1, verdict confirmed
  - v13: fixed @`add7611` — `admin.order.mark-paid` carries admin_user_id, order_id, order_number and the committed invoice_number; the id is passed from the controller rather than through a new HttpContext dependency
  - v13: verified — revert-and-rerun held, and the new test asserts admin_user_id and the committed invoice number in one log line

### PPW-606 — Only the pre-commit attempted invoice number is logged; the committed number is never logged

- **What:** Two deliveries race; the loser burns nextval and retries. Logs show invoice.creation.number-attempted twice with different numbers and number-collision-retry without a number; nothing states which number committed. Reconstructing which sequence values were burned (real gaps in the fiscal series) needs a diff of 30-day-retained logs against the table.
- **Evidence:** `Services/Invoicing/InvoiceCreationService.cs:98`; `Services/Invoicing/InvoiceCreationService.cs:96`, `Controllers/WebhooksController.cs:449`, `Controllers/WebhooksController.cs:471`, `Services/AdminOrderService.cs:433`, `Services/Invoicing/PostgresInvoiceNumberingService.cs:40`, `Data/PhotoPrintDbContext.cs:412`
- **Suggested fix:** Emit invoice.creation.issued with order_id and invoice_number after the successful SaveChanges, and include the abandoned number in the collision-retry line.
- **History:**
  - v12: found by the certification pass — observability, convergence 1, verdict plausible. No failing execution exists. Nothing mutates invoice.InvoiceNumber between the log (line 98) and SaveChangesAsync, so per order_id the last number-attempted line before a successful save IS the committed number; retries re-log a fresh number, and exhaustion/duplicate lines mark the non-committing cases. The committed number also lives permanently in Invoices.InvoiceNumber, a stronger record than 30-day logs. Burned values are the attempted lines. Observability nit, no wrong result. Guard hunt: No guard. Both commit paths return success with no log: WebhooksController.cs:457-458 and AdminOrderService.cs:441-442 log nothing after SaveChanges; retry/exhausted logs (WebhooksController.cs:472, 480) omit the number; downstream logs use invoice_id only. The single incidental carrier is InvoiceUploadJob.cs:291 logging key=invoices/yyyy/MM/{InvoiceNumber}.pdf — a later background job that can fail or lag, not a commit-time record.
  - v12: owner regraded 🟠 to ⚪ on 2026-08-24 and moved it to backlog — the committed number is already permanent on the row, so there is no real gap

### PPW-607 — Admin- and config-sourced fields (invoice line name) reach the UBL XML with no control-char guard and no truncation

- **What:** Admin pastes a product name copied from Word containing U+000B (Word's manual line break). No validator rejects it (Admin product validators have MaximumLength only). At Paid, XmlTextWriter emits `&#xB;` rather than throwing (verified empirically), so the stored XML is unparseable; ANAF 400s and the job retries it forever, never filing.
- **Evidence:** `Services/Invoicing/InvoiceXmlBuilder.cs:204`
- **Suggested fix:** Strip/reject XML-invalid chars and truncate inside InvoiceXmlBuilder (covers product name, size, finish, AWB note, seller); also add TextValidation.HasNoXmlInvalidChars plus a length rule to the product/size/finish validators. **Files:** `Services/Invoicing/InvoiceXmlBuilder.cs:204`, `Services/Invoicing/InvoiceXmlBuilder.cs:67`, `Validators/Admin/CreateProductRequestValidator.cs:10`, `Validators/TextValidation.cs:10`, `Services/OrderService.cs:89`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:269`. **Path:** CreateProductRequestValidator.Name has only NotEmpty+MaximumLength (no TextValidation.HasNoXmlInvalidChars, unlike address/name validators). Admin saves name "PosterMare"; OrderService.cs:89 snapshots it; InvoiceUploadJob:269 builds the Item/Name from it. I ran the builder's exact writer setup: XmlTextWriter emits `Poster&#xB;Mare` without throwing, and XDocument.Parse of that output throws "hexadecimal value 0x0B, is an invalid character". The bad XML is persisted to XmlPayload and re-sent each tick. Caveat: retries are budget-capped, not literally forever; the description is also untruncated (>100 chars possible). **Test shape:** InvoiceXmlBuilderTests.ProductNameWithControlChar_StillProducesParseableXml: arrange an order whose ProductSnapshot.ProductName contains (char)0x0B; act Build(...); assert XDocument.Parse(UTF8 bytes) succeeds and Item/Name has no 0x0B. Reddens today with XmlException. **Trigger-list-shaped:** no (character filtering and truncation in the builder and validators) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — input-validation, completeness-critic, convergence 2, verdict confirmed
  - v13: fixed @`166230a` — every field routed through the address formatter drops XML-invalid characters, and the UBL line description is wrapped in it
  - v13: verified — revert-and-rerun held, and the row's files were touched by no later commit in the round

### PPW-608 — Admin cannot mark an order Paid by hand — NEXT_STATUSES has no AwaitingPayment entry

- **What:** AdminOrderService.UpdateStatusAsync fully supports AwaitingPayment->Paid (stamps PaidAt, allocates the invoice, handles number collision). But NEXT_STATUSES lacks an 'AwaitingPayment' key, so nextStatuses is [] and the whole 'Schimbă status' card is hidden by @if in the template. An admin reconciling an offline bank transfer has no way to mark the order Paid from the UI.
- **Evidence:** `src/app/features/admin/pages/order-detail/admin-order-detail-page.ts:19`; `src/app/features/admin/pages/order-detail/admin-order-detail-page.ts:19`, `src/app/features/admin/pages/order-detail/admin-order-detail-page.ts:66`, `src/app/features/admin/pages/order-detail/admin-order-detail-page.html:114`, `src/app/features/admin/pages/state-machine/admin-state-machine-page.ts:90`, `Services/AdminOrderService.cs:149`, `Services/OrderStatusMachine.cs:20`
- **Suggested fix:** Add AwaitingPayment: ['Paid', 'Cancelled', 'PaymentFailed'] to NEXT_STATUSES and confirm the mark-Paid action in the template.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict plausible. Mechanically true: status 'AwaitingPayment' misses NEXT_STATUSES, nextStatuses is [], and html:114's @if hides the card. But it is not a failure — the admin state-machine page's own rule (line 90) tells admins exactly this: manual Paid marking for offline reconciliation "exists only via the API — the panel has no button for it." Behaviour matches the documented contract, so this is a feature gap, not a defect. Guard hunt: No guard. NEXT_STATUSES (admin-order-detail-page.ts:19-23) has no AwaitingPayment key, so nextStatuses is [] and the "Schimbă status" card is hidden by @if in admin-order-detail-page.html:114. That page is the only UI caller of AdminService.updateOrderStatus. The closest thing is documentation, not a check: admin-state-machine-page.ts:90 states manual marking as Paid "există doar prin API — panoul nu are buton pentru ea", confirming the gap.
  - v13: fixed @`add7611` — `NEXT_STATUSES` gains `AwaitingPayment: [Paid, PaymentFailed, Cancelled]`, so an offline transfer can be reconciled from the UI the API already supported
  - v13: verified — revert-and-rerun held, and the new spec asserts `Paid` is offered for an order awaiting payment

### PPW-609 — One generic error string blames the cart for every payment failure, and EuPlatesc failures are silent

- **What:** Any createStripeIntent failure (400 address validation, 409 idempotency divergence with divergentFields, 500, network) renders 'Verificați că aveți articole în coș.' while the pay button stays disabled forever — a dead end at the last checkout step. payWithEuPlatesc's error handler only clears the spinner, so the same failure on that tab shows nothing at all.
- **Evidence:** `src/app/features/checkout/pages/payment-step.ts:188`
- **Suggested fix:** Branch on HttpErrorResponse status, surface the API ProblemDetails detail/divergentFields, add a retry button, and show a message on the EuPlatesc error path. **Files:** `src/app/features/checkout/pages/payment-step.ts:188`, `src/app/features/checkout/pages/payment-step.ts:54`, `src/app/features/checkout/pages/payment-step.ts:235`, `src/app/core/interceptors/error.interceptor.ts:35`, `src/app/core/services/payment.service.ts:18`. **Path:** Delivery complete, Stripe loads, POST /payments/stripe/intent returns 409 (idempotency divergence). error handler (188) sets "Verificați că aveți articole în coș."; stripeReady stays false, so "Plătește acum" is permanently disabled (54) with no retry. Switching to EuPlatesc and clicking gives the same 409: handler (235-237) only clears the spinner, and errorInterceptor toasts only 403/5xx/status-0, so nothing is shown. 500/network do get a generic toast. **Test shape:** payment-step.spec.ts: "surfaces the server reason on intent 409 and on EuPlatesc failure". Arrange delivery-complete state, flush POST 409 with divergentFields. Assert error text is not the cart string and pay is retryable; on EuPlatesc click + 409, assert a visible error. **Trigger-list-shaped:** yes (a payment-step error state machine) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict confirmed
  - v13: fixed @`06fd2b1` — retired by deletion: with one processor there is no shared error string to mislabel, and the EuPlatesc branch it named is gone

### PPW-610 — The invoice-number-exhausted 409 message is replaced by a generic admin failure toast

- **What:** AdminOrderService throws ConflictException with a specific Romanian message ('nu s-a putut aloca un număr de factură ... reîncearcă după verificarea seriei'). The subscribe error handler sets actionError='Actualizarea statusului a eșuat.' and error.interceptor ignores 409, so the admin never learns the order stayed unpaid for a numbering reason and that manual reconciliation is needed.
- **Evidence:** `src/app/features/admin/pages/order-detail/admin-order-detail-page.ts:133`; `src/app/features/admin/pages/order-detail/admin-order-detail-page.ts:19`, `src/app/features/admin/pages/order-detail/admin-order-detail-page.ts:133`, `src/app/features/admin/pages/order-detail/admin-order-detail-page.html:114`, `src/app/core/services/admin.service.ts:66`, `Services/AdminOrderService.cs:149`, `Middleware/ExceptionHandlerMiddleware.cs:21`
- **Suggested fix:** Read err.error.detail/title from the ProblemDetails body into actionError instead of a fixed string.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict plausible. Not reachable from this page. The 409 comes only from the newStatus==Paid branch (AdminOrderService.cs:149-164), but the UI's NEXT_STATUSES map (admin-order-detail-page.ts:19-23) offers Printing/Cancelled/Shipped/Delivered only — never Paid — and an AwaitingPayment order yields an empty list, so the form is not even rendered (html:114). This page is the sole UI caller of updateOrderStatus, so no click can produce that message. Other 409s in the status path don't exist (transition errors are 400). Guard hunt: No guard. admin-order-detail-page.ts:132-136 `error: () =>` drops the HttpErrorResponse and hardcodes 'Actualizarea statusului a eșuat.'; nothing reads err.error message/detail. error.interceptor.ts:22-41 handles only 401/403/>=500/0, so 409 shows nothing. Backend does throw it: AdminOrderService.cs:163-164 ConflictException on PaidSaveOutcome.NumberExhausted from the admin Paid path. Finding is real.
  - v12: owner regraded 🟠 to 🟡 on 2026-08-24 and moved it to backlog — the path is unreachable until PPW-608 ships a Paid transition in the admin UI

### PPW-611 — SPA still sends the deprecated shippingCostRon, so every checkout logs a tampering warning

- **What:** CreateOrderRequest still declares shippingCostRon and payment-step.ts:252 populates it. DetectLegacyShippingCostFilter (on PaymentsController) logs WARN 'payments.shipping-cost-tampering-attempt' for any body containing that key. Every legitimate checkout therefore emits a tampering warning — the signal is 100% false positives, real tampering is indistinguishable, and the filter's documented removal criterion (zero WARNs) can never be met.
- **Evidence:** `src/app/core/models/payment.model.ts:8`
- **Suggested fix:** Drop shippingCostRon from CreateOrderRequest and from buildOrderRequest in payment-step.ts; the server already re-derives the cost. **Files:** `src/app/core/models/payment.model.ts:8`, `src/app/features/checkout/pages/payment-step.ts:252`, `src/app/core/services/checkout-state.service.ts:12`, `src/app/core/services/payment.service.ts:19`, `Controllers/PaymentsController.cs:17`, `Filters/DetectLegacyShippingCostFilter.cs:69`. **Path:** Checkout: checkout-state seeds shippingCostRon: 0 (line 12), so buildOrderRequest (payment-step.ts:252) always sets the key; payment.service.ts:19 POSTs it to /api/payments/stripe/intent. PaymentsController.cs:17 attaches DetectLegacyShippingCostFilter, whose case-insensitive key scan (line 69) matches and logs WARN "payments.shipping-cost-tampering-attempt". Every legitimate checkout warns, so the "zero WARNs" removal criterion is unreachable and real tampering is indistinguishable. **Test shape:** payment-step.spec.ts: "stripe intent body omits the deprecated shipping cost". Arrange checkout state with method Courier (cost 20); act payWithStripe(); assert HttpTestingController's expectOne body has no own key matching /shippingcostron/i. Reddens today. **Trigger-list-shaped:** no (drops a deprecated request field) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict confirmed
  - v12 fix round: fixed @`2acda1f` — the deprecated shippingCostRon left the request and its model, so a checkout no longer logs a tampering warning that would mask a real one

### PPW-612 — Checkout address form mirrors only the phone rule, so the new fiscal-address length/charset caps surface as a 400 at the payment step

- **What:** CreateOrderRequestValidator caps combined street+number+block at 150 chars, city at 50, recipientName at 200 and rejects XML-invalid chars. addressForm has no maxLength validators and no combined-length check, so a long street plus block passes the client gate and 'Continuă'; the 400 only fires on /checkout/plata, where it renders as 'Verificați că aveți articole în coș.' with the offending field two pages back.
- **Evidence:** `src/app/features/checkout/pages/delivery-step.ts:336`
- **Suggested fix:** Add Validators.maxLength mirroring the server caps plus a group-level combined street+number+block <=150 validator and maxlength attributes on the inputs. **Files:** `src/app/features/checkout/pages/delivery-step.ts:336`, `src/app/features/checkout/pages/delivery-step.ts:354`, `src/app/features/checkout/pages/review-step.ts:187`, `src/app/core/services/checkout-state.service.ts:52`, `src/app/features/checkout/pages/payment-step.ts:188`, `Validators/Payments/CreateOrderRequestValidator.cs:42`. **Path:** Type street = 140 chars, number "12", block "Bl A2 Sc 1 Ap 45" (combined 160 > 150). addressForm only has Validators.required (+phone rules), so it is VALID, canContinue() true, "Continuă" navigates; review-step validates only the terms checkbox; isDeliveryComplete() checks non-blank only. payment-step's initStripe POSTs, FluentValidation auto-validation returns 400, and the error callback shows "Nu s-a putut crea sesiunea de plată. Verificați că aveți articole în coș." City > 50 behaves identically. **Test shape:** delivery-step.spec.ts: "blocks continue when street+number+block exceed 150 chars" — patch addressForm with a 140-char street, number, long block; expect canContinue() false (and a length field-error). Currently true, so it reddens. **Trigger-list-shaped:** no (client validators mirroring server caps) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — input-validation, frontend-ux, convergence 2, verdict confirmed
  - v13: fixed @`5cd48a5` — the checkout address form mirrors the server caps, including a group-level combined street length, so an over-long field fails where it is typed
  - v13: verified — the mechanical red leg is not trustworthy here — this row's commit touches a file a later commit in the round touched again, so a file-level revert can break compilation and read as red. Re-proved by hand in isolation: widening every new cap to 100000 reddened the combined-street-line spec, restored green

### PPW-613 — VAT is never shown in the SPA although the API now returns NetTotalRon/VatRon/VatRate

- **What:** OrderDetailDto server-side carries NetTotalRon, VatRon and VatRate (the point of the VAT bolt), but the Angular OrderDetailDto omits all three and no template renders a TVA line — review-step, confirmation-page and order-detail-page show only Subtotal/Transport/Total. A repo-wide grep for TVA/vatRon finds one static sentence on the pricing page. The customer never sees the VAT amount they are invoiced.
- **Evidence:** `src/app/core/models/order.model.ts:32`
- **Suggested fix:** Add netTotalRon/vatRon/vatRate to the FE model and render a 'TVA (19%)' line on the review, confirmation and order-detail totals. **Files:** `src/app/core/models/order.model.ts:33`, `src/app/features/orders/pages/order-detail-page.ts:91`, `src/app/features/orders/pages/confirmation-page.ts:41`, `src/app/features/checkout/pages/review-step.ts:57`, `DTOs/Orders/OrderDetailDto.cs:8`, `Controllers/InvoicesController.cs:38`. **Path:** GET /api/orders/{id} returns NetTotalRon/VatRon/VatRate (OrderDetailDto.cs:8-10). order.model.ts:33 omits all three; order-detail-page.ts:91-101 renders Subtotal/Transport/Total, confirmation-page.ts:41 only Total, review-step.ts:57 Subtotal/Transport/Total. UI-wide grep for vatRon|vatRate|netTotalRon = zero hits; "TVA" = one static pricing-page sentence. The customer invoice-PDF endpoint (InvoicesController:38) is called by no UI service either. No runtime error — pure disclosure gap; VAT never reaches the customer's screen. **Test shape:** order-detail-page.spec.ts "renders the TVA line": arrange stub order with vatRon 19.44, vatRate 0.19; render component; assert host text contains "TVA" and "19.44". Reddens today — template has no TVA row. **Trigger-list-shaped:** no (three model fields and a template line) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict confirmed
  - v13: fixed @`6812453` — order detail and the confirmation page render a TVA line from the server `vatRon`/`vatRate`; the payment-status DTO carries the two fields, and the review step is deliberately left out (no order exists yet, so the rate would have to be hardcoded in the SPA)
  - v13: verified — revert-and-rerun held, and the row's files were touched by no later commit in the round

### PPW-614 — Hardcoded 20/25 RON shipping defaults with no error handling can differ from the invoiced total

- **What:** easyboxCostRon/courierCostRon start at 20/25 and getShippingCost() is subscribed with no error callback. If the operator changes Shipping:EasyboxCostRon to 15, or the cost call fails/is slow and the user clicks a delivery card first, setMethod stores 20 into checkout state. The recap page shows Transport 20.00 and a matching Total, while OrderService re-derives 15 — the customer agrees to a total that differs from the charged and invoiced amount.
- **Evidence:** `src/app/features/checkout/pages/delivery-step.ts:327`
- **Suggested fix:** Block method selection until both costs resolve (or re-read the cost inside selectMethod), and surface an error when getShippingCost fails instead of silently keeping the defaults. **Files:** `src/app/features/checkout/pages/delivery-step.ts:327`, `src/app/features/checkout/pages/delivery-step.ts:363`, `src/app/features/checkout/pages/delivery-step.ts:445`, `src/app/core/services/checkout-state.service.ts:25`, `src/app/features/checkout/pages/review-step.ts:62`, `Services/OrderService.cs:100`. **Path:** Set Shipping:EasyboxCostRon=15. On /checkout/livrare ngOnInit fires GET /shipping/cost with no error callback; easyboxCostRon stays 20 while pending (or forever if it 500s). User clicks Easybox now: selectMethod stores 20 via setMethod, persisted to sessionStorage. Nothing re-syncs when 15 arrives. Recap renders Transport 20.00 and Total subtotal+20; OrderService.cs:100 re-derives 15 (client can't send it — DetectLegacyShippingCostFilter), so charge and invoice line read 15. **Test shape:** delivery-step.spec: "stores the server cost, not the 20 RON default" — arrange HttpTestingController without flushing (or flush an error), click the Easybox radio, then flush {costRon:15}; assert checkoutState.snapshot.shippingCostRon === 15. **Trigger-list-shaped:** yes (gates the delivery-step state machine on a pending call) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict confirmed
  - v13: fixed @`6812453` — the cost signals start null, both cards stay disabled until the server answers, a failure offers a retry, and a price arriving after a restored method updates the stored cost without wiping the locker or address
  - v13: verified — revert-and-rerun held, and the row's files were touched by no later commit in the round

### PPW-615 — A non-succeeded, non-error Stripe result leaves the user stranded with no feedback

- **What:** payWithStripe only handles result.error and status==='succeeded'. For 'processing' or 'requires_capture' nothing happens: no message, no navigation, spinner cleared. A user whose payment is genuinely processing sees the button simply re-enable and will click again. A rejected confirmCardPayment promise (network drop) is worse — stripeLoading stays true, so the button is disabled forever.
- **Evidence:** `src/app/features/checkout/pages/payment-step.ts:221`
- **Suggested fix:** Wrap the await in try/catch that clears stripeLoading and shows an error, and add an else branch messaging any status other than 'succeeded'. **Files:** `src/app/features/checkout/pages/payment-step.ts:213`, `src/app/features/checkout/pages/payment-step.ts:217`, `src/app/features/checkout/pages/payment-step.ts:219`, `src/app/features/checkout/pages/payment-step.ts:46`, `src/app/features/checkout/pages/payment-step.ts:200`, `Services/StripePaymentGateway.cs:21`. **Path:** Line 213 awaits confirmCardPayment with no try/catch: any rejection skips line 217, so stripeLoading stays true and the button is disabled forever with no message. Reachable trigger: switch to EuPlatesc and back — *ngIf (line 46) destroys #stripe-card-element while stripeReady stays true, so pay runs against a detached element and Stripe rejects. Resolved 'processing' also falls through silently. 'requires_capture' is unreachable (no CaptureMethod set, StripePaymentGateway.cs:21). **Test shape:** payment-step.spec.ts: inject fake stripeInstance/cardElement; confirmCardPayment rejects (and a second case resolves status 'processing'); await payWithStripe(); expect stripeLoading() false and stripeError() non-null. Both fail today. **Trigger-list-shaped:** yes (adds the missing catch and status branches to the pay state machine) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict confirmed
  - v13: partly retired by PR #13 — the tab-switch path that detached the mounted Stripe element is gone with the second processor. The finding stays open on its other path: a Stripe result that is neither succeeded nor an error still leaves the customer with no feedback
  - v12 fix round: fixed @`901f8a2` — a result that is neither success nor error now tells the customer, a rejected confirm call clears the spinner, and an intent that never got created offers a retry. The tab-switch half of this finding had already died with the second processor

### PPW-616 — Saved addresses allow City 100 while checkout caps it at 50, and the new prefill copies them in

- **What:** Signed-in user has a saved address with a 60-char city. delivery-step.prefillFromSavedAddress patches it into the form, whose only rule is Validators.required, so the client gate passes. CreateOrder rejects it via MaximumLength(CityNameMaxLength=50). The user sees a 400 on a field they never typed and no maxlength hint.
- **Evidence:** `Validators/Account/SavedAddressValidator.cs:26`
- **Suggested fix:** Align SavedAddressValidator and the Angular form to InvoiceAddressFormatter.CityNameMaxLength / PartyNameMaxLength, and add maxlength attributes plus client validators mirroring the server caps. **Files:** `Validators/Account/SavedAddressValidator.cs:26`, `Validators/Payments/CreateOrderRequestValidator.cs:45`, `Services/Invoicing/InvoiceAddressFormatter.cs:8`, `Data/PhotoPrintDbContext.cs:436`, `src/app/features/checkout/pages/delivery-step.ts:340`, `src/app/features/checkout/pages/delivery-step.ts:429`. **Path:** Save an address with a 60-char city: SavedAddressValidator caps City at 100 and the column is HasMaxLength(100), so it persists. At checkout, prefillFromSavedAddress patches city into addressForm, whose city control is `['', Validators.required]` only, and the input has no maxlength attribute — form valid, Continue enabled. POST create-order hits CreateOrderRequestValidator line 45, MaximumLength(50) → 400 on a field the user never typed, with no hint. **Test shape:** CreateOrderRequestValidator_rejects_city_that_SavedAddressValidator_accepts: arrange a 60-char city; assert SavedAddressValidator passes while CreateOrderRequest fails on ShippingAddress.City — reddens once both share CityNameMaxLength (plus UI spec: prefilled 60-char city leaves addressForm invalid). **Trigger-list-shaped:** no (aligns two validators and the form) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — completeness-critic, convergence 1, verdict confirmed
  - v13: fixed @`5cd48a5` — the saved-address city cap is aligned to `InvoiceAddressFormatter.CityNameMaxLength`, so a prefill cannot carry an over-long value into checkout
  - v13: verified — the mechanical red leg is not trustworthy here — this row's commit touches a file a later commit in the round touched again, so a file-level revert can break compilation and read as red. Re-proved by hand in isolation: putting the city cap back to 100 reddened `SavedAddressValidator`, restored green

### PPW-617 — The paid-transition invoice retry/rollback state machine is implemented twice with divergent guards and no shared test

- **What:** AdminOrderService.SaveWithInvoiceAsync guards on _db.Entry(invoice).State == EntityState.Unchanged; WebhooksController.SaveOrderPaidWithInvoiceAsync guards on != EntityState.Added. Any future creation-service path returning a Modified or Detached entity is treated as already-invoiced by the webhook and pushed to SaveChanges by the admin path. Nothing drives both entry points through one collision harness.
- **Evidence:** `Services/AdminOrderService.cs:437`; `Services/AdminOrderService.cs:436`, `Controllers/WebhooksController.cs:450`, `Services/Invoicing/InvoiceCreationService.cs:60`, `Services/Invoicing/InvoiceCreationService.cs:94`, `Program.cs:267`, `Tests/Unit/Services/AdminOrderServicePaidRaceTests.cs:330`
- **Suggested fix:** Extract the collision-retry loop into one service used by both callers, or add a parameterised test forcing the same number collision through both entry points and asserting identical end state.
- **History:**
  - v12: found by the certification pass — quality, completeness-critic, convergence 2, verdict plausible. Not constructible. The only IInvoiceCreationService implementation returns exactly two shapes: an existing row from a tracking query (Unchanged) or a freshly Added invoice — no AsNoTracking, no pre-modified invoice on either scoped context. For both shapes the guards `== Unchanged` and `!= Added` agree exactly, so no input, state, or timing yields divergent behaviour. Modified/Detached needs a future code change. Each path also has its own race test file; only the shared harness is missing. Real as a maintainability risk, not a defect. Guard hunt: No guard. The two predicates really do diverge: AdminOrderService.cs:436 (`== EntityState.Unchanged`) vs WebhooksController.cs:450 (`!= EntityState.Added`). Nothing pins the returned entity's state — InvoiceCreationService.cs:60-72 just returns the query result or an Added row, and IInvoiceCreationService.cs:26 documents idempotency only, not tracking state. No shared collision test: only WebhooksControllerInvoiceRaceTests.cs exercises the webhook copy. Today's two states coincide, so this is latent, not live.
  - v12: owner regraded 🟠 to 🟡 on 2026-08-24 and moved it to backlog — latent today; it becomes a trap only when the duplicated state machine is next changed

### PPW-618 — Cloud tier and the new cross-tier fallback read are proven only against fakes

- **What:** All 10 skipped tests in the green run are S3StorageServiceIntegrationTests. The newly persisted Invoice.StorageLocation and the Cloud-to-Local fallback read only ever run against an in-memory storage double, so the RecordMiss heuristic that distinguishes a missing key from a missing bucket (S3 maps both to FileNotFoundException) is unverified against a real S3 client.
- **Evidence:** `Controllers/InvoicesController.cs:99`
- **Suggested fix:** Run the MinIO suite with STORAGE_TEST_* set in the certification evidence, and add a fallback-read test on the real S3 service covering missing key versus missing bucket. **Files:** `Controllers/InvoicesController.cs:86`, `Controllers/InvoicesController.cs:99`, `Controllers/InvoicesController.cs:122`, `Services/S3StorageService.cs:119`, `Services/LocalStorageService.cs:74`, `Tests/Unit/Controllers/InvoicesControllerTests.cs:398`. **Path:** Provider=S3, bucket deleted; invoice stamped Local, PDF absent on disk. Local throws bare FileNotFoundException (LocalStorageService.cs:74); the Cloud fallback maps NoSuchBucket 404 to FileNotFoundException with an AmazonS3Exception inner (S3StorageService.cs:119), so RecordMiss swaps it in. But S3 attaches that same inner for a merely missing key, so bucket-vs-key is indistinguishable and the comment's claim is false. All six test doubles throw inner-less exceptions, leaving the discriminating branch unexercised. **Test shape:** InvoicesControllerTests.BlobMissing_KeepsBucketFault: local mock throws bare FileNotFoundException, cloud mock throws one wrapping AmazonS3Exception(404 NoSuchKey); assert logged miss_cause is the local miss, not the S3 key message. Reddens today. **Trigger-list-shaped:** yes (changes the storage-miss classification layer) — no approach pre-check run.
- **History:**
  - v12: found by the certification pass — completeness-critic, convergence 1, hinted, verdict confirmed
  - v12: owner regraded 🟠 to 🟡 on 2026-08-24 and moved it to backlog — the customer sees the correct 404 either way; only the diagnostics are unproven

### PPW-619 — OrderNumberService's manually opened DbConnection is never closed, pinning it for the rest of the scope

- **What:** Every Postgres order creation opens the EF connection by hand for the nextval call and never closes it. EF did not open it, so it stays open until the scoped DbContext is disposed at end of request, holding a pool slot across all remaining work (Stripe/EuPlatesc round-trips included).
- **Evidence:** `Services/OrderNumberService.cs:34`; `Services/OrderNumberService.cs:32-39`, `Services/OrderService.cs:150,189`, `Controllers/PaymentsController.cs:132,163-164`, `Program.cs:190`
- **Suggested fix:** Use _db.Database.SqlQueryRaw<long>("SELECT nextval(...) AS \"Value\"").SingleAsync(ct) like PostgresInvoiceNumberingService does, so EF owns open/close.
- **History:**
  - v12: found by the certification pass — correctness, convergence 1, verdict confirmed. Confirmed via code: on Postgres, GenerateAsync calls conn.OpenAsync() directly on the raw ADO.NET connection (bypassing EF's Database.OpenConnectionAsync). EF only auto-closes a connection it opened itself; since this connection was already Open when EF next touches it (SaveChangesAsync), EF treats it as externally-owned and never closes it. In PaymentsController.CreateIntentAsync, after CreateFromCartAsync (which calls GenerateAsync then SaveChangesAsync), the scoped _db's connection stays open through the Stripe/EuPlatesc network round-trip and the second SaveChangesAsync, closing only when the request-scoped DbContext disposes.

### PPW-620 — Admin invoice paging orders by a non-unique CreatedAt with no unique tiebreaker

- **What:** Two invoices share CreatedAt (webhook burst); Skip/Take can return one of them on both page 1 and page 2 and omit another entirely. OrderService already added a unique tiebreaker for exactly this on its own paged query, so the invoice list is the inconsistent one.
- **Evidence:** `Controllers/AdminInvoicesController.cs:57`; `Controllers/AdminInvoicesController.cs:56-59`, `Models/Invoice.cs:61`, `Services/OrderService.cs:398-399`, `Services/AdminOrderService.cs:93`
- **Suggested fix:** Add .ThenByDescending(i => i.Id) (or Number) to make the ordering total.
- **History:**
  - v12: found by the certification pass — correctness, quality, convergence 2, verdict confirmed. Seed 3 invoices with identical CreatedAt (a webhook burst sets it explicitly, or two inserts land in the same DB tick — SQLite/Postgres timestamp precision plus concurrent inserts makes this plausible, and nothing constrains CreatedAt to be distinct). Call GET /api/admin/invoices?page=1&size=2 then page=2&size=2. Skip/Take over ties with only OrderByDescending(CreatedAt) has no defined tie order in SQL/EF — separate round trips are not guaranteed to agree, so an invoice can appear on both pages while another is skipped entirely. AdminOrderService.cs:93 and OrderService.cs:398-399 both append .ThenBy(o => o.Id) for exactly this reason; AdminInvoicesController.cs:57 doesn't.

### PPW-621 — Per-customer invoice PDF is cached for a year with no revalidation

- **What:** On a shared/kiosk browser a customer downloads /api/orders/{id}/invoice. After logout, anyone re-entering that URL (browser history, autocomplete) is served the cached PDF — full buyer name, address and purchase — from disk cache; the request never reaches the server, so the ownership check never runs. No Vary either.
- **Evidence:** `Controllers/InvoicesController.cs:132`; `Controllers/InvoicesController.cs:45-58 (ownership check)`, `Controllers/InvoicesController.cs:132 (Cache-Control header)`, `Extensions/GuestSessionExtensions.cs:10-22 (DualAuthPolicy)`, `Extensions/ClaimsPrincipalExtensions.cs:9-24 (identity extraction)`
- **Suggested fix:** Use "private, no-store" like the sibling /api/orders/{id}/photos endpoint, or a short max-age with must-revalidate plus Vary: Authorization.
- **History:**
  - v12: found by the certification pass — security, convergence 1, verdict confirmed. Line 132 sets `Cache-Control: private, max-age=31536000, immutable` with no `Vary`, no `ETag`, no `no-store`. Ownership check (lines 45-58) runs only when the request reaches the controller. On a shared browser: user A downloads GET /api/orders/{id}/invoice, browser disk-caches the 200 response keyed by URL (private caches honor max-age regardless of auth changes; immutable suppresses revalidation for a year). A logs out; user B (or A re-entering via history/autocomplete) hits the same URL — browser serves the cached PDF straight from disk, request never leaves the client, so the ownership/Forbid check at line 58 never executes.

### PPW-622 — Buyer fiscal address survives logout in sessionStorage and prefills the next account

- **What:** Customer A completes checkout: name, phone, street, number, city, county, postal code are written to sessionStorage 'fotoTipar_checkout'. logout() clears only access_token. Customer B logs in in the same tab; delivery-step.ts:366 patches the form from that snapshot, showing A's home address and phone.
- **Evidence:** `src/app/core/services/auth.service.ts:174`; `src/app/core/services/auth.service.ts:174`, `src/app/core/services/checkout-state.service.ts:5,58-63,79-83`, `src/app/features/checkout/pages/delivery-step.ts:366-367,397-417`, `src/app/features/orders/pages/confirmation-page.ts`, `src/app/layout/header/header.ts:42-45`
- **Suggested fix:** Call CheckoutStateService.reset() from logout() (and on login of a different subject), so the checkout snapshot dies with the session.
- **History:**
  - v12: found by the certification pass — security, convergence 1, verdict confirmed. Customer A fills delivery-step form; CheckoutStateService.saveToStorage writes name/phone/street/city/etc to sessionStorage['fotoTipar_checkout']. A logs out via header.ts -> AuthService.logout() (auth.service.ts:174-180), which clears only access_token/isAuthenticated/isAdmin/currentUser - not that sessionStorage key. Customer B logs in in the same tab, opens checkout. delivery-step.ts:366-367 reads checkoutState.snapshot.shippingAddress (still A's data) and patchValue's the form immediately. prefillAddress()/prefillFromSavedAddress() (lines 397-430) then run but only fill fields that are still empty, so B's own guest/account data is skipped and A's stale name/phone/address stays displayed and submittable.

### PPW-623 — EuPlatesc IPN fingerprint is verified with a non-fixed-time string compare

- **What:** The unauthenticated /api/webhooks/euplatesc endpoint compares the computed HMAC-MD5 to the attacker-supplied fp via string.Equals(OrdinalIgnoreCase), which short-circuits on first mismatch. An attacker with many attempts has a timing side channel on the only gate protecting order-status transitions.
- **Evidence:** `Services/EuPlatescService.cs:92`; `Services/EuPlatescService.cs:92`, `Services/EuPlatescService.cs:60-67`, `Controllers/WebhooksController.cs:158-176`
- **Suggested fix:** Compare the raw MAC bytes with CryptographicOperations.FixedTimeEquals after parsing both hex strings; reject on length mismatch first.
- **History:**
  - v12: found by the certification pass — security, convergence 1, verdict plausible. Confirmed: EuPlatescService.cs:92 compares HMAC-MD5 via string.Equals(OrdinalIgnoreCase) on a 32-hex fp, called from WebhooksController.cs:170 on the [AllowAnonymous] /api/webhooks/euplatesc endpoint gating order status. The code fact is real, but the claimed harm (timing-oracle brute force) needs statistical timing measurement across many HTTP round-trips, not a single deterministic input->wrong-output case; network jitter dwarfs any per-character comparison delta, so no fixed-input unit test can redden to prove exploitability. Guard hunt: EuPlatescService.cs:92 uses string.Equals(computed, receivedFp, OrdinalIgnoreCase) — short-circuiting, not constant-time. WebhooksController.cs:170 calls ValidateIpnSignature directly on the unauthenticated endpoint with no rate-limit/lockout/IP-allowlist wrapping it. No constant-time comparison (e.g. CryptographicOperations.FixedTimeEquals) or throttling guard exists anywhere in the collaborators.

### PPW-624 — ANAF response body is read into memory with no size cap and then persisted unbounded

- **What:** A misbehaving or hijacked ANAF endpoint streams a huge body for 30s (the client timeout); ReadAsStringAsync buffers it all, then XDocument.Parse doubles it — repeated for up to MaxBatchSize=50 rows per tick. An oversized errorMessage is also written verbatim into Invoice.LastError (text) and the logs.
- **Evidence:** `Services/Invoicing/Anaf/AnafSpvClient.cs:73`; `Program.cs:293-303`, `Services/Invoicing/Anaf/AnafSpvClient.cs:73`, `Services/Invoicing/Anaf/AnafSpvClient.cs:172-183`, `Extensions/SamedayServiceCollectionExtensions.cs:54`, `Services/Invoicing/InvoiceLifecycle.cs:91`, `Data/PhotoPrintDbContext.cs:406`
- **Suggested fix:** Set MaxResponseContentBufferSize on the ANAF HttpClient (e.g. 1 MB) and truncate error text before storing it in LastError or logging it.
- **History:**
  - v12: found by the certification pass — security, convergence 1, verdict confirmed. Program.cs:293-303 registers the ANAF HttpClient with only Timeout=30s — no MaxResponseContentBufferSize, unlike Sameday's client which sets 10MB (SamedayServiceCollectionExtensions.cs:54). A 200 OK response streaming a huge body within 30s hits AnafSpvClient.cs:73/134 ReadAsStringAsync(ct) with no cap, then SafeParse (line 176) XDocument.Parse doubles memory. An oversized errorMessage flows to InvoiceLifecycle.cs:91/105 into Invoice.LastError, an unbounded Postgres "text" column (PhotoPrintDbContext.cs:406) — no truncation anywhere.

### PPW-625 — The PDF-ready notification fires inside the render-once branch, so a throw there loses it permanently

- **What:** NotifyAsync throws (once a real email send exists) after PdfStoragePath was already committed. The generic catch records a pending error and releases the claim; the next tick sees PdfStoragePath set, skips step 2 entirely, and the customer is never notified — no retry, no state marking the notification as owed.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:293`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:279-294`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:247-262`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:315-322`, `Services/Invoicing/InvoicePdfReadyNotifier.cs:21-37`
- **Suggested fix:** Move NotifyAsync outside the render branch behind its own persisted flag (e.g. PdfNotifiedAt), or wrap it so a failure cannot be mistaken for a build failure.
- **History:**
  - v12: found by the certification pass — race, convergence 1, verdict confirmed. Tick 1: UploadPendingAsync builds XML (step1, saved), then step2 renders PDF, sets invoice.PdfStoragePath+StorageLocation, calls SaveChangesAsync (line 289, committed) — THEN calls notifier.NotifyAsync (line 293). If that throws, the generic catch at line 315 runs RecordPendingErrorAsync + ReleaseClaimAsync and returns; invoice stays Pending. Tick 2: row re-selected, needsOrder is false (XmlPayload and PdfStoragePath both already set at lines 247-262), so order is never loaded and the whole step-2 `if (string.IsNullOrEmpty(invoice.PdfStoragePath))` block (line 279) — including the notify call — is skipped forever; execution falls straight to step 3 (ANAF upload). No field tracks "notified" separately from "PdfStoragePath set," so the notification is permanently lost with no retry and no admin-visible signal. Today NotifyAsync (InvoicePdfReadyNotifier.cs:21-37) is a no-op placeholder that can't throw, so this is dormant, not currently exploitable — matching the finding's own "once a real email send exists" caveat.

### PPW-626 — Cloud blob is orphaned when the storage tier flips between a failed path-stamp and the retry

- **What:** storage.SaveAsync writes the PDF to Cloud, then SaveChangesAsync fails, so PdfStoragePath and StorageLocation are never stamped. Cloud is later disabled; the next tick re-renders to Local and stamps Local. The cloud object at invoices/yyyy/MM/{number}.pdf is referenced by nothing and no cleanup sweep covers invoice keys.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:287`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:279-289`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:315-321`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:238-247`, `Services/Invoicing/InvoiceStorageKeys.cs:18-22`, `Services/StorageRouter.cs:22`, `BackgroundJobs/UploadCleanupJob.cs:1-33`
- **Suggested fix:** Stamp PdfStoragePath and StorageLocation before the blob write (or have the invoice cleanup sweep reconcile invoices/ keys against Invoice rows).
- **History:**
  - v12: found by the certification pass — race, convergence 1, hinted, verdict confirmed. Tick N: Step 1 XML saves OK. Step 2 renders PDF, computes deterministic key InvoiceStorageKeys.ForPdf(invoice) (from IssuedAt+InvoiceNumber, unaffected by retries), storage.SaveAsync writes to Cloud, then SaveChangesAsync throws — caught by the generic catch (line 315), which records the error and releases the claim without any storage rollback. DB row keeps PdfStoragePath/StorageLocation unset. Cloud is then disabled. Tick N+1 reloads the same invoice, still sees PdfStoragePath empty, re-renders, re-derives the same key, but now storage=Local; SaveAsync writes locally and SaveChangesAsync succeeds, stamping StorageLocation=Local. The earlier Cloud object at that key is now referenced by nothing, and only UploadCleanupJob exists (covers Upload entities, not Invoice) — no sweep targets invoice keys.

### PPW-627 — Vat:Rate accepts unlimited decimal places while Orders.VatRate is numeric(5,4) and rounds silently

- **What:** Vat:Rate = 0.19999 passes validation. VatCalculator computes VatRon at 19.999%, but Postgres stores VatRate as 0.2000. On the next load VatRateFromInvoice returns 0.2000, so the UBL Percent is 20.00 while TaxAmount was computed at 19.999% — ANAF's arithmetic check rejects the invoice. InMemory keeps full precision, so no test can see it.
- **Evidence:** `Validators/VatSettingsValidator.cs:23`; `Validators/VatSettingsValidator.cs:23`, `Configuration/VatSettings.cs:17`, `Services/OrderService.cs:145`, `Data/PhotoPrintDbContext.cs:338`, `Services/Invoicing/InvoiceCreationService.cs:49`, `Services/Invoicing/InvoiceXmlBuilder.cs:176`
- **Suggested fix:** Add a validator rule that Vat:Rate has at most 4 decimal places (decimal.Round(rate,4) == rate), matching the numeric(5,4) column.
- **History:**
  - v12: found by the certification pass — db-parity, convergence 1, verdict confirmed. Vat:Rate=0.19999 passes the validator (only checks 0<r<1, no scale check). OrderService.cs:145 computes VatRon/NetTotalRon at the full-precision rate and stores order.VatRate=0.19999. The Postgres column is decimal(5,4) (PhotoPrintDbContext.cs:338), which rounds the stored value to 0.2000 on write. InvoiceCreationService.CreateForOrderAsync(orderId) reloads the order later (e.g. from a webhook's separate DbContext/request), getting the rounded 0.2000. InvoiceXmlBuilder.cs:176 emits Percent=20.00 against NetTotalRon/VatRon computed at 19.999%; for large gross totals (verified: ~100000 RON gross gives ~0.84 RON mismatch) this exceeds any reasonable ANAF arithmetic tolerance.

### PPW-628 — Migration Down() drops only invoice_seq_ft_2026, so lazily-created year sequences survive a rebuild and skip numbers

- **What:** A dev/CI database that has issued 2027 invoices is down-migrated then re-upped. Invoices is empty and invoice_seq_ft_2026 is recreated at 1, but invoice_seq_ft_2027 (created lazily by PostgresSequences) survives at 412. The first 2027 invoice on the rebuilt database is FT-2027-00413 — a 412-number silent skip, the exact thing the numbering invariant forbids.
- **Evidence:** `Migrations/20260820133204_InitialPostgres.cs:752`; `Migrations/20260820133204_InitialPostgres.cs:746`, `Migrations/20260820133204_InitialPostgres.cs:752`, `Services/Invoicing/PostgresInvoiceNumberingService.cs:36-38`, `Data/PostgresSequences.cs:11-36`, `Tests/Integration/PostgresInvoiceNumberingServiceIntegrationTests.cs:57`
- **Suggested fix:** In Down(), drop every sequence matching invoice_seq_% via a DO block over pg_class instead of naming invoice_seq_ft_2026.
- **History:**
  - v12: found by the certification pass — db-parity, convergence 1, verdict confirmed. 1) Issue an invoice with year=2027: PostgresInvoiceNumberingService.NextNumberAsync builds seqName "invoice_seq_ft_2027" and calls PostgresSequences.EnsureAsync, which runs CREATE SEQUENCE IF NOT EXISTS — creating it outside migration tracking. Repeat until nextval=412. 2) `dotnet ef database update <prev>` runs Down(): drops Invoices table and DROP SEQUENCE IF EXISTS "invoice_seq_ft_2026" only — invoice_seq_ft_2027 is untouched, still at 412. 3) `dotnet ef database update` runs Up() again: Invoices recreated empty, invoice_seq_ft_2026 recreated fresh at 1. 4) First 2027 invoice call: EnsureAsync sees invoice_seq_ft_2027 already exists, nextval returns 413 -> FT-2027-00413, a 412-number silent skip on an otherwise-empty table.

### PPW-629 — Admin invoice ListAsync output is never asserted — paging, ordering, status filter and the Orders join are unverified

- **What:** The only test calls ListAsync with an empty query and asserts a log line. Flip OrderByDescending to ascending, drop the status Where, or make Skip off-by-one and nothing reddens. The inner Join on Orders also silently drops rows from items while total still counts them, so page sizes look short with no signal.
- **Evidence:** `Tests/Unit/Controllers/AdminInvoicesControllerTests.cs:39`; `Tests/Unit/Controllers/AdminInvoicesControllerTests.cs:39`, `Controllers/AdminInvoicesController.cs:42`, `Controllers/AdminInvoicesController.cs:56`
- **Suggested fix:** Seed invoices across statuses and pages; assert items ids/order, that total counts filtered rows, and that a status filter excludes others.
- **History:**
  - v12: found by the certification pass — tests-coverage, convergence 1, verdict confirmed. Seed 2 invoices with different AnafStatus/CreatedAt/OrderId (one pointing at a nonexistent Order). Call ListAsync with query.Status="Rejected". The sole test, ListAsync_LogsAdminUserId (line 39-50), passes an empty AdminInvoiceListQuery on an empty in-memory DB and only asserts a log Message via LogCapture — it never casts the returned IActionResult or reads items/total. Flip OrderByDescending→OrderBy, remove the Where(i => i.AnafStatus == parsed) filter, change Skip((page-1)*size) to Skip(page*size), or swap the inner Join for one that drops unmatched Orders — every one of these still logs "admin.invoice.list ... total={Total}" with the same admin id, so the sole test stays green.

### PPW-630 — Quarterly gap-audit query uses session-timezone EXTRACT while the unique index uses AT TIME ZONE 'UTC'

- **What:** An invoice issued 2026-12-31 23:30 UTC is year 2026 for uq_invoices_series_year_number but year 2027 for the audit query when the psql session TimeZone is Europe/Bucharest. The fiscal-year audit then reports a phantom gap (or hides a real one) in the numbers handed to the accountant.
- **Evidence:** `docs/DEPLOYMENT.md:1498`; `docs/DEPLOYMENT.md:1494-1508`, `Migrations/20260820133204_InitialPostgres.cs:739-743`, `Data/PhotoPrintDbContext.cs:28-31`
- **Suggested fix:** Change both EXTRACT calls to EXTRACT(YEAR FROM ("IssuedAt" AT TIME ZONE 'UTC')) so the audit matches the index expression exactly.
- **History:**
  - v12: found by the certification pass — requirements, convergence 1, verdict confirmed. Migration (line 741-743) defines the index as EXTRACT(YEAR FROM ("IssuedAt" AT TIME ZONE 'UTC')) — a fixed UTC bucket, immune to session TimeZone. DEPLOYMENT.md's audit query (lines 1498, 1505) does bare EXTRACT(YEAR FROM "IssuedAt") on the timestamptz column — Postgres implicitly converts to session TimeZone before extracting. Insert IssuedAt='2026-12-31 23:30:00+00' (index buckets it 2026). Run the audit with `SET TIME ZONE 'Europe/Bucharest'` (nothing in the runbook pins UTC): local time is 2027-01-01 01:30 EET, so the WHERE/JOIN EXTRACT returns 2027. The row is dropped from the 2026 `expected`/join pool and folded into 2027's, so the query reports phantom gaps in 2026 (row missing from its true bucket) and/or absorbs a stray high Number into 2027's MAX(Number), inflating generate_series and reporting false gaps there too.

### PPW-631 — Bolt-038 test report cites a migration that no longer exists and misstates numbering test coverage

- **What:** Someone certifying the numbering ACs reads this report: it credits migration 20260603101910_AddVatAndInvoices (squashed away; the chain is 20260820133204_InitialPostgres + two AddColumns), describes "in-memory PostgreSQL" and a two-provider matrix that no longer exists, and states the Postgres path has no direct tests — while Integration/PostgresInvoiceNumberingServiceIntegrationTests.cs runs a real Postgres concurrency test.
- **Evidence:** `memory-bank/bolts/038-vat-calculation/ddd-03-test-report.md:54`; `memory-bank/bolts/038-vat-calculation/ddd-03-test-report.md:54`, `memory-bank/bolts/038-vat-calculation/ddd-03-test-report.md:35`, `memory-bank/bolts/038-vat-calculation/ddd-03-test-report.md:84`, `Migrations/20260820133204_InitialPostgres.cs`, `Tests/Integration/PostgresInvoiceNumberingServiceIntegrationTests.cs`, `Tests/Helpers/PostgresTestDatabase.cs:25-49`
- **Suggested fix:** Refresh the test report's file list, migration name and coverage claims against the current suite, or mark it superseded by data-stack.md.
- **History:**
  - v12: found by the certification pass — requirements, convergence 1, verdict confirmed. Reading memory-bank/bolts/038-vat-calculation/ddd-03-test-report.md:54 and the repo confirms all three claims: (1) migration 20260603101910_AddVatAndInvoices is absent from src/PhotoPrint.API/Migrations/ — actual chain is 20260820133204_InitialPostgres + two later AddColumn migrations; (2) report line 35 calls the coverage "in-memory PostgreSQL", but PostgresTestDatabase.cs:25-49 does CREATE DATABASE and Database.Migrate() against a real reachable Postgres server (throws with "these tests need a reachable PostgreSQL server" if none found); (3) report line 84 claims "the Postgres path has no direct unit tests," yet PostgresInvoiceNumberingServiceIntegrationTests.cs runs real concurrency tests (e.g. NextNumberAsync_LosesTheSequenceCreateRace_StillReturnsANumber) against that live Postgres instance. A certifier trusting this doc would look for a nonexistent migration file and wrongly believe the numbering service's Postgres concurrency path is untested by design.

### PPW-632 — Customer-facing blob-missing error is English and carries no correlationId, against api-conventions

- **What:** A customer whose PDF blob is gone gets problem+json with title "Invoice PDF unavailable" and an English detail, bypassing ExceptionHandlerMiddleware. api-conventions.md requires Romanian user-facing detail and always a correlationId, so the Romanian UI shows English text and support cannot correlate the report to logs.
- **Evidence:** `Controllers/InvoicesController.cs:126`; `Controllers/InvoicesController.cs:66-70,119-129`, `Middleware/ExceptionHandlerMiddleware.cs:74,176-182,230-243`, `Extensions/HttpContextExtensions.cs:20-21`, `memory-bank/standards/api-conventions.md:118-119`
- **Suggested fix:** Return the message in Romanian and include correlationId (or throw a typed exception so ExceptionHandlerMiddleware formats it).
- **History:**
  - v12: found by the certification pass — requirements, convergence 1, verdict confirmed. GET /api/orders/{id}/invoice where the Invoice row exists but its PdfStoragePath no longer resolves in any storage tier (both GetStreamAsync calls throw FileNotFoundException). Code falls through to line 126: `return Problem(title: "Invoice PDF unavailable", detail: "The invoice record points at a file that is no longer in storage.", statusCode: 404)`. This calls ControllerBase.Problem() directly, returning an IActionResult — it never throws, so ExceptionHandlerMiddleware (the only place that stamps correlationId via context.GetCorrelationId() at line 74/236, and the only place producing Romanian text like the 500 fallback at line 178) never runs. The client receives problem+json with English title/detail and no correlationId extension, contradicting api-conventions.md:118-119 ("All error detail messages in Romanian" / "Always include correlationId").

### PPW-633 — Full fiscal address is now mandatory for Easybox orders — a customer-visible scope change with no story or AC

- **What:** Parcel-locker checkout now rejects orders that previously succeeded (address fields required, combined street length capped). No story, AC, or requirements/decision-index entry records this contract change; it appears only as an aside in DEPLOYMENT §15.7. A future reviewer cannot tell intended requirement from accidental tightening.
- **Evidence:** `Validators/Payments/CreateOrderRequestValidator.cs:23`; `Validators/Payments/CreateOrderRequestValidator.cs:18-49`, `Services/Invoicing/InvoiceAddressFormatter.cs:14-33`, `src/app/features/checkout/pages/delivery-step.ts:128-137`, `src/app/features/checkout/pages/delivery-step.ts:354-359`, `docs/stories/epic-3-checkout/US-301/frontend-delivery-method.instructions.md:20-26`
- **Suggested fix:** Add the requirement to the intent's requirements.md (or a decision-index entry) naming the invoice-buyer-address reason and the Easybox impact.
- **History:**
  - v12: found by the certification pass — requirements, convergence 1, verdict plausible. Behavior matches code exactly: CreateOrderRequestValidator (lines 18-49) requires full address+recipient+phone for Easybox, identical to Courier. But this is coordinated, intentional, and documented in-code: InvoiceAddressFormatter.EnsureBuyerAddressUsable mandates StreetName/CityName/PostalZone for any invoice, and the frontend (delivery-step.ts) already shows one shared "Adresa de facturare" form for Easybox with an explicit user hint explaining why. Nothing here is a wrong runtime result — it's a real, cross-stack, rationale-documented requirement of the invoicing feature. The "no story/AC" complaint is a documentation-process gap, not a functional bug, so no failing execution or regression test exists to construct. Guard hunt: No guard found. Original story memory-bank/intents/014-payment-hardening/units/001-shipping-cost-server-side/stories/002-create-order-validator.md (bolt 034) only required EasyboxLockerId for Easybox, not a full address. Bolt 039 ddd-02-technical-design.md:249 notes invoicing needs order.ShippingAddress but never mandates tightening the Easybox validator, and decision-index.md has no entry on it. Only justification is the inline comment at CreateOrderRequestValidator.cs:38 — no story/AC/decision record.

### PPW-634 — Lazy creation of a fiscal-year invoice sequence is completely silent

- **What:** Vat:InvoiceSeries is edited to a typo, or the year rolls to 2027. EnsureAsync creates invoice_seq_<series>_<year> starting at 1 with no log line, and the DO block swallows the concurrent-create race in SQL. Numbering restarts from 000001 on a new series with zero operator signal that a new sequence was born.
- **Evidence:** `Data/PostgresSequences.cs:23`; `Data/PostgresSequences.cs:11-36`, `Services/Invoicing/PostgresInvoiceNumberingService.cs:27-54`, `Services/Invoicing/InvoiceCreationService.cs:74-99`, `Configuration/VatSettings.cs:21`, `Validators/VatSettingsValidator.cs:26-29`, `appsettings.json:79`
- **Suggested fix:** Have EnsureAsync report whether it created the sequence (e.g. RETURNING/xmax check) and log invoice.numbering.sequence-created at Warning with the name.
- **History:**
  - v12: found by the certification pass — observability, convergence 1, verdict confirmed. Fix-generated by PPW-569. Confirmed by reading the code. VatSettings.InvoiceSeries (appsettings, validator only checks regex, no allow-list) plus issuedAt.Year build seqName in PostgresInvoiceNumberingService.NextNumberAsync (line 36), which calls PostgresSequences.EnsureAsync (line 38) with no logging around it. EnsureAsync itself (lines 23-35) is a static method with no ILogger, runs a DO block that creates the sequence IF NOT EXISTS and swallows the concurrent-create race via the duplicate_table/duplicate_object/unique_violation handler — no log line anywhere in this path. Only an out-of-range value (line 46) is logged; first-ever nextval() on a brand-new series/year silently returns 1. A typo'd Vat:InvoiceSeries or a year rollover therefore mints a fresh sequence with zero dedicated signal — the only trace is the generic "number-attempted" log in InvoiceCreationService showing a reset invoice_number, easy to miss.

### PPW-635 — Polly retry pipeline has no OnRetry logging, so a degrading ANAF is invisible

- **What:** ANAF intermittently returns 429/503 and succeeds on the second attempt. All three retries with 1s/2s/4s backoff happen with no log line and no metric, so every call quietly takes seconds longer and the degradation is only visible once it fails outright.
- **Evidence:** `Services/Invoicing/Anaf/AnafResilienceHandler.cs:33`; `Services/Invoicing/Anaf/AnafResilienceHandler.cs:30-52`, `Program.cs:282-307`, `Extensions/ObservabilityExtensions.cs:94-102`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:302-474`
- **Suggested fix:** Add OnRetry to the RetryStrategyOptions logging anaf.spv.retry with attempt number, status code and delay at Warning (or Information).
- **History:**
  - v12: found by the certification pass — observability, convergence 1, verdict confirmed. ANAF returns 503, 503, 200. Pipeline built at AnafResilienceHandler.cs:32-43 has ShouldHandle but no OnRetry delegate, so retries 1 and 2 fire with 1s/2s delay and emit zero log lines or metric increments from this class. No collaborator fills the gap: DI wiring (Program.cs:291-303) attaches no logger/meter to the pipeline, ObservabilityExtensions.cs never registers a Polly meter/ActivitySource, and InvoiceUploadJob only records MetricNames.AnafStatusValues after the overall call resolves — never per attempt. Call succeeds on attempt 3 with no trace the first two failed.

### PPW-636 — A garbage HTTP 200 body is reported as the same unreachable incident as a network outage

- **What:** A proxy or captive portal returns an HTML error page with HTTP 200. SafeParse throws AnafUnreachableException with HttpStatus null, and the job logs anaf.upload-job.unreachable status= (empty) — byte-identical to a DNS or connect failure. The operator investigates ANAF instead of the network path in front of it.
- **Evidence:** `Services/Invoicing/Anaf/AnafSpvClient.cs:179`; `Services/Invoicing/Anaf/AnafSpvClient.cs:64-71`, `Services/Invoicing/Anaf/AnafSpvClient.cs:172-183`, `Services/Invoicing/Anaf/AnafSpvClient.cs:52-55`, `Services/Invoicing/Anaf/AnafExceptions.cs:44-59`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:355-363`
- **Suggested fix:** Use a distinct exception or a reason field (transport / http_status / unparseable_body) and log the response content-type plus first-bytes length, never the body.
- **History:**
  - v12: found by the certification pass — observability, convergence 1, verdict confirmed. Proxy/captive portal returns HTTP 200 with an HTML body. AnafSpvClient.UploadAsync (line 70) sees IsSuccessStatusCode=true, so it proceeds to SafeParse(body,endpoint). XDocument.Parse throws XmlException on the HTML (e.g. unescaped &, DOCTYPE), and SafeParse's catch (line 179-182) throws AnafUnreachableException(endpoint, inner: ex) with httpStatus left at its default null. That is the exact same exception type/shape as the HttpRequestException path (line 52-55) used for a real DNS/connect failure, which also passes httpStatus:null. InvoiceUploadJob's catch (line 355-363) logs "anaf.upload-job.unreachable ... status={HttpStatus}" for both, producing an identical log line regardless of cause.

### PPW-637 — Unhandled-Stripe-event line is LogDebug under an Information floor, so it never emits

- **What:** A new Stripe event type (e.g. charge.dispute.created) is enabled in the dashboard but not handled here. Serilog MinimumLevel.Default is Information in every environment, so the Debug line is filtered out; the request logs a plain 200 and nobody can tell the event was silently dropped. ExceptionHandlerMiddleware documents this exact trap.
- **Evidence:** `Controllers/WebhooksController.cs:149`; `Controllers/WebhooksController.cs:138-151`, `appsettings.json:176-183`, `appsettings.Development.json:47-55`
- **Suggested fix:** Raise to LogInformation, matching the request.client_aborted precedent in ExceptionHandlerMiddleware.
- **History:**
  - v12: found by the certification pass — observability, convergence 1, verdict confirmed. Stripe fires a new event type, e.g. charge.dispute.created. StripeWebhookAsync verifies the signature, extracts eventType, hits the switch default at line 148-150: only `_logger.LogDebug("Unhandled Stripe event type: {Type}", eventType)` runs, no RecordPaymentWebhook call, then returns Ok() (200). Both appsettings.json and appsettings.Development.json set Serilog MinimumLevel.Default to "Information" with no override for this controller/namespace, so the Debug call is filtered before it reaches any sink. Result: a 200 response, zero log line, zero metric — the dropped event is invisible.

### PPW-638 — Fulfilment ZIP entry name interpolates an unsanitized product name

- **What:** A product named "Foto 10x15 / lucios" (slashes are legal — no charset rule on Product.Name) yields entries "001_Foto 10x15 / lucios.jpg", so the ZIP contains nested directories instead of flat print files; a name with ".." makes unzip/7-Zip refuse or warn on the whole archive.
- **Evidence:** `Services/AdminOrderService.cs:249`; `Services/AdminOrderService.cs:249`, `Services/OrderService.cs:87-92`, `Models/Product.cs:6`, `Validators/Admin/CreateProductRequestValidator.cs:10-12`, `Models/ProductSnapshot.cs:5`
- **Suggested fix:** Sanitize the entry name — strip path separators, ".." and control chars, cap length — or drop the name entirely and use a fixed "NNN_<uploadId>.<ext>" scheme.
- **History:**
  - v12: found by the certification pass — input-validation, convergence 1, verdict confirmed. Admin creates a product with Name="Foto 10x15 / lucios" — CreateProductRequestValidator only enforces NotEmpty+MaxLength(200), no charset check, so it saves. OrderService.cs:89 copies ci.Product.Name verbatim into ProductSnapshot.ProductName at checkout. AdminOrderService.cs:249 builds entryName = "001_Foto 10x15 / lucios.jpg" and passes it to ZipArchive.CreateEntry, which treats "/" as a path separator, writing a nested directory entry instead of a flat file — contradicting the intended flat per-item layout.

### PPW-639 — Upload quota is enforced for guests only; registered users are uncapped

- **What:** Self-registration is open and the API configures no rate limiting at all. A registered attacker loops POST /api/uploads/batch with 50 MB JPEGs (500 MB per request); MaxUploadsPerSession=100 is inside `if (guestSessionId.HasValue)`, so originals accumulate until disk / object-store cost blows up.
- **Evidence:** `Services/UploadService.cs:67`; `Services/UploadService.cs:67-75`, `Controllers/UploadsController.cs:102-155`, `Extensions/SecurityExtensions.cs:56-102`, `Configuration/RateLimitSettings.cs:1-14`
- **Suggested fix:** Apply MaxUploadsPerSession (or a per-user byte budget) to userId as well as guestSessionId, and add per-identity rate limiting on the upload endpoints.
- **History:**
  - v12: found by the certification pass — input-validation, convergence 1, hinted, verdict confirmed. Registered user (userId set, guestSessionId null) calls POST /api/uploads/batch repeatedly. In UploadService.UploadAsync, the count-and-cap block (lines 67-75) is gated by `if (guestSessionId.HasValue)`, which is false, so it's skipped entirely; no other check bounds uploads by userId. Each file is written via `_router.Local.SaveAsync` and a row persisted with no cap, so uploads accumulate without limit. Correction to the finding: SecurityExtensions.cs does add a global 100 req/min-per-IP rate limiter (contradicting "no rate limiting at all"), but it only throttles request rate, not accumulated storage — 100 requests/min x up to 500MB/batch is still unbounded growth over time.

### PPW-640 — /checkout/recapitulare has no delivery-complete guard and mislabels a null method as courier

- **What:** With a stale or empty fotoTipar_checkout state the user can open /checkout/recapitulare directly. method===null renders '🚚 Curier la domiciliu', shippingCostRon 0 makes Total equal Subtotal. They tick the terms, click Plătește acum, and payment-step's ngOnInit bounces them straight back to /checkout/livrare with no explanation.
- **Evidence:** `src/app/features/checkout/pages/review-step.ts:41`; `src/app/features/checkout/pages/review-step.ts:41-42`, `src/app/features/checkout/pages/review-step.ts:62,189-193`, `src/app/core/services/checkout-state.service.ts:7-13,43-48`, `src/app/features/checkout/pages/payment-step.ts:161-168`, `src/app/features/checkout/checkout.routes.ts:13-20`
- **Suggested fix:** Apply the same isDeliveryComplete() check in ReviewStep.ngOnInit (or a route guard) and render the method label from the actual value with an explicit unset case.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict confirmed. Fresh/cleared sessionStorage → CheckoutStateService.loadFromStorage returns INITIAL_STATE (method:null, shippingCostRon:0). No canActivate guard on 'recapitulare' route (checkout.routes.ts). User opens /checkout/recapitulare directly: line 41 ternary defaults null to '🚚 Curier la domiciliu'; Total=Subtotal (line 62). Ticks terms, clicks Plătește acum → proceed() navigates to /checkout/plata. PaymentStep.ngOnInit (payment-step.ts:163) calls isDeliveryComplete(), which returns false since method is null, and silently router.navigate(['/checkout/livrare']) with no error message shown.

### PPW-641 — No admin UI for the invoice list, ANAF retry, or UBL XML endpoints

- **What:** AdminInvoicesController exposes GET /api/admin/invoices, POST /{id}/retry and GET /{id}/xml, and the unconfirmed-upload counter can park an invoice as Failed. No admin route or service method references any of them, so recovering a Failed ANAF submission requires a hand-rolled HTTP call with an admin JWT.
- **Evidence:** `src/app/features/admin/admin.routes.ts:8`; `src/app/features/admin/admin.routes.ts:1-30`, `Controllers/AdminInvoicesController.cs:1`
- **Suggested fix:** Add an /admin/facturi page listing invoices by ANAF state with a retry button and an XML download.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict confirmed. Admin opens the SPA under /admin; admin.routes.ts defines only children '', comenzi, comenzi/:orderId, produse, stari-comenzi. An invoice fails ANAF submission (unconfirmed-upload counter parks it Failed). No route, component, or service in src/PhotoPrint.UI references /api/admin/invoices, /retry, or /xml (repo-wide grep for invoice/anaf/admin-invoices under the UI folder returns zero matches). The admin has no in-app page to see or retry the Failed invoice; only a raw HTTP call with an admin JWT works.

### PPW-642 — logout() resets returnUrl, so a mid-checkout token expiry dumps the user at the upload page

- **What:** A logged-in customer on /checkout/plata whose JWT expires gets a 401; error.interceptor calls auth.logout() then navigates to /auth/login. logout() sets returnUrl='/tipareste' and the interceptor never records the current URL, so after re-login the customer lands on the upload page rather than back at checkout, with no hint about what happened.
- **Evidence:** `src/app/core/services/auth.service.ts:179`; `src/app/core/services/auth.service.ts:174-180`, `src/app/core/interceptors/error.interceptor.ts:22-34`, `src/app/features/auth/pages/login/login-page.ts:51-59`, `src/app/core/guards/auth.guard.ts:19`
- **Suggested fix:** Have the interceptor call auth.setReturnUrl(router.url) after logout(), and stop logout() from overwriting a freshly set returnUrl.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict confirmed. Authenticated customer on /checkout/plata; JWT expires. API call 401s. error.interceptor.ts:24 auth.isAuthenticated() true, so it calls auth.logout() (never setReturnUrl with the current URL), then router.navigateByUrl('/auth/login'). logout() (auth.service.ts:179) explicitly resets returnUrl to '/tipareste'. On successful re-login, login-page.ts:56-58 reads auth.getReturnUrl() -> '/tipareste' and navigates there, not back to checkout.

### PPW-643 — Two unbounded subscriptions in ReviewStep.ngOnInit

- **What:** cartService.cart$ and checkoutState.deliveryState$ are subscribed without takeUntilDestroyed/DestroyRef. Navigating livrare -> recapitulare -> livrare repeatedly accumulates one live subscription per visit, each writing into a destroyed component's signals for the rest of the session.
- **Evidence:** `src/app/features/checkout/pages/review-step.ts:196`; `src/app/features/checkout/pages/review-step.ts:178-198`, `src/app/core/services/cart.service.ts:21-24`, `src/app/core/services/checkout-state.service.ts:15-19`, `src/app/features/checkout/checkout.routes.ts:10-15`
- **Suggested fix:** Pipe both through takeUntilDestroyed(inject(DestroyRef)) or replace them with toSignal.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict confirmed. Router nav livrare→recapitulare instantiates ReviewStep, ngOnInit subscribes to cart$ and deliveryState$ (root BehaviorSubjects, never complete). Nav back to livrare destroys ReviewStep with no ngOnDestroy/takeUntilDestroyed, so the subscription lives on. Repeating the round-trip N times stacks N live subscriptions, each still invoking c => this.cart.set(c) against a detached component instance whenever cart or delivery state next emits.

### PPW-644 — Order ZIP blob URL is revoked synchronously after click, which can abort the download

- **What:** downloadZip creates an <a>, clicks it, removes it and immediately calls URL.revokeObjectURL in the same tick. Firefox and Safari have historically not committed the download before the URL is revoked, so the admin's order archive download silently produces nothing and the error handler never fires (the HTTP call succeeded).
- **Evidence:** `src/app/core/services/admin.service.ts:92`; `src/app/core/services/admin.service.ts:81-95`
- **Suggested fix:** Defer the revoke (setTimeout ~1s or requestAnimationFrame) and keep the anchor in the DOM until then.
- **History:**
  - v12: found by the certification pass — frontend-ux, convergence 1, verdict confirmed. admin.service.ts:81-95 downloadZip: tap() runs synchronously after the blob HTTP response — creates `<a>`, sets href=createObjectURL(blob), appends, calls a.click(), removeChild, then revokeObjectURL(a.href) all in the same call stack/tick. click() on an anchor with `download` triggers the browser's save-to-disk path, which on some engines resolves the blob: URL asynchronously (after current task/microtask). Revoking immediately races that resolution: the URL can be invalidated before the browser has read the blob data, so the save silently fails/produces a 0-byte file, and since the Observable already emitted via map(()=>undefined), no error surfaces to the caller.

### PPW-645 — A DDL DO-block runs before every number allocation instead of once per series/year

- **What:** Every paid webhook and every checkout order pays two DB round-trips (CREATE SEQUENCE IF NOT EXISTS wrapped in a DO block, then nextval) inside the payment transaction, though the sequence can only be missing on the first allocation of a series-year. OrderNumberService.cs:30 does the same per order.
- **Evidence:** `Services/Invoicing/PostgresInvoiceNumberingService.cs:38`; `Services/Invoicing/PostgresInvoiceNumberingService.cs:38`, `Data/PostgresSequences.cs:11-36`, `Services/OrderNumberService.cs:30`, `Services/Invoicing/InvoiceCreationService.cs:77`, `Services/OrderService.cs:150`
- **Suggested fix:** Cache the ensured sequence names per process (MemoryCacheOnceRegistry already exists) or call EnsureAsync only after nextval fails with undefined_table, so the steady state is one round-trip.
- **History:**
  - v12: found by the certification pass — quality, convergence 1, verdict confirmed. PostgresInvoiceNumberingService.NextNumberAsync (line 38) and OrderNumberService.GenerateAsync (line 30) call PostgresSequences.EnsureAsync unconditionally on every invocation, with no static/instance cache of already-ensured names. EnsureAsync always executes the DO $$ CREATE SEQUENCE IF NOT EXISTS ... END $$ block via ExecuteSqlRawAsync, then the caller runs a second query for nextval. So the 100th invoice for series 'ft' year 2026 still pays the DO-block round trip before nextval, even though the sequence was created on the 1st call.

### PPW-646 — Polling loads the whole invoice row, including XmlPayload, to read two fields

- **What:** With MaxBatchSize=50 Submitted invoices, every poll tick pulls 50 full rows — each carrying the entire UBL XML text column — over the wire, while only AnafUploadId and CreatedAt are used. The batch query two screens earlier deliberately projects three columns; this one does not.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:421`; `Services/Invoicing/Anaf/InvoiceUploadJob.cs:98-104`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:414-423`, `Services/Invoicing/Anaf/InvoiceUploadJob.cs:462-467`, `Models/Invoice.cs:41`, `Data/PhotoPrintDbContext.cs:405`
- **Suggested fix:** Project: `.Select(i => new { i.AnafUploadId, i.CreatedAt })` and pass CreatedAt into IsBudgetExhausted instead of the entity.
- **History:**
  - v12: found by the certification pass — quality, convergence 1, verdict confirmed. Seed 50 Invoices with AnafStatus=Submitted, non-null AnafUploadId, and large XmlPayload strings. Run one poll tick: ProcessBatchAsync's projected batch query (line 98-104) returns 50 rows cheaply, but the foreach dispatches each to PollSubmittedAsync, whose query at line 421-422 (`db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == invoiceId, ct)`) has no `.Select`, so EF Core generates SELECT * and materializes the whole entity including XmlPayload — yet only AnafUploadId (line 423) and CreatedAt (line 465, via IsBudgetExhausted) are read. 50x unnecessary XML-column transfer per tick, confirmed by contrast with the batch query's explicit 3-column projection two screens earlier.

### PPW-647 — AddInvoiceUnknownUploadOutcomes leaves a permanent DEFAULT 0 that the model does not declare

- **What:** AddColumn's defaultValue:0 persists as a column DEFAULT in Postgres, but the model and snapshot declare none (unlike StorageLocation, which uses HasDefaultValue). This is the only model-vs-migration divergence in the whole chain; anyone diffing the live schema against the model sees a phantom default, and EF will never scaffold the DROP DEFAULT because its diff is snapshot-based.
- **Evidence:** `Migrations/20260821110018_AddInvoiceUnknownUploadOutcomes.cs:18`
- **Suggested fix:** Either add .HasDefaultValue(0) to the model so the snapshot records it, or append migrationBuilder.Sql to ALTER COLUMN "UnknownUploadOutcomes" DROP DEFAULT after the backfill.
- **History:**
  - v12: found by the certification pass — db-parity, convergence 1, verdict unverified-cleanup. Fix-generated by PPW-559

### PPW-648 — The VAT rounding-mode test mostly asserts decimal.Round's own behaviour and never pins the net-side mode

- **What:** Four of the five assertions in Rounding_uses_AwayFromZero... call decimal.Round directly, testing .NET rather than VatCalculator. Only the last line exercises the code. Dropping MidpointRounding.AwayFromZero from the net rounding (grossTotalRon - vat) reddens nothing, since no case makes ToEven and AwayFromZero disagree there.
- **Evidence:** `Tests/Unit/Services/VatCalculatorTests.cs:57`
- **Suggested fix:** Delete the decimal.Round-only assertions and add a (gross, rate) case where the net rounding itself sits on a midpoint the two modes resolve differently.
- **History:**
  - v12: found by the certification pass — tests-coverage, convergence 1, verdict unverified-cleanup

### PPW-649 — metrics.md still marks invoice_anaf_status_total as future and never incremented

- **What:** At this commit InvoiceUploadJob increments the meter, yet metrics.md:29 says increment sites are still to come, the label section is headed "future", and line 106 uses "a declared-but-never-incremented metric (invoice_anaf_status_total today)" as its worked example — so an operator verifying the ANAF SLO panel is told the metric cannot be emitting.
- **Evidence:** `memory-bank/operations/metrics.md:69`
- **Suggested fix:** Drop the "future" markers, note the increment sites in InvoiceUploadJob, and pick a different never-incremented example for the dashboard caveat.
- **History:**
  - v12: found by the certification pass — requirements, convergence 1, verdict unverified-cleanup
  - v13: fixed @`6977d5b` — metrics.md drops the "future" marker, documents the new `retrying` value and corrects the series counts
  - v13: verified by reading — a metrics.md row has nothing to redden; it shares its commit with PPW-600, whose tests carry the code half

### PPW-650 — Story 001's AC to document shipping as VAT-inclusive in decision-index.md is not done

- **What:** Shipping is folded into the gross before VAT extraction, so the AC's literal example (VAT on subtotal only, TotalRon = S + shipping) no longer holds and per-line reconciliation depends on the choice. decision-index.md carries no entry, so a reviewer scanning "Read when" lines for VAT scope finds nothing and may "fix" the composition.
- **Evidence:** `Services/VatCalculator.cs:14`
- **Suggested fix:** Add a short decision-index entry for shipping treated as VAT-inclusive at the goods rate, pointing at VatCalculator and the XML builder's shipping line.
- **History:**
  - v12: found by the certification pass — requirements, convergence 1, verdict unverified-cleanup

### PPW-651 — Both admin retry-refusal branches log nothing despite the class's audit-logged claim

- **What:** An admin clicks retry at the moment the worker picks the row up; RetryAsync's CAS loses and a 409 invoice-cas-lost is returned. Only the success path logs admin.invoice.retry, so a repeated admin-vs-worker race leaves no trace to diagnose.
- **Evidence:** `Controllers/AdminInvoicesController.cs:123`
- **Suggested fix:** Log admin.invoice.retry-refused with admin_user_id, invoice_id and reason (not-retryable vs cas-lost) before each Conflict return.
- **History:**
  - v12: found by the certification pass — observability, convergence 1, verdict unverified-cleanup

### PPW-652 — Paid webhook spends two extra round-trips re-loading order relations it could have Included

- **What:** Every successful payment: GetByPaymentIntentIdAsync loads the order with User, then FireOrderConfirmedEmailAsync issues a separate query for Items and another for EasyboxLocker — three round-trips on the payment hot path where one Include chain would do.
- **Evidence:** `Controllers/WebhooksController.cs:402`
- **Suggested fix:** Add a service query that Includes User, Items and EasyboxLocker for the webhook path, and drop LoadOrderDetailsForEmailAsync.
- **History:**
  - v12: found by the certification pass — quality, convergence 1, verdict unverified-cleanup

### PPW-653 — Duplicated ANAF status triage with a provably dead branch, repeated in both client methods

- **What:** The `>= 500 || RequestTimeout` branch throws `new AnafUnreachableException(endpoint, httpStatus: …)` and the very next `!IsSuccessStatusCode` branch throws the identical exception with identical arguments, so the first branch can never change any outcome. The same four-check block is copy-pasted at line 128.
- **Evidence:** `Services/Invoicing/Anaf/AnafSpvClient.cs:67`
- **Suggested fix:** Extract one `EnsureUsableResponse(response, endpoint)` helper containing the 401 check and a single non-success throw; call it from both methods.
- **History:**
  - v12: found by the certification pass — quality, convergence 1, verdict unverified-cleanup

### PPW-654 — Migration hardcodes invoice_seq_ft_2026, duplicating a name the service derives from config

- **What:** Vat:InvoiceSeries is set to anything but FT, or the year rolls to 2027: the seeded sequence is never used and the lazy EnsureAsync creates the real one, so the migration ships a permanently empty object plus a second place that encodes the naming rule.
- **Evidence:** `Migrations/20260820133204_InitialPostgres.cs:746`
- **Suggested fix:** Drop the seed CREATE/DROP SEQUENCE from the migration — PostgresSequences.EnsureAsync already creates it on first use — or derive the name from one shared helper.
- **History:**
  - v12: found by the certification pass — quality, convergence 1, verdict unverified-cleanup

### PPW-655 — Runtime Math.Max clamps duplicate ANAF ranges the settings validator already enforces, with a divergent floor

- **What:** The job is only registered when Anaf:Enabled, exactly when the validator rejects PollIntervalMinutes<1, ClaimTtlMinutes<2 and MaxUnknownUploadOutcomes<1 at boot, so all three clamps are unreachable. Worse, line 96 re-derives the cooldown as Math.Max(2, PollIntervalMinutes) instead of reusing _pollIntervalMinutes, so one setting yields two different derived intervals.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:41`
- **Suggested fix:** Delete the clamps and rely on the validated options; compute the cooldown from the single _pollIntervalMinutes field.
- **History:**
  - v12: found by the certification pass — quality, convergence 1, verdict unverified-cleanup

### PPW-656 — Third copy of the mandatory-address field list in checkout-state.service.ts

- **What:** hasFiscalAddress() hardcodes seven required fields; delivery-step's addressForm validators encode the same set, and CreateOrderRequestValidator a third time. Making one field optional (or adding one) requires three edits, and missing one lets the stepper unlock payment for a state the server will 400.
- **Evidence:** `src/app/core/services/checkout-state.service.ts:51`
- **Suggested fix:** Export one REQUIRED_ADDRESS_FIELDS list from the shipping model and drive both the form validators and hasFiscalAddress() from it.
- **History:**
  - v12: found by the certification pass — quality, convergence 1, verdict unverified-cleanup

### PPW-657 — Lens manifest omits three changed files and names one that did not change

- **What:** The manifest lists only two migrations, so the third (AddInvoiceUnknownUploadOutcomes, the DDL behind the park-as-Failed counter) is owned by no lens, as are Tests/Helpers/UncommittedRelationCreator.cs and Integration/PostgresSequencesTests.cs. It also names review-step as changed when it is not, so a lens sent there finds nothing and moves on.
- **Evidence:** `Migrations/20260821110018_AddInvoiceUnknownUploadOutcomes.cs:1`
- **Suggested fix:** Regenerate the manifest from the diff file list, and assign the third migration to whichever lens verifies boot-time DDL against Postgres.
- **History:**
  - v12: found by the certification pass — completeness-critic, convergence 1, verdict unverified-cleanup

### PPW-659 — Not-yet-due Rejected invoices fill the upload batch and starve Pending uploads

- **What:** Seller CUI is wrong, so ANAF rejects everything. 50+ Rejected rows exist. The coarse filter admits any Rejected row with UpdatedAt older than MinBackoffHours (1h), but real due slots are +5h/+21h/+85h. Ordered by CreatedAt, those 50 oldest rows fill Take(MaxBatchSize=50) every tick, each only logging "rejected-not-due" — no Pending invoice is ever uploaded for up to 85h.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:103`
- **Suggested fix:** Compute due-ness in SQL (CreatedAt + cumulative backoff <= now) instead of MinBackoffHours, or fetch Rejected rows in a separate slice with its own cap so they cannot consume the Pending budget. **Trace:** Real. Defaults BackoffHours [1,4,16,64], MaxBatchSize 50, interval 30min. Invoice created T0, rejected at T0+21.5h (MarkRejectedAsync stamps UpdatedAt); NextResubmitAt returns T0+85h, but the coarse filter admits it from T0+22.5h. Not-due path only LogDebug-returns, leaving UpdatedAt untouched, so the row re-enters every tick. 50 such older rows, OrderBy(CreatedAt), fill Take(50); a Pending invoice created later never enters the batch for ~62h. **Test shape:** NotDueRejectedRowsDoNotStarvePending: seed 50 Rejected invoices CreatedAt=T0, UpdatedAt=T0+21.5h, plus one Pending created T0+22h; clock T0+25h; run one tick; assert IAnafSpvClient.UploadAsync called for the Pending invoice.
- **History:**
  - v15: found by the delta pass — correctness, convergence 1, verdict confirmed
  - v15: fixed @`9527eba` — rejections are a separate slice capped at `MaxBatchSize / 10` and ordered by oldest transition, so they cannot crowd out Pending uploads. Re-merging the two queries reddens the new test
  - v16: verified — re-merging the two batch queries reddened the starvation test, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-660 — A succeeded webhook on an order already moved to PaymentFailed leaves the customer charged and the order unfulfillable

- **What:** Card declined -> payment_intent.payment_failed sets order PaymentFailed. The customer re-clicks 'Plătește acum' (payment-step reuses the same clientSecret) with a good card; it succeeds. payment_intent.succeeded finds status PaymentFailed, not AwaitingPayment, so it only logs. PaymentFailed->Paid is not in OrderStatusMachine, so even admin mark-paid throws. Cart already cleared; confirmation page shows 'Comanda nu a fost găsită'.
- **Evidence:** `Controllers/WebhooksController.cs:242`
- **Suggested fix:** Allow PaymentFailed -> Paid in OrderStatusMachine and treat succeeded-on-PaymentFailed as a normal Paid transition (invoice + side effects), rather than a log-only dead end. **Trace:** Real. Decline: payment_step keeps the same clientSecret and mounted card, button re-enabled (payment-step.ts:208), while payment_failed sets PaymentFailed (Webhooks:242). Second confirm with a good card succeeds; Stripe reuses the PI. succeeded handler: HasBeenPaid false, Status != AwaitingPayment, so it only logs (Webhooks:190-217). No PaymentFailed->Paid edge exists (OrderStatusMachine:18-27), so nothing recovers. Cart cleared, confirmation shows the not-found text (confirmation-page.ts:271). **Test shape:** WebhooksControllerTests.StripeSucceeded_AfterPaymentFailed_MarksOrderPaid: arrange order Status=PaymentFailed with PaymentIntentId pi_1; act POST /api/webhooks/stripe payment_intent.succeeded for pi_1; assert Status==Paid, PaidAt set, invoice created. Reddens today (stays PaymentFailed).
- **History:**
  - v15: found by the delta pass — race, convergence 1, verdict confirmed
  - v15: fixed @`c60260c` — `PaymentFailed → Paid` is legal and the succeeded webhook treats it like `AwaitingPayment`, so a second card that works completes the order instead of logging over a real charge
  - v16: verified without a revert proof — covered by the new webhook test plus the retargeted transition tests; a revert would have to undo the transition table, which the retargeted tests already pin from both directions. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-661 — Checkout idempotency key is never retired after a paid order, so the next checkout is redirected to the old order and the new basket deleted

- **What:** Guest pays; the settling screen says "you may close the page", so they do — attempts.clear() never runs. Same day they build a new basket and open /checkout/plata. The stored key is reused; the server 409s with the OLD paid order id; payment-step routes there; the confirmation page shows "Comandă confirmată #old" and clearCart() deletes the new basket. Nothing was ordered.
- **Evidence:** `src/app/core/services/checkout-attempt.service.ts:49`
- **Suggested fix:** Retire the key as soon as confirmCardPayment succeeds (keep only orderId for the settle wait), so the next checkout mints a fresh key. **Trace:** Real. Guest pays: payWithStripe navigates to confirmation without clearing; only a settled read clears, so closing the settling page leaves the key stored (24h TTL, same guest fingerprint). New basket, /checkout/plata reuses it. OrderService.cs:118 throws IdempotencyKeyConsumedException for a fresh non-AwaitingPayment/non-Failed holder before any divergence check, so 409 carries the old paid order id; handleIntentError navigates there; confirmation clears cart. New basket gone, nothing ordered. **Test shape:** payment-step.spec: "reused key after a paid order starts a fresh attempt". Arrange stored attempt (orderId set, <24h) + createStripeIntent stubbed 409 {orderId:'old'}. Act createIntent. Assert no navigate to /comanda/old, retry sent with a new key.
- **History:**
  - v15: found by the delta pass — frontend-ux, convergence 1, verdict confirmed
  - v15: fixed @`2302bec` — the key is retired as soon as the card is confirmed, keeping only the order id for the settle wait. Dropping the `retired` check reddens the new spec
  - v16: verified — dropping the `retired` check reddened the retire-key spec, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-662 — Retry after a declined card reuses the same client secret whose order the failure webhook already moved to PaymentFailed

- **What:** Card declined: confirmCardPayment returns error and Stripe's payment_intent.payment_failed sets the order PaymentFailed. The user retypes the card and clicks "Plătește acum" again — same clientSecret, succeeds, money taken. The succeeded webhook cannot do PaymentFailed→Paid (OrderStatusMachine), so it logs "manual reconciliation required"; the confirmation page shows "Comanda nu a fost găsită".
- **Evidence:** `src/app/features/checkout/pages/payment-step.ts:208`
- **Suggested fix:** After result.error, discard clientSecret/cardElement and force a fresh createIntent (new attempt key) before allowing another confirm. **Trace:** Decline: confirmCardPayment returns error; payment-step.ts:206-211 only sets a message — stripeLoading false, stripeReady true, clientSecret unchanged, so the pay button stays live. Stripe's payment_failed webhook flips the order AwaitingPayment→PaymentFailed (WebhooksController.cs:242). Declined intents stay requires_payment_method, so a second click on the same secret with a fresh card succeeds and charges. The succeeded handler sees PaymentFailed (not AwaitingPayment, not paid) and only logs "manual reconciliation required" (:206-212); PaymentFailed→Paid isn't in OrderStatusMachine. Confirmation page shows "nu a fost găsită". **Test shape:** payment-step.spec.ts "second attempt after a decline does not reuse the dead client secret": stub confirmCardPayment to return {error}, click pay twice; assert createStripeIntent ran again and the second confirm got the new secret.
- **History:**
  - v15: found by the delta pass — frontend-ux, convergence 1, verdict confirmed
  - v15: fixed @`2302bec` — a decline discards the client secret and unmounts the card, so a retry must mint a fresh key and intent. Removing the discard call reddens the new spec
  - v16: verified — removing the discard call reddened the dead-secret spec, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-663 — EuPlatesc columns removed by editing the already-applied baseline migration, so existing databases keep Orders.PaymentProcessor NOT NULL

- **What:** Verified on the dev DB: __EFMigrationsHistory holds 20260820133204_InitialPostgres and Orders."PaymentProcessor" is text NOT NULL, no default. Migrate() at boot skips the edited baseline, and the model no longer maps the column, so the first checkout INSERT (and --seed-dev) dies with 23502 not-null violation. Checkout is dead in every already-migrated environment.
- **Evidence:** `Migrations/20260820133204_InitialPostgres.cs:216`
- **Suggested fix:** Restore the baseline's Up() and add a forward migration that drops PaymentProcessor, EuPlatescTransactionId and EuPlatescRedirectUrl; add a test that migrates from the old baseline, not just a fresh DB. **Trace:** Constructed and confirmed against the live dev DB. `psql` shows `Orders."PaymentProcessor"` = text NOT NULL, no default, and `__EFMigrationsHistory` holds only `20260820133204_InitialPostgres` — so `Migrate()` (Program.cs:329) skips the edited baseline; the two pending migrations touch only Invoices. Grep finds zero `PaymentProcessor` in src/, so the EF INSERT at OrderService.cs:171 (and DevDataSeed.cs:166) omits it → Postgres 23502. Checkout dead on every migrated DB. **Test shape:** Relational test: apply chain on PostgresTestDatabase, `ALTER TABLE "Orders" ADD "PaymentProcessor" text NOT NULL` (legacy state), run `Migrate()`, then insert an Order — reddens with 23502 until a DropColumn migration exists.
- **History:**
  - v15: found by the delta pass — db-parity, convergence 1, verdict confirmed
  - v15: fixed @`de1b70d` — the applied baseline is restored and a forward `DropEuPlatescColumns` migration drops the three columns. Confirmed on the dev database before the change: history held only the baseline, `Orders."PaymentProcessor"` was `text NOT NULL` with no default, and the model no longer mapped it. The new chain test starts from that legacy state and reddens when the drop is emptied
  - v16: verified — emptying the drop migration's `Up` reddened the legacy-state chain test, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-664 — Automatic rejection-resubmit nulls PdfStoragePath, revoking the customer's invoice

- **What:** Invoice rejected by ANAF but PDF already rendered and downloadable. At the 1h backoff slot ResubmitRejectedAsync calls RetryAsync, which now sets PdfStoragePath=null; GET /api/orders/{id}/invoice returns 404 ("Factura se pregătește") until the worker re-renders on a later tick — 30 min at default PollIntervalMinutes, longer while ANAF is unreachable. Repeats at every backoff slot.
- **Evidence:** `Services/Invoicing/InvoiceLifecycle.cs:200`
- **Suggested fix:** Keep PdfStoragePath on the worker's automatic resubmit (clear it only on the admin retry endpoint), or re-render the PDF before flipping the row back to Pending. **Trace:** Real. UploadPendingAsync renders and stores the PDF (step 2) before submitting (step 3), so a Rejected row always has PdfStoragePath and is downloadable — InvoicesController checks ownership and path only, never status. At the due backoff slot ResubmitRejectedAsync calls RetryAsync(Rejected), which nulls PdfStoragePath and LastError. The row re-enters the next Pending batch, so the customer sees 404 / "Factura se pregătește" for up to one poll interval (30 min default), repeating each slot, plus a duplicate PDF-ready notification. **Test shape:** InvoiceUploadJobTests.RejectedResubmit_KeepsRenderedPdf: arrange Rejected invoice, PdfStoragePath="x", UpdatedAt past first backoff slot; act one job tick; assert status Pending and PdfStoragePath still "x".
- **History:**
  - v15: found by the delta pass — correctness, convergence 1, verdict confirmed
  - v15: fixed @`9527eba` — the worker resubmits through a new `RequeueRejectedAsync` that keeps `PdfStoragePath`; only the admin retry drops it. Swapping back to `RetryAsync` reddens the new test
  - v16: verified — swapping back to `RetryAsync` reddened the kept-PDF test, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-665 — Any non-2xx 4xx from ANAF maps to content-rejected and permanently parks the invoice as Failed

- **What:** A wrong Anaf:BaseUrl (404) or a scope-less token (403) makes UploadAsync throw AnafContentRejectedException, and InvoiceUploadJob parks the row Pending→Failed with "no number of retries changes the answer". There is no batch short-circuit like the auth one, so all 50 Pending invoices in the first tick go Failed and each needs a manual admin retry.
- **Evidence:** `Services/Invoicing/Anaf/AnafSpvClient.cs:74`
- **Suggested fix:** Restrict AnafContentRejectedException to 400/422 (ideally only with an ANAF error body); treat 403/404/405 as AnafUnreachableException, and short-circuit the batch after the first content rejection. **Trace:** Set Anaf:BaseUrl to a wrong path (404) or issue a scope-less token (403). AnafResilienceHandler.IsRetryable rejects both (only 5xx/408/429); AnafAuthHandler only refreshes on 401. AnafSpvClient.cs:74 falls through to AnafContentRejectedException. InvoiceUploadJob catches it, calls ParkUnbuildableAsync → Pending becomes Failed with the claim cleared. Only `authFailed` short-circuits the batch, so the loop repeats this for every Pending row up to MaxBatchSize=50, each needing the admin retry endpoint. **Test shape:** UploadJob_ParksEveryPendingRow_WhenAnafReturns404: arrange three Pending invoices + stub handler answering 404 to upload; act one tick; assert all three are Failed (expected: stay Pending, batch short-circuits after the first misconfiguration).
- **History:**
  - v15: found by the delta pass — correctness, convergence 1, verdict confirmed
  - v15: fixed @`5d84a5e` — only 400 and 422 are content rejections; 403, 404 and 405 read as unreachable, so one wrong setting no longer parks every Pending invoice
  - v16: verified — restoring the catch-all 4xx mapping reddened the misconfiguration theory, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-666 — OrderService frees the idempotency key on a fresh PaymentFailed order while its PaymentIntent is still chargeable

- **What:** Tab 1 gets order O1, card declines, webhook sets PaymentFailed; tab 1 still holds O1's StripeClientSecret. Tab 2 posts the same Idempotency-Key: the holder is fresh but PaymentFailed, so the key is freed and order O2 with a new PaymentIntent is created. Tab 1 retries with a good card and tab 2 pays — two charges, two orders, two invoices, no gateway-level dedupe (Stripe is keyed by order id).
- **Evidence:** `Services/OrderService.cs:129`
- **Suggested fix:** When freeing the key from a fresh PaymentFailed holder, also cancel/void that order's PaymentIntent and clear its StripeClientSecret so only one intent per basket stays confirmable. **Trace:** Real. WebhooksController:242 only flips status; no cancel exists on IStripePaymentGateway, so O1's PaymentIntent stays requires_payment_method and payment-step.ts:197 keeps clientSecret, re-confirming after a decline. OrderService:117 lets a fresh PaymentFailed holder fall to :129, freeing the key; O2 gets a new intent keyed by O2.Id (PaymentsController:82). Both cards charge. Correction: O1's later success hits the else at WebhooksController:208 — one invoice, not two. **Test shape:** OrderServiceIdempotencyTests.FreshPaymentFailedHolder_DoesNotFreeKey: arrange an order holding the key, Status=PaymentFailed, CreatedAt=now; act CreateFromCartAsync with the same key and caller; assert no second order is inserted (replay or 409). Today it creates O2.
- **History:**
  - v15: found by the delta pass — correctness + race, convergence 2, verdict confirmed
  - v15: fixed @`c60260c` — handing the key on from a fresh failed holder cancels that order's PaymentIntent and clears its client secret, so one basket cannot hold two confirmable intents
  - v16: verified without a revert proof — covered by the new abandon-intent test; the recording gateway asserts the cancel by id, which cannot pass without the call. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-667 — ANAF 429/503 counts as an unknown upload outcome and parks the invoice after 3 ticks

- **What:** AnafSpvClient maps 429 and 5xx to AnafUnreachableException, which the job records via RecordUnknownUploadOutcomeAsync. A 429 (or a maintenance 503) definitively filed nothing, yet three such ticks (~90 min at defaults) exhaust MaxUnknownUploadOutcomes=3 and park the row Failed with 'reconcile the invoice number in ANAF SPV'. Only a manual admin retry resumes it, inside the 5-business-day deadline.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:441`
- **Suggested fix:** Only count genuinely ambiguous outcomes (timeout, connection drop, unparseable 200) against the blind-repost budget; treat 429 and 503 as plain retryable outages via RecordPendingErrorAsync. **Trace:** Real. ANAF answers 429 (or 503) to every upload; Polly's 3 retries exhaust, client throws AnafUnreachableException(429). Job's catch calls RecordUnknownUploadOutcomeAsync, incrementing persisted UnknownUploadOutcomes. The cooldown makes the row eligible every other 30-min tick, so at t0, t0+60, t0+120 the third increment hits MaxUnknownUploadOutcomes=3 and ParkPendingAsFailedAsync sets Failed with the "reconcile in ANAF SPV" text. Nothing auto-resumes Failed; only the admin retry endpoint does. Timing ~2 h, not 90 min. **Test shape:** InvoiceUploadJobTests: "Rate_limited_upload_does_not_spend_blind_repost_budget" — stub IAnafSpvClient throwing AnafUnreachableException(httpStatus:429) on three due ticks; assert invoice stays Pending with UnknownUploadOutcomes 0, not Failed.
- **History:**
  - v15: found by the delta pass — race, convergence 1, verdict confirmed
  - v15: fixed @`5d84a5e` — 429 and 503 are refusals before ANAF reads anything, so they record a plain pending error instead of spending the blind-repost budget
  - v16: verified — making `FiledNothing` always false reddened the 429/503 theory, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-668 — Two concurrent same-key payment requests both call Stripe and one turns a 409 into a 500

- **What:** Customer opens the payment step in two tabs. Both send the same Idempotency-Key; one inserts the order, the other replays with StripeClientSecret still null and takes the replay-recovery path. Both then call CreatePaymentIntentAsync with order.Id as Stripe's idempotency key while the first is in flight. Stripe answers 409 idempotency_error; StripeException has no mapping in ExceptionHandlerMiddleware, so the tab gets a 500 and the generic 'Nu s-a putut crea sesiunea de plată'.
- **Evidence:** `Controllers/PaymentsController.cs:81`
- **Suggested fix:** Catch StripeException with type idempotency_error on the recovery path and re-read the order's persisted StripeClientSecret (or retry briefly) instead of letting it surface as a 500. **Trace:** Tab A saves the order (AwaitingPayment, secret null), then calls Stripe. Tab B, inside that window, has FindKeyHolderAsync return A's fresh AwaitingPayment row → ReplayOrConflict → WasIdempotentReplay with null secret → controller line 81 calls Stripe with the same order-id key while A is in flight → 409 idempotency_error. StripeException is absent from _exceptionMappings, so the else branch returns 500. Caveat: stripe-dotnet auto-retries 409s, masking short races; no double charge occurs. **Test shape:** Integration: CreateStripeIntent_WhenGatewayRejectsConcurrentIdempotentCall_DoesNotReturn500. Arrange existing AwaitingPayment order with null StripeClientSecret plus fake gateway throwing StripeException (409 idempotency_error); act POST same Idempotency-Key; assert 502/409, not 500.
- **History:**
  - v15: found by the delta pass — race, convergence 1, verdict confirmed
  - v15: fixed @`18f0b1c` — a gateway `idempotency_error` re-reads the persisted secret or answers 409, never the 500 that told the customer their basket was broken
  - v16: verified without a revert proof — covered by the new gateway-race path; no test double throws a real `StripeException` with an `idempotency_error` type, so this rests on the code path plus the compile-time filter, not on a red leg. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-669 — Post-commit webhook side effects are lost for good when one throws, because the retry hits the already-paid guard

- **What:** After the Paid+invoice commit, LoadOrderDetailsForEmailAsync (or the promoter/AWB enqueue) throws on a transient DB blip. The exception escapes the action, Stripe gets a 500 and retries; the retry sees HasBeenPaid, records Duplicate and returns before any side effect runs. The order-confirmation email is never sent and has no recovery sweep behind it.
- **Evidence:** `Controllers/WebhooksController.cs:201`
- **Suggested fix:** Wrap each post-commit side effect in its own try/catch that logs and continues, or drive them from a restartable outbox keyed on the order so a retry can replay only what is missing. **Trace:** Real. Paid+invoice commits at line 195. Then line 200/288 hits a transient failure (hub SendAsync, or Entry(order).Collection(Items).LoadAsync). Nothing catches it; the exception is unmapped, so ExceptionHandlerMiddleware returns 500. Stripe retries; line 183 HasBeenPaid(Paid) is true, records Duplicate, returns. FireOrderConfirmedEmail never ran, so no Pending email row exists for EmailRetryJob to pick up. Promotion and AWB do have sweeps (PromotionRecoveryScanner, AwbRetryJob); the email has none. **Test shape:** WebhooksController test: first payment_intent.succeeded delivery with hub or Items-load throwing once -> 500; replay same event; assert IOrderEmailService.FireOrderConfirmedEmail never invoked and metric records Duplicate.
- **History:**
  - v15: found by the delta pass — race, convergence 1, verdict confirmed
  - v15: fixed @`18f0b1c` — each post-paid side effect runs in its own try/catch with a Sentry capture. The outbox half is deliberately not done: a confirmation email lost to a transient fault stays lost, now visibly. Recorded in resolution-v15 Decisions
  - v16: verified — disabling the per-effect catch reddened the side-effect test, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-670 — One transient poll failure replaces the payment-submitted screen with "order not found"

- **What:** Payment submitted, page polling and showing "Plata a fost trimisă pentru comanda #X". Poll 3 fails (Wi-Fi blip, 500, or a guest 401 that also makes errorInterceptor clear the guest token). settling is set to null and order() is null, so the customer sees "Comanda nu a fost găsită sau nu a fost finalizată" plus a home link, while the payment is actually in flight.
- **Evidence:** `src/app/features/orders/pages/confirmation-page.ts:287`
- **Suggested fix:** Keep the last settling snapshot on a poll error (only the very first read may fall through to the not-found state) and show a retry link. **Trace:** Real. Read 1 returns AwaitingPayment and attempts.isWaitingFor is true, so settling is set and the "Plata a fost trimisă… #FT-20260001" panel renders. Poll 3 (t=6s) rejects; the error handler at line 287-290 unconditionally does settling.set(null) with order() still null and no retry, so the only remaining template branch is .state-error — "Comanda nu a fost găsită sau nu a fost finalizată" plus the home link — while the payment is still in flight. **Test shape:** confirmation-page.spec.ts: "keeps the settling panel when a later poll fails". Arrange AwaitingPayment + submitted, advance 0, assert .settling; switch getPaymentStatus to throwError(500), advance 3000; assert .settling still shown with FT-20260001 and .state-error null.
- **History:**
  - v15: found by the delta pass — frontend-ux, convergence 1, verdict confirmed
  - v15: fixed @`2302bec` — only the first read may fall through to not-found; a later failure keeps the settling panel and says the check failed
  - v16: verified — removing the settling guard reddened the later-poll-failure spec, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-671 — combinedStreetLength group error is never rendered, so Continue is disabled with no explanation

- **What:** Customer pastes a full address into "Strada" and adds block/apartment detail; street+number+block exceeds 150 chars. Every field control is individually valid, so no field-error appears, but addressForm.invalid makes canContinue() false — "Continuă →" is permanently greyed out with nothing on screen explaining why, and continue()/markAllAsTouched can never run. Length breaches on street/city/recipientName render "Câmp obligatoriu" on a filled field.
- **Evidence:** `src/app/features/checkout/pages/delivery-step.ts:384`
- **Suggested fix:** Render addressForm.errors.streetLineTooLong as a form-level message, and give maxlength breaches their own message instead of "Câmp obligatoriu". **Trace:** Pick Courier (costs loaded). Fill every field validly, street = 160 chars (control's maxLength is 255, so street is valid; number/block empty-or-short). combinedStreetLength joins parts → 160 > 150 → group error {streetLineTooLong}. statusChanges emits INVALID → addressValid() false → canContinue() false → "Continuă →" is [disabled], so continue()/markAllAsTouched never run. No control is invalid, so no @if field-error renders. grep confirms streetLineTooLong appears in no template. **Test shape:** delivery-step.spec: "shows a message when street+number+block exceed 150" — arrange Courier + valid address with 160-char street; act detectChanges; assert Continue disabled AND some .field-error text present (fails today).
- **History:**
  - v15: found by the delta pass — frontend-ux, convergence 1, verdict confirmed
  - v15: fixed @`2302bec` — the combined-street-line error renders as a form-level message, and an over-long field says so rather than claiming it is empty
  - v16: verified — disabling both error templates reddened the too-long-together spec, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-672 — Settle-poll setTimeout is never cancelled on destroy, so a late poll clears a newer basket

- **What:** Webhook lags; page is polling. Customer navigates to /tipareste and adds new photos. 3 s later the pending timer fires, the poll returns Paid, and the destroyed component runs checkoutState.reset() + cartService.clearCart() — a DELETE that wipes the freshly built basket. Up to 10 requests and signal writes also continue after destroy.
- **Evidence:** `src/app/features/orders/pages/confirmation-page.ts:282`
- **Suggested fix:** Clear the pending timeout in a DestroyRef.onDestroy callback (or use timer+takeUntilDestroyed), and gate the cart clear on attempts.isWaitingFor(orderId). **Trace:** Real. Poll pending at line 282; component has no OnDestroy/DestroyRef and never clears the timer. Guest on AwaitingPayment page (attempts.isWaitingFor true) navigates to /tipareste and adds photos. Within the remaining 30 s budget a timer fires, read() re-subscribes, webhook has landed so status is Paid, and the settled branch (262-267) runs unconditionally: checkoutState.reset() plus cartService.clearCart() — a DELETE that empties the server cart and localStorage of the new basket. **Test shape:** confirmation-page.spec: "does not clear a new cart after destroy" — arrange AwaitingPayment + isWaitingFor true, fixture.destroy(), tick(3000), flush Paid response; assert httpMock.expectNone(DELETE /cart) and clearCart spy not called.
- **History:**
  - v15: found by the delta pass — correctness + frontend-ux, convergence 2, verdict confirmed
  - v15: fixed @`2302bec` — the poll timer is cleared on destroy and the cart is only cleared for an order this browser was waiting on
  - v16: verified — removing the `clearTimeout` reddened the destroyed-page spec, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-673 — Invoice download uses a detached anchor and revokes the object URL in the same tick as click()

- **What:** Guest on Firefox clicks "Descarcă factura". The blob arrives, but link is never appended to the document and URL.revokeObjectURL runs synchronously after click(), so no file is saved and no error is shown — invoiceMessage stays null. This page is the guest's only route to a legally required invoice. admin.service.downloadZip already appends/removes the anchor.
- **Evidence:** `src/app/features/orders/pages/confirmation-page.ts:322`
- **Suggested fix:** Follow the existing pattern: document.body.appendChild(link), click, removeChild, and revoke the URL in a setTimeout/next tick. **Trace:** Confirmed code: line 319 builds an anchor never inserted, 322 clicks it, 323 revokes in the same tick. Guest clicks "Descarcă factura"; blob arrives; the anchor's download fetch is queued as a task, so the URL is already revoked when it runs — download aborts. invoiceLoading clears, invoiceMessage stays null, so nothing is shown. The detached-anchor half is browser-version dependent; the same-tick revoke and the silent failure are not. **Test shape:** confirmation-page.spec: "attaches the invoice link and revokes after the click" — stub URL.createObjectURL/revokeObjectURL, spy body.appendChild and anchor.click; call downloadInvoice with a blob; assert anchor was connected at click and revoke ran after, not same tick.
- **History:**
  - v15: found by the delta pass — correctness + frontend-ux, convergence 2, verdict confirmed
  - v15: fixed @`2302bec` — the anchor is attached before the click and the object URL revoked on the next tick, matching the pattern `admin.service.downloadZip` already used
  - v16: verified — reverting to the detached anchor reddened the download spec, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-674 — Admin cross-customer invoice read can be logged with an empty admin id

- **What:** Admin sends Authorization: Bearer <admin JWT> plus X-Guest-Token: <any valid guest GUID> to GET /api/orders/{victimOrder}/invoice. DualAuth authenticates both schemes and merges identities, so GetUserIdOrNull() returns null (guest_session_id claim present) while IsInRole("Admin") stays true: the PDF is served and the audit line reads admin_id= with no id.
- **Evidence:** `Controllers/InvoicesController.cs:72`
- **Suggested fix:** Take the admin id from the Bearer ClaimsIdentity (FindFirst on that identity) rather than GetUserIdOrNull, and/or refuse the admin override when a guest identity is also attached. **Trace:** Admin sends Bearer admin JWT + X-Guest-Token of any valid session, GET /api/orders/{victim}/invoice. The DualAuth policy authenticates both schemes; PolicyEvaluator merges identities into one principal. ClaimsPrincipalExtensions.cs:12 sees the guest_session_id claim and returns null, so userId=null; guestSessionId doesn't match the order, so owns=false. IsInRole scans all identities, finds the JWT's ClaimTypes.Role=Admin, so the PDF is served and line 72 logs admin_id= empty. **Test shape:** InvoicesControllerTests "AdminReadWithGuestTokenLogsAdminId": arrange principal holding both a JWT identity (NameIdentifier=adminId, Role=Admin) and a GuestToken identity (guest_session_id=other), act GetInvoiceAsync on a foreign order, assert captured log contains adminId.
- **History:**
  - v15: found by the delta pass — security, convergence 1, verdict confirmed
  - v15: fixed @`5d84a5e` — the audit line reads the admin id from the Bearer identity through a new `GetBearerUserIdOrNull`, so a request also carrying a guest token cannot log an empty one
  - v16: verified — reading the merged principal again reddened the admin-id test, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-675 — Pooled test database is reused without checking the migration chain actually applied

- **What:** CREATE DATABASE commits, then Migrate() throws (Ctrl+C on this machine mid-run, server restart, bad migration) and the database is left behind. The next run sees DatabaseExists, truncates nothing, and hands out a schema-less slot; every test fails 'relation "Orders" does not exist'. The sweep skips current-prefix names, so it never self-heals.
- **Evidence:** `Tests/Helpers/PostgresTestDatabase.cs:227`
- **Suggested fix:** Drop the database in LeaseSlot's catch, and on the reuse path assert GetPendingMigrations() is empty (or call Migrate()) before returning the lease. **Trace:** Real. Run 1: slot created (line 233), then Ctrl+C kills the process mid-Migrate (or during AwbCreatorTests, which drops "Orders" at line 236 and relies on Dispose to drop the DB). The database survives with no/partial schema. Run 2: same salt+fingerprint, so the sweep's `NOT LIKE current%` (line 289) skips it; DatabaseExists (227) takes the truncate path, whose pg_tables aggregate tolerates missing tables; the slot is handed out schema-less and every test throws relation "Orders" does not exist. **Test shape:** LeaseSlot_RepairsAnExistingSlotMissingItsSchema: arrange — pre-create the pool-named database (salt/fingerprint prefix) empty; act — construct PostgresTestDatabase; assert — NewContext().Orders.CountAsync() returns 0 instead of throwing "relation does not exist".
- **History:**
  - v15: found by the delta pass — db-parity, convergence 1, verdict confirmed
  - v15: fixed @`18f0b1c` — a leased slot with pending migrations is migrated before use, and a slot whose first migration fails is dropped rather than left to poison later runs
  - v16: verified — removing the repair `Migrate()` reddened the lost-schema test, restored green. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-676 — Content-rejected branch ignores a lost park CAS: claim stays held, no LastError, metric still counted

- **What:** Another replica flips the row out of Pending between the upload and the park. ParkUnbuildableAsync returns false but the return value is discarded: the Failed metric is incremented anyway, no LastError is written, and the worker's ClaimedAt is left set — so the admin list shows a stuck invoice with no reason until the claim TTL expires.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:435`
- **Suggested fix:** Mirror the InvoiceNotBuildableException branch: on parked==false, call RecordPendingErrorAsync and ReleaseClaimAsync, and only count the Failed metric when the park actually succeeded. **Trace:** (unchallenged lens verdict, not a refutation)
- **History:**
  - v15: found by the delta pass — correctness, convergence 1, verdict unverified-low
  - v15: fixed @`5d84a5e` — a lost park CAS records the error, releases the claim and logs, instead of counting a park that did not happen
  - v16: verified without a revert proof — no test: the branch needs a park CAS to lose, which needs a concurrent status change mid-call. Recorded as untested. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-677 — Webhook AlreadyInvoiced return leaves the uncommitted Paid transition on the scoped context

- **What:** Two Stripe deliveries race; the loser finds the winner's committed invoice and returns AlreadyInvoiced without saving or reloading, leaving Order.Status=Paid and PaidAt modified on the request-scoped DbContext. The admin twin (AbandonToWinnerAsync) reloads for exactly this reason. Any later SaveChangesAsync added to this request would commit the loser's PaidAt over the winner's.
- **Evidence:** `Controllers/WebhooksController.cs:328`
- **Suggested fix:** Reload the order entity (as AbandonToWinnerAsync does) before returning AlreadyInvoiced from both the pre-insert check and the unique-index catch. **Trace:** (unchallenged lens verdict, not a refutation)
- **History:**
  - v15: found by the delta pass — correctness, convergence 1, verdict unverified-low
  - v15: backlogged — unverified-low tier with no trace built; it cannot strand a payment or an invoice

### PPW-678 — Invoice number allocated outside the transaction that inserts the row, against the numbering service's contract

- **What:** PostgresInvoiceNumberingService documents 'callers must invoke this inside the same transaction that persists the Invoice row', but no caller opens a transaction: nextval autocommits, then SaveChanges runs separately. Two orders paid concurrently can commit numbers out of order relative to IssuedAt (FT-2026-00007 committed before 00006), and every failed insert burns a number outside any rollback scope.
- **Evidence:** `Services/Invoicing/InvoiceCreationService.cs:93`
- **Suggested fix:** Either open one transaction spanning NextNumberAsync and SaveChanges, or update the numbering service's doc comment to state the real (transaction-free) contract. **Trace:** (unchallenged lens verdict, not a refutation)
- **History:**
  - v15: found by the delta pass — race, convergence 1, verdict unverified-low
  - v15: backlogged — a contract nit against the numbering service's own doc comment; ADR-020 already accepts the gap-on-rollback this describes

### PPW-679 — After the 10-poll budget the payment-confirming spinner spins forever with no terminal message

- **What:** Webhook is delayed beyond 30 s (Stripe retry, worker restart). Polling stops at poll 10 but settling stays set, so the customer is left staring at an animated "Se confirmă plata..." spinner indefinitely, with no way to re-check and no statement that the app has stopped looking.
- **Evidence:** `src/app/features/orders/pages/confirmation-page.ts:280`
- **Suggested fix:** When polls hit MAX_SETTLE_POLLS, swap the spinner for a settled-message plus a "Verifică din nou" button that calls read(). **Trace:** (unchallenged lens verdict, not a refutation)
- **History:**
  - v15: found by the delta pass — frontend-ux, convergence 1, verdict unverified-low
  - v15: fixed @`2302bec` — a spent poll budget ends with a message instead of a spinner that never stops
  - v16: verified without a revert proof — covered by the existing spent-budget spec, which now also asserts the terminal message. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-680 — canContinue ignores shippingCostsReady, so a restored session proceeds on a stale shipping cost

- **What:** Customer picked Courier earlier (cost 25 in sessionStorage), then reloads /checkout/livrare while the shipping endpoint is down. Radios are disabled and prices show "se încarcă…", but the restored address keeps the form valid, so Continue is enabled and review/payment display the stale 25 RON — exactly the "customer agrees to a total nobody bills" case the new gate was added to prevent.
- **Evidence:** `src/app/features/checkout/pages/delivery-step.ts:393`
- **Suggested fix:** Include shippingCostsReady() in canContinue() (or re-gate on a confirmed cost for the selected method) so the cost-error banner blocks progress. **Trace:** (unchallenged lens verdict, not a refutation)
- **History:**
  - v15: found by the delta pass — frontend-ux, convergence 1, verdict unverified-low
  - v15: fixed @`2302bec` — `canContinue` requires both shipping prices, so a restored session cannot proceed on a stored one
  - v16: verified without a revert proof — covered by the existing before-prices spec through `canContinue`. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.

### PPW-681 — Non-owner invoice PDF served with a one-year immutable browser cache

- **What:** An ops admin fetches /api/orders/{X}/invoice; the response still carries private, max-age=31536000, immutable, so customer X's name/address PDF is written to that browser profile's disk cache under a stable URL. Anyone else using that profile who reopens the URL gets the PDF from cache with no server authorization check.
- **Evidence:** `Controllers/InvoicesController.cs:149`
- **Suggested fix:** Now that non-owners can fetch this endpoint, send private, no-store (or a short max-age) for the invoice PDF, matching OrderPaymentStatusController. **Trace:** (unchallenged lens verdict, not a refutation)
- **History:**
  - v15: found by the delta pass — security, convergence 1, verdict unverified-low
  - v15: backlogged — restates the caching half of PPW-621, which the owner already sent to the backlog

### PPW-682 — ResetForTest deletes the migration's 42 EasyboxLocker seed rows and never restores them

- **What:** Verified: every pooled database has EasyboxLockers count 0, while a freshly migrated one has 42. The reset excludes only __EFMigrationsHistory. A Postgres-backed test that reads seeded lockers, or inserts an order whose EasyboxLockerId is a seeded id, passes on a fresh slot and fails (23503) on a reused one — result depends on pool age.
- **Evidence:** `Tests/Helpers/PostgresTestDatabase.cs:106`
- **Suggested fix:** Exclude the migration's seeded reference tables from the wipe, or re-insert the seed rows after each reset — the same treatment sequences already get. **Trace:** (unchallenged lens verdict, not a refutation)
- **History:**
  - v15: found by the delta pass — db-parity, convergence 1, verdict unverified-low
  - v15: backlogged — test-database helper, not product

### PPW-683 — DropAllForeignKeys does not mark the pooled database dirty, so a constraint-free schema can be handed on

- **What:** A test class using IClassFixture<PostgresTestDatabase> (std pool) calls DropAllForeignKeys(); ExecuteInternal skips _schemaTouched, so Dispose returns the slot instead of dropping it. The next class leasing that slot runs with no referential integrity, and a test asserting an FK violation goes false-green. Latent today — the only caller uses Throwaway().
- **Evidence:** `Tests/Helpers/PostgresTestDatabase.cs:166`
- **Suggested fix:** Route DropAllForeignKeys() through Execute() so it sets _schemaTouched, or make it private to the ForeignKeyFreeTestDatabase construction path. **Trace:** (unchallenged lens verdict, not a refutation)
- **History:**
  - v15: found by the delta pass — db-parity, convergence 1, verdict unverified-low
  - v15: backlogged — test-database helper, not product

### PPW-684 — Test-database sweep is scoped to its own salt, so pools from other worktrees are never reclaimed

- **What:** The sweep's prefix is pp_test_<salt>_, so it only removes stale schemas from the same output directory. Each worktree (the owner runs 2-3) leaves a permanent pool, and a deleted worktree's pool survives forever. Measured now: 40 pp_test_* databases, 347 MB, across three salts — unbounded growth on a machine already described as overloaded.
- **Evidence:** `Tests/Helpers/PostgresTestDatabase.cs:292`
- **Suggested fix:** Sweep on the pp_test_ prefix across salts when the advisory lock is free, or stamp a last-used timestamp per pool and drop pools unused for N days. **Trace:** (unchallenged lens verdict, not a refutation)
- **History:**
  - v15: found by the delta pass — db-parity, convergence 1, verdict unverified-low
  - v15: backlogged — test-database helper, not product

### PPW-685 — ResetSequences drops every public sequence the migration script did not literally CREATE, including identity-owned ones

- **What:** The whitelist is regex-scraped from CREATE SEQUENCE statements, but identity/serial sequences are created implicitly by the column definition and never appear. Add one int identity column and its sequence gets DROP SEQUENCE ... CASCADE on the first reset, breaking that column permanently in the pooled database while __EFMigrationsHistory still claims the migration applied.
- **Evidence:** `Tests/Helpers/PostgresTestDatabase.cs:128`
- **Suggested fix:** Skip sequences that pg_sequences reports as owned by a column (pg_depend/pg_get_serial_sequence) instead of relying only on the scraped whitelist. **Trace:** (unchallenged lens verdict, not a refutation)
- **History:**
  - v15: found by the delta pass — db-parity, convergence 1, verdict unverified-low
  - v15: backlogged — test-database helper, not product

### PPW-686 — MaxBatchSize is used unclamped unlike the upload job's other settings

- **What:** PollIntervalMinutes, ClaimTtlMinutes and MaxUnknownUploadOutcomes are all floored with Math.Max, but Take(_settings.MaxBatchSize) is raw. Anaf:MaxBatchSize=0 silently stops all e-Factura submission with a healthy-looking "batch size=0" absence, and a negative value makes Postgres reject the LIMIT on every tick.
- **Evidence:** `Services/Invoicing/Anaf/InvoiceUploadJob.cs:109`
- **Suggested fix:** Clamp in the constructor, e.g. _maxBatchSize = Math.Clamp(_settings.MaxBatchSize, 1, 500), and use that field in the query. **Trace:** (cleanup — not verified)
- **History:**
  - v15: found by the delta pass — correctness, convergence 1, verdict unverified-cleanup
  - v15: fixed @`9527eba` — `MaxBatchSize` is clamped to 1–500 in the constructor, like `PollIntervalMinutes` and `MaxUnknownUploadOutcomes`
  - v16: verified without a revert proof — a clamp with no test of its own; the settings validator bounds `MaxBatchSize` upstream. Full touched surface green in sequential batches: 1290 API tests (10 skipped) across Unit.Services, Unit.Controllers, Unit.Data, Unit.Observability, Unit.Middleware, Unit.Validators and Integration, plus all 520 UI specs.
