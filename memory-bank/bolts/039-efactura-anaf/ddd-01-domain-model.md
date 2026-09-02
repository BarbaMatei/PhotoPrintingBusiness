---
stage: model
bolt: 039-efactura-anaf
created: 2026-06-03T11:00:00Z
---

## Static Model: e-Factura Generation & ANAF Submission

> Bolt 039 takes everything bolt 038 staged (Invoice table, numbering
> service, VAT snapshot) and animates it: builds the legally-compliant
> UBL XML, renders a customer-facing PDF, submits to ANAF SPV, and
> exposes admin tools to inspect and retry. This is the regulated
> half of intent 016 — the customer never sees most of it, but the
> tax authority does, and a quiet outage here turns into a fiscal
> liability fast.

### Relevant prior decisions (decision-index scan)

| ADR | Why it matters here |
|---|---|
| **ADR-002** — `ValidationFilter` returns 422, FluentValidation required | The admin endpoints' DTOs (pagination params, retry path id) follow the project convention. No data annotations. |
| **ADR-003** — Trust client `X-Correlation-Id` | The `InvoiceUploadJob` and ANAF HTTP client emit correlation ids on every outbound log. Inherits the existing middleware. |
| **ADR-004** — State conflicts return 409 | `POST /api/admin/invoices/{id}/retry` returns 409 when `AnafStatus` is not in `{Rejected, Failed}` — explicit "wrong state" rather than 400 (which would imply a request-shape problem). |
| **ADR-005** — Idempotency excludes shipping address | Not directly applicable; mentioned because the `Paid → InvoiceCreated` transition is reachable through Stripe webhook replay and must remain idempotent. Bolt 035 already guarantees this; bolt 039 must preserve it (see Open invariant 1). |
| **ADR-007** — Storage adapter accepts caller-supplied keys | The PDF storage key is computed in `InvoiceStorageKeys` (application layer), not inside the renderer or the adapter. Format: `invoices/{yyyy}/{mm}/{invoiceNumber}.pdf`. |
| **ADR-008** — Two-tier storage with per-upload `StorageLocation` | Invoices are post-payment artefacts, so they go to the cloud tier whenever `Storage:Provider = "S3"`. They never live in the local tier (no privacy-minimisation reason to gate them — they're customer-receipts, not raw uploads). |
| **ADR-013** — In-process singleton token cache (Sameday pattern) | `AnafTokenProvider` follows the same shape: singleton, `SemaphoreSlim(1,1)` against thundering-herd, 60s pre-expiry safety. The ANAF OAuth token (separate from the PKCS#12 client cert) is cached identically. |
| **ADR-014** — 401 retry in `DelegatingHandler`, not in Polly | `AnafAuthHandler` owns "session expired → re-auth → retry once → fail". Polly stays in charge of transient 5xx retries on its own budget. Same separation as Sameday. |
| **ADR-015** — Accept duplicate vendor calls; rely on vendor idempotency + DB re-check | If `InvoiceUploadJob` runs on multiple replicas, two replicas may dispatch the same invoice. Mitigation: ANAF dedupes server-side via the `InvoiceNumber` itself (it's globally unique in our database), and the worker re-checks `AnafStatus == Pending AND AnafUploadId IS NULL` before writing the new `AnafUploadId`. The exact dual-half pattern from bolt 037's AWB. |
| **ADR-016** — CAS via `ExecuteUpdateAsync` for status transitions | `Invoice.AnafStatus` transitions use the same compare-and-swap shape. Status enum stays as a `varchar(30)`; no `RowVersion`. `affected == 0` means "another worker / admin moved it" — Info-level, not an error. |
| **ADR-019** — `MidpointRounding.AwayFromZero` for regulatory math | Inherited transparently. Bolt 039 never re-rounds — it copies `Invoice.NetTotalRon / VatRon / TotalRon` straight into the XML and PDF. If a future PR introduces a new rounding site here (e.g. per-line VAT recomputation in the XML), ADR-019 applies. |
| **ADR-020** — Postgres `SEQUENCE` for invoice numbering, accept gap-on-rollback | Bolt 039 is the first real *consumer* of `IInvoiceNumberingService`. The single-statement INSERT-with-`nextval()` pattern is the wire by which numbering and persistence are bound: bolt 039 owns the Paid-transition transactional scope that calls into the numbering service. |

### Entities

#### `Invoice` (existing aggregate root from bolt 038, lifecycle filled in)

Bolt 038 shipped the schema; bolt 039 ships the behaviour. No new
columns — the table already has every field this bolt needs:
`XmlPayload`, `PdfStoragePath`, `AnafUploadId`, `AnafStatus`,
`LastError`. The Invoice aggregate gains a lifecycle state machine
(detailed below) and three new co-actors: an XML body, a PDF body, and
an ANAF round-trip identifier.

The aggregate boundary stays at one row. The XML and PDF are byte
payloads stored on the row (XML inline, PDF via `IStorageService`) —
no child entities, no cascade rules.

**New invariants introduced in bolt 039**:

- `XmlPayload IS NOT NULL` once `AnafUploadId IS NOT NULL`. You
  cannot have submitted to ANAF without having built the XML.
- `PdfStoragePath IS NOT NULL` once the customer email is sent. Email
  delivery is the observable downstream of "PDF exists"; if the email
  goes out claiming an attached invoice, the storage path must point
  at a real object.
- `LastError IS NOT NULL` when `AnafStatus IN (Rejected, Failed)`. An
  error state without an error message is a logging bug, not a
  legitimate state.
- `AnafStatus` transitions follow the state machine below; no
  out-of-band edits permitted.

### Value objects

#### `Seller`

The seller's fiscal identity — required by every UBL invoice. Read
from the `Seller:` config section at startup; immutable for the
lifetime of the process. Bolt 039 does not allow per-request seller
overrides.

```text
Seller
├── Name              : string    (e.g. "FotoTipar SRL")
├── Cui               : string    (Romanian fiscal code, e.g. "RO12345678")
├── RegistrationNumber: string    (e.g. "J40/1234/2026")
├── Address
│   ├── Line1         : string
│   ├── City          : string
│   ├── PostalCode    : string
│   └── CountryCode   : string    (ISO 3166-1 alpha-2, e.g. "RO")
└── IbanRon           : string    (for the BG-19 PaymentMeans block)
```

**Invariants**:
- All fields except `IbanRon` are required; missing values → boot
  failure with the standard "configuration not valid" message.
- `Cui` matches `^RO\d{2,10}$` (the project's Romanian-CUI shape).
- `CountryCode` is `RO` in production; the format is enforced for
  forward-compat (the seller-as-config pattern would extend to
  multi-country shops if that ever happened).

#### `Buyer`

A view onto `Order` (existing entity); not a separately-persisted
object. Built at XML-build time to give the builder a tidy projection.

```text
Buyer
├── Name        : string         ("Persoană fizică" if guest, otherwise the customer's name)
├── Cui         : string?        (null for guests; populated for B2B with VAT id)
├── Email       : string
└── Address
    ├── Line1   : string
    ├── City    : string
    ├── PostalCode : string?
    └── CountryCode: string
```

**Invariants**:
- A non-null `Cui` matches the same Romanian-CUI shape as the seller.
  A buyer's `Cui` is optional (guests omit it); the XML omits
  `BT-48 (BuyerVATIdentifier)` accordingly.
- `Name` is non-empty. Guests get the literal `"Persoană fizică"`
  (this is the Romanian legal phrase for "natural person") per the
  story-001 edge-case table.

#### `InvoiceLine`

Per-product line in the invoice. One per `OrderItem`, plus exactly
one synthetic line for shipping when `ShippingCostRon > 0`.

```text
InvoiceLine
├── Description  : string         (the product or "Transport" for the shipping line)
├── Quantity     : int            (1 for shipping; product quantity otherwise)
├── UnitPriceRon : decimal        (VAT-inclusive)
├── LineTotalRon : decimal        (Quantity × UnitPriceRon)
├── VatCategory  : VatCategoryCode (default S — standard rate)
└── VatRate      : decimal        (snapshot from Invoice; same on every line in v1)
```

**Invariants**:
- `Quantity ≥ 1`; zero-quantity lines never appear (rejected
  upstream).
- `LineTotalRon = Quantity × UnitPriceRon` rounded to 2 dp using
  `MidpointRounding.AwayFromZero` per ADR-019.
- The sum of `LineTotalRon` across all lines equals
  `Invoice.TotalRon` (within ±0.01 RON aggregate rounding tolerance,
  matched by an XSD-emitted `BG-22` total).
- Shipping line, when present, uses `VatCategory = S` and the same
  rate as goods (per the resolved Open Question Q1 in bolt 038's
  domain model — shipping is taxed at the same rate as goods in the
  Romanian convention used here).

#### `VatCategoryCode`

UBL enum constrained to the subset we actually use:

```text
VatCategoryCode
├── S — Standard rate (19% — the only one used in v1)
├── Z — Zero-rated   (reserved for future export scenarios)
├── E — Exempt       (reserved)
└── AE — Reverse charge (reserved; out of scope per intent)
```

**Invariant**: only `S` ships in v1. Validators reject any other
value at the boundary; the enum exists to keep the model
forward-looking without committing to behaviour.

#### `InvoiceNumber` (from bolt 038 — referenced here)

Format `FT-YYYY-NNNNN`. Bolt 039 reads this value off `Invoice` and
emits it as UBL `BT-1` (the official `InvoiceNumber`). Bolt 039 does
not change the format or the allocation strategy — that's bolt 038's
contract.

#### `AnafUploadResult`

Returned by `IAnafSpvClient.UploadAsync`.

```text
AnafUploadResult
├── UploadId       : string       (ANAF's "index de încărcare" — opaque to us)
└── SubmittedAt    : DateTimeOffset
```

The `UploadId` is the only handle we keep on the ANAF side; status
polls use it.

#### `AnafStatusResult`

Returned by `IAnafSpvClient.GetStatusAsync`.

```text
AnafStatusResult
├── Status         : AnafExternalStatus  (Validated | Rejected | InProgress | Unknown)
├── ErrorMessage   : string?              (populated only on Rejected)
└── ProcessedAt    : DateTimeOffset?      (populated when ANAF emits one)
```

`AnafExternalStatus.Validated` maps to our `InvoiceAnafStatus.Accepted`.
The naming divergence is deliberate: "Validated" is ANAF's word, and
"Accepted" is our internal lifecycle word. Stage 2's mapping table
documents the wire-to-domain rule.

#### `InvoiceUploadAttempt`

Diagnostic value object emitted by `InvoiceUploadJob`. Not persisted
in v1 — lives only as a log event payload. Stage 2 will decide
whether to durable-persist it (current call: no; lift to a row only if
incidents surface).

```text
InvoiceUploadAttempt
├── InvoiceId      : Guid
├── InvoiceNumber  : string
├── AttemptNumber  : int           (1-based; resets on admin retry)
├── BackoffWaited  : TimeSpan
├── Outcome        : AttemptOutcome (Submitted | TransientFailure | RejectedByAnaf | GiveUp)
└── CorrelationId  : string
```

### Aggregates

#### `Invoice` (existing aggregate root)

Same root as bolt 038. Bolt 039 fills in lifecycle behaviour without
introducing new child entities. The aggregate is still "one row, one
transaction" — the XML lives inline, the PDF lives on `IStorageService`
(an external bytes-store, not an aggregate child).

**Why not bring `InvoiceLine` into the aggregate?**

- Lines are derived from `OrderItem` at XML/PDF build time. We don't
  persist them separately because the order already owns that data —
  duplicating it on a child table of `Invoice` would create two
  sources of truth for the same numbers and a window for them to
  diverge. The snapshot semantics (Invoice freezes Order's totals) is
  the only "lock" we need; the line-by-line composition is rebuilt
  from `OrderItem` whenever XML or PDF needs them.
- A future re-issue ("storno") would freeze lines onto the storno
  invoice itself, since the original order's items might have
  changed. That's an intent-022 concern, not bolt 039's.

### Domain events

#### `InvoiceIssued` (already declared in bolt 038)

**Trigger**: `Order.Status` transitions to `Paid` and the in-transaction
invoice insert commits.
**Payload**: `Invoice.Id`, `InvoiceNumber`, `IssuedAt`, `OrderId`.

Bolt 039 is the first **consumer**. The XML build, PDF render, and
ANAF upload are downstream reactions. In v1 the consumer is
`InvoiceUploadJob` polling for invoices in `AnafStatus = Pending`,
not a true event-bus subscriber. Stage 2 will resolve whether to keep
that polling shape or replace it with an in-process queue (the
trade-off is the same one ADR-010 settled for promotion: in-memory
`Channel<T>` vs. polling table).

#### `InvoiceXmlBuilt`

**Trigger**: `IInvoiceXmlBuilder.Build` returns successfully and the
bytes are written to `Invoice.XmlPayload`.
**Payload**: `Invoice.Id`, `InvoiceNumber`, `XmlSizeBytes`.

Log-only event in v1.

#### `InvoicePdfRendered`

**Trigger**: `IInvoicePdfRenderer.RenderAsync` completes and
`IStorageService.SaveAsync` succeeds.
**Payload**: `Invoice.Id`, `InvoiceNumber`, `PdfStoragePath`,
`PdfSizeBytes`.

Two downstream consumers:
1. The order-confirmation email pipeline checks for this event (in v1:
   a "did this just become available?" DB read) before attaching the
   PDF to a delayed email.
2. The customer-facing `GET /api/orders/{id}/invoice` becomes
   non-404.

#### `InvoiceSubmittedToAnaf`

**Trigger**: `IAnafSpvClient.UploadAsync` returns 200 and `AnafUploadId`
is written.
**Payload**: `Invoice.Id`, `InvoiceNumber`, `AnafUploadId`,
`SubmittedAt`.

#### `InvoiceAnafStatusChanged`

**Trigger**: Any successful CAS on `Invoice.AnafStatus`. Covers
both worker-driven transitions (poll succeeds, response classified)
and admin-driven transitions (manual retry resets to `Pending`).
**Payload**: `Invoice.Id`, `InvoiceNumber`, `OldStatus`, `NewStatus`,
`Reason` (`worker-poll | admin-retry | give-up-backoff-exhausted`).

#### `InvoiceUploadFailed`

**Trigger**: `InvoiceUploadJob` exhausts the backoff schedule
(`1h, 4h, 16h, 64h`) without an Accepted status, and the invoice
transitions to `Failed`.
**Payload**: `Invoice.Id`, `InvoiceNumber`, `LastError`, `AttemptCount`.

In v1: emit a log event at `Error` level and increment the
`invoice_anaf_status_total{status="failed"}` Prometheus meter that
bolt 044 already defined. Admin alerting is downstream of the
existing observability stack — bolt 045 already has the Sentry hook.

### Domain services

#### `IInvoiceXmlBuilder`

```text
Build(order: Order, invoice: Invoice, seller: Seller) → byte[]
```

**Responsibility**: produce a UBL 2.1 + CIUS-RO compliant XML payload
for one invoice, as a UTF-8 byte array (so the same bytes can be both
written to `Invoice.XmlPayload` and POSTed to ANAF without
re-serialisation drift).

**Pure** — no DB, no I/O, no logger. Input is the full `Order`
projection (so the builder can walk `Items`), the existing `Invoice`
row (for the official number and snapshot totals), and the seller
config (for the BG-13 seller block). Stage 2 documents the
projection shape.

**Output contract**:
- Document validates against `UBL-Invoice-2.1.xsd` + the bundled
  CIUS-RO patch. The test suite asserts this on every emitted
  document.
- Required UBL Business Terms (BT/BG) present per story 001:
  `BT-1, BT-2, BT-3, BT-22, BT-31/32, BT-44+, BG-25, BG-22`.
- Per-line `VatCategory = S` in v1; reduced/exempt slots reserved
  but never selected by code paths in v1.
- All currency amounts emitted with `currencyID="RON"` and two
  decimal places.
- All `cbc:IssueDate` values formatted as `yyyy-MM-dd` (no time
  component — UBL forbids it on `BT-2`).

#### `IInvoicePdfRenderer`

```text
RenderAsync(order: Order, invoice: Invoice, seller: Seller, ct: CancellationToken)
    → byte[]
```

**Responsibility**: produce a customer-friendly A4 PDF receipt
matching the data shipped to ANAF. Source of bytes is a Razor
template (`Templates/Invoices/Invoice.cshtml`) plus a PDF engine —
either QuestPDF or PuppeteerSharp. Stage 2 picks one (Stage 3 may
elevate the choice to an ADR).

**Output contract**:
- A4, single document (multi-page allowed; the renderer doesn't try
  to fit-to-one).
- Contains all fields enumerated in story 003: invoice number,
  issue date, seller and buyer parties, line items with
  quantity / unit price / line VAT, totals
  (`NetTotalRon`, `VatRon`, `TotalRon`), payment processor name,
  AWB number when present, fiscal note ("Document generat
  electronic, valid fără semnătură").
- No customer signature line. No "manual stamp" placeholder.
- Locale: `ro-RO` for date and number formatting (`1.234,56` not
  `1,234.56`).

**Why a Razor template?** The team already uses Razor for email
templates; keeping the invoice template alongside lowers the
learning surface. The Razor → HTML step is intermediate; QuestPDF
(if chosen) consumes the HTML; PuppeteerSharp (if chosen) renders
it through Chromium. Stage 2 will commit to one.

#### `IAnafTokenProvider`

```text
GetAccessTokenAsync(ct: CancellationToken) → string
InvalidateToken()                                    → void
```

**Responsibility**: own the OAuth 2 access token to the ANAF SPV
endpoint. In-process singleton, `SemaphoreSlim(1,1)`-gated against
thundering herd, 60s pre-expiry safety window. Same shape as
`SamedayTokenProvider` (ADR-013); the rationale is identical.

**Cert handling**: a PKCS#12 client cert (`AnafCertPath` env var) is
loaded once at first use into an `X509Certificate2` and attached to
the OAuth POST. Cert is held in memory; reload-on-SIGHUP is out of
scope (a process restart is the recover-path for cert rotation).
Boot-time validation that the cert file exists and is loadable is
done by the config validator — fail fast, do not boot if the cert
isn't on disk.

#### `IAnafSpvClient`

```text
UploadAsync(invoiceXmlBytes: byte[], ct: CancellationToken)
    → AnafUploadResult                               // 200 OK contract

GetStatusAsync(uploadId: string, ct: CancellationToken)
    → AnafStatusResult                               // poll contract
```

**Responsibility**: HTTP transport to ANAF SPV. Uses `IHttpClientFactory`
with a named client wired through:
1. `AnafAuthHandler` (DelegatingHandler) — owns the 401-retry path
   per ADR-014 pattern. Calls `IAnafTokenProvider.InvalidateToken()`
   on first 401, re-attempts once, raises `AnafAuthException` on a
   second 401.
2. Polly transient-failure pipeline — retries 5xx, 408, 429 only.
   401 stays out of Polly's retry set.

Logging: every request logs at `Information` with the correlation
id; the body is **never** logged. The XML payload is potentially
multi-kB and contains buyer name/address — emitting it to Serilog
sinks would violate ADR-006-adjacent secret/PII hygiene. Stage 2
will pin a `Sentry`-scope-scrubber rule for any inadvertent leak.

#### `IInvoiceLifecycle`

```text
TryTransitionAsync(
    invoiceId : Guid,
    expected  : InvoiceAnafStatus,
    target    : InvoiceAnafStatus,
    mutator   : Action<Invoice>,  // sets AnafUploadId / LastError etc.
    ct        : CancellationToken)
    → bool                                           // false if CAS lost
```

**Responsibility**: thin façade around `ExecuteUpdateAsync` per
ADR-016. Every status mutation in bolt 039 (worker poll outcome,
admin retry, give-up after backoff exhaustion) goes through this
service. The `mutator` arg lets the caller bundle related field
writes (e.g. `Submitted → Pending` admin retry also clears
`LastError`).

Returns `false` when the CAS predicate misses (another worker won
the race, or the row was admin-edited in the meantime). Caller logs
at `Information` and exits — not an error.

#### `InvoiceUploadJob : BackgroundService`

**Responsibility**: pull pending and submitted invoices and advance
them through the ANAF lifecycle. One tick = one batch.

**Tick behaviour** (Stage 2 details):
1. Find invoices with `AnafStatus IN (Pending, Submitted)` whose
   next-retry time is past.
2. For each `Pending`: build XML (skip if `XmlPayload` already
   populated), upload, CAS to `Submitted` (or to `Failed` /
   reschedule on transient).
3. For each `Submitted`: poll status, CAS to `Accepted` / `Rejected`
   per response. On `Rejected`, schedule the next retry per backoff.
4. On `Rejected` with retry budget exhausted: CAS to `Failed`.

**Cadence**: default 30 minutes per `Anaf:PollIntervalMinutes`
config. Configurable down to 1 minute for staging.

**Retry schedule**: `1h → 4h → 16h → 64h → give up`. After the 4th
rejection the invoice transitions to `Failed` and waits for admin
intervention. The schedule is hardcoded; if configurability is
needed it'll be deferred to a follow-up bolt.

**Multi-replica safety**: per ADR-015's acceptance posture, two
replicas may dispatch the same invoice in the same tick. ANAF
dedupes on `InvoiceNumber` server-side, and the CAS write is the
last line of defence: only one replica's `Submitted` CAS will
succeed; the loser logs and exits.

### Repository interfaces

No new repositories. `Invoice` is accessed via the existing
`PhotoPrintDbContext.DbSet<Invoice>`, following the project's
repository-light convention.

### Lifecycle state machine

The Invoice's `AnafStatus` is the load-bearing state column. Every
transition is CAS-protected per ADR-016.

```text
                        ┌──────────────────┐
                        │   (row insert)   │
                        └────────┬─────────┘
                                 │
                                 ▼
   ┌──────────┐ admin manual retry  ┌──────────┐
   │  Pending │◀─────────────────────│ Rejected │
   └────┬─────┘                     └──────────┘
        │                              ▲
        │ upload OK                    │ poll: Rejected (with retry budget left)
        ▼                              │
   ┌──────────┐ poll: InProgress       │
   │ Submitted│────────(self loop)─────┤
   └────┬─────┘                        │
        │                              │
        │ poll: Validated              │
        ▼                              │
   ┌──────────┐                        │
   │ Accepted │  ← terminal happy path │
   └──────────┘                        │
                                       │
                            ┌──────────┴───────────┐
                            │ retry budget exhaust │
                            ▼                       
                       ┌──────────┐                  
                       │  Failed  │ ← terminal, requires admin
                       └────┬─────┘
                            │
                            │ admin manual retry
                            ▼
                       (back to Pending)
```

**Allowed transitions** (the CAS predicate enforces each):

| From | To | Trigger |
|---|---|---|
| (new) | `Pending` | Invoice row insert at Paid transition |
| `Pending` | `Submitted` | Upload returned 200 + `AnafUploadId` written |
| `Pending` | `Pending` | Upload threw transient — only `UpdatedAt` and `LastError` mutate; status unchanged (the row is still pending) |
| `Submitted` | `Accepted` | Poll returned `Validated` |
| `Submitted` | `Rejected` | Poll returned `Rejected`; `LastError` populated |
| `Submitted` | `Submitted` | Poll returned `InProgress`; only `UpdatedAt` mutates |
| `Rejected` | `Pending` | Admin retry (clears `LastError`, increments attempt count) |
| `Rejected` | `Failed` | Backoff budget exhausted |
| `Failed` | `Pending` | Admin retry |
| `Accepted` | — | terminal |

**Forbidden transitions** (CAS naturally rejects):
- `Accepted → anything`. Once ANAF said yes, the invoice is locked.
- `Failed → Submitted` directly. Admin must reset to `Pending`, which
  forces a re-upload (and a fresh `AnafUploadId`).
- Skip transitions (`Pending → Accepted` without `Submitted`). Cannot
  happen via the state machine; would only be reachable by direct DB
  edit, which is out of scope.

### Ubiquitous language

| Term | Definition |
|---|---|
| **UBL 2.1** | Universal Business Language, OASIS standard XML for invoices. The format ANAF accepts. |
| **CIUS-RO** | Core Invoice Usage Specification — Romania. ANAF's profile of UBL 2.1 — constrains which UBL fields are mandatory and adds Romania-specific business rules. |
| **BT-N / BG-N** | UBL Business Term / Business Group identifiers. `BT-1` is the invoice number; `BG-22` is the document totals group. Used unambiguously in spec and code comments. |
| **ANAF SPV** | Spațiu Privat Virtual — the private portal each fiscal entity has at ANAF. The endpoint we POST invoices to. |
| **Upload ID** (`index de încărcare`) | The opaque handle ANAF returns on a successful upload; the only identifier we keep to poll for status. |
| **Validated** / **Accepted** | ANAF's word / our word for "this invoice is now legally registered". |
| **Rejected** | ANAF's word for "this invoice has a content error — fix and resubmit". Mapped to our same word. |
| **AnafStatus** | The lifecycle column on `Invoice`. Our internal enum; the wire status from ANAF is a separate value mapped into it. |
| **Idempotency on the vendor side** | The technique (ADR-015) of letting the vendor's dedupe key (here: the invoice number itself) absorb our duplicate calls. |
| **PKCS#12 / .p12** | The cert format ANAF requires for the OAuth pre-issue step. Loaded once at startup into `X509Certificate2`. |
| **Dual-write rollout** | The bolt-level posture for prod cutover: generate XML + upload to ANAF for a week without sending PDFs to customers. Inspectable, reversible. |

### Stories coverage check

- ✅ Story **001 (UBL XML builder)** — `IInvoiceXmlBuilder`, `Seller` value object, `Buyer` projection, `InvoiceLine` value object, `VatCategoryCode` enum, UBL business-term coverage list, edge cases (guest buyer, zero items).
- ✅ Story **002 (ANAF SPV client + worker)** — `IAnafTokenProvider`, `IAnafSpvClient`, `InvoiceUploadJob`, `AnafUploadResult`, `AnafStatusResult`, retry schedule (1h/4h/16h/64h), state machine, multi-replica acceptance per ADR-015, cert handling at boot.
- ✅ Story **003 (PDF renderer + endpoint)** — `IInvoicePdfRenderer`, Razor + (QuestPDF or PuppeteerSharp), storage key policy (ADR-007 conformance), customer endpoint contract, email-attachment trigger.
- ✅ Story **004 (Admin list + retry)** — `IInvoiceLifecycle.TryTransitionAsync` covers admin retry; admin endpoints (list/retry/xml) are application-layer; pagination shape inherits from existing admin helpers; 409 on wrong-state retry per ADR-004.

### Open invariants (resolved before Stage 4)

1. **Invoice row creation: at the Paid webhook or in a follow-up worker?**
   The strongest gap-free posture (ADR-020) keeps the
   `INSERT … SELECT nextval()` inside the Stripe webhook's existing
   transaction. The competing posture lifts invoice creation to a
   background worker so the webhook stays fast. Tentative
   resolution: **inside the webhook transaction**, because the
   webhook is already idempotent (bolt 035) and adding one INSERT
   doesn't materially extend its latency. Stage 2 will commit.

2. **PDF rendering library: QuestPDF vs PuppeteerSharp.** QuestPDF is
   pure-managed, no Chromium dependency, faster startup, but its
   layout DSL diverges from HTML+CSS so the existing email-template
   skills don't transfer. PuppeteerSharp uses Chromium, faithful to
   the Razor → HTML model, but adds a ~200MB headless browser to the
   container image. Tentative recommendation: **QuestPDF**, on the
   strength of operational simplicity (no Chromium in the prod image,
   no per-host browser cache). Stage 2 commits; Stage 3 may elevate
   this to an ADR.

3. **Dual-write rollout posture.** The bolt's overview names a
   week-long dual-write phase: generate XML, upload to ANAF, but
   don't email the PDF to customers yet. Implementation question:
   is this a feature flag (`Invoicing:CustomerEmailAttachments:Enabled`)
   or a process-level dark-launch? Tentative resolution: **feature
   flag** in `appsettings.json`, defaults to `false` so a hot deploy
   doesn't accidentally start emailing. Stage 2 details.

4. **Polling vs in-process channel for `InvoiceUploadJob`.** ADR-010
   accepted in-process `Channel<T>` over a polling table for the
   promotion worker, with crash recovery via a startup scan.
   Bolt 039 starts simpler: poll the `Invoices` table every 30
   minutes for rows in `Pending` / `Submitted`. The pendulum may
   swing later if ANAF latency makes 30-min polling feel sluggish;
   v1 stays with polling because the cadence is right-sized for a
   "submit within 5 business days" SLA. Stage 2 commits.

5. **Email pipeline coordination.** The order-confirmation email
   (existing) fires immediately on Paid; the PDF may not yet exist.
   Story 003 specifies: if PDF exists at email-send time, attach;
   otherwise send the email with a "Invoice will follow" line and
   trigger a follow-up email when the PDF lands. Stage 2 details
   the trigger mechanism — currently leaning towards a small
   `InvoicePdfReadyNotifier` invoked from the renderer that
   re-sends the attachment-only email.

### Forward references

- **Intent 022 (coupons)** subtracts from the gross before VAT is
  computed (bolt 038's contract); bolt 039 reads the snapshot off
  `Invoice` so it's unaffected by the discount-on-net pattern. The
  XML's BG-22 totals always match the snapshot; the customer's
  PDF shows pre-discount line totals minus a single "Discount" line,
  mirroring the order summary they confirmed at checkout.
- **Bolt 044 (observability, complete)** already defines
  `invoice_anaf_status_total{status}` — bolt 039 adds the
  increment calls at every state transition.
- **Bolt 045 (Sentry, complete)** already defines the Sentry scope
  enricher; the ANAF HTTP client adds a `vendor=anaf` tag to outbound
  spans so dashboards can isolate ANAF latency from Stripe / Sameday.
- **A future "storno" intent** (refund/credit-note invoices) will
  reuse `IInvoiceNumberingService` with `Series = "FS"` — no
  rework required, the design supports it.
