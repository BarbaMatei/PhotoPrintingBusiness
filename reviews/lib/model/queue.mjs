// What a ledger's open rows mean for the loop: which 🟠 wait in the queue, which are a batch of
// their own, and how much serious work the router's later rows still count. QUEUE_THRESHOLD lives
// here rather than in records/schema.mjs because it is a routing decision, not a record shape —
// nothing that reads or writes a file needs the number.
// The threshold plus the pre-certification sweep are what guarantee no 🟠 outlives the loop.
import { join } from 'node:path'
import { openIds, readLedger } from '../records/ledger.mjs'

// Open 🟠 below this count wait in the queue; at it or over, they are a fix round of their own.
export const QUEUE_THRESHOLD = 3

// The ledger's open work by severity, or null when the target has no ledger at all — a caller
// with no ledger falls back to the metrics tally, which is a different (weaker) question.
// `rows`/`idRows` let a caller report the rows it could not parse: a row the router cannot read
// can only make the loop quieter, so the gap is never silent.
export function openLedger(dir) {
  const led = readLedger(join(dir, 'ledger.md'))
  if (!led) return null
  return { high: openIds(led.rows, '🔴'), medium: openIds(led.rows, '🟠'), rows: led.rows.length, idRows: led.idRows }
}

export const isBatch = medium => medium.length >= QUEUE_THRESHOLD

// Still-open serious work for the rows below the ledger rows: 🔴 always count, 🟠 only as a
// batch — a queued medium that also read as "serious" would have the router contradict itself.
export const seriousCount = ({ high, medium }) => high.length + (isBatch(medium) ? medium.length : 0)
