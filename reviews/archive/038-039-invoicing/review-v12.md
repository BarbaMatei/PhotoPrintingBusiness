---
type: review
target: 038-039-invoicing
version: 12
supersedes: 9
commit: 090873d
branch: feat/bolt-038-vat-calculation
pass-type: certification
date: 2026-08-21
lenses: [correctness, security, race, db-parity, tests-coverage, requirements, observability, input-validation, frontend-ux, quality, completeness-critic]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-579, PPW-584, PPW-580, PPW-581, PPW-582, PPW-583, PPW-586, PPW-585, PPW-489, PPW-524]
findings: { high: 11, medium: 36, low: 35, cleanup: 14, refuted: 3 }
tests: { dotnet: "1455/1465 — 10 skipped, 0 failed", frontend: "48/48 test files" }
---

# Review v12 — 038-039-invoicing

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-579 | 🔴 | Static ro-RO culture in InvoicePdfDocument throws on the Alpine production image, wedging every invoice PDF | `Services/Invoicing/InvoicePdfDocument.cs:19` | yes |
| PPW-584 | 🔴 | SPA never sends an Idempotency-Key and PaymentStep mints a fresh order on every mount | `src/app/core/services/payment.service.ts:18` | yes |
| PPW-580 | 🔴 | One MaxBatchSize batch mixes cooldown-exempt Submitted polls with Pending uploads, so stuck polls starve new invoices out of filing | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:102` | yes |
| PPW-581 | 🔴 | Expired or revoked ANAF credentials never reach the auth-outage alert; they fan out as N generic row-failed errors per tick | `Services/Invoicing/Anaf/AnafTokenProvider.cs:109` | yes |
| PPW-582 | 🔴 | Confirmation page races the payment webhook and redirects the paying customer home | `src/app/features/orders/pages/confirmation-page.ts:208` | yes |
| PPW-583 | 🔴 | Switching payment tabs destroys the Stripe card element but leaves the pay button enabled | `src/app/features/checkout/pages/payment-step.ts:196` | yes |
| PPW-586 | 🔴 | Neither invoice controller has an HTTP-pipeline test, so endpoint authorization and DualAuth guest ownership are unverified | `Tests/Unit/Controllers/InvoicesControllerTests.cs:52` | yes |
| PPW-585 | 🔴 | Recapitulare hides the new fiscal address for locker orders, and an unchanged spec pins that behaviour | `src/app/features/checkout/pages/review-step.spec.ts:126` | yes |
| PPW-489 | 🔴 | Polly retries the non-idempotent ANAF upload POST on ambiguous-outcome errors | `Services/Invoicing/Anaf/AnafResilienceHandler.cs:33` | no — decision first |
| PPW-524 | 🔴 | The whole invoicing feature has no SPA consumer and no lens covered the frontend | `Controllers/InvoicesController.cs:1` | no — decision first |
| PPW-587 | 🟠 | A permanent HTTP 4xx content rejection is classified as unreachable/transient, so the row retries forever and is never parked | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:355` | yes |
| PPW-588 | 🟠 | Unknown-outcome budget covers only client timeouts, so AnafUnreachableException gets unlimited blind re-POSTs | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:355` | yes |
| PPW-590 | 🟠 | PollSubmittedAsync takes no claim, so every replica polls every Submitted row on every tick | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:421` | yes |
| PPW-592 | 🟠 | ANAF-supplied index_incarcare is accepted unvalidated into a varchar(100) column, turning a filed invoice into a blind re-upload loop | `Services/Invoicing/Anaf/AnafSpvClient.cs:91` | yes |
| PPW-591 | 🟠 | No setval reconciliation: an invoice sequence that lags the Invoices table wedges every paid order | `Services/Invoicing/PostgresInvoiceNumberingService.cs:40` | yes |
| PPW-589 | 🟠 | nextval commits outside the insert transaction, so a lost duplicate-delivery race permanently burns a fiscal invoice number | `Services/Invoicing/PostgresInvoiceNumberingService.cs:40` | yes |
| PPW-598 | 🟠 | Admin retry never re-renders the PDF, contradicting the documented fix-forward-and-re-render rollback | `Services/Invoicing/InvoiceLifecycle.cs:165` | yes |
| PPW-600 | 🟠 | FR-4's exponential backoff (1h/4h/16h/64h) never runs — Rejected is terminal until an admin acts | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:99` | yes |
| PPW-602 | 🟠 | Invoice 404 advertises Retry-After 30 seconds although the PDF can be a 30-minute poll interval away | `Controllers/InvoicesController.cs:68` | yes |
| PPW-603 | 🟠 | The poll leg has no catch, so an ANAF outage logs Error row-failed there while the upload leg logs Warning unreachable | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:414` | yes |
| PPW-604 | 🟠 | No metric marks a stuck or retrying invoice, so the sole ANAF panel goes blind during an outage | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:344` | yes |
| PPW-596 | 🟠 | No admin access to an invoice PDF, so FR-5's role override and the inspection-week runbook are undelivered | `Controllers/InvoicesController.cs:58` | yes |
| PPW-597 | 🟠 | Invoice-by-email (FR-5, story 003) is not implemented while ddd-02 describes it as shipped | `Services/Invoicing/InvoicePdfReadyNotifier.cs:31` | yes |
| PPW-599 | 🟠 | Documented batch-retry SQL in DEPLOYMENT.md reposts the identical rejected XML and re-parks on the first timeout | `docs/DEPLOYMENT.md:1531` | yes |
| PPW-601 | 🟠 | system-architecture.md was never updated for the invoicing feature, breaking the descriptive-standards rule | `memory-bank/standards/system-architecture.md:83` | yes |
| PPW-593 | 🟠 | Admin retry's Rejected/Failed status whitelist has no test; only the 409-free happy path is covered | `Tests/Unit/Controllers/AdminInvoicesControllerTests.cs:75` | yes |
| PPW-594 | 🟠 | The new Invoice.StorageLocation stamp is never asserted after a PDF save | `Tests/Unit/Services/Invoicing/Anaf/InvoiceUploadJobTests.cs:304` | yes |
| PPW-595 | 🟠 | QuestPDF licence is set by the test class itself, so the production licence wiring is unverified | `Tests/Unit/Services/Invoicing/InvoicePdfRendererTests.cs:23` | yes |
| PPW-605 | 🟠 | Manual admin mark-Paid issues a fiscal invoice with no log naming the admin | `Services/AdminOrderService.cs:154` | yes |
| PPW-606 | 🟠 | Only the pre-commit attempted invoice number is logged; the committed number is never logged | `Services/Invoicing/InvoiceCreationService.cs:98` | yes |
| PPW-607 | 🟠 | Admin- and config-sourced fields (invoice line name) reach the UBL XML with no control-char guard and no truncation | `Services/Invoicing/InvoiceXmlBuilder.cs:204` | yes |
| PPW-609 | 🟠 | One generic error string blames the cart for every payment failure, and the legacy processor failures are silent | `src/app/features/checkout/pages/payment-step.ts:188` | yes |
| PPW-611 | 🟠 | SPA still sends the deprecated shippingCostRon, so every checkout logs a tampering warning | `src/app/core/models/payment.model.ts:8` | yes |
| PPW-612 | 🟠 | Checkout address form mirrors only the phone rule, so the new fiscal-address length/charset caps surface as a 400 at the payment step | `src/app/features/checkout/pages/delivery-step.ts:336` | yes |
| PPW-613 | 🟠 | VAT is never shown in the SPA although the API now returns NetTotalRon/VatRon/VatRate | `src/app/core/models/order.model.ts:32` | yes |
| PPW-614 | 🟠 | Hardcoded 20/25 RON shipping defaults with no error handling can differ from the invoiced total | `src/app/features/checkout/pages/delivery-step.ts:327` | yes |
| PPW-615 | 🟠 | A non-succeeded, non-error Stripe result leaves the user stranded with no feedback | `src/app/features/checkout/pages/payment-step.ts:221` | yes |
| PPW-608 | 🟠 | Admin cannot mark an order Paid by hand — NEXT_STATUSES has no AwaitingPayment entry | `src/app/features/admin/pages/order-detail/admin-order-detail-page.ts:19` | yes |
| PPW-610 | 🟠 | The invoice-number-exhausted 409 message is replaced by a generic admin failure toast | `src/app/features/admin/pages/order-detail/admin-order-detail-page.ts:133` | yes |
| PPW-616 | 🟠 | Saved addresses allow City 100 while checkout caps it at 50, and the new prefill copies them in | `Validators/Account/SavedAddressValidator.cs:26` | yes |
| PPW-617 | 🟠 | The paid-transition invoice retry/rollback state machine is implemented twice with divergent guards and no shared test | `Services/AdminOrderService.cs:437` | yes |
| PPW-618 | 🟠 | Cloud tier and the new cross-tier fallback read are proven only against fakes | `Controllers/InvoicesController.cs:99` | yes |
| PPW-516 | 🟠 | Exhausted invoice-number retry answers the payment processor 200, killing its last retry | `Controllers/WebhooksController.cs:304` | no — decision first |
| PPW-520 | 🟠 | Per-line PriceAmount x InvoicedQuantity no longer equals LineExtensionAmount, and nothing asserts it | `Services/Invoicing/InvoiceXmlBuilder.cs:219` | no — decision first |
| PPW-526 | 🟠 | the legacy processor paid leg's new three-state outcome and its rollback have no endpoint-driven test | `Controllers/WebhooksController.cs:205` | no — decision first |
| — | 🟡 | Invoicing worker and ANAF client minors — PPW-624, PPW-625, PPW-626, PPW-634, PPW-635, PPW-636, PPW-645, PPW-646, PPW-498 | `Services/Invoicing/Anaf/` | no |
| — | 🟡 | Invoice numbering and provider-behaviour minors — PPW-619, PPW-627, PPW-628, PPW-505 | `Services/Invoicing/PostgresInvoiceNumberingService.cs` | no |
| — | 🟡 | Invoice API, admin API and log-signal minors — PPW-620, PPW-621, PPW-629, PPW-632, PPW-637, PPW-519, PPW-501 | `Controllers/InvoicesController.cs` | no |
| — | 🟡 | Checkout and customer-page minors — PPW-622, PPW-640, PPW-641, PPW-642, PPW-643, PPW-644 | `src/app/features/checkout/` | no |
| — | 🟡 | Input-validation, security and records minors — PPW-623, PPW-638, PPW-639, PPW-630, PPW-631, PPW-633, PPW-563, PPW-572 | `Validators/` | no |
| — | ⚪ | Duplication and dead-path cleanups — PPW-652, PPW-653, PPW-654, PPW-655, PPW-656, PPW-545, PPW-546, PPW-575 | `Services/Invoicing/` | no |
| — | ⚪ | Records, test and migration-hygiene cleanups — PPW-647, PPW-648, PPW-649, PPW-650, PPW-651, PPW-657 | `memory-bank/operations/metrics.md` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The invoice PDF and XML services exist only when the ANAF flag is on, and no test boots either dependency-injection configuration | Both services are registered above the flag gate, so both configurations resolve and no resolution failure exists; the residual — no PDF while the flag is off, and a 404 download — is the two-stage rollout the settings file documents |
| A throw in the paid webhook's broadcast or email step permanently skips photo promotion and courier notification | The in-request skip is real and a redelivery short-circuits as a duplicate, but a promotion scanner and a courier retry job each re-enqueue every affected paid order at boot and on a timer, and each is registered under the same flag as the enqueue it backs — a delay, not a loss |
| A lost compare-and-swap on the submitted transition throws away the ANAF upload id and leaves the filed invoice Pending, so the next tick re-files it | The compare-and-swap can only lose when the row is no longer Pending, so the premise contradicts itself; the next batch re-reads the status and skips the row |

## Notes for the fixer

This certification **does not certify**. It found eleven high-severity rows, eight of them new,
so the loop re-arms rather than closing. Nothing here is approved.

Read `PPW-579` first and treat it as a release gate: every invoice PDF fails on the production
image, which no earlier pass reached because the failure needs the Alpine base, not the code.

Order after that: `PPW-584` (a double charge on the money path), then the checkout pair
`PPW-582` and `PPW-583`, then the worker pair `PPW-580` and `PPW-581`, then `PPW-586` and
`PPW-585`. Six of the eight new high rows carry an approach pre-check on their ledger row and
**every one came back `revised`** — adopt the revision, and re-check only where you deviate.
Three revisions change the shape of the fix, not its details: `PPW-582` cannot be fixed in the
SPA alone (a guest customer's order read answers 401), `PPW-584`'s key cannot live in the
checkout state (sessionStorage is per tab), and `PPW-586`'s fix belongs in the shared claims
extension, where it also changes order attribution for a signed-in caller carrying a stale
guest token — that is a second defect wearing the same code fact, and it needs its own test.

Two high rows are decisions, not fixes: `PPW-489` and `PPW-524` re-raise decided rows, with the
prior ruling on each ledger row. Three lenses agreed on `PPW-489`. `PPW-524` is the first
verdict the `frontend-ux` lens ever gave this target — it never ran in eleven passes — and five
of the eleven high rows come from it, so treat "the frontend was never reviewed" as the reason
this pass looks like a first pass. `PPW-589`, `PPW-597`, `PPW-600`, `PPW-608` and `PPW-633`
also need a ruling before code moves: each contradicts a written decision, contract or story.

Coupling worth knowing: `PPW-583` must land its stuck-spinner half in the same commit, or the
new spec cannot even await stability. `PPW-580` must not write the cooldown with raw SQL — four
existing tests break. `PPW-618` re-opens `PPW-554`'s premise: the storage adapter cannot tell a
missing bucket from a missing key, so the test that row asks for cannot pass as written.
`PPW-585`'s spec pins today's wrong behaviour, so retarget it rather than adding a case.

Test-harness traps, all named on the rows: the invoice-job suite runs on real PostgreSQL per
test, the SPA is zoneless so `fakeAsync` does not exist, the Stripe module is unmocked in every
payment spec (the card element is mounted zero times today), and two payment specs currently
pass for the wrong reason.

Two deliberate deviations in this pass. First, approach pre-checks ran only for the six
high-severity trigger-list-shaped fixes, not for all 47 serious rows; at this scale that would
have cost more than the pass, and the fix round runs its own checks. Second, the protocol asks
a target's first certification to run as a pair of passes; this ran as one, on the owner's
ruling of 2026-08-21. Both are recorded on the metrics line.

The 🟡 and ⚪ rows are grouped in the table to stay inside the file's size cap; every id is
named, and each one's full detail is on its ledger row. The three refuted rows sit inside the
severity counts in the frontmatter — one high, one medium, one low.

Suite state at `090873d`, measured and final for this pass: backend 1455 passed, 0 failed, 10
skipped of 1465 (the skips are the storage suite, which needs its own credentials); frontend 48
test files, all passing. No suite was re-run during synthesis.
