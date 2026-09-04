---
stage: design
bolt: 047-coupon-domain-and-api
created: 2026-09-03T21:05:00Z
revised: 2026-09-04T00:20:00Z
---

## Technical Design: coupon-domain-and-api

> **Revision note.** This document was rewritten after the mandatory stage-2 adversarial design
> check (`bolt-process.md`). The check returned 6 blockers, 6 serious and 4 minor findings; the
> full list and its disposition is the last section, and the design below already reflects it.
> The first draft's transaction shape, invoice-XML plan, test placement and cart-read semantics
> were all wrong and have been replaced — reading only this version is correct.

> Note on the stage-2 "no source code" constraint. The generic bolt-type template forbids
> reading source in stages 1–2; this repo's `standards/bolt-process.md` makes a
> **caller-impact sweep over all existing consumers mandatory** in ddd-02 ("grep ALL existing
> consumers, no blank rows"). The repo standard is the more specific rule and wins, so the
> existing code was read to produce the sweep below. No source file was modified in this stage.

### Architecture Pattern

Same layered shape the codebase already uses — **controllers → services → `PhotoPrintDbContext`**,
no repositories, no mediator, no domain-event bus. Two new services (`CouponService`,
`AdminCouponService`), one pure static calculator, three new entities, and additive changes to
two existing services (`CartService`, `OrderService`).

Rationale: the alternative (introducing a repository or a coupon bounded-context module for one
feature) would make coupons the only part of the system with that shape, which the review record
punishes as an inconsistent new pattern rather than rewarding as cleanliness.

### Layer Structure

```text
┌───────────────────────────────────────────────────────────────────┐
│ Presentation   CartController (+2 actions), AdminCouponsController │
│                ExceptionHandlerMiddleware (error-code extension)   │
├───────────────────────────────────────────────────────────────────┤
│ Application    CouponService, AdminCouponService,                  │
│                CartService (preview), OrderService (redemption)    │
├───────────────────────────────────────────────────────────────────┤
│ Domain         CouponDiscountCalculator (pure), CouponType,        │
│                Coupon / CouponRedemption / CartCoupon entities     │
├───────────────────────────────────────────────────────────────────┤
│ Infrastructure PhotoPrintDbContext (3 DbSets + config),            │
│                baseline migration (edited in place)                │
└───────────────────────────────────────────────────────────────────┘
```

### API Design

All customer endpoints keep the existing `DualAuth` policy (JWT **or** `X-Guest-Token`);
all admin endpoints are `[Authorize(Roles = "Admin")]`.

| Endpoint | Method | Request | Success | Errors |
|---|---|---|---|---|
| `/api/cart/coupon` | POST | `{ "code": "VARA25" }` | 200 `CartResponseDto` with coupon fields | 422 `INVALID_COUPON`, 422 `MIN_SUBTOTAL_NOT_MET`, 422 `EMPTY_CART`, 429 |
| `/api/cart/coupon` | DELETE | — | 200 `CartResponseDto`, coupon cleared | — (idempotent) |
| `/api/cart` | GET | — | 200 `CartResponseDto`, coupon re-validated **read-only** with `couponStatus` / `couponReason` | unchanged |
| `/api/admin/coupons` | GET | `?status=active\|inactive\|expired&page=1&size=20` | 200 `{ items, total, page, size }` | — (paging clamped, never 422) |
| `/api/admin/coupons` | POST | `CouponCreateRequest` | 201 + `Location` | 422 validation, 409 `DUPLICATE_CODE` |
| `/api/admin/coupons/{id}` | PUT | `CouponUpdateRequest` | 200 `CouponDto` | 404, 422, 409 `CODE_IMMUTABLE_AFTER_REDEMPTION` |
| `/api/admin/coupons/{id}` | DELETE | — | 204 | 404, 409 `COUPON_ALREADY_INACTIVE` |
| `/api/admin/coupons/{id}/redemptions` | GET | `?page&size` | 200 `{ items, total, page, size }` | 404 |
| `/api/payments/stripe/intent` | POST | unchanged | unchanged | **+409 `COUPON_EXHAUSTED`, `INVALID_COUPON`, `MIN_SUBTOTAL_NOT_MET`, `ORDER_TOTAL_BELOW_MINIMUM`** |

**Error contract.** Errors keep the RFC 7807 shape from `api-conventions.md` and gain one
extension member, `code`, carrying the machine-readable reason:

```json
{ "type": "...", "title": "Unprocessable Entity", "status": 422,
  "detail": "Codul introdus nu este valid sau a expirat.",
  "code": "INVALID_COUPON", "correlationId": "..." }
```

Mechanism: a marker interface `IErrorCoded { string ErrorCode { get; } }` in
`PhotoPrint.API.Exceptions`; `ExceptionHandlerMiddleware` writes `problem.Extensions["code"]`
(and the same field in the Development diagnostic shape) whenever the thrown exception
implements it. Deliberately generic rather than coupon-specific — the existing
`divergentFields` / `orderId` extensions are one-off `is` checks and a third copy would have
cemented the pattern. Two new exception types implement it:
`CouponRejectedException : UnprocessableEntityException` (422) and
`CouponConflictException : ConflictException` (409). The middleware's mapping table is keyed on
the **exact** runtime type, so both need their own entries — a subclass does not inherit its
base's mapping.

**Reason codes, HTTP status and Romanian copy** (the frontend map in bolt 048 mirrors this table):

| Code | HTTP | Message |
|---|---|---|
| `INVALID_COUPON` | 422 apply / 409 checkout | „Codul introdus nu este valid sau a expirat." |
| `MIN_SUBTOTAL_NOT_MET` | 422 apply / 409 checkout | „Codul se aplică doar la comenzi de cel puțin {min} RON." |
| `COUPON_EXHAUSTED` | 409 | „Codul a atins limita de utilizări." |
| `EMPTY_CART` | 422 | „Coșul este gol." |
| `ORDER_TOTAL_BELOW_MINIMUM` | 409 | „După reducere, valoarea comenzii este prea mică pentru a fi plătită online. Adaugă produse sau elimină codul." |
| `DUPLICATE_CODE` | 409 | „Există deja un cupon cu acest cod." |
| `COUPON_ALREADY_INACTIVE` | 409 | „Cuponul este deja dezactivat." |
| `CODE_IMMUTABLE_AFTER_REDEMPTION` | 409 | „Codul nu mai poate fi modificat după prima utilizare." |

**Two deviations from `api-conventions.md`, both recorded in that file in the same change**
(standards here are descriptive, so reality and the standard move together):

1. `DELETE /api/cart/coupon` returns **200 with the recomputed cart**, not 204. The deleted
   thing is a sub-state of the cart and every caller needs the new totals immediately; a 204
   would force a second round trip on the checkout path.
2. An unusable code at **apply** time is **422**, not the 409 the convention's
   "structurally valid but conflicting with state" rule would give. The stories' acceptance
   criteria fix 422 for `INVALID_COUPON` / `MIN_SUBTOTAL_NOT_MET`, and treating the submitted
   code as a failed field validation is the reading that matches the customer's mental model.
   At **checkout** the same conditions are 409, because there the conflict is with an order the
   customer is trying to place rather than with a field they just typed. Both statuses are
   listed per code in the table above precisely so the frontend has one place to map from.

**Rate limiting (new mechanism, feature grade).** `POST /api/cart/coupon` carries
`[EnableRateLimiting(CouponRateLimitPolicy)]`. Unlike the existing named policies, which share a
single global bucket, this one is **partitioned per identity** (user id, else guest session id,
else remote IP), because a global bucket would let one prober lock out every customer. Default
15 attempts/minute — a human typing a promo code needs a handful; 15 leaves room for typos and
retries while cutting the enumeration rate (see finding 11) from 6 000/hour/IP to 900/hour/identity.
Configurable as `RateLimit:Coupon:PermitLimit`, in the same window as the rest.
Rejections surface through the existing `OnRejected` handler (429 + `Retry-After`) and are
covered by a test.

### Data Model

Three new tables plus two columns on `Orders`.

| Table | Columns | Indexes / constraints |
|---|---|---|
| `Coupons` | `Id uuid PK`, `Code varchar(20)`, `Type varchar(20)`, `Value numeric(10,2)`, `MinSubtotalRon numeric(10,2)`, `ValidFrom timestamptz`, `ValidUntil timestamptz`, `MaxRedemptions int NULL`, `RedemptionsCount int NOT NULL DEFAULT 0`, `IsActive bool NOT NULL DEFAULT true`, `CreatedAt timestamptz`, `UpdatedAt timestamptz NULL` | `ix_coupons_code` UNIQUE on `Code`; `ix_coupons_is_active_valid_until` on `(IsActive, ValidUntil)` for the admin list |
| `CouponRedemptions` | `Id uuid PK`, `CouponId uuid FK→Coupons Restrict`, `OrderId uuid FK→Orders Cascade`, `UserId uuid NULL FK→Users SetNull`, `DiscountRon numeric(10,2)`, `RedeemedAt timestamptz` | `ix_coupon_redemptions_order_id` **UNIQUE**; `ix_coupon_redemptions_coupon_id`; `ix_coupon_redemptions_user_coupon` on `(UserId, CouponId)` |
| `CartCoupons` | `Id uuid PK`, `UserId uuid NULL FK→Users Cascade`, `GuestSessionId uuid NULL`, `CouponId uuid FK→Coupons Cascade`, `AppliedAt timestamptz` | `ix_cart_coupons_user` UNIQUE on `UserId`; `ix_cart_coupons_guest` UNIQUE on `GuestSessionId`; check constraint `CK_CartCoupons_OneOwner` (relational only) |
| `Orders` (+2) | `CouponCode varchar(20) NULL`, `DiscountRon numeric(10,2) NOT NULL DEFAULT 0` | — |

Notes:

- **`Code` is `varchar(20)`, not the story's `varchar(50)`** — the validator caps codes at 20
  characters (`[A-Z0-9]{4,20}`, requirements Q2), and a column three times wider than its
  validator is exactly the drift a review lens calls out. `Orders.CouponCode` matches at 20.
- **Code matching is normalisation, not case-insensitive comparison.** Every write path
  (`AdminCouponService`, any future seed or import) routes through one
  `CouponCode.Normalize(raw)` — trim, `ToUpperInvariant` — and every lookup normalises the input
  and compares with `==`. So the plain btree `ix_coupons_code` is used on the apply path (the
  < 100 ms NFR) and uniqueness is real rather than per-exact-case. No `citext` extension and no
  functional index are needed; the invariant lives in one function, named in one place.
- **No `RowVersion` column.** See ADR-025 and the concurrency section.
- `CouponRedemptions.OrderId` is `Cascade`: orders are never deleted today, but a redemption
  pointing at nothing would be worse than none. `CouponId` is `Restrict` so a coupon with
  history cannot be hard-deleted even by a future mistake.
- `CartCoupons` unique indexes rely on PostgreSQL treating NULLs as distinct, exactly like the
  existing `ix_cart_items_user_upload` / `ix_cart_items_guest_upload` pair.
- `CartCoupons.GuestSessionId` carries **no** FK, mirroring `CartItem.GuestSessionId`.

**Migration.** Nothing is deployed, so `data-stack.md` applies: the single baseline migration
`20260820133204_InitialPostgres` is **edited in place** — new `CreateTable` calls and the two
`Orders` columns go into its `Up` (mirror drops into `Down`), and the model snapshot is
regenerated. Method: change the model, scaffold a throwaway delta migration to obtain EF's own
DDL and the regenerated `PhotoPrintDbContextModelSnapshot.cs`, paste the DDL into the baseline,
copy the snapshot into the baseline's `.Designer.cs`, delete the throwaway files. The three
hand-written pieces of the baseline (42 `EasyboxLocker` seed rows, the raw-SQL
`uq_invoices_series_year_number` index, the `invoice_seq_ft_2026` sequence) are never touched,
so they survive by construction. `MigrationChainTests` proves the edited chain still applies to
an empty PostgreSQL database and that the snapshot still matches the model. This is the wave's
only migration.

### Concurrency design (the guarantee this bolt is gated on)

**Mechanism: database-side compare-and-swap, no concurrency token** (ADR-025).

```sql
UPDATE "Coupons"
   SET "RedemptionsCount" = "RedemptionsCount" + 1
 WHERE "Id" = @id
   AND "IsActive"
   AND "ValidFrom" <= @now AND "ValidUntil" > @now
   AND ("MaxRedemptions" IS NULL OR "RedemptionsCount" < "MaxRedemptions");
-- affected = 1 → redeemed;  affected = 0 → lost the race, or the coupon changed under us
```

expressed in EF as `ExecuteUpdateAsync` over that predicate. The predicate carries **the whole
redeemability rule**, not just the cap, so a coupon deactivated or expired between validation
and commit cannot slip through — there is no read-then-act window on any of the four conditions.

**On `affected = 0` the reason is not knowable from the boolean**, so the failure path issues one
extra read to classify (`!IsActive` → `INVALID_COUPON`, outside the window → `INVALID_COUPON`,
count at cap → `COUPON_EXHAUSTED`, row gone → `INVALID_COUPON`). That read costs nothing on the
success path and is what lets an admin's mid-checkout deactivation say „codul nu este valid"
rather than falsely telling the customer the code hit a usage limit.

**Where the CAS sits: immediately before COMMIT, after the order insert has succeeded.**

```text
  load cart ─► resolve + fully validate coupon (read) ─► compute discount
      │           (precise reason code comes from THIS read: inactive / window /
      │            min-subtotal / already at cap)
      ├─► payable-gross floor check (see below) — before anything is written
      ├─► idempotency resolution (may return a replay; no coupon side effect yet)
      │
      ├── no coupon applied ──► existing path, byte-for-byte unchanged (no transaction)
      │
      └── coupon applied ─► BEGIN (PostgreSQL only)
                             ├─ add Order + Items + CouponRedemption
                             ├─ SaveChanges (existing retry loop; EF takes a savepoint before
                             │   each save inside the transaction, so a unique-index rejection
                             │   rolls back to it and the loop still works)
                             ├─ any early `return` here (an idempotent replay discovered on a
                             │   key collision) leaves the transaction uncommitted → nothing
                             │   was written and nothing was counted (invariant 5)
                             ├─ CAS increment ── affected = 0 ─► classify ─► throw 409
                             │                    (rollback: no order, no redemption row)
                             └─ COMMIT
```

Putting the CAS **last** rather than first is load-bearing, and the first draft had it first:

- A row lock taken by an `UPDATE` is held until the transaction ends, not for the statement.
  CAS-first would hold the coupon row across order-number generation, the whole insert and the
  entire retry loop — serialising every checkout on a site-wide promo behind one row.
- Worse, it created a real deadlock cycle: transaction A holding the coupon row and blocking on
  a duplicate `Idempotency-Key`, transaction B holding that key's index entry and waiting for
  the coupon row. PostgreSQL aborts one with SQLSTATE `40P01`, which matches neither
  `IsIdempotencyKeyViolation` nor `IsOrderNumberViolation` (both keyed on `23505`) — so a normal
  double-submit of the same basket would have escaped both `when` filters as a raw 500.
  With the CAS last, nothing waits on anything after taking the coupon lock, so no cycle exists.
- It also keeps the InMemory path honest (below).

**Gating the transaction on "a coupon is applied"** keeps the far more common no-coupon path
exactly as it is today — no new transaction semantics for orders without a coupon, and therefore
no regression surface for the payment-idempotency behaviour bolts 034/035 fought for.

**Idempotency divergence check must compare the pre-discount gross.** `DivergentFields` today
compares `existing.TotalRon` against a freshly computed candidate total. With coupons that is
wrong in both directions: a replay of a discounted order computes a different candidate (409 on
a legitimate retry the customer cannot escape), and a coupon whose state changed between the two
calls would flip a replay into a conflict. The comparison changes to the **undiscounted** gross —
`existing.SubtotalRon + existing.ShippingCostRon` versus `candidateSubtotal + candidateShipping`
— which is a property of the *request* rather than of coupon state. For every existing
(coupon-free) order the two expressions are equal, so no current behaviour changes; a regression
test pins that.

**Redemption release and transfer.** A redemption is taken at order creation, when the amount to
charge is fixed, because a customer who has been quoted and charged a discounted price cannot
afterwards be told the coupon was refused. That means an unpaid order holds a slot. What happens
to that slot depends on whether a replacement order follows.

*Transfer — a replacement order follows.* Two paths in `OrderService` build a replacement order
for the same customer and the same cart: a fresh `PaymentFailed` holder whose `Idempotency-Key` is
being reused (a declined card retried), and stale-key (>24 h) reclamation. Releasing the slot there
and re-redeeming it would take two decisions in two units of work, and between them the redemption
is free while a discounted order still stands — another caller can take the last slot and the
paying customer gets their own 409. So where the replacement carries the same coupon the redemption
is **moved, not re-taken**: the abandoned holder's `CouponRedemptions` row is repointed to the
replacement inside the single transaction that creates it, and the holder is abandoned in the same
unit of work. `RedemptionsCount` never changes, so `ConsumeOrThrowAsync` **must not run on this
path** — running it would double-count an uncapped coupon and 409 a capped one, rolling back a
replacement the customer is entitled to.

*The five decisions the retry path takes, in order.* The holder's slot is a fact the rest of the
flow has to be told about, so it is read before the coupon is resolved and it drives the branch:

| # | Condition | What happens |
|---|---|---|
| 1 | read the holder's `CouponRedemptions` row (`AsNoTracking`) before resolving | yields `heldSlot` — the coupon id and row id, or null |
| 2 | resolving the replacement's coupon | `ResolveForOrderAsync` is told `heldSlot?.CouponId` and validates that one coupon against `RedemptionsCount - 1`; without this the holder's own slot exhausts a `MaxRedemptions = 1` coupon and the retry 409s `COUPON_EXHAUSTED` after the intent is already dead — no order, no live intent, and the coupon locked to a dead order |
| 3 | the resolved coupon **is** the held one | transfer: repoint the row, no insert, no CAS |
| 4 | the resolved coupon is a **different** one (apply A, decline, apply B, retry) | release the held row and take a normal CAS redemption for B — a transfer here would leave the row on A while the order says B, and B's cap would never see the redemption |
| 5 | no coupon resolves (code cleared, or `NO_DISCOUNT`) | release the held row; the replacement carries no discount |

Rows 4 and 5 release inside the same transaction as the replacement's insert, never before it, so a
failed insert rolls the release back with it — otherwise the discounted holder still stands with its
slot free, which is the window this whole section exists to close. That widens the transaction gate
from "a coupon is applied" to "a coupon is applied **or** a held slot is being moved or released";
`ReleaseForOrderAsync` already declines to open its own transaction when one is ambient, so it needs
no change.

*Mechanism for the repoint, named because house style points the other way.* The rest of this bolt
writes counters with `ExecuteUpdateAsync`, which executes immediately and outside EF's batch. Here
that is wrong twice: PostgreSQL checks foreign keys per statement rather than at commit, so an
`UPDATE` landing before the replacement's `INSERT` fails against a row that does not exist yet, and
`ExecuteUpdateAsync` throws on the InMemory provider the integration suite defaults to. The repoint
is therefore an ordinary tracked-entity write — load the row, assign `redemption.OrderId = order.Id`
(order ids are client-generated, so the value exists before the insert) and let one
`SaveChangesAsync` emit both statements, EF's batching ordering the dependent update after the
principal insert. A `[PG]` test pins that ordering rather than trusting it.

*The repoint replaces the insert; it does not join it.* `OrderService` today unconditionally adds a
new `CouponRedemption` whenever a coupon applies. On the transfer path that insert must not happen
at all: the row already exists, so insert-plus-repoint is a duplicate-key failure against the unique
index on `CouponRedemptions.OrderId` (invariant 6) that the save loop does not recover — it
recognises only the idempotency-key and order-number indexes — and it surfaces as an unhandled 500
which InMemory cannot see. For the same reason `DetachFailedInsert` must detach only rows it
created: detaching a pre-existing tracked row would silently abandon the repoint. It gains the
holder for the opposite reason — the holder's abandoned status and nulled key are in-memory edits
that must not survive a failed insert into some later `SaveChanges` on the request-scoped context.

*Why `Cancelled`.* `OrderStatus` has no abandoned member, and the one status a late
`payment_intent.succeeded` cannot walk out of is `Cancelled`: `PaymentFailed → Paid` is deliberately
legal (a declined intent stays chargeable), so leaving the holder `PaymentFailed` would let a late
success flip an order whose redemption now belongs to a different order. With the holder
`Cancelled`, `WebhooksController.HandleStripePaymentSucceededAsync` falls to its else branch and
logs *customer charged, order not Paid, manual reconciliation required* — the late charge is refused
by order state and shouted about instead of quietly producing a second paid order. The payment-side
half of that is the parked double-charge cluster PPW-687…PPW-690 (owner ruling 2026-09-03), so the
tests here assert the coupon side only: where the row sits, and that `RedemptionsCount` did not move.

*How the machine is asked, and why not two new transitions.* The abandon is a **named method**,
`OrderStatusMachine.Abandon(order)`, legal from `AwaitingPayment` and `PaymentFailed` only — not two
new rows in `ValidTransitions`. `ValidTransitions` is what `CanTransition` answers for every other
caller, and `AwaitingPayment → Cancelled` there would make `AdminOrderService.CancelOrderAsync`
accept unpaid orders it refuses today, firing a cancellation email, a SignalR broadcast, a doomed
Stripe refund and a cloud-original purge for an order nobody paid for. Cancelling unpaid orders may
well be wanted, but it is a different story with its own copy and its own tests, and it is not this
bolt's to ship. A named method keeps admin cancellation bit-identical, keeps the machine the only
place that knows the lifecycle, and costs one line in its `///` transition table.

*The one place `Cancelled` still bites, checked rather than assumed.* `Cancelled` is a cloud-original
purge sweep status (`ArchiveSettings.OriginalPurgeSweepStatuses`), so `OriginalPurgeRecoveryScanner`
will pick up an abandoned holder — and `OriginalPurger` skips an upload only while a `Paid` or
`Printing` sibling order holds it, not an `AwaitingPayment` one. The scanner nevertheless cannot
touch this case: it selects only orders whose items point at an upload already in
`StorageLocation.Cloud`, and promotion to cloud happens on payment, so an order that was never paid
has no cloud original to sweep. Where an upload *is* cloud-resident it was promoted by an earlier
paid order — which is itself the `Paid`/`Printing` sibling that makes the purger skip it. No change
is needed here; the fact is written down because it stops being true the moment promotion moves
earlier than payment.

*Release — no replacement follows, or the replacement drops the coupon.* Deleting the
`CouponRedemption` row and decrementing only if a row was actually deleted, in one transaction,
stays for rows 4 and 5 above and for the cases where nothing takes the abandoned order's place: a
`PaymentFailed` order that is never retried, a stale key that is never reused, and
`AdminOrderService.CancelOrderAsync`. Of those, the admin cancellation and rows 4–5 have callers in
this bolt; the other two have no trigger until a reclaim job exists, which is the residual below.

**Accepted residual, stated rather than hidden:** an order abandoned and never retried holds its
redemption indefinitely. There is no sweeper, because a periodic reclaimer is a new background
mechanism at feature grade (definition-of-done rule 2 and class 12) and this bolt has no budget
for one. The consequence is real and worth writing down: guest tokens are free, so a script can
exhaust a capped promo by creating orders it never pays for. The levers that exist today are the
admin raising `MaxRedemptions` and the per-identity rate limit above. A reclaim job for abandoned
`AwaitingPayment` orders is the recommended follow-up and is named in ddd-03; `ReleaseForOrderAsync`
is the function it would call, which is why the release path survives the move to transfer.

**Payable-gross floor.** Stripe cannot charge zero and rejects amounts under its per-currency
minimum; the resulting `StripeException` is not an `idempotency_error`, so it misses
`PaymentsController`'s only catch and would surface as a 500 — after the redemption had
committed, leaving the order permanently unpayable and the slot permanently spent. Two guards:

- the admin validator rejects `Percent` values of 100 (allowed range `(0, 100)`), because a
  full-price giveaway needs a zero-charge order flow that does not exist;
- `OrderService` refuses, **before opening the transaction**, when
  `payableGross < Stripe:MinimumChargeRon` (default `2.00`, Stripe's RON floor), with 409
  `ORDER_TOTAL_BELOW_MINIMUM`. No order, no redemption, an actionable Romanian message.

This corrects ddd-01, which said a fully discounted order is legal and still invoiced: it is not,
until a zero-charge path exists.

**InMemory divergence (stated, not hidden).** `ExecuteUpdateAsync` and real transactions do not
exist on the EF InMemory provider, the integration-test default. `CouponService` branches on
`Database.ProviderName` exactly as `OrderNumberService` and `StaticShippingService` already do:
on InMemory it re-reads the row, re-checks the whole redeemability rule and increments through
the change tracker. Because the CAS now runs **after** the order insert and after every early
return, the InMemory branch can no longer leave a pending increment on the request-scoped
context for `PaymentsController`'s later `SaveChangesAsync` to flush — the first draft's shape
would have made "a replay redeems nothing" true on PostgreSQL and false on InMemory. The InMemory
path is still **not** concurrency-safe and is not claimed to be: every redemption-semantics test
runs against real PostgreSQL (see Test Plan), and `ddd-03` says so under "what this suite cannot
prove".

### Cart-state lifecycle (`CartCoupon`)

**Reads re-validate and report; reads never write.** A `GET /api/cart` re-validates the stored
coupon on every read and puts the outcome in the body: `couponStatus` is `valid` or `stale`, and
when it is `stale`, `couponReason` carries the same `CouponErrorCodes` value the checkout 409
would carry (`INVALID_COUPON`, `COUPON_EXHAUSTED`, `MIN_SUBTOTAL_NOT_MET`), `discountRon` is `0`
and the totals are the undiscounted ones. `couponCode` stays populated so the SPA can name the
code it is refusing. The stored row is **not** deleted — not on reads, and no longer on writes
either: server-side auto-clear is gone. Two reasons. A stale coupon can become valid again (a
cart under `MinSubtotalRon` grows, a not-yet-active window opens), and deleting it destroys a
state the customer never asked to lose. And silently emptying the field is exactly the
invisible-failure shape that produced the original defect: a cart that looked clean on every read
and then 409'd at checkout. Bolt 048 renders the stale state and offers "remove coupon", which
calls the existing `DELETE /api/cart/coupon`.

Making the read *write* stays refused for the reasons it was refused before: it would turn every
cart poll on the checkout path into a write against the < 100 ms NFR, and a client disconnect
mid-request is swallowed by `ExceptionHandlerMiddleware` — the row would have been deleted with
no response and no log tying cause to effect. The read also stays silent in the log: it runs on
every poll, so a stale coupon is reported in the body, not shouted once per second into the log.

The checkout 409 stays as the last line of defence, with the same reason code in its body. A cart
read is a preview, never a reservation (invariant 3); the order path re-validates from scratch
under its own transaction and is the only authority. The cart state exists so the customer is
never surprised by that 409, not to replace it.

| Event | Effect |
|---|---|
| `POST /api/cart/coupon` valid | insert or replace the owner's single row |
| `POST /api/cart/coupon` invalid | nothing written; 422 |
| `DELETE /api/cart/coupon` | row deleted (no-op if absent) |
| `GET /api/cart` | re-validated **read-only**; a coupon that no longer qualifies comes back as `couponStatus: stale` with its reason code and a zero discount |
| `POST /api/cart` (`SetCartAsync`) | re-validated exactly the same way; nothing is written and nothing is deleted |
| `DELETE /api/cart` (`ClearCartAsync`) | row deleted in the same call |
| `POST /api/cart/merge` | the guest row moves to the user when the user has none; otherwise the user's row wins and the guest row is deleted |
| order created | row left in place — payment can still fail and the customer may retry with the same code |
| guest session expires | `GuestSessionCleanupJob` deletes the expired sessions' `CartCoupons` alongside the sessions |

Residuals, accepted and recorded:

- A stale coupon row survives until the customer removes it, the cart is cleared, or the guest
  session is swept. Nothing collects rows for coupons that expired months ago. They are harmless
  (every read re-validates and every one of them reports `stale`), and one row per owner is the
  ceiling, so a sweeper would buy nothing this bolt needs.
- Nothing server-side clears the cart (or therefore the applied coupon) after a successful
  payment; today only `DELETE /api/cart` from the SPA does. A customer who closes the tab keeps
  both their cart items and their applied code. This is the pre-existing cart-clearing gap, not
  a coupon-specific one, and a coupon still attached to a still-populated cart is not incorrect —
  the next checkout re-validates and consumes a slot legitimately. Not fixed here: the fix lives
  in the Paid transition, which is 038-039 territory and parked by the owner's 2026-09-03 ruling.
- `GuestSessionCleanupJob` deletes only sessions with `ClaimedByUserId == null`, so a claimed
  session's `CartCoupon` is not swept by the new delete either. In practice the merge that
  claims a session moves or deletes that row, so the residual is a row orphaned by a merge that
  never happened — the same shape `CartItem` already has.

**Inherited divergence this bolt sits on top of (not introduced by it, not fixed by it).**
`CartService` prices a group off *that size's* tiers using the group's total copies
(`CartService.cs:245`), while `OrderService` prices each item off *all active sizes'* tiers using
that item's own quantity (`OrderService.cs:561`) — a difference the code documents as deliberate.
So the cart's subtotal and the order's subtotal can already differ today, before any coupon
exists. The coupon inherits it in two places: `MinSubtotalRon` is evaluated against the cart
basis at apply time and the order basis at checkout, and a `Percent` discount is computed on
each. Because bulk tiers make the cart basis the cheaper of the two, the usual direction is that
the checkout discount is slightly larger than previewed, not smaller; the reverse can make a
checkout answer 409 `MIN_SUBTOTAL_NOT_MET` for a cart the customer was told qualified. Unifying
the two pricing bases is a change to the core pricing path, well beyond this bolt's stories, so
it is recorded here and in the hand-off rather than attempted.

### Caller-impact sweep

Every existing consumer of a contract this bolt touches. No blank rows.

**`CartResponseDto` (record gains 9 members)**

| Consumer | Outcome |
|---|---|
| `Services/CartService.BuildResponse` | **updated** — computes and passes the coupon fields |
| `Services/CartService.GetCartAsync` (`CartResponseDto.Empty`) | **updated** — `Empty` gains zeroed coupon fields |
| `Services/CartService.SetCartAsync` / `MergeCartsAsync` | **updated** — both re-validate read-only, nothing is auto-deleted; merge moves the guest row |
| `Services/ICartService` | **updated** — new `ApplyCouponAsync` / `ClearCouponAsync` members |
| `Controllers/CartController` (3 `ProducesResponseType`) | **updated** — two new actions; existing attributes unchanged |
| `Tests/Integration/CartControllerIntegrationTests` (5 deserialisation sites) | **unaffected** — added members deserialise without touching the assertions; coupon cases are new tests |
| `Tests/Unit/Services/CartServiceTests:106` (`BeEquivalentTo(CartResponseDto.Empty)`) | **unaffected** — compares against the same updated constant |
| UI `core/models/cart.model.ts` (`CartResponseDto`, `EMPTY_CART`) | **updated** (bolt 048) |
| UI `core/services/cart.service.ts` + `.spec.ts` (`makeCart` helper) | **updated** (bolt 048) |
| UI `features/cart/pages/cart-page.ts` | **updated** (bolt 048) |
| UI `features/checkout/pages/review-step.ts` + `.spec.ts` (`makeCart`) | **updated** (bolt 048) |

**`IOrderService.CreateFromCartAsync` (behaviour extended, signature unchanged)**

| Consumer | Outcome |
|---|---|
| `Controllers/PaymentsController:59` | **unaffected in signature, newly reachable statuses** — the four new 409s flow through the existing middleware; `amountBani` reads `order.TotalRon`, already discounted, and the payable floor guarantees it is chargeable |
| `OrderService.DivergentFields` / `ItemsSignature` / the `total` local | **updated** — the divergence check moves to the pre-discount gross (see Concurrency). This is the row the first draft missed entirely |
| `Tests/Unit/Services/OrderServiceTests` (24 call sites) | **unaffected** — no coupon applied → the untouched no-transaction path; one new test pins that a coupon-free order's divergence answer is unchanged |
| `Tests/Unit/Services/OrderServiceIdempotencyConcurrencyTests` (6 call sites) | **unaffected** — same reason |

**`ICouponService` (two members change shape)**

`ResolveForCartAsync` stops returning `null` for "applied but unusable" — it returns a
`CartCouponView(Guid CouponId, string Code, CouponType Type, decimal DiscountRon, bool IsStale,
string? ReasonCode)` and is `null` only when the owner has no row at all, which is the single
change that makes a stale coupon distinguishable from no coupon. Its `deleteWhenUnusable`
parameter goes away with the auto-clear it existed to switch on. `ResolveForOrderAsync` gains an
optional held-coupon id so the retry path can validate against `RedemptionsCount - 1`.

| Consumer | Outcome |
|---|---|
| `Services/CartService.BuildResponseAsync` | **updated** — maps the view onto `couponStatus` / `couponReason`, zeroes the discount when stale |
| `Services/CartService.AddItemAsync:150` (`deleteUnusableCoupon: true`) | **updated** — the only caller that passed `true`; the flag and the call argument both go |
| `Services/CartService.SetCartAsync` / `ClearCartAsync` / `MergeCartsAsync` | **unaffected in behaviour** — they pass no flag today; clear and merge still delete rows deliberately, which is not auto-clear |
| `Services/OrderService.CreateFromCartAsync` | **updated** — passes `heldSlot?.CouponId` into `ResolveForOrderAsync` |
| `Services/AdminOrderService.CancelOrderAsync` (`ReleaseForOrderAsync`) | **unaffected** — that member's signature does not change |
| `Program.cs` DI registration | **unaffected** — same interface, same lifetime |
| `Tests/Unit/Services/AdminOrderServicePaidRaceTests` (DI composition) | **unaffected** — already registers `ICouponService` |
| the `coupon.auto-cleared` log line | **deleted** — no code path can emit it once reads and writes both stop deleting |

**`OrderStatusMachine` (+1 member, `ValidTransitions` byte-identical)**

`Abandon(Order order)` is a named method, not two new `ValidTransitions` rows, so nothing that
asks `CanTransition` sees a new answer. The sweep below is over consumers of
`OrderStatus.Cancelled`, because an unpaid `Cancelled` order is a state the system has never held
before.

| Consumer | Outcome |
|---|---|
| `Services/OrderStatusMachine.ValidTransitions` / `CanTransition` / `Transition` | **unaffected** — untouched; `Abandon` is a separate entry point with its own legality check (`AwaitingPayment`, `PaymentFailed`) |
| `Services/AdminOrderService.CancelOrderAsync:273` | **unaffected, deliberately** — still refuses unpaid orders, because the transition table did not widen; cancelling unpaid orders is a different story with its own copy, email and tests |
| `Controllers/WebhooksController.HandleStripePaymentSucceededAsync` | **unaffected in code, newly reachable branch** — a `Cancelled` holder falls to the existing else and logs *customer charged, order not Paid, manual reconciliation required*; the payment-side remedy is the parked PPW-687…PPW-690 cluster |
| `Configuration/ArchiveSettings.OriginalPurgeSweepStatuses:62` + `BackgroundJobs/OriginalPurgeRecoveryScanner` | **unaffected in practice, checked not assumed** — the scanner selects only orders whose items point at an upload already in `StorageLocation.Cloud`, and promotion happens on payment, so an order that was never paid has no cloud original to sweep; where one is cloud-resident it was promoted by a paid order, which is the `Paid`/`Printing` sibling `OriginalPurger:105` skips |
| `Services/AdminStatsService` (6 sites) | **unaffected and correct** — excludes `Cancelled` from revenue and counts, which is what an abandoned unpaid holder should contribute |
| `Services/OrderPhotoPromoter:88` | **unaffected** — skips `Cancelled`, so an abandoned holder promotes nothing |
| `Services/Sameday/AwbCreator:230` | **unaffected** — skips `Cancelled`, so no AWB is cut for an abandoned holder |
| `Data/Seed/DevDataSeed:261` | **unaffected** — seed data only |
| UI order status labels / admin filters | **unaffected** — `Cancelled` already renders; bolt 048 adds no new status |

**`Order` entity (+2 columns) / order money contract**

| Consumer | Outcome |
|---|---|
| `Services/OrderService.GetOrderDetailAsync` → `OrderDetailDto` | **updated** — carries `CouponCode` + `DiscountRon`, otherwise the page shows subtotal + shipping ≠ total |
| `Controllers/OrderPaymentStatusController` → `OrderPaymentStatusDto` | **updated** — the confirmation page needs the discount (bolt 048) |
| `Services/AdminOrderService.BuildDetailDto` → `AdminOrderDetailDto` | **updated** — same reconciliation argument |
| `Services/AdminOrderService.CancelOrderAsync` | **updated** — releases the order's redemption |
| `Services/Invoicing/InvoiceCreationService` | **unaffected** — snapshots `order.NetTotalRon/VatRon/TotalRon`, already discounted |
| `Services/Invoicing/InvoicePdfDocument` | **updated in bolt 048** — a `Reducere` line above the VAT total |
| `Services/Invoicing/InvoiceXmlBuilder` | **updated** — see the invoice section below; the residual-adjustment loop is the part that must change, not just the totals block |
| `Controllers/WebhooksController:291` (logs `order.TotalRon`) | **unaffected** |
| `Controllers/PaymentsController:81` (`amountBani`) | **unaffected** — charges the discounted total, which is the intent |
| `Services/AdminStatsService` (revenue) | **unaffected** — sums `TotalRon`, already net of discount |
| `Data/Seed/DevDataSeed` (6 orders) | **unaffected** — `DiscountRon` defaults to 0 |
| UI `core/models/order.model.ts`, `payment.model.ts`, `admin.model.ts` | **updated (optional members)** in bolt 048, so existing spec mocks keep type-checking |
| UI `features/orders/pages/order-detail-page.ts` / `confirmation-page.ts` | **updated** in bolt 048 |
| UI `features/admin/pages/order-detail/admin-order-detail-page.ts` | **updated** in bolt 048 — one discount row |

**`ExceptionHandlerMiddleware` (mapping table + one extension member)**

| Consumer | Outcome |
|---|---|
| every existing mapped exception type | **unaffected** — additive entries; `code` is written only for `IErrorCoded`, which no existing type implements |
| `Tests/Integration/MappedServerErrorSentryTests` | **unaffected** — asserts 5xx capture, untouched |
| UI `core/interceptors/error.interceptor.ts` | **unaffected** — reads `status`, not extensions |

**`PhotoPrintDbContext` / migration**

| Consumer | Outcome |
|---|---|
| `Tests/Integration/MigrationChainTests` | **exercised** — proves the edited baseline applies from empty and the snapshot matches |
| `Helpers/PostgresTestDatabase` (schema-hash pooling) | **unaffected by design** — the hash changes, a new pooled database is leased, stale ones are swept |
| every other service using the context | **unaffected** — additive DbSets |

**`GuestSessionCleanupJob` / `SecurityExtensions` / `StripeSettings`**

| Consumer | Outcome |
|---|---|
| `BackgroundJobs/GuestSessionCleanupJob.CleanupAsync` | **updated** — deletes the expired sessions' `CartCoupons` in the same unit of work |
| `Extensions/SecurityExtensions` rate-limiter block | **updated** — one added per-identity policy; the global and auth limiters are untouched |
| `Configuration/StripeSettings` | **updated** — one added `MinimumChargeRon` property, defaulted, so no configuration change is required anywhere |
| `Tests/Integration/RateLimitIntegrationTests` | **unaffected** — asserts the global and auth limiters; the coupon policy gets its own test |

### Invoice representation of a discount

`Invoice` snapshots the order's already-discounted `NetTotalRon` / `VatRon` / `TotalRon`, so the
totals are correct for free. The **lines** are not, and the existing code makes it worse than a
missing feature:

`InvoiceXmlBuilder` builds one line per order item from the undiscounted gross line total, plus a
`Transport` line, extracts each line's net, and then adds the whole rounding residual to the last
line so the lines sum to `invoice.NetTotalRon`. With a discount that residual **is the discount**,
so the entire discount would be subtracted from whichever line happens to be last — normally
`Transport`, which can drive its `LineExtensionAmount` negative and be rejected by EN16931. That
is precisely the false transport line ddd-01 refuses to print.

Corrected shape:

```text
  lineNetTarget      = net of the UNDISCOUNTED gross = Extract(SubtotalRon + ShippingCostRon).Net
  per-line nets      = extracted per line, residual reconciled against lineNetTarget
  discountNet        = lineNetTarget - invoice.NetTotalRon        (0 when there is no coupon)

  cac:AllowanceCharge  ChargeIndicator=false, AllowanceChargeReason="Reducere <CODE>",
                       Amount=discountNet, TaxCategory S @ rate
  LineExtensionAmount  = lineNetTarget
  AllowanceTotalAmount = discountNet          (element omitted entirely when 0)
  TaxExclusiveAmount   = invoice.NetTotalRon  ( = LineExtensionAmount - AllowanceTotalAmount )
  TaxInclusiveAmount   = invoice.TotalRon
  PayableAmount        = invoice.TotalRon
  TaxTotal/TaxableAmount = invoice.NetTotalRon
```

With no discount every value collapses to today's, so an undiscounted invoice is byte-identical
and the existing tests stand. The builder reads the undiscounted gross from `order`, so `Invoice`
needs no new column. This answers requirements Q3 (the architect review's P20 proposal): the
shipped `Order.CouponCode` + `DiscountRon` shape is kept, and the UBL `AllowanceCharge`
representation is adopted for ANAF correctness.

### Security Design

| Concern | Approach |
|---|---|
| Coupon enumeration | Apply is behind `DualAuth` **and** a per-identity 15/min limiter. `INVALID_COUPON` covers unknown, inactive, not-yet-valid and expired alike, so those four are indistinguishable. `MIN_SUBTOTAL_NOT_MET` *does* reveal that a live code exists and leaks its threshold — that is forced by the story's acceptance criteria, which fix both the code and the Romanian copy; the rate limit is the compensating control and the residual is recorded here rather than denied |
| Admin-only surface | `[Authorize(Roles = "Admin")]` on the controller, matching `AdminProductsController`; every mutation logs the acting admin's user id |
| Client-supplied discount | Never trusted: the discount is computed server-side from the stored coupon at both preview and order time. The request body carries a code and nothing else |
| Cross-owner access | `CartCoupon` rows are looked up by the caller's own identity only; there is no id-addressable cart-coupon endpoint |
| Admin rename racing a redemption | The rename is itself a CAS — `WHERE Id = @id AND RedemptionsCount = 0` — so a redemption committed between the read and the write makes the rename fail with 409 instead of leaving an order (and possibly an issued invoice) naming a code no coupon has |
| PII in logs | Log lines carry the code, the reason and ids — never email or address |
| Injection | EF parameterisation throughout; the CAS is `ExecuteUpdateAsync` over a LINQ predicate, not string SQL |
| Unbounded paging | `page` and `size` are clamped (`page >= 1`, `size` 1–100) rather than 422'd, matching the neighbouring admin controllers; `page = 0` cannot produce a negative `Skip` |

### NFR Implementation

| Requirement | Design approach |
|---|---|
| Apply coupon p95 < 100 ms | One indexed equality lookup on `ix_coupons_code` (normalisation makes the btree usable) plus the cart query the endpoint already runs; the read path writes nothing |
| Redemption adds no cost to un-couponed orders | The transaction and the CAS exist only when a coupon is applied |
| Redemption holds no lock longer than it must | The CAS is the last statement before COMMIT |
| Invoice correctness | Discount reduces the gross before VAT extraction; `Invoice` snapshots the order, so the maths has exactly one implementation |
| Observability | Structured events at Information: `coupon.applied`, `coupon.rejected`, `coupon.redeemed`, `coupon.released`, `coupon.exhausted`, `coupon.transferred`, `admin.coupon.*` |
| New mechanisms at feature grade | Two: the CAS (ADR-025, ADR-016's pattern on a second table) and the per-identity rate limiter (config, stated default, 429 contract, test) |

### Backlog sweep (`reviews/state/backlog.md`)

Rows whose Area this bolt touches — `payments`, `orders`. Nothing under `reviews/state/` is
edited by this bolt; the coordinator writes the re-deferral notes at merge time.

| Row | Decision |
|---|---|
| PPW-687, PPW-688, PPW-689, PPW-690 (declined-card double-charge cluster) | **re-deferred: owner ruling 2026-09-03**. Noted: this bolt transfers the redemption on the `PaymentFailed` retry path and marks the abandoned holder `Cancelled`, which touches the same code region; the payment semantics of a late success are unchanged and its tests assert the coupon side only |
| PPW-691, PPW-692, PPW-694 | **re-deferred: owner ruling 2026-09-03** |
| PPW-698, PPW-702, PPW-703, PPW-704, PPW-705 | **re-deferred: owner ruling 2026-09-03** |
| PPW-709, PPW-710 | **re-deferred: owner ruling 2026-09-03** |
| PPW-39 (global single-column idempotency uniqueness needs a per-tenant composite index) | **re-deferred: this bolt owns the wave's only migration, but the fix changes payment-idempotency semantics, not coupons; pulling it in would widen a money path beyond these stories** |
| PPW-32 (payments controller persists through the DbContext directly) | **re-deferred: the coupon path adds no controller-level persistence** |
| PPW-387, PPW-397 (webhook metric labels) | **re-deferred: webhook metrics untouched** |
| PPW-544 (new Must rules lack `WithMessage`) | **pulled in as a standard**: every new coupon validator rule carries a Romanian `WithMessage`, so this bolt adds no new instances of the class; the existing rows stay with their target |
| PPW-617 (paid-transition invoice retry implemented twice) | **re-deferred: the paid transition is untouched** |
| PPW-633 (mandatory fiscal address for Easybox) | **re-deferred: unrelated** |
| PPW-637, PPW-638, PPW-652, PPW-677 | **re-deferred: untouched paths** |
| PPW-610 (invoice-number-exhausted 409 replaced by a generic toast) | **re-deferred: admin invoice UI untouched** |
| PPW-504 (`OrderDetailDto` grew fields with no lens covering the frontend contract) | **pulled in as a constraint**: this bolt extends `OrderDetailDto` again, so the frontend contract change is listed in the sweep above and covered by a bolt-048 spec — which is what the row asked for |
| PPW-194, PPW-211, PPW-215, PPW-555, PPW-426 (admin ZIP export, cancellation logging) | **re-deferred: the export path is untouched.** `CancelOrderAsync` is edited for the redemption release, but its logging and ZIP behaviour are not |

### Failure-mode table

Every row names the test that goes red if the mode regresses. `[PG]` marks tests that must run
against real PostgreSQL because InMemory cannot exercise the mechanism.

| What can fail | What should happen | Which test proves it | Log line |
|---|---|---|---|
| Concurrent checkouts race for the last slot | Exactly `MaxRedemptions` redemptions; losers 409 `COUPON_EXHAUSTED`; **no** order row for them | `[PG] CouponRedemptionConcurrencyRelationalTests.ParallelCheckouts_ForCappedCoupon_RedeemExactlyTheCap_AndCreateNoLosingOrders` | `coupon.exhausted` |
| Coupon deactivated between apply and checkout | 409 `INVALID_COUPON` (not `COUPON_EXHAUSTED`), no order | `[PG] CouponRedemptionRelationalTests.Checkout_CouponDeactivatedAfterValidation_Returns409InvalidCoupon` | `coupon.rejected reason=INVALID_COUPON` |
| Coupon expires between apply and checkout | 409 `INVALID_COUPON`, no order; the window is in the CAS predicate | `[PG] CouponRedemptionRelationalTests.Checkout_CouponExpiredAfterValidation_Returns409` | as above |
| Order insert fails after validation | Transaction rolls back; count unchanged; no redemption row | `[PG] CouponRedemptionRelationalTests.Checkout_OrderInsertFails_LeavesNoRedemptionAndNoCountChange` | existing `DbUpdateException` warning |
| Idempotent replay of a coupon order | Original order returned; count unchanged; exactly one redemption row | `[PG] CouponRedemptionRelationalTests.Checkout_IdempotentReplay_DoesNotRedeemTwice` | `payments.idempotency.replay` |
| Replay of a **discounted** order is judged divergent | Replay succeeds; the divergence check uses the pre-discount gross | `OrderServiceCouponTests.Replay_OfDiscountedOrder_IsNotTreatedAsDivergent` and `OrderServiceTests.DivergenceCheck_ForCouponFreeOrder_IsUnchanged` | — |
| Declined card retried with the same key | The redemption **moves** to the replacement inside its transaction; count unchanged; one row, on the new order; the abandoned holder ends `Cancelled` | `[PG] CouponRedemptionRelationalTests.PaymentFailedRetry_TransfersTheRedemptionToTheReplacement` | `coupon.transferred` |
| Stale (>24 h) key reclaimed | Same transfer; count unchanged | `[PG] CouponRedemptionRelationalTests.StaleKeyReclamation_TransfersTheRedemptionToTheReplacement` | `coupon.transferred` |
| Replacement order fails to insert after the transfer statement | The whole transaction rolls back: the redemption is still on the abandoned order and `RedemptionsCount` is unchanged | `[PG] CouponRedemptionRelationalTests.ReplacementInsertFails_LeavesTheRedemptionOnTheAbandonedOrder` | — |
| Late `payment_intent.succeeded` for an order whose redemption was transferred away | Refused by order state (`Cancelled` is terminal for payment), logged as needing manual reconciliation; the redemption stays on the replacement and the count does not move. Only the coupon side is asserted — the payment-side double-charge cluster PPW-687…PPW-690 is parked by owner ruling 2026-09-03 | `[PG] CouponRedemptionRelationalTests.LateSuccessForTheCancelledHolder_LeavesTheRedemptionOnTheReplacement` | — |
| Admin cancels an order | Its redemption is released; count decrements once, never below zero | `AdminOrderServiceCouponTests.CancelOrder_ReleasesRedemption_AndIsIdempotent` | `coupon.released` |
| Discount would leave an uncharageable total | 409 `ORDER_TOTAL_BELOW_MINIMUM` **before** any write; no order, no redemption | `OrderServiceCouponTests.Checkout_DiscountLeavesTotalBelowStripeMinimum_Returns409_AndWritesNothing` | `coupon.rejected reason=ORDER_TOTAL_BELOW_MINIMUM` |
| Admin creates a 100 % coupon | 422 at validation | `AdminCouponValidatorTests.Create_PercentValueOf100_IsRejected` | — |
| Discount larger than the goods subtotal | Capped at the goods subtotal | `CouponDiscountCalculatorTests.Compute_FixedValueAboveSubtotal_CapsAtSubtotal` | — |
| VAT computed in the wrong order | A test pins `VAT(gross − discount)` against the wrong-order figure | `OrderServiceCouponTests.Checkout_WithDiscount_ExtractsVatAfterDiscount_NotBefore` | — |
| Rounding drifts from ADR-019 | `AwayFromZero` at 2 dp everywhere | `CouponDiscountCalculatorTests.Compute_PercentWithHalfBani_RoundsAwayFromZero` | — |
| `FreeShipping` with zero shipping cost | Discount 0 → **no** redemption, **no** `CouponCode` on the order (invariants 2 and 3 hold) | `OrderServiceCouponTests.Checkout_FreeShippingWithZeroShippingCost_RedeemsNothing` | `coupon.rejected reason=NO_DISCOUNT` |
| `FreeShipping` discount exceeds the goods subtotal | Allowed — the cap is the payable gross, not the goods subtotal | `CouponDiscountCalculatorTests.Compute_FreeShipping_IsCappedAtPayableGross_NotGoods` | — |
| Unknown / inactive / not-yet-valid code applied | 422 `INVALID_COUPON`, nothing stored, indistinguishable from one another | `CartCouponEndpointsIntegrationTests.ApplyCoupon_UnknownInactiveOrExpired_AllReturn422InvalidCoupon` | `coupon.rejected` |
| Coupon applied to an empty cart | 422 `EMPTY_CART`, nothing stored | `CartCouponEndpointsIntegrationTests.ApplyCoupon_EmptyCart_Returns422EmptyCart` | `coupon.rejected` |
| Second code applied over a first | Replaced, not stacked; one row per owner | `CartCouponEndpointsIntegrationTests.ApplyCoupon_Twice_ReplacesSilently` | `coupon.applied` |
| Cart shrinks below the minimum after apply | Every read reports `couponStatus: stale`, `couponReason: MIN_SUBTOTAL_NOT_MET`, zero discount, and writes nothing; adding items back makes the same row report `valid` again | `CartServiceCouponTests.GetCart_SubtotalBelowMinimum_ReportsStale_AndWritesNothing` + `CartServiceCouponTests.GetCart_BackAboveTheMinimum_ReportsValidAgain` | — |
| Coupon expires between apply and the next cart read | The read reports `stale` / `INVALID_COUPON` with zero discount and deletes nothing | `CartServiceCouponTests.GetCart_CouponExpiredAfterApply_ReportsStaleInvalid` | — |
| Coupon exhausted by other customers between apply and checkout | The cart read reports `stale` / `COUPON_EXHAUSTED`; checkout still refuses with 409 `COUPON_EXHAUSTED` — the read is a preview, the order path is the authority | `CartServiceCouponTests.GetCart_CouponExhaustedAfterApply_ReportsStaleExhausted` + `CouponCheckoutIntegrationTests.CreateOrder_ExhaustedCoupon_Returns409` | `coupon.rejected` |
| Admin deactivates a coupon sitting in carts | Every affected cart read reports `stale` / `INVALID_COUPON` on its next poll, with the row untouched | `CartServiceCouponTests.GetCart_CouponDeactivatedByAdmin_ReportsStaleInvalid_AndWritesNothing` | — |
| Cart contents replaced wholesale (`POST /api/cart`) while a stale coupon sits on it | The write path re-validates exactly like a read: it reports `couponStatus` / `couponReason` and deletes nothing, so saving a cart can never silently drop the coupon | `CartServiceCouponTests.SetCart_WithStaleCoupon_ReportsStale_AndDeletesNothing` | — |
| Guest applies a code then logs in | The code survives the cart merge | `CartCouponEndpointsIntegrationTests.MergeCarts_GuestHadCoupon_TransfersToUser` | `coupon.applied` |
| Code enumeration probing | 429 after the per-identity limit, `Retry-After` set | `CouponRateLimitIntegrationTests.ApplyCoupon_BeyondPerIdentityLimit_Returns429` | existing rejection handler |
| Admin creates a duplicate code | 409 `DUPLICATE_CODE`, no row; normalisation means `vara25` collides with `VARA25` | `AdminCouponsIntegrationTests.CreateCoupon_DuplicateCodeDifferingOnlyInCase_Returns409` | `admin.coupon.create-rejected` |
| Admin renames a code while a checkout redeems it | 409 `CODE_IMMUTABLE_AFTER_REDEMPTION`; the rename CAS fails | `[PG] AdminCouponRelationalTests.RenameRacingARedemption_Fails_AndLeavesTheCodeIntact` | `admin.coupon.update-rejected` |
| Admin deletes an already-inactive coupon | 409 `COUPON_ALREADY_INACTIVE` | `AdminCouponsIntegrationTests.DeleteCoupon_AlreadyInactive_Returns409` | `admin.coupon.deactivate-rejected` |
| Non-admin calls an admin coupon endpoint | 403 | `AdminCouponsIntegrationTests.AdminEndpoints_NonAdminUser_Return403` | — |
| Discounted invoice reaches ANAF | `AllowanceCharge` emitted; `TaxExclusiveAmount = LineExtensionAmount − AllowanceTotalAmount`; no line goes negative | `InvoiceXmlBuilderTests.Build_OrderWithDiscountAndShipping_KeepsTransportLinePositive_AndReconcilesTotals` | — |
| Undiscounted invoice regresses | XML unchanged from today | `InvoiceXmlBuilderTests.Build_OrderWithoutDiscount_EmitsNoAllowanceCharge` | — |
| Guest session expires with a coupon applied | Row deleted with the session | `GuestSessionCleanupJobTests.Cleanup_ExpiredSessionWithCartCoupon_DeletesTheCoupon` | existing cleanup log |

### Test Plan

- **Unit, pure**: `CouponDiscountCalculatorTests` — all three types, caps, rounding, the
  payable-gross basis.
- **Unit, InMemory** (`PhotoPrint.Tests.Unit.Services`) — only where the mechanism under test is
  not the CAS: `CouponServiceTests` (validation matrix, apply/replace/clear, normalisation),
  `CartServiceCouponTests` (preview on read and on write, deleting nothing, stale reporting, merge transfer),
  `AdminCouponServiceTests` (paging, filters, soft delete), `AdminCouponValidatorTests`,
  `OrderServiceCouponTests` (discount maths, VAT ordering, payable floor, divergence check).
- **Integration, InMemory** (`PhotoPrint.Tests.Integration`): `CartCouponEndpointsIntegrationTests`
  (HTTP contract, status codes, the `code` extension, dual-auth), `AdminCouponsIntegrationTests`
  (role gate, CRUD, redemption stats), `CouponRateLimitIntegrationTests`.
- **Integration, real PostgreSQL** — everything that depends on the CAS or on a real transaction:
  `CouponRedemptionConcurrencyRelationalTests` (**the gate**) and `CouponRedemptionRelationalTests`
  (deactivation, expiry, rollback, replay, transfer and release paths), `AdminCouponRelationalTests`.
  `PostgresCouponFactory`, a `PostgresPaymentFactory` sibling, backs the API with a real database.
  The gate asserts: N parallel checkouts against `MaxRedemptions = 5` produce exactly 5
  `CouponRedemption` rows, `RedemptionsCount = 5`, exactly 5 orders carrying the code, and 409
  `COUPON_EXHAUSTED` for every other caller with no order row created.
- **Not proven by this suite** (carried into ddd-03): atomicity on InMemory; ANAF's own validator
  accepting the `AllowanceCharge` shape (schema-level reconciliation is asserted, the regulator's
  tooling is not run); redemptions held by orders that are abandoned and never retried; per-user
  redemption caps (out of scope).

---

## Validation

- [x] Architecture pattern selected and documented
- [x] All layers designed with responsibilities
- [x] API contracts defined, including the error-code contract and both convention deviations
- [x] Database schema designed, with the migration method stated
- [x] NFRs addressed
- [x] Security patterns applied
- [x] **Caller-impact sweep** — every consumer enumerated, no blank rows
- [x] **Failure-mode table** — every mode has a named test and a log line
- [x] Backlog sweep recorded
- [x] **Stage-2 adversarial design check run and folded in** (below)

## Adversarial design check — findings and disposition

### Check 1 — the whole design (stage 2)

One fresh agent, briefed to attack the design (races, missed callers, absent failure modes,
second-path asymmetry, money correctness, resource bounds). 16 findings; every one is dispositioned.

| # | Finding | Disposition |
|---|---|---|
| 1 | **Blocker.** `DivergentFields` compares `TotalRon`, so every retry of a discounted order 409s and the customer can never reach the order they created | **Fixed in design** — the comparison moves to the pre-discount gross; `DivergentFields` added to the caller sweep; two regression tests named |
| 2 | **Blocker.** A declined-card retry with the same key builds a second order and redeems the coupon twice | **Fixed in design** — the redemption is transferred to the replacement inside one transaction on the `PaymentFailed` retry path (no release-and-re-redeem window); failure-mode rows + `[PG]` tests |
| 3 | **Blocker.** Unpaid orders consume redemptions forever; free guest tokens make a capped promo trivially exhaustible | **Partly fixed, partly accepted in writing** — transferred on the two retry paths and released on admin cancellation; no sweeper (a new periodic mechanism, out of budget), so the abuse vector is stated with its levers and a reclaim job is recommended in ddd-03 |
| 4 | **Blocker.** A fully or heavily discounted order cannot be charged; the Stripe rejection is unmapped → 500 *after* the redemption committed | **Fixed in design** — payable-gross floor checked before any write (409 `ORDER_TOTAL_BELOW_MINIMUM`), `Percent = 100` rejected at validation; ddd-01's "a fully discounted order is legal" is corrected |
| 5 | **Blocker.** The existing residual loop in `InvoiceXmlBuilder` would dump the whole discount onto the last line — usually `Transport` — driving it negative, and double-count against `AllowanceCharge` | **Fixed in design** — the residual now reconciles to the undiscounted line-net target; `AllowanceCharge` carries the discount; two tests, one of which pins that an undiscounted invoice is unchanged |
| 6 | **Blocker.** The tests named as proof of the redemption guarantees were placed in the InMemory suite, and the InMemory increment leaks onto the request-scoped context for `PaymentsController` to flush | **Fixed in design** — every redemption-semantics test moves to real PostgreSQL and is marked `[PG]`; moving the CAS after the order insert removes the leak |
| 7 | **Serious.** "Goods subtotal" means two different numbers (cart basis vs order basis) and the design treated it as one | **Accepted and documented, not fixed** — a pre-existing divergence in the pricing path; unifying it is beyond these stories. Recorded in the cart-lifecycle section and flagged in the hand-off |
| 8 | **Serious.** The CAS-first placement holds the coupon row lock for the whole transaction (serialised checkouts) and creates a `40P01` deadlock that escapes both `23505` filters as a 500 | **Fixed in design** — the CAS moves to the last statement before COMMIT, which removes the cycle and shortens the lock to the commit window |
| 9 | **Serious.** One boolean cannot yield three reason codes, and the predicate omitted `ValidFrom`/`ValidUntil` | **Fixed in design** — the window is in the predicate; `affected = 0` triggers one classification read so deactivation is not reported as a usage limit |
| 10 | **Serious.** Admin rename is an unguarded check-then-act against a concurrent redemption | **Fixed in design** — the rename is a CAS on `RedemptionsCount = 0`; failure-mode row + `[PG]` test |
| 11 | **Serious.** `MIN_SUBTOTAL_NOT_MET` makes the endpoint a code-existence oracle, contradicting the stated security design | **Fixed and honestly restated** — the story fixes the code and its copy, so the oracle stays; a per-identity rate limiter is the compensating control and the security table now says so instead of claiming no oracle exists |
| 12 | **Serious.** `FreeShipping` contradicts the goods-subtotal cap, and zero shipping yields a zero discount whose redemption semantics were undefined | **Fixed in design** — the cap is the payable gross; a zero discount redeems nothing and writes no `CouponCode`; two tests |
| 13 | **Minor.** Nothing server-side clears `CartCoupon` after payment; the claimed-session cleanup path misses it | **Accepted and documented** — the cart-clearing gap is pre-existing and its fix lives in the Paid transition, which the owner's 2026-09-03 ruling parks |
| 14 | **Minor.** `GET /api/cart` would have become a write on every read | **Fixed in design, then tightened** — reads never write; the delete first moved to `SetCartAsync`, and the F9 amendment removed server-side clearing altogether (check 2, finding 8) |
| 15 | **Minor.** Two unrecorded contract divergences: paging shape vs the neighbouring admin controllers, and 422-vs-409 against the convention | **Fixed in design** — paging is clamped like its neighbours; the 422/409 split is recorded as an explicit second deviation with one status table for bolt 048 |
| 16 | **Minor.** Case-insensitive matching would defeat the index and leave uniqueness per-exact-case | **Fixed in design** — normalise on write, compare exactly, one `CouponCode.Normalize` |

### Check 2 — the F8 and F9 amendments (2026-09-04)

A second fresh agent, briefed on the amended sections only (the transfer path and the cart-state
lifecycle) and told to attack them the same way. 8 findings; every one is dispositioned, and the
first three would each have shipped a defect.

| # | Finding | Disposition |
|---|---|---|
| 1 | **Blocker.** With the pre-resolve release gone, the abandoned holder's own row still counts, so `Validate` exhausts a `MaxRedemptions = 1` coupon and the retry 409s `COUPON_EXHAUSTED` with the intent already dead — no order, no live intent, coupon locked to a dead order | **Fixed in design** — `ResolveForOrderAsync` is told the held coupon id and validates that coupon against `RedemptionsCount - 1` (decision table row 2) |
| 2 | **Blocker.** "The replacement has no redemption row yet" is false: `OrderService` inserts one unconditionally whenever a coupon applies, so repoint-plus-insert is a duplicate key on the unique `CouponRedemptions.OrderId` index — a 500 the save loop does not recover and InMemory cannot see | **Fixed in design** — the repoint **replaces** the insert on the transfer path; stated explicitly, with the detach rule that follows from it |
| 3 | **Blocker.** `ConsumeOrThrowAsync` would still run on the transfer path, double-counting an uncapped coupon and 409ing a capped one — rolling back a replacement the customer is entitled to | **Fixed in design** — the transfer path skips the CAS entirely; the count never moves |
| 4 | **Major.** Nothing pinned that the coupon resolved at retry is the coupon the held row carries (apply A, decline, apply B, retry) | **Fixed in design** — transfer only when the ids match; a different coupon releases the held row and takes a normal CAS (rows 3 and 4) |
| 5 | **Major.** A retry that resolves no coupon at all had neither a transfer nor a release, orphaning the slot | **Fixed in design** — explicit release branch (row 5) |
| 6 | **Major.** The repoint mechanism was unspecified, and house style (`ExecuteUpdateAsync`) is wrong here twice: it executes outside EF's batch, so the `UPDATE` can precede the replacement's `INSERT` and fail PostgreSQL's per-statement FK check, and it throws on the InMemory provider | **Fixed in design** — tracked-entity write flushed by one `SaveChangesAsync`, with a `[PG]` test pinning the statement order rather than trusting EF's batching |
| 7 | **Major.** Two new `ValidTransitions` rows had no caller-impact entry and would silently widen `AdminOrderService.CancelOrderAsync` to unpaid orders (cancellation email, SignalR broadcast, doomed refund, cloud purge) | **Fixed in design** — a named `OrderStatusMachine.Abandon(order)` instead, `ValidTransitions` byte-identical, plus a sweep block over all nine `OrderStatus.Cancelled` consumers |
| 8 | **Minor, several contract and doc gaps in F9.** `CouponResolution` carries no reason; `deleteWhenUnusable` becomes dead; `CouponCode = applied?.Code` makes stale indistinguishable from absent; no `ICouponService` sweep row; the Test Plan still said "delete on write"; `EMPTY_CART` is unreachable on the read path; no failure-mode row for the read-only re-validation on `POST /api/cart` | **Fixed in design** — `ResolveForCartAsync` returns a `CartCouponView` that is null only when no row exists, `deleteWhenUnusable` and the `coupon.auto-cleared` log are removed, `CouponCode` stays populated when stale, both sweep blocks are written, the Test Plan line is corrected, `EMPTY_CART` is deliberately excluded from the read-path reason set, and the `SetCartAsync` row is in the failure-mode table |

The check also cleared one hazard it went looking for: an abandoned holder cannot have its
originals purged from under a paid replacement, because `OriginalPurger` skips any upload a `Paid`
or `Printing` sibling order still holds.
