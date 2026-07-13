---
type: seeded-experiment-pass
pass: A
framing: "new maintainer reading cold"
model: opus (all 7 lenses)
commit: 4ea6342 (seeded worktree)
synthesized: 2026-07-04
note: synthesized BEFORE opening the answer key
---

# Seeded experiment — Pass A synthesis (blind)

Seven blinded Opus lenses (correctness, security, requirements/observability, quality,
DB-parity, tests, completeness critic) over the seeded worktree. Deduped by defect+location;
`lenses` = how many of the 7 independently raised it (convergence signal).

## Distinct defects raised (Pass A)

| # | Location | Defect | Max severity | Lenses | Confidence |
|---|----------|--------|--------------|--------|-----------|
| A1 | OrderService.cs:326 | `easyboxLockerId` divergence check re-tests `DeliveryType` (copy-paste); locker never compared | serious | 6/7 | high |
| A2 | OrderService.cs:28 | `IdempotencyWindow = FromHours(12)` but every doc/comment says 24h | serious | 6/7 | high |
| A3 | OrderService.cs:188 (+ dead :234) | order-number retry catch doesn't gate on `IsOrderNumberViolation` (which is dead code) → retries ANY DbUpdateException 3× | serious | 5/7 | high |
| A4 | OrderService.cs:309 | `FindKeyHolderAsync` authed branch `o.UserId == userId \|\| o.UserId == null` → matches guest/orphan orders (cross-tenant) | serious | 4/7 | high (behavior) / medium (exploit) |
| A5 | IdempotencyKeyFilter.cs:43 | length check `>= MaxKeyLength` rejects a valid 80-char key (off-by-one; should be `>`) | medium | 5/7 | high |
| A6 | IdempotencyKeyFilter.cs:61 | missing-key event `LogWarning` despite doc/comment saying Information (OBS-3 regressed) | serious→cleanup | 5/7 | high |
| A7 | Migration ...AddOrderIdempotencyKey.cs:62 | `StripeClientSecret` DDL `varchar(255)` while model+snapshot say 512 (explicit `type:` wins) | serious | 2/7 | high |
| A8 | PhotoPrintDbContext.cs:310 | `IdempotencyKey` model `HasMaxLength(40)` vs migration/snapshot/filter 80 (3-way drift) | medium | 2/7 | high |
| A9 | ExceptionHandlerMiddleware.cs:82-85 | cross-tenant conflict comment promises "Error event incl. key"; code logs Warning, no key | minor | 3/7 | high (mismatch) |
| A10 | PaymentsController.cs:135 | dead local `hasCachedValue` (switch recomputes inline) | cleanup | 4/7 | high |
| A11 | Snapshot / model index filter + TEXT | SQLite-flavored snapshot → phantom Npgsql migration diff (known/deferred DB-1/DB-2) | medium | 1/7 | high |
| A12 | tests: cross-tenant InMemory test | asserts 200+distinct order (non-prod behavior; vacuous green on the provider that can't enforce the index) | minor | 1/7 | medium |
| A13 | PaymentsController.cs:57 | `(long)(o.TotalRon*100)` truncates instead of rounds | minor | 1/7 | low |
| A14 | replay / replay-recovery log events | never asserted by any test | minor | 1/7 | high |
| A15 | Postgres arm entirely untested (no Migrate(), no Testcontainers) | provider-only branches ship green | medium | 1/7 | high |

Lower-signal singletons also noted: `OrderService.cs:78` arbitrary-first-finish (pre-existing, low);
divergence coverage gaps for totalRon/deliveryType; concurrent-winner-divergence path untested.

## Pass A character
Very high internal convergence on A1–A6 (the OrderService + filter cluster). The DB-parity lens
was the sole source of the migration-DDL findings (A7, A8) — no other Pass A lens reached them.
