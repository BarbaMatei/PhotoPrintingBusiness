#!/usr/bin/env node
// Records auditor — mechanical checks over reviews/ structured records, per metrics-schema.md v3.
// Checks: metrics.jsonl parses + validates (strict for lines dated >= 2026-07-30, v3 fix-round
// lines strict from 2026-08-03, lenient with grandfathered drift before); new_findings tallies
// vs findings[]; fix-round findings tallies vs the resolution frontmatter; review-v<n>.md <->
// metrics pairing; worklog.jsonl event shape; every cited commit resolves AND is reachable from
// a pushed ref (tag or remote branch); correction lines reference real passes; index.md mentions
// each pass; citation-leak count.
// Prose bodies (ledgers, resolutions) are NOT checked — numbers there stay a human's job.
//
// Usage: node reviews/lib/records-auditor.mjs [--root <repoRoot>] [target ...]
// Exit 0 = no errors (warnings allowed) · 1 = errors.
import { readFileSync, readdirSync, existsSync } from 'node:fs'
import { execSync } from 'node:child_process'
import { join, dirname, relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { REVIEWS as LIVE_REVIEWS, INDEX as INDEX_FILE, TRACK_RECORD, ID_COUNTER } from './paths.mjs'

const argv = process.argv.slice(2)
let ROOT = null
const only = []
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === '--root') ROOT = argv[++i]
  else only.push(argv[i])
}
if (!ROOT) ROOT = join(dirname(fileURLToPath(import.meta.url)), '..', '..')
const REVIEWS = join(ROOT, 'reviews')

const V2_CUTOFF = '2026-07-30'
const V3_CUTOFF = '2026-08-03' // fix-round lines + runtime fields exist only from here
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

const errors = [], warnings = [], infos = []
const err = m => errors.push(m)
const warn = m => warnings.push(m)
const info = m => infos.push(m)

function git(cmd) {
  return execSync(`git ${cmd}`, { cwd: ROOT, stdio: ['ignore', 'pipe', 'pipe'] }).toString().trim()
}
const shaCache = new Map()
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
const SHA_RE = /^[0-9a-f]{7,40}$/

function listTargets(all = false) {
  const out = []
  for (const e of readdirSync(REVIEWS, { withFileTypes: true })) {
    if (!e.isDirectory() || ['lib', 'experiments', 'archive', 'state', 'rules', 'runbooks', 'notes', 'system', 'templates'].includes(e.name)) continue
    out.push({ name: e.name, dir: join(REVIEWS, e.name), archived: false })
  }
  const arch = join(REVIEWS, 'archive')
  if (existsSync(arch)) for (const e of readdirSync(arch, { withFileTypes: true })) {
    if (e.isDirectory()) out.push({ name: e.name, dir: join(arch, e.name), archived: true })
  }
  return only.length && !all ? out.filter(t => only.some(o => t.name.includes(o))) : out
}

function num(v) { return typeof v === 'number' && Number.isFinite(v) }
function numOrNull(v) { return v === null || num(v) }

// Counts statuses from a resolution's "## Findings" body table (frontmatter map on old rounds).
function resolutionTallies(text) {
  const t = { fixed: 0, wont_fix: 0, deferred: 0, disputed: 0, false_positive: 0, open: 0 }
  const end = text.indexOf('\n---', 3)
  const fm = text.startsWith('---') && end !== -1 ? text.slice(3, end) : ''
  let found = false
  // New shape: "## Findings" body table. Old shape (grandfathered targets): frontmatter map.
  const section = text.split(/^## /m).find(s => s.startsWith('Findings')) ?? ''
  const rowRe = /^\|\s*(?:D\d+|[A-Za-z]+-\d+)\s*\|\s*([a-z-]+)\s*\|/gm
  const mapRe = /^ {2}[A-Za-z]+-?\d+:\s*\{\s*status:\s*([a-z-]+)/gm
  const matches = [...section.matchAll(rowRe)]
  for (const m of matches.length ? matches : fm.matchAll(mapRe)) {
    found = true
    const s = m[1]
    if (s === 'fixed') t.fixed++
    else if (s === 'wont-fix') t.wont_fix++
    else if (s === 'deferred' || s === 'backlog') t.deferred++ // backlog = the deferred bucket in metrics tallies
    else if (s === 'disputed') t.disputed++
    else if (s === 'false-positive') t.false_positive++
    else t.open++ // open, in-progress, anything non-terminal
  }
  return found ? t : null
}

function auditTarget(t) {
  const tag = t.name + (t.archived ? ' (archive)' : '')
  const strictTier = t.archived ? warn : err // archives never hard-fail on record shape
  const reviewVersions = readdirSync(t.dir).map(f => /^review-v(\d+)\.md$/.exec(f)).filter(Boolean).map(m => Number(m[1]))
  const metricsPath = join(t.dir, 'metrics.jsonl')
  if (!existsSync(metricsPath)) {
    info(`${tag}: no metrics.jsonl (${reviewVersions.length} review file(s)) — skipped as a non-code target`)
    return
  }
  const legacyDrift = new Map() // key -> count, aggregated per target
  const drift = k => legacyDrift.set(k, (legacyDrift.get(k) || 0) + 1)
  const lines = readFileSync(metricsPath, 'utf8').split('\n').filter(l => l.trim())
  const passes = new Map() // pass -> [line objects]
  const fixRounds = new Set() // round numbers with a fix-round line
  const shas = new Map()   // sha -> where
  let holdsCertification = false

  // A fix-round disposition can change after the line was rendered (an owner parks a finding at
  // hand-back). The line is never edited, so a later correction supersedes the cross-check.
  const correctedRoundFields = new Map()
  for (const raw of lines) {
    let c
    try { c = JSON.parse(raw) } catch { continue }
    if (!c?.correction_for || !num(c.correction_for.round)) continue
    if (!correctedRoundFields.has(c.correction_for.round)) correctedRoundFields.set(c.correction_for.round, new Set())
    correctedRoundFields.get(c.correction_for.round).add(c.correction_for.field)
  }

  lines.forEach((raw, idx) => {
    const at = `${tag} metrics line ${idx + 1}`
    let o
    try { o = JSON.parse(raw) } catch (e) { err(`${at}: unparseable JSON (${e.message})`); return }

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
          if (!/^(PPW-\d+)$/.test(f.d || '')) err(`${fat}: d must be "PPW-<n>"`)
          if (typeof f.new !== 'boolean') err(`${fat}: new must be boolean`)
          if (!SEVS.has(f.sev)) err(`${fat}: sev "${f.sev}" invalid`)
          if (f.fix_generated !== null && f.fix_generated !== undefined && !/^PPW-\d+$/.test(f.fix_generated)) err(`${fat}: fix_generated must be PPW-<n>|null`)
          for (const k of Object.keys(f)) if (!['d', 'new', 'sev', 'fix_generated', 'sev_delta'].includes(k)) err(`${fat}: unknown key "${k}" on a verification entry`)
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
  for (const v of reviewVersions.sort((a, b) => a - b)) {
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

  // worklog: every event line must parse and carry t + ev; fix-round lines want event backing
  const worklogPath = join(t.dir, 'worklog.jsonl')
  if (existsSync(worklogPath)) {
    readFileSync(worklogPath, 'utf8').split('\n').filter(l => l.trim()).forEach((raw, i) => {
      try {
        const e = JSON.parse(raw)
        if (typeof e.t !== 'string' || typeof e.ev !== 'string') err(`${tag} worklog line ${i + 1}: every event needs string "t" and "ev"`)
      } catch (e) { err(`${tag} worklog line ${i + 1}: unparseable JSON (${e.message})`) }
    })
  } else if (fixRounds.size) {
    warn(`${tag}: ${fixRounds.size} fix-round metrics line(s) but no worklog.jsonl — runtime is not backed by events`)
  }

  // commit resolvability + reachability from a pushed ref
  for (const [sha, where] of shas) {
    const r = checkSha(sha)
    if (!r.resolves) strictTier(`${where}: commit ${sha} does not resolve in this repo`)
    else if (!r.pushed) strictTier(`${where}: commit ${sha} is reachable from NO pushed ref (tag or remote branch) — evidence is single-machine`)
  }

  // certified targets are "under watch" and must be listed in the track record
  if (holdsCertification) {
    if (TRACK === null) strictTier(`${tag}: holds a certification but reviews/state/track-record.md is missing`)
    else if (!TRACK.includes(t.name)) strictTier(`${tag}: holds a certification but is not listed in reviews/state/track-record.md`)
  }

  // index.md mention (warn-level; prose matching is fuzzy; archive rows use ranges — skipped)
  const short = t.name.split('-')[0]
  if (INDEX && !t.archived) for (const p of passes.keys()) {
    const re = new RegExp(`\\|\\s*${short}\\s*\\|[^\\n]*v${p}\\b`)
    if (!re.test(INDEX)) warn(`${tag}: no index.md row mentions ${short} v${p}`)
  }

  if (legacyDrift.size) {
    const parts = [...legacyDrift].map(([k, n]) => `${k} ×${n}`).join(' · ')
    info(`${tag}: grandfathered v1 drift — ${parts}`)
  }
}

// Path constants come from paths.mjs; rebased onto REVIEWS so --root keeps working.
const underRoot = p => join(REVIEWS, relative(LIVE_REVIEWS, p))
const INDEX = existsSync(underRoot(INDEX_FILE)) ? readFileSync(underRoot(INDEX_FILE), 'utf8') : null
if (!INDEX) warn('reviews/state/index.md not found — index pairing skipped')
const TRACK = existsSync(underRoot(TRACK_RECORD)) ? readFileSync(underRoot(TRACK_RECORD), 'utf8') : null

for (const t of listTargets()) auditTarget(t)

// Global id uniqueness: a PPW-<n> has exactly one home ledger, across live + archive,
// regardless of any target filter — duplicate mints from parallel sessions land here.
{
  const mintHomes = new Map()
  for (const t of listTargets(true)) {
    const lp = join(t.dir, 'ledger.md')
    if (!existsSync(lp)) continue
    for (const m of readFileSync(lp, 'utf8').matchAll(/^\|\s*(PPW-\d+)\s*\|/gm)) {
      const prev = mintHomes.get(m[1])
      if (prev && prev !== t.name) err(`duplicate id ${m[1]}: ledger rows in both ${prev} and ${t.name} — an id is minted once, globally`)
      else if (prev) err(`duplicate id ${m[1]}: listed twice in ${t.name}'s ledger — one table row per id`)
      else mintHomes.set(m[1], t.name)
    }
  }
  const counterPath = underRoot(ID_COUNTER)
  if (existsSync(counterPath) && mintHomes.size) {
    const next = Number(readFileSync(counterPath, 'utf8').trim())
    const maxUsed = Math.max(...[...mintHomes.keys()].map(k => Number(k.slice(4))))
    if (Number.isFinite(next) && maxUsed >= next) err(`id-counter says the next free id is PPW-${next} but PPW-${maxUsed} is already minted`)
  }
}

// Citation-leak scan: finding-ID / review / ADR references in source COMMENTS.
// v2 scan (2026-07-30): counts comment text only — strings and test names are excluded (test
// names are the review system's accepted leak channel). Spec-file comments count.
// `--citations` lists every hit. Uses git grep (tracked files only).
function commentStart(line) {
  const t = line.trimStart()
  if (t.startsWith('*') || t.startsWith('/*')) return line.length - t.length
  let i = -1
  while ((i = line.indexOf('//', i + 1)) !== -1) if (i === 0 || line[i - 1] !== ':') return i
  return -1
}
if (!only.length || argv.includes('--citations')) {
  const pat = String.raw`\b(BUG|SEC|OBS|QUAL|REQ|NEW|CLOUD|INPUT|TEST|DOC|DB|FE|INFO|OPS|PERF|PPW)-[0-9]+\b|\bSF[0-9]+\b|reviews? 0[0-9]{2}-v[0-9]|ADR-0[0-9]{2}|\(D[0-9]{1,3}[,) ]|\(F[0-9]{1,3}[,) ]`
  try {
    let rows = []
    try { rows = git(`grep -P -n "${pat}" -- "src/**/*.cs" "src/**/*.ts"`).split(/\r?\n/) } catch { /* exit 1 = no matches */ }
    const re = new RegExp(pat)
    const hits = []
    for (const row of rows) {
      const m = /^(.+?:\d+):(.*)$/.exec(row)
      if (!m) continue
      const cs = commentStart(m[2])
      if (cs !== -1 && re.test(m[2].slice(cs))) hits.push(`${m[1]}: ${m[2].trim()}`)
    }
    if (argv.includes('--citations')) for (const h of hits) console.log(`CITE    ${h}`)
    const files = new Set(hits.map(h => h.slice(0, h.indexOf(':')))).size
    info(`citation-leak scan (comments only): ${hits.length} occurrence(s) in ${files} file(s) — target is 0 (CLAUDE.md comment rule)`)
  } catch { warn('citation scan skipped (git grep -P unavailable)') }
}

for (const m of errors) console.log(`ERROR   ${m}`)
for (const m of warnings) console.log(`WARN    ${m}`)
for (const m of infos) console.log(`note    ${m}`)
console.log(`\n${errors.length} error(s), ${warnings.length} warning(s).`)
process.exit(errors.length ? 1 : 0)
