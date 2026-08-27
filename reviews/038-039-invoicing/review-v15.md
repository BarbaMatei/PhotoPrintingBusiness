---
type: review
target: 038-039-invoicing
version: 15
supersedes: 12
commit: 5fca3cf
branch: feat/bolt-038-vat-calculation
pass-type: delta-discovery
date: 2026-08-27
lenses: [correctness, race, frontend-ux, security, db-parity]
lenses-not-run: [requirements, quality, tests-coverage, input-validation, observability, completeness-critic]
verdict: request-changes
blockers: [PPW-659, PPW-660, PPW-661, PPW-662, PPW-663]
findings: { high: 5, medium: 12, low: 10, cleanup: 1, refuted: 0 }
tests: { dotnet: "n/a — lenses do not run the suite", frontend: "n/a — lenses do not run the suite" }
---

# Review v15 — 038-039-invoicing

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-659 | 🔴 | Not-yet-due Rejected invoices fill the upload batch and starve Pending uploads | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:103` | yes |
| PPW-660 | 🔴 | A succeeded webhook on an order already moved to PaymentFailed leaves the customer charged and the order unfulfillable | `Controllers/WebhooksController.cs:242` | yes |
| PPW-661 | 🔴 | Checkout idempotency key is never retired after a paid order, so the next checkout is redirected to the old order and the new basket deleted | `src/app/core/services/checkout-attempt.service.ts:49` | yes |
| PPW-662 | 🔴 | Retry after a declined card reuses the same client secret whose order the failure webhook already moved to PaymentFailed | `src/app/features/checkout/pages/payment-step.ts:208` | yes |
| PPW-663 | 🔴 | the legacy processor columns removed by editing the already-applied baseline migration, so existing databases keep Orders.PaymentProcessor NOT NULL | `Migrations/20260820133204_InitialPostgres.cs:216` | yes |
| PPW-664 | 🟠 | Automatic rejection-resubmit nulls PdfStoragePath, revoking the customer's invoice | `Services/Invoicing/InvoiceLifecycle.cs:200` | yes |
| PPW-665 | 🟠 | Any non-2xx 4xx from ANAF maps to content-rejected and permanently parks the invoice as Failed | `Services/Invoicing/Anaf/AnafSpvClient.cs:74` | yes |
| PPW-666 | 🟠 | OrderService frees the idempotency key on a fresh PaymentFailed order while its PaymentIntent is still chargeable | `Services/OrderService.cs:129` | yes |
| PPW-667 | 🟠 | ANAF 429/503 counts as an unknown upload outcome and parks the invoice after 3 ticks | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:441` | yes |
| PPW-668 | 🟠 | Two concurrent same-key payment requests both call Stripe and one turns a 409 into a 500 | `Controllers/PaymentsController.cs:81` | no |
| PPW-669 | 🟠 | Post-commit webhook side effects are lost for good when one throws, because the retry hits the already-paid guard | `Controllers/WebhooksController.cs:201` | no |
| PPW-670 | 🟠 | One transient poll failure replaces the payment-submitted screen with "order not found" | `src/app/features/orders/pages/confirmation-page.ts:287` | yes |
| PPW-671 | 🟠 | combinedStreetLength group error is never rendered, so Continue is disabled with no explanation | `src/app/features/checkout/pages/delivery-step.ts:384` | yes |
| PPW-672 | 🟠 | Settle-poll setTimeout is never cancelled on destroy, so a late poll clears a newer basket | `src/app/features/orders/pages/confirmation-page.ts:282` | yes |
| PPW-673 | 🟠 | Invoice download uses a detached anchor and revokes the object URL in the same tick as click() | `src/app/features/orders/pages/confirmation-page.ts:322` | yes |
| PPW-674 | 🟠 | Admin cross-customer invoice read can be logged with an empty admin id | `Controllers/InvoicesController.cs:72` | yes |
| PPW-675 | 🟠 | Pooled test database is reused without checking the migration chain actually applied | `Tests/Helpers/PostgresTestDatabase.cs:227` | no |
| PPW-676 | 🟡 | Content-rejected branch ignores a lost park CAS: claim stays held, no LastError, metric still counted | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:435` | yes |
| PPW-677 | 🟡 | Webhook AlreadyInvoiced return leaves the uncommitted Paid transition on the scoped context | `Controllers/WebhooksController.cs:328` | no |
| PPW-678 | 🟡 | Invoice number allocated outside the transaction that inserts the row, against the numbering service's contract | `Services/Invoicing/InvoiceCreationService.cs:93` | no |
| PPW-679 | 🟡 | After the 10-poll budget the payment-confirming spinner spins forever with no terminal message | `src/app/features/orders/pages/confirmation-page.ts:280` | yes |
| PPW-680 | 🟡 | canContinue ignores shippingCostsReady, so a restored session proceeds on a stale shipping cost | `src/app/features/checkout/pages/delivery-step.ts:393` | yes |
| PPW-681 | 🟡 | Non-owner invoice PDF served with a one-year immutable browser cache | `Controllers/InvoicesController.cs:149` | no |
| PPW-682 | 🟡 | ResetForTest deletes the migration's 42 EasyboxLocker seed rows and never restores them | `Tests/Helpers/PostgresTestDatabase.cs:106` | no |
| PPW-683 | 🟡 | DropAllForeignKeys does not mark the pooled database dirty, so a constraint-free schema can be handed on | `Tests/Helpers/PostgresTestDatabase.cs:166` | no |
| PPW-684 | 🟡 | Test-database sweep is scoped to its own salt, so pools from other worktrees are never reclaimed | `Tests/Helpers/PostgresTestDatabase.cs:292` | no |
| PPW-685 | 🟡 | ResetSequences drops every public sequence the migration script did not literally CREATE, including identity-owned ones | `Tests/Helpers/PostgresTestDatabase.cs:128` | no |
| PPW-686 | ⚪ | MaxBatchSize is used unclamped unlike the upload job's other settings | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:109` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| — | No lens finding was refuted; 17 of 28 went through a trace skeptic and all 17 came back confirmed. The 11 low/cleanup rows were left unverified by design (severity tier). |

## Notes for the fixer

- **This pass exists to catch what the last two fix rounds broke, and that is most of it.** 8 rows come from the v13 round (PPW-659, PPW-664, PPW-665, PPW-667, PPW-671, PPW-674, PPW-676, PPW-680), 7 from the v12 payment-flow cluster (PPW-661, PPW-662, PPW-666, PPW-670, PPW-672, PPW-673, PPW-679), and PPW-663 from PR #13. Fix those before anything pre-existing: the loop cannot certify code whose last round left it worse in these places.
- **PPW-663 is live-broken, not hypothetical.** Confirmed against the dev database: `__EFMigrationsHistory` holds only `20260820133204_InitialPostgres`, `Orders."PaymentProcessor"` is `text NOT NULL` with no default, and the model no longer maps it. Every Order INSERT there fails 23502. Editing an applied migration in place is invisible to `Database.Migrate()`, which is why no test caught it. The fix is to restore the baseline `Up()` and add a forward `DropColumn` migration — and this also weakens the PPW-560 ruling recorded a few commits earlier, which reasoned about a *deleted* migration id rather than an *edited* one.
- **PPW-659 and PPW-664 are the same fix colliding with itself.** PPW-600 made the worker resubmit rejections; PPW-598 made retry clear `PdfStoragePath`. Together, the automatic resubmit now revokes a customer invoice every backoff slot, and the loose coarse filter lets not-yet-due rows crowd out every Pending upload. Treat them as one change.
- **PPW-660, PPW-661 and PPW-662 are one story.** A declined card leaves the order `PaymentFailed` with a still-chargeable intent; the retry button reuses it; the succeeded webhook has no `PaymentFailed → Paid` edge. Fixing only the SPA half leaves the charge stranded, and fixing only the webhook leaves the double-intent path open.
- PPW-666 sits underneath them: the settled-key rule frees the key from a fresh `PaymentFailed` holder without voiding that order's PaymentIntent, so two intents for one basket can both be confirmable.
- The four `PostgresTestDatabase` rows (PPW-675, PPW-682–PPW-685) are test-infrastructure, not product. They belong to the machinery discussion, not this release gate.
- No `decidedFindings` list was bound for this run (SF40), so nothing here was auto-matched against the ledger's 97 terminal rows. PPW-673 is the same class as backlog row PPW-644 (blob URL revoked too early) and PPW-681 restates the caching half of backlog row PPW-621 — reconcile those two by hand rather than treating them as wholly new.
