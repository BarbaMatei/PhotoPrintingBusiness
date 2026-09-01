// Tests for records-auditor.mjs: real-repo smoke checks, the reviewed-unit window, and the two
// seams the CLI now composes — records/validate.mjs and fix/handback-gates.mjs, called in-process.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only records-auditor
import { check, run, GOOD_ROOT } from '../lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'
import { auditIds, auditTarget, citationScan, listTargets } from '../../records/validate.mjs'
import { auditHandBackGates } from '../../fix/handback-gates.mjs'
import { versions } from '../../model/target.mjs'

// ---------- records auditor: smoke run against the real repo ----------
{
  const r = run('records/records-auditor.mjs', ['038-039-invoicing'])
  check('auditor exits 0 on the live target', r.code === 0, `exit ${r.code}: ${r.out.trim().split('\n').slice(-3).join(' | ')}`)
}
{
  // Archives are closed books (owner ruling, 2026-08-28): one note, and none of the per-line,
  // per-resolution or per-commit checks the live pass runs.
  const r = run('records/records-auditor.mjs', ['044-045-observability'])
  const lines = r.out.trim().split('\n').filter(l => l.trim())
  check('an archived target reports one skip note and nothing else',
    r.code === 0 && lines.length === 2 && /^note\s+044-045-observability \(archive\): closed books/.test(lines[0]) && /^0 error\(s\), 0 warning\(s\)/.test(lines[1]),
    `exit ${r.code}: ${r.out.trim()}`)
}
{
  const r = run('records/records-auditor.mjs', ['--root', GOOD_ROOT])
  check('auditor reports a cross-target duplicate id', r.out.includes('duplicate id PPW-9001'), 'no duplicate-id error in the output')
  check('auditor accepts a well-formed verification findings[] with lineage', !r.out.includes('908-verification-lineage metrics line 2 findings['), r.out.split('\n').find(l => l.includes('line 2 findings[')) ?? '')
  check('auditor rejects a malformed verification findings[] entry', r.out.includes('908-verification-lineage metrics line 3 findings[') && r.out.includes('d must be') && r.out.includes('sev_delta malformed'), 'no shape error for the malformed lineage entry')
}

// ---------- validate.mjs: the seams the CLI composes ----------
{
  const REVIEWS = join(GOOD_ROOT, 'reviews')
  const all = listTargets(REVIEWS)
  check('listTargets skips the folders under reviews/ that are not targets',
    !all.some(t => ['state', 'lib', 'archive', 'system'].includes(t.name)) && all.length > 30,
    JSON.stringify(all.map(t => t.name).slice(0, 5)))
  check('a positional filter matches target names by substring',
    listTargets(REVIEWS, { only: ['921'] }).map(t => t.name).join(',') === '921-gates-bad',
    JSON.stringify(listTargets(REVIEWS, { only: ['921'] }).map(t => t.name)))
  check('the cross-target scans ignore the filter, so a duplicate id is found whatever was asked for',
    listTargets(REVIEWS, { only: ['921'], all: true }).length === all.length,
    `${listTargets(REVIEWS, { only: ['921'], all: true }).length} vs ${all.length}`)

  // The validator reports through the reporters the CLI passes in and never prints or exits
  // itself; an archived target is not validated at all.
  const collect = () => { const o = { errors: [], warnings: [], infos: [] }; return { ...o, err: m => o.errors.push(m), warn: m => o.warnings.push(m), info: m => o.infos.push(m) } }
  const ctxFor = (c, gates) => ({ err: c.err, warn: c.warn, info: c.info, checkSha: () => ({ resolves: true, pushed: true }), versions, gates, index: null, track: null })

  let sawEvents = null
  const c1 = collect()
  auditTarget({ name: '954-voided-misstamp', dir: join(REVIEWS, '954-voided-misstamp'), archived: false },
    ctxFor(c1, (t, tag, roundDates, events) => { sawEvents = events }))
  check('auditTarget hands the hand-back gates the void-filtered events, not the raw log',
    sawEvents !== null && !sawEvents.some(e => e.ev === 'void'), JSON.stringify(sawEvents?.map(e => e.ev)))

  const T = mkdtempSync(join(tmpdir(), 'records-validate-tier-'))
  mkdirSync(T, { recursive: true })
  writeFileSync(join(T, 'metrics.jsonl'), '')
  writeFileSync(join(T, 'review-v1.md'), '---\npass-type: discovery\n---\n\n# Review v1\n')
  const c2 = collect()
  let archivedGateCalls = 0
  auditTarget({ name: '990-tier', dir: T, archived: true }, ctxFor(c2, () => { archivedGateCalls++ }))
  const c3 = collect()
  auditTarget({ name: '990-tier', dir: T, archived: false }, ctxFor(c3, () => { }))
  const PAIRING = 'review-v1.md has no metrics line'
  check('an archived target is skipped after one note, hand-back gates included',
    c2.infos.length === 1 && /closed books/.test(c2.infos[0]) && !c2.errors.length && !c2.warnings.length && archivedGateCalls === 0,
    `${JSON.stringify(c2)} · ${archivedGateCalls} gate call(s)`)
  check('the same records on a live target are an error — one severity, no lenient tier',
    c3.errors.some(m => m.includes(PAIRING)) && !c3.warnings.length,
    `live ${JSON.stringify(c3.errors)}`)
  rmSync(T, { recursive: true, force: true })

  // A certified live target is "under watch": the track-record check used to be exercised only
  // through an archived target, which is no longer validated at all.
  const C = mkdtempSync(join(tmpdir(), 'records-validate-certified-'))
  mkdirSync(C, { recursive: true })
  writeFileSync(join(C, 'metrics.jsonl'), `${JSON.stringify({
    target: '991-certified', pass: 1, type: 'discovery', subtype: 'certification-single',
    date: '2026-08-30', commit: 'abc1234', outcome: 'certified', mediums_open_at_close: 0,
    new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, findings: [],
  })}\n`)
  writeFileSync(join(C, 'review-v1.md'), '---\npass-type: certification\ncommit: abc1234\n---\n\n# Review v1\n')
  const cNoTrack = collect()
  auditTarget({ name: '991-certified', dir: C, archived: false }, ctxFor(cNoTrack, () => { }))
  const cTracked = collect()
  auditTarget({ name: '991-certified', dir: C, archived: false },
    { ...ctxFor(cTracked, () => { }), track: '| 991-certified | v1 |' })
  check('a live target holding a certification must be listed in the track record',
    cNoTrack.errors.some(m => /holds a certification but reviews\/state\/track-record\.md is missing/.test(m)) &&
    !cTracked.errors.length,
    `no track ${JSON.stringify(cNoTrack.errors)} · listed ${JSON.stringify(cTracked.errors)}`)
  rmSync(C, { recursive: true, force: true })

  // Archiving does not end a certification, and both holders are archived — so the watch-list
  // check survives the archive skip. The line it reads is pre-v2 shaped (a retired `certified`
  // field, prose in lenses[]): the scan must find the fact without validating anything.
  const K = mkdtempSync(join(tmpdir(), 'records-validate-archive-cert-'))
  mkdirSync(K, { recursive: true })
  writeFileSync(join(K, 'metrics.jsonl'), `${JSON.stringify({
    target: '995-archived-certified', pass: 9, type: 'discovery', subtype: 'certification-single',
    date: '2026-07-05', commit: 'abc1234', certified: 'serious-clean', lenses: ['prose about lenses'],
  })}\n`)
  const cUnlisted = collect()
  auditTarget({ name: '995-archived-certified', dir: K, archived: true },
    { ...ctxFor(cUnlisted, () => { }), track: '| 900-someone-else | v1 |' })
  check('an archived certification holder missing from the track record is still an error',
    cUnlisted.errors.length === 1 && /995-archived-certified \(archive\): holds a certification but is not listed in reviews\/state\/track-record\.md/.test(cUnlisted.errors[0]),
    JSON.stringify(cUnlisted.errors))
  const cListed = collect()
  auditTarget({ name: '995-archived-certified', dir: K, archived: true },
    { ...ctxFor(cListed, () => { }), track: '| 995-archived-certified | certified |' })
  check('a listed archived holder passes, and its pre-v2 fields are validated nowhere',
    !cListed.errors.length && !cListed.warnings.length && cListed.infos.length === 1,
    JSON.stringify([cListed.errors, cListed.warnings]))
  writeFileSync(join(K, 'metrics.jsonl'), `${JSON.stringify({
    target: '995-archived-certified', pass: 9, type: 'discovery', subtype: 'certification-single',
    date: '2026-08-30', commit: 'abc1234', outcome: 'not-certified',
  })}\n`)
  const cFailed = collect()
  auditTarget({ name: '995-archived-certified', dir: K, archived: true },
    { ...ctxFor(cFailed, () => { }), track: '| 900-someone-else | v1 |' })
  check('a recorded certification failure is not a holding, so the track record is not required',
    !cFailed.errors.length, JSON.stringify(cFailed.errors))
  rmSync(K, { recursive: true, force: true })

  // A line that is not a record object used to walk into a TypeError, which named no line at all.
  const N = mkdtempSync(join(tmpdir(), 'records-validate-non-record-'))
  mkdirSync(N, { recursive: true })
  writeFileSync(join(N, 'metrics.jsonl'), 'null\n{oops\n')
  const cBad = collect()
  auditTarget({ name: '992-non-record', dir: N, archived: false }, ctxFor(cBad, () => { }))
  check('a metrics line that is not a record is an error naming the line and the fault',
    cBad.errors.some(m => /992-non-record metrics line 1: not a JSON object \(null\)/.test(m)) &&
    cBad.errors.some(m => /992-non-record metrics line 2: unparseable JSON/.test(m)),
    JSON.stringify(cBad.errors))
  rmSync(N, { recursive: true, force: true })

  const c4 = collect()
  auditIds(listTargets(REVIEWS, { all: true }), { err: c4.err, counterPath: join(REVIEWS, 'state', 'id-counter') })
  check('auditIds reports an id whose rows live in two ledgers',
    c4.errors.some(m => /duplicate id PPW-9001/.test(m)), JSON.stringify(c4.errors))

  // The one check the archive ruling keeps: the id scan still reads a closed target's ledger, so
  // a mint that collides with an archived row is caught even though nothing else there is read.
  const A = mkdtempSync(join(tmpdir(), 'records-validate-archive-ids-'))
  const liveDir = join(A, '993-live'), archDir = join(A, 'archive', '994-archived')
  mkdirSync(liveDir, { recursive: true })
  mkdirSync(archDir, { recursive: true })
  const oneRow = id => `| ID | Sev | First seen | Title | File | Status | Affirmed |\n|---|---|---|---|---|---|---|\n| ${id} | 🔴 | v1 | a row | \`x.cs\` | open |  |\n`
  writeFileSync(join(liveDir, 'ledger.md'), oneRow('PPW-9931'))
  writeFileSync(join(archDir, 'ledger.md'), oneRow('PPW-9931'))
  const cIds = collect()
  auditIds(listTargets(A, { all: true }), { err: cIds.err, counterPath: join(A, 'no-such-counter') })
  check('the id scan still reads an archived ledger after the archive skip',
    cIds.errors.some(m => /duplicate id PPW-9931/.test(m) && /993-live/.test(m) && /994-archived/.test(m)),
    JSON.stringify(cIds.errors))
  rmSync(A, { recursive: true, force: true })

  // The scan counts comment text only: a finding id inside a string or a test name is the
  // review system's accepted leak channel and must not be reported.
  const c5 = collect()
  const fakeGrep = () => ['src/A.cs:3:    // PPW-1 fixed here', 'src/B.cs:9:    var s = "PPW-2";', 'src/C.ts:4:  const url = "http://x/PPW-3"'].join('\n')
  const hits = citationScan({ git: fakeGrep, info: c5.info, warn: c5.warn })
  check('the citation scan counts a comment and skips a string and a URL',
    hits.length === 1 && /A\.cs:3/.test(hits[0]), JSON.stringify(hits))
  check('the scan reports its count as a note, never as an error',
    c5.infos.length === 1 && /1 occurrence\(s\) in 1 file\(s\)/.test(c5.infos[0]) && !c5.warnings.length, JSON.stringify(c5.infos))
  // git grep exits 1 for "nothing matched" — a clean answer — and 128 for a scan it could not
  // run at all. Reading the second as zero occurrences would report a whole-repo backstop as
  // clean when it never ran, so the two are pinned apart.
  const exiting = status => () => { throw Object.assign(new Error(`git grep failed (${status})`), { status }) }
  const c6 = collect()
  check('a git grep exiting 1 reads as no matches, and the note still lands',
    citationScan({ git: exiting(1), info: c6.info, warn: c6.warn }).length === 0 &&
    /0 occurrence\(s\)/.test(c6.infos[0]) && !c6.warnings.length, JSON.stringify([c6.infos, c6.warnings]))
  const c7 = collect()
  check('a git grep that could not run warns and returns nothing, never a clean zero',
    citationScan({ git: exiting(128), info: c7.info, warn: c7.warn }) === null &&
    !c7.infos.length && c7.warnings.length === 1 && /could not run \(exit 128\)/.test(c7.warnings[0]),
    JSON.stringify([c7.infos, c7.warnings]))
}

// ---------- handback-gates.mjs: called directly ----------
{
  const REVIEWS = join(GOOD_ROOT, 'reviews')
  const t = { name: '921-gates-bad', dir: join(REVIEWS, '921-gates-bad'), archived: false }
  const said = []
  auditHandBackGates(t, t.name, new Map(), [], m => said.push(m))
  check('with no events at all the gates refuse for want of a round-start, and say so once',
    said.length === 1 && /no round-start worklog event for round 1/.test(said[0]), JSON.stringify(said))

  const good = { name: '922-gates-good', dir: join(REVIEWS, '922-gates-good'), archived: false }
  const quiet = []
  auditHandBackGates(good, good.name, new Map(), JSON.parse('[]'), m => quiet.push(m))
  check('the gates report through the tier they are given and nowhere else',
    quiet.length === 1 && /922-gates-good resolution-v1\.md/.test(quiet[0]), JSON.stringify(quiet))
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
    const r = run('records/records-auditor.mjs', ['--root', T, target])
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
    const r = run('records/records-auditor.mjs', ['--root', T, target])
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
    const r = run('records/records-auditor.mjs', ['--root', T, legacyTarget])
    check('a resolution closed before V3_CUTOFF stays silent even with no fix-round line', r.code === 0 && !r.out.includes('unit records pending'), `exit ${r.code}: ${r.out.trim()}`)
  }

  rmSync(T, { recursive: true, force: true })
}

{
  const r = run('records/records-auditor.mjs', ['--root', GOOD_ROOT])
  check('auditor accepts seed_round and area lineage keys when well-formed', !r.out.includes('line 2 findings[0]: seed_round') && !r.out.includes('line 2 findings[0]: area'), r.out.split('\n').find(l => l.includes('line 2 findings[0]')) ?? '')
  check('auditor rejects a non-numeric seed_round and an off-vocabulary area', r.out.includes('seed_round must be a round number or null') && r.out.includes('area "checkout" — one of the twelve backlog area words only'), 'no seed-lineage shape errors in the output')

  // hand-back gates (audit R1–R4, applied only to rounds closed on/after 2026-08-28)
  check('auditor refuses a resolved round with no round-review pair (R3)', r.out.includes('921-gates-bad resolution-v1.md: code was fixed but the round has no round-review-dispatched/returned pair'), 'no R3 refusal in the output')
  check('auditor refuses red test runs with no test-meaning audit (R4)', r.out.includes('921-gates-bad resolution-v1.md: regression tests ran red but no test-audit-returned event'), 'no R4 refusal in the output')
  check('auditor refuses a trigger-classified fix with no check evidence (R2)', r.out.includes('PPW-9211 is trigger-classified by its fix brief but no pre-check verdict was consumed and no check-dispatched event names it'), 'no R2 refusal in the output')
  check('auditor refuses an overlap cluster with no protocol block event (R1)', r.out.includes('PPW-9212, PPW-9213 share a stateful surface') && r.out.includes('no protocol-written event covers them'), 'no R1 missing-protocol refusal in the output')
  check('auditor refuses a protocol written after the cluster was fixed (R1 spec-theatre)', r.out.includes('PPW-9214, PPW-9215') && r.out.includes('timestamped after the cluster\'s first finding event'), 'no R1 ordering refusal in the output')
  check('auditor accepts a round carrying all four kinds of hand-back evidence', !r.out.includes('922-gates-good resolution-v1.md:'), r.out.split('\n').find(l => l.includes('922-gates-good resolution-v1.md:')) ?? '')
  // A mis-stamp repaired with a void is erased for every reader, so it cannot gate hand-back.
  check('a voided mis-stamp no longer trips the R1 ordering gate',
    !r.out.includes('954-voided-misstamp resolution-v1.md:'), r.out.split('\n').find(l => l.includes('954-voided-misstamp resolution-v1.md:')) ?? '')
  check('auditor grandfathers resolved rounds from before the 2026-08-28 cut-off', !r.out.includes('901-good-target resolution-v1.md:') && !r.out.includes('901-good-target: resolved without'), r.out.split('\n').find(l => l.includes('901-good-target resolution')) ?? '')
}
