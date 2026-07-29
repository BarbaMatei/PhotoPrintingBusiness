---
type: review
target: 015-sameday-shipping
version: 3
supersedes: 2
commit: 8584572
branch: feat/bolt-036-sameday-api-client
pass-type: discovery
sub-type: certification-pair
date: 2026-07-27
reviewer: certification pair (two independent blinded full-manifest passes, cert-A + cert-B)
verdict: request-changes
blockers: [F1, F2, F3]
findings: { high: 3, medium: 9, doc: 1, reraise-decided: "D21, D23, D31 (+ backlog lows)", refuted: 1 }
tests: { dotnet: "893/893 (+10 skipped MinIO)", frontend: "451/451" }
cost: { agents: 82, tokens: 4618312, passes: 2, agents_by_stage: { cert-A: "35 (11 lenses/1 dedup/23 skeptic, 11 re-raise-skip)", cert-B: "47 (11 lenses/1 dedup/35 skeptic, 11 re-raise-skip)" } }
---

# Review v3 — 015-sameday-shipping (certification pair)

Two independent blinded full-manifest passes (cert-A, cert-B) against frozen commit `8584572`, each
seeded with the 21 decided backlog/false-positive items. Full-loop-tier certification.

**Verdict: `request-changes` — NOT certified.** The pair surfaced **3 High blockers**, and by protocol
a new 🔴 re-arms the loop (→ fix round). The pair earned its cost: **it independently caught a
live-checkout regression the v1 fix round introduced and the v2 verification missed** — exactly the
fixer≠verifier gap review-v2 flagged (v2 was same-session; this pair was independent, blinded).

**Cross-pass convergence** (both A and B independently raised it — the certification precision signal):
F1 (canContinue), F3 (AWB per-order guard, 3 lenses each pass), F4 (isDeliveryComplete gate), F7
(EuPlatesc enqueue untested), the D21 PII re-raise, and the ADR-015 doc drift. F2 (tracking timeout)
was A-only but I confirmed it against the code.

## Findings (ranked)

Convergence = independent lenses within a pass; "A+B" = both passes found it.

| ID | D# | Sev | Passes | Verdict | Finding | File |
|----|----|-----|--------|---------|---------|------|
| **F1** | D43 | 🔴 High | A+B | confirmed | **Easybox `Continue` stays disabled after the customer types name+phone — `canContinue` is a signal `computed()` that can't observe `easyboxContactForm.valid`, so typing never re-enables it. Live-checkout dead-end. Regression from v1 F4.** *(BLOCKER)* | `UI/…/delivery-step.ts:326` |
| **F2** | D44 | 🔴 High | A | confirmed | **A slow Sameday response (>10s `HttpClient.Timeout`) throws `TaskCanceledException`; the job's `catch (OperationCanceledException) { return; }` treats it as shutdown and exits the poll loop — delivery detection dead until process restart.** *(BLOCKER)* | `BackgroundJobs/ShipmentTrackingJob.cs:54` |
| **F3** | D45 | 🔴 High | A+B (3 lenses each) | confirmed | **AWB creation has no per-order guard before the vendor call: a retry re-send or a 2nd replica can POST `/api/awb` twice for one order; the DB CAS blocks only the 2nd DB write, not the 2nd billable AWB. Duplicate-safety rests entirely on unverified Sameday server-side dedup. Residual of D2.** *(BLOCKER / owner decision)* | `Services/Sameday/AwbCreator.cs:69` |
| F4 | D46 | 🟠 Med | A+B | confirmed | `isDeliveryComplete()` Easybox branch ignores the now-mandatory recipient contact → the stepper unlocks "Plată", user skips `continue()` → order posts `shippingAddress=null` → backend 400. Regression from v1 F4. | `UI/…/checkout-state.service.ts:51` |
| F5 | D47 | 🟠 Med | A | confirmed | Same `OperationCanceledException`-as-shutdown bug in `AwbDispatcher.ProcessAsync` — a timed-out AWB create is swallowed with no log and no in-process retry; waits on the 60-min sweep. | `BackgroundJobs/AwbDispatcher.cs:69` |
| F6 | D48 | 🟠 Med | A (3 lenses) | confirmed | An in-transit poll with no vendor timestamp stores `LastTrackingSyncAt = UtcNow`; a later real `Delivered` scan carries an earlier timestamp, trips the monotonic guard, and the order never transitions to Delivered. (Interaction of backlog D24+D26, elevated — it blocks delivery.) | `BackgroundJobs/ShipmentTrackingJob.cs:139` |
| F7 | D49 | 🟠 Med | A+B | confirmed | The **EuPlatesc** webhook→AWB enqueue has no test (only Stripe does, from v1 F6) — deleting `NotifyPaidAsync` from the EuPlatesc branch stays green while every EuPlatesc-paid order silently gets no label. | `Tests/…/PaymentControllerIntegrationTests.cs` |
| F8 | D50 | 🟠 Med | A | confirmed | `AwbDispatcher`'s outcome routing + delayed re-enqueue is untested (only the pure `NextDispatchDelay`); inverting transient/non-transient or dropping the re-enqueue stays green. | `BackgroundJobs/AwbDispatcher.cs:83` |
| F9 | D51 | 🟠 Med | B | confirmed | The load-bearing `Status != Cancelled` persist guard (v1 F12) has no test — "simplifying" it to `== Paid` (the exact trap agent-1 warned of) would lose a billed AWB for a mid-call Paid→Printing order, suite still green. | `Services/Sameday/AwbCreator.cs:107` |
| F10 | D52 | 🟠 Med | B | confirmed | A `429` that survives the 3 retries maps to `SamedayValidationException` → `GiveUp` (permanent) + Error "permanent-fail", instead of a transient retry. | `Services/Sameday/SamedayClient.cs:139` |
| F11 | D54 | 🟠 Med | A+B | confirmed | Paid→Cancelled during the vendor call orphans a real billable AWB; the guard correctly refuses the DB write but only logs "orphaned" — no compensating void or ops alert. Residual of D12 (external-create-then-persist). | `Services/Sameday/AwbCreator.cs:141` |
| F12 | D31 | 🟠 Med | A+B | re-raise (elevated) | `GenerateAwbAsync` still returns the "generează manual" fallback when enabled; an admin hitting `/shipping/awb` books a portal AWB while the job already created one → duplicate label. Prior call: backlog Low — **cert elevates to Medium** (duplicate-label risk). | `Services/SamedayShippingService.cs:52` |
| F13 | D53 | 🟡 Doc | A+B | plausible | ADR-015 + the 037 domain model name `awbPayment` as the idempotency key; the code correctly uses `clientInternalReference=OrderNumber`. Code is right (skeptic refuted the runtime harm), but the doc is a maintainer trap. | `memory-bank/…/adr-015-*.md` |
| — | D21 | 🟡 Low | A+B | re-raise | Vendor 4xx body (recipient PII) reaches Error logs. Prior: backlog Low — stands, but note the multi-lens re-raise. | `Services/Sameday/SamedayClient.cs:139` |
| — | D23 | 🟡 Low | A | re-raise | Migrations + timestamptz CAS never run on Postgres. Prior: backlog Low (dual-DB parity) — stands. | `Tests/…/OrderSamedayFieldsTests.cs` |
| — | — | — | B | refuted | "ADR names wrong field → silent break": code correct + wire test guards it; dropped (doc point kept as F13). | `Services/Sameday/SamedayClient.cs:106` |

The remaining raw findings (each pass deduped ~39–43 canonical) are backlog Low/Cleanup re-raises
(D24–D41) carrying their prior decisions — no change. Full per-lens output is persisted in the two
run transcripts (`tasks/wstv8pq9d.output`, `tasks/waaoljyfi.output`).

## What this means

- **Two blockers are the v1 fix round's own regressions/gaps** (F1, F4 — the Easybox contact UI) that
  the same-session v2 verification could not catch. F1's v1 test passed only because of assertion
  ordering (it filled the form before the single change-detection cycle).
- **F2 is a genuine pre-existing 🔴** (the timeout-kills-tracking bug predates the fix round; the D3
  restructure preserved the faulty catch).
- **F3 is the D2 residual** — the v1 fix closed the DB double-write; both cert passes independently say
  the *vendor* double-call is still open and shouldn't rest on unverified Sameday dedup. This needs an
  owner decision (accept the interim residual + orphan logging, or add a durable per-order claim).

## Verdict: `request-changes`

Not certified. Next: a **fix round** (blocker-first: F1, F2, F3), then re-verification, then a fresh
certification pair. Still dormant behind the two `false` flags. Tests green at the frozen commit
(893 / 451).
