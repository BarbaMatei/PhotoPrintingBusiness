#!/usr/bin/env node
// Validated worklog stamper: the only supported way to append an event to a target's
// worklog.jsonl. Owns the timestamp (ISO-8601 with the local UTC offset) and rejects a
// passed --t, enforces the event vocabulary and each event's required fields, and guards
// round-start/round-end pairing so only one round is open at a time. Resolves the target
// folder as reviews/<target> or reviews/archive/<target> (whichever exists), and only
// creates reviews/<target>/ fresh when neither does.
//
// Usage: node reviews/lib/wl.mjs [--root <repoRoot>] <target> <ev> [--<key> <value>]...
//          [--json '<obj>']
// Numeric-looking flag values become numbers; --json merges a raw object for fields flags
// can't express (e.g. nested "of"). Also exports appendEvent(root, target, event) for other
// lib scripts to call in-process; the CLI is a thin wrapper over it.
// Exit: 0 appended (prints the appended line) · 1 usage or validation error (prints
// "ERROR <reason>", appends nothing).
import { readFileSync, appendFileSync, existsSync, mkdirSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'

const VOCAB = new Set([
  'round-start', 'round-end', 'triage-done', 'gate-open', 'gate-closed', 'gate-parked',
  'check-dispatched', 'check-returned', 'test-run', 'finding', 'micro-review-dispatched',
  'micro-review-returned', 'doc-gate', 'pass-launch', 'pass-records-done', 'run-start',
  'run-end', 'note', 'void', 'verify-result',
])

const REQUIRED = {
  'round-start': ['round'],
  'round-end': ['round'],
  'triage-done': ['round', 'clusters'],
  'gate-open': ['reason'],
  'gate-closed': ['reason'],
  'gate-parked': ['kind', 'default', 'reason'],
  'test-run': ['kind'],
  'finding': ['id', 'status'],
  'micro-review-dispatched': ['cluster'],
  'micro-review-returned': ['cluster'],
  'pass-launch': ['pass', 'type'],
  'pass-records-done': ['pass'],
  'verify-result': ['id', 'verdict'],
  'void': ['of'],
}

const TEST_KINDS = new Set(['red', 'green', 'final', 'baseline', 'revert-and-rerun'])
const PPW = /^PPW-\d+$/

function readEvents(wlPath) {
  if (!existsSync(wlPath)) return []
  return readFileSync(wlPath, 'utf8').split(/\r?\n/).filter(l => l.trim()).map(l => JSON.parse(l))
}

function openRound(events) {
  let open = null
  for (const e of live(events)) {
    if (e.ev === 'round-start') open = e.round
    else if (e.ev === 'round-end' && e.round === open) open = null
  }
  return open
}

function deepEqual(a, b) {
  if (a === b) return true
  if (typeof a !== typeof b || a === null || b === null) return false
  if (typeof a !== 'object') return false
  const ak = Object.keys(a), bk = Object.keys(b)
  return ak.length === bk.length && ak.every(k => deepEqual(a[k], b[k]))
}

// Every reader of the log has to agree on what a void erased, or the stamper and the renderer
// disagree about which round is open after a repair.
function live(events) {
  const voids = events.filter(e => e.ev === 'void' && e.of && typeof e.of === 'object' && Object.keys(e.of).length)
  return events.filter(e => e.ev !== 'void' && !voids.some(v => Object.keys(v.of).every(k => deepEqual(e[k], v.of[k]))))
}

function closestTimestamps(events, tIso) {
  const target = Date.parse(tIso)
  return events.map(e => e.t)
    .filter(t => typeof t === 'string' && Number.isFinite(Date.parse(t)))
    .sort((a, b) => Math.abs(Date.parse(a) - target) - Math.abs(Date.parse(b) - target))
    .slice(0, 3)
}

function validateShape(event) {
  const { ev } = event
  for (const key of REQUIRED[ev] ?? []) {
    if (event[key] === undefined) throw new Error(`"${ev}" requires "${key}"`)
  }
  if ((ev === 'round-start' || ev === 'round-end' || ev === 'triage-done') && typeof event.round !== 'number') {
    throw new Error(`"${ev}" requires "round" to be a number`)
  }
  if (ev === 'test-run' && !TEST_KINDS.has(event.kind)) {
    throw new Error(`"test-run" requires "kind" to be one of ${[...TEST_KINDS].join('|')}`)
  }
  if ((ev === 'finding' || ev === 'verify-result') && !PPW.test(event.id)) {
    throw new Error(`"${ev}" requires "id" shaped like PPW-<n>`)
  }
  if (ev === 'void') {
    const of_ = event.of
    if (typeof of_ !== 'object' || of_ === null || Array.isArray(of_) || of_.ev === undefined || of_.t === undefined) {
      throw new Error('"void" requires "of" to be an object with at least "ev" and "t"')
    }
  }
}

function nowIso() {
  const d = new Date()
  const pad = n => String(n).padStart(2, '0')
  const offMin = -d.getTimezoneOffset()
  const sign = offMin >= 0 ? '+' : '-'
  const abs = Math.abs(offMin)
  const off = `${sign}${pad(Math.floor(abs / 60))}:${pad(abs % 60)}`
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}${off}`
}

export function appendEvent(root, target, event) {
  if (!event || typeof event !== 'object') throw new Error('event must be an object')
  const { ev } = event
  if (!ev) throw new Error('missing "ev"')
  if (!VOCAB.has(ev)) throw new Error(`unknown ev "${ev}"`)
  if ('t' in event) throw new Error('"t" is generated by the stamper — a passed value is rejected')
  validateShape(event)

  const candidateDirs = [join(root, 'reviews', target), join(root, 'reviews', 'archive', target)]
  const dir = candidateDirs.find(existsSync) ?? candidateDirs[0]
  const wlPath = join(dir, 'worklog.jsonl')
  const existing = readEvents(wlPath)

  if (ev === 'round-start') {
    if (!existsSync(join(dir, `resolution-v${event.round}.md`))) {
      const label = dir === candidateDirs[1] ? `archive/${target}` : target
      throw new Error(`round-start ${event.round}: no resolution-v${event.round}.md in reviews/${label}/`)
    }
    const open = openRound(existing)
    if (open !== null) {
      throw new Error(`round-start ${event.round}: round ${open} is still open — stamp its round-end first (a re-start after round-end is how a multi-part round is recorded)`)
    }
  }
  if (ev === 'round-end') {
    const open = openRound(existing)
    if (open !== event.round) throw new Error(`round-end ${event.round}: no open round-start for ${event.round}`)
  }
  if (ev === 'void') {
    const match = existing.find(e => Object.keys(event.of).every(k => deepEqual(e[k], event.of[k])))
    if (!match) {
      const closest = closestTimestamps(existing, event.of.t)
      throw new Error(`void: no worklog event matches ${JSON.stringify(event.of)} — closest timestamps: ${closest.length ? closest.join(', ') : '(worklog is empty)'}`)
    }
  }

  const { ev: _ev, ...rest } = event
  const stamped = { t: nowIso(), ev, ...rest }
  mkdirSync(dir, { recursive: true })
  const prev = existsSync(wlPath) ? readFileSync(wlPath, 'utf8') : ''
  appendFileSync(wlPath, (prev && !prev.endsWith('\n') ? '\n' : '') + JSON.stringify(stamped) + '\n')
  return stamped
}

function coerce(v) {
  return /^-?\d+(\.\d+)?$/.test(v) ? Number(v) : v
}

function isMain() {
  return process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href
}

if (isMain()) {
  try {
    const argv = process.argv.slice(2)
    let root = null
    const rest = []
    const event = {}
    for (let i = 0; i < argv.length; i++) {
      const a = argv[i]
      if (a === '--root') root = argv[++i]
      else if (a === '--json') Object.assign(event, JSON.parse(argv[++i]))
      else if (a.startsWith('--')) event[a.slice(2)] = coerce(argv[++i])
      else rest.push(a)
    }
    if (!root) root = join(dirname(fileURLToPath(import.meta.url)), '..', '..')
    const [target, ev] = rest
    if (!target || !ev) {
      throw new Error("usage: wl.mjs [--root <repoRoot>] <target> <ev> [--<key> <value>]... [--json '<obj>']")
    }
    event.ev = ev
    const stamped = appendEvent(root, target, event)
    console.log(JSON.stringify(stamped))
    process.exit(0)
  } catch (e) {
    console.error(`ERROR ${e.message}`)
    process.exit(1)
  }
}
