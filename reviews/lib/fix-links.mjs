#!/usr/bin/env node
// Reference keeper for the review system's prose. Two jobs:
//   check (default): every markdown link under reviews/ must resolve to a file.
//   apply: rewrite path fragments after a move (edit MOVES, run with --apply).
// Moving a file = git mv + update lib/paths.mjs + MOVES here + run --apply, then check.
import { readFileSync, writeFileSync, existsSync, readdirSync, statSync } from 'node:fs'
import { join, dirname, relative, sep } from 'node:path'
import { REVIEWS, REPO } from './records/schema.mjs'

const MOVES = {
  'system/review-v1.md': 'system/review-v1/review-v1.md',
  'system/resolution-v1.md': 'system/review-v1/resolution-v1.md',
  'system/summary-v1.md': 'system/review-v1/summary-v1.md',
  'system/scorecard.md': 'system/review-v1/scorecard.md',
}

const mdFiles = []
;(function walk(d) {
  for (const e of readdirSync(d, { withFileTypes: true })) {
    const p = join(d, e.name)
    if (e.isDirectory()) walk(p)
    else if (e.name.endsWith('.md')) mdFiles.push(p)
  }
})(REVIEWS)
const extra = [
  join(REPO, 'CLAUDE.md'), join(REPO, '.githooks', 'pre-commit'),
  join(REPO, 'memory-bank', 'standards', 'bolt-process.md'),
]
for (const d of [join(REPO, '.claude', 'skills')]) {
  if (!existsSync(d)) continue
  for (const e of readdirSync(d, { withFileTypes: true })) {
    const s = join(d, e.name, 'SKILL.md')
    if (e.isDirectory() && existsSync(s)) extra.push(s)
  }
}
for (const s of [...extra, ...readdirSync(join(REVIEWS, 'lib')).filter(f => f.endsWith('.mjs') || f.endsWith('.wf.js')).map(f => join(REVIEWS, 'lib', f))].filter(existsSync)) mdFiles.push(s)

if (process.argv.includes('--apply')) {
  let hits = 0
  for (const p of mdFiles) {
    if (p === join(REVIEWS, 'lib', 'fix-links.mjs')) continue
    let txt = readFileSync(p, 'utf8')
    const before = txt
    for (const [oldF, newF] of Object.entries(MOVES)) {
      txt = txt.split('reviews/' + oldF).join('reviews/' + newF)
      if (dirname(p) === REVIEWS) {
        txt = txt.replace(new RegExp('\\]\\((?:\\./)?' + oldF.replace('.', '\\.') + '(#[^)]*)?\\)', 'g'), '](' + newF + '$1)')
      }
    }
    if (txt !== before) { writeFileSync(p, txt); hits++ }
  }
  // A moved file's own relative links still point from its OLD directory: re-resolve each
  // against the old location and re-relativize from the new one.
  for (const [oldF, newF] of Object.entries(MOVES)) {
    const p = join(REVIEWS, newF)
    if (!existsSync(p) || !newF.endsWith('.md')) continue
    const oldDir = dirname(join(REVIEWS, oldF)), newDir = dirname(p)
    if (oldDir === newDir) continue
    let txt = readFileSync(p, 'utf8')
    const before = txt
    txt = txt.replace(/\]\(([^)\s]+)\)/g, (whole, t) => {
      if (/^https?:|^#|^mailto:/.test(t)) return whole
      const [path, ...anch] = t.split('#')
      const anchor = anch.length ? '#' + anch.join('#') : ''
      if (existsSync(join(newDir, path))) return whole
      const abs = join(oldDir, path)
      if (!existsSync(abs)) return whole
      return '](' + relative(newDir, abs).split(sep).join('/') + anchor + ')'
    })
    if (txt !== before) { writeFileSync(p, txt); hits++ }
  }
  console.log(`apply: ${hits} files rewritten`)
}

let broken = 0
for (const p of mdFiles.filter(f => f.endsWith('.md') && f.startsWith(REVIEWS))) {
  const txt = readFileSync(p, 'utf8')
  for (const m of txt.matchAll(/\]\(([^)\s]+)\)/g)) {
    const t = m[1]
    if (/^https?:|^#|^mailto:/.test(t)) continue
    if (!existsSync(join(dirname(p), t.split('#')[0]))) { console.log(`BROKEN ${p.slice(REPO.length + 1)}: ${t}`); broken++ }
  }
}
console.log(broken ? `${broken} broken link(s)` : 'all reviews/ links resolve')
process.exit(broken ? 1 : 0)
