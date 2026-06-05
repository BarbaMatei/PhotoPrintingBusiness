---
intent: 028-test-architecture
phase: inception
status: inception-complete
created: 2026-06-05T09:00:00Z
updated: 2026-06-05T09:00:00Z
source: docs/analysis/architect-review-2026-06-03.md (Group 4 — P27, P28)
priority_score: 20
---

# Requirements: Test Architecture (Shared Factory Base, Builders, TimeProvider)

## Intent Overview

Three compounding test-architecture problems: (1) 11 `WebApplicationFactory<Program>` subclasses each duplicate 30–80 lines of identical config (Cors, RateLimit, JwtSettings, Email provider, health-check paths) — a standard-config change needs 11 edits; (2) there is no `TestBuilders/`, so every test file inlines its own user/order/cart seeding; (3) 25 tests under `Unit/` construct a real `PhotoPrintDbContext`, so they are micro-integration tests masquerading as unit tests, making the pyramid misleading and the suite slow/brittle. Compounding all three: `TimeProvider` is half-adopted — the 2026-vintage code (bolts 037/039/044) injects it and uses `FakeTimeProvider`, while 35 older files hard-code `DateTimeOffset.UtcNow` (63 calls; `AuthService.cs` alone has 13), so token-expiry/lockout logic can't be tested deterministically. This intent promotes the bolt-044 `ObservabilityFactoryBase` into a shared base, adds Builders, reclassifies the misnamed tests, and finishes the `TimeProvider` migration. It **must track intent 027 (layering) in lockstep** — interleaved, not sequential. Ship P28 first (it adds constructor params that Builders then hide).

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| One place to change shared test config | Standard config edited once, not in 11 factories | Should |
| Reusable, fluent test data seeding | `Builders/` cover the 6 most-used entities | Should |
| Honest test pyramid | DbContext-backed tests live under `Integration/`, not `Unit/` | Should |
| Deterministic time in tests | Zero raw `DateTimeOffset.UtcNow` in `Application/`+`Infrastructure/`; `FakeTimeProvider` usable everywhere | Should |

---

## Functional Requirements

### FR-1 (P28): Adopt `TimeProvider` consistently (kill 63 raw `DateTimeOffset.UtcNow` calls)
- **Description**: Inject `TimeProvider _clock` and replace `DateTimeOffset.UtcNow` → `_clock.GetUtcNow()` across the 35 older files, priority-ordered: `AuthService.cs` (13 calls — refresh-token/lockout/email-confirm/password-reset expiry), then `AccountService` (4), `AdminOrderService` (3), `OrderService` (1), `EuPlatescService` (3), all `BackgroundJobs/*.cs` (6 files). Add a banned-API rule forbidding `DateTimeOffset.UtcNow` in `Application/`+`Infrastructure/`. `Models/*.cs` default-property clocks may stay (write-time fallback) provided tests construct via Builders that set the clock explicitly.
- **Acceptance Criteria**:
  - Analyzer flags any new raw `UtcNow` in the two layers; existing offenders removed.
  - At least one time-sensitive scenario per refactored service is unit-tested with `FakeTimeProvider` (e.g. token expiry advances deterministically — no `Thread.Sleep`, no "within 5 seconds" assertions).
  - Constructor-signature changes (e.g. `AuthService(TimeProvider ...)`) are absorbed by P27 Builders/test helpers, not scattered across every test.
- **Priority**: Should
- **Related Stories**: TBD

### FR-2 (P27): Shared `IntegrationTestBase` / `TestApplicationFactory` + `Builders/` + reclassification
- **Description**: (a) Promote `MetricsEndpointIntegrationTests.ObservabilityFactoryBase` to a `public abstract PhotoPrintTestApplicationFactory` under `tests/_Base/`, holding the 25 standard config keys + the InMemory swap + a no-op email service; refactor the 11 factories to inherit it and keep only their feature-specific overrides (~30 LOC each). (b) Add `tests/Builders/` with fluent builders for the 6 most-used entities (User, Order, CartItem, Invoice, Upload, …). (c) Reclassify the 25 DbContext-constructing "unit" tests into `tests/Integration/ServiceLevel/` (Option A — rename the folder; the `[Fact]` content is correct, the naming was wrong), mirroring the feature folders from intent 027.
- **Acceptance Criteria**:
  - All 11 factories inherit the shared base; the standard config exists in exactly one file.
  - `Builders/` provide fluent seeding (`new UserBuilder().Confirmed().WithEmail(...).Build()`); used by refactored tests.
  - The 25 misnamed tests live under `Integration/ServiceLevel/`; `Unit/` contains only genuine unit tests (pure `Domain/` logic + mocked-dep service tests).
  - CI `dotnet test --filter` patterns updated for the new folders; full suite green.
- **Priority**: Should
- **Related Stories**: TBD

---

## Non-Functional Requirements

### Maintainability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Shared test config duplication | Edit count for a config change | 1 (was 11) |
| Test pyramid honesty | Misclassified tests | 0 DbContext tests under `Unit/` |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Deterministic time-based tests | Flakiness from wall-clock | 0 (`FakeTimeProvider`) |
| No behaviour change | Suite result | Baseline maintained |

---

## Constraints

### Technical Constraints
- **Lockstep with intent 027** — interleave the PRs; do not run sequentially (writing the base against the OLD folder shape then rewriting for the NEW shape is wasted work).
- Ship **P28 before P27** — `TimeProvider` constructor params land first; Builders then hide the new signatures from test code.
- xUnit `IClassFixture<T>` share-per-class has ordering gotchas; verify nothing depends on per-test isolation the share would break.

### Business Constraints
- Post-launch / non-blocking by behaviour, but a healthy suite is what gives confidence to deploy — strong companion to intents 027/029.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| The 25 DbContext tests are correct, only misnamed | A reclassified test was actually wrong | Reclassification is content-preserving; failures surface immediately |
| `FakeTimeProvider` package is already referenced (newer test files) | Extra dependency needed | It is referenced; add centrally via CPM (intent 025) if not |
| Model default-clock fallbacks can stay | A test depends on a frozen model default | Builders set the clock explicitly; document the exception |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Reclassify misnamed tests by folder move (Option A) vs introduce repositories (Option B)? | Dev | 2026-06-26 | Option A — intent 027 P24 explicitly rejects repositories |
| Q2: Which 6 entities get Builders first? | Dev | 2026-06-26 | User, Order, CartItem, Invoice, Upload + one TBD |
| Q3: Keep or replace `Models/*.cs` default-clock assignments? | Dev | 2026-06-26 | Recommend keep (write-time fallback); Builders override in tests |
