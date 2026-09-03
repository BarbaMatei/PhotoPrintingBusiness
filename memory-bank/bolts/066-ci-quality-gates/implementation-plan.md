---
stage: plan
bolt: 066-ci-quality-gates
created: 2026-09-03T20:47:00Z
---

## Implementation Plan: CI Quality Gates

### Objective

Add the two pre-launch frontend quality gates the project lacks: a bundle-size budget that
fails the build when the SPA bloats, and three Playwright smoke tests over the real-money
paths (guest checkout, admin login, admin real-time order updates), run by a new CI workflow.

### Measured baseline (2026-09-03, `npm run build -- --configuration=production`)

| Metric | Current |
|---|---|
| Initial total (raw) | **331.99 kB** (93.23 kB transfer) |
| Largest initial chunk | 151.25 kB |
| Largest lazy chunk | 214.43 kB (`admin-page`) |
| Largest component stylesheet (source) | 26.2 kB `admin-products-page.scss`, 16.2 kB `home-page.ts` inline, 11.8 kB `header.scss` |
| Existing budgets | `initial` 1MB warn / 2MB error; `anyComponentStyle` 20kB warn / **16kB error** (warning above error — inverted) |

### Deliverables

- `angular.json` — real budgets on the `production` configuration (the configuration CI already
  builds), replacing the inverted/ineffective pair.
- `src/PhotoPrint.UI/playwright.config.ts` — Playwright runner config; starts the Angular dev
  server itself (`webServer`) and targets `http://localhost:4200`.
- `src/PhotoPrint.UI/e2e/guest-checkout.spec.ts` — guest builds a basket and reaches checkout
  review with the right total.
- `src/PhotoPrint.UI/e2e/admin-login.spec.ts` — seeded admin logs in and reaches `/admin`.
- `src/PhotoPrint.UI/e2e/realtime-order.spec.ts` — an order-status change broadcast over SignalR
  updates an open admin orders list without a reload.
- `src/PhotoPrint.UI/e2e/fixtures/` — a small real JPEG for the upload leg + shared helpers.
- `docker-compose.e2e.yml` — new override that publishes the API on host port **5052** (the port
  `environment.ts` already points at) so the local dev stack and CI use one URL pair.
- `.github/workflows/playwright-e2e.yml` — new workflow: boot API+Postgres via docker compose,
  seed, run the specs, upload the HTML report on failure.
- `@playwright/test` added to `src/PhotoPrint.UI` devDependencies (no `.csproj` / CPM change).

### Dependencies

- **docker compose + the repo `Dockerfile`**: boots API + PostgreSQL 16 in CI. The API image also
  bakes the SPA, but e2e serves the SPA from `ng serve` so the app under test is the source tree.
- **Seed data**: `ProductCatalogSeed` (catalog + admin `mateibarba@yahoo.com` / `Admin1234!`) and
  `DevDataSeed` (6 orders spanning statuses) via `dotnet PhotoPrint.API.dll --seed-dev`.
  Both are idempotent.
- **Angular budgets**: enforced by the existing `ci.yml` `web` job, which already runs
  `npm run build -- --configuration=production` on every PR. No edit to `ci.yml` (another group
  owns existing workflows this wave); the budget change alone makes that job a gate.

### Technical approach

**Budgets.** Set on the `production` configuration only (that is what CI builds and what the
Docker image builds):

- `initial`: `maximumWarning: 400kB`, `maximumError: 500kB`.
- `anyComponentStyle`: `maximumWarning: 4kB`, `maximumError: 16kB`.

**Playwright.** `playwright.config.ts` in `src/PhotoPrint.UI` (the npm project that owns the
runner), specs under `e2e/`, `testDir: './e2e'` so Vitest (which globs `**/*.spec.ts` under
`src/`) never picks them up. `webServer` runs `npm start` on 4200 and reuses a running server
locally. One project (Chromium), `retries: 1` in CI only, trace on first retry.

**The three specs.**

1. *guest-checkout* — home → `/tipareste` → product → upload a fixture JPEG → pick size/finish and
   quantity → add to cart → `/cos` shows the line and total → `/checkout/livrare` (Courier +
   address) → `/checkout/recapitulare` shows the same total. Stops at the Stripe boundary
   (see Deviations).
2. *admin-login* — `/auth/login` with the seeded admin → lands on the admin area, dashboard
   renders.
3. *realtime-order* — admin session on `/admin/comenzi` with a seeded order; the test issues a real
   authenticated `PATCH /api/admin/orders/{id}/status` through Playwright's request context
   (the same call the admin UI makes), then asserts the open list's status badge changes with no
   reload. The server-side broadcast (`AdminOrderService` → `OrderStatusChanged`) and the SPA's
   `AdminHubService` subscription are both real; only the click that triggers it is replaced, which
   keeps the assertion on the real-time path rather than on a second browser's form.

**One URL pair everywhere.** `environment.ts` already points at `http://localhost:5052/api`; the
compose override publishes the API there, so no new environment file, no `fileReplacements`, and
the same specs run locally against a `dotnet run` API and in CI against the container.

**Booting the stack (as corrected by the design check).** The base compose file declares
`env_file: .env` and a fixed project name `fototipar-dev`, and the API refuses every request —
`/health` included — when `JwtSettings__PrivateKeyPem` is empty, because the JWT bearer options are
built on each request through the default authenticate scheme. So the workflow, in one step:

1. `cp .env.example .env` (the file is gitignored and absent in a fresh checkout; compose aborts
   without it),
2. generates an RSA keypair and exports the PEM as a shell variable, which
   `docker-compose.e2e.yml` interpolates into `environment: JwtSettings__PrivateKeyPem: ${JWT_PEM}`
   (a multi-line PEM cannot travel through an `.env` file, shell interpolation carries it intact),
3. runs everything under `-p fototipar-e2e` so an e2e run never adopts or destroys a developer's
   `fototipar-dev` containers and volumes.

Seeding runs as `docker compose … run --rm api --seed-dev`: the exec-form entrypoint appends the
argument, migrations apply, both seeds run, the process exits before the HTTP pipeline is built.

**Real-time readiness.** The hub is `[Authorize(Roles="Admin")]` and nothing in the API reads a
token from the query string, so the SignalR WebSocket transport (which can only pass the token that
way) is rejected and the client falls back to long polling, which carries the `Authorization`
header. Two consequences the spec must respect: waiting for a WebSocket would hang forever, and a
broadcast fired before the fallback connects is dropped, not queued (`Clients.All` has no replay).
The spec therefore waits for the long-poll connect request to `/hubs/admin-orders` before it
triggers the status change. This transport fallback is pre-existing behaviour, not something this
bolt changes; it is recorded in the test report.

**Choosing the order to transition.** `OrderStatusMachine` allows 8 edges and the seed plants each
status once, so a fixed order id is a one-shot: the second run gets a 400 and re-seeding does not
reset it (the seed skips on existence). The spec reads `GET /api/admin/orders` and picks the first
order with a legal successor, preferring `Paid → Printing` (`Printing → Shipped` sends the shipped
email). A repeat local run against a persisted volume needs `docker compose … down -v` first.

**Admin authentication in the specs.** The access token lives in `sessionStorage`, which Playwright's
`storageState` does not persist — so there is no stored-login shortcut. Both admin specs log in
through the real login form, and the realtime spec lifts the token with
`page.evaluate(() => sessionStorage.getItem('access_token'))` for its API call.

### Caller-impact sweep

| Consumer of what this bolt touches | Effect |
|---|---|
| `.github/workflows/ci.yml` `web` job (`npm run build -- --configuration=production`) | **Becomes the budget gate.** Verified green at the new numbers before hand-off; file itself untouched. |
| `Dockerfile` stage `ui-build` (`npm run build -- --configuration=production`) | Same budgets apply — an over-budget SPA now fails the image build too. Intended; verified by the same local build. |
| `.github/workflows/deploy.yml` (builds the image on main) | Inherits the Dockerfile behaviour above; no edit. |
| `npm test` / Vitest (`@angular/build:unit-test`) | Unaffected: it collects `src/**/*.spec.ts`; the new specs live in `e2e/` outside `src/`. Confirmed by running the UI suite after the change. |
| `docker-compose.yml` (dev stack, port 8080) | Untouched. The new `docker-compose.e2e.yml` is an *override* used only with an explicit `-f` pair; plain `docker compose up` behaves exactly as before. |
| `angular.json` `development` configuration / `ng serve` | No budgets there; dev server behaviour unchanged. |
| `package.json` (UI) | One devDependency + `package-lock.json`. No `.csproj`, no `Directory.Packages.props`. |
| `.github/workflows/playwright-e2e.yml` (new, third budget consumer) | The compose build runs the Dockerfile's `ui-build` stage, so an over-budget SPA now also fails the e2e workflow — inside a Docker build, where the message is less obvious. Accepted: the `web` job in `ci.yml` fails first and states the budget plainly. |
| `docker-compose.yml` project name `fototipar-dev` and its `pgdata`/`apidata` volumes | Protected by running the e2e stack under `-p fototipar-e2e`; without that an e2e teardown would delete a developer's dev database and uploads. |

### Failure-mode table

| What can fail | What should happen | Which test proves it | What is logged |
|---|---|---|---|
| SPA grows past the error budget | Production build fails; `ci.yml` `web` job red | Manual injection at plan-verification time: temporarily set `maximumError` below current size and confirm the build fails (recorded in the test report) | Angular budget error naming the bundle and the overage |
| A component stylesheet passes 4 kB | Build warns, does not fail | Same build run: the 4 kB warning list is recorded in the test report | Angular budget warning per stylesheet |
| API not ready when specs start | Workflow fails at an explicit health wait, before Playwright runs | CI step `Wait for API health` with a bounded loop (60 × 2 s) | The step prints `docker compose logs api` on timeout |
| Seed did not run / catalog empty | Guest-checkout spec fails fast on an empty catalog with a clear message | `guest-checkout.spec.ts` asserts a product card exists before uploading | Playwright trace + HTML report artifact |
| SignalR broadcast never arrives | Spec fails on a bounded `expect(...).toHaveText` (10 s) rather than hanging | `realtime-order.spec.ts` | Report artifact; browser console captured in the trace |
| Dev server port 4200 already taken locally | `reuseExistingServer` locally; in CI the port is always free and `webServer` owns it | n/a (config) | Playwright prints the webServer log |
| A spec flakes on a slow CI runner | One retry in CI, trace kept on the retry; two consecutive red runs = a real failure | Config: `retries: process.env.CI ? 1 : 0` | Trace zip in the artifact |
| `.env` absent (fresh checkout) | Workflow creates it from `.env.example` before compose runs; compose would otherwise abort before starting a container | CI step `Prepare stack config` | Compose's own "env file not found" |
| JWT signing key empty | Generated per run and injected through the override; without it every request, `/health` included, returns 500 and the health wait fails loudly | Same step + the health wait | 500s with the "PrivateKeyPem is required" message in `docker compose logs api` |
| The hub has not finished connecting when the status changes | The broadcast is dropped, not delayed, so the spec waits for the long-poll connect request before triggering | `realtime-order.spec.ts` (explicit `waitForRequest` on `/hubs/admin-orders`) | Report artifact |
| The chosen order has no legal next status (re-run against a persisted volume) | Spec picks an order with a legal successor from the live list, and skips with a clear message if none exists | `realtime-order.spec.ts` | Report artifact |
| `ng serve` cold start exceeds Playwright's 60 s `webServer` default | `timeout: 180_000`, so a slow first compile is not reported as a test failure | Config | Playwright prints the dev-server log |
| Playwright browsers not installed on the runner | Workflow installs `chromium --with-deps` after `npm ci` | CI step `Install Playwright browser` | Playwright's own "Executable doesn't exist" |

### Backlog sweep (`reviews/state/backlog.md`, areas this bolt touches)

Areas touched: frontend build config and frontend test infrastructure (`tests`), plus the CI
surface. No `reviews/state/**` file is edited (this wave's rule) — the coordinator writes the
re-deferral notes at merge time.

The `tests` area is the one this bolt touches, so all 28 of its rows are ruled on individually.
None is pulled in: every row is a *unit*-test gap in backend or component code this bolt does not
modify, and this bolt adds browser-level smoke coverage rather than unit coverage. The closest
call is PPW-125, which sits on the same guest-upload leg the guest-checkout spec drives — the
spec covers the happy path only, so the error path that row names stays open.

| Rows | Ruling |
|---|---|
| PPW-125 (guest-init error path when files are dropped is untested) | **re-deferred**: the new e2e drives the guest-upload *happy* path only; proving the failed-init path needs a Vitest spec on the upload page, outside both stories. Nearest row to this bolt's surface — worth pulling into whichever bolt next touches upload. |
| PPW-101, PPW-144 (guest-session recovery after failed init; bomb-to-422 never reached end to end) | **re-deferred**: upload/decode paths; no code here changes them. |
| PPW-93, PPW-120, PPW-121, PPW-122 (decode cap, slot release, allocator mapping, cleanup delete) | **re-deferred**: backend xUnit gaps; this bolt runs no `dotnet test` and changes no backend file. |
| PPW-188, PPW-224, PPW-237 (archive-guard seeding, S3 retry/presign, promoter bytes) | **re-deferred**: backend storage tests, untouched. |
| PPW-189, PPW-190, PPW-239 (order-detail refresh guard, lightbox focus trap, close-during-refresh) | **re-deferred**: gallery/lightbox Vitest gaps; those components are outside both stories. |
| PPW-363, PPW-367, PPW-386, PPW-389, PPW-395, PPW-403, PPW-404, PPW-405, PPW-420, PPW-423, PPW-427, PPW-434, PPW-459 (observability suite) | **re-deferred**: backend observability test-suite quality, no overlap with the frontend gates. |
| PPW-466 (two email tests flake under parallel load) | **re-deferred**: backend xUnit flake; this bolt runs no `dotnet test`. |
| PPW-648 (VAT rounding-mode test asserts `decimal.Round`) | **re-deferred**: invoicing test, another group's surface this wave. |
| All rows in other areas (`uploads`, `jobs`, `records`, `observability`, `data`, `payments`, `orders`, `shipping`, `edge`, `gallery`, `auth`) | Not touched by this bolt. |

### Deviations from the stories (recorded, with reasons)

1. **Budget numbers.** Story 001 names `initial` 500kB warn / 750kB error and its third criterion
   asks for "just above current with a documented reduction target". Measured current is 332 kB, so
   750 kB would let the bundle more than double before CI noticed. Taking the third criterion as
   the binding one: **400 kB warn / 500 kB error** (+20 % / +50 % headroom). Reduction target for a
   later bolt: keep `initial` under 300 kB.
2. **`anyComponentStyle` error stays at 16 kB, with the 4 kB target as a warning.** A 4 kB *error*
   fails the build today on stylesheets no story in this intent touches, and breaking up home
   (bolt 067) does not fix those. The 4 kB warning makes every offender visible on each build;
   promoting it to an error needs its own bolt for the admin pages. The offender list is quoted in
   the test report from a real production build — `anyComponentStyle` measures the *built*
   stylesheet, so the source-file sizes in the baseline table above are an indication, not evidence
   (today's config already errors above 16 kB and the build is green, so every built stylesheet is
   already under 16 kB).
3. **Guest checkout stops before Stripe.** Story 002 asks for "guest → Stripe test mode →
   confirmation". The repo has no Stripe test keys and CI has no secret to supply one
   (`environment.ts` ships `pk_test_placeholder`), so a card confirmation cannot run honestly here.
   The spec drives every app-owned step up to and including checkout review, and stops at the
   Stripe element. What is left unproven is stated in the test report; enabling the last leg needs
   a `STRIPE_TEST_*` secret and is a separate change.
4. **The real-money e2e never touches production money.** Everything runs against the ephemeral
   compose stack with seeded data.
5. **The e2e job is slower than the "~3 min" criterion, though the suite is not.** The compose
   build runs the whole `Dockerfile`, including an `npm ci` + production Angular build the e2e never
   serves, with no layer cache available to a plain `docker compose build`. The *suite* target
   stands; the job around it is several minutes longer. Recorded rather than optimised: switching
   the compose build to buildx with a GitHub Actions cache, or to a build target that skips the SPA
   stage, is a change to the shared build path that deserves its own bolt and its own verification.
6. **The e2e exercises the development build, the budget gates the production build.** `ng serve`
   uses `environment.ts` (unoptimised, API cross-origin on 5052); the budget applies to
   `--configuration=production` (`environment.prod.ts`, optimised, same-origin behind the combined
   image). The two gates therefore cover different artefacts — deliberately, since building the
   production bundle for every e2e run would double the job — and the cross-origin CORS/SignalR path
   the specs drive does not exist in production.
7. **Assertions on cart and checkout-review markup are text-based on purpose.** Those files belong
   to another session this wave, so no `data-testid` hooks may be added to them. The specs assert on
   headings and the total row by visible Romanian text; if that wording changes in the other
   session's work, the specs need a follow-up. Known cross-instance risk, flagged in the hand-off.

### Acceptance criteria

- [ ] `angular.json` production budgets set as above; `npm run build -- --configuration=production`
      passes at the new numbers, and fails when `maximumError` is temporarily set below current size.
- [ ] `@playwright/test` in UI devDependencies; `playwright.config.ts` present; `npx playwright test
      --list` shows exactly the three specs.
- [ ] The three specs pass against a booted stack.
- [ ] `.github/workflows/playwright-e2e.yml` exists, is the only workflow file added or changed,
      boots the stack via docker compose, seeds it, runs the specs, and uploads the report on failure.
- [ ] `npm test -- --watch=false` (Vitest) still collects only `src/**` specs and stays green.
- [ ] No backend source, `.csproj`, `Directory.Packages.props`, memory-bank index or
      `reviews/state/**` file is modified.

### Adversarial design check (bolt-process.md stage-2 gate)

Run 2026-09-03 as a fresh subagent against the first draft of this plan, brief: "attack this design
— races, resource bounds, missed callers, failure modes absent from the table, factual errors".
14 findings; all folded in above. The two blockers and the material ones:

| # | Finding | Disposition |
|---|---|---|
| 1 | **Blocker.** `docker compose` aborts before starting anything: `api` declares `env_file: .env`, which is gitignored and absent in a fresh checkout. | Workflow creates it from `.env.example`. |
| 2 | **Blocker.** With an empty `JwtSettings__PrivateKeyPem` *every* request 500s — `/health` included, because the default authenticate scheme builds the bearer options per request — so the health wait could never go green. (Seeding is unaffected: `--seed-dev` returns before the pipeline is built.) | Keypair generated per run, PEM injected through the override by shell interpolation. |
| 3 | No Playwright browser install anywhere; the story's "official Playwright action" does not exist. | `npx playwright install --with-deps chromium` step. |
| 4 | The hub's WebSocket transport can never authenticate (no query-string token reader server-side), so the client silently falls back to long polling; waiting for a WebSocket would hang, and a broadcast fired before the fallback connects is dropped. | Spec waits for the long-poll connect request; behaviour recorded in the test report. |
| 5 | A fixed status transition is one-shot — the state machine has 8 edges and the seed plants each status once, so a second run gets 400 and re-seeding does not reset it. | Spec picks a transitionable order from the live list; local re-runs documented as needing `down -v`. |
| 6 | The base compose project name is fixed (`fototipar-dev`), so an e2e teardown would destroy a developer's dev database and uploads. | Everything runs under `-p fototipar-e2e`. |
| 7, 12 | The image build rebuilds the SPA on the e2e critical path; the e2e serves the development build while the budget gates the production build. | Recorded as deviations 5 and 6 rather than optimised. |
| 8 | `storageState` cannot carry this app's login: the access token lives in `sessionStorage`. | Both admin specs log in through the form; the token is lifted with `page.evaluate`. |
| 9 | Cart/review assertions land on another session's files, where no test hooks may be added. | Deviation 7: text-based selectors, risk flagged. |
| 10 | Three flake sources: the size radio has no default, the delivery radios stay disabled until shipping costs load, and the file input is `hidden`; also, the guest token is only minted on `/tipareste/:id`, so a spec that jumps straight to `/cos` gets bounced to login. | Encoded in the spec flow. |
| 11 | Deviation 2 cited *source* stylesheet sizes as evidence about a *built*-size budget. | Corrected: the offender list is quoted from a real production build in the test report. |
| 13 | The backlog sweep listed 5 of the 28 rows in the `tests` area. | All 28 ruled on above. |
| 14 | `webServer` default timeout of 60 s is short for a cold `ng serve` of this app. | `timeout: 180_000`. |

Confirmed correct by the same check, so not changed: Vitest will not collect `e2e/**` (the
unit-test builder globs from `sourceRoot: src`, and neither tsconfig reaches `e2e/`); port 5052
matches `environment.ts` and the API's launch profile; CORS already allows `http://localhost:4200`
with credentials, so SignalR negotiate passes; `docker compose run --rm api --seed-dev` works as
described; the broadcast is `Clients.All` and the orders page mutates the row in place, so the badge
updates; the budget numbers are safe against the measured tree.

### Human validation checkpoint (stage 1)

Self-validated 2026-09-03 per the wave-1 coordinator addendum (specsmd checkpoints are validated by
the executing session and recorded here). Outcome: **approved to implement**, with the seven
deviations above recorded rather than silently taken, and after the adversarial design check
required by `memory-bank/standards/bolt-process.md` ran and its 14 findings were folded in.
