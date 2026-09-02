// Unit tests for the records readers, imported in-process (no child process): the one
// frontmatter parser and section splitter, the one worklog loader and void rule, the one
// metrics reader. The consumers' own test files pin the behaviour each of them keeps; these
// pin the seams the consumers now share — above all where the strict and the lenient
// frontmatter read deliberately disagree, and what a void erases.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only records
import { check } from '../lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'
import { dateValue, parse, section, value, word } from '../../records/frontmatter.mjs'
import { deepEqual, live, matchesVoid, readEvents, readLines as readWorklogLines, voidsOf } from '../../records/worklog.mjs'
import { appendLine, readMetrics, readLines as readMetricsLines } from '../../records/metrics.mjs'
import { blocks as ledgerBlocks, ids as ledgerIds, severityOf } from '../../records/ledger.mjs'
import { fixedRows, meta as resolutionMeta, rows as resolutionRows, tallies } from '../../records/resolution.mjs'

// ---------- frontmatter: the block ----------
{
  const lf = '---\nstatus: resolved\nclosed: 2026-08-27\n---\n# Title\n\n## Findings\n| PPW-1 | fixed |\n'
  const p = parse(lf)
  check('a strict parse reads the frontmatter without its own newlines',
    p.fm === 'status: resolved\nclosed: 2026-08-27', JSON.stringify(p.fm))
  check('a strict parse hands back the body after the closing ---',
    p.body === '# Title\n\n## Findings\n| PPW-1 | fixed |\n', JSON.stringify(p.body))

  const crlf = lf.replace(/\n/g, '\r\n')
  check('a CRLF file reads the same keys, with no stray \\r at either end',
    parse(crlf).fm === 'status: resolved\r\nclosed: 2026-08-27', JSON.stringify(parse(crlf).fm))
  check('a CRLF file yields the same frontmatter values as the LF one',
    value(parse(crlf).fm, 'status') === 'resolved' && dateValue(parse(crlf).fm, 'closed') === '2026-08-27',
    JSON.stringify(parse(crlf).fm))

  check('text with no opening --- reads as no frontmatter, body untouched',
    parse('# Title\n').fm === null && parse('# Title\n').body === '# Title\n', JSON.stringify(parse('# Title\n')))
  check('a block with no closing --- reads as no frontmatter, strict and lenient alike',
    parse('---\nstatus: open\n').fm === null && parse('---\nstatus: open\n', { lenient: true }).fm === null,
    JSON.stringify([parse('---\nstatus: open\n'), parse('---\nstatus: open\n', { lenient: true })]))

  // The two reads part company here, so both are pinned: the lenient one lets the opener's own
  // newline close an empty block (and a caller then sees "present but empty"), the strict one
  // demands a terminator of its own and reports no frontmatter at all.
  const empty = '---\n---\n# Title\n'
  check('an empty block reads as present-but-empty leniently and absent strictly',
    parse(empty, { lenient: true }).fm === '' && parse(empty).fm === null,
    JSON.stringify([parse(empty, { lenient: true }).fm, parse(empty).fm]))
  check('leniently, an opener with no newline after it still opens a block',
    parse('---x\nstatus: open\n---\n', { lenient: true }).fm === 'x\nstatus: open' &&
    parse('---x\nstatus: open\n---\n').fm === null,
    JSON.stringify(parse('---x\nstatus: open\n---\n', { lenient: true }).fm))
  check('a lenient read keeps the block verbatim, leading newline and trailing \\r included',
    parse(crlf, { lenient: true }).fm === '\r\nstatus: resolved\r\nclosed: 2026-08-27\r',
    JSON.stringify(parse(crlf, { lenient: true }).fm))
  check('exactly one newline after the closing --- is dropped from the body',
    parse('---\na: 1\n---\n\nbody\n').body === '\nbody\n', JSON.stringify(parse('---\na: 1\n---\n\nbody\n').body))
  check('the earliest closing --- wins, so a later one stays in the body',
    parse('---\na: 1\n---\nbody\n---\nmore\n').fm === 'a: 1', JSON.stringify(parse('---\na: 1\n---\nbody\n---\nmore\n').fm))
}

// ---------- frontmatter: the value readers ----------
{
  const fm = 'status: resolved\ntitle: a long value with spaces  \nfixed_commit:\nclosed: not-a-date\nversion: 3'
  check('value() takes the rest of the key\'s line, trimmed',
    value(fm, 'title') === 'a long value with spaces', JSON.stringify(value(fm, 'title')))
  check('value() of a missing key is null', value(fm, 'answers') === null, String(value(fm, 'answers')))
  // value()'s gap after the colon crosses newlines, so an empty key reads the NEXT line whole —
  // the shape every caller of value() relies on, and the reason word() exists beside it.
  check('value() of an empty key reads the next line, where word() stays on its own',
    value(fm, 'fixed_commit') === 'closed: not-a-date' && word(fm, 'fixed_commit') === null,
    JSON.stringify([value(fm, 'fixed_commit'), word(fm, 'fixed_commit')]))
  check('value() of the last key with an empty value is null, having no next line to reach',
    value('version: 3\nfixed_commit:', 'fixed_commit') === null,
    String(value('version: 3\nfixed_commit:', 'fixed_commit')))
  check('word() takes the first token only',
    word(fm, 'title') === 'a', String(word(fm, 'title')))
  check('word() stays on the key\'s own line, so an empty value reads as absent',
    word(fm, 'fixed_commit') === null, String(word(fm, 'fixed_commit')))
  check('word() with acrossLines reads the next line\'s first token for an empty value',
    word(fm, 'fixed_commit', { acrossLines: true }) === 'closed:', String(word(fm, 'fixed_commit', { acrossLines: true })))
  check('dateValue() reads an ISO date and refuses any other shape',
    dateValue('closed: 2026-08-27', 'closed') === '2026-08-27' && dateValue(fm, 'closed') === null,
    String(dateValue(fm, 'closed')))
  check('the readers tolerate an empty or null frontmatter',
    value(null, 'status') === null && word('', 'status') === null, 'null/empty fm')
}

// ---------- frontmatter: the section splitter ----------
{
  const body = '# Title\n\n## Scope\n| Cluster |\n\n## Findings\n| PPW-1 | fixed |\n\n## Decisions\n### PPW-1\n'
  check('section() returns the named section, heading word first',
    section(body, 'Findings') === 'Findings\n| PPW-1 | fixed |\n\n', JSON.stringify(section(body, 'Findings')))
  check('section() finds a later section too', section(body, 'Decisions').startsWith('Decisions\n### PPW-1'), JSON.stringify(section(body, 'Decisions')))
  check('a missing section is the empty string, never undefined', section(body, 'Refuted') === '', JSON.stringify(section(body, 'Refuted')))
  check('a "## " inside a table cell does not open a section',
    section('## Findings\n| a | not ## Scope |\n', 'Scope') === '', JSON.stringify(section('## Findings\n| a | not ## Scope |\n', 'Scope')))
}

// ---------- resolution: the Findings table, the tallies, the fixed rows ----------
{
  const res = `---
type: resolution
status: resolved
closed: 2026-08-30
---

# Resolution v1 — 970-resolution-reader

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-1 | fixed | \`aaaaaaa\`, \`bbbbbbb\` | two commits |
| PPW-2 | backlog |  | queued |
| PPW-3 | Fixed | \`ccccccc\` | capitalised, so no status word |
| D5 | fixed | \`ddddddd\` | a pre-PPW id shape |
| PPW-4 | fixed

## Decisions

### PPW-2

Queued.
`
  check('rows() reads every Findings row in file order, with its status and commit cell',
    resolutionRows(res).map(r => `${r.id}=${r.status}`).join(' ') === 'PPW-1=fixed PPW-2=backlog PPW-3=Fixed PPW-4=fixed',
    JSON.stringify(resolutionRows(res)))
  // The pre-PPW "D<n>" row shape is gone from the record set, so the reader no longer accepts it.
  check('a "D<n>" row is not a Findings row any more',
    !resolutionRows(res).some(r => r.id === 'D5') && tallies(res).fixed === 1,
    JSON.stringify(resolutionRows(res).map(r => r.id)))
  check('a row truncated before the commit cell has a null commit',
    resolutionRows(res).find(r => r.id === 'PPW-4').commit === null, JSON.stringify(resolutionRows(res)))

  const t = tallies(res)
  check('backlog counts into the deferred bucket, as the metrics tallies read it',
    t.deferred === 1 && t.fixed === 1, JSON.stringify(t))
  check('a status cell that is not one lowercase word, or a truncated row, states no status to count',
    t.fixed + t.deferred + t.wont_fix + t.disputed + t.false_positive + t.open === 2, JSON.stringify(t))
  check('a table with no countable row tallies null, so a cross-check is skipped not zeroed',
    tallies('## Findings\n| ID | Status |\n') === null, String(tallies('## Findings\n| ID | Status |\n')))

  check('fixedRows() takes every sha in a multi-commit cell, in cell order',
    fixedRows(res).find(r => r.id === 'PPW-1').commits.join(', ') === 'aaaaaaa, bbbbbbb',
    JSON.stringify(fixedRows(res)))
  check('a fixed row whose cell holds no sha is returned with an empty commit list, never dropped',
    fixedRows('| PPW-9 | fixed | pending |').length === 1 && fixedRows('| PPW-9 | fixed | pending |')[0].commits.length === 0,
    JSON.stringify(fixedRows('| PPW-9 | fixed | pending |')))

  check('meta() reads the status and closed scalars', resolutionMeta(res).status === 'resolved' && resolutionMeta(res).closed === '2026-08-30', JSON.stringify(resolutionMeta(res)))
  check('meta() of a resolution with no status key reads open, never null',
    resolutionMeta('---\ntype: resolution\n---\n').status === 'open', JSON.stringify(resolutionMeta('---\ntype: resolution\n---\n')))
}

// ---------- ledger: ids, severities, detail blocks ----------
{
  const led = `# Ledger — 970-ledger-reader

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-1 | 🔴 | v1 | a | x.cs | fixed | \`aaaaaaa\` |
| PPW-2 | 🟡 | v1 | b | y.cs | open |  |
| PPW-1 | 🔴 | v1 | a | x.cs | fixed | \`aaaaaaa\` |

### PPW-1

- **Fix brief:** Trigger-list-shaped: yes
- **History:**
  - v1: fixed

### PPW-2

- **History:**
  - v1: found
`
  check('ids() lists every table row in file order, repeats included',
    ledgerIds(led).join(' ') === 'PPW-1 PPW-2 PPW-1', JSON.stringify(ledgerIds(led)))
  check('severityOf() maps each id to its severity emoji',
    severityOf(led).get('PPW-1') === '🔴' && severityOf(led).get('PPW-2') === '🟡', JSON.stringify([...severityOf(led)]))
  const b = ledgerBlocks(led)
  check('blocks() splits each detail block at the next id heading',
    b.size === 2 && /Trigger-list-shaped/.test(b.get('PPW-1')) && !/Trigger-list-shaped/.test(b.get('PPW-2')),
    JSON.stringify([...b.keys()]))
  check('the last block runs to the end of the file', /v1: found/.test(b.get('PPW-2')), JSON.stringify(b.get('PPW-2')))
}

// ---------- worklog: lines, parse errors, void filtering ----------
{
  const T = mkdtempSync(join(tmpdir(), 'records-worklog-'))
  const dir = join(T, '970-records-readers')
  mkdirSync(dir, { recursive: true })
  const events = [
    { t: '2019-02-01T10:00:00+03:00', ev: 'round-start', round: 1 },
    { t: '2019-02-01T10:05:00+03:00', ev: 'note', text: 'kept' },
    { t: '2019-02-01T10:06:00+03:00', ev: 'note', text: 'erased' },
    { t: '2019-02-01T10:07:00+03:00', ev: 'void', of: { ev: 'note', t: '2019-02-01T10:06:00+03:00' } },
  ]
  check('readEvents of a target with no worklog is null, not an empty list',
    readEvents(dir) === null, String(readEvents(dir)))
  writeFileSync(join(dir, 'worklog.jsonl'), `${events.map(e => JSON.stringify(e)).join('\n')}\n\nnot json\n`)

  const lines = readWorklogLines(dir)
  check('readLines numbers the non-blank lines from 1, skipping the blank one',
    lines.length === 5 && lines[4].n === 5, JSON.stringify(lines.map(l => l.n)))
  check('a line that is not JSON comes back as an error, with its raw text',
    lines[4].event === null && lines[4].raw === 'not json' && /JSON/i.test(lines[4].error.message),
    String(lines[4].error?.message))

  const kept = readEvents(dir)
  check('readEvents drops the void and what it names, and skips the unparseable line',
    kept.map(e => e.ev + (e.text ? `:${e.text}` : '')).join(' ') === 'round-start note:kept',
    JSON.stringify(kept))

  // One void, two matching events: every match is dropped (the renderer notes it; the rule is here).
  const two = [
    { t: '2019-03-01T10:00:00Z', ev: 'doc-gate', round: 1 },
    { t: '2019-03-01T10:00:00Z', ev: 'doc-gate', round: 2 },
    { t: '2019-03-01T11:00:00Z', ev: 'void', of: { ev: 'doc-gate', t: '2019-03-01T10:00:00Z' } },
  ]
  check('a void matching several events erases every one of them',
    live(two).length === 0, JSON.stringify(live(two)))
  check('a void keyed more narrowly erases only its own match',
    live([two[0], two[1], { t: '2019-03-01T11:00:00Z', ev: 'void', of: { ev: 'doc-gate', t: '2019-03-01T10:00:00Z', round: 2 } }])
      .map(e => e.round).join(',') === '1',
    JSON.stringify(live([two[0], two[1], { t: '2019-03-01T11:00:00Z', ev: 'void', of: { ev: 'doc-gate', t: '2019-03-01T10:00:00Z', round: 2 } }])))
  const emptyVoid = { t: '2019-03-01T12:00:00Z', ev: 'void', of: {} }
  check('a void with an empty "of" erases nothing',
    voidsOf([emptyVoid]).length === 0 && live([two[0], emptyVoid]).length === 1, JSON.stringify(live([two[0], emptyVoid])))
  // An `of` that is not an object has no keys to match on, so every event would match it: such a
  // void is dropped by the filter rather than erasing the whole log.
  const stringVoid = { t: '2019-03-01T12:30:00Z', ev: 'void', of: 'doc-gate' }
  check('a void whose "of" is a string erases nothing either',
    voidsOf([stringVoid]).length === 0 && live([two[0], two[1], stringVoid]).length === 2,
    JSON.stringify(live([two[0], two[1], stringVoid])))

  // The one equality rule is deep and key-order-blind — a JSON.stringify comparison would call
  // these two different and leave the event standing.
  const nested = { t: '2019-04-01T10:00:00Z', ev: 'void', of: { ev: 'gate-parked', payload: { a: 1, b: 2 } } }
  const parked = { t: '2019-04-01T09:00:00Z', ev: 'gate-parked', payload: { b: 2, a: 1 } }
  check('deepEqual matches nested objects whatever order their keys are written in',
    deepEqual({ a: 1, b: { c: 2 } }, { b: { c: 2 }, a: 1 }) && !deepEqual({ a: 1 }, { a: 1, b: 2 }),
    'deepEqual')
  check('so a void naming a nested field erases the event it names',
    matchesVoid(parked, nested) && live([parked, nested]).length === 0, JSON.stringify(live([parked, nested])))
  rmSync(T, { recursive: true, force: true })
}

// ---------- metrics: corrections split, bad lines, append ----------
{
  const T = mkdtempSync(join(tmpdir(), 'records-metrics-'))
  const dir = join(T, '971-records-readers')
  mkdirSync(dir, { recursive: true })
  check('readMetrics of a target with no metrics.jsonl is null, not an empty pair',
    readMetrics(dir) === null, String(readMetrics(dir)))

  writeFileSync(join(dir, 'metrics.jsonl'), [
    JSON.stringify({ target: '971', pass: 1, type: 'discovery', date: '2026-08-01' }),
    JSON.stringify({ target: '971', correction_for: { pass: 1, field: 'new_findings' }, date: '2026-08-02', note: 'recount' }),
    JSON.stringify({ target: '971', round: 1, type: 'fix-round', date: '2026-08-03' }),
    'null',
    '{ not json',
  ].join('\n') + '\n')

  const m = readMetrics(dir)
  check('readMetrics keeps the pass and fix-round lines in file order',
    m.lines.map(l => l.type).join(',') === 'discovery,fix-round', JSON.stringify(m.lines))
  check('readMetrics splits the correction lines out of the routing view',
    m.corrections.length === 1 && m.corrections[0].correction_for.field === 'new_findings', JSON.stringify(m.corrections))
  check('a line that is not JSON, and one that parses to a non-object, are not records',
    m.lines.length === 2 && m.corrections.length === 1, JSON.stringify(m))
  // Every consumer reports these lines rather than dropping them silently, so the reader hands
  // back which line and why — the auditor errors on them, the router and speed report note them.
  check('a skipped line comes back with its number, its raw text and the reason',
    m.skipped.length === 2 &&
    m.skipped[0].n === 4 && m.skipped[0].raw === 'null' && m.skipped[0].reason === 'not a JSON object (null)' &&
    m.skipped[1].n === 5 && /^unparseable JSON \(/.test(m.skipped[1].reason),
    JSON.stringify(m.skipped))

  let threw = null
  try { readMetrics(dir, { strict: true }) } catch (e) { threw = e }
  check('strict mode throws on a line that is not JSON, so a router never routes on half a file',
    threw !== null && /JSON/i.test(threw.message), String(threw?.message))

  const raw = readMetricsLines(dir)
  check('readLines numbers every non-blank line and keeps the raw text for the validator',
    raw.length === 5 && raw[4].n === 5 && raw[4].raw === '{ not json' && raw[4].error !== null,
    JSON.stringify(raw.map(l => l.n)))
  check('readLines keeps the corrections interleaved where the file has them',
    raw[1].line.correction_for.pass === 1, JSON.stringify(raw[1].line))

  const dir2 = join(T, '972-records-readers')
  mkdirSync(dir2, { recursive: true })
  appendLine(dir2, { target: '972', pass: 1 })
  writeFileSync(join(dir2, 'metrics.jsonl'), readFileSync(join(dir2, 'metrics.jsonl'), 'utf8').replace(/\n$/, ''))
  appendLine(dir2, { target: '972', pass: 2 })
  check('appendLine heals a file whose last line lost its newline',
    readMetrics(dir2).lines.map(l => l.pass).join(',') === '1,2',
    JSON.stringify(readFileSync(join(dir2, 'metrics.jsonl'), 'utf8')))
  rmSync(T, { recursive: true, force: true })
}
