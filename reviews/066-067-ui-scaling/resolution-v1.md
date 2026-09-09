---
type: resolution
target: 066-067-ui-scaling
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 3dfa20c
closed: 2026-09-08
---

# Resolution v1 — 066-067-ui-scaling

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-762 | fixed | `7256cce`, `479de38`, `3dfa20c` | `ARG RUNTIME_UID=1001` plus a recreated `app` user, so the runtime uid no longer follows the base tag; `ContainerRuntimeTests` guards the pin, `USER app` and the ENV-shadowing arg name. Volume chown recorded in `docs/DEPLOYMENT.md`. |
| PPW-763 | fixed | `ecceaa2`, `d14b936` | The wait now counts a WebSocket frame **or** a transport request carrying `id=` on `/hubs/admin-orders`. The row's premise is wrong: the transport is long polling, so a frame-only gate could never resolve. CI-only proof. |
| PPW-764 | fixed | `324c998`, `b1b363f`, `479de38` | `stack.ts` throws unless `E2E_ADMIN_EMAIL`/`E2E_ADMIN_PASSWORD` are exported; guarded against a `??` fallback and against CI exporting after the suite. CI derives them from the seed unless secrets override. Seeder half parked. |
| PPW-765 | fixed | `e6f4f42` | `BaseApiService`'s doc comment and bolt 067's criterion now name the six migrated services and the six that still compose their own URLs. |
| PPW-766 | fixed | `546bc44` | Spec pins `FT-2026-0006`/`FT-2026-0007`, `DevDataSeed` ships both `Paid`, trace is `retain-on-failure`; `RealtimeE2eSeedTests` ties the seeded Paid count to `retries + 1`. |
| PPW-767 | fixed | `9b1c123` | Gap recorded only: bolt 066 carries an unticked criterion for the untested Stripe-to-confirmation leg and story 002 an AC note; the spec was deliberately not extended. |
| PPW-768 | fixed | `7ace250` | `delivery-step.spec.ts` clicks `.locker-item` and `.retry-link` in the child's rendered DOM instead of calling `selectLocker`/`retrySearch`. |
| PPW-769 | fixed | `5f4d434` | New `product-admin.service.spec.ts` pins verb, absolute URL and body for all nine endpoints (the row says 11), plus a guard that `environment.apiUrl` still equals the literal. |
| PPW-770 | fixed | `6a8070a`, `eb6336e` | `admin.service.spec` asserts `responseType === 'blob'`, the download-zip URL and the saved filename; `order.service.spec` pins `GET /orders/:id/photos`, which is JSON. Both now use a literal API root plus an `environment.apiUrl` guard. |
| PPW-771 | deferred | — | parked: a production-bundle Playwright project plus its CI job is a follow-up bolt, which the review itself marks no-follow-up; the owner decides. |
| PPW-772 | deferred | — | parked: the suggested self-heal is refuted (the guest token is the `GuestSessions` row id) and the alternative edits files outside this diff; the owner decides. |
| PPW-773 | fixed | `b9ecd80`, `b1b363f`, `3dfa20c` | README, `tech-stack.md`, the workflow header and bolt 066's overview say pre-payment funnel plus admin paths, and name what is uncovered: order creation, Stripe intent, webhook, invoice, AWB. |
| PPW-774 | fixed | `8588c62`, `b1b363f` | Compose Stripe keys interpolate as `${Stripe__*:-placeholder}`, so a real key in `.env` wins; `ComposeEnvPrecedenceTests` guards the precedence. The README no longer claims both compose files pin placeholders. |
| PPW-775 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-776 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-777 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-778 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-779 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-780 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-781 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-782 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-783 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-784 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-785 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-786 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-787 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-788 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-789 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-790 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-791 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-792 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-793 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-794 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-795 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-796 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-797 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-798 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-799 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-800 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-801 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-802 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-803 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-804 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-805 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-806 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-807 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-808 | backlog | — | minor — ledger backlog, out of round 1's scope |
| PPW-809 | backlog | — | minor — ledger backlog, out of round 1's scope |

## Scope

| Cluster | Findings | Files | Protocol |
|---|---|---|---|
| A — container runtime uid + compose key precedence | PPW-762, PPW-774 | `Dockerfile` · `docker-compose.yml` · `src/PhotoPrint.Tests/Unit/Configuration/ContainerRuntimeTests.cs` | — |
| B — realtime e2e attempts + seeded Paid orders | PPW-763, PPW-766 | `src/PhotoPrint.UI/e2e/realtime-order.spec.ts` · `src/PhotoPrint.UI/playwright.config.ts` · `src/PhotoPrint.API/Data/Seed/DevDataSeed.cs` · `src/PhotoPrint.Tests/Unit/Data/RealtimeE2eSeedTests.cs` | realtime-order attempts (PPW-763, PPW-766) |
| C — e2e admin credentials | PPW-764 | `src/PhotoPrint.UI/e2e/support/stack.ts` · `src/PhotoPrint.UI/e2e/admin-login.spec.ts` · `.github/workflows/playwright-e2e.yml` · `.env.example` · `README.md` | — |
| D — API URL ownership + migrated-service specs | PPW-765, PPW-769, PPW-770 | `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts` · `core/services/product-admin.service.spec.ts` · `core/services/admin.service.spec.ts` · `memory-bank/bolts/067-ui-scaling-and-e2e-ui/bolt.md` | API URL ownership (PPW-765, PPW-769, PPW-770) |
| E — locker outputs driven through the parent DOM | PPW-768 | `src/PhotoPrint.UI/src/app/features/checkout/pages/delivery-step.spec.ts` | — |
| F — e2e coverage claims | PPW-767, PPW-773 | `README.md` · `memory-bank/standards/tech-stack.md` · `.github/workflows/playwright-e2e.yml` · `memory-bank/bolts/066-ci-quality-gates/bolt.md` · `memory-bank/intents/030-ui-scaling-and-e2e/units/001-ci-quality-gates/stories/002-playwright-e2e-smoke-tests.md` | — |
| G — parked scope calls | PPW-771, PPW-772 | — (no edit: both root causes sit outside this branch's diff) | — |

## Decisions

### Protocol — realtime-order attempts (PPW-763, PPW-766)

States of a seeded order the realtime spec can drive: `Paid` (a usable target), `Printing`
(consumed by an attempt), any other status (never a target).

- An attempt drives **exactly one** order, picked from a pinned list of seeded order numbers,
  and **never** PATCHes an order it has not first read as `Paid`.
- A CI job runs **at most** `retries + 1` attempts, so the seed ships **at least** `retries + 1`
  orders in `Paid` — today 2 (`FT-2026-0006`, `FT-2026-0007`) against `retries: 1`.
- An attempt that finds **no** pinned order in `Paid` fails naming the pinned numbers and their
  observed statuses; it **never** falls back to an unpinned order.
- **Only** `DevDataSeed` mints those Paid orders. The spec never creates or re-seeds one and
  teardown never resets a consumed one: `OrderStatusMachine` has no `Printing → Paid` edge and a
  second `--seed-dev` is a no-op.
- The handshake assertion counts **either** a WebSocket frame on `/hubs/admin-orders` **or** a
  transport request carrying `id=`, and **never** the `/negotiate` POST. The real transport is long
  polling — the hub needs the Admin role, a WebSocket can only carry its token in the query string,
  and no `JwtBearerEvents.OnMessageReceived` reads one — so a WebSocket-only gate never resolves.

### Protocol — API URL ownership (PPW-765, PPW-769, PPW-770)

- **Exactly one** module composes an absolute API URL from `environment.apiUrl`: `BaseApiService`.
  A migrated service **never** reads `environment.apiUrl` itself.
- `BaseApiService`'s doc comment names the migrated services and the unmigrated remainder (auth,
  guest-auth, cart, payment, upload, admin-hub), so it can **only** claim what the code does.
- **Every** endpoint of a migrated service is pinned by a spec asserting the literal absolute URL
  and the HTTP verb, so a URL or verb drift reddens before review.
- A file endpoint is pinned by `responseType === 'blob'`; a JSON endpoint **never** carries that
  assertion (`getOrderPhotos` returns JSON, so PPW-770's title is half wrong).

### The runtime uid is pinned by `RUNTIME_UID`, not `APP_UID` (PPW-762)

The check asked for `APP_UID`. Docker turns every `ARG` name into a build-stage variable, and the
base image already exports `APP_UID=1654`; an `ARG APP_UID` would shadow that export for the whole
stage and change what `USER $APP_UID` means elsewhere. `RUNTIME_UID` cannot collide, so the
Dockerfile takes `ARG RUNTIME_UID=1001`, deletes the inherited `app` user and recreates it at that
uid. `ContainerRuntimeTests` asserts the literal 1001, the recreate, and that no `ARG APP_UID` is
reintroduced. Cluster A carries no protocol block: PPW-762 owns the image’s runtime uid and
PPW-774 compose’s env-var precedence, so the two fixes share no state.

### PPW-764 is fixed on the e2e side only; the seeded pair stays (PPW-764)

Fixer decision, parked for the owner (default: fix the e2e half now). `ProductCatalogSeed.cs:32-33`
still holds `AdminEmail`/`AdminPassword` as literals; that file is outside this branch's diff and
rotating a seeded credential needs an owner call on how dev and CI get the new one. What landed:
`stack.ts` reads both from the environment and throws a Romanian message when either is missing, so
no credential is committed under `e2e/`, and CI derives them from the seed's own constants unless
`secrets.E2E_ADMIN_EMAIL`/`E2E_ADMIN_PASSWORD` are set. Two deviations from the check: the accessors
are lazy functions, not module constants, so `guest-checkout.spec.ts` (which imports only the
addresses) still runs without them; and no `E2E_ADMIN_*` entry was added to `docker-compose.e2e.yml`
because nothing in the API reads those names — the browser-side suite does.

### Retries stay at one; the seed grew instead (PPW-766)

The original check wanted `playwright-config.spec.ts` asserting `retries === 0`. Its own revision
supersedes that: a retry is worth keeping, so the fix removes the *reason* it could not pass. The
spec now drives a pinned order number, `DevDataSeed` ships two `Paid` orders, and
`RealtimeE2eSeedTests` fails if the seeded `Paid` count ever drops below `retries + 1` — the
invariant test for the realtime-order protocol. Trace moved to `retain-on-failure` so the first
attempt's trace survives too.

### The service specs assert a literal API root, plus one guard (PPW-769, PPW-770)

The checks wrote expectations as `${environment.apiUrl}/...`, which would pass even if `apiUrl`
drifted. The specs instead spell the absolute URL out (`http://localhost:5052/api/...`) and one
assertion per file pins `environment.apiUrl` to that same literal, so a drift reddens in exactly
one place instead of silently rewriting every expectation.

### PPW-767 is recorded, not closed by test (PPW-767)

Extending `guest-checkout.spec.ts` through Stripe test mode needs `STRIPE_TEST_*` keys in the e2e
compose stack and a confirmation-page assertion — new coverage, not a fix to this branch. Per the
brief the gap is only recorded: an unticked criterion in bolt 066, a "partially delivered"
sub-bullet on story 002's first AC, and a corrected walkthrough summary.

### Two scope calls parked for the owner (PPW-771, PPW-772)

Both are `deferred`, never `wont-fix`. PPW-771 (Playwright runs the dev bundle) needs a second
Playwright project against `dist/` plus a CI job to build it — a follow-up bolt, and the review
itself records no follow-up. PPW-772's suggested self-heal is refuted: the guest token *is* the
`GuestSessions` row id, so minting a new one from an interceptor cannot recover the old cart, and
the honest fix (server-side re-issue) edits files outside this diff.

### The 35 minor findings go to the ledger backlog (PPW-775, PPW-776, PPW-777, PPW-778, PPW-779, PPW-780, PPW-781, PPW-782, PPW-783, PPW-784, PPW-785, PPW-786, PPW-787, PPW-788, PPW-789, PPW-790, PPW-791, PPW-792, PPW-793, PPW-794, PPW-795, PPW-796, PPW-797, PPW-798, PPW-799, PPW-800, PPW-801, PPW-802, PPW-803, PPW-804, PPW-805, PPW-806, PPW-807, PPW-808, PPW-809)

Out of round 1's scope by the brief: 22 amber and 13 grey rows stay `backlog` in the table above
and live on in `ledger.md` for a later round or bolt. None of them blocks a serious fix.

### Revert proofs — repo-file and seed invariants (PPW-762, PPW-763, PPW-764, PPW-766, PPW-774)

Each fix was proved red by the mutation its finding describes, then reverted with `git checkout --`.
Counts are the runs the wrapper recorded, one run per cluster carrying that cluster's mutations:

- PPW-762 + PPW-774 — drop the uid pin, and restore the bare `sk_test_placeholder` → cluster A red,
  `passed 1, failed 2` (one test each in `ContainerRuntimeTests` / `ComposeEnvPrecedenceTests`).
- PPW-766 — cut the second `Paid` order from `DevDataSeed` → `RealtimeE2eSeedTests` red 1 of 1.
- PPW-764 — paste the seeded email or password back into `e2e/support/stack.ts`, and move the
  credential step after `npm run e2e` → `E2eCredentialsTests` red 2 of 2.
- Follow-ups — remove `USER app`, add a `?? 'qa@fototipar.ro'` fallback in `stack.ts`, and replace
  the two `$GITHUB_ENV` exports with a line that merely mentions them → red `passed 199, failed 3`,
  one per hardened assertion.
- PPW-763 — no local red is possible: the assertion lives in a Playwright spec and this machine has
  no Docker. Read-verified (dual-signal `expect.poll`, `npm run e2e:check` clean); the only real
  proof is the CI e2e job on this PR.

### Revert proofs — frontend specs (PPW-768, PPW-769, PPW-770)

- PPW-768 — delete the `(lockerSelected)` and `(retry)` bindings from `delivery-step.ts`'s inline
  template → cluster E red, `passed 29, failed 2` (the two new tests).
- PPW-769 + PPW-770 — flip `replacePricingTiers` to POST, swap `downloadZip`'s `getBlob` for `get`,
  and point `getOrderPhotos` at `/photo` → cluster D red, `passed 24, failed 3`.
- Follow-up guards — point `environment.apiUrl` at `http://localhost:5052` (no `/api`) → red
  `passed 2, failed 27`, the three API-root guards among them.

### Round review and test audit — round 1

Round review: 4 must-fix, all folded in. (1) PPW-763's own fix could never pass — traced the hub to
long polling; the wait is now transport-agnostic. (2) uid 1001 leaves an older `apidata` volume
owned by the base image's uid and a build-time `chown` cannot reach it — `docs/DEPLOYMENT.md` now
carries the one-line chown. (3) the README's "both compose files pin placeholder Stripe keys" became
false with PPW-774 — reworded. (4) two of the three service specs interpolated `environment.apiUrl`
instead of pinning it — guards added. Test audit: 6 pass, 3 suspect (`E2eCredentialsTests`,
`admin.service.spec`, `order.service.spec`), all three closed by the follow-ups; its revert-proof
arithmetic corrections are folded into the two blocks above.

Left open for the re-reviewer, none of them a fix to this round's diff: no runtime fail-fast if
`/app/Storage` is unwritable (a production mechanism, wider than PPW-762); `docker-compose.e2e.yml`
still carries bare Stripe placeholders that no test guards; `Stripe__PublishableKey` has no reader
in the API; `BaseApiService`'s hand-listed service names sit on a concrete class with no test; and
PPW-768's class survives in `password-change-form.submitted` and `address-list-item.remove`.
