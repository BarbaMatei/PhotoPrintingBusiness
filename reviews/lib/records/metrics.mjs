// The one metrics.jsonl reader and the one appender. Every reader routes on the pass and
// fix-round lines and must not count a correction line among them (a correction supersedes a
// field of an earlier line; it is not a pass of its own), so readMetrics splits the two apart.
// readLines keeps line numbers and raw text for the validator, which reports a line that is not
// JSON and checks corrections itself.
import { readFileSync, appendFileSync, existsSync } from 'node:fs'
import { join } from 'node:path'

// One entry per non-blank line, in file order: { n, raw, line, error }. `line` is null when the
// line is not JSON. null (not []) when the target has no metrics.jsonl at all.
export function readLines(dir) {
  const path = join(dir, 'metrics.jsonl')
  if (!existsSync(path)) return null
  const out = []
  for (const raw of readFileSync(path, 'utf8').split(/\r?\n/)) {
    if (!raw.trim()) continue
    const n = out.length + 1
    try { out.push({ n, raw, line: JSON.parse(raw), error: null }) }
    catch (error) { out.push({ n, raw, line: null, error }) }
  }
  return out
}

// Why a raw line is not a record, or null when it is one. One home for the wording, because the
// three readers that meet such a line report it three ways — the auditor as an error, the router
// and the speed report as a note — and all three must name the same fault.
export const skipReason = ({ line, error }) =>
  error ? `unparseable JSON (${error.message})`
    : !line || typeof line !== 'object' ? `not a JSON object (${line === null ? 'null' : typeof line})`
      : null

// { lines, corrections, skipped }, or null when the target has no metrics.jsonl. A line that is
// not JSON, or that parses to something other than an object, is not a record: it lands in
// `skipped` as { n, raw, reason } for the caller to report — unless `strict`, where an
// unparseable line throws, as the router and the summary reader need.
export function readMetrics(dir, { strict = false } = {}) {
  const raw = readLines(dir)
  if (raw === null) return null
  const lines = [], corrections = [], skipped = []
  for (const entry of raw) {
    const reason = skipReason(entry)
    if (reason) {
      if (entry.error && strict) throw entry.error
      skipped.push({ n: entry.n, raw: entry.raw, reason })
      continue
    }
    const o = entry.line
    if (o.correction_for) corrections.push(o)
    else lines.push(o)
  }
  return { lines, corrections, skipped }
}

// Appends one line, healing a file whose last line lost its newline.
export function appendLine(dir, obj) {
  const path = join(dir, 'metrics.jsonl')
  const prev = existsSync(path) ? readFileSync(path, 'utf8') : ''
  appendFileSync(path, (prev && !prev.endsWith('\n') ? '\n' : '') + JSON.stringify(obj) + '\n')
}
