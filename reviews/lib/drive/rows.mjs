// The router's decision surface as data: one entry per README router row, in the order the router
// tries them (first match wins). Each entry carries the README cells (`state`/`next`, so the table
// in reviews/README.md is generated from this list rather than kept in step by hand) and, for the
// rows the router decides mechanically, a `when` predicate over the state route-next-pass.mjs has
// already computed plus an `answer` that prints the row's lines.
//
// `impl` says how a row is realized:
//   row   — walked by route-next-pass.mjs: `when` decides, `answer` speaks.
//   guard — decided before or around the walk, because it runs while the state is still being
//           assembled (row 1, the terminal row) or wraps every fix-round answer (the design-pass
//           brake, which is the convergence check inside routeFixRound).
//   note  — printed as a NOTE on another row, never routed on its own.
//   prose — a state no target can be in when the router runs (a certification's own verdict).
// Reads nothing; every fact comes in through the state object.
import { QUEUE_THRESHOLD, isBatch } from '../model/queue.mjs'
import { GATES } from './gates.mjs'

// Pass-cost estimates from the recorded history (metrics.jsonl roll-ups, 2026-07-30).
export const COST = {
  'full discovery': '~2.5–3M tokens / ~48 agents (11-lens manifest; lean 5-lens ≈ 1.6M)',
  'delta discovery': '~0.6–1.2M tokens (5-lens cap, 600k output budget script-enforced)',
  'verification': '~60–250k agent tokens + main-agent revert-and-rerun work',
  'lens-coverage discovery': '~0.3–0.5M tokens (one owed lens, full scope, plus dedup/skeptics)',
  'fix round': 'unmetered; scales with finding count (/fix-review)',
  'certification (pair)': '~4.0–4.6M tokens (two blinded full passes)',
  'certification (single)': '~2.9M tokens (re-certification after a small verified fix round)',
}

export const ROWS = [
  {
    name: 'no-review',
    impl: 'guard',
    state: 'No `review-v1.md`',
    next: '**Full discovery**',
  },
  {
    name: 'closed',
    impl: 'guard',
    state: 'The ledger frontmatter carries `closed:`',
    next: '**Terminal** — loop done; the target stays under watch',
  },
  {
    name: 'unit-records-pending',
    impl: 'row',
    state: 'The newest resolution reads `resolved` and `metrics.jsonl` carries no fix-round line for its round',
    next: '**Verification** — runs at the round\'s tip; round + verification are one **reviewed unit** with one set of records and one doc gate. This row outranks every row below it: until the unit\'s records render, both the open ledger rows and the latest metrics line describe work the round has already answered',
    when: s => s.standsDown,
    answer: (s, a) => {
      a.say(`ROUTER: resolution-v${s.RN} resolved, not yet re-reviewed (row 3).`)
      a.finish(0, 'verification (reviewed unit — render records once, after it)', null)
    },
  },
  {
    name: 'armed',
    impl: 'row',
    state: 'An open 🔴 in the ledger · a reopened fix on the latest line · a still-open fix-caused 🟠 regression (its lineage on a verification line\'s `findings[]`)',
    next: '**Fix round now** (quiet counter resets)',
    when: s => Boolean(s.led) && s.armed.length > 0,
    answer: (s, a) => a.routeFixRound(`ROUTER: the loop is armed — ${s.armed.join(' · ')}.`),
  },
  {
    name: 'batch',
    impl: 'row',
    state: '≥ 3 open non-regression 🟠 in the ledger',
    next: '**Fix round** (the batch)',
    when: s => Boolean(s.led) && isBatch(s.openMedium) && !s.closing,
    answer: (s, a) => a.routeFixRound(`ROUTER: batch of ${a.count(s.openMedium.length, 'open medium')} at or over the queue threshold of ${QUEUE_THRESHOLD} (${s.openMedium.join(', ')}).`),
  },
  {
    name: 'queued',
    impl: 'row',
    state: '1–2 open non-regression 🟠',
    next: '**Queued** — proceed as if quiet; the sweep row fires before certification',
    when: s => Boolean(s.led) && s.openMedium.length > 0 && !s.closing,
    // The one row that answers nothing: it records the queue and the walk carries on.
    answer: (s, a) => {
      a.say(`QUEUED: ${s.openMedium.join(', ')} (${s.openMedium.length} below the threshold of ${QUEUE_THRESHOLD})`)
      s.queued = s.openMedium
    },
  },
  {
    name: 'design-pass',
    impl: 'guard',
    gate: GATES.designPass,
    state: 'Two consecutive fix rounds seeded the same component at s ≥ 0.3³',
    next: '**Design pass**³ (owner gate) — further fix rounds there are refused; it intercepts **every** fix-round answer, in the router and in the unattended policy alike (below)',
  },
  {
    name: 'loop-close',
    impl: 'row',
    gate: GATES.loopClose,
    state: 'Certification passed on the latest pass, no post-cert fix round pending',
    next: '**Close the loop** (owner gate) — open 🟠 stand down here and roll into the backlog²',
    when: s => s.closing,
    answer: (s, a) => {
      a.say('ROUTER: certification passed on the latest pass; no post-cert fix round is pending.')
      a.finish(2, null, 'close the loop (record `closed:` in the ledger frontmatter + index row, README note ²) — owner decision', GATES.loopClose)
    },
  },
  {
    name: 'verification-not-clean',
    impl: 'row',
    state: 'Latest line is a verification with reopened fixes, or with serious findings still open',
    next: '**Fix round**',
    when: s => s.L.type === 'verification' && ((s.L.reopened || 0) > 0 || s.openSerious > 0),
    answer: (s, a) => {
      if ((s.L.reopened || 0) > 0) a.routeFixRound('ROUTER: reopened fixes re-arm the loop (last row).')
      a.routeFixRound('ROUTER: verification surfaced new serious findings (last row).')
    },
  },
  {
    name: 'verification-clean',
    impl: 'row',
    gate: GATES.deltaWorthiness,
    state: 'Latest line is a clean verification',
    next: '**Judgment call**: delta-worthy¹ → **delta discovery**; patch-grade → loop **quiet**',
    when: s => s.L.type === 'verification',
    answer: (s, a) => {
      a.say(`ROUTER: verification clean (0 reopened, ${s.cleanBasis}).`)
      a.say('FACTS for the delta-worthiness call (row 4/5): delta-worthy = the fix round fixed a 🔴, added/converted a mechanism, or changed a design; anything else is patch-grade → loop quiet.')
      if (s.owed.length) a.say(`NOTE: lens-coverage debt — never run on this target: ${s.owed.join(', ')}; certification is refused until each has run (a lean lens-coverage discovery clears one, ${COST['lens-coverage discovery']}).`)
      if (!s.measured) a.say(`NOTE: round r${s.lines[s.lastFixIdx].round}'s seed rate is unmeasured — no blind pass has run since it, so "quiet" is unmeasured and certification is refused until a delta discovery measures it (convergence rule, 2026-08-28).`)
      a.finish(3, null, `if delta-worthy → delta discovery (${COST['delta discovery']}); if patch-grade → loop quiet and certification is next, which ALWAYS needs your explicit go-ahead — first attempt = pair (${COST['certification (pair)']}), re-certification after a small verified fix round = single pass (${COST['certification (single)']}), README note ² — but certification stays refused while a NOTE above names owed lenses or an unmeasured seed rate`, GATES.deltaWorthiness)
    },
  },
  {
    name: 'resolution-resolved',
    impl: 'row',
    state: 'Resolution `resolved`, its round\'s records already rendered, not yet re-reviewed',
    next: '**Verification** — the same reviewed unit, entered from a target whose records were rendered before the verification (legacy order)',
    when: s => s.RN >= s.N && s.rStatus === 'resolved',
    answer: (s, a) => {
      a.say(`ROUTER: resolution-v${s.RN} resolved, not yet re-reviewed (row 3).`)
      a.finish(0, 'verification (reviewed unit — render records once, after it)', null)
    },
  },
  {
    name: 'resolution-unresolved',
    impl: 'row',
    state: 'Resolution `open` or `in-progress` answering the latest review',
    next: '**Fix round**',
    when: s => s.RN >= s.N && Boolean(s.rStatus) && s.rStatus !== 'resolved',
    answer: (s, a) => a.routeFixRound(`ROUTER: resolution-v${s.RN} is ${s.rStatus} (row 2).`),
  },
  {
    name: 'open-serious',
    impl: 'row',
    state: 'Open serious findings with no resolution answering the latest review',
    next: '**Fix round**',
    when: s => s.L.verdict === 'request-changes' || s.openSerious > 0,
    answer: (s, a) => a.routeFixRound(`ROUTER: open serious findings with no resolution answering review-v${s.N} (row 2).`),
  },
  {
    name: 'sweep',
    impl: 'row',
    state: 'Loop quiet, any 🟠 still open (queued or not)',
    next: '**Sweep round**, then its verification, then certification',
    when: s => s.cleanDiscovery && s.queued.length > 0,
    answer: (s, a) => a.routeFixRound(`ROUTER: sweep before certification — ${a.count(s.queued.length, 'open medium')} must drain (${s.queued.join(', ')}) before the loop quiets.`),
  },
  {
    name: 'lens-debt',
    impl: 'row',
    state: 'Loop quiet, a manifest lens never ran on the target',
    next: '**Lens-coverage discovery**³ — certification refused until every manifest lens has run',
    when: s => s.cleanDiscovery && s.owed.length > 0,
    answer: (s, a) => {
      a.say(`ROUTER: loop quiet, but these manifest lenses have never run on this target: ${s.owed.join(', ')} — certification refused on lens-coverage debt (audit R5); the owed lens runs first as a lean pass.`)
      a.finish(0, `lens-coverage discovery (${s.owed[0]})`, null)
    },
  },
  {
    name: 'unmeasured-seed',
    impl: 'note',
    state: 'Loop quiet, no blind pass since the last substantive fix round',
    next: '**Delta discovery** — a note and a gate refusal, not a routed row (below); that round\'s seed rate³ is unmeasured, and unmeasured is not quiet',
  },
  {
    name: 'certification',
    impl: 'row',
    gate: GATES.certificationGoAhead,
    state: 'Loop quiet, nothing open, lens coverage complete',
    next: '**Certification**² (owner gate)',
    when: s => s.cleanDiscovery,
    answer: (s, a) => {
      a.say(`ROUTER: discovery-type pass clean (${s.cleanBasis}, 0 reopened) and every manifest lens has run — loop quiet (row 6).`)
      a.finish(2, null, `certification — ALWAYS needs the owner's explicit go-ahead outside an unattended run: first attempt = pair (${COST['certification (pair)']}), re-certification after a small verified fix round = single pass (${COST['certification (single)']}), README note ²`, GATES.certificationGoAhead)
    },
  },
  {
    name: 'certified',
    impl: 'prose',
    state: 'Certification quiet',
    next: '**Certified** — verdict `approved`; loop done',
  },
]

export const walkableRows = () => ROWS.filter(r => r.impl === 'row')
