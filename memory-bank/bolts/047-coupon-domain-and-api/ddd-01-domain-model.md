---
stage: model
bolt: 047-coupon-domain-and-api
created: 2026-09-03T20:45:00Z
---

## Static Model: coupon-domain-and-api

Scope: stories 001-coupon-schema, 002-cart-coupon-endpoints,
003-redemption-on-order-create, 004-admin-coupon-crud.

### Entities

- **Coupon** (aggregate root): `Id`, `Code` (uppercase, unique, `[A-Z0-9]{4,20}`),
  `Type` (`Percent|Fixed|FreeShipping`), `Value`, `MinSubtotalRon`, `ValidFrom`,
  `ValidUntil`, `MaxRedemptions` (nullable = unlimited), `RedemptionsCount`,
  `IsActive`, `CreatedAt`, `UpdatedAt`.
  Business rules:
  - `Code` is normalised to upper-case on write and matched case-insensitively on read.
  - `ValidUntil > ValidFrom`; `Value > 0`; a `Percent` value is in `(0, 100]`.
  - `RedemptionsCount` only ever increases, and only through a redemption.
  - `MaxRedemptions` is a hard cap: `RedemptionsCount <= MaxRedemptions` is an invariant
    the database enforces, not the application (see *Redemption invariants*).
  - Deactivation is soft (`IsActive = false`); a coupon row is never deleted, because
    `CouponRedemption` rows point at it and invoices are already issued against it.
  - `Code` is immutable once the coupon has at least one redemption — an issued invoice
    names the code.

- **CouponRedemption** (entity inside the Coupon aggregate, written once, never updated):
  `Id`, `CouponId`, `OrderId`, `UserId` (nullable — guests), `DiscountRon`, `RedeemedAt`.
  Business rules:
  - Exactly one redemption per order (`OrderId` unique) — an order cannot consume two
    coupons, and a retried order creation cannot consume two redemptions.
  - `DiscountRon` is greater than zero; a coupon that would discount nothing is not redeemed.
  - Immutable: refund-time reversal is a separate intent.

- **CartCoupon** (entity): `Id`, `UserId?`, `GuestSessionId?`, `CouponId`, `AppliedAt`.
  The applied-but-not-yet-redeemed state. **There is no `Cart` row in this system** — a
  cart is the set of `CartItem` rows sharing an owner — so "the applied code is stored on
  the cart row" from story 002 becomes a one-row-per-owner side table.
  Business rules:
  - Exactly one of `UserId` / `GuestSessionId` is set (same one-owner rule as `CartItem`
    and `Upload`).
  - At most one CartCoupon per owner — re-applying replaces, never stacks. This is the
    schema-level expression of "one coupon at a time" (requirements Q1).
  - It is *state*, not a promise: it carries no reservation on `RedemptionsCount`.

- **Order** (existing aggregate root, extended): `CouponCode` (nullable, the code as it
  read at order time), `DiscountRon` (non-negative, default 0).
  Business rules:
  - `CouponCode` is a **snapshot string**, not a foreign key: the order records what the
    customer was told, and stays readable if the coupon is later renamed or deactivated.
    The auditable link to the coupon row is `CouponRedemption.OrderId`.
  - Money invariant (see *VAT order*): `TotalRon = SubtotalRon + ShippingCostRon - DiscountRon`.

### Value Objects

- **CouponType**: `Percent` | `Fixed` | `FreeShipping`. Stored as a string
  (`HasConversion<string>()`, the codebase's enum convention).
- **CouponPreview**: `Code`, `Type`, `DiscountRon`, `SubtotalRon`, `TotalRon` — what the
  cart shows before checkout. Derived, never persisted.
- **CouponRejection**: a machine-readable reason (`INVALID_COUPON`,
  `MIN_SUBTOTAL_NOT_MET`, `COUPON_EXHAUSTED`, `DUPLICATE_CODE`) plus a Romanian message.
  The machine reason is the contract the frontend maps copy from; the message is the
  fallback.
- **DiscountAmount**: a non-negative RON amount, rounded to 2 decimals with
  `MidpointRounding.AwayFromZero` (ADR-019 — this value reaches an invoice and ANAF).
  Its cap is the amount it reduces: `Percent` and `Fixed` are capped at the goods subtotal,
  `FreeShipping` equals the shipping cost, and every type is bounded above by the payable gross.
  *(Corrected after the stage-2 design check, which found the earlier "capped at the goods
  subtotal" phrasing contradicted `FreeShipping` on a small basket with expensive delivery.)*

### Aggregates

- **Coupon** — Members: `Coupon` (root), `CouponRedemption`.
  Invariants:
  1. `MaxRedemptions IS NULL OR RedemptionsCount <= MaxRedemptions` — always, under any
     interleaving of concurrent checkouts.
  2. `RedemptionsCount` equals the number of `CouponRedemption` rows for the coupon.
  3. A redemption exists only for an order that also carries the matching `CouponCode`
     and a `DiscountRon` equal to `CouponRedemption.DiscountRon`.
- **Order** — unchanged boundary; gains two snapshot fields. The `CouponRedemption` row is
  created in the same transaction as the order but belongs to the Coupon aggregate.
- **CartCoupon** — its own tiny aggregate; deleting it never cascades to a Coupon.

### Coupon lifecycle

```text
                      admin POST                admin PUT/DELETE
                          |                            |
        (none) ------> Active ------------------> Inactive  (soft, terminal for new use)
                          |                            ^
                 time passes past ValidUntil           |
                          v                            |
                       Expired  ------------------------
                  (derived, not stored)

  Redeemable at instant t for goods subtotal S, iff:
      IsActive
   && ValidFrom <= t < ValidUntil
   && S >= MinSubtotalRon
   && (MaxRedemptions IS NULL OR RedemptionsCount < MaxRedemptions)
```

`Expired` is **derived from `ValidUntil`, never stored** — a stored status would need a
sweeper and could disagree with the clock. The admin list's `status=expired` filter is a
query predicate over `ValidUntil`, not a column.

### Redemption invariants (the load-bearing part of this bolt)

1. **A redemption is atomic with the order it belongs to.** Either the order row, its
   items, the `CouponRedemption` row and the incremented `RedemptionsCount` all exist, or
   none of them do. There is no window in which an order carries a discount that was never
   redeemed, and none in which a redemption points at an order that does not exist.
2. **Over-redemption is impossible, not merely unlikely.** The cap check and the increment
   are one operation decided by the database, not a read-then-write in application memory.
   The application never holds "the current count" and adds one to it.
3. **A preview is not a reservation.** `POST /api/cart/coupon` tells the customer what the
   discount *would* be. Between that call and checkout the coupon can be exhausted,
   deactivated or expire; the order-creation path re-validates everything from scratch and
   is the only authority. This race is accepted and named, not closed — closing it would
   mean holding stock-like reservations for abandoned carts.
4. **A rejected redemption creates no order.** A customer whose coupon is exhausted at
   checkout gets `409 COUPON_EXHAUSTED` and an unchanged cart — never a silently
   full-price order, and never an order row that has to be cleaned up afterwards.
5. **An idempotent replay redeems nothing.** Replaying a create-order request with the same
   `Idempotency-Key` returns the original order; it must not consume a second redemption.
6. **One coupon per order, one redemption per coupon per order.** Enforced by the unique
   index on `CouponRedemptions.OrderId`.
7. **A redemption held by an order that is provably abandoned is released.** *(Added after the
   stage-2 design check.)* A redemption is taken when the amount to charge is fixed, so an
   unpaid order holds a slot. Three paths already know an order is finished with: a
   `PaymentFailed` order whose idempotency key is being reused for a retry, a stale (>24 h) key
   being reclaimed, and an admin cancellation. Each deletes the redemption row and decrements
   the count, never below zero. Without this, one declined card burns two slots for one
   purchase. An order abandoned and never retried keeps its slot — a stated residual, not a
   claim of completeness.

### VAT order (irreversible once invoices are issued)

Romanian retail prices in this system are **VAT-inclusive** (gross); `VatCalculator`
*extracts* VAT from a gross amount rather than adding it on top (bolt 038, ADR-019).
The discount is therefore a reduction of the **gross** amount, and VAT is extracted from
the already-discounted gross:

```text
  goodsGross    = sum of item line totals             (VAT-inclusive)
  discount      = f(coupon, goodsGross, shippingGross) (capped, >= 0, 2dp AwayFromZero)
  shippingGross = server-resolved shipping cost        (VAT-inclusive)

  payableGross  = goodsGross + shippingGross - discount
  vat           = round(payableGross * rate / (1 + rate), 2, AwayFromZero)
  net           = payableGross - vat
```

Equivalently, and this is the sentence that matters:
**the discount is applied first and VAT is computed on what is left.**
Computing VAT on the undiscounted total and then subtracting the discount from the net
would overstate output VAT on every discounted order — money the seller does not owe but
has already declared. It is irreversible because the wrong figure is filed with ANAF and
printed on a legal invoice; correcting it afterwards needs a credit note per invoice.

Consequences that follow from this ordering:

- `Order.NetTotalRon + Order.VatRon = Order.TotalRon` still holds (within 0.01), with
  `TotalRon` already net of the discount.
- `Invoice` snapshots the order's figures, so an invoice is automatically correct once the
  order is.
- The invoice must still *show* the discount as its own line (Romanian fiscal practice:
  a discount is a separate line, not an adjusted unit price), and the e-Factura UBL must
  represent it as `cac:AllowanceCharge` with `cbc:ChargeIndicator=false` so that
  `TaxExclusiveAmount = LineExtensionAmount - AllowanceTotalAmount` reconciles.

Per-type discount definition:

| Type | Discount | Notes |
|---|---|---|
| `Percent` | `min(round(goodsGross * Value/100, 2, AwayFromZero), goodsGross)` | Applies to goods only, never to shipping. |
| `Fixed` | `min(Value, goodsGross)` | Capped at goods so the order can never go negative. |
| `FreeShipping` | `shippingGross` | Shipping stays on the invoice at its real price and the discount cancels it, so the transport line is not misstated. At cart time shipping is unknown, so the preview discount is 0 and the type is surfaced instead. |

Edge cases the model settles now: `discount > subtotal` is impossible by the cap;
`discount == subtotal` yields a zero goods contribution with VAT extracted from the
shipping remainder only.

**Correction after the stage-2 design check:** an order whose payable gross falls to zero — or
below the payment processor's per-currency minimum — is **refused**, not invoiced. Stripe cannot
charge zero, and the rejection would surface as a 500 *after* the redemption had committed,
leaving an unpayable order holding a redemption slot forever. A zero-charge order needs a
payment path that does not exist, so `Percent = 100` is rejected at coupon creation and a
checkout below the floor is refused with `ORDER_TOTAL_BELOW_MINIMUM`. A discount that would
reduce nothing (`FreeShipping` where shipping costs nothing) redeems nothing: no count
increment, no redemption row, and no `CouponCode` on the order, so invariants 2 and 3 hold.

### Domain Events

None are published — this codebase has no event bus, and introducing one for coupons would
be a new mechanism at feature grade (definition-of-done rule 2) with no consumer. The
observable facts are emitted as structured log events instead:

- **coupon.applied**: Trigger: preview accepted on the cart — Payload: code, discount, owner kind.
- **coupon.rejected**: Trigger: preview or order-time validation fails — Payload: code, reason.
- **coupon.redeemed**: Trigger: redemption committed with the order — Payload: code, order id, discount.
- **coupon.exhausted**: Trigger: the atomic cap check refuses at checkout — Payload: code, order attempt.
- **coupon.auto-cleared**: Trigger: a stored cart coupon stops validating on a cart read — Payload: code, reason.

### Domain Services

- **ICouponService**: Operations:
  `PreviewAsync(code, goodsSubtotal)` — preview or rejection;
  `ApplyToCartAsync(owner, code, goodsSubtotal)` — stores the CartCoupon, returns preview;
  `ClearCartCouponAsync(owner)`;
  `ResolveForCartAsync(owner, goodsSubtotal)` — the still-valid applied coupon, clearing it
  when it no longer validates;
  `TryRedeemAsync(couponId, orderId, userId, discount)` — the atomic cap-check-and-increment.
  Dependencies: `PhotoPrintDbContext`, logger.
- **CouponDiscountCalculator** (pure, static): Operations:
  `Compute(type, value, goodsGross, shippingGross)`.
  Dependencies: none — pure decimal maths, unit-testable without a database. Keeping it pure
  is what lets the ordering above be pinned by a test no infrastructure can weaken.
- **IAdminCouponService**: Operations: `ListAsync(status, page, size)`, `CreateAsync`,
  `UpdateAsync`, `DeactivateAsync`, `ListRedemptionsAsync(couponId, page, size)`.
  Dependencies: `PhotoPrintDbContext`, logger.

### Repository Interfaces

This codebase has **no repository layer** — services use `PhotoPrintDbContext` directly
(`CartService`, `OrderService`, `InvoiceCreationService` all do). Introducing repositories
for coupons alone would be an inconsistent new pattern. The DbContext gains three DbSets:
`Coupons`, `CouponRedemptions`, `CartCoupons`.

### Ubiquitous Language

- **Coupon**: the definition of an offer. Lives forever once created.
- **Code**: the string a customer types. Upper-case, unique across coupons.
- **Redemption**: the act of a coupon being consumed by one order. Countable, capped.
- **Applied**: a coupon attached to a cart. Reversible, not counted, not a reservation.
- **Preview**: the discount the cart would get, recomputed on every cart read.
- **Discount**: the RON amount removed from the gross payable total.
- **Exhausted**: `RedemptionsCount` has reached `MaxRedemptions`. Terminal for new orders,
  invisible on the coupon's own row (no status column).
- **Goods subtotal**: the VAT-inclusive sum of cart or order item line totals, excluding shipping.
- **Payable gross**: goods subtotal plus shipping minus discount; the amount charged, VAT included.
- **Soft delete**: `IsActive = false`. There is no hard delete of a coupon.

### Story coverage

| Story | Covered by |
|---|---|
| 001-coupon-schema | `Coupon`, `CouponRedemption`, `CartCoupon`, `Order.CouponCode/DiscountRon` |
| 002-cart-coupon-endpoints | `CartCoupon`, `CouponPreview`, `CouponRejection`, `ICouponService` |
| 003-redemption-on-order-create | Redemption invariants 1-6, VAT order, `TryRedeemAsync` |
| 004-admin-coupon-crud | `Coupon` lifecycle, `IAdminCouponService`, soft delete, code immutability |

### Deviations from the story text, settled in ddd-02 / ADR

1. **No `RowVersion`, no optimistic concurrency.** Stories 001 and 003 name a `RowVersion`
   column, an EF concurrency token and a retry-once. `data-stack.md` states this codebase has
   **no concurrency tokens anywhere** and that correctness under concurrency comes from
   database-side atomic operations; ADR-016 already chose compare-and-swap via
   `ExecuteUpdateAsync` for exactly this shape of problem. The invariant the story wants is
   preserved and strengthened — a CAS cannot lose a race that it then has to retry.
   Recorded as an ADR in stage 3.
2. **`CartCoupon` side table** instead of "a column on the cart row", because no cart row exists.
3. **`FreeShipping` keeps the shipping price on the invoice** and cancels it through the
   discount, rather than zeroing `Order.ShippingCostRon` as the requirements' assumption
   sketched — zeroing it would print a false transport line on a fiscal document.

---

## Validation

Checked against the `ddd-construction-bolt` stage-1 completion criteria by the executing
agent. This bolt runs unattended under the Wave 1 coordinator addendum, so specsmd
human-validation checkpoints are self-validated and the outcome recorded here; the
`bolt-process.md` gates (stage-2 adversarial check, stage-4 fresh-eyes review) run as
separate fresh agents and are not self-validated.

- [x] All domain entities identified and documented
- [x] Business rules captured for each entity
- [x] Aggregate boundaries defined
- [x] Domain events specified (as log events, with the rationale for having no bus)
- [x] Repository interfaces defined (recorded as deliberately absent, with rationale)
- [x] All stories covered by the domain model

**Outcome**: approved, proceeding to Stage 2 (Technical Design).
