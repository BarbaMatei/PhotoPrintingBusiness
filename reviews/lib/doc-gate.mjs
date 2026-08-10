#!/usr/bin/env node
// Deterministic half of the round-end doc gate (reviews/doc-contracts.md).
// Checks whichever of the round's files exist against the templates: frontmatter keys,
// heading order and wording, size caps, ID rules, ledger append-only (vs git HEAD),
// cross-file agreement. The Haiku judge covers language; this covers structure.
// Judges only — never edits. Old-shape targets are out of scope (grandfathered).
//
// Usage: node reviews/lib/doc-gate.mjs [--root <repoRoot>] <target> <pass>
// Exit: 0 clean · 1 violations (listed) · 2 usage/IO error.
import { readFileSync, existsSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { execFileSync } from 'node:child_process'

const argv = process.argv.slice(2)
let root = null
const rest = []
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === '--root') root = argv[++i]
  else rest.push(argv[i])
}
const [target, passArg] = rest
const pass = Number(passArg)
if (!target || !Number.isFinite(pass)) {
  console.error('usage: node reviews/lib/doc-gate.mjs [--root <repoRoot>] <target> <pass>')
  process.exit(2)
}
const REVIEWS = root ? join(root, 'reviews') : join(dirname(fileURLToPath(import.meta.url)), '..')
let dir = join(REVIEWS, target)
if (!existsSync(dir)) dir = join(REVIEWS, 'archive', target)
if (!existsSync(dir)) { console.error(`no such target folder: ${join(REVIEWS, target)} (also tried archive/)`); process.exit(2) }
const name = target.replace(/^archive\//, '')
const gitPath = dir.slice(join(REVIEWS, '..').length + 1).replace(/\\/g, '/')

const problems = []
const bad = (file, msg) => problems.push(`${file}: ${msg}`)

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

const SEV = ['🔴', '🟠', '🟡', '⚪']
const BANNED = /\b(critical|severe|blocker)s?\b/i

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
    const rows = p.body.split('\n').filter(l => /^\|\s*F\d+\s*\|/.test(l))
    for (const r of rows) {
      const cells = r.split('|').map(c => c.trim())
      if (!/^D\d+$/.test(cells[2] ?? '')) bad(reviewFile, `finding row "${cells[1]}" has no D# in column 2 — reviews are finalized after reconciliation`)
      if (!SEV.includes(cells[3] ?? '')) bad(reviewFile, `finding row "${cells[1]}" severity cell is "${cells[3]}" — one of ${SEV.join(' ')} only`)
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
    for (const r of findingsSection.split('\n').filter(l => /^\|/.test(l) && !/^\|\s*(D#|-)/.test(l))) {
      const cells = r.split('|').map(c => c.trim())
      if (!/^D\d+$/.test(cells[1] ?? '')) { bad(resolutionFile, `findings row key "${cells[1]}" — D# only (doc-contracts.md)`); continue }
      resolutionKeys.push(cells[1])
      if (!RSTAT.test(cells[2] ?? '')) bad(resolutionFile, `${cells[1]} status "${cells[2]}" — one status word, never "verified" (that belongs to a re-review)`)
      if ((cells[4] ?? '').length > 240) bad(resolutionFile, `${cells[1]} note is ${cells[4].length} chars — cap is 240; the story goes in Decisions`)
    }
    if (!resolutionKeys.length) bad(resolutionFile, 'the "## Findings" table has no rows')
    const got = headings(p.body)
    if (!got[0] || !new RegExp(`^# Resolution v${pass} — ${name}$`).test(got[0]))
      bad(resolutionFile, `title is "${got[0] ?? '(none)'}" — template requires "# Resolution v${pass} — ${name}"`)
    for (const h of ['## Findings', '## Scope', '## Decisions']) if (!got.includes(h)) bad(resolutionFile, `missing heading "${h}"`)
    if (bodyLines(p.body) > 200) bad(resolutionFile, `body is ${bodyLines(p.body)} non-empty lines — cap is 200`)
    const decisions = p.body.split(/^### /m).slice(1)
    for (const d of decisions) {
      const n = d.split('\n').filter(l => l.trim() !== '').length - 1
      if (n > 15) bad(resolutionFile, `decision "${d.split('\n')[0]}" is ${n} lines — cap is 15`)
    }
  }
}

// ---------- ledger.md: block caps, status vocabulary, append-only vs git HEAD ----------
const STATUSES = new Set(['open', 'in-progress', 'fixed', 'verified', 'wont-fix', 'deferred', 'disputed', 'false-positive', 'backlog'])
const blocksOf = raw => {
  const map = new Map()
  for (const m of raw.matchAll(/^### (D\d+)[^\n]*\n([\s\S]*?)(?=^### D\d+|(?![\s\S]))/gm)) map.set(m[1], m[2])
  return map
}
if (existsSync(join(dir, 'ledger.md'))) {
  const raw = read('ledger.md')
  for (const m of raw.matchAll(/^\|\s*(D\d+)\s*\|(.*)\|\s*$/gm)) {
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
  const blockers = (fmVal(reviewFm, 'blockers') ?? '').match(/D\d+/g) ?? []
  for (const b of blockers) if (!resolutionKeys.includes(b))
    bad(resolutionFile, `review blocker ${b} has no entry in the findings map`)
}

if (problems.length) {
  console.log(`DOC GATE: ${problems.length} violation(s) for ${target} v${pass}:\n`)
  for (const p of problems) console.log(`  - ${p}`)
  console.log('\nFix the files and re-run. The gate judges only — it never edits (reviews/doc-contracts.md).')
  process.exit(1)
}
console.log(`DOC GATE: clean for ${target} v${pass}.`)
