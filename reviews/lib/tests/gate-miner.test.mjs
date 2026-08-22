// Tests for gate-miner.mjs: disapproval mining, since-filtering, and mixed-offset timestamp bucketing.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only gate-miner
import { check, run } from './lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'

// ---------- gate-miner: disapproval detection, since-filtering, summary ----------
// Fixture dates sit in 2019, far from the real wall-clock date, so a --since default that
// wrongly used "now" instead of "the newest event seen" would drop everything and fail these.
{
  const T = mkdtempSync(join(tmpdir(), 'gate-miner-core-'))
  const target = '960-gate-miner-core'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  const REASON_A = 'heading order mismatch in review-v1.md'
  const JUDGE_C = 'disapprove-then-fixed: fixed_commit rendered as the closing sentence text'
  const NOTE_B = 'nothing unusual, quick approve'
  const STUB = '[ ] lintable? -> add a check to doc-gate.mjs + a fixture to run-tests.mjs'
  writeFileSync(join(dir, 'worklog.jsonl'), [
    { t: '2019-01-01T10:00:00+03:00', ev: 'doc-gate', verdict: 'disapprove', round: 1, reason: REASON_A },
    { t: '2019-01-15T09:00:00+03:00', ev: 'doc-gate', verdict: 'approve', round: 2, note: NOTE_B },
    { t: '2019-01-20T11:00:00+03:00', ev: 'doc-gate', round: 9, lint: 'clean', judge: JUDGE_C, auditor: '0 errors' },
  ].map(e => JSON.stringify(e)).join('\n') + '\n')

  {
    const r = run('gate-miner.mjs', ['--root', T])
    check('gate-miner exits 0 with no --since', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
    check('both disapprovals print their full reason/judge text', r.out.includes(REASON_A) && r.out.includes(JUDGE_C), r.out.trim())
    check('the approve event does not print', !r.out.includes(NOTE_B), r.out.trim())
    check('default --since (30 days before the newest event seen) counts both', r.out.includes('total disapprovals: 2'), r.out.trim())
    check('the summary breaks the count down per target', r.out.includes(`${target}: 2`), r.out.trim())
    const stubCount = r.out.split(STUB).length - 1
    check('two distinct reasons each get their own stub checklist line', stubCount === 2, `stub appeared ${stubCount} time(s)`)
  }
  {
    const r = run('gate-miner.mjs', ['--root', T, '--since', '2019-01-10'])
    check('--since excludes the earlier disapproval', r.code === 0 && !r.out.includes(REASON_A), r.out.trim())
    check('--since keeps the later disapproval', r.out.includes(JUDGE_C), r.out.trim())
    check('the filtered summary counts 1', r.out.includes('total disapprovals: 1'), r.out.trim())
  }
  {
    const r = run('gate-miner.mjs', ['--root', T, '--since', '2019-02-01'])
    check('a --since after every event yields zero, not an error', r.code === 0 && r.out.includes('total disapprovals: 0'), `exit ${r.code}: ${r.out.trim()}`)
    check('no reason text leaks into a zero-match run', !r.out.includes(REASON_A) && !r.out.includes(JUDGE_C), r.out.trim())
  }
  rmSync(T, { recursive: true, force: true })
}

// ---------- gate-miner: per-target summary, archive scan by default, target filtering, dedup ----------
{
  const T = mkdtempSync(join(tmpdir(), 'gate-miner-multi-'))
  const wl = (target, events, archived) => {
    const dir = join(T, 'reviews', ...(archived ? ['archive', target] : [target]))
    mkdirSync(dir, { recursive: true })
    writeFileSync(join(dir, 'worklog.jsonl'), events.map(e => JSON.stringify(e)).join('\n') + '\n')
  }
  const SHARED_REASON = 'filename used as link text'
  wl('962-gate-miner-multi-a', [
    { t: '2019-02-01T08:00:00Z', ev: 'doc-gate', verdict: 'disapprove', round: 4, reason: SHARED_REASON },
    { t: '2019-02-02T08:00:00Z', ev: 'doc-gate', verdict: 'disapprove', round: 5, reason: 'dense multi-id sentence in Decisions' },
  ])
  wl('963-gate-miner-multi-b', [
    { t: '2019-02-03T08:00:00Z', ev: 'doc-gate', pass: 3, verdict: 'disapprove', reason: SHARED_REASON },
  ])
  wl('964-gate-miner-archived', [
    { t: '2019-02-04T08:00:00Z', ev: 'doc-gate', verdict: 'disapprove', round: 1, reason: 'unauthorized metrics-schema vocabulary in prose' },
  ], true)

  {
    const r = run('gate-miner.mjs', ['--root', T])
    check('gate-miner scans archived targets by default', r.code === 0 && r.out.includes('964-gate-miner-archived: 1'), r.out.trim())
    check('per-target counts are correct across live targets', r.out.includes('962-gate-miner-multi-a: 2') && r.out.includes('963-gate-miner-multi-b: 1'), r.out.trim())
    check('the total sums every target, live and archived', r.out.includes('total disapprovals: 4'), r.out.trim())
    check('round/pass prints whichever field is present', r.out.includes('round 4') && r.out.includes('pass 3'), r.out.trim())
    const stubCount = r.out.split('[ ] lintable?').length - 1
    check('a reason repeated across targets gets one stub line, not two', stubCount === 3, `stub appeared ${stubCount} time(s)`)
  }
  {
    const r = run('gate-miner.mjs', ['--root', T, '964-gate-miner-archived'])
    check('a positional target argument narrows the scan', r.code === 0 && r.out.includes('total disapprovals: 1'), r.out.trim())
    check('the narrowed scan excludes the other targets', !r.out.includes('962-gate-miner-multi-a') && !r.out.includes('963-gate-miner-multi-b'), r.out.trim())
  }
  rmSync(T, { recursive: true, force: true })
}

// ---------- gate-miner: mixed Z/offset timestamps — bucketing and print order by instant ----------
// The real 038-039 worklog mixes `Z` and `+03:00` stamps. A raw ISO-string compare mis-orders
// and mis-buckets across that mix: "...T23:30:00Z" sorts before "...T01:00:00+03:00" the next
// calendar day even though the +03:00 stamp is the earlier instant (it's 3h behind its own
// written day). Per the header's bucketing/ordering contract, both are judged by parsed instant.
{
  const T = mkdtempSync(join(tmpdir(), 'gate-miner-tz-'))
  const target = '966-gate-miner-mixed-tz'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  const REASON_Z = 'Z stamp reading the earlier day by string alone'
  const REASON_OFFSET = 'offset stamp reading the next local day but the earlier instant'
  writeFileSync(join(dir, 'worklog.jsonl'), [
    { t: '2026-08-21T23:30:00Z', ev: 'doc-gate', verdict: 'disapprove', round: 1, reason: REASON_Z },
    { t: '2026-08-22T01:00:00+03:00', ev: 'doc-gate', verdict: 'disapprove', round: 2, reason: REASON_OFFSET },
  ].map(e => JSON.stringify(e)).join('\n') + '\n')

  {
    const r = run('gate-miner.mjs', ['--root', T])
    check('both mixed-offset disapprovals are counted', r.code === 0 && r.out.includes('total disapprovals: 2'), r.out.trim())
    check('both bucket to the same true UTC day (2026-08-21), not two different days',
      (r.out.match(/2026-08-21 ·/g) || []).length === 2 && !r.out.includes('2026-08-22 ·'), r.out.trim())
    check('the earlier instant (the +03:00 stamp) prints before the later one (the Z stamp)',
      r.out.indexOf(REASON_OFFSET) > -1 && r.out.indexOf(REASON_OFFSET) < r.out.indexOf(REASON_Z), r.out.trim())
  }
  {
    const r = run('gate-miner.mjs', ['--root', T, '--since', '2026-08-22'])
    check('--since buckets by the true UTC day: both real instants are 2026-08-21, so both are excluded (the +03:00 stamp\'s raw string alone would have wrongly survived)',
      r.code === 0 && r.out.includes('total disapprovals: 0'), r.out.trim())
    check('neither reason text leaks through the boundary', !r.out.includes(REASON_OFFSET) && !r.out.includes(REASON_Z), r.out.trim())
  }
  {
    const r = run('gate-miner.mjs', ['--root', T, '--since', '2026-08-21'])
    check('--since on the true UTC day itself keeps both', r.code === 0 && r.out.includes('total disapprovals: 2'), r.out.trim())
  }
  rmSync(T, { recursive: true, force: true })
}

// ---------- gate-miner: an approved round's note mentioning "disapprove" still matches ----------
// Real shape, 038-039-invoicing worklog line 8: an overall-approved pass whose note narrates
// per-round disapprovals inline. The pinned match rule (verdict/judge/reason/note, case
// insensitive) doesn't care about the top-level verdict — this locks that in.
{
  const T = mkdtempSync(join(tmpdir(), 'gate-miner-approve-note-'))
  const target = '967-gate-miner-approve-note'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  const NOTE = 'lint clean throughout; Sonnet judge disapproved rounds 1-4 (heading/title mismatches, unauthorized metrics-schema vocabulary in prose) — approved round 5'
  writeFileSync(join(dir, 'worklog.jsonl'), JSON.stringify(
    { t: '2026-08-13T08:45:00Z', ev: 'doc-gate', pass: 'v1', verdict: 'approve', rounds: 5, note: NOTE }
  ) + '\n')

  const r = run('gate-miner.mjs', ['--root', T])
  check('an approve-verdict event whose note mentions "disapprove" still matches', r.code === 0 && r.out.includes(NOTE), r.out.trim())
  check('the summary counts it despite the overall verdict being approve', r.out.includes('total disapprovals: 1'), r.out.trim())
  check('round/pass falls back to the pass field when there is no round', r.out.includes('pass v1'), r.out.trim())
  rmSync(T, { recursive: true, force: true })
}
