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
import { execSync, spawnSync } from 'node:child_process'
import { join, relative } from 'node:path'
import { ID_COUNTER, INDEX as INDEX_FILE, REVIEWS as LIVE_REVIEWS, TRACK_RECORD } from './schema.mjs'
import { auditIds, auditTarget, citationScan, listTargets } from './validate.mjs'
import { auditHandBackGates } from '../fix/handback-gates.mjs'
import { versions } from '../model/target.mjs'
import { repoRoot, takeRoot } from '../cli/args.mjs'

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
// Two git processes for a whole records tree instead of three per commit; a batch that cannot run
// leaves the cache empty, and checkSha below then asks per sha exactly as it always did.
function warmShaCache(shas) {
  if (!shas.length) return
  const batch = spawnSync('git', ['cat-file', '--batch-check'], { cwd: ROOT, encoding: 'utf8', input: `${shas.join('\n')}\n`, maxBuffer: 1 << 28 })
  if (batch.status !== 0 || typeof batch.stdout !== 'string') return
  const out = batch.stdout.split('\n')
  const oids = new Map()
  shas.forEach((sha, i) => {
    const parts = (out[i] ?? '').trim().split(' ')
    if (parts[1] === 'commit') oids.set(sha, parts[0])
  })
  let pushed = new Set()
  if (oids.size) {
    const revs = spawnSync('git', ['rev-list', '--remotes', '--tags'], { cwd: ROOT, encoding: 'utf8', maxBuffer: 1 << 28 })
    if (revs.status !== 0 || typeof revs.stdout !== 'string') return
    pushed = new Set(revs.stdout.split('\n').filter(Boolean))
  }
  for (const sha of shas) {
    const oid = oids.get(sha)
    shaCache.set(sha, oid ? { resolves: true, pushed: pushed.has(oid) } : { resolves: false, pushed: false })
  }
}
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

const targets = listTargets(REVIEWS, { only })

// The commits are named deep inside auditTarget, so a silent pass reported nowhere collects them
// first and the batch below resolves the whole set at once.
const probed = new Set()
const quiet = () => { }
for (const t of targets) {
  auditTarget(t, { err: quiet, warn: quiet, info: quiet, gates: quiet, versions, index: INDEX, track: TRACK,
    checkSha: sha => { probed.add(sha); return { resolves: true, pushed: true } } })
}
warmShaCache([...probed])

const ctx = { err, warn, info, checkSha, versions, gates: auditHandBackGates, index: INDEX, track: TRACK }
for (const t of targets) auditTarget(t, ctx)

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
