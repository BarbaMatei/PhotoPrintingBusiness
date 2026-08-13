#!/usr/bin/env node
// Mechanical router for the review loop: reads a target's records and prints the next pass
// per the README router (first matching row wins), with cost estimate and gates.
// State sources (machine-readable only): metrics.jsonl (auditor-validated), resolution
// frontmatter (status/fixed_commit), review file inventory, ledger frontmatter `closed:`.
// It never launches anything — the loop-driver skill executes what this prints.
//
// Usage: node reviews/lib/route-next-pass.mjs [--root <repoRoot>] <target>
// Exit codes: 0 = next pass determined (or target closed) · 2 = OWNER GATE required first ·
//             3 = judgment call needed (facts printed; README router row cited) · 1 = error.
import { readFileSync, readdirSync, existsSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

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
  'fix round': 'unmetered; scales with finding count (/fix-review)',
  'certification (pair)': '~4.0–4.6M tokens (two blinded full passes)',
  'certification (single)': '~2.9M tokens (re-certification after a small verified fix round)',
}

function findDir(name) {
  const hits = []
  for (const base of [REVIEWS, join(REVIEWS, 'archive')]) {
    if (!existsSync(base)) continue
    for (const e of readdirSync(base, { withFileTypes: true })) {
      if (e.isDirectory() && e.name.includes(name) && !['lib', 'experiments', 'archive', 'state', 'rules', 'runbooks', 'notes', 'system', 'templates'].includes(e.name)) {
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
  const block = /^---\r?\n([\s\S]*?)\r?\n---/.exec(readFileSync(file, 'utf8'))
  if (!block) return null
  const m = new RegExp(`^${key}:\\s*(.+?)\\s*$`, 'm').exec(block[1])
  return m ? m[1] : null
}
const out = []
const say = s => out.push(s)
function finish(code, next, gate) {
  if (next) say(`NEXT: ${next}${COST[next.toLowerCase().replace(/ \(.*$/, '')] ? '' : ''}`)
  for (const [k, v] of Object.entries(COST)) if (next && next.toLowerCase().startsWith(k)) say(`COST: ${v}`)
  if (gate) say(`GATE: ${gate}`)
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

const metricsPath = join(t.dir, 'metrics.jsonl')
if (!existsSync(metricsPath)) { say(`STATE: ${reviews.length} review file(s), no metrics.jsonl — non-code target?`); finish(3, null, null) }
const allLines = readFileSync(metricsPath, 'utf8').split(/\r?\n/).filter(l => l.trim()).map(l => JSON.parse(l))
const lines = allLines.filter(l => !l.correction_for)
if (!lines.length) { say('STATE: metrics.jsonl has no usable pass lines (empty or corrections-only) — repair the records first (append-only, per metrics-schema.md)'); finish(3, null, null) }
const L = lines[lines.length - 1]
// Corrections are authoritative but not machine-applied here — surface the ones that
// correct the latest line: round-keyed for a fix-round line, pass-keyed for a pass line.
for (const c of allLines.filter(l => l.correction_for && (
  L.type === 'fix-round'
    ? Number.isFinite(l.correction_for.round) && l.correction_for.round === L.round
    : Number.isFinite(l.correction_for.pass) && l.correction_for.pass === L.pass
))) {
  const which = Number.isFinite(c.correction_for.pass) ? `pass ${c.correction_for.pass}` : `fix round ${c.correction_for.round}`
  say(`NOTE: a correction line is on file for ${which} ("${c.correction_for.field}") — read it before acting on this state.`)
}

const resolutions = readdirSync(t.dir).map(f => /^resolution-v(\d+)\.md$/.exec(f)).filter(Boolean).map(m => Number(m[1]))
const RN = resolutions.length ? Math.max(...resolutions) : 0
const rStatus = RN ? fm(join(t.dir, `resolution-v${RN}.md`), 'status') : null
const rCommit = RN ? fm(join(t.dir, `resolution-v${RN}.md`), 'fixed_commit') : null

const serious = L.new_findings ? (L.new_findings.high || 0) + (L.new_findings.medium || 0) : 0
say(`STATE: latest review v${N} · last ${L.type === 'fix-round' ? `fix round r${L.round}` : `pass v${L.pass} ${L.type}`}${L.subtype ? ` (${L.subtype})` : ''} → ${L.verdict ?? '—'}${L.outcome ? ` · outcome ${L.outcome}` : ''} · new serious ${serious} · reopened ${L.reopened || 0}`)
say(`STATE: latest resolution v${RN || '—'}${rStatus ? ` (${rStatus}${rCommit ? ` @${rCommit}` : ''})` : ''}`)

// Certified with NO post-cert fix round pending → close decision belongs to the owner.
// A resolved post-cert fix round must fall through to the verification branch below (row 3):
// 015's post-cert round is the precedent — its verification reopened 4 fixes.
if (L.outcome === 'certified' && L.pass === N && (!RN || RN < N)) {
  say('ROUTER: certification passed on the latest pass; no post-cert fix round is pending.')
  finish(2, null, `close the loop (record \`closed:\` in the ledger frontmatter + index row, README note ²) — owner decision`)
}

// Latest pass is a verification: its results decide. This branch must run before the
// resolved-resolution branch — verifications write no review file, so N stays at the
// discovery version and RN === N would otherwise re-route to verification forever.
if (L.type === 'verification') {
  if ((L.reopened || 0) > 0) { say('ROUTER: reopened fixes re-arm the loop (last row).'); finish(0, 'fix round', null) }
  if (serious > 0) { say('ROUTER: verification surfaced new serious findings (last row).'); finish(0, 'fix round', null) }
  say('ROUTER: verification clean (0 reopened, 0 new serious).')
  say('FACTS for the delta-worthiness call (row 4/5): delta-worthy = the fix round fixed a 🔴, added/converted a mechanism, or changed a design; anything else is patch-grade → loop quiet.')
  finish(3, null, `if delta-worthy → delta discovery (${COST['delta discovery']}); if patch-grade → loop quiet and certification is next, which ALWAYS needs your explicit go-ahead — first attempt = pair (${COST['certification (pair)']}), re-certification after a small verified fix round = single pass (${COST['certification (single)']}), README note ²`)
}
// A fix round exists for the latest review and is resolved → verification.
if (RN === N && rStatus === 'resolved') {
  say(`ROUTER: resolution-v${N} resolved, not yet re-reviewed (row 3).`)
  finish(0, 'verification', null)
}
if (RN === N && rStatus && rStatus !== 'resolved') {
  say(`ROUTER: resolution-v${N} is ${rStatus} (row 2).`)
  finish(0, 'fix round', null)
}

if (L.verdict === 'request-changes' || serious > 0) {
  say(`ROUTER: open serious findings with no resolution answering review-v${N} (row 2).`)
  finish(0, 'fix round', null)
}
say('ROUTER: no row matched mechanically — decide from the README router with the facts above.')
finish(3, null, null)
