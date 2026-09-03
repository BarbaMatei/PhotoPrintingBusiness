---
intent: 032-regression-and-e2e-stabilization
phase: inception
status: units-defined
created: 2026-06-05T11:00:00Z
updated: 2026-06-05T11:00:00Z
source: docs/analysis/ai-workflow-review-2026-06-05.md §6 (Phase 3 — Stabilize)
priority_score: roadmap-phase-3
---

# Requirements: Regression Pass & Comprehensive E2E Testing Module

## Intent Overview

The application has grown to 355+ backend files, ~100 backend test classes and 46 frontend specs, but its end-to-end coverage is — by design of the backlog so far — only the **three smoke tests** that bolt 066 introduces (guest checkout, admin login, real-time SignalR). Phase 3 of the owner's roadmap (`docs/analysis/ai-workflow-review-2026-06-05.md` §6) calls for **stabilizing the application** before any environment work begins: (a) a deliberate **full regression pass** over every shipped feature, and (b) a **comprehensive end-to-end testing module covering the ENTIRE application** — every major user journey end to end, not just the happy paths.

This intent does **not** re-plan the e2e foundation or the shared test factories — those already exist as planned work and this intent **builds on them**:

- **Bolt 066** (intent 030) ships `@playwright/test`, the `playwright-e2e.yml` CI workflow, the three smoke specs, and the docker-compose boot pattern. This intent treats bolt 066 as the **foundation it extends** — same runner, same CI harness, same fixtures — growing it from 3 smoke specs to full-journey coverage.
- **Bolt 062** (intent 028) ships the shared backend test-application factory base and the fluent test-data Builders. This intent's **seeded/deterministic e2e data strategy reuses those Builders** rather than inventing a parallel data layer.

The journeys to cover span the entire customer- and admin-facing surface: guest checkout, registered checkout, authentication (email/password, Google, guest-session claim), uploads, cart (incl. guest→user merge), payments via **Stripe test mode** and EuPlatesc test mode, order history, account management (profile, password, address book), and admin order/product/invoice management. Coverage of **coupons (bolt 047/048)** and **refunds (bolt 068/069)** is included but gated on those bolts shipping — the e2e specs for them are authored as "should" and wired in once the underlying features exist, so this intent never blocks on, nor re-implements, them.

The regression-pass half is a **methodology + execution** deliverable: a documented, repeatable regression checklist mapped to shipped intents, an explicit pass run against the current build, a triaged defect log feeding back into the backlog, and a recorded baseline so future waves can re-run it cheaply.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Every major user journey is automated end-to-end | E2e specs cover all journeys in FR-2 (guest + registered checkout, auth ×3, uploads, cart+merge, payments, orders, account, admin) and pass in CI | Must |
| The application is provably stable before Phase 4 | A full regression pass is executed against the current build; every shipped intent has a checklist result (pass / known-issue) | Must |
| E2e is deterministic, not flaky | E2e suite green across 3 consecutive CI runs; deterministic seed data; bounded waits (no fixed sleeps) | Must |
| Regression is repeatable, not a one-off | A documented regression checklist + recorded baseline that a future wave can re-run | Should |
| Defects found are fed back, not lost | Every regression/e2e failure is triaged into the backlog (intent/bolt or KNOWN_FAILURES entry) | Should |

---

## Functional Requirements

### FR-1: Deterministic, seeded e2e test-data strategy

- **Description**: Define and implement a single, deterministic data strategy that every e2e journey runs against, reusing bolt 062's fluent Builders and the existing `--seed` / `--seed-dev` modes (`ProductCatalogSeed`, `DevDataSeed`). The strategy must produce a known catalog (products, sizes, finishes, pricing tiers), a known admin user, known Easybox lockers, and per-spec isolation so specs do not contend over shared mutable state.
- **Acceptance Criteria**:
  - A documented e2e data contract lists every seeded entity the specs rely on (product slugs, admin credentials, locker IDs) and is the single source of truth for spec fixtures.
  - E2e specs obtain their data exclusively from the seed + Builders — no spec hand-creates data via undocumented API calls except where the journey under test *is* that creation.
  - Re-running the suite twice in a row on the same seeded DB yields identical results (idempotent setup/teardown; per-spec uniqueness via Builder-generated identifiers).
  - Stripe and EuPlatesc run in **test mode** with documented test card / IPN fixtures; no live keys.
- **Priority**: Must
- **Related Stories**: 032 unit 001

### FR-2: Comprehensive end-to-end journey coverage (the whole application)

- **Description**: Extend the bolt-066 Playwright module from 3 smoke specs to full coverage of every major user journey, organised by domain. Each journey covers the realistic path including the key alternate/error branches that matter for a payment site (declined card, validation failure, ownership denial, expired guest session), not only the happy path.
- **Acceptance Criteria** (each is one or more specs):
  - **Guest checkout**: upload → format/finish selection → cart → guest details → Stripe test-mode payment → order confirmation; plus a declined-card branch.
  - **Registered checkout**: login → upload → cart → checkout → payment → order in history.
  - **Authentication**: email/password register + email-verification gate + login + logout; Google sign-in (mocked id_token verification in test mode); guest-session **claim** (guest order becomes visible after registering/logging in).
  - **Uploads & cart**: multi-file upload with progress + thumbnail; cart edit/remove; **guest→user cart merge** on login.
  - **Payments**: Stripe test-mode success + decline; EuPlatesc test-mode initiate + IPN callback success.
  - **Order history & detail**: list pagination, ownership enforcement (cannot view another user's order), order-detail photo grid.
  - **Account management**: edit profile, change password, add/edit/delete saved address.
  - **Admin**: admin login (gated), order list + status transition, product create/edit, invoice list + (when bolt 039 shipped) PDF/XML retrieval.
  - **Coupons** (gated on bolt 047/048): apply valid coupon at cart → discount line → order reflects discount; invalid/expired coupon rejected. Authored as `should`; skipped/`fixme` until the feature ships.
  - **Refunds** (gated on bolt 068/069): admin issues full/partial refund → order status `Refunded` → (when bolt 068 shipped) credit-note path. Authored as `should`; skipped/`fixme` until the feature ships.
  - All non-gated specs pass in CI on the shared `playwright-e2e.yml` harness within a documented time budget.
- **Priority**: Must (gated coupon/refund specs are Should)
- **Related Stories**: 032 unit 002, 032 unit 003

### FR-3: E2e CI integration, stability & reporting

- **Description**: Integrate the full suite into CI building on bolt 066's workflow — without ballooning PR time. Provide a stable execution model (retries-on-flake bounded, trace/video on failure), a fast subset on PR and the full suite on a schedule/label, and a readable report artifact.
- **Acceptance Criteria**:
  - PR runs a **fast tier** (the bolt-066 smoke set + the highest-value journeys) within a documented budget; the **full suite** runs on every merge to main (owner decision, 2026-06-05).
  - Playwright config sets bounded `retries` (e.g. 1–2 in CI), captures trace + video + screenshot on failure, and uploads them as a CI artifact.
  - No fixed `sleep`/`waitForTimeout` calls; all waits are condition-based (web-first assertions, SignalR awaited with a bounded timeout) to eliminate flake.
  - Suite demonstrated green across **3 consecutive** scheduled/CI runs as the stability gate.
- **Priority**: Must
- **Related Stories**: 032 unit 002

### FR-4: Documented regression-pass methodology + executed baseline

- **Description**: Produce a repeatable regression checklist that maps every shipped intent/feature to a verifiable check (automated where an e2e/integration test exists, manual otherwise), execute one full pass against the current build, and record the result as a dated baseline. Triage all findings into the backlog.
- **Acceptance Criteria**:
  - A `docs/testing/regression-checklist.md` (or equivalent under memory-bank) enumerates checks grouped by shipped intent (001–024 and any shipped 025–031 bolts), each marked automated-by-e2e / automated-by-integration / manual.
  - One full pass is executed and recorded with date, build SHA, and per-check result (pass / known-issue / fail).
  - Each failure/known-issue is linked to a backlog item: a new bolt, an existing planned bolt, or a `KNOWN_FAILURES.md` entry (the file introduced by bolt 057).
  - The checklist explicitly cross-references which checks are now covered by the FR-2 e2e specs so the manual surface shrinks over time.
- **Priority**: Should
- **Related Stories**: 032 unit 003

---

## Non-Functional Requirements

### Performance

| Requirement | Metric | Target |
|-------------|--------|--------|
| PR e2e fast tier | wall-clock per CI run | < ~8 min (documented; smoke + top journeys) |
| Full e2e suite | wall-clock per scheduled run | < ~25 min |

### Reliability

| Requirement | Metric | Target |
|-------------|--------|--------|
| E2e determinism | consecutive green scheduled runs | 3 in a row before declaring stable |
| Flake elimination | fixed-sleep waits in specs | 0 (condition-based waits only) |
| Failure diagnosability | artifacts on failure | trace + video + screenshot uploaded |

### Maintainability

| Requirement | Metric | Target |
|-------------|--------|--------|
| Data strategy duplication | parallel e2e data layers | 0 (reuse bolt 062 Builders + existing seeds) |
| Selector stability | reliance on `data-testid` | all journey specs select via `data-testid`, not text/CSS |

### Security

| Requirement | Standard | Notes |
|-------------|----------|-------|
| No live payment keys | Stripe/EuPlatesc **test mode** only | Test card + test IPN fixtures; secrets via CI env, never committed |
| Ownership coverage | journeys assert authz | order-detail cross-user denial is an explicit e2e check |

---

## Constraints

### Technical Constraints

**Project-wide standards**: tech-stack, coding-standards (Testing Strategy §), system-architecture loaded by Construction Agent.

**Intent-specific constraints**:
- **Build on, do not duplicate, bolt 066**: same Playwright runner, same `playwright-e2e.yml` harness, same docker-compose boot. This intent extends that module; it does not introduce a second e2e framework.
- **Build on, do not duplicate, bolt 062**: e2e seed/data fixtures reuse the fluent Builders and shared factory base; no parallel test-data layer.
- The e2e module exercises the app through the **real backend** (docker-compose API + Postgres + UI), so it is the first place that catches the InMemory-vs-Postgres parity gap (the `db-parity` review lens exists for it). (Closing that gap in the unit and integration suites is a separate concern; this intent simply runs against real Postgres.)
- Coupon (047/048) and refund (068/069) journeys are **authored but gated**: written as `should`, marked `test.fixme`/skipped until the feature bolts ship, then un-gated. This intent must not re-implement or re-plan those features.

### Business Constraints
- Phase 3 of the roadmap: must complete **before** Phase 4 (environment triad). This is a stabilization gate, not new product capability.
- No new customer-facing behaviour is introduced; this is test + verification work only.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| Bolt 066 (Playwright foundation + CI workflow) ships before this intent's construction | This intent has no runner/CI harness to extend | Declare bolt 066 a hard dependency on every e2e bolt; if 066 slips, this intent's e2e bolts block (do not duplicate the foundation) |
| Bolt 062 (Builders + shared factory) ships before this intent's construction | E2e data strategy has no Builders to reuse | Declare bolt 062 a dependency on the data-strategy bolt; if it slips, the seed-only path still works but loses Builder reuse |
| Stripe + EuPlatesc test modes are sufficient to drive full payment journeys headlessly | A journey needs a real redirect/3DS flow that test mode can't script | Use Stripe test cards incl. the 3DS-required card; for EuPlatesc, drive the IPN callback directly with a signed test payload |
| The dev DB can be seeded deterministically for e2e | Specs contend on shared mutable state and flake | Per-spec Builder-generated unique identifiers; idempotent seed; isolated test users |
| Coupons/refunds ship during or after this intent | Gated specs stay skipped at intent close | That is acceptable — gated specs are `should`; un-gating is a trivial follow-up when the features land |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: PR fast-tier vs full-suite split — exact journey list and time budget? | Maintainer | TBD | Recommend smoke (066) + guest checkout + auth + admin order on PR; everything nightly |
| Q2: Trigger for the full suite — nightly schedule, `run-full-e2e` label, or merge-to-main? | Maintainer | 2026-06-05 | **RESOLVED (owner, 2026-06-05): full suite runs on merge to main** |
| Q3: Should the regression checklist live in `docs/testing/` or under `memory-bank/operations/`? | Maintainer | TBD | Recommend `docs/testing/regression-checklist.md`; pending |
| Q4: Google sign-in e2e — mock the id_token verification, or use a dedicated Google test account? | Maintainer | 2026-06-05 | **RESOLVED (owner, 2026-06-05): mock the server-side id_token verification** (no external dependency in CI) |
| Q5: Should this intent also stand up the real-Postgres integration-test profile (ai-workflow-review §2.3), or leave it to bolt 062? | Maintainer | TBD | §2.3 suggests folding into bolt 062; this intent runs e2e against real Postgres regardless — pending |
