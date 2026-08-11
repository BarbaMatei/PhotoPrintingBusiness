---
type: owner-summary
target: 015-sameday-shipping
pass: 3
pass-type: discovery
commit: 8584572
date: 2026-07-27
decisions-needed: 2
---

# Owner summary — 015-sameday-shipping v3

Not certified. Two independent blinded full audits of the frozen code both ran, and they found 3 High defects, so this goes back for another fix round rather than closing ([review-v3.md](review-v3.md); detail per D# in ledger.md). The pair earned its cost: it caught a checkout bug the earlier same-session check missed, which is exactly why an independent pass exists. Tests are still green, 893 backend and 451 frontend. The Sameday jobs are still off behind the two flags, but two of the three worst defects are in the checkout screen, which is live regardless of those flags.

## Needs your decision

1. 🔴 Fix the three worst defects now — PPW-282, PPW-283, PPW-284, with PPW-285 riding along. Suggested: yes, fix now, about half a day plus the missing tests the pair asked for (PPW-286 to PPW-291).
   - Locker checkout dead-ends (PPW-282, both passes): after a customer picks a locker and types their name and phone, the "Continuă" button never re-enables, so they cannot reach payment. This is a live regression the round-1 fix introduced and the same-session verification missed. PPW-285 is its twin: the stepper lets the customer skip to payment and hit a 400.
   - A slow Sameday response kills delivery tracking (PPW-283, one pass): one request over the 10-second timeout stops the tracking loop entirely until the application restarts, so deliveries stop being detected. Pre-existing.
2. 🔴 Duplicate label safety — accept the residual, or build a real guard? Both passes independently flagged that nothing stops two label-creation calls reaching Sameday for one order, from a retry or a second server; the database check only blocks the second save, not the second billed label. Today it rests on Sameday deduplicating on its side, which nobody has confirmed. Suggested: decide — (a) accept it as an interim risk and keep the orphan warning, which is what the code does now and costs nothing, or (b) add a durable per-order claim before the vendor call, about half a day, which removes the risk. Same theme: PPW-293, a cancelled-mid-call order leaves an orphaned paid label with no automatic void, and PPW-270, the manual admin label endpoint can double-book.

## Reasons to doubt

- This pass was genuinely independent and blinded, two fresh full-manifest runs, so it is stronger evidence than the v2 verification, which ran in the same session and missed PPW-282 and PPW-285 ([metrics.jsonl](metrics.jsonl), pass 3).
- PPW-283 and PPW-286, the timeout bug, were found by only one of the two passes. Real — the synthesizer confirmed it against the code — but a single-pass find is weaker signal than the cross-pass ones, PPW-282, PPW-284, PPW-285 and PPW-288.
- PPW-284 cannot be settled from our side. It depends on how Sameday actually handles a repeated reference, which needs the vendor to confirm.
- A certification pass cannot approve when it finds new High defects. By rule it returns `request-changes` and the loop restarts.

## Filed automatically

The backlog Low and Cleanup rows, PPW-259 to PPW-280, were re-found and carry their prior deferred decisions unchanged; each is described on its ledger.md row. The exception is PPW-270, which the pair re-opened and raised to Medium for the duplicate-label risk, so it joins this fix round.

## State

A new High defect restarts the loop, so the router proposes a fix round next, worst first: PPW-282, PPW-283, PPW-284. Then re-verification, then a fresh certification. I would do the two checkout regressions, PPW-282 and PPW-285, first, since they affect live checkout, and I want your call on PPW-284 before touching the label-creation path.
