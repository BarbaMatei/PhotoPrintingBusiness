---
type: owner-summary
target: 015-sameday-shipping
pass: 5
pass-type: certification
commit: 5fc330b
date: 2026-07-28
decisions-needed: 1
---

# Sameday shipping — certification (v5): **Certified**

The review loop is **done**. One last independent pass looked at the whole feature fresh and found
**no serious bug** — nothing exploitable, nothing broken by earlier fixes, nothing that regressed.
So the code is certified. It found a pile of smaller gaps, but every one of them can only happen
*after* you turn Sameday on, and it's still off.

## Needs your decision

**One, and it's not urgent.** The pass found 12 medium-sized gaps ([D55–D66](review-v5.md#the-follow-up-list--a-pre-enable-checklist-none-live-today)) that are worth fixing *before* you switch Sameday on — but none can happen today (both switches are off).
- **Recommended: defer them.** Park the whole list as a "before we enable Sameday" checklist and
  do it as one focused round when you're actually ready to go live. The feature is dormant, so
  there's no cost to waiting and no reason to spend on it now.
- Alternative: I clear the checklist now in one fix round. More spend today, for a feature you
  aren't enabling yet.

Either way the certification stands — this choice is only about *when*, not *whether*.

## Reasons to doubt

- **One pass, not the usual two** — you approved that shortcut. Two independent passes ran last
  round ([v3](review-v3.md)) on almost the same code, so the risk is low, but one pass sees less.
- **A platform outage hit the first attempt** — 34 of 64 review agents failed on Anthropic 500
  errors (the whole checking stage). I re-ran only the failed parts from a cached checkpoint, and
  the second attempt completed clean with **0 errors** ([metrics](metrics.jsonl) pass 5), so the
  final result is whole — but it took two attempts and ~5.2M tokens total.
- **Two risks can't be proven until you enable Sameday:** the app has never been started with the
  Sameday switch on, so a possible wiring loop ([D65](findings-v5.md)) would first show up in
  staging; and avoiding a double courier charge still rests on Sameday's own duplicate-detection,
  which we haven't confirmed with them ([D45 residual](review-v5.md), ADR-015).
- **17 new medium gaps on a feature already reviewed three times** reads like a lot — but they're
  mostly missing *tests* and problems that only bite once the flags flip, not live defects.

## Filed automatically

35 findings (D55–D89: 17 medium, 19 low, 6 cleanup, 1 refuted) went to the [ledger](ledger.md)
backlog as the pre-enable checklist. Four earlier deferrals (D50/D23/D29/D39) were re-checked and
still stand. **One to keep your eye on:** [D56](findings-v5.md) — the label PDF an admin would print
is saved but never shown anywhere in the admin screen, so "download the label" isn't actually usable
yet.

## State

Loop **closed — Certified** (`5fc330b`, backend 898 / frontend 452 green). Nothing pushed. Per your
roadmap this feature waits; the D55–D66 checklist is the gate to revisit before Sameday goes live.
