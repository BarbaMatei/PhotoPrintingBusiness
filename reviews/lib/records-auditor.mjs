#!/usr/bin/env node
// Records auditor — the CLI over the mechanical checks on reviews/ structured records. It owns
// argv, the repo root, git, the two cross-target files, the warning/error tiers and the report;
// the checks themselves live in records/validate.mjs (record shape, per metrics-schema.md v3, and
// the citation-leak scan) and fix/handback-gates.mjs (the R1-R4 hand-back gates, which enforce
// the fixer's contract rather than a record's shape).
// Prose bodies (ledgers, resolutions) are NOT checked — numbers there stay a human's job.
//
// Usage: node reviews/lib/records-auditor.mjs [--root <repoRoot>] [--citations] [target ...]
// Exit 0 = no errors (warnings allowed) · 1 = errors.
import { readFileSync, existsSync } from 'node:fs'
import { execSync } from 'node:child_process'
import { join, relative } from 'node:path'
import { ID_COUNTER, INDEX as INDEX_FILE, REVIEWS as LIVE_REVIEWS, TRACK_RECORD } from './records/schema.mjs'
import { auditIds, auditTarget, citationScan, listTargets } from './records/validate.mjs'
import { auditHandBackGates } from './fix/handback-gates.mjs'
import { versions } from './model/target.mjs'
import { repoRoot, takeRoot } from './cli/args.mjs'

const argv = process.argv.slice(2)
const { root, rest: only } = takeRoot(argv)
const ROOT = repoRoot(import.meta.url, root)
const REVIEWS = join(ROOT, 'reviews')

const errors = [], warnings = [], infos = []
const err = m => errors.push(m)
const warn = m => warnings.push(m)
const info = m => infos.push(m)

function git(cmd) {
  return execSync(`git ${cmd}`, { cwd: ROOT, stdio: ['ignore', 'pipe', 'pipe'] }).toString().trim()
}
const shaCache = new Map()
function checkSha(sha) {
  if (shaCache.has(sha)) return shaCache.get(sha)
  const r = { resolves: false, pushed: false }
  try { r.resolves = git(`cat-file -t ${sha}`) === 'commit' } catch { }
  if (r.resolves) {
    try { if (git(`tag --contains ${sha}`)) r.pushed = true } catch { }
    if (!r.pushed) { try { if (git(`branch -r --contains ${sha}`)) r.pushed = true } catch { } }
  }
  shaCache.set(sha, r)
  return r
}

// Path constants come from records/schema.mjs; rebased onto REVIEWS so --root keeps working.
const underRoot = p => join(REVIEWS, relative(LIVE_REVIEWS, p))
const INDEX = existsSync(underRoot(INDEX_FILE)) ? readFileSync(underRoot(INDEX_FILE), 'utf8') : null
if (!INDEX) warn('reviews/state/index.md not found — index pairing skipped')
const TRACK = existsSync(underRoot(TRACK_RECORD)) ? readFileSync(underRoot(TRACK_RECORD), 'utf8') : null

const ctx = { err, warn, info, checkSha, versions, gates: auditHandBackGates, index: INDEX, track: TRACK }
for (const t of listTargets(REVIEWS, { only })) auditTarget(t, ctx)

auditIds(listTargets(REVIEWS, { only, all: true }), { err, counterPath: underRoot(ID_COUNTER) })

if (!only.length || argv.includes('--citations')) {
  const hits = citationScan({ git, info, warn })
  if (hits && argv.includes('--citations')) for (const h of hits) console.log(`CITE    ${h}`)
}

for (const m of errors) console.log(`ERROR   ${m}`)
for (const m of warnings) console.log(`WARN    ${m}`)
for (const m of infos) console.log(`note    ${m}`)
console.log(`\n${errors.length} error(s), ${warnings.length} warning(s).`)
process.exit(errors.length ? 1 : 0)
