---
type: owner-summary
target: 038-039-invoicing
pass: 12
pass-type: certification
commit: 090873d
date: 2026-08-21
decisions-needed: 7
---

# Owner summary — 038-039-invoicing v12

Eleven lenses searched the whole invoicing feature at the frozen commit `090873d`. They found 96
problems, 79 of them new, and eleven are 🔴 — so **this pass does not certify** and the loop
re-arms ([review-v12.md](review-v12.md)). Two of the eleven hurt a paying customer on day one, and
our tests could see neither: one needs the production image, and five of the eleven come from a lens
that had never run here.

## Needs your decision

1. **No invoice PDF can ever be produced in production.** The image we deploy carries no language
   data, and the invoice document asks for Romanian formatting as it loads, so the first render
   throws and the worker retries for ever: no PDF, no filing to the tax authority, a permanent 404
   on the download. *Fix now, ~1h, as a release gate: add the language package to the image, plus a
   test that renders with the invariant-language switch on — how the lens reproduced it.*
   [PPW-579](ledger.md) · `Services/Invoicing/InvoicePdfDocument.cs:19`

2. **The shop can charge one customer twice for one basket.** The website never sends the
   duplicate-payment key the API was built to read, and the payment page makes a new order on every
   load, so two tabs — or Back then forward — mean two orders, two charges, two invoice numbers. The
   server half of that protection exists and is tested. *Fix now, ~1d; the pre-check revised the
   design — browser-wide storage for the key, and the page must handle the conflict answer.* [PPW-584](ledger.md)

3. **A guest who pays is sent to the homepage**, basket already emptied, because the confirmation
   page reads the order before the payment webhook marks it paid. The pre-check found the obvious
   fix impossible in the website alone: a guest cannot read that order at all. *Decide the scope —
   a small server change plus polling, or an order read that reconciles with the payment
   processor.* [PPW-582](ledger.md)

4. **Re-raise of your `wont-fix` on the retry posture.** Three lenses independently found one upload
   can be re-sent to the tax authority up to eight times, with no test pinning how many sends a
   single call makes. *Re-affirm or overturn; the prior ruling is on the row.* [PPW-489](ledger.md)

5. **Re-raise of your deferral on the missing website invoice.** The endpoint accepts guests, but
   nothing links to it and guests have no order page, so the legally required invoice is unreachable
   for our main customer type. *Re-affirm or overturn.* [PPW-524](ledger.md)

6. **Five rows where the code contradicts something written down** — the accepted numbering-gap
   decision, the promised invoice e-mail, the documented retry schedule, the admin panel's contract,
   and the locker address rule no story records. *Rule on each first; these are contract choices,
   not bugs.* [PPW-589, PPW-597, PPW-600, PPW-608, PPW-633](ledger.md)

7. **This ran as one pass, not the pair the protocol asks of a first certification**, on your ruling
   of 2026-08-21. *Decide whether a second independent pass runs after the fix round.*
   [metrics.jsonl](metrics.jsonl)

## Reasons to doubt

- No lens is owed now, but `frontend-ux` ran here for the **first time in twelve passes** and gave 5
  of the 11 🔴 rows — that says as much about our coverage as about the code
  ([review-v12.md](review-v12.md)).
- New problems per full pass went **up**, not down: 37 at v1, 79 here. A falling count is our
  "ready to close" signal, and it is absent ([metrics.jsonl](metrics.jsonl)).
- 84 of the 96 findings came from a single lens, only 2 had three or more lenses agree, six are
  `plausible` rather than confirmed (PPW-606, PPW-608, PPW-610, PPW-617, PPW-623, PPW-633), and the
  14 ⚪ rows got no adversarial check at all ([metrics.jsonl](metrics.jsonl)).
- Design pre-checks ran for only 6 of the 47 serious rows, to keep the cost under the pass itself.
  **All six came back `revised`**, so the suggested fixes on the other 41 are likely wrong too.
- Five findings sit on topics a shared prompt hint planted (PPW-563, PPW-594, PPW-618, PPW-626,
  PPW-639), so they are not independent discoveries. Blinding stays best-effort: nothing verifies it.
- The cloud-storage suite (10 tests) is skipped for want of credentials, so the download's cloud path
  is proven only against fakes, and PPW-618 shows its miss-cause rule cannot tell the two causes apart.

## Filed automatically

39 minors — 28 🟡 and 11 ⚪ — entered the ledger as `backlog` (PPW-619 to PPW-657). One deserves your
eye anyway: [PPW-621](ledger.md) caches a customer's invoice PDF in the browser for a year with no
revalidation, so a shared computer serves it again after logout.

## State

Eight new 🔴 and 32 new 🟠 rows stand `open`, 39 minors are backlogged, and nothing changed to
`fixed` or `verified`. Fourteen findings re-raised existing rows, thirteen of them already decided,
and three suspicions were refuted. Next is a fix round over the 47 serious rows, then a
re-certification — this commit is not certified.
