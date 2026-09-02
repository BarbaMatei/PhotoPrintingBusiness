// Tests for doc-gate.mjs: target-mode doc-contract linting and state-file linting.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only doc-gate
import { check, run, firstLine, GOOD_ROOT, BAD_STATE_ROOT } from '../lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'

// ---------- doc-gate: the conforming target is clean ----------
{
  const r = run('records/doc-gate.mjs', ['--root', GOOD_ROOT, '901-good-target', '1'])
  check('doc-gate exits 0 on the conforming target', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('doc-gate reports the conforming target clean, state checks included', r.out.includes('clean for 901-good-target v1 + state'), firstLine(r.out))
}

// ---------- doc-gate: every planted violation on the broken target ----------
{
  const r = run('records/doc-gate.mjs', ['--root', GOOD_ROOT, '902-broken-target', '1'])
  check('doc-gate exits 1 on the broken target', r.code === 1, `exit ${r.code}`)
  const expected = [
    'frontmatter missing "lenses-not-run:"',
    'pass-type verification — verification passes write no review file',
    'heading 2 is "## Findings (2)"',
    'finding row key "F3"',
    'severity cell is "High"',
    'findings files are retired',
    'banned severity synonym "critical"',
    'commit ddddddd differs from review-v1.md\'s aaaaaaa',
    'PPW-9102 status "verified"',
    'PPW-9103 note is 293 chars — cap is 240',
    'detail block PPW-9103 is 21 lines — cap is 20',
    'PPW-9102 Status cell is "fixed at v1, see the history"',
    'review blocker PPW-9101 has no entry in the findings map',
    'PPW-9104 status deferred has no Decisions block — every non-fixed status needs its rationale (doc-contracts.md)',
  ]
  for (const e of expected) check(`doc-gate reports: ${e}`, r.out.includes(e), 'not in the gate output')
}

// ---------- doc-gate: Check A's Decisions-heading match is scoped to "## Decisions" only ----------
// A stray "### ... (PPW-n)" heading anywhere else in the body (e.g. under Findings) must not
// satisfy the rule, and must not count against the per-decision 15-line cap either.
{
  const T = mkdtempSync(join(tmpdir(), 'doc-gate-decisions-scope-'))
  const target = '940-decisions-scope'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  const filler = Array.from({ length: 17 }, (_, i) => `- filler line ${i + 1}`).join('\n')
  const resolution = (pass, decisionsBody) => `---
type: resolution
target: ${target}
version: ${pass}
answers: review-v${pass}.md
status: resolved
fixed_commit: aaaaaaa
---

# Resolution v${pass} — ${target}

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9500 | deferred | — | Queued behind other work. |

### A stray heading that must not count (PPW-9500)

${filler}

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A | PPW-9500 | \`Services/Fixture.cs\` | not needed (fixture) |

## Decisions

${decisionsBody}
`
  writeFileSync(join(dir, 'resolution-v1.md'), resolution(1, '### Some unrelated call (PPW-9999)\n\nNothing about PPW-9500 here.'))
  const bad = run('records/doc-gate.mjs', ['--root', T, target, '1'])
  check('doc-gate fires when the only matching heading sits outside Decisions',
    bad.code === 1 && bad.out.includes('PPW-9500 status deferred has no Decisions block — every non-fixed status needs its rationale (doc-contracts.md)'),
    `exit ${bad.code}: ${bad.out.trim()}`)
  check('the stray 17-line heading outside Decisions is not counted against the 15-line cap',
    !bad.out.includes('cap is 15'), bad.out.trim())

  writeFileSync(join(dir, 'resolution-v2.md'), resolution(2, '### Queued until the rewrite lands (PPW-9500)\n\nNot a defect this round can close.'))
  const good = run('records/doc-gate.mjs', ['--root', T, target, '2'])
  check('doc-gate is clean once the real Decisions section names the id, decoy heading notwithstanding',
    good.code === 0, `exit ${good.code}: ${good.out.trim()}`)

  rmSync(T, { recursive: true, force: true })
}

// ---------- doc-gate target mode: state files lint in the same run, keyed to the target ----------
{
  const r = run('records/doc-gate.mjs', ['--root', BAD_STATE_ROOT, '901-good-target', '1'])
  check('doc-gate exits 1 when the state files it also lints in target mode are broken', r.code === 1, `exit ${r.code}`)
  check('doc-gate labels the combined run "<target> v<pass> + state"', r.out.includes('violation(s) for 901-good-target v1 + state'), firstLine(r.out))
  const expected = [
    'PPW-9999 is not in 901-good-target\'s ledger',
    'sha `abcdef1` does not resolve',
    'key "BUG-2" — PPW-<n> only',
  ]
  for (const e of expected) check(`doc-gate target mode reports the state violation: ${e}`, r.out.includes(e), 'not in the gate output')
}

// ---------- doc-gate target mode: silently skips the state half when reviews/state is absent ----------
{
  const T = mkdtempSync(join(tmpdir(), 'doc-gate-no-state-'))
  mkdirSync(join(T, 'reviews', '999-no-state-dir'), { recursive: true })
  writeFileSync(join(T, 'reviews', '999-no-state-dir', 'ledger.md'),
    '---\ntype: review-ledger\ntarget: 999-no-state-dir\nupdated: 2026-08-22\n---\n\n# Ledger — 999-no-state-dir\n\n## Findings\n\n| ID | Sev | First seen | Title | File | Status | Affirmed |\n|---|---|---|---|---|---|---|\n')
  const r = run('records/doc-gate.mjs', ['--root', T, '999-no-state-dir', '1'])
  check('doc-gate exits 0 with no reviews/state dir present', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('doc-gate skips the state half silently, no "+ state" suffix and no state/ labels',
    r.out.includes('clean for 999-no-state-dir v1') && !r.out.includes('+ state') && !r.out.includes('state/'), r.out.trim())
  rmSync(T, { recursive: true, force: true })
}

// ---------- doc-gate state mode ----------
{
  const r = run('records/doc-gate.mjs', ['--root', GOOD_ROOT, 'state'])
  check('doc-gate state exits 0 on the good state fixtures', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('doc-gate state reports the good state fixtures clean', r.out.includes('clean for the state files'), firstLine(r.out))
}
{
  const r = run('records/doc-gate.mjs', ['--root', BAD_STATE_ROOT, 'state'])
  check('doc-gate state exits 1 on the bad state fixtures', r.code === 1, `exit ${r.code}`)
  const expected = [
    '4 cells — a row has exactly 5',
    'key "BUG-2" — PPW-<n> only',
    'PPW-9302: severity cell is "High"',
    'PPW-9303: What cell is empty',
    'PPW-9304: Area "`storage/gallery`"',
    'PPW-9305: What cell spans more than one line',
    'State cell is 6 lines — cap is 5',
    'New H/M/L/C cell is "0/0/0"',
    'target "999" is not a target folder key',
    'description is 56 words — cap is 50',
    '6 cells — a pass row has 5, or 7 when Outcome and Files apply',
  ]
  for (const e of expected) check(`doc-gate state reports: ${e}`, r.out.includes(e), 'not in the gate output')
}

// ---------- doc-gate: the V4 resolution shape (audit R1/R2, rounds closed >= 2026-08-28) ----------
{
  const r = run('records/doc-gate.mjs', ['--root', GOOD_ROOT, '923-newshape', '1'])
  check('doc-gate accepts a post-cutoff resolution with a Protocol column and a quantified protocol block', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
}

{
  const r = run('records/doc-gate.mjs', ['--root', GOOD_ROOT, '924-oldshape', '1'])
  check('doc-gate refuses the retired Approach-check column after the cut-off', r.code === 1 && r.out.includes('Approach-check column — retired 2026-08-28'), r.out.trim())
  check('doc-gate requires the Protocol column after the cut-off', r.out.includes('no Protocol column'), r.out.trim())
  check('doc-gate refuses a protocol block with no quantified invariant', r.out.includes('protocol block "vague" states no quantified invariant'), r.out.trim())
}
