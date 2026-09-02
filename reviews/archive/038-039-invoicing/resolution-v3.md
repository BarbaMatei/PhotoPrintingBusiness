---
type: resolution
target: 038-039-invoicing
version: 3
answers: pass v3 (verification — index row)
status: resolved
fixed_commit: 0a250b9
closed: 2026-08-14
---

# Resolution v3 — 038-039-invoicing

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-508 | fixed | `0a250b9` | Both vacuous proofs replaced by one test driving the real Stripe endpoint: it asserts the recorded label is `failed`, never `duplicate`, and that the transition is rolled back. The reload catch no longer lets cancellation escape |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — exhausted-retry proof | PPW-508 | `Controllers/WebhooksController.cs`, `Tests/Unit/Controllers/WebhooksControllerInvoiceRaceTests.cs` | not needed (test repair plus a one-line catch widening) |

## Decisions

### Both legs proven against the real endpoint, not the helper (PPW-508)

The previous round asserted the label by invoking the private `ResultLabelFor` through
reflection, and asserted the rollback from a test that never applied the transition it was
meant to undo. Both passed whether or not the fix existed. The replacement drives
`StripeWebhookAsync` end to end on PostgreSQL and reads the metric through `MetricCapture`, so the
label the handler actually records is what gets asserted. Each leg was proven red separately —
putting the `duplicate` mislabel back reddens it, and so does deleting the reload block — then
green once restored.

### The reload catch no longer excludes cancellation (PPW-508)

The verification pass found a third defect in the same mechanism: the rollback's catch excluded
`OperationCanceledException`, so a cancelled token during the reload escaped the helper and the
caller's `RecordPaymentWebhook` never ran — the same hole the round-2 approach-check refused a
rethrow to avoid. The catch is now unconditional. Swallowing cancellation here is deliberate:
the charge has already happened, and losing it from the metric is worse than finishing the
handler on a token nobody is waiting on. It has no test — forcing `ReloadAsync` to throw a
cancellation needs a fake provider, and the value did not justify one.

### Two minor rows left for the backlog (PPW-509, PPW-510)

Both are 🟡, so the router routes them to the ledger backlog rather than this round. Neither is
a regression from this round's work. They stay `open` on the ledger, unchanged.
