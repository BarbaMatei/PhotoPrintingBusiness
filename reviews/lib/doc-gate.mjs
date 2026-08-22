#!/usr/bin/env node
// Deterministic half of the round-end doc gate (reviews/rules/doc-contracts.md).
// Checks whichever of the round's files exist against the templates: frontmatter keys,
// heading order and wording, size caps, ID rules, ledger append-only (vs git HEAD),
// cross-file agreement. The Sonnet judge covers language; this covers structure.
// Judges only — never edits. Old-shape targets are out of scope (grandfathered).
//
// Usage: node reviews/lib/doc-gate.mjs [--root <repoRoot>] <target> <pass>
//          (also lints reviews/state/backlog.md and index.md, keyed to this target)
//        node reviews/lib/doc-gate.mjs [--root <repoRoot>] state   (the cross-target files alone)
// Exit: 0 clean · 1 violations (listed) · 2 usage/IO error.
import { readFileSync, existsSync, readdirSync } from 'node:fs'
import { join, relative } from 'node:path'
import { execFileSync } from 'node:child_process'
import { BACKLOG, INDEX, REVIEWS as REVIEWS_HOME } from './paths.mjs'

const argv = process.argv.slice(2)
let root = null
const rest = []
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === '--root') root = argv[++i]
  else rest.push(argv[i])
}

const problems = []
const bad = (file, msg) => problems.push(`${file}: ${msg}`)
const SEV = ['🔴', '🟠', '🟡', '⚪']
const BANNED = /\b(critical|severe|blocker)s?\b/i
// doc-contracts.md stays the prose authority for what each area covers.
const AREAS = ['payments', 'orders', 'shipping', 'uploads', 'gallery', 'auth', 'edge', 'observability', 'jobs', 'data', 'tests', 'records']
const TARGETLESS = new Set(['lib', 'experiments', 'archive', 'state', 'rules', 'runbooks', 'notes', 'system', 'templates'])

function report(label) {
  if (problems.length) {
    console.log(`DOC GATE: ${problems.length} violation(s) for ${label}:\n`)
    for (const p of problems) console.log(`  - ${p}`)
    console.log('\nFix the files and re-run. The gate judges only — it never edits (reviews/rules/doc-contracts.md).')
    process.exit(1)
  }
  console.log(`DOC GATE: clean for ${label}.`)
  process.exit(0)
}

// Lints reviews/state/*.md; target mode below runs this too so the index row lands linted.
const stateRows = raw => raw.replace(/\r\n/g, '\n').split('\n')
  .map((line, i) => ({ line, n: i + 1, cells: line.split('|').map(c => c.trim()) }))
  .filter(r => /^\|/.test(r.line) && r.cells[1] !== 'ID' && r.cells[1] !== 'Target' && r.cells[1] !== 'Date' && !/^:?-{2,}:?$/.test(r.cells[1]))
const stateAt = p => (root ? join(root, 'reviews', relative(REVIEWS_HOME, p)) : p)
const stateSection = (raw, h) => {
  const m = new RegExp(`^## ${h}$([\\s\\S]*?)(?=^## |(?![\\s\\S]))`, 'm').exec(raw)
  return m ? m[1] : null
}

function lintStateFiles() {
  const rows = stateRows
  const at = stateAt
  const words = s => s.replace(/\[[^\]]*\]\([^)]*\)/g, ' ').split(/\s+/).filter(w => /[a-z0-9]/i.test(w)).length

  const backlogFile = at(BACKLOG)
  if (!existsSync(backlogFile)) bad('state/backlog.md', 'file is missing')
  else {
    const list = rows(readFileSync(backlogFile, 'utf8'))
    if (!list.length) bad('state/backlog.md', 'no table rows found')
    for (const r of list) {
      const f = 'state/backlog.md'
      if (r.cells.length !== 7) { bad(f, `line ${r.n}: ${Math.max(r.cells.length - 2, 0)} cells — a row has exactly 5 (ID · Target · Sev · What · Area); escape or reword a stray "|"`); continue }
      const [, id, tgt, sev, what, area] = r.cells
      if (!/^PPW-\d+$/.test(id)) bad(f, `line ${r.n}: key "${id}" — PPW-<n> only (doc-contracts.md)`)
      if (!SEV.includes(sev)) bad(f, `${id}: severity cell is "${sev}" — one of ${SEV.join(' ')} only`)
      if (!tgt) bad(f, `${id}: Target cell is empty`)
      if (!what) bad(f, `${id}: What cell is empty`)
      else if (/<br\s*\/?>/i.test(what)) bad(f, `${id}: What cell spans more than one line — a backlog row is one line`)
      const plain = area.replace(/`/g, '')
      if (!AREAS.includes(plain)) bad(f, `${id}: Area "${area}" — one of the twelve areas only: ${AREAS.join(' · ')}`)
    }
  }

  const indexFile = at(INDEX)
  if (!existsSync(indexFile)) bad('state/index.md', 'file is missing')
  else {
    const f = 'state/index.md'
    const raw = readFileSync(indexFile, 'utf8').replace(/\r\n/g, '\n')
    const reviewsHome = root ? join(root, 'reviews') : REVIEWS_HOME
    const keys = new Set()
    for (const base of [reviewsHome, join(reviewsHome, 'archive')]) {
      if (!existsSync(base)) continue
      for (const e of readdirSync(base, { withFileTypes: true })) {
        if (!e.isDirectory() || TARGETLESS.has(e.name)) continue
        keys.add(e.name)
        const num = /^\d+(-\d+)?/.exec(e.name)
        if (num) { keys.add(num[0]); keys.add(num[0].split('-')[0]) }
      }
    }
    const section = h => stateSection(raw, h)
    const glance = section('Targets at a glance')
    if (glance === null) bad(f, 'missing heading "## Targets at a glance"')
    else for (const r of rows(glance)) {
      const state = r.cells[2] ?? ''
      const breaks = (state.match(/<br\s*\/?>/gi) ?? []).length
      if (breaks > 4) bad(f, `glance row "${r.cells[1]}": State cell is ${breaks + 1} lines — cap is 5 (doc-contracts.md)`)
    }
    const passes = section('Passes')
    if (passes === null) bad(f, 'missing heading "## Passes"')
    else {
      const list = rows(passes)
      if (!list.length) bad(f, 'the "## Passes" table has no rows')
      for (const r of list) {
        const [, date, tgt, passCell, , counts, description] = r.cells
        const where = `pass row "${date} ${tgt} ${passCell}"`
        if (r.cells.length !== 7 && r.cells.length !== 9) bad(f, `${where}: ${Math.max(r.cells.length - 2, 0)} cells — a pass row has 5, or 7 when Outcome and Files apply (doc-contracts.md)`)
        if (!/^\d+\/\d+\/\d+\/\d+/.test((counts ?? '').trim())) bad(f, `${where}: New H/M/L/C cell is "${counts ?? ''}" — must open with <h>/<m>/<l>/<c>`)
        if (!/system/i.test(tgt ?? '') && !keys.has((tgt ?? '').trim())) bad(f, `${where}: target "${tgt}" is not a target folder key (${[...keys].filter(k => /^\d/.test(k)).sort().join(' · ')}); only meta rows are exempt`)
        const w = description ? words(description) : 0
        if (w > 50) bad(f, `${where}: description is ${w} words — cap is 50`)
      }
    }
  }
}

if (rest[0] === 'state') {
  lintStateFiles()
  report('the state files')
}

const [target, passArg] = rest
const pass = Number(passArg)
if (!target || !Number.isFinite(pass)) {
  console.error('usage: node reviews/lib/doc-gate.mjs [--root <repoRoot>] <target> <pass> | state')
  process.exit(2)
}
const REVIEWS = root ? join(root, 'reviews') : REVIEWS_HOME
let dir = join(REVIEWS, target)
if (!existsSync(dir)) dir = join(REVIEWS, 'archive', target)
if (!existsSync(dir)) { console.error(`no such target folder: ${join(REVIEWS, target)} (also tried archive/)`); process.exit(2) }
const name = target.replace(/^archive\//, '')
const gitPath = dir.slice(join(REVIEWS, '..').length + 1).replace(/\\/g, '/')

const read = f => readFileSync(join(dir, f), 'utf8').replace(/\r\n/g, '\n')
const split = raw => {
  const m = /^---\n([\s\S]*?)\n---\n?([\s\S]*)$/.exec(raw)
  return m ? { fm: m[1], body: m[2] } : null
}
const fmKeys = fm => fm.split('\n').filter(l => /^[a-zA-Z_-]+:/.test(l)).map(l => l.split(':')[0])
const fmVal = (fm, key) => new RegExp(`^${key}:\\s*(.+?)\\s*$`, 'm').exec(fm)?.[1] ?? null
const headings = body => body.split('\n').filter(l => /^#{1,3} /.test(l))
const bodyLines = body => body.split('\n').filter(l => l.trim() !== '').length

function checkHeadings(file, body, expected) {
  const got = headings(body)
  for (let i = 0; i < expected.length; i++) {
    if (!got[i]) { bad(file, `missing heading "${expected[i].source ?? expected[i]}"`); continue }
    const ok = expected[i] instanceof RegExp ? expected[i].test(got[i]) : got[i] === expected[i]
    if (!ok) bad(file, `heading ${i + 1} is "${got[i]}" — template requires "${expected[i].source ?? expected[i]}" (verbatim, no decorations)`)
  }
  if (got.length > expected.length) bad(file, `extra heading(s): ${got.slice(expected.length).join(' · ')}`)
}
function checkFrontmatter(file, fm, required) {
  const keys = fmKeys(fm)
  for (const k of required) if (!keys.includes(k)) bad(file, `frontmatter missing "${k}:"`)
}

// ---------- review-v<pass>.md ----------
const reviewFile = `review-v${pass}.md`
let reviewFm = null
if (existsSync(join(dir, reviewFile))) {
  const p = split(read(reviewFile))
  if (!p) bad(reviewFile, 'no frontmatter block')
  else {
    reviewFm = p.fm
    checkFrontmatter(reviewFile, p.fm, ['type', 'target', 'version', 'supersedes', 'commit', 'branch', 'pass-type', 'date', 'lenses', 'lenses-not-run', 'verdict', 'blockers', 'findings', 'tests'])
    const pt = fmVal(p.fm, 'pass-type')
    if (pt === 'verification') bad(reviewFile, 'pass-type verification — verification passes write no review file')
    checkHeadings(reviewFile, p.body, [
      new RegExp(`^# Review v${pass} — ${name}$`),
      /^## Findings$/, /^## Refuted$/, /^## Notes for the fixer$/,
    ])
    if (bodyLines(p.body) > 120) bad(reviewFile, `body is ${bodyLines(p.body)} non-empty lines — cap is 120`)
    const findingsOnly = p.body.split(/^## /m).find(s => s.startsWith('Findings')) ?? ''
    const rows = findingsOnly.split('\n').filter(l => /^\|\s*(?:PPW-|—|F\d|[A-Z]{2,7}-\d)/.test(l))
    for (const r of rows) {
      const cells = r.split('|').map(c => c.trim())
      if (!/^(PPW-\d+|—)$/.test(cells[1] ?? '')) bad(reviewFile, `finding row key "${cells[1]}" — PPW-<n> only (doc-contracts.md); reviews are finalized after reconciliation`)
      if (!SEV.includes(cells[2] ?? '')) bad(reviewFile, `finding row "${cells[1]}" severity cell is "${cells[2]}" — one of ${SEV.join(' ')} only`)
    }
    if (existsSync(join(dir, `findings-v${pass}.md`))) bad(`findings-v${pass}.md`, 'findings files are retired — detail lives on the ledger row (doc-contracts.md)')
  }
}

// ---------- summary-v<pass>.md ----------
const summaryFile = `summary-v${pass}.md`
if (existsSync(join(dir, summaryFile))) {
  const p = split(read(summaryFile))
  if (!p) bad(summaryFile, 'no frontmatter block')
  else {
    checkFrontmatter(summaryFile, p.fm, ['type', 'target', 'pass', 'pass-type', 'commit', 'date', 'decisions-needed'])
    checkHeadings(summaryFile, p.body, [
      new RegExp(`^# Owner summary — ${name} v${pass}$`),
      /^## Needs your decision$/, /^## Reasons to doubt$/, /^## Filed automatically$/, /^## State$/,
    ])
    if (bodyLines(p.body) > 60) bad(summaryFile, `body is ${bodyLines(p.body)} non-empty lines — cap is 60`)
    const banned = p.body.match(BANNED)
    if (banned) bad(summaryFile, `banned severity synonym "${banned[0]}" — use the four severity words (doc-contracts.md)`)
    if (reviewFm) {
      const rc = fmVal(reviewFm, 'commit'), sc = fmVal(p.fm, 'commit')
      if (rc && sc && rc !== sc) bad(summaryFile, `commit ${sc} differs from ${reviewFile}'s ${rc}`)
    }
  }
}

// ---------- resolution-v<pass>.md ----------
const resolutionFile = `resolution-v${pass}.md`
let resolutionKeys = []
if (existsSync(join(dir, resolutionFile))) {
  const p = split(read(resolutionFile))
  if (!p) bad(resolutionFile, 'no frontmatter block')
  else {
    checkFrontmatter(resolutionFile, p.fm, ['type', 'target', 'version', 'answers', 'status', 'fixed_commit'])
    if (/^findings:/m.test(p.fm)) bad(resolutionFile, 'frontmatter carries a findings map — per-finding state lives in the "## Findings" body table (doc-contracts.md)')
    const RSTAT = /^(fixed|wont-fix|deferred|disputed|false-positive|backlog)$/
    const findingsSection = p.body.split(/^## /m).find(s => s.startsWith('Findings')) ?? ''
    const resolutionRows = []
    for (const r of findingsSection.split('\n').filter(l => /^\|/.test(l) && !/^\|\s*(ID|D#|-)/.test(l))) {
      const cells = r.split('|').map(c => c.trim())
      if (!/^PPW-\d+$/.test(cells[1] ?? '')) { bad(resolutionFile, `findings row key "${cells[1]}" — PPW-<n> only (doc-contracts.md)`); continue }
      resolutionKeys.push(cells[1])
      resolutionRows.push({ id: cells[1], status: cells[2] ?? '' })
      if (!RSTAT.test(cells[2] ?? '')) bad(resolutionFile, `${cells[1]} status "${cells[2]}" — one status word, never "verified" (that belongs to a re-review)`)
      if ((cells[4] ?? '').length > 240) bad(resolutionFile, `${cells[1]} note is ${cells[4].length} chars — cap is 240; the story goes in Decisions`)
    }
    if (!resolutionKeys.length) bad(resolutionFile, 'the "## Findings" table has no rows')
    const got = headings(p.body)
    if (!got[0] || !new RegExp(`^# Resolution v${pass} — ${name}$`).test(got[0]))
      bad(resolutionFile, `title is "${got[0] ?? '(none)'}" — template requires "# Resolution v${pass} — ${name}"`)
    for (const h of ['## Findings', '## Scope', '## Decisions']) if (!got.includes(h)) bad(resolutionFile, `missing heading "${h}"`)
    if (bodyLines(p.body) > 200) bad(resolutionFile, `body is ${bodyLines(p.body)} non-empty lines — cap is 200`)
    const decisionsSection = p.body.split(/^## /m).find(s => s.startsWith('Decisions')) ?? ''
    const decisions = decisionsSection.split(/^### /m).slice(1)
    for (const d of decisions) {
      const n = d.split('\n').filter(l => l.trim() !== '').length - 1
      if (n > 15) bad(resolutionFile, `decision "${d.split('\n')[0]}" is ${n} lines — cap is 15`)
    }
    const decisionHeadings = decisions.map(d => d.split('\n')[0])
    for (const { id, status } of resolutionRows) {
      if (status === 'fixed') continue
      if (!decisionHeadings.some(h => h.includes(id)))
        bad(resolutionFile, `${id} status ${status} has no Decisions block — every non-fixed status needs its rationale (doc-contracts.md)`)
    }
  }
}

// ---------- ledger.md: block caps, status vocabulary, append-only vs git HEAD ----------
const STATUSES = new Set(['open', 'in-progress', 'fixed', 'verified', 'wont-fix', 'deferred', 'disputed', 'false-positive', 'backlog'])
const blocksOf = raw => {
  const map = new Map()
  for (const m of raw.matchAll(/^### (PPW-\d+)[^\n]*\n([\s\S]*?)(?=^### PPW-\d+|(?![\s\S]))/gm)) map.set(m[1], m[2])
  return map
}
if (existsSync(join(dir, 'ledger.md'))) {
  const raw = read('ledger.md')
  for (const m of raw.matchAll(/^\|\s*(PPW-\d+)\s*\|(.*)\|\s*$/gm)) {
    const cells = m[2].split('|').map(c => c.trim())
    const status = cells[4]
    if (status !== undefined && status !== '' && !STATUSES.has(status.replace(/\*/g, '')))
      bad('ledger.md', `${m[1]} Status cell is "${status}" — the status word only; narration goes in the History lines`)
  }
  const blocks = blocksOf(raw)
  for (const [id, text] of blocks) {
    const n = text.split('\n').filter(l => l.trim() !== '').length
    if (n > 20) bad('ledger.md', `detail block ${id} is ${n} lines — cap is 20`)
  }
  try {
    const head = execFileSync('git', ['show', `HEAD:${gitPath}/ledger.md`], { cwd: join(REVIEWS, '..'), encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }).replace(/\r\n/g, '\n')
    const old = blocksOf(head)
    for (const [id, oldText] of old) {
      const now = blocks.get(id)
      if (now === undefined) { bad('ledger.md', `detail block ${id} exists in HEAD but was deleted — blocks are append-only`); continue }
      const oldLines = oldText.split('\n').filter(l => l.trim() !== '')
      const nowLines = now.split('\n').filter(l => l.trim() !== '')
      for (let i = 0; i < oldLines.length; i++) {
        if (nowLines[i] !== oldLines[i]) { bad('ledger.md', `detail block ${id} line ${i + 1} changed — blocks are append-only (History lines are added, never edited)`); break }
      }
    }
  } catch { /* file not in HEAD yet (first round) — nothing to compare */ }
}

// ---------- cross-file: blockers answered when a resolution exists ----------
if (reviewFm && resolutionKeys.length) {
  const blockers = (fmVal(reviewFm, 'blockers') ?? '').match(/PPW-\d+/g) ?? []
  for (const b of blockers) if (!resolutionKeys.includes(b))
    bad(resolutionFile, `review blocker ${b} has no entry in the findings map`)
}

// ---------- state files: PPW ids and shas this target's rows cite must check out ----------
function checkTargetStateSanity() {
  const indexFile = stateAt(INDEX)
  if (!existsSync(indexFile)) return
  const raw = readFileSync(indexFile, 'utf8').replace(/\r\n/g, '\n')
  const f = 'state/index.md'

  const hasLedger = existsSync(join(dir, 'ledger.md'))
  const ledgerIds = new Set()
  if (hasLedger) for (const m of read('ledger.md').matchAll(/PPW-\d+/g)) ledgerIds.add(m[0])

  const numPrefix = (/^\d+(-\d+)?/.exec(name) ?? [name])[0]
  const passesKeys = new Set([name, numPrefix, numPrefix.split('-')[0]])
  const glanceRe = new RegExp(`^${numPrefix.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}(?!\\d)`)

  let gitAvailable = true
  try { execFileSync('git', ['rev-parse', '--git-dir'], { cwd: join(REVIEWS, '..'), stdio: ['ignore', 'ignore', 'ignore'] }) }
  catch { gitAvailable = false }
  const shaCache = new Map()
  const shaResolves = sha => {
    if (!shaCache.has(sha)) {
      try { execFileSync('git', ['cat-file', '-e', sha], { cwd: join(REVIEWS, '..'), stdio: ['ignore', 'ignore', 'ignore'] }); shaCache.set(sha, true) }
      catch { shaCache.set(sha, false) }
    }
    return shaCache.get(sha)
  }
  const scan = (where, text) => {
    if (hasLedger) for (const m of text.matchAll(/PPW-\d+/g)) if (!ledgerIds.has(m[0])) bad(f, `${where}: ${m[0]} is not in ${name}'s ledger`)
    if (!gitAvailable) return
    for (const m of text.matchAll(/\b[0-9a-f]{7,40}\b/g)) {
      const tok = m[0]
      if (!/\d/.test(tok) || /^\d+$/.test(tok)) continue
      if (!shaResolves(tok)) bad(f, `${where}: sha \`${tok}\` does not resolve (git cat-file -e)`)
    }
  }

  const glance = stateSection(raw, 'Targets at a glance')
  if (glance) for (const r of stateRows(glance)) if (glanceRe.test((r.cells[1] ?? '').trim()))
    scan(`glance row "${r.cells[1]}"`, r.cells[2] ?? '')
  const passes = stateSection(raw, 'Passes')
  if (passes) for (const r of stateRows(passes)) if (passesKeys.has((r.cells[2] ?? '').trim()))
    scan(`pass row "${r.cells[1]} ${r.cells[2]} ${r.cells[3]}"`, r.line)
}

let stateLabel = ''
if (existsSync(join(REVIEWS, 'state'))) {
  lintStateFiles()
  checkTargetStateSanity()
  stateLabel = ' + state'
}

report(`${target} v${pass}${stateLabel}`)
