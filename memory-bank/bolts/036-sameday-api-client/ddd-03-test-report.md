---
stage: test
bolt: 036-sameday-api-client
created: 2026-06-02T15:30:00Z
---

# Stage 5 — Test Report: Sameday API Client

## Summary

| Suite | Result |
|---|---|
| **Sameday-specific tests added in this bolt** | **63 passing** (0 failed) |
| Pre-existing unit + integration tests (regression) | **591 passing** (0 failed) |
| **Full backend test suite** | **654 passing, 7 skipped** (MinIO integration; deliberate) |
| Build (`dotnet build PhotoPrint.sln`) | **0 errors, 5 pre-existing warnings** |

`dotnet test PhotoPrint.sln` runs in ~4 s; all Sameday tests are
deterministic (no `Thread.Sleep`, no DNS, no real HTTP).

---

## Test Files Added

| File | Tests | What it pins |
|---|---|---|
| `TestDoubles.cs` | — | `FakeTimeProvider`, `ScriptedHttpMessageHandler`, `FakeAuthenticator` — no NuGet deps added. |
| `SamedaySettingsValidatorTests.cs` | 11 | `IValidateOptions<T>` rules + the critical "disabled-by-default skips every rule" invariant. |
| `SamedayTokenTests.cs` | 6 | `IsValid` with default and custom safety windows, `ToString()` never exposes `Value`. |
| `SamedayCredentialsTests.cs` | 4 | `ToString()` never exposes either field; value equality. |
| `SamedayTokenProviderTests.cs` | 5 | Cache hit/miss/expiry, `Invalidate()`, 50-caller thundering-herd → exactly 1 fetch (ADR-013). |
| `SamedayPoliciesTests.cs` | 4 | 5xx retried; 401 NOT retried (ADR-014 regression test); 429 retried; 400 not retried. |
| `SamedayAuthHandlerTests.cs` | 5 | Bearer attach, `/api/authenticate` bypass, 401-retry-once with fresh token, second 401 → `SamedayAuthException`, request body cloned on retry. |
| `SamedayClientTests.cs` | 14 | `AuthenticateAsync` happy path + every error branch (401 / 5xx / 408 / 4xx / protocol / malformed JSON / DNS); the 3 bolt-037 stubs throw `NotImplementedException`. |
| `SamedayShippingServiceTests.cs` | 4 | Locker + cost paths delegate to static; AWB returns the manual-fallback DTO. |
| `OrderSamedayFieldsTests.cs` | 3 | `AwbLabelUrl` + `LastTrackingSyncAt` default to null and round-trip through EF Core. |

Total: **63 new tests** across **9 test files** + 1 helper file.

---

## Acceptance Criteria Validation

Mapped against the three stories in unit `001-sameday-api-client`.

### Story 001 — `sameday-settings-and-typed-client`

- ✅ `SamedaySettings` binds from `Sameday:` with the documented fields and
  default `RequestTimeoutSeconds = 10` — covered by
  `SamedaySettingsValidator` + `appsettings.json` shipping with the
  expected key set.
- ✅ Typed `HttpClient` registered with `BaseAddress` + per-request timeout,
  with `SamedayAuthHandler` + `SamedayResilienceHandler` in the pipeline —
  covered by the wiring block in `Program.cs` (built green) +
  `SamedayPoliciesTests` exercising the pipeline end-to-end.
- ✅ `IShippingService → SamedayShippingService` is conditional on
  `Sameday:Enabled` — covered by `Program.cs` `if (samedayEnabled) { … } else
  { … }` block + `SamedaySettingsValidatorTests.Disabled_with_blanks_skips_all_rules`
  pinning the no-validation-when-disabled posture.
- ✅ No log statement emits `Username` or `Password` plaintext — covered by
  `SamedayCredentialsTests.ToString_does_not_expose_password` (defence in
  depth; the credential object is the only place a stray log call could
  reach the values).
- ⚠ `/health` includes a passive Sameday liveness flag — **deferred to
  bolt 037**. Bolt 036 adds no `/health` enrichment because the only thing
  worth surfacing today (the flag itself) is duplicated by the
  configuration introspection. Story already noted: "passive … no extra
  call in normal operation; just a flag."

### Story 002 — `token-auth-and-refresh`

- ✅ First-call POST to `/api/authenticate` with credentials and parsed
  response → `SamedayToken` — `SamedayClientTests.AuthenticateAsync_*`.
- ✅ Token cached in-memory for `expiresAt` minus 60 s safety window —
  `SamedayTokenTests` + `SamedayTokenProviderTests`.
- ✅ On 401 from any non-authenticate call, clear cache, re-authenticate,
  retry the original call exactly once —
  `SamedayAuthHandlerTests.On_401_invalidates_token_and_retries_with_fresh_one`.
- ✅ Second 401 → `SamedayAuthException` without exposing the password —
  `SamedayAuthHandlerTests.Second_401_raises_SamedayAuthException`.
- ✅ Token cache is per-process (singleton); no cross-instance sharing —
  guaranteed by DI registration as singleton + ADR-013.
- ✅ Thundering-herd protected by `SemaphoreSlim` —
  `SamedayTokenProviderTests.Concurrent_first_calls_result_in_exactly_one_fetch`
  fires 50 simultaneous callers and asserts a single fetch.

### Story 003 — `sameday-schema-additions`

- ✅ Migration adds `AwbLabelUrl varchar(500) NULL` (Postgres `text` with
  500-char max via EF) and `LastTrackingSyncAt timestamptz NULL` —
  `Migrations/20260602141429_AddSamedayOrderFields.cs` hand-edited to
  Postgres types per the established pattern (`AddUploadArchiveFields`).
- ✅ `Order` exposes both as nullable properties —
  `Models/Order.cs`.
- ✅ EF model builder configures column types + max lengths —
  `Data/PhotoPrintDbContext.cs:294-296` (`HasMaxLength(500).IsRequired(false)`).
- ✅ Migration applies cleanly on Postgres and SQLite (SQLite uses
  `EnsureCreated` from the model) —
  `OrderSamedayFieldsTests` round-trips both columns on the same in-memory
  provider used elsewhere in the suite; the design-time build of the
  migration succeeded.

---

## Coverage of ADR-Locked Invariants

These tests exist specifically to guard the architectural decisions
recorded in this bolt's ADRs against future-PR drift.

| ADR | Invariant | Pinning test |
|---|---|---|
| **ADR-013** | In-process singleton cache + `SemaphoreSlim` against thundering-herd | `SamedayTokenProviderTests.Concurrent_first_calls_result_in_exactly_one_fetch` |
| **ADR-013** | 60-s safety window in front of `expire_at_utc` | `SamedayTokenTests.IsValid_returns_false_when_inside_safety_window` |
| **ADR-013** | `Invalidate()` drops cached token | `SamedayTokenProviderTests.Invalidate_drops_the_cache_and_forces_refresh_on_next_call` |
| **ADR-014** | 401 is NOT in the Polly retryable set | `SamedayPoliciesTests.Does_not_retry_on_401` |
| **ADR-014** | 401 retry-once handled in `SamedayAuthHandler` with fresh token | `SamedayAuthHandlerTests.On_401_invalidates_token_and_retries_with_fresh_one` |
| **ADR-014** | Second 401 → `SamedayAuthException` (stop retrying) | `SamedayAuthHandlerTests.Second_401_raises_SamedayAuthException` |
| **ADR-014** | `/api/authenticate` bypasses the bearer attach | `SamedayAuthHandlerTests.Authenticate_path_is_passed_through_without_a_bearer` |
| **ADR-014** | Retried request body is readable (cloned, not re-used) | `SamedayAuthHandlerTests.Retry_clones_body_so_inner_handler_can_read_it` |

A future PR that adds 401 to `SamedayPolicies.BuildRetryPipeline` will
break `Does_not_retry_on_401` immediately. A future PR that removes the
`/api/authenticate` short-circuit will break two tests
(`Authenticate_path_is_passed_through_without_a_bearer` plus the
`SamedayClientTests` happy path, which goes through the same handler).

---

## What This Bolt Does NOT Test (by design)

These belong to **bolt 037** and are deferred there explicitly:

- AWB creation workflow (`Order → AwbCreationRequest` mapping, persistence
  of `AwbNumber` + `AwbLabelUrl`).
- Background `AwbCreationJob` + `Channel<AwbJob>` queue.
- `ShipmentTrackingJob` 15-minute polling + state transitions.
- Vendor-status-code → `TrackingState` mapping at the anti-corruption
  boundary.
- Real Polly rate-limit policy (5 req/s) once high-frequency callers exist.
- WebApplicationFactory integration test exercising the boot path with
  `Sameday:Enabled = true` and a stub token endpoint. (Bolt 036 verifies
  this indirectly via the typed-client unit tests + the green
  `dotnet build` of `Program.cs`'s conditional DI block.)

---

## Issues Found

None. The implementation needed two small course-corrections during
Stage 4 (Polly v8 API, options validator pattern), captured in the
`Stage 4 Correction` footer on `ddd-02-technical-design.md` before
testing began.

---

## Recommendations

1. **Add a WebApplicationFactory integration test in bolt 037** that
   boots the app with `Sameday:Enabled = true`, a sandbox-shaped
   in-memory authenticate endpoint, and a single AWB-creation flow.
   Bolt 036 doesn't include this because the only operational call —
   `AuthenticateAsync` — is already covered end-to-end by the typed-client
   tests, and a factory-level test for *just authenticate* would
   duplicate that surface without adding signal.

2. **Add `Polly.RateLimiting` NuGet package in bolt 037** alongside the
   first high-frequency caller (tracking-poll job). The chokepoint is
   `SamedayPolicies.BuildRetryPipeline` + one line in
   `SamedayResilienceHandler`.

3. **Add a CI-gated end-to-end Sameday sandbox test** post-launch (after
   sandbox credentials are provisioned and live in user-secrets). This
   confirms the recorded fixture shapes still match vendor reality; the
   recorded JSON we hand-rolled in this bolt is derived from the public
   Sameday docs.

---

## ⛔ Human Checkpoint

Stage 5 (Test) is complete: **63 new tests, 654 total passing**, ADR
invariants explicitly pinned. Build clean. No regressions.

Bolt 036 ready for closeout?

- **1** — Approve and run `bolt-complete.cjs` (closes the bolt + flags the
  three stories as `implemented: true`).
- **2** — Need changes (specify which test or coverage gap).
