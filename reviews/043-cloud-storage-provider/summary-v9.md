---
type: owner-summary
target: 043-cloud-storage-provider
pass: 9
pass-type: certification (single-pass, recorded deviation)
commit: ac97e42
date: 2026-07-27
decisions-needed: 0
ruled: 2026-07-27
---

# Bolt 043 (cloud photo storage) — certification summary

**Certified clean of serious defects** on 2026-07-22: 0 High, 0 fix-caused regressions; the
data-loss bugs found at v7 (a retried upload silently truncating a paid photo; shared photos
deleted while another order still needed them) are fixed and independently verified
([review-v9](review-v9.md) · [v8 verification](review-v8.md) · suites .NET 719/719, FE 439/439).

## Needs your decision (3) — all ruled 2026-07-27 ([resolution-v9](resolution-v9.md))

1. **A customer who just paid can see "Fotografiile … nu mai sunt disponibile"** on their
   order page in the minutes before photos finish moving to cloud storage — wrong message,
   real customers. Suggested: **fix now** (~small: gate the message on order/photo state,
   `order-detail-page.ts`) · [D83](ledger.md) → **Ruled: fix now — fixed `d041295`+`b9af326`**
2. **The EuPlatesc payment path is never tested for triggering photo archiving** (the Stripe
   twin is tested; the wiring exists but no test asserts it). Suggested: **fix now** (~1 test,
   `PaymentControllerIntegrationTests.cs`) · [D84](ledger.md) → **Ruled: wont-fix — EuPlatesc
   is slated for removal**
3. **The photo-backfill admin command got lighter review scrutiny** than everything else, and
   running it while the live archiver works is untested. Suggested: **defer** to the
   pre-deployment (3-env) phase — it's an operator tool, unused until then · [D85](ledger.md)
   → **Ruled: defer to 3-env**

## Reasons to doubt

- Certification ran as **one** full review, not the usual two, under the recorded deviation
  (a full pair had just audited near-identical code at v7) · [review-v9](review-v9.md)
- The v7 pair's overlap math estimates **~7 serious problems may remain unfound** — a lower
  bound, since all reviewers share one model · [overlap-pair-v7.md](overlap-pair-v7.md)
- The certificate means *no serious defect survives*, not *zero defects*: a second pass would
  re-mine Mediums · [review-v9](review-v9.md)
- New-findings trend across full passes is not decaying (v7: 34 → v9: 30 canonical), typical
  for first-certification features · [metrics.jsonl](metrics.jsonl)

## Filed automatically

5 minor findings → ledger backlog ([D86–D88, D90](ledger.md) + [D89](ledger.md)). One worth an
eye: **D89** — 67 code comments across 27 files cite finding/review IDs, now banned by the
CLAUDE.md comment rule; wants a dedicated cleanup sweep someday.

## State

Loop **complete** — no further review warranted. Triage ruled 2026-07-27; a verification
pass on the D83 fix (`d041295`) is the last box to tick, then the branch is merge-ready.
