---
type: owner-summary
target: 015-sameday-shipping
pass: 3
pass-type: certification-pair
commit: 8584572
date: 2026-07-27
decisions-needed: 2
---

# Owner summary — 015-sameday-shipping v3 (certification)

**Not certified.** Two independent, blinded full audits of the frozen code both ran, and they found
**3 serious problems** ([review-v3.md](review-v3.md)) — so this goes back for another fix round rather
than closing. The pair earned its cost: **it caught a checkout bug the earlier same-session check
missed**, which is exactly why an independent pass exists. Tests are still green (893 / 451); the
Sameday jobs are still off behind the two flags — but two of the three blockers are in the **checkout
UI, which is live regardless of those flags.**

## Needs your decision

1. **Fix the blocker cluster (recommended: yes, fix now).** Three serious defects:
   - **Easybox checkout dead-ends** — after a customer picks a locker and types their name + phone, the
     "Continuă" button never re-enables, so they can't reach payment. This is a **live** regression my
     earlier fix introduced and my own (same-session) verification missed. ([D43](review-v3.md), both
     passes; also **D46** — the stepper lets them skip to payment and hit a 400.)
   - **A slow Sameday response kills delivery tracking** — one request over the 10s timeout stops the
     tracking loop entirely until the app restarts; deliveries stop being detected. Pre-existing.
     ([D44](review-v3.md).)
   → **Fix all three (~half a day) + add the missing tests** (EuPlatesc label enqueue, dispatcher
   retry, the mid-call guard) the pair flagged (D47–D52).
2. **AWB duplicate-safety — accept the residual, or build a real guard?** Both passes independently
   flagged that nothing stops two label-creation calls going to Sameday for one order (a retry or a
   second server); the database check only blocks the second *save*, not the second *billed label*.
   Today it rests on Sameday deduping server-side, which we haven't confirmed. → **Decide:** (a) accept
   it as an interim risk + keep the orphan warning (cheap, what the code does now), or (b) add a durable
   per-order "claim" before the vendor call (~half a day, removes the risk). ([D45](review-v3.md), both
   passes, 3 lenses each.) Same theme: **D54** (a cancelled-mid-call order leaves an orphaned paid label
   with no auto-void) and **D31** (the manual admin AWB endpoint can double-book).

## Reasons to doubt

- **This pass was genuinely independent + blinded** (two fresh full-manifest runs) — stronger than the
  v2 verification, which ran in the same session and missed D43/D46. That gap is now closed by evidence.
- **D44/D47 (the timeout bug) were found by only one of the two passes** — real (I confirmed it against
  the code), but a single-pass find is weaker signal than the cross-pass ones (D43, D45, D46, D49).
- **D45 can't be fully judged from our side** — it depends on how Sameday actually handles a repeated
  reference, which needs vendor confirmation.
- A certification pass **cannot approve** on a re-find of new serious defects — by rule it returns
  request-changes and the loop re-arms.

## Filed automatically

The backlog Low/Cleanup items (D20–D41) were re-found and carry their prior "deferred" decisions
unchanged ([ledger.md](ledger.md)) — except **D31**, which the pair re-opened and raised to Medium
(duplicate-label risk) for this fix round.

## State

Router: a new 🔴 re-arms the loop → **fix round next** (blocker-first: D43, D44, D45), then
re-verification, then a fresh certification pair. Say the word to start the fix round — I'd suggest
doing the two checkout regressions (D43, D46) first since they affect live checkout, and getting your
call on D45 before I touch the AWB path.
