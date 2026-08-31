#!/usr/bin/env node
// Speed report — where a review loop's wall-clock time went and the acceptance metrics the
// loop is graded on. Reads a target's worklog.jsonl (void events drop what they match) and
// metrics.jsonl; writes nothing.
//
// Definitions. Every gap between consecutive events is charged to exactly one bucket, priority
// gate > round > pass: `owner wait` inside a gate-open→gate-closed span; `fix-round work`
// inside a round span (a round-start opens it, the next round-end closes it; two starts on one
// instant are one stamp restamped and the later wins; a round-end with nothing open is a resumed
// round opening at its latest same-numbered triage-done, unless that would reach across a closed
// run or a whole pass, which is a stray stamp); `pass work` inside pass-launch→pass-records-
// done; `records+gates` outside those when the gap touches a doc-gate, or runs on from a
// round-end/pass-records-done/doc-gate and is no longer than the 30-minute nobody-at-the-wheel
// cap the metrics schema already uses; `idle/other` the rest. Metrics: `all-in min per fixed
// finding` = per round, its first round-start → the first doc-gate approve after its last
// round-end, plus the verification pass that follows before the next round starts, over its fixed
// findings (median across rounds); `doc-gate first-pass approval` = share of gate sittings (adjacent
// doc-gate events sharing a round/pass key) whose first event is not a disapproval; `record
// sittings per fixed finding` = sittings ÷ fixed findings; `correction lines` = metrics.jsonl
// lines carrying correction_for, counted cumulatively up to --day, an undated one left out.
//
// Second mode, the lint miner: `--disapprovals` lists the doc gate's judge disapprovals across
// every target, live and archived, each with a stub checklist line, so a person decides which
// become deterministic doc-gate checks (the scan and its date rule live in measure/gates.mjs).
// Positional arguments narrow it by substring of the target name; --day and --json are the
// measurement's flags and do not apply to it.
//
// Usage: node reviews/lib/speed-report.mjs [--root <repoRoot>] <target> [--day YYYY-MM-DD] [--json]
//        node reviews/lib/speed-report.mjs [--root <repoRoot>] --disapprovals [--since YYYY-MM-DD] [target ...]
// Exit: 0 = printed · 1 = no such target, no worklog, a worklog line that is unparseable JSON or
//       carries an unparseable timestamp, a flag given no value, a malformed --day, a --day the
//       worklog has no events on, or a flag used outside its own mode. A --disapprovals scan that
//       matches nothing still exits 0.
import { existsSync } from 'node:fs'
import { join } from 'node:path'
import { dayOf, disapprovalLines, epochOf, isApprove, isDisapproval } from './measure/gates.mjs'
import { at as atOf, covers, lenientSpans, REFUSED, short, spanOf, stamp as stampOf } from './model/spans.mjs'
import { repoRoot } from './cli/args.mjs'
import { readMetrics } from './records/metrics.mjs'
import { live, readLines } from './records/worklog.mjs'

const CAP_MIN = 30
const RECORDS_ANCHOR = new Set(['round-end', 'pass-records-done', 'doc-gate'])
const STOPS_RECORDS = new Set(['run-end', 'gate-parked', 'gate-open'])

const USAGE = 'usage: speed-report.mjs [--root <repoRoot>] <target> [--day YYYY-MM-DD] [--json] · speed-report.mjs [--root <repoRoot>] --disapprovals [--since YYYY-MM-DD] [target ...]'
function fail(m) { console.error(`ERROR   ${m}`); process.exit(1) }

const argv = process.argv.slice(2)
let ROOT = null, DAY = null, SINCE = null, JSON_OUT = false, DISAPPROVALS = false
const rest = []
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === '--root' || argv[i] === '--day' || argv[i] === '--since') {
    const flag = argv[i], value = argv[++i]
    if (value == null || /^--/.test(value)) fail(`${flag} needs a value; ${USAGE}`)
    if (flag === '--root') ROOT = value
    else if (flag === '--day') DAY = value
    else SINCE = value
  } else if (argv[i] === '--json') JSON_OUT = true
  else if (argv[i] === '--disapprovals') DISAPPROVALS = true
  else rest.push(argv[i])
}
ROOT = repoRoot(import.meta.url, ROOT)

// ---------- the lint miner: every target's gate disapprovals, no target argument needed ----------
if (DISAPPROVALS) {
  if (DAY || JSON_OUT) fail(`--disapprovals reports the gate disapprovals, so --day and --json do not apply; ${USAGE}`)
  try { console.log(disapprovalLines(join(ROOT, 'reviews'), { since: SINCE, filters: rest }).join('\n')) }
  catch (e) { fail(`reading the worklogs: ${e.message}`) }
  process.exit(0)
}
if (SINCE) fail(`--since applies to --disapprovals only; ${USAGE}`)

const target = rest[0]
if (!target) fail(USAGE)
if (DAY != null && !/^\d{4}-\d{2}-\d{2}$/.test(DAY)) fail(`--day "${DAY}" — an ISO date, e.g. 2026-08-21; ${USAGE}`)
const dir = join(ROOT, 'reviews', target)
if (!existsSync(dir)) fail(`no reviews/${target}/ under ${ROOT}`)

const notes = []
const note = m => notes.push(m)
const min = ms => ms / 60000
const round1 = n => Math.round(n * 10) / 10
const round3 = n => Math.round(n * 1000) / 1000

// ---------- worklog: void filtering, day filtering, stable order by instant ----------
function loadEvents() {
  const lines = readLines(dir)
  if (lines === null) fail(`no reviews/${target}/worklog.jsonl — nothing to measure`)
  for (const l of lines) if (l.error) fail(`worklog line ${l.n}: unparseable JSON (${l.error.message})`)
  return live(lines.map(l => l.event))
}

const all = loadEvents()
for (const e of all) if (!Number.isFinite(epochOf(e))) fail(`worklog event with unparseable timestamp: ${JSON.stringify(e)}`)
const events = (DAY ? all.filter(e => dayOf(e) === DAY) : all)
  .map((e, i) => ({ e, i, ms: epochOf(e) }))
  .sort((a, b) => a.ms - b.ms || a.i - b.i)
  .map(x => x.e)
if (!events.length) fail(DAY ? `worklog has no events on ${DAY}` : `reviews/${target}/worklog.jsonl is empty`)

const at = i => atOf(events, i)
const stamp = (ev, i) => stampOf(events, ev, i)

// ---------- spans: rounds, passes, owner gates (the lenient strategy — see model/spans.mjs) ----------
const pairSpans = (startEv, endEv, key, resume) => lenientSpans(events, { startEv, endEv, key, resume, note })

const passSpans = pairSpans('pass-launch', 'pass-records-done', 'pass', null)

// A resumed round restarts at its latest triage-done; an end reaching back past a run or pass is dropped.
const resumeRound = (e, i, spans) => {
  const after = spans.filter(s => s.key === e.round).reduce((a, s) => Math.max(a, s.to), -1)
  let from = null
  for (let j = i - 1; j > after; j--) if (events[j].ev === 'triage-done' && events[j].round === e.round) { from = j; break }
  if (from == null) return null
  const runEnd = events.slice(from, i + 1).find(x => x.ev === 'run-end')
  const pass = passSpans.find(s => s.from >= from && s.to <= i)
  const across = runEnd ? `the run-end at ${runEnd.t}` : pass ? `the whole of pass ${pass.key}` : null
  if (!across) return from
  note(`${stamp('round-end', i)} (round ${e.round}) would resume ${round1(min(epochOf(e) - epochOf(events[from])))} min back at the triage-done at ${at(from)}, across ${across} — the stray end is reported, not charged`)
  return REFUSED
}

const roundSpans = pairSpans('round-start', 'round-end', 'round', resumeRound)
const gateSpans = pairSpans('gate-open', 'gate-closed', 'reason', null)

// ---------- buckets ----------
const buckets = { 'owner wait': 0, 'fix-round work': 0, 'pass work': 0, 'records+gates': 0, 'idle/other': 0 }
let recordsOpen = false
for (let i = 0; i < events.length - 1; i++) {
  const a = events[i], b = events[i + 1]
  if (RECORDS_ANCHOR.has(a.ev)) recordsOpen = true
  else if (STOPS_RECORDS.has(a.ev)) recordsOpen = false
  const gap = min(epochOf(b) - epochOf(a))
  let bucket
  if (covers(gateSpans, i)) bucket = 'owner wait'
  else if (covers(roundSpans, i)) bucket = 'fix-round work'
  else if (covers(passSpans, i)) bucket = 'pass work'
  else if (a.ev === 'doc-gate' || b.ev === 'doc-gate') bucket = 'records+gates'
  else if (recordsOpen && gap <= CAP_MIN) bucket = 'records+gates'
  else bucket = 'idle/other'
  buckets[bucket] += gap
}
const spanMin = min(epochOf(events[events.length - 1]) - epochOf(events[0]))

// ---------- doc-gate sittings ----------
const gateKey = e => (e.round != null ? `round ${e.round}` : e.pass != null ? `pass ${e.pass}` : 'unkeyed')

const sittings = []
for (let i = 0; i < events.length; i++) {
  if (events[i].ev !== 'doc-gate') continue
  const last = sittings[sittings.length - 1]
  if (last && last.to === i - 1 && last.key === gateKey(events[i])) { last.to = i; last.events.push(events[i]) }
  else sittings.push({ key: gateKey(events[i]), from: i, to: i, events: [events[i]] })
}
const firstPass = sittings.filter(s => !isDisapproval(s.events[0])).length

// ---------- findings ----------
const fixedByRound = new Map()
let fixedTotal = 0
for (let i = 0; i < events.length; i++) {
  const e = events[i]
  if (e.ev !== 'finding' || e.status !== 'fixed') continue
  fixedTotal++
  const r = spanOf(roundSpans, i)?.key ?? e.round
  if (r != null) fixedByRound.set(r, (fixedByRound.get(r) ?? 0) + 1)
}

// ---------- all-in minutes per fixed finding ----------
const perRound = []
for (const key of [...new Set(roundSpans.map(s => s.key))].sort((a, b) => a - b)) {
  const mine = roundSpans.filter(s => s.key === key)
  const startMs = epochOf(events[mine[0].from])
  const endIdx = mine[mine.length - 1].to
  let gateIdx = null
  for (let i = endIdx + 1; i < events.length; i++) if (events[i].ev === 'doc-gate' && isApprove(events[i])) { gateIdx = i; break }
  let verifyMin = 0, verifyPass = null
  if (gateIdx != null) {
    // A verification launched after the next round has started answers that round, not this one.
    let limit = events.length
    for (let i = gateIdx + 1; i < events.length; i++) if (events[i].ev === 'round-start') { limit = i; break }
    const v = passSpans.find(s => s.from >= gateIdx && s.from < limit && events[s.from].type === 'verification')
    if (v) { verifyMin = min(epochOf(events[v.to]) - epochOf(events[v.from])); verifyPass = v.key }
  }
  const fixed = fixedByRound.get(key) ?? 0
  const allIn = gateIdx == null ? null : min(epochOf(events[gateIdx]) - startMs) + verifyMin
  perRound.push({ round: key, all_in_min: allIn == null ? null : round1(allIn), fixed, verification_pass: verifyPass, per_fixed_min: allIn == null || !fixed ? null : round1(allIn / fixed) })
  if (gateIdx == null) note(`round ${key} has no doc-gate approve after its last round-end — its all-in time is unmeasured, not zero`)
}
const ratios = perRound.map(r => r.per_fixed_min).filter(v => v != null).sort((a, b) => a - b)
const median = !ratios.length ? null
  : ratios.length % 2 ? ratios[(ratios.length - 1) / 2]
    : round1((ratios[ratios.length / 2 - 1] + ratios[ratios.length / 2]) / 2)

// ---------- correction lines ----------
const metrics = readMetrics(dir)
let corrections = 0, undated = 0
if (!metrics) note(`no reviews/${target}/metrics.jsonl — correction lines counted as 0`)
else for (const o of metrics.corrections) {
  if (!o.date) { undated++; continue }
  if (!DAY || o.date <= DAY) corrections++
}
if (undated) note(`${undated} correction line(s) carry no date — left out of the count rather than dated by guess`)

// ---------- report ----------
const report = {
  target, day: DAY, events: events.length,
  started: events[0].t, ended: events[events.length - 1].t, span_min: round1(spanMin),
  buckets: Object.fromEntries(Object.entries(buckets).map(([k, v]) => [k, { min: round1(v), pct: spanMin ? round1((v / spanMin) * 100) : 0 }])),
  metrics: {
    all_in_min_per_fixed_finding: { per_round: perRound, median_min_per_fixed: median },
    doc_gate_first_pass_approval: { sittings: sittings.length, first_pass: firstPass, rate: sittings.length ? round3(firstPass / sittings.length) : null },
    record_sittings_per_fixed_finding: { sittings: sittings.length, fixed_findings: fixedTotal, per_fixed: fixedTotal ? round3(sittings.length / fixedTotal) : null },
    correction_lines_cumulative: corrections,
  },
  notes,
}

if (JSON_OUT) { console.log(JSON.stringify(report, null, 2)); process.exit(0) }

const pad = (s, n) => String(s).padEnd(n)
const num = (v, n = 8) => String(v == null ? '—' : v).padStart(n)
console.log(`speed report  ${target}${DAY ? `  ${DAY}` : ''}`)
console.log(`${report.events} events  ${report.started} → ${report.ended}  span ${report.span_min} min`)
console.log('')
console.log('buckets')
for (const [k, v] of Object.entries(report.buckets)) console.log(`  ${pad(k, 16)}${num(v.min)} min${num(v.pct, 7)}%`)
console.log('')
console.log('metrics')
console.log(`  all-in min per fixed finding (median of ${ratios.length} round(s)): ${num(median, 0)}`)
for (const r of perRound) console.log(`    round ${pad(r.round, 3)}${num(r.all_in_min)} min  ÷ ${num(r.fixed, 3)} fixed = ${num(r.per_fixed_min)} min/finding${r.verification_pass != null ? `  (incl. verification pass ${r.verification_pass})` : ''}`)
console.log(`  doc-gate first-pass approval: ${num(report.metrics.doc_gate_first_pass_approval.rate, 0)}  (${firstPass} of ${sittings.length} sitting(s))`)
console.log(`  record sittings per fixed finding: ${num(report.metrics.record_sittings_per_fixed_finding.per_fixed, 0)}  (${sittings.length} sitting(s) ÷ ${fixedTotal} fixed)`)
console.log(`  correction lines (cumulative${DAY ? ` to ${DAY}` : ''}): ${corrections}`)
if (notes.length) {
  console.log('')
  for (const n of notes) console.log(`NOTE    ${n}`)
}
