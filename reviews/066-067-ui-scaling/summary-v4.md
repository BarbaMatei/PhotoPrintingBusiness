---
type: owner-summary
target: 066-067-ui-scaling
pass: 4
pass-type: delta-discovery
commit: dc00a63
date: 2026-09-09
decisions-needed: 7
---

# Owner summary — 066-067-ui-scaling v4

A delta pass re-reviewed only the code the two fix rounds changed (5 lenses, the delta cap; [review-v4](review-v4.md)). It found 23 new rows — 14 🟠 and 9 🟡 — and 20 of them were introduced by the fixes themselves: 19 by round 1 and 1 by round 2 ([metrics](metrics.jsonl)). One earlier row, [PPW-764](ledger.md), goes back to `open`: no fix failed, but the half of it that was deliberately left to intent 033 still ships a working admin password in the repo. Verdict: request-changes.

## Needs your decision

1. **The committed admin credential is still live.** `--seed` on a fresh production database creates an Admin account whose email and password are string literals at `src/PhotoPrint.API/Data/Seed/ProductCatalogSeed.cs:32-33`, documented at `docs/DEPLOYMENT.md:200`. Round 1 fixed the e2e half only, by an explicit decision to leave the seeder to intent 033 ([PPW-764](ledger.md)). Suggested: read that pair from configuration and refuse to seed without it, ~2h with a new test — or say plainly that intent 033 owns it and accept the exposure until then.
2. **The guard meant to prevent exactly that passes on the defect.** It scans only `e2e/*.ts`, so the seeder's literals never reach it ([PPW-812](ledger.md), `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:20`); three sibling rows change shape once item 1 is fixed ([PPW-817](ledger.md), [PPW-820](ledger.md), [PPW-823](ledger.md)). Suggested: fix as one cluster in the same round, ~2–3h.
3. **CI keeps the admin token in a build artifact.** Playwright failure traces carry the login request body and the Bearer token and are uploaded on failure ([PPW-811](ledger.md), `src/PhotoPrint.UI/playwright.config.ts:19`). Suggested: fix now, ~30min — or accept it once item 1 removes the shared password.
4. **The dev seeder's new Paid order never reaches an already-seeded database**, and its approach pre-check found that a naive insert-only backfill throws on the unguarded uploads insert ([PPW-815](ledger.md), `src/PhotoPrint.API/Data/Seed/DevDataSeed.cs:30`); its only test runs on the in-memory provider, so no PostgreSQL constraint is exercised ([PPW-822](ledger.md)). Suggested: a reconciling backfill in one transaction plus a Postgres test, ~2h.
5. **The documented remedy for the uploads-volume permission error boots the API as root** ([PPW-810](ledger.md), `docs/DEPLOYMENT.md:242`), and the uid assertion behind it only greps the Dockerfile ([PPW-813](ledger.md)). The pre-check refuted the original suggestion; the revised approach sits on PPW-813's ledger row. Suggested: fix the pair together, ~2h, and note that measuring the real uid needs Docker in CI.
6. **The realtime e2e gate counts a request that may have failed authentication** instead of an established connection ([PPW-814](ledger.md), `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:48`), and its guard pins the poll's shape rather than the counter and listener order ([PPW-821](ledger.md)). Suggested: fix the spec first, then the guard, ~1h.
7. **Three records now assert things this review corrected**: intent 030's requirements ([PPW-818](ledger.md)), the compose precedence guard reading the wrong file ([PPW-819](ledger.md)), and `BaseApiService`'s doc list omitting both auth interceptors ([PPW-816](ledger.md)). Suggested: fold into the same round, ~1h.

## Reasons to doubt

- Six manifest lenses were owed and did not run; four of them (db-parity, input-validation, observability, race) have never run on this target at all, and a delta pass cannot clear that debt — it needs full scope ([review-v4](review-v4.md)).
- Serious findings per pass are not falling: v1 found 3 🔴 + 10 🟠, v4 found 0 🔴 + 14 🟠 ([metrics](metrics.jsonl)).
- Nine rows carry an `unverified-low` verdict — 🟡 rows skip the adversarial re-check by design — and one finder was `hinted`, meaning it saw a prior finding ([metrics](metrics.jsonl)).
- No agent was skipped for budget: about 222k of the 600k output-token budget was used ([metrics](metrics.jsonl)).
- A delta pass cannot certify. Playwright cannot run on this machine, so CI on [PR #21](https://github.com/BarbaMatei/PhotoPrintingBusiness/pull/21) is the only evidence for anything under `e2e/`.

## Filed automatically

Nine new 🟡 rows went straight to the ledger backlog, PPW-824 to PPW-832 ([ledger](ledger.md)). One deserves your eye anyway: [PPW-827](ledger.md) writes a repo-derived value into `$GITHUB_ENV` with a bare `echo`, the shape an env-injection uses.

## State

The router stands at a fix round for the 15 open rows (1 🔴, 14 🟠); PPW-771 and PPW-772 stay deferred on your parked scope decisions ([ledger](ledger.md)). This run was scoped to the delta pass alone, so nothing was fixed and the loop stops here until you say otherwise.
