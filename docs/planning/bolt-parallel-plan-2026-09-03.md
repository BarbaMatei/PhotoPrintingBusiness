# Bolt Parallel Implementation Plan — 2026-09-03

> Author: bolt-parallel-planner. Repo: `D:/photo printing website` — .NET 8 API + Angular 21 SPA +
> PostgreSQL/EF Core, plus the agentic tooling family (intent 035) that lives in `reviews/` and
> `.claude/skills/`.
> **Supersedes** `docs/planning/bolt-parallel-plan-2026-06-05.md` (which covered only bolts 047–069).
> Branch state at planning time: `main` at `ec12d93`, everything merged, working tree clean.
> Scope: **every** planned bolt left in `memory-bank/bolts/` — both families, planned side by side.
> Deployment is out of scope of this plan entirely.

---

## 0. Pre-flight (short, unlike June)

`main` at `ec12d93` already contains every shipped bolt (035–045, 049, 051–053) and every inception
artifact for 047–094. No fast-forward, no stale-main problem, no in-flight feature branch. The four
stale branches (`feat/bolt-038-vat-calculation` local + remote,
`origin/feat/bolt-035-payment-idempotency`, `origin/feat/bolt-040-containers-and-pipelines`,
`origin/feat/bolt-041-secrets-management`) are all contained in `main` — cleanup only, not work:

```powershell
git -C "D:\photo printing website" branch -d feat/bolt-038-vat-calculation
git -C "D:\photo printing website" push origin --delete feat/bolt-035-payment-idempotency feat/bolt-040-containers-and-pipelines feat/bolt-041-secrets-management feat/bolt-038-vat-calculation
```

One thing **must** happen before Wave 1 and it is not a code change: the `story-index.md` drift in
§1 gets its own small PR, so instances read a truthful index.

---

## 1. Inventory & drift findings

Candidate = `status: planned` in `bolt.md` frontmatter, cross-checked against `git log --all -i --grep`,
`git branch -a`, and the code in-tree. **44 planned bolts.** No bolt in this list has a feat commit or
a branch — all of it is fresh work.

| Bolt | Intent | Frontmatter | Git evidence | Verdict |
|---|---|---|---|---|
| 046-distributed-state-redis | 021 | `planned` | only `7f3a44e chore(planning): deprioritize bolt 046` | **PARKED — not scheduled** (§2) |
| 047-coupon-domain-and-api | 022 | `planned` | none | PLAN — fresh (EF migration) |
| 048-coupon-frontend | 022 | `planned` | none | PLAN — fresh |
| 054–058 (hardening, boot manifest, docs) | 025, 026 | `planned` | none | PLAN — fresh |
| 059–062 (layering + test infra) | 027, 028 | `planned` | none | PLAN — fresh (**structural refactor**) |
| 063–065 (access, decomposition, persistence) | 029 | `planned` | none | PLAN — fresh |
| 066–067 (CI gates, UI scaling) | 030 | `planned` | none | PLAN — fresh |
| 068–069 (refunds) | 031 | `planned` | none | PLAN — fresh (EF migration on 068) |
| 070–072 (e2e data, journeys, regression) | 032 | `planned` | none | PLAN — fresh |
| 073–075 (environment triad) | 033 | `planned` | none | PLAN — fresh (readiness only, no deploy) |
| 076–082 (EU research spikes x7) | 034 | `planned` | none | PLAN — fresh (docs-only spikes) |
| 083–084 (EU synthesis + briefs) | 034 | `planned` | none | PLAN — fresh (docs-only, owner decision inside 083) |
| 085–086 (Phase 1 verification) | 035 | `planned` | none | PLAN — **verification bolts, run first** |
| 087–090, 092–093 (bug-hunter construction) | 035 | `planned` | none | PLAN — fresh (tooling, `reviews/lib`) |
| 091-phase-3-oracle-grounding | 035 | `planned`, `blocks: true` | none | **PLACEHOLDER — external gate closed** (§2) |
| 094-optional-integration | 035 | `planned` | none | **NOT SCHEDULED — adoption gate** (§2) |

### Drift findings (reported, not silently resolved)

1. **`story-index.md` still shows shipped work as `NOT STARTED`.** Lines ~796–828 (intent 016 →
   bolts 038/039) and ~962–988 (intent 020 → bolts 044/045) list every story as not started, and the
   roll-up at line 562 says `NOT STARTED: 20 (intents 016, 020, 021, 022 — 7 planned bolts)`.
   All four bolts shipped: commits `0c6938c feat(invoicing): … (bolt 038) … (bolt 039)` and
   `c09675d feat(observability): … (bolt 044) and … (bolt 045)`, and the code is in-tree
   (`src/PhotoPrint.API/Controllers/InvoicesController.cs`, `AdminInvoicesController.cs`,
   `src/PhotoPrint.API/Services/VatCalculator.cs`). The index *overview* already says 016/020 shipped —
   only the per-story lines and the roll-up are stale, a partial-edit drift. **The June plan flagged
   exactly this and it was never applied.**
   **Recommended fix (one small PR before Wave 1, not by any instance):** flip those story lines to
   IMPLEMENTED and correct the line-562 roll-up.
2. **085/086 gate 087 in practice, but the graph says they gate nothing.** Both carry
   `enables_bolts: []` and the note *"edges left empty on purpose because nothing gates on them"*,
   yet bolt 085 stage 3 mandates: each gap found *"becomes a new story file … with
   `assigned_bolt: 087-phase-2-trust`, and its id is appended to bolt 087's `stories:` list"*.
   That is a write into `memory-bank/bolts/087-phase-2-trust/bolt.md`. **Scheduling consequence:**
   085/086 must land before 087 starts (this plan puts them in Wave 1 and 087 in Wave 2). Recommend
   amending 085/086 `notes:` to say so; do not silently add an `enables_bolts` edge.
3. **`enables_bolts` is not the inverse of `requires_bolts`.** 062 (`enables_bolts: []`) is required by
   070; 066 enables only 067 but is required by 070 and 071; 059 enables 060/061 but is required by
   064, 065, 068; 063 enables nothing but is required by 068. **This plan scheduled from
   `requires_bolts` only** — the authoritative direction, verified bolt by bolt. The reverse edges are
   documentation debt, not a blocker.
4. **089 and 090 are not as disjoint as their own wave notes claim.** 089 says *"runs in parallel with
   bolt 090 — all-new disjoint skill directories … no shared files are touched"*; but both name
   `reviews/lib/records/schema.mjs` as *"the manifest's one machine home"*, 090 adds two lens rows to
   it, and 089 says *"where a lens is added, its row in … schema.mjs"*. Rated **MED**, not LOW.
   Handled by running them serially on one branch at 3 instances (§3, group X-3).
5. **062's lockstep constraint is invisible to the graph.** `requires_bolts: []`, but its Notes say
   *"Lockstep / interleaved with intent 027 (bolts 059–061)"* and 059's Notes say *"Lockstep with bolt
   062. Schedule a quiet window."* Kept inside the structural group (P-F), as June did.
6. **094's order and its dependency disagree (harmless).** Frontmatter `requires_bolts: [092]` allows it
   any time after 092; integration contract §7 lists it after 091 (step 11). Both are satisfiable
   because 094 is adoption-gated and unscheduled — named here so nobody "fixes" one to match the other.
7. **Bolt 050 does not exist** — unallocated by design, stated in `story-index.md`. No action.

---

## 2. Exclusions

| Bolt | Reason | Action |
|---|---|---|
| **046-distributed-state-redis** (intent 021) | **Parked by owner ruling** — the Redis multi-server backplane is scaling work; the app is not deployed and there is no real scaling pressure. Recorded in commit `7f3a44e`, `story-index.md` line 8, and the owner's standing decision. | **Not scheduled.** Revisit only on real scaling pressure. |
| **091-phase-3-oracle-grounding** (intent 035) | **External gate closed.** It needs the knowledge-builder's `ledger-query` interface (contract §2, requirements D6). The knowledge builder has **no inception and no bolt numbers** — contract §7 still names its steps by phase, "until its own inception assigns bolt numbers". `blocks: true`. | **Placeholder wave W12 only** — "when the gate opens". Nothing else waits on it (contract §7 step 10). |
| **094-optional-integration** (intent 035) | **Adoption-gated.** Build only if the owner adopts CI code-scanning and/or an issue tracker for findings. All three stories are Could. | **Not scheduled.** One owner sentence unlocks it; it may then run any wave after 092 (W5). |
| **050** | Never allocated; no directory. | None. |

**41 bolts are scheduled** across 11 working waves; 3 are excluded as above.

---

## 3. Groups

23 groups. Naming follows the repo convention (`feat/bolt-<NNN>-<slug>` single,
`feat/bolts-<NNN>-<NNN>-<theme>` multi), verified against `git branch -a`.

**MIGRATION = the group adds a real EF migration** (touching
`src/PhotoPrint.API/Migrations/PhotoPrintDbContextModelSnapshot.cs`). Only **P-B (047)** and
**P-I (068)** do, and they are four waves apart.

### Product family

| Group | Branch | Bolts (serial) | Theme and footprint | Migration | Size |
|---|---|---|---|---|---|
| **P-A** | `feat/bolt-054-dependency-hardening` | 054 | OTel CVE bump, Central Package Management (`Directory.Packages.props` + **every** `.csproj`), `renovate.json`, ForwardedHeaders in `Program.cs` (append-only), `DEPLOYMENT.md` | — | S (~1d) |
| **P-B** | `feat/bolts-047-048-coupons` | 047 → 048 | Coupons: new `Coupons`/`CouponRedemptions` entities, cart + admin endpoints, redemption on order create, `Program.cs` DI append; Angular cart/summary/review + invoice PDF line | **YES** | M (~2–3d) |
| **P-C** | `feat/bolts-066-067-ui-scaling` | 066 → 067 | `angular.json` budgets, Playwright module + `.github/workflows/playwright-e2e.yml`; then `BaseApiService`, home/account/delivery-step component breakups | — | M (~2–3d) |
| **P-D** | `feat/bolt-057-architecture-docs` | 057 | Docs-only: `docs/architecture/*`, `tech-stack.md`, `KNOWN_FAILURES.md`, audit checklist | — | XS (~0.5d) |
| **P-E** | `feat/bolts-055-056-058-boot-manifest` | 055 → 056 → 058 | **Rewrites `Program.cs`** into `Add*` extensions + typed `IFeatureGate`; `/api/admin/system-info`, job liveness, ANAF invoice metrics/SLO; Angular admin "System" tab | — | M (~2–3d) |
| **P-F** | `feat/bolts-059-062-layering-and-tests` | 059 → 060 → 061 → 062 | **STRUCTURAL REFACTOR**: Domain/Infrastructure/Web/Application folders + namespaces (~200 files), `Abstractions/`, handler pattern, and the lockstep test-project reshape (TimeProvider, shared factory, Builders, reclassification) | empty verify only | L (~4–5d) |
| **P-G** | `feat/bolt-063-access-hardening` | 063 | Global per-IP rate limiter in the security extension, `Policies.Admin` constant + 6 controllers' `[Authorize]` | — (attribute swap only) | S (~1d) |
| **P-H** | `feat/bolts-064-065-decomposition` | 064 → 065 | AuthService split into 3, thin `WebhooksController`, `OrderPhotoQueryService`; 17 `IEntityTypeConfiguration<T>` files | empty verify only | M (~2d) |
| **P-I** | `feat/bolt-068-refund-domain` | 068 | DDD bolt: refund schema + `Refunded` state, refund service (Stripe + EuPlatesc), ANAF credit note (UBL 381), admin refund endpoint | **YES** | M (~2–3d) |
| **P-J** | `feat/bolt-069-refund-ui` | 069 | Admin order-detail refund action + modal, Romanian error copy | — | S (~1d) |
| **P-K** | `feat/bolts-070-071-e2e-data-and-journeys` | 070 → 071 | `e2e/fixtures/` data contract + Builder-backed fixtures + payment test-mode fixtures + real-Postgres compose boot; then the full journey suite + CI tiering | — | L (~4–5d) |
| **P-L** | `feat/bolt-072-regression-methodology` | 072 | Regression checklist per shipped intent, **one executed baseline pass**, triage into backlog / `KNOWN_FAILURES.md` | — | M (~2d, **machine-exclusive**) |
| **P-M** | `feat/bolts-073-075-environment-triad` | 073 → 074 → 075 | `appsettings.{dev-env}.json`, `docker-compose.dev-env.yml`, config map, `ValidateOnStart` parity; secrets matrix + `.env.dev-env.example` + seeding policy + prod demo-data guard; promotion runbook + deferral note | — | M (~2–3d) |
| **P-N** | `feat/bolts-076-080-eu-research-market` | 076 → 080 | Spikes: fulfillment/logistics (8h) + tax/invoicing/compliance (8h) → `docs/analysis/eu-expansion/track-1-*.md`, `track-5-*.md` | — | M (16h box) |
| **P-O** | `feat/bolts-077-078-eu-research-experience` | 077 → 078 | Spikes: site/URL architecture (6h) + Angular 21 i18n (6h) → `track-2-*.md`, `track-3-*.md` | — | M (12h box) |
| **P-P** | `feat/bolts-079-081-082-eu-research-platform` | 079 → 081 → 082 | Spikes: backend localization (4h) + payments (4h) + **repo-bound seam audit, read-only** (6h) → `track-4/6/7-*.md` | — | M (14h box) |
| **P-Q** | `feat/bolts-083-084-eu-synthesis` | 083 → 084 | Options paper → owner decision → ADR in `memory-bank/standards/decision-index.md` → implementation brief | — | M (10h box) |

### Agentic family (intent 035 — never touches application source)

| Group | Branch | Bolts (serial) | Theme and footprint | Size |
|---|---|---|---|---|
| **X-0** | `feat/bolts-085-086-review-loop-verification` | 085 → 086 | **Verification bolts.** Read the seams, record a per-story verdict; write `implementation-plan.md` + `test-walkthrough.md` in each bolt folder, flip satisfied story files, **create gap stories and append their ids to 087's `stories:` list**. Builds nothing. | XS (4h box) |
| **X-1** | `feat/bolt-087-trust-upgrades` | 087 | Risk score in the scoring code, `tool-ingest`, execution proof in the Verify slot, `git-revision-tracking` moved/fixed detection. Seams under `reviews/lib/**`, tests in `reviews/lib/tests`. **Engine change.** | M (6h box) |
| **X-2** | `feat/bolt-088-map-and-reachability` | 088 | The missing Map slot: `app-mapping`, shared `code-index`, framework-aware `reachability`, the scoring extension (**same scoring code 087 touched**), and the budget/incremental half of 24d; wired into `reviews/lib/discovery-review.wf.js`. **Engine change.** | M (5h box) |
| **X-3** | `feat/bolts-089-090-specialists` | 089 → 090 | `taint-analysis` on the security lens; `dependency-audit` + `config-auditor` lens rows in `reviews/lib/records/schema.mjs` (+ their prompts; runbook tables regenerate — never hand-edit); `root-cause-clustering` in `reviews/lib/records/ledger.mjs`. **Engine change.** *(Split into two branches only at 4+ instances — see §4.)* | M (8h box) |
| **X-4** | `feat/bolt-092-learn-and-measure` | 092 | Standing eval corpus + poison fixture, recall/escape metrics in `reviews/lib/measure/`, curator automation + speed report, the Learn step in the pass router. **Engine change.** | M (5h box) |
| **X-5** | `feat/bolt-093-remediation-handoff` | 093 | Non-fixer `regression-harvest` at `reviews/lib/fix/handback-gates.mjs`; idempotent `fix-request-emit` store keyed by `correlation_id`. **Engine change.** | M (4h box) |
| **X-6** | `feat/bolt-091-oracle-grounding` | 091 | **Placeholder only** — `intent-lookup` over the knowledge-builder's `ledger-query`, plus the three contract extensions. Do not cut this worktree until the gate opens. | M (4h box) |

---

## 4. Conflict matrix

Rated only for groups that could co-occur. **HIGH** = same files / file moves / two real EF migrations.
**MED** = both append to a hot shared file (mechanical, append-only). **LOW** = disjoint.

| Pair | Rating | Reason |
|---|---|---|
| P-A x P-B | **MED** | Both append to `Program.cs`; P-A rewrites every `.csproj` for CPM — if P-B adds a package it goes in `Directory.Packages.props` after P-A merges. Only P-B has a migration. |
| P-A x P-C | **LOW** | Backend csproj/Program vs Angular + `playwright-e2e.yml`. Both touch `.github/workflows/` but different files. |
| P-A x X-0, P-A x P-D | **LOW** | Config/backend vs docs and verification records. |
| P-B x P-C | **LOW** | P-B's Angular work is cart/checkout/invoice-PDF; P-C's is home/account/delivery-step + CI. |
| P-B x P-D, P-B x X-0 | **LOW** | Backend + FE vs docs. |
| P-C x P-D, P-C x X-0 | **LOW** | Frontend/CI vs docs. |
| **P-A x P-E** | **HIGH** | Both edit `Program.cs`, and P-E *rewrites* it into `Add*` extensions while P-A rewrites every `.csproj`. **Never co-schedule** (P-A W1, P-E W2). |
| **P-E x P-G** | **HIGH** | P-G registers the global limiter in the security extension P-E is creating. **Never co-schedule** (P-E W2, P-G W4). |
| P-E x X-1 | **LOW** | `src/**` vs `reviews/lib/**`. Engine-merge rule §4a still applies. |
| **P-F x any product group** | **HIGH** | ~200 files move across Domain/Infrastructure/Web/Application namespaces and the test project reshapes. Every concurrent product branch would rebase onto renamed paths. **Exclusive on `src/**`.** |
| **P-F x X-2** | **LOW** | The one legitimate exception: intent 035 is **read-only on application source** by mandate, so an agentic branch never sees the renames. Only §4a governs. |
| P-G x P-H | **MED** | Both post-refactor backend: P-G swaps `[Authorize]` on 6 controllers, P-H splits `AuthService` and adds `Configurations/`. Keep P-G's attribute swaps isolated; P-H must not re-touch `[Authorize]` lines. |
| P-G x X-3, P-H x X-3 | **LOW** | `src/**` vs `reviews/lib/**`. |
| **P-H x P-I** | **MED to HIGH** | P-H's 065 regenerates an *empty* verification migration that touches the snapshot; P-I adds a *real* one. **Different waves** (W4 / W5); if they ever slip together, **P-I rebases and regenerates last**. |
| P-I x X-4, P-J x X-5 | **LOW** | Backend / Angular vs `reviews/lib/**`. |
| **089 x 090 (inside X-3)** | **MED** | Both add lens rows to `reviews/lib/records/schema.mjs` — contradicting their own wave notes (§1 drift 4). Serial on one branch at 3 instances removes it entirely. |
| P-N x P-O x P-P | **LOW** | Seven docs-only spikes, one output file each under `docs/analysis/eu-expansion/`. |
| P-Q x anything | **LOW** | Docs + one `decision-index.md` ADR row; it runs alone anyway. |

### 4a. The two cross-family rules (the ones that actually bite)

**Rule 1 — engine-vs-gate.** Every agentic construction bolt (087, 088, 089/090, 092, 093, and 091
later) edits `reviews/lib/**` and `.claude/skills/**` — *the machinery that runs stage 6 of
`memory-bank/standards/bolt-process.md` for every product bolt*. A product bolt's review loop is a
sequence of passes whose records, router decisions and lens manifest all describe the engine they ran
on. Therefore:

- **No product review loop may be open across an engine merge.** An engine PR merges only at a wave
  boundary where `node reviews/lib/route-next-pass.mjs` reports every product target quiet, closed or
  parked. If a loop is still live, either hold the engine PR one wave, or take an owner ruling to
  record the engine bump on that target's ledger (that is precisely what 087's
  `git-revision-tracking` story exists to make legible).
- **At most one engine-changing group per wave**, and it merges **last** in its wave.
- Product instances never run a review pass out of their worktree, so an in-flight engine branch
  cannot leak into a product review: the branch is unmerged and stage 6 runs from `main`.

**Rule 2 — the shared review-state files are a single global sequence.**
`reviews/state/id-counter` holds *one number* (currently `711`) that is the next free `PPW-<n>`;
`reviews/lib/review/mint-id.mjs` reads it, bumps it, and writes the ids straight into a target's
ledger. Two worktrees running review passes at the same time both read `711` and **mint duplicate PPW
ids** — git conflicts on the counter file, but the duplicated ids are already inside two ledgers by
then. The same applies to `reviews/state/index.md` (append-only pass log), `reviews/state/backlog.md`
and `reviews/state/track-record.md`. Therefore:

- **Stage 6 runs centrally, one target at a time, outside the worktrees** — from the shared checkout
  on `main` (or one dedicated review worktree), driven by the owner between waves. This is also forced
  by the machine: `CLAUDE.md` says a full test run saturates it, and the review loop runs
  full-manifest passes.
- **Instances never touch `reviews/state/**`, never mint a PPW id, never run `loop-driver`.** They
  stop at stage 5, set `status: review-pending` (not `complete` — `bolt-process.md` allows `complete`
  only after stage 6's first discovery pass), open the PR, and hand over.
- Exception, and it is a useful one: the **agentic bolts' own** stage-6 target is the review system
  itself (`reviews/system/`), whose ids are `SF<n>`, *outside* the `PPW-<n>` sequence. An agentic
  review and a product review therefore never collide on ids — they still collide on the machine, so
  still one at a time. **Owner call:** route agentic bolts to the existing `reviews/system/` meta
  target, or give each its own `reviews/<bolt>/` target (§9).

`memory-bank/story-index.md` and the other `memory-bank` index files remain guaranteed-conflict files:
**no instance edits them**; the index is updated once per wave at integration time.

### 4b. Review-loop capacity per wave

Entry tiers (`reviews/README.md`) decide the depth, and the machine runs **one target at a time**:

| Tier | Which groups | Cost |
|---|---|---|
| **Full loop, ends at certification** (money, auth, migrations, new external input) | P-B (047/048), P-G (063), P-H (064 auth split), P-I (068) | Discovery + fix rounds + verification + a **certification pair** on first attempt (~2x a full pass) |
| **Ordinary** (one discovery + fixes + verification) | P-A, P-C, P-E, P-F, P-J, P-K, X-1…X-5 | 1 discovery + rounds |
| **Quick pass or skip** (docs, config, no behaviour change) | P-D, P-L (its own baseline is the evidence), P-M, P-N/P-O/P-P, P-Q, X-0 | 1 quick pass or skipped by tier |

Discovery passes implied per wave: **W1 ~3** (one of them full-loop), **W2 2**, **W3 2**, **W4 3**
(two full-loop), **W5 2** (one full-loop), **W6 2**, **W7 1**, **W8 0–1**, **W9 1**, **W10 0–1**,
**W11 1**. Waves 2, 3, 5 and 6 are narrow on purpose so the queue drains.

---

## 5. Wave schedule

Dependency edges used (from `requires_bolts`, verified file by file):
`047→048` · `054→063` · `055→056→058` · `059→{060,065}` · `060→061` · `{059,061}→064` ·
`{059,063}→068→069` · `066→067` · `{066,062}→070→071→072` · `073→074→075` ·
`{076…082}→083→084` · `087→088→{089,090}→092→093` · `{089,090}→091 (gated)` · `092→094 (gated)` ·
plus the practical edge `{085,086}→087` from drift finding 2.

**Wave width is set by three things, in this order:** the dependency graph, the conflict matrix, and
**review-loop capacity** — one target reviewed at a time on this machine. A wave that lands five
targets does not become five parallel loops; it becomes a queue. Waves 2, 3, 5 and 6 are deliberately
narrow so the loop catches up, not because the work could not be split.

| Wave | Groups | Bolts | Optimal instances | At 3 instances |
|---|---|---|---|---|
| W1 | P-A, P-B, P-C, X-0 + P-D | 054 · 047, 048 · 066, 067 · 085, 086 · 057 | 4 | X-0 + P-D ride the first instance to free up (P-A finishes in ~1d) |
| W2 | P-E, X-1 | 055, 056, 058 · 087 | 2 | 2 — third instance is review headroom |
| W3 | P-F (exclusive on `src`), X-2 | 059, 060, 061, 062 · 088 | 2 | 2 |
| W4 | P-G, P-H, X-3 | 063 · 064, 065 · 089, 090 | 3 (4 if 089/090 split) | 3 — keep 089→090 serial on one branch |
| W5 | P-I, X-4 | 068 · 092 | 2 | 2 |
| W6 | P-J, X-5 | 069 · 093 | 2 | 2 |
| W7 | P-K | 070, 071 | 1 | 1 |
| W8 | P-L | 072 | 1 (**machine-exclusive**) | 1 |
| W9 | P-M | 073, 074, 075 | 1 | 1 |
| W10 | P-N, P-O, P-P | 076, 080 · 077, 078 · 079, 081, 082 | 3 | 3 |
| W11 | P-Q | 083, 084 | 1 | 1 |
| W12 | X-6 | 091 | placeholder — gate closed | — |

### Wave-boundary justification

- **W1** — everything here is dependency-free and LOW/MED-disjoint. P-B is the only migration, and it
  is the longest-lead full-loop-tier target, so it starts first. X-0 is a 4-hour verification that
  *must* precede 087 (drift 2) and writes only records and story files. **At 2 instances:** run P-A
  then P-B on one, P-C on the other; X-0 + P-D slip to W2.
- **W2** — P-E is the `Program.cs` rewriter; it is isolated from both other `Program.cs` editors
  (P-A in W1, P-G in W4). X-1 is the first engine change and needs 085/086's gap stories from W1.
  No third group exists that is both unblocked and safe — the macro order holds 073–075 and 076–084
  back, and W1's review queue (one full-loop target plus two ordinary) is the real constraint.
- **W3** — the structural refactor gets its exclusive window on `src/**` (its own Notes: *"Schedule a
  quiet window"*), with 062 kept in lockstep as both bolts demand. X-2 is the single legitimate
  co-runner, because intent 035 is read-only on application source, so no rename can reach it.
- **W4** — everything here waits on W3's layered shape (064, 065) or on W1's 054 (063), and on 088
  from W3 for the specialists. P-G x P-H is MED and manageable; neither adds a real migration.
- **W5** — P-I is the second and last real migration and it is alone in its wave. X-4 needs 089/090.
- **W6** — P-J is a small UI bolt behind 068; X-5 needs 092. This closes the agentic backlog except
  the two gated bolts.
- **W7–W8** — e2e and regression, per the macro order. 070→071→072 is a strict chain; 072 executes a
  full regression baseline, which saturates the machine, so it gets its own wave and no co-runner that
  runs tests.
- **W9** — the environment triad, a strict 073→074→075 chain, config and docs only, no deployment.
- **W10** — seven docs-only spikes; the widest safe wave in the plan. **At 3:** P-N, P-O, P-P as
  grouped. **If only 2:** fold P-O into P-P's instance and expect ~26h of research on one branch.
- **W11** — synthesis needs all seven tracks and carries the owner-decision checkpoint; it runs alone
  so the decision is not competing with merges.

---

## 6. Per-wave footprint table

| Wave | Group | Files / areas touched | Conflicts to watch |
|---|---|---|---|
| W1 | P-A | `Directory.Packages.props` (new), every `*.csproj`, `renovate.json`, `Program.cs` (append one block), `DEPLOYMENT.md` | `Program.cs` shared with P-B (append-only) |
| W1 | P-B | `Models/`, `Data/PhotoPrintDbContext.cs`, new migration + **snapshot**, `Controllers/CartController.cs`, admin coupon controller, `Services/OrderService.cs`, `Program.cs` (DI append), UI cart/checkout, invoice PDF template | Sole migration in the wave; `Program.cs` with P-A |
| W1 | P-C | `src/PhotoPrint.UI/angular.json`, `e2e/`, `.github/workflows/playwright-e2e.yml`, `home-page.*`, `saved-addresses`, `profile`, `delivery-step`, new `base-api.service.ts` | Different Angular areas from P-B; different workflow file from P-A |
| W1 | X-0 | `memory-bank/bolts/085-*`, `086-*`, intent-035 story files, **`memory-bank/bolts/087-*/bolt.md` `stories:` list** | Must land before X-1 starts (W2) |
| W1 | P-D | `docs/architecture/*`, `memory-bank/standards/tech-stack.md`, `KNOWN_FAILURES.md` | none |
| W2 | P-E | **`Program.cs` (rewrite)**, new `Extensions/`, `FeatureFlags/`, `AdminSystemInfoController.cs`, `SystemInfo/`, metrics, `slos.md`, Angular `features/admin/pages/system/` | Owns `Program.cs` this wave; nothing else may edit it |
| W2 | X-1 | `reviews/lib/` scoring + verify + a new `tool-ingest` script, `reviews/lib/tests/` | **Engine change** — merges last in the wave (Rule 1) |
| W3 | P-F | ~200 files across `src/PhotoPrint.API/**` (Domain/Infrastructure/Web/Application), `src/PhotoPrint.Tests/**` reshape, `Configurations` placement | Exclusive on `src/**`; empty `Add-Migration` proof after **each** internal PR |
| W3 | X-2 | `reviews/lib/` new map/index/reachability tools, `reviews/lib/discovery-review.wf.js`, scoring (same file as 087), `reviews/lib/tests/` | **Engine change**; immune to P-F's renames |
| W4 | P-G | security extension (`Program.cs` / `Extensions/`), `Policies` constant, 6 controllers' `[Authorize]` | MED with P-H |
| W4 | P-H | `Application/Auth/**` split, `WebhooksController`, `OrderPhotoQueryService`, `Infrastructure/Data/Configurations/*` + `OnModelCreating`, **empty** verification migration | Snapshot touched (empty) — keep out of the same wave as a real migration |
| W4 | X-3 | `reviews/lib/records/schema.mjs` (lens rows), lens prompts, `reviews/lib/records/ledger.mjs`, `reviews/lib/tests/` | 089 and 090 share `schema.mjs` — serial |
| W5 | P-I | `Application/Refunds/**`, `Infrastructure/Payments/**`, ANAF credit-note builder, **real migration + snapshot**, admin endpoint | Sole migration in the wave |
| W5 | X-4 | `reviews/lib/measure/**`, eval-corpus fixtures, curator/speed-report automation, pass-router Learn row | **Engine change** |
| W6 | P-J | Angular admin order-detail + modal + Romanian copy | none |
| W6 | X-5 | `reviews/lib/fix/handback-gates.mjs`, fix-request store, records tree | **Engine change** |
| W7 | P-K | `e2e/fixtures/**`, `e2e/**` journey specs, a compose file for the Postgres e2e boot, CI tiers in `.github/workflows/` | Builds on P-C's Playwright module and P-F's Builders — do not rebuild either |
| W8 | P-L | regression checklist doc, the dated baseline report, `KNOWN_FAILURES.md`, backlog triage | Runs the full suite — **no other instance runs tests during this wave** |
| W9 | P-M | `appsettings.{dev-env}.json`, `docker-compose.dev-env.yml`, `docs/environments/config-map.md`, `.env.dev-env.example`, seeding selector + prod guard, promotion runbook, `DEPLOYMENT.md` cross-link | Boot validation spins containers — avoid co-running container work |
| W10 | P-N / P-O / P-P | `docs/analysis/eu-expansion/track-{1..7}-*.md`, one file per spike | none (docs-only) |
| W11 | P-Q | `docs/analysis/eu-expansion-architecture-study.md`, `memory-bank/standards/decision-index.md` (one ADR row), `docs/planning/i18n-readiness-brief-<date>.md` | `decision-index.md` — sole editor this wave |

---

## 7. Worktree setup

Worktrees live in `D:\worktrees\` — a sibling of the repo, never nested inside it. **Never
`git switch` in `D:\photo printing website`**: other Claude Code sessions share that checkout.
Cut each wave's worktrees **only after the previous wave has fully landed on `main`**.

```powershell
# once per session, refresh the shared checkout's main pointer
git -C "D:\photo printing website" fetch origin
git -C "D:\photo printing website" log --oneline -1 origin/main
```

### Wave 1
```powershell
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-054" -b feat/bolt-054-dependency-hardening origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-047-048" -b feat/bolts-047-048-coupons origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-066-067" -b feat/bolts-066-067-ui-scaling origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-085-086" -b feat/bolts-085-086-review-loop-verification origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-057" -b feat/bolt-057-architecture-docs origin/main
```

### Wave 2
```powershell
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-055-056-058" -b feat/bolts-055-056-058-boot-manifest origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-087" -b feat/bolt-087-trust-upgrades origin/main
```

### Wave 3
```powershell
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-059-062" -b feat/bolts-059-062-layering-and-tests origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-088" -b feat/bolt-088-map-and-reachability origin/main
```

### Wave 4
```powershell
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-063" -b feat/bolt-063-access-hardening origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-064-065" -b feat/bolts-064-065-decomposition origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-089-090" -b feat/bolts-089-090-specialists origin/main
```

### Wave 5
```powershell
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-068" -b feat/bolt-068-refund-domain origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-092" -b feat/bolt-092-learn-and-measure origin/main
```

### Wave 6
```powershell
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-069" -b feat/bolt-069-refund-ui origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-093" -b feat/bolt-093-remediation-handoff origin/main
```

### Waves 7–11
```powershell
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-070-071" -b feat/bolts-070-071-e2e-data-and-journeys origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-072" -b feat/bolt-072-regression-methodology origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-073-075" -b feat/bolts-073-075-environment-triad origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-076-080" -b feat/bolts-076-080-eu-research-market origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-077-078" -b feat/bolts-077-078-eu-research-experience origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-079-081-082" -b feat/bolts-079-081-082-eu-research-platform origin/main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-083-084" -b feat/bolts-083-084-eu-synthesis origin/main
```

### Cleanup after each PR merges
```powershell
git -C "D:\photo printing website" worktree remove "D:\worktrees\<name>"
git -C "D:\photo printing website" branch -d <branch>
git -C "D:\photo printing website" push origin --delete <branch>
```

---

## 8. Kickoff prompts

One block per instance. Each is self-contained: paste it as the first message of a fresh Claude Code
instance launched **in that worktree**.

Every product prompt ends at stage 5 by design — stage 6 (the review loop) runs centrally between
waves (§4a Rule 2).

### W1 — Instance A (P-A / 054)
```
You are implementing bolt group dependency-hardening on branch feat/bolt-054-dependency-hardening in this worktree.

Bolts, in strict order:
1. 054-dependency-and-boot-hardening — read memory-bank/bolts/054-dependency-and-boot-hardening/bolt.md first. Internal story order is STRICT: 001-patch-otel-cve -> 002-central-package-management -> 003-renovate-config -> 004-forwarded-headers-metrics.

Implement it through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with this bolt's id. The bolt type definition under .specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md dictates the stages, activities and artifacts — follow it exactly. memory-bank/standards/bolt-process.md is the lifecycle; memory-bank/standards/definition-of-done.md is the hand-back checklist. Update bolt.md frontmatter (status, current_stage, stages_completed) and the stage checkboxes as you go.

Conflict rules for this wave (three other instances are working in parallel on coupons, UI scaling, and docs/verification):
- Do NOT touch: any Angular/frontend file, any EF migration, any controller business logic.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file (updated once, at merge time).
- Your Program.cs change is ONLY the ForwardedHeadersMiddleware registration — append-only and minimal. Do NOT refactor Program.cs into extension methods; that is bolt 055 in wave 2.
- Central Package Management edits every .csproj (remove inline Version=, add Directory.Packages.props). Keep Stripe.net unified to one version.
- No EF migration in this bolt.
- Do NOT run the review loop, do NOT touch reviews/state/**, do NOT mint a PPW id.

Test scope (CLAUDE.md hard rule — never run the whole suite by default): dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~PhotoPrint.Tests.Unit.Controllers" for the webhook/metrics surface you touch, plus dotnet list package --vulnerable. No npm test — you changed no frontend code.

Done means: all four stories implemented, vulnerable-scan clean, restore resolves one version per package, the scoped tests green, DEPLOYMENT.md updated, bolt.md at stage 5 with status: review-pending (NOT complete — stage 6 runs centrally), branch pushed, PR opened against main with gh pr create. Do NOT merge the PR — merge order is coordinated centrally.
```

### W1 — Instance B (P-B / 047 + 048)
```
You are implementing bolt group coupons on branch feat/bolts-047-048-coupons in this worktree.

Bolts, in strict order:
1. 047-coupon-domain-and-api — read memory-bank/bolts/047-coupon-domain-and-api/bolt.md first. This is a ddd-construction-bolt: model -> design -> implement -> test.
2. 048-coupon-frontend — read memory-bank/bolts/048-coupon-frontend/bolt.md. simple-construction-bolt: plan -> implement -> test.

Implement both through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with each bolt's id in turn. The bolt type definitions under .specsmd/aidlc/templates/construction/bolt-types/ dictate the stages, activities and artifacts — follow them exactly. Read memory-bank/standards/data-stack.md before touching entities or migrations, and memory-bank/standards/bolt-process.md for the lifecycle gates (adversarial design check after stage 2, fresh-eyes micro-review after stage 4). Update bolt.md frontmatter and stage checkboxes as you go.

Two things this bolt is gated on: the concurrent-redemption integration test (the single most important guarantee), and the discount-then-VAT ordering, which must be written into memory-bank/standards/decision-index.md because it is irreversible once invoices are issued.

Conflict rules for this wave (others are on dependency hardening, UI scaling, docs/verification):
- Do NOT touch: Directory.Packages.props or any .csproj package version (Instance A owns Central Package Management this wave); home-page.*, saved-addresses, profile, delivery-step (Instance C owns those).
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- YOU OWN THE ONLY EF MIGRATION IN THIS WAVE. Generate it normally; PostgreSQL only (see data-stack.md).
- Keep Program.cs DI additions append-only and minimal.
- Your frontend area is cart / checkout summary / review / confirmation / invoice PDF only.
- Do NOT run the review loop, do NOT touch reviews/state/**, do NOT mint a PPW id.

Test scope: dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~PhotoPrint.Tests.Integration" for the redemption path plus the coupon unit namespaces you add; npm test -- --watch=false --include='**/cart*.spec.ts' (and the other specs you touch) from src/PhotoPrint.UI. Do not run both suites at once.

Done means: all five stories implemented, the concurrent-redemption test green, scoped tests green, both bolt.md files at their last pre-review stage with status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W1 — Instance C (P-C / 066 + 067)
```
You are implementing bolt group ui-scaling-and-e2e on branch feat/bolts-066-067-ui-scaling in this worktree.

Bolts, in strict order:
1. 066-ci-quality-gates — read memory-bank/bolts/066-ci-quality-gates/bolt.md first (angular.json budgets, then 3 Playwright smoke specs + the CI workflow).
2. 067-ui-scaling-and-e2e-ui — read memory-bank/bolts/067-ui-scaling-and-e2e-ui/bolt.md (BaseApiService, then home-page breakup, account pages, locker selector — one logical PR-sized change each).

Implement both through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with each bolt's id. The simple-construction-bolt type definition under .specsmd/aidlc/templates/construction/bolt-types/ dictates stages and artifacts — follow it exactly. Angular 21, standalone, zoneless, Vitest, Prettier, no ESLint, no NgModules (CLAUDE.md). Update bolt.md frontmatter and checkboxes as you go.

Conflict rules for this wave (others are on dependency hardening, coupons, docs/verification):
- Do NOT touch: cart / checkout summary / review / confirmation / invoice PDF (Instance B owns those); any .csproj or Directory.Packages.props (Instance A owns CPM); any backend service or controller.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- Your CI file is .github/workflows/playwright-e2e.yml — a NEW file. Do not edit existing workflows.
- No EF migration.
- Do NOT run the review loop, do NOT touch reviews/state/**, do NOT mint a PPW id.

Test scope: from src/PhotoPrint.UI, npm test -- --watch=false --include='**/<component>*.spec.ts' for each component you break up, plus npm run build for the budget check, plus the 3 new Playwright specs. No dotnet test — you changed no backend code.

Done means: budgets enforced in CI, 3 e2e smoke specs green, the four page breakups done with specs, both bolt.md files at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W1 — Instance D (X-0 / 085 + 086, then P-D / 057)
```
You are implementing bolt group review-loop-verification on branch feat/bolts-085-086-review-loop-verification in this worktree. When it is done and its PR is open, you will cut a second branch for bolt 057 (instructions at the end).

Bolts, in strict order:
1. 085-phase-1-skeleton-core — read memory-bank/bolts/085-phase-1-skeleton-core/bolt.md first.
2. 086-phase-1-skeleton-agents — read memory-bank/bolts/086-phase-1-skeleton-agents/bolt.md.

These are VERIFICATION bolts. They build NOTHING. They confirm, story by story, that the review loop under reviews/ already satisfies the seven Phase 1 stories of intent 035 — or name exactly where it does not.

Implement them through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with each bolt's id; the simple-construction-bolt type definition gives the stages, used here as plan -> verify -> record.

Required reading before you start, in this order:
- docs/agent-systems/bug-hunter-build-guide.md, section "## Implementation status (2026-09)" — the claim table you are checking, and the v3.7 extension notes on each Prompt.
- memory-bank/intents/035-bug-hunter-agent-system/units.md — unit 001, which names the seam said to satisfy each story.
- reviews/README.md — the loop's conventions, the router, the entry tiers.
Each story's **Status:** line names the seam. Open that seam, read it, and run the behaviour where it can be run. Verdict per story: satisfied / satisfied with a gap (name it) / not satisfied.

Rule for this family: extend the review loop at the seam named in each story. NEVER build the June skeleton beside it. Start from the guide's "## Implementation status (2026-09)" table and the v3.7 extensions. For these two bolts that rule means: you write no engine code at all.

Conflict rules for this wave (three other instances are working on backend and frontend product bolts):
- Do NOT touch any file under src/. Intent 035 is read-only on application source.
- Do NOT change any behaviour under reviews/lib/** — you are reading it, not fixing it.
- Do NOT touch reviews/state/** and do NOT mint a PPW id.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- You DO write: implementation-plan.md and test-walkthrough.md in each bolt folder; **Status:** and status fields on the intent-035 Phase 1 story files; new gap-story files under memory-bank/intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/stories/ with assigned_bolt: 087-phase-2-trust; and the ids of those gap stories appended to the stories: list in memory-bank/bolts/087-phase-2-trust/bolt.md. That last edit is required by bolt 085 stage 3 and is why this branch must merge before bolt 087 starts.

Done means: a verdict table with file:line and command-output evidence for all seven stories, gap stories created and registered on 087, both bolt.md files at status: review-pending (stage 6 for these is a docs-tier quick pass, run centrally), branch pushed, PR opened against main. Do NOT merge the PR.

THEN, from the shared checkout, cut the second branch and repeat this process for 057:
  git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-057" -b feat/bolt-057-architecture-docs origin/main
Bolt 057-architecture-and-standards-docs is docs-only: docs/architecture/*, memory-bank/standards/tech-stack.md, KNOWN_FAILURES.md, the quarterly audit checklist. Verify every claim against installed dependencies before writing it. Same conflict rules; separate PR.
```

### W2 — Instance A (P-E / 055 + 056 + 058)
```
You are implementing bolt group boot-composition-and-manifest on branch feat/bolts-055-056-058-boot-manifest in this worktree.

Bolts, in strict order:
1. 055-boot-composition-and-flags — read memory-bank/bolts/055-boot-composition-and-flags/bolt.md first (Program.cs -> five Add* extensions, then the typed IFeatureGate).
2. 056-system-manifest-and-liveness — /api/admin/system-info, background-job liveness heartbeat, ANAF invoice metrics + SLO.
3. 058-observability-boot-manifest-ui — the Angular admin "System" tab.

Implement all three through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with each bolt's id in turn; the simple-construction-bolt type definition dictates stages and artifacts. Read memory-bank/standards/system-architecture.md and api-conventions.md before designing the manifest endpoint. Update bolt.md frontmatter and checkboxes as you go.

Conflict rules for this wave (one other instance is working on the review-loop engine under reviews/lib — no overlap with you):
- YOU OWN Program.cs THIS WAVE. It is a rewrite into Add* extension methods; nobody else edits it. It must stay behaviour-identical.
- Do NOT touch reviews/**, .claude/skills/**, or memory-bank/story-index.md.
- No EF migration in any of these three bolts. If a schema change looks necessary, stop and ask.
- The manifest endpoint is admin-only and must not leak secrets or connection strings — treat that as a hard requirement, not a nicety.
- Do NOT run the review loop, do NOT touch reviews/state/**, do NOT mint a PPW id.

Test scope: dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~PhotoPrint.Tests.Unit.Controllers" and the integration namespace covering boot/health; from src/PhotoPrint.UI, npm test -- --watch=false --include='**/system*.spec.ts'. Add the two regression tests the bolt names: Enabled=false registers nothing, and a stale heartbeat reports degraded.

Done means: all six stories implemented, scoped tests green, the three bolt.md files at status: review-pending, branch pushed, one PR opened against main. Do NOT merge the PR.
```

### W2 — Instance B (X-1 / 087)
```
You are implementing bolt group trust-upgrades on branch feat/bolt-087-trust-upgrades in this worktree.

Bolt: 087-phase-2-trust — read memory-bank/bolts/087-phase-2-trust/bolt.md first. Story order is strict: 001-severity-scoring -> 002-tool-ingest -> 003-bug-verifier -> 004-git-revision-tracking. Story 005 is already satisfied by the pass router — no work.

Note: bolt 085 (wave 1) may have appended gap stories to this bolt's stories: list. Read the list as it stands on main; those gap stories are yours too.

Implement through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with this bolt's id; the simple-construction-bolt type definition dictates the stages and artifacts.

THE RULE FOR THIS FAMILY: extend the review loop at the seam named in each story — build each piece as a script or skill in that tree (reviews/lib/**, .claude/skills/**), with a test under reviews/lib/tests, following reviews/README.md's conventions. NEVER build the June skeleton beside the loop. Start from docs/agent-systems/bug-hunter-build-guide.md section "## Implementation status (2026-09)" and the v3.7 extensions; each guide Prompt stays the specification of the piece's behaviour. Read memory-bank/intents/035-bug-hunter-agent-system/units.md unit 002 before stage 1.

Owner decision needed at stage 1, ask before implementing: does the execution proof run on the host (the repo's own dotnet test / npm test commands) or in a throwaway container? NFR-3 caps and the no-production-data rule apply either way.

Conflict rules for this wave (one other instance is rewriting Program.cs in src/):
- Do NOT touch any file under src/. Intent 035 is read-only on application source.
- Do NOT edit reviews/state/** (id-counter, index.md, backlog.md, track-record.md) and do NOT mint a PPW id — those are a single global sequence owned by the central review runs.
- Do NOT hand-edit generated runbook tables; they regenerate from their machine home.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- This branch changes the review engine. It merges LAST in the wave, and only when no product review loop is mid-run.

Test scope: node the tests under reviews/lib/tests (the new ones plus the ones already covering the files you touched). No dotnet test, no npm test — you changed no application code.

Done means: the four gaps closed at their named seams each with a test, a run carrying a risk score, a high-severity finding carrying a failing test that names the commit it was taken on, bolt.md at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W3 — Instance A (P-F / 059 + 060 + 061 + 062) — EXCLUSIVE on src/
```
You are implementing bolt group layering-and-test-architecture on branch feat/bolts-059-062-layering-and-tests in this worktree. This is the structural refactor: no other instance is touching src/ during this wave.

Bolts, in strict order:
1. 059-layering-foundation — read memory-bank/bolts/059-layering-foundation/bolt.md first. Internal order is strict and each step is its own reviewable commit: ADR (no four projects) -> Domain/ -> Infrastructure/ -> Web/ -> Application/<Feature>/.
2. 060-conventions-and-policy — Abstractions/ subfolders, the no-repository policy doc + the IQueryable analyzer.
3. 061-handler-pattern — ICommandHandler/IEventDispatcher, CreateOrderHandler, OrderPaidEventDispatcher, retry-invoice and promote-photos handlers.
4. 062-test-infrastructure — TimeProvider adoption, shared WebApplicationFactory base, fluent Builders, reclassification of misnamed unit tests. Both 059 and 062 say LOCKSTEP: interleave 062's test moves with the layering steps rather than doing them at the end.

Implement all four through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with each bolt's id; the simple-construction-bolt type definition dictates the stages. Read memory-bank/standards/data-stack.md and coding-standards.md before you move persistence code.

Hard gate, after EVERY internal step: build + the tests covering the moved area green, AND an Add-Migration NoOpVerify that comes out EMPTY (then delete it). Zero behaviour change, zero schema drift is the whole point of this group.

Conflict rules for this wave (one other instance is working on reviews/lib — it never touches src/, and you never touch reviews/):
- Do NOT touch reviews/**, .claude/skills/**, docs/agent-systems/**.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- Do NOT add a real EF migration. If the snapshot changes, you have changed behaviour — stop and fix.
- Do NOT bundle any behaviour change, rename-for-taste, or opportunistic fix into this refactor.
- Do NOT run the review loop, do NOT touch reviews/state/**, do NOT mint a PPW id.

Test scope: this is the one group where a broad run is justified, and CLAUDE.md still applies — run it in SEQUENTIAL BATCHES by namespace (dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~PhotoPrint.Tests.Unit.<Area>", then ...~PhotoPrint.Tests.Integration), one batch at a time, never in parallel, and never alongside npm test.

Done means: four layers by folder and namespace, four controllers no longer injecting DbContext, layer rules codified, the analyzer passing, handlers in place, the test project reshaped, an empty Add-Migration after each step, all four bolt.md files at status: review-pending, branch pushed. Open ONE PR against main, or a stacked series mirroring the internal order — say which in the PR description. Do NOT merge.
```

### W3 — Instance B (X-2 / 088)
```
You are implementing bolt group map-and-reachability on branch feat/bolt-088-map-and-reachability in this worktree.

Bolt: 088-phase-3-map-and-reachability — read memory-bank/bolts/088-phase-3-map-and-reachability/bolt.md first. Build order: 001-app-mapping -> 002-code-index -> 003-reachability -> 004-severity-scoring-reachability-ext, plus the budget-and-incremental half of 017-orchestrator-scale-ext. 005-flow-tracing is left as-is — no work.

Implement through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with this bolt's id; the simple-construction-bolt type definition dictates stages and artifacts.

THE RULE FOR THIS FAMILY: extend the review loop at the seam named in each story — a script or skill in that tree (reviews/lib/**), a test under reviews/lib/tests, reviews/README.md's conventions. NEVER build the June skeleton beside the loop. Start from docs/agent-systems/bug-hunter-build-guide.md "## Implementation status (2026-09)" and the v3.7 extensions; read memory-bank/intents/035-bug-hunter-agent-system/units.md unit 003 (3a) before stage 1.

Two specifics from the bolt: code-index is a SHARED DETERMINISTIC TOOL with the knowledge builder (integration contract §7) — keep all judgment out of it. And reachability must be framework-aware for this DI + attribute-routing .NET stack: an unknown gets a weight, not a silent zero. The scoring extension (14b) edits the same scoring code bolt 087 touched, so rebase on main after 087 lands rather than reimplementing it.

Conflict rules for this wave (the other instance is doing the structural refactor across ~200 files in src/):
- Do NOT touch any file under src/. Intent 035 is read-only on application source — this is also what makes it safe to run beside the refactor.
- Do NOT edit reviews/state/** and do NOT mint a PPW id.
- Do NOT hand-edit generated runbook tables.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- This branch changes the review engine. It merges LAST in the wave, and only when no product review loop is mid-run.

Test scope: the tests under reviews/lib/tests — the new one per piece, plus the scoring tests re-run. No dotnet test, no npm test.

Done means: map, index, reachability and the scoring extension live at their seams each with a test, risk combining severity + convergence + reachability, the budget unit in place, bolt.md at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W4 — Instance A (P-G / 063)
```
You are implementing bolt group access-hardening on branch feat/bolt-063-access-hardening in this worktree.

Bolt: 063-access-hardening — read memory-bank/bolts/063-access-hardening/bolt.md first. Stories: 001-global-rate-limit (per-IP sliding window) then 002-admin-policy-constant (Policies.Admin + migrate 6 controllers). It requires 054 (real client IP behind the proxy), which shipped in wave 1.

Implement through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with this bolt's id; the simple-construction-bolt type definition dictates the stages. Read memory-bank/standards/api-conventions.md (auth headers, error shapes) before designing.

Conflict rules for this wave (one instance is decomposing AuthService and the persistence config; one is on the review-loop engine):
- Register the limiter inside the security extension created by bolt 055 — do NOT restructure Program.cs further.
- Story 002 is an ATTRIBUTE SWAP ONLY. No EF schema migration. Touch only the [Authorize] lines on the six controllers.
- Do NOT touch Application/Auth/** or Infrastructure/Data/Configurations/** — the other backend instance owns those this wave.
- Do NOT touch reviews/**, .claude/skills/**, or memory-bank/story-index.md.
- Do NOT run the review loop, do NOT touch reviews/state/**, do NOT mint a PPW id.

Test scope: dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~PhotoPrint.Tests.Integration" for the two required tests — 401 for an anonymous admin call, 429 over the limit — plus the controller unit namespace. No npm test.

Done means: both stories implemented with those tests green, no snapshot change, bolt.md at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W4 — Instance B (P-H / 064 + 065)
```
You are implementing bolt group service-decomposition on branch feat/bolts-064-065-decomposition in this worktree.

Bolts, in strict order:
1. 064-service-decomposition — read memory-bank/bolts/064-service-decomposition/bolt.md first: split AuthService into three, thin WebhooksController, extract OrderPhotoQueryService.
2. 065-persistence-config — 17 IEntityTypeConfiguration<T> files, OnModelCreating under 100 LOC.

Implement both through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with each bolt's id; the simple-construction-bolt type definition dictates the stages. Read memory-bank/standards/data-stack.md before moving persistence configuration.

Zero behaviour change in both bolts. Bolt 065's proof is an Add-Migration NoOpVerify that comes out EMPTY (then delete it).

Conflict rules for this wave (one instance is adding the rate limiter and swapping [Authorize] attributes; one is on the review-loop engine):
- Do NOT touch [Authorize] lines or the security extension — the other backend instance owns those this wave.
- Do NOT add a real EF migration. Your only migration is the empty verification one, and it is deleted before you push.
- Do NOT touch reviews/**, .claude/skills/**, or memory-bank/story-index.md.
- Do NOT run the review loop, do NOT touch reviews/state/**, do NOT mint a PPW id.

Test scope: dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~PhotoPrint.Tests.Unit.Services" plus the auth and payment/webhook integration namespaces. No npm test.

Done means: AuthService split with its integration suite green, webhooks thin, OrderPhotoQueryService extracted, 17 configuration files with an empty verification migration, both bolt.md files at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W4 — Instance C (X-3 / 089 + 090)
```
You are implementing bolt group specialists on branch feat/bolts-089-090-specialists in this worktree.

Bolts, in strict order (SERIAL on one branch — they both write to the lens manifest, so they are not run in parallel here):
1. 089-phase-3-specialists-a — read memory-bank/bolts/089-phase-3-specialists-a/bolt.md first. Only 006-taint-analysis is a gap; 007/008 are partial by design and 009 is satisfied — no work on those.
2. 090-phase-3-specialists-b — 010-dependency-audit-agent, 011-config-auditor-agent, 013-root-cause-clustering. 012 is satisfied by the race lens — no work.

Implement both through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with each bolt's id; the simple-construction-bolt type definition dictates the stages.

THE RULE FOR THIS FAMILY: extend the review loop at the seam named in each story — a script or skill in that tree (reviews/lib/**, .claude/skills/**), a test under reviews/lib/tests, reviews/README.md's conventions. NEVER build the June skeleton beside the loop. Start from docs/agent-systems/bug-hunter-build-guide.md "## Implementation status (2026-09)" and the v3.7 extensions; read memory-bank/intents/035-bug-hunter-agent-system/units.md unit 003 (3b) before stage 1.

Known overlap you must respect: both bolts write to reviews/lib/records/schema.mjs — the manifest's ONE machine home. Add rows there append-only, and NEVER hand-edit the runbook tables, which regenerate from that file. The two new lenses consume tool-ingest from bolt 087; the clustering work extends reviews/lib/records/ledger.mjs.

Conflict rules for this wave (two instances are working in src/ on access hardening and service decomposition):
- Do NOT touch any file under src/. Intent 035 is read-only on application source.
- Do NOT edit reviews/state/** and do NOT mint a PPW id.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- This branch changes the review engine. It merges LAST in the wave, and only when no product review loop is mid-run.

Test scope: the tests under reviews/lib/tests — one per piece, plus the tests already covering schema.mjs and ledger.mjs. Dependency hits must carry a CVE id and a live-advisory source. No dotnet test, no npm test.

Done means: taint-analysis, the two new lenses and root-cause clustering live at their seams each with a test, both bolt.md files at status: review-pending, branch pushed, one PR opened against main. Do NOT merge the PR.
```

### W5 — Instance A (P-I / 068)
```
You are implementing bolt group refund-domain on branch feat/bolt-068-refund-domain in this worktree.

Bolt: 068-refund-domain-and-api — read memory-bank/bolts/068-refund-domain-and-api/bolt.md first. This is a ddd-construction-bolt: model -> design -> implement -> test. Stories in order: 001-refund-schema-and-status, 002-refund-service-stripe-euplatesc, 003-anaf-credit-note (UBL type 381), 004-admin-refund-endpoint. All four are Must.

Implement through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with this bolt's id; the ddd-construction-bolt type definition under .specsmd/aidlc/templates/construction/bolt-types/ dictates the stages, artifacts and checkpoints. Before stage 2 read memory-bank/standards/data-stack.md, system-architecture.md (payments) and api-conventions.md, and run the adversarial design check from memory-bank/standards/bolt-process.md.

This is regulated, money-moving, fiscally-recorded work. The state machine, gateway idempotency and DB/gateway/ANAF consistency are the bolt's reason to exist — a refund that succeeds at the gateway and fails in the DB must be recoverable and must not double-refund. There is no optimistic concurrency in this repo (CLAUDE.md): use unique indexes and violation detection.

Conflict rules for this wave (one other instance is on the review-loop engine — no overlap with you):
- YOU OWN THE ONLY EF MIGRATION IN THIS WAVE. PostgreSQL only. Verify dotnet ef database update applies cleanly on a fresh database.
- Do NOT touch reviews/**, .claude/skills/**, or memory-bank/story-index.md.
- Do NOT build any UI — bolt 069 is the admin action, next wave.
- Do NOT run the review loop, do NOT touch reviews/state/**, do NOT mint a PPW id.

Test scope: dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~PhotoPrint.Tests.Integration" for the refund path and the credit note, plus the refund unit namespaces you add. No npm test.

Done means: all four stories implemented, the state-machine / idempotency / credit-note / endpoint tests green, the failure-mode table from stage 2 carried into the test report and filled, bolt.md at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W5 — Instance B (X-4 / 092)
```
You are implementing bolt group learn-and-measure on branch feat/bolt-092-learn-and-measure in this worktree.

Bolt: 092-phase-4-learn-and-measure — read memory-bank/bolts/092-phase-4-learn-and-measure/bolt.md first. The gaps are 003-eval-corpus (with a poison fixture), 004-eval-metrics (recall and escape), 005-curator-agent and 006-orchestrator-learn-ext. 002-bug-lifecycle is satisfied; 001-suppression-learning is SUPERSEDED — the loop never suppresses a finding, it attaches the prior decision to it (integration contract §6.5). Do not build suppression.

Implement through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with this bolt's id; the simple-construction-bolt type definition dictates the stages.

THE RULE FOR THIS FAMILY: extend the review loop at the seam named in each story — a script or skill in that tree (reviews/lib/measure/**, the fixture builder, the speed report, the pass router's Learn row), a test under reviews/lib/tests, reviews/README.md's conventions. NEVER build the June skeleton beside the loop. Start from docs/agent-systems/bug-hunter-build-guide.md "## Implementation status (2026-09)" and the v3.7 extensions; read memory-bank/intents/035-bug-hunter-agent-system/units.md unit 004 before stage 1.

Conflict rules for this wave (one instance is building the refund domain in src/):
- Do NOT touch any file under src/. Intent 035 is read-only on application source.
- Do NOT edit reviews/state/** and do NOT mint a PPW id.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- This branch changes the review engine. It merges LAST in the wave, and only when no product review loop is mid-run.

Test scope: the tests under reviews/lib/tests, including one that proves the poison fixture is caught. No dotnet test, no npm test.

Done means: the standing eval corpus with its poison fixture, recall/escape metrics, curator automation and the Learn wiring live at their seams each with a test, bolt.md at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W6 — Instance A (P-J / 069)
```
You are implementing bolt group refund-ui on branch feat/bolt-069-refund-ui in this worktree.

Bolt: 069-refund-return-flow-ui — read memory-bank/bolts/069-refund-return-flow-ui/bolt.md first. One story: 001-admin-refund-action (full and partial refund with a reason, from the admin order-detail page).

Implement through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with this bolt's id; the simple-construction-bolt type definition dictates the stages. Angular 21, standalone, zoneless, Vitest, Prettier only. UI strings are Romanian — every backend error code needs its Romanian message.

It consumes the admin refund endpoint from bolt 068, which landed last wave, and should use the BaseApiService introduced by bolt 067.

Conflict rules for this wave (one other instance is on the review-loop engine):
- Frontend only. Do NOT change the refund API or any backend service — if the endpoint is wrong, report it, do not patch around it.
- Do NOT touch reviews/**, .claude/skills/**, or memory-bank/story-index.md.
- Do NOT run the review loop, do NOT touch reviews/state/**, do NOT mint a PPW id.

Test scope: from src/PhotoPrint.UI, npm test -- --watch=false --include='**/order-detail*.spec.ts' plus any modal spec you add. No dotnet test.

Done means: the action and modal work for full and partial refunds, every error code maps to Romanian copy, specs green, bolt.md at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W6 — Instance B (X-5 / 093)
```
You are implementing bolt group remediation-handoff on branch feat/bolt-093-remediation-handoff in this worktree.

Bolt: 093-phase-5-remediation — read memory-bank/bolts/093-phase-5-remediation/bolt.md first. Two gaps: 001-regression-harvest (the surviving tripwire must be written by someone who did NOT write the fix) and 004-fix-request-emit (an idempotent fix-request store keyed by correlation_id, with the fix_status lifecycle; bug-bolts carry the correlation_id in bolt.md frontmatter — integration contract §4). 002-fix-verification and 005-orchestrator-remediation-ext are satisfied; 003-fix-proposal is left as-is by design.

Implement through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with this bolt's id; the simple-construction-bolt type definition dictates the stages.

THE RULE FOR THIS FAMILY: extend the review loop at the seam named in each story — here the hand-back gates at reviews/lib/fix/handback-gates.mjs and the records tree — with a test under reviews/lib/tests, following reviews/README.md's conventions. NEVER build the June skeleton beside the loop. Start from docs/agent-systems/bug-hunter-build-guide.md "## Implementation status (2026-09)" and the v3.7 extensions; read memory-bank/intents/035-bug-hunter-agent-system/units.md unit 005 and integration contract §4 before stage 1.

Conflict rules for this wave (one instance is building the refund admin UI):
- Do NOT touch any file under src/. Intent 035 is read-only on application source.
- Do NOT edit reviews/state/** and do NOT mint a PPW id.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- This branch changes the review engine. It merges LAST in the wave, and only when no product review loop is mid-run.

Test scope: the tests under reviews/lib/tests plus the verification pass's own tests. Prove the store is idempotent under a repeated emit with the same correlation_id. No dotnet test, no npm test.

Done means: both gaps closed at their seams each with a test, bolt.md at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W7 — Instance A (P-K / 070 + 071)
```
You are implementing bolt group e2e-data-and-journeys on branch feat/bolts-070-071-e2e-data-and-journeys in this worktree.

Bolts, in strict order:
1. 070-e2e-data-strategy — read memory-bank/bolts/070-e2e-data-strategy/bolt.md first: the documented data contract, Builder-backed guest/user/admin fixtures, Stripe + EuPlatesc test-mode fixtures, and a real-Postgres compose boot.
2. 071-e2e-journey-coverage — the eight journey story groups plus CI tiering (fast PR tier + scheduled full suite), bounded retries, failure artifacts, flake elimination.

Implement both through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with each bolt's id; the simple-construction-bolt type definition dictates the stages.

REUSE, DO NOT REBUILD: bolt 066 already shipped the Playwright module and playwright-e2e.yml; bolt 062 already shipped the fluent Builders and the shared factory base. If either looks unfit, report it — do not fork it. Read memory-bank/standards/data-stack.md for the Postgres and PostgresTestDatabase rules.

Conflict rules for this wave (you are the only instance):
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- Do NOT touch reviews/**, and do NOT run the review loop or mint a PPW id.
- Coupon and refund journeys exist and are gated per story 007 — author them, keep them gated as the story says.
- No EF migration.

Test scope: the e2e suite itself, run tier by tier, plus npm test only for specs you change. Do not run dotnet test and the e2e suite at the same time — this machine saturates.

Done means: fixtures deterministic and idempotent on re-run, the journey suite green in its PR tier, CI tiers configured, flake budget met, both bolt.md files at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W8 — Instance A (P-L / 072) — machine-exclusive
```
You are implementing bolt group regression-methodology on branch feat/bolt-072-regression-methodology in this worktree. You are the ONLY instance running this wave: story 002 executes a full regression baseline and this machine saturates under it.

Bolt: 072-regression-methodology — read memory-bank/bolts/072-regression-methodology/bolt.md first. Stories in order: 001-regression-checklist (mapped to every shipped intent, each item tagged automated-by-e2e / automated-by-integration / manual), 002-execute-regression-baseline (one dated pass), 003-triage-findings-to-backlog (new bolt / existing bolt / KNOWN_FAILURES.md).

Implement through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with this bolt's id; the simple-construction-bolt type definition dictates the stages.

The automated-by tags must be TRUE against the suite as it exists after bolt 071 — check each one, do not copy the intent list and assume coverage.

Conflict rules:
- Do NOT fix defects you find. Triage them: a finding becomes a backlog entry, a new bolt proposal, or a KNOWN_FAILURES.md row. Fixing here would make the baseline unreproducible.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- Do NOT touch reviews/**, and do NOT run the review loop or mint a PPW id — the review backlog under reviews/state/backlog.md is read-only input for your triage.
- No production code changes, no EF migration.

Test scope: this is the one bolt whose job IS the full run. Run it in sequential batches per CLAUDE.md — API by namespace, then the e2e tiers, then the UI by feature folder. One batch at a time. Record what was run, when, and on which commit.

Done means: a checklist covering every shipped intent, one dated baseline result with a go / known-issues verdict, every finding triaged to a named destination, bolt.md at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W9 — Instance A (P-M / 073 + 074 + 075)
```
You are implementing bolt group environment-triad on branch feat/bolts-073-075-environment-triad in this worktree.

Bolts, in strict order:
1. 073-config-tiers-and-compose — the named dev-env tier, layered appsettings, docker-compose.dev-env.yml, the three-tier config map, ValidateOnStart parity.
2. 074-secrets-and-seeding — the secrets tier matrix, .env.dev-env.example, the seeding policy and selector, the Production demo-data guard.
3. 075-promotion-readiness — the dev-to-prod promotion runbook and the deployment-deferral note cross-linked from DEPLOYMENT.md.
Read each memory-bank/bolts/<id>/bolt.md before starting it.

Implement all three through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with each bolt's id; the simple-construction-bolt type definition dictates the stages.

THIS IS READINESS ONLY. Nothing is deployed. No host is stood up, no real secret is provisioned, no image is pushed, no pipeline is triggered. Bolt 075 documents how a future promotion would go and explicitly defers the deployment itself. If a story looks like it asks you to deploy, re-read it and stop.

Conflict rules for this wave (you are the only instance):
- The local and production tiers stay behaviourally unchanged — prove it, do not assume it.
- Real secrets never enter the repo. Templates and placeholders only.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- Do NOT touch reviews/**, and do NOT run the review loop or mint a PPW id.
- No EF migration.

Test scope: the dev-env tier boots locally, docker compose config validates, a missing secret fails loudly at boot, production config is unchanged, and the demo-data guard makes seeding impossible in Production. Add the boot-validation tests to the existing configuration test namespace and run only that namespace.

Done means: all ten stories implemented, the three bolt.md files at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W10 — Instance A (P-N / 076 + 080)
```
You are implementing bolt group eu-research-market on branch feat/bolts-076-080-eu-research-market in this worktree.

Bolts, in strict order (both are spike-bolts — time-boxed research, zero production code):
1. 076-research-tracks (story 001-t1-fulfillment-logistics, 8h box) -> docs/analysis/eu-expansion/track-1-fulfillment.md
2. 080-research-tracks (story 005-t5-tax-invoicing-compliance, 8h box) -> docs/analysis/eu-expansion/track-5-tax-compliance.md
Read each memory-bank/bolts/<id>/bolt.md first, and docs/planning/eu-expansion-research-brief-2026-06-05.md for the owner's Checkpoint-1 decisions (compare both tiers, one brand EU-wide, ship from Romania, local currencies) — those are settled inputs, not open questions.

Implement through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with each bolt's id; the spike-bolt type definition under .specsmd/aidlc/templates/construction/bolt-types/ dictates the stages and the time box. RESPECT THE TIME BOX — a spike that overruns reports what it has.

Track 5 is the highest-rigor track: every VAT rate, threshold and e-invoicing mandate needs a dated source and must be current to 2026, and its conclusions must be expressed as concrete impact on the existing VatCalculator (bolt 038) and the e-Factura path (bolt 039).

Conflict rules for this wave (two other instances are writing other research tracks):
- Write ONLY your two track files under docs/analysis/eu-expansion/. Do not touch another track's file, the synthesis paper, or decision-index.md — bolt 083 owns those next wave.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- Zero production code. Zero test runs.
- Do NOT touch reviews/**, and do NOT run the review loop or mint a PPW id.

Done means: both track documents complete with sourced numbers per corridor / per country, both bolt.md files at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W10 — Instance B (P-O / 077 + 078)
```
You are implementing bolt group eu-research-experience on branch feat/bolts-077-078-eu-research-experience in this worktree.

Bolts, in strict order (spike-bolts, time-boxed, zero production code):
1. 077-research-tracks (story 002-t2-site-url-architecture, 6h box) -> docs/analysis/eu-expansion/track-2-site-architecture.md
2. 078-research-tracks (story 003-t3-frontend-i18n, 6h box) -> docs/analysis/eu-expansion/track-3-frontend-i18n.md
Read each memory-bank/bolts/<id>/bolt.md first, plus docs/planning/eu-expansion-research-brief-2026-06-05.md for the settled Checkpoint-1 decisions.

Implement through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with each bolt's id; the spike-bolt type definition dictates the stages and the time box. RESPECT THE TIME BOX.

Track 2 must state each option's environment-triad multiplier (referencing intent 033, which shipped as bolts 073-075). Track 3 is Angular 21 specifically — built-in compile-time i18n versus runtime libraries, with real bundle-impact numbers and the interaction with each track-2 option. RTL is not required.

Conflict rules for this wave (two other instances are writing other research tracks):
- Write ONLY your two track files under docs/analysis/eu-expansion/. Do not touch another track's file, the synthesis paper, or decision-index.md.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- Zero production code. Zero test runs. Do not start an i18n migration — this is a study.
- Do NOT touch reviews/**, and do NOT run the review loop or mint a PPW id.

Done means: both track documents complete, options compared with evidence rather than preference, both bolt.md files at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W10 — Instance C (P-P / 079 + 081 + 082)
```
You are implementing bolt group eu-research-platform on branch feat/bolts-079-081-082-eu-research-platform in this worktree.

Bolts, in strict order (spike-bolts, time-boxed, zero production code):
1. 079-research-tracks (story 004-t4-backend-localization, 4h box) -> docs/analysis/eu-expansion/track-4-backend-localization.md
2. 081-research-tracks (story 006-t6-payments-checkout, 4h box) -> docs/analysis/eu-expansion/track-6-payments.md
3. 082-research-tracks (story 007-t7-codebase-seam-audit, 6h box) -> docs/analysis/eu-expansion/track-7-seam-audit.md
Read each memory-bank/bolts/<id>/bolt.md first, plus docs/planning/eu-expansion-research-brief-2026-06-05.md.

Implement through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with each bolt's id; the spike-bolt type definition dictates the stages and the time box. RESPECT THE TIME BOX.

Track 4 must cover the deferred-culture trap: the culture belongs on the job or entity, not ambient at send time. Track 7 is REPO-BOUND AND READ-ONLY — no web research: count where RO / RON / ro-RO are hardcoded across Angular, backend messages, emails, invoice PDFs, legal pages and SEO/meta, size currency hardcoding as its own area, give file and occurrence counts per area plus the top ten heaviest spots, and note what bolts 058, 067 and 069 (all shipped by now) added to the bill.

Conflict rules for this wave (two other instances are writing other research tracks):
- Write ONLY your three track files under docs/analysis/eu-expansion/. Do not touch another track's file, the synthesis paper, or decision-index.md.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- Zero production code — track 7 especially is a read-only audit. Zero test runs.
- Do NOT touch reviews/**, and do NOT run the review loop or mint a PPW id.

Done means: three track documents complete, track 7's counts reproducible from the commands you record, all three bolt.md files at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W11 — Instance A (P-Q / 083 + 084)
```
You are implementing bolt group eu-synthesis on branch feat/bolts-083-084-eu-synthesis in this worktree.

Bolts, in strict order:
1. 083-synthesis-and-decision (spike-bolt, 6h box) — synthesize the seven track findings into two or three coherent, costed bundles; finalize docs/analysis/eu-expansion-architecture-study.md; run the OWNER-DECISION checkpoint; record the ADR in memory-bank/standards/decision-index.md.
2. 084-implementation-briefs (simple-construction-bolt, 4h box) — author docs/planning/i18n-readiness-brief-<date>.md from the ADR: ordered readiness requirements, seam prep only, no translations.
Read each memory-bank/bolts/<id>/bolt.md first, and all seven track files under docs/analysis/eu-expansion/.

Implement through the specsmd construction flow: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill with each bolt's id; the bolt type definitions dictate the stages, including 083's owner-decision checkpoint.

STOP at the owner-decision checkpoint and ask. Do not pick the bundle yourself, do not write the ADR before the owner has chosen, and do not start bolt 084 until the ADR exists. Bolt 084 is the inception feed for a future cycle — it plans readiness, it does not implement it.

Conflict rules for this wave (you are the only instance):
- You are the sole editor of memory-bank/standards/decision-index.md this wave — one ADR row, append-only.
- Do NOT edit memory-bank/story-index.md or any memory-bank index file.
- Zero production code, zero test runs, no translations, no i18n migration.
- Do NOT touch reviews/**, and do NOT run the review loop or mint a PPW id.

Done means: the options paper finalized, the owner's decision recorded as an ADR, the readiness brief authored, both bolt.md files at status: review-pending, branch pushed, PR opened against main. Do NOT merge the PR.
```

### W12 — placeholder (X-6 / 091) — do not launch yet
```
DO NOT LAUNCH THIS INSTANCE until the gate opens. Bolt 091-phase-3-oracle-grounding is blocked on the
knowledge-builder's ledger-query interface (integration contract §2 and §7 step 10, requirements D6).
The knowledge builder has no inception and no bolt numbers yet. When it exists and ledger-query is
callable, cut the worktree and use the X-family template above, with:
  branch: feat/bolt-091-oracle-grounding
  bolt: 091-phase-3-oracle-grounding (stories 014-intent-lookup, 015-hunters-contract-ext,
        016-verifier-scoring-contract-ext, and the oracle half of 017-orchestrator-scale-ext)
  same family rule: extend the review loop at the seam named in each story, start from the guide's
  "## Implementation status (2026-09)" table and the v3.7 extensions, never build the June skeleton.
```

---

## 9. PR merge and integration plan

Each group branch becomes its own GitHub PR into `main`. **PRs within a wave merge sequentially,
never together.** Order within a wave: **dependency order first, then lowest-conflict first, then the
migration-bearing branch, then the engine-changing branch LAST.**

### Per-merge protocol (every PR, every wave)
1. The PR's CI is green (build + the scoped tests the instance ran + any e2e/budget tier that applies).
2. Merge the PR to `main`.
3. **Sync step:** for every still-open PR in the same wave, update its branch from the freshly-moved
   `main` ("Update branch" in the GitHub UI, or
   `git -C "D:\worktrees\<name>" pull --rebase origin main`) and let CI re-run.
4. Run the tests covering what just landed on `main` before merging the next PR. A full run only if
   the wave was cross-cutting, and then in sequential batches (`CLAUDE.md`).
5. After the wave's last PR lands: **one** `story-index.md` update for the whole wave, as a small
   follow-up PR. No instance touched it.
6. Only then cut the next wave's worktrees, from the updated `main`.

### Merge order per wave
- **W1:** `feat/bolt-054-dependency-hardening` (CPM lands first so everyone else rebases onto it) →
  `feat/bolts-066-067-ui-scaling` → `feat/bolt-057-architecture-docs` →
  **`feat/bolts-085-086-review-loop-verification`** (must precede any 087 work) →
  `feat/bolts-047-048-coupons` **last** (the wave's only EF migration).
- **W2:** `feat/bolts-055-056-058-boot-manifest` → `feat/bolt-087-trust-upgrades` **last** (engine).
  Before merging 087, check the router: no product loop mid-run.
- **W3:** `feat/bolts-059-062-layering-and-tests` (one PR or a bottom-up stack; after each,
  `Add-Migration NoOpVerify` on `main` must come out **empty**) → `feat/bolt-088-map-and-reachability`
  **last** (engine). 088 rebases onto the merged 087 scoring code.
- **W4:** `feat/bolt-063-access-hardening` → `feat/bolts-064-065-decomposition` (rebase first so its
  `[Authorize]` context matches the merged `Policies.Admin`; its verification migration must still come
  out empty — if the snapshot moved, delete and regenerate) → `feat/bolts-089-090-specialists` **last**
  (engine).
- **W5:** `feat/bolt-068-refund-domain` (sole real migration; confirm `dotnet ef database update` on a
  fresh database) → `feat/bolt-092-learn-and-measure` **last** (engine).
- **W6:** `feat/bolt-069-refund-ui` → `feat/bolt-093-remediation-handoff` **last** (engine).
- **W7–W9, W11:** single PR each.
- **W10:** the three research PRs in any order — no overlap.

### Migration-collision summary
Only **047 (W1)** and **068 (W5)** add real EF migrations, four waves apart. **No wave contains two
migration-bearing branches.** 065 (W4) and 059–062 (W3) produce *empty* verification migrations that
are deleted before push; if the snapshot has moved under one of them at merge time, delete and
re-run `Add-Migration NoOpVerify` and confirm it is still empty. No rebase-and-regenerate dance is
expected anywhere in this plan.

### Engine-merge check (Rule 1, mechanical)
Before merging any X-family PR:
```powershell
node "D:\photo printing website\reviews\lib\route-next-pass.mjs"
```
Merge only if every product target reads quiet, closed or parked. Otherwise hold the engine PR to the
next wave boundary, or take an owner ruling to record the engine bump on the open target's ledger.

---

## 10. What the owner does between waves

1. **Review and merge the wave's PRs**, one at a time, in the §9 order. Never two at once.
2. **Run stage 6 — the review loop — centrally, one target at a time**, from the shared checkout on
   `main`: *"Continue the review loop for `<target>`"*, or *"Run the review loop unattended for
   `<target>`"* when you want it driven to quiet without gate-by-gate approval. Entry tiers
   (`reviews/README.md`) decide the depth; §4b lists each group's tier.
3. **Flip each bolt's frontmatter to `complete`** only after that bolt's first discovery pass has run
   (`bolt-process.md`). Instances leave them at `review-pending`.
4. **Answer the loop's owner gates:** certification go-ahead, the design-pass gate, close-the-loop, and
   every parked decision listed in the run-end report.
5. **Give the rulings this plan needs** (§11): the 087 sandbox question, the agentic review target, any
   engine-merge held by an open loop, 083's bundle decision, and the 094 adoption call.
6. **One `story-index.md` update per wave**, as a small follow-up PR — the single place indexes change.
7. **Prune** merged branches and worktrees, then cut the next wave's worktrees from the updated `main`.

---

## 11. Open questions needing an owner decision

1. **The 038-039 backlog sits in the same money paths bolt 047 touches.** `reviews/state/index.md`
   records that target closed *not certified*, with 11 rows still open — 4 red — and the note
   *"Fix before this feature takes a real card: PPW-687…PPW-690"*, a path where a declined card can
   produce two paid, invoiced, labelled orders from one basket. Coupons (047) change order totals and
   the invoice line. **Do you want a fix bolt for those 11 rows before or beside Wave 1, or does
   coupons proceed and the backlog stay parked until deployment approaches?** This plan schedules
   coupons in W1 as written; say the word and it moves.
2. **Where does the 087 execution proof run** — on the host with the repo's own `dotnet test` /
   `npm test`, or in a throwaway container? Bolt 087 stage 1 says agree this with you before
   implementing.
3. **Which review target do the agentic bolts get?** The existing meta target `reviews/system/`
   (`SF<n>` ids, outside the `PPW-<n>` sequence, out of the doc contracts' scope), or a per-bolt
   `reviews/<bolt>/` target under the normal contracts? The first is cheaper and keeps ids separate;
   the second keeps `bolt-process.md` stage 6 literal.
4. **Engine merges when a product loop is still open** (§4a Rule 1): hold the engine PR a wave, or
   record the engine bump on the open target's ledger and continue?
5. **Compressing the tail.** The EU research spikes (W10) are docs-only, run no tests and touch no
   file any other group touches — they could run as background filler from W8 onward and save roughly
   two waves. That deviates from your stated macro order (e2e → environments → EU study), so it needs
   your word. The same is true, more weakly, of 073 (dependency-free config work) overlapping W7.
6. **094 adoption gate:** do you want findings uploaded to GitHub code scanning (SARIF + CI gate)
   and/or synced to an issue tracker? One sentence unlocks the bolt; it can run any wave after W5.
7. **Index fixes recommended, not applied** (§1): the `story-index.md` stale `NOT STARTED` lines for
   bolts 038/039/044/045, and the 085/086 `notes:` amendment recording that they gate 087 in practice.
