// Tests for speed-report.mjs: the bucket state machine, the acceptance metrics, --json
// round-tripping, and the mislabelled/duplicate round stamps a live worklog carries.
//
// The fixtures are verbatim copies of 038-039-invoicing's worklog.jsonl and metrics.jsonl.
// They carry five worklog events and three correction lines appended after the owner's
// 2026-08-22 reference measurement of 2026-08-21, so the reference ranges are asserted against
// the snapshot that measurement saw (the worklog cut at the v12 pass-launch, 175 events) and
// the frozen full day is pinned by its own exact numbers instead.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only speed-report
import { check, run } from './lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { tmpdir } from 'node:os'

const FIXTURE = join(dirname(fileURLToPath(import.meta.url)), 'fixtures', 'speed-report')
const TARGET = '038-039-invoicing'
const DAY = '2026-08-21'
// Where the owner's reference measurement stopped: the v12 certification launch.
const REF_CUT = Date.parse('2026-08-21T19:49:58+03:00')
const roots = []

const fixtureLines = f => readFileSync(join(FIXTURE, f), 'utf8').split(/\r?\n/).filter(l => l.trim())

function rootWith(worklog, metrics, target = TARGET) {
  const T = mkdtempSync(join(tmpdir(), 'speed-report-'))
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  writeFileSync(join(dir, 'worklog.jsonl'), worklog.join('\n') + '\n')
  if (metrics) writeFileSync(join(dir, 'metrics.jsonl'), metrics.join('\n') + '\n')
  roots.push(T)
  return T
}
const jsonOf = r => { try { return JSON.parse(r.out) } catch { return null } }
const within = (v, lo, hi) => typeof v === 'number' && v >= lo && v <= hi

// ---------- the frozen baseline: every number the day currently computes to ----------
{
  const T = rootWith(fixtureLines('worklog.jsonl'), fixtureLines('metrics.jsonl'))
  const r = run('speed-report.mjs', ['--root', T, TARGET, '--day', DAY, '--json'])
  check('frozen baseline exits 0', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  const j = jsonOf(r)
  check('frozen baseline emits parseable JSON', j != null, r.out.slice(0, 400))
  if (j) {
    check('frozen baseline day span', j.events === 180 && j.span_min === 763.4, `${j.events} events, span ${j.span_min}`)
    check('frozen baseline started/ended', j.started === '2026-08-21T08:07:59+03:00' && j.ended === '2026-08-21T20:51:20+03:00', `${j.started} → ${j.ended}`)
    check('frozen baseline fix-round work', j.buckets['fix-round work'].min === 262.1, JSON.stringify(j.buckets['fix-round work']))
    check('frozen baseline pass work', j.buckets['pass work'].min === 185.1, JSON.stringify(j.buckets['pass work']))
    check('frozen baseline records+gates', j.buckets['records+gates'].min === 201.7, JSON.stringify(j.buckets['records+gates']))
    check('frozen baseline idle/other', j.buckets['idle/other'].min === 114.6, JSON.stringify(j.buckets['idle/other']))
    check('frozen baseline owner wait is 0 — the day stamps no gate-open', j.buckets['owner wait'].min === 0, JSON.stringify(j.buckets['owner wait']))
    const sum = Object.values(j.buckets).reduce((a, b) => a + b.min, 0)
    check('the five buckets add up to the span', Math.abs(sum - j.span_min) <= 0.3, `${sum} vs ${j.span_min}`)
    check('bucket percentages are of the span', Math.abs(j.buckets['fix-round work'].pct - 34.3) < 0.1, String(j.buckets['fix-round work'].pct))
    const m = j.metrics
    check('frozen baseline median all-in min per fixed finding', m.all_in_min_per_fixed_finding.median_min_per_fixed === 25.2, JSON.stringify(m.all_in_min_per_fixed_finding.median_min_per_fixed))
    check('four rounds get an all-in row', m.all_in_min_per_fixed_finding.per_round.length === 4, JSON.stringify(m.all_in_min_per_fixed_finding.per_round.map(x => x.round)))
    const r9 = m.all_in_min_per_fixed_finding.per_round.find(x => x.round === 9)
    check("round 9's three parts run to its v10 verification", r9 && r9.all_in_min === 231.8 && r9.fixed === 8 && r9.verification_pass === 10, JSON.stringify(r9))
    const r10 = m.all_in_min_per_fixed_finding.per_round.find(x => x.round === 10)
    check('a round no verification pass follows still reports its gate time', r10 && r10.all_in_min === 42.8 && r10.verification_pass === null, JSON.stringify(r10))
    check('frozen baseline doc-gate sittings', m.doc_gate_first_pass_approval.sittings === 12 && m.doc_gate_first_pass_approval.first_pass === 8, JSON.stringify(m.doc_gate_first_pass_approval))
    check('frozen baseline record sittings per fixed finding', m.record_sittings_per_fixed_finding.fixed_findings === 29 && m.record_sittings_per_fixed_finding.per_fixed === 0.414, JSON.stringify(m.record_sittings_per_fixed_finding))
    check('correction lines are counted cumulatively to --day', m.correction_lines === 25, String(m.correction_lines))
  }

  // The live log mislabels rounds 6/7/8/9; none of it may abort the report.
  const notes = (j?.notes ?? []).join('\n')
  check('the duplicate round-start names both round numbers', /round-start at 2026-08-21T08:07:59\+03:00 \(round 6\) opens while round 7/.test(notes), notes)
  check('the round-6 round-end that closes nothing is named', /round-end at 2026-08-21T08:57:07\+03:00 \(round 6\) closes nothing/.test(notes), notes)
  check('the round-end mislabelled 7 is named as round 8\'s end', /round-end at 2026-08-21T10:44:01\+03:00 carries round 7 but closes the span opened at 2026-08-21T10:15:47\+03:00 with round 8/.test(notes), notes)
  check("round 9's two unstamped resumptions are named with their triage-done", /round-end at 2026-08-21T15:35:04\+03:00 closes a resumed round 9 .* opened at 2026-08-21T14:57:17\+03:00/.test(notes) && /round-end at 2026-08-21T16:58:06\+03:00 closes a resumed round 9 .* opened at 2026-08-21T16:15:29\+03:00/.test(notes), notes)
  check('the unopened gate-closed is named, not counted', /gate-closed at 2026-08-21T12:35:27\+03:00 .* closes nothing/.test(notes), notes)

  // ---------- --json round-trips: the text report prints the same numbers ----------
  const txt = run('speed-report.mjs', ['--root', T, TARGET, '--day', DAY])
  check('the text report exits 0', txt.code === 0, `exit ${txt.code}: ${txt.out.trim()}`)
  check('the text report prints the span', txt.out.includes('span 763.4 min'), txt.out.split('\n')[1])
  check('the text report prints the buckets', txt.out.includes('fix-round work     262.1 min') && txt.out.includes('records+gates      201.7 min'), txt.out)
  check('the text report prints the metrics', txt.out.includes('median of 4 round(s)): 25.2') && txt.out.includes('0.667  (8 of 12 sitting(s))') && txt.out.includes('correction lines: 25'), txt.out)
  check('every NOTE reaches both outputs', (j?.notes ?? []).every(n => txt.out.includes(n)), txt.out)
  const j2 = jsonOf(run('speed-report.mjs', ['--root', T, TARGET, '--day', DAY, '--json']))
  check('--json is stable across runs', JSON.stringify(j2) === JSON.stringify(j), 'two --json runs of the same input differ')
}

// ---------- the owner's reference measurement of 2026-08-21, reproduced ----------
{
  const T = rootWith(fixtureLines('worklog.jsonl').filter(l => Date.parse(JSON.parse(l).t) <= REF_CUT), fixtureLines('metrics.jsonl'))
  const j = jsonOf(run('speed-report.mjs', ['--root', T, TARGET, '--day', DAY, '--json']))
  check('the reference snapshot reports', j != null)
  if (j) {
    check('reference snapshot has the 175 events the measurement saw', j.events === 175, String(j.events))
    check('total span 700-704 min', within(j.span_min, 700, 704), String(j.span_min))
    check('fix-round work 255-275 min', within(j.buckets['fix-round work'].min, 255, 275), String(j.buckets['fix-round work'].min))
    check('records+gates 185-215 min', within(j.buckets['records+gates'].min, 185, 215), String(j.buckets['records+gates'].min))
    check('doc-gate first-pass approval 0.5-0.65', within(j.metrics.doc_gate_first_pass_approval.rate, 0.5, 0.65), String(j.metrics.doc_gate_first_pass_approval.rate))
    check('correction lines = 25', j.metrics.correction_lines === 25, String(j.metrics.correction_lines))
  }
}

// ---------- the state machine on a hand-built day ----------
{
  const wl = [
    { t: '2019-03-01T09:00:00+03:00', ev: 'round-start', round: 1 },
    { t: '2019-03-01T09:10:00+03:00', ev: 'triage-done', round: 1, clusters: 2 },
    { t: '2019-03-01T09:40:00+03:00', ev: 'finding', id: 'PPW-1', status: 'fixed' },
    { t: '2019-03-01T10:00:00+03:00', ev: 'round-end', round: 1 },
    { t: '2019-03-01T10:20:00+03:00', ev: 'doc-gate', round: 1, verdict: 'disapprove', reason: 'heading order' },
    { t: '2019-03-01T10:30:00+03:00', ev: 'doc-gate', round: 1, verdict: 'approve' },
    { t: '2019-03-01T11:00:00+03:00', ev: 'pass-launch', pass: 2, type: 'verification' },
    { t: '2019-03-01T11:30:00+03:00', ev: 'gate-open', reason: 'owner ruling' },
    { t: '2019-03-01T12:30:00+03:00', ev: 'gate-closed', reason: 'owner ruling' },
    { t: '2019-03-01T13:00:00+03:00', ev: 'pass-records-done', pass: 2, type: 'verification' },
    { t: '2019-03-01T15:00:00+03:00', ev: 'run-end', passes: 1 },
  ].map(e => JSON.stringify(e))
  const T = rootWith(wl, ['{"target":"t","date":"2019-03-01","correction_for":{"round":1,"field":"findings"},"note":"x"}'])
  const j = jsonOf(run('speed-report.mjs', ['--root', T, TARGET, '--json']))
  check('the hand-built day reports', j != null)
  if (j) {
    check('a round span is fix-round work', j.buckets['fix-round work'].min === 60, JSON.stringify(j.buckets['fix-round work']))
    check('a gate span inside a pass outranks the pass', j.buckets['owner wait'].min === 60, JSON.stringify(j.buckets['owner wait']))
    check('the rest of the pass is pass work', j.buckets['pass work'].min === 60, JSON.stringify(j.buckets['pass work']))
    check('round-end to gate and between gate events is records+gates', j.buckets['records+gates'].min === 60, JSON.stringify(j.buckets['records+gates']))
    check('a gap past the 30-minute cap is idle, not records', j.buckets['idle/other'].min === 120, JSON.stringify(j.buckets['idle/other']))
    check('a doc-gate sitting groups adjacent same-key events', j.metrics.doc_gate_first_pass_approval.sittings === 1, JSON.stringify(j.metrics.doc_gate_first_pass_approval))
    check('a sitting opening on a disapprove is not a first-pass approval', j.metrics.doc_gate_first_pass_approval.rate === 0, JSON.stringify(j.metrics.doc_gate_first_pass_approval))
    check('all-in runs round-start to the approve plus the verification that follows', j.metrics.all_in_min_per_fixed_finding.per_round[0].all_in_min === 210, JSON.stringify(j.metrics.all_in_min_per_fixed_finding.per_round[0]))
    check('an untagged fixed finding is attributed to the round span it sits in', j.metrics.all_in_min_per_fixed_finding.per_round[0].fixed === 1, JSON.stringify(j.metrics.all_in_min_per_fixed_finding.per_round[0]))
    check('a correction line without --day still counts', j.metrics.correction_lines === 1, String(j.metrics.correction_lines))
  }
  const other = jsonOf(run('speed-report.mjs', ['--root', T, TARGET, '--day', '2019-03-01', '--json']))
  check('--day keeps a day the events are on', other && other.events === 11, JSON.stringify(other?.events))
}

// ---------- void events drop what they match, before anything is measured ----------
{
  const wl = [
    { t: '2019-04-01T09:00:00+03:00', ev: 'round-start', round: 1 },
    { t: '2019-04-01T09:30:00+03:00', ev: 'round-end', round: 1 },
    { t: '2019-04-01T10:00:00+03:00', ev: 'round-start', round: 2 },
    { t: '2019-04-01T11:00:00+03:00', ev: 'round-end', round: 2 },
    { t: '2019-04-01T11:05:00+03:00', ev: 'void', of: { ev: 'round-end', t: '2019-04-01T09:30:00+03:00', round: 1 } },
  ].map(e => JSON.stringify(e))
  const T = rootWith(wl, null)
  const j = jsonOf(run('speed-report.mjs', ['--root', T, TARGET, '--json']))
  check('a voided round-end is gone before pairing, so round 1 stays open to 11:00', j != null && j.buckets['fix-round work'].min === 120, JSON.stringify(j?.buckets))
  check("the voided round's start is reported as closed by the next round-end", j != null && j.notes.some(n => /carries round 2 but closes the span opened at 2019-04-01T09:00:00\+03:00 with round 1/.test(n)), JSON.stringify(j?.notes))
  check('the round-start left with no end of its own is named as a duplicate', j != null && j.notes.some(n => /round-start at 2019-04-01T10:00:00\+03:00 \(round 2\) opens while round 1/.test(n)), JSON.stringify(j?.notes))
  check('a missing metrics.jsonl is a note, not a failure', j != null && j.metrics.correction_lines === 0 && j.notes.some(n => /metrics\.jsonl/.test(n)), JSON.stringify(j?.notes))
}

// ---------- refusals ----------
{
  const T = rootWith(['{"t":"2019-05-01T09:00:00+03:00","ev":"note","text":"x"}'], null)
  const bad = run('speed-report.mjs', ['--root', T, 'no-such-target'])
  check('an unknown target exits 1', bad.code === 1 && /no reviews\/no-such-target\//.test(bad.out), `exit ${bad.code}: ${bad.out.trim()}`)
  const badDay = run('speed-report.mjs', ['--root', T, TARGET, '--day', 'yesterday'])
  check('a malformed --day exits 1', badDay.code === 1 && /--day "yesterday"/.test(badDay.out), `exit ${badDay.code}: ${badDay.out.trim()}`)
  const emptyDay = run('speed-report.mjs', ['--root', T, TARGET, '--day', '2019-05-02'])
  check('a --day with no events exits 1', emptyDay.code === 1 && /no events on 2019-05-02/.test(emptyDay.out), `exit ${emptyDay.code}: ${emptyDay.out.trim()}`)
  check('no target exits 1 with the usage line', run('speed-report.mjs', ['--root', T]).code === 1)

  const broken = rootWith(['{"t":"2019-05-01T09:00:00+03:00","ev":"round-start","round":1}', '{oops'], null)
  const r = run('speed-report.mjs', ['--root', broken, TARGET])
  check('an unparseable worklog line exits 1 naming the line', r.code === 1 && /worklog line 2/.test(r.out), `exit ${r.code}: ${r.out.trim()}`)

  const noLog = mkdtempSync(join(tmpdir(), 'speed-report-'))
  mkdirSync(join(noLog, 'reviews', TARGET), { recursive: true })
  roots.push(noLog)
  const r2 = run('speed-report.mjs', ['--root', noLog, TARGET])
  check('a target with no worklog exits 1', r2.code === 1 && /worklog\.jsonl/.test(r2.out), `exit ${r2.code}: ${r2.out.trim()}`)
}

for (const T of roots) rmSync(T, { recursive: true, force: true })
