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
// Output: ACTION: auto|stop, then NEXT (auto only: fix round · delta discovery ·
// lens-coverage discovery (<lens>) · certification (pair) · certification (single) ·
// close the loop) and REASON.
// A design-pass gate and any gate override logged after the run's start always stop.
// Exit: 0 answered · 1 usage error or unknown target.
import { readFileSync, readdirSync, existsSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { readLedger, openIds, standsDown } from './ledger.mjs'
import { parse } from './records/frontmatter.mjs'
import { readMetrics } from './records/metrics.mjs'
import { readEvents } from './records/worklog.mjs'
import { MANIFEST_LENSES } from './records/schema.mjs'

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

const metricsLines = targetDir => readMetrics(targetDir)?.lines ?? []
function hasCertification(targetDir) {
  return metricsLines(targetDir).some(e => e.outcome === 'certified' || /^certification/.test(e.subtype ?? ''))
}
// Convergence rule (2026-08-28): certification is never auto-approved while a manifest lens
// has not run on the target, or while no blind pass has run since the last substantive fix
// round (its seed rate would be unmeasured). Mirrors the router's row-6 refusal.
function certificationBlocker(targetDir) {
  const lines = metricsLines(targetDir)
  const lensUnion = new Set()
  for (const l of lines) if (Array.isArray(l.lenses)) for (const k of l.lenses) lensUnion.add(k)
  const owed = MANIFEST_LENSES.filter(k => !lensUnion.has(k))
  if (owed.length) return { next: `lens-coverage discovery (${owed[0]})`, reason: `certification refused on lens-coverage debt — never run on this target: ${owed.join(', ')} (audit R5)` }
  const isBlind = l => l.type === 'discovery' || l.type === 'delta-discovery'
  const substantive = l => l.type === 'fix-round' && (l.findings?.fixed ?? 0) > 0 && (l.tests?.invocations ?? 0) > 0
  let lastFixIdx = -1
  for (let i = lines.length - 1; i >= 0; i--) if (substantive(lines[i])) { lastFixIdx = i; break }
  if (lastFixIdx !== -1 && !lines.some((l, i) => i > lastFixIdx && isBlind(l)))
    return { next: 'delta discovery', reason: `round r${lines[lastFixIdx].round}'s seed rate is unmeasured — no blind pass has run since it; a delta discovery measures it before certification (convergence rule, 2026-08-28)` }
  return null
}
// Gate overrides (COMMENTS_OK / DOCGATE_OK, logged by the pre-commit hook) are a hard stop
// during an unattended run: any override newer than the run's start ends the delegation.
{
  const overridesPath = join(REVIEWS, 'state', 'overrides.jsonl')
  // Voided events are dropped here too: a mis-stamped run-start must not move the cut-off.
  const events = readEvents(dir)
  if (existsSync(overridesPath) && events) {
    let runStart = null
    for (const e of events) {
      if (e.ev === 'run-start' && Number.isFinite(Date.parse(e.t)) && (runStart === null || Date.parse(e.t) > runStart)) runStart = Date.parse(e.t)
    }
    if (runStart !== null) {
      for (const l of readFileSync(overridesPath, 'utf8').split(/\r?\n/).filter(x => x.trim())) {
        try {
          const o = JSON.parse(l)
          if (Number.isFinite(Date.parse(o.t)) && Date.parse(o.t) > runStart)
            stop(`a gate override (${o.var}) was logged at ${o.t}, after this run started — an override during an unattended run is a hard stop (audit R2)`)
        } catch { }
      }
    }
  }
}

// Open ledger work outranks any answer that would launch a certification — and only those
// answers, so a delta-worthy round is judged first and keeps its delta discovery. Silent on a
// target with no ledger, on a round awaiting its verification, and at the loop-close gate:
// 🟠 open at close roll into the backlog by design.
function openWork() {
  const led = readLedger(join(dir, 'ledger.md'))
  if (!led || standsDown(dir)) return null
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
    ? parse(readFileSync(reviewPath, 'utf8')).fm ?? ''
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
  const blocker = certificationBlocker(dir)
  if (blocker) {
    say('ACTION', 'auto')
    say('NEXT', blocker.next)
    say('REASON', `patch-grade by the mechanical half of the rule (no high-severity id fixed), but ${blocker.reason}`)
    process.exit(0)
  }
  const hasCert = hasCertification(dir)
  say('ACTION', 'auto')
  say('NEXT', hasCert ? 'certification (single)' : 'certification (pair)')
  say('REASON', 'patch-grade by the mechanical half of the rule (no high-severity id fixed); loop quiet — certification proceeds on the standing owner approval (2026-08-20)')
  process.exit(0)
}
if (gateKind === 'certification-go-ahead') {
  sweepFirst()
  const blocker = certificationBlocker(dir)
  if (blocker) {
    say('ACTION', 'auto')
    say('NEXT', blocker.next)
    say('REASON', blocker.reason)
    process.exit(0)
  }
  const hasCert = hasCertification(dir)
  say('ACTION', 'auto')
  say('NEXT', hasCert ? 'certification (single)' : 'certification (pair)')
  say('REASON', 'loop quiet — certification proceeds on the standing owner approval (2026-08-20)')
  process.exit(0)
}
if (gateKind === 'design-pass') {
  stop('a design pass reimplements a component against a new protocol spec — an owner decision with no written delegation')
}
stop(`gate "${gateKind}" has no written delegation — fail closed`)
