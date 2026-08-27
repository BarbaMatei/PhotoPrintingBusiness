---
type: resolution
target: 038-039-invoicing
version: 12
answers: review-v12.md
status: resolved
fixed_commit: ec29613
closed: 2026-08-27
---

# Resolution v12 — 038-039-invoicing

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-579 | fixed | `8e71c63`, `ed3ce30` | runtime stage installs `icu-libs` + `icu-data-full`, its `ENV` turns the base image's invariant flag off, and the API runtimeconfig pins it off, which outranks the variable. Culture stays `ro-RO`. New surface: the image's ICU packages |
| PPW-584 | fixed | `d572a1a`, `2acda1f`, `901f8a2` | one key per basket in localStorage, reused across mounts and cleared when the order settles; a settled key now 409s naming its order and the page sends the customer there instead of charging again |
| PPW-580 | wont-fix | — | owner ruled 2026-08-22; reaching it needs the tax authority to keep erroring on `stareMesaj` until 50 rows are stuck, and he accepts that risk |
| PPW-581 | wont-fix | — | owner ruled 2026-08-22; reaching it needs revoked or expired ANAF credentials, and he accepts that risk |
| PPW-582 | fixed | `d572a1a`, `901f8a2` | new guest-readable `GET /api/orders/{id}/payment-status`; the confirmation page waits on it for up to ten reads instead of sending a paying customer home |
| PPW-583 | fixed | `06fd2b1` | retired by deletion: PR #13 removed the second processor, so no tab switcher and no destroyed card element exist. Nothing verifies it — the surface is gone |
| PPW-586 | fixed | `8950624` | six tests through the real pipeline: 401 anonymous, 403 for another customer and another guest session, 404 for both owners, and the admin override — which reddens when reverted |
| PPW-585 | fixed | `c03f99a` | the recap renders the invoiced address for locker orders too; the spec that pinned the old behaviour was retargeted |
| PPW-489 | wont-fix | — | the earlier owner ruling stands; v12 raised it again and this round did not revisit it |
| PPW-524 | fixed | `5324a1c` | the confirmation page now has an invoice download reading `GET /api/orders/{id}/invoice` through the service, because an anchor cannot send the guest header |
| PPW-615 | fixed | `901f8a2` | a card result that is neither success nor error now says so, a rejected confirm call clears the spinner, and an intent that cannot be created offers a retry |
| PPW-611 | fixed | `2acda1f` | the deprecated `shippingCostRon` is gone from the request and its model, so a checkout no longer logs a tampering warning |
| PPW-596 | fixed | `e3f4bb8` | an Admin may read a customer invoice; the read is logged as `invoice.pdf.admin-read` |
| PPW-607 | fixed | `166230a` | every field routed through the address formatter drops XML-invalid characters, and the UBL line description is wrapped in it |
| PPW-612 | fixed | `5cd48a5` | the checkout form mirrors the server caps, including a group-level combined street length |
| PPW-616 | fixed | `5cd48a5` | the saved-address city cap is aligned to `InvoiceAddressFormatter.CityNameMaxLength` |
| PPW-597 | fixed | `5324a1c`, `beb7732` | resolved on the doc side plus a real route: ddd-02 says the email is unshipped, and the customer has a download. Story 003 stays open as scope |
| PPW-587 | fixed | `32d4eee` | a non-success status that is not 408/429/5xx raises `AnafContentRejectedException` and the row is parked as Failed |
| PPW-588 | fixed | `32d4eee` | an upload-leg outage spends the blind-repost budget and keeps the claim, so no second replica re-posts an unknown outcome |
| PPW-604 | fixed | `32d4eee` | a `retrying` value joins `invoice_anaf_status_total`; the cardinality budget was raised from 4 to 5 deliberately |
| PPW-591 | fixed | `72202c0` | a taken-number collision runs `setval` past `MAX("Number")` for the series and the UTC year of `IssuedAt`, mirroring the unique index |
| PPW-600 | fixed | `6977d5b` | Rejected rows re-enter the batch on cumulative `BackoffHours` milestones and reach Failed when the schedule is spent |
| PPW-649 | fixed | `6977d5b` | metrics.md drops the "future" marker, documents `retrying` and corrects the series counts |
| PPW-592 | fixed | `add7611` | the client rejects an over-wide `index_incarcare`, and a failed status write after a successful upload counts as an unknown outcome |
| PPW-598 | fixed | `add7611` | `RetryAsync` clears `PdfStoragePath`, so the runbook's fix-forward-and-re-render actually re-renders |
| PPW-599 | fixed | `add7611` | the documented batch SQL also clears `XmlPayload`, `PdfStoragePath`, `UnknownUploadOutcomes` and `ClaimedAt` |
| PPW-602 | fixed | `add7611` | the 404 carries a `Retry-After` of `Anaf:PollIntervalMinutes`, matching the only producer of the PDF |
| PPW-605 | fixed | `add7611` | `admin.order.mark-paid` carries admin_user_id, order_id, order_number and the committed invoice_number |
| PPW-608 | fixed | `add7611` | `NEXT_STATUSES` gains `AwaitingPayment`, so an offline transfer is reconcilable from the UI the API already supported |
| PPW-613 | fixed | `6812453` | order detail and confirmation render a TVA line from the server `vatRon`/`vatRate`; the review step is left out on purpose |
| PPW-614 | fixed | `6812453` | the cost signals start null, the cards stay disabled until the server answers, and a late price updates the stored cost |
| PPW-560 | fixed | `56eb9be` | no database ever ran the deleted chain, so §7 states the real three-migration chain and gives the history reseed if one appears |
| PPW-509 | fixed | `ec29613` | the pipeline sentence is gone from the settings docstring, which points at `AnafSettings` instead |
| PPW-510 | fixed | `ec29613` | the ADR keeps its frozen-record convention and gains one note naming both statements that no longer hold |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — production image globalization | PPW-579 | `Dockerfile`, `src/PhotoPrint.API/PhotoPrint.API.csproj`, `src/PhotoPrint.Tests/Unit/Services/Invoicing/InvoicePdfCultureTests.cs`, `docs/DEPLOYMENT.md` | not needed (the v12 pre-check classified it not trigger-list-shaped: a base-image and configuration change plus a renderer test) |
| B — untouched this round | PPW-580, PPW-581, PPW-582, PPW-583, PPW-584, PPW-585, PPW-586, PPW-489, PPW-524 | — | not needed (no code changed) |
| C — checkout payment flow (final round, cluster 1 of 3) | PPW-584, PPW-582, PPW-615, PPW-611 | `src/app/core/services/checkout-attempt.service.ts`, `core/services/payment.service.ts`, `core/services/auth.service.ts`, `core/models/payment.model.ts`, `features/checkout/pages/payment-step.ts`, `features/orders/pages/confirmation-page.ts`, `Controllers/OrderPaymentStatusController.cs`, `DTOs/Orders/OrderPaymentStatusDto.cs`, their specs/tests, `memory-bank/standards/{system-architecture,api-conventions}.md` | v12 pre-checks consumed for PPW-584 (revised) and PPW-582 (revised); one new adversarial check for the PPW-615 pay-state machine and the 409 handling |
| D — customer- and admin-facing invoice access (cluster 2 of 3) | PPW-596, PPW-607, PPW-612, PPW-616, PPW-597, PPW-524, PPW-585, PPW-613, PPW-614 | `Controllers/InvoicesController.cs`, `Services/Invoicing/{InvoiceAddressFormatter,InvoiceXmlBuilder}.cs`, `Validators/Account/SavedAddressValidator.cs`, `features/checkout/pages/{delivery-step,review-step}.ts`, `features/orders/pages/{order-detail-page,confirmation-page}.ts`, `core/models/{order,payment}.model.ts`, `core/services/checkout-state.service.ts`, their specs | not needed (field caps, a template line and an authorization branch); PPW-614 gates a state machine, checked against the existing selectMethod contract
| E — ANAF worker and numbering (cluster 3 of 3) | PPW-587, PPW-588, PPW-592, PPW-598, PPW-599, PPW-600, PPW-602, PPW-604, PPW-605, PPW-608, PPW-591, PPW-649, PPW-560, PPW-509, PPW-510 | `Services/Invoicing/Anaf/{AnafExceptions,AnafSpvClient,InvoiceUploadJob}.cs`, `Services/Invoicing/{InvoiceLifecycle,PostgresInvoiceNumberingService,InvoiceCreationService}.cs`, `Services/AdminOrderService.cs`, `Observability/MetricNames.cs`, `docs/DEPLOYMENT.md`, `memory-bank/operations/metrics.md`, their tests | not needed for the doc rows; the three trigger-list-shaped ones (PPW-591, PPW-600, PPW-592) were each proven by revert-and-rerun instead of a pre-check, since no lens ran a pre-check on them

## Decisions

### ICU goes into the image; the invoice keeps its Romanian culture (PPW-579)

The owner's ruling was that production must be able to render Romanian invoices, not that the
culture should be dropped. So the renderer is untouched and the deployed image changed.

- The runtime stage installs `icu-libs` **and** `icu-data-full`. Alpine's `icu-libs` pulls only
  the English data set, and a Romanian locale that falls back to root prints `1,234.56` on a
  fiscal invoice with no error anywhere — a silently wrong invoice is worse than the crash.
- Invariant mode is turned off twice: the stage's `ENV` sets
  `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false`, and the API project sets
  `InvariantGlobalization=false`. Both, because a compose `environment:` block or the server
  `.env` overrides an image `ENV`, while the runtimeconfig switch the project property emits
  outranks the variable.
- That precedence was measured, not assumed. A probe app carrying the property resolved `ro-RO`
  and printed `1.234,56` with `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` set; with the property
  removed and the same variable it threw `CultureNotFoundException` — "ro-ro is an invalid
  culture identifier". The built `PhotoPrint.API.runtimeconfig.json` now carries
  `"System.Globalization.Invariant": false`.

### What the tests prove, and what nothing here proves (PPW-579)

The fix lives in the deployed image, so two of the three tests are a deployment contract: one
reads the built `PhotoPrint.API.runtimeconfig.json` beside the test binaries, one reads the
Dockerfile's runtime stage with comments dropped and continuation lines joined, so a reflow
cannot slip past it. The third reads the renderer's culture field by reflection.

- Revert-and-rerun: with `Dockerfile` and the project file back at `8e71c63~1`, those two tests
  failed, 2 of 3. Restored: 10 of 10 across the `InvoicePdf` tests.
- The third is a canary, not a red leg — it passes on any host with ICU, however the image is
  configured. It reddens where it matters: under `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` it
  fails with `TypeInitializationException` → `CultureNotFoundException`, the production failure
  itself, and it also reddens if the renderer is switched to invariant formatting. The micro-review
  found it passing in both states, which is why the tests were retargeted before hand-back.
- **The image was not built.** There is no Docker on this machine, so the two package names are
  not proven to install. If either is wrong the deploy workflow's image build fails loudly.

### The class sweep found one site (PPW-579)

`src/PhotoPrint.API` was swept for `CultureInfo`, `GetCultureInfo`, `new CultureInfo(`,
`CreateSpecificCulture`, hard-coded culture names, `TimeZoneInfo`, `IdnMapping` and
`CompareInfo`. `Services/Invoicing/InvoicePdfDocument.cs:19` is the only site that needs a real
culture. Every other formatting call passes `CultureInfo.InvariantCulture` on purpose —
`Controllers/WebhooksController.cs:200`, `Services/LegacyProcessorService.cs:26` and `:120`, and six
sites in `Services/Invoicing/InvoiceXmlBuilder.cs` — which is correct there: the UBL XML and the
processor signatures are machine-read and must not carry Romanian separators.

There is no `TimeZoneInfo` or local-time use anywhere in the API, so `tzdata` was left out of the
image. Neither compose file, nor `.env.example`, nor any workflow sets the globalization variable,
so nothing else re-enables invariant mode today.

### The nine rows this round did not change (PPW-580 … PPW-524)

- PPW-580 and PPW-581 are `wont-fix` on the owner's ruling of 2026-08-22. What it costs if that
  call is wrong: PPW-580 leaves newly paid invoices unfiled past the five-day deadline while
  stuck polls hold the whole batch, and PPW-581 leaves a credential outage paging nobody — it
  shows only generic per-row failures, so nobody learns that filing has stopped.
- PPW-585 and PPW-586 were regraded 🔴 to 🟠 on 2026-08-22 after the driver checked them with the
  owner. Both stay open at medium.
- PPW-582, PPW-583 and PPW-584 are deliberately left for a later round: the owner scoped this
  round to PPW-579. PPW-584 is the double-charge path.
- PPW-489 and PPW-524 carry earlier owner decisions that v12 raised again; neither was revisited.
- Their rows above read `deferred` because that is the only legal status for "not addressed in
  this round". Their **ledger rows stay `open`**, so the next discovery pass does not read them
  as decided. The resolution stays `in-progress` for the same reason.

### The claim stays when ANAF never answered (PPW-588)

The first attempt released the claim, matching the other error paths. That is wrong here: an
upload whose outcome nobody knows may already be filed under this invoice number, and a released
claim invites the next replica to file it again. So the outage branch counts the unknown outcome
against `Anaf:MaxUnknownUploadOutcomes`, keeps the claim, and lets the TTL — not a guess — decide
when another worker may look. The test asserts `ClaimedAt` is still set, which is what reverting
the change breaks. Same reasoning applied to PPW-592: a status write that fails after a successful
upload counts as an unknown outcome instead of rethrowing into a re-file loop.

### Rejections are retried, not documented away (PPW-600)

The row offered a choice: implement FR-4's backoff, or amend the spec to say rejections are
admin-only. Retrying was chosen because the five-business-day submission deadline is a legal one
and the alternative silently spends it. The schedule derives from `CreatedAt` against cumulative
`BackoffHours` with no persisted counter, which is what ADR-024 already decided; the next slot is
the first milestone after the row's last transition. Rejected reaches Failed through a new
`GiveUpOnRejectedAsync`, because `MarkFailedAsync` CASes on Submitted and could never fire.

### Reconciliation runs on collision, not at boot or per allocation (PPW-591)

Reconciling at boot would need a series-and-year sweep before the app serves traffic, and doing it
inside every allocation adds a `MAX()` read to the hot path — PPW-645 already objects to the DDL
check that is there. The collision retry is the one place that knows something is wrong, so the
self-heal sits in both retry loops (webhook and admin) and costs nothing in normal flow. `setval`
is non-transactional, which is what makes the heal survive the failed attempt that triggered it.

### VAT is shown where the server states it (PPW-613)

The row asked for a TVA line on review, confirmation and order detail. Review was left out: no
order exists at that point, so the SPA would have to hold its own copy of the rate — the same
defect class as PPW-614, which this round removed. Confirmation and order detail render the
server's `vatRon` and `vatRate`; the guest-readable payment-status DTO was widened by those two
fields, which discloses nothing a customer is not already entitled to about their own order.
