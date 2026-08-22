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
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs'
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

// ---------- render-records: void events, paired spans, render-at-resolved ----------
// The single-slice reader (first round-start with the number → last round-end) over-counted a
// round whose number appeared twice by ~5x and charged a multi-part round for the records and
// gate time between its parts.
{
  const T = mkdtempSync(join(tmpdir(), 'render-spans-'))
  const target = '921-spans'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  const wl = events => writeFileSync(join(dir, 'worklog.jsonl'), `${events.map(e => JSON.stringify(e)).join('\n')}\n`)
  const resolution = (n, status, fixedCommit) => `---
type: resolution
target: ${target}
version: ${n}
answers: review-v${n}.md
status: ${status}
fixed_commit:${fixedCommit ? ` ${fixedCommit}` : ''}
closed:
---

# Resolution v${n} — ${target}

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-92${n}1 | fixed | \`abc1234\` | done |
`
  for (const n of [1, 4, 6, 7, 8]) writeFileSync(join(dir, `resolution-v${n}.md`), resolution(n, 'resolved', 'abc1234'))
  const at = hhmm => `2026-08-21T${hhmm}:00+03:00`
  const line = (out, key) => out.split('\n').find(l => l.includes(`"${key}"`)) ?? out.trim().slice(0, 200)

  // A round-start stamped with the wrong round number, voided and re-stamped: the void names
  // "round", so it must leave the corrected same-timestamp stamp alone.
  const mislabel = [
    { t: at('10:15'), ev: 'round-start', round: 8 },
    { t: at('10:15'), ev: 'round-start', round: 7 },
    { t: at('10:44'), ev: 'round-end', round: 7 },
    { t: at('11:36'), ev: 'round-start', round: 8 },
    { t: at('12:00'), ev: 'round-end', round: 8 },
  ]
  const voidEvent = { t: at('12:01'), ev: 'void', of: { ev: 'round-start', t: at('10:15'), round: 8 } }

  wl([...mislabel, voidEvent])
  {
    const r = run('render-records.mjs', ['--root', T, target, '--round', '8', '--dry-run'])
    check('renderer counts only the surviving span of a voided mislabel', r.code === 0 && r.out.includes('"active_s": 1440'),
      `exit ${r.code}: ${line(r.out, 'active_s')}`)
    check('renderer brackets the round by its span, not by the voided stamp',
      r.out.includes(`"started": "${at('11:36')}"`) && r.out.includes(`"ended": "${at('12:00')}"`), `${line(r.out, 'started')} ${line(r.out, 'ended')}`)
  }
  {
    const r = run('render-records.mjs', ['--root', T, target, '--round', '7', '--dry-run'])
    check('a void naming round 8 leaves the same-timestamp round-7 stamp countable',
      r.code === 0 && r.out.includes('"active_s": 1740') && r.out.includes(`"started": "${at('10:15')}"`),
      `exit ${r.code}: ${line(r.out, 'active_s')} ${line(r.out, 'started')}`)
  }

  wl(mislabel.filter(e => !(e.ev === 'round-start' && e.round === 7)))
  {
    const r = run('render-records.mjs', ['--root', T, target, '--round', '8', '--dry-run'])
    check('renderer aborts on the unvoided mislabel instead of spanning both parts', r.code === 1, `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check('the abort names the unclosed start, the foreign stamp that follows it, and both repairs',
      r.out.includes(at('10:15')) && r.out.includes(at('10:44')) && r.out.includes('round-end --round 8') && r.out.includes('void'), r.out.trim())
  }

  wl([
    { t: at('10:15'), ev: 'round-start', round: 8 },
    { t: at('11:36'), ev: 'round-start', round: 8 },
    { t: at('12:00'), ev: 'round-end', round: 8 },
  ])
  {
    const r = run('render-records.mjs', ['--root', T, target, '--round', '8', '--dry-run'])
    check('renderer aborts on a second round-start while the round is open', r.code === 1, `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check('the abort names both round-start timestamps and suggests a void',
      r.out.includes(at('10:15')) && r.out.includes(at('11:36')) && r.out.includes('wl.mjs'), r.out.trim())
  }

  wl([
    { t: at('10:00'), ev: 'round-start', round: 8 },
    { t: at('10:20'), ev: 'round-end', round: 8 },
    { t: at('10:30'), ev: 'round-end', round: 8 },
  ])
  {
    const r = run('render-records.mjs', ['--root', T, target, '--round', '8', '--dry-run'])
    check('renderer aborts on a round-end that closes nothing', r.code === 1 && r.out.includes(at('10:30')) && r.out.includes(at('10:20')), `exit ${r.code}: ${r.out.trim()}`)
    check('that abort also offers the resumed-without-a-start repair, not just the void',
      r.out.includes('resumed') && r.out.includes('round-start'), r.out.trim())
  }

  // A round left open while another round runs is a missing round-end, not work in progress: the
  // single-slice reader charged the second round's events to the first.
  wl([
    { t: at('09:00'), ev: 'round-start', round: 1 },
    { t: at('09:10'), ev: 'test-run', kind: 'red' },
    { t: at('11:00'), ev: 'round-start', round: 2 },
    { t: at('11:10'), ev: 'test-run', kind: 'green' },
    { t: at('11:30'), ev: 'round-end', round: 2 },
  ])
  {
    const r = run('render-records.mjs', ['--root', T, target, '--round', '1', '--dry-run'])
    check('renderer aborts when a later round runs inside an unclosed round', r.code === 1 && !r.out.includes('"invocations"'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check('that abort names the unclosed start and the first foreign round stamp',
      r.out.includes(at('09:00')) && r.out.includes(at('11:00')), r.out.trim())
  }

  wl([
    { t: at('09:00'), ev: 'round-start', round: 1 },
    { t: at('09:10'), ev: 'test-run', kind: 'red' },
  ])
  {
    const r = run('render-records.mjs', ['--root', T, target, '--round', '1', '--dry-run'])
    check('a round still open at the end of the log renders in progress', r.code === 0 && r.out.includes('no round-end yet'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check('the in-progress render ends at the last event',
      r.out.includes(`"started": "${at('09:00')}"`) && r.out.includes(`"ended": "${at('09:10')}"`), `${line(r.out, 'started')} ${line(r.out, 'ended')}`)
  }

  wl([
    { t: at('09:00'), ev: 'round-start', round: 6 },
    { t: at('09:10'), ev: 'test-run', kind: 'red' },
    { t: at('09:20'), ev: 'round-end', round: 6 },
    { t: at('09:50'), ev: 'test-run', kind: 'green' },
    { t: at('10:30'), ev: 'round-start', round: 6 },
    { t: at('10:40'), ev: 'test-run', kind: 'green' },
    { t: at('10:50'), ev: 'round-end', round: 6 },
    { t: at('14:00'), ev: 'round-start', round: 6 },
    { t: at('14:05'), ev: 'test-run', kind: 'final', passed: 9, failed: 0 },
    { t: at('14:20'), ev: 'round-end', round: 6 },
  ])
  {
    const r = run('render-records.mjs', ['--root', T, target, '--round', '6', '--dry-run'])
    check('renderer sums a three-part round\'s spans and skips the time between them',
      r.code === 0 && r.out.includes('"active_s": 3600') && r.out.includes('"idle_s": 0'), `exit ${r.code}: ${line(r.out, 'active_s')} ${line(r.out, 'idle_s')}`)
    check('a multi-part round is bracketed by its first and last span',
      r.out.includes(`"started": "${at('09:00')}"`) && r.out.includes(`"ended": "${at('14:20')}"`), `${line(r.out, 'started')} ${line(r.out, 'ended')}`)
    check('renderer counts only the events inside spans',
      r.out.includes('"invocations": 3') && r.out.includes('"green_runs": 1'), `${line(r.out, 'invocations')} ${line(r.out, 'green_runs')}`)
  }

  // Two stamps can share a timestamp, so a broad "of" erases more than the one event meant.
  wl([
    { t: at('09:00'), ev: 'round-start', round: 4 },
    { t: at('09:10'), ev: 'test-run', kind: 'red' },
    { t: at('09:10'), ev: 'test-run', kind: 'red' },
    { t: at('09:20'), ev: 'round-end', round: 4 },
    { t: at('09:30'), ev: 'void', of: { ev: 'test-run', t: at('09:10') } },
  ])
  {
    const r = run('render-records.mjs', ['--root', T, target, '--round', '4', '--dry-run'])
    check('renderer warns when one void matches more than one event', r.code === 0 && r.out.includes('matches 2 events'), `exit ${r.code}: ${r.out.split('\n').find(l => l.includes('void')) ?? r.out.trim().slice(0, 200)}`)
    check('every event the void matched is dropped', r.out.includes('"invocations": 0'), line(r.out, 'invocations'))
  }

  writeFileSync(join(dir, 'resolution-v5.md'), resolution(5, 'in-progress', null))
  wl([
    { t: at('08:00'), ev: 'round-start', round: 5 },
    { t: at('08:20'), ev: 'round-end', round: 5 },
  ])
  const metricsPath = join(dir, 'metrics.jsonl')
  {
    const r = run('render-records.mjs', ['--root', T, target, '--round', '5', '--no-index'])
    check('renderer refuses to append for a round that is not resolved', r.code === 1 && r.out.includes('--in-progress'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check('the refused append wrote no metrics line', !existsSync(metricsPath), 'metrics.jsonl exists')
  }
  {
    const r = run('render-records.mjs', ['--root', T, target, '--round', '5', '--dry-run'])
    check('renderer still dry-runs a round that is not resolved', r.code === 0, `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
  }
  {
    const r = run('render-records.mjs', ['--root', T, target, '--round', '5', '--in-progress', '--no-index'])
    const written = existsSync(metricsPath) ? readFileSync(metricsPath, 'utf8').trim().split('\n') : []
    check('--in-progress appends the line anyway', r.code === 0 && written.length === 1, `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check('the appended line is this round\'s fix-round line', written.length === 1 && JSON.parse(written[0]).type === 'fix-round' && JSON.parse(written[0]).round === 5, written.join(' | '))
  }

  wl([...mislabel, voidEvent])
  {
    const before = readFileSync(metricsPath, 'utf8').trim().split('\n').length
    const r = run('render-records.mjs', ['--root', T, target, '--round', '8', '--no-index'])
    const written = readFileSync(metricsPath, 'utf8').trim().split('\n')
    check('renderer appends the line at hand-back for a resolved round with no flag',
      r.code === 0 && written.length === before + 1, `exit ${r.code}: ${written.length} line(s), was ${before}: ${r.out.trim().slice(0, 200)}`)
    const appended = written.length ? JSON.parse(written[written.length - 1]) : {}
    check('the hand-back line carries this round\'s number and span-derived runtime',
      appended.type === 'fix-round' && appended.round === 8 && appended.runtime?.active_s === 1440, JSON.stringify(appended.runtime ?? appended))
    const again = run('render-records.mjs', ['--root', T, target, '--round', '8', '--no-index'])
    check('a second hand-back render refuses rather than duplicating the line',
      again.code === 1 && readFileSync(metricsPath, 'utf8').trim().split('\n').length === written.length, `exit ${again.code}: ${again.out.trim().slice(0, 200)}`)
  }

  rmSync(T, { recursive: true, force: true })
}
// ---------- render-records: index rows, ledger flips, verification mode ----------
// The renderer wrote only metrics.jsonl and printed a suggestion line — the index row and the
// ledger status flips were hand-copied, which is where rows went missing or contradicted.
{
  const T = mkdtempSync(join(tmpdir(), 'render-index-'))
  const target = '938-index-rows'
  const dir = join(T, 'reviews', target)
  const stateDir = join(T, 'reviews', 'state')
  mkdirSync(dir, { recursive: true })
  mkdirSync(stateDir, { recursive: true })
  const indexPath = join(stateDir, 'index.md')
  const ledgerPath = join(dir, 'ledger.md')
  const metricsPath = join(dir, 'metrics.jsonl')
  const wlPath = join(dir, 'worklog.jsonl')
  const read = p => existsSync(p) ? readFileSync(p, 'utf8') : ''
  const metricsLines = () => read(metricsPath).split('\n').filter(l => l.trim())
  const at = hhmm => `2026-08-21T${hhmm}:00+03:00`
  const wl = events => writeFileSync(wlPath, `${events.map(e => JSON.stringify(e)).join('\n')}\n`)

  writeFileSync(join(dir, 'review-v1.md'), '---\ntype: review\ntarget: 938-index-rows\nversion: 1\ncommit: abc1234\n---\n\n# Review v1\n')
  writeFileSync(join(dir, 'resolution-v1.md'), `---
type: resolution
target: ${target}
version: 1
answers: review-v1.md
status: resolved
fixed_commit: def5678
closed: 2026-08-21
---

# Resolution v1 — ${target}

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9381 | fixed | \`abc1234\` | rethrows the last error |
| PPW-9382 | deferred | — | queued behind the log-floor rewrite |
| PPW-9389 | fixed | \`abc1234\` | row the ledger never got |
`)
  const LEDGER_ROW_1 = '| PPW-9381 | 🔴 | v1 | The retry loop drops the last error | `Services/Retry.cs:12` | in-progress | `0000000` |'
  const LEDGER_ROW_2 = '| PPW-9382 | 🟠 | v1 | The sweep logs below the level floor | `Jobs/Sweep.cs:30` | open | `0000000` |'
  const HIST_1 = '  - v1: found by the correctness lens'
  const HIST_2 = '  - v1: found by the observability lens'
  writeFileSync(ledgerPath, `---
type: review-ledger
target: ${target}
updated: 2026-08-21
---

# Ledger — ${target}

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
${LEDGER_ROW_1}
${LEDGER_ROW_2}

### PPW-9381 — The retry loop drops the last error

- **What:** The last attempt's exception is swallowed and the caller sees success.
- **Evidence:** \`Services/Retry.cs:12-18\`.
- **Suggested fix:** Rethrow the last exception.
- **History:**
${HIST_1}

### PPW-9382 — The sweep logs below the level floor

- **What:** The sweep logs at Debug, under the configured floor, so nothing is recorded.
- **Evidence:** \`Jobs/Sweep.cs:30\`.
- **Suggested fix:** Log at Information.
- **History:**
${HIST_2}
`)
  const INDEX_SEP = '|---|---|---|---|---|---|---|'
  const OLD_PASS_ROW = '| 2026-08-20 | 938 | v1 discovery (2 lenses) | request-changes | 1/1/0/0 | Worst is PPW-9381, a retry loop that drops its last error | [review](../938-index-rows/review-v1.md) · [ledger](../938-index-rows/ledger.md) |'
  writeFileSync(indexPath, `---
type: review-index
updated: 2026-08-21
---

# Review Index

Fixture copy of the two tables the renderer inserts into: one row per target, one row
per pass.

## Targets at a glance

| Target | State |
|---|---|
| 938 index rows | Open: one discovery pass at \`abc1234\`, one fix round resolved. <br> Re-arms on a new 🔴. |

## Passes

| Date | Target | Pass | Verdict | New H/M/L/C | Outcome | Files |
${INDEX_SEP}
${OLD_PASS_ROW}
`)
  writeFileSync(join(stateDir, 'backlog.md'), `---
type: review-backlog
updated: 2026-08-21
---

# Backlog — unfixed minors from closed targets

Fixture copy: one conforming row.

| ID | Target | Sev | What | Area |
|---|---|---|---|---|
| PPW-9382 | 938-index-rows | 🟠 | The sweep logs below the level floor | \`observability\` |
`)
  wl([
    { t: at('10:00'), ev: 'round-start', round: 1 },
    { t: at('10:05'), ev: 'triage-done', round: 1, clusters: 2, pre_cleared: 1 },
    { t: at('10:10'), ev: 'check-dispatched', cluster: 'retry' },
    { t: at('10:15'), ev: 'micro-review-dispatched', cluster: 'retry' },
    { t: at('10:20'), ev: 'test-run', kind: 'final', passed: 12, failed: 0 },
    { t: at('10:25'), ev: 'round-end', round: 1 },
  ])

  const OUTCOME = 'Both named rows closed: the retry loop now rethrows its last error, and the log-floor row is queued behind the rewrite it waits on.'
  const indexBefore = read(indexPath)
  const ledgerBefore = read(ledgerPath)
  const untouched = () => !existsSync(metricsPath) && read(indexPath) === indexBefore && read(ledgerPath) === ledgerBefore

  {
    const r = run('render-records.mjs', ['--root', T, target])
    check('renderer refuses to append records with no --outcome', r.code === 1 && r.out.includes('--outcome'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check('the refused render wrote no metrics line, no index row and no ledger flip', untouched(), 'one of metrics.jsonl / index.md / ledger.md changed')
  }
  {
    const long = Array.from({ length: 51 }, (_, i) => `w${i}`).join(' ')
    const r = run('render-records.mjs', ['--root', T, target, '--outcome', long])
    check('renderer refuses an --outcome over the 50-word index cap', r.code === 1 && r.out.includes('51 words') && r.out.includes('50'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check('the over-cap refusal wrote nothing either', untouched(), 'one of metrics.jsonl / index.md / ledger.md changed')
  }
  // A "|" or a newline in the outcome would be written straight into the pipe-delimited row, and the
  // broken row only surfaces at the next doc gate — by then it needs hand repair.
  for (const [label, text] of [['a "|"', 'Both rows closed | and the queue drained'], ['a newline', 'Both rows closed\nand the queue drained']]) {
    const r = run('render-records.mjs', ['--root', T, target, '--outcome', text])
    check(`renderer refuses an --outcome carrying ${label}`, r.code === 1 && r.out.includes('one pipe-delimited line'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check(`the ${label} refusal wrote nothing`, untouched(), 'one of metrics.jsonl / index.md / ledger.md changed')
  }
  {
    const r = run('render-records.mjs', ['--root', T, target, '--outcome'])
    check('--outcome with no value prints the usage line, not a stack trace', r.code === 1 && r.out.includes('usage: render-records.mjs') && !r.out.includes('TypeError'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    const flag = run('render-records.mjs', ['--root', T, target, '--outcome', '--dry-run'])
    check('an --outcome that swallowed the next flag is refused', flag.code === 1 && flag.out.includes('another flag'), `exit ${flag.code}: ${flag.out.trim().slice(0, 200)}`)
  }
  {
    const r = run('render-records.mjs', ['--root', T, target, '--outcome', OUTCOME, '--dry-run'])
    check('dry-run prints the index row it would insert', r.code === 0 && r.out.includes('| 2026-08-21 | 938 | v1 fix round (2 clusters, 1 approach-check, 1 micro-review) |'), `exit ${r.code}: ${r.out.split('\n').find(l => l.startsWith('| 2026')) ?? r.out.trim().slice(0, 200)}`)
    check('dry-run prints the ledger flips it would make', r.out.includes('PPW-9381 → fixed at `def5678`') && r.out.includes('PPW-9382 → deferred'), r.out.split('\n').filter(l => l.includes('→')).join(' | '))
    check('dry-run wrote nothing', untouched(), 'one of metrics.jsonl / index.md / ledger.md changed')
  }

  {
    const r = run('render-records.mjs', ['--root', T, target, '--outcome', OUTCOME])
    check('renderer appends the fix round\'s records', r.code === 0 && metricsLines().length === 1, `exit ${r.code}: ${r.out.trim().slice(0, 300)}`)
    check('the renderer warns about a findings row with no ledger row', r.out.includes('PPW-9389 has no ledger row'), r.out.split('\n').find(l => l.includes('9389')) ?? r.out.trim().slice(0, 200))
    const expectedRow = `| 2026-08-21 | 938 | v1 fix round (2 clusters, 1 approach-check, 1 micro-review) | — (resolved) | 0/0/0/0 | ${OUTCOME} | [resolution](../938-index-rows/resolution-v1.md) · [ledger](../938-index-rows/ledger.md) |`
    const expectedIndex = indexBefore.replace(`${INDEX_SEP}\n`, `${INDEX_SEP}\n${expectedRow}\n`)
    check('the index row lands as the newest Passes row and every other byte is unchanged', read(indexPath) === expectedIndex,
      read(indexPath).split('\n').filter(l => l.startsWith('| 2026')).join('\n'))
    const expectedLedger = ledgerBefore
      .replace(LEDGER_ROW_1, '| PPW-9381 | 🔴 | v1 | The retry loop drops the last error | `Services/Retry.cs:12` | fixed | `def5678` |')
      .replace(LEDGER_ROW_2, '| PPW-9382 | 🟠 | v1 | The sweep logs below the level floor | `Jobs/Sweep.cs:30` | deferred | `0000000` |')
      .replace(HIST_1, `${HIST_1}\n  - v1: fix round — fixed at \`abc1234\``)
      .replace(HIST_2, `${HIST_2}\n  - v1: fix round — deferred`)
    check('the ledger flip changes only Status, Affirmed and one appended History line per row', read(ledgerPath) === expectedLedger,
      read(ledgerPath).split('\n').filter(l => l.includes('PPW-938') || l.includes('fix round')).join('\n'))
  }

  // A Windows checkout keeps index.md and ledger.md CRLF, so the verification stage runs against
  // CRLF copies: an inserted line has to match its neighbours, not leave a lone LF behind.
  const crlf = t => t.replace(/\r?\n/g, '\r\n')
  writeFileSync(indexPath, crlf(read(indexPath)))
  writeFileSync(ledgerPath, crlf(read(ledgerPath)))
  const afterRound = { index: read(indexPath), ledger: read(ledgerPath), metrics: metricsLines().length }
  const verifyEvents = [
    { t: at('11:00'), ev: 'pass-launch', pass: 'v2', type: 'verification' },
    { t: at('11:05'), ev: 'verify-result', id: 'PPW-9381', verdict: 'held', commit: 'aaa1111' },
    { t: at('11:10'), ev: 'verify-result', id: 'PPW-9382', verdict: 'test-never-red' },
    { t: at('11:15'), ev: 'test-run', kind: 'final', passed: 20, failed: 0 },
  ]
  const V_OUTCOME = 'The retry fix held on its own revert; the log-floor row reopened because its test stays green with the defect back in place.'
  const vUntouched = () => read(indexPath) === afterRound.index && read(ledgerPath) === afterRound.ledger && metricsLines().length === afterRound.metrics

  wl([...verifyEvents])
  {
    const r = run('render-records.mjs', ['--root', T, target, '--verification', 'v2', '--outcome', V_OUTCOME])
    check('verification refuses while the pass has no pass-records-done', r.code === 1 && r.out.includes('pass-records-done') && r.out.includes('--in-progress'), `exit ${r.code}: ${r.out.trim().slice(0, 250)}`)
    check('the unfinished-pass refusal wrote nothing', vUntouched(), 'index.md, ledger.md or metrics.jsonl changed')
  }
  {
    const r = run('render-records.mjs', ['--root', T, target, '--verification', 'v2', '--outcome', V_OUTCOME, '--dry-run'])
    check('an unfinished verification still dry-runs', r.code === 0 && r.out.includes('no pass-records-done yet'), `exit ${r.code}: ${r.out.trim().slice(0, 250)}`)
    check('the verification dry-run wrote nothing', vUntouched(), 'index.md, ledger.md or metrics.jsonl changed')
  }

  wl([...verifyEvents, { t: at('11:20'), ev: 'pass-records-done', pass: 'v2' }])
  {
    const r = run('render-records.mjs', ['--root', T, target, '--verification', 'v2', '--outcome', V_OUTCOME, '--new-findings', '0,1,0,0'])
    check('verification mode appends its metrics line', r.code === 0 && metricsLines().length === afterRound.metrics + 1, `exit ${r.code}: ${r.out.trim().slice(0, 300)}`)
    const appended = metricsLines().length ? JSON.parse(metricsLines()[metricsLines().length - 1]) : {}
    check('the verification line tallies held as verified and everything else as reopened',
      appended.type === 'verification' && appended.pass === 2 && appended.verified === 1 && appended.reopened === 1,
      JSON.stringify(appended).slice(0, 300))
    check('the verification line names the reopened id, carries the fixed verdict and the span\'s runtime',
      appended.notes === 'reopened: PPW-9382 (test-never-red)' && appended.verdict === 'approve-with-followups'
      && appended.runtime?.started === at('11:00') && appended.runtime?.ended === at('11:20'),
      JSON.stringify({ notes: appended.notes, verdict: appended.verdict, runtime: appended.runtime }))
    check('the verification line carries no outcome or subtype key and takes its counts from --new-findings',
      !('outcome' in appended) && !('subtype' in appended) && appended.new_findings?.medium === 1 && appended.tests?.passed === 20,
      JSON.stringify({ new_findings: appended.new_findings, tests: appended.tests }))
    // records-auditor hard-requires `commit` on every pass line, and a verification is anchored at
    // the commit whose fixes it checks — so with no --commit it reads the newest resolution's.
    check('with no --commit the line falls back to the newest resolution\'s fixed_commit and says so',
      appended.commit === 'def5678' && r.out.includes("resolution-v1.md's fixed_commit def5678"), `commit ${appended.commit}: ${r.out.split('\n').find(l => l.includes('fixed_commit')) ?? ''}`)
    const expectedRow = `| 2026-08-21 | 938 | v2 verification (anchored) | approve-with-followups | 0/1/0/0 | ${V_OUTCOME} | [ledger](../938-index-rows/ledger.md) |`
    check('the verification index row lands newest-first with the anchored pass cell, CRLF and all', read(indexPath) === afterRound.index.replace(`${INDEX_SEP}\r\n`, `${INDEX_SEP}\r\n${expectedRow}\r\n`),
      read(indexPath).split('\n').filter(l => l.startsWith('| 2026')).join('\n'))
    const expectedLedger = afterRound.ledger
      .replace('| `Services/Retry.cs:12` | fixed | `def5678` |', '| `Services/Retry.cs:12` | verified | `aaa1111` |')
      .replace('| `Jobs/Sweep.cs:30` | deferred | `0000000` |', '| `Jobs/Sweep.cs:30` | open | `0000000` |')
      .replace('  - v1: fix round — fixed at `abc1234`', '  - v1: fix round — fixed at `abc1234`\r\n  - v2: verification — held')
      .replace('  - v1: fix round — deferred', '  - v1: fix round — deferred\r\n  - v2: verification — reopened (test-never-red)')
    check('a held row flips to verified at the event\'s commit and a reopened row back to open', read(ledgerPath) === expectedLedger,
      read(ledgerPath).split('\n').filter(l => l.includes('PPW-938') || l.includes('verification')).join('\n'))
  }
  {
    const after = { index: read(indexPath), ledger: read(ledgerPath), metrics: metricsLines().length }
    const r = run('render-records.mjs', ['--root', T, target, '--verification', 'v2', '--outcome', V_OUTCOME])
    check('a second verification render for the same pass refuses rather than duplicating the line',
      r.code === 1 && r.out.includes('correction line'), `exit ${r.code}: ${r.out.trim().slice(0, 250)}`)
    check('the duplicate refusal left all three files alone',
      read(indexPath) === after.index && read(ledgerPath) === after.ledger && metricsLines().length === after.metrics, 'a file changed')

    wl([...verifyEvents, { t: at('11:30'), ev: 'pass-launch', pass: 'v2', type: 'verification' }])
    const twice = run('render-records.mjs', ['--root', T, target, '--verification', '2', '--outcome', V_OUTCOME])
    check('a second pass-launch for an open pass aborts', twice.code === 1 && twice.out.includes(at('11:00')) && twice.out.includes(at('11:30')) && twice.out.includes('wl.mjs'), `exit ${twice.code}: ${twice.out.trim().slice(0, 250)}`)
    check('the unpairable-stamp abort left all three files alone',
      read(indexPath) === after.index && read(ledgerPath) === after.ledger && metricsLines().length === after.metrics, 'a file changed')

    const gate = run('doc-gate.mjs', ['--root', T, 'state'])
    check('both generated index rows pass the state doc gate', gate.code === 0, gate.out.trim().slice(0, 400))
  }

  {
    wl([...verifyEvents, { t: at('11:20'), ev: 'pass-records-done', pass: 'v2' },
      { t: at('12:00'), ev: 'pass-launch', pass: 'v3', type: 'verification' },
      { t: at('12:05'), ev: 'verify-result', id: 'PPW-9381', verdict: 'held', commit: 'bbb2222' },
      { t: at('12:10'), ev: 'pass-records-done', pass: 'v3' }])
    const r = run('render-records.mjs', ['--root', T, target, '--verification', 'v3', '--outcome', V_OUTCOME, '--commit', 'ccc3333', '--no-index'])
    const appended = metricsLines().length ? JSON.parse(metricsLines()[metricsLines().length - 1]) : {}
    check('an explicit --commit wins over the resolution fallback', r.code === 0 && appended.commit === 'ccc3333' && !r.out.includes('fixed_commit def5678'),
      `exit ${r.code}: commit ${appended.commit}`)
  }
  {
    const bare = '939-no-anchor'
    mkdirSync(join(T, 'reviews', bare), { recursive: true })
    writeFileSync(join(T, 'reviews', bare, 'worklog.jsonl'), [
      { t: at('13:00'), ev: 'pass-launch', pass: 'v1', type: 'verification' },
      { t: at('13:05'), ev: 'verify-result', id: 'PPW-9391', verdict: 'held' },
      { t: at('13:10'), ev: 'pass-records-done', pass: 'v1' },
    ].map(e => JSON.stringify(e)).join('\n') + '\n')
    const r = run('render-records.mjs', ['--root', T, bare, '--verification', 'v1', '--outcome', 'The one fix held on its own revert.', '--no-index'])
    const written = existsSync(join(T, 'reviews', bare, 'metrics.jsonl')) ? JSON.parse(readFileSync(join(T, 'reviews', bare, 'metrics.jsonl'), 'utf8').trim()) : {}
    check('commit is null, and said to be, when there is no --commit and no resolution to read it from',
      r.code === 0 && written.commit === null && r.out.includes('commit will be null'), `exit ${r.code}: ${JSON.stringify(written).slice(0, 200)}`)
    check('a held row with no commit on its event leaves Affirmed alone', r.out.includes('PPW-9391 → verified') && !r.out.includes('PPW-9391 → verified at'),
      r.out.split('\n').find(l => l.includes('→')) ?? r.out.trim().slice(0, 200))
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
  // A git hook's own GIT_DIR/GIT_WORK_TREE/GIT_COMMON_DIR (etc.) would otherwise leak in here
  // and redirect these calls at the hook's repo instead of T; ask git for the authoritative
  // list (git help githooks: "unset $(git rev-parse --local-env-vars)") rather than guessing.
  const FALLBACK_GIT_ENV_VARS = ['GIT_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_OBJECT_DIRECTORY', 'GIT_ALTERNATE_OBJECT_DIRECTORIES', 'GIT_CEILING_DIRECTORIES', 'GIT_PREFIX']
  const localEnvVars = spawnSync('git', ['rev-parse', '--local-env-vars'], { encoding: 'utf8' })
  const gitEnvVars = localEnvVars.status === 0 ? localEnvVars.stdout.split(/\r?\n/).filter(Boolean) : FALLBACK_GIT_ENV_VARS
  const gitEnv = { ...process.env }
  for (const k of gitEnvVars) delete gitEnv[k]
  const g = (...a) => spawnSync('git', ['-C', T, ...a], { encoding: 'utf8', env: gitEnv })
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

  const wlPath = join(T, 'reviews', '950-verify-target', 'worklog.jsonl')
  const wlLines = () => existsSync(wlPath) ? readFileSync(wlPath, 'utf8').split(/\r?\n/).filter(Boolean).map(l => JSON.parse(l)) : []

  const dry = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--dry-run'])
  check('verify-fixes dry-run derives the plan', dry.code === 0 && dry.out.includes('calc.txt') && dry.out.includes('PhotoPrint.Tests.Unit.CalcTests'), dry.out.trim())
  check('verify-fixes --dry-run appends no worklog event', wlLines().length === 0, JSON.stringify(wlLines()))

  const live = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen])
  check('verify-fixes proves red-then-green and reports held', live.code === 0 && live.out.includes('"verdict":"held"') && live.out.includes('SUMMARY: 1/1 held'), live.out.trim())
  check("verify-fixes warns when HEAD has moved past the resolution's fixed_commit",
    live.out.includes(`warning: HEAD is not the resolution's fixed_commit ${sha}`), live.out.trim())
  {
    const verifyResults = wlLines().filter(e => e.ev === 'verify-result')
    check('verify-fixes appends exactly one verify-result event for the held row', verifyResults.length === 1, JSON.stringify(verifyResults))
    check('the verify-result event names PPW-9501 held', verifyResults[0]?.id === 'PPW-9501' && verifyResults[0]?.verdict === 'held', JSON.stringify(verifyResults))
  }
  // A live run's worklog write is a deliberate change, not revert-and-restore leftovers — commit
  // it (as a real caller would) before checking the tree is otherwise clean.
  g('add', '.'); g('commit', '-qm', 'worklog')
  check('verify-fixes leaves the tree clean', g('status', '--porcelain').stdout.trim() === '', g('status', '--porcelain').stdout)

  const liveNoEvents = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen, '--no-events'])
  check('verify-fixes --no-events reports held without appending', liveNoEvents.code === 0 && liveNoEvents.out.includes('"verdict":"held"'), liveNoEvents.out.trim())
  check('verify-fixes --no-events appends no additional worklog event', wlLines().filter(e => e.ev === 'verify-result').length === 1, JSON.stringify(wlLines()))

  {
    const decoyT = mkdtempSync(join(tmpdir(), 'decoy-'))
    const dg = (...a) => spawnSync('git', ['-C', decoyT, ...a], { encoding: 'utf8', env: gitEnv })
    dg('init', '-q', '-b', 'main')
    dg('config', 'user.email', 'decoy@test'); dg('config', 'user.name', 'decoy')
    writeFileSync(join(decoyT, 'marker.txt'), 'untouched\n')
    dg('add', '.'); dg('commit', '-qm', 'decoy base')
    const decoyHead = dg('rev-parse', 'HEAD').stdout.trim()
    const leakedEnv = { ...process.env, GIT_DIR: join(decoyT, '.git'), GIT_WORK_TREE: decoyT, GIT_COMMON_DIR: join(decoyT, '.git') }
    const leaked = spawnSync(process.execPath, [join(REVIEWS, 'lib', 'verify-fixes.mjs'), '--root', T, '950-verify-target', '--test-cmd-api', redGreen, '--no-events'], { encoding: 'utf8', cwd: REPO, env: leakedEnv })
    const leakedOut = `${leaked.stdout ?? ''}${leaked.stderr ?? ''}`
    check('verify-fixes ignores a leaked GIT_DIR/GIT_WORK_TREE/GIT_COMMON_DIR and still verifies the real target',
      leaked.status === 0 && leakedOut.includes('"verdict":"held"'), leakedOut.trim())
    check('verify-fixes leaves the real fixture tree clean despite the leaked env', g('status', '--porcelain').stdout.trim() === '', g('status', '--porcelain').stdout)
    check('verify-fixes never touches the decoy repo the leaked env pointed at',
      dg('rev-parse', 'HEAD').stdout.trim() === decoyHead && dg('status', '--porcelain').stdout.trim() === '',
      `decoy HEAD now ${dg('rev-parse', 'HEAD').stdout.trim()} (was ${decoyHead}); status: ${dg('status', '--porcelain').stdout.trim()}`)
    check('the decoy marker file is untouched', readFileSync(join(decoyT, 'marker.txt'), 'utf8') === 'untouched\n', readFileSync(join(decoyT, 'marker.txt'), 'utf8'))
    rmSync(decoyT, { recursive: true, force: true })
  }

  const neverRed = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', 'node -e "process.exit(0)"', '--no-events'])
  check('verify-fixes reopens a fix whose test never goes red', neverRed.code === 1 && neverRed.out.includes('"verdict":"test-never-red"'), neverRed.out.trim())

  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'dirty\n')
  const dirty = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen])
  check('verify-fixes refuses a dirty tree', dirty.code === 2, `exit ${dirty.code}: ${dirty.out.trim()}`)
  g('checkout', '--', '.')

  // A fix that took a follow-up commit lists both in the Commit cell; reverting only the first
  // would leave the follow-up's code in place and the test green for the wrong reason.
  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'fixed and polished\n')
  g('add', '.'); g('commit', '-qm', 'follow-up')
  const sha2 = g('rev-parse', '--short', 'HEAD').stdout.trim()
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${sha2}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9501 | fixed | \`${sha}\`, \`${sha2}\` | fixture fix with a follow-up |\n| PPW-9502 | fixed | — | fixture row whose cell names no commit |\n`)
  g('add', '.'); g('commit', '-qm', 'resolution with two commits')
  const multi = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen, '--no-events'])
  check('verify-fixes covers a row whose Commit cell lists two commits', multi.out.includes('"id":"PPW-9501"') && multi.out.includes('"verdict":"held"'), multi.out.trim())
  check('verify-fixes never skips a fixed row it cannot parse', multi.out.includes('"id":"PPW-9502"') && multi.out.includes('"verdict":"unparsable-commit"'), multi.out.trim())
  check('verify-fixes counts both rows in its summary', multi.out.includes('SUMMARY: 1/2 held') && multi.code === 1, `exit ${multi.code}: ${multi.out.trim()}`)
  check('verify-fixes leaves the tree clean after a multi-commit revert', g('status', '--porcelain').stdout.trim() === '', g('status', '--porcelain').stdout)

  // Two rows that both complete a full revert -> red -> restore -> green cycle in the same run:
  // row 2's `git reset --hard` must not wipe row 1's already-appended (uncommitted) worklog event.
  mkdirSync(join(T, 'src', 'app2'), { recursive: true })
  writeFileSync(join(T, 'src', 'app2', 'a.txt'), 'buggyA\n')
  writeFileSync(join(T, 'src', 'app2', 'b.txt'), 'buggyB\n')
  g('add', '.'); g('commit', '-qm', 'two-row base')
  writeFileSync(join(T, 'src', 'app2', 'a.txt'), 'fixedA\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit', 'ATests.cs'), 'test a\n')
  g('add', '.'); g('commit', '-qm', 'fix a')
  const shaA = g('rev-parse', '--short', 'HEAD').stdout.trim()
  writeFileSync(join(T, 'src', 'app2', 'b.txt'), 'fixedB\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit', 'BTests.cs'), 'test b\n')
  g('add', '.'); g('commit', '-qm', 'fix b')
  const shaB = g('rev-parse', '--short', 'HEAD').stdout.trim()
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${shaB}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9510 | fixed | \`${shaA}\` | two-row fixture: row A |\n| PPW-9511 | fixed | \`${shaB}\` | two-row fixture: row B |\n`)
  g('add', '.'); g('commit', '-qm', 'two-row resolution')
  const twoRowTpl = `node -e "const fs=require('fs'); const f=process.argv[1].indexOf('ATests')>=0?'src/app2/a.txt':'src/app2/b.txt'; process.exit(fs.readFileSync(f,'utf8').indexOf('buggy')>=0?1:0)" {filter}`
  const twoRow = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', twoRowTpl])
  check('verify-fixes holds both rows of the two-row run', twoRow.code === 0 && twoRow.out.includes('SUMMARY: 2/2 held'), twoRow.out.trim())
  {
    const twoRowResults = wlLines().filter(e => e.ev === 'verify-result' && (e.id === 'PPW-9510' || e.id === 'PPW-9511'))
    check("row 2's reset --hard did not wipe row 1's already-appended worklog event",
      twoRowResults.length === 2 && twoRowResults.every(e => e.verdict === 'held'), JSON.stringify(wlLines()))
  }
  check('verify-fixes leaves the tree clean after the two-row run (worklog aside)',
    g('status', '--porcelain', '--', '.', ':(exclude)reviews/950-verify-target/worklog.jsonl').stdout.trim() === '',
    g('status', '--porcelain').stdout)
  g('add', '.'); g('commit', '-qm', 'worklog after two-row run')

  // A frontend spec with no installed dependencies must not be run: the runner would fail to
  // start in both legs, which reads as a red that reddened for the wrong reason.
  mkdirSync(join(T, 'src', 'PhotoPrint.UI', 'src'), { recursive: true })
  writeFileSync(join(T, 'src', 'app', 'widget.txt'), 'buggy\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.UI', 'src', 'widget.spec.ts'), 'spec body\n')
  g('add', '.'); g('commit', '-qm', 'ui fix')
  const uiSha = g('rev-parse', '--short', 'HEAD').stdout.trim()
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${uiSha}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9503 | fixed | \`${uiSha}\` | fixture fix carrying a frontend spec |\n`)
  g('add', '.'); g('commit', '-qm', 'ui resolution')
  const ui = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen, '--no-events'])
  check('verify-fixes refuses a frontend row with no installed dependencies', ui.out.includes('"verdict":"env-missing"') && ui.out.includes('node_modules'), ui.out.trim())
  check('verify-fixes ran no test for the refused frontend row', ui.out.includes('"red_exits":[]'), ui.out.trim())
  rmSync(T, { recursive: true, force: true })
}

// ---------- wl: the validated worklog stamper ----------
{
  const T = mkdtempSync(join(tmpdir(), 'wl-'))
  const target = '930-wl-target'
  mkdirSync(join(T, 'reviews', target), { recursive: true })
  writeFileSync(join(T, 'reviews', target, 'resolution-v1.md'), '---\nstatus: resolved\n---\n')
  const wlPath = join(T, 'reviews', target, 'worklog.jsonl')
  const lines = () => existsSync(wlPath) ? readFileSync(wlPath, 'utf8').split(/\r?\n/).filter(l => l.trim()) : []

  let r = run('wl.mjs', ['--root', T, target, 'pass-launch', '--pass', 'v1', '--type', 'full discovery'])
  check('wl appends a valid event and exits 0', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('wl prints exactly the appended line', lines().length === 1 && r.out.trim() === lines()[0], r.out.trim())
  let firstEvent = lines().length ? JSON.parse(lines()[0]) : null
  check('wl stamps a timestamp with the local UTC offset', !!firstEvent && /[+-]\d{2}:\d{2}$/.test(firstEvent.t), JSON.stringify(firstEvent))

  r = run('wl.mjs', ['--root', T, target, 'not-a-real-event'])
  check('wl refuses an unknown ev', r.code === 1 && r.out.includes('ERROR') && r.out.includes('unknown ev'), r.out.trim())
  check('wl appended nothing for the unknown ev', lines().length === 1, String(lines().length))

  const shapeCases = [
    ['round-start', [], 'round'],
    ['triage-done', ['--round', '1'], 'clusters'],
    ['gate-open', [], 'reason'],
    ['gate-parked', ['--kind', 'fixer-decision', '--default', 'deferred'], 'reason'],
    ['test-run', [], 'kind'],
    ['finding', ['--id', 'PPW-1'], 'status'],
    ['micro-review-dispatched', [], 'cluster'],
    ['pass-launch', ['--pass', 'v1'], 'type'],
    ['pass-records-done', [], 'pass'],
    ['verify-result', ['--id', 'PPW-1'], 'verdict'],
    ['void', [], 'of'],
  ]
  for (const [ev, args, field] of shapeCases) {
    const before = lines().length
    const rr = run('wl.mjs', ['--root', T, target, ev, ...args])
    check(`wl refuses ${ev} missing "${field}"`, rr.code === 1 && rr.out.includes('ERROR'), rr.out.trim())
    check(`wl appends nothing for ${ev} missing "${field}"`, lines().length === before, String(lines().length))
  }

  r = run('wl.mjs', ['--root', T, target, 'test-run', '--kind', 'bogus'])
  check('wl refuses a test-run kind outside the enum', r.code === 1 && r.out.includes('ERROR'), r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'finding', '--id', 'BUG-1', '--status', 'fixed'])
  check('wl refuses a finding id not shaped like PPW-<n>', r.code === 1 && r.out.includes('ERROR'), r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'run-start', '--t', '2020-01-01T00:00:00+00:00'])
  check('wl refuses a passed --t (the stamper owns time)', r.code === 1 && r.out.includes('ERROR'), r.out.trim())
  check('wl appends nothing when --t is passed', lines().length === 1, String(lines().length))

  r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '7'])
  check('wl refuses round-start when resolution-v7.md is missing', r.code === 1 && r.out.includes('ERROR') && r.out.includes('resolution-v7'), r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '1'])
  check('wl accepts round-start 1 (resolution-v1.md exists)', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '2'])
  check('wl refuses round-start 2 while round 1 is still open', r.code === 1 && r.out.includes('ERROR'), r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-end', '--round', '2'])
  check('wl refuses round-end 2 with no open round-start for 2', r.code === 1 && r.out.includes('ERROR'), r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-end', '--round', '1'])
  check('wl accepts round-end 1, closing the open round', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '1'])
  check('wl accepts a same-number restart after round-end (multi-part round)', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-end', '--round', '1'])
  check('wl closes the restarted round', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'void', '--json', '{"of":{"ev":"round-start","t":"1999-01-01T00:00:00+00:00"}}'])
  check('wl refuses a void with no matching event', r.code === 1 && r.out.includes('ERROR') && r.out.includes('closest timestamps'), r.out.trim())

  if (firstEvent) {
    r = run('wl.mjs', ['--root', T, target, 'void', '--json', JSON.stringify({ of: { ev: firstEvent.ev, t: firstEvent.t } })])
    check('wl accepts a void matching an existing event', r.code === 0, r.out.trim())
    check('wl worklog grew by exactly one line for the accepted void', lines().length === 6, String(lines().length))
  } else {
    check('wl accepts a void matching an existing event', false, 'no earlier event was captured to void')
  }

  rmSync(T, { recursive: true, force: true })
}
{
  const T = mkdtempSync(join(tmpdir(), 'wl-void-'))
  const target = '932-wl-void'
  mkdirSync(join(T, 'reviews', target), { recursive: true })
  for (const n of [1, 2]) writeFileSync(join(T, 'reviews', target, `resolution-v${n}.md`), '---\nstatus: resolved\n---\n')
  const wlPath = join(T, 'reviews', target, 'worklog.jsonl')
  const lines = () => existsSync(wlPath) ? readFileSync(wlPath, 'utf8').split(/\r?\n/).filter(l => l.trim()) : []

  let r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '1'])
  check('wl opens round 1', r.code === 0, r.out.trim())
  const opened = lines().length ? JSON.parse(lines()[0]) : null

  r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '1'])
  check('wl refuses a repeat round-start for the round already open', r.code === 1 && r.out.includes('ERROR') && r.out.includes('still open'), r.out.trim())
  check('wl appended nothing for the repeat round-start', lines().length === 1, String(lines().length))

  if (opened) {
    r = run('wl.mjs', ['--root', T, target, 'void', '--json', JSON.stringify({ of: { ev: 'round-start', t: opened.t, round: 1 } })])
    check('wl voids the round-start it just stamped', r.code === 0, r.out.trim())
    r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '2'])
    check('a voided round-start no longer holds the round open for the stamper', r.code === 0, r.out.trim())
  } else {
    check('a voided round-start no longer holds the round open for the stamper', false, 'no round-start was captured to void')
  }

  rmSync(T, { recursive: true, force: true })
}
{
  const T = mkdtempSync(join(tmpdir(), 'wl-inprocess-'))
  const target = '931-wl-inprocess'
  mkdirSync(join(T, 'reviews', target), { recursive: true })
  try {
    const { appendEvent } = await import('../wl.mjs')
    const stamped = appendEvent(T, target, { ev: 'note', text: 'in-process call' })
    check('appendEvent returns the stamped event with an offset timestamp',
      stamped.ev === 'note' && /[+-]\d{2}:\d{2}$/.test(stamped.t), JSON.stringify(stamped))
    const written = readFileSync(join(T, 'reviews', target, 'worklog.jsonl'), 'utf8').trim()
    check('appendEvent wrote exactly the stamped line to disk', written === JSON.stringify(stamped), written)
  } catch (e) {
    check('appendEvent is importable and usable in-process', false, String(e))
  }
  rmSync(T, { recursive: true, force: true })
}

if (failures.length) {
  console.log(`FAIL: ${failures.length} of ${count} assertion(s) failed:\n`)
  for (const f of failures) console.log(`  - ${f}`)
  process.exit(1)
}
console.log(`${count} assertions, all passed`)
