#!/usr/bin/env node
// Unattended-run policy for the review loop: the written delegated-decision rules, executable.
// The driver consults this at every router gate instead of stopping. Every gate with a written
// rule below proceeds on the owner's standing approval (2026-08-20) — certification and loop
// close included. Fail closed: a gate kind this file does not know answers "stop".
//
// No gate that leads to a certification may pass while the ledger still has open work: the
// router only meets the pre-certification sweep on its loop-quiet row, so both certification-
// bound gates read the ledger here too.
//
// Usage: node reviews/lib/autonomy-policy.mjs [--root <repoRoot>] <target> decide <gate-kind>
// Output: ACTION: auto|stop, then NEXT (auto only: fix round · delta discovery · certification
// (pair) · certification (single) · close the loop) and REASON.
// Exit: 0 answered · 1 usage error or unknown target.
import { readFileSync, readdirSync, existsSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { readLedger, openIds, standsDown } from './ledger.mjs'

const argv = process.argv.slice(2)
let root = null
const rest = []
for (let i = 0; i < argv.length; i++) argv[i] === '--root' ? (root = argv[++i]) : rest.push(argv[i])
const [target, cmd, gateKind] = rest
const REVIEWS = root ? join(root, 'reviews') : join(dirname(fileURLToPath(import.meta.url)), '..')
if (!target || cmd !== 'decide' || !gateKind) {
  console.error('usage: node reviews/lib/autonomy-policy.mjs [--root <repoRoot>] <target> decide <gate-kind>')
  process.exit(1)
}
const dir = [join(REVIEWS, target), join(REVIEWS, 'archive', target)].find(existsSync)
if (!dir) { console.error(`no reviews folder for "${target}"`); process.exit(1) }

const say = (k, v) => console.log(`${k}: ${v}`)
const stop = reason => { say('ACTION', 'stop'); say('REASON', reason); process.exit(0) }

function hasCertification(targetDir) {
  const metricsPath = join(targetDir, 'metrics.jsonl')
  return existsSync(metricsPath) && readFileSync(metricsPath, 'utf8').split(/\r?\n/).filter(l => l.trim())
    .some(l => { try { const e = JSON.parse(l); return e.outcome === 'certified' || /^certification/.test(e.subtype ?? '') } catch { return false } })
}

function latestPassType() {
  const metricsPath = join(dir, 'metrics.jsonl')
  if (!existsSync(metricsPath)) return null
  const lines = readFileSync(metricsPath, 'utf8').split(/\r?\n/).filter(l => l.trim())
    .map(l => { try { return JSON.parse(l) } catch { return null } })
    .filter(l => l && !l.correction_for)
  return lines.length ? lines[lines.length - 1].type ?? null : null
}

// Open ledger work outranks any answer that would launch a certification — and only those
// answers, so a delta-worthy round is judged first and keeps its delta discovery. Silent on a
// target with no ledger, on a round awaiting its verification, and at the loop-close gate:
// 🟠 open at close roll into the backlog by design.
function openWork() {
  const led = readLedger(join(dir, 'ledger.md'))
  if (!led || standsDown(dir, latestPassType())) return null
  const high = openIds(led.rows, '🔴')
  if (high.length) return `the loop is armed — ${high.length} open 🔴 (${high.join(', ')})`
  const medium = openIds(led.rows, '🟠')
  if (medium.length) return `sweep before certification — ${medium.length} open medium${medium.length === 1 ? '' : 's'} must drain (${medium.join(', ')})`
  return null
}
function sweepFirst() {
  const reason = openWork()
  if (!reason) return
  say('ACTION', 'auto')
  say('NEXT', 'fix round')
  say('REASON', reason)
  process.exit(0)
}

if (gateKind === 'loop-close') {
  say('ACTION', 'auto')
  say('NEXT', 'close the loop')
  say('REASON', 'standing owner approval (2026-08-20): the run closes the loop itself and reports the close')
  process.exit(0)
}
if (gateKind === 'delta-worthiness') {
  // Judge the round that just ran, which is the newest resolution. A round answering a
  // verification pass raises no review file, so it has no blocker list and is patch-grade
  // unless its own review file says otherwise.
  const resolutions = readdirSync(dir).map(f => /^resolution-v(\d+)\.md$/.exec(f)).filter(Boolean).map(m => Number(m[1]))
  if (!resolutions.length) stop('no resolution file — delta-worthiness cannot be judged mechanically')
  const RN = Math.max(...resolutions)
  const resPath = join(dir, `resolution-v${RN}.md`)
  const reviewPath = join(dir, `review-v${RN}.md`)
  const fmBlock = existsSync(reviewPath)
    ? /^---\r?\n([\s\S]*?)\r?\n---/.exec(readFileSync(reviewPath, 'utf8'))?.[1] ?? ''
    : ''
  const lines = fmBlock.split(/\r?\n/)
  const bi = lines.findIndex(l => /^blockers:/.test(l))
  const blockers = []
  if (bi >= 0) {
    blockers.push(...(lines[bi].match(/PPW-\d+/g) ?? []))
    for (let i = bi + 1; i < lines.length && /^\s/.test(lines[i]); i++) blockers.push(...(lines[i].match(/PPW-\d+/g) ?? []))
  }
  const fixed = new Set([...readFileSync(resPath, 'utf8').matchAll(/^\|\s*(PPW-\d+)\s*\|\s*fixed\s*\|/gm)].map(m => m[1]))
  const hit = blockers.filter(b => fixed.has(b))
  if (hit.length) {
    say('ACTION', 'auto')
    say('NEXT', 'delta discovery')
    say('REASON', `the fix round fixed high-severity ${hit.join(', ')} — delta-worthy by the mechanical half of the rule`)
    process.exit(0)
  }
  sweepFirst()
  const hasCert = hasCertification(dir)
  say('ACTION', 'auto')
  say('NEXT', hasCert ? 'certification (single)' : 'certification (pair)')
  say('REASON', 'patch-grade by the mechanical half of the rule (no high-severity id fixed); loop quiet — certification proceeds on the standing owner approval (2026-08-20)')
  process.exit(0)
}
if (gateKind === 'certification-go-ahead') {
  sweepFirst()
  const hasCert = hasCertification(dir)
  say('ACTION', 'auto')
  say('NEXT', hasCert ? 'certification (single)' : 'certification (pair)')
  say('REASON', 'loop quiet — certification proceeds on the standing owner approval (2026-08-20)')
  process.exit(0)
}
stop(`gate "${gateKind}" has no written delegation — fail closed`)
