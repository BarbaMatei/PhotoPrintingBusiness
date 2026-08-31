#!/usr/bin/env node
// Mechanical router for the review loop: reads a target's records and prints the next pass
// per the README router (first matching row wins), with cost estimate and gates.
// The rows themselves are data — drive/rows.mjs, which the README table is generated from; this
// file assembles the state they judge, walks them in order, and owns the two guard shapes that
// resist a row: the early guards that decide while the state is still being read, and the
// convergence brake every fix-round answer passes through.
// State sources (machine-readable only): the ledger's Findings table (which findings are still
// open) and its frontmatter `closed:`, metrics.jsonl (auditor-validated), resolution frontmatter
// (status/fixed_commit), review file inventory.
// It never launches anything — the loop-driver skill executes what this prints.
// A fix round and its verification are one reviewed unit: the unit's records render once, after
// the verification, so open 🔴/🟠 come from the ledger and not from the metrics tally. Open 🟠
// under QUEUE_THRESHOLD queue rather than spawn a round of their own, and the queue must drain
// in a sweep round before certification.
//
// Usage: node reviews/lib/route-next-pass.mjs [--root <repoRoot>] <target>
// Exit codes: 0 = next pass determined (or target closed) · 2 = OWNER GATE required first ·
//             3 = judgment call needed (facts printed; README router row cited) · 1 = error.
import { readFileSync, readdirSync, existsSync } from 'node:fs'
import { join } from 'node:path'
import { newest, versions } from './model/target.mjs'
import { atLoopClose, standsDown } from './model/open-work.mjs'
import { convergenceCheck, lastSubstantive, owedLenses, seedMeasured } from './model/convergence.mjs'
import { QUEUE_THRESHOLD, openLedger, seriousCount } from './model/queue.mjs'
import { repoRoot, takeRoot } from './cli/args.mjs'
import { parse, value } from './records/frontmatter.mjs'
import { readMetrics } from './records/metrics.mjs'
import { TARGETLESS } from './records/schema.mjs'
import { COST, ROWS } from './drive/rows.mjs'
import { GATES } from './drive/gates.mjs'

const { root, rest } = takeRoot(process.argv.slice(2))
const arg = rest.at(-1) ?? null
const REVIEWS = join(repoRoot(import.meta.url, root), 'reviews')
if (!arg) { console.error('usage: node reviews/lib/route-next-pass.mjs [--root <repoRoot>] <target>'); process.exit(1) }

function findDir(name) {
  const hits = []
  for (const base of [REVIEWS, join(REVIEWS, 'archive')]) {
    if (!existsSync(base)) continue
    for (const e of readdirSync(base, { withFileTypes: true })) {
      if (e.isDirectory() && e.name.includes(name) && !TARGETLESS.has(e.name)) {
        hits.push({ dir: join(base, e.name), name: e.name, archived: base.endsWith('archive') })
      }
    }
  }
  const exact = hits.find(h => h.name === name)
  if (exact) return exact
  if (hits.length > 1) {
    console.error(`ambiguous target "${name}" — matches: ${hits.map(h => h.name).join(', ')}`)
    process.exit(1)
  }
  return hits[0] ?? null
}
// Frontmatter-only lookup: a key in the document body never counts.
const fm = (file, key) => {
  const block = parse(readFileSync(file, 'utf8')).fm
  return block === null ? null : value(block, key)
}
const count = (n, word, plural = `${word}s`) => `${n} ${n === 1 ? word : plural}`

const out = []
const say = s => out.push(s)
function finish(code, next, gate, kind) {
  if (next) say(`NEXT: ${next}`)
  for (const [k, v] of Object.entries(COST)) if (next && next.toLowerCase().startsWith(k)) say(`COST: ${v}`)
  if (gate) say(`GATE: ${gate}`)
  if (kind) say(`GATE_KIND: ${kind}`)
  console.log(out.join('\n'))
  process.exit(code)
}

const t = findDir(arg)
if (!t) {
  say(`STATE: no reviews folder matches "${arg}"`)
  say(`ROUTER: row 1 — no review-v1.md`)
  finish(0, 'full discovery', null) // entry tier per README decides full-loop vs lean
}
say(`TARGET: ${t.name}${t.archived ? ' (archive)' : ''}`)

// Terminal state: ledger frontmatter `closed:`
const ledger = join(t.dir, 'ledger.md')
const closed = existsSync(ledger) ? fm(ledger, 'closed') : null
if (closed) {
  say(`STATE: loop CLOSED — ${closed}`)
  say(`ROUTER: terminal. Target is under watch (track-record.md); a new serious finding in its files re-arms per the README re-arm rule.`)
  finish(0, null, null)
}

const reviews = versions(t.dir, 'review')
if (!reviews.length) { say('STATE: folder exists, no review-v1.md'); say('ROUTER: row 1'); finish(0, 'full discovery', null) }
const N = Math.max(...reviews)

const metrics = readMetrics(t.dir, { strict: true })
if (!metrics) { say(`STATE: ${reviews.length} review file(s), no metrics.jsonl — non-code target?`); finish(3, null, null, GATES.noMetrics) }
const { lines, corrections } = metrics
if (!lines.length) { say('STATE: metrics.jsonl has no usable pass lines (empty or corrections-only) — repair the records first (append-only, per metrics-schema.md)'); finish(3, null, null, GATES.recordsBroken) }
const L = lines[lines.length - 1]
// Corrections are authoritative but not machine-applied here — surface the ones that
// correct the latest line: round-keyed for a fix-round line, pass-keyed for a pass line.
for (const c of corrections.filter(l => (
  L.type === 'fix-round'
    ? Number.isFinite(l.correction_for.round) && l.correction_for.round === L.round
    : Number.isFinite(l.correction_for.pass) && l.correction_for.pass === L.pass
))) {
  const which = Number.isFinite(c.correction_for.pass) ? `pass ${c.correction_for.pass}` : `fix round ${c.correction_for.round}`
  say(`NOTE: a correction line is on file for ${which} ("${c.correction_for.field}") — read it before acting on this state.`)
}

// Convergence-rule state (2026-08-28): lens-coverage union, seed-rate lineage, round sizes.
const owed = owedLenses(lines)
const lastFixIdx = lastSubstantive(lines)
const measured = seedMeasured(lines, lastFixIdx)
// EVERY fix-round answer goes through here, so the convergence brake guards them all: a component
// two rounds have failed to converge on is not patched a third time by whichever row answered first.
// That is why the design-pass row is a guard and not a row of its own — no row can outrank it.
function routeFixRound(reason) {
  say(reason)
  const c = convergenceCheck(lines)
  if (c) {
    if (c.notShrinking)
      say(`NOTE: fix rounds are not shrinking (r${c.r1.round}: ${c.r1.findings.fixed} fixed → r${c.r2.round}: ${c.r2.findings.fixed}) — the convergence rule expects each round strictly smaller than the one before.`)
    if (c.nonConvergent) {
      if (!c.capped) {
        say(`ROUTER: rounds r${c.r1.round} and r${c.r2.round} both seeded serious findings in "${c.area}" at s ≥ 0.3 (s=${c.s1.toFixed(2)}, ${c.s2.toFixed(2)}) — patching declared non-convergent for that component; further fix rounds there are refused (convergence rule, 2026-08-28).`)
        // The gate names the work it refuses: a gate line read on its own must say what is waiting.
        const trigger = reason.replace(/^ROUTER: /, '').replace(/\.$/, '')
        finish(2, null, `design pass for "${c.area}" — an R1 protocol spec at component level, reimplementation against it, then discovery; recorded as a fix round whose metrics notes carry design-pass:${c.area}; at most one per component per loop — owner decision. The fix round it refuses was triggered by: ${trigger}`, GATES.designPass)
      }
      say(`NOTE: rounds r${c.r1.round}/r${c.r2.round} still seed "${c.area}" at s ≥ 0.3, but its one design pass per loop already ran — the fix round proceeds; raise it with the owner if it seeds again.`)
    }
  }
  finish(0, 'fix round', null)
}

const RN = newest(t.dir, 'resolution')
const rStatus = RN ? fm(join(t.dir, `resolution-v${RN}.md`), 'status') : null
const rCommit = RN ? fm(join(t.dir, `resolution-v${RN}.md`), 'fixed_commit') : null

const serious = L.new_findings ? (L.new_findings.high || 0) + (L.new_findings.medium || 0) : 0
say(`STATE: latest review v${N} · last ${L.type === 'fix-round' ? `fix round r${L.round}` : `pass v${L.pass} ${L.type}`}${L.subtype ? ` (${L.subtype})` : ''} → ${L.verdict ?? '—'}${L.outcome ? ` · outcome ${L.outcome}` : ''} · new serious ${serious} · reopened ${L.reopened || 0}`)
say(`STATE: latest resolution v${RN || '—'}${rStatus ? ` (${rStatus}${rCommit ? ` @${rCommit}` : ''})` : ''}`)

const led = openLedger(t.dir)
const openHigh = led ? led.high : []
const openMedium = led ? led.medium : []
if (led) {
  say(`STATE: ledger open 🔴 ${openHigh.length} · open 🟠 ${openMedium.length} (queue threshold ${QUEUE_THRESHOLD})`)
  const unread = led.idRows - led.parsedRows
  if (unread > 0) say(`NOTE: ${unread} of ${count(led.idRows, 'ledger row')} did not parse (severity or status cell off-format) — a row the router cannot read can only make the loop quieter, so read the ledger yourself before acting on this state.`)
}

// 🟠 open at a passing certification roll into the backlog, so they never pre-empt the owner's close.
const closing = atLoopClose(L, N, RN)
// A fix-caused 🟠 re-arms the loop where a plain 🟠 would queue — for as long as it stays open.
const regression = !led ? null : lines.filter(l => l.type === 'verification')
  .flatMap(l => l.findings ?? [])
  .find(f => f && f.fix_generated != null && f.sev === 'medium' && openMedium.includes(f.d))
const armed = []
if (led) {
  if (openHigh.length) armed.push(`${openHigh.length} open 🔴 in the ledger (${openHigh.join(', ')})`)
  if ((L.reopened || 0) > 0) armed.push(`${count(L.reopened, 'reopened fix', 'reopened fixes')} on the latest line`)
  if (regression) armed.push(`a fix-caused 🟠 regression (${regression.d}, from the fix for ${regression.fix_generated})`)
}
// Still-open serious work for the later rows: a ledger'd target counts 🔴 always, 🟠 only as a batch.
const openSerious = led ? seriousCount(led) : serious
const cleanBasis = led ? 'nothing open in the ledger' : '0 new serious'

const state = {
  dir: t.dir, N, RN, rStatus, rCommit, lines, L, serious,
  led, openHigh, openMedium, armed, closing, queued: [], openSerious, cleanBasis,
  owed, lastFixIdx, measured,
  standsDown: standsDown(t.dir),
  cleanDiscovery: (L.type === 'discovery' || L.type === 'delta-discovery') && openSerious === 0 && (L.reopened || 0) === 0 && L.verdict !== 'request-changes',
}
const api = { say, finish, routeFixRound, count }

for (const row of ROWS) {
  if (row.impl !== 'row' || !row.when(state)) continue
  row.answer(state, api)
}

say('ROUTER: no row matched mechanically — decide from the README router with the facts above.')
finish(3, null, null, GATES.noRowMatched)
