---
type: review
version: 9
supersedes: 8
pass-type: verification
target: Bolt 035 — payment idempotency (verify resolution-v8)
branch: feat/bolt-035-payment-idempotency
commit: 01b5264
base: 50fc692 (the v8-reviewed commit)
date: 2026-07-04
reviewer: multi-lens harness (Opus 4.8) — 4 isolated anchored verification lenses + build/test
method: VERIFICATION pass (anchored) — each lens was given the specific v8 finding + its resolution-v8 note + the fix diff (50fc692→01b5264) and asked to SKEPTICALLY confirm the fix held / the regression test is non-vacuous / no regression was introduced. Distinct from a discovery pass (this does NOT re-audit the whole feature and CANNOT emit `approved`).
verdict: approve-with-followups
outcome: { verified: 13, reopened: 1, deferrals_accepted: 4 }
---

# Review v9 — Bolt 035 payment idempotency (verification of resolution-v8)

Anchored re-review of [resolution-v8.md](resolution-v8.md)'s 14 fixes + 4 deferrals, against
the fix commit `01b5264`. Four isolated lenses (correctness/concurrency · security/input ·
quality/altitude · docs/observability+deferrals), each blinded to the others, each trying to
*refute* the claim that its findings were fixed. Build + full suite re-run as the harness's
own evidence.

> This is a **verification** pass, not discovery. Per `reviews/README.md` it can confirm "these
> fixes held" and at most emit `approve-with-followups` — it does **not** certify the feature
> clean (that needs a saturated discovery pass). It correctly landed one **reopen**.

## Verification & build

| Check | Result |
|-------|--------|
| `dotnet build PhotoPrint.sln -c Debug` | ✅ 0 errors (only the pre-existing NU1603 Stripe.net + 1 CS1998, both unrelated) |
| `dotnet test PhotoPrint.sln` | ✅ **487 passed / 0 failed / 0 skipped** (baseline 474; +13 net new regression tests) |
| Non-vacuity (revert-and-rerun) | ✅ Re-confirmed by the correctness lens for BUG-3 (restore the intermediate save → rollback test red) and BUG-4 (retries=0 → collision test 500s) |

## Per-finding verdict

| ID | Sev | resolution-v8 | v9 verdict | Evidence (file:line) |
|----|-----|---------------|-----------|----------------------|
| OBS-1 | 🟠 M | fixed | **verified** | `IdempotencyKeyTakenException : ConflictException` (sealed) + explicit exact-type 409 mapping entry (`ExceptionHandlerMiddleware.cs:15`) — required since the dict is keyed by `GetType()`; thrown only on the cross-tenant path (`OrderService.cs:186`); reserved `cross-tenant-conflict` log fires (mw `:81-84`); test asserts 409 + event. Same-caller divergence still throws `IdempotencyConflictException`. |
| BUG-1 | 🟡 L | fixed | **verified** | Shared `IdempotencyKeyIndexName` referenced by BOTH the index (`PhotoPrintDbContext.cs:338`) and the Postgres match (`OrderService.cs:222`) — a real reference, rename → compile break; SQLite arm uses extended `2067` + `nameof(Order.IdempotencyKey)`; test pins both premises. OrderNumber violation routed to a separate predicate. |
| BUG-3 | 🟡 L | fixed | **verified** | Intermediate `SaveChangesAsync` after the stale-free is GONE (diff-confirmed); happy-path proves no within-batch collision; rollback test is non-vacuous (would go red against the two-save code — the interceptor only throws when an Added order is present). |
| BUG-4 | 🟡 L | fixed | **verified** | Retry bounded by `MaxOrderNumberRetries=3` (no infinite loop; persistent clash propagates); test uses the REAL generator + is non-vacuous (retries=0 → 500); unrelated FK still propagates; replay/409 semantics preserved; keyless path now also benefits (strict improvement). |
| SEC-2 | 🟡 L | fixed | **verified** | Trim ordered before the length check (`IdempotencyKeyFilter.cs:37`); filter unit tests set the raw header directly (bypassing HTTP OWS-strip) and prove padded==plain dedupe. |
| REQ-1 | 🟡 L | fixed | **verified** | Stale-free only runs for an owner-scoped holder (`OrderService.cs:97-114`, `:305-309`); ddd-01 (invariant+glossary), `Order.cs`, `IOrderService.cs` state owner-scoping consistently. |
| OBS-2 | ⚪ C | fixed | **verified** | `IdempotencyConflictProblemDetails` with nullable `DivergentFields`; both endpoints type the 409; runtime body unchanged; field present only for the same-caller conflict, absent for the cross-tenant type — matches the DTO. |
| OBS-3 | ⚪ C | fixed | **REOPEN** | Code is correct (`LogInformation`, event/fields unchanged, `IdempotencyKeyFilter.cs:61`). But the doc alignment OBS-3 scoped is **incomplete** — 4 stale "Warning/WARN" references remain and contradict the fix + the updated glossary: `ddd-01:118`, `ddd-02:117`, `ddd-02:324`, and the filter class summary `IdempotencyKeyFilter.cs:12-13`. Finish the alignment. |
| QUAL-1 | ⚪ C | fixed | **verified** | Dead `GetByIdempotencyKeyAsync` gone (no live refs; `IsFresh`/`FindKeyHolderAsync` retained); tests retargeted; both-null guard moved to `CreateFromCartAsync`. |
| QUAL-2 | ⚪ C | fixed | **verified** | `PricingTierResolver.Resolve` is byte-for-byte the prior rule; both callers delegate keeping their own source+qty; behavior-preserving; misleading comment corrected. |
| QUAL-3 | ⚪ C | fixed | **verified** | 3 fixtures delegate to `TestCartSeed.Build`; `SizeId` set consistently; order flow reads `Product.Sizes`, not `CartItem.SizeId` → InMemory behavior unchanged. |
| QUAL-4 | ⚪ C | fixed | **verified** | Winners built via the real `CreateFromCartAsync`; no magic 6/20/26 totals remain; distinct vs equal order-number pins the intended collision path in each test. |
| QUAL-5 | ⚪ C | fixed | **verified** | Switch over `(replay, cached)` preserves all three branches; `(true,true)` returns before the gateway call (genuinely skipped). |
| QUAL-6 | ⚪ C | fixed | **verified** | `WriteProblemDetailsAsync` is instance + uses `_environment`; `IsDevContext` service-locator deleted; dev/prod shape unchanged, both carry `divergentFields`. |

## Deferrals (all re-affirmed **sound**)

| ID | v9 judgment | Evidence |
|----|-------------|----------|
| DB-1 | **deferral-sound** | Zero `Migrate(`/`MigrateAsync`/`GetPendingMigrations` in tests; every fixture uses `EnsureCreated` → the Postgres arm runs in tests nowhere. Migration breadcrumb accurate. Migration/deploy-phase home. |
| DB-2 | **deferral-sound** | Snapshot genuinely SQLite-flavored for the idempotency artifacts (`TEXT`, unfiltered index) vs Npgsql `varchar(80)` + filtered; migration comment acknowledges the phantom diff. Same home as DB-1. |
| SEC-1 | **deferral-sound** | Durable fix is genuinely a schema/migration change (composite per-tenant index + matcher rename); LOW-exploitability argument honest (GUID keys, self-limiting probe); accepted-residual threat note present + accurate (`PhotoPrintDbContext.cs:324-335`). Shares root with REQ-1. |
| BUG-2 | **deferral-sound** | Stripe keyed by `o.Id` (`PaymentsController.cs:60-61`); EuPlatesc has no gateway key; asymmetry documented at the build path + ddd-02. No double CHARGE today — `invoice_id == order.Id` (stable across concurrent recovery) folds into the HMAC, so EuPlatesc maps both rebuilt URLs to one order; only the "verbatim replay" invariant momentarily breaks. Row-lock fix legitimately needs the unbuilt Postgres arm. |

## Regressions / fix-generativity

- **No behavioral regressions** across all four lenses. Tenant isolation intact (cross-tenant path throws, never replays/leaks; `FindKeyHolderAsync` owner-scoped; both-null guard fires). No hidden behavior change in any refactor. The OrderService retry-loop restructure keeps a single pending INSERT (no double-insert) and preserves replay/409.
- **One fix-generated incompleteness caught — OBS-3.** An incomplete doc edit left 4 references contradicting the shipped code. This is precisely the fix-generativity class `reviews/README.md` flags (2 of the 5 out-of-audit findings in v1–v7 were fix-introduced doc drift). The code is correct; only the doc alignment is unfinished.

## Recommendation

**Approve with follow-ups.** 13 of 14 fixes **verified**, 4 deferrals **accepted** on sound
fact-based rationale, 0 regressions. **OBS-3 reopened** for a trivial, fully-enumerated doc-only
completion (no code change, no blocker). Once the 4 stale Warning references are aligned, every
v8 finding is terminal-and-confirmed for the resolution loop.

Note (two loops): this verification confirms *the fixes held*; it does **not** certify bolt-035
clean as a feature. Closing the feature still requires a **saturated discovery** pass (K
independent blinded audits agreeing) — tracked separately, not part of this verification.

---

*Process: [reviews/README.md](../README.md). v9 is a verification re-review; it flips resolution-v8's
confirmed fixes to `verified` and reopens OBS-3. Only a saturated discovery pass may emit `approved`.*
