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

Existing Vitest coverage: `saved-addresses-page.spec.ts` (12 tests), `profile-page.spec.ts` (11),
`delivery-step.spec.ts` (28), plus the service specs. `home-page.ts` has no spec at all.

**That coverage cannot detect this refactor breaking.** The two account specs contain zero DOM
queries — every assertion drives the component instance, so both templates could render nothing and
all 23 tests would stay green. In `delivery-step.spec.ts` exactly one assertion reaches the block
being extracted (`By.css('.easybox-section')`); `.locker-item`, `.locker-list`, `.city-search`,
`.no-lockers`, `.search-error` and `app-locker-map` appear in no spec in the repo. So "the existing
specs still pass" is **not** evidence here, and this plan does not treat it as such: each split is
preceded by DOM assertions written against the **pre-split** component, which then become the
baseline the split has to keep green.

### Deliverables

1. `core/services/api/base-api.service.ts` + spec — typed `get/post/put/patch/delete` over the API
   root, optional query params, and an escape hatch for bespoke calls. No `Idempotency-Key` support:
   its only producer is `payment.service.ts`, which another session owns this wave, so shipping the
   header now would be a mechanism with no caller and no real exercise.
2. Six data services migrated onto it: `order`, `product`, `shipping`, `account`, `admin`,
   `product-admin` (see Scope boundaries for the ones deliberately left alone).
3. `features/home/` — a thin container plus seven section components, each with its own stylesheet,
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
resource path, so every existing service spec keeps asserting the same URLs. Three constraints,
each from a real trap:

- **The URL it builds is absolute.** `jwtInterceptor` and `guestInterceptor` both gate on
  `req.url.startsWith(environment.apiUrl)`; a relative path would drop the `Authorization` and
  `X-Guest-Token` headers with no error anywhere. The base's own spec installs both interceptors
  and asserts the header arrives.
- **A query param is dropped only when it is `undefined` or `null`, never when it is empty.**
  `ShippingService.getLockers('')` is a real call and `delivery-step.spec.ts` asserts the literal
  URL `…/shipping/lockers?city=`; a "drop falsy" helper would silently break six tests.
- **Escape-hatch and shape-preserving call sites, named up front**: `admin.service.ts`'s blob
  invoice download (`responseType: 'blob'` plus its DOM side effect), `product.service.ts`'s
  `BehaviorSubject` catalog cache (whose spec asserts `expectNone` on the second call, so the cache
  must not be routed through the base), and the `map()` projections in `order.service.ts` and
  `admin.service.ts`, which stay in the service.

**Page breakups.** Same pattern each time: the container keeps every service call, signal, form
group and public method the existing specs drive; children take `input()`s and emit `output()`s and
own no state. Nothing moves out of the container that a spec calls on the component instance.

Five rules make that safe here, each one a trap this codebase actually sets:

1. **Reactive forms across a boundary.** `formControlName` resolves its `ControlContainer` with
   `@Host() @SkipSelf()`, and a child's host element stops that lookup — so a moved field throws
   NG01050 at runtime rather than failing a test. Every form child therefore declares
   `viewProviders: [{ provide: ControlContainer, useExisting: FormGroupDirective }]`, and the
   `<form [formGroup]>` element stays in the container (a nested `<form>` is invalid HTML and
   swallows `ngSubmit`). The `fi()` / `isInvalid()` helpers the moved markup calls move with it.
2. **Styles do not travel with markup.** Emulated encapsulation scopes every rule to its own
   component, so each moved block's rules move too — and rules used on *both* sides of the split
   (`.field-error` in `delivery-step.ts` is used by the locker block and by the address form that
   stays) are duplicated deliberately, not moved.
3. **A host element appears where there was none.** Every new child sets `:host { display: block }`
   — or `display: contents` where it sits inside a flex parent and must not become a flex item, as
   the locker block does inside `.delivery-step`.
4. **Zoneless change detection.** An OnPush child re-renders when an input binding changes or an
   event fires inside it. Everything passed down here is read from a signal in the parent template,
   which is safe; the exceptions are expressions derived from *form* state (`citySearch.value`,
   `passwordForm.errors?.['mismatch']`, `fi()`, `isInvalid()`), which today refresh only because
   they share one view with the rest of the page. `cdr.markForCheck()` marks a view and its
   ancestors — never a child — so those calls do nothing once the markup is in a child. Any child
   that renders form-derived state mirrors that state into a signal, the way `delivery-step.ts`
   already does with `toSignal(...statusChanges)`.
5. **Signal inputs.** New children use `input()` / `output()`, consistent with `shared/components/`;
   their specs set inputs with `fixture.componentRef.setInput(...)`, since an `input()` is readonly.
   No component mixes the two styles.

**Home.** Seven children, not the five the story guessed: `hero-section`, `photo-mosaic`,
`format-strip`, `how-it-works`, `quality-highlight`, `pricing-teaser`, `cta-banner`. The mosaic is
split from the hero rather than bundled with it because hero (≈2.7 kB of source) plus mosaic
(≈3.5 kB) would land a single child near the 4 kB warning the split exists to clear; separately none
exceeds ~2.3 kB. Only `pricing-teaser` takes data (the tier cards and product name the container
computes from the catalog); the rest are static markup. Each child gets an external `.scss`. The
source file carries a UTF-8 BOM and double-encoded comment banners (`â•â•`); the new files are
written without either, and the banners are dropped rather than copied.

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
| `delivery-step.spec.ts` (28 tests) | They drive `selectMethod` and assert `lockers()`, `selectedLockerId()`, `lockerSearchError()` and HTTP URLs — all of which stay on the container. Only `By.css('.easybox-section')` reaches the extracted markup, so these tests are a weak witness; the new DOM assertions are the real gate. |
| `saved-addresses-page.spec.ts` (12) + `profile-page.spec.ts` (11) | 23 tests, none with a DOM query. They keep the containers' methods honest and prove nothing about the templates; rendering assertions are added before each split. |
| `checkout-shell` / checkout routes | Unchanged: the delivery step keeps its selector, path and route. |
| Bolt 066's `guest-checkout.spec.ts` | Drives the delivery step's **Courier** path and the address form — never Easybox, so it exercises none of the extracted locker code. It proves the container and the address form survive; re-run after the delivery-step commit. |
| The 4 kB `anyComponentStyle` warning list | Home drops off it; the admin pages stay. |
| `angular.json` budgets | New component files are lazy-loaded with their page, so `initial` should not move; checked by a production build at the end. |

### Failure-mode table

| What can fail | What should happen | Which test proves it | What is logged |
|---|---|---|---|
| A migrated service changes a URL, verb or param | Its existing spec's `expectOne` fails | `order/product/shipping/account/admin.service.spec.ts` | Vitest diff of expected vs actual request |
| The base emits a relative URL, so the interceptors skip it and the request goes out unauthenticated | Absolute URL always; the base's spec runs with both interceptors installed and asserts the header | `base-api.service.spec.ts` (new) | Vitest |
| A query param with an undefined value is sent as the string "undefined" | The param is omitted | `base-api.service.spec.ts` (new) | Vitest |
| An empty-string param is dropped, changing `?city=` to no query at all | Empty strings survive; only `undefined`/`null` are dropped | `base-api.service.spec.ts` (new) + the six `delivery-step.spec.ts` locker tests | Vitest |
| The product catalog cache is routed through the base and stops caching | Second `getCatalog()` issues no request | `product.service.spec.ts` (`expectNone`, existing) | Vitest |
| A form field moved into a child cannot find its `formGroup` | NG01050 at render; caught by the new rendering assertions rather than at runtime in a browser | new DOM assertions on both account pages | Angular throws with the control name |
| A child renders form-derived state that never refreshes under zoneless CD | The child mirrors form state into a signal | new spec: type into a field, assert the error text appears | Vitest |
| A moved style block stays behind and the child renders unstyled | Rules move with their markup; shared rules are duplicated | not test-provable here — checked by reading the built stylesheet list and by eye | Angular budget list |
| A child component silently stops rendering a section | The container spec asserts every section is present **and** that the five `routerLink` hrefs are what they were | `home-page.spec.ts` (new) | Vitest |
| The pricing teaser stops falling back when the catalog is empty, or mishandles a tier count | Assertions for 0, 1, 3 and 4 tiers | `pricing-teaser.spec.ts` (new) | Vitest |
| The format strip loses its duplicated marquee loop and stops scrolling seamlessly | Item count is twice the label count | `format-strip.spec.ts` (new) | Vitest |
| The locker list stops reaching the map or the selection stops propagating | `delivery-step.spec.ts` locker tests fail | existing 28 tests, unchanged | Vitest |
| A form's validation moves with the markup and stops blocking submit | The container's form specs fail | existing account specs | Vitest |
| The refactor bloats the initial bundle | Production build fails or warns | `npm run build -- --configuration=production` at the end | Angular budget output |
| A visual regression on home | Compared by hand against the pre-refactor page; no screenshot baseline exists in this repo | — (stated gap) | — |

### Backlog sweep (`reviews/state/backlog.md`)

Areas touched: frontend components and frontend service plumbing — `tests`, `gallery`, `auth`, and
also `shipping` and `orders`, because the delivery step and the order/admin services are in scope.
No `reviews/state/**` file is edited (the coordinator writes the notes at merge time).

| Rows | Ruling |
|---|---|
| PPW-189, PPW-190, PPW-197, PPW-217, PPW-231, PPW-239, PPW-143, PPW-146, PPW-191, PPW-192, PPW-193 (`gallery` + its `tests` rows) | **re-deferred**: all sit in order-detail, the lightbox and the upload gallery — pages this bolt does not break up. |
| PPW-123, PPW-145, PPW-133 (`auth`, guest-session self-heal) | **re-deferred**: this bolt deliberately leaves `auth.service.ts` and `guest-auth.service.ts` un-migrated for exactly the reason these rows exist. |
| PPW-622, PPW-642 (`auth`, buyer address in sessionStorage, returnUrl on logout) | **re-deferred**: checkout/session behaviour owned by another session this wave. |
| PPW-101, PPW-125 and the remaining `tests` rows | **re-deferred**: backend and upload-page unit gaps, outside these four pages. |
| **PPW-699** (`shipping`, 🟡 — "delivery-step's shipping-costs continue gate is untested and the new per-field maxlength branch is unreachable in the case it was added for") | **pulled in, in part.** The untested half sits on `canContinue()`'s `shippingCostsReady()` check in the very file story 004 refactors, and the pre-split baseline assertions cover it anyway, so a test lands here. The maxlength half is a behavioural question about the address form, which this bolt deliberately does not restructure, so that half stays open. The row is not closed and its ledger is not edited: nothing under `reviews/` is this session's to write this wave. |
| PPW-329 to PPW-334, PPW-394, PPW-464, PPW-465 (`shipping`) | **re-deferred**: all backend Sameday client/AWB behaviour; this bolt changes no backend file and does not touch `shipping.service.ts`'s semantics, only where its HTTP call is composed. |
| PPW-194, PPW-211, PPW-215, PPW-426, PPW-504, PPW-555, PPW-610, PPW-690, PPW-691, PPW-692, PPW-709 (`orders`) | **re-deferred**: backend ZIP/export and confirmation-page behaviour. The confirmation-page rows belong to another session's surface this wave; the rest are backend. |

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
4. **Home splits into seven children, not five.** The story's names were a guess at the sections;
   the page actually has hero, photo mosaic, format strip, how-it-works, quality highlight, pricing
   teaser and CTA — and the mosaic is its own component so the hero clears the 4 kB stylesheet
   warning this split exists to clear.
5. **Profile has no email-change form to extract.** Story 003 asks for `email-change-form`; the page
   shows the email as a disabled field and offers no change flow. The third extracted component is
   the account-deletion card, which is real.
6. **"No page > ~200 LOC" is met for home and the two account pages, not for `delivery-step.ts`.**
   Its remaining bulk is one address form plus its styles; splitting that too would move the 28
   specs' DOM out from under them for no structural gain. Final LOC is reported in the test report.
7. **No screenshot diff.** The story asks for before/after screenshots of home. The repo has no
   visual-regression tooling and this session has no browser stack for the SPA outside CI; the
   guarantee is instead: markup and style rules moved with their sections, `:host { display: block }`
   on every new child, plus the new container and section specs. Layout regressions from the added
   host elements are the residual risk, and they are not test-provable here.
8. **No `Idempotency-Key` on `BaseApiService`.** Story 001 asks for it. Its only producer is
   `payment.service.ts`, which another session owns this wave, so the header would ship with no
   caller — a mechanism whose only exercise is its own test, which the new-mechanism bar argues
   against. It is named for the follow-up bolt that migrates `payment.service.ts`.
9. **Story 004 states `delivery-step.ts` is 382 LOC; it is 567.** The file grew after the story was
   written. The extraction target is unchanged.
10. **The e2e safety net does not cover the extracted locker code.** Bolt 066's guest-checkout spec
    drives the Courier path only; the Easybox search, list and map have no e2e coverage. The gate for
    story 004 is therefore the new Vitest DOM assertions, not the e2e — the e2e proves the container
    and the address form still work.

### Acceptance criteria

- [ ] `BaseApiService` exists with typed verbs, params and an escape hatch; its own spec proves the
      absolute URL reaches the interceptors, that `undefined`/`null` params are dropped and empty
      strings are not.
- [ ] The six named services route through it, and their existing specs pass unchanged.
- [ ] `home-page.ts` is a container under ~120 LOC with seven section components, each with its own
      stylesheet; home leaves the 4 kB stylesheet warning list.
- [ ] New specs exist where there were none: home container (sections present + the five
      `routerLink` hrefs), `pricing-teaser` (0/1/3/4 tiers), `format-strip` (doubled marquee).
- [ ] `saved-addresses-page.ts` and `profile-page.ts` are containers plus the named children, with
      their 23 existing tests green **and** new rendering assertions that would fail if a child
      stopped rendering or a moved field lost its form group.
- [ ] `locker-selector` is extracted; `delivery-step.spec.ts`'s 28 tests pass unchanged, plus new
      assertions for the locker list, the map's inputs and outputs, the search error retry, and the
      shipping-costs continue gate (PPW-699's testable half).
- [ ] `npm test -- --watch=false` green; `npm run build` (production is the default configuration)
      within budget; the e2e suite green after the delivery-step commit.

### Adversarial design check (bolt-process.md stage-2 gate)

Run 2026-09-04 as a fresh subagent against the first draft, brief: "attack this refactoring plan —
which existing assertions would actually break, zoneless traps, Angular API choice, BaseApiService
compatibility, wave ownership, bundle effects, factual errors". 17 findings; all folded in above.
The three blockers and the material ones:

| # | Finding | Disposition |
|---|---|---|
| 1 | **Blocker.** `formControlName` cannot cross a component boundary — `@Host() @SkipSelf()` stops at the child's host — so every moved account-form field would throw NG01050 at runtime. | Rule 1 in Technical approach: `viewProviders` with `ControlContainer`, `<form>` stays in the container, helpers move with the markup. |
| 2 | **Blocker.** "The existing specs still pass" is not evidence: the two account specs have zero DOM queries, and only one assertion in `delivery-step.spec.ts` reaches the extracted block. | Pre-split DOM baselines are now the gate; recorded in Measured starting point and the acceptance criteria. |
| 3 | **Blocker (factual).** The caller table claimed spec coverage for `.locker-item`, `.city-search` and `app-locker-map` that exists nowhere in the repo. | Table corrected. |
| 4 | An "omit empty params" base would rewrite `?city=` and break six locker tests. | Base drops only `undefined`/`null`; asserted in its own spec. |
| 5 | Component styles do not travel with markup, and `.field-error` is used on both sides of the locker split. | Rule 2: rules move with their block; shared rules duplicated. |
| 6 | Extraction inserts a host element, so "markup moved verbatim" was false — the host becomes an inline box, and a flex item inside `.delivery-step`. | Rule 3: `:host { display: block }`, or `contents` in the flex parent. |
| 7 | Zoneless: form-derived expressions in a child refresh only by luck, and `markForCheck()` never reaches a child. | Rule 4: children mirror form state into a signal. |
| 8 | The e2e named as story 004's safety net never selects Easybox. | Deviation 10. |
| 9 | The home mitigation would miss dropped `routerLink`s, the doubled marquee and tier-count edges. | Failure-mode rows + acceptance criteria for the three new home specs. |
| 10 | The backlog sweep skipped `shipping` and `orders`, and PPW-699 sits on the file story 004 refactors. | Both areas ruled on; PPW-699 partly pulled in. |
| 11 | `hero-section` with the mosaic inside it would land near the 4 kB warning the split exists to clear. | Seven children; mosaic split out (deviation 4). |
| 12 | `Idempotency-Key` would ship with no caller. | Dropped (deviation 8). |
| 13 | The escape hatch was asserted but never located. | Four call sites named in Technical approach. |
| 14 | A relative URL from the base would silently strip auth headers, since both interceptors gate on the `apiUrl` prefix. | Absolute URL; base spec runs with interceptors installed. |
| 15 | The repo mixes `@Input()` and `input()`; signal inputs are fine but specs must use `setInput`. | Rule 5. |
| 16 | Spec counts were overstated (12 + 11 = 23, not 26), story 004's LOC figure is stale, `admin-hub.service.ts` is SignalR not HTTP, and `--configuration=production` is already the default. | Corrected throughout; deviation 9 records the story's figure. |
| 17 | `home-page.ts` carries a UTF-8 BOM and double-encoded banner comments. | New files written without either; banners dropped, as the comment rule requires anyway. |

Confirmed correct by the same check, so not changed: the wave-ownership claim (all four pages are
reached only through `loadComponent`, so `initial` cannot move and inline-vs-external styles change
nothing about which chunk the CSS lands in); the three scope exclusions; refusing a second error
layer over `errorInterceptor`; home having six real sections and profile having no email-change
flow; and leaving the address form inside `delivery-step.ts`.

### Human validation checkpoint (stage 1)

Self-validated per the wave-1 coordinator addendum, after the adversarial design check ran and its
17 findings were folded in. Outcome: **approved to implement**, with ten deviations recorded.
Drafted while bolt 066's e2e verification ran in CI; implementation starts only once 066 is at
`review-pending`.
