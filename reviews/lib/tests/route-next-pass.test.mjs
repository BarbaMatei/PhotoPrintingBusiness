// Tests for route-next-pass.mjs: fixture-state routing, gate kinds, and ledger-row parsing.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only route-next-pass
import { check, run, GOOD_ROOT } from './lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'

// ---------- route-next-pass: three fixture states ----------
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '903-closed-target'])
  check('router exits 0 on a closed target', r.code === 0, `exit ${r.code}`)
  check('router reports the closed loop as terminal', r.out.includes('STATE: loop CLOSED') && r.out.includes('ROUTER: terminal.'), r.out.trim())
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '901-good-target'])
  check('router exits 0 on a resolved resolution', r.code === 0, `exit ${r.code}`)
  check('router picks verification after a resolved resolution', r.out.includes('NEXT: verification'), r.out.trim())
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '904-clean-verification'])
  check('router exits 3 on a clean verification', r.code === 3, `exit ${r.code}`)
  check('router reports the clean verification and asks for the delta-worthiness call',
    r.out.includes('ROUTER: verification clean (0 reopened, 0 new serious).') && r.out.includes('GATE:'), r.out.trim())
}

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
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '913-loop-quiet'])
  check('router exits 2 on a clean discovery-type pass (row 6)', r.code === 2, `exit ${r.code}`)
  check('router names the certification-go-ahead gate kind', r.out.includes('GATE_KIND: certification-go-ahead'), r.out.trim())
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '914-resolution-above-review'])
  check('router picks verification when the resolved resolution outnumbers the newest review', r.code === 0 && r.out.includes('NEXT: verification'), `exit ${r.code}: ${r.out.trim()}`)
  check('router names the resolution it routed on', r.out.includes('resolution-v2 resolved'), r.out.trim())
}

// ---------- route-next-pass: a ledger row the router could not read ----------
// Silently skipping a row can only make the loop quieter, which is the wrong direction to fail in,
// so the count of id-shaped rows is compared against the rows that actually parsed.
{
  const T = mkdtempSync(join(tmpdir(), 'router-unparsed-row-'))
  const target = '951-unparsed-ledger-row'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  writeFileSync(join(dir, 'review-v1.md'), '---\ntype: review\ntarget: 951-unparsed-ledger-row\nversion: 1\ncommit: ddddde1\n---\n\n# Review v1\n')
  writeFileSync(join(dir, 'metrics.jsonl'), JSON.stringify({
    target, pass: 1, type: 'discovery', date: '2026-08-22', commit: 'ddddde1', verdict: 'approve-with-followups',
    // Every manifest lens listed: this fixture is about an unparsable ledger row, not lens debt.
    lenses: ['correctness', 'security', 'requirements', 'quality', 'tests-coverage', 'completeness-critic', 'db-parity', 'input-validation', 'observability', 'race', 'frontend-ux'],
    new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0,
  }) + '\n')
  writeFileSync(join(dir, 'ledger.md'), `---
type: review-ledger
target: ${target}
updated: 2026-08-22
---

# Ledger — ${target}

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9511 | 🔴 | v1 | A real row the router reads | \`Services/Fixture.cs:5\` | verified | \`ddddde2\` |
| PPW-9512 | — | v1 | A refuted row whose severity cell is a dash | \`Services/Fixture.cs:9\` | false-positive | \`ddddde2\` |
`)
  const r = run('route-next-pass.mjs', ['--root', T, target])
  const note = r.out.split('\n').find(l => l.startsWith('NOTE:')) ?? ''
  check('router notes a ledger row it could not parse, with the count', note.includes('1 of 2'), r.out.trim())
  check('the note says which way the gap fails', /quiet/.test(note), note || r.out.trim())
  check('an unparsed row does not stop the router routing', r.code === 2 && r.out.includes('GATE_KIND: certification-go-ahead'), `exit ${r.code}: ${r.out.trim()}`)
  rmSync(T, { recursive: true, force: true })
}

{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '907-correction-target'])
  check("router surfaces the latest round's correction", r.out.includes('for fix round 1'), r.out.trim())
  check("router hides other rounds' and pass-keyed corrections behind a fix-round line", !r.out.includes('fix round 99') && !r.out.includes('new_findings'), r.out.trim())
}

// ---------- convergence rule + lens-coverage debt (audit R5, 2026-08-28) ----------
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '915-lens-debt'])
  check('router refuses row 6 on lens-coverage debt and routes the owed lens', r.code === 0 && r.out.includes('NEXT: lens-coverage discovery (frontend-ux)'), `exit ${r.code}: ${r.out.trim()}`)
}

{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '916-unmeasured-seed'])
  check('router flags an unmeasured seed rate at the delta-worthiness gate', r.code === 3 && r.out.includes('seed rate is unmeasured'), `exit ${r.code}: ${r.out.trim()}`)
}

{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '917-non-convergent'])
  check('router declares a component non-convergent at s >= 0.3 on two consecutive rounds', r.code === 2 && r.out.includes('GATE_KIND: design-pass') && r.out.includes('"payments"'), `exit ${r.code}: ${r.out.trim()}`)
}

{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '918-design-capped'])
  check('router routes a fix round once the component used its one design pass', r.code === 0 && r.out.includes('NEXT: fix round') && r.out.includes('design pass per loop already ran'), `exit ${r.code}: ${r.out.trim()}`)
}
