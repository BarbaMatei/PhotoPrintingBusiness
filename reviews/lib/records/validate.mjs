// The schema half of the records auditor: the mechanical checks over one target's structured
// records, per rules/metrics-schema.md v3. Checks metrics.jsonl parses and validates,
// new_findings tallies against findings[], fix-round tallies against the resolution's Findings
// rows, review-v<n>.md <-> metrics pairing, worklog event shape, index and track-record mentions,
// cross-target id uniqueness against the id-counter, and the citation-leak scan over source
// comments.
//
// An archived target is validated no further than the cross-target id scan (owner ruling,
// 2026-08-28): its books never change again, so re-reading their shape only kept tolerance code
// alive for records nobody can repair. The one fact still read there is whether it holds a
// certification, because that keeps it under watch and on the track record. Every other check
// below runs on live records only, and reports at one severity.
//
// Nothing here prints or exits: every check reports through the err/warn/info functions the CLI
// passes in, so the CLI owns the output format and the exit code. Three collaborators are injected
// for the same reason, and because a records/ module may not reach into model/ or fix/: `checkSha`
// (git), `versions` (model/target.mjs) and `gates` (fix/handback-gates.mjs).
import { readFileSync, readdirSync, existsSync } from 'node:fs'
import { join } from 'node:path'
import { AREAS, SHA_RE, TARGETLESS, V3_CUTOFF } from './schema.mjs'
import { ids as ledgerIds } from './ledger.mjs'
import { meta as resolutionMeta, tallies as resolutionTallies } from './resolution.mjs'
import { live, readLines as readWorklogLines } from './worklog.mjs'
import { readLines as readMetricsLines, skipReason } from './metrics.mjs'

// The metrics line's field lists, in the order rules/metrics-schema.md prints them: the key sets
// below are derived from them, so a field this validator accepts and a row in the schema table are
// the same fact. The Type and Meaning cells are prose for cli/docs-sync.mjs and nothing reads them.
export const V2_FIELDS = [
  { cell: '`target`', type: 'string', meaning: 'the reviewed unit, e.g. `"043-cloud-storage-provider"`' },
  { cell: '`pass`', type: 'int', meaning: 'review version (matches `review-v<n>.md`); a certification **pair** writes two lines with the same `pass` and subtypes A/B' },
  { cell: '`type`', type: '`"discovery"` \\| `"delta-discovery"` \\| `"verification"`', meaning: 'certification passes are `discovery` (they are full-manifest passes and belong on the decay curve)' },
  { cell: '`subtype`', type: '`"certification-pair-A"` \\| `"certification-pair-B"` \\| `"certification-single"` \\| absent', meaning: 'discovery lines only' },
  { cell: '`date`', type: 'ISO date', meaning: 'when the pass ran' },
  { cell: '`commit`', type: 'string', meaning: 'the commit reviewed' },
  { cell: '`code_tip`', type: 'string, optional', meaning: 'tree tip when it differs from the reviewed commit' },
  { cell: '`delta_base`', type: 'string, delta passes', meaning: 'base commit of the reviewed diff' },
  { cell: '`lenses`', type: 'array of lens keys \\| null', meaning: 'keys only (e.g. `"correctness"`), never prose' },
  { cell: '`verdict`', type: 'string \\| null', meaning: 'the review\'s verdict' },
  { cell: '`outcome`', type: '`"certified"` \\| `"not-certified"` \\| absent', meaning: 'certification lines only' },
  { cell: '`mediums_open_at_close`', type: 'int', meaning: '**required when `outcome: "certified"`** — 🟠 count not `fixed`/`verified` at close (mirrors the index-row rule, calibration 2026-07-29)' },
  { cell: '`new_findings`', type: '`{high, medium, low, cleanup}`', meaning: '**new** problems this pass named (info items count as `cleanup`, note it)' },
  { cell: '`findings`', type: 'array', meaning: '**required on discovery/delta lines** — one entry per canonical finding, see below. **Optional on verification lines** that name new defects: one entry per new defect, carrying only `{d, new, sev, fix_generated, sev_delta?}` — this is where fix lineage gets counted, since fix-caused defects surface mainly in verifications' },
  { cell: '`refinds_identity`', type: 'int', meaning: 'same problem as an earlier finding (reconciler judgment)' },
  { cell: '`reraises_of_decided`', type: 'int', meaning: 'findings re-raising an accepted wont-fix / deferral / dismissal' },
  { cell: '`refuted`', type: 'int', meaning: 'candidate findings recorded as false positives this pass' },
  { cell: '`deferrals_upheld`', type: 'int, optional', meaning: 'prior terminal decisions re-affirmed this pass (canonical name)' },
  { cell: '`disputed`', type: 'historical only', meaning: 'trace-first verification (2026-07-27) can no longer produce it' },
  { cell: '`verified`', type: 'int', meaning: 'findings flipped to `verified` this pass' },
  { cell: '`reopened`', type: 'int', meaning: 'findings reopened this pass' },
  { cell: '`tests`', type: '`{passed, failed}` \\| null', meaning: '**combined** suites (backend + frontend) at the reviewed commit; per-suite splits go in `notes`' },
  { cell: '`cost`', type: '`{agents, tokens, agents_by_stage?}`', meaning: '`tokens` = output tokens the pass\'s workflow(s) reported (never `subagent_tokens`); `agents_by_stage` keys: `lenses, dedup, skeptics_guard, skeptics_trace, reraise_skipped, budget_skipped, approach_checks` — copy from the discovery script\'s `_canonical` line; `approach_checks` counts the synthesis-time pre-checks (v3)' },
  { cell: '`runtime`', type: '`{started, ended}`, v3', meaning: 'ISO timestamps from the loop-driver\'s `pass-launch` / `pass-records-done` worklog stamps' },
  { cell: '`notes`', type: 'string', meaning: 'anything a future analysis will wish it knew' },
]

export const V3_FIX_FIELDS = [
  { cell: '`target` / `type` / `date`', type: 'string / `"fix-round"` / ISO date', meaning: 'as for passes' },
  { cell: '`round`', type: 'int', meaning: 'matches `resolution-v<n>.md` (fix rounds have no `pass`)' },
  { cell: '`base_commit`', type: 'string', meaning: 'the reviewed commit the round answers (review frontmatter)' },
  { cell: '`fixed_commit`', type: 'string \\| null', meaning: 'the resolution\'s `fixed_commit` (null while `in-progress`)' },
  { cell: '`findings`', type: '`{fixed, wont_fix, deferred, disputed, false_positive, open}`', meaning: 'counts from the resolution\'s `## Findings` body table (`in-progress` counts as `open`; `backlog` counts as `deferred`)' },
  { cell: '`tests`', type: '`{invocations, red_runs, green_runs, final: {passed, failed}}`', meaning: 'from `test-run` events; `final` from the last `kind:final` event, null if none' },
  { cell: '`approach_checks`', type: '`{pre_cleared_consumed, run, tokens}`', meaning: 'review-time verdicts used vs checks this round ran (`check-*` events); `tokens` null if unreported' },
  { cell: '`micro_reviews`', type: '`{count, follow_up_fixes}`', meaning: 'composition reviews the round dispatched and the extra fixes they caused — round reviews from the v4 cut-off, per-cluster micro-reviews before it (the field keeps its first name)' },
  { cell: '`cost`', type: '`{agents, tokens}`', meaning: 'subagents this round dispatched; tokens null unless known' },
  { cell: '`runtime`', type: '`{started, ended, active_s, blocked_s, idle_s, blocked: [{reason, s}]}`', meaning: 'see derivation below' },
  { cell: '`notes`', type: 'string', meaning: 'e.g. `pilot`, deviations, what broke' },
]

// Every key a field row names, whole-cell backticks only.
const keysOf = rows => new Set(rows.flatMap(r => [...r.cell.matchAll(/`([a-z_]+)`/g)].map(m => m[1])))

const TYPES = new Set(['discovery', 'delta-discovery', 'verification'])
const SUBTYPES = new Set(['certification-pair-A', 'certification-pair-B', 'certification-single'])
const SEVS = new Set(['high', 'medium', 'low', 'cleanup'])
const VERDICTS = new Set(['confirmed', 'plausible', 'refuted', 're-raise', 'unverified-cleanup', 'unverified-low', 'unverified-over-budget'])
const TOP_KEYS = keysOf(V2_FIELDS)
const STAGE_KEYS = new Set(['lenses', 'dedup', 'skeptics_guard', 'skeptics_trace', 'reraise_skipped', 'budget_skipped', 'approach_checks'])
const FIX_KEYS = keysOf(V3_FIX_FIELDS)
const FIX_TALLY_KEYS = ['fixed', 'wont_fix', 'deferred', 'disputed', 'false_positive', 'open']
const RUNTIME_KEYS = new Set(['started', 'ended', 'active_s', 'blocked_s', 'idle_s', 'blocked'])

function num(v) { return typeof v === 'number' && Number.isFinite(v) }
function numOrNull(v) { return v === null || num(v) }

// Every target with records: live folders first, then archive/. `only` filters by substring, the
// way the CLI's positional arguments read; `all` ignores it, for the cross-target scans.
export function listTargets(reviews, { only = [], all = false } = {}) {
  const out = []
  for (const e of readdirSync(reviews, { withFileTypes: true })) {
    if (!e.isDirectory() || TARGETLESS.has(e.name)) continue
    out.push({ name: e.name, dir: join(reviews, e.name), archived: false })
  }
  const arch = join(reviews, 'archive')
  if (existsSync(arch)) for (const e of readdirSync(arch, { withFileTypes: true })) {
    if (!e.isDirectory() || TARGETLESS.has(e.name)) continue
    out.push({ name: e.name, dir: join(arch, e.name), archived: true })
  }
  return only.length && !all ? out.filter(t => only.some(o => t.name.includes(o))) : out
}

// Certification evidence on an ARCHIVED line, read leniently and validated not at all: a pre-v2
// line records the fact in the retired `certified` field, or in a certification subtype with no
// `outcome` beside it. A recorded failure (`outcome: "not-certified"`) is not a holding.
const archiveHoldsCertification = dir => (readMetricsLines(dir) ?? []).some(({ line: o }) =>
  o && typeof o === 'object' && o.outcome !== 'not-certified' && (
    o.outcome === 'certified' || Boolean(o.certified) ||
    (typeof o.subtype === 'string' && o.subtype.startsWith('certification'))))

export function auditTarget(t, ctx) {
  const { err, warn, info, checkSha, versions, gates, index, track } = ctx
  if (t.archived) {
    // Archiving does not end a certification: the target stays under watch, so the track record
    // must keep listing it. Both certification holders are archived, so this is the one check
    // that still crosses into a closed target's metrics — for that fact and nothing else.
    const tag = `${t.name} (archive)`
    if (archiveHoldsCertification(t.dir)) {
      if (track === null) err(`${tag}: holds a certification but reviews/state/track-record.md is missing`)
      else if (!track.includes(t.name)) err(`${tag}: holds a certification but is not listed in reviews/state/track-record.md`)
    }
    info(`${tag}: closed books — records are not validated; only the cross-target id scan and, for a certification holder, the track-record listing are checked`)
    return
  }
  // Seed lineage (convergence rule, 2026-08-28): which fix round a fix-caused finding is
  // attributed to, plus the component word for per-area convergence accounting.
  const checkSeedKeys = (f, fat) => {
    if (f.seed_round !== undefined && f.seed_round !== null && !num(f.seed_round)) err(`${fat}: seed_round must be a round number or null (a missing value means "not yet measured", never zero)`)
    if (f.area !== undefined && !AREAS.includes(f.area)) err(`${fat}: area "${f.area}" — one of the twelve backlog area words only`)
  }

  const tag = t.name
  const reviewVersions = versions(t.dir, 'review')
  const metricsLines = readMetricsLines(t.dir)
  if (metricsLines === null) {
    info(`${tag}: no metrics.jsonl (${reviewVersions.length} review file(s)) — skipped as a non-code target`)
    return
  }
  const passes = new Map() // pass -> [line objects]
  const fixRounds = new Set() // round numbers with a fix-round line
  const roundDates = new Map() // round -> the fix-round line's date (hand-back gate cut-off)
  const shas = new Map()   // sha -> where
  let holdsCertification = false

  // A fix-round disposition can change after the line was rendered (an owner parks a finding at
  // hand-back). The line is never edited, so a later correction supersedes the cross-check.
  const correctedRoundFields = new Map()
  for (const { line: c } of metricsLines) {
    if (!c?.correction_for || !num(c.correction_for.round)) continue
    if (!correctedRoundFields.has(c.correction_for.round)) correctedRoundFields.set(c.correction_for.round, new Set())
    correctedRoundFields.get(c.correction_for.round).add(c.correction_for.field)
  }

  metricsLines.forEach(({ n, line: o, error }) => {
    const at = `${tag} metrics line ${n}`
    // A line that is not a record object is named here; walking into one would throw a
    // TypeError that says nothing about which line is wrong.
    const skip = skipReason({ line: o, error })
    if (skip) { err(`${at}: ${skip}`); return }

    if (o.correction_for) {
      if (!o.target || !o.date || !o.note) err(`${at}: correction line missing target/date/note`)
      const { pass, round } = o.correction_for
      if (!num(pass) && !num(round)) err(`${at}: correction_for needs a pass number (pass lines) or a round number (fix-round lines)`)
      else if (num(pass) && ![...passes.keys()].includes(pass) && !reviewVersions.includes(pass))
        err(`${at}: correction_for.pass ${pass} matches no known pass`)
      else if (num(round) && !existsSync(join(t.dir, `resolution-v${round}.md`)))
        err(`${at}: correction_for.round ${round} matches no resolution-v${round}.md`)
      return
    }

    if (o.type === 'fix-round') {
      for (const k of ['target', 'round', 'type', 'date', 'base_commit', 'findings', 'runtime']) if (o[k] === undefined) err(`${at}: fix-round line missing required field "${k}"`)
      if (o.target && o.target !== t.name) err(`${at}: target "${o.target}" does not match folder "${t.name}"`)
      for (const k of Object.keys(o)) if (!FIX_KEYS.has(k)) err(`${at}: unknown fix-round field "${k}"`)
      if (o.findings) {
        for (const k of Object.keys(o.findings)) if (!FIX_TALLY_KEYS.includes(k)) err(`${at}: unknown findings key "${k}"`)
        for (const k of FIX_TALLY_KEYS) if (!num(o.findings[k])) err(`${at}: findings.${k} missing or non-numeric`)
      }
      if (o.tests !== undefined && o.tests !== null) {
        for (const k of Object.keys(o.tests)) if (!['invocations', 'red_runs', 'green_runs', 'final'].includes(k)) err(`${at}: unknown tests key "${k}"`)
        for (const k of ['invocations', 'red_runs', 'green_runs']) if (o.tests[k] !== undefined && !num(o.tests[k])) err(`${at}: tests.${k} must be a number`)
        if (o.tests.final !== undefined && o.tests.final !== null && (!num(o.tests.final.passed) || !num(o.tests.final.failed))) err(`${at}: tests.final must be {passed, failed}|null`)
      }
      if (o.runtime) {
        for (const k of Object.keys(o.runtime)) if (!RUNTIME_KEYS.has(k)) err(`${at}: unknown runtime key "${k}"`)
        for (const k of ['active_s', 'blocked_s', 'idle_s']) if (o.runtime[k] !== undefined && !num(o.runtime[k])) err(`${at}: runtime.${k} must be a number`)
        if (o.runtime.blocked !== undefined && (!Array.isArray(o.runtime.blocked) || o.runtime.blocked.some(b => typeof b?.reason !== 'string' || !num(b?.s)))) err(`${at}: runtime.blocked must be [{reason, s}]`)
      }
      if (o.approach_checks) for (const k of Object.keys(o.approach_checks)) {
        if (!['pre_cleared_consumed', 'run', 'tokens'].includes(k)) err(`${at}: unknown approach_checks key "${k}"`)
        else if (k === 'tokens' ? !numOrNull(o.approach_checks[k]) : !num(o.approach_checks[k])) err(`${at}: approach_checks.${k} malformed`)
      }
      if (o.micro_reviews) for (const k of Object.keys(o.micro_reviews)) if (!['count', 'follow_up_fixes'].includes(k) || !num(o.micro_reviews[k])) err(`${at}: micro_reviews.${k} malformed`)
      if (o.cost) for (const k of Object.keys(o.cost)) {
        if (!['agents', 'tokens'].includes(k)) err(`${at}: unknown fix-round cost key "${k}"`)
        else if (!numOrNull(o.cost[k])) err(`${at}: cost.${k} must be number|null`)
      }
      if (num(o.round)) {
        fixRounds.add(o.round)
        if (typeof o.date === 'string') roundDates.set(o.round, o.date)
        const resPath = join(t.dir, `resolution-v${o.round}.md`)
        if (!existsSync(resPath)) err(`${at}: fix-round line for round ${o.round} but no resolution-v${o.round}.md`)
        else if (o.findings && correctedRoundFields.get(o.round)?.has('findings')) {
          warn(`${at}: findings tallies superseded by a later correction line — cross-check skipped`)
        }
        else if (o.findings) {
          const tallies = resolutionTallies(readFileSync(resPath, 'utf8'))
          if (!tallies) warn(`${at}: resolution-v${o.round}.md has no Findings rows — tally cross-check skipped`)
          else for (const k of FIX_TALLY_KEYS) if (num(o.findings[k]) && o.findings[k] !== tallies[k])
            err(`${at}: findings.${k}=${o.findings[k]} but resolution-v${o.round}.md Findings rows count ${tallies[k]}`)
        }
      }
      for (const k of ['base_commit', 'fixed_commit']) {
        const v = o[k]
        if (typeof v === 'string' && SHA_RE.test(v)) shas.set(v, `${at} (${k})`)
      }
      return
    }

    for (const k of ['target', 'pass', 'type', 'date', 'commit']) if (o[k] === undefined) err(`${at}: missing required field "${k}"`)
    if (o.target && o.target !== t.name) err(`${at}: target "${o.target}" does not match folder "${t.name}"`)
    if (num(o.pass)) passes.set(o.pass, [...(passes.get(o.pass) || []), o])

    if (o.type && !TYPES.has(o.type)) err(`${at}: type "${o.type}" not in ${[...TYPES].join('|')}`)
    if (o.subtype !== undefined && !SUBTYPES.has(o.subtype)) err(`${at}: unknown subtype "${o.subtype}"`)

    for (const k of Object.keys(o)) if (!TOP_KEYS.has(k)) err(`${at}: unknown field "${k}"`)

    if (o.lenses !== undefined && o.lenses !== null) {
      if (!Array.isArray(o.lenses)) err(`${at}: lenses must be an array or null`)
      else if (o.lenses.some(l => typeof l !== 'string' || /\s/.test(l))) err(`${at}: lenses must be bare lens keys, not prose`)
    }

    if (o.new_findings) for (const s of SEVS) if (!num(o.new_findings[s])) err(`${at}: new_findings.${s} missing or non-numeric`)
    for (const k of ['refinds_identity', 'reraises_of_decided', 'refuted', 'verified', 'reopened']) if (o[k] !== undefined && !num(o[k])) err(`${at}: ${k} must be a number`)

    if (o.tests !== undefined && o.tests !== null) {
      if (!num(o.tests.passed) || !num(o.tests.failed)) err(`${at}: tests must be {passed, failed}`)
      for (const k of Object.keys(o.tests)) if (!['passed', 'failed'].includes(k)) err(`${at}: unknown tests key "${k}"`)
    }

    if (o.cost !== undefined && o.cost !== null) {
      for (const k of Object.keys(o.cost)) if (!['agents', 'tokens', 'agents_by_stage'].includes(k)) err(`${at}: unknown cost key "${k}"`)
      if (o.cost.tokens !== undefined && !numOrNull(o.cost.tokens)) err(`${at}: cost.tokens must be number|null`)
      if (o.cost.agents !== undefined && !numOrNull(o.cost.agents)) err(`${at}: cost.agents must be number|null`)
      if (o.cost.agents_by_stage) for (const k of Object.keys(o.cost.agents_by_stage))
        if (!STAGE_KEYS.has(k)) err(`${at}: unknown agents_by_stage key "${k}"`)
    }

    if (o.runtime !== undefined && o.runtime !== null) {
      if (typeof o.date === 'string' && o.date < V3_CUTOFF) err(`${at}: runtime predates v3 (${V3_CUTOFF})`)
      for (const k of Object.keys(o.runtime)) if (!['started', 'ended'].includes(k))
        err(`${at}: pass runtime allows only {started, ended} — the full split belongs to fix-round lines`)
    }

    if (o.outcome === 'certified') holdsCertification = true
    if (o.outcome !== undefined) {
      if (!['certified', 'not-certified'].includes(o.outcome)) err(`${at}: outcome must be certified|not-certified`)
      if (!o.subtype) err(`${at}: certification line (outcome set) requires subtype`)
      if (o.outcome === 'certified' && !num(o.mediums_open_at_close)) err(`${at}: outcome certified requires mediums_open_at_close (calibration 2026-07-29)`)
    }

    // Verification lines may carry findings[] for fix lineage (schema, SF-era 2026-08-12):
    // per entry only {d, new, sev, fix_generated, sev_delta} — the lens-stage keys belong
    // to discovery entries.
    if (o.type === 'verification' && o.findings !== undefined) {
      if (!Array.isArray(o.findings)) err(`${at}: findings must be an array`)
      else {
        const tally = { high: 0, medium: 0, low: 0, cleanup: 0 }
        o.findings.forEach((f, i) => {
          const fat = `${at} findings[${i}]`
          if (!/^PPW-\d+$/.test(f.d || '')) err(`${fat}: d must be "PPW-<n>"`)
          if (typeof f.new !== 'boolean') err(`${fat}: new must be boolean`)
          if (!SEVS.has(f.sev)) err(`${fat}: sev "${f.sev}" invalid`)
          if (f.fix_generated !== null && f.fix_generated !== undefined && !/^PPW-\d+$/.test(f.fix_generated)) err(`${fat}: fix_generated must be PPW-<n>|null`)
          if (f.sev_delta !== null && f.sev_delta !== undefined && !/^(high|medium|low|cleanup)->(high|medium|low|cleanup)$/.test(f.sev_delta)) err(`${fat}: sev_delta malformed`)
          checkSeedKeys(f, fat)
          for (const k of Object.keys(f)) if (!['d', 'new', 'sev', 'fix_generated', 'sev_delta', 'seed_round', 'area'].includes(k)) err(`${fat}: unknown key "${k}" on a verification entry`)
          if (f.new === true && SEVS.has(f.sev)) tally[f.sev]++
        })
        if (o.new_findings) for (const s of SEVS) if (num(o.new_findings[s]) && o.new_findings[s] !== tally[s])
          err(`${at}: new_findings.${s}=${o.new_findings[s]} but findings[] has ${tally[s]} new ${s} entries`)
      }
    }

    if (o.type === 'discovery' || o.type === 'delta-discovery') {
      if (!Array.isArray(o.findings)) err(`${at}: ${o.type} line requires findings[]`)
      else {
        const tally = { high: 0, medium: 0, low: 0, cleanup: 0 }
        o.findings.forEach((f, i) => {
          const fat = `${at} findings[${i}]`
          if (!/^F\d+$/.test(f.f || '')) err(`${fat}: f must be "F<n>"`)
          if (!/^PPW-\d+$/.test(f.d || '')) err(`${fat}: d must be "PPW-<n>" (reconcile before appending)`)
          if (typeof f.new !== 'boolean') err(`${fat}: new must be boolean`)
          if (!SEVS.has(f.sev)) err(`${fat}: sev "${f.sev}" invalid`)
          if (!Array.isArray(f.lenses) || !f.lenses.length) err(`${fat}: lenses[] required`)
          if (!num(f.conv) || f.conv < 1) err(`${fat}: conv must be >= 1`)
          if (typeof f.hinted !== 'boolean') err(`${fat}: hinted must be boolean`)
          if (!VERDICTS.has(f.verdict)) err(`${fat}: verdict "${f.verdict}" invalid`)
          if (f.fix_generated !== null && f.fix_generated !== undefined && !/^PPW-\d+$/.test(f.fix_generated)) err(`${fat}: fix_generated must be PPW-<n>|null`)
          if (f.sev_delta !== null && f.sev_delta !== undefined && !/^(high|medium|low|cleanup)->(high|medium|low|cleanup)$/.test(f.sev_delta)) err(`${fat}: sev_delta malformed`)
          checkSeedKeys(f, fat)
          if (f.new === true && SEVS.has(f.sev)) tally[f.sev]++
        })
        if (o.new_findings) for (const s of SEVS) if (num(o.new_findings[s]) && o.new_findings[s] !== tally[s])
          err(`${at}: new_findings.${s}=${o.new_findings[s]} but findings[] has ${tally[s]} new ${s} entries`)
      }
    }

    for (const k of ['commit', 'code_tip', 'delta_base', 'base']) {
      const v = o[k]
      if (typeof v === 'string' && SHA_RE.test(v)) shas.set(v, `${at} (${k})`)
    }
  })

  // review-v<n>.md <-> metrics pairing (+ frontmatter commit collection)
  let missingFm = 0
  for (const v of reviewVersions) {
    if (!passes.has(v)) err(`${tag}: review-v${v}.md has no metrics line`)
    const head = readFileSync(join(t.dir, `review-v${v}.md`), 'utf8').slice(0, 800)
    const cm = /^commit:\s*([0-9a-f]{7,40})\b/m.exec(head)
    if (cm) shas.set(cm[1], `${tag} review-v${v}.md frontmatter`)
    if (!/^pass-type:/m.test(head)) missingFm++
  }
  if (missingFm) warn(`${tag}: ${missingFm} review file(s) missing pass-type frontmatter (pre-convention)`)
  for (const [p, ls] of passes) {
    // Verification passes write no review file (doc-contracts.md, 2026-08-10).
    if (!reviewVersions.includes(p) && !ls.every(l => l.type === 'verification'))
      err(`${tag}: metrics line for pass ${p} has no review-v${p}.md`)
    if (ls.length > 1 && !ls.every(l => l.subtype && l.subtype.startsWith('certification-pair'))) warn(`${tag}: ${ls.length} metrics lines share pass ${p} without pair subtypes`)
  }

  // resolved-no-line is legal in-flight unless closed predates V3_CUTOFF (fix-round lines did not exist before it).
  for (const f of readdirSync(t.dir)) {
    const m = /^resolution-v(\d+)\.md$/.exec(f)
    if (!m || fixRounds.has(Number(m[1]))) continue
    const { status, closed } = resolutionMeta(readFileSync(join(t.dir, f), 'utf8'))
    if (status === 'resolved' && !(closed && closed < V3_CUTOFF))
      warn(`${tag}: resolution-v${m[1]} resolved, no fix-round line yet — unit records pending`)
  }

  // worklog: every event line must parse and carry t + ev; fix-round lines want event backing.
  // The shape check runs over the raw lines — a void erases an event, never its malformed shape.
  const worklogLines = readWorklogLines(t.dir)
  const worklogEvents = []
  if (worklogLines !== null) {
    for (const { n, event: e, error } of worklogLines) {
      if (error) err(`${tag} worklog line ${n}: unparseable JSON (${error.message})`)
      else if (typeof e.t !== 'string' || typeof e.ev !== 'string') err(`${tag} worklog line ${n}: every event needs string "t" and "ev"`)
      else worklogEvents.push(e)
    }
  } else if (fixRounds.size) {
    warn(`${tag}: ${fixRounds.size} fix-round metrics line(s) but no worklog.jsonl — runtime is not backed by events`)
  }

  // Every reader drops voided events: a mis-stamp repaired with a void must not gate hand-back.
  gates(t, tag, roundDates, live(worklogEvents), err)

  // commit resolvability + reachability from a pushed ref
  for (const [sha, where] of shas) {
    const r = checkSha(sha)
    if (!r.resolves) err(`${where}: commit ${sha} does not resolve in this repo`)
    else if (!r.pushed) err(`${where}: commit ${sha} is reachable from NO pushed ref (tag or remote branch) — evidence is single-machine`)
  }

  // certified targets are "under watch" and must be listed in the track record
  if (holdsCertification) {
    if (track === null) err(`${tag}: holds a certification but reviews/state/track-record.md is missing`)
    else if (!track.includes(t.name)) err(`${tag}: holds a certification but is not listed in reviews/state/track-record.md`)
  }

  // index.md mention (warn-level; prose matching is fuzzy)
  const short = t.name.split('-')[0]
  if (index) for (const p of passes.keys()) {
    const re = new RegExp(`\\|\\s*${short}\\s*\\|[^\\n]*v${p}\\b`)
    if (!re.test(index)) warn(`${tag}: no index.md row mentions ${short} v${p}`)
  }
}

// Global id uniqueness: a PPW-<n> has exactly one home ledger, across live + archive, regardless
// of any target filter — duplicate mints from parallel sessions land here.
export function auditIds(targets, { err, counterPath }) {
  const mintHomes = new Map()
  for (const t of targets) {
    const lp = join(t.dir, 'ledger.md')
    if (!existsSync(lp)) continue
    for (const id of ledgerIds(readFileSync(lp, 'utf8'))) {
      const prev = mintHomes.get(id)
      if (prev && prev !== t.name) err(`duplicate id ${id}: ledger rows in both ${prev} and ${t.name} — an id is minted once, globally`)
      else if (prev) err(`duplicate id ${id}: listed twice in ${t.name}'s ledger — one table row per id`)
      else mintHomes.set(id, t.name)
    }
  }
  if (existsSync(counterPath) && mintHomes.size) {
    const next = Number(readFileSync(counterPath, 'utf8').trim())
    const maxUsed = Math.max(...[...mintHomes.keys()].map(k => Number(k.slice(4))))
    if (Number.isFinite(next) && maxUsed >= next) err(`id-counter says the next free id is PPW-${next} but PPW-${maxUsed} is already minted`)
  }
}

// Citation-leak scan: finding-ID / review / ADR references in source COMMENTS. Stays in the
// validator by owner ruling (2026-08-28): one grep per run is the whole-repo backstop the
// pre-commit comment gate, which sees only the lines a commit adds, cannot be.
// v2 scan (2026-07-30): counts comment text only — strings and test names are excluded (test
// names are the review system's accepted leak channel). Spec-file comments count.
// Uses git grep, so tracked files only. Returns the hit lines, or null when the scan could not run.
const PATTERN = String.raw`\b(BUG|SEC|OBS|QUAL|REQ|NEW|CLOUD|INPUT|TEST|DOC|DB|FE|INFO|OPS|PERF|PPW)-[0-9]+\b|\bSF[0-9]+\b|reviews? 0[0-9]{2}-v[0-9]|ADR-0[0-9]{2}|\(D[0-9]{1,3}[,) ]|\(F[0-9]{1,3}[,) ]`

function commentStart(line) {
  const t = line.trimStart()
  if (t.startsWith('*') || t.startsWith('/*')) return line.length - t.length
  let i = -1
  while ((i = line.indexOf('//', i + 1)) !== -1) if (i === 0 || line[i - 1] !== ':') return i
  return -1
}

export function citationScan({ git, info, warn }) {
  let rows = []
  try {
    rows = git(`grep -P -n "${PATTERN}" -- "src/**/*.cs" "src/**/*.ts"`).split(/\r?\n/)
  } catch (e) {
    // git grep exits 1 for "no matches" and 128 for a scan it could not run (no PCRE support,
    // no repository). Only the first is a clean answer; the second must not read as one.
    if (e?.status !== 1) {
      warn(`citation scan skipped — git grep -P could not run (exit ${e?.status ?? '?'}): ${String(e?.message ?? e).split('\n')[0]}`)
      return null
    }
  }
  const re = new RegExp(PATTERN)
  const hits = []
  for (const row of rows) {
    const m = /^(.+?:\d+):(.*)$/.exec(row)
    if (!m) continue
    const cs = commentStart(m[2])
    if (cs !== -1 && re.test(m[2].slice(cs))) hits.push(`${m[1]}: ${m[2].trim()}`)
  }
  const files = new Set(hits.map(h => h.slice(0, h.indexOf(':')))).size
  info(`citation-leak scan (comments only): ${hits.length} occurrence(s) in ${files} file(s) — target is 0 (CLAUDE.md comment rule)`)
  return hits
}
