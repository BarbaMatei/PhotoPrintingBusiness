---
bolt: 047-coupon-domain-and-api
created: 2026-09-04T00:50:00Z
status: accepted
superseded_by:
---

# ADR-026: A Discount Reduces the Gross Before VAT Is Extracted, Never the Net After

## Context

Romanian retail prices in this system are **VAT-inclusive**. `VatCalculator.ExtractBreakdown`
takes a gross amount and a rate and *extracts* the VAT that is already inside it — it never adds
VAT on top (bolt 038; the rounding mode is fixed by ADR-019). Every order stores the result as a
snapshot: `Order.NetTotalRon`, `Order.VatRon`, `Order.VatRate`, with the invariant
`Net + Vat = Total` (±0.01). `Invoice` copies those three figures verbatim at the Paid transition
and they are what reaches the customer's PDF and ANAF's e-Factura system.

Introducing a coupon inserts a subtraction into that chain, and there are two places it can go:

- **A:** subtract the discount from the gross, then extract VAT from what is left.
- **B:** extract VAT from the undiscounted gross, then subtract the discount from the net.

The two produce different numbers on every discounted order, and only one of them is the VAT the
seller actually owes. The choice is not reversible in the ordinary sense: once an invoice is
issued with the wrong figure, it has been printed as a legal document, filed with ANAF through
e-Factura, and entered in the VAT return. Correcting it is not an `UPDATE` — it is a credit note
per invoice plus an amended declaration.

The intent's requirements name the outcome ("Discount applies to pre-VAT subtotal; VAT computed
on the discounted net", "Critical: VAT is computed on the post-discount net") but do not state it
as a durable rule anywhere an agent working on a *later* feature — a refund, a partial return, a
B2B rate, a second currency — would find it. That is what this ADR exists for.

## Decision

**The discount is applied to the gross amount, and VAT is extracted afterwards.** Option A.

```text
  goodsGross    = sum of item line totals            (VAT-inclusive)
  shippingGross = server-resolved shipping cost      (VAT-inclusive)
  discount      = f(coupon, goodsGross, shippingGross)   (>= 0, 2 dp, AwayFromZero)

  payableGross  = goodsGross + shippingGross - discount
  vat           = round(payableGross * rate / (1 + rate), 2, AwayFromZero)
  net           = payableGross - vat
```

In code this is one call — `VatCalculator.ExtractBreakdown(payableGross, rate)` — and the only
thing this ADR forbids is calling it with a gross that has not yet had the discount taken out.

The order-level invariant that follows, and which any future money change must preserve:

```text
  Order.TotalRon = Order.SubtotalRon + Order.ShippingCostRon - Order.DiscountRon
  Order.NetTotalRon + Order.VatRon   = Order.TotalRon                (within 0.01)
```

On the invoice the discount must additionally be **visible as its own line**, not folded into
adjusted unit prices: a `Reducere` line on the PDF, and in the UBL a `cac:AllowanceCharge` with
`cbc:ChargeIndicator=false` whose net amount makes
`TaxExclusiveAmount = LineExtensionAmount − AllowanceTotalAmount` reconcile. Invoice lines keep
their undiscounted amounts.

## Rationale

VAT is owed on consideration actually received. If a customer pays 81 RON for goods listed at
101 RON, the taxable base is 81 RON, and the VAT inside it is what the seller declares. Option B
declares VAT on 101 RON and then hands the customer a smaller net — it overstates output VAT on
every discounted order. That is money the seller does not owe but has already told the state it
does, and it is wrong in the direction that is hardest to notice, because the customer's total is
still right and only the split is wrong.

Concretely, at 19% on 100 RON of goods, 20 RON courier and a 30 RON coupon:

| | Option A (chosen) | Option B (rejected) |
|---|---|---|
| Payable gross | 90.00 | 90.00 |
| VAT declared | 14.37 | 19.16 |
| Net | 75.63 | 70.84 |
| `Net + Vat` | 90.00 ✓ | 90.00 ✓ |

Both reconcile to the amount charged, which is exactly why the error survives a casual check.
The 4.79 RON difference per order is the seller's, and it accumulates silently across a campaign.

Keeping the discount as a separate invoice line rather than reducing unit prices is the other
half: it preserves the audit trail from catalogue price to amount paid, which is what a fiscal
inspection follows, and it is the representation EN16931 / CIUS-RO expects.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| Option B — VAT on the undiscounted gross, discount off the net | Invoice lines and VAT stay identical to an undiscounted order, so less code moves | Overstates output VAT on every discounted order; the error is invisible because the total still reconciles; unwinding it needs a credit note per invoice | Wrong, and expensively wrong |
| Pro-rate the discount across item lines and reduce unit prices | No `AllowanceCharge` needed; lines sum straight to the net | Destroys the catalogue-price-to-paid audit trail; rounding residue has to be dumped on some line; a customer comparing the invoice to the shop sees prices that never existed | Loses the trail an inspection follows |
| Treat the discount as a zero-rated negative line | Simple to emit | A negative line on a type-380 invoice is rejected by EN16931 validation, and a zero-rated line misstates the tax category | Invalid document |
| Store gross and net discounts as separate columns | Both figures always available | Two sources of truth for one amount; they can drift under a rate change | Violates "one constant, one home" |

## Consequences

### Positive

- Declared VAT equals VAT owed on every discounted order, with no reconciliation step.
- The rule is one sentence and one call site, so it is checkable by reading rather than by
  arithmetic.
- `Invoice` needs no discount column: it snapshots figures that are already correct.
- The e-Factura representation reconciles by construction, so a discounted invoice is not a
  special case at ANAF.

### Negative

- The invoice XML builder becomes more complex: lines must carry undiscounted nets while the
  totals carry discounted ones, and the existing rounding-residual adjustment has to reconcile
  against the undiscounted line sum rather than the invoice net.
- A fully discounted order (payable gross zero) cannot be charged by the payment processor and is
  therefore refused rather than invoiced. That is a consequence of this ordering meeting a
  processor minimum, and it is why `Percent = 100` is rejected at coupon creation.

### Risks

- **A later feature applies a discount after extraction** — a refund, a partial return, a loyalty
  credit, a B2B net-price mode — and silently reintroduces option B. Mitigated by this ADR's
  "read when" trigger listing those exact features, and by a unit test that pins the chosen
  figures against the option-B figures so the wrong ordering cannot pass green.
- **A second VAT rate** (reduced-rate goods, or an EU B2B customer) would make a single
  gross-level subtraction ambiguous: the discount would need allocating across rate groups before
  extraction. Out of scope today (every line is standard rate `S`), but this is the point at
  which this ADR must be revisited rather than extended.

## Related

- **Stories**: 003-redemption-on-order-create, 001-cart-coupon-ux (the invoice line)
- **Standards**: recorded in `standards/decision-index.md`; the money invariant is stated in
  `bolts/047-coupon-domain-and-api/ddd-01-domain-model.md`
- **Previous ADRs**: ADR-019 (`MidpointRounding.AwayFromZero` for regulatory decimal maths),
  ADR-020 (invoice numbering), ADR-021 (QuestPDF, where the `Reducere` line is rendered)
