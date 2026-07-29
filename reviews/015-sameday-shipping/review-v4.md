---
type: review
target: 015-sameday-shipping
version: 4
supersedes: 3
commit: 5fc330b
branch: feat/bolt-036-sameday-api-client
pass-type: verification
date: 2026-07-27
reviewer: anchored verification (revert-and-rerun + the v3 fix-diff micro-review)
verdict: approve-with-followups
findings: { verified: 20, deferred: 1, reopened: 0, new: 0 }
tests: { dotnet: "898/898 (+10 skipped MinIO)", frontend: "452/452" }
---

# Review v4 — 015-sameday-shipping (verification of the v3 fix round)

Anchored verification of [resolution-v3.md](resolution-v3.md) (the certification fix round) at
`5fc330b`. Confirms the fixes held; capped at `approve-with-followups`. **0 reopened, 0 new.**

## Method & result

**Revert-and-rerun (mechanical) — the 3 blockers, clean attribution, zero collateral:**

| Bug re-introduced | Red test | Finding |
|---|---|---|
| `canContinue` reads `easyboxContactForm.valid` (non-signal) again | `typing the Easybox contact … re-enables Continue` (451 others pass) | D43 |
| remove the per-poll `OperationCanceledException` catch | `A_poll_timeout_does_not_fault_the_tick` | D44 |
| remove the claim-freshness predicate | `Second_creator_skips_…_when_a_fresh_claim_is_held` | D45 |

Each bug reddened exactly its own regression test and nothing else — non-vacuous.

**Verified via the v3 fix-diff micro-review (two independent fresh-context agents, both clean) +
inspection — 17 findings:** D46, D47, D48, D49, D51, D52, D54, D31, D53, and the folded backlog D21,
D22, D24, D26, D28, D36, D41. The micro-review explicitly checked "no test passes only because a
change is hidden" (regression Q3) and confirmed the claim's round-trip, the removed monotonic guard,
and the `toSignal` reactivity. D45's durable-claim design also passed an adversarial approach-check
before implementation.

**Deferred (not verified — not fixed):** D50 (AwbDispatcher runtime test) — open coverage gap.

## Build & tests (at `5fc330b`)

- **.NET:** `898/898` passed, 10 skipped (MinIO). **Frontend (Vitest):** `452/452`.

## Verdict: `approve-with-followups`

The v3 fixes held. This confirms the fixes, not feature-cleanliness — a certification pass is still
owed. Per the owner's decision, that will be a **single-pass certification (recorded deviation)**: a
broad blinded pair (v3) ran today and the fix round was independently approach-checked + micro-reviewed,
so one fresh full-manifest pass is a proportionate saturation check (precedent: 043-v9).

**Follow-ups (not blockers):**
1. **D45 crash-window residual** — verify Sameday's create-idempotency before enabling the jobs
   (ADR-015 amended; accepted + Error-alerted).
2. **D50** — dispatcher-runtime test (needs a background-service harness).
3. **Backlog** — D20, D23, D25, D27, D29, D30, D33, D35, D37, D38, D39, D40 remain deferred.
4. Still dormant behind `Sameday:Enabled`=false + `Jobs:Enabled`=false.
