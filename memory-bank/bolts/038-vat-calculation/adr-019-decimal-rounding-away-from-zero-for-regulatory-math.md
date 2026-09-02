---
bolt: 038-vat-calculation
created: 2026-06-03T05:00:00Z
status: accepted
---

# ADR-019: `MidpointRounding.AwayFromZero` for Legal / Regulatory Decimal Math

## Context

The Romanian VAT calculation in `VatCalculator.ExtractBreakdown` rounds
to 2 decimal places using `MidpointRounding.AwayFromZero`. C#'s default
`decimal.Round(x, 2)` — with no explicit mode argument — uses
`MidpointRounding.ToEven` ("banker's rounding").

For a single calculation the choice looks academic. Across a codebase
that ships financial artefacts to a tax authority (ANAF e-Factura,
bolt 039), the choice is **load-bearing**. The same VAT-shaped math
recurs in several upcoming surfaces:

- Bolt 039: UBL XML body must agree to the cent with the
  `Order.VatRon` / `NetTotalRon` already persisted; ANAF rejects any
  discrepancy.
- Intent 022 (coupons): discount subtracts from the pre-VAT subtotal,
  then the same VAT extraction runs on the reduced gross — same
  rounding, same expected output.
- Future credit notes / refunds / per-product VAT rates.
- Future financial reports.

Any of these paths that calls `decimal.Round(x, 2)` without specifying
the mode silently picks up the .NET default (`ToEven`) and disagrees
with `VatCalculator`. The disagreement is small per row (one cent in
the rare half-cent cases), but it accumulates across many invoices
and is the kind of audit-time finding that costs days to chase.

We had to decide whether the rounding mode is a per-call-site choice
(implicit default acceptable) or a project-wide invariant.

## Decision

**All decimal rounding in legal / regulatory code paths uses
`MidpointRounding.AwayFromZero`. The default
`decimal.Round(x, decimals)` overload (no mode argument) is
FORBIDDEN in any path that produces a value written to an invoice,
submitted to ANAF, reported to a customer as a tax amount, or used in
any downstream calculation of one of those.**

Restated as invariants:

- VAT, totals, discounts, refunds — any cent-precision derived value —
  uses `MidpointRounding.AwayFromZero` explicitly.
- `VatCalculator.ExtractBreakdown` is the canonical reference. Any
  future class that does similar math (e.g. a future
  `DiscountCalculator`, `RefundCalculator`) imports its rounding
  approach from here, not from `decimal.Round`'s default.
- PR review on any `decimal.Round` call site in `Services/` or
  `Controllers/` must verify the mode is explicit. If the call lives
  in a path the regulator never sees (admin dashboard's "approximate
  total" badge, etc.), the default is fine — but the reviewer must
  consciously allow it.
- A unit test pins `VatCalculator`'s behaviour against the
  `ToEven` default — the test constructs an input where the two
  modes disagree and asserts `AwayFromZero`'s answer.

## Rationale

`AwayFromZero` is the established Romanian accountancy convention and
the rounding the ANAF reference tooling assumes. Banker's rounding is
the IEEE-754 default and what statistical literature recommends, but:

1. **Auditors expect `AwayFromZero`.** It's the rounding everyone
   learns in primary school. A line where 0.005 rounds to 0.01 looks
   correct; a line where 0.005 rounds to 0.00 looks like a bug, even
   though it's "statistically fair."
2. **ANAF's validators are deterministic and use the same convention.**
   Submissions where our VAT differs from ANAF's recomputation are
   rejected.
3. **`ToEven` is non-intuitive at the line level.** `0.005` rounds to
   `0.00` but `0.015` rounds to `0.02`. Side-by-side on a customer
   invoice, this looks like inconsistent math.
4. **The performance difference is negligible.**

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| **`MidpointRounding.AwayFromZero`** (chosen) | Matches Romanian convention + ANAF tooling; intuitive at the line level; deterministic | None for the regulatory audience | — |
| **`MidpointRounding.ToEven` (.NET default)** | Statistically fair across many roundings; IEEE-754 standard | Disagrees with Romanian accountancy convention; ANAF tooling differs; counter-intuitive line-by-line | Wrong primitive for the regulatory audience |
| **`MidpointRounding.ToZero` (truncation)** | Trivial | Systematically under-reports VAT → shorts the state → triggers audit | Legally wrong |
| **Round at the rendering layer (PDF / XML), not at compute** | Storage holds the unrounded decimal | Storage round-trips disagree with displayed values; downstream math accumulates floating-point-style noise on `decimal` | Wrong layer — rounding is part of the legal value, not a display concern |
| **Per-call-site choice (no project rule)** | Maximum flexibility | The default `decimal.Round` quietly returns to `ToEven` — exactly the trap this ADR exists to close | — |

## Consequences

### Positive

- **One canonical rounding for the entire regulatory surface.**
  Future tax-adjacent code has a single source of truth.
- **PR review has a clear rule.** Any `decimal.Round(x, n)` call in
  a `Services/` path without an explicit mode is flagged.
- **Test pins the behaviour.** The unit test that asserts
  `AwayFromZero`'s result for a half-cent input fails immediately if
  the call site loses its mode argument.

### Negative

- **A few extra characters per call site.** `decimal.Round(x, 2, MidpointRounding.AwayFromZero)`
  is verbose compared to `decimal.Round(x, 2)`. Trivial cost.
- **Code reviewers must know to look for it.** Mitigated by this
  ADR's "Read when" hints surfacing it during VAT-adjacent review.

### Risks

- **Risk: a new contributor calls `decimal.Round(x, 2)` without the
  mode** in a regulatory path. Highest-likelihood silent regression.
  Mitigation: this ADR + the unit test + PR review.
- **Risk: a third-party library reformats values with its own
  rounding** (PDF generator's currency formatter, XML serializer's
  numeric output). Mitigated by always passing pre-rounded values
  into those libraries — never depend on their internal rounding for
  the value's regulatory meaning.
- **Risk: the rule generalises too broadly.** Not every `decimal`
  rounding in the codebase is regulatory (admin dashboard cosmetic
  totals don't have to follow). Mitigated by scoping the rule to
  "legal / regulatory" paths; reviewers exercise judgement on the
  rest.

## Related

- **Stories**: 038-001-vat-fields-and-computation (the immediate
  consumer); bolt 039's UBL XML serialisation will use the same
  mode for its line-item VAT amounts.
- **Previous ADRs**: ADR-005 (idempotency excludes shipping
  address) — the replay path returns the persisted (already-rounded)
  values, so the rounding mode of the original creation is what an
  idempotent replay sees.
- **Future ADRs**: intent 022 (coupons) will follow this rule when
  computing discount-adjusted totals.
- **External**: [Romanian Fiscal Code (ANAF) — invoice rounding
  conventions](https://www.anaf.ro).
- **Read when**: writing any code that rounds a `decimal` to a fixed
  number of decimal places in a financial / regulatory context;
  reviewing PRs that touch VAT, totals, discounts, refunds, or any
  value written to an invoice or report; debugging "why does my
  invoice's `VatRon` disagree with ANAF's recomputation?"; tempted
  to "simplify" a `decimal.Round(x, 2, MidpointRounding.AwayFromZero)`
  call by dropping the mode argument (don't — it's load-bearing);
  designing similar rounding rules for other regulatory domains.
