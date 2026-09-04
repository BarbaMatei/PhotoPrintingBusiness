---
stage: implement
bolt: 066-ci-quality-gates
created: 2026-09-04T00:20:00Z
---

## Implementation Walkthrough: CI Quality Gates

### Summary

The Angular production build now carries budgets that actually bite, and a new Playwright suite
drives three real-money paths — guest checkout, admin login, and the admin real-time order feed —
against a stack booted from docker compose. A new workflow runs that suite on every pull request
and on every push outside `main`; the budget gate rides the production build the existing `ci.yml`
already runs, so no existing workflow was edited.

### Structure Overview

The runner lives in the UI npm project (`src/PhotoPrint.UI`), with specs in a sibling `e2e/` folder
rather than under `src/`, which keeps them entirely outside Vitest's collection root and outside both
existing tsconfigs; a third tsconfig covers the e2e sources for editors and for an explicit
type-check. The specs talk to two addresses only: the SPA served by `ng serve` on 4200 (started by
Playwright itself) and the API on 5052 — the port `environment.ts` already calls, so no e2e-specific
Angular configuration or environment file exists. A compose overlay publishes the containerised API
on that port under its own project name and its own volumes.

### Completed Work

- [x] `src/PhotoPrint.UI/angular.json` — production budgets replaced: `initial` warns at 400 kB and
      fails at 500 kB (measured baseline 331.99 kB); `anyComponentStyle` warns at 4 kB and keeps its
      16 kB error. The previous pair was inert and inverted (1 MB/2 MB, and a 20 kB warning above a
      16 kB error).
- [x] `src/PhotoPrint.UI/playwright.config.ts` — one Chromium project, serial (`workers: 1`) because
      the specs share server state, one retry in CI only, trace on the retry, and a `webServer` that
      owns the dev server with a 180 s start budget.
- [x] `src/PhotoPrint.UI/e2e/support/stack.ts` — the addresses, the seeded admin credentials, the
      admin login helper, the `sessionStorage` token lift, and a locale-tolerant amount parser.
- [x] `src/PhotoPrint.UI/e2e/guest-checkout.spec.ts` — home → catalog → product → photo upload →
      size choice → add to cart → cart → delivery (courier + address) → review, asserting the cart
      subtotal matches what the format page quoted and the grand total equals subtotal + shipping.
- [x] `src/PhotoPrint.UI/e2e/admin-login.spec.ts` — an unauthenticated `/admin` visit is bounced to
      login, the seeded admin signs in, lands back on `/admin`, and a token is stored.
- [x] `src/PhotoPrint.UI/e2e/realtime-order.spec.ts` — admin watches the orders list; a real status
      change issued through the admin API arrives over SignalR and repaints the row's badge with no
      navigation.
- [x] `src/PhotoPrint.UI/e2e/fixtures/sample-photo.jpg` — a real 1200×800 JPEG for the upload leg.
- [x] `src/PhotoPrint.UI/tsconfig.e2e.json` + a reference from `tsconfig.json` — Node types for the
      e2e sources without leaking them into the app or unit-test builds.
- [x] `docker-compose.e2e.yml` — new overlay: own project name and volumes, API published on 5052,
      database and mail ports unpublished, JWT key injected from the environment.
- [x] `.github/workflows/playwright-e2e.yml` — new workflow: prepare `.env` and a fresh keypair,
      boot the stack, wait for health, seed catalog + admin + demo orders, install dependencies and
      the browser, run the suite, upload the report and API log on failure, tear down with volumes.
- [x] `.gitignore` — Playwright run artefacts.
- [x] `src/PhotoPrint.UI/package.json` — `@playwright/test` and `@types/node` as devDependencies.

### Key Decisions

- **Budgets tightened below the story's numbers.** 750 kB against a 332 kB bundle would let it more
  than double before CI noticed; 500 kB is +50 %. The story's own third criterion asks for "just
  above current", which the numbers now satisfy.
- **4 kB is a warning, not an error.** Six built stylesheets exceed it today (admin products 13.97 kB,
  home 10.98 kB, header 6.68 kB, admin orders 4.88 kB, admin order detail 4.62 kB, admin state
  machine 4.43 kB); most belong to pages no story in this intent touches. The warning publishes the
  list on every build; bolt 067 removes home from it.
- **One URL pair for local and CI.** Publishing the containerised API on 5052 means the same specs
  run against a locally launched API and against the container, with no `fileReplacements` and no
  extra environment file.
- **The compose overlay is separate and self-contained.** The dev stack keeps its name, ports and
  volumes; only an explicit two-file invocation reaches the e2e stack.
- **The real-time spec triggers its broadcast through the admin API, not a second browser.** The
  broadcast, the hub and the SPA subscription are all real; replacing only the click keeps the
  assertion on the real-time path and halves the spec's moving parts.
- **The suite is serial.** All three specs mutate shared server state (a cart, an order status).

### Deviations from Plan

None in substance. The plan was itself revised before implementation to absorb the adversarial
design check's 14 findings — the two blockers (compose cannot start without `.env`; every request
including `/health` fails with an empty JWT key) are handled in the workflow's preparation step, and
the seven recorded deviations from the stories are listed in `implementation-plan.md`.

### Dependencies Added

- [x] `@playwright/test` — the e2e runner (dev-only, no effect on the shipped bundle).
- [x] `@types/node` — required by the Playwright config and the e2e helpers; scoped to
      `tsconfig.e2e.json`, so neither the app build nor the unit-test build sees it.

### Developer Notes

- The stack must be seeded before the suite: an unseeded catalog fails the guest spec on its first
  assertion, by design.
- The status transition the real-time spec performs is one-way. A second local run against a
  persisted volume finds no `Paid` order and skips with a message; `down -v` restores it.
- The SPA's SignalR connection never uses WebSockets here — the hub requires the Admin role and the
  API reads no query-string token, so the client falls back to long polling. That is pre-existing
  behaviour; the spec waits for the long-poll connect before triggering a broadcast, because
  `Clients.All` has no replay for a client that has not finished connecting.
- Cart and checkout-review assertions are text-based on purpose: those files belong to another
  session this wave, so no test hooks could be added to them.
