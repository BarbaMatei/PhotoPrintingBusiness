// The one worklog.jsonl reader and the one void-equality rule. Every reader of a worklog drops
// what a void erased, or two readers disagree after a repair — so `live` is the only definition
// of "what happened", and a void's `of` matches on a key subset, deeply, in any key order.
// readLines keeps the file's line numbers and raw text for the validators that must report a
// line that is not JSON; readEvents is the filtered view every measuring reader wants.
import { readFileSync, existsSync } from 'node:fs'
import { join } from 'node:path'

// One entry per non-blank line, in file order: { n, raw, event, error }. `event` is null when the
// line is not JSON. null (not []) when the target has no worklog at all.
export function readLines(dir) {
  const path = join(dir, 'worklog.jsonl')
  if (!existsSync(path)) return null
  const out = []
  for (const raw of readFileSync(path, 'utf8').split(/\r?\n/)) {
    if (!raw.trim()) continue
    const n = out.length + 1
    try { out.push({ n, raw, event: JSON.parse(raw), error: null }) }
    catch (error) { out.push({ n, raw, event: null, error }) }
  }
  return out
}

export function deepEqual(a, b) {
  if (a === b) return true
  if (typeof a !== typeof b || a === null || b === null) return false
  if (typeof a !== 'object') return false
  const ak = Object.keys(a), bk = Object.keys(b)
  return ak.length === bk.length && ak.every(k => deepEqual(a[k], b[k]))
}

// A void with an empty or non-object `of` erases nothing — it would otherwise match every event.
export const voidsOf = events =>
  events.filter(e => e.ev === 'void' && e.of && typeof e.of === 'object' && Object.keys(e.of).length)

export const matchesVoid = (event, voidEvent) =>
  Object.keys(voidEvent.of).every(k => deepEqual(event[k], voidEvent.of[k]))

export function live(events) {
  const voids = voidsOf(events)
  return events.filter(e => e.ev !== 'void' && !voids.some(v => matchesVoid(e, v)))
}

// Parsed, void-filtered events; unparseable lines are skipped. null when there is no worklog.
export function readEvents(dir) {
  const lines = readLines(dir)
  return lines === null ? null : live(lines.filter(l => l.event).map(l => l.event))
}
