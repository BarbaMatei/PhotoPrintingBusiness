#!/usr/bin/env node
// Records renderer — turns a pass's worklog (plus, for a fix round, the hand-written resolution)
// into that pass's metrics line, its row in the index's Passes table, and the ledger status flips
// it implies. Two modes: fix round (the default) and `--verification <pass>`.
//
// Runtime covers only paired spans — round-start/round-end for a fix round, pass-launch/
// pass-records-done for a verification. A round stopped and resumed has several, and the records
// and gate time between them belongs to no round. Events a `void` event names are dropped before
// anything is measured. An unpairable stamp aborts instead of over-counting, and an append waits
// for the resolution to read `status: resolved` (or the pass to be stamped done).
//
// Every check runs before the first write, so a refusal leaves metrics.jsonl, index.md and
// ledger.md all untouched. Prose stays a human's: the renderer writes one metrics line, one index
// row, and per finding a Status/Affirmed cell plus one appended History line.
//
// Usage: node reviews/lib/render-records.mjs <target> [--round <n>] --outcome "<text>"
//          [--no-index] [--dry-run] [--in-progress] [--root <repoRoot>]
//        node reviews/lib/render-records.mjs <target> --verification <pass> --outcome "<text>"
//          [--new-findings <h,m,l,c>] [--commit <sha>] [--no-index] [--dry-run] [--in-progress]
// Exit 0 = rendered (or dry-run) · 1 = cannot render: missing records, a duplicate metrics line,
// a bad worklog, unpaired stamps, an --outcome that is missing, over 50 words or carries a "|" or a
// line break, no Passes table to insert into, or an unresolved resolution / unstamped
// pass-records-done without --in-progress.
import { readFileSync, writeFileSync, existsSync } from 'node:fs'
import { join, relative } from 'node:path'
import { sliceSpans, strictSpans } from './model/spans.mjs'
import { newest } from './model/target.mjs'
import { repoRoot, takeRoot } from './cli/args.mjs'
import { INDEX, REVIEWS as REVIEWS_HOME } from './records/schema.mjs'
import { parse, section, word } from './records/frontmatter.mjs'
import { appendLine, readMetrics } from './records/metrics.mjs'
import { live, matchesVoid, readLines, voidsOf } from './records/worklog.mjs'

const { root, rest: argv } = takeRoot(process.argv.slice(2))
let ROUND = null, DRY = false, ALLOW_UNRESOLVED = false
let PASS = null, OUTCOME = null, NEW_FINDINGS = '0,0,0,0', COMMIT = null, NO_INDEX = false
const rest = []
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === '--round') ROUND = Number(argv[++i])
  else if (argv[i] === '--verification') PASS = String(argv[++i])
  else if (argv[i] === '--outcome') OUTCOME = argv[++i]
  else if (argv[i] === '--new-findings') NEW_FINDINGS = argv[++i]
  else if (argv[i] === '--commit') COMMIT = argv[++i]
  else if (argv[i] === '--no-index') NO_INDEX = true
  else if (argv[i] === '--dry-run') DRY = true
  else if (argv[i] === '--in-progress') ALLOW_UNRESOLVED = true
  else rest.push(argv[i])
}
const ROOT = repoRoot(import.meta.url, root)
const USAGE = 'usage: render-records.mjs <target> [--round <n> | --verification <pass>] --outcome "<text>" [--new-findings h,m,l,c] [--commit <sha>] [--no-index] [--dry-run] [--in-progress]'
const target = rest[0]
if (!target) fail(USAGE)
const dir = join(ROOT, 'reviews', target)
if (!existsSync(dir)) fail(`no reviews/${target}/`)

function fail(m) { console.error(`ERROR   ${m}`); process.exit(1) }
// Fixers write pre_cleared either as a count or as the list of ids it counts.
const preClearedCount = v => Number.isFinite(v) ? v : Array.isArray(v) ? v.length : 0
const note = m => console.log(`note    ${m}`)
const ts = e => Date.parse(e.t)
// The same count doc-gate applies to the cell: a link is one space, not one word.
const words = s => s.replace(/\[[^\]]*\]\([^)]*\)/g, ' ').split(/\s+/).filter(w => /[a-z0-9]/i.test(w)).length
const asCode = v => String(v).split(',').map(s => s.trim().replace(/[`"]/g, '')).filter(Boolean).map(s => `\`${s}\``).join(', ')
const count = (n, word) => `${n} ${word}${n === 1 ? '' : 's'}`

const passNum = v => Number(String(v ?? '').replace(/^v/i, ''))
const VERIFY = PASS !== null
if (VERIFY && !Number.isFinite(passNum(PASS))) fail(`--verification "${PASS}" — a pass number (12 or v12)`)
if (VERIFY && ROUND !== null) fail('--verification and --round are different modes — pass one')
const pass = VERIFY ? passNum(PASS) : null

const OUTCOME_CAP = 50
if (OUTCOME === undefined) fail(`--outcome takes the text of the index row's Outcome cell; ${USAGE}`)
if (OUTCOME != null) {
  if (/^--/.test(OUTCOME)) fail(`--outcome reads "${OUTCOME}", which is another flag — quote the outcome text; ${USAGE}`)
  // The index row is pipe-delimited and one line: a stray "|" or newline would be written as a broken row.
  const stray = /[|\r\n]/.exec(OUTCOME)
  if (stray) fail(`--outcome contains ${stray[0] === '|' ? 'a "|"' : 'a line break'} — an index row is one pipe-delimited line; reword it`)
  const w = words(OUTCOME)
  if (!w) fail('--outcome is empty — the index row needs one or two sentences saying what the pass proved')
  if (w > OUTCOME_CAP) fail(`--outcome is ${w} words — the index cap is ${OUTCOME_CAP}; shorten it (doc-contracts.md)`)
}

// ---------- worklog: void filtering, then paired spans ----------
function loadEvents() {
  const lines = readLines(dir)
  if (lines === null) fail('no worklog.jsonl — the loop stamps it as work happens; nothing to compute runtime from')
  for (const l of lines) if (l.error) fail(`worklog line ${l.n}: unparseable JSON (${l.error.message})`)
  const logged = lines.map(l => l.event)
  // A void's "of" matches on a key subset, so one carrying "round" must not hit a same-timestamp stamp for another round.
  for (const v of voidsOf(logged)) {
    const hits = logged.filter(e => e.ev !== 'void' && matchesVoid(e, v)).length
    if (hits > 1) note(`the void at ${v.t} matches ${hits} events — every one of them is dropped; narrow its "of" if only one was meant`)
  }
  return live(logged)
}

const voidHint = (ev, t, key, val) => `void the wrong stamp: node reviews/lib/wl.mjs ${target} void --json '{"of":{"ev":"${ev}","t":"${t}","${key}":${JSON.stringify(val)}}}'`

// ---------- index row: built here, inserted as the newest row of the Passes table ----------
const indexPath = join(ROOT, 'reviews', relative(REVIEWS_HOME, INDEX))
const targetKey = /^\d+/.exec(target)?.[0] ?? target
// These files are CRLF: split on \n keeps every \r, so an inserted line needs its own to match.
const crOf = lines => lines.some(l => l.endsWith('\r')) ? '\r' : ''

function planIndex(row) {
  if (NO_INDEX || OUTCOME == null) return null
  if (!existsSync(indexPath)) fail(`no reviews/state/index.md under ${ROOT} — pass --no-index for a target outside the index`)
  const lines = readFileSync(indexPath, 'utf8').split('\n')
  const heading = lines.findIndex(l => /^## Passes\s*$/.test(l))
  if (heading === -1) fail('reviews/state/index.md has no "## Passes" heading — nowhere to insert the row')
  let header = -1
  for (let i = heading + 1; i < lines.length; i++) {
    if (/^## /.test(lines[i])) break
    if (/^\|/.test(lines[i])) { header = i; break }
  }
  if (header === -1) fail('reviews/state/index.md: the "## Passes" table has no header row')
  if (!/^\|[\s\-:|]+\|\s*$/.test(lines[header + 1] ?? '')) fail(`reviews/state/index.md line ${header + 1}: the "## Passes" header row is not followed by a |---| separator`)
  return { lines, at: header + 2, row, cr: crOf(lines) }
}

// ---------- ledger: Status/Affirmed cells and one appended History line ----------
const ledgerPath = join(dir, 'ledger.md')

function flipRow(text, { id, status, affirmed, history }) {
  const lines = text.split('\n')
  const rowAt = lines.findIndex(l => new RegExp(`^\\|\\s*${id}\\s*\\|`).test(l))
  if (rowAt === -1) return { text, warning: `${id} has no ledger row — nothing flipped for it` }
  const cells = lines[rowAt].split('|')
  if (cells.length < 8) return { text, warning: `${id}'s ledger row has ${Math.max(cells.length - 2, 0)} cells, too few to hold a Status — left untouched` }
  cells[6] = ` ${status} `
  if (affirmed) {
    if (cells.length > 8) cells[7] = ` ${affirmed} `
    else return { text, warning: `${id}'s ledger row has no Affirmed cell — left untouched` }
  }
  lines[rowAt] = cells.join('|')
  const blockAt = lines.findIndex(l => new RegExp(`^### ${id}\\b`).test(l))
  if (blockAt === -1) return { text: lines.join('\n'), warning: `${id} has no detail block — status flipped, no history line appended` }
  let end = lines.length
  for (let i = blockAt + 1; i < lines.length; i++) if (/^### /.test(lines[i])) { end = i; break }
  const offset = lines.slice(blockAt, end).findIndex(l => /^- \*\*History:\*\*/.test(l))
  if (offset === -1) return { text: lines.join('\n'), warning: `${id}'s detail block has no "- **History:**" line — status flipped, no history line appended` }
  let at = blockAt + offset
  for (let i = at + 1; i < end; i++) if (/^ {2}- /.test(lines[i])) at = i
  lines.splice(at + 1, 0, history + crOf(lines))
  return { text: lines.join('\n') }
}

function planLedger(flips) {
  if (!flips.length) return null
  if (!existsSync(ledgerPath)) { note(`no ledger.md in reviews/${target}/ — no status flips`); return null }
  let text = readFileSync(ledgerPath, 'utf8')
  const done = []
  for (const f of flips) {
    const r = flipRow(text, f)
    if (r.warning) note(r.warning)
    if (r.text !== text) done.push(f)
    text = r.text
  }
  return done.length ? { text, done } : null
}

// ---------- duplicate guard ----------
// A correction line supersedes a field of an earlier line, so it is never the duplicate.
const alreadyHas = match => (readMetrics(dir)?.lines ?? []).some(match)

function writeRecords(line, indexPlan, ledgerPlan, appended) {
  appendLine(dir, line)
  note(`${appended} — now run: node reviews/lib/records-auditor.mjs ${target}`)
  if (indexPlan) {
    const lines = [...indexPlan.lines]
    lines.splice(indexPlan.at, 0, indexPlan.row + indexPlan.cr)
    writeFileSync(indexPath, lines.join('\n'))
    note('inserted the index row as the newest row of the Passes table')
  }
  if (ledgerPlan) {
    writeFileSync(ledgerPath, ledgerPlan.text)
    note(`flipped ${ledgerPlan.done.length} ledger row(s)`)
  }
}

function show(line, indexPlan, ledgerPlan, flips) {
  console.log(JSON.stringify(line, null, 2))
  if (indexPlan) console.log(`\nindex row (reviews/state/index.md, newest row of the Passes table):\n${indexPlan.row}`)
  else note(NO_INDEX ? '--no-index: no index row' : 'no --outcome given — no index row to preview')
  console.log(`\nledger flips (${ledgerPlan ? ledgerPlan.done.length : 0} of ${flips.length}):`)
  for (const f of flips) console.log(`  ${f.id} → ${f.status}${f.affirmed ? ` at ${f.affirmed}` : ''}`)
}

if (VERIFY) renderVerification()
else renderFixRound()

// ---------- fix round ----------
function renderFixRound() {
  const newestRes = newest(dir, 'resolution')
  if (!newestRes) fail(`no resolution-v<n>.md in reviews/${target}/`)
  const round = ROUND ?? newestRes
  const resPath = join(dir, `resolution-v${round}.md`)
  if (!existsSync(resPath)) fail(`no resolution-v${round}.md`)
  const resText = readFileSync(resPath, 'utf8')

  // ---------- resolution frontmatter: findings map + status + fixed_commit ----------
  const parsed = parse(resText, { lenient: true })
  if (parsed.fm === null) fail('resolution has no frontmatter')
  const fm = parsed.fm
  const findings = new Map() // id -> {status, commit, note}
  // New shape: "## Findings" body table. Old shape (grandfathered): frontmatter map.
  const fSection = section(parsed.body, 'Findings')
  for (const r of fSection.split('\n').filter(l => /^\|\s*(?:D\d+|[A-Za-z]+-\d+)\s*\|/.test(l))) {
    const c = r.split('|').map(x => x.trim())
    findings.set(c[1], { status: c[2], commit: c[3] ?? null })
  }
  if (!findings.size) for (const m of fm.matchAll(/^ {2}([A-Za-z]+-?\d+(?:–[A-Za-z]*\d+)?):\s*\{\s*status:\s*([a-z-]+)([^\n]*)/gm) ?? []) {
    const restLine = m[3]
    const c = (/commit:\s*"([^"]+)"/.exec(restLine) || /commit:\s*([0-9a-f]{7,40})/.exec(restLine))?.[1] ?? null
    const noteTxt = /note:\s*"((?:[^"\\]|\\.)*)"/.exec(restLine)?.[1] ?? null
    findings.set(m[1], { status: m[2], commit: c, note: noteTxt })
  }
  if (!findings.size) fail('no "## Findings" table rows and no frontmatter findings map')
  const resStatus = word(fm, 'status') ?? 'open'
  // word() stays on the key's own line, so an empty value never reads the NEXT key's name.
  const fixedCommitRaw = word(fm, 'fixed_commit') || 'null'
  const fixedCommit = fixedCommitRaw === 'null' ? null : fixedCommitRaw.replace(/"/g, '')

  const TALLY = { fixed: 'fixed', 'wont-fix': 'wont_fix', deferred: 'deferred', backlog: 'deferred', disputed: 'disputed', 'false-positive': 'false_positive' }
  const tallies = { fixed: 0, wont_fix: 0, deferred: 0, disputed: 0, false_positive: 0, open: 0 }
  for (const { status } of findings.values()) tallies[TALLY[status] ?? 'open']++

  const events = loadEvents()
  const hint = (ev, t) => voidHint(ev, t, 'round', round)
  const { spans, open: openStart } = strictSpans(events, {
    startEv: 'round-start', endEv: 'round-end', belongs: e => e.round === round, fail,
    onSecondStart: (o, e) => `worklog: round-start ${round} at ${e.t} while the round-start at ${o.t} is still open — one of the two is a mislabel; ${hint('round-start', o.t)}`,
    onForeign: (o, e) => `worklog: round-start ${round} at ${o.t} has no round-end — ${e.ev} ${e.round} at ${e.t} follows it, so this round's end went unstamped; stamp it (node reviews/lib/wl.mjs ${target} round-end --round ${round}) or, if the start itself was the mislabel, ${hint('round-start', o.t)}`,
    onStrayEnd: (e, done) => {
      const prior = done.length ? `the previous span already closed at ${events[done[done.length - 1].to].t}` : `no round-start ${round} precedes it`
      return `worklog: round-end ${round} at ${e.t} closes nothing — ${prior}. If it is a stray stamp, ${hint('round-end', e.t)}. If the round really was resumed, the missing stamp is its round-start: re-stamp that instead, because voiding this end merges the two parts and restores the over-count`
    },
  })
  if (openStart) {
    spans.push({ from: openStart.i, to: events.length - 1 })
    note('no round-end yet — treating the last event as the current end (in-progress render)')
  }
  if (!spans.length) fail(`worklog has no round-start for round ${round}`)
  const { seqs: spanEvents, flat: inSpans } = sliceSpans(events, spans, { fail })

  // ---------- runtime: blocked (gates) / active (gaps <= 30 min) / idle, per span ----------
  const CAP_S = 30 * 60 // schema v3: gaps above this with no open gate = nobody at the wheel
  const blocked = []
  let activeRaw = 0, spanS = 0
  for (const seq of spanEvents) {
    const last = seq[seq.length - 1]
    let open = null
    for (const e of seq) {
      if (e.ev === 'gate-open' && !open) open = e
      else if (e.ev === 'gate-closed' && open) { blocked.push({ reason: open.reason ?? e.reason ?? 'unstated', s: Math.round((ts(e) - ts(open)) / 1000) }); open = null }
    }
    if (open) { blocked.push({ reason: `${open.reason ?? 'unstated'} (never closed)`, s: Math.round((ts(last) - ts(open)) / 1000) }); note('a gate-open has no gate-closed — counted to the end of its round span') }
    let inGate = false
    for (let i = 0; i < seq.length - 1; i++) {
      if (seq[i].ev === 'gate-open') inGate = true
      if (seq[i].ev === 'gate-closed') inGate = false
      if (inGate) continue
      const gap = (ts(seq[i + 1]) - ts(seq[i])) / 1000
      if (gap >= 0 && gap <= CAP_S) activeRaw += gap
    }
    spanS += Math.round((ts(last) - ts(seq[0])) / 1000)
  }
  const blockedS = blocked.reduce((a, b) => a + b.s, 0)
  const activeS = Math.round(activeRaw)
  const lastSpan = spanEvents[spanEvents.length - 1]
  const firstEvent = spanEvents[0][0], lastEvent = lastSpan[lastSpan.length - 1]
  const idleS = Math.max(0, spanS - activeS - blockedS)

  // ---------- counters from events ----------
  const by = ev => inSpans.filter(e => e.ev === ev)
  const testRuns = by('test-run')
  const finals = testRuns.filter(e => e.kind === 'final')
  const checksReturned = by('check-returned')
  const checkTokens = checksReturned.reduce((a, e) => a + (Number.isFinite(e.tokens) ? e.tokens : NaN), 0)
  const triage = by('triage-done')[0]
  const checksRun = by('check-dispatched').length
  // The round-scope review replaced per-cluster micro-reviews; both shapes count into one field.
  const microD = by('micro-review-dispatched').length + by('round-review-dispatched').length
  const microFound = [...by('micro-review-returned'), ...by('round-review-returned')].reduce((a, e) => a + (Number.isFinite(e.found) ? e.found : 0), 0)

  // ---------- base commit from the review frontmatter ----------
  const revPath = join(dir, `review-v${round}.md`)
  const baseCommit = existsSync(revPath) ? (/^commit:\s*([0-9a-f]{7,40})\b/m.exec(readFileSync(revPath, 'utf8').slice(0, 800))?.[1] ?? null) : null
  if (!baseCommit) note(`review-v${round}.md commit not found — base_commit will be null`)

  const line = {
    target, round, type: 'fix-round', date: new Date(ts(lastEvent)).toISOString().slice(0, 10),
    base_commit: baseCommit, fixed_commit: fixedCommit,
    findings: tallies,
    tests: { invocations: testRuns.length, red_runs: testRuns.filter(e => e.kind === 'red').length, green_runs: testRuns.filter(e => e.kind === 'green').length, final: finals.length ? { passed: finals[finals.length - 1].passed ?? null, failed: finals[finals.length - 1].failed ?? null } : null },
    approach_checks: { pre_cleared_consumed: preClearedCount(triage?.pre_cleared), run: checksRun, tokens: Number.isFinite(checkTokens) && checksReturned.length ? checkTokens : null },
    micro_reviews: { count: microD, follow_up_fixes: microFound },
    cost: { agents: checksRun + by('test-audit-dispatched').length + microD, tokens: null },
    runtime: { started: firstEvent.t, ended: lastEvent.t, active_s: activeS, blocked_s: blockedS, idle_s: idleS, blocked },
    notes: resStatus === 'resolved' ? '' : `resolution status: ${resStatus}`,
  }

  const clusters = Number.isFinite(triage?.clusters) ? triage.clusters : 0
  const indexPlan = planIndex(`| ${line.date} | ${targetKey} | v${round} fix round (${count(clusters, 'cluster')}, ${count(checksRun, 'approach-check')}, ${count(microD, 'micro-review')}) | — (${resStatus}) | 0/0/0/0 | ${OUTCOME} | [resolution](../${target}/resolution-v${round}.md) · [ledger](../${target}/ledger.md) |`)

  const flips = [...findings].map(([id, f]) => {
    const rowCommit = f.commit && /[0-9a-f]{7,40}/.test(f.commit) ? asCode(f.commit) : null
    return {
      id, status: f.status,
      affirmed: f.status === 'fixed' && fixedCommit ? asCode(fixedCommit) : null,
      history: `  - v${round}: fix round — ${f.status}${f.status === 'fixed' && rowCommit ? ` at ${rowCommit}` : ''}`,
    }
  })
  const ledgerPlan = planLedger(flips)

  if (!DRY) {
    if (OUTCOME == null && !NO_INDEX) fail(`--outcome "<text>" is required to append records (it is the index row's Outcome cell, max ${OUTCOME_CAP} words) — or pass --no-index for a target outside the index`)
    if (resStatus !== 'resolved' && !ALLOW_UNRESOLVED) fail(`resolution-v${round}.md reads status: ${resStatus} — finish the round or pass --in-progress (--dry-run renders at any status)`)
    if (alreadyHas(o => o.type === 'fix-round' && o.round === round)) fail(`metrics.jsonl already has a fix-round line for round ${round} — append a correction line instead (schema: Corrections)`)
  }
  show(line, indexPlan, ledgerPlan, flips)
  if (DRY) { note('dry-run: nothing written'); process.exit(0) }
  writeRecords(line, indexPlan, ledgerPlan, `appended fix-round line for round ${round}`)
}

// ---------- verification ----------
function renderVerification() {
  const parts = String(NEW_FINDINGS).split(',').map(s => s.trim())
  if (parts.length !== 4 || parts.some(p => !/^\d+$/.test(p))) fail(`--new-findings "${NEW_FINDINGS}" — four counts, h,m,l,c (e.g. 0,1,0,0)`)
  const [high, medium, low, cleanup] = parts.map(Number)

  const events = loadEvents()
  const hint = (ev, e) => voidHint(ev, e.t, 'pass', e.pass)
  const { spans, open: openPass } = strictSpans(events, {
    startEv: 'pass-launch', endEv: 'pass-records-done', belongs: e => passNum(e.pass) === pass, fail,
    onSecondStart: (o, e) => `worklog: pass-launch ${PASS} at ${e.t} while the pass-launch at ${o.t} is still open — one of the two is a mislabel; ${hint('pass-launch', o)}`,
    onForeign: (o, e) => `worklog: pass-launch ${PASS} at ${o.t} has no pass-records-done — ${e.ev} ${e.pass} at ${e.t} follows it, so this pass's records-done went unstamped; stamp it (node reviews/lib/wl.mjs ${target} pass-records-done --pass v${pass}) or, if the launch itself was the mislabel, ${hint('pass-launch', o)}`,
    onStrayEnd: (e, done) => {
      const prior = done.length ? `the previous span already closed at ${events[done[done.length - 1].to].t}` : `no pass-launch ${PASS} precedes it`
      return `worklog: pass-records-done ${PASS} at ${e.t} closes nothing — ${prior}. If it is a stray stamp, ${hint('pass-records-done', e)}`
    },
  })
  if (openPass) {
    spans.push({ from: openPass.i, to: events.length - 1 })
    note('no pass-records-done yet — treating the last event as the current end (in-progress render)')
  }
  if (!spans.length) fail(`worklog has no pass-launch for pass v${pass}`)
  const { seqs: spanEvents, flat: inSpans } = sliceSpans(events, spans, { fail })
  const lastSpan = spanEvents[spanEvents.length - 1]
  const firstEvent = spanEvents[0][0], lastEvent = lastSpan[lastSpan.length - 1]

  const results = new Map() // id -> the last verify-result for it in the span
  for (const e of inSpans) if (e.ev === 'verify-result' && e.id) results.set(e.id, e)
  if (!results.size) note(`no verify-result events in the pass v${pass} span — the line will read 0 verified, 0 reopened`)
  const held = [...results.values()].filter(e => e.verdict === 'held')
  const reopened = [...results.values()].filter(e => e.verdict !== 'held')

  const scored = inSpans.filter(e => e.ev === 'test-run' && Number.isFinite(e.passed) && Number.isFinite(e.failed))
  const lastRun = scored[scored.length - 1]

  const line = {
    target, pass, type: 'verification', date: new Date(ts(lastEvent)).toISOString().slice(0, 10),
    commit: COMMIT ?? anchorCommit(),
    verdict: 'approve-with-followups',
    new_findings: { high, medium, low, cleanup },
    verified: held.length, reopened: reopened.length,
    tests: lastRun ? { passed: lastRun.passed, failed: lastRun.failed } : null,
    cost: { agents: null, tokens: null },
    runtime: { started: firstEvent.t, ended: lastEvent.t },
    notes: reopened.length ? `reopened: ${reopened.map(e => `${e.id} (${e.verdict})`).join(', ')}` : '',
  }

  const indexPlan = planIndex(`| ${line.date} | ${targetKey} | v${pass} verification (anchored) | approve-with-followups | ${high}/${medium}/${low}/${cleanup} | ${OUTCOME} | [ledger](../${target}/ledger.md) |`)

  const flips = [...results.values()].map(e => e.verdict === 'held'
    ? { id: e.id, status: 'verified', affirmed: e.commit ? asCode(e.commit) : null, history: `  - v${pass}: verification — held` }
    : { id: e.id, status: 'open', affirmed: null, history: `  - v${pass}: verification — reopened (${e.verdict})` })
  const ledgerPlan = planLedger(flips)

  if (!DRY) {
    if (OUTCOME == null && !NO_INDEX) fail(`--outcome "<text>" is required to append records (it is the index row's Outcome cell, max ${OUTCOME_CAP} words) — or pass --no-index for a target outside the index`)
    if (openPass && !ALLOW_UNRESOLVED) fail(`worklog has no pass-records-done for pass v${pass} — finish the pass (node reviews/lib/wl.mjs ${target} pass-records-done --pass v${pass}) or pass --in-progress (--dry-run renders at any point)`)
    if (alreadyHas(o => o.type === 'verification' && o.pass === pass)) fail(`metrics.jsonl already has a verification line for pass ${pass} — append a correction line instead (schema: Corrections)`)
  }
  show(line, indexPlan, ledgerPlan, flips)
  if (DRY) { note('dry-run: nothing written'); process.exit(0) }
  writeRecords(line, indexPlan, ledgerPlan, `appended verification line for pass v${pass}`)
}

// A verification is anchored at the commit whose fixes it checks: the newest resolution's.
function anchorCommit() {
  const N = newest(dir, 'resolution')
  if (N) {
    const raw = word(readFileSync(join(dir, `resolution-v${N}.md`), 'utf8'), 'fixed_commit') ?? ''
    const sha = raw.replace(/["`]/g, '')
    if (sha && sha !== 'null') { note(`no --commit given — reading resolution-v${N}.md's fixed_commit ${sha} as the reviewed commit`); return sha }
  }
  note('no --commit given and no resolution names a fixed_commit — commit will be null')
  return null
}
