---
type: review
target: 015-sameday-shipping
version: 6
supersedes: 5
commit: 1816f5f
branch: feat/bolt-036-sameday-api-client
pass-type: verification
date: 2026-07-29
reviewer: anchored verification (independent session; revert-and-rerun + fix-diff inspection)
verdict: approve-with-followups
findings: { verified: 37, reopened: 4, deferrals-upheld: 3, wont-fix-upheld: 2, false-positive-upheld: 2, new: 7 }
tests: { dotnet: "916/916 (+10 skipped MinIO)", frontend: "457/457" }
---

# Review v6 — 015-sameday-shipping (verification of the v5 post-certification fix round)

Anchored verification of [resolution-v5.md](resolution-v5.md) at code tip `1816f5f`
(baseline `5fc330b`). Independent session — nothing in the fixer's own "no regressions" claim was
taken on trust; every statement below rests on a run I made. Capped at `approve-with-followups`:
verification says "these fixes held", never "the feature is clean".

## Result

**33 of the 37 behavioral/coverage fixes are non-vacuous. 4 fixes are reopened — not because the
code is wrong, but because nothing can prove it: removing each one leaves both suites fully green.**
Plus 7 new findings (1 Low, 6 Cleanup), no regressions, and all 7 non-`fixed` dispositions upheld.

- **Reopened: D27, D39, D71, D79.** Each re-arms the loop → next pass is a fix round.
- **0 fix-caused regressions.** No revert reddened a test outside its own fix's scope.
- **0 new 🔴/🟠.**

## Revert-and-rerun — the mechanical core

Each row: the fix was reverted in source only, the whole suite run, the tree restored. Predicted
failure sets were written down before each run and matched exactly, which establishes both
non-vacuity (the test *can* go red) and zero collateral (nothing else went red) in one shot.

### Backend — 4 batches, 19 predicted reds, 19 observed, 0 unpredicted

| D# | Bug re-introduced | Red test |
|----|-------------------|----------|
| D55 | drop the Easybox address length caps | `Easybox_WithOversizedAddressField_FailsOnThatField` |
| D61 | drop the `HasEnoughDigits` phone rule | `WithDigitPoorPhone_FailsOnPhone` (both cases) |
| D56 | null the label-url / shipped / delivered projection | `GetOrderDetailAsync_surfaces_the_label_url_and_tracking_timestamps` |
| D24 | restore the fabricated `?? UtcNow` observedAt | `GetTrackingAsync_leaves_observedAt_null_when_the_vendor_omits_it` |
| D57 | drop the stale-claim TTL clause from the claim CAS | `Reclaims_a_stale_claim_and_creates_the_AWB` |
| D60 | drop the over-length label-url clamp | `Persists_the_AWB_but_drops_an_over_length_label_url` |
| D63 | drop the Auth/Protocol catches before the base catch | `A_systemic_auth_failure_does_not_fault_the_tick_and_is_marked_once` |
| D67 | restore the full-interval poll floor | `Polls_an_order_synced_a_full_interval_ago` |
| D58 | no-op the claim release | `Releases_the_claim_after_a_definitive_failure` + `Returns_RetryLater_transient_on_SamedayUnreachableException` |
| D77a | drop the sweep's claim clause | `Skips_orders_with_a_fresh_claim` |
| D77b | narrow the clause to `AwbClaimedAt == null` | `Enqueues_orders_whose_claim_is_stale` |
| D76 | poll only the first in-window id | `Polls_multiple_in_window_orders_in_one_tick` |
| D50 / D82 | re-enqueue without `Attempt + 1` | `DelayedReEnqueueAsync_re_enqueues_the_next_attempt_after_the_delay` |
| D68 | `PreserveClaim: false` on timeout and on retryable-status | `Preserves_the_claim_on_a_vendor_timeout` + `Preserves_the_claim_on_a_retryable_status_from_the_create_call` |
| D68 / D82 | drop the claim-TTL floor from `ComputeReEnqueueDelay` | `ComputeReEnqueueDelay_floors_a_preserve_claim_outcome_past_the_claim_TTL` |
| D65 | ctor-inject `ISamedayTokenProvider` into the auth handler | `Enabled_root_resolves_client_creator_and_jobs_without_a_DI_cycle` |

### Frontend — 6 predicted reds, 6 observed

| D# | Bug re-introduced | Red test |
|----|-------------------|----------|
| D64 | drop `selectedLockerId.set(null)` from `selectMethod` | `switching Easybox → Courier → Easybox clears the stale locker selection` |
| D69 | drop the `phoneDigits` validator from the contact form | `a digit-poor Easybox phone keeps Continue disabled` |
| D80 | drop `lockerSearchError.set(true)` from the search `catchError` | `a failed locker search surfaces a distinct error, then recovers` |
| D59 / D74 | make `getStoredSession()` return null | `prefills the Easybox contact from a stored guest session` |
| D78a | make `setLocker` clear `shippingAddress` | `setLocker preserves an already-entered Easybox contact` |
| D35 | prime the locker list unconditionally on init | `does not fetch lockers on init for a courier-only user` (+16 siblings, see below) |

Two rows carry collateral that is **inherent, not fix-caused**:

- **D35** reddens 16 further `delivery-step.spec.ts` tests. Lazy priming changed the spec helper's
  contract (`flushCosts()` no longer flushes a locker request), so every test in the file asserts
  the no-init-fetch invariant through `http.verify()`. Over-pinned, not mis-pinned.
- **D78b** (`does not render a street-address line for an Easybox order`) only reddens when the
  template is broken hard enough to throw — and then 5 sibling tests fail with it. See the D78
  caveat below.

### D65 — the DI cycle is real, and the test is load-bearing

The most valuable single result of this pass. Reverting the lazy token-provider resolution
reproduces an actual boot failure on the `Sameday:Enabled=true` path:

```
System.InvalidOperationException: ValueFactory attempted to access the Value property of this instance.
  at Microsoft.Extensions.Http.DefaultHttpClientFactory.CreateHandler(String name)
  at Microsoft.Extensions.Http.DefaultHttpClientFactory.CreateClient(String name)
```

The fixer's approach-check conclusion ("confirmed a real cycle") is independently confirmed, and
`SamedayCompositionRootTests` catches it. Had the flag been flipped in staging on the pre-fix code,
the first Sameday request would have thrown. **This finding alone justified the fix round.**

## Reopened — 4 fixes nothing can prove

Method: I removed each fix entirely and ran **both** suites. Every one stayed fully green
(916/916 · 457/457). Per [runbook step 2](../runbook-verification.md), a fix whose test cannot go
red is not verified. In all four cases the code reads correct on inspection — what is missing is the
test that would catch its deletion, which is exactly what this loop refuses to take on faith.

| D# | Fix that vanished silently | Why it matters |
|----|---------------------------|----------------|
| **D27** | the monotonic guard on the non-delivered `LastTrackingSyncAt` write (`Status = Shipped AND stamp < now`) | The two tracking tests assert `LastTrackingSyncAt == T0`, which passes with or without the guard. A cross-replica ordering fix with no ordering test. |
| **D71** | the explicit `DelayGenerator` producing 1 / 4 / 16 s | Reverting to Polly's base-2 `Exponential` (**the exact defect the finding described**) is invisible: the only retry test scripts a single retry, whose delay is 1 s under both schedules. |
| **D79** | the server `name` claim **and** the client `currentUser$` population | The finding was "signed-in prefill is dead code — `currentUser$` never emits". The fix makes it emit; nothing asserts that it does. This is the guest/signed-in prefill cluster — CLAUDE.md defect class 11, this repo's most re-found cluster. |
| **D39** | `services.AddScoped<StaticShippingService>()` | The fix traded a compile-safe `new` for a DI-resolution dependency. Delete the registration and the suite is green, but `IShippingService` would throw at the first request on the enabled path. `SamedayCompositionRootTests` resolves the client, the creator and the 3 hosted services — **not** `IShippingService`. |

D39's fix is one line in the same test: add `sp.GetRequiredService<IShippingService>()`. D27/D71/D79
each need one small assertion.

## Recorded gaps on fixes that did hold

- **D68 — the persist-fail leg is untested.** Setting `PreserveClaim: false` on the post-create
  persist-failure path reddens nothing; the timeout and retryable-status legs both redden. The
  mechanism is pinned, one branch of it is not. Not a reopen — `PreserveClaim` itself goes red.
- **D78 — the review-step assertion does not pin what it claims.** `expect(summary.textContent).not.toContain('undefined')`
  cannot fail in Angular: `{{ undefined }}` renders as an empty string, never the literal text.
  I removed the `method === 'Courier'` gate using safe navigation (the realistic drift) and **nothing
  went red**; only a hard non-null-assertion crash reddens it, and then 5 siblings fail too. D78's
  other half (`setLocker` contact preservation) is properly non-vacuous.

## Fixes verified by inspection (no behavioral assertion available)

Log-only, pure-refactor, or DI/config hardening, where the review asked for no test and there is no
observable behavior to regress: **D62, D66, D73, D29, D25, D20, D70, D30, D37, D38, D75, D84, D85,
D86, D87**. Each read in the fix diff and confirmed to do what its resolution note claims.

The line I drew between this list and the reopens: a fix is reopened when a silent revert
reintroduces the exact defect the finding described **on the default path**. D25 (UTC normalisation)
only bites on a non-UTC host; D20/D70 remove a knob or a cap without producing a defect. D27/D71/D79/D39
all reintroduce the original defect on the default path.

## The three cluster questions (runbook step 4)

**Durable claim / `PreserveClaim` (D68, D57, D58, D82, D50) — clean.**
*Class or instance:* I checked every sibling that could leave a billable AWB. `SamedayProtocolException`
and a JSON failure on a **2xx** response can also mean the vendor billed an AWB, and they carry no
`PreserveClaim` — but they return `IsTransient: false`, so the dispatcher never re-enqueues in-process
and the next attempt is the ≤60 min retry sweep, far past the 5 min claim TTL. The hazard `PreserveClaim`
exists to stop ("re-call in ~30 s") cannot arise there. The `PreserveClaim` set is therefore exactly
{transient outcomes that may have billed} — complete. The fixer's judgement holds.
*New surface at the bar:* sized default (`false`), a signal (`sameday.awb.claim-held`), failure-mode
tests on 2 of 3 legs, docs on `AwbCreationOutcome`. *Regression:* the `HandleOutcomeAsync`
`async Task`→`Task`/`break` rewrite and the `Task.Run`→direct-await change are behaviour-preserving;
`Task.Delay(delay, _clock, ct)` is `TimeProvider.System` in production.

**Label-url clamp + migration (D60) — holds, with unclamped siblings.**
`dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model since the
last migration": snapshot and model agree. The original migration created `character varying(500)`,
so the new `oldType` is accurate and the alter chain is consistent. *Class:* `AwbNumber`
(`varchar(100)`) is written on the same post-bill persist and is **not** clamped (NF3); the `Created`
outcome still carries the unclamped url while the row stores null (NF4). *Not exercised against real
Postgres* — the standing D23 deferral.

**Tracking cadence + monotonic write (D67 / D27) — correct, under-tested.**
The tick-start `now` is threaded into `PollOneAsync` and used consistently for `DeliveredAt`,
`LastTrackingSyncAt` and `UpdatedAt`. An order eligible under `stamp < now − (interval − 30 s)`
always satisfies the write guard's `stamp < now`, so D27 never blocks a legitimate write — no
regression. The flat 30 s buffer is half the interval at the validator's minimum
`TrackingIntervalMinutes = 1` (NF6, harmless: the D27 guard covers it).

**DI composition (D65 / D39) — cycle broken; one pre-existing lifetime smell.**
I diffed the old `Program.cs` block against `AddSamedayIntegration` line by line: identical apart
from the buffer cap and the `StaticShippingService` registration, same call position, no ordering
change. *Class:* `ISamedayAuthenticator` is a **singleton** factory resolving the **transient** typed
`ISamedayClient`, so one client instance is captured for process life and its handler is never
rotated (NF1) — carried verbatim from the old `Program.cs`, not introduced here.

**`SamedayClient` status-ladder dedup (D75) — complete.**
All 4 ladders route through `EnsureSuccessOrThrowAsync`; the label path still returns an undisposed
response on success and disposes on every throw. One deliberate behaviour change: the shared
`IsRetryableStatus` bounds 5xx at `< 600`, where the old inline ladders used unbounded `>= 500`, so a
nonstandard 6xx is now a permanent GiveUp instead of a retry. The finding named that drift and the
fix chose the bounded version on purpose.

## New findings (7) — all backlog, none re-arms the loop

| ID | Sev | Finding | Site |
|----|-----|---------|------|
| D90 | 🟡 Low | `ISamedayAuthenticator` singleton captures the transient typed `ISamedayClient` → the `IHttpClientFactory` handler is never rotated (stale DNS / socket on a long-lived process). Pre-existing; carried into the new extension | `Extensions/SamedayServiceCollectionExtensions.cs:37` |
| D91 | ⚪ | `ISamedayClient` doc still claims the AWB/label/tracking methods throw `NotImplementedException` "until bolt 037" — stale. The identical claim was stripped from `SamedayClient.cs` this round; the interface twin was missed | `Services/Sameday/ISamedayClient.cs:8` |
| D92 | ⚪ | `AwbNumber` (`varchar(100)`) is the unclamped sibling of D60's clamp on the same post-bill persist | `Services/Sameday/AwbCreator.cs:190` |
| D93 | ⚪ | `AwbCreationOutcome.Created` reports the unclamped `result.LabelUrl` while the row stores null — outcome and row disagree (no consumer reads it today) | `Services/Sameday/AwbCreator.cs:207` |
| D94 | ⚪ | D20's `MaxRequestsPerSecond` is absent from `appsettings.json`, from `SamedaySettingsValidator`, and from bolt-037 ddd-02's settings block — an operator cannot discover it | `Configuration/SamedaySettings.cs:52` |
| D95 | ⚪ | D67's 30 s poll buffer is a flat inline constant, not scaled to the interval | `BackgroundJobs/ShipmentTrackingJob.cs:77` |
| D96 | ⚪ | Record accuracy: resolution-v5 and index.md report "backend 914" (tip measures **916**) and count "fixed: 30 … 9 folded backlog" where the frontmatter holds **41** `fixed` entries (11 folded); index.md cites `66c6d50` as the fix commit rather than the tip `1816f5f` | `reviews/015-sameday-shipping/resolution-v5.md:75` |

## Non-`fixed` dispositions — all upheld

Per [runbook step 3](../runbook-verification.md), each cited file was diffed against the
last-affirmed commit `5fc330b` first; unchanged files needed no agent.

| D# | Disposition | Verification |
|----|-------------|--------------|
| D72 | deferred | `20260602190046_*` unchanged since `5fc330b`. No deployed data exists, so there are no legacy Shipped rows to backfill; the admin transition stamps `ShippedAt` going forward. **Agree** — a data-migration for zero rows is churn. Belongs in the deploy runbook. |
| D81 | deferred | `SamedaySettingsValidator.cs` unchanged since `5fc330b`; `LockerServiceId`/`CourierServiceId` still default to placeholder `7`. **Agree** — the feature is dormant and configuring real ids is the existing pre-enable step. Note it is now the *second* unvalidated Sameday setting (with D94). |
| D40 | deferred | The two June designers and the model snapshot unchanged since `5fc330b`. **Agree** — pre-existing bolt-035 drift, Stripe secrets are ~66 chars, harmless; a bolt-035 groom item. |
| D83 | wont-fix | The bundled locker-map UX is now covered by delivery-step specs (I reddened three of them). **Agree** — a retro-story would add nothing. |
| D89 | wont-fix | `GetLabelPdfAsync` still has **no production caller** (interface, impl, one unit test). **Agree with a caveat**: the rationale rests on a "pre-enable admin label-proxy endpoint" that is not tracked on any checklist. If that endpoint is never built the method stays dead — record it as a pre-enable item or the wont-fix becomes stale. |
| D88 | false-positive | Npgsql maps any-offset `DateTimeOffset` to the UTC instant (the Kind restriction is `DateTime`-only), and both `DeliveredAt` operands are `DateTimeOffset`. **Agree** — premise false. |
| D33 | false-positive | Confirmed by reading the current tick: it selects ids only under `AsNoTracking`, each `PollOneAsync` loads its order once on its own scope, and **no `inWindow` variable exists** — only `inWindowIds`, which is used. **Agree** — obsolete. |

## Build, tests, and the comment rule

At the restored tip `1816f5f`, run by me:

- **.NET** `916/916` passed, 10 skipped (MinIO — no `STORAGE_TEST_*` env). **Frontend (Vitest)**
  `457/457` across 48 files.
- The expected 916 matches; **resolution-v5's "914" is stale** (D96) — it predates the two tests
  `1816f5f` added.
- **CLAUDE.md comment rule: clean.** Scanning every added (`+`) line of
  `git diff 5fc330b 1816f5f -- 'src/**'` for `ADR-`, `FR-<n>`, `bolt`, `D<n>`, `BUG-`, `SEC-`,
  `review <n>` returns **0 hits**; the same pattern hits **9** removed lines, so the scan works and
  the fixer genuinely stripped them. Pre-existing citations survive in files this diff did not touch
  (`AwbCreationRequest.cs`, `IAwbJobQueue.cs`, `ISamedayTokenProvider.cs`) and one it did
  (`SamedayAuthHandler.cs:12`, an untouched class-doc line citing ADR-014) — flagged as part of D91's
  cluster, not a rule breach in this diff.

## Verdict: `approve-with-followups`

The mechanism work of this round is sound and, where tested, genuinely pinned — the DI-cycle break
in particular prevented a real staging boot failure. But 4 fixes are invisible to the suite, and one
of them (D79) sits in the repo's most re-found defect cluster. The round is not closed.

### Escalation call: a fresh CERTIFICATION pass IS warranted — after a fix round

Two independent reasons, either sufficient:

1. **Full-loop-tier touches.** [README entry tiers](../README.md#entry-tiers--does-a-change-get-the-loop-at-all)
   put concurrency, migrations, and money paths in the full loop. This round touched all of them:
   the durable-claim/`PreserveClaim` concurrency mechanism, `AlterAwbLabelUrlLength`, the billable
   AWB persist path, and the whole DI composition root. README note ² says a backlogged minor fixed
   later needs only verification **"unless the fix touches full-loop-tier code"** — it does, so the
   exemption does not apply.
2. **4 reopened fixes re-arm the loop** ([README](../README.md#what-re-arms-the-loop--exactly-three-things)),
   which resets the quiet counter regardless of tier.

Sequence: **fix round (D27/D39/D71/D79 tests) → verification → certification.** The certification
should be a single fresh full-manifest pass, not a blinded pair: the v5 pass ran one day ago on
near-identical code, and this round's delta is ~2.1k lines against it. The v5 `CERTIFIED` outcome
should be read as **provisional** until that pass runs — it certified `5fc330b`, and 41 fixes have
landed since.

### Follow-ups (not blockers)

1. **D45 vendor-idempotency residual** — unchanged and still accepted. Verify Sameday's
   create-idempotency on `ClientInternalReference` before flipping either flag (ADR-015).
2. **D23** — migrations and the `timestamptz` CAS are still never exercised against Postgres. This
   round *added* a migration, so the deferral now covers more untested DDL.
3. **D89's premise** — track the admin label-proxy endpoint, or the wont-fix goes stale.
4. **Backlog:** D72, D81, D40, D83, D89 upheld; D90–D96 added.
5. Still dormant behind `Sameday:Enabled=false` + `Sameday:Jobs:Enabled=false`. Nothing in this
   pass is reachable in production today.
