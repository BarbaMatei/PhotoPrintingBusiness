#!/usr/bin/env node
// Speed report — where a review loop's wall-clock time went and the acceptance metrics the
// loop is graded on. Reads a target's worklog.jsonl (void events drop what they match) and
// metrics.jsonl; writes nothing.
//
// Definitions. Every gap between consecutive events is charged to exactly one bucket, priority
// gate > round > pass: `owner wait` inside a gate-open→gate-closed span; `fix-round work`
// inside a round span (a round-start opens it, the next round-end closes it — a round-end with
// nothing open is a resumed round and opens at its next same-numbered triage-done, because a
// resumption's round-start often goes unstamped); `pass work` inside pass-launch→pass-records-
// done; `records+gates` outside those when the gap touches a doc-gate, or runs on from a
// round-end/pass-records-done/doc-gate and is no longer than the 30-minute nobody-at-the-wheel
// cap the metrics schema already uses; `idle/other` the rest. Metrics: `all-in min per fixed
// finding` = per round, its first round-start → the first doc-gate approve after its last
// round-end, plus the verification pass that follows, over that round's fixed findings (median
// across rounds); `doc-gate first-pass approval` = share of gate sittings (runs of adjacent
// doc-gate events sharing a round/pass key) whose first event is not a disapproval; `record
// sittings per fixed finding` = sittings ÷ fixed findings; `correction lines` = metrics.jsonl
// lines carrying correction_for, counted cumulatively up to --day.
//
// Usage: node reviews/lib/speed-report.mjs [--root <repoRoot>] <target> [--day YYYY-MM-DD] [--json]
// Exit: 0 = printed · 1 = no such target, no/unreadable worklog, a malformed --day, or a --day
//       the worklog has no events on.
import { readFileSync, existsSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const CAP_MIN = 30
const RECORDS_ANCHOR = new Set(['round-end', 'pass-records-done', 'doc-gate'])
const STOPS_RECORDS = new Set(['run-end', 'gate-parked', 'gate-open'])
const DISAPPROVAL_FIELDS = ['verdict', 'judge', 'reason', 'note']

const argv = process.argv.slice(2)
let ROOT = null, DAY = null, JSON_OUT = false
const rest = []
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === '--root') ROOT = argv[++i]
  else if (argv[i] === '--day') DAY = argv[++i]
  else if (argv[i] === '--json') JSON_OUT = true
  else rest.push(argv[i])
}
if (!ROOT) ROOT = join(dirname(fileURLToPath(import.meta.url)), '..', '..')
const USAGE = 'usage: speed-report.mjs [--root <repoRoot>] <target> [--day YYYY-MM-DD] [--json]'

function fail(m) { console.error(`ERROR   ${m}`); process.exit(1) }

const target = rest[0]
if (!target) fail(USAGE)
if (DAY != null && !/^\d{4}-\d{2}-\d{2}$/.test(DAY)) fail(`--day "${DAY}" — an ISO date, e.g. 2026-08-21; ${USAGE}`)
const dir = join(ROOT, 'reviews', target)
if (!existsSync(dir)) fail(`no reviews/${target}/ under ${ROOT}`)

const notes = []
const note = m => notes.push(m)
const min = ms => ms / 60000
const round1 = n => Math.round(n * 10) / 10
const round2 = n => Math.round(n * 100) / 100
const round3 = n => Math.round(n * 1000) / 1000

// ---------- worklog: void filtering, day filtering, stable order by instant ----------
function loadEvents() {
  const wlPath = join(dir, 'worklog.jsonl')
  if (!existsSync(wlPath)) fail(`no reviews/${target}/worklog.jsonl — nothing to measure`)
  const logged = readFileSync(wlPath, 'utf8').split(/\r?\n/).filter(l => l.trim()).map((l, i) => {
    try { return JSON.parse(l) } catch (e) { fail(`worklog line ${i + 1}: unparseable JSON (${e.message})`) }
  })
  const sameVal = (a, b) => a === b || (!!a && !!b && typeof a === 'object' && typeof b === 'object' && JSON.stringify(a) === JSON.stringify(b))
  const voids = logged.filter(e => e.ev === 'void' && e.of && typeof e.of === 'object' && Object.keys(e.of).length)
  const matchesVoid = (e, v) => Object.keys(v.of).every(k => sameVal(e[k], v.of[k]))
  return logged.filter(e => e.ev !== 'void' && !voids.some(v => matchesVoid(e, v)))
}

// Real worklogs mix `Z` and offset stamps, so the day comes from the parsed instant.
const epochOf = e => Date.parse(e.t)
const dayOf = e => new Date(epochOf(e)).toISOString().slice(0, 10)

const all = loadEvents()
for (const e of all) if (!Number.isFinite(epochOf(e))) fail(`worklog event with unparseable timestamp: ${JSON.stringify(e)}`)
const events = (DAY ? all.filter(e => dayOf(e) === DAY) : all)
  .map((e, i) => ({ e, i, ms: epochOf(e) }))
  .sort((a, b) => a.ms - b.ms || a.i - b.i)
  .map(x => x.e)
if (!events.length) fail(DAY ? `worklog has no events on ${DAY}` : `reviews/${target}/worklog.jsonl is empty`)

const at = i => events[i].t
const stamp = (ev, i) => `${ev} at ${at(i)}`

// ---------- spans: rounds, passes, owner gates (index ranges, so duplicate stamps stay apart) ----------
function pairSpans(startEv, endEv, key, resume) {
  const spans = []
  let open = null
  for (let i = 0; i < events.length; i++) {
    const e = events[i]
    if (e.ev === startEv) {
      if (open) { note(`${stamp(startEv, i)} (${key} ${e[key]}) opens while ${key} ${open.key} from ${at(open.from)} is still open — the duplicate is ignored`); continue }
      open = { key: e[key], from: i }
    } else if (e.ev === endEv) {
      if (open) {
        if (open.key !== e[key] && e[key] != null) note(`${stamp(endEv, i)} carries ${key} ${e[key]} but closes the span opened at ${at(open.from)} with ${key} ${open.key} — read it as ${open.key}'s end`)
        spans.push({ key: open.key, from: open.from, to: i })
        open = null
      } else {
        const from = resume ? resume(e, i, spans) : null
        if (from == null) note(`${stamp(endEv, i)} (${key} ${e[key]}) closes nothing and no resumption stamp precedes it — ignored`)
        else { spans.push({ key: e[key], from, to: i }); note(`${stamp(endEv, i)} closes a resumed ${key} ${e[key]} whose ${startEv} went unstamped — opened at ${at(from)}`) }
      }
    }
  }
  if (open) { spans.push({ key: open.key, from: open.from, to: events.length - 1 }); note(`${stamp(startEv, open.from)} has no ${endEv} — measured to the last event`) }
  return spans
}

// A resumed fix round re-triages before it works again, so its triage-done is where it restarts.
const resumeRound = (e, i, spans) => {
  const after = spans.filter(s => s.key === e.round).reduce((a, s) => Math.max(a, s.to), -1)
  for (let j = after + 1; j < i; j++) if (events[j].ev === 'triage-done' && events[j].round === e.round) return j
  return null
}

const roundSpans = pairSpans('round-start', 'round-end', 'round', resumeRound)
const passSpans = pairSpans('pass-launch', 'pass-records-done', 'pass', null)
const gateSpans = pairSpans('gate-open', 'gate-closed', 'reason', null)

const covers = (spans, i) => spans.some(s => i >= s.from && i < s.to)
const spanOf = (spans, i) => spans.find(s => i >= s.from && i <= s.to)

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
const isDisapproval = e => DISAPPROVAL_FIELDS.some(k => typeof e[k] === 'string' && /disapprove/i.test(e[k]))
const isApprove = e => !isDisapproval(e) && DISAPPROVAL_FIELDS.some(k => typeof e[k] === 'string' && /^approve/i.test(e[k]))

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
    const v = passSpans.find(s => s.from >= gateIdx && events[s.from].type === 'verification')
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
const metricsPath = join(dir, 'metrics.jsonl')
let corrections = 0
if (!existsSync(metricsPath)) note(`no reviews/${target}/metrics.jsonl — correction lines counted as 0`)
else for (const l of readFileSync(metricsPath, 'utf8').split(/\r?\n/).filter(l => l.trim())) {
  let o; try { o = JSON.parse(l) } catch { continue }
  if (o.correction_for && (!DAY || !o.date || o.date <= DAY)) corrections++
}

// ---------- report ----------
const report = {
  target, day: DAY, events: events.length,
  started: events[0].t, ended: events[events.length - 1].t, span_min: round1(spanMin),
  buckets: Object.fromEntries(Object.entries(buckets).map(([k, v]) => [k, { min: round1(v), pct: spanMin ? round1((v / spanMin) * 100) : 0 }])),
  metrics: {
    all_in_min_per_fixed_finding: { per_round: perRound, median_min_per_fixed: median },
    doc_gate_first_pass_approval: { sittings: sittings.length, first_pass: firstPass, rate: sittings.length ? round3(firstPass / sittings.length) : null },
    record_sittings_per_fixed_finding: { sittings: sittings.length, fixed_findings: fixedTotal, per_fixed: fixedTotal ? round3(sittings.length / fixedTotal) : null },
    correction_lines: corrections,
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
console.log(`  correction lines: ${corrections}`)
if (notes.length) {
  console.log('')
  for (const n of notes) console.log(`NOTE    ${n}`)
}
