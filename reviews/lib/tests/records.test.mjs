// Unit tests for the records readers, imported in-process (no child process): the one
// frontmatter parser and section splitter, the one worklog loader and void rule, the one
// metrics reader. The consumers' own test files pin the behaviour each of them keeps; these
// pin the seams the consumers now share — above all where the strict and the lenient
// frontmatter read deliberately disagree, and what a void erases.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only records
import { check } from './lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'
import { dateValue, parse, section, value, word } from '../records/frontmatter.mjs'
import { deepEqual, live, matchesVoid, readEvents, readLines as readWorklogLines, voidsOf } from '../records/worklog.mjs'
import { appendLine, readMetrics, readLines as readMetricsLines } from '../records/metrics.mjs'

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
