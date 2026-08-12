#!/usr/bin/env node
// Suite for the gate machinery: doc-gate (target mode + state mode), route-next-pass, and one
// smoke run of the records auditor against the real repo. No framework — plain asserts over
// child-process runs, so a git hook can run it. Fixtures are a miniature fake repo under
// tests/fixtures: `repo` holds one conforming target, one deliberately broken one, two router
// states and the good state files; `bad-state` holds the broken state files.
//
// Usage: node reviews/lib/tests/run-tests.mjs
// Exit: 0 all assertions passed · 1 one or more failed.
import { spawnSync } from 'node:child_process'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { REVIEWS, REPO } from '../paths.mjs'

const FIXTURES = join(dirname(fileURLToPath(import.meta.url)), 'fixtures')
const GOOD_ROOT = join(FIXTURES, 'repo')
const BAD_STATE_ROOT = join(FIXTURES, 'bad-state')

let count = 0
const failures = []
function check(name, ok, detail) {
  count++
  if (!ok) failures.push(detail ? `${name}\n      ${detail}` : name)
}
function run(script, args) {
  const r = spawnSync(process.execPath, [join(REVIEWS, 'lib', script), ...args], { encoding: 'utf8', cwd: REPO })
  if (r.error) return { code: -1, out: String(r.error) }
  return { code: r.status, out: `${r.stdout ?? ''}${r.stderr ?? ''}` }
}
const firstLine = out => out.split('\n')[0]

// ---------- doc-gate: the conforming target is clean ----------
{
  const r = run('doc-gate.mjs', ['--root', GOOD_ROOT, '901-good-target', '1'])
  check('doc-gate exits 0 on the conforming target', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('doc-gate reports the conforming target clean', r.out.includes('clean for 901-good-target v1'), firstLine(r.out))
}

// ---------- doc-gate: every planted violation on the broken target ----------
{
  const r = run('doc-gate.mjs', ['--root', GOOD_ROOT, '902-broken-target', '1'])
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
  ]
  for (const e of expected) check(`doc-gate reports: ${e}`, r.out.includes(e), 'not in the gate output')
}

// ---------- doc-gate state mode ----------
{
  const r = run('doc-gate.mjs', ['--root', GOOD_ROOT, 'state'])
  check('doc-gate state exits 0 on the good state fixtures', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('doc-gate state reports the good state fixtures clean', r.out.includes('clean for the state files'), firstLine(r.out))
}
{
  const r = run('doc-gate.mjs', ['--root', BAD_STATE_ROOT, 'state'])
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

// ---------- records auditor: smoke run against the real repo ----------
{
  const r = run('records-auditor.mjs', ['044-045-observability'])
  check('auditor exits 0 on 044-045-observability', r.code === 0, `exit ${r.code}: ${r.out.trim().split('\n').slice(-3).join(' | ')}`)
}
{
  const r = run('records-auditor.mjs', ['043-cloud-storage-provider'])
  check('auditor finds the track record for a certified target', !r.out.includes('track-record.md is missing'), r.out.split('\n').find(l => l.includes('track-record')) ?? firstLine(r.out))
}
{
  const r = run('records-auditor.mjs', ['--root', GOOD_ROOT])
  check('auditor reports a cross-target duplicate id', r.out.includes('duplicate id PPW-9001'), 'no duplicate-id error in the output')
  check('auditor accepts a well-formed verification findings[] with lineage', !r.out.includes('908-verification-lineage metrics line 2 findings['), r.out.split('\n').find(l => l.includes('line 2 findings[')) ?? '')
  check('auditor rejects a malformed verification findings[] entry', r.out.includes('908-verification-lineage metrics line 3 findings[') && r.out.includes('d must be'), 'no shape error for the malformed lineage entry')
}
{
  const r = run('render-records.mjs', ['--root', GOOD_ROOT, '901-good-target', '--dry-run'])
  check('renderer buckets a backlog row as deferred', r.out.includes('"deferred": 1') && r.out.includes('"open": 0'), r.out.split('\n').filter(l => /"(deferred|open)"/.test(l)).join(' ').trim() || r.out.trim().slice(0, 160))
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '907-correction-target'])
  check("router surfaces the latest round's correction", r.out.includes('for fix round 1'), r.out.trim())
  check("router hides other rounds' and pass-keyed corrections behind a fix-round line", !r.out.includes('fix round 99') && !r.out.includes('new_findings'), r.out.trim())
}

if (failures.length) {
  console.log(`FAIL: ${failures.length} of ${count} assertion(s) failed:\n`)
  for (const f of failures) console.log(`  - ${f}`)
  process.exit(1)
}
console.log(`${count} assertions, all passed`)
