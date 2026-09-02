---
stage: design
bolt: 038-vat-calculation
created: 2026-06-03T04:30:00Z
---

## Technical Design: vat-calculation

### Architecture Pattern

**Migration + pure-helper + thin-service**, behind no feature flag (this
is a structural change to the legal contract of every order; flag-gating
it would create two-modes-of-truth confusion). Provider-aware
implementation for the numbering service because Postgres and PostgreSQL have
fundamentally different concurrency primitives for monotone counters.

Rationale:
- Matches the project's two-provider convention: `Program.cs` already
  branches on `dbProvider` for several existing paths. Adding one more
  branch in DI is idiomatic.
- VAT computation is a pure-function concern; isolating it in
  `VatCalculator` keeps the formula testable as a property and prevents
  it from leaking into the `OrderService` body.
- Invoice numbering must be a separate seam because bolt 039 will need
  it on its own schedule (the worker that creates the XML/PDF needs a
  number too, not just `OrderService`).
- No feature flag: unlike the Sameday / Sentry / Observability bolts,
  there's no "baseline that should remain byte-identical." The schema
  changes the order shape — there is no off-mode.

### Layer Structure

```text
┌─────────────────────────────────────────────────────────────┐
│ Presentation     OrderDetailDto extension                    │
│                  (NetTotalRon / VatRon / VatRate added)      │
├─────────────────────────────────────────────────────────────┤
│ Application      OrderService.CreateFromCartAsync             │
│                    — calls VatCalculator                     │
│                    — persists 3 new columns                  │
│                  (Invoice creation lives in bolt 039;        │
│                   bolt 038 only ships the seam to allocate   │
│                   a number)                                  │
├─────────────────────────────────────────────────────────────┤
│ Domain           Order (extended, 3 new columns)              │
│                  Invoice (new aggregate root, persistence    │
│                    shape only — no behaviour bolt 038)        │
│                  VatCalculator (pure helper)                 │
│                  InvoiceNumber (value object, format)        │
├─────────────────────────────────────────────────────────────┤
│ Infrastructure   EF migrations (Orders ALTER + Invoices      │
│                    CREATE + sequence-seed for current year)   │
│                  IInvoiceNumberingService (interface)        │
│                  PostgresInvoiceNumberingService             │
│                  PostgresInvoiceNumberingService               │
└─────────────────────────────────────────────────────────────┘
```

### Project structure additions

```text
src/PhotoPrint.API/
├── Configuration/
│   └── VatSettings.cs                          ← new
├── Models/
│   └── Invoice.cs                              ← new entity
├── Services/
│   ├── VatCalculator.cs                        ← new pure helper
│   └── Invoicing/                              ← new folder
│       ├── IInvoiceNumberingService.cs
│       ├── PostgresInvoiceNumberingService.cs
│       ├── PostgresInvoiceNumberingService.cs
│       └── InvoiceNumber.cs                    ← value object
├── DTOs/Orders/
│   └── OrderDetailDto.cs                       ← MODIFIED (+ VAT fields)
├── Services/OrderService.cs                    ← MODIFIED (+ VAT call)
├── Data/PhotoPrintDbContext.cs                 ← MODIFIED (+ DbSet<Invoice>, mappings)
└── Migrations/
    └── <timestamp>_AddVatAndInvoices.cs        ← new EF migration

(No appsettings.json change beyond a single Vat section.)
```

### Configuration shape

```jsonc
"Vat": {
  "Rate": 0.19,
  "InvoiceSeries": "FT"
}
```

`VatSettings`:

```text
public sealed class VatSettings
{
    public const string SectionName = "Vat";
    public decimal Rate { get; init; } = 0.19m;
    public string  InvoiceSeries { get; init; } = "FT";
}
```

**Validation** (`VatSettingsValidator : IValidateOptions<VatSettings>`):
- `Rate ∈ (0, 1)` — can't be zero (VAT is real); can't be ≥ 1 (would
  break the extraction formula `r / (1+r)`).
- `InvoiceSeries` matches `^[A-Z]{2,10}$` — uppercase ASCII letters, 2–10.

No `Enabled` flag — VAT computation is unconditional.

### Domain layer: VatCalculator (pure helper)

```text
public static class VatCalculator
{
    public static VatBreakdown ExtractBreakdown(decimal grossTotalRon, decimal vatRate)
    {
        if (grossTotalRon < 0m)
            throw new ArgumentOutOfRangeException(nameof(grossTotalRon));
        if (vatRate < 0m || vatRate >= 1m)
            throw new ArgumentOutOfRangeException(nameof(vatRate));

        var vat = decimal.Round(
            grossTotalRon * vatRate / (1m + vatRate),
            decimals: 2,
            mode: MidpointRounding.AwayFromZero);

        var net = decimal.Round(
            grossTotalRon - vat,
            decimals: 2,
            mode: MidpointRounding.AwayFromZero);

        return new VatBreakdown(net, vat, grossTotalRon, vatRate);
    }
}

public readonly record struct VatBreakdown(
    decimal NetTotalRon, decimal VatRon, decimal TotalRon, decimal VatRate);
```

**Properties testable via xUnit `[Theory]`**:
- `Net + Vat ≈ Total` within 0.01 RON for any `(total, rate)` in
  `[0, 1_000_000]` × `(0, 0.5]`.
- For `(100.00, 0.19)`: `net == 84.03`, `vat == 15.97`.
- For `(0, 0.19)`: `net == 0`, `vat == 0`.

### Domain layer: Invoice entity (persistence shape only)

```text
public class Invoice
{
    public Guid           Id              { get; set; }
    public Guid           OrderId         { get; set; }
    public string         InvoiceNumber   { get; set; } = "";
    public string         Series          { get; set; } = "";
    public DateTimeOffset IssuedAt        { get; set; }
    public decimal        NetTotalRon     { get; set; }
    public decimal        VatRon          { get; set; }
    public decimal        TotalRon        { get; set; }
    public string?        XmlPayload      { get; set; }
    public string?        PdfStoragePath  { get; set; }
    public string?        AnafUploadId    { get; set; }
    public InvoiceAnafStatus AnafStatus   { get; set; } = InvoiceAnafStatus.Pending;
    public string?        LastError       { get; set; }
    public DateTimeOffset CreatedAt       { get; set; }
    public DateTimeOffset? UpdatedAt      { get; set; }

    public Order? Order { get; set; }
}

public enum InvoiceAnafStatus
{
    Pending, Submitted, Accepted, Rejected, Failed,
}
```

EF mapping (in `PhotoPrintDbContext.OnModelCreating`):
- Primary key on `Id`.
- Foreign key on `OrderId` → `Orders.Id`.
- Unique index on `InvoiceNumber`.
- `AnafStatus` stored as varchar(30) via `HasConversion<string>()` —
  matches the project's convention for enum-as-text (intent 001).
- `NetTotalRon`, `VatRon`, `TotalRon` mapped as `numeric(18,2)`.

Bolt 038 does NOT create any `Invoice` rows. The schema and entity exist
to be consumed by bolt 039.

### Domain layer: InvoiceNumber value object

```text
public readonly record struct InvoiceNumber(string Series, int Year, int Number)
{
    public override string ToString() => $"{Series}-{Year:D4}-{Number:D5}";

    public static bool TryParse(string raw, out InvoiceNumber result) { /* ... */ }
}
```

Just a formatted representation. The DB stores the string; this struct
exists for the few places that need to read it back semantically.

### Infrastructure: IInvoiceNumberingService

```text
public interface IInvoiceNumberingService
{
    Task<InvoiceNumber> NextNumberAsync(
        string series, int year, CancellationToken ct = default);
}
```

Two implementations, registered per provider:

#### `PostgresInvoiceNumberingService`

Strategy: `CREATE SEQUENCE IF NOT EXISTS` then `nextval()`, both via raw
SQL through `DbContext.Database.ExecuteSqlInterpolatedAsync`. Idempotent
on the create — safe to call before every nextval at negligible cost.

```text
Pseudocode (final SQL TBD in Stage 4):

    var seqName = $"invoice_seq_{series.ToLowerInvariant()}_{year}";

    // 1. Ensure the sequence exists (idempotent).
    await db.Database.ExecuteSqlRawAsync(
        $"CREATE SEQUENCE IF NOT EXISTS \"{seqName}\" START 1 INCREMENT 1");

    // 2. Allocate the next number atomically.
    var next = await db.Database
        .SqlQueryRaw<long>($"SELECT nextval('\"{seqName}\"')::bigint AS \"Value\"")
        .SingleAsync(ct);

    return new InvoiceNumber(series, year, (int)next);
```

**Concurrency**: `nextval()` is atomic across all concurrent
transactions — no two callers ever get the same number. This is the
load-bearing Postgres guarantee.

**Gap-on-rollback** (the load-bearing legal concern, surfaced in
Stage 1): if the calling transaction rolls back AFTER `nextval()`
returned, the sequence stays advanced; the number is "burned." For the
intended call path (numbered allocated by bolt 039 right before the
INSERT that commits) this is extraordinarily rare. The mitigation
strategy from the story's edge-case table holds:
- Allocate the number **inside the same transaction** that persists
  the Invoice row.
- Quarterly audit of `Invoices.InvoiceNumber` series vs the Postgres
  sequence's current value — gaps explained in an accountant-facing
  report.

#### `PostgresInvoiceNumberingService`

PostgreSQL has no `SEQUENCE`. Strategy: `SELECT MAX(Number)` for the
`(series, year)` partition, increment by one, write atomically. PostgreSQL
is single-writer so the entire `MAX + INSERT` sequence inside a
transaction is naturally serialised.

```text
Pseudocode:

    await using var tx = await db.Database.BeginTransactionAsync(ct);

    var max = await db.Invoices
        .Where(i => i.Series == series && i.IssuedAt.Year == year)
        .Select(i => (int?)EF.Functions.Json...)   // parse Number out of InvoiceNumber
        .MaxAsync(ct);
    // (More robust: persist Number as a column on Invoice too — see below.)

    var next = (max ?? 0) + 1;
    // bolt 039's insertion path uses this `next` to assemble the InvoiceNumber.
    await tx.CommitAsync(ct);
    return new InvoiceNumber(series, year, next);
```

**Wait — that doesn't parse cleanly.** PostgreSQL's JSON functions vary;
parsing `InvoiceNumber` strings to extract the number is fragile. Two
options:

1. Add a `Number int NOT NULL` column to `Invoices`. Slight schema
   bloat but the dev path becomes a trivial `MAX(Number)`. **Chosen.**
2. Use string parsing. Rejected (provider-specific quirks).

So `Invoice` actually has one more column:

| Column | Type | Notes |
|---|---|---|
| `Number` | `int NOT NULL` | The numeric portion of `InvoiceNumber`. Redundant with the full string but cheap to maintain and trivial to query. |

`Number` is filled at insert time from `nextval()`. The full `InvoiceNumber` string is
assembled at insert time and persisted alongside.


### Application layer: OrderService change

Single insertion point in `CreateFromCartAsync`, right before
`_db.Orders.Add(order)`:

```text
Pseudocode (Stage 4 will refine):

    var rate     = _vatSettings.Rate;
    var subtotal = orderItems.Sum(i => i.LineTotalRon);          // existing
    var shipping = await _shipping.GetShippingCostAsync(...)     // existing
    var gross    = subtotal + shipping.CostRon;                  // existing

    // NEW: extract VAT breakdown from the gross.
    var vat = VatCalculator.ExtractBreakdown(gross, rate);

    var order = new Order
    {
        // ... existing fields ...
        SubtotalRon     = subtotal,
        ShippingCostRon = shipping.CostRon,
        TotalRon        = gross,                                  // unchanged
        NetTotalRon     = vat.NetTotalRon,                       // NEW
        VatRon          = vat.VatRon,                            // NEW
        VatRate         = vat.VatRate,                           // NEW
        Items           = orderItems,
    };
```

**Idempotency replay compatibility** (ADR-005):

The replay path returns the previously-persisted `Order`. The new VAT
columns were persisted alongside the original; replay returns them
unchanged. No recomputation. If the `Vat:Rate` setting was changed
between original and replay, the snapshot in the order's `VatRate`
column is what the replay returns — which is correct per the
"snapshot" invariant from Stage 1.

### Presentation layer: OrderDetailDto extension

The customer-facing `GET /api/orders/{id}` already returns an
`OrderDetailDto`. Three fields are added:

```diff
 public sealed record OrderDetailDto(
     Guid OrderId,
     string OrderNumber,
     string Status,
     decimal SubtotalRon,
+    decimal NetTotalRon,
+    decimal VatRon,
+    decimal VatRate,
     decimal ShippingCostRon,
     decimal TotalRon,
     ...
 );
```

The FE will display them in a later intent; the backend ships the data
now. No new endpoint, no auth change, no contract break (additive only).

### Data Model

#### Migration 1 — Orders columns

```sql
ALTER TABLE "Orders" ADD COLUMN "NetTotalRon" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE "Orders" ADD COLUMN "VatRon"      numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE "Orders" ADD COLUMN "VatRate"     numeric(5,4)  NOT NULL DEFAULT 0.19;
```

**Backfill posture**: the `DEFAULT 0` is the migration-applied default
for existing rows. Pre-existing orders carry `NetTotalRon = 0,
VatRon = 0` which is **incorrect** for legal purposes — but those orders
predate the VAT feature and have no invoice. They will not be re-invoiced
retroactively. Documented in Stage 4's implementation walkthrough so an
auditor reading the DB knows why the columns are zero on old rows.

#### Migration 2 — Invoices table

```sql
CREATE TABLE "Invoices" (
    "Id"             uuid          PRIMARY KEY,
    "OrderId"        uuid          NOT NULL REFERENCES "Orders"("Id"),
    "InvoiceNumber"  varchar(50)   NOT NULL UNIQUE,
    "Series"         varchar(10)   NOT NULL,
    "Number"         int           NOT NULL,
    "IssuedAt"       timestamptz   NOT NULL,
    "NetTotalRon"    numeric(18,2) NOT NULL,
    "VatRon"         numeric(18,2) NOT NULL,
    "TotalRon"       numeric(18,2) NOT NULL,
    "XmlPayload"     text          NULL,
    "PdfStoragePath" varchar(500)  NULL,
    "AnafUploadId"   varchar(100)  NULL,
    "AnafStatus"     varchar(30)   NOT NULL,
    "LastError"      text          NULL,
    "CreatedAt"      timestamptz   NOT NULL,
    "UpdatedAt"      timestamptz   NULL
);

CREATE INDEX "ix_invoices_order_id" ON "Invoices"("OrderId");
CREATE INDEX "ix_invoices_anaf_status" ON "Invoices"("AnafStatus")
    WHERE "AnafStatus" IN ('Pending', 'Submitted', 'Rejected', 'Failed');
CREATE UNIQUE INDEX "uq_invoices_series_year_number"
    ON "Invoices"("Series", (EXTRACT(YEAR FROM "IssuedAt")::int), "Number");
```

The composite unique index on `(Series, year, Number)` is the
last-line-of-defence against numbering races. The Postgres sequence
should make duplicates impossible, but a unique constraint catches the
exotic case where the sequence is somehow reset (operator error during
restore, etc.).

The filtered index on `AnafStatus` accelerates the bolt-039 worker
that pulls pending/failed invoices.

#### Migration 3 — Postgres sequence seed (Postgres-only)

```sql
-- Idempotent: safe to re-run.
CREATE SEQUENCE IF NOT EXISTS "invoice_seq_ft_2026" START 1 INCREMENT 1;
```

Migration only seeds the current year's sequence (2026). Subsequent
years are auto-created by `PostgresInvoiceNumberingService` on first
call via the same `IF NOT EXISTS` clause.

### Security Design

| Concern | Mitigation |
|---|---|
| VAT computation tampered via client input | VAT is server-side only; the client never sends rate or breakdown. Inputs to `VatCalculator` come from `IOptions<VatSettings>` (boot-validated) and the persisted line totals. |
| Idempotency replay returns inconsistent breakdown | Snapshot persisted on first creation; replay returns it. No recomputation. |
| Race produces duplicate `InvoiceNumber` | Postgres `nextval()` is atomic; `uq_invoices_series_year_number` is the backstop; ADR-004 says state conflicts return 409 (out of scope for bolt 038 — bolt 039 owns insertion). |
| `XmlPayload` is large + sensitive | `text` column; logging never logs body (Sentry scrubber already redacts request bodies, ADR-018 not directly relevant but the pattern holds). |

### NFR Implementation

| Requirement | Design Approach |
|---|---|
| **VAT formula is deterministic** | `MidpointRounding.AwayFromZero` is explicit; not provider- or culture-dependent. Property test verifies. |
| **Numbering is gap-free under normal flow** | Postgres `nextval()` + same-transaction insert. Out-of-band gaps (transaction rollback) flagged for quarterly audit, not eliminated. |
| **Numbering is gap-free under concurrent Paid transitions** | `nextval()` is atomic; no two transactions ever see the same value. Test: 100 concurrent callers, assert 100 distinct numbers in `[1..100]`. |
| **Schema migrations are reversible** | EF migration's `Down()` drops the columns and the table. The sequence is dropped via raw SQL in the migration's `Down`. The Down path acknowledges data loss (the new columns can't be reconstructed). |
| **Numbering path works** | `PostgresInvoiceNumberingService` resolved at DI time; `nextval()` inside the caller's transaction. |

### Integration Points

```text
┌────────────────────────────────────────────────────────────┐
│ OrderService.CreateFromCartAsync                            │
│   ↓ calls VatCalculator.ExtractBreakdown(gross, rate)      │
│   ↓ persists Order with NetTotalRon / VatRon / VatRate     │
│                                                             │
│ (Bolt 039 future):                                          │
│ WebhooksController.HandleStripePaymentSucceededAsync       │
│   ↓ on Paid transition, calls                              │
│   ↓ IInvoiceNumberingService.NextNumberAsync("FT", 2026)   │
│   ↓ INSERT Invoice (in same transaction as Order update)    │
│   ↓ enqueue InvoiceUploadJob                                │
└────────────────────────────────────────────────────────────┘
```

Bolt 038 leaves the seam (`IInvoiceNumberingService` registered) and
the persistence shape (`Invoice` entity, `Invoices` table) but does not
write any rows. Bolt 039 picks both up.

### Wiring order in Program.cs

After the existing `// ── Payments` block, before the `// ── Admin` block:

```text
// ── Invoicing (intent 016 / bolt 038) ─────────────────────────────
builder.Services.Configure<PhotoPrint.API.Configuration.VatSettings>(
    builder.Configuration.GetSection(PhotoPrint.API.Configuration.VatSettings.SectionName));
builder.Services.AddSingleton<
    Microsoft.Extensions.Options.IValidateOptions<PhotoPrint.API.Configuration.VatSettings>,
    PhotoPrint.API.Validators.VatSettingsValidator>();
builder.Services.AddOptions<PhotoPrint.API.Configuration.VatSettings>().ValidateOnStart();

if (dbProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<
        PhotoPrint.API.Services.Invoicing.IInvoiceNumberingService,
        PhotoPrint.API.Services.Invoicing.PostgresInvoiceNumberingService>();
}
else
{
    builder.Services.AddScoped<
        PhotoPrint.API.Services.Invoicing.IInvoiceNumberingService,
        PhotoPrint.API.Services.Invoicing.PostgresInvoiceNumberingService>();
}
```

### Test plan (preview of Stage 5)

| Layer | Tests |
|---|---|
| Unit (pure helper) | `VatCalculatorTests` — `(100, 0.19) → (84.03, 15.97, 100)`; `(0, 0.19) → (0, 0, 0)`; rounding boundaries (e.g. `1.005` → up); property test that `net + vat ≈ total` ±0.01. |
| Unit (validator) | `VatSettingsValidatorTests` — rate ∈ (0,1), series matches the regex. |
| Unit (numbering — Postgres) | Mocked-DbContext-free: hard to exercise `nextval()` without a real Postgres. Either:  (a) integration test against a `Testcontainers.PostgreSql` instance, or  (b) testing-host that uses PostgreSQL path only. **Chosen: (b)** — PostgreSQL path covers the contract, Postgres path is verified by a single integration test below. |
| Unit (numbering — PostgreSQL) | `PostgresInvoiceNumberingServiceIntegrationTests` — sequential allocation; 100-thread concurrency test that asserts 100 distinct numbers in `[1..100]`; year crossover starts a new sequence at 1. |
| Integration (OrderService) | The existing `OrderService` tests are extended to assert the new VAT columns are populated correctly for various subtotals. |
| Integration (migration smoke) | `Testing` environment runs through the migration; existing tests already test boot-with-migration on PostgreSQL. |

### Open design questions (resolved before Stage 4)

1. **Add `Number` int column to `Invoices`, or parse from string?**
   → Add the column. Cross-provider parity, trivial query, ~4 bytes/row.

2. **VAT formula rounding mode** → `MidpointRounding.AwayFromZero`,
   explicitly. Romanian accountancy convention; banker's rounding
   produces "weird" totals that auditors flag.

3. **Sequence-per-(series, year) vs single sequence**
   → Per (series, year). Crossing into 2027 must start at
   `FT-2027-00001`, not `FT-2027-456291`. The legal trail reads
   year-by-year.

4. **Where does the bolt-038 increment to bolt-044's
   `invoice_anaf_status_total` happen?** → Not in bolt 038. The
   meter is defined; increments ship with bolt 039 where the status
   transitions live.

5. **`OrderDetailDto` contract break?** → Additive only — new fields
   on an existing DTO is non-breaking for JSON consumers. FE will
   ignore unknown fields it doesn't render yet.

### Forward references

- **Bolt 039** is the load-bearing consumer. Its insertion path uses
  `IInvoiceNumberingService.NextNumberAsync` and the `Invoice`
  shape this bolt persists. If the service contract or the
  `Invoice` columns change in 039, this bolt's tests should still
  pass — coupling is via the interface, not via internals.
- **Intent 022 (coupons)** will subtract the discount from
  `subtotal + shipping` BEFORE the call to
  `VatCalculator.ExtractBreakdown`. The formula itself stays exactly
  as written here.
- **Future ADR candidates (Stage 3)**:
  - Shipping is VAT-inclusive at the same rate as goods.
  - `MidpointRounding.AwayFromZero` is the canonical rounding mode
    for any future legal/regulatory math in this codebase.
  - Postgres `SEQUENCE` per (series, year) is the chosen numbering
    primitive — alternative (counter table) considered and rejected
    for concurrency reasons, with the gap-on-rollback risk explicitly
    accepted.

### Acceptance criteria mapped to design

**Story 001 (VAT fields + computation)**
- ✅ EF migration adds three columns with documented defaults.
- ✅ `OrderService.CreateFromCartAsync` populates all three via
  `VatCalculator.ExtractBreakdown(gross, rate)`.
- ✅ Rate from `IOptions<VatSettings>.Value.Rate`, default `0.19m`.
- ✅ `(100.00, 0.19) → (84.03, 15.97)` — pinned by unit test.
- ✅ `OrderDetailDto` extension (additive).

**Story 002 (Invoice + numbering)**
- ✅ `Invoices` table per the documented schema.
- ✅ Postgres `SEQUENCE invoice_seq_ft_2026` created in migration.
- ✅ `IInvoiceNumberingService.NextNumberAsync("FT", 2026)` returns
  `FT-2026-00001`, `…00002`, … via `nextval()`.
- ✅ Atomic across concurrent transactions — `nextval()` is atomic.
- ✅ 2027 crossover starts a fresh sequence — service creates
  `invoice_seq_ft_2027` lazily on first call.
