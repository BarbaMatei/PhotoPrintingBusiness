// Tests for render-records.mjs: backlog bucketing, frontmatter/worklog edge shapes, and span math.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only render-records
import { check, run, GOOD_ROOT } from './lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'

// ---------- render-records: dry-run backlog bucketing ----------
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
    { t: '2026-08-21T10:21:00+03:00', ev: 'round-review-dispatched', round: 1 },
    { t: '2026-08-21T10:22:00+03:00', ev: 'round-review-returned', round: 1, found: 2 },
    { t: '2026-08-21T10:23:00+03:00', ev: 'test-audit-dispatched', round: 1 },
    { t: '2026-08-21T10:24:00+03:00', ev: 'test-audit-returned', round: 1, verdict: 'clean' },
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
    check('renderer counts a round review into micro_reviews and its agents into cost', r.out.includes('"count": 1') && r.out.includes('"follow_up_fixes": 2') && r.out.includes('"agents": 2'),
      r.out.split('\n').filter(l => /"(count|follow_up_fixes|agents)"/.test(l)).join(' ').trim())
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
