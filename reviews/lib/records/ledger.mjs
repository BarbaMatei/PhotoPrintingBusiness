// Reader for a target's ledger Findings table — the record of which findings are still open.
// The router and the autonomy policy both route on it, so the row shape and the definition of
// "open" live here once; the status and severity words come from records/schema.mjs. What the
// open rows then MEAN for the loop (queue, batch, sweep, stand-down) is a derived fact and lives
// in model/ — this file only reads.
// Row shape (doc-contracts.md): | PPW-<n> | 🔴|🟠|🟡|⚪ | first seen | title | file | status | affirmed |
import { readFileSync, existsSync } from 'node:fs'
import { OPEN_STATUSES, SEVERITIES, STATUSES } from './schema.mjs'

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
