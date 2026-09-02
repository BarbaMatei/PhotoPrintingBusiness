---
bolt: 052-archive-retention
created: 2026-05-29T12:30:00Z
status: accepted
superseded_by: null
---

# ADR-012: Retention Anchor = `Order.PaidAt`

## Context

Bolt 052 introduces a periodic retention job that deletes an upload's large preview +
thumbnail from cloud storage once a configurable window (default 12 months) has elapsed.
The job's central question is: *measured from when?*

The intent's requirements document specifies "from order completion" but the existing
schema has no single field that uniformly captures that concept. The `Order` aggregate
carries:

- `Order.CreatedAt` — cart-created timestamp (too early).
- `Order.PaidAt` — set in the same code path that triggers cloud promotion (bolt 051).
- `Order.UpdatedAt` — touched by *every* status transition and by note edits.
- No `ShippedAt`, no `DeliveredAt`, no `CompletedAt`.

The retention query has to be cheap, correct, and unambiguous: a single column with a
single semantic, indexable, and meaningful for **every** order whose photos reached the
cloud — including the orders that went `Paid → Cancelled` (which still have promoted
photos that need cleaning up after the retention window).

## Decision

**Use `Order.PaidAt` as the retention anchor.** The retention query is:

```csharp
db.Uploads
  .Where(u => u.StorageLocation == StorageLocation.Cloud)
  .Where(u => u.LargePreviewPath != null || u.ThumbnailPath != null)
  .Where(u => db.OrderItems
      .Where(oi => oi.UploadId == u.Id)
      .Any(oi => oi.Order.PaidAt != null
              && oi.Order.PaidAt < UtcNow - RetentionWindow))
```

No new columns added by bolt 052. The columns bolt 051 already shipped
(`LargePreviewPath`, `OriginalPurgedAt`) carry the post-purge state. A separate
nullability change to `Upload.FilePath` is also part of bolt 052 (so the purge
can null it), but that's unrelated to the *anchor* decision recorded here —
see `ddd-02-technical-design.md §Implementation Correction`.

The customer-facing retention statement becomes: **"We keep your photos for N months
after you paid us."** Default `N = 12`, configurable.

## Rationale

### Why `PaidAt` and not the alternatives

| Alternative | Why rejected |
|-------------|--------------|
| **A new `Order.CompletedAt` column** set when the status transitions to Shipped / Delivered | Cleanest semantic ("12 months from when you got your photos"), but: **null on `Paid → Cancelled` orders**. Cancelled orders had photos promoted; those photos still need retention cleanup. A null anchor either forces a `COALESCE(CompletedAt, PaidAt)` everywhere — which is just "PaidAt as fallback" with extra steps — or requires a special-case branch in the retention query, both code paths, and both tests. The complexity isn't paying for itself. |
| **`Order.UpdatedAt` filtered by current status** | `UpdatedAt` is touched on every status transition *and* on note edits via `AdminOrderService.UpdateNotesAsync`. An admin adding a typo correction to a 6-month-old shipped order resets the retention clock. Behaviorally wrong; would also need re-deriving the "right" timestamp from status history. |
| **The `OrderStatusHistory` table** | Doesn't exist. Building one to anchor retention is the wrong scale of solution. |
| **Approximate via `Order.PaidAt + estimated fulfilment days`** | Adds a config knob without solving any real problem. Just use `PaidAt` directly. |

### Why `PaidAt` is the right answer

- **Always set** on every order whose photos reached the cloud (bolt 051's promoter only
  runs on `Status == Paid`, which is set in the same transition as `PaidAt`).
- **Never re-set** by any code path — `OrderStatusMachine.Transition` only writes it when
  going `AwaitingPayment → Paid`, and no other code path mutates it.
- **Indexable** — already used in admin queries; suitable as a retention-query predicate.
- **Single semantic** — "the moment we got the customer's payment." Honest, defensible,
  uniform across order types (regular, cancelled-after-payment, refunded).
- **No migration.** Zero schema changes this bolt.

### The accepted trade-off

Anchoring on `PaidAt` rather than a notional `DeliveredAt` means:

- **A 1-week fulfilment** → customer has ~51 weeks of archive access. Indistinguishable
  from a delivery anchor.
- **A 4-week fulfilment** (typical for premium photo printing) → customer has ~48 weeks.
  Still very close.
- **A 3-month fulfilment** (worst-case for backordered products) → customer has ~9 months.
  Real but acceptable loss.
- **An indefinitely-stuck fulfilment** → customer still gets up to 12 months from payment.
  *This is actually a feature*: no order can hold cloud storage forever just because it
  never reaches a "completion" status.

The fix for slow fulfilment is operational (SLA, courier integration), not architectural.

## Consequences

### Positive

- **Minimal schema change.** No `CompletedAt` column. The only bolt-052 migration is a
  one-line nullability flip on `Upload.FilePath` (orthogonal to this decision; see the
  design doc) — no Postgres-vs-PostgreSQL type quirks, no backfill.
- **One query covers every order shape.** No special-cases for Cancelled, no `COALESCE`,
  no status-history lookups.
- **Stable**: `PaidAt` is write-once. The retention clock never resets.
- **Aligns with privacy posture**: "we kept your data N months from receipt" is the
  cleanest GDPR-style statement we can make.
- **Cancelled orders behave correctly**: their promoted photos get retention-cleaned 12
  months after `PaidAt`, no special-case code.

### Negative

- **Slow fulfilment shortens customer-visible archive lifetime.** A 3-month fulfilment
  becomes 9 months of archive, not 12. Mitigated by:
  - operational fulfilment SLAs,
  - making `RetentionMonths` configurable upward if a business decision calls for it.
- **The phrase "12 months from order completion"** in the requirements document is
  technically not literally true. The customer-facing statement needs to be "12 months
  from payment" — a documentation/UX adjustment, not a code one.

### Risks

- **Risk**: a future requirements change wants "12 months from delivery" explicitly,
  with reliable delivery timestamps. **Mitigation**: at that point we'd add a
  `DeliveredAt` column populated by the courier IPN, plus a `COALESCE(DeliveredAt,
  PaidAt + 90d, PaidAt)` retention expression. The ADR would be superseded; the
  retention-job code would change in one place.
- **Risk**: someone reads the retention job and assumes the anchor is configurable;
  changes the predicate to use a different column without realising the cancelled-order
  case. **Mitigation**: this ADR; a comment on the retention query in
  `ArchiveRetentionJob.SweepAsync` pointing here.

## Related

- **Stories**: 002-retention-cleanup-job (intent 024 / unit 002). Story 001 is unaffected
  — it uses the status transition, not a time anchor.
- **Standards**: complements ADR-011 (Per-Upload Atomicity) — that ADR pins *how*
  promotion writes propagate; this ADR pins *when* retention deletes fire.
- **Previous ADRs**: ADR-007 (caller-supplied keys, used unchanged by the retention
  delete); ADR-008 (`IStorageRouter.Cloud` is what executes the delete); ADR-011 (the
  delete direction mirrors that ADR's invariant: cloud op first, row update second).
- **Future**: if a `DeliveredAt` column is added (e.g. as part of courier IPN
  integration), this ADR may be superseded by a "Retention Anchor = `DeliveredAt`
  (with `PaidAt` fallback)" replacement.
