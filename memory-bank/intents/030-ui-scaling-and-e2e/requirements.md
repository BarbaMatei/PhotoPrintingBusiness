---
intent: 030-ui-scaling-and-e2e
phase: inception
status: inception-complete
created: 2026-06-05T09:00:00Z
updated: 2026-06-05T09:00:00Z
source: docs/analysis/architect-review-2026-06-03.md (Group 6 — P18, P26)
priority_score: 17
---

# Requirements: UI Scaling Refactor & E2E Smoke Tests

## Intent Overview

The Angular frontend is the project's healthiest layer at the *service* level (largest service ≈ 179 LOC), but its *components* don't scale: `home-page.ts` is **951 LOC**, four other pages exceed 380 LOC, and the inline-template + inline-fetch + inline-state pattern repeats. All 14 core services hand-roll `HttpClient` calls with no shared base (duplicated error translation, `withCredentials`, idempotency-key threading). Separately, there is **no bundle-size budget in CI and no e2e tests** — both launch blockers for a payment-processing site. This intent breaks up the largest pages into smart-container + dumb-child components, introduces a `BaseApiService`, pins CI bundle budgets, and adds three Playwright e2e smoke tests on the real-money paths. It is **completely independent of the backend refactors** (intents 027–029) and can run on a second developer in parallel. Ship P18 first (1-line config + e2e foundation), then P26 page-by-page.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| The three real-money paths are automated before launch | 3 passing e2e: guest checkout, admin login, real-time SignalR | Must |
| Bundle bloat is caught in PR | CI fails when initial bundle exceeds budget | Should |
| Large pages are maintainable | No page component > ~200 LOC after breakup | Should |
| HTTP plumbing is DRY | All services route through `BaseApiService` | Should |

---

## Functional Requirements

### FR-1 (P18): CI bundle-size budget + 3 Playwright e2e smoke tests
- **Description**: Pin Angular `budgets` in `angular.json` and add `@playwright/test` with three scenarios + a GH Actions workflow that boots API+UI (docker-compose) and runs them.
- **Acceptance Criteria**:
  - `angular.json`: `initial` budget `maximumWarning: 500kB`, `maximumError: 750kB`; `anyComponentStyle` `maximumError: 4kB`. Build fails on error threshold.
  - Three e2e specs: `guest-checkout.spec.ts` (guest → Stripe → confirmation), `admin-login.spec.ts`, `realtime-order.spec.ts` (admin sees SignalR broadcast).
  - `playwright-e2e.yml` runs in CI using the official Playwright action; budget ~3 min/run.
- **Priority**: Must
- **Related Stories**: TBD

### FR-2 (P26): Break up the four largest pages + introduce `BaseApiService`
- **Description**: Split the oversized pages into a thin smart container + dumb child components, and extract a `core/services/api/base-api.service.ts` centralising `withCredentials`, error translation, and idempotency-key threading; migrate services onto it one at a time.
- **Acceptance Criteria**:
  - `home-page.ts` (951 LOC) → thin container (~100 LOC) + `hero-section`, `value-props`, `pricing-teaser`, `trust-strip`, `cta-banner` components.
  - `saved-addresses-page.ts` (498) → container + `address-form` + `address-list-item`; `profile-page.ts` (473) → container + `personal-info-form` + `email-change-form` + `password-change-form`; `delivery-step.ts` (382) → extract `locker-selector`.
  - `BaseApiService` provides typed `get/post/put/delete` with `withCredentials`, `catchError` translation, and optional `Idempotency-Key`; services migrated onto it (start with `order.service.ts`).
  - Vitest suite green after each migration; home-page visually unchanged (before/after screenshots).
- **Priority**: Should
- **Related Stories**: TBD

---

## Non-Functional Requirements

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Initial bundle size | `main` initial | < 500kB warn / < 750kB error |
| Component style budget | per-component CSS | < 4kB |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Real-money path coverage | E2e scenarios | 3 passing in CI |
| No visual regression | Home page | Screenshot diff acceptable |

### Maintainability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Page component size | LOC | No page > ~200 LOC |
| HTTP plumbing duplication | Hand-rolled HttpClient services | 0 (all via `BaseApiService`) |

---

## Constraints

### Technical Constraints
- Independent of backend intents 027–029; parallelisable on a second developer.
- Ship P18 before P26 (budget + e2e foundation first).
- Playwright needs browsers cached in CI (~200MB); use the official action.
- P26 component breakups land as one PR per page (home → saved-addresses → profile → delivery-step).

### Business Constraints
- P18 e2e is a pre-launch must-have (three real-money paths). P26 is post-launch / non-blocking (no behaviour change), but high payback per dev-day before launch.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| 500kB initial budget is realistic for the current bundle | CI red on first run | Measure current size; set budget just above with a reduction target |
| The four pages decompose cleanly into smart/dumb split | Hidden cross-component coupling | Take screenshots; migrate one page per PR |
| `BaseApiService` covers all current call shapes | A service has a bespoke need | Keep an escape hatch for non-standard calls; document |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Exact initial-bundle budget given current size? | Dev | 2026-06-26 | Measure first; recommend current+10% with a downward target |
| Q2: Is a second developer available to parallelise this with the backend work? | Maintainer | 2026-06-26 | Pending |
| Q3: Stub or use Stripe test mode in the guest-checkout e2e? | Dev | 2026-06-26 | Recommend Stripe test mode against a seeded test product |
