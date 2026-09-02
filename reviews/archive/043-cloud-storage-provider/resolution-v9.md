---
type: resolution
target: 043-cloud-storage-provider
version: 9
answers: review-v9.md
status: resolved
fixed_commit: b9af326
closed: 2026-07-27
---

# Resolution v9 — 043-cloud-storage-provider

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-232 | fixed | `b9af326` | The empty-archive message is gated on the order's lifecycle: before payment and during production it says the photos are being prepared, and the "no longer available" copy is kept for the later statuses. First fix at `d041295`. |
| PPW-233 | wont-fix | — | Owner ruling on 2026-07-27: the legacy processor is slated for removal, so testing its wiring buys nothing durable. The Stripe twin stays tested. See Decisions. |
| PPW-234 | deferred | — | Owner ruling on 2026-07-27: the backfill command is operator tooling for the deployment stage, so its scrutiny lands there with PPW-169 and PPW-209. See Decisions. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Empty-archive message gated on the order's lifecycle (`d041295`, `b9af326`) | PPW-232 | `UI/…/order-detail-page.ts`, `UI/…/order-detail-page.spec.ts` | not needed (a condition on an existing message) |
| B — Ruled without code | PPW-233, PPW-234 | — | not needed (no code changed) |

## Decisions

### The certification follow-ups were the only open items left (PPW-232, PPW-233, PPW-234)

The certification pass left exactly three Medium items needing a decision; everything else it raised
was already terminal on the ledger. The owner ruled all three on 2026-07-27 through the owner summary
for this pass.

### the legacy processor coverage will not be built

The reason is strategic rather than technical: the owner intends to remove that payment gateway. Any
later finding about the legacy processor-only coverage or parity should cite this ruling instead of being fixed.

### The backfill command is re-checked at the deployment stage

Re-check when the three-environment stage starts, or the first time the command is run against a real
environment, whichever comes first. Affirmed at `d041295`.

### The fix review caught one status the first fix missed

One anchored review of the fix diff ran. The class sweep was clean, since there is a single site, and
nothing adjacent regressed. It caught one edge that was fixed in the follow-up commit: the
payment-failed status sits outside the linear order chain and fell into the optimistic copy, so it is
now treated like a cancelled order, with both it and the pending status added to the tested matrix.

### One document still disagrees with the code

No ruling was requested. The certification pass flagged that the bolt-053 implementation plan says the
photos endpoint returns 404 for a non-owner while the shipped code and the upheld decision use 403.
That is a document-versus-code reconcile for the next documentation sweep, not a code change.
