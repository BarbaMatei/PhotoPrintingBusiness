---
type: owner-summary
target: 038-039-invoicing
pass: 6
pass-type: delta-discovery
commit: 1c217f4
date: 2026-08-20
decisions-needed: 4
---

# Owner summary — 038-039-invoicing v6

## Needs your decision

1. **The API will not start on a fresh production database.** The invoice table has a uniqueness
   rule built on "the year this invoice was issued". Postgres refuses to build it, because the
   year comes from a timestamp whose meaning depends on the time zone, and only fixed expressions
   are allowed there. The first boot against real Postgres fails, undoes its own setup, and the
   application never comes up. Dev builds its database another way and never runs this step.
   *Fix now, ~1h, with a test that applies the setup steps to real Postgres in CI — none exists.*
   [PPW-513](ledger.md) · `Migrations/20260603101910_AddVatAndInvoices.cs:112`

2. **Locker orders produce legally invalid invoices.** With Easybox pickup the locker supplies
   the address, so checkout sends empty street, city and postal code. Nothing fills them in, and
   the builder copies them into the e-Factura document where all three are mandatory.
   *Fix now, ~2h: fall back to the locker address or require the fields, and reject a blank buyer
   address in the builder.*
   [PPW-512](ledger.md) · `Services/Invoicing/InvoiceXmlBuilder.cs:121`

3. **A timed-out call to the tax authority stops the invoice worker, and the API with it.** The
   outbound call gives up after 30 seconds and reports that as a cancellation, which nothing on
   the path treats as a failure worth recording, so it escapes the background worker — and a
   worker that throws shuts the host down.
   *Fix now, under 1h: translate the timeout into the "unreachable" failure already handled.*
   [PPW-515](ledger.md) · `Services/Invoicing/Anaf/InvoiceUploadJob.cs:81`

4. **When invoice numbering gives up, the card reference vanishes.** The money is taken, the
   order is deliberately left unpaid, and an error is written for manual reconciliation — but the
   processor's reference for that charge is wiped moments earlier and omitted from the error.
   *Fix now, minutes: log the processor reference, order number and amount before the rollback.*
   [PPW-514](ledger.md) · `Controllers/WebhooksController.cs:427`

## Reasons to doubt

- **Six of eleven lenses did not run** — the cap for this pass type is five
  ([runbook-discovery.md](../../runbooks/runbook-discovery.md)). `security`, `requirements`,
  `quality`, `input-validation`, `race` and `frontend-ux` never searched this work
  ([review-v6.md](review-v6.md) frontmatter).
- **The client side has never been reviewed at all**, across six passes ([PPW-524](ledger.md)).
- **This pass type cannot certify** — its verdict is capped by design
  ([README](../../README.md)). Clean here means "this diff is clean", never "the feature is clean".
- **New findings per pass are not decaying**: 37 at v1, 38 here, after four rounds of fixing
  ([metrics.jsonl](metrics.jsonl)).
- **PPW-521, PPW-524 and PPW-531 are `plausible`, not `confirmed`**, and the 15 minor rows got no
  adversarial check, per this pass's budget rules ([metrics.jsonl](metrics.jsonl)).
- **Blinding was weaker than intended.** The prior-findings list reached the deduplication step
  as a file path, not inline; it reported zero re-raises where synthesis found three by hand
  (PPW-516, PPW-519, PPW-526 on the [ledger](ledger.md)), so the file was probably never read.
  Overlap counts from this pass are not trustworthy ([metrics.jsonl](metrics.jsonl) notes).

## Filed automatically

15 minor rows went to the ledger backlog — nine 🟡, six ⚪ ([ledger.md](ledger.md)). One deserves
your eye: [PPW-539](ledger.md) — the new column and uniqueness rule never reach an existing dev
database, so anyone who keeps their local copy silently runs the old shape.

## State

Four 🔴 rows open, so the loop re-arms into a fix round, 🔴 first. Certification is further away
than before this pass, and still needs your explicit go-ahead.
