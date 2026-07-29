---
intent: 026-observability-boot-manifest
phase: inception
status: inception-complete
created: 2026-06-05T09:00:00Z
updated: 2026-06-05T09:00:00Z
source: docs/analysis/architect-review-2026-06-03.md (Group 2 — P04, P07, P10, P12, P17, P19)
priority_score: 20
---

# Requirements: Observability, Boot Composition & System Manifest

## Intent Overview

This intent makes the running system **inspectable and self-documenting** — the maintainer's third explicit concern: ~11 background jobs, 7+ feature flags, and dozens of off-by-default code paths are invisible, so a regression in (e.g.) `Anaf:Enabled=false` boot goes unnoticed until production. It bundles six proposals that together: (a) make the 534-LOC `Program.cs` boot script readable and unit-testable (P07); (b) replace string-typed feature flags with a typed, testable `IFeatureGate` registry (P10); (c) expose a `/api/admin/system-info` manifest derived from that registry so a flag/job regression is caught at PR time (P04); (d) catch the silent-death-of-a-background-job scenario with a liveness health check + ANAF invoice metrics tied to the 5-business-day legal SLA (P17); (e) consolidate the scattered multi-replica reasoning into one doc (P12); and (f) make the standards docs stop lying about the stack (P19). Ship after/parallel to Group 1; P07 → P10 → P04 is the internal dependency chain.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Make "what is wired right now" a single queryable source of truth | `GET /api/admin/system-info` lists every hosted service, flag, route, CLI verb | Should |
| Catch off-by-default regressions at PR time, not in prod | Integration test asserts manifest reflects flag state | Should |
| Detect silently-dead background jobs | Liveness health check reports Degraded within 3× a job's interval | Must |
| Surface ANAF submission lag against its legal SLA | `invoice_upload_*` metrics + an SLO entry | Must |
| Boot script is reviewable and per-subsystem testable | `Program.cs` ≈ 120 LOC of composition | Should |
| Standards docs match reality | `tech-stack.md` reflects Angular 21 / Vitest; no phantom deps | Must |

---

## Functional Requirements

### FR-1 (P07): Extract `Program.cs` subsystem composition into 5 extension methods
- **Description**: Move the inline Sameday, ANAF, Invoicing, Payments, and Sentry DI blocks into `Extensions/<Subsystem>Extensions.cs` (`AddSameday`, `AddAnaf`, `AddInvoicing`, `AddPayments`, `AddSentry`) following the existing `AddSocialAuth`/`AddObservability` precedent. The `if (xEnabled)` guard moves inside each extension; `QuestPDF.Settings.License` co-locates into `AddInvoicing`.
- **Acceptance Criteria**:
  - `Program.cs` reduces to a fluent chain of `AddX(...)` calls (~120 LOC).
  - Composition order preserved (`AddInvoicing` before `AddAnaf` — ANAF depends on Invoicing services); a host-boot test guards the ordering.
  - Per-extension unit test: "with `Enabled=false` the extension registers nothing background-y" (catches accidental always-on `InvoiceUploadJob`).
- **Priority**: Should
- **Related Stories**: TBD

### FR-2 (P10): Centralise feature flags via a typed `IFeatureGate`
- **Description**: Replace ad-hoc `Configuration.GetSection(...).GetValue<bool>("Enabled")` reads with a typed `IFeatureGate` over a `FeatureFlag` enum and a static registry (enum → config key + default + description), bound from `IConfiguration` at boot and cached. The registry doubles as the data source for the P04 manifest.
- **Acceptance Criteria**:
  - `FeatureFlag` enum covers all current flags: Sameday, SamedayJobs, Sentry, Observability, Anaf, InvoiceEmailAttachments, PhotoArchive, OldOriginalArchive.
  - `IsEnabled(FeatureFlag)` + `GetAll()` implemented by `ConfigFeatureGate`; unit-tested against missing/malformed config (a typo'd key resolves to the documented default, not a silent false).
  - Every former string read is migrated to the gate; `Program.cs` reads `gate.IsEnabled(...)`.
  - Documented as **boot-time only** (not hot-reloadable) — out of scope for the bolt-046-deprioritized phase.
- **Priority**: Should
- **Related Stories**: TBD

### FR-3 (P04): `GET /api/admin/system-info` feature-manifest endpoint
- **Description**: Add an admin-only endpoint that introspects DI + config and returns a `SystemManifest` (version/commit, hosted services + status + gating flag, feature flags, admin routes, webhook routes, CLI verbs). Cache ~30s. Render in an Admin "System" tab.
- **Acceptance Criteria**:
  - `GET /api/admin/system-info` returns 200 `SystemManifest` behind `[Authorize]` admin policy (see intent 029 P08); anonymous → 401.
  - Manifest is 100% derived from `IFeatureGate.GetAll()` (P10) for the flag section — no duplicated flag list.
  - Integration test: with `Anaf:Enabled=true`, manifest reports `InvoiceUploadJob: Running`; with the registration removed, the test fails (regression caught at PR time).
  - No secrets exposed in the payload.
- **Priority**: Should
- **Related Stories**: TBD

### FR-4 (P17): Background-job liveness health check + ANAF invoice metrics
- **Description**: (a) Add an `IHeartbeat` that each `BackgroundService` beats per tick, and a `BackgroundJobLivenessCheck` that reports Degraded when any heartbeat is older than 3× its scheduled interval — catching the framework-swallowed-exception silent-death case. (b) Add `invoice_upload_total{result}` counter + `invoice_upload_lag_seconds` histogram mirroring the existing `payment_webhook_total` pattern, plus an SLO entry for the ADR-024 5-business-day ANAF SLA.
- **Acceptance Criteria**:
  - `IHeartbeat.Beat(jobName)` called per tick in each hosted service; `Snapshot()` exposes last-beat timestamps.
  - Health check returns Degraded for a stale heartbeat (test stops a job → check degrades).
  - `FotoMetrics.InvoiceUpload` stamped at end of `InvoiceUploadJob.ProcessOneAsync` with `result: accepted | rejected | failed | retried`; lag histogram recorded.
  - `slos.md` gains an ANAF upload-lag SLO.
- **Priority**: Must
- **Related Stories**: TBD

### FR-5 (P12): Multi-replica-readiness consolidation doc
- **Description**: Write `docs/architecture/multi-replica-readiness.md` consolidating the in-process-state reasoning currently scattered across ADRs 010 (promotion `Channel<T>`), 013 (Sameday token cache), 015 (accept-duplicate AWB), 016 (status CAS), 023 (ANAF polling). One section per concern, each stating "today: X / future bolt 046: Y."
- **Acceptance Criteria**:
  - Doc covers all five concerns, each citing its ADR.
  - Linked from `memory-bank/standards/system-architecture.md`.
  - Aligns with the [[project_bolt_046_deprioritized]] decision (documentation only — does not implement the Redis backplane).
- **Priority**: Could
- **Related Stories**: TBD

### FR-6 (P19): Refresh standards docs + add `KNOWN_FAILURES.md` + audit-checklist ritual
- **Description**: Rewrite `tech-stack.md` against the real `package.json`/`.csproj` (Angular 21 not 17+, Vitest not Jasmine/Karma, remove phantom `heic2any`/`ng2-charts`, correct config-driven email provider). Enumerate the 7 consistently-failing tests in `docs/KNOWN_FAILURES.md` with reasons + tracking issues. Add a 1-page `docs/ARCHITECTURE_AUDIT_CHECKLIST.md` (vulnerabilities, outdated, LOC growth, ADR additions, doc rot).
- **Acceptance Criteria**:
  - `tech-stack.md` claims verifiably match installed dependencies.
  - `KNOWN_FAILURES.md` lists each of the 7 skips/failures with a reason — replaces tribal knowledge.
  - `ARCHITECTURE_AUDIT_CHECKLIST.md` exists and is referenced from the standards index.
- **Priority**: Must
- **Related Stories**: TBD

---

## Non-Functional Requirements

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| `system-info` introspection | Response (cached) | < 50ms cache hit; 30s cache TTL |
| Liveness check overhead | Per-tick heartbeat | O(1), negligible |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Detect dead background job | Time-to-Degraded | ≤ 3× the job's scheduled interval |
| ANAF SLA visibility | SLO defined | 5 business days (ADR-024) |

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Manifest exposure | Admin-only, no secrets | Endpoint behind admin policy (intent 029 P08) |

---

## Constraints

### Technical Constraints
- Internal dependency order: **P07 → P10 → P04** (P04 consumes `IFeatureGate.GetAll()`); P17, P12, P19 parallel.
- `IFeatureGate` is boot-time only — no hot reload (consistent with bolt-046 deprioritization).
- P12 is documentation only; must not be read as a commitment to build the Redis backplane.

### Business Constraints
- P17 and P19 are pre-launch must-haves (silent-job-death = missed ANAF SLA = compliance risk; lying docs = onboarding poison). P04/P10 are strong nice-to-haves; P07/P12 are health/clarity.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| All feature flags are boot-time config keys (none runtime-dynamic) | A flag needs hot reload | Out of scope; documented; revisit with bolt 046 |
| `IEndpointRouteBuilder` reflection can enumerate routes for the manifest | Route list incomplete | Fall back to a curated static list; flag gaps in the manifest |
| The 7 failing tests are genuinely expected (CI S3 skips etc.) | A real failure is hiding among them | `KNOWN_FAILURES.md` forces a per-test reason; unexplained ones become bugs |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Does the manifest enumerate routes via reflection or a curated list? | Dev | 2026-06-19 | Recommend reflection with a static fallback |
| Q2: Heartbeat staleness multiplier — 3× interval, or per-job tuned? | Dev/Ops | 2026-06-19 | Default 3×; allow per-job override |
| Q3: Are there exactly 7 known-failing tests, and what is each reason? | Dev | 2026-06-19 | Audit during P19; document each |
