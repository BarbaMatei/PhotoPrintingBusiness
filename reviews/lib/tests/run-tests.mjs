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
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs'
import { tmpdir } from 'node:os'
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
  check('auditor rejects a malformed verification findings[] entry', r.out.includes('908-verification-lineage metrics line 3 findings[') && r.out.includes('d must be') && r.out.includes('sev_delta malformed'), 'no shape error for the malformed lineage entry')
}
{
  const r = run('render-records.mjs', ['--root', GOOD_ROOT, '901-good-target', '--dry-run'])
  check('renderer buckets a backlog row as deferred', r.out.includes('"deferred": 1') && r.out.includes('"open": 0'), r.out.split('\n').filter(l => /"(deferred|open)"/.test(l)).join(' ').trim() || r.out.trim().slice(0, 160))
}

// ---------- render-records: frontmatter and worklog edge shapes ----------
// An in-progress round leaves fixed_commit empty, and a fixer may write pre_cleared as the list
// of ids rather than their count. Both once produced a wrong metrics line.
{
  const T = mkdtempSync(join(tmpdir(), 'render-records-'))
  const write = (target, name, body) => {
    mkdirSync(join(T, 'reviews', target), { recursive: true })
    writeFileSync(join(T, 'reviews', target, name), body)
  }
  const worklog = preCleared => [
    { t: '2026-08-21T10:00:00+03:00', ev: 'round-start', round: 1 },
    { t: '2026-08-21T10:05:00+03:00', ev: 'triage-done', round: 1, clusters: 1, pre_cleared: preCleared },
    { t: '2026-08-21T10:20:00+03:00', ev: 'test-run', kind: 'final', passed: 12, failed: 0 },
    { t: '2026-08-21T10:25:00+03:00', ev: 'round-end', round: 1 },
  ].map(e => JSON.stringify(e)).join('\n')
  const resolution = (status, fixedCommit) => `---
type: resolution
target: 920-open-round
version: 1
answers: review-v1.md
status: ${status}
fixed_commit:${fixedCommit ? ` ${fixedCommit}` : ''}
closed:
---

# Resolution v1 — 920-open-round

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9201 | fixed | \`abc1234\` | done |
`
  const review = `---
type: review
target: 920-open-round
version: 1
commit: abc1234
---

# Review v1
`

  write('920-open-round', 'review-v1.md', review)
  write('920-open-round', 'worklog.jsonl', worklog(['PPW-9201', 'PPW-9202', 'PPW-9203']))
  write('920-open-round', 'resolution-v1.md', resolution('in-progress', null))
  {
    const r = run('render-records.mjs', ['--root', T, '920-open-round', '--dry-run'])
    check('renderer exits 0 on an in-progress round', r.code === 0, `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check('renderer reads an empty fixed_commit as null', r.out.includes('"fixed_commit": null'),
      r.out.split('\n').find(l => l.includes('fixed_commit')) ?? r.out.trim().slice(0, 200))
    check('renderer never reads the next frontmatter key as the commit', !r.out.includes('closed:"'),
      r.out.split('\n').find(l => l.includes('fixed_commit')) ?? '')
    check('renderer counts pre_cleared given as a list of ids', r.out.includes('"pre_cleared_consumed": 3'),
      r.out.split('\n').find(l => l.includes('pre_cleared_consumed')) ?? '')
    check('renderer still reads the review commit as the base', r.out.includes('"base_commit": "abc1234"'),
      r.out.split('\n').find(l => l.includes('base_commit')) ?? '')
  }

  write('920-open-round', 'worklog.jsonl', worklog(2))
  write('920-open-round', 'resolution-v1.md', resolution('resolved', 'def5678'))
  {
    const r = run('render-records.mjs', ['--root', T, '920-open-round', '--dry-run'])
    check('renderer still reads a filled fixed_commit', r.out.includes('"fixed_commit": "def5678"'),
      r.out.split('\n').find(l => l.includes('fixed_commit')) ?? r.out.trim().slice(0, 200))
    check('renderer still counts pre_cleared given as a number', r.out.includes('"pre_cleared_consumed": 2'),
      r.out.split('\n').find(l => l.includes('pre_cleared_consumed')) ?? '')
  }

  rmSync(T, { recursive: true, force: true })
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '907-correction-target'])
  check("router surfaces the latest round's correction", r.out.includes('for fix round 1'), r.out.trim())
  check("router hides other rounds' and pass-keyed corrections behind a fix-round line", !r.out.includes('fix round 99') && !r.out.includes('new_findings'), r.out.trim())
}

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
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '913-loop-quiet', 'decide', 'certification-go-ahead'])
  check('policy answers a clean discovery loop-quiet gate with a first certification pair', r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: certification (pair)'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '912-recert', 'decide', 'certification-go-ahead'])
  check('policy answers a loop-quiet gate for a re-certified target with a single pass', r.code === 0 && r.out.includes('NEXT: certification (single)'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '914-resolution-above-review', 'decide', 'delta-worthiness'])
  check('policy judges the newest resolution, not the one paired with the newest review', r.code === 0 && r.out.includes('NEXT: certification (pair)'), r.out.trim())
  check('policy calls a round with no review file of its own patch-grade', r.out.includes('patch-grade'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '909-certified-target', 'decide', 'loop-close'])
  check('policy closes the loop under the standing approval', r.out.includes('ACTION: auto') && r.out.includes('NEXT: close the loop'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '909-certified-target', 'decide', 'mystery-gate'])
  check('policy fails closed on an unknown gate kind', r.out.includes('ACTION: stop'), r.out.trim())
}

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
  g('add', '.'); g('commit', '-qm', 'resolution')
  const redGreen = `node -e "process.exit(require('fs').readFileSync('src/app/calc.txt','utf8').includes('buggy')?1:0)"`

  const dry = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--dry-run'])
  check('verify-fixes dry-run derives the plan', dry.code === 0 && dry.out.includes('calc.txt') && dry.out.includes('PhotoPrint.Tests.Unit.CalcTests'), dry.out.trim())

  const live = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen])
  check('verify-fixes proves red-then-green and reports held', live.code === 0 && live.out.includes('"verdict":"held"') && live.out.includes('SUMMARY: 1/1 held'), live.out.trim())
  check('verify-fixes leaves the tree clean', g('status', '--porcelain').stdout.trim() === '', g('status', '--porcelain').stdout)
  check("verify-fixes warns when HEAD has moved past the resolution's fixed_commit",
    live.out.includes(`warning: HEAD is not the resolution's fixed_commit ${sha}`), live.out.trim())

  const neverRed = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', 'node -e "process.exit(0)"'])
  check('verify-fixes reopens a fix whose test never goes red', neverRed.code === 1 && neverRed.out.includes('"verdict":"test-never-red"'), neverRed.out.trim())

  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'dirty\n')
  const dirty = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen])
  check('verify-fixes refuses a dirty tree', dirty.code === 2, `exit ${dirty.code}: ${dirty.out.trim()}`)
  g('checkout', '--', '.')
  rmSync(T, { recursive: true, force: true })
}

if (failures.length) {
  console.log(`FAIL: ${failures.length} of ${count} assertion(s) failed:\n`)
  for (const f of failures) console.log(`  - ${f}`)
  process.exit(1)
}
console.log(`${count} assertions, all passed`)
