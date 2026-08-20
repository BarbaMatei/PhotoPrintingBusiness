# Unattended Review Loop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The review loop runs a target end to end — discovery, fix round, verification, repeat, certification, close — in one driver run, as if the owner approved each step, with every pass executed in subagents.

**Architecture:** The mechanical router (`route-next-pass.mjs`) gains machine-readable gate kinds. A new policy module (`autonomy-policy.mjs`) is the written delegated-decision rules, executable: at each gate the driver asks it and gets `auto` (take the written default, continue) or `stop`. Every gate is delegated under the owner's standing approval (2026-08-20) — certification go-ahead and loop close included; the run behaves as if the owner approved each step. Each delegated decision is **parked**: the written default is taken, the question is recorded, and the owner reads the batch at run end. The run stops only for a question whose answer only the owner knows (a fixer blocker needing a ruling), broken records, or a gate kind with no written rule (fail closed). A new `verify-fixes.mjs` mechanizes the verification runbook's revert-and-rerun step. The loop-driver and fix-review skills get unattended modes that use all of the above. State lives entirely in the existing records (worklog, metrics, resolutions), so a killed run resumes by re-invoking the same phrase — the router is stateless.

**Tech Stack:** Plain Node ESM scripts (`.mjs`, no npm dependencies) in `reviews/lib/`, tested by the existing plain-assert fixture suite `reviews/lib/tests/run-tests.mjs`. Skill prose in `.claude/skills/*/SKILL.md`. Git for the revert-and-rerun mechanics.

**Spec:** `reviews/notes/self-driving-loop-design.md` — option B ("Make it cheap and unattended") plus the owner conversation of 2026-08-20 (subagent passes, parked gates; the owner explicitly removed every token and pass limit and delegated every gate — certification and close included — as a standing approval: "work as if I intervened and approved each step until the review is done". Do not reintroduce a limit or a wait).

## Global Constraints

- Node scripts are plain ESM `.mjs`, zero npm dependencies, matching the style of `reviews/lib/route-next-pass.mjs` (header usage comment, `--root` override for fixtures, exit codes documented in the header).
- Every `reviews/lib/` change extends `reviews/lib/tests/run-tests.mjs` and the suite must pass before commit (the pre-commit hook runs it).
- The pre-commit hook blocks added comment lines. The only comments these scripts carry are the top-of-file usage headers (the established `reviews/lib` idiom — they explain why/how, not narration). When the hook lists only those lines, re-run the same commit with `COMMENTS_OK=1`. Never `--no-verify`.
- Commits: conventional style, exactly one sentence, subject line only, no body, no trailers.
- **Standing approval** (owner decision 2026-08-20): inside an unattended run, every gate — certification launch and loop close included — proceeds on the owner's standing written approval and is reported at run end. Outside unattended runs, the README hard rules stand unchanged (certification and close wait for the owner). Task 4 updates the README and the driver's Never list so the standard stays descriptive.
- The policy module still fails closed — a gate kind with no written rule stops the run. That is missing information, not a limit.
- **No token or pass limits** (owner decision 2026-08-20): an unattended run ends at loop CLOSED, a policy `stop`, a fixer question only the owner can answer, or the no-progress guard. The no-progress guard is a breakage detector (a pass repeated without recording anything), never a spend ceiling.
- Never edit any `review-v*.md`. Records are append-only per their contracts.
- One test process at a time on this machine, always scoped filters. `verify-fixes.mjs` runs its test commands strictly sequentially.
- Markdown added under `reviews/` follows the language rules in `reviews/rules/doc-contracts.md` (vocabulary or everyday English, short sentences, exact facts).

---

### Task 1: Router — machine-readable gate kinds

`route-next-pass.mjs` today prints prose `GATE:` lines and exits 2/3; a policy module cannot parse prose reliably. Add a `GATE_KIND: <slug>` line on every gated exit.

**Files:**
- Modify: `reviews/lib/route-next-pass.mjs`
- Create: `reviews/lib/tests/fixtures/repo/reviews/909-certified-target/review-v1.md`
- Create: `reviews/lib/tests/fixtures/repo/reviews/909-certified-target/metrics.jsonl`
- Test: `reviews/lib/tests/run-tests.mjs`

**Interfaces:**
- Consumes: nothing new.
- Produces: output line `GATE_KIND: <slug>` with slugs exactly `loop-close` · `delta-worthiness` · `no-metrics` · `records-broken` · `no-row-matched`. Task 2's policy CLI and Task 4's driver prose consume these strings verbatim.

- [ ] **Step 1: Add the failing assertions**

Append to `reviews/lib/tests/run-tests.mjs`, after the existing route-next-pass block (after the `904-clean-verification` check, around line 103):

```js
// ---------- route-next-pass: gate kinds ----------
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '909-certified-target'])
  check('router exits 2 on a certified target with no pending fix round', r.code === 2, `exit ${r.code}`)
  check('router names the loop-close gate kind', r.out.includes('GATE_KIND: loop-close'), r.out.trim())
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '904-clean-verification'])
  check('router names the delta-worthiness gate kind', r.out.includes('GATE_KIND: delta-worthiness'), r.out.trim())
}
```

- [ ] **Step 2: Create the certified-target fixture**

`reviews/lib/tests/fixtures/repo/reviews/909-certified-target/review-v1.md`:

```markdown
---
type: review
target: 909-certified-target
version: 1
supersedes: null
commit: eeeeee1
branch: fixture/gate-tests
pass-type: certification
date: 2026-08-20
lenses: [security]
lenses-not-run: []
verdict: approved
blockers: []
findings: { high: 0, medium: 0, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 909-certified-target

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- This target exists so the router meets a certified pass with no pending fix round.
```

`reviews/lib/tests/fixtures/repo/reviews/909-certified-target/metrics.jsonl`:

```
{"target":"909-certified-target","pass":1,"type":"certification","date":"2026-08-20","commit":"eeeeee1","verdict":"approved","outcome":"certified","new_findings":{"high":0,"medium":0,"low":0,"cleanup":0},"reopened":0,"verified":0}
```

- [ ] **Step 3: Run the suite to verify the new assertions fail**

Run: `node reviews/lib/tests/run-tests.mjs`
Expected: FAIL — the `GATE_KIND` string assertions must fail (the exit-2 assertion on 909 may already pass).

- [ ] **Step 4: Implement in `route-next-pass.mjs`**

Replace `finish` with:

```js
function finish(code, next, gate, kind) {
  if (next) say(`NEXT: ${next}`)
  for (const [k, v] of Object.entries(COST)) if (next && next.toLowerCase().startsWith(k)) say(`COST: ${v}`)
  if (gate) say(`GATE: ${gate}`)
  if (kind) say(`GATE_KIND: ${kind}`)
  console.log(out.join('\n'))
  process.exit(code)
}
```

(The dead `${COST[...] ? '' : ''}` expression in the old first line goes away with this rewrite.)

Update exactly these call sites, leaving every other one untouched:
- no-metrics branch (`no metrics.jsonl — non-code target?`): `finish(3, null, null, 'no-metrics')`
- no-usable-lines branch (`no usable pass lines`): `finish(3, null, null, 'records-broken')`
- certified-close branch (exit 2, `close the loop …`): keep the existing gate text as-is, add `'loop-close'` as the fourth argument
- clean-verification branch (exit 3, the delta-worthiness gate): add `'delta-worthiness'` as the fourth argument, gate text unchanged
- final fallthrough (`no row matched mechanically`): `finish(3, null, null, 'no-row-matched')`

- [ ] **Step 5: Run the suite to verify it passes**

Run: `node reviews/lib/tests/run-tests.mjs`
Expected: all assertions pass, including every pre-existing one.

- [ ] **Step 6: Commit**

```bash
git add reviews/lib/route-next-pass.mjs reviews/lib/tests/
git commit -m "feat(reviews): print machine-readable gate kinds from the router"
```

---

### Task 2: Autonomy policy — the `decide` command

The written delegated-decision rules as an executable module. The driver consults it at every router gate; it answers `auto` (with the next move) or `stop`. Under the standing approval it delegates everything it has a written rule for — certification and loop close included — and fails closed on anything else.

**Files:**
- Create: `reviews/lib/autonomy-policy.mjs`
- Create: `reviews/lib/tests/fixtures/repo/reviews/910-delta-worthy/review-v1.md`
- Create: `reviews/lib/tests/fixtures/repo/reviews/910-delta-worthy/resolution-v1.md`
- Create: `reviews/lib/tests/fixtures/repo/reviews/911-patch-grade/review-v1.md`
- Create: `reviews/lib/tests/fixtures/repo/reviews/911-patch-grade/resolution-v1.md`
- Create: `reviews/lib/tests/fixtures/repo/reviews/911-patch-grade/metrics.jsonl`
- Create: `reviews/lib/tests/fixtures/repo/reviews/912-recert/review-v1.md`
- Create: `reviews/lib/tests/fixtures/repo/reviews/912-recert/resolution-v1.md`
- Create: `reviews/lib/tests/fixtures/repo/reviews/912-recert/metrics.jsonl`
- Test: `reviews/lib/tests/run-tests.mjs`

**Interfaces:**
- Consumes: Task 1's `GATE_KIND` slugs; `review-v<n>.md` frontmatter `blockers:` (inline YAML array of `PPW-<n>`); `resolution-v<n>.md` Findings rows `| PPW-<n> | <status> | … |`; `metrics.jsonl` (any prior `"type":"certification"` line switches re-certification to a single pass).
- Produces: CLI `node reviews/lib/autonomy-policy.mjs [--root <r>] <target> decide <gate-kind>` printing `ACTION: auto|stop`, then `NEXT: <move>` (auto only) and `REASON: <text>`. `NEXT` is one of `delta discovery` · `certification (pair)` · `certification (single)` · `close the loop`. Exit 0 when answered, 1 on usage/lookup error. Task 4's driver consumes this.

- [ ] **Step 1: Add the failing assertions**

Append to `run-tests.mjs`:

```js
// ---------- autonomy-policy: decide ----------
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '910-delta-worthy', 'decide', 'delta-worthiness'])
  check('policy auto-routes a blocker-fixing round to delta discovery', r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: delta discovery'), r.out.trim())
  check('policy names the fixed blocker in its reason', r.out.includes('PPW-9910'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '911-patch-grade', 'decide', 'delta-worthiness'])
  check('policy routes a patch-grade round to a first certification pair', r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: certification (pair)'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '912-recert', 'decide', 'delta-worthiness'])
  check('policy routes a re-certification as a single pass', r.code === 0 && r.out.includes('NEXT: certification (single)'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '909-certified-target', 'decide', 'loop-close'])
  check('policy closes the loop under the standing approval', r.out.includes('ACTION: auto') && r.out.includes('NEXT: close the loop'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '909-certified-target', 'decide', 'mystery-gate'])
  check('policy fails closed on an unknown gate kind', r.out.includes('ACTION: stop'), r.out.trim())
}
```

- [ ] **Step 2: Create the two decide fixtures**

`910-delta-worthy/review-v1.md` — copy the 904 fixture's review shape exactly, changing only: `target: 910-delta-worthy`, `verdict: request-changes`, `blockers: [PPW-9910]`, `findings: { high: 1, medium: 0, low: 0, cleanup: 0, refuted: 0 }`, one findings row `| PPW-9910 | 🔴 | The refund is written twice | \`Services/Fixture.cs:10\` | yes |`, and the fixer note `- This target exists so the policy meets a fix round that fixed a blocker.`

`910-delta-worthy/resolution-v1.md`:

```markdown
---
type: resolution
target: 910-delta-worthy
version: 1
answers: review-v1.md
status: resolved
fixed_commit: ffffff1
---

# Resolution v1 — 910-delta-worthy

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9910 | fixed | `ffffff1` | The refund now writes once; a regression test drives the double call. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — refund | PPW-9910 | `Services/Fixture.cs` | not needed (one-line guard) |

## Decisions

### None this round

No decision was needed; the single fix followed the review's suggestion.
```

`911-patch-grade/` — same pair, with `target: 911-patch-grade`, `verdict: request-changes`, `blockers: []`, findings `{ high: 0, medium: 1, … }`, row `| PPW-9920 | 🟠 | The label reads the stale row | \`Services/Fixture.cs:20\` | yes |`, resolution row `| PPW-9920 | fixed | \`ffffff2\` | The label reads the fresh row; a regression test pins it. |`. Its `metrics.jsonl` carries the 904 fixture's two lines with `"target":"911-patch-grade"` — a discovery and a clean verification, **no** certification line, so the policy picks the pair.

`912-recert/` — copy of the `911-patch-grade` trio, with `target: 912-recert`, ids `PPW-9930`, commit `ffffff3`, and one extra `metrics.jsonl` line appended:

```
{"target":"912-recert","pass":1,"type":"certification","date":"2026-08-19","commit":"eeeeee2","verdict":"approved","outcome":"certified","new_findings":{"high":0,"medium":0,"low":0,"cleanup":0},"reopened":0,"verified":0}
```

(A prior certification line is exactly what flips the policy from pair to single.)

- [ ] **Step 3: Run the suite to verify the new assertions fail**

Run: `node reviews/lib/tests/run-tests.mjs`
Expected: FAIL — the four new policy assertions error (`autonomy-policy.mjs` does not exist; `run()` reports code -1).

- [ ] **Step 4: Write `reviews/lib/autonomy-policy.mjs`**

```js
#!/usr/bin/env node
// Unattended-run policy for the review loop: the written delegated-decision rules, executable.
// The driver consults this at every router gate instead of stopping. Every gate with a written
// rule below proceeds on the owner's standing approval (2026-08-20) — certification and loop
// close included. Fail closed: a gate kind this file does not know answers "stop".
//
// Usage: node reviews/lib/autonomy-policy.mjs [--root <repoRoot>] <target> decide <gate-kind>
// Output: ACTION: auto|stop, then NEXT (auto only: delta discovery · certification (pair) ·
// certification (single) · close the loop) and REASON.
// Exit: 0 answered · 1 usage error or unknown target.
import { readFileSync, readdirSync, existsSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const argv = process.argv.slice(2)
let root = null
const rest = []
for (let i = 0; i < argv.length; i++) argv[i] === '--root' ? (root = argv[++i]) : rest.push(argv[i])
const [target, cmd, gateKind] = rest
const REVIEWS = root ? join(root, 'reviews') : join(dirname(fileURLToPath(import.meta.url)), '..')
if (!target || cmd !== 'decide' || !gateKind) {
  console.error('usage: node reviews/lib/autonomy-policy.mjs [--root <repoRoot>] <target> decide <gate-kind>')
  process.exit(1)
}
const dir = [join(REVIEWS, target), join(REVIEWS, 'archive', target)].find(existsSync)
if (!dir) { console.error(`no reviews folder for "${target}"`); process.exit(1) }

const say = (k, v) => console.log(`${k}: ${v}`)
const stop = reason => { say('ACTION', 'stop'); say('REASON', reason); process.exit(0) }

if (gateKind === 'loop-close') {
  say('ACTION', 'auto')
  say('NEXT', 'close the loop')
  say('REASON', 'standing owner approval (2026-08-20): the run closes the loop itself and reports the close')
  process.exit(0)
}
if (gateKind === 'delta-worthiness') {
  const reviews = readdirSync(dir).map(f => /^review-v(\d+)\.md$/.exec(f)).filter(Boolean).map(m => Number(m[1]))
  if (!reviews.length) stop('no review file — delta-worthiness cannot be judged mechanically')
  const N = Math.max(...reviews)
  const resPath = join(dir, `resolution-v${N}.md`)
  if (!existsSync(resPath)) stop(`no resolution-v${N}.md — delta-worthiness cannot be judged mechanically`)
  const fmBlock = /^---\r?\n([\s\S]*?)\r?\n---/.exec(readFileSync(join(dir, `review-v${N}.md`), 'utf8'))?.[1] ?? ''
  const lines = fmBlock.split(/\r?\n/)
  const bi = lines.findIndex(l => /^blockers:/.test(l))
  const blockers = []
  if (bi >= 0) {
    blockers.push(...(lines[bi].match(/PPW-\d+/g) ?? []))
    for (let i = bi + 1; i < lines.length && /^\s/.test(lines[i]); i++) blockers.push(...(lines[i].match(/PPW-\d+/g) ?? []))
  }
  const fixed = new Set([...readFileSync(resPath, 'utf8').matchAll(/^\|\s*(PPW-\d+)\s*\|\s*fixed\s*\|/gm)].map(m => m[1]))
  const hit = blockers.filter(b => fixed.has(b))
  if (hit.length) {
    say('ACTION', 'auto')
    say('NEXT', 'delta discovery')
    say('REASON', `the fix round fixed high-severity ${hit.join(', ')} — delta-worthy by the mechanical half of the rule`)
    process.exit(0)
  }
  const metricsPath = join(dir, 'metrics.jsonl')
  const hasCert = existsSync(metricsPath) && readFileSync(metricsPath, 'utf8').split(/\r?\n/).filter(l => l.trim())
    .some(l => { try { return JSON.parse(l).type === 'certification' } catch { return false } })
  say('ACTION', 'auto')
  say('NEXT', hasCert ? 'certification (single)' : 'certification (pair)')
  say('REASON', 'patch-grade by the mechanical half of the rule (no high-severity id fixed); loop quiet — certification proceeds on the standing owner approval (2026-08-20)')
  process.exit(0)
}
stop(`gate "${gateKind}" has no written delegation — fail closed`)
```

- [ ] **Step 5: Run the suite to verify it passes**

Run: `node reviews/lib/tests/run-tests.mjs`
Expected: all assertions pass. If the auditor smoke section (`records-auditor.mjs --root GOOD_ROOT`) newly complains about the 909/910/911 fixtures, adjust those fixtures' records until the auditor treats them like 904 — never weaken the auditor.

- [ ] **Step 6: Commit**

```bash
git add reviews/lib/autonomy-policy.mjs reviews/lib/tests/
git commit -m "feat(reviews): add the executable delegated-decision policy for unattended runs"
```

---

### Task 3: `verify-fixes.mjs` — mechanical revert-and-rerun

The verification runbook's step 2, scripted: for every `fixed` row of the latest resolution, revert the fix commit's source files (tests kept), prove the regression tests go red, restore, prove green. Agents still answer the runbook's three per-cluster questions and the judgment items (step 3 — a `git diff` the driver runs itself); this script only manufactures red/green evidence. Scope cut on purpose: judgment-item affirmation stays manual — it is one cheap diff, and parsing ledger history for affirmed commits is not worth the fragility.

**Files:**
- Create: `reviews/lib/verify-fixes.mjs`
- Test: `reviews/lib/tests/run-tests.mjs` (throwaway git repo in the OS temp dir)

**Interfaces:**
- Consumes: `resolution-v<n>.md` Findings rows (`| PPW-<n> | fixed | \`<sha>\` | … |`); git history.
- Produces: CLI `node reviews/lib/verify-fixes.mjs [--root <repoRoot>] <target> [--only PPW-1,PPW-2] [--dry-run] [--test-cmd-api "<tpl>"] [--test-cmd-ui "<tpl>"]`. One JSON line per row: `{"id","verdict","commit","filters":[],"red_exits":[],"green_exits":[]}` with verdict one of `held` · `test-never-red` · `no-test` · `test-only` · `unreachable-commit` · `revert-failed` · `green-failed` · `rename-in-fix` (plus `dry-run` in dry-run mode), then `SUMMARY: <held>/<total> held`. Exit 0 = every row `held`; 1 = any other verdict; 2 = dirty tree or usage error. Task 4's driver runs this before the verification subagent and hands the subagent its output.

- [ ] **Step 1: Add the failing assertions (temp-repo fixture)**

Append to `run-tests.mjs` (add `mkdtempSync, mkdirSync, writeFileSync, rmSync` to the `node:fs` import and `tmpdir` from `node:os` at the top):

```js
// ---------- verify-fixes: revert-and-rerun against a throwaway repo ----------
{
  const T = mkdtempSync(join(tmpdir(), 'verify-fixes-'))
  const g = (...a) => spawnSync('git', ['-C', T, ...a], { encoding: 'utf8' })
  g('init', '-q', '-b', 'main')
  g('config', 'user.email', 'fixture@test'); g('config', 'user.name', 'fixture')
  mkdirSync(join(T, 'src', 'app'), { recursive: true })
  mkdirSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit'), { recursive: true })
  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'buggy\n')
  g('add', '.'); g('commit', '-qm', 'base')
  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'fixed\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit', 'CalcTests.cs'), 'test body\n')
  g('add', '.'); g('commit', '-qm', 'fix')
  const sha = g('rev-parse', '--short', 'HEAD').stdout.trim()
  mkdirSync(join(T, 'reviews', '950-verify-target'), { recursive: true })
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${sha}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9501 | fixed | \`${sha}\` | fixture fix |\n`)
  const redGreen = `node -e "process.exit(require('fs').readFileSync('src/app/calc.txt','utf8').includes('buggy')?1:0)"`

  const dry = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--dry-run'])
  check('verify-fixes dry-run derives the plan', dry.code === 0 && dry.out.includes('calc.txt') && dry.out.includes('PhotoPrint.Tests.Unit.CalcTests'), dry.out.trim())

  const live = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen])
  check('verify-fixes proves red-then-green and reports held', live.code === 0 && live.out.includes('"verdict":"held"') && live.out.includes('SUMMARY: 1/1 held'), live.out.trim())
  check('verify-fixes leaves the tree clean', g('status', '--porcelain').stdout.trim() === '', g('status', '--porcelain').stdout)

  const neverRed = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', 'node -e "process.exit(0)"'])
  check('verify-fixes reopens a fix whose test never goes red', neverRed.code === 1 && neverRed.out.includes('"verdict":"test-never-red"'), neverRed.out.trim())

  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'dirty\n')
  const dirty = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen])
  check('verify-fixes refuses a dirty tree', dirty.code === 2, `exit ${dirty.code}: ${dirty.out.trim()}`)
  g('checkout', '--', '.')
  rmSync(T, { recursive: true, force: true })
}
```

- [ ] **Step 2: Run the suite to verify the new assertions fail**

Run: `node reviews/lib/tests/run-tests.mjs`
Expected: FAIL — the verify-fixes assertions error (script missing).

- [ ] **Step 3: Write `reviews/lib/verify-fixes.mjs`**

```js
#!/usr/bin/env node
// Mechanical half of the verification runbook: for every `fixed` row of the latest
// resolution, revert the fix commit's source files (tests kept), prove the regression tests
// go red, restore with `git reset --hard HEAD`, prove green. One test process at a time,
// strictly sequential — the machine rule. Judgment items (wont-fix/deferred/disputed
// affirmations) stay with the verifier: one cheap diff each, not worth scripting.
// Test commands are injectable so the fixture suite can drive a throwaway repo.
//
// Usage: node reviews/lib/verify-fixes.mjs [--root <repoRoot>] <target>
//          [--only PPW-1,PPW-2] [--dry-run]
//          [--test-cmd-api "<tpl with {filter}>"] [--test-cmd-ui "<tpl with {name}>"]
// Output: one JSON line per row {id, verdict, commit, filters, red_exits, green_exits},
// then "SUMMARY: <held>/<total> held". Verdicts: held · test-never-red · no-test ·
// test-only · unreachable-commit · revert-failed · green-failed · rename-in-fix · dry-run.
// Exit: 0 all held · 1 any other verdict · 2 dirty tree or usage error.
import { readFileSync, readdirSync, existsSync, rmSync } from 'node:fs'
import { spawnSync } from 'node:child_process'
import { join, dirname, basename } from 'node:path'
import { fileURLToPath } from 'node:url'

const argv = process.argv.slice(2)
let root = null, only = null, dryRun = false
let tplApi = 'dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~{filter}"'
let tplUi = 'npm --prefix src/PhotoPrint.UI test -- --watch=false --include=**/{name}*.spec.ts'
const rest = []
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === '--root') root = argv[++i]
  else if (argv[i] === '--only') only = new Set(argv[++i].split(','))
  else if (argv[i] === '--dry-run') dryRun = true
  else if (argv[i] === '--test-cmd-api') tplApi = argv[++i]
  else if (argv[i] === '--test-cmd-ui') tplUi = argv[++i]
  else rest.push(argv[i])
}
const target = rest[0]
const REPO = root ?? join(dirname(fileURLToPath(import.meta.url)), '..', '..')
if (!target) { console.error('usage: node reviews/lib/verify-fixes.mjs [--root <repoRoot>] <target> [--only ids] [--dry-run] [--test-cmd-api tpl] [--test-cmd-ui tpl]'); process.exit(2) }
const dir = [join(REPO, 'reviews', target), join(REPO, 'reviews', 'archive', target)].find(existsSync)
if (!dir) { console.error(`no reviews folder for "${target}"`); process.exit(2) }

const git = (...a) => spawnSync('git', a, { cwd: REPO, encoding: 'utf8' })
const runCmd = c => spawnSync(c, { cwd: REPO, encoding: 'utf8', shell: true, timeout: 600000 })

const versions = readdirSync(dir).map(f => /^resolution-v(\d+)\.md$/.exec(f)).filter(Boolean).map(m => Number(m[1]))
if (!versions.length) { console.error(`no resolution file in ${dir}`); process.exit(2) }
const N = Math.max(...versions)
const rows = [...readFileSync(join(dir, `resolution-v${N}.md`), 'utf8')
  .matchAll(/^\|\s*(PPW-\d+)\s*\|\s*fixed\s*\|\s*`?([0-9a-f]{7,40})`?\s*\|/gm)]
  .map(m => ({ id: m[1], commit: m[2] }))
  .filter(r => !only || only.has(r.id))
if (!rows.length) { console.error(`resolution-v${N}.md has no matching fixed rows`); process.exit(2) }

if (git('status', '--porcelain').stdout.trim() !== '') {
  console.error('the tree is dirty — verification reverts files and restores with reset --hard; commit or stash first')
  process.exit(2)
}

const isTest = p => /^src\/PhotoPrint\.Tests\//.test(p) || /\.spec\.ts$/.test(p)
const results = []
for (const row of rows) {
  const res = { id: row.id, verdict: null, commit: row.commit, filters: [], red_exits: [], green_exits: [] }
  results.push(res)
  if (git('cat-file', '-e', row.commit).status !== 0 ||
      git('merge-base', '--is-ancestor', row.commit, 'HEAD').status !== 0) { res.verdict = 'unreachable-commit'; continue }
  const entries = git('show', '--name-status', '--format=', row.commit).stdout
    .split('\n').filter(l => l.trim()).map(l => l.split('\t'))
  if (entries.some(e => e[0].startsWith('R'))) { res.verdict = 'rename-in-fix'; continue }
  const tests = entries.filter(e => isTest(e[1])).map(e => e[1])
  const source = entries.filter(e => !isTest(e[1]))
  if (!tests.length) { res.verdict = 'no-test'; continue }
  if (!source.length) { res.verdict = 'test-only'; continue }
  const cmds = tests.map(p => p.endsWith('.cs')
    ? tplApi.replace('{filter}', 'PhotoPrint.Tests.' + p.replace(/^src\/PhotoPrint\.Tests\//, '').replace(/\.cs$/, '').split('/').join('.'))
    : tplUi.replace('{name}', basename(p).replace(/\.spec\.ts$/, '')))
  res.filters = cmds
  if (dryRun) { res.verdict = 'dry-run'; res.plan = { source: source.map(e => e[1]), tests }; continue }

  let reverted = true
  for (const [st, f] of source) {
    if (st === 'A') { try { rmSync(join(REPO, f)) } catch { reverted = false } }
    else if (git('checkout', `${row.commit}^`, '--', f).status !== 0) reverted = false
  }
  if (!reverted) {
    git('reset', '--hard', 'HEAD')
    res.verdict = 'revert-failed'
    continue
  }
  for (const c of cmds) res.red_exits.push(runCmd(c).status ?? -1)
  const restore = git('reset', '--hard', 'HEAD')
  if (restore.status !== 0 || git('status', '--porcelain').stdout.trim() !== '') {
    console.error(`FATAL: restore failed after reverting ${row.id} — check the tree by hand`)
    process.exit(2)
  }
  if (!res.red_exits.some(x => x !== 0)) { res.verdict = 'test-never-red'; continue }
  for (const c of cmds) res.green_exits.push(runCmd(c).status ?? -1)
  res.verdict = res.green_exits.every(x => x === 0) ? 'held' : 'green-failed'
}

for (const r of results) console.log(JSON.stringify(r))
const held = results.filter(r => r.verdict === 'held' || r.verdict === 'dry-run').length
console.log(`SUMMARY: ${held}/${results.length} held`)
process.exit(held === results.length ? 0 : 1)
```

- [ ] **Step 4: Run the suite to verify it passes**

Run: `node reviews/lib/tests/run-tests.mjs`
Expected: PASS, including the tree-clean and dirty-tree refusal assertions.

- [ ] **Step 5: Sanity-run dry mode against the real repo**

Run: `node reviews/lib/verify-fixes.mjs 038-039-invoicing --dry-run`
Expected: exit 0, one JSON plan line per `fixed` row of the latest resolution, filters shaped `PhotoPrint.Tests.<Namespace>.<Class>`. No git mutation (dry-run never touches the tree). If the resolution has no `fixed` rows at the time, the "no matching fixed rows" exit-2 message is the correct result — note it and move on.

- [ ] **Step 6: Update the verification runbook (descriptive-standards rule)**

In `reviews/runbooks/runbook-verification.md`, step 2, append one sentence:

```
`node reviews/lib/verify-fixes.mjs <target>` runs this step mechanically and prints one
verdict line per fix; `test-never-red`, `revert-failed` and `green-failed` rows come back
to you for a reopen or a hand check.
```

- [ ] **Step 7: Commit**

```bash
git add reviews/lib/verify-fixes.mjs reviews/lib/tests/run-tests.mjs reviews/runbooks/runbook-verification.md
git commit -m "feat(reviews): script the verification revert-and-rerun step (verify-fixes.mjs)"
```

---

### Task 4: Unattended mode — loop-driver skill, README, vocabulary

The driver learns to chain passes under the policy, executing every pass in subagents. Docs change in the same commit (descriptive-standards rule).

**Files:**
- Modify: `.claude/skills/loop-driver/SKILL.md` (insert a new section between "## 5 · Close out" and "## 6 · Closing a loop")
- Modify: `reviews/README.md` (insert after the "Standing instruction" paragraph in "How to run")
- Modify: `reviews/rules/doc-contracts.md` (two vocabulary entries)

**Interfaces:**
- Consumes: Task 1's `GATE_KIND` lines, Task 2's policy CLI, Task 3's `verify-fixes.mjs`.
- Produces: the trigger phrase "run the review loop unattended for `<target>`"; the worklog event shapes for unattended runs, locked here for every later task: `{"t":"<iso>","ev":"run-start"}` · `{"t":"<iso>","ev":"gate-parked","kind":"<slug>","default":"<what was taken>","reason":"<why>"}` · `{"t":"<iso>","ev":"run-end","passes":<int>,"parked":<int>}` (the existing `pass-launch` shape is unchanged; the records auditor only requires string `t` and `ev` on worklog events, so no auditor change). Task 5's fixer variant and Task 7's pilot consume these.

- [ ] **Step 1: Insert the SKILL.md section**

Insert into `.claude/skills/loop-driver/SKILL.md`, between sections 5 and 6, verbatim:

```markdown
## Unattended runs — "run the review loop unattended for <target>"

The "until a gate" mode, extended by the written delegation in
`reviews/lib/autonomy-policy.mjs`. One run = the whole remaining loop, driven to
`loop CLOSED`, certification included. The owner's "unattended" instruction is the
standing approval (2026-08-20): it is the explicit go-ahead the Never list requires for
certification-grade launches and the owner's word for the close — the run behaves as if
the owner approved each step, and reports every delegated decision at the end.
Everything else in this skill still applies per pass — audit, records, doc gate.
Consulting the written policy is not pre-answering a gate: the delegation is the owner's
standing decision, and any gate kind without one stops the run. There is no token or pass
limit on an unattended run — the owner removed them on purpose; do not invent one. The
run stops early only when it needs something no rule can supply: a fixer question only
the owner can answer, records that stay broken after one repair, or an unknown gate kind.

**Open the run.** Append `run-start` to the worklog. Announce in one line: target, state,
and that the run drives to close — certification included — reporting every decision at
the end.

**Each iteration:**

1. Audit + route as in step 1. Auditor red → one repair attempt; still red → end the run.
2. Router exit 0: append `pass-launch` as usual and execute the pass in a subagent
   (table below).
3. Router exit 2/3: run `node reviews/lib/autonomy-policy.mjs <target> decide
   <GATE_KIND>`. `ACTION: auto` → append `gate-parked` (`{kind, default, reason}`) and
   take the printed `NEXT`: a pass name is executed like a router answer (back to 2);
   `close the loop` executes section 6's close sequence — the standing approval is the
   owner's word it requires. `ACTION: stop` → end the run with the gate's question in
   the report.
4. No-progress guard: if the routed pass repeats the previous pass type and
   `metrics.jsonl` gained no line in between, end the run — a pass is not recording.
   This is a breakage detector, not a limit.
5. Router prints `loop CLOSED` → the run is done; close it out.

**Pass execution — always in subagents in this mode** (the driver only routes, records,
and reports; subagents return a summary of at most 20 lines, and state is re-read from
the records, never from the subagent's prose):

| Pass | How |
|---|---|
| full / delta discovery / certification | as section 3 — the workflow script already fans out; run synthesis + records per runbook-discovery (certification pair = two blinded passes per README note ²) |
| verification | run `node reviews/lib/verify-fixes.mjs <target>` yourself, then one subagent for the runbook's judgment items, its three per-cluster questions, and the records — given the script's JSON output, the resolution, and the fix diff |
| fix round | one subagent instructed to load the `/fix-review` skill and follow its **Unattended variant** section |

The session-model guard still applies: on a Fable session, discovery-scale launches
proceed resume-ready, and the workflow runId goes into the worklog event.

**Close the run.** Append `run-end` (`{passes, parked}`). Report in one message: each
pass with its one-line outcome, every parked item (kind, the default taken, what needs
the owner's ruling), and how the run ended (loop closed, or the question it stopped on).
This report is the batched owner sitting — each ruling made on it is recorded where that
round's rules say (resolution `Decisions`, ledger rows, the backlog).
```

- [ ] **Step 2: Update the SKILL.md frontmatter description and the Never list**

In the same file's frontmatter `description`, replace the fragment `stop at every owner gate, run it, and leave the records clean — one pass per invocation.` with `stop at every owner gate, run it, and leave the records clean — one pass per invocation, or drive the whole loop under the written policy when the owner says "unattended".`

In the `## Never` list, amend two bullets so they stay true:
- `Launch a certification-grade pass without an explicit go-ahead given in this invocation.` → append ` An unattended run's opening instruction is that go-ahead (standing approval 2026-08-20).`
- `Close a loop yourself — \`closed:\` goes into the ledger frontmatter only on the owner's word, …` → append to that bullet: ` An unattended run carries that word (standing approval 2026-08-20); the close is reported at run end.`

- [ ] **Step 3: Insert the README section**

In `reviews/README.md`, immediately after the "Standing instruction" paragraph, insert:

```markdown
### Unattended runs

*"Run the review loop unattended for `<target>`"* drives the whole remaining loop —
certification and close included — as if the owner approved each step:

- The instruction is a **standing approval** (owner decision 2026-08-20): it is the
  explicit go-ahead for certification-grade launches and the owner's word for the close,
  for that run. Outside an unattended run, both wait for the owner exactly as before.
- [lib/autonomy-policy.mjs](lib/autonomy-policy.mjs) is the written delegation. At every
  router gate the driver asks it; the answer is `auto` (take the written default,
  continue) or `stop`. A gate it does not know stops the run.
- Every delegated decision is **parked**: the driver takes the written default, appends a
  `gate-parked` worklog event, and lists every parked item in the run-end report for the
  owner's ruling.
- A run has no token or pass limit (owner decision 2026-08-20). It ends at `loop CLOSED`,
  a policy `stop`, a fixer question only the owner can answer, or the no-progress guard —
  a pass repeating without recording anything.
- Passes execute in subagents; the driver only routes, records, and reports. A killed run
  resumes by repeating the same phrase — the router reads state from the records alone.

The loop-driver skill owns the sequence; the fixer's side lives in the `/fix-review`
skill's unattended variant.
```

- [ ] **Step 4: Amend the standing-instruction paragraph and add the vocabulary entries**

In `reviews/README.md`'s "Standing instruction" paragraph, the sentence fragment
`**certification always waits for an explicit owner go-ahead**` gains ` — an unattended
run's opening instruction is that go-ahead (standing approval 2026-08-20, "Unattended
runs" below)`.

In `reviews/rules/doc-contracts.md`, in the Vocabulary list, after the **owner gate** entry, insert:

```markdown
- **unattended run** — one driver run driving the loop to close under the written policy
  (`lib/autonomy-policy.mjs`) and the owner's standing approval, stopping only for a
  question only the owner can answer, broken records, or the no-progress guard.
- **parked** — a gate decision taken by written default during an unattended run,
  awaiting the owner's ruling in the run-end report.
```

- [ ] **Step 5: Verify**

Run: `node reviews/lib/tests/run-tests.mjs`
Expected: PASS (doc-gate fixtures are unaffected by vocabulary additions; this run guards against accidental breakage).
Then re-read the three edited files once, checking: the SKILL.md section references only commands and slugs that exist after Tasks 1–3 (`decide`, `GATE_KIND`, `verify-fixes.mjs`), and the README links resolve.

- [ ] **Step 6: Commit**

```bash
git add .claude/skills/loop-driver/SKILL.md reviews/README.md reviews/rules/doc-contracts.md
git commit -m "feat(reviews): add the unattended run mode to the loop driver and its written policy to the README"
```

---

### Task 5: Fix-review skill — unattended variant

The fixer's stage 0b (one batched owner gate) becomes parking when the round runs inside an unattended run. Everything else in the fixer contract is untouched.

**Files:**
- Modify: `.claude/skills/fix-review/SKILL.md` (insert a new section after "### Stage 0c — Checks fly", before "### Per cluster")

**Interfaces:**
- Consumes: Task 4's `gate-parked` event shape.
- Produces: parked findings carry status `deferred` with a note starting `parked:` — Task 4's verification subagent affirms them as judgment items; the run-end report lists them.

- [ ] **Step 1: Insert the section**

Insert verbatim:

```markdown
### Unattended variant — a fix round inside an unattended run

Applies only when the driver's instruction says the round is unattended. Everything in
this skill still applies except stage 0b:

- **No owner gate.** Each triage-collected decision is parked instead: append
  `gate-parked` (`{kind: "fixer-decision", default, reason}`) to the worklog, take the
  conservative default, and record the parked question plus the default taken in this
  round's `Decisions`.
- **Conservative defaults.** A finding needing an owner ruling (a wont-fix intent, a
  capability removal, a scope question) is set `deferred` with a note starting `parked:`
  — never `wont-fix`, never silently fixed. A defect noticed outside the finding set is
  parked the same way in `Decisions`; no backlog row is minted, because routing it is
  the owner's ruling.
- **Blocker exception.** A decision that blocks a 🔴 fix ends the round: leave
  `status: in-progress`, append `round-end`, and hand the driver the question — the run
  stops with it.
- Hand-back is unchanged: renderer, auditor, doc gate, index row. `status: resolved` is
  legal with parked findings — `deferred` is a terminal status, and the run-end report
  carries every parked item to the owner.
```

- [ ] **Step 2: Update the frontmatter description**

In the same file's frontmatter `description`, replace `triage first, one batched owner gate, then clusters` with `triage first, one batched owner gate (parked instead when the run is unattended), then clusters`.

- [ ] **Step 3: Verify and commit**

Re-read the inserted section once against the fixer contract above it — it must contradict nothing outside stage 0b.

```bash
git add .claude/skills/fix-review/SKILL.md
git commit -m "feat(reviews): park fixer owner-gate decisions in unattended fix rounds"
```

---

### Task 6: Design-note sync

Reality changed; the standard that states it updates (CLAUDE.md hard rule).

**Files:**
- Modify: `reviews/notes/self-driving-loop-design.md`

**Interfaces:** none — prose only.

- [ ] **Step 1: Update the build-order table**

In the "Tools — status and build order" **to-build** table: row 2 (**Verification-pass script**) — replace its "Status and why" cell with `Built 2026-08-20 as lib/verify-fixes.mjs (revert-and-rerun mechanized; judgment items stay one hand-run diff each)`. Move the row into the "Built and operating" table with the same one-line description if that reads cleaner; either form is fine, one of them must happen.

- [ ] **Step 2: Update the completion audit**

In "Where this stands — completion audit": the row `Run without owner babysitting` changes from `partial` to `built (delegated gates)` with evidence `unattended mode 2026-08-20: every gate delegated under the standing approval (certification and close included), parked decisions, subagent passes, no limits by owner decision; re-invocation still manual (same phrase resumes)`. The row `Verify fixes` changes from `partial` to `built` with evidence `verify-fixes.mjs + one subagent per pass`.

- [ ] **Step 3: Update "What breaks first with zero owner presence"**

Item 1 (`Nothing re-invokes the loop`) gets one appended sentence: `Partly closed 2026-08-20: one unattended run now chains passes to a hard stop; between runs, re-invocation is still a human (or scheduled) "run the loop unattended" — auto-scheduling stays an owner opt-in.` Item 2 (`Verification is manual`) gets: `Closed 2026-08-20 by verify-fixes.mjs.`

- [ ] **Step 4: Commit**

```bash
git add reviews/notes/self-driving-loop-design.md
git commit -m "docs(reviews): record option B as built in the self-driving loop notes"
```

---

### Task 7: Pilot — one unattended run on a live target (OWNER-GATED)

The doc-gate/auditor pair has never processed a genuinely new flow end to end; the first unattended run will find friction the fixtures cannot. Run it with the owner watching.

**Files:** none created by hand — the run itself writes the records.

**Interfaces:**
- Consumes: everything above.
- Produces: the first `run-start` → `run-end` worklog span on a real target, and a friction list.

- [ ] **Step 1: Get the owner's explicit go-ahead**

Announce: `Pilot: run the review loop unattended for 038-039-invoicing — drives to certification and close (038-039 is full-loop tier; certification alone runs ~2.9–4.6M tokens), no limits, every decision parked and reported at the end. Go?` Do not proceed without the answer — this spends real tokens.

- [ ] **Step 2: Execute**

Invoke the loop-driver skill with: `run the review loop unattended for 038-039-invoicing`.

- [ ] **Step 3: Verify the run's records**

- `node reviews/lib/records-auditor.mjs 038-039-invoicing` — exits clean.
- `reviews/038-039-invoicing/worklog.jsonl` — one `run-start`, `pass-launch` events per pass, `gate-parked` events where gates were delegated, one `run-end`.
- The run-end report reached the owner with every parked item listed.
- `node reviews/lib/route-next-pass.mjs 038-039-invoicing` — the printed state matches what the report claimed; if the run reached the close, it prints `loop CLOSED` and the folder sits under `reviews/archive/`.

- [ ] **Step 4: Record friction**

Any friction (a gate kind the policy lacked, a verdict `verify-fixes.mjs` got wrong, a record the subagent missed) goes into the run-end report as a plain list — those are the next round of build work, not silent fixes.

---

## Self-review notes (checked at plan time)

- Spec coverage: option B = verification script (Task 3) + delegated-decision policy (Tasks 2, 4, 5) + unattended chaining (Task 4) + pilot (Task 7). The scheduler half is deliberately reduced to documented re-invocation (README, Task 4) — auto-scheduling is an owner opt-in later. Budgeting was in option B's spirit but the owner removed it explicitly (2026-08-20): no token or pass limits anywhere, and every gate — certification and close included — proceeds on the standing approval; the run stops only for an owner-only question, broken records, an unknown gate kind, or the no-progress guard.
- Consistency: `GATE_KIND` slugs (Task 1), worklog event shapes (Task 4), and verdict strings (Task 3) are each defined once and consumed by exact string elsewhere.
- Auditor safety: worklog events only require string `t` and `ev` (records-auditor.mjs line ~334); the renderer filters by known event names — new events are inert to both.
