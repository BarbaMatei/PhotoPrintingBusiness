// The doc gate as the measurements read it: whether a gate event is a disapproval, which UTC day
// an event happened on, and the lint miner's report over every target. One home, because the
// speed report's first-pass-approval metric and the miner's scan asked the same question with two
// copies of the answer.
//
// Lint miner (reporter half): lists the doc gate's judge disapprovals with a stub checklist line,
// so a human/agent can decide which become deterministic doc-gate.mjs checks. It never edits
// anything and never classifies "lintable" itself. `speed-report.mjs --disapprovals` is its CLI.
// Bucketing/ordering contract: every date comparison (--since, "newest seen", print order) uses
// the event's parsed instant normalized to its UTC calendar day, never the raw ISO-string prefix —
// real worklogs mix `Z` and offset (`+03:00`) stamps, and a naive string compare mis-buckets and
// mis-orders across that mix.
import { readdirSync, existsSync } from 'node:fs'
import { join } from 'node:path'
import { readEvents } from '../records/worklog.mjs'
import { TARGETLESS } from '../records/schema.mjs'

// The field that trips the match isn't necessarily the one worth printing — priority differs.
const MATCH_FIELDS = ['verdict', 'judge', 'reason', 'note']
const TEXT_FIELDS = ['reason', 'judge', 'note', 'verdict']
const STUB = '[ ] lintable? -> add a check to doc-gate.mjs + a fixture to run-tests.mjs'

export const isDisapproval = e => MATCH_FIELDS.some(k => typeof e[k] === 'string' && /disapprove/i.test(e[k]))
export const isApprove = e => !isDisapproval(e) && MATCH_FIELDS.some(k => typeof e[k] === 'string' && /^approve/i.test(e[k]))

// See the bucketing/ordering contract in the header — both derive from the parsed instant.
export const epochOf = e => (typeof e.t === 'string' ? Date.parse(e.t) : NaN)
export const dayOf = e => { const ms = epochOf(e); return Number.isFinite(ms) ? new Date(ms).toISOString().slice(0, 10) : null }

const textOf = e => TEXT_FIELDS.map(k => e[k]).find(v => typeof v === 'string') ?? ''

function listTargets(reviewsDir, filters) {
  const out = []
  for (const base of [reviewsDir, join(reviewsDir, 'archive')]) {
    if (!existsSync(base)) continue
    for (const e of readdirSync(base, { withFileTypes: true })) {
      if (!e.isDirectory() || TARGETLESS.has(e.name)) continue
      out.push({ name: e.name, dir: join(base, e.name) })
    }
  }
  return filters.length ? out.filter(t => filters.some(f => t.name.includes(f))) : out
}

// The miner's report as printable lines, in print order; the caller joins and prints them. Header,
// SUMMARY and both breakdowns are always there, a zero-match scan included. `since` defaults to 30
// days before the newest worklog event in the scanned scope (never wall-clock, so a run is
// reproducible); `filters` narrow the scan by substring of the target name.
export function disapprovalLines(reviewsDir, { since: sinceArg = null, filters = [] } = {}) {
  const targets = listTargets(reviewsDir, filters)
  const events = []
  // A voided mis-stamp is not a disapproval anyone must triage, so the filtered view is read.
  for (const t of targets) for (const event of readEvents(t.dir) ?? []) events.push({ target: t.name, event })

  let newest = null
  for (const { event } of events) {
    const d = dayOf(event)
    if (d && (!newest || d > newest)) newest = d
  }

  let since = sinceArg
  if (!since && newest) {
    const cutoff = new Date(`${newest}T00:00:00Z`)
    cutoff.setUTCDate(cutoff.getUTCDate() - 30)
    since = cutoff.toISOString().slice(0, 10)
  }

  const disapprovals = events
    .filter(({ event }) => event.ev === 'doc-gate' && isDisapproval(event))
    .filter(({ event }) => { const d = dayOf(event); return d && (!since || d >= since) })
    .sort((a, b) => epochOf(a.event) - epochOf(b.event))

  const out = [`DISAPPROVALS: ${targets.length} target(s) scanned, since ${since ?? '(no worklog events found)'}`, '']
  for (const { target, event } of disapprovals) {
    const which = event.round !== undefined ? `round ${event.round}` : event.pass !== undefined ? `pass ${event.pass}` : '—'
    out.push(`${dayOf(event)} · ${target} · ${which}`, `  ${textOf(event)}`, '')
  }

  out.push('SUMMARY', `  total disapprovals: ${disapprovals.length}`, '  by target:')
  const byTarget = new Map()
  for (const { target } of disapprovals) byTarget.set(target, (byTarget.get(target) ?? 0) + 1)
  for (const [t, c] of byTarget) out.push(`    ${t}: ${c}`)

  out.push('  distinct reasons:')
  const seen = new Set()
  for (const { event } of disapprovals) {
    const text = textOf(event)
    if (seen.has(text)) continue
    seen.add(text)
    out.push(`    - ${text}`, `      ${STUB}`)
  }
  return out
}
