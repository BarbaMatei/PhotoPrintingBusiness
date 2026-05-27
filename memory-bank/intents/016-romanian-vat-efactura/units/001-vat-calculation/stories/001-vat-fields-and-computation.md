---
id: 001-vat-fields-and-computation
unit: 001-vat-calculation
intent: 016-romanian-vat-efactura
status: draft
priority: must
created: 2026-05-25T10:15:00Z
assigned_bolt: 038-vat-calculation
implemented: false
---

# Story: 001-vat-fields-and-computation

## User Story

**As** the platform owner
**I want** every order to carry the VAT breakdown alongside its total
**So that** compliance and customer invoices are correct from the moment the order is created

## Acceptance Criteria

- [ ] EF migration adds `Orders.NetTotalRon numeric(18,2) NOT NULL DEFAULT 0`, `VatRon numeric(18,2) NOT NULL DEFAULT 0`, `VatRate numeric(5,4) NOT NULL DEFAULT 0.19`.
- [ ] `OrderService.CreateFromCartAsync` sets, for VAT-inclusive subtotal `S` and rate `r`:
  - `VatRon = round(S * r / (1 + r), 2)`
  - `NetTotalRon = round(S - VatRon, 2)`
  - `VatRate = r`
  - `TotalRon = S + shippingCost`  (shipping VAT included in totals; see Q1)
- [ ] `r` is read from `IOptions<VatSettings>.Value.Rate`, default 0.19.
- [ ] For a cart subtotal of 100.00 RON, the order has `NetTotalRon = 84.03, VatRon = 15.97, TotalRon = 100.00 + shipping`.
- [ ] An order-summary API response now returns the breakdown (FE will display in a later intent).

## Technical Notes

```csharp
public sealed class VatSettings
{
    public const string SectionName = "Vat";
    public decimal Rate { get; init; } = 0.19m;
}
```

- Document in `decision-index.md` that shipping is treated as VAT-inclusive at the same rate as goods. Q1 in requirements lets us revisit.

## Dependencies

### Requires
- intent 014 (clean totals)

### Enables
- 002-invoice-entity-and-numbering
- All of unit 002

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Free order (100% discount, intent 022) | `NetTotalRon = 0, VatRon = 0` |
| Rounding produces 0.01 cumulative difference | Acceptable; numbers reconcile within ±0.01 |
| Negative subtotal (defensive) | Reject upstream; never store negative VAT |

## Out of Scope

- Per-product reduced rates.
