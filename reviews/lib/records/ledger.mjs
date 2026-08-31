// Reader for a target's ledger Findings table — the record of which findings are still open.
// The router and the autonomy policy both route on it, so the row shape and the definition of
// "open" live here once; the status and severity words come from records/schema.mjs. What the
// open rows then MEAN for the loop (queue, batch, sweep, stand-down) is a derived fact and lives
// in model/ — this file only reads.
// Row shape (doc-contracts.md): | PPW-<n> | 🔴|🟠|🟡|⚪ | first seen | title | file | status | affirmed |
// The "### <id>" detail blocks under the table are read here too: the doc gate caps their size
// and compares them against HEAD, and the hand-back gates read the fix brief out of them.
import { readFileSync, existsSync } from 'node:fs'
import { OPEN_STATUSES, SEVERITIES, STATUSES } from './schema.mjs'

const ROW = new RegExp(`^\\|\\s*(PPW-\\d+)\\s*\\|\\s*(${SEVERITIES.join('|')})\\s*\\|[^|\\n]*\\|[^|\\n]*\\|[^|\\n]*\\|\\s*([a-z-]+)\\s*\\|`, 'gm')
const ID_ROW = /^\|\s*(PPW-\d+)\s*\|/gm
const SEV_ROW = new RegExp(`^\\|\\s*(PPW-\\d+)\\s*\\|\\s*(${SEVERITIES.join('|')})\\s*\\|`, 'gm')
const BLOCK = /^### (PPW-\d+)[^\n]*\n([\s\S]*?)(?=^### PPW-\d+|(?![\s\S]))/gm

// null when the target has no ledger at all: callers fall back to their pre-ledger behaviour.
export function readLedger(file) {
  if (!existsSync(file)) return null
  const text = readFileSync(file, 'utf8')
  const rows = [...text.matchAll(ROW)].filter(m => STATUSES.includes(m[3])).map(m => ({ id: m[1], sev: m[2], status: m[3] }))
  return { rows, idRows: ids(text).length }
}

// Every id with a table row, in file order — repeats included, which is how the duplicate-id
// scan sees an id listed twice in one ledger.
export const ids = text => [...text.matchAll(ID_ROW)].map(m => m[1])

// id -> severity emoji, for the readers that need only the severity of a row.
export const severityOf = text => new Map([...text.matchAll(SEV_ROW)].map(m => [m[1], m[2]]))

// id -> its "### <id>" detail block body. The block runs to the next id heading, so the fix
// brief, the pre-check verdict and the History lines all read out of one string.
export const blocks = text => new Map([...text.matchAll(BLOCK)].map(m => [m[1], m[2]]))

export const openIds = (rows, sev) => rows.filter(r => r.sev === sev && OPEN_STATUSES.includes(r.status)).map(r => r.id)
