---
bolt: 047-coupon-domain-and-api
created: 2026-09-04T00:40:00Z
status: accepted
superseded_by:
---

# ADR-025: Coupon Redemption Uses a Database Compare-and-Swap, Not a `RowVersion` Token

## Context

`Coupon.MaxRedemptions` is a hard cap: a promotion limited to 100 uses must be redeemable exactly
100 times no matter how many customers check out at once. The stories that specify this feature
(`001-coupon-schema`, `003-redemption-on-order-create`) prescribe the mechanism as well as the
guarantee: a `RowVersion bytea` column on `Coupons`, mapped as an EF Core concurrency token, an
in-memory `RedemptionsCount += 1`, and "if a `RowVersion` conflict occurs, retry once; second
conflict → 409".

Three forces pull against that prescription.

1. `standards/data-stack.md` states, as a description of what the codebase *is*: "there are **no
   concurrency tokens** (no RowVersion/xmin anywhere). Correctness under concurrency comes from
   unique indexes + violation detection + retry… A change needing optimistic concurrency is a
   schema decision, not a quick add."
2. ADR-016 already answered the same question for `Order.Status`: background workers transition
   it with `ExecuteUpdateAsync` and a `WHERE` clause pinning the expected state — a database-native
   compare-and-swap whose affected-row count is the success signal — and it says explicitly that
   the shape "generalises beyond bolt 037: any future job that mutates a status column should
   adopt the same shape", and that a `RowVersion` should not be introduced "unless the *value*
   semantics — not just the transition — need protecting".
3. The redemption counter is exactly a transition, not a value: nobody needs to know which
   *version* of the coupon they read, only whether a slot was still free at the instant of the
   write.

There is a second, independent decision bundled here because it is inseparable in practice:
**where** in `OrderService.CreateFromCartAsync` the atomic step runs. That method has an existing
unique-index recovery loop (duplicate `Idempotency-Key`, duplicate `OrderNumber`) with early
returns, entity detaching and a follow-up query after a failed `SaveChanges`, and it currently
runs with no explicit transaction at all.

## Decision

Redemption is a single database statement:

```sql
UPDATE "Coupons"
   SET "RedemptionsCount" = "RedemptionsCount" + 1
 WHERE "Id" = @id
   AND "IsActive"
   AND "ValidFrom" <= @now AND "ValidUntil" > @now
   AND ("MaxRedemptions" IS NULL OR "RedemptionsCount" < "MaxRedemptions");
```

issued through `ExecuteUpdateAsync`. One affected row means redeemed; zero means the caller lost
the race or the coupon changed underneath, and a single follow-up read classifies which so the
customer is told the truth (`INVALID_COUPON` for a deactivated or expired coupon,
`COUPON_EXHAUSTED` for a full one) instead of one generic answer.

No `RowVersion` column exists on `Coupons`. No retry loop exists for redemption.

The statement runs **as the last operation before `COMMIT`**, inside a transaction that is opened
only when the order actually carries a coupon, and after the order, its items and the
`CouponRedemption` row have been inserted successfully.

The mirror operation, releasing a redemption held by a provably abandoned order, is the same
shape: delete the `CouponRedemption` row, and decrement only when a row was actually deleted.

On the EF InMemory provider — the integration-test default, which supports neither
`ExecuteUpdateAsync` nor real transactions — `CouponService` branches to a read-check-increment
through the change tracker, exactly as `OrderNumberService` and `StaticShippingService` already
branch on `Database.ProviderName`. That branch is not concurrency-safe and is not claimed to be.

## Rationale

A compare-and-swap has no read-then-write window, so it cannot lose an update and therefore has
nothing to retry. The token-plus-retry design is strictly weaker for this problem: with 100
concurrent checkouts and 5 free slots it produces roughly 95 conflicts that are *not* exhaustion,
retries them, and the story's own rule then converts a second conflict into a 409 — so a customer
holding a perfectly valid coupon can be refused because of scheduling luck rather than because
the promotion ran out. Under the CAS, every refusal is a true answer.

Folding the whole redeemability rule — active, inside its validity window, under its cap — into
the `WHERE` clause rather than only the cap matters as much as the atomicity: it is what closes
the window in which an admin deactivates a coupon, or the clock passes `ValidUntil`, between
validation and commit.

The placement decision was made under an adversarial design check, which found that running the
CAS *first* (the intuitive reading of "reserve, then write") was wrong on two counts. A row lock
taken by an `UPDATE` is held until the transaction ends, not for the statement: CAS-first would
serialise every checkout on a site-wide promotion behind one row for the duration of an entire
order insert. Worse, it created a deadlock cycle with the idempotency index — transaction A
holding the coupon row while blocked inserting a duplicate key, transaction B holding that key's
index entry while waiting for the coupon row. PostgreSQL breaks such a cycle with SQLSTATE
`40P01`, which matches neither `IsIdempotencyKeyViolation` nor `IsOrderNumberViolation` (both keyed
on `23505`), so the ordinary double-submit that idempotency exists to handle would have escaped
both `when` filters as a raw 500. With the CAS last, nothing waits on anything after the coupon
row is locked, so no cycle can form.

Running it last also keeps the InMemory branch honest. With the CAS first, an idempotent replay
discovered during unique-index recovery would return *after* an in-memory increment, leaving a
pending change on the request-scoped `DbContext` that `PaymentsController`'s later
`SaveChangesAsync` would flush — making "a replay redeems nothing" true on PostgreSQL and false
on InMemory, in the very test that is supposed to prove it.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| `RowVersion` token + retry-once (as the stories specify) | Familiar EF idiom; matches the story text literally | Introduces the codebase's only concurrency token; refuses valid coupons under load; needs a retry budget that is itself a tuning decision; contradicts `data-stack.md` and ADR-016 | Weaker guarantee at higher cost, against two standing decisions |
| `SELECT … FOR UPDATE` then increment | Explicit and readable | Two round trips holding a lock for both; the lock is held from the read to the commit, which is longer than the CAS holds it; provider-specific raw SQL | More lock, more code, no more safety |
| A unique index on `(CouponId, sequence)` and let the insert fail | Uses the codebase's stated "unique index + violation detection" mechanism | Needs a per-coupon sequence number the domain does not have; `RedemptionsCount` would become derived, breaking the admin list's cheap read | Invents state to avoid an `UPDATE` |
| Advisory lock per coupon | Trivially correct | A held advisory lock outlives a crashed session only until disconnect; adds a second concurrency mechanism to the codebase | No advantage over the CAS |
| Count redemptions at read time instead of storing a counter | No counter to keep in sync (invariant 2 disappears) | The cap check becomes `COUNT(*)` under a lock; the admin list pays a correlated count per coupon | Slower, and the CAS predicate cannot be expressed as one statement |

## Consequences

### Positive

- Over-redemption is impossible by construction, not by care, and the guarantee is one statement
  a reviewer can read.
- No new schema concept enters the codebase; `data-stack.md`'s "no concurrency tokens" sentence
  stays true, and ADR-016's pattern gets its second instance rather than a competing one.
- Every refusal carries a true reason, because the classification read runs only on failure.
- The coupon row is locked only between the CAS and the commit, so concurrent checkouts on a
  popular promotion queue for microseconds rather than for a whole order insert.
- Orders without a coupon keep their current behaviour exactly: no transaction, no new statement,
  no new failure mode on the payment path bolts 034/035 hardened.

### Negative

- The mechanism is invisible to the default integration-test stack. Every guarantee in this ADR
  is proven only by tests that run against a real PostgreSQL database; an InMemory test asserting
  redemption semantics would be theatre, so those tests are not written there.
- `RedemptionsCount` must equal the number of `CouponRedemption` rows, and that invariant is now
  maintained by two statements in two places (redeem and release) rather than by a single
  aggregate operation.
- The `affected = 0` path costs one extra read. It runs only when a redemption is refused.

### Risks

- **A future contributor "hardens" this by adding a `RowVersion`.** Mitigated by this ADR being
  indexed with an explicit "tempted to add a concurrency token" trigger, the same way ADR-020
  guards the invoice sequence.
- **A future write path updates `RedemptionsCount` outside `CouponService`** — an admin bulk edit,
  a data fix — and breaks the count-equals-rows invariant. Mitigated by the admin update path
  never writing the counter, and by the redemption-stats endpoint reading the rows rather than
  the counter, so a divergence is visible rather than silent.
- **Release makes the counter non-monotonic**, so anything that assumed "count only ever rises"
  would be wrong. Nothing does today; the decrement is guarded to never go below zero.

## Related

- **Stories**: 001-coupon-schema, 003-redemption-on-order-create
- **Standards**: `standards/data-stack.md` (concurrency section — this ADR is its second worked
  example and does not change it)
- **Previous ADRs**: ADR-016 (CAS via `ExecuteUpdateAsync` for `Order.Status`), ADR-015 (accepting
  duplication rather than adding a lock), ADR-020 (a database primitive chosen over an
  application-level counter)
