#!/usr/bin/env node
// Lint miner (reporter half): scans worklogs for doc-gate judge disapprovals and prints
// each one with a stub checklist line, so a human/agent can decide which become
// deterministic doc-gate.mjs checks. Never edits anything, never classifies "lintable" itself.
//
// Usage: node reviews/lib/gate-miner.mjs [--root <repoRoot>] [--since YYYY-MM-DD] [target ...]
//   No target ⇒ every target, live and archived. --since default = 30 days before the
//   newest worklog event seen in the scanned scope (never wall-clock, so a run is reproducible).
// Bucketing/ordering contract: every date comparison (--since, "newest seen", print order) uses
// the event's parsed instant normalized to its UTC calendar day, never the raw ISO-string
// prefix — real worklogs mix `Z` and offset (`+03:00`) stamps, and a naive string compare
// mis-buckets and mis-orders across that mix.
// Exit: 0 always, even with zero matches · 1 on an IO error while reading worklogs.
import { readdirSync, existsSync } from 'node:fs'
import { join } from 'node:path'
import { repoRoot, takeRoot } from './cli/args.mjs'
import { readEvents } from './records/worklog.mjs'
import { TARGETLESS } from './records/schema.mjs'
// The field that trips the match isn't necessarily the one worth printing — priority differs.
const MATCH_FIELDS = ['verdict', 'judge', 'reason', 'note']
const TEXT_FIELDS = ['reason', 'judge', 'note', 'verdict']
const STUB = '[ ] lintable? -> add a check to doc-gate.mjs + a fixture to run-tests.mjs'

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

const isDisapproval = e => MATCH_FIELDS.some(k => typeof e[k] === 'string' && /disapprove/i.test(e[k]))
const textOf = e => TEXT_FIELDS.map(k => e[k]).find(v => typeof v === 'string') ?? ''
// See the bucketing/ordering contract in the header — both derive from the parsed instant.
const epochOf = e => typeof e.t === 'string' ? Date.parse(e.t) : NaN
const dateOf = e => { const ms = epochOf(e); return Number.isFinite(ms) ? new Date(ms).toISOString().slice(0, 10) : null }

function main() {
  const { root, rest } = takeRoot(process.argv.slice(2))
  let sinceArg = null
  const filters = []
  for (let i = 0; i < rest.length; i++) {
    if (rest[i] === '--since') sinceArg = rest[++i]
    else filters.push(rest[i])
  }
  const reviewsDir = join(repoRoot(import.meta.url, root), 'reviews')

  const targets = listTargets(reviewsDir, filters)
  const events = []
  // A voided mis-stamp is not a disapproval anyone must triage, so the filtered view is read.
  for (const t of targets) for (const event of readEvents(t.dir) ?? []) events.push({ target: t.name, event })

  let newest = null
  for (const { event } of events) {
    const d = dateOf(event)
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
    .filter(({ event }) => { const d = dateOf(event); return d && (!since || d >= since) })
    .sort((a, b) => epochOf(a.event) - epochOf(b.event))

  console.log(`GATE MINER: ${targets.length} target(s) scanned, since ${since ?? '(no worklog events found)'}\n`)
  for (const { target, event } of disapprovals) {
    const which = event.round !== undefined ? `round ${event.round}` : event.pass !== undefined ? `pass ${event.pass}` : '—'
    console.log(`${dateOf(event)} · ${target} · ${which}`)
    console.log(`  ${textOf(event)}\n`)
  }

  console.log('SUMMARY')
  console.log(`  total disapprovals: ${disapprovals.length}`)
  console.log('  by target:')
  const byTarget = new Map()
  for (const { target } of disapprovals) byTarget.set(target, (byTarget.get(target) ?? 0) + 1)
  for (const [t, c] of byTarget) console.log(`    ${t}: ${c}`)

  console.log('  distinct reasons:')
  const seen = new Set()
  for (const { event } of disapprovals) {
    const text = textOf(event)
    if (seen.has(text)) continue
    seen.add(text)
    console.log(`    - ${text}`)
    console.log(`      ${STUB}`)
  }
}

try {
  main()
  process.exit(0)
} catch (e) {
  console.error(`ERROR ${e.message}`)
  process.exit(1)
}
