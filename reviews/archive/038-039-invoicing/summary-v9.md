---
type: owner-summary
target: 038-039-invoicing
pass: 9
pass-type: delta-discovery
commit: c8d6bb4
date: 2026-08-21
decisions-needed: 8
---

# Owner summary — 038-039-invoicing v9

Five lenses searched everything that changed since the v6 pass — three fix rounds plus the rewrite that made
PostgreSQL the only database. They found 24 problems, 21 of them new; three are 🔴, and two of those were created by
this loop's own earlier fixes. The verdict is request-changes ([review-v9.md](review-v9.md)).

## Needs your decision

1. **Every parcel-locker order is now permanently un-invoiceable.** The guard the v6 round added refuses a blank
   buyer address, nothing fills one in for a locker order, so the invoice fails after the charge and the download
   stays a 404 for ever. *Your ruling comes first: the pre-check proved the locker holds no postal code at all, so
   which address a locker order carries is a decision, not a fix.* [PPW-557](ledger.md) · `InvoiceXmlBuilder.cs:131`

2. **Anyone on the internet can exhaust the API's memory in one request.** The Stripe callback accepts a body of any
   size and copies all of it into memory before it checks the signature. *Fix now, ~2h: reject anything over 1 MB
   before verifying, plus the two sibling endpoints the pre-check caught doing the same.* [PPW-558](ledger.md) · `WebhooksController.cs:69`

3. **A slow tax-authority call makes us file the same invoice twice.** The v6 round's fix claimed a timed-out upload
   keeps its place in the queue; it does not, because that hold expires 20 minutes before the next attempt.
   *Fix now, ~3h, together with [PPW-566](ledger.md): the pre-check found the drafted approach for both unbuildable
   as written, and named what to do instead.* [PPW-559](ledger.md) · `Anaf/InvoiceUploadJob.cs:345`

4. **The helper that makes a real test database is one decision, not five.** It builds about 100 databases per run on
   the machine you already call saturated, hard-fails where it used to skip, mislabels every failure as "server
   unreachable", and clears every connection pool in the process. *Choose the depth; the branch
   `chore/faster-relational-tests` already has work in flight here.* [PPW-561](ledger.md) · [PPW-562](ledger.md) · [PPW-563](ledger.md)

5. **A manual "mark as paid" that loses a race emails the customer a second confirmation** and overwrites the payment
   time the invoice was issued against. *Fix now, ~3h with [PPW-567](ledger.md) and [PPW-568](ledger.md), same file
   and same origin; the pre-check found a likelier second version of the race.* [PPW-564](ledger.md) · `AdminOrderService.cs:425`

6. **A database that ran the old setup steps cannot start on the new ones.** No such database exists yet, so this is a
   first-deployment decision. *Either write the one-time reseed into the deployment guide, or record that only fresh
   databases are supported.* [PPW-560](ledger.md) · `Migrations/20260820133204_InitialPostgres.cs:10`

7. **Nothing checks that the database plan still matches the code's model.** There is no drift today, but the next
   generated migration would inherit any. *Fix now, minutes: one assertion in the migration test.* [PPW-565](ledger.md)

8. **A test gap you already waived came back.** Both payment callbacks now share one failure-counting wrapper, only
   the Stripe half is tested, and the legacy processor half answers a different response format. *Re-affirm the waiver on the
   ground that the legacy processor is being removed, or overturn it — [PPW-511](ledger.md) still tracks that the removal is
   written down nowhere.* [PPW-526](ledger.md) · `Controllers/WebhooksController.cs:204`

## Reasons to doubt

- **Six of eleven lenses did not search this diff** — five is this pass type's cap
  ([runbook-discovery.md](../../runbooks/runbook-discovery.md)); the client side is still unreviewed at nine passes ([PPW-524](ledger.md)).
- **This pass type cannot certify** ([README](../../README.md)) — clean would have meant "this diff is clean", never
  "the feature is clean".
- **Each 🔴 rests on one lens plus a built trace**, not on lenses agreeing ([metrics.jsonl](metrics.jsonl), `conv: 1`),
  and eleven of the 24 got no adversarial check at all, by this pass type's own rules (`unverified-*`).
- **Seven findings had their topic planted by a shared prompt hint**, including the only three-lens agreement,
  [PPW-560](ledger.md) ([metrics.jsonl](metrics.jsonl), `hinted: true`).
- **[PPW-560](ledger.md) and [PPW-565](ledger.md) are `plausible`, not `confirmed`** — both traces found today's
  failing state unreachable.
- **Nine of the 21 new rows were created by an earlier fix in this same loop** ([metrics.jsonl](metrics.jsonl),
  `fix_generated`); new findings per pass run 37 at v1, 38 at v6, 21 here.
- **The suite state is a scoped run**: 382 passed and 0 failed ([metrics.jsonl](metrics.jsonl)), 10 of 392 skipped
  ([review-v9.md](review-v9.md)); and blinding is best-effort, with no tool checking that the lenses obeyed it.

## Filed automatically

Eleven minor rows went to the ledger backlog — seven 🟡, four ⚪ ([ledger.md](ledger.md)). One deserves your eye:
[PPW-572](ledger.md) — the "alert only once" helper can tell two callers both that they are first, harmless today
only because every current user runs on a single background loop.

## State

Three 🔴 open, so the loop re-arms into a fix round, 🔴 first, and certification moves further away. Eight approach
pre-checks already sit on the ledger rows — one refuted, seven revised — so the fix round starts from them.
