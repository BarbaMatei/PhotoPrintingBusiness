---
type: owner-summary
target: 015-sameday-shipping
pass: 4
pass-type: verification
commit: 5fc330b
date: 2026-07-27
decisions-needed: 0
---

# Sameday shipping — verification of the fix round (v4)

The three blocking bugs the certification found last round were fixed, and this pass confirms the
fixes actually work. Nothing new is broken; nothing needs you right now.

## Needs your decision

**Nothing new.** Every fix from the [last round](resolution-v3.md) held under re-testing, and no new
problem surfaced. The one open risk (D45, below) is one you already accepted. The final saturation
check (a single fresh certification pass) is queued next and runs without you.

## Reasons to doubt

- **This pass cannot certify — by design.** A verification pass only re-checks fixes; it can't
  declare the feature clean. That's the certification pass, still owed. See [review-v4](review-v4.md).
- **The next certification will be one pass, not the usual two.** You approved this shortcut. It's
  reasonable here — two independent passes already ran today ([v3](review-v3.md)) and the fix round
  was independently checked — but one pass sees less than two.
- **One gap wasn't closed (D50):** the background job that hands orders to Sameday has no automated
  test that runs the job end-to-end (writing one needs test scaffolding we don't have yet). Deferred,
  [ledger](ledger.md).
- **D45 residual:** if the server crashes in the ~1-second window between asking Sameday for a label
  and saving it, avoiding a duplicate label depends on Sameday's own duplicate-detection, which we
  haven't confirmed with them. Accepted and alarm-logged; the note to verify it with Sameday before
  going live is in [ADR-015](../../memory-bank/bolts/037-awb-and-tracking-jobs/adr-015-accept-duplicate-awb-create-on-multi-replica.md).

## Filed automatically

12 lower-severity items remain on the [ledger](ledger.md) backlog (D20, D23, D25, D27, D29, D30, D33,
D35, D37, D38, D39, D40) — none blocking; the whole feature is still switched off in production.

## State

Verification passed (0 reopened, backend 898 / frontend 452 green at `5fc330b`). Next and last:
a single-pass certification against this same commit. If it finds no serious defect, the feature is
certified and the loop closes.
