---
type: owner-summary
target: 043-cloud-storage-provider
pass: 9
pass-type: certification
commit: ac97e42
date: 2026-07-27
decisions-needed: 3
---

# Owner summary — 043-cloud-storage-provider v9

The cloud photo storage feature is certified clean of serious defects at `ac97e42`: no High findings, and nothing broken by the previous round's fixes ([review-v9.md](review-v9.md)). The two data-loss bugs found days earlier are fixed and independently checked — a retried upload that silently truncated a paid customer's photo, and shared photos deleted while another order still needed them. Both suites pass, 719 backend and 439 frontend. The pass found ten Medium items; three need your decision and none of them re-opens the review.

## Needs your decision

1. 🟠 A customer who has just paid can see "Fotografiile … nu mai sunt disponibile" on their order page, in the minutes before the photos finish moving to cloud storage — the wrong message, shown to real customers. Suggested: fix now, a small change gating the message on the order's state in `order-detail-page.ts`. PPW-232 on [ledger.md](ledger.md). Ruled 2026-07-27: fix now. Fixed at `d041295` and `b9af326`.
2. 🟠 The EuPlatesc payment path is never tested for triggering photo archiving. The wiring exists and the Stripe twin has a test, but nothing asserts this one, so deleting the call would go unnoticed. Suggested: fix now, about one test. PPW-233 on [ledger.md](ledger.md). Ruled 2026-07-27: wont-fix, because the EuPlatesc gateway is slated for removal.
3. 🟠 The photo-backfill admin command got lighter review scrutiny than everything else, and running it while the live archiver works is untested. Suggested: defer to the three-environment stage, since it is an operator tool nobody uses before then. PPW-234 on [ledger.md](ledger.md). Ruled 2026-07-27: defer to the three-environment stage.

## Reasons to doubt

- Certification ran as one full review rather than the usual two, an approved deviation justified by the pair that audited near-identical code days earlier ([review-v9.md](review-v9.md)).
- That earlier pair overlapped on only 4 of its 34 findings, and on 2 of its 12 serious ones. The standard estimate from that overlap puts about 19 serious problems within reach of the search, so roughly 7 serious ones may still be unfound. Treat it as a floor rather than a ceiling, because every reviewer here runs on the same model.
- The certificate means no serious defect survives, not that none remain. A second independent pass would surface a different set of Medium items ([review-v9.md](review-v9.md)).
- New findings are not tailing off across full passes: 34 at the certification pair, 30 here ([metrics.jsonl](metrics.jsonl)). That is normal for a feature reaching certification for the first time.
- The metrics line for this pass counts 5 new Medium, 6 new Low and 2 new Cleanup items, while its own note and the ledger name 3 new Medium, 4 new Low and 1 new Cleanup. The metrics file is append-only and was never corrected, so the ledger identities are the ones to trust ([metrics.jsonl](metrics.jsonl)).

## Filed automatically

Five minor findings went to the ledger backlog at this pass: PPW-235, PPW-236, PPW-237, PPW-239 and PPW-238, each described on its [ledger.md](ledger.md) row. One deserves your eye: PPW-238 — 67 code comments across 27 files name finding, review and design-record identifiers, which the repository comment rule now bans. It wants a dedicated cleanup sweep rather than a per-file scramble. The ledger row carries whatever happened to each of them after this pass.

## State

The review loop is complete. The serious-defect population is closed and independently confirmed closed, so no further discovery or certification pass is warranted. Triage was ruled on 2026-07-27 ([resolution-v9.md](resolution-v9.md)): PPW-232 fixed, PPW-233 wont-fix, PPW-234 deferred. What remains is a follow-up list that does not gate closure — the cluster deferred to the concurrency-token work, the two items deferred to the three-environment stage, and the 34 backlog rows carried to the backlog.
