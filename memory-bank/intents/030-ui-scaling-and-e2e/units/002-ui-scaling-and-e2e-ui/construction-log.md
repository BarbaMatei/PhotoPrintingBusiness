---
unit: 002-ui-scaling-and-e2e-ui
intent: 030-ui-scaling-and-e2e
created: 2026-09-04T00:55:00Z
last_updated: 2026-09-04T13:10:00Z
---

# Construction Log: 002-ui-scaling-and-e2e-ui

## Original Plan

**From Inception**: 1 bolt planned
**Planned Date**: 2026-06-05T09:30:00Z

| Bolt ID | Stories | Type |
|---------|---------|------|
| 067-ui-scaling-and-e2e-ui | 4 stories | simple-construction-bolt |

## Replanning History

| Date | Action | Change | Reason | Approved |
|------|--------|--------|--------|----------|

*(None.)*

## Current Bolt Structure

| Bolt ID | Stories | Status | Changed |
|---------|---------|--------|---------|
| 067-ui-scaling-and-e2e-ui | 4 | 🔄 in progress (stage 3) | - |

## Execution History

| Date | Bolt | Event | Details |
|------|------|-------|---------|
| 2026-09-04T00:55:00Z | 067 | started | Stage 1: Plan (drafted while bolt 066's e2e verification ran in CI) |
| 2026-09-04T11:20:00Z | 067 | stage-complete | Plan → Implement, after the adversarial design check (17 findings, 3 blockers, all folded in) |
| 2026-09-04T11:45:00Z | 067 | story-complete | 001-base-api-service — BaseApiService + 11 specs; six services migrated, their 129 existing tests unchanged and green |
| 2026-09-04T12:05:00Z | 067 | story-complete | 002-home-page-breakup — 951 LOC → 54 LOC container + 7 section components; 12 new specs; home left the 4 kB stylesheet warning list |
| 2026-09-04T12:25:00Z | 067 | story-complete | 003-account-pages-breakup — profile 473→224 LOC, saved-addresses 498→325 LOC, 5 child components, 13 new rendering assertions |
| 2026-09-04T12:40:00Z | 067 | paused | Story 004 code complete (locker-selector extracted, delivery-step 567→497 LOC, its 28 tests green, production build clean). **Stopped here on the coordinator's soft stop.** |
| 2026-09-04T13:10:00Z | 067 | stage-complete | Implement → Test. Story 004's owed coverage written (6 locker-selector specs + the delivery-step continue gate, PPW-699 testable half); implementation-walkthrough.md added; build 330.13 kB initial |

## Notes

- **Where work stopped (2026-09-04T12:40Z).** All four stories' code is written and committed. What
  story 004 still owes is the *new* Vitest coverage the design check demanded, because the existing
  28 delivery-step tests barely touch the extracted markup: assertions for the locker list rendering,
  the map's inputs and its `lockerSelected` output reaching `selectLocker`, the search-error retry,
  and the shipping-costs continue gate (the testable half of PPW-699). After that: stage 3's test
  report, the fresh-eyes micro-review, and the flip to `review-pending`.
- **`bolt-complete.cjs` is not run here either**, for the reason recorded in unit 001's log.
- The zoneless signal mirror added to the extracted form children cannot be proven by Vitest: the
  test fixture's `detectChanges()` refreshes OnPush children whether or not they are dirty, so the
  staleness it guards against does not reproduce in the harness. Recorded rather than claimed.

## Stage exit — 067-ui-scaling-and-e2e-ui — implement — 2026-09-04T13:10:00Z

- Done: wrote the coverage story 004 owed and closed the implement stage.
  New file `src/PhotoPrint.UI/src/app/features/checkout/components/locker-selector.spec.ts` — 6 specs:
  "renders one entry per locker and marks the selected one", "emits the clicked locker to the container",
  "hands the map the same lockers and selection, and forwards a pin click",
  "offers a retry when the search failed, and says nothing about an empty city",
  "reports an empty city only once one has been typed",
  "shows the ‘pick a locker’ error only when the container asks for it".
  One spec appended to `src/PhotoPrint.UI/src/app/features/checkout/pages/delivery-step.spec.ts`:
  "keeps Continue disabled for a restored delivery method until both server prices arrive" — this is the
  test name for PPW-699 in the stage-3 test report. Wrote
  `memory-bank/bolts/067-ui-scaling-and-e2e-ui/implementation-walkthrough.md` (the bolt type's required stage-2
  artifact, missing until now) and advanced `bolt.md` frontmatter: status in-progress, started
  2026-09-04T00:55:00Z, plan and implement in stages_completed, current_stage test, stage boxes 1–2 ticked.
  Results — locker-selector: passed 6, failed 0. delivery-step: passed 29, failed 0 (28 pre-existing + 1 new).
  Mutation check: with `if (!this.shippingCostsReady()) return false;` removed from `canContinue`, the run was
  passed 28, failed 1 and the only failure was the new gate test; the source file was restored with
  `git checkout --`. `npm run build`: initial total 330.13 kB / 92.82 kB transfer, no errors, no new warnings
  (home is still off the 4 kB stylesheet list).
- Decisions: the continue gate is reached through a *restored* method — `CheckoutStateService.setMethod(...)`
  before the fixture is created — because `selectMethod()` refuses to run before prices arrive, so no other
  path leaves `deliveryMethod()` set with `shippingCostsReady()` false. Courier, not Easybox, so no stray
  locker request trips `HttpTestingController.verify()`. PPW-699 was pulled in only in part, as the plan ruled:
  the continue-gate half is tested; the row is NOT closed and nothing under `reviews/` was touched.
  `status` stopped at in-progress on purpose — the flip to review-pending belongs to the next session.
- Dead ends: none new. Still true from story 004: the zoneless signal mirror in the extracted form children
  cannot be proven by Vitest, because the fixture's `detectChanges()` refreshes OnPush children whether or not
  they are dirty — do not retry it. `delivery-step.spec.ts` is CRLF with no trailing newline; append to it
  with a script that matches, not a shell append.
- Next: stage 3 (test) — write `memory-bank/bolts/067-ui-scaling-and-e2e-ui/test-walkthrough.md` starting from
  the results above, naming PPW-699's tested half by its test name and its per-field `maxlength` half as still
  open; then run the fresh-eyes micro-review as a fresh subagent per `memory-bank/standards/bolt-process.md`,
  flip `bolt.md` to `status: review-pending` with `completed` set, and push.
