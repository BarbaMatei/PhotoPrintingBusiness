---
type: review
target: 066-067-ui-scaling
version: 4
supersedes: null
commit: dc00a63
branch: feat/bolts-066-067-ui-scaling
pass-type: delta-discovery
date: 2026-09-09
lenses: [correctness, security, requirements, tests-coverage, completeness-critic]
lenses-not-run: [quality, frontend-ux, db-parity, input-validation, observability, race]
verdict: request-changes
blockers: [PPW-764]
findings: { high: 1, medium: 14, low: 10, cleanup: 0, refuted: 0 }
tests: { dotnet: "212/212", frontend: "60/60" }
---

# Review v4 — 066-067-ui-scaling

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-764 | 🔴 | E2E stack seeds an admin account whose password is a committed repo constant, usable on a first production deploy | `src/PhotoPrint.UI/e2e/support/stack.ts:7` | yes |
| PPW-810 | 🟠 | DEPLOYMENT.md's permission-denied remedy boots the API as root instead of chowning the uploads volume | `docs/DEPLOYMENT.md:242` | yes |
| PPW-811 | 🟠 | Playwright failure traces carrying the admin login POST body and Bearer token are uploaded as a CI artifact | `src/PhotoPrint.UI/playwright.config.ts:19` | yes |
| PPW-812 | 🟠 | The no-seeded-credential guard scans only e2e/*.ts, so the still-committed seeder pair keeps it green | `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:20` | yes |
| PPW-813 | 🟠 | RUNTIME_UID is asserted by grepping the Dockerfile, never measured against the effective identity or a writable mount | `Dockerfile:36` | yes |
| PPW-814 | 🟠 | The new handshake gate counts a transport request being issued, which may have 401'd, instead of an established hub connection | `src/PhotoPrint.UI/e2e/realtime-order.spec.ts:48` | yes |
| PPW-815 | 🟠 | The new Paid seed order is never backfilled onto an already-seeded database, and no test covers that leg | `src/PhotoPrint.API/Data/Seed/DevDataSeed.cs:30` | yes |
| PPW-816 | 🟠 | BaseApiService's list of direct environment.apiUrl consumers omits both auth-gating interceptors | `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.ts:18` | yes |
| PPW-817 | 🟠 | The e2e workflow re-derives the committed admin credential as a fallback, contradicting the required-no-default contract | `.github/workflows/playwright-e2e.yml:101` | yes |
| PPW-818 | 🟠 | Intent 030 was flipped to complete while its own requirements still assert the three targets this review corrected | `memory-bank/intents/030-ui-scaling-and-e2e/requirements.md:68` | yes |
| PPW-819 | 🟠 | The compose env-precedence guard reads only docker-compose.yml, never the prod overlay where precedence bites | `src/PhotoPrint.Tests/Unit/Configuration/ComposeEnvPrecedenceTests.cs:13` | yes |
| PPW-820 | 🟠 | The credential guard hard-requires the admin email and password to stay string literals in the seeder | `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:75` | yes |
| PPW-821 | 🟠 | The handshake guard pins the poll's shape but neither the signal counter's initial value nor listener ordering | `src/PhotoPrint.Tests/Unit/Configuration/RealtimeE2eHandshakeTests.cs:27` | yes |
| PPW-822 | 🟠 | The new Paid seed order is verified only on EF InMemory, so no PostgreSQL constraint is exercised | `src/PhotoPrint.Tests/Unit/Data/RealtimeE2eSeedTests.cs:41` | yes |
| PPW-823 | 🟠 | The documented E2E_ADMIN_* secrets override makes the admin specs fail, because the seeder reads no configuration | `.github/workflows/playwright-e2e.yml:94` | yes |
| PPW-782 | 🟡 | E2E_* variables documented in .env.example, which no e2e code reads | `.env.example:84` | no |
| PPW-824 | 🟡 | The no-fallback credential guard is a narrow-window regex that a two-line reshape defeats | `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:37` | no |
| PPW-825 | 🟡 | The compose precedence regex silently skips any Stripe line it cannot parse | `src/PhotoPrint.Tests/Unit/Configuration/ComposeEnvPrecedenceTests.cs:14` | no |
| PPW-826 | 🟡 | CI resolves the e2e admin email and password independently, so one rotated secret mixes sources | `.github/workflows/playwright-e2e.yml:101` | no |
| PPW-827 | 🟡 | A repo-file-derived value is written to $GITHUB_ENV with a bare echo (env-injection shape) | `.github/workflows/playwright-e2e.yml:106` | no |
| PPW-828 | 🟡 | Bolt 066's e2e-stability criteria stay ticked on CI evidence that predates the spec rewrites they measure | `memory-bank/bolts/066-ci-quality-gates/bolt.md:74` | no |
| PPW-829 | 🟡 | README's two-Paid-orders promise holds only on a database that was never seeded before | `README.md:113` | no |
| PPW-830 | 🟡 | The new apiUrl pins sit in consumer specs, not in the spec that owns url(), so the coupling stays untested | `src/PhotoPrint.UI/src/app/core/services/api/base-api.service.spec.ts:10` | no |
| PPW-831 | 🟡 | The workflow-order guard compares text offsets in the YAML instead of actual execution order | `src/PhotoPrint.Tests/Unit/Configuration/E2eCredentialsTests.cs:54` | no |
| PPW-832 | 🟡 | Story 001 is complete with all three acceptance boxes unchecked and no partial-delivery note | `memory-bank/intents/030-ui-scaling-and-e2e/units/002-ui-scaling-and-e2e-ui/stories/001-base-api-service.md:22` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| Story 001's three capabilities were never built (would have been 🟠) | The base service is built and used; its own doc comment states the interceptor split, its optional `headers` argument threads an idempotency key, and `withCredentials` is moot under header JWT — only the unrecorded substitution survives, as 🟡 `PPW-832` |
| Intent 030's stale `phase: inception` and `updated:` date are drift | Every `complete` intent in the repo carries both — repo-wide convention, not a defect; only the three false assertions in `PPW-818` stand |
| CI authenticates with a wrong credential today (`PPW-817`) | The workflow's fallback resolves the same pair CI's own throwaway database is seeded from, so no run fails; the defect is the contract three files assert and this step breaks — hence `plausible`, not `confirmed` |
| A seeded order number could collide with a real one (`PPW-815`) | `OrderNumberService.FormatOrderNumber` emits `FT-20260001` with no inner dash — a different shape from the seed's `FT-2026-0001` |
| A root entrypoint that chowns `/app/Storage` fixes the volume-permission failure (`PPW-813`) | Approach pre-check refuted it: the app writes to `Storage:BasePath` (`/var/app/uploads` in Production), not the mount path — the chown would target a directory nothing uses |

## Notes for the fixer

- `PPW-764` is the only blocker and the only 🔴: its round 1 fix held on its own lever, and this pass reopened the row because the seeder half was deliberately left to intent 033. Nothing regressed — read the ledger History before touching `e2e/support/stack.ts` again.
- Nine rows (`PPW-812`, `PPW-817`, `PPW-820`, `PPW-823`, `PPW-824`, `PPW-826`, `PPW-827`, `PPW-831`, and `PPW-764` itself) are one cluster: the seeded admin credential and how CI resolves it. Fix `PPW-812`'s configuration read first — `PPW-820` fails on the corrected code until its helper is relaxed, and `PPW-817`/`PPW-823` disappear or change shape once the seeder honours the same variable names.
- `PPW-815`, `PPW-822` and `PPW-829` are one cluster on the dev seeder. `PPW-815`'s pre-check found two traps: the uploads `AddRange` is unguarded (a naive per-id backfill throws on duplicate keys), and the live failure on a persistent database is rows-exist-but-consumed, which insert-only logic never repairs.
- `PPW-813` and `PPW-810` are one cluster on container storage. Do not implement `PPW-813`'s original suggestion; the revised approach on its ledger row (align the mount with `Storage:BasePath`, keep the pinned uid plus the documented one-off chown, add a fail-fast write probe and fix the disk health check's path) replaces it.
- `PPW-814` and `PPW-821` touch the same realtime spec and its guard. Fix the spec's gate first, then pin the counter's initializer and listener order, or the guard will lock in whatever shape the fix leaves.
- Three rows are guards that redden on the real fix or stay green on the real defect (`PPW-812`, `PPW-819`, `PPW-820`, `PPW-821`, `PPW-825`, `PPW-831`). Treat a green run of `Unit.Configuration` as no evidence until each is repaired.
- Four manifest lenses have still never run on this target (db-parity, input-validation, observability, race), and a delta pass cannot clear that debt — it needs full scope.
- Playwright cannot run on this machine; CI on PR #21 is the only evidence for anything in `e2e/`.
