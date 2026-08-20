---
type: resolution
target: 038-039-invoicing
version: 4
answers: pass v4 (verification — index row)
status: resolved
fixed_commit: 07b0c1b
closed: 2026-08-14
---

# Resolution v4 — 038-039-invoicing

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-508 | fixed | `07b0c1b` | Added the cancellation proof the pass showed was cheap: the Sentry mock cancels the token from its capture callback, landing it inside the reload. Red with the old filter restored, green without. EuPlatesc leg dropped on the owner's word |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — cancellation proof | PPW-508 | `Tests/Unit/Controllers/WebhooksControllerInvoiceRaceTests.cs` | not needed (test-only) |

## Decisions

### The skipped test needed no fake provider (PPW-508)

The previous round claimed forcing the reload to throw a cancellation required a fake provider,
and shipped the widened catch untested on that basis. The claim was never checked and is false.
The verification pass measured the cheap route and this round implemented it: the `Sentry.IHub`
mock already in the file captures immediately before the reload, so cancelling a token source
from its callback lands cancellation inside `ReloadAsync`. Restoring the old
`when (reloadEx is not OperationCanceledException)` filter reddens it with the exception
escaping through `SqliteCommand.ExecuteReaderAsync`; removing the filter turns it green. Forty-six
lines, existing helpers only.

### The EuPlatesc coverage gap is dropped, not fixed (PPW-508)

The verification pass found the second call site proven by nothing — reverting it alone left
every test green. The owner ruled mid-round that the EuPlatesc integration is being removed and
only Stripe will remain, so a test written against that path would be deleted with it. A drafted
test for the site was removed rather than committed. The source at both sites is correct; what
is missing is a proof for a path scheduled for deletion.

### One claim in the previous round was wrong (PPW-508)

The v3 resolution said both vacuous proofs were replaced. Only one was.
`ResultLabelFor_maps_each_outcome_to_its_slo_label` was kept and still exercises the private
helper rather than either call site, so it survives a call-site revert. It is harmless beside
the endpoint-driven test that now carries the real proof, and it stays, but the claim that it
was replaced was false and is corrected here.
