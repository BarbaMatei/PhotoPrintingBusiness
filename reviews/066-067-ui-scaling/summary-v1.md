---
type: owner-summary
target: 066-067-ui-scaling
pass: 1
pass-type: discovery
commit: 20c5c96
date: 2026-09-08
decisions-needed: 13
---

# Owner summary — 066-067-ui-scaling v1

Seven blinded lenses searched this branch's frontend and CI diff (PR #21) plus both bolts'
artifacts, and named 48 problems: 3 🔴, 10 🟠, 35 minor. The verdict is `request-changes`
([review-v1.md](review-v1.md)). Two 🔴 are in the new Playwright and Docker work; the third is
a password that has been in the repository all along and that this bolt's e2e helper republishes.

## Needs your decision

1. 🔴 [PPW-762](ledger.md) — after a rebuild the container can run as a different user id than
   the one owning the existing photo-storage volume, so every upload write fails while
   `/health` still answers 200 (`Dockerfile:36`). Fix now, ~30 min; needs a container check.
2. 🔴 [PPW-763](ledger.md) — the only real-time end-to-end test waits for a network request a
   WebSocket connection never produces, so it times out after 30 s on every CI run
   (`e2e/realtime-order.spec.ts:55`). Fix now, ~30 min; real proof comes only from CI.
3. 🔴 [PPW-764](ledger.md) — the seeded administrator's password is a constant in the repository
   (`ProductCatalogSeed.cs:32-33`), `docs/DEPLOYMENT.md:200` runs that seed on a first deploy,
   and `e2e/support/stack.ts:6-7` republishes the pair. Fix the e2e half now (~20 min) and route
   the seeder half to intent 033 unit 002, which already owns per-tier seed policy — the
   pre-check found doing it here collides with planned work. **Decide this one first.**
4. 🟠 [PPW-765](ledger.md) — six services still build their own URLs, so "one place owns URL
   composition" is not yet true although the bolt criterion is ticked. Fix now, ~1 h.
5. 🟠 [PPW-766](ledger.md) — the CI retry re-runs the real-time test after it consumed the only
   test order it can use, so the retry can never pass. Fix now, ~1 h, per the pre-check's
   revision (pin the order, keep the retry).
6. 🟠 [PPW-767](ledger.md) — guest checkout is tested only to the review step; payment and
   confirmation are untested while the story ticks "3 e2e pass". Record the gap now (~15 min);
   extending the spec is a bolt of its own.
7. 🟠 [PPW-768](ledger.md), [PPW-769](ledger.md), [PPW-770](ledger.md) — three test gaps the
   refactor left: the extracted locker selector's two outputs, all 11 migrated product-admin
   endpoints, the admin ZIP download. Each proven by breaking the code and watching the suites
   stay green. Fix now, ~2 h for all three, test-only.
8. 🟠 [PPW-771](ledger.md) — Playwright tests the development bundle, never the production one
   that ships. Defer to a follow-up bolt; a new CI job, ~half a day.
9. 🟠 [PPW-772](ledger.md) — a guest whose session expires during checkout is sent to the login
   page instead of being helped. The suggested self-heal was **refuted** by the pre-check (a new
   session loses their cart and photos); the safe fix is the redirect alone, in a file outside
   this branch's diff. Your scope call.
10. 🟠 [PPW-773](ledger.md), [PPW-774](ledger.md) — the README calls the smoke tests "real-money
    paths", overstating them, and `docker-compose.yml:51` overrides a developer's real Stripe
    key with a placeholder. Fix now, ~20 min for both.

## Reasons to doubt

- Four manifest lenses never ran — `db-parity`, `input-validation`, `observability`, `race` —
  judged to have no surface in a frontend and CI diff. That is coverage debt a certification
  would be refused over ([metrics.jsonl](metrics.jsonl)).
- 13 findings carry `unverified-cleanup` (no skeptic ever challenges ⚪, so they rest on one
  lens's word) and 2 were `hinted`, so their convergence is not independent evidence ([metrics.jsonl](metrics.jsonl)).
- No trend yet: v1 is the first pass here, so the new-findings curve has one point (3 + 10) ([metrics.jsonl](metrics.jsonl)).
- The Playwright suite cannot run on this machine (no Docker), so every e2e claim rests on
  reading plus the CI runs in the bolts' test walkthroughs — and PPW-763 and PPW-766 are exactly
  the kind of defect only a real CI run confirms.
- Blinding is best-effort: prompts bar the lenses from `reviews/` and git history, nothing verifies it ([review-v1.md](review-v1.md)).

## Filed automatically

35 minor rows (22 🟡, 13 ⚪) went to the ledger without needing a decision
([ledger.md](ledger.md)). One deserves a glance: [PPW-809](ledger.md) — two thirds of the
6,158-line frontend diff is Prettier reformatting interleaved with behaviour changes, with no
`format:check` script or CI step holding it.

## State

All 48 rows are `open`. The router's next step is fix round 1 on the three 🔴 and the in-scope
🟠, then a verification of that round. Certification is out of scope by your instruction, and
the loop stays open for you to close after your own review of PR #21.
