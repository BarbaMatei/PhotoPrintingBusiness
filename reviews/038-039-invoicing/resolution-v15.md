---
type: resolution
target: 038-039-invoicing
version: 15
answers: review-v15.md
status: resolved
fixed_commit: 18f0b1c
closed: 2026-08-27
---

# Resolution v15 — 038-039-invoicing

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-663 | fixed | `de1b70d`, `pending` | the columns come out of the baseline itself on the owner's ruling that this pre-deploy branch ships one migration; the forward drop migration is withdrawn. See Decisions |
| PPW-659 | fixed | `9527eba` | rejections are fetched as their own slice with a cap of `MaxBatchSize / 10`, ordered by oldest transition, so they cannot consume the budget Pending uploads share |
| PPW-664 | fixed | `9527eba` | the worker's automatic resubmit goes through a new `RequeueRejectedAsync` that keeps `PdfStoragePath`; only the admin retry still drops it |
| PPW-686 | fixed | `9527eba` | `MaxBatchSize` is clamped to 1–500 in the constructor, like the job's other settings |
| PPW-660 | fixed | `c60260c` | `PaymentFailed → Paid` is a legal transition and the succeeded webhook treats that status like `AwaitingPayment`, so a card that works on the second try completes the order |
| PPW-666 | fixed | `c60260c` | handing the key on from a fresh failed holder now cancels that order's PaymentIntent and clears its client secret, so one basket cannot hold two confirmable intents |
| PPW-661 | fixed | `2302bec` | the key is retired the moment the card is confirmed; the order id stays so the confirmation page can still wait on it |
| PPW-662 | fixed | `2302bec` | a decline discards the client secret and unmounts the card, so the retry has to mint a fresh key and intent |
| PPW-670 | fixed | `2302bec` | only the first read may fall through to not-found; a later failure keeps the settling panel and says the check failed |
| PPW-671 | fixed | `2302bec` | the combined-street-line error renders as a form-level message, and an over-long field says so instead of "Câmp obligatoriu" |
| PPW-672 | fixed | `2302bec` | the poll timer is cleared in a `DestroyRef.onDestroy`, and the cart is only cleared for an order this browser was waiting on |
| PPW-673 | fixed | `2302bec` | the invoice anchor is attached before the click and the object URL revoked on the next tick, matching `admin.service.downloadZip` |
| PPW-679 | fixed | `2302bec` | a spent poll budget ends with a message instead of a spinner that never stops |
| PPW-680 | fixed | `2302bec` | `canContinue` requires both shipping prices, so a restored session cannot proceed on a stored one |
| PPW-665 | fixed | `5d84a5e` | only 400 and 422 are content rejections; 403, 404 and 405 are misconfigurations and now read as unreachable, so one wrong setting no longer parks every invoice |
| PPW-667 | fixed | `5d84a5e` | 429 and 503 are refusals at the door, so they record a plain pending error instead of spending the blind-repost budget |
| PPW-674 | fixed | `5d84a5e` | the audit line takes the admin id from the Bearer identity, so a request also carrying a guest token can no longer log an empty one |
| PPW-676 | fixed | `5d84a5e` | a lost park CAS records the error, releases the claim and logs, instead of counting a park that did not happen |
| PPW-668 | fixed | `18f0b1c` | a gateway `idempotency_error` re-reads the persisted secret, or answers 409 — never the 500 that told the customer their basket was broken |
| PPW-669 | fixed | `18f0b1c` | each post-paid side effect runs in its own try/catch with a Sentry capture, so one failure no longer costs the rest through Stripe's already-paid retry |
| PPW-675 | fixed | `18f0b1c` | a leased slot with pending migrations is migrated before use, and a slot whose first migration fails is dropped rather than left to poison later runs |
| PPW-677 | backlog | — | 🟡 webhook `AlreadyInvoiced` leaves the uncommitted Paid transition on the scoped context; unverified-low tier, no trace built |
| PPW-678 | backlog | — | 🟡 the invoice number is allocated outside the transaction that inserts the row, against the numbering service's own contract |
| PPW-681 | backlog | — | 🟡 restates the caching half of backlog row PPW-621 (a year-long immutable cache on a non-owner read) |
| PPW-682 | backlog | — | 🟡 `ResetForTest` deletes the migration's 42 locker seed rows and never restores them |
| PPW-683 | backlog | — | 🟡 `DropAllForeignKeys` does not mark the pooled database dirty |
| PPW-684 | backlog | — | 🟡 the test-database sweep is scoped to its own salt, so other worktrees' pools are never reclaimed |
| PPW-685 | backlog | — | 🟡 `ResetSequences` drops every public sequence the migration script did not literally create |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — the migration that had already run | PPW-663 | `Migrations/20260820133204_InitialPostgres.cs` and the three Designer snapshots, `Migrations/PhotoPrintDbContextModelSnapshot.cs`, `Tests/Integration/MigrationChainTests.cs`, `docs/DEPLOYMENT.md`, `memory-bank/standards/data-stack.md` | not needed; the claim was confirmed against the live dev database before any code changed, and the final shape was the owner's ruling rather than a pre-check |
| B — the two ANAF fixes colliding | PPW-659, PPW-664, PPW-686 | `Services/Invoicing/Anaf/InvoiceUploadJob.cs`, `Services/Invoicing/{InvoiceLifecycle,IInvoiceLifecycle}.cs`, `Tests/Unit/Services/Invoicing/Anaf/InvoiceUploadJobTests.cs` | not needed (a query split and a second lifecycle transition), and both were proven by targeted revert |
| C — the declined-card story | PPW-660, PPW-666, PPW-661, PPW-662 | `Services/OrderStatusMachine.cs`, `Controllers/WebhooksController.cs`, `Services/OrderService.cs`, `Services/{IStripePaymentGateway,StripePaymentGateway}.cs`, `core/services/checkout-attempt.service.ts`, `features/checkout/pages/payment-step.ts`, their tests | not needed; the review's own note that fixing either half alone leaves a hole was the design input |
| D — the rest of the SPA regressions | PPW-670, PPW-671, PPW-672, PPW-673, PPW-679, PPW-680 | `features/orders/pages/confirmation-page.ts`, `features/checkout/pages/delivery-step.ts`, their specs | not needed (state, template and lifecycle fixes) |
| E — ANAF classification and the audit line | PPW-665, PPW-667, PPW-674, PPW-676 | `Services/Invoicing/Anaf/{AnafSpvClient,InvoiceUploadJob}.cs`, `Extensions/ClaimsPrincipalExtensions.cs`, `Controllers/InvoicesController.cs`, their tests | not needed (status classification and a claims read) |
| F — pre-existing mediums | PPW-668, PPW-669, PPW-675 | `Controllers/{PaymentsController,WebhooksController}.cs`, `Tests/Helpers/PostgresTestDatabase.cs`, their tests | not needed; PPW-669's outbox half is explicitly not done — see Decisions |

## Decisions

### Every fix in this round was proven by a targeted revert (all clusters)

The v14 verification learned that a file-level revert of a multi-commit round can redden from a
compile error instead of a failing test. So each row here was reverted by its own smallest lever —
one branch, one call, one constant — with the rest of the tree intact, and the specific test run:
PPW-659 (re-merge the two queries), PPW-664 (`RetryAsync` in place of `RequeueRejectedAsync`),
PPW-661 (drop the `retired` check), PPW-662 (remove the discard call), PPW-663 (empty the drop
migration's `Up`), PPW-665, PPW-667, PPW-669, PPW-670, PPW-671, PPW-672, PPW-673, PPW-674, PPW-675.
All reddened and restored green.

### PPW-669 is half-fixed on purpose

The review offered a per-effect try/catch **or** a restartable outbox. The try/catch is in; the
outbox is not. What that leaves: a confirmation email lost to a transient fault stays lost, because
nothing sweeps for orders that are Paid with no email sent. The Sentry capture makes it visible
rather than silent, which is the difference between an operator finding out and nobody finding out.
An outbox is a design decision about a new persistent artefact, and it belongs to the owner.

### The seven 🟡 rows are backlogged, not judged (PPW-677, PPW-678, PPW-681, PPW-682, PPW-683, PPW-684, PPW-685)

The earlier 87 backlog rows got there by the owner's triage. These seven follow the same route
without a ruling from him, because none of them can strand a payment or an invoice: four are in the
test-database helper, one repeats a decided row, and two are contract tidiness. If he wants them
graded differently, the rows are there to regrade.

### The baseline is edited in place until the first deploy (PPW-663, owner ruling)

The first fix took the textbook route: leave the applied migration truthful, drop the columns in a
new one. The owner overruled it — nothing is deployed, the branch that reaches `main` is meant to
carry one migration, and he wants no trace of the removed processor anywhere in the repository. So
the columns came out of the baseline itself and the forward migration was deleted, along with the
test that started from the legacy state.

What that costs, stated plainly: `Migrate()` compares ids and not contents, so any database that
already ran the old baseline keeps the three columns and, because one was `NOT NULL` with no
default while the model no longer maps it, fails every `INSERT` into `Orders` with `23502`. Exactly
one such database existed — the developer's — and it was brought in line with
`ALTER TABLE "Orders" DROP COLUMN`, keeping all 6 of its orders. `docs/DEPLOYMENT.md` §7 and
`memory-bank/standards/data-stack.md` now record the policy and the date it reverses: the first
deploy, after which an applied migration is frozen.

### PPW-663 changes what the PPW-560 note claimed

PPW-560 was closed a few commits earlier with a note reasoning about a migration id this repo no
longer contains. This round found the worse case: an id it *still* contains, whose body was
rewritten. `Migrate()` compares ids and skips it, so the edit reaches new databases only —
confirmed on the dev database, where `Orders.PaymentProcessor` was still `NOT NULL` while the code
had stopped setting it. Both documents now carry the rule that applies before the first deploy — the baseline is
edited in place — and the one that replaces it after, when an applied migration becomes frozen.
