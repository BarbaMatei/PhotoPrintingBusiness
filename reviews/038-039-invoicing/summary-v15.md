---
type: owner-summary
target: 038-039-invoicing
pass: 15
pass-type: delta-discovery
commit: 5fca3cf
date: 2026-08-27
decisions-needed: 3
---

# Owner summary — 038-039-invoicing v15

Five blinded lenses read everything that changed since the last full pass — the EuPlatesc
removal, the payment-flow cluster and the round that closed every remaining ledger row — and
found **28 defects: 5 serious, 12 medium, 10 minor, 1 cleanup**. The verdict is
**request-changes**. The uncomfortable part is where they came from: **15 of the 28 were
introduced by the last two fix rounds**, and one more by the EuPlatesc removal. The round
closed 26 rows and broke roughly a dozen things doing it.

## Needs your decision

- **The dev database is already broken and I can fix it (PPW-663).** The EuPlatesc removal
  deleted three columns by editing the migration that had *already run*, so PostgreSQL never
  hears about it. On your machine `Orders.PaymentProcessor` is still `NOT NULL` with no default
  while the code no longer sets it — the next checkout there dies. The fix is to put the old
  migration back as it was and add a new one that drops the columns properly. **Suggested: let me
  do it**; it is the one item here that stops the app working today.
- **How to end a declined payment (PPW-660, PPW-662).** Today a declined card leaves the order
  marked failed but its payment still chargeable, and the retry button re-uses it. If the second
  try succeeds the money is taken and the order can never be marked paid — not even by an admin.
  Two ways out: let a failed order become paid, or force the retry to start a brand-new payment.
  **Suggested: both**, since either alone leaves a hole.
- **The ten minor rows need your usual triage.** I left them `open` rather than guessing; the 87
  already in the backlog got there by your ruling, not mine. Four of them are about the test
  database helper rather than the product.

## Reasons to doubt

- **Only five of eleven lenses ran** (`correctness`, `race`, `frontend-ux`, `security`,
  `db-parity`), because a delta pass is capped at five. `requirements`, `quality` and
  `tests-coverage` did not look at this code. The last time a lens ran for the first time on this
  target it produced five of eleven serious rows, so absence here is not evidence of health.
- **Nothing was auto-matched against your 97 decided rows.** I passed that list in a form the
  script does not read (filed as SF40), so a defect you already ruled on could appear here as new.
  Two do: PPW-673 repeats backlog row PPW-644, and PPW-681 repeats half of PPW-621.
- **Every one of the 17 findings that faced a skeptic survived it.** A pass where nothing is
  refuted is either an honest pass or an uncritical one; the traces read as concrete, and one
  (PPW-663) I confirmed by hand against the live database, which is the reason I lean honest.
- **The verification that preceded this pass was weaker than its own summary suggested.** Ten of
  its 21 mechanical proofs could have reddened from a compile error rather than a failing test;
  hand proofs closed that, but it took a second pass to notice.

## Filed automatically

Nothing. All 28 rows are `open` and awaiting the fix round or your triage.

## State

The ledger went from **no open row** to 28 open: PPW-659…PPW-686. The 26 rows fixed in the v13
round stay `verified` — none of them reopened; these are new defects beside them, mostly in the
same code. The next pass is a fix round on the five serious rows plus the regressions this loop
caused; certification cannot run until that clears, and closure still waits for you.
