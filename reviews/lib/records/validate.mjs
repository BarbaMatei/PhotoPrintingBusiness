// The schema half of the records auditor: the mechanical checks over one target's structured
// records, per rules/metrics-schema.md v3. Checks metrics.jsonl parses and validates (strict for
// lines dated on/after V2_CUTOFF, v3 fix-round lines strict from V3_CUTOFF, lenient with
// grandfathered drift before), new_findings tallies against findings[], fix-round tallies against
// the resolution's Findings rows, review-v<n>.md <-> metrics pairing, worklog event shape, index
// and track-record mentions, cross-target id uniqueness against the id-counter, and the
// citation-leak scan over source comments.
//
// Nothing here prints or exits: every check reports through the err/warn/info functions the CLI
// passes in, so the CLI owns the output format and the exit code. Three collaborators are injected
// for the same reason, and because a records/ module may not reach into model/ or fix/: `checkSha`
// (git), `versions` (model/target.mjs) and `gates` (fix/handback-gates.mjs).
import { readFileSync, readdirSync, existsSync } from 'node:fs'
import { join } from 'node:path'
import { AREAS, SHA_RE, TARGETLESS, V2_CUTOFF, V3_CUTOFF } from './schema.mjs'
import { ids as ledgerIds } from './ledger.mjs'
import { meta as resolutionMeta, tallies as resolutionTallies } from './resolution.mjs'
import { live, readLines as readWorklogLines } from './worklog.mjs'
import { readLines as readMetricsLines } from './metrics.mjs'

const TYPES = new Set(['discovery', 'delta-discovery', 'verification'])
const SUBTYPES = new Set(['certification-pair-A', 'certification-pair-B', 'certification-single'])
const SEVS = new Set(['high', 'medium', 'low', 'cleanup'])
const VERDICTS = new Set(['confirmed', 'plausible', 'refuted', 're-raise', 'unverified-cleanup', 'unverified-low', 'unverified-over-budget'])
const TOP_KEYS = new Set(['target', 'pass', 'type', 'subtype', 'date', 'commit', 'code_tip', 'delta_base', 'lenses', 'verdict', 'outcome', 'mediums_open_at_close', 'new_findings', 'findings', 'refinds_identity', 'reraises_of_decided', 'refuted', 'disputed', 'deferrals_upheld', 'verified', 'reopened', 'tests', 'cost', 'runtime', 'notes'])
const STAGE_KEYS = new Set(['lenses', 'dedup', 'skeptics_guard', 'skeptics_trace', 'reraise_skipped', 'budget_skipped', 'approach_checks'])
const LEGACY_TOP = new Set(['base', 'certified', 'deferred_reaffirmed', 'disputed_upheld'])
const FIX_KEYS = new Set(['target', 'round', 'type', 'date', 'base_commit', 'fixed_commit', 'findings', 'tests', 'approach_checks', 'micro_reviews', 'cost', 'runtime', 'notes'])
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
    if (e.isDirectory()) out.push({ name: e.name, dir: join(arch, e.name), archived: true })
  }
  return only.length && !all ? out.filter(t => only.some(o => t.name.includes(o))) : out
}

export function auditTarget(t, ctx) {
  const { err, warn, info, checkSha, versions, gates, index, track } = ctx
  // Seed lineage (convergence rule, 2026-08-28): which fix round a fix-caused finding is
  // attributed to, plus the component word for per-area convergence accounting.
  const checkSeedKeys = (f, fat) => {
    if (f.seed_round !== undefined && f.seed_round !== null && !num(f.seed_round)) err(`${fat}: seed_round must be a round number or null (a missing value means "not yet measured", never zero)`)
    if (f.area !== undefined && !AREAS.includes(f.area)) err(`${fat}: area "${f.area}" — one of the twelve backlog area words only`)
  }

  const tag = t.name + (t.archived ? ' (archive)' : '')
  const strictTier = t.archived ? warn : err // archives never hard-fail on record shape
  const reviewVersions = versions(t.dir, 'review')
  const metricsLines = readMetricsLines(t.dir)
  if (metricsLines === null) {
    info(`${tag}: no metrics.jsonl (${reviewVersions.length} review file(s)) — skipped as a non-code target`)
    return
  }
  const legacyDrift = new Map() // key -> count, aggregated per target
  const drift = k => legacyDrift.set(k, (legacyDrift.get(k) || 0) + 1)
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
    if (error) { err(`${at}: unparseable JSON (${error.message})`); return }

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
      const badFr = (typeof o.date === 'string' && o.date >= V3_CUTOFF) ? err : strictTier
      for (const k of ['target', 'round', 'type', 'date', 'base_commit', 'findings', 'runtime']) if (o[k] === undefined) badFr(`${at}: fix-round line missing required field "${k}"`)
      if (o.target && o.target !== t.name) err(`${at}: target "${o.target}" does not match folder "${t.name}"`)
      for (const k of Object.keys(o)) if (!FIX_KEYS.has(k)) badFr(`${at}: unknown fix-round field "${k}"`)
      if (o.findings) {
        for (const k of Object.keys(o.findings)) if (!FIX_TALLY_KEYS.includes(k)) badFr(`${at}: unknown findings key "${k}"`)
        for (const k of FIX_TALLY_KEYS) if (!num(o.findings[k])) badFr(`${at}: findings.${k} missing or non-numeric`)
      }
      if (o.tests !== undefined && o.tests !== null) {
        for (const k of Object.keys(o.tests)) if (!['invocations', 'red_runs', 'green_runs', 'final'].includes(k)) badFr(`${at}: unknown tests key "${k}"`)
        for (const k of ['invocations', 'red_runs', 'green_runs']) if (o.tests[k] !== undefined && !num(o.tests[k])) badFr(`${at}: tests.${k} must be a number`)
        if (o.tests.final !== undefined && o.tests.final !== null && (!num(o.tests.final.passed) || !num(o.tests.final.failed))) badFr(`${at}: tests.final must be {passed, failed}|null`)
      }
      if (o.runtime) {
        for (const k of Object.keys(o.runtime)) if (!RUNTIME_KEYS.has(k)) badFr(`${at}: unknown runtime key "${k}"`)
        for (const k of ['active_s', 'blocked_s', 'idle_s']) if (o.runtime[k] !== undefined && !num(o.runtime[k])) badFr(`${at}: runtime.${k} must be a number`)
        if (o.runtime.blocked !== undefined && (!Array.isArray(o.runtime.blocked) || o.runtime.blocked.some(b => typeof b?.reason !== 'string' || !num(b?.s)))) badFr(`${at}: runtime.blocked must be [{reason, s}]`)
      }
      if (o.approach_checks) for (const k of Object.keys(o.approach_checks)) {
        if (!['pre_cleared_consumed', 'run', 'tokens'].includes(k)) badFr(`${at}: unknown approach_checks key "${k}"`)
        else if (k === 'tokens' ? !numOrNull(o.approach_checks[k]) : !num(o.approach_checks[k])) badFr(`${at}: approach_checks.${k} malformed`)
      }
      if (o.micro_reviews) for (const k of Object.keys(o.micro_reviews)) if (!['count', 'follow_up_fixes'].includes(k) || !num(o.micro_reviews[k])) badFr(`${at}: micro_reviews.${k} malformed`)
      if (o.cost) for (const k of Object.keys(o.cost)) {
        if (!['agents', 'tokens'].includes(k)) badFr(`${at}: unknown fix-round cost key "${k}"`)
        else if (!numOrNull(o.cost[k])) badFr(`${at}: cost.${k} must be number|null`)
      }
      if (num(o.round)) {
        fixRounds.add(o.round)
        if (typeof o.date === 'string') roundDates.set(o.round, o.date)
        const resPath = join(t.dir, `resolution-v${o.round}.md`)
        if (!existsSync(resPath)) badFr(`${at}: fix-round line for round ${o.round} but no resolution-v${o.round}.md`)
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

    const strict = typeof o.date === 'string' && o.date >= V2_CUTOFF
    const bad = strict ? err : strictTier

    for (const k of ['target', 'pass', 'type', 'date', 'commit']) if (o[k] === undefined) bad(`${at}: missing required field "${k}"`)
    if (o.target && o.target !== t.name) err(`${at}: target "${o.target}" does not match folder "${t.name}"`)
    if (num(o.pass)) passes.set(o.pass, [...(passes.get(o.pass) || []), o])

    if (o.type && !TYPES.has(o.type)) {
      if (!strict && o.type === 'certification') drift('type:"certification" (read as discovery + certification-single)')
      else bad(`${at}: type "${o.type}" not in ${[...TYPES].join('|')}`)
    }
    if (o.subtype !== undefined && !SUBTYPES.has(o.subtype)) bad(`${at}: unknown subtype "${o.subtype}"`)

    for (const k of Object.keys(o)) {
      if (TOP_KEYS.has(k)) continue
      if (!strict && LEGACY_TOP.has(k)) { drift(`field "${k}"`); continue }
      bad(`${at}: unknown field "${k}"`)
    }

    if (o.lenses !== undefined && o.lenses !== null) {
      if (!Array.isArray(o.lenses)) bad(`${at}: lenses must be an array or null`)
      else if (o.lenses.some(l => typeof l !== 'string' || /\s/.test(l))) {
        if (strict) err(`${at}: lenses must be bare lens keys, not prose`)
        else drift('prose in lenses[]')
      }
    }

    if (o.new_findings) for (const s of SEVS) if (!num(o.new_findings[s])) bad(`${at}: new_findings.${s} missing or non-numeric`)
    for (const k of ['refinds_identity', 'reraises_of_decided', 'refuted', 'verified', 'reopened']) if (o[k] !== undefined && !num(o[k])) bad(`${at}: ${k} must be a number`)

    if (o.tests !== undefined && o.tests !== null) {
      if (!num(o.tests.passed) || !num(o.tests.failed)) bad(`${at}: tests must be {passed, failed}`)
      for (const k of Object.keys(o.tests)) if (!['passed', 'failed'].includes(k)) {
        if (!strict && k.startsWith('frontend_')) drift('tests.frontend_* (v2 combines suites)')
        else bad(`${at}: unknown tests key "${k}"`)
      }
    }

    if (o.cost !== undefined && o.cost !== null) {
      for (const k of Object.keys(o.cost)) {
        if (['agents', 'tokens', 'agents_by_stage'].includes(k)) continue
        if (!strict && k === 'subagent_tokens') { drift('cost.subagent_tokens (read as cost.tokens)'); continue }
        bad(`${at}: unknown cost key "${k}"`)
      }
      if (o.cost.tokens !== undefined && !numOrNull(o.cost.tokens)) bad(`${at}: cost.tokens must be number|null`)
      if (o.cost.agents !== undefined && !numOrNull(o.cost.agents)) bad(`${at}: cost.agents must be number|null`)
      if (strict && o.cost.agents_by_stage) for (const k of Object.keys(o.cost.agents_by_stage))
        if (!STAGE_KEYS.has(k)) err(`${at}: unknown agents_by_stage key "${k}"`)
    }

    if (o.runtime !== undefined && o.runtime !== null) {
      if (typeof o.date === 'string' && o.date < V3_CUTOFF) bad(`${at}: runtime predates v3 (${V3_CUTOFF})`)
      for (const k of Object.keys(o.runtime)) if (!['started', 'ended'].includes(k))
        bad(`${at}: pass runtime allows only {started, ended} — the full split belongs to fix-round lines`)
    }

    if (o.outcome === 'certified' || o.certified) holdsCertification = true
    if (o.outcome !== undefined) {
      if (!['certified', 'not-certified'].includes(o.outcome)) bad(`${at}: outcome must be certified|not-certified`)
      if (strict && !o.subtype) err(`${at}: certification line (outcome set) requires subtype`)
      if (o.outcome === 'certified' && strict && !num(o.mediums_open_at_close)) err(`${at}: outcome certified requires mediums_open_at_close (calibration 2026-07-29)`)
    }

    // Verification lines may carry findings[] for fix lineage (schema, SF-era 2026-08-12):
    // per entry only {d, new, sev, fix_generated, sev_delta} — the lens-stage keys belong
    // to discovery entries.
    if (strict && o.type === 'verification' && o.findings !== undefined) {
      if (!Array.isArray(o.findings)) err(`${at}: findings must be an array`)
      else {
        const tally = { high: 0, medium: 0, low: 0, cleanup: 0 }
        o.findings.forEach((f, i) => {
          const fat = `${at} findings[${i}]`
          if (!/^(PPW-\d+|D\d+)$/.test(f.d || '')) err(`${fat}: d must be "PPW-<n>" (pre-2026-08-11 lines carry "D<n>")`)
          if (typeof f.new !== 'boolean') err(`${fat}: new must be boolean`)
          if (!SEVS.has(f.sev)) err(`${fat}: sev "${f.sev}" invalid`)
          if (f.fix_generated !== null && f.fix_generated !== undefined && !/^(PPW-\d+|D\d+)$/.test(f.fix_generated)) err(`${fat}: fix_generated must be PPW-<n>|null (pre-2026-08-11: D<n>)`)
          if (f.sev_delta !== null && f.sev_delta !== undefined && !/^(high|medium|low|cleanup)->(high|medium|low|cleanup)$/.test(f.sev_delta)) err(`${fat}: sev_delta malformed`)
          checkSeedKeys(f, fat)
          for (const k of Object.keys(f)) if (!['d', 'new', 'sev', 'fix_generated', 'sev_delta', 'seed_round', 'area'].includes(k)) err(`${fat}: unknown key "${k}" on a verification entry`)
          if (f.new === true && SEVS.has(f.sev)) tally[f.sev]++
        })
        if (o.new_findings) for (const s of SEVS) if (num(o.new_findings[s]) && o.new_findings[s] !== tally[s])
          err(`${at}: new_findings.${s}=${o.new_findings[s]} but findings[] has ${tally[s]} new ${s} entries`)
      }
    }

    if (strict && (o.type === 'discovery' || o.type === 'delta-discovery')) {
      if (!Array.isArray(o.findings)) err(`${at}: strict ${o.type} line requires findings[]`)
      else {
        const tally = { high: 0, medium: 0, low: 0, cleanup: 0 }
        o.findings.forEach((f, i) => {
          const fat = `${at} findings[${i}]`
          if (!/^F\d+$/.test(f.f || '')) err(`${fat}: f must be "F<n>"`)
          if (!/^(PPW-\d+|D\d+)$/.test(f.d || '')) err(`${fat}: d must be "PPW-<n>" (reconcile before appending; pre-2026-08-11 lines carry "D<n>")`)
          if (typeof f.new !== 'boolean') err(`${fat}: new must be boolean`)
          if (!SEVS.has(f.sev)) err(`${fat}: sev "${f.sev}" invalid`)
          if (!Array.isArray(f.lenses) || !f.lenses.length) err(`${fat}: lenses[] required`)
          if (!num(f.conv) || f.conv < 1) err(`${fat}: conv must be >= 1`)
          if (typeof f.hinted !== 'boolean') err(`${fat}: hinted must be boolean`)
          if (!VERDICTS.has(f.verdict)) err(`${fat}: verdict "${f.verdict}" invalid`)
          if (f.fix_generated !== null && f.fix_generated !== undefined && !/^(PPW-\d+|D\d+)$/.test(f.fix_generated)) err(`${fat}: fix_generated must be PPW-<n>|null (pre-2026-08-11: D<n>)`)
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
    if (!passes.has(v)) strictTier(`${tag}: review-v${v}.md has no metrics line`)
    const head = readFileSync(join(t.dir, `review-v${v}.md`), 'utf8').slice(0, 800)
    const cm = /^commit:\s*([0-9a-f]{7,40})\b/m.exec(head)
    if (cm) shas.set(cm[1], `${tag} review-v${v}.md frontmatter`)
    if (!/^pass-type:/m.test(head)) missingFm++
  }
  if (missingFm) warn(`${tag}: ${missingFm} review file(s) missing pass-type frontmatter (pre-convention)`)
  for (const [p, ls] of passes) {
    // Verification passes write no review file (doc-contracts.md, 2026-08-10).
    if (!reviewVersions.includes(p) && !ls.every(l => l.type === 'verification'))
      strictTier(`${tag}: metrics line for pass ${p} has no review-v${p}.md`)
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
  gates(t, tag, roundDates, live(worklogEvents), strictTier)

  // commit resolvability + reachability from a pushed ref
  for (const [sha, where] of shas) {
    const r = checkSha(sha)
    if (!r.resolves) strictTier(`${where}: commit ${sha} does not resolve in this repo`)
    else if (!r.pushed) strictTier(`${where}: commit ${sha} is reachable from NO pushed ref (tag or remote branch) — evidence is single-machine`)
  }

  // certified targets are "under watch" and must be listed in the track record
  if (holdsCertification) {
    if (track === null) strictTier(`${tag}: holds a certification but reviews/state/track-record.md is missing`)
    else if (!track.includes(t.name)) strictTier(`${tag}: holds a certification but is not listed in reviews/state/track-record.md`)
  }

  // index.md mention (warn-level; prose matching is fuzzy; archive rows use ranges — skipped)
  const short = t.name.split('-')[0]
  if (index && !t.archived) for (const p of passes.keys()) {
    const re = new RegExp(`\\|\\s*${short}\\s*\\|[^\\n]*v${p}\\b`)
    if (!re.test(index)) warn(`${tag}: no index.md row mentions ${short} v${p}`)
  }

  if (legacyDrift.size) {
    const parts = [...legacyDrift].map(([k, n]) => `${k} ×${n}`).join(' · ')
    info(`${tag}: grandfathered v1 drift — ${parts}`)
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
  try {
    let rows = []
    try { rows = git(`grep -P -n "${PATTERN}" -- "src/**/*.cs" "src/**/*.ts"`).split(/\r?\n/) } catch { /* exit 1 = no matches */ }
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
  } catch {
    warn('citation scan skipped (git grep -P unavailable)')
    return null
  }
}
