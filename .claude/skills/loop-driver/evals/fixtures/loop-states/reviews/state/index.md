---
type: review-index
updated: 2026-09-01
---

# Review index — loop-driver eval fixture

Synthetic records for the loop-driver skill evals. Nothing here describes real code: every
target below is a router state, built so one eval case lands on one row. The commits are real
commits of this repository, because the auditor resolves every sha it reads.

## Targets at a glance

| Target | State |
|---|---|
| 091 fixround | One 🔴 open in the ledger, no resolution — the loop is armed. |
| 094 quiet | Round 1 verified clean at v2; the delta-worthiness call is open. |
| 095 postcert | Certified at v2, then one post-cert round resolved and never verified. |
| 096 rotten | Two passes ran; the pass-2 metrics line was lost, so the auditor is red. |
| 097 mockpass | Round 1 resolved at `147fa87`; its verification has not run. |

## Passes

| Date | Target | Pass | Verdict | New H/M/L/C | Outcome | Files |
|---|---|---|---|---|---|---|
| 2026-08-29 | 094 | v2 verification (anchored) | approve-with-followups | 0/0/0/0 | Revert-and-rerun held the guest-cart merge fix; nothing reopened. | [ledger](../094-quiet/ledger.md) |
| 2026-08-29 | 094 | v1 fix round (1 cluster, 0 approach-checks, 1 micro-review) | — (resolved) | 0/0/0/0 | Round 1 fixed the guest-cart merge and added the both-sides regression test. | [resolution](../094-quiet/resolution-v1.md) · [ledger](../094-quiet/ledger.md) |
| 2026-08-31 | 095 | v2 certification (single) | approved | 0/1/0/0 | Certified with one 🟠 filed; the post-cert round that answers it is resolved and unverified | [review](../095-postcert/review-v2.md) · [ledger](../095-postcert/ledger.md) |
| 2026-08-30 | 097 | v1 discovery (11 lenses) | request-changes | 1/0/0/0 | One 🔴: the invoice total ignores the shipping line | [review](../097-mockpass/review-v1.md) · [ledger](../097-mockpass/ledger.md) |
| 2026-08-29 | 096 | v2 delta discovery (5 lenses) | approve | 0/0/0/0 | Nothing new; the pass-2 metrics line is the one this fixture is missing | [review](../096-rotten/review-v2.md) |
| 2026-08-28 | 096 | v1 discovery (11 lenses) | approve-with-followups | 0/0/1/0 | One 🟡, backlogged | [review](../096-rotten/review-v1.md) |
| 2026-08-26 | 095 | v1 discovery (11 lenses) | approve | 0/0/0/0 | Clean full pass; the certification followed it | [review](../095-postcert/review-v1.md) · [ledger](../095-postcert/ledger.md) |
| 2026-08-25 | 094 | v1 discovery (11 lenses) | request-changes | 1/0/0/0 | One 🔴: the guest cart merge drops the signed-in items | [review](../094-quiet/review-v1.md) · [ledger](../094-quiet/ledger.md) |
| 2026-08-25 | 091 | v1 discovery (11 lenses) | request-changes | 1/0/0/0 | One 🔴: the discount clamp is missing | [review](../091-fixround/review-v1.md) · [ledger](../091-fixround/ledger.md) |
