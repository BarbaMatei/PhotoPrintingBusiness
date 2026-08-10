---
type: resolution
target: 015-sameday-shipping
review-version: 3
status: resolved
fixed_commit: 5fc330b
closed: 2026-07-27
findings:
  F1:  { status: fixed, commit: aada94b, note: "canContinue reads toSignal-mirrored form validity; typing now re-enables Continue. Same fix applied to the courier form's latent bug." }
  F2:  { status: fixed, commit: 18e7815, note: "tracking loop catch gated on the stopping token + per-poll OCE catch; AwbRetryJob gated too (class sweep, 5fc330b). A slow Sameday response no longer kills delivery detection." }
  F3:  { status: fixed, commit: 2c434ad, note: "durable per-order AwbClaimedAt claim before the vendor call (approach-checked). Closes the CONCURRENT double-call. Crash-window residual accepted+alerted — see decisions + ADR-015." }
  F4:  { status: fixed, commit: aada94b, note: "isDeliveryComplete() Easybox branch now requires the recipient contact, so the stepper can't skip to a payment 400." }
  F5:  { status: fixed, commit: 2c434ad, note: "vendor-call timeout -> transient RetryLater in AwbCreator; dispatcher + AwbRetryJob shutdown catches gated (5fc330b)." }
  F6:  { status: fixed, commit: 18e7815, note: "LastTrackingSyncAt = poll clock (always forward) + monotonic guard removed. A later Delivered with an earlier vendor timestamp is no longer dropped. (Also fixes the latent re-poll-every-tick throttle bug.)" }
  F7:  { status: fixed, commit: 19cd0b8, note: "EuPlatesc webhook -> AWB enqueue now asserted (mirrors the Stripe test)." }
  F8:  { status: deferred, commit: null, note: "AwbDispatcher outcome-routing/re-enqueue runtime test NOT added — a faithful test needs a BackgroundService harness with injected Task.Delay (fire-and-forget). The backoff decision is a tested pure function (NextDispatchDelay) and HandleOutcomeAsync is a simple switch. Open coverage gap for the re-reviewer." }
  F9:  { status: fixed, commit: 2c434ad, note: "added a test pinning the != Cancelled persist guard (Paid->Printing mid-call keeps its label)." }
  F10: { status: fixed, commit: 6606c25, note: "a 429 surviving retries -> SamedayUnreachableException (transient), not a permanent GiveUp. Matches the exception's own doc." }
  F11: { status: fixed, commit: 2c434ad, note: "genuine-orphan branch raised to Error (ops-alertable) — there is no vendor void endpoint in the client." }
  F12: { status: fixed, commit: f3d2508, note: "GenerateAwbAsync reports automatic creation (Manual:false) instead of telling the admin to create one manually (double-book risk)." }
  F13: { status: fixed, commit: ce4941a, note: "ADR-015 amended: names clientInternalReference=OrderNumber (not awbPayment), records the claim+guard mechanism and the honest crash residual." }
  D21: { status: fixed, commit: 6606c25, note: "backlog re-raise fixed while here — vendor 4xx body moved off the exception message (out of Error logs)." }
  D22: { status: fixed, commit: 2c434ad, note: "backlog fixed while here — AwbLabelUrl migration ships varchar(500) on Postgres, not unbounded text." }
  D24: { status: fixed, commit: 18e7815, note: "backlog fixed as part of F6." }
  D26: { status: fixed, commit: 18e7815, note: "backlog fixed as part of F6 (monotonic guard removed)." }
  D28: { status: fixed, commit: 6606c25, note: "backlog fixed while here — enqueue log Debug -> Information." }
  D31: { status: fixed, commit: f3d2508, note: "re-opened+elevated by cert, fixed (see F12)." }
  D36: { status: fixed, commit: f3d2508, note: "backlog fixed — deleted the dead TrackingPollOutcome." }
  D41: { status: fixed, commit: f3d2508, note: "backlog fixed — named GramsPerPrint." }
  D23: { status: deferred, commit: null, note: "backlog re-raise — Postgres migrate+CAS Testcontainers test still not added (dual-DB parity; owner's 3-env stage)." }
---

# Resolution v3 — 015-sameday-shipping (certification fix round)

Fixer response to [review-v3.md](review-v3.md) (the certification pair, which returned
`request-changes`). All 3 blockers (F1–F3) and the mediums are fixed, plus 8 backlog items folded in
where they rode the same code. **Status: `resolved`** — handed back for re-verification.

**Tests:** backend `898/898` (+10 skipped MinIO), frontend `452/452`, green at `5fc330b`.

**Process:** F3 (the durable claim — a new concurrency mechanism reversing ADR-015) got an adversarial
approach-check **before** implementation (SOUND-WITH-CHANGES; all six required changes folded in:
`!= Cancelled` write guard, claim release on failure, sweep excludes fresh claims, TTL > vendor
round-trip, separate column, honest ADR). The full fix diff then got a two-agent fresh-eyes
micro-review — both **clean**; the only follow-up was a class-sweep (AwbRetryJob OCE gate, `5fc330b`).

## Fix commits

| Commit | Cluster | Findings |
|---|---|---|
| `aada94b` | Easybox Continue signal + stepper gate | F1 (D43), F4 (D46) |
| `18e7815` | Tracking timeout survival + poll-clock + drop monotonic guard | F2 (D44), F6 (D48), D24, D26 |
| `6606c25` | 429 transient + vendor-body-off-logs + enqueue log level | F10 (D52), D21, D28 |
| `2c434ad` | Durable per-order AWB claim (+ OCE, guard test, orphan Error, migration parity) | F3 (D45), F5 (D47), F9 (D51), F11 (D54), D22 |
| `f3d2508` | Manual-AWB endpoint reports automatic + cleanups | F12 (D31), D41, D36 |
| `19cd0b8` | EuPlatesc enqueue test | F7 (D49) |
| `ce4941a` | ADR-015 amendment | F13 (D53) |
| `5fc330b` | AwbRetryJob OCE gate (micro-review class sweep) | F2/F5 |

## Decisions & rationale

- **F3 / D45 crash-window residual (owner-relevant).** The durable claim closes the *concurrent*
  double vendor-call. A worker that bills a label then dies before persisting is reclaimed after the
  TTL and re-created; whether that mints a *second* billable label still rests on Sameday deduping on
  `clientInternalReference` — **unverified**. There is no client-side close without a vendor
  "AWB-by-reference" lookup (not in the client). Accepted + **alerted** (Error log on the orphan) and
  documented in [ADR-015](../../memory-bank/bolts/037-awb-and-tracking-jobs/adr-015-accept-duplicate-awb-create-on-multi-replica.md).
  **Verify Sameday's create-idempotency before flipping `Sameday:Jobs:Enabled=true`.**
- **F8 / D50 deferred** (see frontmatter): the dispatcher's fire-and-forget runtime isn't unit-tested;
  a faithful test needs a background-service harness with injected delay. Recorded as an open gap.
- **Backlog not folded in** (stay deferred, low value or off-cluster): D20, D23, D25, D27, D29, D30,
  D33, D35 (D19-regression risk), D37, D38, D39, D40. D30/D35/D39 were considered and dropped to
  bound the round.

## Hand-back

Next: a **re-verification** against `5fc330b`, then (loop re-armed by the cert's new 🔴) a fresh
**certification pair** before closure. The fixer does not self-verify. Still dormant behind the two
`false` flags.
