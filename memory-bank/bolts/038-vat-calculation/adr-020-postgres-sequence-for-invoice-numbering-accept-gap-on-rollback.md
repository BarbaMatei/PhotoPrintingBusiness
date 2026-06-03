---
bolt: 038-vat-calculation
created: 2026-06-03T05:00:00Z
status: accepted
---

# ADR-020: Postgres `SEQUENCE` for Invoice Numbering — Accept Gap-on-Rollback

## Context

Romanian Fiscal Code requires invoice numbers within a fiscal year to
be strictly sequential per series — **gap-free**. Bolt 038 needs a
primitive that allocates monotonically-increasing integers atomically
across concurrent transactions and survives multi-replica deployments.

Two Postgres primitives meet the atomicity requirement:

**A. `SEQUENCE` + `nextval()`** — built-in atomic counter. Concurrency-
friendly (no lock contention), fast, idiomatic. Documented trade-off:
**gaps on transaction rollback** — `nextval()` is non-transactional by
design; the sequence advances even if the transaction that called it
rolls back.

**B. Counter table** — a `Counters(Series, Year, Number)` row, fetched
with `FOR UPDATE`, incremented, written back, then INSERT'd alongside
the invoice — all in one transaction. **Strictly gap-free** (rollback
unwinds the counter increment too). Cost: row-level lock contention
on a hot row.

The Fiscal Code language is "no gaps." The implementation question is
whether to interpret that as an absolute database-level guarantee
(counter table) or as a procedural commitment with audit mitigation
(sequence + operations playbook). Both are defensible. The trade-off
is between concurrency headroom and audit-time simplicity.

## Decision

**Invoice numbering uses Postgres `SEQUENCE` per `(series, year)`
partition. Gaps on transaction rollback are explicitly accepted with
documented operational mitigation. The counter-table alternative was
considered and rejected.**

Restated as invariants:

- One sequence per `(series, year)` pair (e.g.
  `invoice_seq_ft_2026`). Crossing into 2027 creates a fresh sequence
  starting at 1 — preserves the year-by-year audit trail
  the Fiscal Code expects.
- The sequence is created idempotently via
  `CREATE SEQUENCE IF NOT EXISTS` on every `NextNumberAsync` call.
  Cheap, safe under concurrency.
- The `nextval()` call MUST happen inside the same transaction that
  persists the `Invoice` row — minimises the rollback-gap window.
  No external I/O (HTTP calls to ANAF, PDF generation, email)
  inside that transaction.
- A composite unique index `(Series, year, Number)` on `Invoices`
  is the last line of defence against any race the sequence might
  lose — operator error during a database restore is the
  primary scenario where this kicks in.
- Operations runs a **quarterly audit**:
  `SELECT series, max(number) - count(*) AS missing FROM invoices
  WHERE date_part('year', issued_at) = $year GROUP BY series`.
  Any `missing > 0` is documented in the accountant-facing report
  with the corresponding incident from the application logs.

## Rationale

For our scale and call path, `SEQUENCE` is the right primitive:

1. **The rollback case is extraordinarily rare in our path.** The
   Paid-transition transaction contains a single `SaveChanges` and no
   external I/O. The Stripe and EuPlatesc webhook handlers commit
   before any post-payment side effect (email send, AWB enqueue,
   photo-promote enqueue) fires. The only routes to a rollback
   are operational incidents (DB connection drop mid-commit, host
   crash) — which warrant their own incident notes regardless.

2. **`nextval()` is atomic across all concurrent transactions.** Two
   payment webhooks landing simultaneously for two different orders
   never get the same number, without any explicit locking. The
   counter-table approach forces every concurrent Paid transition
   to serialise on the row-level lock — manageable today, awkward
   at any scale.

3. **The legal exposure on isolated gaps is low.** The typical
   regulator response to an isolated, documented gap with an
   attached incident note is correction. The exposure on
   **systematic** gaps (a bug producing many gaps per day) would be
   high — but the SEQUENCE primitive doesn't produce systematic
   gaps; it produces "one gap per very rare incident."

4. **The mitigation is cheap.** The quarterly audit query is
   trivial, runs in seconds, and the accountant attaches any
   findings to that quarter's filing.

5. **Composes with future Redis introduction (bolt 046,
   deprioritised).** If bolt 046 ever happens, Redis can supplement
   SEQUENCE for cross-replica coordination of edge cases (e.g.
   pre-allocate a batch of numbers for offline use), without
   replacing it. The counter-table choice would lock us into a
   pessimistic-lock model that scales badly under Redis-coordinated
   workloads.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| **Postgres `SEQUENCE` per (series, year)** (chosen) | Atomic, concurrent, idiomatic, no lock contention; idempotent creation via `IF NOT EXISTS`; natural year crossover | Gaps on transaction rollback; requires operational mitigation | — |
| **Counter table with `FOR UPDATE`** | Strictly gap-free (rollback unwinds increment); single primitive across providers | Row-level lock on a hot row → serialises concurrent Paid transitions; extra DB round-trip; more complex SQL; awkward under future multi-replica scale-out | Trades concurrency headroom for an audit guarantee we can replicate procedurally |
| **Single global sequence (not per-year)** | One fewer object to manage | First invoice of 2027 would be `FT-2027-456291` (whatever the global counter is at year-end), not `FT-2027-00001` — wrong legal trail; would require a complete numbering reset on Jan 1 anyway | — |
| **Application-side counter in Redis** | Atomic, fast, multi-replica-safe | Adds Redis as a tier-1 dependency for the Paid path; persistence story for Redis itself (RDB / AOF) becomes load-bearing for legal correctness; bolt 046 explicitly deprioritised | Wrong dependency level for legal correctness |
| **External numbering service (gRPC)** | Decoupled, audit-friendly | Operationally over-engineered for our scale; another service to monitor / version / fail | — |
| **Don't allocate on Paid — allocate lazily on first invoice access** | Defers the allocation question | Multiple consumers (PDF generator, ANAF submitter, customer-facing endpoint) would race; no clear "the invoice is issued at this moment" | Fundamentally wrong semantics for legal issue date |

## Consequences

### Positive

- **No lock contention on the Paid path.** Two concurrent Paid
  transitions never wait for each other on numbering.
- **No additional schema cost.** Sequences are managed Postgres
  objects; no extra table, no extra row, no extra index beyond the
  existing `(Series, year, Number)` unique constraint on `Invoices`.
- **Idempotent sequence creation** keeps the operational story
  simple — no migration ever needs to "add the 2027 sequence."
- **Year crossover is automatic.** First call after midnight on
  Jan 1 creates the new year's sequence; no human intervention.

### Negative

- **Rare gaps require explanation.** Operations runs the quarterly
  audit and writes an accountant note for any gap. This is
  procedural work, not automated.
- **Postgres-coupled.** SQLite (dev) uses a separate `MAX + 1`
  implementation. Two code paths to test (mitigated: both implement
  `IInvoiceNumberingService` and share the same call-site contract).
- **The unique index is "defence in depth" not "primary mechanism."**
  A reviewer might wonder why we have both `SEQUENCE` and a unique
  constraint; the answer is the unique constraint covers the
  database-restore-error case the sequence can't.

### Risks

- **Risk: a future PR wraps the `nextval()` call in a transaction
  that ALSO performs external I/O** (HTTP call to ANAF, email send,
  etc.). A subsequent failure rolls back the transaction and gaps
  the sequence. Mitigation: convention that the
  `IInvoiceNumberingService` call shares a transaction only with
  the `Invoice` INSERT and any other DB-only writes. PR review on
  any new logic in the Paid path verifies this.

- **Risk: a future PR replaces SEQUENCE with the counter-table
  approach "to harden against gaps."** This ADR is the answer to
  that PR. The rejected-alternative table above is the answer to
  "looks like a free upgrade." The PR author must engage with the
  trade-off (concurrency loss + lock contention) before changing
  the primitive.

- **Risk: systematic gaps from an undetected bug.** Detected by the
  quarterly audit's `missing > 0` finding. Alerting could be added
  if false-positive cost is acceptable (currently low priority —
  bolt 044 has the metrics infrastructure; an alert could be wired
  later).

- **Risk: regulator changes interpretation.** If a future
  enforcement action treats any gap as a violation regardless of
  documentation, this ADR needs revisiting. The counter-table
  alternative is then the right answer; the migration would be a
  one-shot data move plus a code swap.

- **Risk: SQLite dev path drifts from Postgres.** Two
  implementations of `IInvoiceNumberingService` means two test
  paths. Mitigation: shared contract tests that both implementations
  satisfy (Stage 5 will detail).

## Related

- **Stories**: 038-002-invoice-entity-and-numbering (immediate
  consumer); bolt 039's UBL/PDF/ANAF path consumes the assigned
  numbers and treats them as opaque immutable strings.
- **Previous ADRs**: ADR-016 (CAS via `ExecuteUpdateAsync`) — sibling
  pattern: both use database-native primitives for concurrency
  rather than introducing app-level locks; ADR-004 (409 for state
  conflicts) — explicitly names "invoice-number collisions" as the
  paradigmatic 409 case, which would only occur via the unique-index
  backstop.
- **Future ADRs**: bolt 046 (Redis, deprioritised) does not change
  this ADR — Redis-backed numbering would be a meaningful
  alternative for high-throughput scenarios, but our scale doesn't
  warrant the dependency. The two could coexist; SEQUENCE for the
  authoritative count, Redis for batched pre-allocations.
- **External**: [Postgres documentation — `CREATE SEQUENCE`
  non-transactional behaviour](https://www.postgresql.org/docs/current/sql-createsequence.html);
  [Romanian Fiscal Code — invoice numbering
  requirements](https://www.anaf.ro).
- **Read when**: working on `IInvoiceNumberingService` or `Invoice`
  insertion; reviewing PRs that touch the Paid transition's
  transactional scope; designing the bolt-039 worker that creates
  invoices; tempted to "harden" the numbering by switching to a
  counter table (don't, without re-engaging with this ADR's
  trade-off); debugging "why is there a gap between
  `FT-2026-00042` and `FT-2026-00044`?"; auditing invoices for a
  fiscal period; planning Redis-backed alternatives at scale.
