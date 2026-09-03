---
stage: plan
bolt: 067-ui-scaling-and-e2e-ui
created: 2026-09-04T00:55:00Z
---

## Implementation Plan: UI Scaling Refactor

### Objective

Make the Angular component layer maintainable: introduce a shared `BaseApiService` so data services
stop repeating HTTP plumbing, and break the four largest pages into a smart container plus
presentational children — verified by the Vitest suites those pages already have and by the gates
bolt 066 just installed.

### Measured starting point (2026-09-04)

| File | LOC | Built component stylesheet |
|---|---|---|
| `features/home/home-page.ts` | 951 | 10.98 kB (over the new 4 kB warning) |
| `features/checkout/pages/delivery-step.ts` | 567 | under 4 kB |
| `features/account/pages/saved-addresses/saved-addresses-page.ts` | 498 | under 4 kB |
| `features/account/pages/profile/profile-page.ts` | 473 | under 4 kB |
| Data services under `core/services/` | 14 files | — |

Existing Vitest coverage that must stay green: `saved-addresses-page.spec.ts` (14 tests),
`profile-page.spec.ts` (12), `delivery-step.spec.ts` (28), plus the service specs. `home-page.ts`
has no spec at all today.

### Deliverables

1. `core/services/api/base-api.service.ts` + spec — typed `get/post/put/patch/delete` over the API
   root, optional query params and `Idempotency-Key`, and an escape hatch for bespoke calls.
2. Six data services migrated onto it: `order`, `product`, `shipping`, `account`, `admin`,
   `product-admin` (see Scope boundaries for the ones deliberately left alone).
3. `features/home/` — a thin container plus one component per section, each with its own stylesheet,
   plus the first specs this page has ever had.
4. `features/account/pages/saved-addresses/components/` — `address-form`, `address-list-item`.
5. `features/account/pages/profile/components/` — `personal-info-form`, `password-change-form`,
   `account-deletion-card`.
6. `features/checkout/components/locker-selector.ts` — the Easybox search + list + map block lifted
   out of `delivery-step.ts`.

Each of the four page breakups is a separate commit, in the story order, so a bisect lands on one
page.

### Dependencies

- **Bolt 066** — the bundle budget and the three e2e smoke specs are the safety net for a refactor
  with no behavioural intent. The guest-checkout spec covers the delivery step; the admin specs
  cover nothing these pages touch, so the account and home pages lean on Vitest.
- No backend change, no API contract change, no migration.

### Technical approach

**BaseApiService.** A thin injectable that owns the `environment.apiUrl` prefix and the option
shapes the services repeat. It does **not** add error handling: `errorInterceptor` already maps
401/403/5xx/network centrally, and a second layer would double the toasts. It does not set
`withCredentials` either — see Deviations. Services keep their own public API and their own
resource path, so every existing service spec keeps asserting the same URLs.

**Page breakups.** Same pattern each time: the container keeps every service call, signal, form
group and public method the existing specs drive; children take `input()`s and emit `output()`s and
own no state. Nothing moves out of the container that a spec calls on the component instance. DOM
class names travel with the markup, so DOM-level assertions in the existing specs keep matching —
a child's markup still renders inside the parent fixture.

**Home.** Six real sections, not the five the story guessed: `hero-section` (hero + photo mosaic),
`format-strip`, `how-it-works`, `quality-highlight`, `pricing-teaser`, `cta-banner`. Only
`pricing-teaser` takes data (the tier cards and the product name the container computes from the
catalog); the rest are static markup. Each child gets an external `.scss`, which is also what pulls
home under the 4 kB stylesheet warning.

**Locker selector.** Inputs: the locker list, the selected id, the search `FormControl`, the two
error flags. Outputs: locker chosen, retry requested. The debounced search stream, the priming
logic, `selectMethod`, `canContinue` and the address form all stay in `delivery-step.ts` — that is
where its 28 specs point.

### Scope boundaries (wave-1 ownership)

Not touched, and why:

- `cart.service.ts`, `payment.service.ts`, `checkout-attempt.service.ts`, `checkout-state.service.ts`
  and the cart / review / confirmation / invoice pages — another session owns that surface this wave.
- `auth.service.ts` and `guest-auth.service.ts` — the guest/auth token matrix is this repo's most
  re-found defect cluster (definition-of-done class 11). A mechanical migration there buys tidiness
  and risks a class of defect the review history says is expensive; left for a bolt that can pay for
  the matrix walk.
- `upload.service.ts` — multipart upload with progress events; the escape hatch exists for it, but
  moving it is not worth a behaviour risk in this bolt.
- Any backend file, `.csproj`, `Directory.Packages.props`, existing workflow, memory-bank index or
  `reviews/state/**`.

### Caller-impact sweep

| Consumer of what this bolt changes | Effect |
|---|---|
| `order.service.spec.ts`, `product.service.spec.ts`, `shipping.service.spec.ts`, `account.service.spec.ts`, `admin.service.spec.ts` | URLs, verbs, params and bodies unchanged, so the `HttpTestingController` expectations still match. These specs are the migration's proof. |
| `product-admin.service.ts` (no spec) | Migrated last and exercised through `admin-products-page`; its lack of a spec is noted in the test report. |
| `home-page.ts` consumers: `app.routes.ts` (`loadComponent`) | Container keeps its class name and file path, so the route is untouched. |
| `delivery-step.spec.ts` (28 tests) driving `selectMethod`, `citySearch`, `.locker-item`, `app-locker-map` | Container API and DOM classes preserved; the child renders inside the same fixture. |
| `saved-addresses-page.spec.ts`, `profile-page.spec.ts` (26 tests) driving `openAddForm`, `saveNew`, `saveEdit`, `deleteAddress`, `saveProfile`, `changePassword`, `requestDeletion` | All stay on the container. |
| `checkout-shell` / checkout routes | Unchanged: the delivery step keeps its selector, path and route. |
| Bolt 066's `guest-checkout.spec.ts` | Drives the delivery step and the home hero link — re-run after the delivery-step commit. |
| The 4 kB `anyComponentStyle` warning list | Home drops off it; the admin pages stay. |
| `angular.json` budgets | New component files are lazy-loaded with their page, so `initial` should not move; checked by a production build at the end. |

### Failure-mode table

| What can fail | What should happen | Which test proves it | What is logged |
|---|---|---|---|
| A migrated service changes a URL, verb or param | Its existing spec's `expectOne` fails | `order/product/shipping/account/admin.service.spec.ts` | Vitest diff of expected vs actual request |
| `BaseApiService` drops the `Idempotency-Key` header | Header assertion fails | `base-api.service.spec.ts` (new) | Vitest |
| A query param with an undefined value is sent as the string "undefined" | The param is omitted | `base-api.service.spec.ts` (new) | Vitest |
| A child component silently stops rendering a section | The container spec asserting the section's presence fails | `home-page.spec.ts` (new) | Vitest |
| The pricing teaser stops falling back when the catalog is empty | Fallback cards assertion fails | `pricing-teaser.spec.ts` (new) | Vitest |
| The locker list stops reaching the map or the selection stops propagating | `delivery-step.spec.ts` locker tests fail | existing 28 tests, unchanged | Vitest |
| A form's validation moves with the markup and stops blocking submit | The container's form specs fail | existing account specs | Vitest |
| The refactor bloats the initial bundle | Production build fails or warns | `npm run build -- --configuration=production` at the end | Angular budget output |
| A visual regression on home | Compared by hand against the pre-refactor page; no screenshot baseline exists in this repo | — (stated gap) | — |

### Backlog sweep (`reviews/state/backlog.md`)

Areas touched: frontend components and frontend service plumbing — `tests`, `gallery`, `auth`.
No row is pulled in; no `reviews/state/**` file is edited.

| Rows | Ruling |
|---|---|
| PPW-189, PPW-190, PPW-197, PPW-217, PPW-231, PPW-239, PPW-143, PPW-146, PPW-191, PPW-192, PPW-193 (`gallery` + its `tests` rows) | **re-deferred**: all sit in order-detail, the lightbox and the upload gallery — pages this bolt does not break up. |
| PPW-123, PPW-145, PPW-133 (`auth`, guest-session self-heal) | **re-deferred**: this bolt deliberately leaves `auth.service.ts` and `guest-auth.service.ts` un-migrated for exactly the reason these rows exist. |
| PPW-622, PPW-642 (`auth`, buyer address in sessionStorage, returnUrl on logout) | **re-deferred**: checkout/session behaviour owned by another session this wave. |
| PPW-101, PPW-125 and the remaining `tests` rows | **re-deferred**: backend and upload-page unit gaps, outside these four pages. |

### Deviations from the stories (recorded)

1. **No `withCredentials: true`.** Story 001 asks for it, but this SPA carries no cookie: the JWT
   goes through `jwtInterceptor` and the guest token through `guestInterceptor`, and CLAUDE.md
   records that there is deliberately no refresh-token flow. Turning it on would change every
   request's CORS semantics to buy nothing.
2. **No error translation inside `BaseApiService`.** `errorInterceptor` already owns 401 logout /
   guest-token clearing and the 403/5xx/network toasts; a second layer would double user-facing
   messages. The base service passes errors through.
3. **Six of fourteen services migrated, not all.** See Scope boundaries: four belong to another
   session this wave, and three (auth, guest-auth, upload) carry a risk this bolt should not take.
   The unit brief's "all services route through BaseApiService" is therefore not met in full, and
   the remainder is named for a follow-up bolt.
4. **Home splits into six children, not five.** The story's names were a guess at the sections;
   the page actually has hero, format strip, how-it-works, quality highlight, pricing teaser and CTA.
5. **Profile has no email-change form to extract.** Story 003 asks for `email-change-form`; the page
   shows the email as a disabled field and offers no change flow. The third extracted component is
   the account-deletion card, which is real.
6. **"No page > ~200 LOC" is met for home and the two account pages, not for `delivery-step.ts`.**
   Its remaining bulk is one address form plus its styles; splitting that too would move the 28
   specs' DOM out from under them for no structural gain. Final LOC is reported in the test report.
7. **No screenshot diff.** The story asks for before/after screenshots of home. The repo has no
   visual-regression tooling and this session has no browser stack for the SPA outside CI; the
   guarantee is instead: identical markup and styles moved verbatim, plus the new container spec.

### Acceptance criteria

- [ ] `BaseApiService` exists with typed verbs, params, optional idempotency key and an escape hatch;
      its own spec covers those.
- [ ] The six named services route through it, and their existing specs pass unchanged.
- [ ] `home-page.ts` is a container under ~120 LOC with one component per section, each with its own
      stylesheet; home leaves the 4 kB stylesheet warning list.
- [ ] `saved-addresses-page.ts` and `profile-page.ts` are containers plus the named children, with
      their 26 existing tests green.
- [ ] `locker-selector` is extracted and `delivery-step.spec.ts`'s 28 tests pass unchanged.
- [ ] `npm test -- --watch=false` green; `npm run build -- --configuration=production` within budget;
      the e2e suite green after the delivery-step commit.

### Human validation checkpoint (stage 1)

Self-validated per the wave-1 coordinator addendum. Drafted while bolt 066's e2e verification ran in
CI; implementation starts only after 066 reaches `review-pending`. The adversarial design check runs
as a fresh subagent against this plan before any code, per `memory-bank/standards/bolt-process.md`.
