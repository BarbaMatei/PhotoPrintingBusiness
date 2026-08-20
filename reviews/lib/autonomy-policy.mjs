#!/usr/bin/env node
// Unattended-run policy for the review loop: the written delegated-decision rules, executable.
// The driver consults this at every router gate instead of stopping. Every gate with a written
// rule below proceeds on the owner's standing approval (2026-08-20) — certification and loop
// close included. Fail closed: a gate kind this file does not know answers "stop".
//
// Usage: node reviews/lib/autonomy-policy.mjs [--root <repoRoot>] <target> decide <gate-kind>
// Output: ACTION: auto|stop, then NEXT (auto only: delta discovery · certification (pair) ·
// certification (single) · close the loop) and REASON.
// Exit: 0 answered · 1 usage error or unknown target.
import { readFileSync, readdirSync, existsSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

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

if (gateKind === 'loop-close') {
  say('ACTION', 'auto')
  say('NEXT', 'close the loop')
  say('REASON', 'standing owner approval (2026-08-20): the run closes the loop itself and reports the close')
  process.exit(0)
}
if (gateKind === 'delta-worthiness') {
  const reviews = readdirSync(dir).map(f => /^review-v(\d+)\.md$/.exec(f)).filter(Boolean).map(m => Number(m[1]))
  if (!reviews.length) stop('no review file — delta-worthiness cannot be judged mechanically')
  const N = Math.max(...reviews)
  const resPath = join(dir, `resolution-v${N}.md`)
  if (!existsSync(resPath)) stop(`no resolution-v${N}.md — delta-worthiness cannot be judged mechanically`)
  const fmBlock = /^---\r?\n([\s\S]*?)\r?\n---/.exec(readFileSync(join(dir, `review-v${N}.md`), 'utf8'))?.[1] ?? ''
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
  const metricsPath = join(dir, 'metrics.jsonl')
  const hasCert = existsSync(metricsPath) && readFileSync(metricsPath, 'utf8').split(/\r?\n/).filter(l => l.trim())
    .some(l => { try { return JSON.parse(l).type === 'certification' } catch { return false } })
  say('ACTION', 'auto')
  say('NEXT', hasCert ? 'certification (single)' : 'certification (pair)')
  say('REASON', 'patch-grade by the mechanical half of the rule (no high-severity id fixed); loop quiet — certification proceeds on the standing owner approval (2026-08-20)')
  process.exit(0)
}
stop(`gate "${gateKind}" has no written delegation — fail closed`)
