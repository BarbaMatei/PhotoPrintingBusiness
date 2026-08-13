---
type: owner-summary
target: 038-039-invoicing
pass: 1
pass-type: discovery
commit: e724528
date: 2026-08-13
decisions-needed: 7
---

# Owner summary — 038-039-invoicing v1

First discovery pass over the Romanian VAT calculation (bolt 038) and e-Factura/ANAF integration (bolt 039) at `e724528` — one PR. It found 37 defects plus 1 refuted suspicion: 10 High, 15 Medium, 5 Low, 7 Cleanup (see [the review](review-v1.md); detail per PPW-# on [the ledger](ledger.md)). Verdict: `request-changes`. This is money- and external-input-touching code (full-loop tier) — nothing here is behind a flag the way 044-045's was; the ANAF integration ships live once `Anaf:Enabled=true`.

## Needs your decision

The 25 High and Medium defects collapse into 7 choices. Counts and PPW-#s are exact.

1. 🔴 Invoice PDFs never actually reach cloud storage — PPW-469, PPW-470. Both bypass `IStorageRouter` and always write/read local disk, regardless of `Storage:Provider`. On any multi-replica deploy, a PDF one instance wrote 404s or 500s on another. Suggested: one DI fix covers both, ~1–2h plus an integration test.
2. 🔴 Concurrent payments can each mint their own fiscal invoice for one order — PPW-471, PPW-490. Neither has a DB-level uniqueness backing its check-then-act creation/numbering; the vetted fix needs a unique constraint plus a provider-specific violation catch at the webhook `SaveChangesAsync` sites, not where either finding originally pointed. Suggested: fix as one cluster, ~3–4h plus a migration and a concurrent-webhook test.
3. 🔴 Two defects in the e-Factura XML itself, the actual content sent to the tax authority — PPW-477, PPW-478. PPW-478: line-level amounts are gross where UBL requires tax-exclusive, so line totals won't reconcile with the document total. PPW-477: a malformed customer name can reach the same legal document with no filter, and a hard ANAF rejection of bad XML is silently swallowed. Suggested: fix both before any real invoice reaches ANAF, ~1 day including new multi-item test fixtures; both carry a vetted approach in the ledger.
4. 🔴 An invoice is often simply unobtainable — PPW-472 (the customer email never sends despite the flag), PPW-473 (guest checkouts always get 401), PPW-474 (admin-reconciled paid orders never get one at all). PPW-473/474 are one-line-pattern fixes, ~1–2h each. PPW-472 is a real scope call: build the email integration now, or ship the flag off and say so honestly in the docstring — either is ~half a day.
5. 🟠 Three separate mechanisms can each send ANAF the same invoice twice — PPW-475, PPW-476, PPW-489. No claim/lease on multi-replica pickup, no way to tell "uploaded, DB didn't record it" from "never uploaded," and Polly retries the upload POST itself on an ambiguous response. PPW-489 has no working mitigation today — the vetted approach-check found the proposed status-check is a no-op against real ANAF. Suggested: fix PPW-475/476 this round (~1 day incl. migration); treat PPW-489 as its own decision — accept ANAF's documented dedupe-by-invoice-number as sufficient, or fund building and sandbox-testing a new lookup call first.
6. 🟠 Ten mostly-mechanical Medium defects, none needing a decision on their own. PPW-479 is an admin page-param overflow. PPW-480 is an admin retry that resubmits identical XML. PPW-481 and PPW-482 are a docstring and an audit-log claim that don't match the code. PPW-483 and PPW-484 are redundant queries. PPW-485 is validation caps wider than the legal XML limits. PPW-486, PPW-487, and PPW-488 are observability gaps that would make a production incident hard to diagnose. Suggested: one fix round together with the High defects, ~1 day.
7. 🟠 Zero tests on the riskiest new code — PPW-491, PPW-492, PPW-493. The most stateful new component (`InvoiceUploadJob`) and the only production numbering path (Postgres) have no direct tests; a regression in either ships silently. Suggested: add these as part of this fix round, not deferred — this is exactly the dual-database test gap this repo's own standards flag.

## Reasons to doubt

- 2 of 10 manifest lenses never ran: `tests-coverage` and `db-parity` — a key-name typo in this pass's dispatch, not a scoping choice (PPW-497). Two single-agent supplemental checks (not full lenses) closed part of the gap and directly surfaced PPW-478 and PPW-505 — real defects a full lens would likely have caught faster, and a signal other gaps may remain uncovered.
- No trend yet — this is the first pass recorded for this target.
- All 10 findings that reached an approach-check came back needing a real correction, not a clean pass — a wrong catch site, a missing migration step, a proposed check that turns out to do nothing against real ANAF. Implement the corrected approach recorded on the ledger row, not the original finding text.
- 3 findings still carry doubt: PPW-477 (the original crash mechanism was disproven; a different, real mechanism was checked directly and holds), PPW-491 (a real coverage gap, with no wrong-output case to build a trace against), PPW-505 (a real mechanism, unclear whether it actually triggers in practice). The 7 Cleanup findings were never checked by a second agent at all — by design.
- Blinding is enforced by prompt only; no tool confirms lenses stayed out of `reviews/`.
- A discovery pass cannot certify. `request-changes` is the strongest statement available here — "the feature is clean" needs a later full-manifest pass after the fixes, folding in the two owed lenses.

## Filed automatically

12 minors went to the ledger backlog, not the fix round:

- PPW-494 — 🟡 — cloned retry `HttpRequestMessage` in `AnafAuthHandler` is never disposed.
- PPW-495 — 🟡 — `status=""` is rejected by the query validator but treated as "no filter" by the controller.
- PPW-496 — 🟡 — no backfill path for orders already Paid before this deploy.
- PPW-497 — 🟡 — discovery manifest omitted ~24 changed files, including the VAT math itself.
- PPW-505 — 🟡 — fiscal-year numbering constraint can disagree between Postgres and .NET at a Dec 31/Jan 1 boundary.
- PPW-498 — ⚪ — Polly retry pipeline never disposes intermediate failed responses.
- PPW-499 — ⚪ — `AnafAuthHandler.CloneAsync` duplicates `SamedayAuthHandler`'s logic verbatim.
- PPW-500 — ⚪ — response-status classification duplicated between `AnafSpvClient.UploadAsync` and `GetStatusAsync`.
- PPW-501 — ⚪ — buyer-name fallback logic duplicated between `InvoiceXmlBuilder` and the PDF renderer.
- PPW-502 — ⚪ — invoice entity config uses a literal `"Sqlite"` string instead of the `DbProviders.Sqlite` constant.
- PPW-503 — ⚪ — `PostgresInvoiceNumberingService` interpolates the sequence name into raw SQL with no in-service validation.
- PPW-504 — ⚪ — `OrderDetailDto` grew 3 required fields with no lens covering the frontend contract.

## State

The ledger now holds PPW-469–505, all newly minted by this pass: 25 High/Medium rows open, 12 Low/Cleanup rows at backlog. The router proposes a fix round next — open High and Medium defects with no resolution. This is a full-loop-tier target (money, external input, migration), so it ends at certification: a separate, explicitly gated spend, not before the 10 High defects are fixed and independently verified, and not without folding in `tests-coverage`/`db-parity` as full lenses.
