// Tests for route-next-pass.mjs: fixture-state routing, gate kinds, and ledger-row parsing.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only route-next-pass
import { check, run, GOOD_ROOT, DRIVE_STATES } from '../lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'

// ---------- route-next-pass: three fixture states ----------
{
  const r = run('drive/route-next-pass.mjs', ['--root', DRIVE_STATES, '903-closed-target'])
  check('router exits 0 on a closed target', r.code === 0, `exit ${r.code}`)
  check('router reports the closed loop as terminal', r.out.includes('STATE: loop CLOSED') && r.out.includes('ROUTER: terminal.'), r.out.trim())
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '901-good-target'])
  check('router exits 0 on a resolved resolution', r.code === 0, `exit ${r.code}`)
  check('router picks verification after a resolved resolution', r.out.includes('NEXT: verification'), r.out.trim())
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', DRIVE_STATES, '904-clean-verification'])
  check('router exits 3 on a clean verification', r.code === 3, `exit ${r.code}`)
  check('router reports the clean verification and asks for the delta-worthiness call',
    r.out.includes('ROUTER: verification clean (0 reopened, 0 new serious).') && r.out.includes('GATE:'), r.out.trim())
}

// ---------- route-next-pass: gate kinds ----------
{
  const r = run('drive/route-next-pass.mjs', ['--root', DRIVE_STATES, '909-certified-target'])
  check('router exits 2 on a certified target with no pending fix round', r.code === 2, `exit ${r.code}`)
  check('router names the loop-close gate kind', r.out.includes('GATE_KIND: loop-close'), r.out.trim())
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', DRIVE_STATES, '904-clean-verification'])
  check('router names the delta-worthiness gate kind', r.out.includes('GATE_KIND: delta-worthiness'), r.out.trim())
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', DRIVE_STATES, '913-loop-quiet'])
  check('router exits 2 on a clean discovery-type pass (row 6)', r.code === 2, `exit ${r.code}`)
  check('router names the certification-go-ahead gate kind', r.out.includes('GATE_KIND: certification-go-ahead'), r.out.trim())
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', DRIVE_STATES, '914-resolution-above-review'])
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
  const r = run('drive/route-next-pass.mjs', ['--root', T, target])
  const note = r.out.split('\n').find(l => l.startsWith('NOTE:')) ?? ''
  check('router notes a ledger row it could not parse, with the count', note.includes('1 of 2'), r.out.trim())
  check('the note says which way the gap fails', /quiet/.test(note), note || r.out.trim())
  check('an unparsed row does not stop the router routing', r.code === 2 && r.out.includes('GATE_KIND: certification-go-ahead'), `exit ${r.code}: ${r.out.trim()}`)
  rmSync(T, { recursive: true, force: true })
}

// ---------- route-next-pass: a metrics line that is not a record ----------
// It is skipped, so it has to be named: a state assembled without a line nobody mentioned is a
// router answer that stops matching the file it was read from.
{
  const T = mkdtempSync(join(tmpdir(), 'router-non-record-line-'))
  const target = '957-non-record-metrics-line'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  writeFileSync(join(dir, 'review-v1.md'), `---\ntype: review\ntarget: ${target}\nversion: 1\ncommit: ddddde1\n---\n\n# Review v1\n`)
  writeFileSync(join(dir, 'metrics.jsonl'), `null\n${JSON.stringify({
    target, pass: 1, type: 'discovery', date: '2026-08-22', commit: 'ddddde1', verdict: 'approve-with-followups',
    lenses: ['correctness', 'security', 'requirements', 'quality', 'tests-coverage', 'completeness-critic', 'db-parity', 'input-validation', 'observability', 'race', 'frontend-ux'],
    new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0,
  })}\n`)
  const r = run('drive/route-next-pass.mjs', ['--root', T, target])
  check('router names a metrics line that is not a record, with its line number',
    r.out.includes('NOTE: metrics.jsonl line 1 is not a record (not a JSON object (null))'), r.out.trim())
  check('the router still routes on the lines that are records',
    r.code === 2 && r.out.includes('GATE_KIND: certification-go-ahead'), `exit ${r.code}: ${r.out.trim()}`)
  rmSync(T, { recursive: true, force: true })
}

{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '907-correction-target'])
  check("router surfaces the latest round's correction", r.out.includes('for fix round 1'), r.out.trim())
  check("router hides other rounds' and pass-keyed corrections behind a fix-round line", !r.out.includes('fix round 99') && !r.out.includes('new_findings'), r.out.trim())
}

// ---------- convergence rule + lens-coverage debt (audit R5, 2026-08-28) ----------
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '925-lens-debt'])
  check('router refuses row 6 on lens-coverage debt and routes the owed lens', r.code === 0 && r.out.includes('NEXT: lens-coverage discovery (frontend-ux)'), `exit ${r.code}: ${r.out.trim()}`)
}

{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '926-unmeasured-seed'])
  check('router flags an unmeasured seed rate at the delta-worthiness gate', r.code === 3 && r.out.includes('seed rate is unmeasured'), `exit ${r.code}: ${r.out.trim()}`)
}

{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '927-non-convergent'])
  check('router declares a component non-convergent at s >= 0.3 on two consecutive rounds', r.code === 2 && r.out.includes('GATE_KIND: design-pass') && r.out.includes('"payments"'), `exit ${r.code}: ${r.out.trim()}`)
}

{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '928-design-capped'])
  check('router routes a fix round once the component used its one design pass', r.code === 0 && r.out.includes('NEXT: fix round') && r.out.includes('design pass per loop already ran'), `exit ${r.code}: ${r.out.trim()}`)
}

// ---------- the brake guards EVERY fix-round answer (owner ruling 1, 2026-08-28) ----------
// The brake used to live inside the router's fix-round helper, which the armed row, the batch row
// and the pre-certification sweep row never went through: a non-convergent component kept being
// patched by whichever row answered first. All three now consult it, and a convergent state on the
// same three rows must still route its fix round.
for (const [target, row, reason] of [
  ['929-armed-non-convergent', 'armed', 'the loop is armed — 1 open 🔴 in the ledger (PPW-9291)'],
  ['930-batch-non-convergent', 'batch', 'batch of 3 open mediums'],
  ['931-sweep-non-convergent', 'sweep', 'sweep before certification — 1 open medium must drain'],
]) {
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, target])
  check(`the ${row} row brakes on a non-convergent component instead of routing a fix round`,
    r.code === 2 && r.out.includes('GATE_KIND: design-pass') && !r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check(`the braked ${row} row still states the row it matched and names the component and both rounds`,
    r.out.includes(reason) && r.out.includes('"payments"') && r.out.includes('rounds r1 and r2'), r.out.trim())
  // A GATE line is read on its own at the owner gate, so it names the work it refuses.
  const gate = r.out.split('\n').find(l => l.startsWith('GATE:')) ?? ''
  check(`the ${row} row's gate line names the trigger it refuses, not only the design pass`,
    gate.includes('The fix round it refuses was triggered by:') && gate.includes(reason.replace('ROUTER: ', '')), gate || r.out.trim())
}
for (const [target, row] of [['918-open-blocker', 'armed'], ['916-medium-batch', 'batch'], ['917-sweep-before-cert', 'sweep']]) {
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, target])
  check(`a convergent ${row} row still routes its fix round`,
    r.code === 0 && r.out.includes('NEXT: fix round') && !r.out.includes('GATE_KIND: design-pass'), `exit ${r.code}: ${r.out.trim()}`)
}

// ---------- the rows as data: the walk's seam ----------
{
  const { ROWS, COST, walkableRows } = await import('../../drive/rows.mjs')
  const { GATES, GATE_DOCS, POLICY_NEXT, NEXT } = await import('../../drive/gates.mjs')
  const kinds = ['row', 'guard', 'note', 'prose']
  check('every row carries the README cells', ROWS.every(r => r.name && r.state && r.next),
    ROWS.filter(r => !(r.name && r.state && r.next)).map(r => r.name).join(', '))
  check('row names are unique', new Set(ROWS.map(r => r.name)).size === ROWS.length)
  check('only the four implementation kinds exist', ROWS.every(r => kinds.includes(r.impl)),
    ROWS.filter(r => !kinds.includes(r.impl)).map(r => `${r.name}: ${r.impl}`).join(', '))
  check('a walked row has both a predicate and an answer',
    walkableRows().every(r => typeof r.when === 'function' && typeof r.answer === 'function'),
    walkableRows().filter(r => typeof r.when !== 'function' || typeof r.answer !== 'function').map(r => r.name).join(', '))
  check('a row that is not walked carries no predicate',
    ROWS.filter(r => r.impl !== 'row').every(r => !r.when && !r.answer),
    ROWS.filter(r => r.impl !== 'row' && (r.when || r.answer)).map(r => r.name).join(', '))
  // The walk is first-match-wins, so the order is the contract.
  const walked = walkableRows().map(r => r.name)
  check('the unit-records row is walked first', walked[0] === 'unit-records-pending', walked.join(' → '))
  check('the certification row is walked last', walked.at(-1) === 'certification', walked.join(' → '))
  check('the armed row outranks the batch and queued rows',
    walked.indexOf('armed') < walked.indexOf('batch') && walked.indexOf('batch') < walked.indexOf('queued'), walked.join(' → '))
  check('the design-pass row stays a guard — no row may outrank the convergence brake',
    ROWS.find(r => r.name === 'design-pass').impl === 'guard')
  check('every gate a row names is in the gate enum',
    ROWS.filter(r => r.gate).every(r => Object.values(GATES).includes(r.gate)),
    ROWS.filter(r => r.gate && !Object.values(GATES).includes(r.gate)).map(r => r.name).join(', '))
  check('every gate kind is documented exactly once',
    GATE_DOCS.length === Object.keys(GATES).length && new Set(GATE_DOCS.map(g => g.kind)).size === GATE_DOCS.length,
    `${GATE_DOCS.length} docs rows vs ${Object.keys(GATES).length} kinds`)
  check('only the gates with a written delegation are marked delegated',
    GATE_DOCS.filter(g => g.delegated).map(g => g.kind).join(',') === [GATES.loopClose, GATES.deltaWorthiness, GATES.certificationGoAhead].join(','),
    GATE_DOCS.filter(g => g.delegated).map(g => g.kind).join(','))
  check('the policy vocabulary is the NEXT map', POLICY_NEXT.length === 6
    && POLICY_NEXT.includes(NEXT.fixRound) && POLICY_NEXT.includes(NEXT.closeLoop), POLICY_NEXT.join(' · '))
  // finish() prefix-matches its NEXT text against COST, so every key must start a real answer.
  const answers = [...POLICY_NEXT, 'full discovery', 'verification (reviewed unit — render records once, after it)']
  check('every cost key is the prefix of an answer the loop can print',
    Object.keys(COST).every(k => answers.some(n => n.toLowerCase().startsWith(k))),
    Object.keys(COST).filter(k => !answers.some(n => n.toLowerCase().startsWith(k))).join(' · '))
}
