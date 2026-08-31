// Reader for a target's ledger Findings table — the record of which findings are still open.
// The router and the autonomy policy both route on it, so the row shape and the definition of
// "open" live here once; the status and severity words come from records/schema.mjs.
// Row shape (doc-contracts.md): | PPW-<n> | 🔴|🟠|🟡|⚪ | first seen | title | file | status | affirmed |
import { readFileSync, readdirSync, existsSync } from 'node:fs'
import { join } from 'node:path'
import { OPEN_STATUSES, SEVERITIES, STATUSES, V3_CUTOFF } from './records/schema.mjs'

export { OPEN_STATUSES }
const ROW = new RegExp(`^\\|\\s*(PPW-\\d+)\\s*\\|\\s*(${SEVERITIES.join('|')})\\s*\\|[^|\\n]*\\|[^|\\n]*\\|[^|\\n]*\\|\\s*([a-z-]+)\\s*\\|`, 'gm')
const ID_ROW = /^\|\s*PPW-\d+\s*\|/gm

// null when the target has no ledger at all: callers fall back to their pre-ledger behaviour.
export function readLedger(file) {
  if (!existsSync(file)) return null
  const text = readFileSync(file, 'utf8')
  const rows = [...text.matchAll(ROW)].filter(m => STATUSES.includes(m[3])).map(m => ({ id: m[1], sev: m[2], status: m[3] }))
  return { rows, idRows: [...text.matchAll(ID_ROW)].length }
}

export const openIds = (rows, sev) => rows.filter(r => r.sev === sev && OPEN_STATUSES.includes(r.status)).map(r => r.id)

// The ledger is not the authority while a resolved round's records are unrendered — unless the round
// closed before V3_CUTOFF, when fix-round lines did not exist yet and its records will never come.
export function standsDown(dir) {
  const files = readdirSync(dir)
  const newest = re => {
    const ns = files.map(f => re.exec(f)).filter(Boolean).map(m => Number(m[1]))
    return ns.length ? Math.max(...ns) : 0
  }
  const RN = newest(/^resolution-v(\d+)\.md$/)
  if (!RN || RN < newest(/^review-v(\d+)\.md$/)) return false
  const block = /^---\r?\n([\s\S]*?)\r?\n---/.exec(readFileSync(join(dir, `resolution-v${RN}.md`), 'utf8'))
  const fm = block ? block[1] : ''
  if ((/^status:\s*(.+?)\s*$/m.exec(fm)?.[1] ?? null) !== 'resolved') return false
  const closed = /^closed:\s*(\d{4}-\d{2}-\d{2})/m.exec(fm)?.[1] ?? null
  if (closed && closed < V3_CUTOFF) return false
  const metrics = join(dir, 'metrics.jsonl')
  if (!existsSync(metrics)) return true
  return !readFileSync(metrics, 'utf8').split(/\r?\n/).filter(l => l.trim())
    .map(l => { try { return JSON.parse(l) } catch { return null } })
    .some(l => l && !l.correction_for && l.type === 'fix-round' && l.round === RN)
}
