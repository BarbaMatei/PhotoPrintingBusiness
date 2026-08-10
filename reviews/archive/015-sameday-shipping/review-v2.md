---
type: review
target: 015-sameday-shipping
version: 2
supersedes: 1
commit: 727a018
branch: feat/bolt-036-sameday-api-client
pass-type: verification
date: 2026-07-27
reviewer: anchored verification (revert-and-rerun + judgment items)
verdict: approve-with-followups
findings: { verified: 21, reopened: 0, new: 0 }
tests: { dotnet: "893/893 (+10 skipped MinIO)", frontend: "451/451" }
---

# Review v2 — 015-sameday-shipping (verification)

Anchored verification of [resolution-v1.md](resolution-v1.md) at the fixed commits
(`edd49f7`..`835e932`, plus `727a018` adding the F18 test this pass). The question is only "did
each fix hold?", not "is the feature clean?" — so the verdict is capped at
`approve-with-followups`. **0 reopened, 0 new, 21 findings verified.**

**Independence note.** The fix round and this verification ran in one session, so fixer≠verifier was
not achieved. The load-bearing check here — **revert-and-rerun** — is bias-free (a test either goes
red when the bug returns or it does not). The design-level review (runbook step 4) was done by
**independent fresh-context agents** during the fix round: two adversarial approach-checks (which
caught two real correctness blockers before implementation) and a two-agent fix-diff micro-review
(which caught one regression + one coverage gap, both fixed).

## Method & result

**Revert-and-rerun (mechanical) — 12 findings, clean attribution, zero collateral:**

| Batch | Bug re-introduced | Red test(s) | Findings |
|---|---|---|---|
| 1 | `ClientInternalReference`→PickupPointId; drop the AWB write guard; drop the CAS `Status==Shipped`; drop the webhook `NotifyPaidAsync` | 5 tests, exactly | F1, F2, F6, F7, F12 |
| 2 | `>=` in `NextDispatchDelay`; unconditional admin AWB; drop `ShippedAt`; drop admin Paid branch; revert courier + Easybox validator rules | 13 tests, all in the reverted classes | F4, F8, F10, F11, F13, F17 |
| F18 | drop `catchError` from the locker-search stream | 1 test (added this pass) | F18 |

Each re-introduced bug reddened **precisely** its own regression test(s) and nothing else (backend
stayed 880–888 green under each batch) — the tests are non-vacuous.

**Judgment items / inspection-verified — 9 findings** (no clean mechanical revert; verified against
the code + the independent design review):

- **F3** (per-order `DbContext`): structural; a shared in-memory-SQLite connection can't faithfully
  reproduce multi-context concurrency, so no unit test reddens it. Verified by the independent
  approach-check (agent judged the per-order scope sound + leak-free) + inspection: the scope opens
  per order **after** the semaphore gate, tick reads are `AsNoTracking` projections.
- **F5** service id + locker OOH id — `Easybox_uses_locker_service…`, `Courier_uses_courier_service…`,
  and the wire test assert the values.
- **F9 / F34** shared limiter — the finite-limit throttle test proves the limiter is shared + active
  (a per-call `new` limiter wouldn't be blocked by the held permit); can't revert cleanly (signature).
- **F14** swallowed-outage log — observability-only (a `LogWarning` in the catch); verified by inspection.
- **F15** log-before-write + persist-fail→`RetryLater` — the drop-table persist-failed test proves the
  transient classification.
- **F16** fresh-context read-back — the happy-path test reads through a fresh `ReadBack` context.
- **F19** single-stream prime — prime + search tests pass; the reorder was confirmed by the micro-review.
- **F32** mapper rejects a blank recipient — `Throws_when_recipient_name/phone_is_blank`.

## Build & tests (at `727a018`)

- **.NET:** `893/893` passed, 10 skipped (MinIO auto-skip). **Frontend (Vitest):** `451/451`.

## Verdict: `approve-with-followups`

All 21 fixes held; nothing reopened. This confirms the fixes, **not** that the feature is clean — a
certification pair is still owed before feature closure (full-loop tier), and the whole path remains
dormant behind `Sameday:Enabled`=false + `Jobs:Enabled`=false.

**Follow-ups (not blockers):**
1. **Owner action before enabling:** set the real `Sameday:CourierServiceId` / `LockerServiceId` from
   the Sameday contract (F5 — per-merchant vendor data, not in the repo).
2. **Owner decision:** admin-marked-paid orders send no confirmation email (photo promotion self-heals
   via the recovery scanner; the email has no backstop). One-line add if wanted.
3. **Backlog:** the 22 Low/Cleanup (D20–D31, D33, D35–D41) remain in [ledger.md](ledger.md), to drain
   in a groomer sweep or the next bolt in this area. F22/F23/F40 (dual-DB parity) are the ones worth
   grooming before enabling on Postgres.
