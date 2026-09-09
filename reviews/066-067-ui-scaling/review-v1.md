---
type: review
target: 066-067-ui-scaling
version: 1
supersedes: null
commit: 20c5c96
branch: feat/bolts-066-067-ui-scaling
pass-type: discovery
date: 2026-09-08
lenses: [correctness, security, requirements, quality, frontend-ux, tests-coverage, completeness-critic]
lenses-not-run: [db-parity, input-validation, observability, race]
verdict: request-changes
blockers: [PPW-762, PPW-763, PPW-764]
findings: { high: 3, medium: 10, low: 22, cleanup: 13, refuted: 1 }
tests: { dotnet: "n/a — no backend change in the diff", frontend: "126/126" }
---

# Review v1 — 066-067-ui-scaling

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-762 | 🔴 | Dockerfile non-root guard drifts the runtime uid off 1001, making the existing uploads/apidata volume unwritable | `Dockerfile:36` | yes |
| PPW-763 | 🔴 | realtime-order.spec.ts waits on a SignalR request pattern the hub connection never produces, so the only real-time spec always times out | `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:55` | yes |
| PPW-764 | 🔴 | E2E stack seeds an admin account whose password is a committed repo constant, usable on a first production deploy | `src/PhotoPrint.UI/e2e/support/stack.ts:7` | yes — e2e half only |
| PPW-765 | 🟠 | Only about half the services route through BaseApiService (auth/guest/money/upload bypass it) yet the criterion is ticked | `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:14` | yes |
| PPW-766 | 🟠 | retries: 1 in CI re-runs the non-idempotent realtime spec, which consumed the seed's only Paid order and can never pass on retry | `src/PhotoPrint.UI/playwright.config.ts:10` | yes |
| PPW-767 | 🟠 | Guest checkout e2e stops at the review step — the Stripe-to-confirmation leg is descoped while the story criterion is ticked | `src/PhotoPrint.UI/e2e/guest-checkout.spec.ts:64` | yes — record the gap |
| PPW-768 | 🟠 | Locker-selector output bindings in delivery-step left uncovered after the extraction | `src/PhotoPrint.UI/src/app/features/checkout/pages/delivery-step.ts:131` | yes |
| PPW-769 | 🟠 | product-admin.service's 11 endpoints migrated to BaseApiService with zero tests | `src/PhotoPrint.UI/src/app/core/services/product-admin.service.ts:73` | yes |
| PPW-770 | 🟠 | admin.service downloadZip and getOrderPhotos migrated but left untested | `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:81` | yes |
| PPW-771 | 🟠 | Playwright smoke-tests the dev bundle (npm start), never the production build | `src/PhotoPrint.UI/playwright.config.ts:25` | no — follow-up |
| PPW-772 | 🟠 | Guest 401 in the error interceptor deletes the guest token and nothing re-issues it outside the upload page | `src/PhotoPrint.UI/src/app/core/interceptors/error.interceptor.ts:33` | no — out of diff scope |
| PPW-773 | 🟠 | README's "real-money smoke paths" claim overstates what the three e2e specs cover | `README.md:88` | yes |
| PPW-774 | 🟠 | New Stripe entries in docker-compose.yml silently override a developer's real keys from .env | `docker-compose.yml:51` | yes |
| PPW-775 | 🟡 | gitleaks allowlist misses the whsec_e2e_placeholder literal and is hand-synced with hooks/pre-commit | `.gitleaks.toml:24` | later |
| PPW-776 | 🟡 | anyComponentStyle budget (4kB warning / 16kB error) matches neither the story's 4kB error criterion nor current stylesheet sizes | `src/PhotoPrint.UI/angular.json:57` | later |
| PPW-777 | 🟡 | webServer command hardcodes port 4200 while the wait URL is configurable, so E2E_BASE_URL cannot actually be changed | `src/PhotoPrint.UI/playwright.config.ts:25` | later |
| PPW-778 | 🟡 | Locker map popup interpolates locker name/address into raw HTML | `src/PhotoPrint.UI/src/app/features/checkout/components/locker-map.ts:135` | later |
| PPW-779 | 🟡 | bolt.md claims three consecutive green CI runs, contradicted by its own Runs table | `memory-bank/bolts/066-ci-quality-gates/bolt.md:74` | later |
| PPW-780 | 🟡 | Implementation walkthrough records inheritance plus a shared error path, but the code uses injection and has neither | `memory-bank/bolts/067-ui-scaling-and-e2e-ui/implementation-walkthrough.md:20` | later |
| PPW-781 | 🟡 | DEPLOYMENT.md file and workflow tables never mention the new e2e stack | `docs/DEPLOYMENT.md:79` | later |
| PPW-782 | 🟡 | E2E_* variables documented in .env.example, which no e2e code reads | `.env.example:84` | later |
| PPW-783 | 🟡 | playwright-e2e workflow has push and pull_request triggers but no concurrency group, doubling every PR run | `.github/workflows/playwright-e2e.yml:7` | later |
| PPW-784 | 🟡 | Descoped e2e remainder lives only in bolt docs with no tracked follow-up | `memory-bank/bolts/067-ui-scaling-and-e2e-ui/test-walkthrough.md:73` | later |
| PPW-785 | 🟡 | Profile page stylesheet copied verbatim into all three extracted child components, dead rules included | `src/PhotoPrint.UI/src/app/features/account/pages/profile/components/personal-info-form/personal-info-form.scss:1` | later |
| PPW-786 | 🟡 | Blob-to-file save logic duplicated between admin.service and a page, and the two copies disagree | `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:82` | later |
| PPW-787 | 🟡 | reuseExistingServer outside CI silently tests whatever server is already on the port, e.g. another worktree's | `src/PhotoPrint.UI/playwright.config.ts:27` | later |
| PPW-788 | 🟡 | E2E workflow triggers leave the deploying commit as the one never smoke-tested | `.github/workflows/playwright-e2e.yml:1` | later |
| PPW-789 | 🟡 | No repaint/change-detection test for the two extracted profile forms | `src/PhotoPrint.UI/src/app/features/account/pages/profile/components/personal-info-form.ts:1` | later |
| PPW-790 | 🟡 | product.service getCatalog() caches the result but not the in-flight request, so concurrent callers duplicate the call | `src/PhotoPrint.UI/src/app/core/services/product.service.ts:22` | later |
| PPW-791 | 🟡 | delivery-step shipping-cost subscriptions are never torn down and write shared checkout state after destroy | `src/PhotoPrint.UI/src/app/features/checkout/pages/delivery-step.ts:495` | later |
| PPW-792 | 🟡 | HomePage.ngOnInit catalog subscription has no takeUntilDestroyed | `src/PhotoPrint.UI/src/app/features/home/home-page.ts:53` | later |
| PPW-793 | 🟡 | Pricing teaser advertises hardcoded prices when the catalog call fails | `src/PhotoPrint.UI/src/app/features/home/components/pricing-teaser/pricing-teaser.html:20` | later |
| PPW-794 | 🟡 | Order ZIP download revokes the blob URL in the same tick as the click, risking a lost download | `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:89` | later |
| PPW-795 | 🟡 | New guest e2e never asserts guestSession survival, the repo's most re-found defect class | `src/PhotoPrint.UI/e2e/guest-checkout.spec.ts:40` | later |
| PPW-796 | 🟡 | README e2e run recipe is POSIX-only on a Windows-primary project | `README.md:95` | later |
| PPW-797 | ⚪ | Dead leftovers in profile-page after the component extraction (Router imported and injected but never used) | `src/PhotoPrint.UI/src/app/features/account/pages/profile/profile-page.ts:10` | later |
| PPW-798 | ⚪ | Prettier-only reflows recorded in the walkthrough as component split wiring | `memory-bank/bolts/067-ui-scaling-and-e2e-ui/implementation-walkthrough.md:51` | later |
| PPW-799 | ⚪ | BaseApiService dropped story 001's named payload and shipped one new option with no caller | `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:10` | later |
| PPW-800 | ⚪ | Fix-stage ruling dismissed the gitleaks sync note after checking the wrong hook | `memory-bank/intents/030-ui-scaling-and-e2e/units/001-ci-quality-gates/construction-log.md:59` | later |
| PPW-801 | ⚪ | Romanian mobile-phone regex copied into four files, with a fifth divergent rule | `src/PhotoPrint.UI/src/app/features/account/pages/profile/profile-page.ts:93` | later |
| PPW-802 | ⚪ | Tier range label formatted a third time in HomePage instead of reusing the shared util | `src/PhotoPrint.UI/src/app/features/home/home-page.ts:43` | later |
| PPW-803 | ⚪ | Third copy of the invalid-field helper, this one with a hand-rolled change-detection counter | `src/PhotoPrint.UI/src/app/features/account/pages/saved-addresses/components/address-form/address-form.ts:32` | later |
| PPW-804 | ⚪ | Address form wrapper and action buttons duplicated for the add and edit cases | `src/PhotoPrint.UI/src/app/features/account/pages/saved-addresses/saved-addresses-page.ts:90` | later |
| PPW-805 | ⚪ | Identity map in admin.service over a response that already has the target shape | `src/PhotoPrint.UI/src/app/core/services/admin.service.ts:58` | later |
| PPW-806 | ⚪ | Saved-addresses cap hardcoded in the toast text next to the constant that holds it | `src/PhotoPrint.UI/src/app/features/account/pages/saved-addresses/saved-addresses-page.ts:236` | later |
| PPW-807 | ⚪ | tech-stack.md's documented count of over-budget stylesheets does not match the recorded build | `memory-bank/standards/tech-stack.md:22` | later |
| PPW-808 | ⚪ | Dead account-page component reformatted instead of deleted | `src/PhotoPrint.UI/src/app/features/account/pages/account-page.ts:1` | later |
| PPW-809 | ⚪ | Large Prettier reformat lands with no format script or CI check to hold it | `src/PhotoPrint.UI/package.json:9` | later |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| LockerSelector reads its required `searchControl` input once, so a reassigned parent control would leave the child bound to a dead FormControl | `delivery-step.ts:373` declares `readonly citySearch = this.fb.control('')` once and never reassigns it; no path exists to the stale binding |

## Notes for the fixer

- Order: PPW-762, PPW-763, PPW-764 first — the three release gates. Everything else after.
- Two findings' root cause sits **outside this branch's diff**: PPW-764's seeder half (`ProductCatalogSeed.cs`, `Program.cs`) and PPW-772 (`error.interceptor.ts`, `guest-or-auth.guard.ts`). Fix only the in-diff half of PPW-764 (`e2e/support/stack.ts`) and leave PPW-772 open with its pre-check attached; the scope call is the owner's.
- PPW-764, PPW-766 and PPW-772 carry an **Approach pre-check** on their ledger rows. Follow the revision as written and re-check only deviations. PPW-772's suggested fix is **refuted** — do not add interceptor-driven guest-session minting.
- PPW-763 and PPW-766 both edit `e2e/realtime-order.spec.ts`; do them in one pass. PPW-765 and PPW-769 both want `product-admin.service.spec.ts`; one file serves both.
- PPW-770's row title is half wrong on purpose-of-record: `getOrderPhotos` returns JSON, not a blob. Only `downloadZip` needs the blob assertion.
- PPW-767 and PPW-773 are two different claims about one code fact (uncovered payment leg): PPW-767 is the ticked criterion, PPW-773 the README wording. Two fixes, not one.
- The Playwright suite cannot run on this machine (no Docker). Any fix touching `e2e/` is verified by reading plus the CI run recorded in the bolts' test walkthroughs, and PPW-763's and PPW-766's real proof only arrives from CI.
- Read the reformatted pages with `git diff --ignore-all-space` (PPW-809) — about two thirds of the frontend diff is Prettier churn interleaved with behaviour changes.
