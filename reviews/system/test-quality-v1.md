---
type: system-review
target: test-quality
version: 1
pass-type: stratified read-only audit
commit: 770f852
date: 2026-08-06
verdict: bimodal — an engineered top tier next to a large decorative floor; 4 confirmed criticals
findings: 309 raised · 20 adversarially verified (10 confirmed / 10 downgraded / 0 refuted) · standing 4 🔴 · 12 verified 🟠 · ~100 unverified 🟠 · 186 🟡/⚪
checked-by: 20 independent verifier agents (one skeptic per finding), verdicts folded in
---

# Test-quality review v1 — how this repo writes tests

The target is the **testing approach and test quality** of both suites: 132 tracked `.cs`
files under `src/PhotoPrint.Tests` (1,029 tests: 961 `[Fact]`, 57 `[Theory]`,
11 `[SkippableFact]`) and 48 Vitest specs under `src/PhotoPrint.UI` (~454 `it(` blocks).
Method: all 180 files triaged, the 60 highest-risk read in full against an 8-dimension
rubric alongside the production code they claim to pin, 7 cross-cutting lenses
(integration architecture, mock discipline, environment fidelity, Angular approach,
determinism, assertion power, stats), then one adversarial verifier per top finding —
20 verified, 10 confirmed at severity, 10 downgraded, 0 refuted. The audit was
**read-only**: no test was executed, no mutation was run; every "this test cannot fail"
claim is a code-reading inference, independently re-derived by a verifier. Like every
review record, this file is immutable once committed; respond in a resolution or v2 file.

## Owner summary — the five things worth your time

1. **The Stripe payment step is protected by nothing** (TQ1). Its only behavioral test
   asserts that a mock the spec itself created exists (`toBeDefined()` on a `vi.fn()`),
   the async Stripe init is never awaited, and there is no `payment.service.spec.ts` at
   all. Order creation, intent creation, its error message, and card confirmation — the
   money path in the SPA — have zero real assertions.
2. **Prod boot is on a provably broken migration path and the suite cannot see it**
   (TQ2). Nothing runs the migration chain on Postgres, and the chain mixes PostgreSQL and
   Npgsql column types, including `AlterColumn` casts Postgres has no cast for. The
   verifier confirmed and sharpened data-stack.md's warning: `Database.Migrate()` at
   first Postgres boot fails before the app serves a request, with all 1,029 tests green.
   CI already runs a postgres:16 container — every test factory hard-codes a fake
   connection string that shadows it.
3. **Two security controls are tested against test-authored stand-ins** (TQ3, plus the
   JWT gap). The rate-limit tests replace the production limiter with one the test
   factory builds, so the app's limiter (and the login brute-force policy, tested
   nowhere) could be deleted with CI green. And no test anywhere presents a forged,
   expired, or foreign-issuer JWT — relaxing `ValidateLifetime`/`ValidateIssuer` in
   production reddens nothing.
4. **A repo-wide read-back pattern makes "Saves/Sets/Stamps" tests blind to a missing
   `SaveChanges`** (TQ6). Unit suites assert through the same tracked `DbContext` the
   service mutated, so EF's identity map answers from memory whether or not the commit
   ran. Confirmed in AdminOrderServiceTests and UploadCleanupJobTests; the same shape
   recurs in ~10 suites; `ChangeTracker.Clear()` appears zero times in the test project.
5. **The audit also found live product bugs** (side-findings): the admin order search
   mixes a normalized needle with a raw one, so a lower-cased or whitespace-padded order
   number finds nothing; the reset-password page handles a 400 the endpoint never
   returns (real failures show the user nothing).

## Baseline scorecard

Graded from this audit's evidence; **locked** as the fixed comparison point for future
re-grades — re-grade against these same dimensions and anchors, not revised ones.

| # | Dimension | Grade /10 |
|---|---|---|
| 1 | Assertion strength — do greens pin behavior? | 4 |
| 2 | Mock discipline — boundaries only, real components proven | 7 |
| 3 | Environment fidelity — the env can prove the claim | 3 |
| 4 | Failure-mode coverage — error paths, unhappy lifecycle | 4 |
| 5 | Regression fidelity — would the test go red? | 4 |
| 6 | Isolation & determinism — flake surface | 6 |
| 7 | Test architecture & maintainability | 5 |
| 8 | Gap honesty — suites state what they cannot prove | 3 |

Anchors (what moved each grade):

1. **Assertion strength 4.** Ceiling is excellent (DashboardMetricNamesTests scrapes a
   real Prometheus exposition; PaymentIdempotencyRelationalTests asserts 409 + row count
   + row ownership on a real unique index). Floor: 106 of 213 integration tests assert
   only an HTTP status code; two verified cannot-fail tests (TQ1, TQ4); hygiene
   otherwise clean (0 `Assert.True(true)`, 0 `Excluding` chains, verify-only tests
   legitimate). Lens suggested 5; the deep-read tail of unasserted DTOs, name-lies and
   never-checked sequences pulls it to 4.
2. **Mock discipline 7.** The 042-D25 class did not recur: all 16 frequently-mocked
   internal types run real somewhere; zero DbContext/DbSet mocks; heavy mocks sit on true
   boundaries; the frontend uses real jsdom localStorage with a purpose-built real-seam
   test. Docked for: both email-sender adapters have no seam and zero tests; the two
   suites that mock HttpResponse are exactly the two that set the refresh-token cookie
   (its HttpOnly/SameSite attributes asserted nowhere); eight UI component specs stub all
   of the component's own services with `vi.fn()` literals.
3. **Environment fidelity 3.** ~57 of 1,029 backend tests reach a relational engine (all
   PostgreSQL), exactly 1 drives HTTP relationally, 0 touch Postgres — while prod boot's
   `Migrate()` is on a path the DDL evidence says fails outright (TQ2). Eight of the
   nine unique indexes that ARE the app's concurrency mechanism have no duplicate-insert
   test. The InMemory default holds row sets production forbids. UI: chart assertions
   impossible in jsdom; one suite fires real HTTP at localhost:5052. The honest
   disclosure in data-stack.md and the existing PostgreSQL fixture pattern are what keep
   this at 3 rather than 2.
4. **Failure-mode coverage 4.** Strong where someone cared (upload bomb guards, the
   idempotency collision matrix, guest claim 400s). But: the auth rate limiters
   (brute-force defence) have no test, a rotated refresh token is never replayed, the
   admin-role guard on AWB creation is unproven, `FireOrderCancelledEmail` is tested by
   nothing, pagination has zero tests, and most UI suites have no error-path test at all.
5. **Regression fidelity 4.** Confirmed blindness patterns: same-context read-backs
   (TQ6), guards deletable with green suites (Stripe refund verify, cart-merge conflict
   guard, `DetachFailedInsert`), tests entering through reflection on private methods,
   UI tests asserting class fields on OnPush components (blind to the exact refresh bug
   they exist to guard). Counterweight: CloudPreviewIntegrationTests annotates each
   assert with the mutation that reddens it — the bar the rest should meet.
6. **Isolation & determinism 6.** Engineered isolation is genuinely strong (documented
   `DisableParallelization` collection, AsyncLocal MetricCapture with its own isolation
   suite, zero env-var mutation, per-test Guid-named stores). Hand-rolled waiting drags:
   14 wall-clock sleeps, of which 4 sites will flake on a loaded machine (9×
   `Task.Delay(200)` over fire-and-forget, a 350ms sleep against a 300ms debounce), and
   RateLimitIntegrationTests leaks an undisposed host per test that manufactures exactly
   that load.
7. **Architecture & maintainability 5.** One well-built spine (the five-deep factory
   chain, PostgresPaymentFactory, deliberate de-duplications with narrated rationale) next
   to: ten copy-pasted host-config roots already diverged, four no-op email fakes under
   four names, five hand-rolled JWT builders drifted to a role the domain does not
   define, a documented factory base class that does not exist, ~111 WAF host boots per
   full run (a direct contributor to the "full runs saturate this machine" rule), and no
   UI setupFiles so per-file boilerplate is copy-pasted 48 times in four variants.
8. **Gap honesty 3.** coding-standards.md requires every suite to state what it cannot
   prove; 4 of 111 backend files and 0 of 48 UI specs do. Repo-level disclosure
   (data-stack.md's cannot-prove table) is honest and specific — that is the 3. Against
   it: verified name-lies (`…Returns200OnDualAuthEndpoint` asserting 401; "shows error
   toast when getAddresses fails" re-testing deleteAddress verbatim; a loading-skeleton
   test asserting zero skeletons; a cancellation test documenting a guarantee that is
   false).

## The thesis

This suite is **bimodal, not mediocre**. Wherever a defect already burned the team —
payment idempotency, observability isolation, guest-session self-heal, upload bomb
guards — the tests are among the best this auditor has graded: real engines, real
seams, mutation-annotated asserts, written rationale. Everywhere else, a green is too
often decorative: status-code-only integration tests, class-field-only component specs,
tests named for behaviors their bodies do not touch. The failure mode is not sloppiness
— it is that **test quality only rises here after an incident**. The fix rounds should
export the incident-grade patterns to the un-burned 80%.

## Findings

TQ# is this target's finding ID. Severity is post-verification. 🔴 = the suite's green
lies about a guarded behavior; 🟠 = a real defect class unprotected or a suite-wide
erosion pattern.

### Criticals — all four survived adversarial verification

| TQ# | Sev | Finding | Verifier outcome |
|---|---|---|---|
| TQ1 | 🔴 | Payment-step spec's only behavioral test asserts its own mock exists (`expect(paymentService.createStripeIntent).toBeDefined()`, [payment-step.spec.ts:85](../../src/PhotoPrint.UI/src/app/features/checkout/pages/payment-step.spec.ts#L85)); async Stripe init never awaited; no `payment.service.spec.ts` exists; intent creation, error branch, `payWithStripe` covered by zero assertions | CONFIRMED 🔴 (3 independent finders, 3 independent confirmations) |
| TQ2 | 🔴 | Nothing applies the migration chain to Postgres and the chain is provider-inconsistent — prod boot `Migrate()` fails (uncastable `AlterColumn`s: timestamptz→INTEGER, bool→TEXT, decimal→TEXT in [20260521094335_AddUploadsTable.cs](../../src/PhotoPrint.API/Migrations/)) while all 1,029 tests stay green; only migration test is PostgreSQL ([UploadMigrationSchemaTests.cs:48](../../src/PhotoPrint.Tests/Unit/Data/UploadMigrationSchemaTests.cs#L48)) | CONFIRMED 🔴 — verifier corrected the failing statement but found earlier, more certain failures; sharpens data-stack.md's standing warning |
| TQ3 | 🔴 | Every rate-limit test runs against a limiter the test factory wrote: `PostConfigure<RateLimiterOptions>` overwrites `GlobalLimiter` in every host ([SecurityBaselineFactory.cs:71](../../src/PhotoPrint.Tests/Integration/SecurityBaselineFactory.cs#L71)); the production limiter could be deleted or set to 1,000,000 with CI green; the login brute-force policy has no test at all | CONFIRMED 🔴 |
| TQ4 | 🔴 | The `X-Correlation-Id` response header is asserted nowhere: the test named for it iterates a helper whose body is `yield break;` and asserts a flag its own delegate set ([CorrelationIdMiddlewareTests.cs:85,140](../../src/PhotoPrint.Tests/Unit/Middleware/CorrelationIdMiddlewareTests.cs#L85)); deleting the middleware's `OnStarting` block leaves the whole project green | CONFIRMED 🔴 |

### Verified majors (12)

| TQ# | Finding | Verifier note |
|---|---|---|
| TQ5 | No test anywhere presents a forged, expired, or foreign-issuer JWT; relaxing `ValidateLifetime`/`ValidateIssuer`/`ClockSkew` reddens nothing ([AuthExtensions.cs:60](../../src/PhotoPrint.API/Extensions/AuthExtensions.cs#L60)) | 🔴→🟠: breaking the key/issuer *values* would redden dozens of tests; only the relaxing direction is invisible |
| TQ6 | Persistence asserts read the same tracked `DbContext` the service mutated — blind to a deleted `SaveChanges` ([AdminOrderServiceTests.cs:252](../../src/PhotoPrint.Tests/Unit/Services/AdminOrderServiceTests.cs#L252), [UploadCleanupJobTests.cs:32](../../src/PhotoPrint.Tests/Unit/Services/UploadCleanupJobTests.cs#L32)) | CONFIRMED; pattern recurs in ~10 suites, `ChangeTracker.Clear()` count in tests: 0 |
| TQ7 | UploadFactory can only seed a healthy guest session, so guest-session death (expired/claimed → 401) is unprovable across uploads, cart, shipping, payments, orders ([UploadFactory.cs:129](../../src/PhotoPrint.Tests/Integration/UploadFactory.cs#L129)) | CONFIRMED; `!session.IsValid` deletable with CI green |
| TQ8 | Both GuestAuthHandler tests hit a Bearer-only endpoint, so neither invokes the handler; one is named `…Returns200…` and asserts 401 ([GuestSessionControllerIntegrationTests.cs:238](../../src/PhotoPrint.Tests/Integration/GuestSessionControllerIntegrationTests.cs#L238)) | 🔴→🟠: handler's happy path runs real elsewhere; fail branches proven nowhere |
| TQ9 | `CancelOrderAsync_StripeOrder_AttemptsStripeRefund` never verifies the refund; the Stripe branch is deletable — a customer's money silently not returned ([AdminOrderServiceTests.cs:663](../../src/PhotoPrint.Tests/Unit/Services/AdminOrderServiceTests.cs#L663)) | 🔴→🟠: single branch; EuPlatesc sibling does it right (`Times.Once`) |
| TQ10 | Post-payment cart clear + checkout reset asserted in neither caller; both stubs installed, neither checked ([confirmation-page.spec.ts:47](../../src/PhotoPrint.UI/src/app/features/orders/pages/confirmation-page.spec.ts#L47)) | 🔴→🟠: service logic itself proven in cart.service.spec; the two-line wiring is what is unprotected |
| TQ11 | `OrderService.DetachFailedInsert` exercised by four tests, asserted by none — a no-op version 500s on recovery replay in prod ([OrderServiceIdempotencyConcurrencyTests.cs:204](../../src/PhotoPrint.Tests/Unit/Services/OrderServiceIdempotencyConcurrencyTests.cs#L204)) | CONFIRMED; scoped-context chain to PaymentsController.cs:159 verified |
| TQ12 | Admin catalog visibility writes (`IsActive`) asserted by status code only; inverting the assignment keeps the suite green ([AdminProductCatalogIntegrationTests.cs:123](../../src/PhotoPrint.Tests/Integration/AdminProductCatalogIntegrationTests.cs#L123)) | 🔴→🟠: read-side filter is proven elsewhere; the admin write effect is not |
| TQ13 | Admin order search has zero tests, and the production predicate mixes normalized/raw needles — a live bug ([AdminOrderServiceTests.cs:840](../../src/PhotoPrint.Tests/Unit/Services/AdminOrderServiceTests.cs#L840), [AdminOrderService.cs:73](../../src/PhotoPrint.API/Services/AdminOrderService.cs#L73)) | CONFIRMED, defect live: order numbers are always upper-case, so `ft-2026-0001` or padded input finds nothing |
| TQ14 | `MergeCarts_ConflictingUploadId_UserItemWins` arranges no conflict; the merge guard is deletable; body comments admit it ([CartServiceTests.cs:291](../../src/PhotoPrint.Tests/Unit/Services/CartServiceTests.cs#L291)) | 🔴→🟠: conflicting state hard to reach via API; still an untested guard behind a lying name in the most re-found cluster |
| TQ15 | The only unhappy-path disk-health test is a tautology (`BeOneOf(Healthy, Unhealthy)`) and on the CI OS never reaches the failure branch ([DiskHealthCheckTests.cs:64](../../src/PhotoPrint.Tests/Unit/HealthChecks/DiskHealthCheckTests.cs#L64)) | 🔴→🟠: peripheral diagnostic; still a can't-fail test |
| TQ16 | UploadCleanupJob soft-delete persistence unfalsifiable via shared singleton DbContext ([UploadCleanupJobTests.cs:32](../../src/PhotoPrint.Tests/Unit/Services/UploadCleanupJobTests.cs#L32)) | 🔴→🟠: eligibility surface well proven; only the commit round-trip is blind (same class as TQ6) |

### Verified minors (2)

- TQ17 🟡 Claim endpoint never asserts *which* user the session was bound to; only the
  jti-vs-sub misread is invisible ([GuestSessionControllerIntegrationTests.cs:162](../../src/PhotoPrint.Tests/Integration/GuestSessionControllerIntegrationTests.cs#L162)).
- TQ18 🟡 Cross-tenant idempotency asserted 200 on InMemory and 409 on PostgreSQL for the
  identical sequence; the 200 is a provider artifact the file does not label
  ([PaymentControllerIntegrationTests.cs:108](../../src/PhotoPrint.Tests/Integration/PaymentControllerIntegrationTests.cs#L108)).

### Unverified majors — the clustered tail (~100 raised, not individually verified)

Verification downgraded 10 of 20 top findings, so expect a similar deflation rate here
(realistically ~half hold at 🟠). Clusters, with representative examples:

- **Status-code-only / DB-effect-unasserted** (~15): half the integration suite (106/213
  tests) stops at a status code; ReplaceTiers never proves old tiers removed; pagination
  untested; LockerDto projection unasserted (a Lat/Lng swap ships green).
- **Production-only code paths with zero coverage** (~12): Npgsql arms of the
  idempotency/order-number violation classifiers; OrderNumberService's Postgres sequence
  branch; the `ILike` city search; check constraints; 8 of 9 unique indexes.
- **Security/auth gaps** (~8): refresh-token cookie attributes asserted nowhere; rotated
  refresh token never replayed; auth rate-limit policy untested; admin-role guard on AWB
  creation unproven; five JWT builders minting `role="User"` which `UserRole` does not
  define.
- **UI error paths & DOM blindness** (~25): twelve component specs assert only class
  fields (all auth pages, all admin pages, cookie banner — no template regression can
  fail them); no spec ever calls `fixture.destroy()`; admin dashboard's four error
  handlers untested; reset-password suite stubs AuthService as a bare `vi.fn()` with no
  other coverage of `resetPassword`.
- **Email subsystem** (~8): both sender adapters seamless and untested;
  `FireOrderCancelledEmail` tested by nothing; EmailRetryJob tested via reflection on a
  private method; nothing proves only `Pending` rows are picked up.
- **Determinism** (4 flake-prone sites): 9× `Task.Delay(200)` fire-and-forget waits; the
  350ms-vs-300ms debounce sleep; EmailRetryJob's wall-clock coupling; the RateLimit host
  leak that manufactures background load.
- **Structural blindness by construction** (~10): cart tier pricing untestable (every
  seed creates exactly one open-ended tier); only-one-default-address invariant never
  exercised with a second address; the interceptor chain composed in the app is never
  tested composed.

The full one-line list is preserved in the audit archive (see Cost & provenance).

### Minors and notes

186 filed (🟡/⚪). Dominant themes: 25 UI construct-only smoke tests, paired-presence
tests that never assert the opposite element's absence, Romanian-label tests that prove
only non-emptiness, name-vs-body mismatches below major, cleanup placed inside test
bodies. Not enumerated here; they ride along with the clusters above during fixes.

## What the good half looks like (protect these)

14 files marked exemplary by deep readers; the patterns worth exporting:

- [PaymentIdempotencyRelationalTests.cs](../../src/PhotoPrint.Tests/Integration/PaymentIdempotencyRelationalTests.cs) — real unique index, asserts status + row count + row ownership, states why it exists.
- [DashboardMetricNamesTests.cs](../../src/PhotoPrint.Tests/Integration/DashboardMetricNamesTests.cs) — holds every dashboard/SLO query against a real scraped exposition, with an explicit allow-list for the two labels it cannot verify.
- [CloudPreviewIntegrationTests.cs](../../src/PhotoPrint.Tests/Integration/CloudPreviewIntegrationTests.cs) — each assert annotated with the mutation that reddens it.
- [error.interceptor.spec.ts:89](../../src/PhotoPrint.UI/src/app/core/interceptors/error.interceptor.spec.ts#L89) / [guest-auth.service.spec.ts:105](../../src/PhotoPrint.UI/src/app/core/services/guest-auth.service.spec.ts#L105) — deliberate real-seam tests written because two halves had each been proven with the other mocked.
- [TestHostConfigurationIsolationTests.cs](../../src/PhotoPrint.Tests/Integration/TestHostConfigurationIsolationTests.cs) + [MetricCapture.cs](../../src/PhotoPrint.Tests/Helpers/MetricCapture.cs) — isolation that is not just designed but *proven*, with its own test.
- [WebhooksControllerMetricsTests.cs](../../src/PhotoPrint.Tests/Unit/Controllers/WebhooksControllerMetricsTests.cs) — signs IPN fixtures with the real HMAC instead of stubbing signature validation.

## Side-findings: product bugs surfaced by the audit

1. **Admin order search is broken for realistic input** (live, verified):
   [AdminOrderService.cs:73-77](../../src/PhotoPrint.API/Services/AdminOrderService.cs#L73) normalizes
   the needle for email matching but matches `OrderNumber` with the raw string; order
   numbers are always upper-case, so lower-cased or whitespace-padded searches return
   nothing, on every provider.
2. **Reset-password failures show the user nothing** (candidate): the endpoint returns
   401 for a bad/expired token; the page handles 400
   ([reset-password-page.spec.ts:95](../../src/PhotoPrint.UI/src/app/features/auth/pages/reset-password/reset-password-page.spec.ts#L95) asserts the 400 branch the server never takes).
3. **Prod-boot migration failure** (TQ2) — already flagged in data-stack.md as a risk;
   this audit upgrades it from "likely type mismatch at first query" to "Migrate() fails
   on uncastable ALTERs", still unexecuted (read-only audit, no Postgres available).
4. **Missing shipping-cost config key silently prices shipping at 0 RON**
   ([ShippingServiceTests.cs:25](../../src/PhotoPrint.Tests/Unit/Services/ShippingServiceTests.cs#L25) context) — a defect-shaped default with no covering test.

## Reasons to doubt this review

- **No mutation was executed.** Every "deletable with CI green" claim is static
  inference. Each was independently re-derived by a second agent reading the same code,
  but a compile-and-run check remains the gold standard and was out of scope (read-only
  by owner decision).
- **The unverified tail is inflated.** The verified sample deflated 50% of findings by
  one rung (10/20), and 0/20 were refuted — expect the ~100 unverified majors to contain
  both duplicates (several appear twice from different lenses) and overstatements.
- **Counts are grep-derived.** "106 of 213 status-code-only", "4 of 111 state gaps",
  mock inventories — all pattern counts, honest but approximate.
- **Zero refutations could mean deferential verifiers.** The 10 downgrades with detailed
  counter-evidence argue against pure deference, but the skeptics were spawned from the
  same model family as the finders.
- **Triage risk-scores were lenient** (only 6 of 180 files scored ≥3), so deep-read
  selection leaned on known-risk clusters and file size; some weak files may have
  escaped deep reading entirely.

## Ranked recommendations

Ordered by (risk retired ÷ effort). R1–R4 retire the criticals.

1. **R1 — Unshadow Postgres.** Point the factories at CI's postgres:16 when the env var
   is present (`PostgresPaymentFactory`-style opt-in), and add one migration-chain test on
   Npgsql. This retires TQ2, the Npgsql classifier arms, the sequence path, and starts
   paying down dimension 3. The container already runs; this is config, not
   infrastructure.
2. **R2 — Rewrite the payment-step spec + add payment.service.spec.ts.** Mock
   `@stripe/stripe-js` at module level (`vi.mock`), await the init, assert
   `createStripeIntent` was *called* with the right request, drive the error branch and
   `payWithStripe`. Retires TQ1.
3. **R3 — Let the production rate limiter run.** Use `builder.UseSetting` (the repo
   already does this exact trick in two factories) instead of `PostConfigure`-replacing
   `GlobalLimiter`; add one test for the `auth` policy. Retires TQ3, plus the brute-force
   gap.
4. **R4 — Assert the correlation header on a real response** through any existing
   factory (one test), and delete the `yield break` helper. Retires TQ4.
5. **R5 — Kill the tracked-read-back class.** One helper (`AssertViaFreshContext` /
   `ChangeTracker.Clear()`) applied across the ~10 job/service suites. Retires TQ6/TQ16
   and the AccountService instance.
6. **R6 — Parameterize `SeedGuestTokenAsync(expiresAt, claimedByUserId)`** and add one
   expired-guest 401 test on a real DualAuth endpoint; repoint the two handler tests.
   Retires TQ7/TQ8 in the repo's most re-found cluster.
7. **R7 — Replace the 14 wall-clock sleeps**: `TaskCompletionSource` for fire-and-forget
   waits, `vi.useFakeTimers` for the debounce, and dispose the RateLimit hosts. Retires
   the flake surface on the machine that CLAUDE.md says saturates.
8. **R8 — Body/DB-effect asserts for the top status-code-only integration tests** —
   start with the ~15 admin/write endpoints where the DB effect is asserted nowhere
   (TQ12 cluster), not all 106.
9. **R9 — One JWT-negative test file**: expired, wrong issuer, wrong key, wrong
   audience, using the existing token builders (three lines each). Retires TQ5.
10. **R10 — UI hygiene floor**: a shared Vitest setup file (storage cleanup before
    construction, teardown, `httpMock.verify()`), plus DOM-level asserts for the twelve
    class-field-only specs, starting with the auth pages.

Explicitly cheap and high-value outside the list: fix the two lying test names
(TQ8, saved-addresses duplicate), and the two live product bugs in side-findings.

## Re-grading protocol

After a fix round: re-run the same 8 dimensions against the same anchors; a dimension
moves only on evidence of the class being retired (not individual instances). The
scorecard above stays as v1 baseline; append, never rewrite. Suggested targets for a
first fix round: dim 3 → 5 (R1), dim 1 → 5 (R2/R4/R8), dim 5 → 5 (R5), dim 6 → 7 (R7).

## Cost & provenance

~4.6M subagent tokens (55 agents: 13 Sonnet triage, 7 lenses, 15 Opus deep-readers, 20
Opus verifiers), plus ~0.4M lost to two interrupted runs earlier. Raw agent returns:
workflow run `wf_e3996b04-4de`, result archive in the session task output
(`wfbmnbhei.output`); per-agent journal in the session transcript directory. Findings
digests (full one-line major/minor lists) were generated from that archive.
