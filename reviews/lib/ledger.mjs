// Reader for a target's ledger Findings table — the record of which findings are still open.
// The router and the autonomy policy both route on it, so the row shape, the status vocabulary
// and the definition of "open" live here once.
// Row shape (doc-contracts.md): | PPW-<n> | 🔴|🟠|🟡|⚪ | first seen | title | file | status | affirmed |
import { readFileSync, existsSync } from 'node:fs'

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
