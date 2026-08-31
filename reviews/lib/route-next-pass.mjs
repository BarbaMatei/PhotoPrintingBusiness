#!/usr/bin/env node
// Mechanical router for the review loop: reads a target's records and prints the next pass
// per the README router (first matching row wins), with cost estimate and gates.
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
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { readLedger, openIds, standsDown } from './ledger.mjs'
import { parse, value } from './records/frontmatter.mjs'
import { readMetrics } from './records/metrics.mjs'
import { MANIFEST_LENSES, TARGETLESS } from './records/schema.mjs'

const argv = process.argv.slice(2)
let root = null, arg = null
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === '--root') root = argv[++i]
  else arg = argv[i]
}
const REVIEWS = root ? join(root, 'reviews') : join(dirname(fileURLToPath(import.meta.url)), '..')
if (!arg) { console.error('usage: node reviews/lib/route-next-pass.mjs [--root <repoRoot>] <target>'); process.exit(1) }

// Pass-cost estimates from the recorded history (metrics.jsonl roll-ups, 2026-07-30).
const COST = {
  'full discovery': '~2.5–3M tokens / ~48 agents (11-lens manifest; lean 5-lens ≈ 1.6M)',
  'delta discovery': '~0.6–1.2M tokens (5-lens cap, 600k output budget script-enforced)',
  'verification': '~60–250k agent tokens + main-agent revert-and-rerun work',
  'lens-coverage discovery': '~0.3–0.5M tokens (one owed lens, full scope, plus dedup/skeptics)',
  'fix round': 'unmetered; scales with finding count (/fix-review)',
  'certification (pair)': '~4.0–4.6M tokens (two blinded full passes)',
  'certification (single)': '~2.9M tokens (re-certification after a small verified fix round)',
}
// Open 🟠 below this count wait in the queue; at it or over, they are a fix round of their own.
const QUEUE_THRESHOLD = 3

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

const reviews = readdirSync(t.dir).map(f => /^review-v(\d+)\.md$/.exec(f)).filter(Boolean).map(m => Number(m[1]))
if (!reviews.length) { say('STATE: folder exists, no review-v1.md'); say('ROUTER: row 1'); finish(0, 'full discovery', null) }
const N = Math.max(...reviews)

const metrics = readMetrics(t.dir, { strict: true })
if (!metrics) { say(`STATE: ${reviews.length} review file(s), no metrics.jsonl — non-code target?`); finish(3, null, null, 'no-metrics') }
const { lines, corrections } = metrics
if (!lines.length) { say('STATE: metrics.jsonl has no usable pass lines (empty or corrections-only) — repair the records first (append-only, per metrics-schema.md)'); finish(3, null, null, 'records-broken') }
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
const lensUnion = new Set()
for (const l of lines) if (Array.isArray(l.lenses)) for (const k of l.lenses) lensUnion.add(k)
const owedLenses = MANIFEST_LENSES.filter(k => !lensUnion.has(k))
const isBlind = l => l.type === 'discovery' || l.type === 'delta-discovery'
const substantive = l => l.type === 'fix-round' && (l.findings?.fixed ?? 0) > 0 && (l.tests?.invocations ?? 0) > 0
let lastFixIdx = -1
for (let i = lines.length - 1; i >= 0; i--) if (substantive(lines[i])) { lastFixIdx = i; break }
const seedMeasured = lastFixIdx === -1 || lines.some((l, i) => i > lastFixIdx && isBlind(l))
const seeds = new Map()
for (const l of lines) if (isBlind(l) && Array.isArray(l.findings)) for (const f of l.findings) {
  if (f.new !== true || (f.sev !== 'high' && f.sev !== 'medium') || !Number.isFinite(f.seed_round)) continue
  if (!seeds.has(f.seed_round)) seeds.set(f.seed_round, new Map())
  const m = seeds.get(f.seed_round)
  const a = typeof f.area === 'string' ? f.area : '(unstated)'
  m.set(a, (m.get(a) || 0) + 1)
}
function routeFixRound(reason) {
  say(reason)
  const subs = lines.filter(substantive)
  if (subs.length >= 2) {
    const [r1, r2] = subs.slice(-2)
    if (r2.findings.fixed >= r1.findings.fixed)
      say(`NOTE: fix rounds are not shrinking (r${r1.round}: ${r1.findings.fixed} fixed → r${r2.round}: ${r2.findings.fixed}) — the convergence rule expects each round strictly smaller than the one before.`)
    const rate = r => {
      const m = seeds.get(r.round)
      let n = 0
      if (m) for (const c of m.values()) n += c
      return { s: n / r.findings.fixed, areas: new Set(m ? m.keys() : []) }
    }
    const a1 = rate(r1), a2 = rate(r2)
    if (a1.s >= 0.3 && a2.s >= 0.3) {
      const common = [...a2.areas].filter(a => a !== '(unstated)' && a1.areas.has(a))
      if (common.length) {
        const area = common[0]
        const capped = lines.some(l => l.type === 'fix-round' && typeof l.notes === 'string' && l.notes.includes(`design-pass:${area}`))
        if (!capped) {
          say(`ROUTER: rounds r${r1.round} and r${r2.round} both seeded serious findings in "${area}" at s ≥ 0.3 (s=${a1.s.toFixed(2)}, ${a2.s.toFixed(2)}) — patching declared non-convergent for that component; further fix rounds there are refused (convergence rule, 2026-08-28).`)
          finish(2, null, `design pass for "${area}" — an R1 protocol spec at component level, reimplementation against it, then discovery; recorded as a fix round whose metrics notes carry design-pass:${area}; at most one per component per loop — owner decision`, 'design-pass')
        }
        say(`NOTE: rounds r${r1.round}/r${r2.round} still seed "${area}" at s ≥ 0.3, but its one design pass per loop already ran — the fix round proceeds; raise it with the owner if it seeds again.`)
      }
    }
  }
  finish(0, 'fix round', null)
}

const resolutions = readdirSync(t.dir).map(f => /^resolution-v(\d+)\.md$/.exec(f)).filter(Boolean).map(m => Number(m[1]))
const RN = resolutions.length ? Math.max(...resolutions) : 0
const rStatus = RN ? fm(join(t.dir, `resolution-v${RN}.md`), 'status') : null
const rCommit = RN ? fm(join(t.dir, `resolution-v${RN}.md`), 'fixed_commit') : null

const serious = L.new_findings ? (L.new_findings.high || 0) + (L.new_findings.medium || 0) : 0
say(`STATE: latest review v${N} · last ${L.type === 'fix-round' ? `fix round r${L.round}` : `pass v${L.pass} ${L.type}`}${L.subtype ? ` (${L.subtype})` : ''} → ${L.verdict ?? '—'}${L.outcome ? ` · outcome ${L.outcome}` : ''} · new serious ${serious} · reopened ${L.reopened || 0}`)
say(`STATE: latest resolution v${RN || '—'}${rStatus ? ` (${rStatus}${rCommit ? ` @${rCommit}` : ''})` : ''}`)

const led = readLedger(ledger)
const openHigh = led ? openIds(led.rows, '🔴') : []
const openMedium = led ? openIds(led.rows, '🟠') : []
if (led) {
  say(`STATE: ledger open 🔴 ${openHigh.length} · open 🟠 ${openMedium.length} (queue threshold ${QUEUE_THRESHOLD})`)
  const unread = led.idRows - led.rows.length
  if (unread > 0) say(`NOTE: ${unread} of ${count(led.idRows, 'ledger row')} did not parse (severity or status cell off-format) — a row the router cannot read can only make the loop quieter, so read the ledger yourself before acting on this state.`)
}

// Row 3 outranks the ledger rows and the verification row: both describe work this round answered.
if (standsDown(t.dir)) {
  say(`ROUTER: resolution-v${RN} resolved, not yet re-reviewed (row 3).`)
  finish(0, 'verification (reviewed unit — render records once, after it)', null)
}
// 🟠 open at a passing certification roll into the backlog, so they never pre-empt the owner's close.
const atLoopClose = L.outcome === 'certified' && L.pass === N && (!RN || RN < N)
let queued = []
if (led) {
  // A fix-caused 🟠 re-arms the loop where a plain 🟠 would queue — for as long as it stays open.
  const regression = lines.filter(l => l.type === 'verification')
    .flatMap(l => l.findings ?? [])
    .find(f => f && f.fix_generated != null && f.sev === 'medium' && openMedium.includes(f.d))
  const armed = []
  if (openHigh.length) armed.push(`${openHigh.length} open 🔴 in the ledger (${openHigh.join(', ')})`)
  if ((L.reopened || 0) > 0) armed.push(`${count(L.reopened, 'reopened fix', 'reopened fixes')} on the latest line`)
  if (regression) armed.push(`a fix-caused 🟠 regression (${regression.d}, from the fix for ${regression.fix_generated})`)
  if (armed.length) {
    say(`ROUTER: the loop is armed — ${armed.join(' · ')}.`)
    finish(0, 'fix round', null)
  }
  if (openMedium.length >= QUEUE_THRESHOLD && !atLoopClose) {
    say(`ROUTER: batch of ${count(openMedium.length, 'open medium')} at or over the queue threshold of ${QUEUE_THRESHOLD} (${openMedium.join(', ')}).`)
    finish(0, 'fix round', null)
  }
  if (openMedium.length && !atLoopClose) {
    say(`QUEUED: ${openMedium.join(', ')} (${openMedium.length} below the threshold of ${QUEUE_THRESHOLD})`)
    queued = openMedium
  }
}
// Still-open serious work for the later rows: a ledger'd target counts 🔴 always, 🟠 only as a batch.
const openSerious = led
  ? openHigh.length + (openMedium.length >= QUEUE_THRESHOLD ? openMedium.length : 0)
  : serious
const cleanBasis = led ? 'nothing open in the ledger' : '0 new serious'

// Certified with NO post-cert fix round pending → close decision belongs to the owner.
// A resolved post-cert fix round must fall through to the verification branch below (row 3):
// 015's post-cert round is the precedent — its verification reopened 4 fixes.
if (atLoopClose) {
  say('ROUTER: certification passed on the latest pass; no post-cert fix round is pending.')
  finish(2, null, `close the loop (record \`closed:\` in the ledger frontmatter + index row, README note ²) — owner decision`, 'loop-close')
}

// Latest pass is a verification: its results decide. This branch must run before the
// resolved-resolution branch — verifications write no review file, so N stays at the
// discovery version and RN === N would otherwise re-route to verification forever.
if (L.type === 'verification') {
  if ((L.reopened || 0) > 0) routeFixRound('ROUTER: reopened fixes re-arm the loop (last row).')
  if (openSerious > 0) routeFixRound('ROUTER: verification surfaced new serious findings (last row).')
  say(`ROUTER: verification clean (0 reopened, ${cleanBasis}).`)
  say('FACTS for the delta-worthiness call (row 4/5): delta-worthy = the fix round fixed a 🔴, added/converted a mechanism, or changed a design; anything else is patch-grade → loop quiet.')
  if (owedLenses.length) say(`NOTE: lens-coverage debt — never run on this target: ${owedLenses.join(', ')}; certification is refused until each has run (a lean lens-coverage discovery clears one, ${COST['lens-coverage discovery']}).`)
  if (!seedMeasured) say(`NOTE: round r${lines[lastFixIdx].round}'s seed rate is unmeasured — no blind pass has run since it, so "quiet" is unmeasured and certification is refused until a delta discovery measures it (convergence rule, 2026-08-28).`)
  finish(3, null, `if delta-worthy → delta discovery (${COST['delta discovery']}); if patch-grade → loop quiet and certification is next, which ALWAYS needs your explicit go-ahead — first attempt = pair (${COST['certification (pair)']}), re-certification after a small verified fix round = single pass (${COST['certification (single)']}), README note ² — but certification stays refused while a NOTE above names owed lenses or an unmeasured seed rate`, 'delta-worthiness')
}
// A fix round exists for the latest review and is resolved → verification. RN can exceed N:
// a fix round answering a verification pass raises no review file, so its resolution takes the
// next free number while N stays at the last discovery.
if (RN >= N && rStatus === 'resolved') {
  say(`ROUTER: resolution-v${RN} resolved, not yet re-reviewed (row 3).`)
  finish(0, 'verification (reviewed unit — render records once, after it)', null)
}
if (RN >= N && rStatus && rStatus !== 'resolved') {
  routeFixRound(`ROUTER: resolution-v${RN} is ${rStatus} (row 2).`)
}

if (L.verdict === 'request-changes' || openSerious > 0) {
  routeFixRound(`ROUTER: open serious findings with no resolution answering review-v${N} (row 2).`)
}
if ((L.type === 'discovery' || L.type === 'delta-discovery') && openSerious === 0 && (L.reopened || 0) === 0 && L.verdict !== 'request-changes') {
  if (queued.length) {
    say(`ROUTER: sweep before certification — ${count(queued.length, 'open medium')} must drain (${queued.join(', ')}) before the loop quiets.`)
    finish(0, 'fix round', null)
  }
  if (owedLenses.length) {
    say(`ROUTER: loop quiet, but these manifest lenses have never run on this target: ${owedLenses.join(', ')} — certification refused on lens-coverage debt (audit R5); the owed lens runs first as a lean pass.`)
    finish(0, `lens-coverage discovery (${owedLenses[0]})`, null)
  }
  say(`ROUTER: discovery-type pass clean (${cleanBasis}, 0 reopened) and every manifest lens has run — loop quiet (row 6).`)
  finish(2, null, `certification — ALWAYS needs the owner's explicit go-ahead outside an unattended run: first attempt = pair (${COST['certification (pair)']}), re-certification after a small verified fix round = single pass (${COST['certification (single)']}), README note ²`, 'certification-go-ahead')
}
say('ROUTER: no row matched mechanically — decide from the README router with the facts above.')
finish(3, null, null, 'no-row-matched')
