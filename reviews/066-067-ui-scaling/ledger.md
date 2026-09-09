---
type: review-ledger
target: 066-067-ui-scaling
updated: 2026-09-09
---

# Ledger — 066-067-ui-scaling

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-762 | 🔴 | v1 | Dockerfile non-root guard drifts the runtime uid off 1001, making the existing uploads/apidata volume unwritable | `Dockerfile:36` | verified | `7256cce`, `479de38`, `3dfa20c` |
| PPW-763 | 🔴 | v1 | realtime-order.spec.ts waits on a SignalR request pattern the hub connection never produces, so the only real-time spec always times out | `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:55` | verified | `52541d1` |
| PPW-764 | 🔴 | v1 | E2E stack seeds an admin account whose password is a committed repo constant, usable on a first production deploy | `src/PhotoPrint.UI/e2e/support/stack.ts:7` | open | `324c998`, `b1b363f`, `479de38` |
| PPW-765 | 🟠 | v1 | Only about half the services route through BaseApiService (auth/guest/money/upload bypass it) yet the criterion is ticked | `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:14` | verified | `e6f4f42` |
| PPW-766 | 🟠 | v1 | retries: 1 in CI re-runs the non-idempotent realtime spec, which consumed the seed's only Paid order and can never pass on retry | `src/PhotoPrint.UI/playwright.config.ts:10` | verified | `546bc44` |
| PPW-767 | 🟠 | v1 | Guest checkout e2e stops at the review step — the Stripe-to-confirmation leg is descoped while the story criterion is ticked | `src/PhotoPrint.UI/e2e/guest-checkout.spec.ts:64` | verified | `9b1c123` |
| PPW-768 | 🟠 | v1 | Locker-selector output bindings in delivery-step left uncovered after the extraction | `src/PhotoPrint.UI/src/app/features/checkout/pages/delivery-step.ts:131` | verified | `7ace250` |
| PPW-769 | 🟠 | v1 | product-admin.service's 11 endpoints migrated to BaseApiService with zero tests | `src/PhotoPrint.UI/src/app/core/services/product-admin.service.ts:73` | verified | `5f4d434` |
| PPW-770 | 🟠 | v1 | admin.service downloadZip and getOrderPhotos migrated but left untested | `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:81` | verified | `6a8070a`, `eb6336e` |
| PPW-771 | 🟠 | v1 | Playwright smoke-tests the dev bundle (npm start), never the production build | `src/PhotoPrint.UI/playwright.config.ts:25` | deferred | |
| PPW-772 | 🟠 | v1 | Guest 401 in the error interceptor deletes the guest token and nothing re-issues it outside the upload page | `src/PhotoPrint.UI/src/app/core/interceptors/error.interceptor.ts:33` | deferred | |
| PPW-773 | 🟠 | v1 | README's "real-money smoke paths" claim overstates what the three e2e specs cover | `README.md:88` | verified | `b9ecd80`, `b1b363f`, `3dfa20c` |
| PPW-774 | 🟠 | v1 | New Stripe entries in docker-compose.yml silently override a developer's real keys from .env | `docker-compose.yml:51` | verified | `8588c62`, `b1b363f` |
| PPW-775 | 🟡 | v1 | gitleaks allowlist misses the whsec_e2e_placeholder literal and is hand-synced with hooks/pre-commit | `.gitleaks.toml:24` | backlog | |
| PPW-776 | 🟡 | v1 | anyComponentStyle budget (4kB warning / 16kB error) matches neither the story's 4kB error criterion nor current stylesheet sizes | `src/PhotoPrint.UI/angular.json:57` | backlog | |
| PPW-777 | 🟡 | v1 | webServer command hardcodes port 4200 while the wait URL is configurable, so E2E_BASE_URL cannot actually be changed | `src/PhotoPrint.UI/playwright.config.ts:25` | backlog | |
| PPW-778 | 🟡 | v1 | Locker map popup interpolates locker name/address into raw HTML | `src/PhotoPrint.UI/src/app/features/checkout/components/locker-map.ts:135` | backlog | |
| PPW-779 | 🟡 | v1 | bolt.md claims three consecutive green CI runs, contradicted by its own Runs table | `memory-bank/bolts/066-ci-quality-gates/bolt.md:74` | backlog | |
| PPW-780 | 🟡 | v1 | Implementation walkthrough records inheritance plus a shared error path, but the code uses injection and has neither | `memory-bank/bolts/067-ui-scaling-and-e2e-ui/implementation-walkthrough.md:20` | backlog | |
| PPW-781 | 🟡 | v1 | DEPLOYMENT.md file and workflow tables never mention the new e2e stack | `docs/DEPLOYMENT.md:79` | backlog | |
| PPW-782 | 🟡 | v1 | E2E_* variables documented in .env.example, which no e2e code reads | `.env.example:84` | backlog | |
| PPW-783 | 🟡 | v1 | playwright-e2e workflow has push and pull_request triggers but no concurrency group, doubling every PR run | `.github/workflows/playwright-e2e.yml:7` | backlog | |
| PPW-784 | 🟡 | v1 | Descoped e2e remainder lives only in bolt docs with no tracked follow-up | `memory-bank/bolts/067-ui-scaling-and-e2e-ui/test-walkthrough.md:73` | backlog | |
| PPW-785 | 🟡 | v1 | Profile page stylesheet copied verbatim into all three extracted child components, dead rules included | `src/PhotoPrint.UI/src/app/features/account/pages/profile/components/personal-info-form/personal-info-form.scss:1` | backlog | |
| PPW-786 | 🟡 | v1 | Blob-to-file save logic duplicated between admin.service and a page, and the two copies disagree | `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:82` | backlog | |
| PPW-787 | 🟡 | v1 | reuseExistingServer outside CI silently tests whatever server is already on the port, e.g. another worktree's | `src/PhotoPrint.UI/playwright.config.ts:27` | backlog | |
| PPW-788 | 🟡 | v1 | E2E workflow triggers leave the deploying commit as the one never smoke-tested | `.github/workflows/playwright-e2e.yml:1` | backlog | |
| PPW-789 | 🟡 | v1 | No repaint/change-detection test for the two extracted profile forms | `src/PhotoPrint.UI/src/app/features/account/pages/profile/components/personal-info-form.ts:1` | backlog | |
| PPW-790 | 🟡 | v1 | product.service getCatalog() caches the result but not the in-flight request, so concurrent callers duplicate the call | `src/PhotoPrint.UI/src/app/core/services/product.service.ts:22` | backlog | |
| PPW-791 | 🟡 | v1 | delivery-step shipping-cost subscriptions are never torn down and write shared checkout state after destroy | `src/PhotoPrint.UI/src/app/features/checkout/pages/delivery-step.ts:495` | backlog | |
| PPW-792 | 🟡 | v1 | HomePage.ngOnInit catalog subscription has no takeUntilDestroyed | `src/PhotoPrint.UI/src/app/features/home/home-page.ts:53` | backlog | |
| PPW-793 | 🟡 | v1 | Pricing teaser advertises hardcoded prices when the catalog call fails | `src/PhotoPrint.UI/src/app/features/home/components/pricing-teaser/pricing-teaser.html:20` | backlog | |
| PPW-794 | 🟡 | v1 | Order ZIP download revokes the blob URL in the same tick as the click, risking a lost download | `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:89` | backlog | |
| PPW-795 | 🟡 | v1 | New guest e2e never asserts guestSession survival, the repo's most re-found defect class | `src/PhotoPrint.UI/e2e/guest-checkout.spec.ts:40` | backlog | |
| PPW-796 | 🟡 | v1 | README e2e run recipe is POSIX-only on a Windows-primary project | `README.md:95` | backlog | |
| PPW-797 | ⚪ | v1 | Dead leftovers in profile-page after the component extraction (Router imported and injected but never used) | `src/PhotoPrint.UI/src/app/features/account/pages/profile/profile-page.ts:10` | backlog | |
| PPW-798 | ⚪ | v1 | Prettier-only reflows recorded in the walkthrough as component split wiring | `memory-bank/bolts/067-ui-scaling-and-e2e-ui/implementation-walkthrough.md:51` | backlog | |
| PPW-799 | ⚪ | v1 | BaseApiService dropped story 001's named payload and shipped one new option with no caller | `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:10` | backlog | |
| PPW-800 | ⚪ | v1 | Fix-stage ruling dismissed the gitleaks sync note after checking the wrong hook | `memory-bank/intents/030-ui-scaling-and-e2e/units/001-ci-quality-gates/construction-log.md:59` | backlog | |
| PPW-801 | ⚪ | v1 | Romanian mobile-phone regex copied into four files, with a fifth divergent rule | `src/PhotoPrint.UI/src/app/features/account/pages/profile/profile-page.ts:93` | backlog | |
| PPW-802 | ⚪ | v1 | Tier range label formatted a third time in HomePage instead of reusing the shared util | `src/PhotoPrint.UI/src/app/features/home/home-page.ts:43` | backlog | |
| PPW-803 | ⚪ | v1 | Third copy of the invalid-field helper, this one with a hand-rolled change-detection counter | `src/PhotoPrint.UI/src/app/features/account/pages/saved-addresses/components/address-form/address-form.ts:32` | backlog | |
| PPW-804 | ⚪ | v1 | Address form wrapper and action buttons duplicated for the add and edit cases | `src/PhotoPrint.UI/src/app/features/account/pages/saved-addresses/saved-addresses-page.ts:90` | backlog | |
| PPW-805 | ⚪ | v1 | Identity map in admin.service over a response that already has the target shape | `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:58` | backlog | |
| PPW-806 | ⚪ | v1 | Saved-addresses cap hardcoded in the toast text next to the constant that holds it | `src/PhotoPrint.UI/src/app/features/account/pages/saved-addresses/saved-addresses-page.ts:236` | backlog | |
| PPW-807 | ⚪ | v1 | tech-stack.md's documented count of over-budget stylesheets does not match the recorded build | `memory-bank/standards/tech-stack.md:22` | backlog | |
| PPW-808 | ⚪ | v1 | Dead account-page component reformatted instead of deleted | `src/PhotoPrint.UI/src/app/features/account/pages/account-page.ts:1` | backlog | |
| PPW-809 | ⚪ | v1 | Large Prettier reformat lands with no format script or CI check to hold it | `src/PhotoPrint.UI/package.json:9` | backlog | |
| PPW-810 | 🟠 | v4 | DEPLOYMENT.md's permission-denied remedy boots the API as root instead of chowning the uploads volume | `docs/DEPLOYMENT.md:242` | open | |
| PPW-811 | 🟠 | v4 | Playwright failure traces carrying the admin login POST body and Bearer token are uploaded as a CI artifact | `src/PhotoPrint.UI/playwright.config.ts:19` | open | |
| PPW-812 | 🟠 | v4 | The no-seeded-credential guard scans only e2e/*.ts, so the still-committed seeder pair keeps it green | `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:20` | open | |
| PPW-813 | 🟠 | v4 | RUNTIME_UID is asserted by grepping the Dockerfile, never measured against the effective identity or a writable mount | `Dockerfile:36` | open | |
| PPW-814 | 🟠 | v4 | The new handshake gate counts a transport request being issued, which may have 401'd, instead of an established hub connection | `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:48` | open | |
| PPW-815 | 🟠 | v4 | The new Paid seed order is never backfilled onto an already-seeded database, and no test covers that leg | `src/PhotoPrint.API/Data/Seed/DevDataSeed.cs:30` | open | |
| PPW-816 | 🟠 | v4 | BaseApiService's list of direct environment.apiUrl consumers omits both auth-gating interceptors | `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:18` | open | |
| PPW-817 | 🟠 | v4 | The e2e workflow re-derives the committed admin credential as a fallback, contradicting the required-no-default contract | `.github/workflows/playwright-e2e.yml:101` | open | |
| PPW-818 | 🟠 | v4 | Intent 030 was flipped to complete while its own requirements still assert the three targets this review corrected | `memory-bank/intents/030-ui-scaling-and-e2e/requirements.md:68` | open | |
| PPW-819 | 🟠 | v4 | The compose env-precedence guard reads only docker-compose.yml, never the prod overlay where precedence bites | `src/PhotoPrint.Tests/Unit/Configuration/ComposeEnvPrecedenceTests.cs:13` | open | |
| PPW-820 | 🟠 | v4 | The credential guard hard-requires the admin email and password to stay string literals in the seeder | `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:75` | open | |
| PPW-821 | 🟠 | v4 | The handshake guard pins the poll's shape but neither the signal counter's initial value nor listener ordering | `src/PhotoPrint.Tests/Unit/Configuration/RealtimeE2eHandshakeTests.cs:27` | open | |
| PPW-822 | 🟠 | v4 | The new Paid seed order is verified only on EF InMemory, so no PostgreSQL constraint is exercised | `src/PhotoPrint.Tests/Unit/Data/RealtimeE2eSeedTests.cs:41` | open | |
| PPW-823 | 🟠 | v4 | The documented E2E_ADMIN_* secrets override makes the admin specs fail, because the seeder reads no configuration | `.github/workflows/playwright-e2e.yml:94` | open | |
| PPW-824 | 🟡 | v4 | The no-fallback credential guard is a narrow-window regex that a two-line reshape defeats | `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:37` | backlog | |
| PPW-825 | 🟡 | v4 | The compose precedence regex silently skips any Stripe line it cannot parse | `src/PhotoPrint.Tests/Unit/Configuration/ComposeEnvPrecedenceTests.cs:14` | backlog | |
| PPW-826 | 🟡 | v4 | CI resolves the e2e admin email and password independently, so one rotated secret mixes sources | `.github/workflows/playwright-e2e.yml:101` | backlog | |
| PPW-827 | 🟡 | v4 | A repo-file-derived value is written to $GITHUB_ENV with a bare echo (env-injection shape) | `.github/workflows/playwright-e2e.yml:106` | backlog | |
| PPW-828 | 🟡 | v4 | Bolt 066's e2e-stability criteria stay ticked on CI evidence that predates the spec rewrites they measure | `memory-bank/bolts/066-ci-quality-gates/bolt.md:74` | backlog | |
| PPW-829 | 🟡 | v4 | README's two-Paid-orders promise holds only on a database that was never seeded before | `README.md:113` | backlog | |
| PPW-830 | 🟡 | v4 | The new apiUrl pins sit in consumer specs, not in the spec that owns url(), so the coupling stays untested | `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.spec.ts:10` | backlog | |
| PPW-831 | 🟡 | v4 | The workflow-order guard compares text offsets in the YAML instead of actual execution order | `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:54` | backlog | |
| PPW-832 | 🟡 | v4 | Story 001 is complete with all three acceptance boxes unchecked and no partial-delivery note | `memory-bank/intents/030-ui-scaling-and-e2e/units/002-ui-scaling-and-e2e-ui/stories/001-base-api-service.md:22` | backlog | |

## Details

### PPW-762 — Dockerfile non-root guard drifts the runtime uid off 1001, making the existing uploads/apidata volume unwritable

- **What:** Base aspnet:8.0-alpine already ships user 'app' (uid 1654), so both guards short-circuit and addgroup/adduser never run. Rebuilding on a host whose apidata volume was created by the old image (uid 1001) leaves /app/Storage owned by 1001; the container runs as 1654 and every upload write fails with permission denied while /health still returns 200.
- **Evidence:** `Dockerfile:36`
- **Suggested fix:** Pin the uid explicitly: ARG APP_UID=1001 and reconcile the existing user (usermod/deluser+adduser), or chown -R app:app /app/Storage in an entrypoint at start.
  - **Fix brief — files:** `Dockerfile:34-37` · `Dockerfile:42-43` · `docker-compose.yml:55-60`
  - **Fix brief — failing path:** Both guards at `Dockerfile:36-37` short-circuit on a base tag that already ships `app` (uid 1654), so uid 1001 is never created; `Dockerfile:42`'s build-time chown cannot reach the named `apidata` volume, which stays owned by 1001 from the old image, and every write under /app/Storage fails at runtime while /health stays 200.
  - **Fix brief — testShape:** A container test: create a volume owned by uid 1001, run the built image with it mounted at /app/Storage, and assert a write to /app/Storage/probe succeeds. Reddens today on a base tag whose `app` user is not 1001.
  - Not trigger-list-shaped (pins an existing uid or chowns the mount at start; changes no key scheme, concurrency model, resource budget or retry semantics, and adds no job, cache, retry, event, limiter, mapping layer or UI state machine).
- **History:**
  - v1: found by 3-lens agreement (correctness, security, completeness-critic), accepted without a skeptic; adversarial verdict `confirmed`, finder confidence 7/10
  - v1: fix round — fixed at `7256cce`, `479de38`, `3dfa20c`
  - v2: verification — held

### PPW-763 — realtime-order.spec.ts waits on a SignalR request pattern the hub connection never produces, so the only real-time spec always times out

- **What:** AdminHubService uses the default transport, so it POSTs /hubs/admin-orders/negotiate (no id=) and then opens a WebSocket carrying id=. Playwright's page.waitForRequest does not report WebSocket connections, so the predicate never matches: `await hubConnected` hits its 30s timeout and the spec fails on its first CI run.
- **Evidence:** `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:55`
- **Suggested fix:** Wait on page.on('websocket') for a URL containing /hubs/admin-orders, or drop the handshake wait and rely on the badge assertion plus the __e2eSameDocument check.
  - **Fix brief — files:** `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:26-44` · `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:55` · `src/PhotoPrint.UI/src/app/core/services/admin-hub.service.ts:54`
  - **Fix brief — failing path:** `admin-hub.service.ts:54` connects with the default transport, so the only request Playwright sees is the `/negotiate` POST without `id=`; the spec's predicate at line 34 requires `id=`, which only the WebSocket upgrade carries, and `page.waitForRequest` never reports WebSockets — so `await hubConnected` times out at 30s.
  - **Fix brief — testShape:** Run the spec against a live stack and require it green: the load-bearing assertion is the badge text at `realtime-order.spec.ts:55` (`toHaveText('În tipărire')`). Reverting the wait to a `waitForRequest` predicate needing `id=` must time out again.
  - Not trigger-list-shaped (swaps one Playwright wait predicate for another inside a test file; adds no production mechanism).
- **History:**
  - v1: found by 3-lens agreement (correctness, tests-coverage, completeness-critic), accepted without a skeptic; adversarial verdict `confirmed`, finder confidence 7/10
  - v1: fix round — fixed at `ecceaa2`, `d14b936`
  - v2: verification — reopened (no-guard-ci-only)
  - v2: fix round — fixed at `52541d1`
  - v3: verification — held

### PPW-764 — E2E stack seeds an admin account whose password is a committed repo constant, usable on a first production deploy

- **What:** DEPLOYMENT.md:200 runs `--seed` on first deploy; ProductCatalogSeed.cs:32-99 creates mateibarba@yahoo.com / Admin1234! with Role=Admin, IsEmailConfirmed=true. The new e2e defaults publish the same pair again. Anyone reading the repo logs in at /auth/login and gets every customer order, PII, photo ZIPs and status changes. No post-deploy step rotates it.
- **Evidence:** `src/PhotoPrint.UI/e2e/support/stack.ts:7` — REAL. Program.cs:331 runs ProductCatalogSeed.ApplyAsync with no environment guard; lines 32-33 hardcode mateibarba@yahoo.com / Admin1234! and 116-130 set Role=Admin, IsEmailConfirmed=true, no forced reset. docs/DEPLOYMENT.md:200 runs --seed on first deploy; its checklist (lines 204-214) never rotates. Anyone reading the repo POSTs /api/auth/login and reaches every [Authorize(Roles="Admin")] controller. Root cause is the seed; stack.ts:6-7 only re-publishes the pair (env-overridable).
- **Suggested fix:** Read the seed admin password from configuration with no default (fail if unset in Production); keep e2e reading only E2E_ADMIN_PASSWORD; add a rotate-admin-password item to the post-deploy checklist.
  - **Fix brief — files:** `src/PhotoPrint.UI/e2e/support/stack.ts:6-7` · `src/PhotoPrint.API/Data/Seed/ProductCatalogSeed.cs:32-33` · `src/PhotoPrint.API/Data/Seed/ProductCatalogSeed.cs:116-130` · `src/PhotoPrint.API/Program.cs:331-338` · `docs/DEPLOYMENT.md:198-214` · `src/PhotoPrint.API/Controllers/AdminOrdersController.cs:11`
  - **Fix brief — failing path:** `stack.ts:6-7` defaults the admin credentials to the seeder's compile-time pair; `ProductCatalogSeed.cs:32-33` hardcodes `mateibarba@yahoo.com` / `Admin1234!` and lines 116-130 create it with Role=Admin and IsEmailConfirmed=true; `Program.cs:331` runs the seeder with no environment guard and `docs/DEPLOYMENT.md:200` runs `--seed` on a first deploy with no rotation step.
  - **Fix brief — testShape:** SeedAdminPasswordIsNotACompileTimeConstant: arrange Production env without ADMIN_SEED_PASSWORD; act ProductCatalogSeed.ApplyAsync; assert it throws (or creates no admin) instead of hashing "Admin1234!" — reddens today since the constant is used unconditionally.
  - **Trigger-list-shaped:** yes (changes the credential-provisioning scheme and adds a fail-closed startup guard).
- **History:**
  - v1: found by 1 lens (security); adversarial verdict `confirmed`, finder confidence 8/10
  - v1: Approach pre-check: revised — fix only `e2e/support/stack.ts` here (drop both literal defaults, require `E2E_ADMIN_EMAIL`/`E2E_ADMIN_PASSWORD`, set them in `playwright-e2e.yml` and `docker-compose.e2e.yml`); the seeder half is a backend change owned by intent 033 unit 002 stories 003/004, must pass credentials into `ApplyAsync` rather than read config statically (`ProductCatalogFactory.cs:78` calls it with a DbContext only), must sit inside the `if (!await db.Users.AnyAsync(...))` at line 85 and not in options validation, and cannot be a Production-only throw because `docker-compose.yml:42` pins Development in CI.
  - v1: fix round — fixed at `324c998`, `b1b363f`, `479de38`
  - v2: verification — held
  - v4: re-found by 2 lenses (security, completeness-critic) at `src/PhotoPrint.API/Data/Seed/ProductCatalogSeed.cs:32-33`; adversarial verdict `confirmed`, finder confidence 9/10. The verified fix held on its own lever — the prior decision was to "fix only `e2e/support/stack.ts` here … the seeder half is a backend change owned by intent 033 unit 002 stories 003/004" — so nothing regressed, but the committed admin pair still creates a production Admin account through the documented `--seed` at `docs/DEPLOYMENT.md:200`. Status back to `open`; area `auth`.

### PPW-765 — Only about half the services route through BaseApiService (auth/guest/money/upload bypass it) yet the criterion is ticked

- **What:** auth, guest-auth, cart, payment, upload and admin-hub still build URLs from environment.apiUrl directly, so "one place that owns URL composition" is false. product-admin.service.ts was migrated and has no spec file at all, so its URL and body shapes are unverified. A future apiUrl shape change must be validated twice, and reviewers reading the diff see a finished migration.
- **Evidence:** `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:14`
- **Suggested fix:** State in the doc comment which services are migrated, and add a product-admin.service.spec.ts asserting each endpoint URL via HttpTestingController.
  - **Fix brief — files:** `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:14` · `src/PhotoPrint.UI/src/app/core/services/auth.service.ts` · `src/PhotoPrint.UI/src/app/core/services/guest-auth.service.ts` · `src/PhotoPrint.UI/src/app/core/services/cart.service.ts` · `src/PhotoPrint.UI/src/app/core/services/payment.service.ts` · `src/PhotoPrint.UI/src/app/core/services/upload.service.ts` · `src/PhotoPrint.UI/src/app/core/services/admin-hub.service.ts:54` · `memory-bank/bolts/067-ui-scaling-and-e2e-ui/bolt.md`
  - **Fix brief — failing path:** The doc comment at `base-api.service.ts:14` claims one owner for URL composition, but auth, guest-auth, cart, payment, upload and admin-hub still read `environment.apiUrl` themselves, so an apiUrl shape change has to be validated in two places and the bolt criterion reads as finished.
  - **Fix brief — testShape:** `product-admin.service.spec.ts` with HttpTestingController: per endpoint, call the method and `expectOne(`${environment.apiUrl}/...`)` asserting the exact method and URL. Reddens on any URL or verb drift. The doc-comment half is verified by reading, not a test.
  - Not trigger-list-shaped (a doc-comment correction plus new HttpTestingController specs; no mechanism added or scheme changed).
- **History:**
  - v1: found by 3-lens agreement (requirements, quality, completeness-critic), accepted without a skeptic; adversarial verdict `confirmed`, finder confidence 7/10
  - v1: fix round — fixed at `e6f4f42`
  - v2: verification — held

### PPW-766 — retries: 1 in CI re-runs the non-idempotent realtime spec, which consumed the seed's only Paid order and can never pass on retry

- **What:** realtime-order.spec.ts PATCHes the seed's single Paid order to Printing. If attempt 1 fails after that PATCH (flaky assert, timeout), retries:1 reruns it against the same DB: no Paid order remains, so the retry fails at order lookup and CI reports the spec's own "run the seed on a clean DB" message, masking the original failure.
- **Evidence:** `src/PhotoPrint.UI/playwright.config.ts:10` — CI=true → retries:1 (config line 11). Seed runs once before the run (workflow lines 71-77) and self-skips on a dirty DB; DevDataSeed has exactly one Paid order (o6, line 289). Attempt 1 PATCHes it to Printing (spec line 50), then flakes on the 20s SignalR badge assert (line 55). The retry re-queries /admin/orders, finds no Paid row, and fails at toBeDefined() with the "rulează seed-ul" message. Caveat: Playwright's report keeps attempt 1's error, so masking is partial.
- **Suggested fix:** Set retries: 0 for the realtime spec, or have it create/reset its own order (re-seed or PATCH back to Paid in a fixture teardown) so it is idempotent.
  - **Fix brief — files:** `src/PhotoPrint.UI/playwright.config.ts:11` · `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:26` · `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:50` · `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:55` · `src/PhotoPrint.API/Data/Seed/DevDataSeed.cs:289` · `.github/workflows/playwright-e2e.yml:71`
  - **Fix brief — failing path:** `playwright.config.ts:11` sets `retries: 1` under CI; the workflow seeds once (`playwright-e2e.yml:71-77`) and `DevDataSeed.cs:34-38` self-skips on a seeded DB; `DevDataSeed.cs:289` holds the only Paid order, which attempt 1 PATCHes to Printing (`realtime-order.spec.ts:50`) before flaking on the 20s badge wait at line 55 — attempt 2 can never see Paid again.
  - **Fix brief — testShape:** Vitest `playwright.config.spec.ts`: arrange CI=1, import the config; assert the realtime spec runs with zero retries (project/spec-level `retries === 0`). Reddens while the global `retries: isCi ? 1 : 0` applies to it.
  - **Trigger-list-shaped:** yes (changes retry semantics, which the list names explicitly).
- **History:**
  - v1: found by 2 lens (correctness, tests-coverage); adversarial verdict `confirmed`, finder confidence 7/10
  - v1: Approach pre-check: revised — keep `retries: 1` and drop the teardown and re-seed ideas outright (`OrderStatusMachine.cs:18-29` has no `Printing → Paid` edge, and `DevDataSeed.cs:34-38` makes a second `--seed-dev` a no-op that breaks the workflow's own grep). Instead pin the spec to the seeded order (`Order6Id` / `FT-2026-0006`) and assert its status so a consumed run fails with a readable message, switch `trace` to `'retain-on-failure'` (line 19's `on-first-retry` is what actually loses the diagnostic), and add a second Paid order to `DevDataSeed` so the retry has a fresh target; per-spec `test.describe.configure({ retries: 0 })` is supported but alone trades away the retry's genuine rescue of the spec's pre-PATCH half.
  - v1: fix round — fixed at `546bc44`
  - v2: verification — held

### PPW-767 — Guest checkout e2e stops at the review step — the Stripe-to-confirmation leg is descoped while the story criterion is ticked

- **What:** Story 002 (Must) names "guest → Stripe test mode → confirmation". The spec stops at the delivery/review summary; no e2e touches payment, order creation, or /checkout/confirmare. bolt.md:74 ticks "3 e2e pass" with no note of the gap, so a merge gate reading the bolt record believes the money path is covered when the money half is untested.
- **Evidence:** `src/PhotoPrint.UI/e2e/guest-checkout.spec.ts:64` — Real. guest-checkout.spec.ts ends at /checkout/recapitulare (lines 68-73); grepping e2e/ for stripe, plata, confirmare returns nothing. The app has /checkout/plata (Stripe card, "Plătește acum") and /comanda/:orderId/confirmare. Break payWithStripe or order creation and all three specs stay green, so bolt.md:74's ticked "3 e2e pass" gates a merge on an untested money leg. Story AC literally says "guest → Stripe test mode → confirmation". Only slip: route is /comanda/:id/confirmare, not /checkout/confirmare.
- **Suggested fix:** Annotate the bolt.md criterion and story 002 with the unproven Stripe/confirmation leg, and file the STRIPE_TEST_* follow-up so the gap has an owner.
  - **Fix brief — files:** `src/PhotoPrint.UI/e2e/guest-checkout.spec.ts:68` · `src/PhotoPrint.UI/src/app/features/checkout/checkout.routes.ts:18` · `src/PhotoPrint.UI/src/app/features/checkout/pages/payment-step.ts:43` · `src/PhotoPrint.UI/src/app/app.routes.ts:35` · `memory-bank/intents/030-ui-scaling-and-e2e/units/001-ci-quality-gates/stories/002-playwright-e2e-smoke-tests.md:22` · `memory-bank/bolts/066-ci-quality-gates/bolt.md:74`
  - **Fix brief — failing path:** `guest-checkout.spec.ts:68-73` ends at `/checkout/recapitulare`; nothing in `e2e/` mentions plata, stripe or confirmare, so breaking `payWithStripe` or order creation leaves all three specs green while `bolt.md:74` ticks the criterion.
  - **Fix brief — testShape:** Extend guest-checkout.spec.ts past recapitulare: continue to /checkout/plata, fill Stripe test card 4242…, click "Plătește acum", assert URL /comanda/<uuid>/confirmare and order number renders. Reddens whenever the payment/order-creation leg breaks.
  - Not trigger-list-shaped (extends an e2e spec and corrects a ticked criterion; no production mechanism).
- **History:**
  - v1: found by 2 lens (requirements, tests-coverage); adversarial verdict `confirmed`, finder confidence 9/10
  - v1: fix round — fixed at `9b1c123`
  - v2: verification — held

### PPW-768 — Locker-selector output bindings in delivery-step left uncovered after the extraction

- **What:** Delete (lockerSelected) or (retry) on <app-locker-selector>. All 126 specs stay green: delivery-step.spec.ts calls comp.selectLocker()/comp.retrySearch() directly and only asserts .easybox-section exists; locker-selector.spec.ts only asserts the outputs emit. Easybox customers can never select a locker; guest-checkout e2e uses Courier only.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/checkout/pages/delivery-step.ts:131` — Verified empirically: I deleted both (lockerSelected) and (retry) from delivery-step.ts:137-138 and ran the two specs — 35/35 passed, then restored the file. Nothing renders the child's .locker-item or .retry-link through the parent: delivery-step.spec calls comp.selectLocker()/retrySearch() directly and asserts only .easybox-section (the child's own root div). Live result: an Easybox buyer's click never reaches selectLocker, so selectedLockerId stays null and Continue stays disabled forever.
- **Suggested fix:** In delivery-step.spec.ts click .locker-item and .retry-link through the rendered DOM and assert selectedLockerId()/re-search, instead of invoking the handlers.
  - **Fix brief — files:** `src/PhotoPrint.UI/src/app/features/checkout/pages/delivery-step.ts:131-139` · `src/PhotoPrint.UI/src/app/features/checkout/pages/delivery-step.ts:529-538` · `src/PhotoPrint.UI/src/app/features/checkout/components/locker-selector.html:13` · `src/PhotoPrint.UI/src/app/features/checkout/components/locker-selector.html:32` · `src/PhotoPrint.UI/src/app/features/checkout/pages/delivery-step.spec.ts:149` · `src/PhotoPrint.UI/src/app/features/checkout/components/locker-selector.spec.ts:52`
  - **Fix brief — failing path:** `delivery-step.ts:137-138` binds `(lockerSelected)` and `(retry)`, but `delivery-step.spec.ts:149` calls `comp.selectLocker()` / `comp.retrySearch()` directly and `locker-selector.spec.ts:52` only checks the child emits — nothing drives the child's `.locker-item` or `.retry-link` through the parent.
  - **Fix brief — testShape:** In delivery-step.spec, after selectEasybox(fixture, [locker('l1')]), click the rendered .locker-item button and expect comp.selectedLockerId()==='l1'; with searchFailed set, click .retry-link and expect a new lockers?city= request.
  - Not trigger-list-shaped (test-only: drives the existing DOM instead of calling handlers).
- **History:**
  - v1: found by 1 lens (tests-coverage); adversarial verdict `confirmed`, finder confidence 8/10
  - v1: fix round — fixed at `7ace250`
  - v2: verification — held

### PPW-769 — product-admin.service's 11 endpoints migrated to BaseApiService with zero tests

- **What:** No product-admin.service.spec.ts exists and its only consumer admin-products-page.ts has no spec. Swap api.put for api.post in replacePricingTiers, or drop a leading slash, and the suite, the production build and all three Playwright specs stay green while every admin price/size/finish edit 404s.
- **Evidence:** `src/PhotoPrint.UI/src/app/core/services/product-admin.service.ts:73` — Confirmed. No product-admin.service.spec.ts and no admin-products-page.spec.ts exist; no UI spec mentions admin/products; the three e2e specs never leave /admin's dashboard. Change replacePricingTiers's api.put to api.post — identical signature, so tsc and the build pass — and the page's save-pricing call hits POST .../sizes/{sizeId}/pricing while the controller declares [HttpPut], so every tier save fails. The leading-slash half is false: url() normalizes a missing slash.
- **Suggested fix:** Add product-admin.service.spec.ts with HttpTestingController: assert method, full absolute URL and body for all 11 methods, mirroring shipping.service.spec.ts.
  - **Fix brief — files:** `src/PhotoPrint.UI/src/app/core/services/product-admin.service.ts:73` · `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:27` · `src/PhotoPrint.API/Controllers/AdminProductsController.cs:103` · `src/PhotoPrint.UI/src/app/features/admin/pages/products/admin-products-page.ts:343` · `src/PhotoPrint.UI/e2e/admin-login.spec.ts:8` · `src/PhotoPrint.UI/src/app/features/admin/pages/admin-page.spec.ts:6`
  - **Fix brief — failing path:** No `product-admin.service.spec.ts` or `admin-products-page.spec.ts` exists, no UI spec mentions admin/products, and the three e2e specs never leave the admin dashboard — so swapping `replacePricingTiers`'s `api.put` for `api.post` compiles, builds and leaves every suite green while the save silently 404s.
  - **Fix brief — testShape:** product-admin.service.spec.ts with HttpTestingController: call replacePricingTiers('p','s',{tiers:[]}), then expectOne(`${environment.apiUrl}/admin/products/p/sizes/s/pricing`) and assert req.request.method === 'PUT'. Reddens on any verb or path change.
  - Not trigger-list-shaped (test-only, one new spec file).
- **History:**
  - v1: found by 1 lens (tests-coverage); adversarial verdict `confirmed`, finder confidence 9/10
  - v1: fix round — fixed at `5f4d434`
  - v2: verification — held

### PPW-770 — admin.service downloadZip and getOrderPhotos migrated but left untested

- **What:** admin.service.spec.ts covers 9 methods, not downloadZip; order.service.spec.ts covers getOrders/getOrderDetail, not getOrderPhotos. Replace api.getBlob with api.get and the ZIP arrives as parsed text, corrupting every admin order download, with nothing red. Same for the presigned photo endpoint.
- **Evidence:** `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:81` — Real, but mutation-only — current code is correct. admin.service.spec covers 9 methods, never downloadZip; admin-order-detail-page.spec never mentions it. Swap line 81 to api.get<Blob> (a bare api.get won't compile) and every UI spec stays green while HttpClient JSON-parses the ZIP bytes and throws. base-api.service.spec:117 does cover getBlob itself. getOrderPhotos (order.service.ts:30) is JSON, not blob — that half is mis-stated, though its URL is still untested (page specs mock the service).
- **Suggested fix:** Add specs asserting request.responseType === 'blob' and the absolute URL for downloadZip, plus URL and method for getOrderPhotos.
  - **Fix brief — files:** `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:80-93` · `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:34` · `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.spec.ts:117-122` · `src/PhotoPrint.UI/src/app/core/services/admin.service.spec.ts:24-152` · `src/PhotoPrint.UI/src/app/core/services/order.service.ts:29-31` · `src/PhotoPrint.UI/src/app/features/admin/pages/order-detail/admin-order-detail-page.ts:141-149`
  - **Fix brief — failing path:** `admin.service.spec.ts` covers nine methods and never `downloadZip` (`admin.service.ts:80-93`), and the order-detail page spec never mentions it — so changing line 81 to `api.get<Blob>` keeps every spec green while HttpClient JSON-parses the ZIP bytes and throws at runtime.
  - **Fix brief — testShape:** In admin.service.spec, 'downloadZip requests a blob': call downloadZip('o1','2026-1'), expectOne(`${base}/orders/o1/download-zip`), assert request.responseType === 'blob', flush a Blob. Reddens on any getBlob→get swap.
  - Not trigger-list-shaped (test-only).
- **History:**
  - v1: found by 1 lens (tests-coverage); adversarial verdict `confirmed`, finder confidence 8/10
  - v1: fix round — fixed at `6a8070a`, `eb6336e`
  - v2: verification — held

### PPW-771 — Playwright smoke-tests the dev bundle (npm start), never the production build

- **What:** webServer runs npm start, so ng serve's development configuration with environment.ts. The production bundle that deploy.yml ships — environment.prod.ts's https://api.fototipar.ro/api, optimizer output, lazy chunks — is never exercised by any spec. A prod-only bundling or env break reaches deploy with three green smoke specs.
- **Evidence:** `src/PhotoPrint.UI/playwright.config.ts:25` — playwright.config.ts:25 runs `npm start`; angular.json:81 defaults serve to build:development, so no fileReplacements — specs hit environment.ts (localhost:5052). The shipped image (Dockerfile:27,40) bakes environment.prod.ts: apiUrl https://api.fototipar.ro/api, stripePublishableKey 'pk_live_placeholder', consumed at base-api.service.ts:26 with no runtime override. Break any of those and all three specs stay green. Caveat: ci.yml:107 does run the production build, so compile/budget breaks still fail CI before deploy.
- **Suggested fix:** Add a CI job that builds --configuration=production, serves dist/ statically with the API base rewritten to the local API, and runs the same three specs.
  - **Fix brief — files:** `src/PhotoPrint.UI/playwright.config.ts:25` · `src/PhotoPrint.UI/angular.json:81` · `src/PhotoPrint.UI/angular.json:43` · `src/PhotoPrint.UI/src/environments/environment.prod.ts:3` · `Dockerfile:27` · `.github/workflows/ci.yml:107`
  - **Fix brief — failing path:** `playwright.config.ts:25` runs `npm start`, and `angular.json:81` defaults serve to `build:development`, so no fileReplacements apply and the specs exercise `environment.ts` (localhost:5052); the shipped image (`Dockerfile:27`) bakes `environment.prod.ts` with `https://api.fototipar.ro/api` and a placeholder Stripe key, which no spec ever loads.
  - **Fix brief — testShape:** Add a Playwright project serving the production bundle (`ng build --configuration=production`, static host on 4200): guest-checkout.spec reddens today — baked apiUrl api.fototipar.ro means the catalog never loads.
  - Not trigger-list-shaped (adds a CI job and a Playwright project; no application mechanism, retry, cache or limiter).
- **History:**
  - v1: found by 1 lens (tests-coverage); adversarial verdict `confirmed`, finder confidence 8/10
  - v1: fix round — deferred

### PPW-772 — Guest 401 in the error interceptor deletes the guest token and nothing re-issues it outside the upload page

- **What:** A guest's stored guestToken has expired. guestOrAuthGuard passes (token present); an OrderService call in checkout 401s; the interceptor silently deletes the token. The only re-issue path (ensureGuestSession) lives in format-selector-page, so a retry 401s again and the next navigation bounces them to /auth/login — a page a guest has no account for.
- **Evidence:** `src/PhotoPrint.UI/src/app/core/interceptors/error.interceptor.ts:33` — Guest on /checkout/plata with an expired guestSession row. createIntent POSTs /payments/stripe/intent (DualAuth); GuestAuthenticationHandler fails the stale session -> 401. Interceptor (not authenticated) calls clearGuestToken. Retry sends no header -> NoResult -> 401 again. No re-issue exists in checkout: ensureGuestSession lives only in format-selector-page, and guest-checkout-form's createGuestSession is reachable only via GuestCheckoutPromptComponent, which no route or template mounts. Next /checkout navigation: guard sees null token -> /auth/login. REAL.
- **Suggested fix:** Move the anonymous re-init into GuestAuthService as a deduped ensureSession(), call it from the 401 guest branch, and send token-less guests to /tipareste rather than /auth/login.
  - **Fix brief — files:** `src/PhotoPrint.UI/src/app/core/interceptors/error.interceptor.ts:33` · `src/PhotoPrint.UI/src/app/core/interceptors/guest.interceptor.ts:21` · `src/PhotoPrint.UI/src/app/core/guards/guest-or-auth.guard.ts:14` · `src/PhotoPrint.UI/src/app/features/checkout/pages/payment-step.ts:137` · `src/PhotoPrint.UI/src/app/features/upload/pages/format-selector/format-selector-page.ts:194` · `src/PhotoPrint.API/Authentication/GuestAuthenticationHandler.cs:44`
  - **Fix brief — failing path:** A guest on `/checkout/plata` with an expired session POSTs `/payments/stripe/intent`; `GuestAuthenticationHandler.cs:44` fails the stale session, the interceptor's non-authenticated branch (`error.interceptor.ts:33`) calls `clearGuestToken`, the retry sends no header and 401s again, and `guest-or-auth.guard.ts:14` then routes the guest to `/auth/login` — no re-issue exists outside the upload page (`format-selector-page.ts:194`).
  - **Fix brief — testShape:** "guest 401 in checkout leaves no session": arrange expired guestSession + PaymentStep; act flush 401 on /payments/stripe/intent; assert getGuestToken() null and guestOrAuthGuard('/checkout') returns a UrlTree to /auth/login with no re-init request.
  - **Trigger-list-shaped:** yes (the suggested fix adds a refresh/self-heal state machine with in-flight dedup — two entries on the list).
- **History:**
  - v1: found by 1 lens (frontend-ux), topic hinted by the shared prompt; adversarial verdict `confirmed`, finder confidence 7/10
  - v1: Approach pre-check: refuted as suggested — the guest token *is* the `GuestSessions` row id (`GuestAuthenticationHandler.cs:34`), so re-issuing mints a new identity: `OrderService.cs:87-100` then throws "Coșul este gol.", the uploads and cart rows keyed to the dead session are unreachable, `GuestEmail` (`OrderService.cs:166-170`) comes back empty, and `FindKeyHolderAsync` (`OrderService.cs:350-368`) scopes payment idempotency by session id, so the 409-with-orderId redirect at `payment-step.ts:155` stops firing. Take the bounce-only fix in the testShape instead: route a token-less guest to `/tipareste` and tell them the session expired and the photos must be re-uploaded. No interceptor-driven session minting on the money path.
  - v1: fix round — deferred

### PPW-773 — README's "real-money smoke paths" claim overstates what the three e2e specs cover

- **What:** No spec places an order: guest-checkout stops at the review step, realtime-order mutates a pre-seeded order. Order creation, Stripe intent, webhook, invoice and AWB stay uncovered end-to-end — and both compose files pin placeholder Stripe keys, so this stack cannot cover them. A reviewer reads "real-money paths covered" and stops looking.
- **Evidence:** `README.md:88` — No execution fails. The sub-facts hold: no spec places an order, realtime mutates a seeded order via PATCH, and e2e Stripe keys are placeholders. But README:88 enumerates its own scope in the same sentence — "guest checkout up to the review step, admin login, and an admin order status change over SignalR" — so a reader isn't told order creation, Stripe, webhook, invoice or AWB are covered. Only the adjective "real-money" is loose; a doc-wording nit, not a defect.
- **Suggested fix:** Reword README/tech-stack.md to "pre-payment funnel + admin paths", and record order placement / payment as the named next e2e gap.
  - **Fix brief — files:** `README.md:88` · `src/PhotoPrint.UI/e2e/guest-checkout.spec.ts:69` · `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:47` · `src/PhotoPrint.UI/e2e/admin-login.spec.ts:16` · `docker-compose.e2e.yml:29`
  - **Fix brief — failing path:** `README.md:88` enumerates its own scope in the same sentence, so the overstatement is the words "real-money smoke paths" over a funnel that stops at the review step; no execution fails.
  - **Fix brief — testShape:** No test — a documentation wording change, verified by reading. The named coverage gap it should record is proven by PPW-767's testShape.
  - Not trigger-list-shaped (documentation wording).
- **History:**
  - v1: found by 1 lens (completeness-critic); adversarial verdict `plausible`, finder confidence 8/10
  - v1: fix round — fixed at `b9ecd80`, `b1b363f`, `3dfa20c`
  - v2: verification — held

### PPW-774 — New Stripe entries in docker-compose.yml silently override a developer's real keys from .env

- **What:** Compose gives `environment:` precedence over `env_file:`. A developer who filled Stripe__SecretKey with a real sk_test_ key in .env (as .env.example line 44 instructs) now runs with sk_test_placeholder: payment-intent creation returns a Stripe auth error and webhook signature verification fails, in a dev stack that worked before.
- **Evidence:** `docker-compose.yml:51` — Dev writes Stripe__SecretKey=sk_test_51real into .env. api lists env_file: .env (line 39) then environment: Stripe__SecretKey: sk_test_placeholder (line 51); Compose gives environment precedence, so the container var is the placeholder. Program.cs:207 constructs StripeClient("sk_test_placeholder") — payment-intent creation returns a Stripe 401, and WebhooksController.cs:91 verifies against whsec_placeholder, so signatures fail. Note Stripe__PublishableKey is not overridden, leaving a real/placeholder mismatch.
- **Suggested fix:** Use defaults that respect .env: `Stripe__SecretKey: ${Stripe__SecretKey:-sk_test_placeholder}` and the same for Stripe__WebhookSecret.
  - **Fix brief — files:** `docker-compose.yml:39` · `docker-compose.yml:51` · `.env.example:44` · `src/PhotoPrint.API/Program.cs:207` · `src/PhotoPrint.API/Controllers/WebhooksController.cs:91` · `src/PhotoPrint.API/appsettings.Development.json:22`
  - **Fix brief — failing path:** `docker-compose.yml:39` lists `env_file: .env` and line 51 sets `Stripe__SecretKey: sk_test_placeholder` under `environment:`; Compose gives `environment:` precedence, so a developer's real key in `.env` is overridden and `Program.cs:207` builds a StripeClient on the placeholder.
  - **Fix brief — testShape:** Shell test render_compose_keeps_env_stripe_key: arrange .env with Stripe__SecretKey=sk_test_real; act `docker compose config`; assert api service env shows sk_test_real, not sk_test_placeholder.
  - Not trigger-list-shaped (a Compose interpolation default such as `${Stripe__SecretKey:-sk_test_placeholder}`; changes no scheme or semantics in code).
- **History:**
  - v1: found by 1 lens (completeness-critic); adversarial verdict `confirmed`, finder confidence 8/10
  - v1: fix round — fixed at `8588c62`, `b1b363f`
  - v2: verification — held

### PPW-775 — gitleaks allowlist misses the whsec_e2e_placeholder literal and is hand-synced with hooks/pre-commit

- **What:** docker-compose.e2e.yml sets Stripe__WebhookSecret: whsec_e2e_placeholder, but the new allowlist regexes cover only sk_test_e2e_placeholder, sk_test_placeholder and whsec_placeholder. A generic-api-key/webhook rule matching that literal reddens secret-scan.yml on every push and blocks the branch.
- **Evidence:** `.gitleaks.toml:24`
- **Suggested fix:** Add `whsec_e2e_placeholder` to the allowlist regexes and mirror the same entry into hooks/pre-commit, as the file header requires.
- **History:**
  - v1: found by 3-lens agreement (correctness, quality, completeness-critic), accepted without a skeptic; adversarial verdict `confirmed`, finder confidence 4/10
  - v1: fix round — backlog

### PPW-776 — anyComponentStyle budget (4kB warning / 16kB error) matches neither the story's 4kB error criterion nor current stylesheet sizes

- **What:** Story 001 AC asks for anyComponentStyle maximumError: 4kB; the file ships 4 kB warn / 16 kB error, and test-walkthrough.md:140 marks "Budgets set and enforced" ✅. A component stylesheet growing from 4 kB to 15 kB therefore prints a warning CI ignores, which is exactly the bloat the story exists to block.
- **Evidence:** `src/PhotoPrint.UI/angular.json:57` — Story AC (001-bundle-size-budget.md) requires anyComponentStyle maximumError:4kB. angular.json:56-58 sets warn:4kB/error:16kB instead. Running `npm run build` now: 5 stylesheets (admin-products-page.scss 13.97kB, header.scss 6.68kB, etc.) already exceed 4kB and print only WARNING, build exits 0 — CI's web job treats this as pass. test-walkthrough.md:140 marks this ✅ despite the mismatch. Not hypothetical: today's build already lives in the warn-only gap.
- **Suggested fix:** Record the 4 kB→16 kB substitution on the ticked criterion (not only in the plan's deviation list) and file the promotion to maximumError as follow-up.
- **History:**
  - v1: found by 2 lens (requirements, quality); adversarial verdict `confirmed`, finder confidence 8/10
  - v1: fix round — backlog

### PPW-777 — webServer command hardcodes port 4200 while the wait URL is configurable, so E2E_BASE_URL cannot actually be changed

- **What:** A dev follows .env.example and sets E2E_BASE_URL=http://localhost:4300. Playwright still runs `npm start -- --port 4200` but waits on :4300, so every run burns the 180s webServer timeout and fails before a single spec executes.
- **Evidence:** `src/PhotoPrint.UI/playwright.config.ts:25` — Set E2E_BASE_URL=http://localhost:4300, run `npx playwright test`. Line 3 sets baseURL=4300; line 25 still runs `npm start -- --port 4200` (package.json:6 start=ng serve, no port override elsewhere); line 26 polls baseURL (4300). Server listens on 4200, Playwright waits on 4300, forever mismatched until the 180s timeout (line 27) fires and the run fails before any spec executes.
- **Suggested fix:** Derive the port from baseURL (`new URL(baseURL).port`) and pass it to `npm start -- --port`, or drop the env override and document 4200 as fixed.
- **History:**
  - v1: found by 2 lens (quality, tests-coverage); adversarial verdict `confirmed`, finder confidence 8/10
  - v1: fix round — backlog

### PPW-778 — Locker map popup interpolates locker name/address into raw HTML

- **What:** The extracted locker-selector now composes app-locker-map, whose bindPopup(`<strong>${locker.name}</strong><br>${locker.address}`) sets innerHTML. Lockers come from StaticShippingService constants today, so it is not exploitable; the first time GET /api/shipping/lockers is backed by the Sameday API or an admin-editable table, a name of `<img src=x onerror=...>` runs script on the checkout page that holds the guest token and cart.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/checkout/components/locker-map.ts:135` — Sink is real: locker-map.ts:135 does `.bindPopup(`<strong>${locker.name}</strong><br>${locker.address}`)`, and Leaflet's bindPopup(string) sets it via innerHTML — unescaped locker.name/address would execute as HTML/script. But the only wired source, LockerDto[] from GET /api/shipping/lockers, is StaticShippingService's hardcoded constants; SamedayShippingService.GetLockersAsync (confirmed by reading it) still delegates straight to the same static fallback, not a live Sameday call or admin table. No code path today feeds attacker-controlled name/address into this component, so there's no current exploit — only the latent risk the finding itself flags for a future wiring change.
- **Suggested fix:** Build the popup from DOM nodes with textContent (or escape the two fields) instead of a template string, before locker data stops being developer-controlled.
- **History:**
  - v1: found by 1 lens (security); adversarial verdict `plausible`, finder confidence 7/10
  - v1: fix round — backlog

### PPW-779 — bolt.md claims three consecutive green CI runs, contradicted by its own Runs table

- **What:** The stability AC ("~3 min and is stable, no flakes") is ticked on evidence the bolt's test-walkthrough refutes: its Runs table shows one green e2e run out of four; the three green runs cited are ci.yml runs, not the e2e workflow. With retries:1 in CI a flaky spec still reports green, so flakiness is untested but recorded as proven.
- **Evidence:** `memory-bank/bolts/066-ci-quality-gates/bolt.md:74` — bolt.md:74 checks "3 e2e pass in CI... (16.7 s, three consecutive green runs)" as proof of the source AC "completes within ~3 min and is stable (no flakes)" (story 002-playwright-e2e-smoke-tests.md:24). But test-walkthrough.md's own Runs table (lines 155-163) shows 4 e2e runs, not 3 green: build-failed, 1/3 passed, 2/3 passed, then 3/3 passed once. The "three consecutive green runs" actually names ci.yml (budget) runs cited at line 54-57 — a different workflow — so bolt.md conflates budget-gate stability with e2e-suite stability. With retries:1 (implementation-plan.md:65/146), a flaky spec reports green anyway, so "no flakes" is asserted on one clean e2e run, not three.
- **Suggested fix:** Correct the claim to one green e2e run, or get two more green e2e runs before ticking; note that retries:1 masks flakes in the same line.
- **History:**
  - v1: found by 1 lens (requirements); adversarial verdict `confirmed`, finder confidence 9/10
  - v1: fix round — backlog

### PPW-780 — Implementation walkthrough records inheritance plus a shared error path, but the code uses injection and has neither

- **What:** Lines 20 and 68 say BaseApiService "is extended, not injected" (also test-walkthrough:80, "the six data services extend it"), and line 32 credits it with "one error path for every verb". The services use inject(BaseApiService) and the base deliberately has no catchError. A maintainer trusting the record extends the class or skips error handling that errorInterceptor actually owns.
- **Evidence:** `memory-bank/bolts/067-ui-scaling-and-e2e-ui/implementation-walkthrough.md:20` — A maintainer reads implementation-walkthrough.md:20 ("extended, not injected") and :32 ("one error path for every verb"), then extends BaseApiService in a new service or omits errorInterceptor-covered error handling, trusting the base to funnel errors. Actual code: account.service.ts:13 does `inject(BaseApiService)`, and base-api.service.ts:17-18 documents "Authentication and error handling are NOT here... errorInterceptor owns the user-facing messages" — no catchError exists in the base. The doc directly contradicts the code on both claims.
- **Suggested fix:** Rewrite the structure overview, key decision and test-walkthrough line to say composition via inject(), with errors left to errorInterceptor by design.
- **History:**
  - v1: found by 1 lens (requirements); adversarial verdict `confirmed`, finder confidence 9/10
  - v1: fix round — backlog

### PPW-781 — DEPLOYMENT.md file and workflow tables never mention the new e2e stack

- **What:** The tables list ci.yml/deploy.yml/secret-scan.yml and docker-compose.yml/.prod.yml only. An operator reading DEPLOYMENT.md after merge sees neither playwright-e2e.yml nor docker-compose.e2e.yml, so a failing advisory e2e job or a stray fototipar-e2e stack has no documented origin. tech-stack.md was updated in the same branch; this was not.
- **Evidence:** `docs/DEPLOYMENT.md:79` — Files exist: .github/workflows/playwright-e2e.yml and docker-compose.e2e.yml (verified via ls). docs/DEPLOYMENT.md section 2 table (lines 79-91) enumerates repo files/workflows but has no row for either. Operator reads DEPLOYMENT.md, sees a failing/unexplained playwright-e2e.yml run or a stray fototipar-e2e compose stack, greps the doc, finds nothing documenting its purpose or origin.
- **Suggested fix:** Add rows for docker-compose.e2e.yml and playwright-e2e.yml (marked advisory, not a merge gate) to the tables at docs/DEPLOYMENT.md:73-81.
- **History:**
  - v1: found by 1 lens (requirements); adversarial verdict `confirmed`, finder confidence 8/10
  - v1: fix round — backlog
  - v4: re-found by 1 lens (completeness-critic) at `.env.example:86`; prior decision carried verbatim — "v1: fix round — backlog". Re-affirmed, still `backlog`: the rewritten line still tells developers to put the e2e admin credentials in a file Playwright never reads, while `README.md:108` says to export them in the shell.

### PPW-782 — E2E_* variables documented in .env.example, which no e2e code reads

- **What:** A developer sets E2E_ADMIN_EMAIL/E2E_ADMIN_PASSWORD in .env for a stack seeded with different credentials. Compose injects .env into the API container only; playwright.config.ts and e2e/support/stack.ts read host process.env and no dotenv loader exists. The values are ignored, defaults are used, and the admin specs fail at login with no hint why.
- **Evidence:** `.env.example:84` — Dev sets E2E_ADMIN_EMAIL/PASSWORD in .env expecting them to reach the seeded admin. stack.ts:6-7 reads process.env directly (no dotenv loader), so a shell-only .env file never populates them — defaults `mateibarba@yahoo.com`/`Admin1234!` are used regardless. Compose (docker-compose.e2e.yml:26-27) only forwards E2E_JWT_PRIVATE_KEY_PEM to the API container. Worse: ProductCatalogSeed.cs:32-33 hardcodes the same admin email/password as constants — --seed-dev never reads any admin-credential env var at all, so even setting E2E_ADMIN_EMAIL in the shell (bypassing .env) would desync credentials from the seeded account. The vars are fully dead.
- **Suggested fix:** State in the .env.example comment that these must be exported in the shell, or load .env from playwright.config.ts.
- **History:**
  - v1: found by 1 lens (requirements); adversarial verdict `confirmed`, finder confidence 8/10
  - v1: fix round — backlog

### PPW-783 — playwright-e2e workflow has push and pull_request triggers but no concurrency group, doubling every PR run

- **What:** A push to a PR branch matches both triggers, starting two identical ~4m40s jobs that each build the API image; three quick pushes leave six live jobs because nothing cancels in progress. Story 002 budgets "~3 min per run", so the actual cost is roughly triple the recorded figure on every push.
- **Evidence:** `.github/workflows/playwright-e2e.yml:7` — Branch "feature-x" has an open PR to main. Dev pushes a code commit (not matching any paths-ignore entry). GitHub fires both `push` (branch not "main", so not excluded by branches-ignore) and `pull_request` (synchronize) for the same commit. playwright-e2e.yml has no `concurrency:` key anywhere in the file (confirmed via repo-wide grep), so both events start independent `playwright` jobs, each building the API image (~4m40s). Three quick pushes leave 6 overlapping jobs since none cancels the others.
- **Suggested fix:** Add a concurrency group keyed on github.ref with cancel-in-progress: true; consider dropping the push trigger where pull_request already covers the branch.
- **History:**
  - v1: found by 1 lens (requirements); adversarial verdict `confirmed`, finder confidence 7/10
  - v1: fix round — backlog

### PPW-784 — Descoped e2e remainder lives only in bolt docs with no tracked follow-up

- **What:** delivery-step (574 LOC), saved-addresses (334), profile (217), eight un-migrated services, withCredentials/Idempotency-Key, the 4 kB error promotion and the Stripe e2e leg are named only in these two bolts' walkthroughs. Stories stay draft/unimplemented and this wave barred queue edits, so at merge the branch closes and none of it is carried anywhere.
- **Evidence:** `memory-bank/bolts/067-ui-scaling-and-e2e-ui/test-walkthrough.md:73` — Confirmed against real files. Stories 001-004 (memory-bank/intents/030.../stories/*.md) all sit status:draft, implemented:false; story 001's ACs for withCredentials/Idempotency-Key are unchecked. implementation-plan.md:189,192 states explicitly the wave won't edit reviews/state/backlog.md. Grepping memory-bank/.specsmd for withCredentials, Idempotency-Key, un-migrated-service count (verified 8 by counting *.service.ts not extending BaseApiService), 4kB-error, Stripe-leg, and per-page LOC finds hits only inside these two bolts' own docs — nowhere else. Post-merge these deviations have no PPW id, so a later backlog sweep (which only reads reviews/state/backlog.md) will never surface them.
- **Suggested fix:** Before merge, file the named remainders as stories or backlog rows so each descope has an owner outside the bolt walkthroughs.
- **History:**
  - v1: found by 1 lens (requirements); adversarial verdict `confirmed`, finder confidence 6/10
  - v1: fix round — backlog

### PPW-785 — Profile page stylesheet copied verbatim into all three extracted child components, dead rules included

- **What:** personal-info-form.scss and password-change-form.scss are byte-identical (94 lines, `diff` returns nothing) and account-deletion-card.scss repeats .card/.card__title again. Each copy ships rules its own template never uses (.card--danger, .field-error-list, .form-error/.form-row), so ~2 kB of duplicated/dead CSS enters the bundle the same branch just capped at 400 kB, and any palette tweak needs three edits.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/account/pages/profile/components/personal-info-form/personal-info-form.scss:1` — Verified as stated: personal-info-form.scss and password-change-form.scss are byte-identical (94 lines each, diff empty), and account-deletion-card.scss repeats the same .card/.card__title block. personal-info-form.html uses only .card, .card__title, .form-row, .form-group, .form-control, .field-error — never .card--danger, .card__title--danger, .field-error-list, or .form-error, all of which sit dead in its scss. This is a real duplication/dead-CSS finding, but there is no runtime input/state that produces a wrong result: unused selectors simply never match, and the duplicate rules render identically wherever they do apply. Nothing executes incorrectly, so no failing trace exists — it's a maintainability/bundle-size issue, not a behavioral bug.
- **Suggested fix:** Extract one `styles/_account-forms.scss` partial (the pattern `styles/_auth-forms.scss` already sets) and `@use 'styles/variables'` instead of re-hardcoding #dc2626/#d1d5db/#16a34a.
- **History:**
  - v1: found by 1 lens (quality); adversarial verdict `plausible`, finder confidence 9/10
  - v1: fix round — backlog

### PPW-786 — Blob-to-file save logic duplicated between admin.service and a page, and the two copies disagree

- **What:** downloadZip builds an anchor, clicks it and calls URL.revokeObjectURL synchronously; confirmation-page.ts:346 saveBlob does the same dance but defers the revoke with setTimeout, its comment stating "revoking in the same tick can beat the save". An admin clicking "download ZIP" on a browser matching that comment gets no file, while the invoice download on the same build works.
- **Evidence:** `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:82` — Admin clicks "Download ZIP" → admin.service.ts downloadZip (lines 82-90) creates blob URL, appends anchor, calls a.click(), then synchronously calls URL.revokeObjectURL in the same tick. confirmation-page.ts saveBlob (346-358) does the identical dance but defers the revoke with setTimeout(0), with a comment stating same-tick revoke "can beat the save" in some browsers (historically Firefox). On such a browser, ZIP download silently fails while invoice download (same app, same blob mechanism) succeeds — a real, verifiable code-level inconsistency, though the underlying browser race can't be reproduced in jsdom/Vitest.
- **Suggested fix:** Extract one `saveBlob(blob, filename)` helper in shared/utils (keeping the deferred revoke) and call it from both; a core service should not touch document.
- **History:**
  - v1: found by 1 lens (quality); adversarial verdict `confirmed`, finder confidence 7/10
  - v1: fix round — backlog

### PPW-787 — reuseExistingServer outside CI silently tests whatever server is already on the port, e.g. another worktree's

- **What:** reuseExistingServer: !isCi and a hardcoded port 4200. With two or three parallel worktrees (the normal workflow here), a stale ng serve left on 4200 from another branch is reused, so all three specs pass against code that is not under review — a green local run proving nothing.
- **Evidence:** `src/PhotoPrint.UI/playwright.config.ts:27` — Worktree A: `npm start -- --port 4200` left running from branch X. Worktree B (different branch, unrelated code) runs `npm run e2e` locally with CI unset, so isCi=false and reuseExistingServer=true. Playwright's webServer probes http://localhost:4200, gets a 200 from worktree A's dev server, and skips launching its own; all specs in worktree B execute against branch X's running app, unrelated to worktree B's changes.
- **Suggested fix:** Fail fast when port 4200 is already serving, or derive the port per worktree, so a reused server is an explicit opt-in.
- **History:**
  - v1: found by 1 lens (tests-coverage); adversarial verdict `confirmed`, finder confidence 7/10
  - v1: fix round — backlog

### PPW-788 — E2E workflow triggers leave the deploying commit as the one never smoke-tested

- **What:** push: branches-ignore: [main] means the merge commit on main never runs the e2e job, and deploy.yml chains off ci.yml alone. A defect introduced by the merge itself — a semantic conflict between two bolt branches that each passed — reaches deploy with no smoke coverage.
- **Evidence:** `.github/workflows/playwright-e2e.yml:1` — PR-A and PR-B each pass playwright-e2e.yml's pull_request run (against main as of sync time), then merge separately. Each merge is a push to main; playwright-e2e.yml's push trigger has branches-ignore:[main] (line 17-18) so it never reruns. ci.yml also excludes push-to-main (ci.yml:8-9), so nothing retests the combined commit. deploy.yml then builds/deploys HEAD via workflow_dispatch (deploy.yml:11) or workflow_run on ci alone (deploy.yml:7-10) — e2e is never in that chain. A combination defect (A+B interaction) ships untested.
- **Suggested fix:** Run the workflow on main too (or on the merge queue), and make deploy.yml depend on it once it has proven stable.
- **History:**
  - v1: found by 1 lens (tests-coverage); adversarial verdict `confirmed`, finder confidence 8/10
  - v1: fix round — backlog

### PPW-789 — No repaint/change-detection test for the two extracted profile forms

- **What:** address-form.ts wires form.events into a signal read by fi(), and saved-addresses-page.spec.ts covers a container-driven markAsTouched() repaint. personal-info-form and password-change-form lack both the plumbing and the test, so a future container that calls markAllAsTouched() before submit shows no validation errors under zoneless change detection.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/account/pages/profile/components/personal-info-form.ts:1` — In ProfilePage (rendered via profile-page.spec.ts's TestBed setup), patch profileForm.firstName to '' then call component.profileForm.markAllAsTouched() directly (simulating a future submit-guard), then fixture.detectChanges(). PersonalInfoForm is OnPush and reads ctrl.touched/.invalid as plain properties in isInvalid(), not a signal (unlike AddressForm's formEvents signal fed by form.events). Since the FormGroup reference passed via the `form` input is unchanged and no event originated inside PersonalInfoForm's own template, Angular skips re-checking its view, so isInvalid('firstName') is never re-evaluated and the '.field-error' span stays absent even though the control is now invalid+touched. Same gap applies to password-change-form.ts.
- **Suggested fix:** Mirror address-form's form.events signal in both profile forms and add the same container-marks-touched repaint spec for each.
- **History:**
  - v1: found by 1 lens (tests-coverage); adversarial verdict `confirmed`, finder confidence 6/10
  - v1: fix round — backlog

### PPW-790 — product.service getCatalog() caches the result but not the in-flight request, so concurrent callers duplicate the call

- **What:** Cold cache. The user lands on / and clicks through to /preturi before the first response returns. HomePage and PricingPage each call getCatalog(), so two GET /products go out and whichever answers last wins the cache. Same for any two catalog consumers mounted together.
- **Evidence:** `src/PhotoPrint.UI/src/app/core/services/product.service.ts:22` — product.service.ts:14-24: getCatalog() reads catalog$$.value synchronously; HttpClient.get (base-api.service.ts:29-31) is a cold Observable that fires a new HTTP request per subscribe, and the response arrives asynchronously (later macrotask). So: HomePage.ngOnInit calls getCatalog() -> cached is null -> subscribes to api.get, GET #1 fires but hasn't resolved. Before it resolves, PricingPage mounts and calls getCatalog() -> catalog$$.value is still null -> subscribes again, GET #2 fires. Two network calls go out; whichever tap() runs last overwrites catalog$$, and the other component's rendered data can be stale/discarded. No in-flight guard exists (no shareReplay, no pending-request field).
- **Suggested fix:** Cache the observable: keep this.api.get(...).pipe(tap(...), shareReplay({bufferSize:1, refCount:false})) in a field, return it while in flight, and clear that field in clearCache() and on error.
- **History:**
  - v1: found by 1 lens (frontend-ux); adversarial verdict `confirmed`, finder confidence 8/10
  - v1: fix round — backlog

### PPW-791 — delivery-step shipping-cost subscriptions are never torn down and write shared checkout state after destroy

- **What:** Easybox is selected and both /shipping/cost calls are slow. The user goes back to the cart, re-enters the delivery step and picks Courier. The destroyed instance's late Easybox response still runs applyCost, writing CheckoutStateService.setShippingCost with the Easybox price, so the summary shows the wrong shipping total.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/checkout/pages/delivery-step.ts:495` — Visit A: checkoutState.snapshot.method='Easybox' (persisted from an earlier selection) so instance A's deliveryMethod signal inits to 'Easybox' (line 362). ngOnInit→loadShippingCosts() fires two slow GETs (lines 495,499), no takeUntilDestroyed. User navigates to cart; router destroys A without cancelling the subscriptions. Instance B mounts, also inits deliveryMethod='Easybox', reloads costs fast, user clicks Courier→selectMethod sets checkoutState method/cost to Courier. Now A's stale Easybox response resolves; applyCost runs in A's closure, reads A's own deliveryMethod signal (still 'Easybox', never touched on A), guard passes, calls checkoutState.setShippingCost(easyboxCost) — overwriting the correct Courier cost while method stays 'Courier'.
- **Suggested fix:** Pipe both getShippingCost calls through takeUntilDestroyed(this.destroyRef), as every other subscription in this component already does.
- **History:**
  - v1: found by 1 lens (frontend-ux); adversarial verdict `confirmed`, finder confidence 6/10
  - v1: fix round — backlog

### PPW-792 — HomePage.ngOnInit catalog subscription has no takeUntilDestroyed

- **What:** The user opens / and immediately routes to /tipareste while GET /products is in flight. The subscription outlives the component and its next/error handler writes catalogSignal on a destroyed instance. Harmless today, but it is the only unguarded subscribe left in the pages this branch rewrote.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/home/home-page.ts:53` — getCatalog() (product.service.ts:14-24) returns either a synchronous complete()-ing Observable (cache hit) or one HttpClient GET (base-api.service.ts:29-31), both of which emit once and complete on their own — no polling, no repeat emissions. If the component is destroyed mid-request, the eventual next/error just calls catalogSignal.set() on a signal nobody reads anymore (view torn down, OnPush, no active consumers) — no exception, no CD error, no visible wrong output, just an inert write and a trivially bounded retention until the HTTP call resolves.
- **Suggested fix:** Inject DestroyRef and pipe getCatalog() through takeUntilDestroyed, matching profile-page.ts and saved-addresses-page.ts.
- **History:**
  - v1: found by 1 lens (frontend-ux); adversarial verdict `plausible`, finder confidence 8/10
  - v1: fix round — backlog

### PPW-793 — Pricing teaser advertises hardcoded prices when the catalog call fails

- **What:** GET /products fails, or returns no product with sizes; home-page's new error handler sets catalogSignal to null, cards() is empty and the teaser renders literal 1.20 / 0.99 / 0.89 lei/buc with no caveat. After an admin price change those figures advertise prices checkout will not honour.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/home/components/pricing-teaser/pricing-teaser.html:20` — HomePage.ngOnInit subscribes to productService.getCatalog(); on error it runs catalogSignal.set(null). pricingCards() computed reads catalogSignal()?.tiers ?? [] -> [] when null, so app-pricing-teaser gets cards=[] . pricing-teaser.html line 20's @if (cards().length === 0) branch renders the literal 1.20/0.99/0.89 lei/buc cards with no error/caveat text (only a "Prețuri pentru X" note appears if productName() is set, which it isn't when catalogSignal is null). Same empty-cards path triggers if getCatalog() succeeds but no product has sizes.length>0 (products.find returns undefined, early return leaves catalogSignal at its initial null).
- **Suggested fix:** Render a neutral "Prețuri indisponibile momentan" state instead of fixed numbers, or source the fallback from the same data the pricing page uses.
- **History:**
  - v1: found by 1 lens (frontend-ux); adversarial verdict `confirmed`, finder confidence 6/10
  - v1: fix round — backlog

### PPW-794 — Order ZIP download revokes the blob URL in the same tick as the click, risking a lost download

- **What:** An admin downloads a large order's photos in Firefox or Safari. URL.revokeObjectURL(a.href) runs synchronously after a.click(), before the browser has read the blob, so the save is cancelled and no file lands on disk. Chrome usually tolerates it, which is why it goes unnoticed.
- **Evidence:** `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:89` — Admin clicks download in Firefox/Safari (or any browser that defers the actual blob read off the click handler's tick). downloadZip() runs a.click() then immediately URL.revokeObjectURL(a.href) on the very next line, same synchronous tap callback — no setTimeout/delay anywhere. If the browser hasn't finished reading the blob before revoke fires, the object URL is invalidated mid-save and the download silently fails or produces a 0-byte/broken file, with no error surfaced to the app.
- **Suggested fix:** Hold the URL in a const and revoke it on a later task: setTimeout(() => URL.revokeObjectURL(url), 0).
- **History:**
  - v1: found by 1 lens (frontend-ux); adversarial verdict `confirmed`, finder confidence 5/10
  - v1: fix round — backlog

### PPW-795 — New guest e2e never asserts guestSession survival, the repo's most re-found defect class

- **What:** The spec drives a guest through upload, cart, delivery and review without ever reading localStorage. A regression that overwrites instead of merge-preserving `guestSession` (definition-of-done class 11) still passes this suite green, and there is no e2e for the guest-to-login merge at all.
- **Evidence:** `src/PhotoPrint.UI/e2e/guest-checkout.spec.ts:40` — guest-checkout.spec.ts (lines 1-75) never calls page.evaluate to read localStorage and never logs in or reloads. Suppose guest-auth.service.ts's persist method is changed to localStorage.setItem('guestSession', JSON.stringify(newPartial)) instead of merging with the existing object — within one page load the in-memory service state (cart totals, delivery address shown in DOM at lines 32-73) stays correct, so every assertion in this spec still passes green while guestSession in storage is silently corrupted.
- **Suggested fix:** Read localStorage.guestSession after upload and after each navigation and assert the token plus prior fields survive; add a login-after-guest merge spec.
- **History:**
  - v1: found by 1 lens (completeness-critic), topic hinted by the shared prompt; adversarial verdict `confirmed`, finder confidence 7/10
  - v1: fix round — backlog

### PPW-796 — README e2e run recipe is POSIX-only on a Windows-primary project

- **What:** The documented steps use `export VAR="$(cat …)"`, `E2E="-f …"` and `sh scripts/gen-dev-keys.sh`. In this project's primary shell (PowerShell 5.1) none of that parses, and there is no PowerShell block even though scripts/gen-dev-keys.ps1 exists. The suite is therefore never run locally, leaving CI as the first executor.
- **Evidence:** `README.md:95` — Windows dev opens PowerShell 5.1 (this project's primary shell), pastes README.md's e2e block verbatim: export E2E_JWT_PRIVATE_KEY_PEM="$(cat secrets/dev-jwt-private.pem)". PowerShell has no export cmdlet, so it errors "term 'export' is not recognized" and the recipe halts before docker compose even runs. Contrast: README lines 32 and 70 DO give a sibling "pwsh ..." line for gen-dev-keys.sh, but the e2e block's export/$(cat)/E2E= lines have no PowerShell equivalent, so the gap is real and inconsistent with the rest of the doc.
- **Suggested fix:** Add a PowerShell variant: gen-dev-keys.ps1, `$env:E2E_JWT_PRIVATE_KEY_PEM = Get-Content -Raw secrets/dev-jwt-private.pem`, and the compose invocation spelled out.
- **History:**
  - v1: found by 1 lens (completeness-critic); adversarial verdict `confirmed`, finder confidence 7/10
  - v1: fix round — backlog

### PPW-797 — Dead leftovers in profile-page after the component extraction (Router imported and injected but never used)

- **What:** ReactiveFormsModule is still imported in the TS file but no longer listed in the component's imports array, and the Router injected at line 81 has no remaining use now that the forms live in child components. With no ESLint in this repo, nothing flags either.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/account/pages/profile/profile-page.ts:10`
- **Suggested fix:** Delete the ReactiveFormsModule import, and the unused router injection together with its Router import.
- **History:**
  - v1: found by 2 lens (quality, frontend-ux); adversarial verdict `unverified-cleanup`, finder confidence 9/10
  - v1: fix round — backlog

### PPW-798 — Prettier-only reflows recorded in the walkthrough as component split wiring

- **What:** account-layout.ts, account-page.ts and account.routes.ts have no functional change — only whole-file Prettier reflow — yet are listed as "follow-on wiring for the two split pages". The same unbudgeted reflow (173 insertions/166 deletions) added 7 net lines to delivery-step.ts, so the extraction ends above its pre-split size and the ≤200-LOC criterion reads as a refactor failure.
- **Evidence:** `memory-bank/bolts/067-ui-scaling-and-e2e-ui/implementation-walkthrough.md:51`
- **Suggested fix:** Label those three files formatting-only, and keep whole-file formatting passes out of refactor commits so LOC evidence stays meaningful.
- **History:**
  - v1: found by 1 lens (requirements); adversarial verdict `unverified-cleanup`, finder confidence 8/10
  - v1: fix round — backlog

### PPW-799 — BaseApiService dropped story 001's named payload and shipped one new option with no caller

- **What:** Story 001 exists for withCredentials, catchError translation and an optional Idempotency-Key; none shipped. ApiOptions.headers did ship and no migrated service passes it — the same "mechanism with no caller" the plan used to justify dropping Idempotency-Key. test-walkthrough.md:153 defends it as "an explicit plan acceptance criterion" the plan does not state.
- **Evidence:** `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:10`
- **Suggested fix:** Drop the unused headers option or give it a caller, and correct the justification in test-walkthrough.md so the two rulings stop contradicting each other.
- **History:**
  - v1: found by 1 lens (requirements); adversarial verdict `unverified-cleanup`, finder confidence 7/10
  - v1: fix round — backlog

### PPW-800 — Fix-stage ruling dismissed the gitleaks sync note after checking the wrong hook

- **What:** The log concludes .githooks/pre-commit has no secret allowlist, so .gitleaks.toml's "keep in sync" note is stale. The actual secret guard is hooks/pre-commit, with a PATTERNS list and path allowlist the note refers to. Inert today (no placeholder matches its {16,} classes), but the next allowlist edit will be reasoned from a wrong record and skip the hook.
- **Evidence:** `memory-bank/intents/030-ui-scaling-and-e2e/units/001-ci-quality-gates/construction-log.md:59`
- **Suggested fix:** Correct the construction-log decision to name hooks/pre-commit as the secret guard, and keep the two allowlists in sync as the note asks.
- **History:**
  - v1: found by 1 lens (requirements); adversarial verdict `unverified-cleanup`, finder confidence 8/10
  - v1: fix round — backlog

### PPW-801 — Romanian mobile-phone regex copied into four files, with a fifth divergent rule

- **What:** /^07[0-9]{8}$/ is inlined in profile-page.ts:93, register-page.ts:45, guest-checkout-form.ts:39 and named PHONE_PATTERN in saved-addresses-page.ts:22, while delivery-step.ts:44 mirrors the server with a different rule. Relaxing the account rule to accept landlines means finding four copies; missing one leaves a screen silently stricter than the API.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/account/pages/profile/profile-page.ts:93`
- **Suggested fix:** Export one ROMANIAN_MOBILE_PATTERN (or a phone validator) from shared/validators and import it in all four; state which one mirrors the server.
- **History:**
  - v1: found by 1 lens (quality); adversarial verdict `unverified-cleanup`, finder confidence 9/10
  - v1: fix round — backlog

### PPW-802 — Tier range label formatted a third time in HomePage instead of reusing the shared util

- **What:** pricingCards() builds `${min}–${max} buc` / `${min}+ buc` inline; pricing-page.ts:263 has the same in tierLabel() and shared/utils/pricing.utils.ts:15 builds the same range without the unit. A copy change (e.g. "buc." or a different dash) shows different labels on /, /preturi and the format selector.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/home/home-page.ts:43`
- **Suggested fix:** Add `tierRangeLabel(tier)` to shared/utils/pricing.utils.ts and call it from home-page and pricing-page.
- **History:**
  - v1: found by 1 lens (quality); adversarial verdict `unverified-cleanup`, finder confidence 8/10
  - v1: fix round — backlog

### PPW-803 — Third copy of the invalid-field helper, this one with a hand-rolled change-detection counter

- **What:** personal-info-form and password-change-form each carry an identical isInvalid(field); address-form instead subscribes to form.events and bumps a formEvents signal read inside fi() purely to force re-render. Three components extracted by one bolt now solve the same problem two ways, and the counter fires a signal write per keystroke and per status change for no behavioural gain over its siblings.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/account/pages/saved-addresses/components/address-form/address-form.ts:32`
- **Suggested fix:** Use one shared helper (or a small directive) for all three; drop the formEvents subscription — Angular's own form event bindings already mark the child view dirty.
- **History:**
  - v1: found by 1 lens (quality); adversarial verdict `unverified-cleanup`, finder confidence 7/10
  - v1: fix round — backlog

### PPW-804 — Address form wrapper and action buttons duplicated for the add and edit cases

- **What:** Lines 68-86 and 90-103 repeat the same [formGroup] form, <app-address-form/> and two-button footer, differing only in the ngSubmit target and the primary label. This ~30-line copy is most of why the page is still 334 LOC after the split, and a change to the footer (e.g. a disabled-while-invalid rule) has to be made twice or silently applies to only one path.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/account/pages/saved-addresses/saved-addresses-page.ts:90`
- **Suggested fix:** Render one block with `(ngSubmit)="save()"` dispatching on editingId(), and a computed submit label; or move the wrapper + footer into AddressForm behind inputs.
- **History:**
  - v1: found by 1 lens (quality); adversarial verdict `unverified-cleanup`, finder confidence 8/10
  - v1: fix round — backlog

### PPW-805 — Identity map in admin.service over a response that already has the target shape

- **What:** getOrders declares the response as `{items, total}` and then pipes `map(r => ({items: r.items, total: r.total}))` to produce AdminOrdersPage, which is exactly `{items, total}`. The pipe copies the object for nothing and hides that the DTO already matches, so a future field added to AdminOrdersPage silently arrives undefined.
- **Evidence:** `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:58`
- **Suggested fix:** Call `this.api.get<AdminOrdersPage>(...)` and delete the pipe and the inline anonymous generic.
- **History:**
  - v1: found by 1 lens (quality); adversarial verdict `unverified-cleanup`, finder confidence 9/10
  - v1: fix round — backlog

### PPW-806 — Saved-addresses cap hardcoded in the toast text next to the constant that holds it

- **What:** MAX_ADDRESSES = 5 gates the "+ Adaugă adresă" button, but the 409 handler says 'Poți salva maximum 5 adrese.' with a literal 5, and the server has its own MaxAddresses = 5 plus its own message. Raising the cap server-side leaves the UI telling users the wrong number.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/account/pages/saved-addresses/saved-addresses-page.ts:236`
- **Suggested fix:** Interpolate MAX_ADDRESSES into the message, or show the server's ConflictException text.
- **History:**
  - v1: found by 1 lens (quality); adversarial verdict `unverified-cleanup`, finder confidence 8/10
  - v1: fix round — backlog

### PPW-807 — tech-stack.md's documented count of over-budget stylesheets does not match the recorded build

- **What:** The standard says "six built stylesheets exceed" the 4 kB warning; the build recorded for this diff emits five warnings (seven source .scss files exceed 4 kB pre-minification). Standards are descriptive by project rule, and this number is the acceptance criterion for the stated reduction target, so the next person measures against a wrong baseline.
- **Evidence:** `memory-bank/standards/tech-stack.md:22`
- **Suggested fix:** Re-read the production build log, correct the count, and name the files so the reduction target has a checkable baseline.
- **History:**
  - v1: found by 1 lens (completeness-critic); adversarial verdict `unverified-cleanup`, finder confidence 6/10
  - v1: fix round — backlog

### PPW-808 — Dead account-page component reformatted instead of deleted

- **What:** AccountPage is referenced nowhere — account.routes.ts routes only AccountLayout, ProfilePage and SavedAddressesPage. The branch's only change to it is Prettier formatting, so no lens flags it, and a "section under development" placeholder stays in the bundle graph of a bolt whose stated goal is shrinking the UI.
- **Evidence:** `src/PhotoPrint.UI/src/app/features/account/pages/account-page.ts:1`
- **Suggested fix:** Delete account-page.ts (and its spec, if any) after confirming no route or template references app-account-page.
- **History:**
  - v1: found by 1 lens (completeness-critic); adversarial verdict `unverified-cleanup`, finder confidence 8/10
  - v1: fix round — backlog

### PPW-809 — Large Prettier reformat lands with no format script or CI check to hold it

- **What:** Roughly two thirds of the 6,158-line frontend diff is reformatting, interleaved with behaviour changes in the same hunks (delivery-step.ts, profile-page.ts, saved-addresses-page.ts). package.json still has no format or format:check script and ci.yml no formatting step, so the repo now carries two styles and a real change can hide in the churn.
- **Evidence:** `src/PhotoPrint.UI/package.json:9`
- **Suggested fix:** Add `"format:check": "prettier --check ."` plus a ci.yml step, and re-read the reformatted pages with `git diff --ignore-all-space` before sign-off.
- **History:**
  - v1: found by 1 lens (completeness-critic); adversarial verdict `unverified-cleanup`, finder confidence 7/10
  - v1: fix round — backlog
### PPW-810 — DEPLOYMENT.md's permission-denied remedy boots the API as root instead of chowning the uploads volume

- **What:** An operator whose uploads break with `permission denied` after upgrading runs the documented remedy. The Dockerfile sets an `ENTRYPOINT` and no `CMD`, and `compose run` replaces only `CMD`, so the argv becomes `dotnet PhotoPrint.API.dll chown -R 1001:1001 /app/Storage`: a root-privileged API boots against the production database (migrations included) and the chown never happens. Uploads stay broken.
- **Evidence:** `docs/DEPLOYMENT.md:242` — `Dockerfile:55` is `ENTRYPOINT ["dotnet","PhotoPrint.API.dll"]` with no `CMD`; `Program.cs:14` passes args to configuration and its CLI verbs, and `chown` matches none, so the container either boots the full API as root against the prod `.env` database or dies on argument parsing. The same entrypoint bug sits on the `--seed` line at `docs/DEPLOYMENT.md:200`.
- **Suggested fix:** Add `--entrypoint sh` and quote the command (`… --entrypoint sh api -c 'chown -R 1001:1001 /app/Storage'`), and fix the `--seed` line the same way.
  - **Fix brief — files:** `docs/DEPLOYMENT.md:242` · `docs/DEPLOYMENT.md:200` · `Dockerfile:55` · `Dockerfile:48` · `docker-compose.prod.yml:31` · `src/PhotoPrint.API/Program.cs:14` · `src/PhotoPrint.API/Program.cs:343`
  - **Fix brief — failing path:** the documented `docker compose -f docker-compose.prod.yml run --rm --user root api chown -R 1001:1001 /app/Storage` appends its words to the image `ENTRYPOINT` instead of replacing it, so `dotnet PhotoPrint.API.dll` runs as root with `chown` as its arguments and the ownership is never changed.
  - **Fix brief — testShape:** `DeploymentDoc_ComposeRunShellCommands_UseEntrypointOverride`: arrange — parse the `ENTRYPOINT`/`CMD` lines of `Dockerfile` and every `compose … run` line in `docs/DEPLOYMENT.md`; act — extract each run command's first word; assert every command whose first word is not `dotnet` passes `--entrypoint`. Reddens on lines 200 and 242 today.
  - Not trigger-list-shaped (a documentation correction to two shell commands; changes no scheme, job, retry or state machine).
- **History:**
  - v4: found by 2 lenses (correctness, completeness-critic); adversarial verdict `confirmed`, finder confidence 9/10
  - v4: lineage — `residual-of: PPW-762`, introduced by fix round 1 (the uid-pin remedy was added with that fix); area `uploads`

### PPW-811 — Playwright failure traces carrying the admin login POST body and Bearer token are uploaded as a CI artifact

- **What:** With `trace: 'retain-on-failure'`, one flaky spec failure writes a trace whose network entry holds the `POST /api/auth/login` body verbatim plus the returned Bearer token, and the `if: failure()` step uploads `test-results/` for 7 days. GitHub's `::add-mask::` redacts logs, never artifact bytes, and public-repo artifacts are downloadable by anyone. Today the leaked pair is the committed dev credential; the moment the repo sets real `E2E_ADMIN_*` secrets this leaks a live one.
- **Evidence:** `src/PhotoPrint.UI/playwright.config.ts:19` — `e2e/support/stack.ts:22` fills `#password` from the environment and `e2e/realtime-order.spec.ts:17` calls `loginAsAdmin`; `.github/workflows/playwright-e2e.yml:105` sets the credentials and line 120 uploads `test-results/` plus `playwright-report` as artifact `playwright-report` with 7-day retention. `.env.example:86` now calls both variables required.
- **Suggested fix:** Set `trace: 'off'` under CI (screenshots and `api-e2e.log` already diagnose), or exclude `test-results/` and trace attachments from the upload, or log the admin in through a non-traced request context.
  - **Fix brief — files:** `src/PhotoPrint.UI/playwright.config.ts:19` · `src/PhotoPrint.UI/e2e/support/stack.ts:22` · `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:17` · `.github/workflows/playwright-e2e.yml:105` · `.github/workflows/playwright-e2e.yml:120` · `.env.example:86`
  - **Fix brief — failing path:** a spec fails after `loginAsAdmin`; Playwright's retained trace records the login request body and response token; the failure-only `upload-artifact` step ships `test-results/` — including that trace — as a downloadable artifact.
  - **Fix brief — testShape:** `E2eArtifacts_CarryNoCredentialTrace`: arrange — read `playwright.config.ts` and `.github/workflows/playwright-e2e.yml`; act — resolve the `trace` value and the failure-upload paths; assert either `trace` is `'off'` when `process.env.CI` is set, or no uploaded path contains `test-results`. Reddens on today's config.
  - Not trigger-list-shaped (two configuration values; changes no scheme, job, retry or state machine).
- **History:**
  - v4: found by 1 lens (security); adversarial verdict `confirmed`, finder confidence 8/10
  - v4: lineage — no earlier row; `seed_round: null` (the trace and upload settings predate every fix round); area `tests`

### PPW-812 — The no-seeded-credential guard scans only e2e/*.ts, so the still-committed seeder pair keeps it green

- **What:** The guard's own failure message says "anyone reading the repo", but its file enumeration is rooted at `src/PhotoPrint.UI/e2e`, four `.ts` files, none of which contains the pair. It is therefore green while `ProductCatalogSeed.cs:32-33` still holds `mateibarba@yahoo.com` / `Admin1234!` and `docs/DEPLOYMENT.md:200` runs `--seed` on production — the credential-removal objective reads satisfied while the production admin login sits in tracked source.
- **Evidence:** `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:20` — ran the class: 3 passed, 0 failed, with the literals still at `src/PhotoPrint.API/Data/Seed/ProductCatalogSeed.cs:32`, hashed into a real Admin user at line 99. `.github/workflows/playwright-e2e.yml:101` additionally re-derives that pair as a shell default, the very committed-credential fallback the `stack.ts` assertion forbids, and no assertion covers it.
- **Suggested fix:** Scan the whole tree for the pair rather than one folder, and make `ProductCatalogSeed` read the admin email and password from configuration and fail fast when unset — then test the fail-fast.
  - **Fix brief — files:** `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:15` · `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:20` · `src/PhotoPrint.API/Data/Seed/ProductCatalogSeed.cs:32` · `src/PhotoPrint.API/Data/Seed/ProductCatalogSeed.cs:99` · `.github/workflows/playwright-e2e.yml:101` · `docs/DEPLOYMENT.md:200`
  - **Fix brief — failing path:** the test enumerates only `src/PhotoPrint.UI/e2e/*.ts`, so the seeder's two `const string` literals and the workflow's `${SECRET_EMAIL:-$(…ProductCatalogSeed.cs…)}` default are both outside its reach and it passes.
  - **Fix brief — testShape:** `SeedSources_CarryNoAdminCredential`: arrange — read `ProductCatalogSeed.cs`; act — regex `AdminEmail|AdminPassword\s*=\s*"`; assert no match. Plus `Workflow_HasNoSeedFileFallback`: assert `.github/workflows/playwright-e2e.yml` contains no `:-$(` substitution for either credential. Both redden today and go green once the seeder reads configuration.
  - **Trigger-list-shaped:** yes (the real fix moves the seeded admin credential to configuration with a fail-closed check — a key-scheme change).
- **History:**
  - v4: found by 2 lenses (requirements, tests-coverage); adversarial verdict `confirmed`, finder confidence 8/10
  - v4: Approach pre-check: not re-run — `PPW-764`'s v1 pre-check already adjudicated the seeder-reads-configuration approach and revised it (credentials passed into `ApplyAsync`, inside the existing user-absent branch, no Production-only throw); this row inherits that verdict.
  - v4: lineage — `residual-of: PPW-764`, introduced by fix round 1 (this guard is that round's regression test); area `tests`

### PPW-813 — RUNTIME_UID is asserted by grepping the Dockerfile, never measured against the effective identity or a writable mount

- **What:** The guard compares the `ARG RUNTIME_UID` default to its own `VolumeOwnerUid` constant, so it can only ever confirm that two numbers in the repo match. Nothing measures the container's effective uid or that the storage root is writable by it: an existing named volume owned by the base image's uid (1654 on the current `aspnet:8.0-alpine`) makes every upload write fail with EACCES while `/health` stays 200 and the test stays green.
- **Evidence:** `Dockerfile:36` — `src/PhotoPrint.Tests/Unit/Configuration/ContainerRuntimeTests.cs` only greps the Dockerfile text. The pre-check found the deeper cause: the volume is mounted at `/app/Storage` (`docker-compose.yml:57`, `docker-compose.prod.yml:46`) but the effective `Storage:BasePath` in Production is `/var/app/uploads` (`src/PhotoPrint.API/appsettings.json:45`) and nothing sets `Storage__BasePath`, so uid 1001 cannot create it under root-owned `/var` — and `src/PhotoPrint.API/Services/LocalStorageService.cs:26-34` swallows exactly that as a boot warning, while `src/PhotoPrint.API/HealthChecks/DiskHealthCheck.cs:22-31` measures free space on `/` and reports healthy.
- **Suggested fix:** Make the mount and the configuration name one directory, then fail loudly: set `Storage__BasePath=/app/Storage` in the compose files and `.env.example`, replace the swallowed warning with a boot-fatal write-then-delete probe under `BasePath` when the provider is Local, and make `DiskHealthCheck` probe that resolved path.
  - **Fix brief — files:** `Dockerfile:36` · `src/PhotoPrint.Tests/Unit/Configuration/ContainerRuntimeTests.cs` · `docker-compose.yml:57` · `docker-compose.prod.yml:46` · `src/PhotoPrint.API/appsettings.json:45` · `src/PhotoPrint.API/Services/LocalStorageService.cs:26` · `src/PhotoPrint.API/HealthChecks/DiskHealthCheck.cs:22` · `.env.example`
  - **Fix brief — failing path:** an upgraded stack keeps a volume owned by the base image's uid and mounts it at `/app/Storage`, while the API writes to `/var/app/uploads`; the directory creation fails, `LocalStorageService` logs a warning and continues, the disk health check measures `/` and returns healthy, and the Dockerfile-grep test passes.
  - **Fix brief — testShape:** `LocalStorage_FailsBootWhenItsRootIsNotWritable`: arrange `Storage:BasePath` pointing at a path occupied by a regular file; act application start; assert it throws instead of logging a warning. Plus `ComposeMountsMatchStorageBasePath`: parse each `docker-compose*.yml` `api` volume target and assert it equals the effective `Storage:BasePath` — reddens today on `/app/Storage` versus `/var/app/uploads`. The effective-uid measurement needs a Docker job in CI and cannot run on this machine.
  - **Trigger-list-shaped:** yes (changes the container's storage-permission model and adds a fail-closed boot probe).
- **History:**
  - v4: found by 3 lenses (correctness, tests-coverage, completeness-critic); verdict `confirmed` on independent agreement, no skeptic, finder confidence 7/10
  - v4: Approach pre-check: revised — the suggested root entrypoint that chowns `/app/Storage` was refuted: it chowns a path the app never writes to, needs `su-exec` (absent from the base image) plus `USER root`, breaks under `user:` overrides, read-only root filesystems and `runAsNonRoot`, and rewrites every inode of a photo volume on each start; the revised approach above (align the mount with `Storage:BasePath`, keep the pinned uid plus the documented one-off chown, add a fail-fast write probe and a real health-check path) replaces it.
  - v4: lineage — `residual-of: PPW-762`, introduced by fix round 1 (that round added the uid pin and this guard); area `uploads`

### PPW-814 — The new handshake gate counts a transport request being issued, which may have 401'd, instead of an established hub connection

- **What:** The hub requires role Admin and the API never reads `access_token` from the query string, so the SignalR client's SSE attempt is a plain GET that 401s. The `page.on('request')` listener increments the counter for that failed request anyway, the poll releases before long polling is accepted, and the `Clients.All` broadcast reaches zero connections — the spec then fails on its own assertion after 20 seconds.
- **Evidence:** `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:48` — the gate is `expect.poll(() => hubSignals).toBeGreaterThan(0)`, fed by a request listener; the assertion it is meant to protect fails at line 82 after the 20s timeout.
- **Suggested fix:** Gate on a completed 200 response to the `id=` negotiate/transport request (`page.waitForResponse`, or count inside a `'response'` listener) or on a received WebSocket frame, never on a request being issued.
  - **Fix brief — files:** `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:45` · `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:48` · `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:82` · `src/PhotoPrint.Tests/Unit/Configuration/RealtimeE2eHandshakeTests.cs:27`
  - **Fix brief — failing path:** the transport GET is issued and 401s; the request listener has already incremented the counter, so the poll returns immediately, the test proceeds to broadcast while no client is subscribed, and the status-change assertion times out.
  - **Fix brief — testShape:** `RealtimeSpec_GatesOnAnAcceptedHandshake`: arrange — read `realtime-order.spec.ts`; act — locate the counter's increment site; assert it lives in a `'response'` (or `'websocket'`) listener that checks a successful status, and that no `'request'` listener feeds it. Reddens today.
  - Not trigger-list-shaped (changes which event a single test waits on; no scheme, job, retry semantics or state machine).
- **History:**
  - v4: found by 3 lenses (correctness, tests-coverage, completeness-critic); verdict `confirmed` on independent agreement, no skeptic, finder confidence 7/10
  - v4: lineage — `residual-of: PPW-763`, introduced by fix round 1 (that round replaced the original wait with this counter); area `tests`

### PPW-815 — The new Paid seed order is never backfilled onto an already-seeded database, and no test covers that leg

- **What:** `ApplyAsync` returns early when the order with the well-known `Order1Id` exists, so a long-lived dev volume — or any e2e volume reused without `down -v` — keeps the old six orders: `--seed-dev` prints "already applied — skipping" and `FT-2026-0007` never appears. The realtime spec then reports it as `lipsă` and fails. The one test seeds a fresh database, so the already-seeded leg, the only way this can fail, is untested.
- **Evidence:** `src/PhotoPrint.API/Data/Seed/DevDataSeed.cs:30` — the guard matches; order 7 is built at lines 308 and 334 and never inserted. `src/PhotoPrint.API/Program.cs:337` invokes it; `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:10` pins `FT-2026-0007`; `src/PhotoPrint.Tests/Unit/Data/RealtimeE2eSeedTests.cs:26` seeds a fresh InMemory database only.
- **Suggested fix:** Guard every batch per well-known id (the uploads `AddRange` included) and make the backfill reconciling rather than insert-only — insert a missing seed order, otherwise reset its `Status`/`PaidAt` to the seeded values — in one transaction, treating a unique violation as "already applied", and gate `--seed-dev` to non-Production environments.
  - **Fix brief — files:** `src/PhotoPrint.API/Data/Seed/DevDataSeed.cs:30` · `src/PhotoPrint.API/Data/Seed/DevDataSeed.cs:89` · `src/PhotoPrint.API/Data/Seed/DevDataSeed.cs:308` · `src/PhotoPrint.API/Data/Seed/DevDataSeed.cs:334` · `src/PhotoPrint.API/Program.cs:337` · `src/PhotoPrint.Tests/Unit/Data/RealtimeE2eSeedTests.cs:26` · `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:10`
  - **Fix brief — failing path:** on a database seeded before order 7 existed, the `Order1Id` guard short-circuits `ApplyAsync`, so `FT-2026-0007` is never inserted and the realtime spec's pinned order is missing.
  - **Fix brief — testShape:** `DevSeed_BackfillsAMissingPinnedOrder`: arrange — apply `DevDataSeed` once, then delete order 7 and its items; act — apply it again; assert seven orders, six uploads, eight order items, no duplicate ids, and order 7 present with `Status == Paid`. Plus a `PostgresTestDatabase`-backed run of the same sequence to prove `ix_orders_order_number` and the upload owner constraint survive a re-run. Reddens today: the second apply inserts nothing.
  - **Trigger-list-shaped:** yes (converts an all-or-nothing guard into a reconciling per-id backfill — it changes the seeder's idempotency semantics).
- **History:**
  - v4: found by 2 lenses (correctness, completeness-critic); adversarial verdict `confirmed`, finder confidence 8/10
  - v4: Approach pre-check: revised — a plain per-id insert-only backfill breaks and under-fixes: the uploads `AddRange` at `DevDataSeed.cs:89` is unguarded and re-adds the same primary keys on a second run, so `SaveChangesAsync` throws before any order lands; and the live failure on a persistent database is rows-exist-but-consumed (the spec permanently flips one Paid order to Printing), which an insert-only backfill never repairs. The pre-check also found `--seed-dev` has no environment gate and is reachable in production, so the backfill must be gated; the revised approach above carries all three.
  - v4: lineage — `residual-of: PPW-766`, introduced by fix round 1 (that round added the second Paid order); area `data`

### PPW-816 — BaseApiService's list of direct environment.apiUrl consumers omits both auth-gating interceptors

- **What:** The doc comment exists to enumerate everywhere an `apiUrl` shape change must be checked, and names only six services. Both interceptors also gate on `req.url.startsWith(environment.apiUrl)`. Change `apiUrl` to a relative `/api` or add a trailing slash, follow the list, and both interceptors silently stop attaching the JWT and guest token — every request 401s and users are logged out.
- **Evidence:** `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:18` — the list omits `src/PhotoPrint.UI/src/app/core/interceptors/jwt.interceptor.ts:15` and `guest.interceptor.ts:16`, both of which compare against `environment.apiUrl`.
- **Suggested fix:** Add both interceptors to the doc-comment list, flagged as the auth-gating callers, and to bolt 067's success criterion at `memory-bank/bolts/067-frontend-http-consolidation/bolt.md:78`.
  - **Fix brief — files:** `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:18` · `src/PhotoPrint.UI/src/app/core/interceptors/jwt.interceptor.ts:15` · `src/PhotoPrint.UI/src/app/core/interceptors/guest.interceptor.ts:16` · `memory-bank/bolts/067-frontend-http-consolidation/bolt.md:78`
  - **Fix brief — failing path:** a developer edits `environment.apiUrl`, checks the six callers the doc comment lists, and ships; the two unlisted interceptors no longer match the new prefix, so no request carries a token and the SPA logs the user out on the first 401.
  - **Fix brief — testShape:** `ApiUrlConsumers_AreAllListed`: arrange — grep `src/PhotoPrint.UI/src` for `environment.apiUrl` outside `base-api.service.ts`; act — read the doc-comment list; assert every matching file appears in it. Reddens today on both interceptors.
  - Not trigger-list-shaped (a documentation list and one bolt criterion; no code behaviour changes).
- **History:**
  - v4: found by 3 lenses (correctness, requirements, completeness-critic); verdict `confirmed` on independent agreement, no skeptic, finder confidence 8/10
  - v4: lineage — `residual-of: PPW-765`, introduced by fix round 1 (that round wrote this doc comment); area `auth`

### PPW-817 — The e2e workflow re-derives the committed admin credential as a fallback, contradicting the required-no-default contract

- **What:** With no `E2E_ADMIN_*` secrets set — the normal case — the workflow step seds `AdminEmail`/`AdminPassword` out of the seeder source, so every CI run authenticates with the committed dev pair. `.env.example` ("required, no default"), `stack.ts`'s throwing `requiredEnv` and the credential guard all state the opposite, and a rotated production secret is never exercised.
- **Evidence:** `.github/workflows/playwright-e2e.yml:101` — the step resolves `${SECRET_EMAIL:-$(sed … ProductCatalogSeed.cs)}`; the same run seeds its throwaway Postgres from that identical file, so the seed constants are the only credentials that exist in CI. Nothing guards the fallback: `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:29` asserts "no fallback" against `stack.ts` only, its literal scan covers `src/PhotoPrint.UI/e2e/*.ts`, and its workflow test only checks that both variables are exported before `npm run e2e`.
- **Suggested fix:** Either require the secrets — fail the step when either is unset — or drop the "required, no default" wording from `.env.example`, `stack.ts` and the guard, so one contract is stated once.
  - **Fix brief — files:** `.github/workflows/playwright-e2e.yml:94-106` · `src/PhotoPrint.UI/e2e/support/stack.ts:8-20` · `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:29-60` · `src/PhotoPrint.API/Data/Seed/ProductCatalogSeed.cs:32` · `docker-compose.e2e.yml:14` · `.env.example:86`
  - **Fix brief — failing path:** no failing execution — the fallback resolves the value that CI's own seeded database holds; the defect is a contract three files assert and a fourth silently breaks, so a rotated secret is never authenticated against.
  - **Fix brief — testShape:** `Workflow_ResolvesCredentialsFromSecretsOnly`: arrange — read `.github/workflows/playwright-e2e.yml`; act — extract the credential-resolution step; assert it contains no `:-$(` shell default and fails when either secret is empty. Reddens today.
  - Not trigger-list-shaped (a CI step and three prose contracts; if the fixer instead teaches the seeder to read configuration, that work is `PPW-812`'s scheme change and carries its pre-check).
- **History:**
  - v4: found by 2 lenses (security, requirements); adversarial verdict `plausible` — the trace found no failing execution, since the fallback resolves the same value CI seeds, and recorded the missing guard instead; finder confidence 9/10
  - v4: lineage — `residual-of: PPW-764`, introduced by fix round 1 (that round moved the credentials into the workflow); area `tests`

### PPW-818 — Intent 030 was flipped to complete while its own requirements still assert the three targets this review corrected

- **What:** A reader opening the now-`complete` intent finds the Must goal "three real-money paths are automated", the NFR "HTTP plumbing duplication: 0 (all via BaseApiService)" and "No page > ~200 LOC". All three are false, and nothing points to intent 032 / bolt 071 where the payment leg actually lives.
- **Evidence:** `memory-bank/intents/030-ui-scaling-and-e2e/requirements.md:68` — `src/PhotoPrint.UI/src/app/features/checkout/pages/delivery-step.ts` is 574 lines and twelve more pages exceed 200; `payment.service.ts`, `auth`, `cart`, `guest-auth` and `upload` still inject `HttpClient` directly; no e2e spec reaches Stripe — `e2e/guest-checkout.spec.ts` ends at `/checkout/recapitulare` — and the intent's Scope Changes table is empty with no pointer to `memory-bank/intents/032-regression-and-e2e-stabilization/requirements.md:19`.
- **Suggested fix:** Correct the FR-1 and NFR wording the way README and tech-stack were corrected, bump `updated`, and cross-reference intent 032 story 001 for the carried-forward payment leg.
  - **Fix brief — files:** `memory-bank/intents/030-ui-scaling-and-e2e/requirements.md:21` · `memory-bank/intents/030-ui-scaling-and-e2e/requirements.md:68` · `memory-bank/intents/030-ui-scaling-and-e2e/requirements.md:69` · `src/PhotoPrint.UI/src/app/features/checkout/pages/delivery-step.ts` · `src/PhotoPrint.UI/src/app/core/services/payment.service.ts` · `memory-bank/intents/032-regression-and-e2e-stabilization/requirements.md:19`
  - **Fix brief — failing path:** the intent's status was flipped to `complete` without reconciling its own goal and NFR text, so the record now asserts three measurable things the code contradicts.
  - **Fix brief — testShape:** `CompleteIntentTargetsHold`: arrange — parse the NFRs of every `complete` intent; act — scan `src/PhotoPrint.UI` pages for line counts and services for `HttpClient` injections; assert no page exceeds the stated limit and the stated duplication count matches. Reddens today (574 lines; five direct injections).
  - Not trigger-list-shaped (record corrections; no code behaviour changes).
- **History:**
  - v4: found by 1 lens (requirements); adversarial verdict `confirmed`, finder confidence 8/10
  - v4: lineage — no earlier row; `seed_round: null` (caused by the bolt-completion commit `dc00a63`, not by a fix round); area `records`

### PPW-819 — The compose env-precedence guard reads only docker-compose.yml, never the prod overlay where precedence bites

- **What:** Add `Stripe__SecretKey: sk_live_placeholder` to the prod overlay's `environment:` block — the exact mistake this guard exists to catch, in the file where it means every production payment intent 401s — and it stays green, because that file is never read. Separately, quoting the dev value (valid YAML) false-fails the check.
- **Evidence:** `src/PhotoPrint.Tests/Unit/Configuration/ComposeEnvPrecedenceTests.cs:13` — the test opens the literal path `docker-compose.yml`. `docker-compose.prod.yml:34` declares `env_file: .env` and line 36 an `environment:` block, so precedence bites there; adding a bare Stripe literal under it leaves the guard green (verified by applying it), and its `(\S+)\s*$` capture includes a leading quote, so `StartWith` false-fails on a quoted value.
- **Suggested fix:** Iterate every `docker-compose*.yml` — allow-listing the e2e overlay's deliberate literals — and strip surrounding quotes before the `StartWith` check.
  - **Fix brief — files:** `src/PhotoPrint.Tests/Unit/Configuration/ComposeEnvPrecedenceTests.cs:13` · `src/PhotoPrint.Tests/Unit/Configuration/ComposeEnvPrecedenceTests.cs:23` · `src/PhotoPrint.Tests/Helpers/RepoFiles.cs:11` · `docker-compose.prod.yml:34` · `docker-compose.yml:52` · `docker-compose.e2e.yml:29`
  - **Fix brief — failing path:** the guard reads one file, so a hardcoded Stripe key added to `docker-compose.prod.yml`'s `environment:` block overrides the operator's real key from `.env` with the test still passing.
  - **Fix brief — testShape:** `ProdCompose_LetsAStripeKeyFromEnvWin`: arrange — enumerate every `docker-compose*.yml` declaring both `env_file` and `environment`; act — read each `Stripe__*` value with surrounding quotes stripped; assert each begins with `${` and that the count of checked keys equals the number of `Stripe__` keys found. Reddens on a bare literal in the prod overlay.
  - Not trigger-list-shaped (widens one test's file set and its parsing; no production code changes).
- **History:**
  - v4: found by 2 lenses (requirements, tests-coverage); adversarial verdict `confirmed`, finder confidence 7/10
  - v4: lineage — `residual-of: PPW-774`, introduced by fix round 1 (this guard is that round's regression test); area `tests`

### PPW-820 — The credential guard hard-requires the admin email and password to stay string literals in the seeder

- **What:** The guard's helper asserts that a `AdminEmail = "…"` match succeeds in the seeder source. A fixer who moves the credentials to configuration — the only real fix for the exposure — makes the guard fail with "the seeder declares AdminEmail". The test punishes the fix and pins the secret in source.
- **Evidence:** `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:75` — the helper regexes `AdminEmail\s*=\s*"…"` out of `src/PhotoPrint.API/Data/Seed/ProductCatalogSeed.cs:32`, where both are `const string` literals. Rewriting them as `_config["Seed:AdminEmail"]` makes both regexes match zero times, so `match.Success` is false and the test fails — verified.
- **Suggested fix:** Have the guard read the expected credential from configuration or the environment, and skip rather than fail when the seeder declares no literal.
  - **Fix brief — files:** `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:12` · `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:73` · `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:75` · `src/PhotoPrint.API/Data/Seed/ProductCatalogSeed.cs:32` · `src/PhotoPrint.API/Data/Seed/ProductCatalogSeed.cs:99` · `src/PhotoPrint.Tests/Helpers/RepoFiles.cs:10`
  - **Fix brief — failing path:** the seeder's literals are replaced by configuration reads; the helper's regex finds nothing; `match.Success` is asserted true; the spec-scanning test fails on the corrected code.
  - **Fix brief — testShape:** `SeedConstant_ToleratesConfiguredCredentials`: arrange — a seeder source that reads `Seed:AdminEmail`/`Seed:AdminPassword` from configuration with no literals; act — run the credential-scan test; assert it passes (skipped for want of a literal, not failed). Fails today with "the seeder declares AdminEmail".
  - Not trigger-list-shaped (relaxes one test helper's assertion; no production code changes).
- **History:**
  - v4: found by 1 lens (tests-coverage); adversarial verdict `confirmed`, finder confidence 8/10
  - v4: lineage — `residual-of: PPW-764`, introduced by fix round 1 (this guard is that round's regression test); area `tests`

### PPW-821 — The handshake guard pins the poll's shape but neither the signal counter's initial value nor listener ordering

- **What:** Change the counter's initializer to `1`, or move the two `page.on` registrations below the first `page.goto`, and the poll passes instantly: the handshake gate becomes a no-op while all 204 configuration tests stay green. Playwright never runs on this machine, so nothing else notices.
- **Evidence:** `src/PhotoPrint.Tests/Unit/Configuration/RealtimeE2eHandshakeTests.cs:27` — both mutations were applied and the tests ran green each time (2 passed, 0 failed), because the regexes pin only the poll condition, the counter's name and `toBeGreaterThan(0)`; nothing reads the initializer at `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:45` or the listener positions at lines 54 and 61. Tree restored afterwards.
- **Suggested fix:** Assert that the counter is declared `= 0` and that both `page.on('request')`/`page.on('websocket')` registrations appear before the first `page.goto` in the spec.
  - **Fix brief — files:** `src/PhotoPrint.Tests/Unit/Configuration/RealtimeE2eHandshakeTests.cs:26` · `src/PhotoPrint.Tests/Unit/Configuration/RealtimeE2eHandshakeTests.cs:34` · `src/PhotoPrint.Tests/Unit/Configuration/RealtimeE2eHandshakeTests.cs:80` · `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:45` · `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:54` · `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:61`
  - **Fix brief — failing path:** the counter is initialized non-zero (or the listeners are registered after navigation); `expect.poll` returns a truthy count immediately; the gate no longer waits for a handshake and the guard tests still pass.
  - **Fix brief — testShape:** `RealtimeSpec_StartsTheSignalCounterAtZero`: arrange — read the spec; act — regex `let\s+<counter>\s*=\s*(\d+)` using the counter name the existing test already extracts; assert the initializer is `0`. Plus assert both `page.on` character offsets precede the first `page.goto` offset. Both mutations above redden it.
  - Not trigger-list-shaped (two added assertions in an existing guard; no production code changes).
- **History:**
  - v4: found by 1 lens (tests-coverage); adversarial verdict `confirmed`, finder confidence 7/10
  - v4: lineage — `residual-of: PPW-763`, introduced by fix round 2 (`52541d1`, which added this guard with the reworked handshake); area `tests`

### PPW-822 — The new Paid seed order is verified only on EF InMemory, so no PostgreSQL constraint is exercised

- **What:** The test runs the dev seeder against `UseInMemoryDatabase` without the catalog seed, so `OrderItem.ProductId` has no `Products` row, the order-number unique index is not enforced and `decimal(10,2)` is ignored. A duplicate order number or a dangling foreign key in order 7 would keep it green; the only real check is the e2e job's `--seed-dev` step, which cannot run on this machine.
- **Evidence:** `src/PhotoPrint.Tests/Unit/Data/RealtimeE2eSeedTests.cs:41` — the seeder never calls the catalog seed, it only borrows its product id (`src/PhotoPrint.API/Data/Seed/DevDataSeed.cs:122`), and the test seeds no products. Swapping the two seed calls at `src/PhotoPrint.API/Program.cs:335`, or setting a duplicate order number, leaves the test green; on PostgreSQL `SaveChangesAsync` raises 23503/23505 (`src/PhotoPrint.API/Data/PhotoPrintDbContext.cs:278`, `:385`), the whole seed rolls back to zero orders and the realtime e2e fails.
- **Suggested fix:** Add a `PostgresTestDatabase`-backed test that runs the catalog seed then the dev seed and asserts the save succeeds and both pinned orders come back `Paid`.
  - **Fix brief — files:** `src/PhotoPrint.Tests/Unit/Data/RealtimeE2eSeedTests.cs:41` · `src/PhotoPrint.API/Data/Seed/DevDataSeed.cs:122` · `src/PhotoPrint.API/Data/Seed/DevDataSeed.cs:328` · `src/PhotoPrint.API/Data/PhotoPrintDbContext.cs:278` · `src/PhotoPrint.API/Data/PhotoPrintDbContext.cs:385` · `src/PhotoPrint.API/Program.cs:335`
  - **Fix brief — failing path:** the provider divergence hides the defect: the InMemory provider ignores foreign keys and unique indexes, so a seed that PostgreSQL rejects outright is recorded as verified.
  - **Fix brief — testShape:** `DevSeed_AppliesOnPostgres` (relational): arrange — `PostgresTestDatabase` with the migration chain applied; act — catalog seed, then dev seed; assert the save succeeds and the pinned `FT-2026-0006`/`FT-2026-0007` come back `Paid`. Reddens with 23505/23503 on a duplicate order number, a dangling product id, or a reversed seed order.
  - Not trigger-list-shaped (adds a relational test; no production code changes).
- **History:**
  - v4: found by 1 lens (tests-coverage), topic `hinted` by the shared prompt so no convergence credit; adversarial verdict `confirmed`, finder confidence 7/10
  - v4: lineage — `residual-of: PPW-766`, introduced by fix round 1 (that round added the second Paid order and this test); area `data`

### PPW-823 — The documented E2E_ADMIN_* secrets override makes the admin specs fail, because the seeder reads no configuration

- **What:** The workflow step's comment invites overriding the credentials via repository secrets. Set either one and the resolved login no longer matches the seeded account — the seeder's credentials are compile-time constants with no configuration path — so the admin specs fail against a user that does not exist, and the failure reads as a broken login page rather than a configuration mistake.
- **Evidence:** `.github/workflows/playwright-e2e.yml:94` — line 101 resolves the email from the secret and exports it; the seeder still creates `mateibarba@yahoo.com` (`src/PhotoPrint.API/Data/Seed/ProductCatalogSeed.cs:32`, private consts, no configuration read), so `src/PhotoPrint.UI/e2e/admin-login.spec.ts:11` submits the overridden address, the login 401s, and both the URL assertion and `loginAsAdmin`'s 15s token wait (`e2e/support/stack.ts:22`) time out. `README.md:106` documents the same override.
- **Suggested fix:** Either teach the seeder to read the same variable names, or delete the secrets fallback together with the "unless the repo overrides them" comment and the README line.
  - **Fix brief — files:** `.github/workflows/playwright-e2e.yml:94` · `.github/workflows/playwright-e2e.yml:101` · `src/PhotoPrint.API/Data/Seed/ProductCatalogSeed.cs:32` · `src/PhotoPrint.UI/e2e/support/stack.ts:22` · `src/PhotoPrint.UI/e2e/admin-login.spec.ts:11` · `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:44` · `README.md:106`
  - **Fix brief — failing path:** a repository sets `secrets.E2E_ADMIN_EMAIL`; the suite logs in with that address while the seeded admin keeps the compiled-in one; every admin spec fails at login.
  - **Fix brief — testShape:** `E2eWorkflow_OffersNoAdminOverrideTheSeederCannotHonor`: arrange — read the workflow and the seeder; act — detect any mention of `secrets.E2E_ADMIN_`; assert it appears only if the seeder reads those same names from configuration. Reddens today.
  - Not trigger-list-shaped (deleting an unsupported override, or else the seeder-reads-configuration work already tracked by `PPW-812` with its pre-check).
- **History:**
  - v4: found by 1 lens (completeness-critic); adversarial verdict `confirmed`, finder confidence 8/10
  - v4: lineage — `residual-of: PPW-764`, introduced by fix round 1 (that round added the secrets-override path); area `tests`

### PPW-824 — The no-fallback credential guard is a narrow-window regex that a two-line reshape defeats

- **What:** Splitting the read over two lines — a `const` holding `process.env['E2E_ADMIN_EMAIL']`, then an exported arrow function returning `E ?? 'mateibarba@yahoo.com'` — restores the committed default with the `NotMatchRegex` assertion still green. Nothing anywhere executes `requiredEnv`, so its throw, the actual protection, is unverified: Playwright cannot run on this machine.
- **Evidence:** `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:37` — the assertion matches a single-line shape only, and no Vitest spec imports or calls `requiredEnv` from `src/PhotoPrint.UI/e2e/support/stack.ts`.
- **Suggested fix:** Move `requiredEnv` into a Playwright-free module and add a Vitest spec that deletes the variable and expects the throw; keep the regex only as a secondary check.
- **History:**
  - v4: found by 3 lenses (correctness, tests-coverage, completeness-critic); verdict `unverified-low` — a delta pass skips skeptics for minors, so the lens verdict stands unchallenged rather than refuted; finder confidence 6/10
  - v4: lineage — `residual-of: PPW-764`, introduced by fix round 1 (this guard is that round's regression test); area `tests`

### PPW-825 — The compose precedence regex silently skips any Stripe line it cannot parse

- **What:** Reverting to `Stripe__SecretKey: sk_test_placeholder  # dev only`, or quoting the value, matches neither capture, so that line never enters the collection under test; the assertion is still satisfied by the webhook-secret line and the regression passes. A real key from `.env` is overridden again and every payment intent gets a Stripe 401.
- **Evidence:** `src/PhotoPrint.Tests/Unit/Configuration/ComposeEnvPrecedenceTests.cs:14` — the `(\S+)\s*$` capture fails on a trailing comment and includes the quote character on a quoted value, and nothing asserts that the number of parsed keys equals the number present.
- **Suggested fix:** Match `^\s+(Stripe__\w+):\s*(.+)$`, trim quotes and trailing comments, and assert the parsed count equals the number of `Stripe__` keys found.
- **History:**
  - v4: found by 1 lens (correctness); verdict `unverified-low` — a delta pass skips skeptics for minors, so the lens verdict stands unchallenged rather than refuted; finder confidence 8/10
  - v4: lineage — `residual-of: PPW-774`, introduced by fix round 1 (this guard is that round's regression test); area `tests`

### PPW-826 — CI resolves the e2e admin email and password independently, so one rotated secret mixes sources

- **What:** A repository that sets only `secrets.E2E_ADMIN_EMAIL` — rotating to a dedicated e2e account — still resolves the password from the seeder source, so the suite logs in with a real address and the seed's password. The admin spec fails on bad credentials with no hint that the two values came from different places.
- **Evidence:** `.github/workflows/playwright-e2e.yml:101` — the email and the password each have their own independent `${SECRET_…:-$(sed … ProductCatalogSeed.cs)}` fallback, with no check that both or neither came from secrets.
- **Suggested fix:** Resolve both from secrets or both from the seed: fail the step when exactly one of the two secrets is set.
- **History:**
  - v4: found by 1 lens (correctness); verdict `unverified-low` — a delta pass skips skeptics for minors, so the lens verdict stands unchallenged rather than refuted; finder confidence 7/10
  - v4: lineage — `residual-of: PPW-764`, introduced by fix round 1 (that round moved the credentials into the workflow); area `tests`

### PPW-827 — A repo-file-derived value is written to $GITHUB_ENV with a bare echo (env-injection shape)

- **What:** `sed -n 's///p'` prints one line per match, so a pull request adding a second `AdminPassword = "…";` line makes the captured value multi-line; a bare `echo "E2E_ADMIN_PASSWORD=$pass" >> $GITHUB_ENV` then sets an attacker-chosen variable (`NODE_OPTIONS=--require /tmp/x.js`, say) for every later step, the suite run included. Escalation is limited today only because the job already runs the pull request's own code.
- **Evidence:** `.github/workflows/playwright-e2e.yml:106` — the same workflow refuses `$GITHUB_ENV` for the JWT private key at lines 43-45 on exactly this reasoning.
- **Suggested fix:** Take a single line (`sed -n '…p;q'`) and pass the values as step-level `env:` on the suite step instead of `$GITHUB_ENV`; if `$GITHUB_ENV` is needed, use the delimited heredoc form.
- **History:**
  - v4: found by 1 lens (security); verdict `unverified-low` — a delta pass skips skeptics for minors, so the lens verdict stands unchallenged rather than refuted; finder confidence 7/10
  - v4: lineage — `residual-of: PPW-764`, introduced by fix round 1 (that round added this resolution step); area `tests`

### PPW-828 — Bolt 066's e2e-stability criteria stay ticked on CI evidence that predates the spec rewrites they measure

- **What:** The criterion "3 e2e pass in CI within ~3 min (16.7 s, three consecutive green runs)" is left untouched, but this review's rounds rewrote the realtime spec's handshake wait twice, pinned it to seeded orders and changed how admin credentials reach the suite. Playwright cannot run here, so the only proof is a future CI run — and story 002's no-flakes criterion and its "official Playwright action" criterion (which the workflow does not use) stay unchecked and unannotated.
- **Evidence:** `memory-bank/bolts/066-ci-quality-gates/bolt.md:74` — the cited timing predates commits `52541d1` and the round 1 spec changes; the workflow at `.github/workflows/playwright-e2e.yml` installs Playwright by hand rather than through the official action.
- **Suggested fix:** Re-stamp the criterion with the post-rewrite CI run id, or unmark it until CI on this pull request is green, and annotate story 002's action criterion as deliberately substituted.
- **History:**
  - v4: found by 2 lenses (requirements, completeness-critic); verdict `unverified-low` — a delta pass skips skeptics for minors, so the lens verdict stands unchallenged rather than refuted; finder confidence 7/10
  - v4: lineage — NEW, possible remainder of `PPW-779` (same criterion, different claim: that row disputes the "three green runs" count, this one the evidence's date); introduced by fix round 1; area `records`

### PPW-829 — README's two-Paid-orders promise holds only on a database that was never seeded before

- **What:** A developer with an existing e2e volume who pulls this branch and re-runs the dev seed hits the all-or-nothing guard: it prints "already applied — skipping" and never inserts the second Paid order, so the realtime spec has at most one — not the two the README promises — until a `down -v`.
- **Evidence:** `README.md:113` — the guard at `src/PhotoPrint.API/Data/Seed/DevDataSeed.cs:30` returns early whenever the first well-known order exists.
- **Suggested fix:** Say the two Paid orders exist only after seeding a clean database, or fix the guard to backfill missing pinned orders (`PPW-815`).
- **History:**
  - v4: found by 1 lens (requirements); verdict `unverified-low` — a delta pass skips skeptics for minors, so the lens verdict stands unchallenged rather than refuted; finder confidence 7/10
  - v4: lineage — `residual-of: PPW-773`, introduced by fix round 1 (that round rewrote this README claim); area `records`

### PPW-830 — The new apiUrl pins sit in consumer specs, not in the spec that owns url(), so the coupling stays untested

- **What:** The base service's own spec still derives its expected root from `environment.apiUrl`, so every URL assertion in it is tautological. `url()` does not normalize a trailing slash and the production environment file is never asserted: an `apiUrl` edited to end in `/api/` makes every request path contain `//` while the whole Vitest suite passes. One of the three consumer specs also claims a coupling that does not hold.
- **Evidence:** `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.spec.ts:10` — the expected root is read from the same constant under test; no case covers a trailing-slash root and no assertion reads `src/PhotoPrint.UI/src/environments/environment.prod.ts`.
- **Suggested fix:** Pin the dev root literally in the base service's own spec, add a `url()` case for a trailing-slash root, and assert the production `apiUrl` shape.
- **History:**
  - v4: found by 3 lenses (requirements, tests-coverage, completeness-critic); verdict `unverified-low` — a delta pass skips skeptics for minors, so the lens verdict stands unchallenged rather than refuted; finder confidence 6/10
  - v4: lineage — `residual-of: PPW-770`, introduced by fix round 1 (that round added these pins to the consumer specs); area `tests`

### PPW-831 — The workflow-order guard compares text offsets in the YAML instead of actual execution order

- **What:** Adding `if: github.event_name == 'push'` to the credential-resolution step leaves the export lines at their earlier file offsets, so both range assertions pass while every pull-request run starts with the variables unset and all three admin specs throw. An export placed in a different job would satisfy it too.
- **Evidence:** `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:54` — the assertions compare character positions within the YAML text, not step indices within a job, and read no `if:` guard.
- **Suggested fix:** Parse the YAML and assert the export step lives in the same job as the suite step, at an earlier step index, with no `if:` guard.
- **History:**
  - v4: found by 1 lens (tests-coverage); verdict `unverified-low` — a delta pass skips skeptics for minors, so the lens verdict stands unchallenged rather than refuted; finder confidence 7/10
  - v4: lineage — `residual-of: PPW-764`, introduced by fix round 1 (this guard is that round's regression test); area `tests`

### PPW-832 — Story 001 is complete with all three acceptance boxes unchecked and no partial-delivery note

- **What:** The frontmatter reads `status: complete` / `implemented: true` while all three acceptance boxes in the story's only criterion sit unchecked. Two of the three capabilities were deliberately delivered elsewhere — the interceptors own credentials and error translation, and the payment service keeps its own idempotency header — but unlike story 002 no partial-delivery note records the substitution, so the decision is invisible to the next reader.
- **Evidence:** `memory-bank/intents/030-ui-scaling-and-e2e/units/002-ui-scaling-and-e2e-ui/stories/001-base-api-service.md:22` — three unchecked boxes under a `complete` status. The trace refuted the stronger reading of this finding: the base service is built and used, its own doc comment (`src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:13-23`) states the interceptor split, its optional `headers` argument (line 75) threads an idempotency key, and `withCredentials` is moot because authentication is a header JWT — hence 🟡, not 🟠.
- **Suggested fix:** Add a partial-delivery note in the shape story 002 uses, stating that the interceptors own authentication and error translation and that the payment service keeps idempotency, or reword the criterion to match what shipped.
- **History:**
  - v4: found by 1 lens (requirements); adversarial verdict `plausible` — the trace found no failing execution and refuted the premise that the capabilities are missing, leaving only the unrecorded substitution; finder confidence 8/10
  - v4: lineage — no earlier row; `seed_round: null` (the story was flipped to complete by the bolt-completion commit `dc00a63`, not by a fix round); area `records`
