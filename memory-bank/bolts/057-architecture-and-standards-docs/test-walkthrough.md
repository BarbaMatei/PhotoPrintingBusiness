---
stage: test
bolt: 057-architecture-and-standards-docs
created: 2026-09-04T02:10:00Z
---

## Test Report: architecture-and-standards-docs

### Summary

- **Tests**: none written, none run. This bolt changes no code, and the kickoff scopes it that
  way explicitly: verification is reading the manifests and the running configuration in this
  worktree. A full suite run would also have saturated a machine shared with three other
  wave-1 sessions.
- **What replaced them**: every claim in the four documents traces to a command, and each was
  run in this worktree at `origin/main` = `182cd50`. The table below is the record. Two fresh
  subagents independently re-ran the checkable claims — the stage-2 adversarial design check
  and the stage-4 fresh-eyes micro-review — and the corrections they forced are listed at the
  bottom.

### Test Files

None. There is no test that can fail when a document is wrong; the equivalent is the quarterly
checklist this bolt ships, whose section 5 exists precisely to re-run these checks.

### Verification record

| Claim | Command | Result |
|---|---|---|
| Angular 21.2, TypeScript 5.9, RxJS 7.8, Vitest 4, jsdom 28 | `cat src/PhotoPrint.UI/package.json` | ✅ exact |
| No `heic2any`, no `ng2-charts`, no ESLint, no e2e framework | same, plus `ls src/PhotoPrint.UI` | ✅ absent — no config, no `lint` script |
| `chart.js` used directly; `leaflet`, `@stripe/stripe-js`, `@microsoft/signalr` v10 present | same | ✅ exact |
| Tests run on Vitest via `@angular/build:unit-test` | `grep -n '"test"' -A2 src/PhotoPrint.UI/angular.json` | ✅ builder confirmed |
| 27 direct NuGet references in the API, 13 in the test project | `grep -c 'PackageReference Include' src/PhotoPrint.API/PhotoPrint.API.csproj src/PhotoPrint.Tests/PhotoPrint.Tests.csproj` | ✅ 27 / 13 |
| OpenTelemetry (8 packages), Prometheus exporter, Sentry, QuestPDF, `Polly.RateLimiting`, `FluentValidation.AspNetCore` all installed and previously undocumented | `cat` both `.csproj`; `git show 0c6938c -- memory-bank/standards/tech-stack.md` | ✅ installed; ✅ the only prior edit to the doc was the Postgres-provider correction, so none of these was ever documented |
| Two OpenTelemetry packages are pre-release out of necessity | `cat` the API csproj | ✅ `Prometheus.AspNetCore` and `Instrumentation.EntityFrameworkCore` are `-beta.1` |
| `Polly.RateLimiting` is for outbound courier calls | `grep -n 'RateLimit' src/PhotoPrint.API/Services/Sameday/SamedayPolicies.cs` | ✅ `SlidingWindowRateLimiter` in the Sameday pipeline |
| `Email:Provider` is required config, not an environment inference | `sed -n '1,60p' src/PhotoPrint.API/Extensions/EmailExtensions.cs` | ✅ throws on missing, on unknown, and on `SendGrid` without an API key; exactly one sender registered |
| The shipped provider defaults | `grep -A4 '"Email"' src/PhotoPrint.API/appsettings.json src/PhotoPrint.API/appsettings.Development.json` | ✅ base `SendGrid`, Development `Smtp` |
| Romanian invoice rendering needs csproj + env + ICU data to agree | `cat src/PhotoPrint.API/PhotoPrint.API.csproj`; `sed -n '25,48p' Dockerfile` | ⚠️ **corrected** — the csproj comment's claim that it overrides an invariant-`true` base image is wrong; the Dockerfile sets the switch to `false` and installs `icu-libs icu-data-full`, and the base image ships no ICU at all |
| No central package management today | `ls Directory.Packages.props Directory.Build.props` | ✅ neither exists |
| CI runs .NET build+test with MinIO and postgres:16-alpine, UI Vitest+build on node 22, no lint | `cat .github/workflows/ci.yml` | ✅ exact |
| `deploy.yml` is reachable only by manual dispatch | `sed -n '1,14p' .github/workflows/deploy.yml` against `ci.yml`'s triggers | ⚠️ **corrected** — `workflow_run` is filtered to `branches: [main]` while `ci.yml` uses `push: branches-ignore: [main]`, so the chain never fires. Escalated; the workflow is off-limits to this bolt |
| Schema is migrated unconditionally at boot | `grep -n 'Database.Migrate' src/PhotoPrint.API/Program.cs` | ✅ `Program.cs:325`, no lock |
| Fourteen hosted services; thirteen via `AddHostedService` | `grep -rn 'AddHostedService' src/PhotoPrint.API --include=*.cs`; `grep -n 'IHostedService' src/PhotoPrint.API/Extensions/ObservabilityExtensions.cs` | ✅ 13 + `ScrapeListenerGuard` as a singleton `IHostedService` |
| Only the AWB and ANAF paths claim rows; `ShipmentTrackingJob` guards only its write | `grep -rln 'ExecuteUpdateAsync' src/PhotoPrint.API --include=*.cs`; `grep -n 'ClaimedAt' …/AwbCreator.cs …/InvoiceUploadJob.cs`; `grep -n 'ClaimedAt' …/ShipmentTrackingJob.cs` | ✅ `Orders.AwbClaimedAt` and `Invoices.ClaimedAt` exist; tracking has no claim column |
| `EmailRetryJob` and `AccountDeletionJob` take no claim | `grep -n 'Where(' src/PhotoPrint.API/BackgroundJobs/EmailRetryJob.cs …/AccountDeletionJob.cs` | ✅ plain selects; no `ExecuteUpdateAsync` in either file |
| `PromotionRecoveryScanner` sweeps at boot **and** every 6 h | `grep -n 'PeriodicTimer' src/PhotoPrint.API/BackgroundJobs/PromotionRecoveryScanner.cs`; `grep -n 'PromotionRecoverySweepIntervalHours' src/PhotoPrint.API/Configuration/OrderPhotoArchiveSettings.cs` | ⚠️ **corrected** — `system-architecture.md` said "boot only" and this bolt had copied it; default is 6 hours. Both documents fixed |
| Both vendor token providers are in-process singletons using client credentials | `grep -n 'grant_type\|Invalidate' …/AnafTokenProvider.cs …/SamedayTokenProvider.cs`; `grep -n 'AnafTokenProvider' src/PhotoPrint.API/Program.cs` | ⚠️ **corrected** — an earlier draft claimed ANAF rotates refresh tokens; it uses `client_credentials`, and both providers expose `Invalidate()` |
| SignalR has no backplane | `grep -rn 'AddSignalR' src/PhotoPrint.API --include=*.cs` | ✅ bare `AddSignalR()` |
| Rate limits, decode cap, stats cache, log-once registries, metrics-denial dedup are per process | targeted `grep` per symbol (recorded in the walkthrough) | ✅ all confirmed at file and line |
| No Data Protection, no antiforgery, no `RowVersion` anywhere | `grep -rn 'DataProtection\|IDataProtector\|Antiforgery\|RowVersion' src/PhotoPrint.API` | ✅ nothing |
| 10 gated S3 tests in one class, every one guarded | `grep -c 'SkippableFact' …/S3StorageServiceIntegrationTests.cs` (11, one of which is prose in a doc comment) and `grep -c 'Skip.IfNot'` (10) | ✅ 1:1, no ungated skippable test |
| The MinIO gate is four env-var checks with no network probe | `sed -n '225,245p' …/S3StorageServiceIntegrationTests.cs` | ✅ exact |
| 17 PostgreSQL-backed test classes, 13 of them under `Unit/` | `grep -rl 'PostgresTestDatabase' src/PhotoPrint.Tests --include=*.cs` (18 files − 2 helpers = 16) plus `grep -rl 'PostgresPaymentFactory'` (adds `PaymentIdempotencyRelationalTests`) | ✅ 17 classes, 4 `Integration/` + 13 `Unit/` |
| Those classes **error** rather than skip, and the precondition is a role that may `CREATE DATABASE` | `sed -n '1,70p'` and `grep -n 'UnreachableMessage' -A14 src/PhotoPrint.Tests/Helpers/PostgresTestDatabase.cs` | ✅ constructor rethrows `InvalidOperationException`; default admin string is `postgres`/`postgres` at `localhost:5432` |
| No skipped, disabled or `.only` frontend specs | `grep -rn 'describe\.skip\|it\.skip\|test\.skip\|\.todo\|xit(\|xdescribe(\|fdescribe(\|fit(\|\.only(' src/PhotoPrint.UI/src --include=*.ts` | ✅ empty |
| No `Fact(Skip = …)`, and no test depends on the clock, the network or the host OS | `grep -rn 'Skip *=' src/PhotoPrint.Tests`; `grep -rn 'Thread.Sleep\|DateTime.Now\|FindSystemTimeZoneById\|IsOSPlatform\|OSVersion' src/PhotoPrint.Tests` | ✅ both empty |
| LOC and file-count baselines (334 / 20,361 · 165 / 32,591 · 1.60× · 100 / 10,706 · 50 / 6,756) | the `find`/`wc` pipeline printed in the checklist's section 3 | ✅ reproduced exactly, twice independently |
| Largest files: `home-page.ts` 951, `InvoiceUploadJob.cs` 615 | the same pipeline with `grep -v ' total$'` | ✅ and both breach the checklist's own 600-line threshold — recorded as owed |
| One timestamped migration, three files | `ls src/PhotoPrint.API/Migrations/` | ⚠️ **corrected** — the first draft's "more than one migration file" threshold would have failed on its own baseline; restated as timestamped migrations |
| 24 ADR files, `total_decisions: 24` | `find memory-bank/bolts -name 'adr-0*.md' \| wc -l`; `grep -n 'total_decisions' memory-bank/standards/decision-index.md` | ✅ matching |
| Doc-rot baseline dates and headers | `git log -1 --format=%ad --date=short` per standards file; `head -6` per file for the header | ⚠️ **corrected** — a first draft invented a header date for `api-conventions.md`, which has none, and pre-dated `tech-stack.md`. Rebuilt; three docs have no header and `decision-index.md`'s `last_updated` is 3 months stale |
| Every relative link in the changed files resolves | scripted link check by the stage-4 reviewer over all seven files | ✅ zero broken, counting the three new files as present |
| Bolt 054 is not merged | `git fetch origin`; `git log origin/main -1`; `git branch -r --contains origin/feat/bolt-054-dependency-hardening` | ✅ `origin/main` = `182cd50`; 054 pushed, unmerged. No claim states its content as present |
| Nothing on the wave's do-not-touch list is modified | `git status --short` and `git diff origin/main...HEAD --name-only` | ✅ see the hand-off list in `bolt.md` |

### Acceptance criteria validation

- ✅ **Five concerns, each citing its ADR, each split today / bolt 046** — plus two blockers
  ahead of them and a third that ADR-008 decided deliberately.
- ✅ **AWB safety described as the `AwbClaimedAt` lease**, with the amendment's honest residual
  (the crash window rests on unverified vendor deduplication) carried over rather than smoothed.
- ✅ **Instance-local state covered symptom by symptom** — nine rows plus a "what is not a
  problem" section so the negatives are not re-investigated.
- ✅ **Never reads as a commitment to build Redis** — bolt 046 is named deprioritized in the
  opening, in every concern's future half, and in the closing list, which also notes that the
  first three steps need no Redis at all.
- ✅ **`system-architecture.md` links to the readiness doc**, and its stale
  `PromotionRecoveryScanner` trigger was corrected rather than propagated.
- ✅ **`tech-stack.md` traces to the manifests**; email is config-driven; the five missing
  library families are named.
- ✅ **Known-failure register lists every gated surface with its mechanism**, says which skips
  and which errors, and states what it cannot measure.
- ✅ **No fabricated tracking ids** — the register explains why gated suites need none.
- ✅ **Checklist covers all five required areas** with a command or procedure and a measured
  baseline per section, plus a run log.
- ✅ **Checklist referenced** from `README.md`, `tech-stack.md` and `CLAUDE.md`'s map table.
- ✅ **`bolt.md` carries the re-verify list and the coordinator pointers.**

### Issues found

Nine claims were wrong in a draft and corrected before hand-back; all nine are in the table
above marked ⚠️, and the two gate records list them with their evidence. The pattern behind the
worst of them is worth stating plainly: **the job facts were first taken from
`system-architecture.md`'s table instead of from the code**, and that table is stale — which is
exactly the failure mode this bolt exists to fix, reproduced inside the bolt itself. The
readiness doc's per-concern halves are now written from the source in every case.

Two findings are outside this bolt's writable surface and are escalated in `bolt.md` rather
than fixed: the `deploy.yml` trigger mismatch, and `docs/DEPLOYMENT.md` §12.6, which still
describes the design as "correct under multi-replica" and credits an idempotency key that
ADR-015's own amendment retracts.

### What this pass cannot prove

- **No pass/fail tally for the suite.** Nothing here measures how many tests pass; the counts
  are classes and gates. The inherited "7 consistently-failing tests" figure is what happens
  when an unmeasured number sits in a document, so none was substituted.
- **Section 1 of the audit checklist has never been run** — both commands need the network.
  Its thresholds are un-anchored until a first real run, which the file says in as many words.
- **The AWB crash window remains unverified** against the courier's deduplication behaviour.
  Documented, not resolved; verification is a vendor question before the jobs are enabled.
- **Nothing here re-proves the ADRs' original reasoning.** Where an ADR and the code disagree,
  the documents describe the code and say the ADR is stale; correcting the ADRs and
  `decision-index.md` is another target's work and off-limits this wave.

### Self-validation (specsmd stage-3 checkpoint)

**Validated 2026-09-04.** Every acceptance criterion checked against the file that satisfies
it; every claim in the deliverables traced to a command in the record above; both
`bolt-process.md` gates run as fresh subagents with their findings folded in and recorded. The
bolt hands back at `status: review-pending` — stage 6 (review) has not run and is the
coordinator's to schedule.
