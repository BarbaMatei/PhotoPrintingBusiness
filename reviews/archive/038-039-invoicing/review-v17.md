---
type: review
target: 038-039-invoicing
version: 17
supersedes: 15
commit: e935fbb
branch: feat/bolt-038-vat-calculation
pass-type: delta-discovery
date: 2026-08-27
lenses: [correctness, db-parity, frontend-ux, tests-coverage, completeness-critic]
lenses-not-run: [security, race, requirements, quality, input-validation, observability]
verdict: request-changes
blockers: [PPW-687, PPW-688, PPW-689, PPW-690]
findings: { high: 4, medium: 7, low: 11, cleanup: 2, refuted: 0 }
tests: { dotnet: "n/a — lenses do not run the suite", frontend: "n/a — lenses do not run the suite" }
---

# Review v17 — 038-039-invoicing

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-687 | 🔴 | payment-step's discardDeadIntent retires the idempotency key on every confirm error — card typos and already-succeeded charges included — wiping the form and creating a second order | `src/app/features/checkout/pages/payment-step.ts:221` | yes |
| PPW-688 | 🔴 | The server's PaymentFailed intent-abandon path is unreachable because the SPA retires the key instead of reusing it, so the declined order's intent stays chargeable and the widened PaymentFailed→Paid transition can auto-fulfil the duplicate | `Services/OrderService.cs:149` | yes |
| PPW-689 | 🔴 | Reclassified 403/404/405 misconfiguration responses now spend the blind-repost budget and park invoices with a false duplicate-filing reason, with no test over the join | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:598` | yes |
| PPW-690 | 🔴 | The new wasWaiting gate stops the cart from ever being cleared on the 409 "already paid" redirect | `src/app/features/orders/pages/confirmation-page.ts:286` | yes |
| PPW-691 | 🟠 | One failed status poll ends the confirmation page's settle wait permanently — no reschedule and no give-up message | `src/app/features/orders/pages/confirmation-page.ts:317` | yes |
| PPW-692 | 🟠 | The destroy guard clears the poll timer but not the in-flight status request, so the poll chain can restart after destroy and clear a new basket | `src/app/features/orders/pages/confirmation-page.ts:278` | yes |
| PPW-693 | 🟠 | The new rejected-invoice slice cap trades pending-starvation for rejection-starvation — not-yet-due rejections fill all 5 rows — and only the first case is tested | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:221` | yes |
| PPW-694 | 🟠 | Widening the transition table silently widened the admin status endpoint, and its only guard test was inverted | `Services/OrderStatusMachine.cs:23` | yes |
| PPW-695 | 🟠 | RequeueRejectedAsync clears XmlPayload but keeps PdfStoragePath, so the filed XML and the customer's PDF can diverge — and the new test pins that stale path | `Services/Invoicing/InvoiceLifecycle.cs:177` | yes |
| PPW-696 | 🟠 | InvoicesController's ownership check still trusts the null-returning GetUserIdOrNull for merged principals; only the audit line was fixed | `Controllers/InvoicesController.cs:62` | yes |
| PPW-697 | 🟠 | EnsureSchemaApplied's pending-migrations early return skips both repair of an interrupted first use and the foreign-key drop on a reused slot | `Tests/Helpers/PostgresTestDatabase.cs:271` | yes |
| PPW-698 | 🟡 | Minting a key after retirement silently drops the stored orderId the settling page depends on, and the spec asserts before minting so it stays hidden | `src/app/core/services/checkout-attempt.service.ts:53` | no |
| PPW-699 | 🟡 | delivery-step's shipping-costs continue gate is untested and the new per-field maxlength branch is unreachable in the case it was added for | `src/app/features/checkout/pages/delivery-step.ts:407` | no |
| PPW-700 | 🟡 | TryDropDatabase drops the database without the ClearAllPools() its sibling Drop() documents as necessary | `Tests/Helpers/PostgresTestDatabase.cs:278` | no |
| PPW-701 | 🟡 | The folded AddColumn left Invoices.UnknownUploadOutcomes with a permanent DEFAULT 0 that the model and snapshot do not declare | `Migrations/20260820133204_InitialPostgres.cs:384` | no |
| PPW-702 | 🟡 | The irreversible Stripe intent cancellation runs before the local transaction commits | `Services/OrderService.cs:56` | no |
| PPW-703 | 🟡 | The abandon-intent test asserts through the SUT's own DbContext, so a missing save stays green | `Tests/Unit/Services/OrderServiceTests.cs:488` | no |
| PPW-704 | 🟡 | The new gateway idempotency-race branch has no test and cannot be reached with the existing fake | `Controllers/PaymentsController.cs:91` | no |
| PPW-705 | 🟡 | The gateway-race 409's crafted message is never shown — the SPA's existing 409 branch swallows it | `Controllers/PaymentsController.cs:104` | no |
| PPW-706 | 🟡 | The slot-repair test leases a second pool slot while its class fixture already holds one | `Tests/Unit/Data/PostgresTestDatabaseTests.cs:18` | no |
| PPW-707 | 🟡 | No schema-level assertion covers the folded Invoices columns after the migration squash | `Migrations/20260820133204_InitialPostgres.cs:382` | no |
| PPW-708 | 🟡 | The squash reuses the baseline migration id while a coexisting worktree still carries the two AddColumn migrations | `Migrations/20260820133204_InitialPostgres.cs:362` | no |
| PPW-709 | ⚪ | A code comment on the confirmation page cites a finding ID, which CLAUDE.md forbids and the pre-commit hook blocks | `src/app/features/orders/pages/confirmation-page.ts:306` | no |
| PPW-710 | ⚪ | The OrderStatusMachine transition-table doc comment was not updated with PaymentFailed → Paid | `Services/OrderStatusMachine.cs:8` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| — | Nothing was refuted. 8 of 24 faced a skeptic; the 4 serious rows were accepted on three-lens independent agreement without one. |

## Notes for the fixer

- **This pass reviewed round 15's own diff, nothing else** — 35 files, 3,705 diff lines, commit range 5fca3cf..e935fbb. Every row here is a defect the fixing introduced or left behind, not a defect of the original feature.
- **PPW-687, PPW-688, PPW-689 and PPW-690 are one story and must be fixed as one change.** A declined card retires the payment key unconditionally, so the server never re-sees it and never cancels the stale intent; the widened `PaymentFailed → Paid` transition then lets a late success on that intent auto-fulfil the first order. One basket, two paid orders, both fulfilled, both invoiced. A mistyped card number is enough to start it.
- **Write the protocol first.** The rule to state is: *one basket yields at most one chargeable intent and at most one paid order.* The four fixes are derived from it, and one test must exercise decline → retry → late success. This is exactly the mechanism the fix-review contract now requires (audit R1, landed at `6a76ad9`).
- PPW-691 is the poll that never reschedules; PPW-692 is a destroy guard that clears the timer but not the in-flight request — and its own test uses a synchronous fake, so it proves nothing.
- Three of round 15's tests pass for the wrong reason. The test-meaning audit (audit R4) exists to catch this class from now on.
- **The loop was closed by the owner after this pass, without certification and without fixing these rows.** They are recorded, not addressed.
