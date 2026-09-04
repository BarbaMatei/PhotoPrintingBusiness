---
stage: test
bolt: 067-ui-scaling-and-e2e-ui
created: 2026-09-04T13:55:00Z
---

## Test Report: 002-ui-scaling-and-e2e-ui (UI half)

### Summary

- **Tests**: 126/126 passed (four scoped Vitest batches, 0 failed)
- **Coverage**: not measured — this repo has no coverage gate and none was added by this bolt
- **Build**: `npm run build` exit 0 — Initial total **330.13 kB** raw / **92.83 kB** estimated
  transfer; home is off the 4 kB per-component style warning list (only the five pre-existing
  admin/header stylesheets remain)
- **E2E**: `npm run e2e:check` exit 0 (config + spec typecheck). The Playwright run itself could
  not execute on this machine — see *What this suite cannot prove*.

Commands, exactly as run (one test process at a time):

```
node reviews/lib/run-scoped-tests.mjs 067-ui-scaling-and-e2e-ui --kind green --ui \
  --include "{base-api.service,account.service,admin.service,order.service,product.service,shipping.service}" --summary --no-events
      -> passed 43, failed 0, exit 0
node reviews/lib/run-scoped-tests.mjs 067-ui-scaling-and-e2e-ui --kind green --ui \
  --include "{home-page,format-strip,pricing-teaser}" --summary --no-events
      -> passed 12, failed 0, exit 1  (unhandled error — diagnosed and fixed, see Issues Found)
node reviews/lib/run-scoped-tests.mjs 067-ui-scaling-and-e2e-ui --kind green --ui \
  --include "{profile-page,saved-addresses-page}" --summary --no-events
      -> passed 36, failed 0, exit 0
node reviews/lib/run-scoped-tests.mjs 067-ui-scaling-and-e2e-ui --kind green --ui \
  --include "{locker-selector,delivery-step}" --summary --no-events
      -> passed 35, failed 0, exit 0
node reviews/lib/run-scoped-tests.mjs 067-ui-scaling-and-e2e-ui --kind green --ui \
  --include "{home-page,format-strip,pricing-teaser,profile-page}" --summary --no-events   (retest after the fixes)
      -> passed 29, failed 0, exit 0
npm --prefix src/PhotoPrint.UI run build        -> exit 0
npm --prefix src/PhotoPrint.UI run e2e:check    -> exit 0
```

No `dotnet test` was run: this bolt changes no backend file.

### Test Files

- [x] `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.spec.ts` - the shared HTTP base:
      absolute URL composition through `environment.apiUrl` (so `jwtInterceptor`/`guestInterceptor`
      still match), query-parameter building, `undefined`/`null` params omitted, empty strings kept,
      one path per verb.
- [x] `account.service.spec.ts`, `admin.service.spec.ts`, `order.service.spec.ts`,
      `product.service.spec.ts`, `shipping.service.spec.ts` - the migrated services' URLs, verbs and
      params, unchanged from before the migration; `product.service.spec.ts` also proves the catalog
      cache still issues no second request (`expectNone`).
- [x] `src/PhotoPrint.UI/src/app/features/home/home-page.spec.ts` - every section renders, the five
      call-to-action `routerLink` hrefs are unchanged, the first product/size feeds the pricing tiers,
      a product with no sizes is ignored, and the page still renders when the catalog request fails.
- [x] `features/home/components/format-strip/format-strip.spec.ts` - the marquee is duplicated
      (item count is twice the label count).
- [x] `features/home/components/pricing-teaser/pricing-teaser.spec.ts` - 0, 1, 3 and 4 tiers,
      including the fallback when the catalog is empty.
- [x] `features/account/pages/profile/profile-page.spec.ts` - the three extracted children render
      into the container's forms; validation still gates submit.
- [x] `features/account/pages/saved-addresses/saved-addresses-page.spec.ts` - the address form and
      list-item children render through the container; add/edit/delete flows unchanged.
- [x] `features/checkout/components/locker-selector.spec.ts` - list rendering and selection, the
      click reaching the container, the map's inputs and its forwarded `lockerSelected` output, the
      search-error retry, the empty-city message only after a search, the validation error only when
      the container asks for it.
- [x] `features/checkout/pages/delivery-step.spec.ts` - the 28 pre-existing delivery tests plus
      *"keeps Continue disabled for a restored delivery method until both server prices arrive"*.

### Acceptance Criteria Validation

- ❌ **No page > ~200 LOC** — not met for two of the four pages. Measured with `wc -l` against
  `origin/main`: `home-page.ts` 951 → **66**, `profile-page.ts` 473 → **217**,
  `saved-addresses-page.ts` 498 → **334**, `delivery-step.ts` 567 → **574**. The delivery step
  *grew* seven lines: commit d7b61b4 is a whole-file Prettier reflow (173 insertions / 166
  deletions) around the extraction, so the locker markup left the file but the reformat put more
  lines back. Home met the bar with room to spare; profile is 17 lines over; saved addresses and the
  delivery step are well over and would each need a second extraction pass.
- ✅ **All services route through BaseApiService** — the six data services extend it;
  `auth.service.ts` and `guest-auth.service.ts` are deliberately left out (guest-session handling,
  per the plan).
- ✅ **Within bundle budget** — production build exit 0, Initial total 330.13 kB / 92.83 kB transfer,
  no `initial` budget error; the new components are lazy-loaded with their pages.
- ✅ **No home visual regression** — compared by eye against the pre-refactor page and by the
  built stylesheet list; no screenshot baseline exists in this repo to compare against.

The walkthrough's line figures for the last three pages (224 / 325 / 497) were optimistic; the
numbers above are the measured ones and supersede them.

### Failure-mode table (from the plan, with the tests that actually prove each row)

| What can fail | Which test proves it | Result |
|---|---|---|
| A migrated service changes a URL, verb or param | `order/product/shipping/account/admin.service.spec.ts` `expectOne` assertions | pass (43-test batch) |
| The base emits a relative URL, so interceptors skip it and the request goes out unauthenticated | `base-api.service.spec.ts` — absolute-URL and header assertions | pass |
| A query param with an `undefined` value is sent as the string "undefined" | `base-api.service.spec.ts` | pass |
| An empty-string param is dropped, turning `?city=` into no query at all | `base-api.service.spec.ts` + the locker specs in `delivery-step.spec.ts` | pass |
| The catalog cache is routed through the base and stops caching | `product.service.spec.ts` (`expectNone`) | pass |
| A form field moved into a child cannot find its `formGroup` (NG01050 at render) | the new rendering assertions in `profile-page.spec.ts` and `saved-addresses-page.spec.ts` | pass |
| A child renders form-derived state that never refreshes under zoneless CD | *no test* — not reproducible in the Vitest harness (see below) | gap, stated |
| A moved style block stays behind and the child renders unstyled | not test-provable here — read the built stylesheet list and checked by eye | gap, stated |
| A child component silently stops rendering a section | `home-page.spec.ts` "renders every section of the page" + "keeps every call-to-action pointing where it did" | pass |
| The pricing teaser mishandles a tier count | `pricing-teaser.spec.ts` (0/1/3/4 tiers) | pass |
| The format strip loses its duplicated marquee loop | `format-strip.spec.ts` | pass |
| The locker list stops reaching the map, or selection stops propagating | the 28 pre-existing `delivery-step.spec.ts` tests + `locker-selector.spec.ts` | pass (35-test batch) |
| A form's validation moves with the markup and stops blocking submit | the existing account specs | pass (36-test batch) |
| The refactor bloats the initial bundle | `npm run build` | pass, 330.13 kB |
| A visual regression on home | — no screenshot baseline | gap, stated |

### PPW-699 (backlog row, pulled in *in part*)

- Tested half: **`delivery-step.spec.ts` → "keeps Continue disabled for a restored delivery method
  until both server prices arrive"** — it drives the Courier path (Easybox would fire an unrelated
  locker request that `HttpTestingController.verify()` would flag) and asserts `canContinue()`'s
  `shippingCostsReady()` check.
- Still open: the per-field `maxlength` half. It is a behavioural question about the address form,
  which this bolt does not restructure, so the branch remains unreachable in the case it was added
  for. The row stays open; nothing under `reviews/` was edited.

### Issues Found

1. **`home-page.ts` handled no catalog error — it escaped as an unhandled rejection.** The new
   "still renders the page when the catalog request fails" spec flushed a 500 and Vitest exited 1
   with `Errors 1 error` while reporting `passed 12, failed 0`. `git show origin/main` confirms the
   pre-split 951-line page had the same `next`-only `subscribe`, so this is a pre-existing gap the
   new test exposed. **Fixed**: `ngOnInit()` now takes `error: () => this.catalogSignal.set(null)`.
   Retest: 29 passed, exit 0.
2. **Duplicate `.hero__visual` wrapper.** `photo-mosaic.html` opened with its own
   `<div class="hero__visual" aria-hidden="true">` while `hero-section.html:76` already provides
   one. `.hero__visual` is styled only in `hero-section.scss:126`, so the inner copy was unstyled
   but still inserted an extra block-level wrapper. **Fixed** — removed, `.photo-mosaic` is now the
   component root; the spec's `app-hero-section app-photo-mosaic .photo-mosaic` selector still
   matches.
3. **Orphaned `showDeleteConfirm` signal in `profile-page.ts`.** The live toggle moved to
   `account-deletion-card.ts:16`; the container kept a dead signal and a dead `.set(false)` in the
   `requestDeletion()` success path. Dead code, not a behaviour bug
   (`account-deletion-card.html:3` switches on `deletionRequested()`). **Fixed** — both removed.
4. **Line-count criterion not met** for `saved-addresses-page.ts` (334) and `delivery-step.ts` (574);
   `profile-page.ts` (217) is marginally over. Recorded above, not fixed here: a second extraction
   pass is new work, not test-stage work.
5. **`product-admin.service.ts` has no spec** (pre-existing). It was migrated onto the base with the
   other five, so its migration is unproven by tests; the migration is a mechanical URL/verb move
   and was checked by reading.

### Fresh-eyes micro-review (two fresh subagents, read-only, per `bolt-process.md`)

Both were asked exactly the three gate questions (class or instance? new surface at the
new-mechanism bar? anything adjacent broken?).

- **Services half** — clean. Three minor "unused surface" notes on `BaseApiService`
  (`base-api.service.ts:6`, `:8-11`, `:26`). **Recorded, not fixed**: the `headers` escape hatch is
  an explicit plan acceptance criterion, and changing the base during the test stage buys no
  behaviour change.
- **Page-splits half** — one material finding (the duplicate `.hero__visual` wrapper) and one minor
  (the orphaned `showDeleteConfirm`), both fixed above, plus an explicit PASS on the e2e selectors:
  bolt 066's `guest-checkout.spec.ts` still matches the post-split DOM.

### What this suite cannot prove

- **Zoneless staleness in the extracted form children.** The fixture's `detectChanges()` refreshes
  OnPush children whether or not they are dirty, so the very failure the signal mirrors guard
  against does not reproduce in Vitest.
- **The Playwright suite did not run on this machine.** Docker is not on PATH here, so
  `docker-compose.e2e.yml` cannot start the API. Evidence stands on: `npm run e2e:check` exit 0,
  bolt 066's CI evidence (3/3 passed in 16.7 s, run 33807570557), and the reviewer's static PASS on
  the selectors after the split. **A CI e2e run on this branch is the missing check** — the
  post-split DOM has never been driven by a real browser.
- **No screenshot baseline** exists in this repo, so "no home visual regression" rests on reading
  the built stylesheet list and looking at the page.
- **Moved style blocks** are verified by eye and by the budget warning list, not by a test.
- **The dev-server e2e path is not the production build path**: `playwright.config.ts` runs
  `npm start`, so a production-only bundling or lazy-chunk fault would not surface there.
- **`product-admin.service.ts`** has no spec, as above.

### Notes

- Signal inputs are set with `fixture.componentRef.setInput(...)` throughout; a plain property
  assignment does not reach an `input()`.
- `delivery-step.spec.ts` is CRLF with no trailing newline — append to it with a script that
  preserves both, or the diff becomes a whole-file rewrite.
- `reviews/lib/run-scoped-tests.mjs` holds a machine-global lock, so the batches ran one at a time;
  brace lists in `--include` let several spec files share one Vitest process.
- Human-validation checkpoint (specsmd): validated in-session. The report is accepted with two
  criteria failing openly — the ~200 LOC bar on two pages, and no browser-level e2e run on this
  branch. Both are named for the reviewer rather than papered over.
