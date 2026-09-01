// Tests for speed-report.mjs: the bucket state machine, the acceptance metrics, --json
// round-tripping, the mislabelled/duplicate/stray round stamps a live worklog carries, and the
// `--disapprovals` lint miner — its disapproval mining, since-filtering and mixed-offset
// timestamp bucketing (measure/gates.mjs).
//
// The fixtures are verbatim copies of 038-039-invoicing's worklog.jsonl and metrics.jsonl.
// They carry five worklog events and three correction lines appended after the owner's
// 2026-08-22 reference measurement of 2026-08-21, so the reference ranges are asserted against
// the snapshot that measurement saw (the worklog cut at the v12 pass-launch, 175 events) and
// the frozen full day is pinned by its own exact numbers instead.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only speed-report
import { check, run, SPEED_FIXTURE as FIXTURE } from '../lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'

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
  writeFileSync(join(dir, 'worklog.jsonl'), worklog.map(e => typeof e === 'string' ? e : JSON.stringify(e)).join('\n') + '\n')
  if (metrics) writeFileSync(join(dir, 'metrics.jsonl'), metrics.join('\n') + '\n')
  roots.push(T)
  return T
}
const jsonOf = r => { try { return JSON.parse(r.out) } catch { return null } }
const reportOf = (T, ...args) => jsonOf(run('measure/speed-report.mjs', ['--root', T, TARGET, ...args, '--json']))
const within = (v, lo, hi) => typeof v === 'number' && v >= lo && v <= hi

// ---------- the frozen baseline: every number the day currently computes to ----------
{
  const T = rootWith(fixtureLines('worklog.jsonl'), fixtureLines('metrics.jsonl'))
  const r = run('measure/speed-report.mjs', ['--root', T, TARGET, '--day', DAY, '--json'])
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
    const rows = m.all_in_min_per_fixed_finding.per_round
    check('frozen baseline median all-in min per fixed finding', m.all_in_min_per_fixed_finding.median_min_per_fixed === 25.2, String(m.all_in_min_per_fixed_finding.median_min_per_fixed))
    check('the day yields four round rows, keyed 6/8/9/10', JSON.stringify(rows.map(x => x.round)) === '[6,8,9,10]', JSON.stringify(rows.map(x => x.round)))
    const r6 = rows.find(x => x.round === 6)
    check('the restamped 08:07:59 round reads as 6, and no verification follows its gate', r6 && r6.all_in_min === 124 && r6.fixed === 16 && r6.verification_pass === null && r6.per_fixed_min === 7.8, JSON.stringify(r6))
    const r8 = rows.find(x => x.round === 8)
    check('round 8 keeps the v8 verification that follows its gate', r8 && r8.all_in_min === 123.5 && r8.fixed === 3 && r8.verification_pass === 8, JSON.stringify(r8))
    const r9 = rows.find(x => x.round === 9)
    check("round 9's three parts run to its v10 verification", r9 && r9.all_in_min === 231.8 && r9.fixed === 8 && r9.verification_pass === 10 && r9.per_fixed_min === 29, JSON.stringify(r9))
    const r10 = rows.find(x => x.round === 10)
    check('a round no verification pass follows still reports its gate time', r10 && r10.all_in_min === 42.8 && r10.fixed === 2 && r10.verification_pass === null, JSON.stringify(r10))
    check('frozen baseline doc-gate sittings', m.doc_gate_first_pass_approval.sittings === 12 && m.doc_gate_first_pass_approval.first_pass === 8 && m.doc_gate_first_pass_approval.rate === 0.667, JSON.stringify(m.doc_gate_first_pass_approval))
    check('frozen baseline record sittings per fixed finding', m.record_sittings_per_fixed_finding.fixed_findings === 29 && m.record_sittings_per_fixed_finding.per_fixed === 0.414, JSON.stringify(m.record_sittings_per_fixed_finding))
    check('the corrections count is named cumulative and is 25', m.correction_lines_cumulative === 25, JSON.stringify(m))
  }

  // The live log restamps and mislabels rounds 6/7/8/9; none of it may abort the report.
  const notes = (j?.notes ?? []).join('\n')
  check('the same-instant round-6 stamp is read as a restamp of round 7', /round-start at 2026-08-21T08:07:59\+03:00 \(round 6\) restamps the round 7 opened at the same instant/.test(notes), notes)
  check("that span's round-end carries a different number and is named", /round-end at 2026-08-21T08:57:07\+03:00 carries round 7 but closes the span opened at 2026-08-21T08:07:59\+03:00 with round 6/.test(notes), notes)
  check('the round-6 round-end that closes nothing is named', /round-end at 2026-08-21T08:57:07\+03:00 \(round 6\) closes nothing/.test(notes), notes)
  check("the round-end mislabelled 7 is named as round 8's end", /round-end at 2026-08-21T10:44:01\+03:00 carries round 7 but closes the span opened at 2026-08-21T10:15:47\+03:00 with round 8/.test(notes), notes)
  check("round 9's two unstamped resumptions are named with their triage-done", /round-end at 2026-08-21T15:35:04\+03:00 closes a resumed round 9 .* opened at 2026-08-21T14:57:17\+03:00/.test(notes) && /round-end at 2026-08-21T16:58:06\+03:00 closes a resumed round 9 .* opened at 2026-08-21T16:15:29\+03:00/.test(notes), notes)
  check('the unopened gate-closed is named with its reason truncated', /gate-closed at 2026-08-21T12:35:27\+03:00 \(reason owner ruled the 4 parked items and authorised the push: P…\) closes nothing/.test(notes), notes)

  // ---------- --json round-trips: the text report prints the same numbers ----------
  const txt = run('measure/speed-report.mjs', ['--root', T, TARGET, '--day', DAY])
  check('the text report exits 0', txt.code === 0, `exit ${txt.code}: ${txt.out.trim()}`)
  check('the text report prints the span', txt.out.includes('span 763.4 min'), txt.out.split('\n')[1])
  check('the text report prints the buckets', txt.out.includes('fix-round work     262.1 min') && txt.out.includes('records+gates      201.7 min'), txt.out)
  check('the text report prints the metrics', txt.out.includes('median of 4 round(s)): 25.2') && txt.out.includes('0.667  (8 of 12 sitting(s))') && txt.out.includes('correction lines (cumulative to 2026-08-21): 25'), txt.out)
  check('every NOTE reaches both outputs', (j?.notes ?? []).every(n => txt.out.includes(n)), txt.out)
  const j2 = reportOf(T, '--day', DAY)
  check('--json is stable across runs', JSON.stringify(j2) === JSON.stringify(j), 'two --json runs of the same input differ')
}

// ---------- the owner's reference measurement of 2026-08-21, reproduced ----------
{
  const T = rootWith(fixtureLines('worklog.jsonl').filter(l => Date.parse(JSON.parse(l).t) <= REF_CUT), fixtureLines('metrics.jsonl'))
  const j = reportOf(T, '--day', DAY)
  check('the reference snapshot reports', j != null)
  if (j) {
    check('reference snapshot has the 175 events the measurement saw', j.events === 175, String(j.events))
    check('total span 700-704 min', within(j.span_min, 700, 704), String(j.span_min))
    check('fix-round work 255-275 min', within(j.buckets['fix-round work'].min, 255, 275), String(j.buckets['fix-round work'].min))
    check('records+gates 185-215 min', within(j.buckets['records+gates'].min, 185, 215), String(j.buckets['records+gates'].min))
    check('doc-gate first-pass approval 0.5-0.65', within(j.metrics.doc_gate_first_pass_approval.rate, 0.5, 0.65), String(j.metrics.doc_gate_first_pass_approval.rate))
    check('correction lines = 25', j.metrics.correction_lines_cumulative === 25, String(j.metrics.correction_lines_cumulative))
  }
}

// ---------- the state machine on a hand-built day ----------
{
  const T = rootWith([
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
  ], ['{"target":"t","date":"2019-03-01","correction_for":{"round":1,"field":"findings"},"note":"x"}'])
  const j = reportOf(T)
  check('the hand-built day reports', j != null)
  if (j) {
    check('a round span is fix-round work', j.buckets['fix-round work'].min === 60, JSON.stringify(j.buckets['fix-round work']))
    check('a gate span inside a pass outranks the pass', j.buckets['owner wait'].min === 60, JSON.stringify(j.buckets['owner wait']))
    check('the rest of the pass is pass work', j.buckets['pass work'].min === 60, JSON.stringify(j.buckets['pass work']))
    check('round-end to gate and between gate events is records+gates', j.buckets['records+gates'].min === 60, JSON.stringify(j.buckets['records+gates']))
    check('a gap past the 30-minute cap is idle, not records', j.buckets['idle/other'].min === 120, JSON.stringify(j.buckets['idle/other']))
    check('a doc-gate sitting groups adjacent same-key events', j.metrics.doc_gate_first_pass_approval.sittings === 1, JSON.stringify(j.metrics.doc_gate_first_pass_approval))
    check('a sitting opening on a disapprove is not a first-pass approval', j.metrics.doc_gate_first_pass_approval.rate === 0, JSON.stringify(j.metrics.doc_gate_first_pass_approval))
    const row = j.metrics.all_in_min_per_fixed_finding.per_round[0]
    check('all-in runs round-start to the approve plus the verification that follows', row.all_in_min === 210, JSON.stringify(row))
    check('an untagged fixed finding is attributed to the round span it sits in', row.fixed === 1, JSON.stringify(row))
    check('a correction line without --day still counts', j.metrics.correction_lines_cumulative === 1, String(j.metrics.correction_lines_cumulative))
  }
  const other = reportOf(T, '--day', '2019-03-01')
  check('--day keeps a day the events are on', other && other.events === 11, JSON.stringify(other?.events))
}

// ---------- a stray round-end may not reach back across a run or a whole pass ----------
{
  const strayDay = [
    { t: '2019-06-01T09:00:00+03:00', ev: 'round-start', round: 1 },
    { t: '2019-06-01T09:10:00+03:00', ev: 'triage-done', round: 1 },
    { t: '2019-06-01T09:30:00+03:00', ev: 'round-end', round: 1 },
    { t: '2019-06-01T09:45:00+03:00', ev: 'triage-done', round: 1 },
    { t: '2019-06-01T10:00:00+03:00', ev: 'run-end', passes: 1 },
    { t: '2019-06-01T11:00:00+03:00', ev: 'pass-launch', pass: 2, type: 'verification' },
    { t: '2019-06-01T13:00:00+03:00', ev: 'pass-records-done', pass: 2, type: 'verification' },
    { t: '2019-06-01T15:30:00+03:00', ev: 'round-end', round: 1 },
  ]
  const j = reportOf(rootWith(strayDay, null))
  check('the stray end is not charged as fix-round work', j != null && j.buckets['fix-round work'].min === 30, JSON.stringify(j?.buckets))
  check('the pass it spans keeps its own time', j != null && j.buckets['pass work'].min === 120 && j.buckets['idle/other'].min === 210, JSON.stringify(j?.buckets))
  check('the refusal NOTE names the span it would have charged, in minutes', j != null && j.notes.some(n => /round-end at 2019-06-01T15:30:00\+03:00 \(round 1\) would resume 345 min back at the triage-done at 2019-06-01T09:45:00\+03:00, across the run-end at 2019-06-01T10:00:00\+03:00 — the stray end is reported, not charged/.test(n)), JSON.stringify(j?.notes))

  const noRunEnd = strayDay.filter(e => e.ev !== 'run-end')
  const k = reportOf(rootWith(noRunEnd, null))
  check('a whole pass inside the would-be span refuses it too', k != null && k.buckets['fix-round work'].min === 30 && k.notes.some(n => /across the whole of pass 2 — the stray end is reported, not charged/.test(n)), JSON.stringify(k?.notes))

  const resumable = [
    { t: '2019-06-02T09:00:00+03:00', ev: 'round-start', round: 1 },
    { t: '2019-06-02T09:30:00+03:00', ev: 'round-end', round: 1 },
    { t: '2019-06-02T10:00:00+03:00', ev: 'triage-done', round: 1 },
    { t: '2019-06-02T10:15:00+03:00', ev: 'triage-done', round: 1 },
    { t: '2019-06-02T11:00:00+03:00', ev: 'round-end', round: 1 },
  ]
  const g = reportOf(rootWith(resumable, null))
  check('a clean resumption opens at the LAST triage-done before the stray end', g != null && g.buckets['fix-round work'].min === 75, JSON.stringify(g?.buckets))
  check('the resumption is named', g != null && g.notes.some(n => /closes a resumed round 1 .* opened at 2019-06-02T10:15:00\+03:00/.test(n)), JSON.stringify(g?.notes))

  const preSpan = [
    { t: '2019-06-03T09:00:00+03:00', ev: 'round-start', round: 1 },
    { t: '2019-06-03T09:10:00+03:00', ev: 'triage-done', round: 1 },
    { t: '2019-06-03T09:30:00+03:00', ev: 'round-end', round: 1 },
    { t: '2019-06-03T10:00:00+03:00', ev: 'round-end', round: 1 },
  ]
  const p = reportOf(rootWith(preSpan, null))
  check('a resumption never reaches back into the round\'s own closed span', p != null && p.buckets['fix-round work'].min === 30 && p.notes.some(n => /closes nothing and no resumption stamp precedes it/.test(n)), JSON.stringify(p?.notes))
}

// ---------- a verification launched after the next round has started is not this round's ----------
{
  const T = rootWith([
    { t: '2019-07-01T09:00:00+03:00', ev: 'round-start', round: 1 },
    { t: '2019-07-01T09:20:00+03:00', ev: 'finding', id: 'PPW-1', status: 'fixed' },
    { t: '2019-07-01T09:30:00+03:00', ev: 'round-end', round: 1 },
    { t: '2019-07-01T09:40:00+03:00', ev: 'doc-gate', round: 1, verdict: 'approve' },
    { t: '2019-07-01T09:50:00+03:00', ev: 'round-start', round: 2 },
    { t: '2019-07-01T10:00:00+03:00', ev: 'pass-launch', pass: 3, type: 'verification' },
    { t: '2019-07-01T10:30:00+03:00', ev: 'pass-records-done', pass: 3, type: 'verification' },
    { t: '2019-07-01T10:40:00+03:00', ev: 'round-end', round: 2 },
  ], null)
  const j = reportOf(T)
  const row = j?.metrics.all_in_min_per_fixed_finding.per_round.find(x => x.round === 1)
  check('an intervening round-start bounds the verification search', row && row.all_in_min === 40 && row.verification_pass === null, JSON.stringify(row))
  const row2 = j?.metrics.all_in_min_per_fixed_finding.per_round.find(x => x.round === 2)
  check('a round with no approve after it is unmeasured, not zero', row2 && row2.all_in_min === null && j.notes.some(n => /round 2 has no doc-gate approve/.test(n)), JSON.stringify(row2))
}

// ---------- two round-starts on one instant: the later is the correction ----------
{
  const j = reportOf(rootWith([
    { t: '2019-08-01T09:00:00+03:00', ev: 'round-start', round: 5 },
    { t: '2019-08-01T09:00:00+03:00', ev: 'round-start', round: 6 },
    { t: '2019-08-01T10:00:00+03:00', ev: 'round-end', round: 6 },
  ], null))
  check('the restamped span is keyed by the later stamp', j != null && JSON.stringify(j.metrics.all_in_min_per_fixed_finding.per_round.map(x => x.round)) === '[6]', JSON.stringify(j?.metrics.all_in_min_per_fixed_finding.per_round))
  check('the restamp costs no time', j != null && j.buckets['fix-round work'].min === 60, JSON.stringify(j?.buckets))
  check('the restamp is named', j != null && j.notes.some(n => /round-start at 2019-08-01T09:00:00\+03:00 \(round 6\) restamps the round 5 opened at the same instant/.test(n)), JSON.stringify(j?.notes))
}

// ---------- --day buckets by the parsed instant's UTC day, not the ISO-string prefix ----------
{
  const T = rootWith([
    { t: '2026-08-21T23:30:00+03:00', ev: 'round-start', round: 1 },
    { t: '2026-08-22T01:00:00+03:00', ev: 'round-end', round: 1 },
    { t: '2026-08-22T04:00:00+03:00', ev: 'doc-gate', round: 1, verdict: 'approve' },
  ], null)
  const j = reportOf(T, '--day', DAY)
  check('an after-midnight local stamp still on the UTC day is kept', j != null && j.events === 2 && j.ended === '2026-08-22T01:00:00+03:00', JSON.stringify({ events: j?.events, ended: j?.ended }))
  check('the kept event is measured, not just counted', j != null && j.buckets['fix-round work'].min === 90, JSON.stringify(j?.buckets))
  const next = reportOf(T, '--day', '2026-08-22')
  check('the event past the UTC boundary lands on the next day', next != null && next.events === 1, JSON.stringify(next?.events))
}

// ---------- void events drop what they match, before anything is measured ----------
{
  const T = rootWith([
    { t: '2019-04-01T09:00:00+03:00', ev: 'round-start', round: 1 },
    { t: '2019-04-01T09:30:00+03:00', ev: 'round-end', round: 1 },
    { t: '2019-04-01T10:00:00+03:00', ev: 'round-start', round: 2 },
    { t: '2019-04-01T11:00:00+03:00', ev: 'round-end', round: 2 },
    { t: '2019-04-01T11:05:00+03:00', ev: 'void', of: { ev: 'round-end', t: '2019-04-01T09:30:00+03:00', round: 1 } },
  ], null)
  const j = reportOf(T)
  check('a voided round-end is gone before pairing, so round 1 stays open to 11:00', j != null && j.buckets['fix-round work'].min === 120, JSON.stringify(j?.buckets))
  check("the voided round's start is reported as closed by the next round-end", j != null && j.notes.some(n => /carries round 2 but closes the span opened at 2019-04-01T09:00:00\+03:00 with round 1/.test(n)), JSON.stringify(j?.notes))
  check('a later start on a different instant is a duplicate, not a restamp', j != null && j.notes.some(n => /round-start at 2019-04-01T10:00:00\+03:00 \(round 2\) opens while round 1 .* the duplicate is ignored/.test(n)), JSON.stringify(j?.notes))
  check('a missing metrics.jsonl is a note, not a failure', j != null && j.metrics.correction_lines_cumulative === 0 && j.notes.some(n => /metrics\.jsonl/.test(n)), JSON.stringify(j?.notes))
}

// ---------- an undated correction line is left out, not dated by guess ----------
{
  const T = rootWith([{ t: '2019-09-01T09:00:00+03:00', ev: 'note', text: 'x' }], [
    '{"target":"t","date":"2019-09-01","correction_for":{"round":1,"field":"findings"},"note":"a"}',
    '{"target":"t","correction_for":{"round":2,"field":"findings"},"note":"b"}',
    '{"target":"t","date":"2019-09-09","correction_for":{"round":3,"field":"findings"},"note":"c"}',
  ])
  const j = reportOf(T, '--day', '2019-09-01')
  check('only dated corrections at or before --day count', j != null && j.metrics.correction_lines_cumulative === 1, String(j?.metrics.correction_lines_cumulative))
  check('the undated correction is reported', j != null && j.notes.some(n => /1 correction line\(s\) carry no date/.test(n)), JSON.stringify(j?.notes))
  const all = reportOf(T)
  check('without --day the undated line is still left out', all != null && all.metrics.correction_lines_cumulative === 2, String(all?.metrics.correction_lines_cumulative))
}

// ---------- a metrics line that is not a record is named, never silently dropped ----------
// The correction count is read off these lines, so a line left out without a word makes the
// measurement quietly disagree with the file.
{
  const T = rootWith([{ t: '2019-09-01T09:00:00+03:00', ev: 'note', text: 'x' }], [
    'null',
    '{"target":"t","date":"2019-09-02","correction_for":{"round":1,"field":"findings"},"note":"a"}',
    '{oops',
  ])
  const j = reportOf(T)
  check('each skipped metrics line is reported with its line number and why',
    j != null && j.notes.some(n => /metrics\.jsonl line 1 is not a record \(not a JSON object \(null\)\)/.test(n)) &&
    j.notes.some(n => /metrics\.jsonl line 3 is not a record \(unparseable JSON/.test(n)),
    JSON.stringify(j?.notes))
  check('the records around a skipped line still count',
    j != null && j.metrics.correction_lines_cumulative === 1, String(j?.metrics.correction_lines_cumulative))
}

// ---------- refusals ----------
{
  const T = rootWith([{ t: '2019-05-01T09:00:00+03:00', ev: 'note', text: 'x' }], null)
  const bad = run('measure/speed-report.mjs', ['--root', T, 'no-such-target'])
  check('an unknown target exits 1', bad.code === 1 && /no reviews\/no-such-target\//.test(bad.out), `exit ${bad.code}: ${bad.out.trim()}`)
  const badDay = run('measure/speed-report.mjs', ['--root', T, TARGET, '--day', 'yesterday'])
  check('a malformed --day exits 1', badDay.code === 1 && /--day "yesterday"/.test(badDay.out), `exit ${badDay.code}: ${badDay.out.trim()}`)
  const emptyDay = run('measure/speed-report.mjs', ['--root', T, TARGET, '--day', '2019-05-02'])
  check('a --day with no events exits 1', emptyDay.code === 1 && /no events on 2019-05-02/.test(emptyDay.out), `exit ${emptyDay.code}: ${emptyDay.out.trim()}`)
  const dayNoValue = run('measure/speed-report.mjs', ['--root', T, TARGET, '--day'])
  check('--day with no value exits 1', dayNoValue.code === 1 && /--day needs a value/.test(dayNoValue.out), `exit ${dayNoValue.code}: ${dayNoValue.out.trim()}`)
  const rootNoValue = run('measure/speed-report.mjs', [TARGET, '--root', '--json'])
  check('--root swallowing the next flag exits 1', rootNoValue.code === 1 && /--root needs a value/.test(rootNoValue.out), `exit ${rootNoValue.code}: ${rootNoValue.out.trim()}`)
  check('no target exits 1 with the usage line', run('measure/speed-report.mjs', ['--root', T]).code === 1)

  const broken = rootWith(['{"t":"2019-05-01T09:00:00+03:00","ev":"round-start","round":1}', '{oops'], null)
  const r = run('measure/speed-report.mjs', ['--root', broken, TARGET])
  check('an unparseable worklog line exits 1 naming the line', r.code === 1 && /worklog line 2/.test(r.out), `exit ${r.code}: ${r.out.trim()}`)

  const badTime = rootWith(['{"t":"the other day","ev":"round-start","round":1}'], null)
  const r3 = run('measure/speed-report.mjs', ['--root', badTime, TARGET])
  check('an unparseable timestamp exits 1', r3.code === 1 && /unparseable timestamp/.test(r3.out), `exit ${r3.code}: ${r3.out.trim()}`)

  const noLog = mkdtempSync(join(tmpdir(), 'speed-report-'))
  mkdirSync(join(noLog, 'reviews', TARGET), { recursive: true })
  roots.push(noLog)
  const r2 = run('measure/speed-report.mjs', ['--root', noLog, TARGET])
  check('a target with no worklog exits 1', r2.code === 1 && /worklog\.jsonl/.test(r2.out), `exit ${r2.code}: ${r2.out.trim()}`)

  const sinceNoValue = run('measure/speed-report.mjs', ['--disapprovals', '--root', T, '--since'])
  check('--since with no value exits 1', sinceNoValue.code === 1 && /--since needs a value/.test(sinceNoValue.out), `exit ${sinceNoValue.code}: ${sinceNoValue.out.trim()}`)
  const sinceAlone = run('measure/speed-report.mjs', ['--root', T, TARGET, '--since', '2019-05-01'])
  check('--since without --disapprovals exits 1', sinceAlone.code === 1 && /--since applies to --disapprovals only/.test(sinceAlone.out), `exit ${sinceAlone.code}: ${sinceAlone.out.trim()}`)
  const minedJson = run('measure/speed-report.mjs', ['--disapprovals', '--root', T, '--json'])
  check('--disapprovals refuses the measurement flags instead of ignoring them', minedJson.code === 1 && /--day and --json do not apply/.test(minedJson.out), `exit ${minedJson.code}: ${minedJson.out.trim()}`)
}

// ---------- --disapprovals (the lint miner): detection, since-filtering, summary ----------
// Fixture dates sit in 2019, far from the real wall-clock date, so a --since default that
// wrongly used "now" instead of "the newest event seen" would drop everything and fail these.
const mined = (T, ...args) => run('measure/speed-report.mjs', ['--disapprovals', '--root', T, ...args])
const minerRoot = tag => {
  const T = mkdtempSync(join(tmpdir(), `speed-report-${tag}-`))
  roots.push(T)
  return T
}
const minerWorklog = (T, target, events, archived) => {
  const dir = join(T, 'reviews', ...(archived ? ['archive', target] : [target]))
  mkdirSync(dir, { recursive: true })
  writeFileSync(join(dir, 'worklog.jsonl'), events.map(e => JSON.stringify(e)).join('\n') + '\n')
}

{
  const T = minerRoot('miner-core')
  const target = '960-gate-miner-core'
  const REASON_A = 'heading order mismatch in review-v1.md'
  const JUDGE_C = 'disapprove-then-fixed: fixed_commit rendered as the closing sentence text'
  const NOTE_B = 'nothing unusual, quick approve'
  const REASON_VOIDED = 'disapprove stamped against the wrong round, repaired with a void'
  const STUB = '[ ] lintable? -> add a check to doc-gate.mjs + a fixture to run-tests.mjs'
  minerWorklog(T, target, [
    { t: '2019-01-01T10:00:00+03:00', ev: 'doc-gate', verdict: 'disapprove', round: 1, reason: REASON_A },
    { t: '2019-01-05T10:00:00+03:00', ev: 'doc-gate', verdict: 'disapprove', round: 4, reason: REASON_VOIDED },
    { t: '2019-01-15T09:00:00+03:00', ev: 'doc-gate', verdict: 'approve', round: 2, note: NOTE_B },
    { t: '2019-01-20T11:00:00+03:00', ev: 'doc-gate', round: 9, lint: 'clean', judge: JUDGE_C, auditor: '0 errors' },
    { t: '2019-01-21T12:00:00+03:00', ev: 'void', of: { ev: 'doc-gate', t: '2019-01-05T10:00:00+03:00', reason: REASON_VOIDED } },
  ])

  {
    const r = mined(T)
    check('--disapprovals exits 0 with no --since', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
    check('both disapprovals print their full reason/judge text', r.out.includes(REASON_A) && r.out.includes(JUDGE_C), r.out.trim())
    check('the approve event does not print', !r.out.includes(NOTE_B), r.out.trim())
    check('a voided disapproval is neither printed nor counted', !r.out.includes(REASON_VOIDED), r.out.trim())
    check('default --since (30 days before the newest event seen) counts both', r.out.includes('total disapprovals: 2'), r.out.trim())
    check('the summary breaks the count down per target', r.out.includes(`${target}: 2`), r.out.trim())
    const stubCount = r.out.split(STUB).length - 1
    check('two distinct reasons each get their own stub checklist line', stubCount === 2, `stub appeared ${stubCount} time(s)`)
  }
  {
    const r = mined(T, '--since', '2019-01-10')
    check('--since excludes the earlier disapproval', r.code === 0 && !r.out.includes(REASON_A), r.out.trim())
    check('--since keeps the later disapproval', r.out.includes(JUDGE_C), r.out.trim())
    check('the filtered summary counts 1', r.out.includes('total disapprovals: 1'), r.out.trim())
  }
  {
    const r = mined(T, '--since', '2019-02-01')
    check('a --since after every event yields zero, not an error', r.code === 0 && r.out.includes('total disapprovals: 0'), `exit ${r.code}: ${r.out.trim()}`)
    check('no reason text leaks into a zero-match run', !r.out.includes(REASON_A) && !r.out.includes(JUDGE_C), r.out.trim())
  }
}

// ---------- --disapprovals: per-target summary, archive scan by default, target filtering, dedup ----------
{
  const T = minerRoot('miner-multi')
  const SHARED_REASON = 'filename used as link text'
  minerWorklog(T, '962-gate-miner-multi-a', [
    { t: '2019-02-01T08:00:00Z', ev: 'doc-gate', verdict: 'disapprove', round: 4, reason: SHARED_REASON },
    { t: '2019-02-02T08:00:00Z', ev: 'doc-gate', verdict: 'disapprove', round: 5, reason: 'dense multi-id sentence in Decisions' },
  ])
  minerWorklog(T, '963-gate-miner-multi-b', [
    { t: '2019-02-03T08:00:00Z', ev: 'doc-gate', pass: 3, verdict: 'disapprove', reason: SHARED_REASON },
  ])
  minerWorklog(T, '964-gate-miner-archived', [
    { t: '2019-02-04T08:00:00Z', ev: 'doc-gate', verdict: 'disapprove', round: 1, reason: 'unauthorized metrics-schema vocabulary in prose' },
  ], true)

  {
    const r = mined(T)
    check('--disapprovals scans archived targets by default', r.code === 0 && r.out.includes('964-gate-miner-archived: 1'), r.out.trim())
    check('per-target counts are correct across live targets', r.out.includes('962-gate-miner-multi-a: 2') && r.out.includes('963-gate-miner-multi-b: 1'), r.out.trim())
    check('the total sums every target, live and archived', r.out.includes('total disapprovals: 4'), r.out.trim())
    check('round/pass prints whichever field is present', r.out.includes('round 4') && r.out.includes('pass 3'), r.out.trim())
    const stubCount = r.out.split('[ ] lintable?').length - 1
    check('a reason repeated across targets gets one stub line, not two', stubCount === 3, `stub appeared ${stubCount} time(s)`)
  }
  {
    const r = mined(T, '964-gate-miner-archived')
    check('a positional target argument narrows the scan', r.code === 0 && r.out.includes('total disapprovals: 1'), r.out.trim())
    check('the narrowed scan excludes the other targets', !r.out.includes('962-gate-miner-multi-a') && !r.out.includes('963-gate-miner-multi-b'), r.out.trim())
  }
}

// ---------- --disapprovals: mixed Z/offset timestamps — bucketing and print order by instant ----------
// The real 038-039 worklog mixes `Z` and `+03:00` stamps. A raw ISO-string compare mis-orders
// and mis-buckets across that mix: "...T23:30:00Z" sorts before "...T01:00:00+03:00" the next
// calendar day even though the +03:00 stamp is the earlier instant (it's 3h behind its own
// written day). Per measure/gates.mjs's bucketing contract, both are judged by parsed instant.
{
  const T = minerRoot('miner-tz')
  const REASON_Z = 'Z stamp reading the earlier day by string alone'
  const REASON_OFFSET = 'offset stamp reading the next local day but the earlier instant'
  minerWorklog(T, '966-gate-miner-mixed-tz', [
    { t: '2026-08-21T23:30:00Z', ev: 'doc-gate', verdict: 'disapprove', round: 1, reason: REASON_Z },
    { t: '2026-08-22T01:00:00+03:00', ev: 'doc-gate', verdict: 'disapprove', round: 2, reason: REASON_OFFSET },
  ])

  {
    const r = mined(T)
    check('both mixed-offset disapprovals are counted', r.code === 0 && r.out.includes('total disapprovals: 2'), r.out.trim())
    check('both bucket to the same true UTC day (2026-08-21), not two different days',
      (r.out.match(/2026-08-21 ·/g) || []).length === 2 && !r.out.includes('2026-08-22 ·'), r.out.trim())
    check('the earlier instant (the +03:00 stamp) prints before the later one (the Z stamp)',
      r.out.indexOf(REASON_OFFSET) > -1 && r.out.indexOf(REASON_OFFSET) < r.out.indexOf(REASON_Z), r.out.trim())
  }
  {
    const r = mined(T, '--since', '2026-08-22')
    check('--since buckets by the true UTC day: both real instants are 2026-08-21, so both are excluded (the +03:00 stamp\'s raw string alone would have wrongly survived)',
      r.code === 0 && r.out.includes('total disapprovals: 0'), r.out.trim())
    check('neither reason text leaks through the boundary', !r.out.includes(REASON_OFFSET) && !r.out.includes(REASON_Z), r.out.trim())
  }
  {
    const r = mined(T, '--since', '2026-08-21')
    check('--since on the true UTC day itself keeps both', r.code === 0 && r.out.includes('total disapprovals: 2'), r.out.trim())
  }
}

// ---------- --disapprovals: an approved round's note mentioning "disapprove" still matches ----------
// Real shape, 038-039-invoicing worklog line 8: an overall-approved pass whose note narrates
// per-round disapprovals inline. The pinned match rule (verdict/judge/reason/note, case
// insensitive) doesn't care about the top-level verdict — this locks that in.
{
  const T = minerRoot('miner-approve-note')
  const NOTE = 'lint clean throughout; Sonnet judge disapproved rounds 1-4 (heading/title mismatches, unauthorized metrics-schema vocabulary in prose) — approved round 5'
  minerWorklog(T, '967-gate-miner-approve-note', [
    { t: '2026-08-13T08:45:00Z', ev: 'doc-gate', pass: 'v1', verdict: 'approve', rounds: 5, note: NOTE },
  ])

  const r = mined(T)
  check('an approve-verdict event whose note mentions "disapprove" still matches', r.code === 0 && r.out.includes(NOTE), r.out.trim())
  check('the summary counts it despite the overall verdict being approve', r.out.includes('total disapprovals: 1'), r.out.trim())
  check('round/pass falls back to the pass field when there is no round', r.out.includes('pass v1'), r.out.trim())
}

for (const T of roots) rmSync(T, { recursive: true, force: true })
