---
stage: model
bolt: 038-vat-calculation
created: 2026-06-03T04:00:00Z
---

## Static Model: vat-calculation

> Bolt 038 introduces the legal-grade foundation that the rest of intent 016
> consumes: a VAT breakdown on every order plus a strictly-sequential
> invoice number per series-year. This is regulatory work — the formulas
> and the gap-free guarantee come from Romanian Fiscal Code, not from
> design preference. Everything else flows from that.

### Relevant prior decisions (decision-index scan)

| ADR | Why it matters here |
|---|---|
| **ADR-004** — State conflicts return 409 | Explicitly names "invoice-number collisions" as a 409 case. Any endpoint that surfaces an invoice and could be raced (admin re-issue, double-trigger of Paid) returns 409 if the number already exists. |
| **ADR-005** — Idempotency excludes shipping address | The OrderService is where VAT computation lands; the idempotency-key replay path must replay the same VAT breakdown without recomputing (otherwise a settings change between replay and original would diverge). Stage 2 will detail. |
| **ADR-016** — CAS via ExecuteUpdateAsync for Order.Status transitions | Bolt 039 will mutate `Invoice.AnafStatus`; the same CAS pattern applies. Out of scope for bolt 038, but the invoice schema must support it (status as enum-string column, no RowVersion). |

### Entities

#### `Order` (modified — no new entity)

Three new columns; no behavioural change beyond computation. Existing
domain rules (status machine, ownership, etc.) are untouched.

| Column | Type | Notes |
|---|---|---|
| `NetTotalRon` | `numeric(18,2) NOT NULL DEFAULT 0` | Sum of line totals + shipping, *excluding* VAT |
| `VatRon` | `numeric(18,2) NOT NULL DEFAULT 0` | The VAT amount extracted from the gross total |
| `VatRate` | `numeric(5,4) NOT NULL DEFAULT 0.19` | The rate at which this order was billed (snapshot, not a live reference to settings) |

**Invariants**:
- `TotalRon = NetTotalRon + VatRon` (within ±0.01 RON rounding tolerance)
- `VatRate` is a snapshot at order-creation time. Changing `Vat:Rate` in
  config later does NOT mutate existing orders. The legal trail must
  show the rate that was applied when the order was created.
- `VatRon = round(TotalRon * VatRate / (1 + VatRate), 2)` with
  `MidpointRounding.AwayFromZero` (Romanian convention — banker's
  rounding produces non-deterministic results when audited).
- For free orders (TotalRon = 0): `NetTotalRon = VatRon = 0`.
- Negative subtotals are upstream-rejected; the columns themselves
  carry no negativity constraint at the DB level (legal returns/credit
  notes are out of scope for this intent).

#### `Invoice` (new aggregate root)

| Column | Type | Notes |
|---|---|---|
| `Id` | `uuid PRIMARY KEY` | |
| `OrderId` | `uuid NOT NULL REFERENCES Orders(Id)` | One-to-one in practice; no enforcement at DB level so we can reissue if business requires |
| `InvoiceNumber` | `varchar(50) NOT NULL UNIQUE` | Format `FT-YYYY-NNNNN`; UNIQUE constraint is the last line of defence against numbering races |
| `Series` | `varchar(10) NOT NULL` | `FT` is the only series today; structure exists for `FP` (proforma), `FS` (storno) later |
| `IssuedAt` | `timestamptz NOT NULL` | The fiscal issue date — derived from `Order.PaidAt`, not from "now" |
| `NetTotalRon` | `numeric(18,2) NOT NULL` | Snapshot from `Order` at issue time |
| `VatRon` | `numeric(18,2) NOT NULL` | Snapshot |
| `TotalRon` | `numeric(18,2) NOT NULL` | Snapshot |
| `XmlPayload` | `text NULL` | UBL XML lands here in bolt 039 |
| `PdfStoragePath` | `varchar(500) NULL` | PDF storage key lands here in bolt 039 |
| `AnafUploadId` | `varchar(100) NULL` | Set by `InvoiceUploadJob` in bolt 039 |
| `AnafStatus` | `varchar(30) NOT NULL` | Enum-string: `Pending`, `Submitted`, `Accepted`, `Rejected`, `Failed` |
| `LastError` | `text NULL` | Set when ANAF submission rejects |
| `CreatedAt` | `timestamptz NOT NULL` | Record creation time (may differ from `IssuedAt` by milliseconds) |
| `UpdatedAt` | `timestamptz NULL` | Set on any mutation after creation |

**Invariants**:
- `InvoiceNumber` is **immutable once written**. Renumbering would
  break the legal audit trail.
- `Series + InvoiceNumber` is globally unique within the database (the
  unique index enforces this — last line of defence against any race
  the sequence might lose).
- `IssuedAt` is derived from `Order.PaidAt`. The legal "date the
  invoice was issued" is the moment the customer paid, not the moment
  we got around to creating the record.
- Snapshots (`NetTotalRon`, `VatRon`, `TotalRon`) are copied from
  `Order` at creation time. Later mutations to the order's totals
  (which shouldn't happen but might via admin override) do NOT
  propagate to the invoice. The invoice is a frozen legal artefact.
- `AnafStatus` defaults to `Pending`; the rest of the lifecycle is
  bolt 039's concern.

### Value objects

#### `InvoiceNumber`

```text
InvoiceNumber
├── Series : string  (e.g. "FT")
├── Year   : int     (e.g. 2026)
└── Number : int     (e.g. 1, padded to 5 digits → "00001")

ToString() → "{Series}-{Year}-{Number:D5}"  (e.g. "FT-2026-00001")
```

**Invariants**:
- `Series` is uppercase, ASCII letters only, 2–10 characters.
- `Year` is the fiscal year of `IssuedAt` (UTC). Crossing midnight on
  Jan 1 starts a new sequence.
- `Number` is strictly monotone increasing within `(Series, Year)`.
  Gaps are NOT permitted by Fiscal Code — see "Open invariant" below.
- The format is part of the legal audit trail; never change it
  without coordinating with the accountant. Customer-facing PDFs and
  ANAF submissions both use the literal string.

#### `VatBreakdown`

```text
VatBreakdown
├── NetTotalRon : decimal  (always ≥ 0, 2 dp)
├── VatRon      : decimal  (always ≥ 0, 2 dp)
├── TotalRon    : decimal  (always ≥ 0, 2 dp)
└── VatRate     : decimal  (currently fixed at 0.19, 4 dp)

Invariant: |NetTotalRon + VatRon - TotalRon| ≤ 0.01
```

The breakdown is derived from `(TotalRon, VatRate)` via the
VAT-inclusive formula (see Domain Service section). It's a pure
function of inputs — no DB, no I/O.

### Aggregates

#### `Order` (existing aggregate root, extended)

Bolt 038 extends `Order`'s set of value-typed fields with the VAT
breakdown. No new entities are pulled into the aggregate. The order
remains the unit of consistency it already is.

#### `Invoice` (new aggregate root)

Sized small: one `Invoice` per `Order` (or zero, if the order never
reached `Paid`). No child entities. The aggregate boundary is exactly
one row.

**Why not nest `Invoice` inside `Order`?**

- An invoice is a legal artefact with its own lifecycle (ANAF
  submission, status polling, retry). Coupling that lifecycle to the
  order's lifecycle would create awkward transactional scopes.
- The invoice persists long after the order is "done" — Romanian
  retention is 10 years. The order may be archived, but the invoice
  cannot be.
- Bolt 039 will introduce a `BackgroundService` that polls ANAF; that
  worker pulls invoices by status, not by order. Easier with a
  separate aggregate.

### Domain events

#### `InvoiceIssued`

**Trigger**: `Order.Status` transitions to `Paid`.
**Payload**: `Invoice.Id`, `InvoiceNumber`, `IssuedAt`.

This event is NOT raised in bolt 038 (the bolt only ships the schema
and the numbering service). Bolt 039 will be the consumer, calling
`IInvoiceNumberingService.NextNumberAsync` and the XML/PDF generation
on this trigger. The event is documented here so the model is
complete.

#### `InvoiceAnafStatusChanged`

Out of scope for bolt 038; sketched for completeness. Owned by bolt
039's submission/polling workers.

### Domain services

#### `IInvoiceNumberingService`

```text
NextNumberAsync(seriesCode: string, year: int, ct: CancellationToken)
    → string  // e.g. "FT-2026-00001"
```

**Responsibility**: allocate the next monotonically-increasing number
for `(series, year)`, atomically and gap-free.

**Implementation strategy** (Stage 2 will detail):
- **Postgres path**: `nextval('invoice_seq_{series}_{year}')`. The
  sequence is created idempotently per `(series, year)` pair via raw
  SQL in the migration; `CREATE IF NOT EXISTS` semantics so re-running
  is safe.
- **SQLite (dev) path**: `MAX(Number) + 1` inside a transaction on a
  serial-write database; safe because SQLite is single-writer.

**Gap-free invariant** — this is the load-bearing legal property:

> If `NextNumberAsync` returns `N`, then exactly one invoice with
> number `N` must persist. Numbers may not be skipped, and numbers may
> not be reused.

Romanian Fiscal Code forbids gaps in invoice numbering within a
fiscal year. A "gap" here means a number that was issued but does not
appear on any invoice — which would typically happen if a transaction
allocated a number then rolled back.

**Postgres sequences gap on rollback.** This is the load-bearing
edge case. Mitigation pattern (Stage 2 design): allocate the number
**inside the same transaction** that persists the invoice. If the
transaction commits, the number is bound to a real invoice. If the
transaction rolls back, both the number and the invoice attempt are
gone — the sequence still advances on the Postgres side, but no
invoice claims the skipped number, which appears as a gap in the audit.

The trick to avoid gaps under Postgres semantics: use a
**single-statement INSERT … SELECT nextval()** rather than two
statements (`SELECT nextval()` then `INSERT`). Postgres still bumps
the sequence on rollback, but the number-and-row are atomically
linked — there is no "allocated but unused" window.

Even with that, sequences gap on **transaction rollback** in
Postgres by design (nextval is non-transactional). For our use case
this happens only when the Paid transition itself rolls back, which
is extraordinarily rare (the transaction includes a single
SaveChanges, no external I/O). The post-mortem mitigation:
quarterly audit of invoice numbers vs sequence value, with
explicit explanation of any gaps documented in an accountant-facing
report. The legal exposure on a gap-of-one-from-a-known-incident is
low; the legal exposure on systematic gaps would be high.

**Concurrency**: `nextval()` is atomic on Postgres regardless of
transaction. Two concurrent `Paid` transitions never get the same
number, even without explicit locking.

#### Pure helper: `VatCalculator`

```text
ExtractBreakdown(grossTotalRon: decimal, rate: decimal) → VatBreakdown
```

Pure function. No DI, no state, no logger. The single place where the
VAT formula lives. Testable as a property — given any
`(grossTotalRon, rate)`, the result satisfies the breakdown's
invariant.

### Repository interfaces

No new repositories. `Invoice` will be accessed via the existing
`PhotoPrintDbContext`'s DbSet, following the project's
repository-light convention (intent 001).

### Ubiquitous language

| Term | Definition |
|---|---|
| **VAT** (TVA in Romanian) | Value-Added Tax. 19% in Romania for standard goods (which photo prints are). |
| **Gross total** (`TotalRon`) | The amount the customer pays. VAT-inclusive. |
| **Net total** (`NetTotalRon`) | The amount excluding VAT. Net = Gross − VatRon. |
| **VatRate** | The rate at which VAT was applied to this specific order. Snapshot; never a live reference. |
| **VAT-inclusive pricing** | Romanian convention: prices quoted to consumers include VAT. The VAT is extracted from the gross, not added on top. Critical for our formula. |
| **Invoice series** | A grouping for invoice numbering (`FT` = factură, `FP` = factură proformă, `FS` = factură storno). Today only `FT` is used. |
| **Sequence** (Postgres) | A database object that hands out monotone integers atomically. Gap-on-rollback is documented behaviour. |
| **Gap-free** | Romanian Fiscal Code requirement: invoice numbers within a fiscal year must form a contiguous integer sequence with no missing values. |
| **ANAF** | Agenția Națională de Administrare Fiscală — Romanian tax authority. The audience for e-Factura submissions. |
| **e-Factura** | Romania's electronic invoicing system. UBL 2.1 XML, mandatory submission to ANAF SPV within 5 business days of issue. |
| **CIUS-RO** | Core Invoice Usage Specification — Romania. The ANAF-mandated profile of UBL 2.1. |
| **Storno** | A correction invoice that nullifies a previously-issued one. Out of scope for this intent. |
| **Reverse charge** | B2B EU rule where VAT is reported by the buyer, not the seller. Out of scope. |

### Stories coverage check

- ✅ Story **001 (VAT fields + computation)** — `Order` extension, `VatBreakdown` value object, `VatCalculator` domain helper, VAT-inclusive formula, configuration source for rate, snapshot semantics.
- ✅ Story **002 (Invoice entity + numbering)** — `Invoice` aggregate root with full column set, `InvoiceNumber` value object with format invariants, `IInvoiceNumberingService` contract with gap-free promise, Postgres-vs-SQLite implementation split, rollback-gap mitigation.

### Open invariants (resolved before Stage 4)

1. **When is the invoice number allocated?** Per the story-002
   edge-case table, allocate "in a separate, idempotent step right
   before `Paid`". Stage 2 will resolve whether that's inside the
   webhook handler's transaction (preferred — keeps the gap-free
   property strongest) or in a follow-up worker. The single-statement
   INSERT-with-nextval pattern argues for inside the same transaction.

2. **Is the `Invoice` row created on Paid or on issue?** Same row;
   creation happens at Paid. Bolt 038 ships the schema; bolt 039
   ships the creation logic. Stage 2 will document the contract so
   bolt 039 implements against a stable shape.

3. **Shipping VAT treatment.** The Open Question Q1 in the intent's
   requirements asks whether shipping is VAT-inclusive at the same
   rate as goods. **Resolved here**: yes, shipping is treated as
   VAT-inclusive at the same rate as goods (the simpler and more
   common Romanian convention). Stage 3 (ADR Analysis) candidate:
   document this as an ADR so future "shipping is a service, taxed
   differently" arguments can be pre-empted.

### Forward references

- **Bolt 039 (e-Factura/ANAF)** consumes everything this bolt ships.
  The invoice schema must support: UBL XML body (`XmlPayload`), PDF
  storage path, ANAF tracking columns (`AnafUploadId`, `AnafStatus`,
  `LastError`). All present.
- **Intent 022 (coupons)** subtracts from `NetTotalRon` before VAT is
  computed. The VAT formula in this bolt is stable; the coupon bolt
  applies the discount to the gross input before
  `VatCalculator.ExtractBreakdown` is called.
- **Bolt 044 (observability, complete)** defined a meter for
  `invoice_anaf_status_total{status}`. The increments ship with
  bolt 039 — bolt 038 doesn't touch the ANAF status column.
