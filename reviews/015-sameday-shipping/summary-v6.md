---
type: owner-summary
target: 015-sameday-shipping
pass: 6
pass-type: verification
commit: 1816f5f
date: 2026-07-29
decisions-needed: 2
---

# Summary v6 — 015-sameday-shipping (verification)

I re-checked the big fix round by deleting each fix and watching whether a test caught it.
**37 of the 41 fixes held. 4 vanished without a single test noticing** — [review-v6.md](review-v6.md).
The best news: the DI wiring bug (D65) was **real**, and turning on the Sameday flag before the
fix would have crashed the first courier call.

## Needs your decision

1. **🟠 Four fixes have no test guarding them — approve a short fix round?**
   [D27, D39, D71, D79](ledger.md#v6--verification-2026-07-29-commit-1816f5f-approve-with-followups) are all written
   correctly, but I removed each one completely and both test suites stayed fully green
   (916 + 457 passing). Nothing would stop a future edit from silently undoing them. The riskiest
   is **D79** — the signed-in shopper's name auto-filling into the delivery form. That is the
   guest/signed-in saved-details area that this project has broken and re-broken more than any
   other (CLAUDE.md class 11).
   *Suggested action:* fix now — **~1–2 h**, four small tests, no production code changes needed
   (D39 is literally one extra line inside the test that already exists,
   [SamedayCompositionRootTests.cs:57](../../src/PhotoPrint.Tests/Unit/Services/Sameday/SamedayCompositionRootTests.cs#L57)).
   *Alternative:* accept them as untested. Reasonable only because the whole courier feature is
   still switched off — but then they must go on the pre-enable checklist, not be forgotten.

2. **🟠 One more certification round before you call this feature done?**
   The fix round changed genuinely risky machinery: the lock that stops the courier being paid
   twice for one parcel, a database change, the money-carrying order path, and the whole
   service-wiring file. [reviews/README.md](../README.md#entry-tiers--does-a-change-get-the-loop-at-all)
   says that grade of change always ends in a certification pass, and the "small backlog fix is
   exempt" shortcut explicitly does not apply when it touches this tier.
   *Suggested action:* run one fresh full pass **after** the fix round — roughly the cost of
   [v5](metrics.jsonl) (~2.9M tokens, ~48 agents), and I would run **one** pass, not two, because
   v5 ran yesterday on almost the same code.
   *Alternative:* sign it off without one and record that choice in the index, which
   [README note ²](../README.md) allows for lower tiers. **Practical consequence either way:
   [v5's "CERTIFIED" now covers older code](review-v5.md) — 41 fixes landed after it.**

## Reasons to doubt

- **This pass cannot certify anything.** A verification pass is capped at
  `approve-with-followups` by [design](../README.md#severity--verdicts) — it only proves "these
  specific fixes hold", never "the feature is clean". No lens searched for new problems.
- **I still found 7 things nobody was looking for** ([D90–D96](ledger.md)), which is a hint that a
  real searching pass would find more. One is a lifetime bug in the wiring that has been there
  since the start ([D90](review-v6.md#new-findings-7--all-backlog-none-re-arms-the-loop)).
- **Two fixes that "held" are weaker than they look.** [D68's](review-v6.md#recorded-gaps-on-fixes-that-held)
  third failure path is untested, and [D78's](review-v6.md#recorded-gaps-on-fixes-that-held)
  new check literally cannot fail — Angular renders a missing value as blank text, never the word
  "undefined" the test looks for.
- **New-problem trend, full search passes only** ([metrics.jsonl](metrics.jsonl)): v1 found 41,
  v3 found 12, v5 found 42. That is **not** a decaying curve — v5 rose again because it was the
  first pass to look at the finished job code. Do not read "certified" as "saturated".
- **Nothing is proven against the real production database.** [D23](ledger.md) is still deferred,
  and this round *added* a migration, so there is now more untested database code than when that
  deferral was written.
- **[D45](review-v5.md#the-d45-vendor-idempotency-residual--re-confirmed-already-accepted) is
  unchanged:** not paying Sameday twice for one parcel still rests on Sameday's own de-duplication,
  which nobody has confirmed with Sameday. **Confirm this before switching the feature on.**

## Filed automatically

7 new low/cleanup items ([D90–D96](ledger.md)) went to the ledger backlog; the 5 upheld earlier
decisions (D72, D81, D40, D83, D89) stay there too. One worth your eye anyway: **[D89](review-v6.md#non-fixed-dispositions--all-upheld)** —
keeping the unused "download the shipping label" code was justified by an admin screen that is not
written down on any checklist. If that screen never gets built, the code stays dead and the
"downloadable label" goal stays undelivered.

## State

Router: 4 reopened fixes **re-arm the loop**
([README](../README.md#what-re-arms-the-loop--exactly-three-things)), so next is a **fix round**,
then verification, then certification. Nothing was pushed; the courier feature remains switched off
behind `Sameday:Enabled=false` and `Sameday:Jobs:Enabled=false`, so none of this is live.
