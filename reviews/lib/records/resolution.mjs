// Reader for a resolution-v<n>.md — the fixer's answer to one review: its frontmatter scalars
// and its "## Findings" table. Four readers ask three different questions of that table (the
// renderer wants every row's status and commit cell, the validator wants the tallies the metrics
// line is cross-checked against, the verifier and the hand-back gates want the `fixed` rows and
// their commits), so the row shape lives here once. A row's Commit cell may list several commits
// — the round-scope review's follow-up is the common case — so `commits` is always a list and a
// `fixed` row with no sha in the cell is returned with an empty one rather than dropped.
// Reads nothing; the caller passes the text.
import { dateValue, parse, section, word } from './frontmatter.mjs'

// | <ID> | <status> | <commit> | <note> |. Ids are PPW-<n> today; the shape stays permissive
// about the prefix because the archived rounds of a renamed target keep their own.
const ROW = /^\|\s*[A-Za-z]+-\d+\s*\|/
const FIXED_ROW = /^\|\s*(PPW-\d+)\s*\|\s*fixed\s*\|([^|]*)\|/gm
const SHA = /[0-9a-f]{7,40}/g

// Which metrics-line findings bucket each status word counts into; `backlog` is the deferred
// bucket, and anything non-terminal (open, in-progress) falls to `open`.
export const TALLY = { fixed: 'fixed', 'wont-fix': 'wont_fix', deferred: 'deferred', backlog: 'deferred', disputed: 'disputed', 'false-positive': 'false_positive' }

export const zeroTally = () => ({ fixed: 0, wont_fix: 0, deferred: 0, disputed: 0, false_positive: 0, open: 0 })

// The frontmatter scalars every reader of a resolution needs. `acrossLines` is the gates' read,
// where a key left empty must not silently borrow the next key's value.
export function meta(text, { acrossLines = false } = {}) {
  const fm = parse(text, { lenient: true }).fm ?? ''
  return {
    fm,
    status: word(fm, 'status', { acrossLines }) ?? 'open',
    closed: dateValue(fm, 'closed', { acrossLines }),
  }
}

// Every "## Findings" row, in file order: { id, status, commit }. `commit` is null when the row
// carries no cell after the status — the marker of a row truncated mid-shape.
export const rows = text =>
  section(text, 'Findings').split('\n').filter(l => ROW.test(l)).map(l => {
    const c = l.split('|').map(x => x.trim())
    return { id: c[1], status: c[2], commit: c[3] ?? null }
  })

// The status tallies a fix-round metrics line is cross-checked against, or null when the table
// has no countable row. A row whose status cell is not one lowercase word, or that is truncated
// before the next "|", states no status to count.
export function tallies(text) {
  const t = zeroTally()
  let found = false
  for (const r of rows(text)) {
    if (r.commit === null || !/^[a-z-]+$/.test(r.status ?? '')) continue
    found = true
    t[TALLY[r.status] ?? 'open']++
  }
  return found ? t : null
}

// The `fixed` rows anywhere in the text: { id, cell, commits }. Read over the whole file, not
// just the Findings section, because that is what the verifier and the gates have always done.
export const fixedRows = text =>
  [...text.matchAll(FIXED_ROW)].map(m => ({ id: m[1], cell: m[2], commits: m[2].match(SHA) ?? [] }))
