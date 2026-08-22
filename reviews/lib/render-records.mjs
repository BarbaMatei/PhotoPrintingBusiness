#!/usr/bin/env node
// Records renderer — turns a fix round's worklog + hand-written resolution into the round's
// fix-round metrics line (runtime split computed from worklog events, tallies read from the
// resolution's Findings table) plus a printed index-row suggestion the fixer pastes by hand.
// It writes only metrics.jsonl; every prose file stays a human's.
//
// Runtime covers only the round's paired round-start/round-end spans — a round stopped and
// resumed has several, and the records/gate time between them belongs to no round. Events a
// `void` event names are dropped before anything is measured. An unpairable round stamp aborts
// instead of over-counting, and an append waits for the resolution to read `status: resolved`.
//
// Usage: node reviews/lib/render-records.mjs <target> [--round <n>] [--dry-run]
//          [--in-progress] [--root <repoRoot>]
// Exit 0 = rendered (or dry-run) · 1 = cannot render (missing records, duplicate line, bad
// worklog, unpaired round stamps, or an unresolved resolution without --in-progress).
import { readFileSync, appendFileSync, readdirSync, existsSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const argv = process.argv.slice(2)
let ROOT = null, ROUND = null, DRY = false, ALLOW_UNRESOLVED = false
const rest = []
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === '--root') ROOT = argv[++i]
  else if (argv[i] === '--round') ROUND = Number(argv[++i])
  else if (argv[i] === '--dry-run') DRY = true
  else if (argv[i] === '--in-progress') ALLOW_UNRESOLVED = true
  else rest.push(argv[i])
}
if (!ROOT) ROOT = join(dirname(fileURLToPath(import.meta.url)), '..', '..')
const target = rest[0]
if (!target) fail('usage: render-records.mjs <target> [--round <n>] [--dry-run] [--in-progress]')
const dir = join(ROOT, 'reviews', target)
if (!existsSync(dir)) fail(`no reviews/${target}/`)

function fail(m) { console.error(`ERROR   ${m}`); process.exit(1) }
// Fixers write pre_cleared either as a count or as the list of ids it counts.
const preClearedCount = v => Number.isFinite(v) ? v : Array.isArray(v) ? v.length : 0
const note = m => console.log(`note    ${m}`)

// ---------- locate the round ----------
const resVersions = readdirSync(dir).map(f => /^resolution-v(\d+)\.md$/.exec(f)).filter(Boolean).map(m => Number(m[1]))
if (!resVersions.length) fail(`no resolution-v<n>.md in reviews/${target}/`)
const round = ROUND ?? Math.max(...resVersions)
const resPath = join(dir, `resolution-v${round}.md`)
if (!existsSync(resPath)) fail(`no resolution-v${round}.md`)
const resText = readFileSync(resPath, 'utf8')

// ---------- resolution frontmatter: findings map + status + fixed_commit ----------
const fmEnd = resText.indexOf('\n---', 3)
const fm = resText.startsWith('---') && fmEnd !== -1 ? resText.slice(3, fmEnd) : fail('resolution has no frontmatter')
const findings = new Map() // id -> {status, commit, note}
// New shape: "## Findings" body table. Old shape (grandfathered): frontmatter map.
const fSection = resText.slice(fmEnd).split(/^## /m).find(s => s.startsWith('Findings')) ?? ''
for (const r of fSection.split('\n').filter(l => /^\|\s*(?:D\d+|[A-Za-z]+-\d+)\s*\|/.test(l))) {
  const c = r.split('|').map(x => x.trim())
  findings.set(c[1], { status: c[2] })
}
if (!findings.size) for (const m of fm.matchAll(/^ {2}([A-Za-z]+-?\d+(?:–[A-Za-z]*\d+)?):\s*\{\s*status:\s*([a-z-]+)([^\n]*)/gm) ?? []) {
  const restLine = m[3]
  const commit = (/commit:\s*"([^"]+)"/.exec(restLine) || /commit:\s*([0-9a-f]{7,40})/.exec(restLine))?.[1] ?? null
  const noteTxt = /note:\s*"((?:[^"\\]|\\.)*)"/.exec(restLine)?.[1] ?? null
  findings.set(m[1], { status: m[2], commit, note: noteTxt })
}
if (!findings.size) fail('no "## Findings" table rows and no frontmatter findings map')
const resStatus = /^status:[ \t]*(\S+)/m.exec(fm)?.[1] ?? 'open'
// \s would cross the newline and read the NEXT key's name as the value of an empty one.
const fixedCommitRaw = /^fixed_commit:[ \t]*(\S*)/m.exec(fm)?.[1] || 'null'
const fixedCommit = fixedCommitRaw === 'null' ? null : fixedCommitRaw.replace(/"/g, '')

const TALLY = { fixed: 'fixed', 'wont-fix': 'wont_fix', deferred: 'deferred', backlog: 'deferred', disputed: 'disputed', 'false-positive': 'false_positive' }
const tallies = { fixed: 0, wont_fix: 0, deferred: 0, disputed: 0, false_positive: 0, open: 0 }
for (const { status } of findings.values()) tallies[TALLY[status] ?? 'open']++

// ---------- worklog: void filtering, then this round's paired spans ----------
const wlPath = join(dir, 'worklog.jsonl')
if (!existsSync(wlPath)) fail(`no worklog.jsonl — the v2 fixer contract writes it as it works; nothing to compute runtime from`)
const logged = readFileSync(wlPath, 'utf8').split(/\r?\n/).filter(l => l.trim()).map((l, i) => {
  try { return JSON.parse(l) } catch (e) { fail(`worklog line ${i + 1}: unparseable JSON (${e.message})`) }
})
// A void's "of" matches on a key subset, so one carrying "round" must not hit a same-timestamp stamp for another round.
const sameVal = (a, b) => a === b || (!!a && !!b && typeof a === 'object' && typeof b === 'object' && JSON.stringify(a) === JSON.stringify(b))
const voids = logged.filter(e => e.ev === 'void' && e.of && typeof e.of === 'object' && Object.keys(e.of).length)
const events = logged.filter(e => e.ev !== 'void' && !voids.some(v => Object.keys(v.of).every(k => sameVal(e[k], v.of[k]))))

const voidHint = (ev, t) => `void the wrong stamp: node reviews/lib/wl.mjs ${target} void --json '{"of":{"ev":"${ev}","t":"${t}","round":${round}}}'`
const spans = []
let openStart = null
for (let i = 0; i < events.length; i++) {
  const e = events[i]
  if (e.ev === 'round-start' && e.round === round) {
    if (openStart) fail(`worklog: round-start ${round} at ${e.t} while the round-start at ${openStart.e.t} is still open — one of the two is a mislabel; ${voidHint('round-start', openStart.e.t)}`)
    openStart = { e, i }
  } else if (e.ev === 'round-end' && e.round === round) {
    if (!openStart) {
      const prior = spans.length ? `the previous span already closed at ${events[spans[spans.length - 1].to].t}` : `no round-start ${round} precedes it`
      fail(`worklog: round-end ${round} at ${e.t} closes nothing — ${prior}; ${voidHint('round-end', e.t)}`)
    }
    spans.push({ from: openStart.i, to: i })
    openStart = null
  }
}
if (openStart) {
  spans.push({ from: openStart.i, to: events.length - 1 })
  note('no round-end yet — treating the last event as the current end (in-progress render)')
}
if (!spans.length) fail(`worklog has no round-start for round ${round}`)
const ts = e => Date.parse(e.t)
const spanEvents = spans.map(s => events.slice(s.from, s.to + 1))
const inSpans = spanEvents.flat()
for (const e of inSpans) if (!Number.isFinite(ts(e))) fail(`worklog event with unparseable timestamp: ${JSON.stringify(e)}`)

// ---------- runtime: blocked (gates) / active (gaps <= 30 min) / idle, per span ----------
const CAP_S = 30 * 60 // schema v3: gaps above this with no open gate = nobody at the wheel
const blocked = []
let activeRaw = 0, spanS = 0
for (const seq of spanEvents) {
  const last = seq[seq.length - 1]
  let open = null
  for (const e of seq) {
    if (e.ev === 'gate-open' && !open) open = e
    else if (e.ev === 'gate-closed' && open) { blocked.push({ reason: open.reason ?? e.reason ?? 'unstated', s: Math.round((ts(e) - ts(open)) / 1000) }); open = null }
  }
  if (open) { blocked.push({ reason: `${open.reason ?? 'unstated'} (never closed)`, s: Math.round((ts(last) - ts(open)) / 1000) }); note('a gate-open has no gate-closed — counted to the end of its round span') }
  let inGate = false
  for (let i = 0; i < seq.length - 1; i++) {
    if (seq[i].ev === 'gate-open') inGate = true
    if (seq[i].ev === 'gate-closed') inGate = false
    if (inGate) continue
    const gap = (ts(seq[i + 1]) - ts(seq[i])) / 1000
    if (gap >= 0 && gap <= CAP_S) activeRaw += gap
  }
  spanS += Math.round((ts(last) - ts(seq[0])) / 1000)
}
const blockedS = blocked.reduce((a, b) => a + b.s, 0)
const activeS = Math.round(activeRaw)
const lastSpan = spanEvents[spanEvents.length - 1]
const firstEvent = spanEvents[0][0], lastEvent = lastSpan[lastSpan.length - 1]
const started = firstEvent.t, ended = lastEvent.t
const idleS = Math.max(0, spanS - activeS - blockedS)

// ---------- counters from events ----------
const by = ev => inSpans.filter(e => e.ev === ev)
const testRuns = by('test-run')
const finals = testRuns.filter(e => e.kind === 'final')
const checksReturned = by('check-returned')
const checkTokens = checksReturned.reduce((a, e) => a + (Number.isFinite(e.tokens) ? e.tokens : NaN), 0)
const triage = by('triage-done')[0]
const microD = by('micro-review-dispatched').length
const microFound = by('micro-review-returned').reduce((a, e) => a + (Number.isFinite(e.found) ? e.found : 0), 0)

// ---------- base commit from the review frontmatter ----------
const revPath = join(dir, `review-v${round}.md`)
const baseCommit = existsSync(revPath) ? (/^commit:\s*([0-9a-f]{7,40})\b/m.exec(readFileSync(revPath, 'utf8').slice(0, 800))?.[1] ?? null) : null
if (!baseCommit) note(`review-v${round}.md commit not found — base_commit will be null`)

const line = {
  target, round, type: 'fix-round', date: new Date(ts(lastEvent)).toISOString().slice(0, 10),
  base_commit: baseCommit, fixed_commit: fixedCommit,
  findings: tallies,
  tests: { invocations: testRuns.length, red_runs: testRuns.filter(e => e.kind === 'red').length, green_runs: testRuns.filter(e => e.kind === 'green').length, final: finals.length ? { passed: finals[finals.length - 1].passed ?? null, failed: finals[finals.length - 1].failed ?? null } : null },
  approach_checks: { pre_cleared_consumed: preClearedCount(triage?.pre_cleared), run: by('check-dispatched').length, tokens: Number.isFinite(checkTokens) && checksReturned.length ? checkTokens : null },
  micro_reviews: { count: microD, follow_up_fixes: microFound },
  cost: { agents: by('check-dispatched').length + microD, tokens: null },
  runtime: { started, ended, active_s: activeS, blocked_s: blockedS, idle_s: idleS, blocked },
  notes: resStatus === 'resolved' ? '' : `resolution status: ${resStatus}`,
}

// ---------- duplicate guard + write ----------
const metricsPath = join(dir, 'metrics.jsonl')
const already = existsSync(metricsPath) && readFileSync(metricsPath, 'utf8').split('\n').filter(l => l.trim()).some(l => {
  try { const o = JSON.parse(l); return o.type === 'fix-round' && o.round === round && !o.correction_for } catch { return false }
})

console.log(JSON.stringify(line, null, 2))
console.log(`\nindex.md status suggestion for "${target}": fix round v${round} ${resStatus} — ${tallies.fixed} fixed / ${tallies.open} open, active ${(activeS / 60).toFixed(0)} min, blocked ${(blockedS / 60).toFixed(0)} min`)
if (DRY) { note('dry-run: nothing written'); process.exit(0) }
if (resStatus !== 'resolved' && !ALLOW_UNRESOLVED) fail(`resolution-v${round}.md reads status: ${resStatus} — finish the round or pass --in-progress (--dry-run renders at any status)`)
if (already) fail(`metrics.jsonl already has a fix-round line for round ${round} — append a correction line instead (schema: Corrections)`)
const prev = existsSync(metricsPath) ? readFileSync(metricsPath, 'utf8') : ''
appendFileSync(metricsPath, (prev && !prev.endsWith('\n') ? '\n' : '') + JSON.stringify(line) + '\n')
note(`appended fix-round line for round ${round} — now run: node reviews/lib/records-auditor.mjs ${target}`)
