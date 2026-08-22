// Reader for a target's ledger Findings table — the record of which findings are still open.
// The router and the autonomy policy both route on it, so the row shape, the status vocabulary
// and the definition of "open" live here once.
// Row shape (doc-contracts.md): | PPW-<n> | 🔴|🟠|🟡|⚪ | first seen | title | file | status | affirmed |
import { readFileSync, readdirSync, existsSync } from 'node:fs'
import { join } from 'node:path'

export const OPEN_STATUSES = ['open', 'in-progress']
const STATUSES = [...OPEN_STATUSES, 'fixed', 'verified', 'wont-fix', 'deferred', 'disputed', 'false-positive', 'backlog']
const ROW = /^\|\s*(PPW-\d+)\s*\|\s*(🔴|🟠|🟡|⚪)\s*\|[^|\n]*\|[^|\n]*\|[^|\n]*\|\s*([a-z-]+)\s*\|/gm
const ID_ROW = /^\|\s*PPW-\d+\s*\|/gm

// null when the target has no ledger at all: callers fall back to their pre-ledger behaviour.
// `idRows` is every row that opens with an id, parsed or not, so a caller can report the gap.
export function readLedger(file) {
  if (!existsSync(file)) return null
  const text = readFileSync(file, 'utf8')
  const rows = [...text.matchAll(ROW)].filter(m => STATUSES.includes(m[3])).map(m => ({ id: m[1], sev: m[2], status: m[3] }))
  return { rows, idRows: [...text.matchAll(ID_ROW)].length }
}

export const openIds = (rows, sev) => rows.filter(r => r.sev === sev && OPEN_STATUSES.includes(r.status)).map(r => r.id)

// The one rule for when the ledger is NOT the authority on open work: a resolved resolution
// answering the latest review whose verification has not run yet. Its round's records render
// after that verification, so its rows still read open|in-progress — verify the round, do not
// re-route it. Every caller asks here so the router and the policy cannot drift apart.
export function standsDown(dir, latestPassType) {
  const files = readdirSync(dir)
  const newest = re => {
    const ns = files.map(f => re.exec(f)).filter(Boolean).map(m => Number(m[1]))
    return ns.length ? Math.max(...ns) : 0
  }
  const RN = newest(/^resolution-v(\d+)\.md$/)
  if (!RN || RN < newest(/^review-v(\d+)\.md$/)) return false
  const block = /^---\r?\n([\s\S]*?)\r?\n---/.exec(readFileSync(join(dir, `resolution-v${RN}.md`), 'utf8'))
  const status = block ? /^status:\s*(.+?)\s*$/m.exec(block[1])?.[1] ?? null : null
  return status === 'resolved' && latestPassType !== 'verification'
}
