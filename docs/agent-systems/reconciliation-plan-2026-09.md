---
type: implementation-plan
status: in execution — Phase 0 ruled 2026-09-02 (D1 a · D2 c · D3 b · D4 c · D5 b · D6 b · D7 a)
created: 2026-09-02
owner: Matei Barba
spec: docs/agent-systems/theory-vs-practice-2026-09.md
---

# Blueprint ⇄ Workbench Reconciliation — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` to
> implement this plan task-by-task — one fresh Opus subagent per task, a reviewer subagent per
> phase. Steps use checkbox (`- [ ]`) syntax for tracking. Every worker reads this file **and** the
> spec before its task.

**Goal:** Leave `analysis/architect-review` holding one consistent truth — the June blueprint
re-baselined to what the summer's review machinery actually built, the machinery's paid-for lessons
written into the blueprint, the bug-hunter inception re-scoped to what is genuinely missing, both
sides cross-linked, stale facts fixed — so the branch can fast-forward into `main` as the merged
state of both worlds.

**Architecture:** Docs-only change set on this branch. The blueprint documents stay the specs of
record and are edited in place with changelog blocks (bug-hunter v3.7, knowledge-builder v3.6,
contract v1.6). The workbench documents gain one pointer section each. The cross analysis is the
bridge document both sides link to. No code, no skills, no runs.

**Tech Stack:** Markdown; `node` for the checks (`reviews/lib/cli/docs-sync.mjs --check`, the
link checker below); git with the repo's `.githooks/pre-commit`.

**Spec:** [theory-vs-practice-2026-09.md](theory-vs-practice-2026-09.md) — every task cites the
section it implements. Read the spec's §3–§8 and §10–§12 before any task.

## Global Constraints

- **Commits:** conventional style, exactly one sentence, subject only, no body, no trailers
  (CLAUDE.md). One commit per task. Subject pattern:
  `docs(<area>): <what> (reconciliation P<phase>.<task>)`.
- **Never edit** `reviews/**/review-v*.md`. Archived records under `reviews/archive/**` are
  historical and stay untouched (Phase 1 leaves their one old link alone, on purpose).
- **Never** `--no-verify`, never `COMMENTS_OK=1`, never `DOCGATE_OK=1`.
- **Comments rule** applies to `.cs/.ts` only; this plan touches none. Do not add code.
- **Standards are descriptive:** where a task changes a stated fact (a version, a status, a build
  order), the document that states it is updated in the same task.
- **Blueprint conventions** (`docs/agent-systems/README.md` → "Conventions for this folder"): one
  spec of record per system, versionless filenames, edited in place, git is the history; the
  contract is normative and wins over any brief; changing the contract means checking every
  consumer in its §8.
- **Additive edits in the guides:** briefs are never rewritten wholesale; they get "extends"
  top-ups, matching the guides' own v3.x pattern (`## Prompt Nb — <component> (extends): …`).
- **Plain language** in every new paragraph; spell out a term the first time; no new acronyms.
- **The 43-brief status table** and the lens-yield numbers come from the spec (§3, §8); copy, do
  not re-derive.
- **Ruling-gated tasks** (marked `GATED: D<n>`) are written for the recommended option and carry a
  delta paragraph for the alternatives. Do not start a gated task before its ruling is recorded in
  Phase 0.

---

## Phase 0 — Decisions register (owner rulings; the gate)

**Rulings (owner, 2026-09-02):** D1 **a** · D2 **c** · D3 **b** · D4 **c** · D5 **b** · D6 **b** ·
D7 **a**. Consequence of D6 b under D1 a: the Reviewer's remaining dimensions are planned as two
new lenses (`design`, `docs-accuracy`) plus an accept / block / revise mapping of the loop's
verdict, as a unit of the re-scoped intent 035 (P2.3, P4.1, P4.2); error-handling / silent-failure
review stays with the defect-side `observability` lens, since both lens kinds run in one engine.

Seven decisions I could not settle at very high confidence. Each has three or more options,
scored 1–5 per dimension (5 = best), a total, my recommendation and my confidence in it. Below
them, the decisions I took myself, each with a one-line opt-out.

Scoring dimensions used (not all apply to every decision):
**Evidence** — how much measured workbench data supports it · **Reuse** — how much of the tested
machinery is kept · **Cost** — build/edit effort (5 = cheapest) · **Separation** — fidelity to the
blueprint's separation-of-powers principle · **Load** — owner reading/decision load (5 = least)
· **Reversibility** — how cheaply it can be undone · **Thesis** — fit with the thesis proposal.

### D1 — Posture: what *is* the engine?

| Option | Evidence | Reuse | Cost | Separation | Load | Reversibility | Total |
|---|---|---|---|---|---|---|---|
| **a. One engine, two modes.** The review loop stays the one Inspector engine. Mode 1 = pre-merge gate (today, all 11 lenses). Mode 2 = scheduled standing sweep over `main` (blueprint's posture; defect lenses + Map slot; drains the backlog). Intent 035 becomes "extend the loop". | 5 | 5 | 5 | 3 | 4 | 5 | **27** |
| b. Two systems. Build the blueprint's bug-hunter from the 43 briefs as its own system (`bug-hunting/**`); keep the review loop as the Reviewer; connect through the contract later. | 1 | 1 | 1 | 5 | 1 | 3 | 12 |
| c. Rename and absorb. Declare `reviews/` *is* the bug-hunter (rename folders, skills, vocabulary to the blueprint's), and build the Reviewer separately as the thin plugin-composed gate the concept note describes. | 3 | 4 | 2 | 4 | 3 | 2 | 18 |
| d. One engine, two front-ends. As (a), plus a formal lens split: reviewer-dimension lenses only pre-merge, defect lenses in both modes, shared ledger/ids/verification underneath. | 4 | 5 | 3 | 4 | 4 | 4 | 24 |

Why (a) beats (d): the data says defect lenses must stay pre-merge (56% of serious findings came
from them alone, pre-merge), so the split in (d) buys separation on paper and loses recall in
practice. Why (a) beats (c): renaming breaks 698 archived rows' paths, the id map, 24 test files
and every skill; the blueprint is paper and cheap to re-baseline instead.
**Recommend a. Confidence 0.8.** Affects: P2.1, P2.2, P2.5, P2.7, P4.*.

### D2 — Where records live

| Option | Gate works | Cross-target queries | Collision safety | Cost | Contract fit | Cold reader | Total |
|---|---|---|---|---|---|---|---|
| a. Status quo. Per-target folder on the feature branch; `reviews/archive/` on `main` after close; global id counter in the repo. | 5 | 2 | 2 | 5 | 2 | 4 | 20 |
| b. Contract model. One store on `main`, runs only in an integration worktree under a run-lock; branch passes write nothing (advisory). | 1 | 5 | 5 | 2 | 5 | 3 | 21 |
| **c. Working copy on the branch, canonical store on `main`.** Records travel with the PR (as today) and land in `reviews/` at merge; add an id-reservation rule (each open target reserves a `PPW` range at open, recorded in `state/id-counter`) so parallel worktrees cannot collide; `state/index.md` + `backlog.md` remain the cross-target store. | 5 | 4 | 4 | 4 | 4 | 4 | **25** |
| d. Split. Prose on the branch, machine records (metrics, ledger rows, counter) written only on `main` through a records worktree. | 4 | 5 | 5 | 1 | 4 | 2 | 21 |

**Recommend c. Confidence 0.7.** The id-reservation mechanism is a later workbench change; this
plan only writes the rule into the contract (§1 row + one sentence) and the README pointer.
Affects: P2.7, P5.1.

### D3 — Execution proof before a 🔴 counts

| Option | Soundness | Discovery cost | False-🔴 risk | Fix-loop fit | Build effort | Total |
|---|---|---|---|---|---|---|
| a. Status quo: skeptics argue from the code; execution only when a fix is verified. | 2 | 5 | 2 | 3 | 5 | 17 |
| **b. Red test required at discovery.** A 🔴 enters the ledger as 🔴 only with a failing test written by a non-fixer "prover"; otherwise it is recorded 🟠 with tag `unproven-high`. The test becomes the proving test the fix verifier already needs. | 5 | 2 | 5 | 5 | 3 | **20** |
| c. Full sandbox (blueprint's `bug-verifier`): container builds the commit, proof runs twice, flake-guarded. | 5 | 1 | 5 | 4 | 1 | 16 |
| **d. Tiered.** At discovery a 🔴 needs a concrete exploit trace confirmed by two independent skeptics; the failing test is required before the certification freeze, not before the ledger row. | 4 | 4 | 4 | 5 | 3 | **20** |

Tie. (b) is stricter and simpler to state; (d) keeps discovery cheap and moves the cost to the
one moment that must be sound. Both are written into the guide as an extension of Prompt 10 and
into the contract as a rule; the workbench implements it later.
**No recommendation between b and d — owner's call. Confidence either way 0.6.** Affects: P3.2, P2.7.

### D4 — Build order: knowledge-builder vs the Map slot

| Option | Evidence of need | Cost | What it unlocks | Risk of unused work | Blueprint alignment | Total |
|---|---|---|---|---|---|---|
| a. Blueprint order: knowledge-builder Phases 1–2 first, then the bug-hunter's oracle tier (bolt 091). | 2 | 1 | 3 | 2 | 5 | 13 |
| b. Map first: code index, application map, reachability (blueprint bolt 088) before any knowledge-builder work. | 4 | 3 | 4 | 3 | 4 | 18 |
| **c. Cheapest gaps first, then Map, knowledge-builder after seeded run 2.** Order: budget unit + metered fix rounds → red test / exploit trace rule (D3) → scanner ingest (dependency audit, static analysis) → Map slot → standing-sweep mode → knowledge-builder only if drift findings appear. | 4 | 5 | 3 | 5 | 3 | **20** |
| d. Thin oracle: no seven-stage librarian; a "contracts index" derived from `memory-bank/standards` and bolt acceptance criteria (what the `requirements` lens already reads), grown into the knowledge-builder only if it proves insufficient. | 3 | 4 | 3 | 4 | 3 | 17 |

Evidence: the `requirements` lens found 23 serious problems on two features reading bolt docs
directly, with no oracle; the 255-row backlog and "every bolt sweeps its area by hand" say the
standing-code posture is the bigger hole. **Recommend c (with d as the eventual shape of the
oracle). Confidence 0.7.** Rewrites contract §7 and the guides' "build order" sections. Affects:
P2.6, P2.7, P4.2.

### D5 — Shape of the intent 035 re-scope

| Option | Traceability | Convention fit | Cost | Clarity for a future builder | Tooling needed | Total |
|---|---|---|---|---|---|---|
| a. Annotate only. Add an "Implementation status" section to the intent's requirements and a status line to each bolt; statuses unchanged (`planned`). | 5 | 4 | 5 | 2 | 5 | 21 |
| **b. Rewrite in place.** Requirements scope → the 16 missing pieces + the gaps of the 15 partials; units re-cut around them; bolts 085–086 (Phase 1, satisfied by the loop) removed with a maintenance-log entry; bolts 087–094 re-briefed with pointers into `reviews/lib`; story-index rows updated. | 3 | 4 | 3 | 5 | 5 | 20 |
| c. Supersede. Leave intent 035 as history with a superseded note; run a fresh inception (036) for the gaps. | 5 | 5 | 2 | 4 | 1 (owner runs the specsmd inception) | 17 |
| d. Split into two inceptions: 036 "standing sweep mode" (Map, scheduled entry, single store) and 037 "trust upgrades" (proof rule, scanners, budget). | 4 | 5 | 2 | 5 | 1 | 17 |

(a) and (b) tie on the numbers. (a) leaves ten `planned` bolts of which two are done and eight
are half-wrong; (b) makes the intent true but rewrites inception artifacts by hand. **Recommend
b; take a if you want minimal churn in `memory-bank/`. Confidence 0.6.** Depends on D1 (posture
decides what the gaps are) and D4 (order). Affects: P4.*.

### D6 — The Reviewer's missing parts

| Option | Cost | Disjointness fidelity | Evidence | Duplication risk | Readiness | Total |
|---|---|---|---|---|---|---|
| a. Stay deferred, as the concept note says; only record that three dimensions run as lenses. | 5 | 3 | 2 | 5 | 5 | 20 |
| b. Plan the rest as lenses now: add `design` and `docs-accuracy` lenses; map the loop's verdicts to the note's accept / block / revise. | 4 | 2 | 4 | 3 | 4 | 17 |
| c. Compose the `pr-review-toolkit` plugins as a separate thin gate, disjoint from the loop (the note's "thin slice"). | 2 | 5 | 3 | 2 | 2 | 14 |
| **d. Defer the build, resolve the open decision now.** The note's OPEN DECISION (who owns error-handling / silent-failure review) is settled: the Inspector owns it — the loop's `observability` lens already does exactly this. Record the three existing reviewer lenses, the verdict vocabulary, and the 21% figure in the note. | 5 | 5 | 3 | 5 | 5 | **23** |

**Recommend d. Confidence 0.85.** Brought to you because it edits an OPEN DECISION you wrote.
Affects: P2.3.

### D7 — Seeded-bug run 2: part of "finishing here"?

| Option | Cost | Value for this plan's docs | Branch stays docs-only | Load | Total |
|---|---|---|---|---|---|
| **a. Post-merge, owner-scheduled.** | 5 | 2 | 5 | 5 | **17** |
| b. Now, before re-baselining. | 1 | 5 | 2 | 1 | 9 |
| c. Now, reduced (about 1M tokens: six seeds, one pass). | 3 | 3 | 2 | 3 | 11 |

**Recommend a. Confidence 0.9 — I proceed with (a) unless you say otherwise.** Nothing in
Phases 1–7 depends on the recall number; the plan records the run as the first post-merge item.

### Decided by me (high confidence; say the word to overturn any)

| # | Decision | Why |
|---|---|---|
| S1 | `suppression-learning` (Prompt 25) is replaced by decision attachment — never suppress a hunter. | 3 of the first 5 re-raised findings were overturned (spec §5 B). |
| S2 | Lessons enter the guides as additive "extends" top-ups under new changelog blocks (bug-hunter v3.7, knowledge-builder v3.6, contract v1.6). | The guides' own convention; nothing thrown away. |
| S3 | Vocabulary: keep both vocabularies; the Rosetta table (spec Appendix A) is placed in both READMEs. No renames on either side. | Renaming the workbench breaks records and tests; renaming the blueprint gains nothing. |
| S4 | Historical analyses (`docs/analysis/*`, `docs/planning/*`) get a dated "snapshot" note, not a rewrite. Planning documents still `planned` (intents 029/032/033, bolt 070) are rewritten to the Postgres-only reality. | History stays history; plans must be true. |
| S5 | The one old link inside `reviews/archive/038-039-invoicing/resolution-v9.md` stays. | Archived records are historical. |
| S6 | The cross analysis (`theory-vs-practice-2026-09.md`) is the bridge document; both READMEs link to it; it gains a final "Rulings and outcome" section. | One place to read the whole story. |
| S7 | The contract gains a `reviews/**` store row and the review loop as a consumer, rather than a second contract. | One normative interface, as the README demands. |
| S8 | Thesis edits are proposed as a diff the owner approves before commit. | Personal academic document. |

---

## Phase 1 — Hygiene (no rulings needed) — spec §10

Agent brief: *Read spec §10. Fix every stale fact listed there without changing meaning
elsewhere. One commit per task. Run the link checker after each task that touches links.*

### Task P1.1: Link checker

**Files:**
- Create (scratchpad, not the repo): `<scratchpad>/check-links.mjs`

**Interfaces:**
- Produces: `node <scratchpad>/check-links.mjs <root> [<root>…]` → prints `BROKEN <file>:<line> → <target>` lines, exits 1 if any, 0 if none. Used by P1.2, P1.3, P2.*, P5.*, P7.2.

- [ ] **Step 1: Write the script**

```js
import { readdirSync, readFileSync, statSync, existsSync } from 'node:fs'
import { join, dirname, resolve, extname } from 'node:path'
const roots = process.argv.slice(2)
if (!roots.length) { console.error('usage: node check-links.mjs <root>...'); process.exit(2) }
const files = []
const walk = d => { for (const e of readdirSync(d, { withFileTypes: true })) {
  const p = join(d, e.name)
  if (e.isDirectory()) { if (e.name !== 'node_modules' && e.name !== '.git') walk(p) }
  else if (extname(e.name) === '.md') files.push(p) } }
for (const r of roots) statSync(r).isDirectory() ? walk(r) : files.push(r)
const re = /\]\(([^)\s]+)\)/g
let broken = 0
for (const f of files) {
  const lines = readFileSync(f, 'utf8').split(/\r?\n/)
  lines.forEach((line, i) => {
    for (const m of line.matchAll(re)) {
      let t = m[1]
      if (/^(https?:|mailto:|#)/.test(t)) continue
      t = t.split('#')[0]
      if (!t) continue
      const target = resolve(dirname(f), decodeURIComponent(t))
      if (!existsSync(target)) { broken++; console.log(`BROKEN ${f}:${i + 1} → ${m[1]}`) }
    }
  })
}
console.log(`${files.length} files scanned, ${broken} broken links`)
process.exit(broken ? 1 : 0)
```

- [ ] **Step 2: Run it on the blueprint folders to get the baseline**

Run: `node <scratchpad>/check-links.mjs docs/agent-systems thesis memory-bank/intents/035-bug-hunter-agent-system`
Expected: the 8 dangling `docs/agent-systems/reviews/...` citations do **not** show (they are in
backticks, not links) — the count of BROKEN lines is the baseline to drive to 0 in P1.2/P1.3.

No commit (scratchpad file).

### Task P1.2: Dangling cross-system-review citations (8)

**Files:**
- Modify: `docs/agent-systems/bug-hunter-build-guide.md:17,27,44,64`
- Modify: `docs/agent-systems/knowledge-builder-build-guide.md:22,33,45,58`

- [ ] **Step 1: Replace each path with a git-history pointer**

For every occurrence of `` `docs/agent-systems/reviews/cross-system-review-v<N>-<date>.md` ``
substitute `` cross-system review v<N> (<date>; the review files were removed in `b4329a8`, read them with `git show b4329a8^:docs/agent-systems/reviews/cross-system-review-v<N>-<date>.md`) ``.
Keep the finding ids (G1–G16, H1–H35, J1, J3) exactly as they are.

- [ ] **Step 2: Check**

Run: `grep -n "agent-systems/reviews/" docs/agent-systems/*.md`
Expected: only the two lines inside `theory-vs-practice-2026-09.md` (§10 table) and the
`git show` pointers just written.

- [ ] **Step 3: Commit**

```bash
git add docs/agent-systems/bug-hunter-build-guide.md docs/agent-systems/knowledge-builder-build-guide.md
git commit -m "docs(agent-systems): point cross-system review citations at git history (reconciliation P1.2)"
```

### Task P1.3: Links broken by the May-analysis rename (22 of 23)

**Files:**
- Modify: `memory-bank/intents/013-upload-cleanup-fix/{inception-log.md:14,requirements.md:7}` and the same two files in intents `014-payment-hardening`, `015-sameday-shipping-integration`, `016-romanian-vat-efactura`, `017-deployment-cicd`, `018-secrets-management`, `019-thumbnail-cache-and-cloud-storage`, `020-observability-stack`, `021-distributed-state-redis`, `022-coupon-promo-codes` (20 lines)
- Modify: `memory-bank/bolts/041-secrets-management/implementation-plan.md:79`
- Modify: `memory-bank/story-index.md:682`
- Leave: `reviews/archive/038-039-invoicing/resolution-v9.md:249` (S5)

- [ ] **Step 1: Rewrite the path**

Replace `docs/architecture-analysis-2026-05-25.md` with `docs/analysis/architect-review-2026-05-25.md`
in the 22 locations. Keep the `#<n>` fragments and the surrounding text.

- [ ] **Step 2: Check**

Run: `grep -rln "docs/architecture-analysis-2026-05-25" --include=*.md . | grep -v node_modules`
Expected: exactly two files — `reviews/archive/038-039-invoicing/resolution-v9.md` and
`docs/agent-systems/theory-vs-practice-2026-09.md`.

- [ ] **Step 3: Commit**

```bash
git add memory-bank/intents memory-bank/bolts/041-secrets-management/implementation-plan.md memory-bank/story-index.md
git commit -m "docs(memory-bank): follow the May architecture analysis to docs/analysis (reconciliation P1.3)"
```

### Task P1.4: SQLite statements (17 files) — S4

**Files (rewrite — plans must be true):**
- Modify: `docs/agent-systems/future/test-quality-system.md:48-52`
- Modify: `memory-bank/intents/029-decomposition-and-hardening/requirements.md:59`, `.../units/003-persistence-config/unit-brief.md:21`, `.../units/003-persistence-config/stories/001-per-entity-configurations.md:22`
- Modify: `memory-bank/intents/032-regression-and-e2e-stabilization/{inception-log.md:63, requirements.md:135, system-context.md:29,52, units/001-e2e-data-strategy/stories/004-real-postgres-e2e-boot.md:16,22,24,30, units/001-e2e-data-strategy/unit-brief.md:89, units/003-regression-methodology/stories/002-execute-regression-baseline.md:46}`
- Modify: `memory-bank/intents/033-environment-triad/{inception-log.md:66, requirements.md:15,17,160,163, system-context.md:24,60, units/001-config-tiers-and-compose/unit-brief.md:109}`
- Modify: `memory-bank/bolts/070-e2e-data-strategy/bolt.md:38`

**Files (dated note — history stays history):**
- Modify: `docs/analysis/ai-workflow-review-2026-06-05.md` (top), `docs/analysis/architect-review-2026-06-03.md` (top)

- [ ] **Step 1: Read the current data standard**

Read `memory-bank/standards/data-stack.md` in full. The facts to carry: PostgreSQL 16 in every
environment via Npgsql; the migration chain applied at boot; EF InMemory is the integration-test
default; relational tests get a throwaway database via `PostgresTestDatabase`; SQLite removed on
2026-08-20.

- [ ] **Step 2: Rewrite each planning statement to that reality**

Rules, applied line by line:
- "runs SQLite locally/test, PostgreSQL in prod" → "runs PostgreSQL 16 everywhere; integration tests default to EF InMemory, relational tests use a throwaway Postgres database (`PostgresTestDatabase`)".
- "the SQLite→Postgres migration gap/caveat" → "the InMemory-vs-Postgres parity gap (the `db-parity` review lens exists for it)".
- "only local stays SQLite" / "Development (SQLite, MailHog…)" → "local runs Postgres via docker-compose (MailHog, relaxed…)".
- "SQLite-DateTime converters" (intent 029 configuration stories) → drop the SQLite clause; keep the `ApplyConfigurationsFromAssembly` intent.
- In `test-quality-system.md` the "Dual-DB parity" bullet becomes "**InMemory-vs-Postgres parity.** Integration tests default to EF InMemory; a suite green there can hide Postgres-only behaviour (concurrency, DDL, type mapping). The review loop's `db-parity` lens exists for exactly this; Test-Quality inherits the concern for the e2e layer, which runs against real Postgres."
- Inception-log decision rows (032:63, 033:66) are dated decisions: keep the row, append " — *superseded 2026-08-20: Postgres-only everywhere*" at the end of the decision cell.

- [ ] **Step 3: Add the snapshot note to the two analyses**

Insert directly under the H1 of each `docs/analysis/*.md` file:
`> **Snapshot of June 2026.** Statements about SQLite describe the app at that date; since 2026-08-20 the application is PostgreSQL-only in every environment (see memory-bank/standards/data-stack.md).`

- [ ] **Step 4: Check**

Run: `git diff --name-only origin/main..HEAD | xargs grep -il "sqlite"`
Expected: only `docs/analysis/ai-workflow-review-2026-06-05.md`, `docs/analysis/architect-review-2026-06-03.md`, the two inception logs (their superseded rows), and `docs/agent-systems/theory-vs-practice-2026-09.md` / `reconciliation-plan-2026-09.md`.

- [ ] **Step 5: Commit**

```bash
git add docs/agent-systems/future/test-quality-system.md docs/analysis memory-bank/intents/029-decomposition-and-hardening memory-bank/intents/032-regression-and-e2e-stabilization memory-bank/intents/033-environment-triad memory-bank/bolts/070-e2e-data-strategy
git commit -m "docs: state the Postgres-only data stack in the June planning documents (reconciliation P1.4)"
```

### Task P1.5: Small stale facts (4)

**Files:**
- Modify: `.github/agents/architect-analyst.agent.md:6` — "10 ranked improvements" → "20 ranked improvements".
- Modify: `memory-bank/intents/035-bug-hunter-agent-system/requirements.md:12,57` and `units.md` (two places: "(42 total)", "(42 stories)") — 42 → 43 (Prompt 31b was added 2026-06-12; the inception log already records it).
- Modify: `docs/planning/bolt-parallel-plan-2026-06-05.md:5` — append to the existing "Branch state at planning time" note: " — *historical: `main` absorbed the cascade through PR #10 on 2026-09-02; only this branch's docs remained.*"
- Owner action, not an agent's: tick `memory-bank/intents/035-bug-hunter-agent-system/inception-log.md:93` ("Human review complete") when P4 is accepted.

- [ ] **Step 1: Apply the four edits**
- [ ] **Step 2: Check**

Run: `grep -n "10 ranked" .github/agents/architect-analyst.agent.md; grep -rn "42" memory-bank/intents/035-bug-hunter-agent-system/requirements.md memory-bank/intents/035-bug-hunter-agent-system/units.md | grep -i "brief\|stor"`
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add .github/agents/architect-analyst.agent.md memory-bank/intents/035-bug-hunter-agent-system/requirements.md memory-bank/intents/035-bug-hunter-agent-system/units.md docs/planning/bolt-parallel-plan-2026-06-05.md
git commit -m "docs: fix the proposal count, the brief count and the stale branch note (reconciliation P1.5)"
```

---

## Phase 2 — Re-baseline the blueprint to reality — spec §3, §8, §11 step 1

Agent brief: *The blueprint must say what exists. Statuses, rosters and "what exists" sections
change; design content does not, except where a ruling (D1, D2, D4, D6) says so. Every new
sentence names the workbench component it refers to, by path.*

### Task P2.1: `future/README.md` — roster, layers, sequencing `GATED: D1, D4`

**Files:**
- Modify: `docs/agent-systems/future/README.md:31-44` (roster), `:10-29` (layers), `:46-57` (connections), `:59-65` (sequencing)

- [ ] **Step 1: Roster rows**

Replace the bug-hunter row with:
`| **bug-hunter** | Inspector — finds defects | **partially built** — the review loop under `reviews/` is this engine running in pre-merge mode: Phase 1 complete, Phases 2/4/5 half built, Phase 3 (map, breadth) missing; 12 of 43 briefs built, 15 partial, 16 missing | [guide](../bug-hunter-build-guide.md) · [status table](../bug-hunter-build-guide.md#implementation-status-2026-09) · [cross analysis](../theory-vs-practice-2026-09.md) |`

Replace the code-review row with:
`| **code-review** | Reviewer — pre-merge, diff-scoped gate | **partial, unplanned** — three of five dimensions run as lenses of the review loop (`requirements`, `quality`, `tests-coverage`); verdict synthesis and contract fidelity not built | [concept](code-review-system.md) |`

- [ ] **Step 2: Layers table** — in the "Do" row, replace `Reviewer\*` with `Reviewer (partial)` and `Inspector` with `Inspector (partial)`; update the footnote to "partial = exists inside the review loop, see the roster".

- [ ] **Step 3: Connections** — add as the first bullet:
`- **The review loop is the Inspector's engine in pre-merge mode.** It gates every bolt today (all eleven lenses) and holds the ledger, the fix verification and the certification record. The scheduled whole-codebase posture the guide describes is the same engine's second mode, not built yet (D1, 2026-09).`

- [ ] **Step 4: Sequencing** — replace the paragraph with the D4 ruling. For the recommended (c):
`Per the pre-deployment roadmap (bolts → AI infra → e2e/regression → 3-env → EU readiness → deploy) and the 2026-09 reconciliation: first the cheapest Inspector gaps (a run budget with metered fix rounds, the proof rule for high-severity findings, deterministic scanner ingest), then the Map slot and the standing-sweep mode, then the knowledge-builder only if intent-drift findings appear; the Reviewer's remaining dimensions and the Conductor follow; Test-Quality aligns with the e2e/regression phase; Observability is strictly post-deployment.`
*Delta for D4 (a):* keep the original knowledge-builder-first sentence and add only the Inspector-status clause. *Delta for D4 (b):* "then the Map slot" moves first.

- [ ] **Step 5: Check** — `node <scratchpad>/check-links.mjs docs/agent-systems/future` → 0 broken.
- [ ] **Step 6: Commit** — `git commit -m "docs(agent-systems): re-baseline the future roster to the review loop that exists (reconciliation P2.1)"`

### Task P2.2: `ARCHITECTURE.md` — what exists today `GATED: D1`

**Files:**
- Modify: `docs/agent-systems/ARCHITECTURE.md:3-5` (header), `:44-46` (Reviewer paragraph), new section after `:8`, `:147-163` (§5 caption), `:249-` (§8 caption)

- [ ] **Step 1: Header** — "Specs of record: bug-hunter **v3.6**, knowledge-builder **v3.5**, integration contract **v1.5**" → v3.7 / v3.6 / v1.6 and append: " Implementation status as of 2026-09 in §0."

- [ ] **Step 2: New §0** inserted before §1:

```markdown
## 0. What exists today (2026-09)

| Role | Blueprint name | Exists as | Status |
|---|---|---|---|
| Builder | AI-DLC / specsmd | the bolt process (`memory-bank/`) | built |
| Inspector | bug-hunter | the review loop — `reviews/`, skills `loop-driver`, `fix-review`, `reconcile-findings`, `owner-summary` — running in pre-merge mode on every bolt | Phase 1 complete; 2/4/5 partial; 3 missing |
| Reviewer | code-review | three lenses of the same loop (`requirements`, `quality`, `tests-coverage`) plus its verdict | partial, unplanned |
| Librarian | knowledge-builder | — (the `requirements` lens reads bolt documents directly) | not started |
| Planner / Wave-orchestrator | — | `.claude/agents/bolt-parallel-planner.md`, `bolt-wave-orchestrator.md` | built |

The review loop was built between June and September 2026 without reference to these specs, and
these specs without reference to it. The reconciliation is recorded in
[theory-vs-practice-2026-09.md](theory-vs-practice-2026-09.md); the 43-brief status table is in the
bug-hunter guide.
```

- [ ] **Step 3: §1 Reviewer paragraph** — replace "The **Reviewer** is dashed: a captured-but-deferred idea … Everything else here is specced and ready to build." with "The **Reviewer** is dashed: partly present as three lenses of the review loop, its verdict synthesis and contract fidelity still on paper (see [code-review-system.md](future/code-review-system.md)). The **Inspector** box is the review loop in pre-merge mode; its scheduled whole-codebase mode is not built (§0)."

- [ ] **Step 4: §5 caption** — append one sentence pointing at the D4 order: "Superseded order (2026-09): see the future README's sequencing and contract §7."

- [ ] **Step 5: §8 "Reading it"** — after "Solid = built or specced-ready; dashed = planned / partial", add "(the Inspector is solid because its engine exists; its standing-sweep mode does not)".

- [ ] **Step 6: Check + commit** — `node <scratchpad>/check-links.mjs docs/agent-systems` → 0 broken; `git commit -m "docs(agent-systems): add the what-exists-today section to the architecture summary (reconciliation P2.2)"`

### Task P2.3: `future/code-review-system.md` — partial status + the open decision `GATED: D6`

**Files:**
- Modify: `docs/agent-systems/future/code-review-system.md:3-6` (status), `:28-40` (disjointness), new section after `:40`

- [ ] **Step 1: Status block** → `> **Status: PARTIALLY BUILT, UNPLANNED (2026-09).** Three of the five dimensions below run today as lenses of the review loop (`reviews/`): intent fidelity as `requirements` (against the bolt's own documents, not an oracle), design quality as `quality` (report-only), test adequacy as `tests-coverage`. The verdict exists (`request-changes` / `approve-with-followups` / `approved`) but is a loop outcome, not the accept / block / revise synthesis described here. Comment/doc accuracy and contract fidelity are not built. The rest of this note stands as the design for the remainder.`

- [ ] **Step 2: Resolve the OPEN DECISION** (ruled D6 b under D1 a) — replace the ⚠️ bullet with:
`- **Resolved (2026-09): error-handling / silent-failure review belongs to the defect side.** The review loop's `observability` lens (swallowed exceptions, indistinguishable incident types, partial-state failures) already owns it. With one engine running both lens kinds (ruling D1), the reviewer lenses do not look for it.`
Then add a subsection `### Planned remainder (ruling D6 b, 2026-09)`: two new lenses — `design` (type design, encapsulation, API surface, naming, pattern consistency against `memory-bank/standards/`; report-only like `quality`) and `docs-accuracy` (comments, doc comments, standards and ADR text that the diff makes false) — and a verdict mapping: `approved` → accept, `request-changes` → block, `approve-with-followups` → revise-and-resubmit, computed by the loop's synthesis step, no separate system. Planned as unit `007-reviewer-remainder` of intent 035 (P4.2); intent fidelity against a contract waits for the knowledge-builder (D4).

- [ ] **Step 3: New section "What exists today (2026-09)"** after the disjointness section:

```markdown
## What exists today (2026-09)

On the two features whose records carry per-finding lens data (152 serious findings), the three
reviewer lenses alone caught 21%, the defect lenses alone 56%, both kinds 12.5%. The reviewer
dimensions earn their place pre-merge; they do not need a separate system to run. What this note
still adds: the two missing dimensions, the accept / block / revise synthesis on top of the loop's
verdict, and reading intent from a contract rather than from the bolt's own text. Source:
[theory-vs-practice-2026-09.md §3](../theory-vs-practice-2026-09.md).
```

- [ ] **Step 4: Check + commit** — link check → 0; `git commit -m "docs(agent-systems): record the reviewer dimensions the review loop already runs (reconciliation P2.3)"`

### Task P2.4: `README.md` (agent-systems) — statuses and the relationship section

**Files:**
- Modify: `docs/agent-systems/README.md:8-16` (table), new section after `:33`, `:42-49` (conventions)

- [ ] **Step 1: Table** — bug-hunter row role text: append " **Implementation status 2026-09: partially built as the review loop (`reviews/`); see the guide's status table.**" Knowledge-builder row: append " **Not started (2026-09).**" Add a row: `| [theory-vs-practice-2026-09.md](theory-vs-practice-2026-09.md) | **The bridge.** Cross analysis of these specs against the review machinery built on main, June–September 2026: concept map, contradictions and rulings, the 43-brief status, next steps. Read it before extending either side. |` and a row for `reconciliation-plan-2026-09.md`.

- [ ] **Step 2: New section**

```markdown
## Relationship to the review loop (`reviews/`)

The review loop is this blueprint's Inspector engine, built first and by hand while reviewing the
bolt branches, running in the guide's "pre-merge" mode on one feature at a time. It keeps the ledger
(`reviews/<target>/ledger.md`, `reviews/state/backlog.md`), verifies fixes by reverting them
(`reviews/lib/verify/verify-fixes.mjs`), reconciles findings (`reconcile-findings` skill), and
records every pass (`reviews/state/index.md`). Its operating rules live in
[`reviews/README.md`](../../reviews/README.md); its design notes in
[`reviews/notes/self-driving-loop-design.md`](../../reviews/notes/self-driving-loop-design.md).
The two vocabularies are mapped in the bridge document's Appendix A. Rule of thumb: the guides say
what the Inspector should become; `reviews/` says what it is.
```

- [ ] **Step 3: Conventions** — add a bullet: "- **Two sides, one truth.** A change to a status, a rule or a build order in these specs is mirrored in `reviews/README.md`'s pointer section when it affects the running loop, and vice versa."

- [ ] **Step 4: Check + commit** — link check → 0; `git commit -m "docs(agent-systems): link the specs to the review loop they describe (reconciliation P2.4)"`

### Task P2.5: Bug-hunter guide — implementation status table + pre-merge mode `GATED: D1`

**Files:**
- Modify: `docs/agent-systems/bug-hunter-build-guide.md:8` (new v3.7 changelog block above the v3.6 one), new section after the changelog blocks (before line 115 `# Part I`), `:317-337` ("Operating the system": the pre-merge bullet)

- [ ] **Step 1: Changelog block**

```markdown
> **What v3.7 adds (changelog).** Reconciliation with the first implementation (2026-09, owner
> rulings D1–D7 in `docs/agent-systems/reconciliation-plan-2026-09.md`): an **Implementation
> status** section mapping every brief to the review loop that exists under `reviews/`; the
> pre-merge run redefined as the engine's first **mode** (stateful gate) beside the scheduled
> **standing sweep** (this guide's original posture); lessons from three months of runs written
> into the briefs as extensions (Part I "Lessons from the first implementation"); Prompt 25
> replaced by decision attachment; the proof rule for high-severity findings (D3). Nothing is
> removed; every brief keeps its number.
```

- [ ] **Step 2: Implementation status section** (heading text exactly `## Implementation status (2026-09)` so the anchor `#implementation-status-2026-09` resolves):
Copy the 43-row table from the spec §8 verbatim (columns Phase | Brief | Workbench equivalent | State), preceded by:
`The review loop under `reviews/` is this system's engine, built June–September 2026 in pre-merge mode. ✓ built in spirit · ◐ partial · ✗ missing. Totals: 12 · 15 · 16. Phase 1 is done; Phase 3 is the hole. The 16 missing briefs are the honest scope of the re-planned intent 035.`

- [ ] **Step 3: Operating the system — the pre-merge bullet** (lines 322–324). Replace "A **pre-merge run is read-only advisory (v3.4):** on a feature-branch worktree it writes no ledger, coverage, or mailbox state — findings go to the PR comment alone." with:
`The engine has **two modes (v3.7, ruling D1).** **Pre-merge mode** — the mode built first, as the review loop: it runs on one feature branch before merge, over the branch diff plus the unchanged code it touches, with every lens; it is stateful (ledger rows, fix verification, certification) and its records travel with the branch (Integration Contract §1, `reviews/**`). **Standing-sweep mode** — this guide's original posture: scheduled, over `main`, whole codebase, Map slot first, draining the backlog; not built yet. A pre-merge run never writes standing-sweep state, and a sweep never edits a branch's records.`
*Delta for D1 (c/d):* name the two systems / two front-ends instead of two modes.

- [ ] **Step 4: Check + commit** — `grep -c "## Implementation status (2026-09)" docs/agent-systems/bug-hunter-build-guide.md` → 1; link check → 0; `git commit -m "docs(agent-systems): map the 43 bug-hunter briefs onto the review loop and define the two modes (reconciliation P2.5)"`

### Task P2.6: Knowledge-builder guide — build order and the interim consumer `GATED: D4`

**Files:**
- Modify: `docs/agent-systems/knowledge-builder-build-guide.md:12` (new v3.6 changelog block above v3.5), `:184-194` ("Build order across systems")

- [ ] **Step 1: Changelog block**

```markdown
> **What v3.6 adds (changelog).** Reconciliation with the first Inspector implementation (2026-09):
> the build order across systems now follows ruling D4 — the librarian is built after the
> Inspector's cheapest gaps and its Map slot, and only if intent-drift findings appear; until then
> the review loop's `requirements` lens reads bolt documents directly and is recorded as the interim
> oracle consumer (Integration Contract §8). No stage, brief or firewall rule changes.
```

- [ ] **Step 2: Build order section** — replace its ordering paragraph with the D4 ruling text (recommended (c): "Inspector cheapest gaps → proof rule → scanner ingest → Map slot → standing-sweep mode → knowledge-builder Phases 1–2 if drift findings appear → Phase 4 loop integration after the Inspector's remediation phase"). *Delta for D4 (a):* leave the section, add only the interim-consumer sentence.

- [ ] **Step 3: Check + commit** — link check → 0; `git commit -m "docs(agent-systems): order the knowledge-builder after the inspector gaps (reconciliation P2.6)"`

### Task P2.7: Integration contract v1.6 `GATED: D1, D2, D3, D4`

**Files:**
- Modify: `docs/agent-systems/integration-contract.md:7` (new v1.6 block above v1.5), `:63-77` (§1 table), `:166-199` (§4), `:273-286` (§6), `:287-311` (§7), `:312-327` (§8)

- [ ] **Step 1: Changelog block**

```markdown
> **v1.6 (2026-09) — reconciliation with the review loop.** The Inspector exists as the review loop
> (`reviews/**`), running in pre-merge mode (D1). §1 gains the `reviews/**` store row and the
> id-reservation rule for parallel worktrees (D2). §4 gains the mapping between the loop's fix
> verdicts and `fix_status`. §6 gains the blinding rule for judgment agents and the "verifier is never
> the fixer" rule inside a system. New §6.5 states the never-suppress rule. §7 is re-ordered per D4.
> §8 lists the review loop's components as consumers. Every rule below that names a brief still
> holds for that brief.
```

- [ ] **Step 2: §1 table row** (after the `bug-hunting/**` row):
`| `reviews/**` (per-target review records on the feature branch; `state/` and `archive/` on `main`) | the review loop — rows flipped only by `render-records.mjs`, events only by `wl.mjs`, ids only by `mint-id.mjs` | everyone |`
And after the single-history paragraph add: `**Id reservation (v1.6, D2):** a target reserves a `PPW` range in `reviews/state/id-counter` when it opens; two worktrees never mint from the same range. Until the reservation is implemented, the duplicate-mint alarm (`mint-id.mjs`) is the guard.`
*Delta for D2 (b):* the row's location becomes "`main` only"; the branch writes nothing.

- [ ] **Step 3: §4 mapping table** appended to the section:

```markdown
**Mapping to the review loop's verdicts (v1.6).** `verify-fixes.mjs` verdicts are the pre-merge
mode's `fix_status`: `held` ≡ `verified-fixed`; `test-never-red`, `no-test` ≡ `closed-unverified`
(no oracle entry, the fix is not counted); `revert-broke-build` ≡ `fix-failed` (re-checked at the
next pass). A fix is never counted on the fixer's word — the same rule, both modes.
```

- [ ] **Step 4: §6 additions** — two bullets at the end: `- **Blinding (v1.6):** a judgment agent (hunter, lens, skeptic, verifier) is given no prior records, finding ids or repository history for the target it judges; agreement planted by a shared hint is flagged (`hinted`) and earns no convergence credit.` and `- **Verifier ≠ fixer (v1.6):** inside a system as between systems — the agent that verifies a fix or audits a test is never the agent that wrote it.`

- [ ] **Step 5: New §6.5**

```markdown
## §6.5 — Never suppress a hunter (v1.6)

A finding the owner dismissed is not filtered out of later runs. It is re-found, the earlier
decision is attached to it verbatim, and a fresh skeptic re-argues it. Suppression patterns (bug-hunter
Prompt 25, original form) are not built: of the first five re-raised findings on the review loop,
three were overturned.
```

- [ ] **Step 6: §7** — replace the interleave list with the D4 order (recommended (c), as in P2.6), keeping the sentence about knowledge-builder Phase 5 preceding Phase 4. Add the D3 rule to §4 as one line: recommended (b) `A 🔴 / Critical finding enters the ledger as such only with a failing test written by a non-fixer; otherwise it is recorded one level lower with the tag `unproven-high`.` *Delta for D3 (d):* "…needs a concrete exploit trace confirmed by two independent skeptics at discovery; the failing test is required before the certification freeze."

- [ ] **Step 7: §8 rows** (append):
`| review loop `discovery-review.wf.js` (lenses, skeptics, synthesis) | §6 blinding; §6.5 never-suppress; §4 proof rule (D3) |`
`| review loop `verify-fixes.mjs` + `render-records.mjs` | §4 `fix_status` mapping; §1 sole writer of `reviews/**` flips |`
`| review loop `reconcile-findings` skill + `mint-id.mjs` | §1 id reservation / duplicate-mint guard |`
`| review loop `requirements` lens | interim oracle consumer until the knowledge-builder exists (§7, D4) |`

- [ ] **Step 8: Check + commit** — `grep -c "§6.5" docs/agent-systems/integration-contract.md` ≥ 2; link check → 0; `git commit -m "docs(agent-systems): contract v1.6 — the review loop as the inspector's pre-merge mode (reconciliation P2.7)"`

Reviewer checkpoint after Phase 2: an Opus reviewer subagent reads the Phase 2 diff against spec
§3, §8 and the D-rulings; every status line must cite a real path; no design content changed
without a ruling.

---

## Phase 3 — Lessons into the blueprint — spec §6, §11 step 5 (decided, S1/S2)

Agent brief: *Add, never rewrite. Each lesson becomes a short "extends (v3.7)" paragraph under the
brief it changes, in the guide's own voice, with the workbench evidence in one clause.*

### Task P3.1: Part I section "Lessons from the first implementation"

**Files:**
- Modify: `docs/agent-systems/bug-hunter-build-guide.md` — new `## Lessons from the first implementation (2026-06 → 2026-09)` inserted after "Build only as far as your bottleneck demands" (line 157–163)

- [ ] **Step 1: Write the section** — ten numbered items, each ≤ 3 lines, each ending with "→ Prompt N" naming the brief it changes:
1 One run is a sample (15·15·18 near-disjoint on one feature) → Prompts 6/7, 27. 2 Fixes seed defects (¼ to all of a late round's serious findings) → Prompts 26, 30–33. 3 Blinding is a mechanism → Prompt 7. 4 Never suppress → Prompt 25. 5 Verifier ≠ fixer inside the system; the fixer's test is audited → Prompts 10, 30. 6 Records need their own gate (lint + judge) → Prompt 4. 7 Owner reading load is the throughput limit (≤ 60-line summary, "reasons to doubt", parked decisions) → Prompts 4, 5. 8 A stop rule is a claim needing an experiment (seeded run 2) → Prompts 27, 28. 9 The system reviews itself → Prompt 29. 10 Build by running (thin slice on a real target) → Part I "The build loop".

- [ ] **Step 2: Commit** — `git commit -m "docs(agent-systems): record the review loop's ten lessons in the bug-hunter guide (reconciliation P3.1)"`

### Task P3.2: Brief extensions, Phases 1–2 (Prompts 3, 4, 5, 7, 10) `GATED: D3 for Prompt 10`

**Files:**
- Modify: `docs/agent-systems/bug-hunter-build-guide.md` — append an `**Extends (v3.7).**` paragraph at the end of Prompt 3 (`:510-527`), Prompt 4 (`:529-553`), Prompt 5 (`:555-575`), Prompt 7 (`:599-646`), Prompt 10 (`:689-718`)

- [ ] **Step 1: Prompt 3 `deduplication`** — lineage: a finding that survives a fix at the same site is NEW with `residual-of: <id>` plus `seed_round` and `area`; `hinted` marks agreement planted by shared context; when unsure, split, never merge; the skill passes a hand-labelled ground-truth set before it is trusted (the review loop's 2026-07-27 gate: 50 problems, 0 over-merges).
- [ ] **Step 2: Prompt 4 `report-rendering`** — the owner summary: ≤ 60 lines, four sections (needs your decision · reasons to doubt, computed from pass data · filed automatically · state), every claim linked to a checkable thing; records pass a deterministic lint and a model judge before they count (the loop's doc gate).
- [ ] **Step 3: Prompt 5 `triage-intake`** — decisions may be parked: an unattended run takes the written default, records `gate-parked`, and lists parked items for one batched owner sitting; a dismissed finding is attached to its re-find, never suppressed (§6.5).
- [ ] **Step 4: Prompt 7 `orchestrator`** — blinding auditor at launch (refuses a hunter whose inputs carry prior records, ids or history); the records gate before close; the system is itself a periodic target of its own hunters.
- [ ] **Step 5: Prompt 10 `Verifier`** — the verifier is never the fixer; a fix's regression test is audited by a non-author for "asserts the literal, fresh-context read, asynchronous fake" failure modes; the D3 rule (recommended (b): a 🔴 needs a failing test written by a non-fixer prover before it is a 🔴). *Delta for D3 (d):* exploit trace + two skeptics at discovery; failing test before certification.
- [ ] **Step 6: Commit** — `git commit -m "docs(agent-systems): extend the phase 1-2 briefs with the loop's dedup, records, triage and verifier lessons (reconciliation P3.2)"`

### Task P3.3: Brief extensions, Phases 4–5 (Prompts 25, 26, 27, 28, 29, 30, 31, 32)

**Files:**
- Modify: `docs/agent-systems/bug-hunter-build-guide.md` — Prompt 25 (`:1048-1062`) rewritten in place; extension paragraphs on Prompts 26, 27, 28, 29, 30, 31, 32

- [ ] **Step 1: Prompt 25** — retitle `## Prompt 25 — Skill: `decision-attachment` (replaces `suppression-learning`, v3.7)`: on a re-find, attach the earlier ruling verbatim and route the finding to a fresh skeptic with the ruling as context; never filter; report the overturn rate. Keep the old text under a one-line note "Original v3 brief superseded by ruling S1 (2026-09); see git history."
- [ ] **Step 2: Prompt 26 `bug-lifecycle`** — seed rate `s(r)` = share of a round's new serious findings whose lineage points at that round's fixes; two consecutive rounds seeding one component at s ≥ 0.3 gate a design pass (component-level protocol and reimplementation) instead of a third fix round.
- [ ] **Step 3: Prompt 27 `eval-corpus`** — the seeded-run protocol: frozen commit, blinded parallel passes, a different implanter than the hunter, per-severity recall; capture–recapture population estimates are valid only for parallel blinded passes on one frozen commit.
- [ ] **Step 4: Prompt 28 `eval-metrics`** — the post-certification escape counter (escapes ÷ certifications = the live false-certification rate); "a zero-serious pass has never been observed" is a reportable fact, not a failure.
- [ ] **Step 5: Prompt 29 `Curator`** — a second Learn output aimed at the Builder: the prevention sweep — mine the ledger for recurring defect classes and hand the Builder a ranked self-sweep to run before requesting review (`docs/prevention-sweep-idea.md`).
- [ ] **Step 6: Prompts 30–32** — a fix round is a mini-bolt: a written protocol block before any cluster of fixes sharing state, one composition review over the round's whole diff, red-first tests audited by a non-author (Prompt 30); the fix verifier's verdict vocabulary `held` / `test-never-red` / `no-test` / `revert-broke-build` (Prompt 31); `fix-proposal` stays never-applied, and the loop's in-loop fixer is the pre-merge-mode exception, with the mini-bolt gates as its price (Prompt 32).
- [ ] **Step 7: Check + commit** — `grep -c "decision-attachment" docs/agent-systems/bug-hunter-build-guide.md` ≥ 2; `git commit -m "docs(agent-systems): extend the learn and remediation briefs with seed rate, escapes and the mini-bolt rule (reconciliation P3.3)"`

Reviewer checkpoint after Phase 3: every extension is additive (no brief text deleted except
Prompt 25's retitle with its note), every evidence clause matches spec §5–§6.

---

## Phase 4 — Re-scope intent 035 and bolts 085–094 — spec §8, §11 step 1 `GATED: D1, D4, D5`

Written for D5 (b). *Delta for D5 (a):* do only P4.1 Step 1 (status section) and add one "Status
vs the review loop" line under `## Overview` of each bolt; skip P4.2–P4.4.

Agent brief: *`memory-bank/` follows `memory-bank/standards/bolt-process.md`. Bolt status
vocabulary is only `planned` / `complete`; intent status `inception-complete` / `units-defined` /
`complete`. A bolt is `complete` only after a discovery pass — none of these qualify, so satisfied
bolts are removed, not completed. Every removal is logged in `maintenance-log.md`.*

### Task P4.1: `requirements.md` — scope becomes the gaps

**Files:**
- Modify: `memory-bank/intents/035-bug-hunter-agent-system/requirements.md` (the header note, "Intent Overview", "In Scope")

- [ ] **Step 1: Header note** — after the tooling-only paragraph add: `> **Re-scoped 2026-09 (reconciliation, rulings D1/D4/D5).** The system's engine exists as the review loop (`reviews/`), built in pre-merge mode. This intent now covers only what that engine lacks: the 16 missing briefs and the gaps of the 15 partial ones, in the order ruled in D4. The status of every brief is the guide's "Implementation status (2026-09)" table.`
- [ ] **Step 2: In Scope** — replace the phase bullets with the gap list grouped as: **Trust upgrades** (tool-ingest P9; severity risk score + reachability weight P8/14b; verifier execution proof P10 per D3; git-revision moved/fixed detection P11) · **Map & breadth** (app-mapping P12, code-index P13, reachability P14, taint P16, dependency-audit P20, config-auditor P21, root-cause one-record-many-locations P23, budget unit + incremental scanning P24d) · **Learn & measure** (standing corpus + poison fixture P27, recall + escape metrics P28, curator automation P29/29b) · **Remediation hand-off** (regression-harvest by a non-fixer P30, fix-request-emit P33) · **Reviewer remainder** (ruling D6 b: `design` lens, `docs-accuracy` lens, accept / block / revise verdict mapping — no separate system) · **Oracle tier** (P24, 24b, 24c — after the knowledge-builder, D4) · **Optional** (SARIF, issue-sync, ci-gate). Each item names its workbench seam (the `reviews/lib` file it extends).
- [ ] **Step 3: Out of scope** — add: "Re-building what the review loop already does (Phase 1, Prompts 1–7, 11b, 19, 22, 26, 31, 31b)."
- [ ] **Step 4: Commit** — `git commit -m "docs(memory-bank): re-scope intent 035 to the inspector gaps the review loop leaves (reconciliation P4.1)"`

### Task P4.2: `units.md` — units re-cut around the gaps

**Files:**
- Modify: `memory-bank/intents/035-bug-hunter-agent-system/units.md`

- [ ] **Step 1** — Decomposition note: "43 briefs" stays; add "12 satisfied by the review loop, tracked in the guide's status table; units below cover the remaining 31 (16 missing + 15 partial)."
- [ ] **Step 2** — Units: `001-phase-1-skeleton` → marked "satisfied by the review loop (2026-09) — no bolt"; `002-phase-2-trust` → trust upgrades (P8/14b, P9, P10, P11); `003-phase-3-breadth-and-scale` → split in text into 003a map (P12–P14, P16, P24d budget) and 003b specialists (P20, P21, P23) with the oracle tier (P24, 24b, 24c) moved to a new `006-oracle-tier` unit gated on the knowledge-builder; `004-phase-4-learn-and-measure` → P27, P28, P29, 29b; `005-phase-5-remediation` → P30, P33 (+ P32 note); optional unchanged; new `007-reviewer-remainder` (ruling D6 b): the `design` lens, the `docs-accuracy` lens, the accept / block / revise verdict mapping — three stories, workbench seam `reviews/lib/records/schema.mjs` (lens manifest) and `reviews/lib/discovery-review.wf.js` (lens prompts, synthesis). Order per D4, the reviewer remainder after the trust upgrades.
- [ ] **Step 3: Commit** — `git commit -m "docs(memory-bank): re-cut the intent 035 units around the missing inspector pieces (reconciliation P4.2)"`

### Task P4.3: Bolts 085–094

**Files:**
- Delete: `memory-bank/bolts/085-phase-1-skeleton-core/`, `memory-bank/bolts/086-phase-1-skeleton-agents/` (satisfied; stories stay under the intent's unit folder for history)
- Modify: `memory-bank/bolts/087-phase-2-trust/bolt.md` … `094-optional-integration/bolt.md`

- [ ] **Step 1: 087–094 frontmatter** — `requires_bolts` of 087 → `[]` (was 086); 091 `requires_bolts` unchanged, add `notes: gated on the knowledge-builder per D4; last in order`; 088 `enables_bolts` unchanged. Body: replace the "Construction Method" box's first sentence with "Each component extends the review loop (`reviews/lib`, `.claude/skills`) at the seam named in its story; build it as a skill or script in that tree, with a test under `reviews/lib/tests`, following `reviews/README.md`'s conventions." Keep the skill-creator mandate only where a new standalone skill is created.
- [ ] **Step 2: Stories** — in each affected story file add a line `**Workbench seam:** <path>` naming the file it extends (from spec §8's "Workbench equivalent" column); stories of satisfied briefs get `**Status:** satisfied by <path> (2026-09)`.
- [ ] **Step 3: Check** — `ls memory-bank/bolts | grep -c "08[5-9]\|09[0-4]"` → 8; `grep -rn "086-phase-1" memory-bank/bolts/087-phase-2-trust/bolt.md` → none.
- [ ] **Step 4: Commit** — `git commit -m "docs(memory-bank): retire the satisfied skeleton bolts and point 087-094 at the review loop's seams (reconciliation P4.3)"`

### Task P4.4: story-index + maintenance-log

**Files:**
- Modify: `memory-bank/story-index.md` (the `### 035-bug-hunter-agent-system` section from line 1738; the Overview counts)
- Modify: `memory-bank/maintenance-log.md` (append an entry)

- [ ] **Step 1: story-index** — under the 035 heading add the re-scope note and mark the 12 satisfied stories `✅ GENERATED · satisfied by the review loop (2026-09)`; adjust the Overview's planned-bolt count (bolts 085/086 removed).
- [ ] **Step 2: maintenance-log** — new dated section "2026-09 — reconciliation of intent 035 with the review loop": what changed, which bolts were removed and why, pointer to the plan and the spec.
- [ ] **Step 3: Commit** — `git commit -m "docs(memory-bank): record the intent 035 re-scope in the story index and maintenance log (reconciliation P4.4)"`

Reviewer checkpoint after Phase 4: every remaining bolt's stories exist on disk; no story points at
a removed bolt; counts in story-index and units.md agree.

---

## Phase 5 — Workbench points back — spec §11 step 1, S3, S6

Agent brief: *`reviews/README.md`, `reviews/rules/*.md`, `reviews/runbooks/*.md` and the two
skills trigger `node reviews/lib/cli/docs-sync.mjs --check` at commit: never edit inside a
`<!-- generated:… -->` block; every relative link must resolve.*

### Task P5.1: `reviews/README.md` — where this sits

**Files:**
- Modify: `reviews/README.md` — new `## Where this sits in the bigger design` inserted before `## Files & conventions` (line 218); frontmatter `updated:` → today

- [ ] **Step 1: Section**

```markdown
## Where this sits in the bigger design

This loop is the Inspector engine of the agent-systems blueprint
([docs/agent-systems/README.md](../docs/agent-systems/README.md)), built first and by hand, running
in the blueprint's **pre-merge mode**: one feature branch at a time, before merge. The blueprint's
other mode — a scheduled sweep of the whole codebase that drains [backlog.md](state/backlog.md) —
is not built. The reconciliation of the two, concept by concept, with the owner's rulings, is
[theory-vs-practice-2026-09.md](../docs/agent-systems/theory-vs-practice-2026-09.md); its Appendix A
maps this README's words (pass, lens, skeptic, `PPW-n`) onto the blueprint's (run, hunter,
verifier, `correlation_id`). The rules the blueprint now states for this loop — blinding, never
suppress, verifier ≠ fixer, the fix-verdict mapping — are in
[integration-contract.md](../docs/agent-systems/integration-contract.md) §1, §4, §6, §6.5.
```

- [ ] **Step 2: Check** — `node reviews/lib/cli/docs-sync.mjs --check` → exit 0.
- [ ] **Step 3: Commit** — `git commit -m "docs(review): point the loop's README at the blueprint it implements (reconciliation P5.1)"`

### Task P5.2: Design notes, open items, prevention sweep

**Files:**
- Modify: `reviews/notes/self-driving-loop-design.md:310` ("Directions") — add option **E — Reconcile with the blueprint (done 2026-09)** as a two-line entry pointing at the bridge document, and one sentence under "Honest concerns": "The blueprint's Map slot (index, reachability) is the largest designed piece this loop lacks."
- Modify: `reviews/notes/open-items.md` — item 6: "**Post-merge follow-ups from the reconciliation.** Seeded run 2 (D7: post-merge, owner-scheduled); an ADR for the standing-sweep mode (D1); the id-reservation mechanism (D2); the proof rule implementation (D3). Context: [reconciliation-plan-2026-09.md](../../docs/agent-systems/reconciliation-plan-2026-09.md)."
- Modify: `docs/prevention-sweep-idea.md` — one line under the title: "Adopted into the blueprint as the Curator's second output (bug-hunter guide, Prompt 29 extension, 2026-09)."

- [ ] **Step 1: Apply** · **Step 2: Check** — `node <scratchpad>/check-links.mjs reviews/notes docs/prevention-sweep-idea.md` → 0 broken · **Step 3: Commit** — `git commit -m "docs(review): record the reconciliation in the design notes and open items (reconciliation P5.2)"`

---

## Phase 6 — Thesis wording (S8: owner approves the diff before commit) — spec §9

### Task P6.1: `thesis/thesis-proposal.md`

**Files:**
- Modify: `thesis/thesis-proposal.md:25` (contribution 3), `:34` (M2), `:38-40` (§6), header (companion line)

- [ ] **Step 1: Contribution 3** → "A working implementation grounded in a real .NET + Angular application — the review loop built June–September 2026 (`reviews/`), extended toward the full Inspector and formalised against the contract — plus an empirical evaluation against a baseline."
- [ ] **Step 2: M2** → "**M2 (weeks 4–8):** Extend and formalise the existing Inspector pipeline (Map → Hunt → Verify → Triage → Report → Learn): add the Map slot and the contract enforcement (mutex, sole-writer checks, id reservation); document the pre-merge mode that exists."
- [ ] **Step 3: §6** — add after the quantitative paragraph: "**Data already on disk (2026-09):** 234 fixes verified by revert-and-rerun with 6 reopened (loop-closure correctness ≈ 97%); two certifications with zero post-certification escapes so far; skeptic `refuted` verdicts on every pass as a false-positive proxy; one seeded-defect run (10/10, uninformative — the second, harder run is the thesis's key experiment); median 25 minutes and ≈ 293k tokens per serious finding."
- [ ] **Step 4: Companion line** — add under the existing companion reference: "Reconciliation of the design with the implementation: [`docs/agent-systems/theory-vs-practice-2026-09.md`](../docs/agent-systems/theory-vs-practice-2026-09.md)."
- [ ] **Step 5: Owner gate** — show the diff; commit only after approval: `git commit -m "docs(thesis): ground the proposal on the review loop that exists (reconciliation P6.1)"`

---

## Phase 7 — Bridge, verification, hand-back — spec §11

### Task P7.1: Rulings and outcome in the bridge document

**Files:**
- Modify: `docs/agent-systems/theory-vs-practice-2026-09.md` — append `## 13. Rulings and outcome (2026-09)`: a table D1–D7 with the ruling and the task that applied it; the S1–S8 list; one line per phase with its commits; frontmatter `status:` → "resolved — rulings applied".
- Update the artifact page (`blueprint-and-workbench.html`, same path, same URL): add a short "Rulings" strip under Part nine listing D1–D7 outcomes; republish.

- [ ] **Step 1: Apply** · **Step 2: Commit** — `git commit -m "docs(agent-systems): record the reconciliation rulings and outcome in the bridge document (reconciliation P7.1)"`

### Task P7.2: Whole-branch verification

- [ ] **Step 1** — `node <scratchpad>/check-links.mjs docs thesis memory-bank/intents/035-bug-hunter-agent-system memory-bank/bolts reviews/notes` → 0 broken.
- [ ] **Step 2** — `node reviews/lib/cli/docs-sync.mjs --check` → exit 0.
- [ ] **Step 3** — stale-term sweep: `grep -rn "specced, ready to build" docs/agent-systems` → 0; `grep -rn "agent-systems/reviews/" docs/agent-systems/*.md | grep -v "git show\|theory-vs-practice\|reconciliation-plan"` → 0; `grep -rn "42 numbered\|42 briefs\|(42 " memory-bank/intents/035-bug-hunter-agent-system` → 0.
- [ ] **Step 4** — commit hygiene: `git log --format=%B origin/main..HEAD | grep -c "Co-Authored-By"` → 0; every subject one sentence (`git log --format=%s origin/main..HEAD`).
- [ ] **Step 5** — `git status` clean; `git diff --stat origin/main..HEAD -- src` → nothing (docs only).

### Task P7.3: Independent review and hand-back

- [ ] **Step 1** — Dispatch one Opus reviewer subagent with: the spec, this plan, `git diff origin/main..HEAD`. Brief: "Verify each ruling D1–D7 and each S1–S8 is applied exactly once and consistently across the guide, the contract, the future notes, the intent and `reviews/README.md`; list every place two documents still disagree on a status, a rule or an order; list any deleted design content." Fix findings inline; re-run P7.2.
- [ ] **Step 2** — Hand back to the owner: the commit list, the P7.3 findings and their fixes, the open owner actions (tick the inception checkbox; approve the thesis diff if still pending), and the merge command:

```bash
git switch main && git merge --ff-only analysis/architect-review && git push origin main
```

(run by the owner; never switch branches in the shared worktree from an agent session — use a
worktree if an agent must operate on `main`).

---

## After the merge (separate plan, not tasks of this file)

1. **Seeded run 2** (D7) — the recall number both worlds rest on; ~2–2.5M tokens; owner-scheduled.
2. **Standing-sweep mode ADR** (D1) and the **id-reservation** mechanism (D2) in `reviews/lib`.
3. **Proof rule** (D3) in `discovery-review.wf.js` + a `prover` role.
4. **Parallel-development plan**: how the photo-printing product bolts and the agentic-system bolts
   (re-scoped 035, then the Reviewer remainder, then the Conductor) advance side by side across
   Claude Code instances — a `bolt-parallel-planner` run over both bolt families with the review
   loop as the shared gate. Written as `docs/agent-systems/parallel-development-plan-2026-09.md`
   once this branch is merged.

## Self-review against the spec

- §3 (identity) → P2.1, P2.2, P2.3, P2.5. §4/§5 (concepts, contradictions) → P2.7, P3.2, P3.3,
  D1–D6. §6 (lessons) → P3.1–P3.3. §7 (gaps) → P4.1–P4.3. §8 (43 briefs) → P2.5, P4.*. §9
  (thesis) → P6.1. §10 (hygiene) → P1.2–P1.5. §11 (next steps) → phases + "After the merge".
  §12 (questions) → Phase 0. Appendix A → S3, P2.4, P5.1. No spec section without a task.
- Every gated task names its ruling and carries a delta for the alternatives.
- Names used across tasks: `Implementation status (2026-09)` (anchor in P2.1 ↔ heading in P2.5);
  `§6.5` (P2.7 ↔ P3.2/P3.3 ↔ P5.1); `decision-attachment` (P3.3 ↔ S1); `unproven-high` (P2.7 ↔
  P3.2); `hinted`, `residual-of`, `seed_round`, `area` (P2.7 ↔ P3.2/P3.3, all existing workbench
  field names in `reviews/lib/records/schema.mjs`).
