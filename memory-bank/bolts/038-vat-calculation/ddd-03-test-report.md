---
stage: test
bolt: 038-vat-calculation
created: 2026-06-03T06:00:00Z
---

## Test Report: vat-calculation

### Summary

- **Bolt-038-scoped tests**: 62 / 62 passed (1s)
- **Full suite**: 870 / 870 passed, 7 skipped (S3 cloud tests — require AWS credentials, expected), 0 failed (14s)
- **New test count delta**: +56 vs. pre-bolt baseline (814 → 870)

### Test Files

- [x] `src/PhotoPrint.Tests/Unit/Services/VatCalculatorTests.cs` (19 cases across 9 methods, including theories)
  - Story-001 example pinned: `(100.00, 0.19) → (84.03, 15.97)`
  - Free order yields zero VAT
  - Five representative `(gross, rate)` inputs verified
  - **ADR-019 rounding-mode invariant pinned**: `(gross=0.125, rate=0.25)` produces raw VAT 0.025, which under `AwayFromZero` rounds to 0.03 but under banker's `ToEven` would round to 0.02. The test asserts 0.03. If a future PR drops the explicit mode argument from `VatCalculator`, this test fails on the second iteration of `decimal.Round`.
  - Property: `Net + Vat ≈ Total ± 0.01` across ~7300 samples at 19%
  - Same property across 5 different VAT rates (5%, 9%, 19%, 21%, 27%)
  - Boundary checks: negative gross throws; rate out of range throws; rate-zero accepted with zero VAT (the helper is more lenient than the validator, by design — testing accidental zero-rate code paths)

- [x] `src/PhotoPrint.Tests/Unit/Configuration/VatSettingsValidatorTests.cs` (23 cases across 7 methods)
  - Default settings (rate=0.19, series=FT) valid
  - Rate ∈ (0, 1) accepted (5 representative values)
  - Rate at or outside the open interval fails (0, negative, ≥1)
  - Valid 2–10-uppercase-ASCII series codes accepted
  - Invalid series codes fail: empty, single char, lowercase, mixed case, contains digits, contains dash, contains underscore, too long
  - Aggregated failures: multiple violations produce multiple failure messages

- [x] `src/PhotoPrint.Tests/Unit/Services/Invoicing/SqliteInvoiceNumberingServiceTests.cs` (12 cases across 8 methods)
  - In-memory SQLite (NOT EF InMemory) — covers the LINQ translation path that EF InMemory mishandles
  - First number in empty year starts at 1, formats as `FT-2026-00001`
  - Next number increments past existing max (seeded gaps OK — `MAX + 1` jumps over them as expected)
  - Series partition is independent (`FP` starts at 1 even if `FT` has 5)
  - Year partition is independent (2027 starts at 1 even with 2026 at 42)
  - Sequential allocate-then-insert cycle yields 1, 2, 3, 4, 5
  - `InvoiceNumber.ToString()` pads to 5 digits (`FT-2026-00100`)
  - Empty / whitespace series throws `ArgumentException`
  - Year out of `[2000, 9999]` throws `ArgumentOutOfRangeException`

- [x] `src/PhotoPrint.Tests/Unit/Services/OrderServiceTests.cs` — extended with 2 new test methods (`CreateFromCartAsync_ValidCart_PopulatesVatBreakdown`, `CreateFromCartAsync_FreeOrder_HasZeroVat`):
  - Cart subtotal 6 + shipping 20 = gross 26.00 → `NetTotalRon=21.85, VatRon=4.15, VatRate=0.19`
  - Reconciliation invariant `|net + vat − total| ≤ 0.01` holds
  - Free order yields zero VAT — guards against div-by-zero / negative-VAT regressions ahead of intent 022 (coupons)

### Acceptance Criteria Validation

**Story 001 — VAT fields and computation**

- ✅ **EF migration adds `Orders.NetTotalRon numeric(18,2) NOT NULL DEFAULT 0`, `VatRon numeric(18,2) NOT NULL DEFAULT 0`, `VatRate numeric(5,4) NOT NULL DEFAULT 0.19`** — migration `20260603101910_AddVatAndInvoices` rewritten from SQLite scaffold to Postgres types (per the existing `AddSamedayOrderFields` convention).
- ✅ **`OrderService.CreateFromCartAsync` sets the breakdown via `VatCalculator.ExtractBreakdown(gross, rate)`** — verified by `CreateFromCartAsync_ValidCart_PopulatesVatBreakdown`.
- ✅ **`r` is read from `IOptions<VatSettings>.Value.Rate`, default 0.19** — DI registered in `Program.cs`; existing tests construct `OrderService` with `Options.Create(new VatSettings())` and get 0.19.
- ✅ **For cart subtotal 100.00 RON, `NetTotalRon=84.03, VatRon=15.97, TotalRon=100.00 + shipping`** — pinned by `VatCalculatorTests.Story_example_100_at_19_percent_yields_84_03_net_and_15_97_vat` (the story's literal example).
- ✅ **Order-summary API returns the breakdown** — `OrderDetailDto` extended with three additive fields; `GetOrderDetailAsync` projects them.

**Story 002 — Invoice entity and numbering**

- ✅ **`Invoices` table with the documented column set** — entity in `Models/Invoice.cs`; EF mapping in `PhotoPrintDbContext`; migration creates the table with Postgres types (`numeric(18,2)`, `character varying`, `timestamp with time zone`).
- ✅ **Postgres `SEQUENCE invoice_seq_ft_2026 START 1 INCREMENT 1`** — created idempotently in the migration via raw SQL; subsequent years are auto-created by `PostgresInvoiceNumberingService` via the same `IF NOT EXISTS` clause.
- ✅ **`IInvoiceNumberingService.NextNumberAsync("FT", 2026)` returns `FT-2026-00001`, `FT-2026-00002`, …** — verified by `SqliteInvoiceNumberingServiceTests.First_number_in_empty_year_starts_at_one` and `Sequential_calls_produce_monotone_numbers`. The Postgres implementation's contract is equivalent (ADR-020); `nextval()` atomicity is a Postgres guarantee, not our code.
- ✅ **No two concurrent transactions produce the same number** — for Postgres, this is the `nextval()` guarantee (atomic across all concurrent transactions). For SQLite (dev), the single-writer model serialises the `MAX + 1` sequence naturally; concurrency isn't a target on dev.
- ✅ **Crossing into 2027 starts a new sequence `FT-2027-00001`** — verified by `Year_partition_is_independent`. Postgres path's auto-create via `IF NOT EXISTS` handles year crossover without migration.

### Issues Found

Three issues surfaced during Stage 5; all resolved without changing the design.

1. **SqliteInvoiceNumberingService used `IssuedAt.Year` in its LINQ filter** — EF Core can't translate `.Year` through the project's SQLite `DateTimeOffset ↔ long` converter. Fixed by switching to a `[yearStart, yearEnd)` date-range comparison; works on both SQLite and Postgres without provider branching. Production-correctness improvement, not just a test artefact.

2. **`OrderServiceTests` construction broken by the new `IOptions<VatSettings>` ctor dep** — pure compile-time fix: added `Options.Create(new VatSettings())` to both `OrderService` construction sites in the tests. No behavioural change.

3. **`ShippingCostDto` constructor signature** — initial draft of `CreateFromCartAsync_FreeOrder_HasZeroVat` passed two arguments; the record has one. Mechanical fix.

### Notes

- **Stage 1 stated invariants are now pinned by tests**:
  - `Net + Vat ≈ Total ± 0.01` — property test
  - `VatRate` is a snapshot — implicit from how `OrderService` stores it; future test could pin by changing the setting and replaying via idempotency
  - `MidpointRounding.AwayFromZero` not `ToEven` — pinned by `Rounding_uses_AwayFromZero_not_banker_s_rounding`
- **The Postgres path has no direct unit tests** — by design (per ddd-02-technical-design's test plan). `nextval()` atomicity is a Postgres guarantee; replicating it in tests would require a Testcontainers Postgres instance, which is heavier than the value. The SQLite path covers the `IInvoiceNumberingService` contract; production Postgres correctness rests on the well-known `nextval()` semantics + the unique index backstop (ADR-020).
- **Backfill posture pinned by the migration's header comment** — pre-existing orders carry `NetTotalRon=0, VatRon=0, VatRate=0.19` and are not retroactively re-invoiced.
- **The `EF1002` warnings on raw SQL in `PostgresInvoiceNumberingService`** are intentional. Both `series` and `year` are validated upstream (`VatSettingsValidator` requires `series` matches `^[A-Z]{2,10}$`, `year` is checked at the method entry to be 2000–9999), so the interpolated SQL is injection-safe. The pattern matches `OrderNumberService.cs`.

### Forward references

- **Bolt 039** consumes everything this bolt ships. The `Invoice` table is ready, the numbering service is ready, the `Invoices` table has columns staged for `XmlPayload`, `PdfStoragePath`, `AnafUploadId`, `AnafStatus`, `LastError`. Bolt 039 owns the actual insertion at the Paid transition (inside the same DB transaction per ADR-020) and the ANAF lifecycle.
- **Intent 022 (coupons)** subtracts from the pre-VAT subtotal, then calls `VatCalculator.ExtractBreakdown` on the reduced gross. The formula stays exactly as written. The `CreateFromCartAsync_FreeOrder_HasZeroVat` test guards the zero-gross edge case ahead of that intent.
- **Bolt 044 meter `invoice_anaf_status_total{status}`** is unused today — increments ship with bolt 039.
