---
stage: implement
bolt: 067-ui-scaling-and-e2e-ui
created: 2026-09-04T13:10:00Z
---

## Implementation Walkthrough: UI Scaling and E2E (UI half)

### Summary

Six data services now share one `BaseApiService` instead of each repeating the base URL, the
options object and the error funnel. The four biggest pages were split into a thin container plus
presentational children: the home page dropped from 951 lines to 54, profile from 473 to 224,
saved addresses from 498 to 325, and the delivery step from 567 to 497 with its Easybox locker
picker moved into its own component. Every split page kept its existing tests and gained new ones
over the extracted markup.

### Structure Overview

`BaseApiService` sits in `core/services/api/` and is extended, not injected: each service passes its
own resource path and keeps its public method names, so no caller changed. The page splits follow
one shape throughout — the container owns the signals, the forms and the HTTP calls; the children
take `input()` signals and raise `output()` events, hold no service and no state of their own, and
live in a `components/` folder beside the page they came from. Section components for the home page
are static markup with no inputs where the original had none; the account and checkout children
receive the container's `FormGroup` or `FormControl` and render into it, which keeps one validation
source and lets the container's submit gate stay unchanged.

### Completed Work

- [x] `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts` — the shared HTTP base:
      resource URL composition, query-parameter building, and one error path for every verb.
- [x] `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.spec.ts` — 11 specs over that
      base, driven through `HttpTestingController`.
- [x] `account.service.ts`, `admin.service.ts`, `order.service.ts`, `product-admin.service.ts`,
      `product.service.ts`, `shipping.service.ts` — migrated onto the base; public signatures and
      their 129 existing tests unchanged.
- [x] `src/PhotoPrint.UI/src/app/features/home/home-page.ts` — 951 → 54 lines; now a container that
      composes seven sections.
- [x] `features/home/components/` — `hero-section`, `format-strip`, `photo-mosaic`, `how-it-works`,
      `pricing-teaser`, `quality-highlight`, `cta-banner`, each with its own template and styles.
      Splitting the stylesheet took the home page off the 4 kB per-component style warning list.
- [x] `home-page.spec.ts`, `format-strip.spec.ts`, `pricing-teaser.spec.ts` — 12 new assertions over
      the composed page and the two sections that take inputs.
- [x] `features/account/pages/profile/profile-page.ts` — 473 → 224 lines, plus
      `personal-info-form`, `password-change-form` and `account-deletion-card` children.
- [x] `features/account/pages/saved-addresses/saved-addresses-page.ts` — 498 → 325 lines, plus
      `address-form` and `address-list-item` children.
- [x] `profile-page.spec.ts`, `saved-addresses-page.spec.ts` — 13 new rendering assertions covering
      the child markup through the container.
- [x] `features/account/pages/account-layout.ts`, `account-page.ts`, `account.routes.ts` — follow-on
      wiring for the two split pages.
- [x] `features/checkout/components/locker-selector.{ts,html,scss}` — the Easybox picker: city
      search field, locker list with selection, the map, the search-failure retry and the
      "pick a locker" error, all driven by inputs and outputs.
- [x] `features/checkout/pages/delivery-step.ts` — 567 → 497 lines; keeps the search control, the
      HTTP calls and the continue gate, and renders the selector.
- [x] `features/checkout/components/locker-selector.spec.ts` — 6 new specs: list rendering and
      selection, the click reaching the container, the map's inputs and its `lockerSelected` output
      being forwarded, the search-error retry, the empty-city message only after a search, and the
      validation error only when the container asks for it.
- [x] `features/checkout/pages/delivery-step.spec.ts` — one new spec,
      *"keeps Continue disabled for a restored delivery method until both server prices arrive"*,
      covering the testable half of backlog row PPW-699.

### Key Decisions

- **Inheritance, not composition, for `BaseApiService`.** Every one of the six services is a thin
  wrapper over one resource; extending kept all call sites and all existing service tests untouched,
  which is the whole point of a refactor bolt.
- **Children take the parent's `FormGroup`/`FormControl`.** Passing form state down and events up
  instead of duplicating controls keeps one validation source, so the containers' submit gates and
  their existing tests needed no change.
- **Home sections are static where the original markup was static.** Inventing inputs for text that
  never varied would have added surface without adding behaviour; only `format-strip` and
  `pricing-teaser` take data.
- **The locker selector stays presentational.** Search debouncing, the HTTP call and the failure flag
  remain in the delivery step, so the extracted component has no service and no lifecycle beyond
  mirroring the search control into a signal.
- **PPW-699 covered by the restored-method path.** `selectMethod()` refuses to run before prices
  arrive, so a method restored from checkout state is the only way to reach the gate with a chosen
  method and prices still missing. Courier rather than Easybox, to avoid an unrelated locker request
  that `HttpTestingController.verify()` would flag.

### Deviations from Plan

None in substance. The plan already carried the adversarial design check's 17 findings (3 blockers)
folded in. Backlog row PPW-699 was pulled in *in part*, exactly as the plan ruled: the continue-gate
half now has a test; the unreachable per-field `maxlength` branch stays open and the row stays open.
Nothing under `reviews/` was edited.

### Dependencies Added

None.

### Developer Notes

- Signal inputs are set in these specs with `fixture.componentRef.setInput(...)`; a plain property
  assignment does not reach an `input()`.
- `delivery-step.spec.ts` is CRLF and has no trailing newline — append to it with a script that
  matches, or the diff turns into a whole-file rewrite.
- The zoneless signal mirror in the extracted form children cannot be proven by Vitest: the fixture's
  `detectChanges()` refreshes OnPush children whether or not they are dirty, so the staleness it
  guards against does not reproduce in the harness.
