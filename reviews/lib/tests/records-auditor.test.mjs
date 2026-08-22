// Tests for records-auditor.mjs: real-repo smoke checks and the reviewed-unit window.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only records-auditor
import { check, run, firstLine, GOOD_ROOT } from './lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'

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

// ---------- records auditor: the reviewed-unit window ----------
// A resolution can flip to status: resolved before render-records.mjs appends that round's
// fix-round line (the line now renders once, after the paired verification confirms the round).
// That gap must warn, not error, and stop warning once the line lands. The worklog also carries
// two new legal event kinds, void and verify-result, which must not read as malformed shapes.
{
  const T = mkdtempSync(join(tmpdir(), 'records-auditor-window-'))
  const target = '922-resolved-window'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  writeFileSync(join(dir, 'metrics.jsonl'), '')
  writeFileSync(join(dir, 'resolution-v1.md'), `---
type: resolution
target: ${target}
version: 1
answers: review-v1.md
status: resolved
fixed_commit: abc1234
closed: 2026-08-21
---

# Resolution v1 — ${target}

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9221 | fixed | \`abc1234\` | done |
`)
  writeFileSync(join(dir, 'worklog.jsonl'), [
    { t: '2026-08-21T10:00:00+03:00', ev: 'round-start', round: 1 },
    { t: '2026-08-21T10:30:00+03:00', ev: 'round-end', round: 1 },
    { t: '2026-08-21T10:35:00+03:00', ev: 'void', of: { ev: 'round-start', t: '2026-08-21T10:00:00+03:00', round: 1 } },
    { t: '2026-08-21T11:00:00+03:00', ev: 'pass-launch', pass: 'v2', type: 'verification' },
    { t: '2026-08-21T11:05:00+03:00', ev: 'verify-result', id: 'PPW-9221', verdict: 'held', commit: 'abc1234' },
    { t: '2026-08-21T11:10:00+03:00', ev: 'pass-records-done', pass: 'v2' },
  ].map(e => JSON.stringify(e)).join('\n') + '\n')
  const WARNING = `${target}: resolution-v1 resolved, no fix-round line yet — unit records pending`

  {
    const r = run('records-auditor.mjs', ['--root', T, target])
    check('auditor tolerates a resolved resolution with no fix-round line yet', r.code === 0 && r.out.includes(WARNING), `exit ${r.code}: ${r.out.trim()}`)
    check('void and verify-result worklog events read as valid shapes', !r.out.includes('worklog line'), r.out.split('\n').find(l => l.includes('worklog line')) ?? '')
  }

  const fixRoundLine = {
    target, round: 1, type: 'fix-round', date: '2026-08-21', base_commit: null, fixed_commit: null,
    findings: { fixed: 1, wont_fix: 0, deferred: 0, disputed: 0, false_positive: 0, open: 0 },
    tests: null, approach_checks: { pre_cleared_consumed: 0, run: 0, tokens: null },
    micro_reviews: { count: 0, follow_up_fixes: 0 }, cost: { agents: 0, tokens: null },
    runtime: { started: '2026-08-21T10:00:00+03:00', ended: '2026-08-21T10:30:00+03:00', active_s: 1800, blocked_s: 0, idle_s: 0, blocked: [] },
    notes: '',
  }
  writeFileSync(join(dir, 'metrics.jsonl'), JSON.stringify(fixRoundLine) + '\n')
  {
    const r = run('records-auditor.mjs', ['--root', T, target])
    check('once the fix-round line lands the warning is gone and the auditor stays exit 0', r.code === 0 && !r.out.includes('unit records pending'), `exit ${r.code}: ${r.out.trim()}`)
  }

  // A resolution closed before V3_CUTOFF never gets a fix-round line — that's a permanent fact
  // of the schema's history, not a pending unit, so it must never trigger the warning.
  const legacyTarget = '923-legacy-pre-cutoff'
  const legacyDir = join(T, 'reviews', legacyTarget)
  mkdirSync(legacyDir, { recursive: true })
  writeFileSync(join(legacyDir, 'metrics.jsonl'), '')
  writeFileSync(join(legacyDir, 'resolution-v1.md'), `---
type: resolution
target: ${legacyTarget}
version: 1
answers: review-v1.md
status: resolved
fixed_commit: abc1234
closed: 2026-07-15
---

# Resolution v1 — ${legacyTarget}

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9231 | fixed | \`abc1234\` | done |
`)
  {
    const r = run('records-auditor.mjs', ['--root', T, legacyTarget])
    check('a resolution closed before V3_CUTOFF stays silent even with no fix-round line', r.code === 0 && !r.out.includes('unit records pending'), `exit ${r.code}: ${r.out.trim()}`)
  }

  rmSync(T, { recursive: true, force: true })
}
