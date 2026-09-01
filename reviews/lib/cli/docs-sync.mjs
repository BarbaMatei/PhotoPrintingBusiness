#!/usr/bin/env node
// Keeps the review system's prose in step with the code that decides. Two jobs:
//   --check (default): every generated block must equal what the code renders today, and every
//                      markdown link under reviews/ must resolve to a file.
//   --write:           regenerate every block in place.
// A generated block is the text between `<!-- generated:<name> -->` and its closing marker; the
// prose around the markers is the owner's and is never touched. docs-blocks.mjs names the one
// machine home of each table, so a rule with two homes cannot drift apart silently.
// Drift and broken links are separate signals: the drift half is fast and mechanical, the link
// half reads records nobody may edit to make a hook green, so only the drift half is hook-grade.
//
// Usage: node reviews/lib/cli/docs-sync.mjs [--root <repoRoot>] [--check | --write] [--no-links] [--links]
// Exit: 0 in step (or written) · 1 a block is stale, a marker is missing, or --check found a
//       broken link. `--no-links` skips the link scan (drift only, what the pre-commit hook runs);
//       `--write` never counts links in its exit unless `--links` asks for it.
import { readFileSync, writeFileSync, existsSync, readdirSync } from 'node:fs'
import { join, dirname, relative } from 'node:path'
import { BLOCKS, hookCoverage, lensCoverage } from './docs-blocks.mjs'
import { repoRoot, takeRoot } from './args.mjs'

const { root, rest } = takeRoot(process.argv.slice(2))
const REPO = repoRoot(import.meta.url, root)
const REVIEWS = join(REPO, 'reviews')
const flag = name => rest.includes(name)

const OPEN = n => `<!-- generated:${n} -->`
const CLOSE = n => `<!-- /generated:${n} -->`
// Every comparison runs on LF text; a file keeps the line ending it already had when written back.
const eolOf = text => (text.includes('\r\n') ? '\r\n' : '\n')
const toLf = text => text.split('\r\n').join('\n')
function splice(text, name, body) {
  const open = text.indexOf(OPEN(name)), close = text.indexOf(CLOSE(name))
  if (open === -1 || close === -1 || close < open) return null
  const head = text.slice(0, open + OPEN(name).length)
  return { text: `${head}\n${body}\n${text.slice(close)}`, current: text.slice(open + OPEN(name).length, close).replace(/^\n|\n$/g, '') }
}

const write = flag('--write')
const problems = []
const stale = []
const files = new Map()
const eols = new Map()
for (const b of BLOCKS) {
  const path = join(REPO, b.file)
  if (!eols.has(path) && existsSync(path)) eols.set(path, eolOf(readFileSync(path, 'utf8')))
  const text = files.get(path) ?? (existsSync(path) ? toLf(readFileSync(path, 'utf8')) : null)
  if (text === null) { problems.push(`${b.file}: file not found (block ${b.name})`); continue }
  const spliced = splice(text, b.name, b.render())
  if (!spliced) { problems.push(`${b.file}: no <!-- generated:${b.name} --> markers`); continue }
  files.set(path, spliced.text)
  if (spliced.current !== b.render()) stale.push({ block: b, current: spliced.current, wanted: b.render() })
}
if (write) {
  for (const [file, text] of files) writeFileSync(file, text.split('\n').join(eols.get(file)))
  console.log(`docs-sync --write: ${BLOCKS.length} blocks rendered into ${files.size} files`)
}
for (const s of stale) {
  if (write) continue
  console.log(`STALE ${s.block.file} — generated:${s.block.name}`)
  const now = s.current.split('\n'), want = s.wanted.split('\n')
  for (let i = 0; i < Math.max(now.length, want.length); i++) {
    if (now[i] === want[i]) continue
    if (now[i] !== undefined) console.log(`  - ${now[i]}`)
    if (want[i] !== undefined) console.log(`  + ${want[i]}`)
  }
}
problems.push(...lensCoverage())
problems.push(...hookCoverage(REPO))

// Every markdown link under reviews/ must resolve to a file.
const mdFiles = []
if (!flag('--no-links')) (function walk(d) {
  for (const e of readdirSync(d, { withFileTypes: true })) {
    const p = join(d, e.name)
    if (e.isDirectory()) walk(p)
    else if (e.name.endsWith('.md')) mdFiles.push(p)
  }
})(REVIEWS)
let broken = 0
for (const p of mdFiles) {
  for (const m of readFileSync(p, 'utf8').matchAll(/\]\(([^)\s]+)\)/g)) {
    const t = m[1]
    if (/^https?:|^#|^mailto:/.test(t)) continue
    if (!existsSync(join(dirname(p), t.split('#')[0]))) { console.log(`BROKEN ${relative(REPO, p)}: ${t}`); broken++ }
  }
}
if (!flag('--no-links')) console.log(broken ? `${broken} broken link(s)` : 'all reviews/ links resolve')
for (const p of problems) console.log(`PROBLEM ${p}`)
// The drift verdict is printed on its own, so a broken link somewhere in the records never hides
// the answer the hook and the suite ask for.
if (!write && !stale.length && !problems.length) console.log(`docs-sync --check: ${BLOCKS.length} generated blocks in step`)
const linksCount = write ? flag('--links') : true
process.exit((write ? 0 : stale.length) + problems.length + (linksCount ? broken : 0) ? 1 : 0)
