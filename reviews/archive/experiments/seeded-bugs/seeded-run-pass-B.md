---
type: seeded-experiment-pass
pass: B
framing: "incident post-mortem author"
model: opus (all 7 lenses)
commit: 4ea6342 (seeded worktree)
synthesized: 2026-07-04
note: synthesized BEFORE opening the answer key
---

# Seeded experiment — Pass B synthesis (blind)

Seven blinded Opus lenses, same breadth as Pass A, framed as an incident post-mortem. Deduped by
defect+location; `lenses` = how many of the 7 independently raised it.

## Distinct defects raised (Pass B)

| # | Location | Defect | Max severity | Lenses | Confidence |
|---|----------|--------|--------------|--------|-----------|
| B1 | OrderService.cs:326 | `easyboxLockerId` divergence check re-tests `DeliveryType`; locker never compared | serious | 6/7 | high |
| B2 | OrderService.cs:28 | `IdempotencyWindow = FromHours(12)` vs documented 24h | serious | 6/7 | high |
| B3 | OrderService.cs:188 (+ dead :234) | retry catch not gated on `IsOrderNumberViolation` (dead) → retries ANY DbUpdateException | serious | 5/7 | high |
| B4 | OrderService.cs:309 | authed scope `\|\| o.UserId == null` → matches guest orders (cross-tenant/IDOR) | serious | 5/7 | high (behavior) / medium (exploit) |
| B5 | IdempotencyKeyFilter.cs:43 | `>= MaxKeyLength` rejects valid 80-char key (off-by-one) | medium | 5/7 | high |
| B6 | IdempotencyKeyFilter.cs:61 | missing-key `LogWarning` vs doc-mandated Information (OBS-3) | serious→cleanup | 5/7 | high |
| B7 | Migration ...AddOrderIdempotencyKey.cs:62 | `StripeClientSecret` DDL `varchar(255)` vs model/snapshot 512 | serious/medium | 2/7 | high |
| B8 | PhotoPrintDbContext.cs:310 | `IdempotencyKey` model 40 vs migration/snapshot/filter 80 (3-way drift) | serious/medium | 2/7 | high |
| B9 | ExceptionHandlerMiddleware.cs:82-85 | cross-tenant conflict comment says "Error incl. key"; code logs Warning, no key | serious→minor | 4/7 | high (mismatch) |
| B10 | PaymentsController.cs:135 | dead local `hasCachedValue` | cleanup | 4/7 | high |
| B11 | Snapshot index-filter / TEXT drift | SQLite-flavored snapshot → phantom Npgsql diff (deferred) | medium | 1/7 | high |
| B12 | PhotoPrintDbContext.cs:24,295 | `OrderNumberIndexName` const referenced only by dead code; index uses literal | cleanup | 1/7 | high |
| B13 | PhotoPrintDbContext.cs:292,305 | `OrderNumber` `HasMaxLength(20)` configured twice | cleanup | 1/7 | high |
| B14 | PaymentsController.cs:57 | `(long)(TotalRon*100)` truncates | minor | 1/7 | low |
| B15 | OrderNumberService.cs:25 | SQLite branch numbers from global Count(), no per-year reset | cleanup | 1/7 | high (behavior) |
| B16 | PaymentsController.cs:42-47 | gateway 502 (BadGatewayException) not in ProducesResponseType | minor | 1/7 | medium |
| B17 | Postgres arm untested / vacuous InMemory greens | provider-only branches + cross-tenant InMemory test prove non-prod behavior | medium | 2/7 | high |

Lower-signal singletons: EuPlatesc recovery-replay path untested (B tests); `GetByIdAsync`/
`GetByPaymentIntentIdAsync` unscoped (low, caller-dependent); stale-key update-then-insert EF
ordering reliance (low); missing-header = all dedup bypassed today (transitional, by design).

## Pass B character
Same dominant cluster (B1–B6) at essentially identical convergence to Pass A. Post-mortem framing
produced a few extra ops/quality singletons (B12, B13, B15, B16) but did not change which core
defects surfaced. DB-parity lens again the sole source of B7/B8.
